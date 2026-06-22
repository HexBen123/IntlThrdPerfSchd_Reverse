using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;
using System.Timers;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.Win32;
using OpenLibSys;

namespace IntlThrdPerfSchd
{
	public class Service1 : ServiceBase
	{
		public class SchedulerThreadData
		{
			public float InstructionCount { get; set; }

			public float Ipc { get; set; }

			public float BranchMiss { get; set; }

			public float CacheMiss { get; set; }

			public float Priority { get; set; }

			public int ArtificialDecision { get; set; }

			public float[] ToArray()
			{
				return new float[5] { InstructionCount, Ipc, BranchMiss, CacheMiss, Priority };
			}

			public string DecisionStr()
			{
				if (ArtificialDecision != 1)
				{
					return "小核";
				}
				return "大核";
			}
		}

		public class SchedulerDataset
		{
			private readonly List<SchedulerThreadData> _data = new List<SchedulerThreadData>();

			private float[] _mean = new float[5];

			private float[] _std = new float[5];

			public int Size => _data.Count;

			public float[] Mean => _mean;

			public float[] Std => _std;

			public void GenerateData(int count = 1000)
			{
				Random random = new Random(42);
				for (int i = 0; i < count; i++)
				{
					SchedulerThreadData schedulerThreadData = new SchedulerThreadData
					{
						InstructionCount = random.Next(1000, 100000),
						Ipc = (float)(random.NextDouble() * 3.0),
						BranchMiss = random.Next(0, 10000),
						CacheMiss = random.Next(0, 5000),
						Priority = random.Next(1, 10)
					};
					float num = schedulerThreadData.Priority * 0.3f + schedulerThreadData.InstructionCount / 100000f * 0.2f + schedulerThreadData.Ipc * 0.3f + (schedulerThreadData.BranchMiss + schedulerThreadData.CacheMiss) / 10000f * 0.2f;
					schedulerThreadData.ArtificialDecision = (((double)num > 2.8) ? 1 : 0);
					_data.Add(schedulerThreadData);
				}
				ComputeNorm();
				Console.WriteLine($"生成 {count} 条数据 (大核 {_data.Count((SchedulerThreadData d) => d.ArtificialDecision == 1)}, 小核 {_data.Count((SchedulerThreadData d) => d.ArtificialDecision == 0)})");
			}

			private void ComputeNorm()
			{
				if (_data.Count == 0)
				{
					return;
				}
				float[][] source = _data.Select((SchedulerThreadData d) => d.ToArray()).ToArray();
				int i;
				for (i = 0; i < 5; i++)
				{
					_mean[i] = source.Average((float[] x) => x[i]);
					_std[i] = (float)Math.Sqrt(source.Average((float[] x) => (x[i] - _mean[i]) * (x[i] - _mean[i])));
					if (_std[i] < 1E-06f)
					{
						_std[i] = 1f;
					}
				}
			}

			public (float[] input, int label) GetItem(int idx)
			{
				float[] array = _data[idx].ToArray();
				float[] array2 = new float[5];
				for (int i = 0; i < 5; i++)
				{
					array2[i] = (array[i] - _mean[i]) / _std[i];
				}
				return (input: array2, label: _data[idx].ArtificialDecision);
			}
		}

		public static class Mat
		{
			public static float[,] Rand(int r, int c, float s)
			{
				float[,] array = new float[r, c];
				Random random = new Random();
				for (int i = 0; i < r; i++)
				{
					for (int j = 0; j < c; j++)
					{
						array[i, j] = (float)(random.NextDouble() * 2.0 - 1.0) * s;
					}
				}
				return array;
			}
		}

		public class CrossAttentionScheduler
		{
			public class ScheduleRecord
			{
				public float[] Features { get; set; } = Array.Empty<float>();

				public float[] RawFeatures { get; set; } = Array.Empty<float>();

				public int Decision { get; set; }

				public float[] Attention { get; set; } = Array.Empty<float>();

				public float[] Probabilities { get; set; } = Array.Empty<float>();

				public long Timestamp { get; set; }

				public float Reward { get; set; }

				public bool HasReward { get; set; }
			}

			private const int DM = 16;

			private const int IN = 5;

			private const int FF = 32;

			private const int CoreTypes = 2;

			private readonly float[,] _emb;

			private readonly float[,] _coreK;

			private readonly float[,] _coreV;

			private readonly float[,] _ff1;

			private readonly float[,] _ff2;

			private readonly float[,] _out;

			private float[] _mean = new float[5];

			private float[] _std = new float[5];

			private int _sampleCount;

			private float[] _runningMean = new float[5];

			private float[] _runningM2 = new float[5];

			private int _totalPredictions;

			private int _correctPredictions;

			private bool _isModelReady;

			private float[] _lastAttention = new float[2];

			private readonly List<ScheduleRecord> _currentWindow = new List<ScheduleRecord>();

			private readonly List<ScheduleRecord> _replayBuffer = new List<ScheduleRecord>();

			private readonly int _replayBufferSize = 10000;

			private readonly long _windowMs = 1000L;

			private float _baselineMetric;

			private float _baselineDecay = 0.99f;

			private float _explorationRate = 0.1f;

			private float _explorationDecay = 0.999f;

			private float _minExplorationRate = 0.01f;

			private readonly Random _rnd = new Random();

			private bool _energyLearningEnabled;

			private readonly List<(long timestamp, float metric)> _metricHistory = new List<(long, float)>();

			private readonly long _metricWindowMs = 300000L;

			private int _scheduleCount;

			private int _energyLearningCount;

			private int _explorationCount;

			public CrossAttentionScheduler()
			{
				float s = 0.2f;
				_emb = Mat.Rand(5, 16, s);
				_coreK = Mat.Rand(2, 16, 0.1f);
				_coreV = Mat.Rand(2, 16, 0.1f);
				_ff1 = Mat.Rand(16, 32, s);
				_ff2 = Mat.Rand(32, 16, s);
				_out = Mat.Rand(16, 2, s);
			}

			public void SetNormalization(float[] mean, float[] std)
			{
				for (int i = 0; i < 5; i++)
				{
					_mean[i] = mean[i];
					_std[i] = std[i];
				}
			}

			private float[] Normalize(float[] raw)
			{
				float[] array = new float[5];
				for (int i = 0; i < 5; i++)
				{
					array[i] = ((_std[i] > 0f) ? ((raw[i] - _mean[i]) / _std[i]) : raw[i]);
				}
				return array;
			}

			public (int pred, float[] p) Forward(float[] x)
			{
				float[] array = new float[16];
				for (int i = 0; i < 16; i++)
				{
					for (int j = 0; j < 5; j++)
					{
						array[i] += x[j] * _emb[j, i];
					}
				}
				float[] array2 = new float[2];
				for (int k = 0; k < 2; k++)
				{
					array2[k] = 0f;
					for (int l = 0; l < 16; l++)
					{
						array2[k] += array[l] * _coreK[k, l];
					}
				}
				float[] array3 = new float[2];
				float num = Math.Max(array2[0], array2[1]);
				float num2 = 0f;
				for (int m = 0; m < 2; m++)
				{
					array3[m] = (float)Math.Exp(array2[m] - num);
					num2 += array3[m];
				}
				for (int n = 0; n < 2; n++)
				{
					array3[n] /= num2;
				}
				if (float.IsNaN(array3[0]) || float.IsNaN(array3[1]))
				{
					array3[0] = 0.5f;
					array3[1] = 0.5f;
				}
				_lastAttention = array3;
				float[] array4 = new float[16];
				for (int num3 = 0; num3 < 16; num3++)
				{
					for (int num4 = 0; num4 < 2; num4++)
					{
						array4[num3] += array3[num4] * _coreV[num4, num3];
					}
				}
				float[] array5 = new float[32];
				for (int num5 = 0; num5 < 32; num5++)
				{
					for (int num6 = 0; num6 < 16; num6++)
					{
						array5[num5] += Math.Max(0f, array4[num6] * _ff1[num6, num5]);
					}
				}
				float[] array6 = new float[16];
				for (int num7 = 0; num7 < 16; num7++)
				{
					for (int num8 = 0; num8 < 32; num8++)
					{
						array6[num7] += array5[num8] * _ff2[num8, num7];
					}
				}
				for (int num9 = 0; num9 < 16; num9++)
				{
					array6[num9] += array[num9];
				}
				float[] array7 = new float[2];
				for (int num10 = 0; num10 < 2; num10++)
				{
					for (int num11 = 0; num11 < 16; num11++)
					{
						array7[num10] += array6[num11] * _out[num11, num10];
					}
				}
				float[] array8 = new float[2];
				num = Math.Max(array7[0], array7[1]);
				num2 = 0f;
				for (int num12 = 0; num12 < 2; num12++)
				{
					array8[num12] = (float)Math.Exp(array7[num12] - num);
					num2 += array8[num12];
				}
				for (int num13 = 0; num13 < 2; num13++)
				{
					array8[num13] /= num2;
				}
				if (float.IsNaN(array8[0]) || float.IsNaN(array8[1]))
				{
					array8[0] = 0.5f;
					array8[1] = 0.5f;
				}
				return (pred: (!(array8[0] > array8[1])) ? 1 : 0, p: array8);
			}

			public void Train(SchedulerDataset ds, int epochs, float lr)
			{
				Random rnd = new Random(123);
				float num = 0f;
				_mean = ds.Mean;
				_std = ds.Std;
				for (int i = 0; i < epochs; i++)
				{
					float num2 = 0f;
					int num3 = 0;
					float num4 = lr * (float)Math.Pow(0.949999988079071, i);
					List<int> list = (from _ in Enumerable.Range(0, ds.Size)
						orderby rnd.Next()
						select _).ToList();
					for (int num5 = 0; num5 < ds.Size; num5++)
					{
						(float[] input, int label) item = ds.GetItem(list[num5]);
						float[] item2 = item.input;
						int item3 = item.label;
						(int pred, float[] p) tuple = Forward(item2);
						int item4 = tuple.pred;
						float[] item5 = tuple.p;
						float num6 = (float)(0.0 - Math.Log(Math.Max((item3 == 0) ? item5[0] : item5[1], 1E-10f)));
						num2 += num6;
						if (item4 == item3)
						{
							num3++;
						}
						float num7 = item5[0] - ((item3 == 0) ? 1f : 0f);
						float num8 = item5[1] - ((item3 == 1) ? 1f : 0f);
						float num9 = num7 - num8;
						for (int num10 = 0; num10 < 16; num10++)
						{
							_out[num10, 0] -= num4 * num7 * 0.1f;
							_out[num10, 1] -= num4 * num8 * 0.1f;
						}
						for (int num11 = 0; num11 < 16; num11++)
						{
							float num12 = num9 * _out[num11, 0] * 0.01f;
							for (int num13 = 0; num13 < 5; num13++)
							{
								_emb[num13, num11] -= num4 * num12 * item2[num13] * 0.01f;
							}
						}
						float[] array = new float[16];
						for (int num14 = 0; num14 < 16; num14++)
						{
							for (int num15 = 0; num15 < 5; num15++)
							{
								array[num14] += item2[num15] * _emb[num15, num14];
							}
						}
						for (int num16 = 0; num16 < 2; num16++)
						{
							float num17 = ((num16 == item3) ? 0.1f : (-0.02f));
							for (int num18 = 0; num18 < 16; num18++)
							{
								float num19 = array[num18] - _coreK[num16, num18];
								_coreK[num16, num18] += num4 * num17 * num19;
								_coreK[num16, num18] = ((_coreK[num16, num18] < -10f) ? (-10f) : ((_coreK[num16, num18] > 10f) ? 10f : _coreK[num16, num18]));
							}
						}
					}
					float num20 = (float)num3 * 100f / (float)ds.Size;
					if (num20 > num)
					{
						num = num20;
					}
					Console.WriteLine($"Epoch {i + 1}: Loss={num2 / (float)ds.Size:F4} Acc={num20:F1}%");
				}
				Console.WriteLine($"\nCross-Attention训练完成! 最佳准确率: {num:F1}%");
				PrintCoreTypeTemplates();
			}

			public void PrintCoreTypeTemplates()
			{
				Console.WriteLine("\n=== 核心类型能力标签（K向量）===");
				for (int i = 0; i < 2; i++)
				{
					string arg = ((i == 0) ? "小核(E-core)" : "大核(P-core)");
					float num = 0f;
					for (int j = 0; j < 16; j++)
					{
						num += Math.Abs(_coreK[i, j]);
					}
					Console.WriteLine($"{arg}: 能量={num:F2}");
				}
				Console.WriteLine("\n=== 调度动作向量（V向量）===");
				for (int k = 0; k < 2; k++)
				{
					string arg2 = ((k == 0) ? "小核(E-core)" : "大核(P-core)");
					float num2 = 0f;
					for (int l = 0; l < 16; l++)
					{
						num2 += Math.Abs(_coreV[k, l]);
					}
					Console.WriteLine($"{arg2}: 能量={num2:F2}");
				}
			}

			public void TrainOnline(float[] x, int label, float lr = 0.1f)
			{
				float[] item = Forward(x).p;
				if (float.IsNaN(item[0]) || float.IsNaN(item[1]))
				{
					Console.WriteLine("[警告] 检测到 NaN，跳过本次更新");
					return;
				}
				float num = item[0] - ((label == 0) ? 1f : 0f);
				float num2 = item[1] - ((label == 1) ? 1f : 0f);
				_ = item[0];
				_ = item[1];
				for (int i = 0; i < 16; i++)
				{
					_out[i, 0] -= lr * num * 0.1f;
					_out[i, 1] -= lr * num2 * 0.1f;
					_out[i, 0] = Math.Max(-5f, Math.Min(5f, _out[i, 0]));
					_out[i, 1] = Math.Max(-5f, Math.Min(5f, _out[i, 1]));
				}
				for (int j = 0; j < 16; j++)
				{
					float num3 = (num - num2) * _out[j, 0] * 0.01f;
					for (int k = 0; k < 5; k++)
					{
						_emb[k, j] -= lr * num3 * x[k] * 0.01f;
						_emb[k, j] = Math.Max(-3f, Math.Min(3f, _emb[k, j]));
					}
				}
				float[] array = new float[16];
				for (int l = 0; l < 16; l++)
				{
					for (int m = 0; m < 5; m++)
					{
						array[l] += x[m] * _emb[m, l];
					}
				}
				for (int n = 0; n < 2; n++)
				{
					float num4 = ((n == label) ? 0.1f : (-0.02f));
					for (int num5 = 0; num5 < 16; num5++)
					{
						float num6 = array[num5] - _coreK[n, num5];
						_coreK[n, num5] += lr * num4 * num6;
						_coreK[n, num5] = Math.Max(-10f, Math.Min(10f, _coreK[n, num5]));
					}
				}
			}

			public void TrainOnlineRaw(float[] raw, int label, float lr = 0.1f)
			{
				for (int i = 0; i < 5; i++)
				{
					if (float.IsNaN(raw[i]) || float.IsInfinity(raw[i]))
					{
						Console.WriteLine($"[警告] 收到无效的原始数据特征[{i}]: {raw[i]}，跳过本次学习");
						return;
					}
				}
				if (!_isModelReady)
				{
					_sampleCount++;
					for (int j = 0; j < 5; j++)
					{
						float num = raw[j] - _runningMean[j];
						_runningMean[j] += num / (float)_sampleCount;
						float num2 = raw[j] - _runningMean[j];
						_runningM2[j] += num * num2;
						_mean[j] = _runningMean[j];
						if (_sampleCount > 1)
						{
							float num3 = Math.Max(0f, _runningM2[j]);
							_std[j] = (float)Math.Sqrt(num3 / (float)(_sampleCount - 1));
						}
						else
						{
							_std[j] = 1f;
						}
						if (_std[j] < 1E-06f)
						{
							_std[j] = 1f;
						}
					}
				}
				float[] x = Normalize(raw);
				TrainOnline(x, label, lr);
			}

			public void Evaluate(SchedulerDataset ds)
			{
				int num = 0;
				Console.WriteLine("\n========== 评估结果 ==========");
				Console.WriteLine(string.Format("{0,-45} {1,-6} {2,-6} {3}", "属性", "人工", "预测", "✓/✗"));
				Console.WriteLine(new string('-', 70));
				for (int i = 0; i < Math.Min(12, ds.Size); i++)
				{
					(float[] input, int label) item = ds.GetItem(i);
					float[] item2 = item.input;
					int item3 = item.label;
					int item4 = Forward(item2).pred;
					bool flag = item4 == item3;
					if (flag)
					{
						num++;
					}
					float[] item5 = ds.GetItem(i).input;
					Console.WriteLine(string.Format("IC:{0:F1} IPC:{1:F2} BM:{2:F0} CM:{3:F0} P:{4:F0} 人工:{5} 预测:{6} {7}", item5[0], item5[1], item5[2], item5[3], item5[4], (item3 == 1) ? "大核" : "小核", (item4 == 1) ? "大核" : "小核", flag ? "✓" : "✗"));
				}
				Console.WriteLine(new string('-', 70));
				Console.WriteLine($"总体准确率: {(float)num * 100f / 12f:F1}%");
			}

			public void Predict(SchedulerThreadData t)
			{
				float[] raw = t.ToArray();
				float[] x = Normalize(raw);
				var (num, array) = Forward(x);
				Console.WriteLine("\n========== 预测结果 ==========");
				Console.WriteLine($"线程属性: IC={t.InstructionCount} IPC={t.Ipc:F2} BM={t.BranchMiss} CM={t.CacheMiss} P={t.Priority}");
				Console.WriteLine("人工决策: " + t.DecisionStr());
				Console.WriteLine(string.Format("模型预测: {0} (大核={1:F1}% 小核={2:F1}%)", (num == 1) ? "大核" : "小核", array[1] * 100f, array[0] * 100f));
				Console.WriteLine($"注意力权重: 小核={_lastAttention[0] * 100f:F1}% 大核={_lastAttention[1] * 100f:F1}%");
				Console.WriteLine("预测正确: " + ((num == t.ArtificialDecision) ? "✓" : "✗"));
			}

			public int Schedule(SchedulerThreadData thread)
			{
				float[] raw = thread.ToArray();
				int item = PredictRaw(raw).pred;
				if (thread.ArtificialDecision >= 0)
				{
					_totalPredictions++;
					if (item == thread.ArtificialDecision)
					{
						_correctPredictions++;
					}
				}
				return item;
			}

			public (int pred, float[] p) PredictRaw(float[] raw)
			{
				float[] x = Normalize(raw);
				return Forward(x);
			}

			public bool IsModelReady(float minAccuracy = 0.6f, int minSamples = 1000)
			{
				if (_isModelReady)
				{
					return true;
				}
				if (_sampleCount < minSamples)
				{
					Console.WriteLine($"[学习状态] 训练样本不足: {_sampleCount}/{minSamples}");
					return false;
				}
				_isModelReady = true;
				_energyLearningEnabled = true;
				Console.WriteLine($"[学习状态] ✓ 模型已准备好! 训练样本: {_sampleCount}，能效学习已启用");
				return true;
			}

			public float[] GetAttention()
			{
				return _lastAttention;
			}

			public void Learn(float instructionCount, float ipc, float branchMiss, float cacheMiss, float priority, int decision, float learningRate = 0.1f)
			{
				float[] raw = new float[5] { instructionCount, ipc, branchMiss, cacheMiss, priority };
				TrainOnlineRaw(raw, decision, learningRate);
			}

			public void Learn(SchedulerThreadData thread, float learningRate = 0.1f)
			{
				Learn(thread.InstructionCount, thread.Ipc, thread.BranchMiss, thread.CacheMiss, thread.Priority, thread.ArtificialDecision, learningRate);
			}

			public int Schedule(float instructionCount, float ipc, float branchMiss, float cacheMiss, float priority)
			{
				float[] raw = new float[5] { instructionCount, ipc, branchMiss, cacheMiss, priority };
				return PredictRaw(raw).pred;
			}

			public (int coreType, float bigCoreProb, float[] attention) ScheduleWithDetails(float instructionCount, float ipc, float branchMiss, float cacheMiss, float priority)
			{
				float[] raw = new float[5] { instructionCount, ipc, branchMiss, cacheMiss, priority };
				var (item, array) = PredictRaw(raw);
				return (coreType: item, bigCoreProb: array[1], attention: _lastAttention);
			}

			public (int coreType, float bigCoreProb, float[] attention) ScheduleWithDetails(SchedulerThreadData thread)
			{
				return ScheduleWithDetails(thread.InstructionCount, thread.Ipc, thread.BranchMiss, thread.CacheMiss, thread.Priority);
			}

			public void EnableEnergyLearning()
			{
				_energyLearningEnabled = true;
				_baselineMetric = 0f;
				_currentWindow.Clear();
				Console.WriteLine("能效学习模式已启用");
			}

			public void DisableEnergyLearning()
			{
				_energyLearningEnabled = false;
				Console.WriteLine("能效学习模式已禁用");
			}

			public int ScheduleAndRecord(float instructionCount, float ipc, float branchMiss, float cacheMiss, float priority)
			{
				_scheduleCount++;
				if (!_energyLearningEnabled)
				{
					return Schedule(instructionCount, ipc, branchMiss, cacheMiss, priority);
				}
				float[] array = new float[5] { instructionCount, ipc, branchMiss, cacheMiss, priority };
				bool flag = false;
				for (int i = 0; i < 5; i++)
				{
					if (float.IsNaN(array[i]) || float.IsInfinity(array[i]))
					{
						flag = true;
						break;
					}
				}
				if (flag)
				{
					Console.WriteLine("[警告] ScheduleAndRecord 收到无效输入，返回随机决策");
					return _rnd.Next(2);
				}
				float[] features = Normalize(array);
				var (num, probabilities) = PredictRaw(array);
				int num2;
				if (_rnd.NextDouble() < (double)_explorationRate)
				{
					num2 = _rnd.Next(2);
					_explorationCount++;
				}
				else
				{
					num2 = num;
				}
				ScheduleRecord item = new ScheduleRecord
				{
					Features = features,
					RawFeatures = array,
					Decision = num2,
					Attention = (float[])_lastAttention.Clone(),
					Probabilities = probabilities,
					Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
					HasReward = false,
					Reward = 0f
				};
				_currentWindow.Add(item);
				return num2;
			}

			public int ScheduleAndRecord(SchedulerThreadData thread)
			{
				return ScheduleAndRecord(thread.InstructionCount, thread.Ipc, thread.BranchMiss, thread.CacheMiss, thread.Priority);
			}

			public void ReceiveEnergyFeedback(float energyMetric)
			{
				if (float.IsNaN(energyMetric) || float.IsInfinity(energyMetric) || energyMetric <= 0f)
				{
					Console.WriteLine($"[警告] 收到无效的能效指标: {energyMetric}，已忽略");
					return;
				}
				long num = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
				_metricHistory.Add((num, energyMetric));
				long cutoff = num - _metricWindowMs;
				_metricHistory.RemoveAll(((long timestamp, float metric) m) => m.timestamp < cutoff);
				if (_energyLearningEnabled && _currentWindow.Count != 0)
				{
					_energyLearningCount++;
					float reward = ComputeReward(energyMetric);
					DistributeReward(reward);
					UpdateBaseline(energyMetric);
					ReplayLearning();
					_explorationRate = Math.Max(_minExplorationRate, _explorationRate * _explorationDecay);
					_currentWindow.Clear();
				}
			}

			private float ComputeReward(float metric)
			{
				if (_baselineMetric <= 0f || float.IsNaN(_baselineMetric) || float.IsInfinity(_baselineMetric))
				{
					return 0f;
				}
				if (metric <= 0f || float.IsNaN(metric) || float.IsInfinity(metric))
				{
					return 0f;
				}
				return (metric - _baselineMetric) / _baselineMetric * 10f;
			}

			private void DistributeReward(float reward)
			{
				if (_currentWindow.Count == 0)
				{
					return;
				}
				if (float.IsNaN(reward) || float.IsInfinity(reward))
				{
					Console.WriteLine($"[警告] 无效的奖励值: {reward}，跳过分配");
					return;
				}
				long num = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
				float num2 = 0f;
				float[] array = new float[_currentWindow.Count];
				for (int i = 0; i < _currentWindow.Count; i++)
				{
					ScheduleRecord scheduleRecord = _currentWindow[i];
					if (scheduleRecord.Probabilities == null || scheduleRecord.Probabilities.Length < 2)
					{
						array[i] = 0.5f;
						num2 += array[i];
						continue;
					}
					float num3 = (float)Math.Exp((0f - (float)Math.Max(0L, num - scheduleRecord.Timestamp)) / (float)_windowMs);
					float val = ((scheduleRecord.Decision == 1) ? scheduleRecord.Probabilities[1] : scheduleRecord.Probabilities[0]);
					val = Math.Max(0.01f, Math.Min(0.99f, val));
					float num4 = val;
					array[i] = num3 * num4;
					num2 += array[i];
				}
				for (int j = 0; j < _currentWindow.Count; j++)
				{
					if (num2 > 0f)
					{
						_currentWindow[j].Reward = reward * array[j] / num2;
					}
					else
					{
						_currentWindow[j].Reward = 0f;
					}
					_currentWindow[j].HasReward = true;
					_replayBuffer.Add(_currentWindow[j]);
				}
				while (_replayBuffer.Count > _replayBufferSize)
				{
					_replayBuffer.RemoveAt(0);
				}
			}

			private void UpdateBaseline(float metric)
			{
				if (metric <= 0f || float.IsNaN(metric) || float.IsInfinity(metric))
				{
					Console.WriteLine($"[警告] 无效的能效指标: {metric}，跳过基线更新");
				}
				else if (_baselineMetric == 0f || float.IsNaN(_baselineMetric) || float.IsInfinity(_baselineMetric))
				{
					_baselineMetric = metric;
				}
				else
				{
					_baselineMetric = _baselineDecay * _baselineMetric + (1f - _baselineDecay) * metric;
				}
			}

			private void ReplayLearning()
			{
				if (_replayBuffer.Count < 10)
				{
					return;
				}
				int num = Math.Min(32, _replayBuffer.Count);
				float num2 = 0.05f;
				for (int i = 0; i < num; i++)
				{
					int index = _rnd.Next(_replayBuffer.Count);
					ScheduleRecord scheduleRecord = _replayBuffer[index];
					if (scheduleRecord.HasReward && scheduleRecord.RawFeatures != null && scheduleRecord.RawFeatures.Length == 5 && !float.IsNaN(scheduleRecord.Reward) && !float.IsInfinity(scheduleRecord.Reward))
					{
						int label = ((scheduleRecord.Reward > 0f) ? scheduleRecord.Decision : (1 - scheduleRecord.Decision));
						float num3 = Math.Min(Math.Abs(scheduleRecord.Reward), 1f);
						float lr = num2 * num3;
						TrainOnlineRaw(scheduleRecord.RawFeatures, label, lr);
					}
				}
			}

			public (int bufferSize, float explorationRate, float baselineMetric) GetEnergyLearningStats()
			{
				return (bufferSize: _replayBuffer.Count, explorationRate: _explorationRate, baselineMetric: _baselineMetric);
			}

			public string GetStatistics()
			{
				float num = 0f;
				float num2 = 0f;
				float num3 = 0f;
				float num4 = 0f;
				int count = _metricHistory.Count;
				if (count > 0)
				{
					num4 = _metricHistory[count - 1].metric;
					num = _metricHistory.Sum(((long timestamp, float metric) m) => m.metric) / (float)count;
					num2 = _metricHistory.Min(((long timestamp, float metric) m) => m.metric);
					num3 = _metricHistory.Max(((long timestamp, float metric) m) => m.metric);
				}
				return "[能效统计 - 5分钟窗口]\n" + $"  指标 - 当前: {num4:F2}, 平均: {num:F2}, 最小: {num2:F2}, 最大: {num3:F2}, 次数: {count}\n" + "[学习状态]\n" + $"  模型就绪: {_isModelReady}, 能效学习: {_energyLearningEnabled}, 学习次数: {_energyLearningCount}\n" + $"  调度次数: {_scheduleCount}, 探索次数: {_explorationCount}, 探索率: {_explorationRate:P2}\n" + $"  缓冲区: {_replayBuffer.Count}/{_replayBufferSize}, 基线: {_baselineMetric:F2}";
			}
		}

		public class ThreadPriorityMapper
		{
			public enum PriorityClass
			{
				Idle = 64,
				BelowNormal = 16384,
				Normal = 32,
				AboveNormal = 32768,
				High = 128,
				Realtime = 4096
			}

			public enum ThreadPriorityLevel
			{
				Idle = -15,
				Lowest = -2,
				BelowNormal = -1,
				Normal = 0,
				AboveNormal = 1,
				Highest = 2,
				TimeCritical = 15
			}

			private static readonly int[,] PriorityMatrix = new int[7, 6]
			{
				{ 15, 15, 15, 15, 15, 31 },
				{ 6, 8, 10, 12, 15, 26 },
				{ 5, 7, 9, 11, 14, 25 },
				{ 4, 6, 8, 10, 13, 24 },
				{ 3, 5, 7, 9, 12, 23 },
				{ 2, 4, 6, 8, 11, 22 },
				{ 1, 1, 1, 1, 1, 16 }
			};

			private static readonly Dictionary<PriorityClass, int> PriorityClassIndex = new Dictionary<PriorityClass, int>
			{
				{
					PriorityClass.Idle,
					0
				},
				{
					PriorityClass.BelowNormal,
					1
				},
				{
					PriorityClass.Normal,
					2
				},
				{
					PriorityClass.AboveNormal,
					3
				},
				{
					PriorityClass.High,
					4
				},
				{
					PriorityClass.Realtime,
					5
				}
			};

			private static readonly Dictionary<ThreadPriorityLevel, int> ThreadPriorityIndex = new Dictionary<ThreadPriorityLevel, int>
			{
				{
					ThreadPriorityLevel.TimeCritical,
					0
				},
				{
					ThreadPriorityLevel.Highest,
					1
				},
				{
					ThreadPriorityLevel.AboveNormal,
					2
				},
				{
					ThreadPriorityLevel.Normal,
					3
				},
				{
					ThreadPriorityLevel.BelowNormal,
					4
				},
				{
					ThreadPriorityLevel.Lowest,
					5
				},
				{
					ThreadPriorityLevel.Idle,
					6
				}
			};

			public static int GetFinalPriority(int priorityClass, int threadPriority)
			{
				if (!PriorityClassIndex.TryGetValue((PriorityClass)priorityClass, out var value))
				{
					value = 2;
				}
				if (!ThreadPriorityIndex.TryGetValue((ThreadPriorityLevel)threadPriority, out var value2))
				{
					value2 = 3;
				}
				return PriorityMatrix[value2, value];
			}

			public static PriorityClass GetPriorityClass(int value)
			{
				return (PriorityClass)value;
			}

			public static ThreadPriorityLevel GetThreadPriority(int value)
			{
				return (ThreadPriorityLevel)value;
			}
		}

		public class ThreadData
		{
			public int ThreadId { get; set; }

			public long InstructionCount { get; set; }

			public double MemoryAccessFrequency { get; set; }

			public double BranchMispredictionRate { get; set; }

			public double Ipc { get; set; }

			public long Timestamp { get; set; }
		}

		public enum ThreadDimension
		{
			InstructionCount,
			MemoryAccessFrequency,
			BranchMispredictionRate,
			Ipc
		}

		public class ThreadClassifier
		{
			private const int MaxCapacity = 3000;

			private const long TtlTicks = 300000000L;

			private Dictionary<int, ThreadData> _threadData;

			public ThreadClassifier()
			{
				_threadData = new Dictionary<int, ThreadData>();
			}

			public void AddThread(ThreadData thread)
			{
				if (thread == null)
				{
					return;
				}
				CleanExpiredData();
				thread.Timestamp = DateTime.Now.Ticks;
				if (_threadData.ContainsKey(thread.ThreadId))
				{
					_threadData[thread.ThreadId] = thread;
					return;
				}
				if (_threadData.Count >= 3000)
				{
					RemoveOldestData();
				}
				_threadData[thread.ThreadId] = thread;
			}

			public int IsAboveThreshold(int threadId, int dimension, bool useQuartile = true)
			{
				CleanExpiredData();
				if (!_threadData.ContainsKey(threadId))
				{
					return 0;
				}
				if (dimension < 0 || dimension > 3)
				{
					return 0;
				}
				ThreadData thread = _threadData[threadId];
				double dimensionValue = GetDimensionValue(thread, dimension);
				double num = (useQuartile ? CalculateQuartile(dimension) : CalculateMedian(dimension));
				return (dimensionValue > num) ? 1 : 0;
			}

			private double GetDimensionValue(ThreadData thread, int dimension)
			{
				return dimension switch
				{
					0 => thread.InstructionCount, 
					1 => thread.MemoryAccessFrequency, 
					2 => thread.BranchMispredictionRate, 
					3 => thread.Ipc, 
					_ => 0.0, 
				};
			}

			private double CalculateMedian(int dimension)
			{
				List<double> list = (from t in _threadData.Values
					select GetDimensionValue(t, dimension) into v
					orderby v
					select v).ToList();
				if (list.Count == 0)
				{
					return 0.0;
				}
				int count = list.Count;
				int num = count / 2;
				if (count % 2 == 0)
				{
					return (list[num - 1] + list[num]) / 2.0;
				}
				return list[num];
			}

			private double CalculateQuartile(int dimension)
			{
				List<double> list = (from t in _threadData.Values
					select GetDimensionValue(t, dimension) into v
					orderby v
					select v).ToList();
				if (list.Count == 0)
				{
					return 0.0;
				}
				int num = (int)((double)list.Count * 0.75);
				if (num >= list.Count)
				{
					num = list.Count - 1;
				}
				return list[num];
			}

			private void CleanExpiredData()
			{
				long currentTime = DateTime.Now.Ticks;
				foreach (int item in (from kvp in _threadData
					where currentTime - kvp.Value.Timestamp > 300000000
					select kvp.Key).ToList())
				{
					_threadData.Remove(item);
				}
			}

			private void RemoveOldestData()
			{
				if (_threadData.Count != 0)
				{
					KeyValuePair<int, ThreadData> keyValuePair = _threadData.OrderBy((KeyValuePair<int, ThreadData> kvp) => kvp.Value.Timestamp).First();
					_threadData.Remove(keyValuePair.Key);
				}
			}

			public int GetThreadCount()
			{
				return _threadData.Count;
			}
		}

		public class ThreadPerformanceData
		{
			public long InstructionCount { get; set; }

			public double MemoryAccessFrequency { get; set; }

			public double BranchMispredictRate { get; set; }

			public double IPC { get; set; }

			public long Timestamp { get; set; }
		}

		public class ThreadDataCollector
		{
			private const int MaxDataCount = 200;

			private readonly List<ThreadPerformanceData> _dataList = new List<ThreadPerformanceData>();

			public void AddData(long instructionCount, double memoryAccessFrequency, double branchMispredictRate, double ipc)
			{
				ThreadPerformanceData item = new ThreadPerformanceData
				{
					InstructionCount = instructionCount,
					MemoryAccessFrequency = memoryAccessFrequency,
					BranchMispredictRate = branchMispredictRate,
					IPC = ipc,
					Timestamp = DateTime.Now.Ticks
				};
				_dataList.Add(item);
				while (_dataList.Count > 200)
				{
					_dataList.RemoveAt(0);
				}
			}

			public int GetMostInfluentialVariableIndex()
			{
				if (_dataList.Count < 2)
				{
					return -1;
				}
				double[] y = _dataList.Select((ThreadPerformanceData d) => d.IPC).ToArray();
				double[] x = ((IEnumerable<ThreadPerformanceData>)_dataList).Select((Func<ThreadPerformanceData, double>)((ThreadPerformanceData d) => d.InstructionCount)).ToArray();
				double[] x2 = _dataList.Select((ThreadPerformanceData d) => d.MemoryAccessFrequency).ToArray();
				double[] x3 = _dataList.Select((ThreadPerformanceData d) => d.BranchMispredictRate).ToArray();
				double num = Math.Abs(CalculatePearsonCorrelation(x, y));
				double num2 = Math.Abs(CalculatePearsonCorrelation(x2, y));
				double num3 = Math.Abs(CalculatePearsonCorrelation(x3, y));
				if (num >= num2 && num >= num3)
				{
					return 0;
				}
				if (num2 >= num && num2 >= num3)
				{
					return 1;
				}
				return 2;
			}

			private double CalculatePearsonCorrelation(double[] x, double[] y)
			{
				if (x.Length != y.Length || x.Length < 2)
				{
					return 0.0;
				}
				int num = x.Length;
				double num2 = 0.0;
				double num3 = 0.0;
				double num4 = 0.0;
				double num5 = 0.0;
				double num6 = 0.0;
				for (int i = 0; i < num; i++)
				{
					num2 += x[i];
					num3 += y[i];
					num4 += x[i] * y[i];
					num5 += x[i] * x[i];
					num6 += y[i] * y[i];
				}
				double num7 = (double)num * num4 - num2 * num3;
				double num8 = Math.Sqrt(((double)num * num5 - num2 * num2) * ((double)num * num6 - num3 * num3));
				if (num8 != 0.0)
				{
					return num7 / num8;
				}
				return 0.0;
			}

			public int GetDataCount()
			{
				return _dataList.Count;
			}

			public void Clear()
			{
				_dataList.Clear();
			}

			public ThreadPerformanceData GetData(int index)
			{
				if (index >= 0 && index < _dataList.Count)
				{
					return _dataList[index];
				}
				return null;
			}

			public IReadOnlyList<ThreadPerformanceData> GetAllData()
			{
				return _dataList.AsReadOnly();
			}
		}

		public class CausalityAnalyzer
		{
			private const int MinDataPoints = 10;

			private const int MaxDataPoints = 30;

			private List<double[]> _dataPoints;

			private List<int> _coreFlags;

			private readonly int _dimension;

			private double _cachedCorrelation;

			private double _cachedElasticity;

			private bool _cacheValid;

			public int DataPointCount => _dataPoints.Count;

			public int Dimension => _dimension;

			public CausalityAnalyzer(int dimension)
			{
				if (dimension < 3)
				{
					throw new ArgumentException("维度必须至少为3（data1, data2和至少一个控制变量）");
				}
				_dimension = dimension;
				_dataPoints = new List<double[]>();
				_coreFlags = new List<int>();
				_cachedCorrelation = 0.0;
				_cachedElasticity = 0.0;
				_cacheValid = false;
			}

			public bool AddDataPoint(int coreFlag, params double[] dataValues)
			{
				if (coreFlag != 0 && coreFlag != 1)
				{
					return false;
				}
				if (dataValues == null || dataValues.Length != _dimension)
				{
					return false;
				}
				double[] array = new double[_dimension];
				Array.Copy(dataValues, array, _dimension);
				_dataPoints.Add(array);
				_coreFlags.Add(coreFlag);
				if (_dataPoints.Count > 30)
				{
					_dataPoints.RemoveAt(0);
					_coreFlags.RemoveAt(0);
				}
				_cacheValid = false;
				return true;
			}

			public double GetPartialCorrelation()
			{
				if (_dataPoints.Count < 10)
				{
					return 0.0;
				}
				if (!_cacheValid)
				{
					UpdateCache();
				}
				return _cachedCorrelation;
			}

			public double GetElasticity()
			{
				if (_dataPoints.Count < 10)
				{
					return 0.0;
				}
				if (!_cacheValid)
				{
					UpdateCache();
				}
				return _cachedElasticity;
			}

			public void Clear()
			{
				_dataPoints.Clear();
				_coreFlags.Clear();
				_cacheValid = false;
			}

			private void UpdateCache()
			{
				_cachedCorrelation = CalculatePartialCorrelation();
				_cachedElasticity = CalculatePartialElasticity();
				_cacheValid = true;
			}

			private double CalculatePartialCorrelation()
			{
				int count = _dataPoints.Count;
				int num = _dimension - 2 + 1;
				double[] array = new double[count];
				double[] array2 = new double[count];
				for (int i = 0; i < count; i++)
				{
					array[i] = _dataPoints[i][0];
					array2[i] = _dataPoints[i][1];
				}
				double[,] array3 = new double[count, num + 1];
				for (int j = 0; j < count; j++)
				{
					array3[j, 0] = 1.0;
					array3[j, 1] = _coreFlags[j];
					for (int k = 0; k < num - 1; k++)
					{
						array3[j, k + 2] = _dataPoints[j][k + 2];
					}
				}
				double[] residuals = GetResiduals(array, array3);
				double[] residuals2 = GetResiduals(array2, array3);
				return CalculateCorrelationFromResiduals(residuals, residuals2);
			}

			private double CalculatePartialElasticity()
			{
				int count = _dataPoints.Count;
				int num = _dimension - 2 + 1;
				double[] array = new double[count];
				double[] array2 = new double[count];
				for (int i = 0; i < count; i++)
				{
					array[i] = _dataPoints[i][0];
					array2[i] = _dataPoints[i][1];
				}
				double[,] array3 = new double[count, num + 1];
				for (int j = 0; j < count; j++)
				{
					array3[j, 0] = 1.0;
					array3[j, 1] = _coreFlags[j];
					for (int k = 0; k < num - 1; k++)
					{
						array3[j, k + 2] = _dataPoints[j][k + 2];
					}
				}
				double[] residuals = GetResiduals(array, array3);
				double[] residuals2 = GetResiduals(array2, array3);
				double num2 = 0.0;
				double num3 = 0.0;
				for (int l = 0; l < count; l++)
				{
					num2 += _dataPoints[l][0];
					num3 += _dataPoints[l][1];
				}
				num2 /= (double)count;
				num3 /= (double)count;
				double num4 = 0.0;
				double num5 = 0.0;
				for (int m = 0; m < count; m++)
				{
					num4 += residuals[m] * residuals2[m];
					num5 += residuals[m] * residuals[m];
				}
				if (num2 == 0.0 || num5 == 0.0)
				{
					return 0.0;
				}
				return num4 / num5 * (num2 / num3);
			}

			private double[] GetResiduals(double[] y, double[,] X)
			{
				int num = y.Length;
				int length = X.GetLength(1);
				double[,] array = new double[length, length];
				double[] array2 = new double[length];
				for (int i = 0; i < length; i++)
				{
					for (int j = 0; j < length; j++)
					{
						double num2 = 0.0;
						for (int k = 0; k < num; k++)
						{
							num2 += X[k, i] * X[k, j];
						}
						array[i, j] = num2;
					}
					double num3 = 0.0;
					for (int l = 0; l < num; l++)
					{
						num3 += X[l, i] * y[l];
					}
					array2[i] = num3;
				}
				double[] array3 = SolveLinearSystem(array, array2, length);
				double[] array4 = new double[num];
				for (int m = 0; m < num; m++)
				{
					double num4 = 0.0;
					for (int n = 0; n < length; n++)
					{
						num4 += X[m, n] * array3[n];
					}
					array4[m] = y[m] - num4;
				}
				return array4;
			}

			private double[] SolveLinearSystem(double[,] A, double[] b, int n)
			{
				for (int i = 0; i < n - 1; i++)
				{
					int num = i;
					double num2 = Math.Abs(A[i, i]);
					for (int j = i + 1; j < n; j++)
					{
						if (Math.Abs(A[j, i]) > num2)
						{
							num = j;
							num2 = Math.Abs(A[j, i]);
						}
					}
					if (num2 < 1E-10)
					{
						return new double[n];
					}
					if (num != i)
					{
						for (int k = i; k < n; k++)
						{
							double num3 = A[i, k];
							A[i, k] = A[num, k];
							A[num, k] = num3;
						}
						double num4 = b[i];
						b[i] = b[num];
						b[num] = num4;
					}
					for (int l = i + 1; l < n; l++)
					{
						double num5 = A[l, i] / A[i, i];
						for (int m = i; m < n; m++)
						{
							A[l, m] -= num5 * A[i, m];
						}
						b[l] -= num5 * b[i];
					}
				}
				double[] array = new double[n];
				for (int num6 = n - 1; num6 >= 0; num6--)
				{
					double num7 = b[num6];
					for (int num8 = num6 + 1; num8 < n; num8++)
					{
						num7 -= A[num6, num8] * array[num8];
					}
					array[num6] = num7 / A[num6, num6];
				}
				return array;
			}

			private double CalculateCorrelationFromResiduals(double[] r1, double[] r2)
			{
				int num = r1.Length;
				double num2 = 0.0;
				double num3 = 0.0;
				double num4 = 0.0;
				double num5 = 0.0;
				double num6 = 0.0;
				for (int i = 0; i < num; i++)
				{
					num2 += r1[i];
					num3 += r2[i];
					num4 += r1[i] * r2[i];
					num5 += r1[i] * r1[i];
					num6 += r2[i] * r2[i];
				}
				double num7 = (double)num * num4 - num2 * num3;
				double num8 = Math.Sqrt(((double)num * num5 - num2 * num2) * ((double)num * num6 - num3 * num3));
				if (num8 == 0.0)
				{
					return 0.0;
				}
				return num7 / num8;
			}

			private double CalculatePearsonCorrelation(int index1, int index2)
			{
				int count = _dataPoints.Count;
				double num = 0.0;
				double num2 = 0.0;
				double num3 = 0.0;
				double num4 = 0.0;
				double num5 = 0.0;
				for (int i = 0; i < count; i++)
				{
					double num6 = _dataPoints[i][index1];
					double num7 = _dataPoints[i][index2];
					num += num6;
					num2 += num7;
					num3 += num6 * num7;
					num4 += num6 * num6;
					num5 += num7 * num7;
				}
				double num8 = (double)count * num3 - num * num2;
				double num9 = Math.Sqrt(((double)count * num4 - num * num) * ((double)count * num5 - num2 * num2));
				if (num9 == 0.0)
				{
					return 0.0;
				}
				return num8 / num9;
			}
		}

		public class NumberProcessor
		{
			private readonly List<long> _numbers;

			public int Count => _numbers.Count;

			public NumberProcessor()
			{
				_numbers = new List<long>();
			}

			public void AddData(long data)
			{
				_numbers.Add(data);
			}

			public void AddDataRange(IEnumerable<long> dataList)
			{
				_numbers.AddRange(dataList);
			}

			public long GetMax()
			{
				if (_numbers.Count == 0)
				{
					return -1L;
				}
				return _numbers.Max();
			}

			public void Clear()
			{
				_numbers.Clear();
			}
		}

		public class DataLinkageAnalyzer
		{
			private readonly int _windowSize;

			private readonly Queue<double> _data1Queue;

			private readonly Queue<double> _data2Queue;

			public int DataCount => _data1Queue.Count;

			public bool CanAnalyze => DataCount >= _windowSize;

			public DataLinkageAnalyzer(int windowSize = 5)
			{
				if (windowSize <= 0)
				{
					throw new ArgumentException("窗口大小必须大于0", "windowSize");
				}
				_windowSize = windowSize;
				_data1Queue = new Queue<double>(windowSize + 1);
				_data2Queue = new Queue<double>(windowSize + 1);
			}

			public void AddData(double data1, double data2)
			{
				_data1Queue.Enqueue(data1);
				_data2Queue.Enqueue(data2);
				if (_data1Queue.Count > _windowSize)
				{
					_data1Queue.Dequeue();
					_data2Queue.Dequeue();
				}
			}

			public LinkageAnalysisResult Analyze()
			{
				if (!CanAnalyze)
				{
					throw new InvalidOperationException($"需要至少{_windowSize}个数据点才能进行分析，当前只有{DataCount}个");
				}
				double[] array = _data1Queue.ToArray();
				double[] array2 = _data2Queue.ToArray();
				return new LinkageAnalysisResult
				{
					CorrelationCoefficient = CalculatePearsonCorrelation(array, array2),
					Covariance = CalculateCovariance(array, array2),
					Data1Mean = array.Average(),
					Data2Mean = array2.Average(),
					Data1StdDev = CalculateStandardDeviation(array),
					Data2StdDev = CalculateStandardDeviation(array2),
					AnalysisWindowSize = _windowSize,
					Timestamp = DateTime.Now
				};
			}

			private double CalculatePearsonCorrelation(double[] data1, double[] data2)
			{
				if (data1.Length != data2.Length)
				{
					throw new ArgumentException("两个数据数组长度必须相同");
				}
				double num = data1.Average();
				double num2 = data2.Average();
				double num3 = 0.0;
				double num4 = 0.0;
				double num5 = 0.0;
				for (int i = 0; i < data1.Length; i++)
				{
					double num6 = data1[i] - num;
					double num7 = data2[i] - num2;
					num3 += num6 * num7;
					num4 += num6 * num6;
					num5 += num7 * num7;
				}
				if (num4 == 0.0 || num5 == 0.0)
				{
					return 0.0;
				}
				return num3 / Math.Sqrt(num4 * num5);
			}

			private double CalculateCovariance(double[] data1, double[] data2)
			{
				if (data1.Length != data2.Length)
				{
					throw new ArgumentException("两个数据数组长度必须相同");
				}
				double num = data1.Average();
				double num2 = data2.Average();
				double num3 = 0.0;
				for (int i = 0; i < data1.Length; i++)
				{
					num3 += (data1[i] - num) * (data2[i] - num2);
				}
				return num3 / (double)data1.Length;
			}

			private double CalculateStandardDeviation(double[] data)
			{
				double mean = data.Average();
				return Math.Sqrt(data.Sum((double x) => Math.Pow(x - mean, 2.0)) / (double)data.Length);
			}

			public void Clear()
			{
				_data1Queue.Clear();
				_data2Queue.Clear();
			}

			public (double[] data1, double[] data2) GetCurrentWindowData()
			{
				return (data1: _data1Queue.ToArray(), data2: _data2Queue.ToArray());
			}

			public int GetLinkageStrengthValue()
			{
				if (!CanAnalyze)
				{
					return -1;
				}
				return (Math.Abs(CalculatePearsonCorrelation(_data1Queue.ToArray(), _data2Queue.ToArray())) > 0.5) ? 1 : 0;
			}

			public double GetElasticity()
			{
				if (!CanAnalyze)
				{
					return 0.0;
				}
				double[] array = _data1Queue.ToArray();
				double[] array2 = _data2Queue.ToArray();
				double num = array.Average();
				double num2 = array2.Average();
				if (num == 0.0 || num2 == 0.0)
				{
					return 0.0;
				}
				double num3 = CalculateStandardDeviation(array) / num;
				double num4 = CalculateStandardDeviation(array2) / num2;
				double num5 = CalculatePearsonCorrelation(array, array2);
				return num4 / num3 * num5;
			}

			public string GetElasticityDescription()
			{
				double num = GetElasticity() * 1.0;
				string arg = ((num >= 0.0) ? "增加" : "减少");
				return $"当data1增加1%时，data2预计{arg}{Math.Abs(num):F2}%";
			}
		}

		public class LinkageAnalysisResult
		{
			public double CorrelationCoefficient { get; set; }

			public double Covariance { get; set; }

			public double Data1Mean { get; set; }

			public double Data2Mean { get; set; }

			public double Data1StdDev { get; set; }

			public double Data2StdDev { get; set; }

			public int AnalysisWindowSize { get; set; }

			public DateTime Timestamp { get; set; }

			public string GetLinkageStrength()
			{
				double num = Math.Abs(CorrelationCoefficient);
				if (num >= 0.8)
				{
					return "强联动";
				}
				if (num >= 0.5)
				{
					return "中等联动";
				}
				if (num >= 0.3)
				{
					return "弱联动";
				}
				return "无显著联动";
			}

			public string GetLinkageDirection()
			{
				if (CorrelationCoefficient > 0.0)
				{
					return "正相关";
				}
				if (CorrelationCoefficient < 0.0)
				{
					return "负相关";
				}
				return "无相关";
			}

			public override string ToString()
			{
				return "联动性分析结果:\n" + $"相关系数: {CorrelationCoefficient:F4}\n" + $"协方差: {Covariance:F4}\n" + $"Data1均值: {Data1Mean:F4}, 标准差: {Data1StdDev:F4}\n" + $"Data2均值: {Data2Mean:F4}, 标准差: {Data2StdDev:F4}\n" + "联动强度: " + GetLinkageStrength() + "\n联动方向: " + GetLinkageDirection() + "\n" + $"分析时间: {Timestamp:yyyy-MM-dd HH:mm:ss}";
			}
		}

		public class ElasticityResult
		{
			public double Elasticity { get; set; }

			public double PercentageChange { get; set; }

			public string Direction { get; set; }

			public string Description { get; set; }

			public DateTime Timestamp { get; set; }

			public override string ToString()
			{
				return "弹性分析结果:\n" + $"弹性系数: {Elasticity:F4}\n" + $"Data1增加1%时，Data2预计{Direction}{Math.Abs(PercentageChange):F2}%\n" + "描述: " + Description + "\n" + $"分析时间: {Timestamp:yyyy-MM-dd HH:mm:ss}";
			}
		}

		public class ThreadExecutionRegistry
		{
			public Dictionary<int, long> _data = new Dictionary<int, long>();

			public int Count => _data.Count;

			public void AddOrUpdate(int threadId, long execTime)
			{
				_data[threadId] = execTime;
			}

			public void Clear()
			{
				_data.Clear();
			}

			public IEnumerable<KeyValuePair<int, long>> GetAllData()
			{
				return _data;
			}
		}

		public class CoreEntry
		{
			public double Uti { get; }

			public int Cid { get; }

			public CoreEntry(double uti, int cid)
			{
				Uti = uti;
				Cid = cid;
			}
		}

		public class CoreEntryComparer : IComparer<CoreEntry>
		{
			public int Compare(CoreEntry x, CoreEntry y)
			{
				int num = x.Uti.CompareTo(y.Uti);
				if (num != 0)
				{
					return num;
				}
				return x.Cid.CompareTo(y.Cid);
			}
		}

		public class CoreManager
		{
			public readonly SortedSet<CoreEntry> _coreSet;

			public readonly CoreEntry[] _currentEntries;

			public readonly CoreEntryComparer _comparer = new CoreEntryComparer();

			public CoreManager(int numCores)
			{
				_coreSet = new SortedSet<CoreEntry>(_comparer);
				_currentEntries = new CoreEntry[numCores];
				for (int i = 0; i < numCores; i++)
				{
					CoreEntry coreEntry = new CoreEntry(0.0, i);
					_coreSet.Add(coreEntry);
					_currentEntries[i] = coreEntry;
				}
			}

			public void UpdateUtilization(int cid, double newUti)
			{
				if (cid < 0 || cid >= _currentEntries.Length)
				{
					throw new ArgumentOutOfRangeException("cid", "Invalid core ID");
				}
				_coreSet.Remove(_currentEntries[cid]);
				CoreEntry coreEntry = new CoreEntry(newUti, cid);
				_coreSet.Add(coreEntry);
				_currentEntries[cid] = coreEntry;
			}

			public int GetMinUtilCore()
			{
				if (_coreSet.Count == 0)
				{
					return -1;
				}
				return _coreSet.Min.Cid;
			}

			public void PrintAllCores()
			{
				Console.WriteLine("Core utilization ranking:");
				int num = 1;
				foreach (CoreEntry item in _coreSet)
				{
					Console.WriteLine($"{num++}. Core {item.Cid}: {item.Uti:P1}");
				}
			}
		}

		public class GaussianRandom
		{
			private readonly Random _random;

			private bool _hasSpare;

			private double _spareValue;

			public GaussianRandom(int? seed = null)
			{
				_random = (seed.HasValue ? new Random(seed.Value) : new Random());
			}

			public double NextGaussian(double mean = 0.0, double stdDev = 1.0)
			{
				if (_hasSpare)
				{
					_hasSpare = false;
					return _spareValue * stdDev + mean;
				}
				double num;
				double num2;
				double num3;
				do
				{
					num = _random.NextDouble() * 2.0 - 1.0;
					num2 = _random.NextDouble() * 2.0 - 1.0;
					num3 = num * num + num2 * num2;
				}
				while (num3 >= 1.0 || num3 == 0.0);
				num3 = Math.Sqrt(-2.0 * Math.Log(num3) / num3);
				_spareValue = num2 * num3;
				_hasSpare = true;
				return num * num3 * stdDev + mean;
			}
		}

		public class NeuralNetwork
		{
			private const int SCALE = 1000;

			private const int NUM_IN = 7;

			private const int f_NODES = 6;

			private const int s_NODES = 5;

			private const int t_NODES = 3;

			private const int p1_NODES = 6;

			private const int q1_NODES = 6;

			private const int p2_NODES = 5;

			private const int q2_NODES = 6;

			private const int m_NODES = 7;

			private int[,] weights_I_f = new int[6, 7];

			private int[,] weights_f_s = new int[5, 6];

			private int[,] weights_s_t = new int[3, 5];

			private int[,] weights_t_p1 = new int[6, 3];

			private int[,] weights_t_p2 = new int[5, 3];

			private int[,] weights_p1_q1 = new int[6, 6];

			private int[,] weights_p2_q2 = new int[6, 5];

			private int[,] weights_q1_o = new int[1, 6];

			private int[,] weights_q2_m = new int[7, 6];

			private int[] sigmoidTable = new int[2001];

			public void InitializeWeights_calc_gauss(ref int[,] weights, int input, int output)
			{
				GaussianRandom gaussianRandom = new GaussianRandom();
				double num = Math.Sqrt(2.0 / (double)(input + output)) * 1000.0;
				for (int i = 0; i < output; i++)
				{
					for (int j = 0; j < input; j++)
					{
						weights[i, j] = (int)(gaussianRandom.NextGaussian() * num);
					}
				}
			}

			public void InitializeWeights_calc(ref int[,] weights, int input, int output)
			{
				Random random = new Random();
				double num = Math.Sqrt(6.0 / (double)(input + output)) * 1000.0;
				for (int i = 0; i < output; i++)
				{
					for (int j = 0; j < input; j++)
					{
						weights[i, j] = (int)(random.NextDouble() * 2.0 * num - num);
					}
				}
			}

			public void InitializeWeights()
			{
				new Random();
				new GaussianRandom();
				InitializeWeights_calc(ref weights_I_f, 7, 6);
				InitializeWeights_calc(ref weights_f_s, 6, 5);
				InitializeWeights_calc(ref weights_s_t, 5, 3);
				InitializeWeights_calc(ref weights_t_p1, 3, 6);
				InitializeWeights_calc(ref weights_t_p2, 3, 5);
				InitializeWeights_calc(ref weights_p1_q1, 6, 6);
				InitializeWeights_calc(ref weights_p2_q2, 5, 6);
				InitializeWeights_calc_gauss(ref weights_q1_o, 6, 1);
				InitializeWeights_calc_gauss(ref weights_q2_m, 6, 7);
			}

			public NeuralNetwork()
			{
				for (int i = 0; i <= 2000; i++)
				{
					sigmoidTable[i] = (int)(1000.0 / (1.0 + Math.Exp((double)(-(i - 1000)) / 100.0)));
				}
				InitializeWeights();
			}

			public int Predict(int[] inputs)
			{
				int[] input = ComputeLayer(inputs, weights_I_f);
				int[] input2 = ComputeLayer(input, weights_f_s);
				int[] input3 = ComputeLayer(input2, weights_s_t);
				int[] input4 = ComputeLayer(input3, weights_t_p1);
				int[] array = ComputeLayer(input4, weights_p1_q1);
				int num = 0;
				for (int i = 0; i < 6; i++)
				{
					num += array[i] * weights_q1_o[0, i];
				}
				return sigmoidTable[ClampIndex(num / 1000)];
			}

			private int ClampIndex(int value)
			{
				return Math.Min(Math.Max(value + 1000, 0), 2000);
			}

			public void Update(int[] inputs, int action, int reward, int neuro_on, int iscomp)
			{
				int[] array = ComputeLayer(inputs, weights_I_f);
				int[] array2 = ComputeLayer(array, weights_f_s);
				int[] array3 = ComputeLayer(array2, weights_s_t);
				if (neuro_on != 0)
				{
					int[] array4 = ComputeLayer(array3, weights_t_p1);
					int[] array5 = ComputeLayer(array4, weights_p1_q1);
					int num = 0;
					for (int i = 0; i < 6; i++)
					{
						num += array5[i] * weights_q1_o[0, i];
					}
					num = sigmoidTable[ClampIndex(num / 1000)];
					int num2 = ((action == 1) ? (1000 - num) : num) * reward * SigmoidDerivative(num) / 1000;
					UpdateWeights(ref weights_q1_o, num2, array5, 0, 1);
					int[] array6 = new int[6];
					for (int j = 0; j < 6; j++)
					{
						array6[j] = num2 * SigmoidDerivative(array5[j]) / 1000;
					}
					int[] delta = BackwardBranch_single(array6, array4, ref weights_p1_q1, 1, 1);
					BackwardBranch_single(delta, array3, ref weights_t_p1, 2, 1);
				}
				else if (neuro_on == 0)
				{
					int[] array7 = ComputeLayer(array3, weights_t_p2);
					int[] array8 = ComputeLayer(array7, weights_p2_q2);
					int[] array9 = ComputeLayer(array8, weights_q2_m);
					int[] array10 = new int[7];
					for (int k = 0; k < 7; k++)
					{
						array10[k] = (inputs[k] - array9[k]) * (inputs[k] - array9[k]);
					}
					int[] delta2 = BackwardBranch_single(array10, array8, ref weights_q2_m, 0, 0);
					int[] delta3 = BackwardBranch_single(delta2, array7, ref weights_p2_q2, 1, 0);
					int[] delta4 = BackwardBranch_single(delta3, array3, ref weights_t_p2, 2, 0);
					int[] delta5 = BackwardBranch_single(delta4, array2, ref weights_s_t, 3, 0);
					int[] delta6 = BackwardBranch_single(delta5, array, ref weights_f_s, 4, 0);
					BackwardBranch_single(delta6, inputs, ref weights_I_f, 5, 0);
				}
			}

			private int[] ComputeLayer(int[] input, int[,] weights)
			{
				int[] array = new int[weights.GetLength(0)];
				for (int i = 0; i < array.Length; i++)
				{
					int num = 0;
					for (int j = 0; j < input.Length; j++)
					{
						num += input[j] * weights[i, j];
					}
					array[i] = sigmoidTable[ClampIndex(num / 1000)];
				}
				return array;
			}

			private void BackwardBranch1(int delta, int[] tLayer, int[] sLayer, int[] fLayer, int[] mLayer, ref int[,] s_t_Weights, ref int[,] f_s_Weights, ref int[,] m_f_Weights, int iscomp, ref int[] deltaM)
			{
				int[] array = new int[tLayer.Length];
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = delta * SigmoidDerivative(tLayer[i]) / 1000;
				}
				UpdateWeights(ref s_t_Weights, array, sLayer, 1, iscomp);
				int[] array2 = ComputeHiddenDelta(array, s_t_Weights, sLayer);
				UpdateWeights(ref f_s_Weights, array2, fLayer, 2, iscomp);
				int[] array3 = ComputeHiddenDelta(array2, f_s_Weights, fLayer);
				UpdateWeights(ref m_f_Weights, array3, mLayer, 3, iscomp);
				deltaM = ComputeHiddenDelta(array3, m_f_Weights, mLayer);
			}

			private int[] BackwardBranch_single(int[] delta, int[] prevlayer, ref int[,] weights, int layerDepth, int iscomp)
			{
				UpdateWeights(ref weights, delta, prevlayer, layerDepth, iscomp);
				return ComputeHiddenDelta(delta, weights, prevlayer);
			}

			private int[] BackwardBranch_merge(int[] delta1, int[] delta2, int[] prevlayer, ref int[,] weights1, ref int[,] weights2, int layerDepth1, int layerDepth2)
			{
				UpdateWeights(ref weights1, delta1, prevlayer, layerDepth1, 0);
				UpdateWeights(ref weights2, delta2, prevlayer, layerDepth2, 0);
				int[] array = ComputeHiddenDelta(delta1, weights1, prevlayer);
				int[] array2 = ComputeHiddenDelta(delta2, weights2, prevlayer);
				int[] array3 = new int[prevlayer.Length];
				for (int i = 0; i < prevlayer.Length; i++)
				{
					array3[i] = array[i] + array2[i];
				}
				return array3;
			}

			private int[] BackwardBranch_merge3(int[] delta1, int[] delta2, int[] delta3, int[] prevlayer, ref int[,] weights1, ref int[,] weights2, ref int[,] weights3, int layerDepth1, int layerDepth2, int layerDepth3)
			{
				UpdateWeights(ref weights1, delta1, prevlayer, layerDepth1, 0);
				UpdateWeights(ref weights2, delta2, prevlayer, layerDepth2, 0);
				UpdateWeights(ref weights3, delta3, prevlayer, layerDepth3, 0);
				int[] array = ComputeHiddenDelta(delta1, weights1, prevlayer);
				int[] array2 = ComputeHiddenDelta(delta2, weights2, prevlayer);
				int[] array3 = ComputeHiddenDelta(delta3, weights3, prevlayer);
				int[] array4 = new int[prevlayer.Length];
				for (int i = 0; i < prevlayer.Length; i++)
				{
					array4[i] = array[i] + array2[i] + array3[i];
				}
				return array4;
			}

			private void BackwardBranch2(int delta, int[] tLayer, int[] s3Layer, int[] s2Layer, int[] s1Layer, ref int[,] s3_t_Weights, ref int[,] s2_s3_Weights, ref int[,] s1_s2_Weights, ref int[,] i_s1_Weights, ref int[] inputs, int iscomp)
			{
				int[] array = new int[tLayer.Length];
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = delta * SigmoidDerivative(tLayer[i]) / 1000;
				}
				UpdateWeights(ref s3_t_Weights, array, s3Layer, 1, iscomp);
				int[] array2 = ComputeHiddenDelta(array, s3_t_Weights, s3Layer);
				UpdateWeights(ref s2_s3_Weights, array2, s2Layer, 2, iscomp);
				int[] array3 = ComputeHiddenDelta(array2, s2_s3_Weights, s2Layer);
				UpdateWeights(ref s1_s2_Weights, array3, s1Layer, 3, iscomp);
				int[] delta2 = ComputeHiddenDelta(array3, s1_s2_Weights, s1Layer);
				UpdateWeights(ref i_s1_Weights, delta2, inputs, 4, iscomp);
			}

			private void UpdateWeights(ref int[,] weights, int delta, int[] prevLayer, int layerDepth, int iscomp)
			{
				float num = ((iscomp != 1) ? (1f * (1f / (float)Math.Pow(2.0, layerDepth))) : (0.1f * (1f / (float)Math.Pow(2.0, layerDepth))));
				for (int i = 0; i < layerDepth; i++)
				{
					int num2 = delta * prevLayer[i] / 1000;
					weights[0, i] += (int)(num * (float)num2);
				}
			}

			private void UpdateWeights(ref int[,] weights, int[] delta, int[] prevLayer, int layerDepth, int iscomp)
			{
				float num = ((iscomp != 1) ? (1f * (1f / (float)Math.Pow(2.0, layerDepth))) : (0.1f * (1f / (float)Math.Pow(2.0, layerDepth))));
				for (int i = 0; i < weights.GetLength(0); i++)
				{
					for (int j = 0; j < weights.GetLength(1); j++)
					{
						int num2 = delta[i] * prevLayer[j] / 1000;
						weights[i, j] += (int)(num * (float)num2);
					}
				}
			}

			private int SigmoidDerivative(int activatedValue)
			{
				int num = activatedValue / 1;
				return num * (1000 - num) / 1000 * 1000 / 1000;
			}

			private int[] ComputeHiddenDelta(int[] nextDelta, int[,] nextWeights, int[] currentLayer)
			{
				int[] array = new int[currentLayer.Length];
				for (int i = 0; i < currentLayer.Length; i++)
				{
					int num = 0;
					for (int j = 0; j < nextDelta.Length; j++)
					{
						num += nextDelta[j] * nextWeights[j, i];
					}
					array[i] = num * SigmoidDerivative(currentLayer[i]) / 10000;
				}
				return array;
			}

			private int ComputeFinalOutput(int[] t1, int[] t2, int[] outputWeights)
			{
				int num = 0;
				for (int i = 0; i < t1.Length; i++)
				{
					num += t1[i] * outputWeights[i];
				}
				for (int j = 0; j < t2.Length; j++)
				{
					num += t2[j] * outputWeights[t1.Length + j];
				}
				return sigmoidTable[ClampIndex(num / 1000)];
			}

			private int[] CombineLayers(int[] layer1, int[] layer2)
			{
				int[] array = new int[layer1.Length + layer2.Length];
				Array.Copy(layer1, 0, array, 0, layer1.Length);
				Array.Copy(layer2, 0, array, layer1.Length, layer2.Length);
				return array;
			}
		}

		public struct ThreadMetrics
		{
			public int BigCoreIPC;

			public int SmallCoreIPC;

			public long InstructionsPerCycle;

			public int Priority;

			public double BigCoreCacheMissRate;

			public double SmallCoreCacheMissRate;

			public long ExecutionTimeMicroseconds;
		}

		public class ThreadSchedulerCore
		{
			private LSTMCell lstm;

			private double[] bigCoreWeights;

			private double[] smallCoreWeights;

			private double learningRate;

			private int inputSize;

			private int hiddenSize;

			private const int BIG_CORE = 1;

			private const int SMALL_CORE = 0;

			private const double CONFIDENCE_THRESHOLD = 0.7;

			private List<LearningRecord> learningHistory;

			private const int MAX_LEARNING_HISTORY_SIZE = 500;

			private List<DecisionRecord> decisionHistory;

			private const int MAX_DECISION_HISTORY_SIZE = 1000;

			public ThreadSchedulerCore(int inputSize = 7, int hiddenSize = 64)
			{
				this.inputSize = inputSize;
				this.hiddenSize = hiddenSize;
				lstm = new LSTMCell(inputSize, hiddenSize);
				bigCoreWeights = InitializeWeights(hiddenSize);
				smallCoreWeights = InitializeWeights(hiddenSize);
				learningRate = 0.01;
				learningHistory = new List<LearningRecord>();
				decisionHistory = new List<DecisionRecord>();
			}

			private double[] InitializeWeights(int size)
			{
				Random random = new Random();
				double[] array = new double[size];
				for (int i = 0; i < size; i++)
				{
					array[i] = (random.NextDouble() - 0.5) * 2.0 / Math.Sqrt(size);
				}
				return array;
			}

			private double[] ConvertToInput(ThreadMetrics threadInfo)
			{
				return new double[7]
				{
					(double)threadInfo.BigCoreIPC / 100.0,
					(double)threadInfo.SmallCoreIPC / 100.0,
					NormalizeInstructions(threadInfo.InstructionsPerCycle),
					(double)threadInfo.Priority / 1500.0,
					threadInfo.BigCoreCacheMissRate,
					threadInfo.SmallCoreCacheMissRate,
					NormalizeExecutionTime(threadInfo.ExecutionTimeMicroseconds)
				};
			}

			private double NormalizeInstructions(long instructions)
			{
				return Math.Min((double)instructions / 1000000.0, 1.0);
			}

			private double NormalizeExecutionTime(long timeMicroseconds)
			{
				return Math.Min((double)timeMicroseconds / 1000000.0, 1.0);
			}

			public (int neuralDecision, double confidence, bool adoptNeural, int finalDecision) MakeDecision(ThreadMetrics currentThreadInfo, int humanDecision)
			{
				double[] input = ConvertToInput(currentThreadInfo);
				double[] hiddenState = lstm.Forward(input);
				double num = CalculateScore(hiddenState, 1);
				double num2 = CalculateScore(hiddenState, 0);
				int num3 = ((num > num2) ? 1 : 0);
				double num4 = Math.Abs(num - num2) / (Math.Abs(num) + Math.Abs(num2) + 1E-08);
				bool flag = num4 > 0.7;
				int num5 = (flag ? num3 : humanDecision);
				RecordDecision(currentThreadInfo, humanDecision, num3, num4, flag, num5);
				return (neuralDecision: num3, confidence: num4, adoptNeural: flag, finalDecision: num5);
			}

			private double CalculateScore(double[] hiddenState, int decision)
			{
				double[] array = ((decision == 1) ? bigCoreWeights : smallCoreWeights);
				double num = 0.0;
				for (int i = 0; i < hiddenState.Length; i++)
				{
					num += hiddenState[i] * array[i];
				}
				Random random = new Random();
				return num + (random.NextDouble() - 0.5) * 0.1;
			}

			public void OnlineLearning(ThreadMetrics currentThreadInfo, ThreadMetrics previousThreadInfo, int previousDecision, bool wasNeuralDecision)
			{
				double reward = CalculateReward(currentThreadInfo, previousThreadInfo, previousDecision);
				double[] input = ConvertToInput(previousThreadInfo);
				double[] previousHiddenState = lstm.GetPreviousHiddenState();
				UpdateWeights(reward, previousHiddenState, previousDecision);
				learningHistory.Add(new LearningRecord
				{
					PreviousThreadInfo = previousThreadInfo,
					CurrentThreadInfo = currentThreadInfo,
					Decision = previousDecision,
					WasNeuralDecision = wasNeuralDecision,
					Reward = reward,
					Timestamp = DateTime.Now
				});
				if (learningHistory.Count > 500)
				{
					learningHistory.RemoveAt(0);
				}
				if (wasNeuralDecision)
				{
					LearnFromNeuralDecision(reward, input, previousDecision);
				}
				else
				{
					LearnFromObservation(currentThreadInfo, previousThreadInfo, previousDecision, reward);
				}
			}

			private double CalculateReward(ThreadMetrics current, ThreadMetrics previous, int previousDecision)
			{
				double num = 0.0;
				double num2 = previous.ExecutionTimeMicroseconds - current.ExecutionTimeMicroseconds;
				num += num2 / 1000.0;
				num = ((previousDecision != 1) ? (num + (double)(current.SmallCoreIPC - previous.SmallCoreIPC) / 100.0) : (num + (double)(current.BigCoreIPC - previous.BigCoreIPC) / 100.0));
				num = ((previousDecision != 1) ? (num + (previous.SmallCoreCacheMissRate - current.SmallCoreCacheMissRate) * 10.0) : (num + (previous.BigCoreCacheMissRate - current.BigCoreCacheMissRate) * 10.0));
				return Math.Max(-1.0, Math.Min(1.0, num));
			}

			private void UpdateWeights(double reward, double[] hiddenState, int decision)
			{
				double[] array = ((decision == 1) ? bigCoreWeights : smallCoreWeights);
				for (int i = 0; i < array.Length; i++)
				{
					double num = reward * hiddenState[i];
					array[i] += learningRate * num;
				}
			}

			private void LearnFromNeuralDecision(double reward, double[] input, int decision)
			{
				lstm.Backward(reward, input);
			}

			private void LearnFromObservation(ThreadMetrics current, ThreadMetrics previous, int humanDecision, double reward)
			{
				double[] input = ConvertToInput(previous);
				double[] hiddenState = lstm.Forward(input);
				double num = CalculateScore(hiddenState, 1);
				double num2 = CalculateScore(hiddenState, 0);
				int num3 = ((num > num2) ? 1 : 0);
				if (num3 != humanDecision)
				{
					double reward2 = reward * (double)((humanDecision == num3) ? 1 : (-1));
					LearnFromNeuralDecision(reward2, input, num3);
				}
			}

			public LearningStatistics GetLearningStatistics()
			{
				if (learningHistory.Count == 0)
				{
					return default(LearningStatistics);
				}
				return new LearningStatistics
				{
					TotalLearningSessions = learningHistory.Count,
					NeuralDecisionCount = learningHistory.Count((LearningRecord r) => r.WasNeuralDecision),
					AverageReward = learningHistory.Average((LearningRecord r) => r.Reward),
					LastLearningTime = learningHistory.Last().Timestamp
				};
			}

			public void ResetLearningHistory()
			{
				learningHistory.Clear();
			}

			public void SetLearningRate(double rate)
			{
				learningRate = Math.Max(0.0001, Math.Min(1.0, rate));
			}

			public double GetLearningRate()
			{
				return learningRate;
			}

			public ModelInfo GetModelInfo()
			{
				return new ModelInfo
				{
					InputSize = inputSize,
					HiddenSize = hiddenSize,
					WeightCount = bigCoreWeights.Length + smallCoreWeights.Length,
					ConfidenceThreshold = 0.7
				};
			}

			private void RecordDecision(ThreadMetrics threadInfo, int humanDecision, int neuralDecision, double confidence, bool adoptNeural, int finalDecision)
			{
				DecisionRecord item = new DecisionRecord
				{
					ThreadInfo = threadInfo,
					HumanDecision = humanDecision,
					NeuralDecision = neuralDecision,
					Confidence = confidence,
					AdoptNeural = adoptNeural,
					FinalDecision = finalDecision,
					Timestamp = DateTime.Now
				};
				decisionHistory.Add(item);
				if (decisionHistory.Count > 1000)
				{
					decisionHistory.RemoveAt(0);
				}
			}

			public SchedulerStatistics GetSchedulerStatistics()
			{
				if (decisionHistory.Count == 0)
				{
					return new SchedulerStatistics
					{
						TotalDecisions = 0,
						NeuralAdoptedCount = 0,
						HumanAdoptedCount = 0,
						DisagreementCount = 0,
						NeuralAdoptionRate = 0.0,
						DisagreementRate = 0.0,
						AverageConfidence = 0.0,
						FirstDecisionTime = DateTime.MinValue,
						LastDecisionTime = DateTime.MinValue
					};
				}
				List<DecisionRecord> list = decisionHistory.Skip(Math.Max(0, decisionHistory.Count - 1000)).ToList();
				int count = list.Count;
				int num = list.Count((DecisionRecord r) => r.AdoptNeural);
				int humanAdoptedCount = count - num;
				int num2 = list.Count((DecisionRecord r) => r.NeuralDecision != r.HumanDecision);
				double averageConfidence = list.Average((DecisionRecord r) => r.Confidence);
				return new SchedulerStatistics
				{
					TotalDecisions = count,
					NeuralAdoptedCount = num,
					HumanAdoptedCount = humanAdoptedCount,
					DisagreementCount = num2,
					NeuralAdoptionRate = (double)num / (double)count,
					DisagreementRate = (double)num2 / (double)count,
					AverageConfidence = averageConfidence,
					FirstDecisionTime = list.First().Timestamp,
					LastDecisionTime = list.Last().Timestamp
				};
			}

			public SchedulerStatistics GetRecentStatistics(int count)
			{
				if (decisionHistory.Count == 0 || count <= 0)
				{
					return GetSchedulerStatistics();
				}
				List<DecisionRecord> list = decisionHistory.Skip(Math.Max(0, decisionHistory.Count - Math.Min(count, decisionHistory.Count))).ToList();
				int count2 = list.Count;
				int num = list.Count((DecisionRecord r) => r.AdoptNeural);
				int humanAdoptedCount = count2 - num;
				int num2 = list.Count((DecisionRecord r) => r.NeuralDecision != r.HumanDecision);
				double averageConfidence = list.Average((DecisionRecord r) => r.Confidence);
				return new SchedulerStatistics
				{
					TotalDecisions = count2,
					NeuralAdoptedCount = num,
					HumanAdoptedCount = humanAdoptedCount,
					DisagreementCount = num2,
					NeuralAdoptionRate = (double)num / (double)count2,
					DisagreementRate = (double)num2 / (double)count2,
					AverageConfidence = averageConfidence,
					FirstDecisionTime = list.First().Timestamp,
					LastDecisionTime = list.Last().Timestamp
				};
			}

			public void ResetDecisionHistory()
			{
				decisionHistory.Clear();
			}

			public int GetDecisionHistoryCount()
			{
				return decisionHistory.Count;
			}

			public int GetLearningHistoryCount()
			{
				return learningHistory.Count;
			}

			public int GetMaxDecisionHistorySize()
			{
				return 1000;
			}

			public int GetMaxLearningHistorySize()
			{
				return 500;
			}

			public void SetMaxDecisionHistorySize(int maxSize)
			{
				if (maxSize < 10)
				{
					maxSize = 10;
				}
				if (maxSize > 10000)
				{
					maxSize = 10000;
				}
				while (decisionHistory.Count > maxSize)
				{
					decisionHistory.RemoveAt(0);
				}
			}

			public void SetMaxLearningHistorySize(int maxSize)
			{
				if (maxSize < 10)
				{
					maxSize = 10;
				}
				if (maxSize > 10000)
				{
					maxSize = 10000;
				}
				while (learningHistory.Count > maxSize)
				{
					learningHistory.RemoveAt(0);
				}
			}

			public (double decisionUsage, double learningUsage) GetHistoryUsage()
			{
				double item = (double)decisionHistory.Count / 1000.0;
				double item2 = (double)learningHistory.Count / 500.0;
				return (decisionUsage: item, learningUsage: item2);
			}
		}

		public struct LearningRecord
		{
			public ThreadMetrics PreviousThreadInfo;

			public ThreadMetrics CurrentThreadInfo;

			public int Decision;

			public bool WasNeuralDecision;

			public double Reward;

			public DateTime Timestamp;
		}

		public struct LearningStatistics
		{
			public int TotalLearningSessions;

			public int NeuralDecisionCount;

			public double AverageReward;

			public DateTime LastLearningTime;
		}

		public struct ModelInfo
		{
			public int InputSize;

			public int HiddenSize;

			public int WeightCount;

			public double ConfidenceThreshold;
		}

		public struct DecisionRecord
		{
			public ThreadMetrics ThreadInfo;

			public int HumanDecision;

			public int NeuralDecision;

			public double Confidence;

			public bool AdoptNeural;

			public int FinalDecision;

			public DateTime Timestamp;
		}

		public struct SchedulerStatistics
		{
			public int TotalDecisions;

			public int NeuralAdoptedCount;

			public int HumanAdoptedCount;

			public int DisagreementCount;

			public double NeuralAdoptionRate;

			public double DisagreementRate;

			public double AverageConfidence;

			public DateTime FirstDecisionTime;

			public DateTime LastDecisionTime;
		}

		internal class LSTMCell
		{
			private int inputSize;

			private int hiddenSize;

			private double[,] Wf = new double[0, 0];

			private double[,] Wi = new double[0, 0];

			private double[,] Wc = new double[0, 0];

			private double[,] Wo = new double[0, 0];

			private double[] bf = new double[0];

			private double[] bi = new double[0];

			private double[] bc = new double[0];

			private double[] bo = new double[0];

			private double[] previousHiddenState = new double[0];

			private double[] previousCellState = new double[0];

			public LSTMCell(int inputSize, int hiddenSize)
			{
				this.inputSize = inputSize;
				this.hiddenSize = hiddenSize;
				InitializeWeights();
				previousHiddenState = new double[hiddenSize];
				previousCellState = new double[hiddenSize];
			}

			private void InitializeWeights()
			{
				Random rand = new Random();
				Wf = InitializeWeightMatrix(hiddenSize, inputSize + hiddenSize, rand);
				Wi = InitializeWeightMatrix(hiddenSize, inputSize + hiddenSize, rand);
				Wc = InitializeWeightMatrix(hiddenSize, inputSize + hiddenSize, rand);
				Wo = InitializeWeightMatrix(hiddenSize, inputSize + hiddenSize, rand);
				bf = InitializeBiasVector(hiddenSize, rand);
				bi = InitializeBiasVector(hiddenSize, rand);
				bc = InitializeBiasVector(hiddenSize, rand);
				bo = InitializeBiasVector(hiddenSize, rand);
			}

			private double[,] InitializeWeightMatrix(int rows, int cols, Random rand)
			{
				double[,] array = new double[rows, cols];
				double num = Math.Sqrt(2.0 / (double)(rows + cols));
				for (int i = 0; i < rows; i++)
				{
					for (int j = 0; j < cols; j++)
					{
						array[i, j] = (rand.NextDouble() - 0.5) * 2.0 * num;
					}
				}
				return array;
			}

			private double[] InitializeBiasVector(int size, Random rand)
			{
				double[] array = new double[size];
				for (int i = 0; i < size; i++)
				{
					array[i] = (rand.NextDouble() - 0.5) * 0.1;
				}
				return array;
			}

			public double[] Forward(double[] input)
			{
				double[] array = new double[inputSize + hiddenSize];
				Array.Copy(input, 0, array, 0, inputSize);
				Array.Copy(previousHiddenState, 0, array, inputSize, hiddenSize);
				double[] array2 = Sigmoid(MatrixVectorMultiply(Wf, array, bf));
				double[] array3 = Sigmoid(MatrixVectorMultiply(Wi, array, bi));
				double[] array4 = Tanh(MatrixVectorMultiply(Wc, array, bc));
				double[] array5 = new double[hiddenSize];
				for (int i = 0; i < hiddenSize; i++)
				{
					array5[i] = array2[i] * previousCellState[i] + array3[i] * array4[i];
				}
				double[] array6 = Sigmoid(MatrixVectorMultiply(Wo, array, bo));
				double[] array7 = new double[hiddenSize];
				for (int j = 0; j < hiddenSize; j++)
				{
					array7[j] = array6[j] * Tanh(array5[j]);
				}
				previousCellState = array5;
				previousHiddenState = array7;
				return array7;
			}

			public void Backward(double reward, double[] input)
			{
				double num = 0.001;
				AdjustWeights(Wf, reward * num);
				AdjustWeights(Wi, reward * num);
				AdjustWeights(Wc, reward * num);
				AdjustWeights(Wo, reward * num);
			}

			private void AdjustWeights(double[,] weights, double adjustment)
			{
				for (int i = 0; i < weights.GetLength(0); i++)
				{
					for (int j = 0; j < weights.GetLength(1); j++)
					{
						weights[i, j] += adjustment * (new Random().NextDouble() - 0.5);
					}
				}
			}

			public double[] GetPreviousHiddenState()
			{
				return previousHiddenState;
			}

			private double[] MatrixVectorMultiply(double[,] matrix, double[] vector, double[] bias)
			{
				int length = matrix.GetLength(0);
				int length2 = matrix.GetLength(1);
				double[] array = new double[length];
				for (int i = 0; i < length; i++)
				{
					double num = 0.0;
					for (int j = 0; j < length2; j++)
					{
						num += matrix[i, j] * vector[j];
					}
					array[i] = num + bias[i];
				}
				return array;
			}

			private double[] Sigmoid(double[] x)
			{
				double[] array = new double[x.Length];
				for (int i = 0; i < x.Length; i++)
				{
					array[i] = 1.0 / (1.0 + Math.Exp(0.0 - x[i]));
				}
				return array;
			}

			private double[] Tanh(double[] x)
			{
				double[] array = new double[x.Length];
				for (int i = 0; i < x.Length; i++)
				{
					array[i] = Math.Tanh(x[i]);
				}
				return array;
			}

			private double Tanh(double x)
			{
				return Math.Tanh(x);
			}
		}

		public class SysInfo
		{
			public long accRewordPerS { get; set; }

			public long accQcount { get; set; }

			public bool IsModelSaved { get; set; }

			public long total_runtime { get; set; }

			public long total_instructions { get; set; }

			public long total_llcmiss { get; set; }

			public bool update { get; set; }

			public long total_energy { get; set; }

			public long total_energy_l { get; set; }

			public long total_energy_e { get; set; }

			public double p_e_ratio { get; set; }

			public ThreadLoadManager4l CoreLoadSeq { get; set; }

			public int Lock1 { get; set; }

			public int Max_gid { get; set; }

			public int Min_gid { get; set; }

			public long Datetime { get; set; }

			public long Maxtime { get; set; }

			public long Qtimeavg { get; set; }

			public long Qtimeacc { get; set; }

			public long Qtimecount { get; set; }

			public uint Availaff { get; set; }

			public uint Availaff1 { get; set; }

			public GroupInfo Groupset { get; set; }

			public int Counter_sys_enabled { get; set; }

			public long Acc_perflvl { get; set; }

			public long Avg_perflvl { get; set; }

			public long Acc_perfcnt { get; set; }

			public long Acc_efflvl { get; set; }

			public long Avg_efflvl { get; set; }

			public long Acc_effcnt { get; set; }

			public SysInfo()
			{
			}

			public SysInfo(int max_gid, int min_gid, uint availaff, uint availaff1)
			{
				Max_gid = max_gid;
				Min_gid = min_gid;
				Groupset = null;
				Lock1 = 0;
				Datetime = DateTime.Now.Ticks;
				Maxtime = 0L;
				Availaff = availaff;
				Qtimeavg = 1500L;
				Availaff1 = availaff1;
				CoreLoadSeq = new ThreadLoadManager4l();
				update = true;
				p_e_ratio = 0.0;
				total_runtime = 0L;
				total_llcmiss = 0L;
				IsModelSaved = false;
			}
		}

		public class GroupInfo
		{
			public int Gid;

			public long B_runtime { get; set; }

			public long B_waittime { get; set; }

			public long B_affinity { get; set; }

			public long B_available { get; set; }

			public long B_utilization { get; set; }

			public long L_runtime { get; set; }

			public long L_waittime { get; set; }

			public long L_affinity { get; set; }

			public long L_available { get; set; }

			public long L_utilization { get; set; }

			public long G_runtime { get; set; }

			public long G_waittime { get; set; }

			public long G_available { get; set; }

			public long G_affinity { get; set; }

			public long G_utilization { get; set; }

			public long Datetime { get; set; }

			public long Intval { get; set; }

			public GroupInfo Next { get; set; }

			public ThreadInfoSimp ThreadSet1 { get; set; }

			public ThreadInfoSimp ThreadSet2 { get; set; }

			public int OnlyBcore { get; set; }

			public GroupInfo()
			{
			}

			public GroupInfo(int gid, long b_runtime, long b_waittime, long b_affinity, long l_runtime, long l_waittime, long l_affinity, long g_runtime, long g_waittime, long g_affinity, long datetime, long intval)
			{
				Gid = gid;
				B_runtime = b_runtime;
				B_waittime = b_waittime;
				B_affinity = b_affinity;
				L_runtime = l_runtime;
				L_waittime = l_waittime;
				L_affinity = l_affinity;
				G_runtime = g_runtime;
				G_waittime = g_waittime;
				G_affinity = g_affinity;
				Datetime = datetime;
				Intval = intval;
				Next = null;
				ThreadSet1 = null;
				ThreadSet2 = null;
				B_available = 1L;
				L_available = 1L;
				G_available = 1L;
				B_utilization = 0L;
				L_utilization = 0L;
				G_utilization = 0L;
				OnlyBcore = 0;
			}
		}

		public class ThreadInfoSimp
		{
			public int Tid { get; set; }

			public long InsPressure { get; set; }

			public long InsPressure1 { get; set; }

			public long InsPressure2 { get; set; }

			public long Ins_per_count { get; set; }

			public long Ipc { get; set; }

			public int CoreType { get; set; }

			public int Group { get; set; }

			public ThreadInfo Belong2thread { get; set; }

			public ThreadInfoSimp Next { get; set; }

			public ThreadInfoSimp()
			{
			}

			public ThreadInfoSimp(int tid, long insPressure, long ipc, long ins_per_count, int coretype, int group, ThreadInfo belong2thread)
			{
				Tid = tid;
				InsPressure = insPressure;
				Ipc = ipc;
				Ins_per_count = ins_per_count;
				Group = group;
				CoreType = coretype;
				Belong2thread = belong2thread;
				Next = null;
				InsPressure1 = 0L;
				InsPressure2 = 0L;
			}
		}

		public class StructThreadInfo
		{
			public class BasicInfo
			{
				public long ThreadId { get; set; }

				public long DateTime { get; set; }

				public long CoreType { get; set; }

				public long IsSched { get; set; }

				public void SchedulingThread()
				{
				}
			}

			public class IpcProcessor
			{
				public long Count { get; set; }

				public long Instructions { get; set; }

				public long Cycles { get; set; }

				public long AvgIpc { get; set; }

				public void Reset()
				{
					Count = 0L;
					Instructions = 0L;
					Cycles = 0L;
				}

				public long CalcIpc()
				{
					if (Cycles == 0L)
					{
						return AvgIpc;
					}
					if (Count > 1000 || Instructions > 300000)
					{
						AvgIpc = 100 * Instructions / Cycles;
						Reset();
						return AvgIpc;
					}
					return AvgIpc;
				}
			}

			public class Ipc
			{
				public IpcProcessor IpcBig { get; set; } = new IpcProcessor();

				public IpcProcessor IpcLittle { get; set; } = new IpcProcessor();

				public long AvgIpcRatio { get; set; }

				public void ResetRatio()
				{
					AvgIpcRatio = 0L;
				}

				public long CalcIpcRatio()
				{
					if (IpcLittle.AvgIpc == 0L)
					{
						return AvgIpcRatio;
					}
					AvgIpcRatio = 100 * IpcBig.AvgIpc / IpcLittle.AvgIpc;
					return AvgIpcRatio;
				}

				public void ResetAll()
				{
					IpcBig.Reset();
					IpcLittle.Reset();
					ResetRatio();
				}
			}

			public class ExecTime
			{
				public long TotalTime { get; set; }

				public long AverageTime { get; set; }

				public long MaxTime { get; set; }

				public long MinTime { get; set; }
			}

			public class Cache
			{
				public long CacheHits { get; set; }

				public long CacheMisses { get; set; }

				public double HitRate { get; set; }
			}

			public BasicInfo ThreadBasicInfo { get; set; } = new BasicInfo();

			public Ipc IpcInfo { get; set; } = new Ipc();

			public ExecTime ExecutionTime { get; set; } = new ExecTime();

			public Cache CacheInfo { get; set; } = new Cache();
		}

		public class ThreadInfo
		{
			public long InfluenceIndex { get; set; }

			public double DataLinkage { get; set; }

			public double Elasticity { get; set; }

			public double DataLinkage1 { get; set; }

			public double Elasticity1 { get; set; }

			public long CodeFootPrint_counter2 { get; set; }

			public long CodeFootPrint_counter1 { get; set; }

			public long CodeFootPrint { get; set; }

			public double UserModeRatio { get; set; }

			public string Type { get; set; }

			public int Tid { get; set; }

			public long DateTime { get; set; }

			public long DateTime4interval { get; set; }

			public long DateTime4sched { get; set; }

			public long IntVal { get; set; }

			public long Count_internal { get; set; }

			public long Count_internal1 { get; set; }

			public long Count_internal2 { get; set; }

			public long Count_sample { get; set; }

			public long Count_sample1 { get; set; }

			public long RunTime { get; set; }

			public long WaitTime { get; set; }

			public long wait_ratio { get; set; }

			public long Duration { get; set; }

			public long Duration_ing { get; set; }

			public long Instruction { get; set; }

			public double InsPressure { get; set; }

			public long Utilization { get; set; }

			public long L1_miss { get; set; }

			public long L1_miss1 { get; set; }

			public long L2_miss { get; set; }

			public long L3_miss { get; set; }

			public long L3_miss1 { get; set; }

			public float Block_avg_ins { get; set; }

			public float Avg_Block_avg_ins { get; set; }

			public float Acc_Block_avg_ins { get; set; }

			public long Ins_sample { get; set; }

			public long Branchs_taken { get; set; }

			public long L4_miss { get; set; }

			public double avgruntime { get; set; }

			public long avgruntime_total { get; set; }

			public long avgruntime_count { get; set; }

			public long Miss_rate { get; set; }

			public int CoreType { get; set; }

			public int PrevCoreType { get; set; }

			public long Dummy { get; set; }

			public long Instruction_big { get; set; }

			public double Clock { get; set; }

			public long Ipc { get; set; }

			public int Is_important_threads { get; set; }

			public long rate_mem { get; set; }

			public long Ins_per_count { get; set; }

			public long Ins_flow { get; set; }

			public long Ins_flow1 { get; set; }

			public long Ins_issue { get; set; }

			public long Ins_retire { get; set; }

			public long Br_eff { get; set; }

			public long PriorityAcc { get; set; }

			public long PriorityAcc1 { get; set; }

			public long Ins_total { get; set; }

			public uint Affinity { get; set; }

			public int Group { get; set; }

			public int Group_original { get; set; }

			public int Sched { get; set; }

			public int Lockdata { get; set; }

			public double Ins_big { get; set; }

			public double Ins_big1 { get; set; }

			public long Avg_ins_big { get; set; }

			public long Acc_ins_big { get; set; }

			public long ratio3 { get; set; }

			public long ratio4 { get; set; }

			public long Ins_little { get; set; }

			public long Clock_big { get; set; }

			public long Clock_litte { get; set; }

			public long Ipc_big { get; set; }

			public long Ipc_ratio { get; set; }

			public long Ipc_ratio1 { get; set; }

			public long Ipc_ratio2 { get; set; }

			public long Ipc_reset_count { get; set; }

			public long threshold { get; set; }

			public long SchedType { get; set; }

			public bool WasNeuroDecision { get; set; }

			public long ThreadType { get; set; }

			public long prevThreadType { get; set; }

			public float newScore { get; set; }

			public float oldScore { get; set; }

			public long maxins { get; set; }

			public int[] inputs { get; set; }

			public int[] previnputs { get; set; }

			public int rs { get; set; }

			public int prevrs { get; set; }

			public long trial_count { get; set; }

			public long trial_switch { get; set; }

			public long sched_count { get; set; }

			public long sched_little_count { get; set; }

			public long rfo_counters { get; set; }

			public long l2ref_counters { get; set; }

			public long rfo_counters1 { get; set; }

			public long rfo_counters2 { get; set; }

			public long rfo_counters3 { get; set; }

			public long l2ref_counters1 { get; set; }

			public long l2ref_counters2 { get; set; }

			public long l2ref_counters3 { get; set; }

			public long demoteacc { get; set; }

			public long avg_ins { get; set; }

			public long avg_ins1 { get; set; }

			public long avg_ins2 { get; set; }

			public long avg_ins3 { get; set; }

			public long l2cr_counters { get; set; }

			public long rfo_ratio { get; set; }

			public long rfo_ratio1 { get; set; }

			public long rfo_ratio2 { get; set; }

			public long rfo_ratio3 { get; set; }

			public long ins_little { get; set; }

			public long clock_little { get; set; }

			public long ipc_big { get; set; }

			public ProcessInfo Processinfo { get; set; }

			public GroupInfo Groupinfo { get; set; }

			public ThreadInfoSimp SimpThread { get; set; }

			public PrevSchedInfo PrevSchedInfo { get; set; }

			public ThreadInfo NextThread { get; set; }

			public long sched_correct { get; set; }

			public long sched_wrong { get; set; }

			public long sched_corr_ratio { get; set; }

			public long update_signal { get; set; }

			public int Perflvl { get; set; }

			public int Efflvl { get; set; }

			public ThreadInfo()
			{
			}

			public ThreadInfo(int tid, long datetime, long intval, long count_internal, long count_sample, long runtime, long waittime, long duration, long instruction, long inspressure, long utilization, int coreType, long dummy, long l1_miss, long l2_miss, long miss_rate, long instruction_big, long clock, long ipc, long priorityacc, long ins_per_count, uint affinity, int group, ThreadInfoSimp infoSimp, long ins_issue, long ins_retire, long br_eff, int sched, long duration_ing, long ins_total, int group_original, long ins_big, long ins_little, long clock_big, long clock_little, long ipc_ratio, long ipc_reset_count, long datetime4interval, long dateTime4sched)
			{
				Tid = tid;
				DateTime = datetime;
				IntVal = intval;
				Count_internal = count_internal;
				Count_sample = count_sample;
				RunTime = runtime;
				WaitTime = waittime;
				Duration = duration;
				Duration_ing = duration_ing;
				Instruction = instruction;
				InsPressure = 749.0;
				Utilization = utilization;
				Instruction_big = instruction_big;
				Clock = 0.0;
				Ins_total = ins_total;
				Ipc = ipc;
				PriorityAcc = priorityacc;
				CoreType = coreType;
				Ins_per_count = ins_per_count;
				Ins_issue = ins_issue;
				Ins_retire = ins_retire;
				Dummy = dummy;
				Sched = sched;
				Lockdata = 0;
				L1_miss = l1_miss;
				Group_original = group_original;
				L2_miss = l2_miss;
				Miss_rate = miss_rate;
				Affinity = affinity;
				Br_eff = br_eff;
				Group = group;
				Groupinfo = null;
				Processinfo = null;
				SimpThread = infoSimp;
				NextThread = null;
				PrevCoreType = 0;
				Ins_big = 0.0;
				Ins_big1 = 0.0;
				Ins_little = ins_little;
				Clock_big = clock_big;
				Clock_litte = 100L;
				Ipc_ratio = Ipc_ratio;
				Ipc_reset_count = Ipc_reset_count;
				threshold = 60000L;
				prevThreadType = 1L;
				ThreadType = 1L;
				Ipc_ratio1 = 0L;
				Ipc_ratio2 = 0L;
				newScore = 0f;
				oldScore = 0f;
				sched_count = 0L;
				sched_little_count = 0L;
				inputs = new int[7];
				previnputs = new int[7];
				trial_switch = 0L;
				trial_count = 0L;
				PrevSchedInfo = new PrevSchedInfo();
				sched_correct = 0L;
				sched_wrong = 0L;
				sched_corr_ratio = 100L;
				rfo_counters = 0L;
				l2ref_counters = 0L;
				rfo_counters1 = 0L;
				l2ref_counters1 = 0L;
				l2cr_counters = 0L;
				rfo_ratio = 1000L;
				rfo_ratio1 = 1000L;
				rfo_ratio2 = 1000L;
				rfo_ratio3 = 1000L;
				update_signal = 0L;
				ipc_big = 100L;
				ratio3 = 50000L;
				ratio4 = 100L;
				SchedType = 1L;
				Is_important_threads = 1;
				WasNeuroDecision = true;
				Type = null;
				UserModeRatio = 0.5;
				DataLinkage = 0.0;
				DateTime4sched = dateTime4sched;
				Elasticity = 0.0;
				InfluenceIndex = 0L;
			}

			public long CalcRatio(long data1, long data2, long source)
			{
				if (data2 > 0)
				{
					return data1 / data2;
				}
				return source;
			}

			public double CalcRatio1(long data1, long data2, double source)
			{
				if (data2 > 0)
				{
					return (double)data1 / (double)data2;
				}
				return source;
			}
		}

		public class PrevSchedInfo
		{
			public long PrevCoreType { get; set; }

			public long Ipc { get; set; }

			public long Ins_per_count { get; set; }

			public long InsPressure { get; set; }

			public long Clock { get; set; }

			public long Ins_big { get; set; }

			public long Clock_litte { get; set; }

			public long Ipc_reset_count { get; set; }
		}

		public class ProcessInfo
		{
			public bool UpdateMaxThread { get; set; }

			public string MaxThreadType { get; set; }

			public int MaxThreadId { get; set; }

			public int MaxThreadId4lat { get; set; }

			public int Initial_state { get; set; }

			public long Maxins { get; set; }

			public long MaxinsOption1 { get; set; }

			public long MaxinsOption2 { get; set; }

			public long MaxinsCount { get; set; }

			public long MaxinsDatetime { get; set; }

			public long MaxinsLock { get; set; }

			public long Observation_count { get; set; }

			public int Neuro_on { get; set; }

			public int Neuro_count { get; set; }

			public int Pid { get; set; }

			public long DateTime { get; set; }

			public long IntVal { get; set; }

			public long Count { get; set; }

			public long RunTime { get; set; }

			public long WaitTime { get; set; }

			public long Duration { get; set; }

			public long Instruction { get; set; }

			public long Instruction_little { get; set; }

			public long Instruction_ratio { get; set; }

			public long Avg_Inspressure { get; set; }

			public int CoreType { get; set; }

			public int Lock1 { get; set; }

			public long sched_correct { get; set; }

			public long sched_wrong { get; set; }

			public long sched_corr_ratio { get; set; }

			public int sched_revert { get; set; }

			public int Index { get; set; }

			public int Perflvl { get; set; }

			public long rfo_counters { get; set; }

			public long l2ref_counters { get; set; }

			public long rfo_counters1 { get; set; }

			public long rfo_counters2 { get; set; }

			public long rfo_counters3 { get; set; }

			public long l2ref_counters1 { get; set; }

			public long l2ref_counters2 { get; set; }

			public long l2ref_counters3 { get; set; }

			public long avg_ins { get; set; }

			public long avg_ins1 { get; set; }

			public long avg_ins2 { get; set; }

			public long avg_ins3 { get; set; }

			public long l2cr_counters { get; set; }

			public long rfo_ratio { get; set; }

			public long maxtime { get; set; }

			public long maxtime1 { get; set; }

			public long maxtime2 { get; set; }

			public long runtime_counter { get; set; }

			public long runtime { get; set; }

			public long runtime_counter1 { get; set; }

			public long runtime1 { get; set; }

			public long datetime_elapse { get; set; }

			public long update_signal { get; set; }

			public int option { get; set; }

			public ProcessInfo NextProcess { get; set; }

			public ThreadInfoSimp ThreadSet { get; set; }

			public long Activethread4perf_count { get; set; }

			public long Activethread4eff_count { get; set; }

			public long Activethreadstatcnt { get; set; }

			public long Acc_Activethreadstatcnt { get; set; }

			public long Activethreadstat_datetime { get; set; }

			public int Activethreadstat_lock { get; set; }

			public long Avg_activethreadstatcnt { get; set; }

			public int SchedMode { get; set; }

			public ProcessInfo()
			{
			}

			public ProcessInfo(int pid, long datetime, long intval, long count, long runtime, long waittime, long duration, long instruction, int coreType, int lock1, long avg_Inspressure, long instruction_little, long instruction_ratio, int index, int perflvl, long maxinsdatetime, long activethreadstat_datetime)
			{
				Pid = pid;
				DateTime = datetime;
				IntVal = intval;
				Count = count;
				RunTime = runtime;
				WaitTime = waittime;
				Duration = duration;
				Instruction = instruction;
				Avg_Inspressure = avg_Inspressure;
				Perflvl = perflvl;
				Neuro_on = 0;
				Neuro_count = 0;
				sched_correct = 0L;
				sched_wrong = 0L;
				sched_revert = 0;
				MaxinsDatetime = maxinsdatetime;
				sched_corr_ratio = 100L;
				CoreType = coreType;
				Lock1 = lock1;
				NextProcess = null;
				ThreadSet = null;
				rfo_counters = 0L;
				rfo_counters1 = 0L;
				l2cr_counters = 0L;
				l2ref_counters = 0L;
				l2ref_counters1 = 0L;
				rfo_ratio = 0L;
				Avg_Inspressure = avg_Inspressure;
				Instruction_little = instruction_little;
				Instruction_ratio = instruction_ratio;
				Index = index;
				maxtime = 100L;
				maxtime1 = 100L;
				option = 1;
				update_signal = 0L;
				MaxinsLock = 0L;
				MaxinsOption1 = 0L;
				MaxinsOption2 = 0L;
				Maxins = 500000L;
				Activethreadstatcnt = 0L;
				Activethreadstat_datetime = activethreadstat_datetime;
				Activethreadstat_lock = 0;
				Avg_activethreadstatcnt = 100L;
				Initial_state = 1;
				MaxThreadId4lat = -1;
				MaxThreadId = -1;
				UpdateMaxThread = true;
				MaxThreadType = null;
			}
		}

		public class CoreInfo
		{
			public Dictionary<int, long> threadContrib = new Dictionary<int, long>();

			public long accRewardPerQ { get; set; }

			public long accRuntimePerQ { get; set; }

			public long mem_ordering_count { get; set; }

			public long mem_ordering_count_e { get; set; }

			public long mem_ordering_count_l { get; set; }

			public float mem_ordering { get; set; }

			public float perf4c { get; set; }

			public float missrateratio4c { get; set; }

			public float ipc4c { get; set; }

			public long missrate4c { get; set; }

			public long missrate4c_e { get; set; }

			public long missrate4c_l { get; set; }

			public long load { get; set; }

			public long load_e { get; set; }

			public long load_l { get; set; }

			public long store { get; set; }

			public long store_e { get; set; }

			public long store_l { get; set; }

			public long cycles4sys { get; set; }

			public long cycles4sys_e { get; set; }

			public long cycles4sys_l { get; set; }

			public long instructions4sys { get; set; }

			public long instructions4sys_e { get; set; }

			public long instructions4sys_l { get; set; }

			public long total_energy { get; set; }

			public long total_energy_e { get; set; }

			public long total_energy_l { get; set; }

			public long AccMaxTime { get; set; }

			public long AvgMaxTime { get; set; }

			public NumberProcessor numberProcessor { get; set; }

			public int Cid { get; set; }

			public long DateTime { get; set; }

			public long DateTime4sched { get; set; }

			public long DateTime4q { get; set; }

			public long IntVal { get; set; }

			public long Count { get; set; }

			public int CounterEnabled { get; set; }

			public long Idletime { get; set; }

			public uint Affinity { get; set; }

			public uint Affinity_origin { get; set; }

			public long RunTime { get; set; }

			public long RunTime4queque { get; set; }

			public long RunTime4queque4sched { get; set; }

			public long RunTime4usage { get; set; }

			public long accRunTime4usage { get; set; }

			public long Threadcount { get; set; }

			public long SustainedThreadcount { get; set; }

			public long SustainedThreadcount4sched { get; set; }

			public long WaitTime { get; set; }

			public long Duration { get; set; }

			public long Intvaltime { get; set; }

			public long QueueInterval { get; set; }

			public long Utilization { get; set; }

			public long Utilization4sched { get; set; }

			public long Utilization4q { get; set; }

			public long Utilization4q4sched { get; set; }

			public int P_state { get; set; }

			public long Instruction_e { get; set; }

			public long Cycle_e { get; set; }

			public long Instruction_l { get; set; }

			public long Cycle_l { get; set; }

			public long Instruction { get; set; }

			public long Cycle { get; set; }

			public long Ipc { get; set; }

			public long Register1 { get; set; }

			public long Register2 { get; set; }

			public long Register3 { get; set; }

			public long Register4 { get; set; }

			public long Register5 { get; set; }

			public long Register6 { get; set; }

			public long Register1_e { get; set; }

			public long Register2_e { get; set; }

			public long Register3_e { get; set; }

			public long Register4_e { get; set; }

			public long Register5_e { get; set; }

			public long Register6_e { get; set; }

			public long Register1_l { get; set; }

			public long Register2_l { get; set; }

			public long Register3_l { get; set; }

			public long Register4_l { get; set; }

			public long Register5_l { get; set; }

			public long Register6_l { get; set; }

			public ThreadExecutionRegistry threadexecinfo { get; set; }

			public long Accthreadcount { get; set; }

			public long Avgthreadcount { get; set; }

			public long Avgthreadtime { get; set; }

			public Node2 ThreadSet { get; set; }

			public ThreadInfoSimp Threadinfosimp { get; set; }

			public CoreInfo()
			{
			}

			public CoreInfo(int cid, long datetime, long intval, long count, long runtime, long waittime, long duration, long utilization, uint affinity, uint affinity_origin, long dateTime4q, long dateTime4sched)
			{
				Cid = cid;
				DateTime = datetime;
				IntVal = intval;
				RunTime = runtime;
				WaitTime = waittime;
				Duration = duration;
				Count = count;
				Utilization = utilization;
				Affinity = affinity;
				Affinity_origin = affinity_origin;
				ThreadSet = null;
				Threadinfosimp = null;
				Idletime = 0L;
				RunTime4queque = 0L;
				Threadcount = 0L;
				SustainedThreadcount = 0L;
				Utilization4q = 0L;
				DateTime4q = dateTime4q;
				CounterEnabled = 0;
				DateTime4sched = dateTime4sched;
				threadexecinfo = new ThreadExecutionRegistry();
				Accthreadcount = 0L;
				Avgthreadcount = 0L;
				Avgthreadtime = 0L;
				numberProcessor = new NumberProcessor();
				AccMaxTime = 0L;
				AvgMaxTime = 0L;
				instructions4sys = 0L;
				instructions4sys_e = 0L;
				instructions4sys_l = 0L;
				total_energy = 0L;
				total_energy_e = 0L;
				total_energy_l = 0L;
				RunTime4usage = 0L;
				accRunTime4usage = 0L;
				cycles4sys = 0L;
				cycles4sys_e = 0L;
				cycles4sys_l = 0L;
				missrate4c = 0L;
				missrate4c_e = 0L;
				missrate4c_l = 0L;
				ipc4c = 0f;
				missrateratio4c = 0f;
				mem_ordering = 0f;
				perf4c = 0f;
				load = 0L;
				load_e = 0L;
				load_l = 0L;
				store = 0L;
				store_e = 0L;
				store_l = 0L;
				mem_ordering_count = 0L;
				mem_ordering_count_e = 0L;
				mem_ordering_count_l = 0L;
				QueueInterval = 0L;
			}

			public float CalcRatio1(long data1, long data2, float source)
			{
				if (data2 > 0)
				{
					return (float)data1 / (float)data2;
				}
				return source;
			}
		}

		public class CoreQueue
		{
			public int Cid { get; set; }

			public long DateTime { get; set; }

			public long IntVal { get; set; }

			public long RunTime { get; set; }

			public long WaitTime { get; set; }

			public long Duration { get; set; }

			public Node2 Next { get; set; }

			public CoreQueue()
			{
			}

			public CoreQueue(int cid, long datetime, long intval, long runtime, long waittime, long duration)
			{
				Cid = cid;
				DateTime = datetime;
				IntVal = intval;
				RunTime = runtime;
				WaitTime = waittime;
				Duration = duration;
				Next = null;
			}
		}

		public class Node2
		{
			public int Id { get; set; }

			public long Value1 { get; set; }

			public int Value2 { get; set; }

			public Node2 Next { get; set; }

			public Node2()
			{
			}

			public Node2(int id, long value1, int value2)
			{
				Id = id;
				Value1 = value1;
				Value2 = value2;
				Next = null;
			}
		}

		public class Node
		{
			public int Id { get; set; }

			public int Value { get; set; }

			public Node Next { get; set; }

			public Node()
			{
			}

			public Node(int id, int value)
			{
				Id = id;
				Value = value;
				Next = null;
			}
		}

		public class Node1
		{
			public int Id { get; set; }

			public long Acc_instruction_b { get; set; }

			public long Acc_aclk_b { get; set; }

			public long Acc_load_b { get; set; }

			public long Acc_store_b { get; set; }

			public long Acc_load_miss_b { get; set; }

			public long Acc_br_b { get; set; }

			public long Acc_runtime_b { get; set; }

			public long Cnt_b { get; set; }

			public long Acc_instruction_l { get; set; }

			public long Acc_aclk_l { get; set; }

			public long Acc_load_l { get; set; }

			public long Acc_load_l_perm { get; set; }

			public long Last_duration { get; set; }

			public long Now_duration { get; set; }

			public long Acc_store_l { get; set; }

			public long Acc_store_l_perm { get; set; }

			public long Acc_load_miss_l { get; set; }

			public long Acc_br_l { get; set; }

			public long Acc_runtime_l { get; set; }

			public long Cnt_l { get; set; }

			public long Ipc_b { get; set; }

			public long Max_ipc_b { get; set; }

			public long Ipc_l { get; set; }

			public long Ipc_l_perm { get; set; }

			public long Max_ipc_l { get; set; }

			public long Ipc_ratio { get; set; }

			public long Br_ratio { get; set; }

			public long Br_load_ratio { get; set; }

			public long Load_miss_ratio_b { get; set; }

			public long Min_load_miss_ratio_b { get; set; }

			public long Load_miss_ratio_l { get; set; }

			public long Avg_runtime_b { get; set; }

			public long Avg_runtime_l { get; set; }

			public long Avg_freq_b { get; set; }

			public long Avg_freq_l { get; set; }

			public long Max_ins { get; set; }

			public long Lock_data { get; set; }

			public long Tag { get; set; }

			public long Duration { get; set; }

			public long Reset_count { get; set; }

			public uint Affinity { get; set; }

			public long Residence { get; set; }

			public Node1 Next { get; set; }

			public Node1()
			{
			}

			public Node1(int id, long acc_instruction_b, long acc_aclk_b, long acc_load_b, long acc_store_b, long acc_load_miss_b, long acc_br_b, long acc_runtime_b, long cnt_b, long acc_instruction_l, long acc_aclk_l, long acc_load_l, long acc_load_l_perm, long last_duration, long now_duration, long acc_store_l, long acc_store_l_perm, long acc_load_miss_l, long acc_br_l, long acc_runtime_l, long cnt_l, long ipc_b, long max_ipc_b, long ipc_l, long ipc_l_perm, long max_ipc_l, long ipc_ratio, long br_ratio, long br_load_ratio, long load_miss_ratio_b, long min_load_miss_ratio_b, long load_miss_ratio_l, long avg_runtime_b, long avg_runtime_l, long avg_freq_b, long avg_freq_l, long max_ins, long lock_data, long tag, long duration, long reset_count, uint affinity, long residence)
			{
				Id = id;
				Acc_instruction_b = acc_instruction_b;
				Acc_aclk_b = acc_aclk_b;
				Acc_load_b = acc_load_b;
				Acc_store_b = acc_store_b;
				Acc_load_miss_b = acc_load_miss_b;
				Acc_br_b = acc_br_b;
				Acc_runtime_b = acc_runtime_b;
				Cnt_b = cnt_b;
				Acc_instruction_l = acc_instruction_l;
				Acc_aclk_l = acc_aclk_l;
				Acc_load_l = acc_load_l;
				Acc_load_l_perm = acc_load_l_perm;
				Last_duration = last_duration;
				Now_duration = now_duration;
				Acc_store_l = acc_store_l;
				Acc_store_l_perm = acc_store_l_perm;
				Acc_load_miss_l = acc_load_miss_l;
				Acc_br_l = acc_br_l;
				Acc_runtime_l = acc_runtime_l;
				Cnt_l = cnt_l;
				Ipc_b = ipc_b;
				Max_ipc_b = max_ipc_b;
				Ipc_l = ipc_l;
				Ipc_l_perm = ipc_l_perm;
				Max_ipc_l = max_ipc_l;
				Ipc_ratio = ipc_ratio;
				Br_ratio = br_ratio;
				Br_load_ratio = br_load_ratio;
				Load_miss_ratio_b = load_miss_ratio_b;
				Min_load_miss_ratio_b = min_load_miss_ratio_b;
				Load_miss_ratio_l = load_miss_ratio_l;
				Avg_runtime_b = avg_runtime_b;
				Avg_runtime_l = avg_runtime_l;
				Avg_freq_b = avg_freq_b;
				Avg_freq_l = avg_freq_l;
				Max_ins = max_ins;
				Lock_data = lock_data;
				Tag = tag;
				Duration = duration;
				Reset_count = reset_count;
				Affinity = affinity;
				Residence = residence;
				Next = null;
			}
		}

		public class NodeT
		{
			public int PId { get; set; }

			public long Data { get; set; }

			public NodeT Next { get; set; }

			public NodeT()
			{
			}

			public NodeT(int pid, long data, NodeT next)
			{
				PId = pid;
				Data = data;
				Next = null;
			}
		}

		public class NodeP
		{
			public int PId { get; set; }

			public long Ins_total { get; set; }

			public long Store_total { get; set; }

			public long Count_total { get; set; }

			public long Intval { get; set; }

			public long Nonstore_store_ratio { get; set; }

			public long Usr_sum { get; set; }

			public long Usr_count { get; set; }

			public long Usr_ratio { get; set; }

			public long Residence { get; set; }

			public long Residence1 { get; set; }

			public Node2 Compare { get; set; }

			public Node2 Compare_final { get; set; }

			public NodeP Next { get; set; }

			public NodeP()
			{
			}

			public NodeP(int pid, long ins_total, long store_total, long count_total, long intval, long nonstore_store_ratio, long usr_sum, long usr_count, long usr_ratio, long residence, long residence1, Node2 compare, Node2 compare_final)
			{
				PId = pid;
				Ins_total = ins_total;
				Store_total = store_total;
				Count_total = count_total;
				Intval = intval;
				Nonstore_store_ratio = nonstore_store_ratio;
				Usr_sum = usr_sum;
				Usr_count = usr_count;
				Usr_ratio = usr_ratio;
				Residence = residence;
				Residence1 = residence1;
				Compare = compare;
				Compare_final = compare_final;
				Next = null;
			}
		}

		public class ThreadLoadManager4b
		{
			public class ThreadLoadNode
			{
				public int ThreadId { get; }

				public long Load { get; }

				public ThreadLoadNode(int threadId, long load)
				{
					ThreadId = threadId;
					Load = load;
				}
			}

			private class NodeComparer : IComparer<ThreadLoadNode>
			{
				public int Compare(ThreadLoadNode x, ThreadLoadNode y)
				{
					int num = y.Load.CompareTo(x.Load);
					if (num != 0)
					{
						return num;
					}
					return x.ThreadId.CompareTo(y.ThreadId);
				}
			}

			private readonly Dictionary<int, ThreadLoadNode> _dict = new Dictionary<int, ThreadLoadNode>();

			private readonly SortedSet<ThreadLoadNode> _sortedSet = new SortedSet<ThreadLoadNode>(new NodeComparer());

			public int Count => _dict.Count;

			public void AddOrUpdate(int threadId, long load)
			{
				if (_dict.TryGetValue(threadId, out var value))
				{
					_sortedSet.Remove(value);
					ThreadLoadNode threadLoadNode = new ThreadLoadNode(threadId, load);
					_dict[threadId] = threadLoadNode;
					_sortedSet.Add(threadLoadNode);
				}
				else
				{
					ThreadLoadNode threadLoadNode2 = new ThreadLoadNode(threadId, load);
					_dict.Add(threadId, threadLoadNode2);
					_sortedSet.Add(threadLoadNode2);
				}
			}

			public List<(int threadId, long load)> TakeTopN(int n)
			{
				return (from node in _sortedSet.Take(n)
					select (ThreadId: node.ThreadId, Load: node.Load)).ToList();
			}

			public List<(int threadId, long load)> TakeBottomN(int n)
			{
				return (from node in _sortedSet.Reverse().Take(n)
					select (ThreadId: node.ThreadId, Load: node.Load)).ToList();
			}

			public int IsInTopPos(ThreadLoadNode node, int pos)
			{
				if (node == null)
				{
					return 0;
				}
				if (!_dict.TryGetValue(node.ThreadId, out var value) || value.Load != node.Load)
				{
					return 0;
				}
				int num = 0;
				foreach (ThreadLoadNode item in _sortedSet)
				{
					if (item.ThreadId == node.ThreadId && item.Load == node.Load)
					{
						return (num < pos) ? 1 : 0;
					}
					num++;
					if (num >= pos)
					{
						break;
					}
				}
				return 0;
			}

			public int GetNodePosition(int threadId)
			{
				if (!_dict.ContainsKey(threadId))
				{
					return -1;
				}
				int num = 0;
				foreach (ThreadLoadNode item in _sortedSet)
				{
					if (item.ThreadId == threadId)
					{
						return num;
					}
					num++;
				}
				return -1;
			}

			public void Clear()
			{
				_dict.Clear();
				_sortedSet.Clear();
			}

			public List<ThreadLoadNode> GetAllNodes()
			{
				return _sortedSet.ToList();
			}
		}

		public class ThreadLoadManager4l
		{
			public class ThreadLoadNode
			{
				public int ThreadId { get; }

				public long Load { get; }

				public ThreadLoadNode(int threadId, long load)
				{
					ThreadId = threadId;
					Load = load;
				}
			}

			private class NodeComparer : IComparer<ThreadLoadNode>
			{
				public int Compare(ThreadLoadNode x, ThreadLoadNode y)
				{
					int num = x.Load.CompareTo(y.Load);
					if (num != 0)
					{
						return num;
					}
					return x.ThreadId.CompareTo(y.ThreadId);
				}
			}

			private readonly Dictionary<int, ThreadLoadNode> _dict = new Dictionary<int, ThreadLoadNode>();

			private readonly SortedSet<ThreadLoadNode> _sortedSet = new SortedSet<ThreadLoadNode>(new NodeComparer());

			public int Count => _dict.Count;

			public void AddOrUpdate(int threadId, long load)
			{
				if (_dict.TryGetValue(threadId, out var value))
				{
					_sortedSet.Remove(value);
					ThreadLoadNode threadLoadNode = new ThreadLoadNode(threadId, load);
					_dict[threadId] = threadLoadNode;
					_sortedSet.Add(threadLoadNode);
				}
				else
				{
					ThreadLoadNode threadLoadNode2 = new ThreadLoadNode(threadId, load);
					_dict.Add(threadId, threadLoadNode2);
					_sortedSet.Add(threadLoadNode2);
				}
			}

			public List<(int threadId, long load)> TakeTopN(int n)
			{
				return (from node in _sortedSet.Take(n)
					select (ThreadId: node.ThreadId, Load: node.Load)).ToList();
			}

			public List<(int threadId, long load)> TakeBottomN(int n)
			{
				return (from node in _sortedSet.Reverse().Take(n)
					select (ThreadId: node.ThreadId, Load: node.Load)).ToList();
			}

			public int IsInTopPos(ThreadLoadNode node, int pos)
			{
				if (node == null)
				{
					return 0;
				}
				if (!_dict.TryGetValue(node.ThreadId, out var value) || value.Load != node.Load)
				{
					return 0;
				}
				int num = 0;
				foreach (ThreadLoadNode item in _sortedSet)
				{
					if (item.ThreadId == node.ThreadId && item.Load == node.Load)
					{
						return (num < pos) ? 1 : 0;
					}
					num++;
					if (num >= pos)
					{
						break;
					}
				}
				return 0;
			}

			public int GetNodePosition(int threadId)
			{
				if (!_dict.ContainsKey(threadId))
				{
					return -1;
				}
				int num = 0;
				foreach (ThreadLoadNode item in _sortedSet)
				{
					if (item.ThreadId == threadId)
					{
						return num;
					}
					num++;
				}
				return -1;
			}

			public void Clear()
			{
				_dict.Clear();
				_sortedSet.Clear();
			}

			public List<ThreadLoadNode> GetAllNodes()
			{
				return _sortedSet.ToList();
			}

			public int GetThreadIdByIndex(int index)
			{
				if (index < 0 || index >= _sortedSet.Count)
				{
					return -1;
				}
				return _sortedSet.ElementAt(index).ThreadId;
			}

			public long GetLoadByIndex(int index)
			{
				if (index < 0 || index >= _sortedSet.Count)
				{
					return -1L;
				}
				return _sortedSet.ElementAt(index).Load;
			}
		}

		public struct PowerStatus
		{
			public byte ACLineStatus;

			public byte BatteryFlag;

			public byte BatteryLifePercent;

			public byte Reserved;

			public int BatteryLifeTime;

			public int BatteryFullLifeTime;
		}

		private enum ThreadAccess : uint
		{
			TERMINATE = 1u,
			SUSPEND_RESUME = 2u,
			GET_CONTEXT = 8u,
			SET_CONTEXT = 0x10u,
			SET_INFORMATION = 0x20u,
			QUERY_INFORMATION = 0x40u
		}

		public enum ProcessAccess : uint
		{
			TERMINATE = 1u,
			CREATE_THREAD = 2u,
			OPERATION_PROTECT_MEMORY = 4u,
			OPERATION_WRITE_MEMORY = 8u,
			OPERATION_READ_MEMORY = 16u,
			DUPLICATE_HANDLE = 64u,
			CREATE_PROCESS = 128u,
			SET_QUOTA = 256u,
			SET_INFORMATION = 512u,
			QUERY_INFORMATION = 1024u,
			QUERY_LIMITED_INFORMATION = 4096u,
			SYNCHRONIZE = 1048576u,
			DELETE = 65536u,
			READ_CONTROL = 131072u,
			WRITE_DAC = 262144u,
			WRITE_OWNER = 524288u,
			STANDARD_RIGHTS_REQUIRED = 983040u,
			PROCESS_ALL_ACCESS = 2035711u
		}

		private struct PROCESS_POWER_THROTTLING_STATE
		{
			public int Version;

			public uint ControlMask;

			public uint StateMask;
		}

		private static readonly object lockProcessCreation = new object();

		private static readonly object thread = new object();

		private static readonly object lockObject = new object();

		private static readonly object Group = new object();

		private static readonly object counts = new object();

		private Node1 record = new Node1();

		private Node wait_queue = new Node();

		private Node1[] threadrecord = new Node1[10000];

		private NodeP[] processrecord = new NodeP[10000];

		private ThreadInfo[] threadinfo = new ThreadInfo[10000];

		private ProcessInfo[] processinfo = new ProcessInfo[10000];

		private Node[] max_ipc_queue = new Node[32];

		private Node[] max_util_queue = new Node[32];

		private Node[] wait_core = new Node[32];

		private Node2[] sched_queue_b2l = new Node2[64];

		private Node2[] sched_queue_l2b = new Node2[64];

		private Node2[] compare = new Node2[64];

		private Node2[] compare_final = new Node2[64];

		private Node2 schedule_queue = new Node2();

		private Node2 schedule_queue_little = new Node2();

		private Node2 schd_queue_b2l = new Node2();

		private Node2 schd_queue_b2s = new Node2();

		private Node2 schd_queue_l2b = new Node2();

		private Node2 schd_queue_s2b = new Node2();

		public long[] lowerlimit = new long[32];

		public long[] upperlimit = new long[32];

		private Guid powerscheme1 = new Guid("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");

		private Guid powerscheme = new Guid("381b4222-f694-41f0-9685-ff5bb260df2e");

		public int node_cap = 500;

		public long num_chain;

		public long num_chain_little;

		public long num_chain_big;

		public long num_chain2;

		public long action_recored;

		public long[] current_freq = new long[32];

		public uint affinitymask;

		public uint affinitymask_little;

		public uint affinitymask_little_p;

		public uint affinitymask_big;

		public uint affinitymask_big_phyx;

		public uint affinitymask_big_smt;

		public uint affinitymask_fake_little;

		public uint affinitymask_all;

		private string number_of_cores;

		private string NumberOfLogicalProcessors;

		public uint eax;

		public uint edx;

		public uint indices;

		public long[] tsc_e = new long[32];

		public long[] tsc_l = new long[32];

		public long[] tsc = new long[32];

		public long[] tsc_total = new long[32];

		public long[] result_ins_e = new long[32];

		public long[] result_ins_l = new long[32];

		public long[] result_ins = new long[32];

		public long[] result_ins_comp_e = new long[32];

		public long[] result_ins_comp_l = new long[32];

		public long[] result_ins_comp = new long[32];

		public long max_single_ratio_big;

		public long max_single_ratio_little;

		public long max_ins_little;

		public long max_ins_big;

		public long max_br_little;

		public long max_br_far_little;

		public long max_br_big;

		public long max_br_far_big;

		public long max_util_big = 50L;

		public long[] single_tag = new long[32];

		public long[] result_br_miss_e = new long[32];

		public long[] result_br_miss_l = new long[32];

		public long[] result_cache_e = new long[32];

		public long[] result_cache_l = new long[32];

		public long[] result_cache = new long[32];

		public long[] result_load_e = new long[32];

		public long[] result_load_l = new long[32];

		public long[] result_load = new long[32];

		public long[] result_store_e = new long[32];

		public long[] result_store_l = new long[32];

		public long[] result_store = new long[32];

		public long[] result_load_l1_e = new long[32];

		public long[] result_load_l1_l = new long[32];

		public long[] result_load_l1 = new long[32];

		public long[] result_br_ins_e = new long[32];

		public long[] result_br_ins_l = new long[32];

		public long[] result_br_ins = new long[32];

		public long[] result_br_indirect_e = new long[32];

		public long[] result_br_indirect_l = new long[32];

		public long[] result_br_indirect = new long[32];

		public long[] br_indirect = new long[32];

		public long[] result_br_far_e = new long[32];

		public long[] result_br_far_l = new long[32];

		public long[] result_br_far = new long[32];

		public long[] br_far = new long[32];

		public long[] result_aclk_e = new long[32];

		public long[] result_aclk_l = new long[32];

		public long[] result_aclk = new long[32];

		public long[] result_mclk_e = new long[32];

		public long[] result_mclk_l = new long[32];

		public long[] result_mclk = new long[32];

		public long[] result_pclk_e = new long[32];

		public long[] result_pclk_l = new long[32];

		public long[] result_pclk = new long[32];

		private Ols myOls = new Ols();

		public long ipc_switch;

		public long active_cores;

		public long[] core_active = new long[32];

		public long active_big_cores;

		public long active_smt_cores;

		public long active_little_cores;

		public long[] single_ratio = new long[32];

		public long[] ht_share = new long[32];

		public long[] br_far_ratio = new long[32];

		public long[] br = new long[32];

		public long[] br_miss = new long[32];

		public long[] cache = new long[32];

		public long[] mem = new long[32];

		public long[] load = new long[32];

		public long[] load_l1 = new long[32];

		public long[] load_l2 = new long[32];

		public long[] load_l3 = new long[32];

		public long[] load_dram = new long[32];

		public long[] cache2mem = new long[32];

		public long[] ins = new long[32];

		public long util_big;

		public long ins_all;

		public long ins_all_avg;

		public long ins_all_whole;

		public long ins_all_whole_sqr;

		public long ins_all_whole_avg;

		public long perf_whole;

		public long perf_whole_old;

		public long perf_whole_avg;

		public long ins_avg;

		public long ins_sqr;

		public long ins_indicator;

		public long ins_big;

		public long ins_big_perm;

		public long ins_constr_smt;

		public long ins_little;

		public long ins_little_perm;

		public long ins_little_comp;

		public long ins_ratio11_perm;

		public long ins_ratio11;

		public long ins_max_comp;

		public long ins_max_load;

		public long ins_max_br;

		public long ins_max;

		public long util_little_all;

		public long aclk_acc;

		public long ins_smt;

		public long ins_little_raw;

		public long ins_big_raw;

		public long ins_smt_raw;

		public long max_ipc;

		private int little_num;

		private int big_num;

		private int core_num;

		private long threshold;

		private long[] datetime_new = new long[32];

		private long[] datetime_old = new long[32];

		private long[] datetime_elapsed = new long[32];

		private long datetime_trigger;

		private long datetime_trigger_little;

		private long datetime_trigger_exchange;

		private long avg_ipc_trigger = 1L;

		private int e_core_position;

		private int currentprocnum_index;

		private long[] count_stat_little = new long[32];

		private long counter_sys;

		private long count_stat;

		private long count_stat1;

		private long count_stat2;

		private long count_stat3;

		private long count_stat4;

		private long count_stat5;

		private long count_stat6;

		private long count_stat7;

		private long counter_action;

		private long counter_action_switch;

		private long[] acc_instruction = new long[32];

		private long[] acc_aclk = new long[32];

		private long[] acc_instruction_comp = new long[32];

		private long[] acc_load = new long[32];

		private long[] acc_datetime = new long[32];

		private long[] acc_instruction1 = new long[32];

		private long[] acc_aclk1 = new long[32];

		private long[] acc_instruction_comp1 = new long[32];

		private long[] acc_load1 = new long[32];

		private long[] acc_datetime1 = new long[32];

		private long[] util = new long[32];

		private long cnt_findnode;

		private long cnt_not_findnode;

		private int switch_to_big;

		private int switch_to_big_cnt;

		private int[] oldthread_waittime = new int[32];

		private int[] schedule_thread = new int[32];

		private int[] max_ipc_thread = new int[32];

		private int[] max_util_thread = new int[32];

		private int[] max_util_little = new int[32];

		private int num_queue = 1;

		private long dummy;

		private int currentthread;

		private int currentprocess;

		private int counter1;

		private int counter2;

		private int counter3;

		private int[] findrecord = new int[32];

		private UIntPtr j;

		private uint mask;

		private uint valueToSet;

		private long acc_util;

		private uint ratio;

		private string ratio_string;

		private uint ratio1;

		private string ratio_string1;

		private long ipc_big_sum;

		private long ipc_little_sum;

		private long ipc_big_count;

		private long ipc_little_count;

		private long ipc_big_avg;

		private long ipc_little_avg;

		private long eff_big_sum;

		private long eff_little_sum;

		private long eff_big_count;

		private long eff_little_count;

		private long eff_big_avg;

		private long eff_little_avg;

		private long ins_big_sum;

		private long ins_little_sum;

		private long ins_big_count;

		private long ins_little_count;

		private long ins_big_avg;

		private long ins_little_avg;

		private long[] ins_total = new long[32];

		private long[] store_total = new long[32];

		private long[] count_total = new long[32];

		private long[] intval = new long[32];

		private long[] nonstore_store_ratio = new long[32];

		private long[] usr_sum = new long[32];

		private long[] usr_count = new long[32];

		private long[] usr_ratio = new long[32];

		private long[] residence_p = new long[32];

		private long[] residence_p1 = new long[32];

		private long[] acc_instruction_b = new long[32];

		private long[] acc_aclk_b = new long[32];

		private long[] acc_load_b = new long[32];

		private long[] acc_store_b = new long[32];

		private long[] acc_load_miss_b = new long[32];

		private long[] acc_br_b = new long[32];

		private long[] acc_runtime_b = new long[32];

		private long[] cnt_b = new long[32];

		private long[] acc_instruction_l = new long[32];

		private long[] acc_aclk_l = new long[32];

		private long[] acc_load_l = new long[32];

		private long[] acc_load_l_perm = new long[32];

		private long[] last_duration = new long[32];

		private long[] now_duration = new long[32];

		private long[] acc_store_l = new long[32];

		private long[] acc_store_l_perm = new long[32];

		private long[] acc_load_miss_l = new long[32];

		private long[] acc_br_l = new long[32];

		private long[] acc_runtime_l = new long[32];

		private long[] cnt_l = new long[32];

		private long[] ipc_b = new long[32];

		private long[] ipc_b_temp = new long[32];

		private long[] max_ipc_b = new long[32];

		private long[] max_ins = new long[32];

		private long[] ipc_l = new long[32];

		private long[] ipc_l_perm = new long[32];

		private long[] max_ipc_l = new long[32];

		private long max_ipc_little;

		private long max_ipc_big;

		private long[] ipc_ratio = new long[32];

		private long[] br_ratio = new long[32];

		private long[] ipc_ratio_temp = new long[32];

		private long[] br_ratio_temp = new long[32];

		private long br_ratio_square;

		private long br_ratio_square_bar;

		private long br_ratio_square_e;

		private long br_ratio_square_count;

		private long ipc_square;

		private long ipc_square_bar;

		private long ipc_square_e;

		private long ipc_square_count;

		private long[] br_load_ratio = new long[32];

		private long[] br_load_ratio_temp = new long[32];

		private long[] load_miss_ratio_b = new long[32];

		private long[] load_miss_ratio_b_temp = new long[32];

		private long[] min_load_miss_ratio_b = new long[32];

		private long[] load_miss_ratio_l = new long[32];

		private long[] avg_runtime_b = new long[32];

		private long[] avg_runtime_l = new long[32];

		private long[] avg_freq_b = new long[32];

		private long[] max_freq_b = new long[32];

		private long[] avg_freq_l = new long[32];

		private long[] lock_data = new long[32];

		private long[] tag = new long[32];

		private long[] duration = new long[32];

		private long[] reset_count = new long[32];

		private uint[] affinity = new uint[32];

		private long[] residence = new long[32];

		private long[] acc_instruction_b1 = new long[32];

		private long[] acc_aclk_b1 = new long[32];

		private long[] acc_load_b1 = new long[32];

		private long[] acc_store_b1 = new long[32];

		private long[] acc_load_miss_b1 = new long[32];

		private long[] acc_br_b1 = new long[32];

		private long[] acc_runtime_b1 = new long[32];

		private long[] cnt_b1 = new long[32];

		private long[] acc_instruction_l1 = new long[32];

		private long[] acc_aclk_l1 = new long[32];

		private long[] acc_load_l1 = new long[32];

		private long[] acc_load_l1_perm = new long[32];

		private long[] last_duration1 = new long[32];

		private long[] now_duration1 = new long[32];

		private long[] acc_store_l1 = new long[32];

		private long[] acc_store_l1_perm = new long[32];

		private long[] acc_load_miss_l1 = new long[32];

		private long[] acc_br_l1 = new long[32];

		private long[] acc_runtime_l1 = new long[32];

		private long[] cnt_l1 = new long[32];

		private long[] ipc_b1 = new long[32];

		private long[] max_ipc_b1 = new long[32];

		private long[] max_ins1 = new long[32];

		private long[] ipc_l1 = new long[32];

		private long[] ipc_l1_perm = new long[32];

		private long[] max_ipc_l1 = new long[32];

		private long[] ipc_ratio1 = new long[32];

		private long[] br_ratio1 = new long[32];

		private long[] br_load_ratio1 = new long[32];

		private long[] load_miss_ratio_b1 = new long[32];

		private long acc_instruction_b1_t;

		private long acc_aclk_b1_t;

		private long acc_load_b1_t;

		private long acc_store_b1_t;

		private long acc_load_miss_b1_t;

		private long acc_br_b1_t;

		private long acc_runtime_b1_t;

		private long cnt_b1_t;

		private long acc_instruction_l1_t;

		private long acc_aclk_l1_t;

		private long acc_load_l1_t;

		private long acc_load_l1_perm_t;

		private long acc_store_l1_t;

		private long acc_store_l1_perm_t;

		private long acc_load_miss_l1_t;

		private long acc_br_l1_t;

		private long acc_runtime_l1_t;

		private long cnt_l1_t;

		private long ipc_b1_t;

		private long max_ipc_b1_t;

		private long ipc_l1_t;

		private long ipc_l1_perm_t;

		private long max_ipc_l1_t;

		private long ipc_ratio1_t;

		private long br_ratio1_t;

		private long br_load_ratio1_t;

		private long load_miss_ratio_b1_t;

		private long min_load_miss_ratio_b1_t;

		private long load_miss_ratio_l1_t;

		private long avg_runtime_b1_t;

		private long avg_runtime_l1_t;

		private long avg_freq_b1_t;

		private long avg_freq_l1_t;

		private long max_ins_t;

		private long lock_data1_t;

		private long tag1_t;

		private long duration1_t;

		private long reset_count1_t;

		private uint affinity1_t;

		private long[] temp1 = new long[32];

		private long[] temp2 = new long[32];

		private long[] temp3 = new long[32];

		private long[] temp4 = new long[32];

		private long[] temp41 = new long[32];

		private long[] temp5 = new long[32];

		private long[] temp51 = new long[32];

		private long[] temp6 = new long[32];

		private long[] temp_ticks = new long[32];

		private long tempp;

		private long tempk;

		private long tempj;

		private long templ;

		private long artif;

		private long neuro;

		private long[] sched_ratio = new long[32];

		private long[] ins_ratio = new long[32];

		private long avg_comp_ldst_ratio;

		private long avg_comp_ldst_sum;

		private long avg_comp_ldst_count;

		private long avg_comp_br_ratio;

		private long avg_comp_br_sum;

		private long avg_comp_br_count;

		private long avg_ipc_ratio_sum;

		private long avg_ipc_ratio_count;

		private long avg_ipc_ratio;

		private long[] min_load_miss_ratio_b1 = new long[32];

		private long[] load_miss_ratio_l1 = new long[32];

		private long[] avg_runtime_b1 = new long[32];

		private long[] avg_runtime_l1 = new long[32];

		private long[] avg_freq_b1 = new long[32];

		private long[] avg_freq_l1 = new long[32];

		private long[] lock_data1 = new long[32];

		private long[] tag1 = new long[32];

		private long[] duration1 = new long[32];

		private long[] reset_count1 = new long[32];

		private uint[] affinity1 = new uint[32];

		private long[] residence1 = new long[32];

		private long[] prev_tag = new long[32];

		private uint[] prev_affinity = new uint[32];

		private long count_fast_ipc;

		private long count_fast_br;

		private long count_fast_comp;

		private long count_slow;

		private long count_heavy;

		private long _6_to_2;

		private long _6_to_1;

		private long _2_to_6;

		private long _6_to_8;

		private long _8_to_6;

		private long type0;

		private long type10;

		private long type1;

		private long type2;

		private long type3;

		private long type4;

		private long type5;

		private long type6;

		private long type7;

		private long type8;

		private long neuro_count;

		private long count_threads;

		private long count_stay_big;

		private int config;

		private int gamemode;

		private long[] core_availability_cnt = new long[32];

		private long[] test_ratio = new long[32];

		private long[] value = new long[32];

		private long max_freq;

		private long insthres;

		private long insthres1;

		private long insthres_lower;

		private long[] exclude_b = new long[32];

		private long[] exclude = new long[32];

		private long[] exclude_all = new long[32];

		private long[] allow_exclude = new long[32];

		private long[] exclude1 = new long[32];

		private long[] exclude_all1 = new long[32];

		private long[] allow_exclude1 = new long[32];

		private long avg_ipc;

		private long acc_ins;

		private long acc_loads;

		private long acc_loads_e;

		private long acc_loads_miss;

		private long acc_loads_miss_e;

		private long acc_brs;

		private long acc_brs_e;

		private long acc_brs_miss;

		private long acc_brs_miss_e;

		private long acc_aclks;

		private long acc_ins_e;

		private long acc_aclks1_e;

		private long acc_aclks_e;

		private long avg_diff;

		private long acc_aclks1;

		private long acc_date;

		private long start;

		private long numberofchain;

		private long acc_ins_b;

		private long acc_ins_l;

		private long acc_ack_b;

		private long acc_ack_l;

		private long avg_ipc_b;

		private long avg_ipc_l;

		private long avg_ipc_ratio_bak;

		private long acc_br_all;

		private long acc_cond_br_all;

		private long avg_cond_br_ratio;

		private long min_cond_br_ratio = 100L;

		private long max_cond_br_ratio;

		private long[] count_intval = new long[32];

		private long count_intval_all;

		private long count_intval_avg;

		private long max_ipc_global;

		private int[] currentprocessor = new int[32];

		private long total_aclks;

		private long total_ins;

		private long total_ins1;

		private long total_aclks1;

		private uint eeax;

		private uint eebx;

		private uint eecx;

		private uint eedx;

		private uint e_msr;

		private uint l_msr;

		private uint l_max_freq;

		private uint max_msr;

		private int[] thread_priority = new int[32];

		private int[] process_priority = new int[32];

		private int[] group_num = new int[32];

		public ThreadInfo[] findthreadinfo = new ThreadInfo[32];

		public GroupInfo[] groupinfo = new GroupInfo[32];

		public GroupInfo primeLgroup = new GroupInfo();

		public GroupInfo subLgroup = new GroupInfo();

		public GroupInfo littleLgroup = new GroupInfo();

		public GroupInfo[] Lgroup = new GroupInfo[32];

		public GroupInfo[] BPgroup = new GroupInfo[32];

		public GroupInfo[] BSgroup = new GroupInfo[32];

		public GroupInfo[] BP_L_group = new GroupInfo[32];

		public GroupInfo[] L_BP_group = new GroupInfo[32];

		public GroupInfo[] BP_L_BS_group = new GroupInfo[32];

		public GroupInfo[] L_BP_BS_group = new GroupInfo[32];

		public GroupInfo[] Perfgroup = new GroupInfo[32];

		public GroupInfo[] Effgroup = new GroupInfo[32];

		public GroupInfo[] Smtgroup = new GroupInfo[32];

		public GroupInfo totalgroup = new GroupInfo();

		public CoreInfo[] coreinfo = new CoreInfo[32];

		public SysInfo sysinfo = new SysInfo();

		public ProcessInfo[] findprocessinfo = new ProcessInfo[32];

		public long[] avg_inspressure = new long[32];

		public int[] activecount = new int[32];

		public int[] activecount1 = new int[32];

		public int numofgroup;

		public long index;

		public int tempgroup;

		public long global_indicator;

		private Dictionary<int, int> index2procnum = new Dictionary<int, int>();

		private Dictionary<int, int> index2procnum4big_p = new Dictionary<int, int>();

		private Dictionary<int, int> index2procnum4big_s = new Dictionary<int, int>();

		private Dictionary<int, int> groupnum2duration = new Dictionary<int, int>();

		public int availLgroupnum = 3;

		public int availBPgroupnum;

		public int availBSgroupnum;

		public int currentperflvl;

		public int currentefflvl;

		public int currentsmtlvl;

		public long Bresidency;

		public long B2Lresidency;

		public long B2L2Sresidency;

		public long B2Sresidency;

		public long B2S2Lresidency;

		public long Lresidency;

		public long L2Bresidency;

		public long L2B2Sresidency;

		public int big;

		public int little;

		public float schedule_little_ratio;

		public int big_actual;

		public int little_actual;

		public long smt;

		public long smt1;

		public long smtt;

		public long smtt1;

		public long smttt;

		public long smttt1;

		public long cycles_per_miss;

		public int perflevel0;

		public int perflevel1;

		public int perflevel2;

		public int perflevel00;

		public int perflevel01;

		public int perflevel02;

		public int perflevel3;

		public int count4level3;

		public int Mode;

		public int perfstatenum;

		public int little_per_group_count = 1;

		public int maxLP;

		public int[] temp_msr1 = new int[32];

		public long acc_ins_big;

		public long acc_ins_big_cnt;

		public long avg_ins_big;

		public long collected_threads_cnt;

		public long condition_cache_miss_enabled;

		private List<uint> level_nodes_l = new List<uint>();

		private List<uint> level_nodes_p = new List<uint>();

		private List<uint> lgroupIndices4sl = new List<uint>();

		private List<uint> lgroupIndices = new List<uint>();

		private List<uint> littleIndices = new List<uint>();

		private List<uint> exlittleIndices = new List<uint>();

		private List<uint> bigPhysicalIndices = new List<uint>();

		private List<uint> bigSmtIndices = new List<uint>();

		public ThreadClassifier threadClassifier = new ThreadClassifier();

		public long accdatalinkage;

		public ThreadPriorityMapper threadmapper = new ThreadPriorityMapper();

		public CrossAttentionScheduler scheduler = new CrossAttentionScheduler();

		public RealtimeScheduler realtimeScheduler = new RealtimeScheduler(64);

		public TransformerScheduler transformerScheduler = new TransformerScheduler("./scheduler_model.bin");

		private float[][] coreFeatures;

		private long SchedulerRuntime;

		private long switchvalue;

		private CoreIndexMapper coreIndex;

		private const int ProcessPowerThrottling = 4;

		private const uint PROCESS_POWER_THROTTLING_EXECUTION_SPEED = 1u;

		private IContainer components;

		public OnlineLearningManager learner { get; private set; }

		public uint GetLevel(int type, int currentlevel)
		{
			List<uint> list = ((type == 1) ? level_nodes_p : level_nodes_l);
			for (int i = 0; i < list.Count; i++)
			{
				if (currentlevel < list[i])
				{
					return Math.Max(0u, list[i] - 1);
				}
			}
			return Math.Max(0u, list[list.Count - 1] - 1);
		}

		private GroupInfo[] CreateGroup(List<int> indices, int groupCount)
		{
			if (groupCount <= 0 || indices.Count == 0)
			{
				return new GroupInfo[0];
			}
			GroupInfo[] array = new GroupInfo[groupCount];
			for (int i = 0; i < groupCount; i++)
			{
				uint num = 0u;
				for (int j = i; j < indices.Count; j += groupCount)
				{
					num |= (uint)(1 << indices[j]);
				}
				array[i] = new GroupInfo(0, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L, num, DateTime.Now.Ticks, 0L);
			}
			return array;
		}

		public int ScheduleCore(ThreadMetrics metrics)
		{
			float factor = GetFactor(metrics.SmallCoreIPC);
			if ((float)metrics.InstructionsPerCycle > 300000f * factor)
			{
				return 1;
			}
			return 0;
		}

		public ThreadInfoSimp test1112(int node_cap, ThreadInfoSimp head, ThreadInfoSimp threadInfo)
		{
			return threadInfo;
		}

		public uint GetAffinity(long missrate)
		{
			uint num = 0u;
			for (int i = 0; i < little_num; i++)
			{
				num |= coreinfo[index2procnum[i]].Affinity;
			}
			for (int j = 0; j < big_num; j++)
			{
				num |= coreinfo[index2procnum4big_p[j]].Affinity;
			}
			for (int k = 0; k < big_num; k++)
			{
				num |= coreinfo[index2procnum4big_s[k]].Affinity;
			}
			return num;
		}

		public uint GetAffinity4BP(long missrate)
		{
			uint num = 0u;
			for (int i = 0; i < big_num; i++)
			{
				num |= coreinfo[index2procnum4big_p[i]].Affinity;
			}
			return num;
		}

		public uint GetAffinity4BS(long missrate)
		{
			uint num = 0u;
			for (int i = 0; i < big_num; i++)
			{
				num |= coreinfo[index2procnum4big_s[i]].Affinity;
			}
			return num;
		}

		public int TestAffinity(uint tempaff)
		{
			for (int i = 0; i < 4; i++)
			{
				if ((Lgroup[i].G_affinity & tempaff) > 0)
				{
					return i;
				}
			}
			return -1;
		}

		public int TestAffinity4BP(uint tempaff)
		{
			for (int i = 0; i < big_num; i++)
			{
				if ((BPgroup[i].G_affinity & tempaff) > 0)
				{
					return i;
				}
			}
			return -1;
		}

		public int TestAffinity4BS(uint tempaff)
		{
			for (int i = 0; i < big_num; i++)
			{
				if ((BSgroup[i].G_affinity & tempaff) > 0)
				{
					return i;
				}
			}
			return -1;
		}

		public int TestAffinity4perf(uint tempaff)
		{
			for (int i = 0; i < 2 * big_num + 4; i++)
			{
				if ((Perfgroup[i].G_affinity & tempaff) > 0)
				{
					return i;
				}
			}
			return 2 * big_num + 3;
		}

		public int TestAffinity4eff(uint tempaff)
		{
			for (int i = 0; i < 2 * big_num + 4; i++)
			{
				if ((Effgroup[i].G_affinity & tempaff) > 0)
				{
					return i;
				}
			}
			return 2 * big_num + 3;
		}

		public int TestAffinity4smt(uint tempaff)
		{
			for (int i = 0; i < 2 * big_num + 4; i++)
			{
				if ((Smtgroup[i].G_affinity & tempaff) > 0)
				{
					return i;
				}
			}
			return 2 * big_num + 3;
		}

		public (int perfResult, int effResult) TestAffinity4all(uint tempaff, uint tempaff1)
		{
			int item = perfstatenum - 1;
			int item2 = perfstatenum - 1;
			bool flag = false;
			bool flag2 = false;
			for (int i = 0; i < perfstatenum; i++)
			{
				if (!flag && (Perfgroup[i].G_affinity & tempaff) > 0)
				{
					item = i;
					flag = true;
				}
				if (!flag2 && (Effgroup[i].G_affinity & tempaff1) > 0)
				{
					item2 = i;
					flag2 = true;
				}
				if (flag && flag2)
				{
					break;
				}
			}
			return (perfResult: item, effResult: item2);
		}

		public (int perfResult, int effResult) TestAffinity4allnosmt(uint tempaff)
		{
			int item = big_num + 3;
			int item2 = big_num + 3;
			bool flag = false;
			bool flag2 = false;
			for (int i = 0; i < big_num + 4; i++)
			{
				if (!flag && (Perfgroup[i].G_affinity & tempaff) > 0)
				{
					item = i;
					flag = true;
				}
				if (!flag2 && (Effgroup[i].G_affinity & tempaff) > 0)
				{
					item2 = i;
					flag2 = true;
				}
				if (flag && flag2)
				{
					break;
				}
			}
			return (perfResult: item, effResult: item2);
		}

		public ThreadInfoSimp UpdateThreadInfoSimp_ascend(int node_cap, ref ThreadInfoSimp head, ThreadInfoSimp threadInfo)
		{
			ThreadInfoSimp threadInfoSimp = null;
			for (ThreadInfoSimp threadInfoSimp2 = head; threadInfoSimp2 != null; threadInfoSimp2 = threadInfoSimp2.Next)
			{
				if (threadInfo.Tid == threadInfoSimp2.Tid)
				{
					if (threadInfoSimp != null)
					{
						threadInfoSimp.Next = threadInfoSimp2.Next;
					}
					else
					{
						head = threadInfoSimp2.Next;
					}
					threadInfoSimp2.Next = null;
					break;
				}
				threadInfoSimp = threadInfoSimp2;
			}
			if (head == null)
			{
				head = threadInfo;
			}
			else
			{
				threadInfoSimp = null;
				ThreadInfoSimp threadInfoSimp2;
				for (threadInfoSimp2 = head; threadInfoSimp2 != null; threadInfoSimp2 = threadInfoSimp2.Next)
				{
					if (threadInfo.Ipc <= threadInfoSimp2.Ipc)
					{
						if (threadInfoSimp != null)
						{
							threadInfoSimp.Next = threadInfo;
							threadInfo.Next = threadInfoSimp2;
						}
						else
						{
							threadInfo.Next = threadInfoSimp2;
							head = threadInfo;
						}
						break;
					}
					threadInfoSimp = threadInfoSimp2;
				}
				if (threadInfoSimp2 == null)
				{
					threadInfoSimp.Next = threadInfo;
					threadInfo.Next = null;
				}
			}
			return threadInfo;
		}

		public GroupInfo UpdateGroupInfo(int node_cap, ref GroupInfo head, GroupInfo groupInfo)
		{
			GroupInfo groupInfo2 = null;
			for (GroupInfo groupInfo3 = head; groupInfo3 != null; groupInfo3 = groupInfo3.Next)
			{
				if (groupInfo.Gid == groupInfo3.Gid)
				{
					if (groupInfo2 != null)
					{
						groupInfo2.Next = groupInfo3.Next;
					}
					else
					{
						head = groupInfo3.Next;
					}
					groupInfo3.Next = null;
					break;
				}
				groupInfo2 = groupInfo3;
			}
			if (head == null)
			{
				head = groupInfo;
			}
			else
			{
				groupInfo2 = null;
				GroupInfo groupInfo3;
				for (groupInfo3 = head; groupInfo3 != null; groupInfo3 = groupInfo3.Next)
				{
					if (groupInfo.Datetime <= groupInfo3.Datetime)
					{
						if (groupInfo2 != null)
						{
							groupInfo2.Next = groupInfo;
							groupInfo.Next = groupInfo3;
						}
						else
						{
							groupInfo.Next = groupInfo3;
							head = groupInfo;
						}
						break;
					}
					groupInfo2 = groupInfo3;
				}
				if (groupInfo3 == null)
				{
					groupInfo2.Next = groupInfo;
					groupInfo.Next = null;
				}
			}
			return groupInfo;
		}

		public ThreadInfoSimp UpdateThreadInfoSimp(int node_cap, ref ThreadInfoSimp head, ThreadInfoSimp threadInfo)
		{
			ThreadInfoSimp threadInfoSimp = null;
			for (ThreadInfoSimp threadInfoSimp2 = head; threadInfoSimp2 != null; threadInfoSimp2 = threadInfoSimp2.Next)
			{
				if (threadInfo.Tid == threadInfoSimp2.Tid)
				{
					if (threadInfoSimp != null)
					{
						threadInfoSimp.Next = threadInfoSimp2.Next;
					}
					else
					{
						head = threadInfoSimp2.Next;
					}
					threadInfoSimp2.Next = null;
					break;
				}
				threadInfoSimp = threadInfoSimp2;
			}
			if (head == null)
			{
				head = threadInfo;
			}
			else
			{
				threadInfoSimp = null;
				ThreadInfoSimp threadInfoSimp2;
				for (threadInfoSimp2 = head; threadInfoSimp2 != null; threadInfoSimp2 = threadInfoSimp2.Next)
				{
					if (threadInfo.Ins_per_count >= threadInfoSimp2.Ins_per_count)
					{
						if (threadInfoSimp != null)
						{
							threadInfoSimp.Next = threadInfo;
							threadInfo.Next = threadInfoSimp2;
						}
						else
						{
							threadInfo.Next = threadInfoSimp2;
							head = threadInfo;
						}
						break;
					}
					threadInfoSimp = threadInfoSimp2;
				}
				if (threadInfoSimp2 == null)
				{
					threadInfoSimp.Next = threadInfo;
					threadInfo.Next = null;
				}
			}
			return threadInfo;
		}

		public ThreadInfoSimp UpdateThreadInfoSimp1(int node_cap, ref ThreadInfoSimp head, ThreadInfoSimp threadInfo)
		{
			ThreadInfoSimp threadInfoSimp = null;
			for (ThreadInfoSimp threadInfoSimp2 = head; threadInfoSimp2 != null; threadInfoSimp2 = threadInfoSimp2.Next)
			{
				if (threadInfo.Tid == threadInfoSimp2.Tid)
				{
					if (threadInfoSimp != null)
					{
						threadInfoSimp.Next = threadInfoSimp2.Next;
					}
					else
					{
						head = threadInfoSimp2.Next;
					}
					threadInfoSimp2.Next = null;
					break;
				}
				threadInfoSimp = threadInfoSimp2;
			}
			if (head == null)
			{
				head = threadInfo;
			}
			else
			{
				threadInfoSimp = null;
				ThreadInfoSimp threadInfoSimp2;
				for (threadInfoSimp2 = head; threadInfoSimp2 != null; threadInfoSimp2 = threadInfoSimp2.Next)
				{
					if (threadInfo.InsPressure >= threadInfoSimp2.InsPressure)
					{
						if (threadInfoSimp != null)
						{
							threadInfoSimp.Next = threadInfo;
							threadInfo.Next = threadInfoSimp2;
						}
						else
						{
							threadInfo.Next = threadInfoSimp2;
							head = threadInfo;
						}
						break;
					}
					threadInfoSimp = threadInfoSimp2;
				}
				if (threadInfoSimp2 == null)
				{
					threadInfoSimp.Next = threadInfo;
					threadInfo.Next = null;
				}
			}
			return threadInfo;
		}

		public ThreadInfoSimp UpdateThreadInfoSimp2(int node_cap, ref ThreadInfoSimp head, ThreadInfoSimp threadInfo)
		{
			ThreadInfoSimp threadInfoSimp = null;
			for (ThreadInfoSimp threadInfoSimp2 = head; threadInfoSimp2 != null; threadInfoSimp2 = threadInfoSimp2.Next)
			{
				if (threadInfo.Tid == threadInfoSimp2.Tid)
				{
					if (threadInfoSimp != null)
					{
						threadInfoSimp.Next = threadInfoSimp2.Next;
					}
					else
					{
						head = threadInfoSimp2.Next;
					}
					threadInfoSimp2.Next = null;
					break;
				}
				threadInfoSimp = threadInfoSimp2;
			}
			if (head == null)
			{
				head = threadInfo;
			}
			else
			{
				threadInfoSimp = null;
				ThreadInfoSimp threadInfoSimp2;
				for (threadInfoSimp2 = head; threadInfoSimp2 != null; threadInfoSimp2 = threadInfoSimp2.Next)
				{
					if (threadInfo.InsPressure1 >= threadInfoSimp2.InsPressure1)
					{
						if (threadInfoSimp != null)
						{
							threadInfoSimp.Next = threadInfo;
							threadInfo.Next = threadInfoSimp2;
						}
						else
						{
							threadInfo.Next = threadInfoSimp2;
							head = threadInfo;
						}
						break;
					}
					threadInfoSimp = threadInfoSimp2;
				}
				if (threadInfoSimp2 == null)
				{
					threadInfoSimp.Next = threadInfo;
					threadInfo.Next = null;
				}
			}
			return threadInfo;
		}

		public ThreadInfoSimp UpdateThreadInfoSimp3(int node_cap, ref ThreadInfoSimp head, ThreadInfoSimp threadInfo)
		{
			ThreadInfoSimp threadInfoSimp = null;
			for (ThreadInfoSimp threadInfoSimp2 = head; threadInfoSimp2 != null; threadInfoSimp2 = threadInfoSimp2.Next)
			{
				if (threadInfo.Tid == threadInfoSimp2.Tid)
				{
					if (threadInfoSimp != null)
					{
						threadInfoSimp.Next = threadInfoSimp2.Next;
					}
					else
					{
						head = threadInfoSimp2.Next;
					}
					threadInfoSimp2.Next = null;
					break;
				}
				threadInfoSimp = threadInfoSimp2;
			}
			if (head == null)
			{
				head = threadInfo;
			}
			else
			{
				threadInfoSimp = null;
				ThreadInfoSimp threadInfoSimp2;
				for (threadInfoSimp2 = head; threadInfoSimp2 != null; threadInfoSimp2 = threadInfoSimp2.Next)
				{
					if (threadInfo.InsPressure2 >= threadInfoSimp2.InsPressure2)
					{
						if (threadInfoSimp != null)
						{
							threadInfoSimp.Next = threadInfo;
							threadInfo.Next = threadInfoSimp2;
						}
						else
						{
							threadInfo.Next = threadInfoSimp2;
							head = threadInfo;
						}
						break;
					}
					threadInfoSimp = threadInfoSimp2;
				}
				if (threadInfoSimp2 == null)
				{
					threadInfoSimp.Next = threadInfo;
					threadInfo.Next = null;
				}
			}
			return threadInfo;
		}

		public ThreadInfo UpdateThreadInfo(int node_cap, ref ThreadInfo head, ThreadInfo threadInfo)
		{
			int num = 0;
			ThreadInfo threadInfo2 = head;
			ThreadInfo threadInfo3 = null;
			for (int i = 0; i < 500; i++)
			{
				if (threadInfo2 == null)
				{
					break;
				}
				if (threadInfo2.Tid == threadInfo.Tid)
				{
					if (threadInfo3 != null)
					{
						threadInfo3.NextThread = threadInfo2.NextThread;
						threadInfo.NextThread = head;
						head = threadInfo;
						threadInfo2.NextThread = null;
						return threadInfo;
					}
					threadInfo.NextThread = threadInfo2.NextThread;
					head = threadInfo;
					threadInfo2.NextThread = null;
					return threadInfo;
				}
				threadInfo3 = threadInfo2;
				threadInfo2 = threadInfo2.NextThread;
				num++;
			}
			threadInfo2 = head;
			if (head == null)
			{
				head = threadInfo;
			}
			else if (head.Tid > 0)
			{
				threadInfo.NextThread = head;
				head = threadInfo;
			}
			else
			{
				threadInfo.NextThread = head.NextThread;
				head = threadInfo;
				threadInfo2.NextThread = null;
			}
			return threadInfo;
		}

		public ProcessInfo UpdateProcessInfo(int node_cap, ref ProcessInfo head, ProcessInfo processInfo)
		{
			int num = 0;
			ProcessInfo processInfo2 = head;
			ProcessInfo processInfo3 = null;
			for (int i = 0; i < 500; i++)
			{
				if (processInfo2 == null)
				{
					break;
				}
				if (processInfo2.Pid == processInfo.Pid)
				{
					if (processInfo3 != null)
					{
						processInfo3.NextProcess = processInfo2.NextProcess;
						processInfo.NextProcess = head;
						head = processInfo;
						processInfo2.NextProcess = null;
						return processInfo;
					}
					processInfo.NextProcess = processInfo2.NextProcess;
					head = processInfo;
					processInfo2.NextProcess = null;
					return processInfo;
				}
				processInfo3 = processInfo2;
				processInfo2 = processInfo2.NextProcess;
				num++;
			}
			processInfo2 = head;
			if (head == null)
			{
				head = processInfo;
			}
			else if (head.Pid > 0)
			{
				processInfo.NextProcess = head;
				head = processInfo;
			}
			else
			{
				processInfo.NextProcess = head.NextProcess;
				head = processInfo;
				processInfo2.NextProcess = null;
			}
			return processInfo;
		}

		public int UpdateNodeP(int node_cap, ref NodeP node, int pid, long ins_total, long store_total, long count_total, long intval, long nonstore_store_ratio, long usr_sum, long usr_count, long usr_ratio, long residence, long residence1, Node2 compare, Node2 compare_final)
		{
			int num = 0;
			NodeP nodeP = node;
			NodeP nodeP2 = null;
			while (nodeP != null)
			{
				if (nodeP.PId == pid)
				{
					nodeP.Ins_total = ins_total;
					nodeP.Store_total = store_total;
					nodeP.Count_total = count_total;
					nodeP.Intval = intval;
					nodeP.Nonstore_store_ratio = nonstore_store_ratio;
					nodeP.Usr_sum = usr_sum;
					nodeP.Usr_count = usr_count;
					nodeP.Usr_ratio = usr_ratio;
					nodeP.Residence = residence;
					nodeP.Residence1 = residence1;
					nodeP.Compare = compare;
					nodeP.Compare_final = compare_final;
					if (nodeP2 != null)
					{
						nodeP2.Next = nodeP.Next;
						nodeP.Next = node;
						node = nodeP;
						return 1;
					}
					return 1;
				}
				_ = nodeP.Next;
				nodeP2 = nodeP;
				nodeP = nodeP.Next;
				num++;
			}
			NodeP nodeP3 = new NodeP(pid, ins_total, store_total, count_total, intval, nonstore_store_ratio, usr_sum, usr_count, usr_ratio, residence, residence1, compare, compare_final);
			nodeP3.Next = node;
			node = nodeP3;
			num++;
			return 0;
		}

		public int FindNodeValueP(ref NodeP node, int pid, ref long ins_total, ref long store_total, ref long count_total, ref long intval, ref long nonstore_store_ratio, ref long usr_sum, ref long usr_count, ref long usr_ratio, ref long residence, ref long residence1, ref Node2 compare, ref Node2 compare_final)
		{
			for (NodeP nodeP = node; nodeP != null; nodeP = nodeP.Next)
			{
				if (nodeP.PId == pid)
				{
					ins_total = nodeP.Ins_total;
					store_total = nodeP.Store_total;
					count_total = nodeP.Count_total;
					intval = nodeP.Intval;
					nonstore_store_ratio = nodeP.Nonstore_store_ratio;
					usr_sum = nodeP.Usr_sum;
					usr_count = nodeP.Usr_count;
					usr_ratio = nodeP.Usr_ratio;
					residence = nodeP.Residence;
					residence1 = nodeP.Residence1;
					compare = nodeP.Compare;
					compare_final = nodeP.Compare_final;
					return 1;
				}
			}
			return 0;
		}

		public void RemoveThread(int tid, int pid)
		{
			int num = Math.Abs(tid % 10000);
			if (threadinfo[num] != null && threadinfo[num].Tid == tid)
			{
				threadinfo[num] = null;
			}
			int num2 = Math.Abs(pid % 10000);
			ProcessInfo processInfo = processinfo[num2];
			if (processInfo != null)
			{
				processInfo.ThreadSet = DeleteThreadSimpFromList(processInfo.ThreadSet, tid);
			}
		}

		private ThreadInfoSimp DeleteThreadSimpFromList(ThreadInfoSimp head, int tid)
		{
			ThreadInfoSimp threadInfoSimp = null;
			for (ThreadInfoSimp threadInfoSimp2 = head; threadInfoSimp2 != null; threadInfoSimp2 = threadInfoSimp2.Next)
			{
				if (threadInfoSimp2.Tid == tid)
				{
					if (threadInfoSimp != null)
					{
						threadInfoSimp.Next = threadInfoSimp2.Next;
					}
					else
					{
						head = threadInfoSimp2.Next;
					}
					break;
				}
				threadInfoSimp = threadInfoSimp2;
			}
			return head;
		}

		public void RemoveProcessAndThreads(int pid)
		{
			int num = Math.Abs(pid % 10000);
			ProcessInfo processInfo = processinfo[num];
			if (processInfo == null || processInfo.Pid != pid)
			{
				return;
			}
			for (ThreadInfoSimp threadInfoSimp = processInfo.ThreadSet; threadInfoSimp != null; threadInfoSimp = threadInfoSimp.Next)
			{
				ThreadInfo belong2thread = threadInfoSimp.Belong2thread;
				if (belong2thread != null)
				{
					int num2 = Math.Abs(belong2thread.Tid % 10000);
					if (threadinfo[num2] != null && threadinfo[num2].Tid == belong2thread.Tid)
					{
						threadinfo[num2] = null;
					}
				}
			}
			processInfo.ThreadSet = null;
			processinfo[num] = null;
		}

		public int UpdateNode(int node_cap, ref Node node, int id, int value)
		{
			int num = 0;
			Node node2 = node;
			Node node3 = null;
			while (node2 != null)
			{
				if (node2.Id == id)
				{
					node2.Value = value;
					if (node3 != null)
					{
						node3.Next = node2.Next;
						node2.Next = node;
						node = node2;
						return 1;
					}
					return 1;
				}
				_ = node2.Next;
				node3 = node2;
				node2 = node2.Next;
				num++;
			}
			Node node4 = new Node(id, value);
			node4.Next = node;
			node = node4;
			num++;
			return 0;
		}

		public int UpdateNode2(ref Node2 node, int id, long value1, int value2, ref long reset_count)
		{
			Node2 node2 = node;
			Node2 node3 = null;
			Node2 node4 = new Node2(id, value1, 0);
			node2 = node;
			for (int i = 0; i < 500; i++)
			{
				if (value1 <= node2.Value1)
				{
					if (node3 == null)
					{
						if (node2.Id == 0)
						{
							node = node4;
							reset_count = 1L;
						}
						else
						{
							node4.Next = node2;
							node = node4;
							reset_count = 1L;
						}
					}
					else
					{
						node4.Next = node2;
						node3.Next = node4;
						reset_count = 1L;
					}
					break;
				}
				if (node2.Id == 0)
				{
					node = node4;
					reset_count = 1L;
					return 2;
				}
				if (node2.Next != null)
				{
					node3 = node2;
					node2 = node2.Next;
					continue;
				}
				node2.Next = node4;
				node4.Next = null;
				reset_count = 1L;
				break;
			}
			return 0;
		}

		public int UpdateNode2_little(ref Node2 node, int id, long value1, int value2, ref long reset_count)
		{
			Node2 node2 = node;
			Node2 node3 = null;
			Node2 node4 = new Node2(id, value1, 0);
			node2 = node;
			for (int i = 0; i < 500; i++)
			{
				if (node2.Id == -1)
				{
					node = node4;
					node.Next = null;
					reset_count = 1L;
					return 1;
				}
				if (value1 >= node2.Value1)
				{
					if (node3 == null)
					{
						node4.Next = node2;
						node = node4;
						reset_count = 1L;
						return 1;
					}
					node4.Next = node2;
					node3.Next = node4;
					reset_count = 1L;
					return 1;
				}
				if (node2.Next != null)
				{
					node3 = node2;
					node2 = node2.Next;
					continue;
				}
				node2.Next = node4;
				node4.Next = null;
				reset_count = 1L;
				return 1;
			}
			return 0;
		}

		public int UpdateNodeP(ref Node2 node, int id, long value1, int value2)
		{
			Node2 node2 = node;
			Node2 node3 = null;
			Node2 node4 = new Node2(id, value1, value2);
			node2 = node;
			for (int i = 0; i < 500; i++)
			{
				if (node2.Id == -1)
				{
					node = node4;
					node.Next = null;
					return 1;
				}
				if (value1 >= node2.Value1)
				{
					if (node3 == null)
					{
						node4.Next = node2;
						node = node4;
						return 1;
					}
					node4.Next = node2;
					node3.Next = node4;
					return 1;
				}
				if (node2.Next != null)
				{
					node3 = node2;
					node2 = node2.Next;
					continue;
				}
				node2.Next = node4;
				node4.Next = null;
				return 1;
			}
			return 0;
		}

		public int ProcessSysinfo(GroupInfo groupInfo, ref SysInfo sysInfo)
		{
			GroupInfo groupInfo2 = groupInfo;
			long num = 0L;
			if (groupInfo2 == null)
			{
				return -1;
			}
			while (groupInfo2 != null)
			{
				num++;
				if (num == 1)
				{
					sysInfo.Max_gid = groupInfo2.Gid;
				}
				if (groupInfo2.Next == null)
				{
					sysInfo.Min_gid = groupInfo2.Gid;
				}
				groupInfo2 = groupInfo2.Next;
			}
			if (groupinfo[sysInfo.Max_gid].L_affinity % 2 == 0L)
			{
				if (groupinfo[sysInfo.Max_gid].ThreadSet2 == null)
				{
					return -1;
				}
				groupinfo[sysInfo.Max_gid].ThreadSet2.Belong2thread.Affinity = (uint)groupinfo[sysInfo.Min_gid].Gid;
			}
			else
			{
				if (groupinfo[sysInfo.Max_gid].ThreadSet1 == null)
				{
					return -1;
				}
				groupinfo[sysInfo.Max_gid].ThreadSet1.Belong2thread.Affinity = (uint)groupinfo[sysInfo.Min_gid].Gid;
			}
			return 0;
		}

		public int ProcessCompare(ThreadInfoSimp node, ref long avg_inspressure)
		{
			long num = 0L;
			for (ThreadInfoSimp threadInfoSimp = node; threadInfoSimp != null; threadInfoSimp = threadInfoSimp.Next)
			{
				if (threadInfoSimp.Tid > 0)
				{
					num++;
					if (num == 1)
					{
						_ = threadInfoSimp.Ins_per_count;
						threadInfoSimp.Belong2thread.Groupinfo = totalgroup;
						threadInfoSimp.Belong2thread.Sched = 1;
						threadInfoSimp.Belong2thread.Duration = 30000L;
						threadInfoSimp.Belong2thread.CoreType = 1;
					}
				}
				threadInfoSimp.Belong2thread.Lockdata = 0;
			}
			return (int)num;
		}

		public int ProcessCompare1(ThreadInfoSimp node, ref long avg_inspressure)
		{
			long num = 0L;
			long num2 = 0L;
			long num3 = 0L;
			ThreadInfoSimp threadInfoSimp;
			for (threadInfoSimp = node; threadInfoSimp != null; threadInfoSimp = threadInfoSimp.Next)
			{
				if (threadInfoSimp.Tid > 0)
				{
					num3++;
					_ = 1;
				}
				_ = threadInfoSimp.Next;
			}
			threadInfoSimp = node;
			num3 = 0L;
			while (threadInfoSimp != null)
			{
				if (threadInfoSimp.Tid > 0)
				{
					num3++;
					if (num3 == 1)
					{
						threadInfoSimp.Belong2thread.Ipc_ratio = 100L;
					}
				}
				threadInfoSimp = threadInfoSimp.Next;
			}
			return (int)num3;
		}

		public int ProcessCompare2(ThreadInfoSimp node, ref long avg_inspressure)
		{
			long num = 0L;
			long num2 = 0L;
			long num3 = 0L;
			ThreadInfoSimp threadInfoSimp;
			for (threadInfoSimp = node; threadInfoSimp != null; threadInfoSimp = threadInfoSimp.Next)
			{
				if (threadInfoSimp.Tid > 0)
				{
					num3++;
					_ = 1;
				}
				_ = threadInfoSimp.Next;
			}
			threadInfoSimp = node;
			num3 = 0L;
			while (threadInfoSimp != null)
			{
				if (threadInfoSimp.Tid > 0)
				{
					num3++;
					if (num3 == 1)
					{
						threadInfoSimp.Belong2thread.Ipc_ratio1 = 100L;
					}
				}
				threadInfoSimp = threadInfoSimp.Next;
			}
			return (int)num3;
		}

		public int ProcessCompare3(ThreadInfoSimp node, ref long avg_inspressure)
		{
			long num = 0L;
			long num2 = 0L;
			long num3 = 0L;
			ThreadInfoSimp threadInfoSimp;
			for (threadInfoSimp = node; threadInfoSimp != null; threadInfoSimp = threadInfoSimp.Next)
			{
				if (threadInfoSimp.Tid > 0)
				{
					num3++;
					_ = 1;
				}
				_ = threadInfoSimp.Next;
			}
			threadInfoSimp = node;
			num3 = 0L;
			while (threadInfoSimp != null)
			{
				if (threadInfoSimp.Tid > 0)
				{
					num3++;
					if (num3 == 1)
					{
						threadInfoSimp.Belong2thread.Ipc_ratio2 = 100L;
					}
				}
				threadInfoSimp = threadInfoSimp.Next;
			}
			return (int)num3;
		}

		public int UpdateNode1(int node_cap, ref Node1 node, int id, long acc_instruction_b, long acc_aclk_b, long acc_load_b, long acc_store_b, long acc_load_miss_b, long acc_br_b, long acc_runtime_b, long cnt_b, long acc_instruction_l, long acc_aclk_l, long acc_load_l, long acc_load_l_perm, long last_duration, long now_duration, long acc_store_l, long acc_store_l_perm, long acc_load_miss_l, long acc_br_l, long acc_runtime_l, long cnt_l, long ipc_b, long max_ipc_b, long ipc_l, long ipc_l_perm, long max_ipc_l, long ipc_ratio, long br_ratio, long br_load_ratio, long load_miss_ratio_b, long min_load_miss_ratio_b, long load_miss_ratio_l, long avg_runtime_b, long avg_runtime_l, long avg_freq_b, long avg_freq_l, long max_ins, long lock_data, long tag, long duration, long reset_count, uint affinity, long residence)
		{
			int num = 0;
			Node1 node2 = node;
			Node1 node3 = null;
			while (node2 != null)
			{
				if (node2.Id == id)
				{
					node2.Acc_instruction_b = acc_instruction_b;
					node2.Acc_aclk_b = acc_aclk_b;
					node2.Acc_load_b = acc_load_b;
					node2.Acc_store_b = acc_store_b;
					node2.Acc_load_miss_b = acc_load_miss_b;
					node2.Acc_br_b = acc_br_b;
					node2.Acc_runtime_b = acc_runtime_b;
					node2.Cnt_b = cnt_b;
					node2.Acc_instruction_l = acc_instruction_l;
					node2.Acc_aclk_l = acc_aclk_l;
					node2.Acc_load_l = acc_load_l;
					node2.Acc_load_l_perm = acc_load_l_perm;
					node2.Last_duration = last_duration;
					node2.Now_duration = now_duration;
					node2.Acc_store_l = acc_store_l;
					node2.Acc_store_l_perm = acc_store_l_perm;
					node2.Acc_load_miss_l = acc_load_miss_l;
					node2.Acc_br_l = acc_br_l;
					node2.Acc_runtime_l = acc_runtime_l;
					node2.Cnt_l = cnt_l;
					node2.Ipc_b = ipc_b;
					node2.Max_ipc_b = max_ipc_b;
					node2.Ipc_l = ipc_l;
					node2.Ipc_l_perm = ipc_l_perm;
					node2.Max_ipc_l = max_ipc_l;
					node2.Ipc_ratio = ipc_ratio;
					node2.Br_ratio = br_ratio;
					node2.Br_load_ratio = br_load_ratio;
					node2.Load_miss_ratio_b = load_miss_ratio_b;
					node2.Min_load_miss_ratio_b = min_load_miss_ratio_b;
					node2.Load_miss_ratio_l = load_miss_ratio_l;
					node2.Avg_runtime_b = avg_runtime_b;
					node2.Avg_runtime_l = avg_runtime_l;
					node2.Avg_freq_b = avg_freq_b;
					node2.Avg_freq_l = avg_freq_l;
					node2.Max_ins = max_ins;
					node2.Lock_data = lock_data;
					node2.Tag = tag;
					node2.Duration = duration;
					node2.Reset_count = reset_count;
					node2.Affinity = affinity;
					node2.Residence = residence;
					if (node3 != null)
					{
						node3.Next = node2.Next;
						node2.Next = node;
						node = node2;
						return 1;
					}
					return 1;
				}
				_ = node2.Next;
				node3 = node2;
				node2 = node2.Next;
				num++;
			}
			Node1 node4 = new Node1(id, acc_instruction_b, acc_aclk_b, acc_load_b, acc_store_b, acc_load_miss_b, acc_br_b, acc_runtime_b, cnt_b, acc_instruction_l, acc_aclk_l, acc_load_l, acc_load_l_perm, last_duration, now_duration, acc_store_l, acc_store_l_perm, acc_load_miss_l, acc_br_l, acc_runtime_l, cnt_l, ipc_b, max_ipc_b, ipc_l, ipc_l_perm, max_ipc_l, ipc_ratio, br_ratio, br_load_ratio, load_miss_ratio_b, min_load_miss_ratio_b, load_miss_ratio_l, avg_runtime_b, avg_runtime_l, avg_freq_b, avg_freq_l, max_ins, lock_data, tag, duration, reset_count, affinity, residence);
			node4.Next = node;
			node = node4;
			num++;
			return 0;
		}

		public int DeleteNode(ref Node2 node, int id)
		{
			Node2 node2 = node;
			Node2 node3 = null;
			while (node2 != null)
			{
				if (node2.Id == id)
				{
					if ((node3 == null) & (node2.Next == null))
					{
						node2.Id = -1;
						node2.Value1 = 0L;
						node2.Value2 = -1;
						node2.Next = null;
						return -1;
					}
					if ((node3 == null) & (node2.Next != null))
					{
						node = node2.Next;
						return 1;
					}
					if ((node3 != null) & (node2.Next == null))
					{
						node3.Next = null;
						return 1;
					}
					node3.Next = node2.Next;
					return 1;
				}
				node3 = node2;
				node2 = node2.Next;
			}
			return 0;
		}

		public int GetNodeValue(ref Node2 node, ref long value)
		{
			Node2 node2 = node;
			for (int i = 0; i < 500; i++)
			{
				if (node2 != null)
				{
					if (node2.Id != 0)
					{
						value = node2.Value1;
						return 1;
					}
					node2 = node2.Next;
					continue;
				}
				return -1;
			}
			return -1;
		}

		public int FindNodeValue2(ref Node2 node, ref long value)
		{
			Node2 node2 = node;
			for (int i = 0; i < 500; i++)
			{
				if (node2 != null)
				{
					if (node2.Id != 0)
					{
						try
						{
							IntPtr intPtr = OpenThread((ThreadAccess)96u, bInheritHandle: false, (uint)node2.Id);
							if (intPtr != IntPtr.Zero)
							{
								CloseHandle(intPtr);
								value = node2.Value1;
								return node2.Id;
							}
							DeleteNode(ref node, node2.Id);
							node2 = node2.Next;
						}
						catch
						{
							DeleteNode(ref node, node2.Id);
							node2 = node2.Next;
						}
					}
					else
					{
						node2 = node2.Next;
					}
					continue;
				}
				return -1;
			}
			return -1;
		}

		public int FindCompareValue(ref Node2 node, int id)
		{
			for (Node2 node2 = node; node2 != null; node2 = node2.Next)
			{
				if (node2.Id == id)
				{
					return node2.Value2;
				}
			}
			return -1;
		}

		public int FindNodeValue(Node node, int id)
		{
			for (Node node2 = node; node2 != null; node2 = node2.Next)
			{
				if (node2.Id == id)
				{
					return node2.Value;
				}
			}
			return -1;
		}

		public int FindMaxIpc(Node node, ref int max_ipc_thread, ref int max_ipc_little)
		{
			for (Node node2 = node; node2 != null; node2 = node2.Next)
			{
				max_ipc_thread = node2.Id;
				max_ipc_little = node2.Value;
			}
			return -1;
		}

		public ThreadInfo FindThread(ref ThreadInfo threadInfo, int tid)
		{
			for (ThreadInfo threadInfo2 = threadInfo; threadInfo2 != null; threadInfo2 = threadInfo2.NextThread)
			{
				if (threadInfo2.Tid == tid)
				{
					return threadInfo2;
				}
			}
			return null;
		}

		public ProcessInfo FindProcess(ref ProcessInfo processInfo, int pid)
		{
			for (ProcessInfo processInfo2 = processInfo; processInfo2 != null; processInfo2 = processInfo2.NextProcess)
			{
				if (processInfo2.Pid == pid)
				{
					return processInfo2;
				}
			}
			return null;
		}

		public int FindNodeValue1(ref Node1 node, int id, ref long acc_instruction_b, ref long acc_aclk_b, ref long acc_load_b, ref long acc_store_b, ref long acc_load_miss_b, ref long acc_br_b, ref long acc_runtime_b, ref long cnt_b, ref long acc_instruction_l, ref long acc_aclk_l, ref long acc_load_l, ref long acc_load_l_perm, ref long last_duration, ref long now_duration, ref long acc_store_l, ref long acc_store_l_perm, ref long acc_load_miss_l, ref long acc_br_l, ref long acc_runtime_l, ref long cnt_l, ref long ipc_b, ref long max_ipc_b, ref long ipc_l, ref long ipc_l_perm, ref long max_ipc_l, ref long ipc_ratio, ref long br_ratio, ref long br_load_ratio, ref long load_miss_ratio_b, ref long min_load_miss_ratio_b, ref long load_miss_ratio_l, ref long avg_runtime_b, ref long avg_runtime_l, ref long avg_freq_b, ref long avg_freq_l, ref long max_ins, ref long lock_data, ref long tag, ref long duration, ref long reset_count, ref uint affinity)
		{
			for (Node1 node2 = node; node2 != null; node2 = node2.Next)
			{
				if (node2.Id == id)
				{
					acc_instruction_b = node2.Acc_instruction_b;
					acc_aclk_b = node2.Acc_aclk_b;
					acc_load_b = node2.Acc_load_b;
					acc_store_b = node2.Acc_store_b;
					acc_load_miss_b = node2.Acc_load_miss_b;
					acc_br_b = node2.Acc_br_b;
					acc_runtime_b = node2.Acc_runtime_b;
					cnt_b = node2.Cnt_b;
					acc_instruction_l = node2.Acc_instruction_l;
					acc_aclk_l = node2.Acc_aclk_l;
					acc_load_l = node2.Acc_load_l;
					acc_load_l_perm = node2.Acc_load_l_perm;
					last_duration = node2.Last_duration;
					now_duration = node2.Now_duration;
					acc_store_l = node2.Acc_store_l;
					acc_store_l_perm = node2.Acc_store_l_perm;
					acc_load_miss_l = node2.Acc_load_miss_l;
					acc_br_l = node2.Acc_br_l;
					acc_runtime_l = node2.Acc_runtime_l;
					cnt_l = node2.Cnt_l;
					ipc_b = node2.Ipc_b;
					max_ipc_b = node2.Max_ipc_b;
					ipc_l = node2.Ipc_l;
					ipc_l_perm = node2.Ipc_l_perm;
					max_ipc_l = node2.Max_ipc_l;
					ipc_ratio = node2.Ipc_ratio;
					br_ratio = node2.Br_ratio;
					br_load_ratio = node2.Br_load_ratio;
					load_miss_ratio_b = node2.Load_miss_ratio_b;
					min_load_miss_ratio_b = node2.Min_load_miss_ratio_b;
					load_miss_ratio_l = node2.Load_miss_ratio_l;
					avg_runtime_b = node2.Avg_runtime_b;
					avg_runtime_l = node2.Avg_runtime_l;
					avg_freq_b = node2.Avg_freq_b;
					avg_freq_l = node2.Avg_freq_l;
					max_ins = node2.Max_ins;
					lock_data = node2.Lock_data;
					tag = node2.Tag;
					duration = node2.Duration;
					reset_count = node2.Reset_count;
					affinity = node2.Affinity;
					return 1;
				}
			}
			return 0;
		}

		public float GetFactor(long missrate)
		{
			return (float)missrate / 100f;
		}

		public int Intval2Limit(int oldthread, long intval, long utility, long nonstore_store_ratio, ref long usr_ratio_avg, ref long ins_big, int currentprocessor, long usr_ratio, ref long max_ins, ref long usr_ratio1, long br_sys, ref long tag, uint affinity, ref long reset_count, ref long usr_ratio_little, ref long prod_cons_ratio, ref long switch1, ref long lock_data, ref long residence_p1, ref long residence_p)
		{
			int num = 0;
			switch ((switch1 != 2) ? 1 : 2)
			{
			case 1:
				if (((int)((uint)(1 << currentprocessor) & affinitymask_little) > 0) & (tag == 2))
				{
					if (reset_count == 0L)
					{
						UpdateNode2_little(ref schd_queue_l2b, oldthread, usr_ratio, 0, ref reset_count);
						return 1;
					}
					break;
				}
				return 0;
			case 2:
				if (((int)((uint)(1 << currentprocessor) & affinitymask_big) > 0) & (tag == 6))
				{
					if (reset_count == 0L)
					{
						UpdateNode2_little(ref schd_queue_b2l, oldthread, usr_ratio, 0, ref reset_count);
						return 2;
					}
					break;
				}
				return 0;
			default:
				return 0;
			}
			return 0;
		}

		public long GetLevel(int type, long active_threads_cnt, long current_level)
		{
			if (type == 3)
			{
				long val = ((active_threads_cnt > little_num) ? (3 + active_threads_cnt - little_num) : Math.Max((long)Math.Ceiling((double)active_threads_cnt / (double)little_per_group_count) - 1, 0L));
				long num = Math.Min(Math.Max(val, current_level), perfstatenum - 1);
				long num2 = 3L;
				long num3 = 3 + big_num;
				if (num <= num2)
				{
					return num;
				}
				if (num <= num3)
				{
					if (current_level > num2)
					{
						return num;
					}
					return num2;
				}
				if (current_level <= num2)
				{
					return num2;
				}
				if (current_level <= num3)
				{
					return num3;
				}
				return num;
			}
			long val2;
			if (active_threads_cnt <= big_num)
			{
				val2 = Math.Max(active_threads_cnt - 1, 0L);
			}
			else
			{
				long num4 = Math.Max((long)Math.Ceiling((double)(active_threads_cnt - big_num) / (double)little_per_group_count) - 1, 0L);
				val2 = Math.Min(big_num + num4, perfstatenum - 1);
			}
			long num5 = Math.Max(val2, current_level);
			long num6 = big_num - 1;
			long num7 = 3 + big_num;
			if (num5 <= num6)
			{
				return num5;
			}
			if (num5 <= num7)
			{
				if (current_level > num6)
				{
					return num5;
				}
				return num6;
			}
			if (current_level <= num6)
			{
				return num6;
			}
			if (current_level <= num7)
			{
				return num7;
			}
			return num5;
		}

		[DllImport("powrprof.dll", SetLastError = true)]
		private static extern bool PowerSetActiveScheme(IntPtr UserPowerKey, ref Guid SchemeGuid);

		[DllImport("kernel32.dll")]
		public static extern bool GetSystemPowerStatus(out PowerStatus BatteryInfo);

		[DllImport("powrprof.dll", SetLastError = true)]
		private static extern uint PowerReadACValue(IntPtr RootPowerKey, ref Guid SchemeGuid, ref Guid SubGroupOfPowerSettingsGuid, ref Guid PowerSettingGuid, ref int Type, ref byte Buffer, ref uint BufferSize);

		[DllImport("powrprof.dll", SetLastError = true)]
		private static extern uint PowerReadDCValue(IntPtr RootPowerKey, ref Guid SchemeGuid, ref Guid SubGroupOfPowerSettingsGuid, ref Guid PowerSettingGuid, ref int Type, ref byte Buffer, ref uint BufferSize);

		[DllImport("kernel32.dll")]
		private static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr OpenProcess(ProcessAccess dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern int GetThreadPriority(IntPtr hThread);

		[DllImport("kernel32.dll")]
		public static extern int GetPriorityClass(IntPtr hProcess);

		[DllImport("kernel32.dll")]
		private static extern IntPtr SetThreadAffinityMask(IntPtr hThread, IntPtr dwThreadAffinityMask);

		[DllImport("kernel32.dll")]
		public static extern IntPtr GetThreadAffinityMask(IntPtr hThread, out uint mask);

		[DllImport("kernel32.dll")]
		private static extern bool CloseHandle(IntPtr handle);

		[DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall)]
		public static extern IntPtr GetCurrentThreadId();

		[DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall)]
		public static extern IntPtr GetCurrentProcessId();

		[DllImport("kernel32.dll")]
		public static extern bool SetThreadIdealProcessor(IntPtr hThread, int dwIdealProcessor);

		[DllImport("kernel32.dll")]
		private static extern bool SetProcessInformation(IntPtr hProcess, int ProcessInformationClass, IntPtr pProcessInformation, uint dwSize);

		public Service1()
		{
			InitializeComponent();
		}

		protected override void OnStart(string[] args)
		{
			int[] array = new int[32];
			_ = new int[32];
			int[] array2 = new int[32];
			long[] array3 = new long[32];
			_ = new long[32];
			long[] array4 = new long[32];
			_ = new int[32];
			_ = new int[32];
			_ = new int[32];
			for (int i = 0; i < Convert.ToUInt32(NumberOfLogicalProcessors); i++)
			{
				array[i] = 0;
				array2[i] = 0;
				array3[i] = 0L;
				array4[i] = 0L;
			}
			currentthread = (int)GetCurrentProcessId();
			try
			{
				foreach (ManagementObject item in new ManagementObjectSearcher("select * from win32_processor").Get())
				{
					number_of_cores = item.GetPropertyValue("numberofcores").ToString().Trim();
					NumberOfLogicalProcessors = item.GetPropertyValue("NumberOfLogicalProcessors").ToString().Trim();
				}
			}
			catch
			{
				return;
			}
			ratio1 = (uint)(Convert.ToUInt64(NumberOfLogicalProcessors) * 100 / Convert.ToUInt64(NumberOfLogicalProcessors));
			ratio_string1 = ratio1.ToString();
			ratio = ratio1;
			ratio_string = ratio.ToString();
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583\\DefaultPowerSchemeValues\\381b4222-f694-41f0-9685-ff5bb260df2e").SetValue("ProvAcSettingIndex", ratio_string1, RegistryValueKind.DWord);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583\\DefaultPowerSchemeValues\\381b4222-f694-41f0-9685-ff5bb260df2e").SetValue("ProvDcSettingIndex", ratio_string1, RegistryValueKind.DWord);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583\\DefaultPowerSchemeValues\\381b4222-f694-41f0-9685-ff5bb260df2e").SetValue("AcSettingIndex", ratio_string1, RegistryValueKind.DWord);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583\\DefaultPowerSchemeValues\\381b4222-f694-41f0-9685-ff5bb260df2e").SetValue("DcSettingIndex", ratio_string1, RegistryValueKind.DWord);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583\\DefaultPowerSchemeValues\\961cc777-2547-4f9d-8174-7d86181b8a7a").SetValue("ProvAcSettingIndex", ratio_string, RegistryValueKind.DWord);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583\\DefaultPowerSchemeValues\\961cc777-2547-4f9d-8174-7d86181b8a7a").SetValue("ProvDcSettingIndex", ratio_string, RegistryValueKind.DWord);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583\\DefaultPowerSchemeValues\\961cc777-2547-4f9d-8174-7d86181b8a7a").SetValue("AcSettingIndex", ratio_string, RegistryValueKind.DWord);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583\\DefaultPowerSchemeValues\\961cc777-2547-4f9d-8174-7d86181b8a7a").SetValue("DcSettingIndex", ratio_string, RegistryValueKind.DWord);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583\\DefaultPowerSchemeValues\\ded574b5-45a0-4f42-8737-46345c09c238").SetValue("ProvAcSettingIndex", ratio_string1, RegistryValueKind.DWord);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583\\DefaultPowerSchemeValues\\ded574b5-45a0-4f42-8737-46345c09c238").SetValue("ProvDcSettingIndex", ratio_string1, RegistryValueKind.DWord);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583\\DefaultPowerSchemeValues\\ded574b5-45a0-4f42-8737-46345c09c238").SetValue("AcSettingIndex", ratio_string1, RegistryValueKind.DWord);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583\\DefaultPowerSchemeValues\\ded574b5-45a0-4f42-8737-46345c09c238").SetValue("DcSettingIndex", ratio_string1, RegistryValueKind.DWord);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583").SetValue("Attributes", 1, RegistryValueKind.DWord);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583").SetValue("FriendlyName", "@%SystemRoot%\\system32\\powrprof.dll,-767,Processor performance core parking min cores", RegistryValueKind.ExpandString);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583").SetValue("ValueIncrement", 1, RegistryValueKind.DWord);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583").SetValue("ValueMax", 100, RegistryValueKind.DWord);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583").SetValue("ValueMin", 0, RegistryValueKind.DWord);
			Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583").SetValue("ValueUnits", "@%SystemRoot%\\system32\\powrprof.dll,-81,percent", RegistryValueKind.ExpandString);
			try
			{
				Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Session Manager\\kernel").SetValue("DefaultDynamicHeteroCpuPolicy", 2, RegistryValueKind.DWord);
				Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Session Manager\\kernel").SetValue("DynamicHeteroCpuPolicyImportant", 2, RegistryValueKind.DWord);
				Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Session Manager\\kernel").SetValue("DynamicHeteroCpuPolicyImportantShort", 2, RegistryValueKind.DWord);
				Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Session Manager\\kernel").SetValue("DynamicHeteroCpuPolicyImportantPriority", 8, RegistryValueKind.DWord);
				Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Session Manager\\kernel").SetValue("DynamicHeteroCpuPolicyMask", 2, RegistryValueKind.DWord);
			}
			catch
			{
			}
			for (int j = 0; j < Convert.ToInt32(NumberOfLogicalProcessors); j++)
			{
				sched_queue_l2b[j] = new Node2(0, 0L, 0);
				sched_queue_b2l[j] = new Node2(0, 0L, 0);
				compare[j] = new Node2(0, 0L, 0);
				compare_final[j] = new Node2(0, 0L, 0);
			}
			for (int k = 0; k < Convert.ToInt32(NumberOfLogicalProcessors); k++)
			{
				tag[k] = 0L;
				oldthread_waittime[k] = 0;
				core_availability_cnt[k] = 1L;
				affinitymask_big |= (uint)(1 << k);
				myOls.CpuidTx(26u, ref l_msr, ref eebx, ref eecx, ref eedx, (UIntPtr)(ulong)Math.Pow(2.0, k));
				if (l_msr == 0)
				{
					string contents = "无法读取寄存器，受限的权限！";
					File.WriteAllText("IntlThrdSchedErrorInfo.txt", contents);
					Environment.Exit(0);
				}
			}
			if (Convert.ToInt32(NumberOfLogicalProcessors) == Convert.ToInt32(number_of_cores))
			{
				Mode = 1;
			}
			else
			{
				Mode = 0;
			}
			maxLP = Convert.ToInt32(NumberOfLogicalProcessors);
			coreFeatures = new float[maxLP][];
			for (int l = 0; l < maxLP; l++)
			{
				coreFeatures[l] = new float[8];
			}
			for (int m = 0; m < maxLP; m++)
			{
				myOls.CpuidTx(26u, ref l_msr, ref eebx, ref eecx, ref eedx, (UIntPtr)(uint)(1 << m));
				if ((l_msr & -16777216) >> 24 != 64)
				{
					lgroupIndices4sl.Add((uint)m);
				}
			}
			for (int n = 0; n < lgroupIndices4sl.Count; n++)
			{
				myOls.RdmsrTx(1905u, ref eax, ref edx, (UIntPtr)(uint)(1 << (int)lgroupIndices4sl[n]));
				if (l_max_freq < (eax & 0xFF))
				{
					l_max_freq = eax & 0xFF;
				}
			}
			for (int num = 0; num < maxLP; num++)
			{
				myOls.CpuidTx(26u, ref l_msr, ref eebx, ref eecx, ref eedx, (UIntPtr)(uint)(1 << num));
				if ((l_msr & -16777216) >> 24 != 64)
				{
					myOls.RdmsrTx(1905u, ref eax, ref edx, (UIntPtr)(uint)(1 << num));
					if ((eax & 0xFF) < l_max_freq)
					{
						exlittleIndices.Add((uint)(1 << num));
					}
					else
					{
						littleIndices.Add((uint)(1 << num));
						affinitymask_little_p |= (uint)(1 << num);
					}
					affinitymask_little |= (uint)(1 << num);
					little_num++;
				}
				else if (Mode == 0)
				{
					if (bigPhysicalIndices.Count == bigSmtIndices.Count)
					{
						bigPhysicalIndices.Add((uint)(1 << num));
						affinitymask_big_phyx |= (uint)(1 << num);
					}
					else
					{
						bigSmtIndices.Add((uint)(1 << num));
						affinitymask_big_smt |= (uint)(1 << num);
					}
				}
				else
				{
					bigPhysicalIndices.Add((uint)(1 << num));
					affinitymask_big_phyx |= (uint)(1 << num);
				}
			}
			affinitymask_all = affinitymask_big_phyx | affinitymask_big | affinitymask_little;
			uint[] array5 = new uint[6];
			for (int num2 = 0; num2 < 4; num2++)
			{
				for (int num3 = 0; num3 < littleIndices.Count; num3++)
				{
					if (num3 % 4 == num2)
					{
						array5[num2] |= littleIndices[num3];
					}
				}
				lgroupIndices.Add(array5[num2]);
			}
			List<uint> list = new List<uint>();
			List<uint> list2 = new List<uint>();
			list.AddRange(bigPhysicalIndices);
			list.AddRange(littleIndices);
			list2.AddRange(littleIndices);
			list2.AddRange(bigPhysicalIndices);
			if (Mode == 0)
			{
				list.AddRange(bigSmtIndices);
				list2.AddRange(bigSmtIndices);
			}
			list.AddRange(exlittleIndices);
			list2.AddRange(exlittleIndices);
			level_nodes_p.Add((uint)bigPhysicalIndices.Count);
			level_nodes_p.Add((uint)(littleIndices.Count + bigPhysicalIndices.Count));
			level_nodes_p.Add((uint)(littleIndices.Count + bigPhysicalIndices.Count + bigSmtIndices.Count));
			level_nodes_p.Add((uint)(littleIndices.Count + bigPhysicalIndices.Count + bigSmtIndices.Count + exlittleIndices.Count));
			level_nodes_l.Add((uint)littleIndices.Count);
			level_nodes_l.Add((uint)(littleIndices.Count + bigPhysicalIndices.Count));
			level_nodes_l.Add((uint)(littleIndices.Count + bigPhysicalIndices.Count + bigSmtIndices.Count));
			level_nodes_l.Add((uint)(littleIndices.Count + bigPhysicalIndices.Count + bigSmtIndices.Count + exlittleIndices.Count));
			coreIndex = new CoreIndexMapper(bigPhysicalIndices, bigSmtIndices, littleIndices, exlittleIndices);
			uint[] array6 = new uint[list.Count + 2];
			for (int num4 = 0; num4 < maxLP; num4++)
			{
				for (int num5 = 0; num5 <= num4; num5++)
				{
					array6[num4] |= list[num5];
				}
				Perfgroup[num4] = new GroupInfo(0, 0L, 0L, list[num4], 0L, 0L, 0L, 0L, 0L, array6[num4], DateTime.Now.Ticks, 0L);
			}
			uint[] array7 = new uint[list2.Count + 2];
			for (int num6 = 0; num6 < maxLP; num6++)
			{
				for (int num7 = 0; num7 <= num6; num7++)
				{
					array7[num6] |= list2[num7];
				}
				Effgroup[num6] = new GroupInfo(0, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L, array7[num6], DateTime.Now.Ticks, 0L);
			}
			big_num = Convert.ToInt32(number_of_cores) - little_num;
			perfstatenum = list.Count;
			for (int num8 = 0; num8 < Convert.ToInt32(NumberOfLogicalProcessors); num8++)
			{
				UIntPtr threadAffinityMask = (UIntPtr)(ulong)(1 << num8);
				uint num9 = 0u;
				uint num10 = 0u;
				myOls.RdmsrTx(1908u, ref num9, ref num10, threadAffinityMask);
				myOls.WrmsrTx(1908u, (num9 & 0xFFFFFF) | 0x80000000u, num10, threadAffinityMask);
				if ((int)((uint)(1 << num8) & affinitymask_little) > 0)
				{
					UIntPtr threadAffinityMask2 = (UIntPtr)(ulong)Math.Pow(2.0, num8);
					myOls.WrmsrTx(390u, 4391027u, 0u, threadAffinityMask2);
					myOls.WrmsrTx(391u, 4391028u, 0u, threadAffinityMask2);
					myOls.WrmsrTx(392u, 4391025u, 0u, threadAffinityMask2);
					myOls.WrmsrTx(393u, 4391619u, 0u, threadAffinityMask2);
					myOls.WrmsrTx(394u, 4391104u, 0u, threadAffinityMask2);
					myOls.WrmsrTx(395u, 4390972u, 0u, threadAffinityMask2);
					myOls.WrmsrTx(396u, 4259900u, 0u, threadAffinityMask2);
				}
				if ((int)((uint)(1 << num8) & (affinitymask_big_phyx | affinitymask_big_smt)) > 0)
				{
					UIntPtr threadAffinityMask3 = (UIntPtr)(ulong)Math.Pow(2.0, num8);
					myOls.WrmsrTx(390u, 4391332u, 0u, threadAffinityMask3);
					myOls.WrmsrTx(391u, 4391588u, 0u, threadAffinityMask3);
					myOls.WrmsrTx(392u, 4391618u, 0u, threadAffinityMask3);
					myOls.WrmsrTx(393u, 4391619u, 0u, threadAffinityMask3);
					myOls.WrmsrTx(394u, 4391104u, 0u, threadAffinityMask3);
					myOls.WrmsrTx(395u, 4390972u, 0u, threadAffinityMask3);
					myOls.WrmsrTx(396u, 4259900u, 0u, threadAffinityMask3);
				}
			}
			littleLgroup = new GroupInfo(0, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L, affinitymask_little, DateTime.Now.Ticks, 0L);
			totalgroup = new GroupInfo(0, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L, affinitymask_little | affinitymask_big_phyx | affinitymask_big_smt, DateTime.Now.Ticks, 0L);
			little_per_group_count = Math.Max(1, little_num / 4);
			Process currentProcess = Process.GetCurrentProcess();
			currentProcess.PriorityClass = ProcessPriorityClass.RealTime;
			try
			{
				PROCESS_POWER_THROTTLING_STATE structure = new PROCESS_POWER_THROTTLING_STATE
				{
					Version = 1,
					ControlMask = 1u,
					StateMask = 1u
				};
				IntPtr intPtr = Marshal.AllocHGlobal(Marshal.SizeOf(structure));
				try
				{
					Marshal.StructureToPtr(structure, intPtr, fDeleteOld: false);
					SetProcessInformation(currentProcess.Handle, 4, intPtr, (uint)Marshal.SizeOf(structure));
				}
				finally
				{
					Marshal.FreeHGlobal(intPtr);
				}
			}
			catch
			{
			}
			long ticks = DateTime.Now.Ticks;
			for (int num11 = 0; num11 < Convert.ToInt32(NumberOfLogicalProcessors); num11++)
			{
				UpdateNode(1, ref wait_core[num11], num11, 0);
				UpdateNode(1, ref max_ipc_queue[num11], -1, -1);
				UpdateNode(1, ref max_util_queue[num11], -1, -1);
				coreinfo[num11] = new CoreInfo(num11, ticks, 0L, 0L, 0L, 0L, 0L, 0L, (uint)(1 << num11), (uint)(1 << num11), ticks, ticks);
				exclude[num11] = 0L;
				exclude_b[num11] = 0L;
				exclude_all[num11] = 0L;
				last_duration[num11] = 0L;
				now_duration[num11] = 0L;
				avg_runtime_b[num11] = 0L;
				avg_runtime_l[num11] = 0L;
				max_ipc_l[num11] = 0L;
				max_ipc_b[num11] = 0L;
				temp4[num11] = 0L;
				temp5[num11] = 0L;
				temp6[num11] = 0L;
			}
			sysinfo = new SysInfo(-1, -1, affinitymask_big_phyx | affinitymask_big_smt | affinitymask_little, affinitymask_little);
			myOls.RdmsrTx(1905u, ref eax, ref edx, (UIntPtr)littleIndices[0]);
			indices = littleIndices[0];
			max_freq = eax & 0xFF;
			insthres = 100 * max_freq;
			insthres1 = 100 * max_freq;
			insthres_lower = 5 * max_freq;
			transformerScheduler.SetTopK(5);
			SchedulerRuntime = transformerScheduler.GetRuntime();
			if (SchedulerRuntime > 19)
			{
				sysinfo.IsModelSaved = true;
				transformerScheduler.SetLearningEnabled(enabled: false);
				transformerScheduler.SetTopK(1);
			}
			switchvalue = (sysinfo.IsModelSaved ? 100 : 30);
			schedule_queue.Id = 0;
			schedule_queue.Value1 = 0L;
			schedule_queue.Value2 = 0;
			schedule_queue.Next = null;
			schedule_queue_little.Id = 0;
			schedule_queue_little.Value1 = 0L;
			schedule_queue_little.Value2 = 0;
			schedule_queue_little.Next = null;
			schd_queue_b2l.Id = 0;
			schd_queue_b2l.Value1 = 0L;
			schd_queue_b2l.Value2 = 0;
			schd_queue_b2l.Next = null;
			schd_queue_b2s.Id = 0;
			schd_queue_b2s.Value1 = 0L;
			schd_queue_b2s.Value2 = 0;
			schd_queue_b2s.Next = null;
			schd_queue_l2b.Id = 0;
			schd_queue_l2b.Value1 = 0L;
			schd_queue_l2b.Value2 = 0;
			schd_queue_l2b.Next = null;
			schd_queue_s2b.Id = 0;
			schd_queue_s2b.Value1 = 0L;
			schd_queue_s2b.Value2 = 0;
			schd_queue_s2b.Next = null;
			new Thread(thread).Start();
			new Thread(thread2).Start();
			void thread()
			{
				using TraceEventSession traceEventSession = new TraceEventSession("ThreadSwitchSession");
				traceEventSession.EnableKernelProvider(KernelTraceEventParser.Keywords.Process | KernelTraceEventParser.Keywords.Thread | KernelTraceEventParser.Keywords.ContextSwitch);
				traceEventSession.Source.Kernel.ThreadStop += delegate(ThreadTraceData data)
				{
					int threadID = data.ThreadID;
					int processID = data.ProcessID;
					RemoveThread(threadID, processID);
				};
				traceEventSession.Source.Kernel.ProcessStop += delegate(ProcessTraceData data)
				{
					int processID = data.ProcessID;
					RemoveProcessAndThreads(processID);
				};
				traceEventSession.Source.Kernel.ThreadCSwitch += delegate(CSwitchTraceData data)
				{
					int num12 = 0;
					uint num13 = 0u;
					uint num14 = 0u;
					int num15 = 0;
					int num16 = 0;
					int num17 = 0;
					num15 = data.OldThreadID;
					_ = data.NewThreadID;
					num16 = data.OldProcessID;
					num17 = data.NewProcessID;
					num12 = data.ProcessorNumber;
					currentprocessor[num12] = num12;
					_ = (uint)(1 << num12) & (affinitymask_big_phyx | affinitymask_big_smt);
					_ = 0;
					UIntPtr threadAffinityMask4 = (UIntPtr)(ulong)Math.Pow(2.0, num12);
					currentprocnum_index = num12 / 2;
					datetime_new[num12] = DateTime.Now.Ticks;
					datetime_elapsed[num12] = (datetime_new[num12] - datetime_old[num12]) / 10;
					datetime_old[num12] = datetime_new[num12];
					long num18 = Math.Abs(num15 % 10000);
					long num19 = Math.Abs(num16 % 10000);
					if (num16 != 0)
					{
						coreinfo[num12].Threadcount++;
						coreinfo[num12].RunTime4queque += datetime_elapsed[num12];
						coreinfo[num12].RunTime4queque4sched += datetime_elapsed[num12];
						coreinfo[num12].RunTime4usage += datetime_elapsed[num12];
						coreinfo[num12].threadexecinfo.AddOrUpdate(num15, datetime_elapsed[num12]);
						coreinfo[num12].numberProcessor.AddData(datetime_elapsed[num12]);
						coreinfo[num12].accRuntimePerQ += datetime_elapsed[num12];
						if (coreinfo[num12].threadContrib.ContainsKey(num15))
						{
							coreinfo[num12].accRewardPerQ -= coreinfo[num12].threadContrib[num15];
						}
						long num20 = coreinfo[num12].accRuntimePerQ * (data.OldThreadPriority + 15) / 15;
						coreinfo[num12].accRewardPerQ += num20;
						coreinfo[num12].threadContrib[num15] = num20;
					}
					if (num16 != 0 && num17 == 0)
					{
						coreinfo[num12].SustainedThreadcount++;
						coreinfo[num12].SustainedThreadcount4sched++;
						coreinfo[num12].Cycle++;
						coreinfo[num12].Accthreadcount += coreinfo[num12].threadexecinfo.Count;
						coreinfo[num12].threadexecinfo.Clear();
						coreinfo[num12].AccMaxTime += Math.Max(coreinfo[num12].numberProcessor.GetMax(), 0L);
						coreinfo[num12].numberProcessor.Clear();
						coreinfo[num12].accRunTime4usage += coreinfo[num12].RunTime4usage;
						coreinfo[num12].Threadcount = 0L;
						coreinfo[num12].RunTime4usage = 0L;
						sysinfo.accRewordPerS += ((coreinfo[num12].threadContrib.Count > 0) ? (coreinfo[num12].accRewardPerQ / coreinfo[num12].threadContrib.Count) : 0);
						sysinfo.accQcount++;
						coreinfo[num12].accRuntimePerQ = 0L;
						coreinfo[num12].accRewardPerQ = 0L;
						coreinfo[num12].threadContrib.Clear();
					}
					long num21 = DateTime.Now.Ticks - coreinfo[num12].DateTime;
					long num22 = DateTime.Now.Ticks - coreinfo[num12].DateTime4sched;
					if (num21 > 1000000 && coreinfo[num12].SustainedThreadcount > 10)
					{
						coreinfo[num12].Utilization = 1000 * coreinfo[num12].RunTime4queque / num21;
						coreinfo[num12].Utilization4q = coreinfo[num12].RunTime4queque / coreinfo[num12].SustainedThreadcount;
						coreinfo[num12].AvgMaxTime = coreinfo[num12].AccMaxTime / coreinfo[num12].SustainedThreadcount;
						coreinfo[num12].Avgthreadcount = 100 * coreinfo[num12].Accthreadcount / coreinfo[num12].SustainedThreadcount;
						coreinfo[num12].QueueInterval = num21 / (coreinfo[num12].SustainedThreadcount * 10);
						if (!sysinfo.IsModelSaved)
						{
							if ((int)((uint)(1 << num12) & (affinitymask_big_phyx | affinitymask_big_smt)) > 0)
							{
								UIntPtr threadAffinityMask5 = (UIntPtr)(ulong)(1 << num12);
								uint num23 = 0u;
								uint num24 = 0u;
								myOls.RdmsrTx(1908u, ref num23, ref num24, threadAffinityMask5);
								myOls.WrmsrTx(1908u, (num23 & 0xFFFFFF) | 0, num24, threadAffinityMask5);
							}
							else if (coreinfo[num12].Utilization4q > 1000)
							{
								count4level3 = 0;
								coreinfo[num12].P_state = 1;
								UIntPtr threadAffinityMask6 = (UIntPtr)(ulong)(1 << num12);
								uint num25 = 0u;
								uint num26 = 0u;
								myOls.RdmsrTx(1908u, ref num25, ref num26, threadAffinityMask6);
								myOls.WrmsrTx(1908u, (num25 & 0xFFFFFF) | 0x55000000, num26, threadAffinityMask6);
								perflevel01++;
							}
							else
							{
								count4level3 = 0;
								coreinfo[num12].P_state = 2;
								UIntPtr threadAffinityMask7 = (UIntPtr)(ulong)(1 << num12);
								uint num27 = 0u;
								uint num28 = 0u;
								myOls.RdmsrTx(1908u, ref num27, ref num28, threadAffinityMask7);
								myOls.WrmsrTx(1908u, (num27 & 0xFFFFFF) | 0x80000000u, num28, threadAffinityMask7);
								perflevel02++;
							}
						}
						else if ((int)((uint)(1 << num12) & (affinitymask_big_phyx | affinitymask_big_smt)) > 0)
						{
							if (coreinfo[num12].Utilization4q > 1000)
							{
								count4level3 = 0;
								coreinfo[num12].P_state = 1;
								UIntPtr threadAffinityMask8 = (UIntPtr)(ulong)(1 << num12);
								uint num29 = 0u;
								uint num30 = 0u;
								myOls.RdmsrTx(1908u, ref num29, ref num30, threadAffinityMask8);
								myOls.WrmsrTx(1908u, (num29 & 0xFFFFFF) | 0x55000000, num30, threadAffinityMask8);
								perflevel1++;
							}
							else
							{
								count4level3 = 0;
								coreinfo[num12].P_state = 2;
								UIntPtr threadAffinityMask9 = (UIntPtr)(ulong)(1 << num12);
								uint num31 = 0u;
								uint num32 = 0u;
								myOls.RdmsrTx(1908u, ref num31, ref num32, threadAffinityMask9);
								myOls.WrmsrTx(1908u, (num31 & 0xFFFFFF) | 0x80000000u, num32, threadAffinityMask9);
								perflevel2++;
							}
						}
						else if (coreinfo[num12].Utilization4q > 1000)
						{
							count4level3 = 0;
							coreinfo[num12].P_state = 1;
							UIntPtr threadAffinityMask10 = (UIntPtr)(ulong)(1 << num12);
							uint num33 = 0u;
							uint num34 = 0u;
							myOls.RdmsrTx(1908u, ref num33, ref num34, threadAffinityMask10);
							myOls.WrmsrTx(1908u, (num33 & 0xFFFFFF) | 0x55000000, num34, threadAffinityMask10);
							perflevel01++;
						}
						else
						{
							count4level3 = 0;
							coreinfo[num12].P_state = 2;
							UIntPtr threadAffinityMask11 = (UIntPtr)(ulong)(1 << num12);
							uint num35 = 0u;
							uint num36 = 0u;
							myOls.RdmsrTx(1908u, ref num35, ref num36, threadAffinityMask11);
							myOls.WrmsrTx(1908u, (num35 & 0xFFFFFF) | 0x80000000u, num36, threadAffinityMask11);
							perflevel02++;
						}
						myOls.RdmsrTx(197u, ref num13, ref num14, threadAffinityMask4);
						coreinfo[num12].instructions4sys_l = num14 | num13;
						coreinfo[num12].instructions4sys = Math.Max(coreinfo[num12].instructions4sys_l - coreinfo[num12].instructions4sys_e, 0L);
						sysinfo.total_instructions += coreinfo[num12].instructions4sys;
						coreinfo[num12].instructions4sys_e = coreinfo[num12].instructions4sys_l;
						myOls.RdmsrTx(198u, ref num13, ref num14, threadAffinityMask4);
						coreinfo[num12].cycles4sys_l = num14 | num13;
						coreinfo[num12].cycles4sys = Math.Max(coreinfo[num12].cycles4sys_l - coreinfo[num12].cycles4sys_e, 0L);
						coreinfo[num12].cycles4sys_e = coreinfo[num12].cycles4sys_l;
						myOls.RdmsrTx(194u, ref num13, ref num14, threadAffinityMask4);
						coreinfo[num12].missrate4c_l = num14 | num13;
						coreinfo[num12].missrate4c = Math.Max(coreinfo[num12].missrate4c_l - coreinfo[num12].missrate4c_e, 0L);
						coreinfo[num12].missrate4c_e = coreinfo[num12].missrate4c_l;
						myOls.RdmsrTx(195u, ref num13, ref num14, threadAffinityMask4);
						coreinfo[num12].load_l = num14 | num13;
						coreinfo[num12].load = Math.Max(coreinfo[num12].load_l - coreinfo[num12].load_e, 0L);
						coreinfo[num12].load_e = coreinfo[num12].load_l;
						myOls.RdmsrTx(196u, ref num13, ref num14, threadAffinityMask4);
						coreinfo[num12].store_l = num14 | num13;
						coreinfo[num12].store = Math.Max(coreinfo[num12].store_l - coreinfo[num12].store_e, 0L);
						coreinfo[num12].store_e = coreinfo[num12].store_l;
						myOls.RdmsrTx(193u, ref num13, ref num14, threadAffinityMask4);
						coreinfo[num12].mem_ordering_count_l = num14 | num13;
						coreinfo[num12].mem_ordering_count = Math.Max(coreinfo[num12].mem_ordering_count_l - coreinfo[num12].mem_ordering_count_e, 0L);
						coreinfo[num12].mem_ordering_count_e = coreinfo[num12].mem_ordering_count_l;
						sysinfo.total_runtime += coreinfo[num12].accRunTime4usage + coreinfo[num12].RunTime4usage;
						coreinfo[num12].accRunTime4usage = 0L;
						coreinfo[num12].RunTime4usage = 0L;
						coreinfo[num12].ipc4c = coreinfo[num12].CalcRatio1(coreinfo[num12].instructions4sys, coreinfo[num12].cycles4sys, coreinfo[num12].ipc4c);
						coreinfo[num12].perf4c = coreinfo[num12].CalcRatio1(coreinfo[num12].instructions4sys, coreinfo[num12].RunTime4queque, coreinfo[num12].perf4c);
						if ((int)((uint)(1 << num12) & (affinitymask_big_phyx | affinitymask_big_smt)) > 0)
						{
							coreinfo[num12].missrateratio4c = coreinfo[num12].CalcRatio1(coreinfo[num12].mem_ordering_count - coreinfo[num12].load, coreinfo[num12].instructions4sys, coreinfo[num12].missrateratio4c);
							sysinfo.total_llcmiss += coreinfo[num12].mem_ordering_count - coreinfo[num12].load;
						}
						else
						{
							coreinfo[num12].missrateratio4c = coreinfo[num12].CalcRatio1(coreinfo[num12].mem_ordering_count + coreinfo[num12].missrate4c + coreinfo[num12].load, coreinfo[num12].instructions4sys, coreinfo[num12].missrateratio4c);
							sysinfo.total_llcmiss += coreinfo[num12].mem_ordering_count + coreinfo[num12].missrate4c + coreinfo[num12].load;
						}
						coreinfo[num12].mem_ordering = coreinfo[num12].CalcRatio1(coreinfo[num12].store, coreinfo[num12].instructions4sys, coreinfo[num12].mem_ordering);
						coreFeatures[num12] = new float[9]
						{
							coreinfo[num12].Utilization,
							coreinfo[num12].Utilization4q,
							coreinfo[num12].Avgthreadcount,
							coreinfo[num12].QueueInterval,
							coreinfo[num12].missrateratio4c,
							coreinfo[num12].mem_ordering,
							coreinfo[num12].ipc4c,
							coreinfo[num12].perf4c,
							num12
						};
						coreinfo[num12].RunTime4queque = 0L;
						coreinfo[num12].SustainedThreadcount = 0L;
						coreinfo[num12].AccMaxTime = 0L;
						coreinfo[num12].Accthreadcount = 0L;
						coreinfo[num12].DateTime = DateTime.Now.Ticks;
					}
					if (num22 > 1000000 && coreinfo[num12].SustainedThreadcount4sched > 10)
					{
						coreinfo[num12].Utilization4sched = 1000 * coreinfo[num12].RunTime4queque4sched / num22;
						coreinfo[num12].Utilization4q4sched = coreinfo[num12].RunTime4queque4sched / coreinfo[num12].SustainedThreadcount4sched;
						coreinfo[num12].RunTime4queque4sched = 0L;
						coreinfo[num12].SustainedThreadcount4sched = 0L;
						sysinfo.CoreLoadSeq.AddOrUpdate(num12, coreinfo[num12].Utilization4sched);
						if ((int)((uint)(1 << num12) & (affinitymask_big_phyx | affinitymask_big_smt)) > 0)
						{
							if (coreinfo[num12].Utilization4sched > 70)
							{
								sysinfo.Availaff &= (uint)(~(1 << num12));
							}
							if (coreinfo[num12].Utilization4sched > 70)
							{
								sysinfo.Availaff1 &= (uint)(~(1 << num12));
							}
							if (coreinfo[num12].Utilization4sched < 37)
							{
								sysinfo.Availaff |= (uint)(1 << num12);
							}
							if (coreinfo[num12].Utilization4sched < 37)
							{
								sysinfo.Availaff1 |= (uint)(1 << num12);
							}
						}
						else
						{
							if (coreinfo[num12].Utilization4sched > 70)
							{
								sysinfo.Availaff &= (uint)(~(1 << num12));
							}
							if (coreinfo[num12].Utilization4sched > 70)
							{
								sysinfo.Availaff1 &= (uint)(~(1 << num12));
							}
							if (coreinfo[num12].Utilization4sched < 37)
							{
								sysinfo.Availaff |= (uint)(1 << num12);
							}
							if (coreinfo[num12].Utilization4sched < 37)
							{
								sysinfo.Availaff1 |= (uint)(1 << num12);
							}
						}
						coreinfo[num12].DateTime4sched = DateTime.Now.Ticks;
					}
					if (currentthread != num16 && num16 != 0)
					{
						lock (lockProcessCreation)
						{
							findprocessinfo[num12] = FindProcess(ref processinfo[num19], num16);
							if (findprocessinfo[num12] == null)
							{
								findprocessinfo[num12] = UpdateProcessInfo(500, ref processinfo[num19], new ProcessInfo(num16, DateTime.Now.Ticks, 0L, 0L, 0L, 0L, 0L, 0L, 0, 0, 0L, 0L, 0L, 0, 0, DateTime.Now.Ticks, DateTime.Now.Ticks));
								findprocessinfo[num12].datetime_elapse = DateTime.Now.Ticks;
							}
						}
						findthreadinfo[num12] = FindThread(ref threadinfo[num18], num15);
						cnt_findnode++;
						if (findthreadinfo[num12] == null)
						{
							cnt_not_findnode++;
							findthreadinfo[num12] = UpdateThreadInfo(500, ref threadinfo[num18], new ThreadInfo(num15, DateTime.Now.Ticks, 0L, 0L, 0L, 0L, 0L, 50000L, 0L, 0L, 0L, 1, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0u, 0, new ThreadInfoSimp(num15, 0L, 0L, 0L, -1, 0, null), 0L, 0L, 0L, 0, 0L, 0L, 1, 0L, 0L, 0L, 0L, 0L, 0L, DateTime.Now.Ticks, DateTime.Now.Ticks));
							findthreadinfo[num12].Processinfo = findprocessinfo[num12];
							ThreadInfoSimp threadInfo = new ThreadInfoSimp(findthreadinfo[num12].Tid, 0L, 0L, 0L, 1, findthreadinfo[num12].Group, findthreadinfo[num12]);
							if (findprocessinfo[num12] != null)
							{
								ThreadInfoSimp head = findprocessinfo[num12].ThreadSet;
								findprocessinfo[num12].ThreadSet = UpdateThreadInfoSimp(500, ref head, threadInfo);
							}
							findthreadinfo[num12].Groupinfo = Perfgroup[perfstatenum - 1];
							findthreadinfo[num12].Perflvl = perfstatenum - 1;
							findthreadinfo[num12].Efflvl = 3;
							findthreadinfo[num12].demoteacc = DateTime.Now.Ticks;
							findthreadinfo[num12].SchedType = 1L;
							findthreadinfo[num12].Sched = 1;
						}
						findthreadinfo[num12].Count_sample++;
						findthreadinfo[num12].RunTime += datetime_elapsed[num12];
						findthreadinfo[num12].WaitTime += oldthread_waittime[num12];
						findthreadinfo[num12].Count_sample1++;
						findthreadinfo[num12].Duration -= datetime_elapsed[num12];
						if (findthreadinfo[num12].Duration < 0)
						{
							findthreadinfo[num12].Duration = 0L;
						}
						if (findthreadinfo[num12].Duration == 0L && findthreadinfo[num12].RunTime > 300000)
						{
							findthreadinfo[num12].Ipc_reset_count = findthreadinfo[num12].RunTime / findthreadinfo[num12].Count_sample;
							findthreadinfo[num12].Count_sample = 0L;
							findthreadinfo[num12].RunTime = 0L;
							findthreadinfo[num12].DateTime = DateTime.Now.Ticks;
						}
					}
					if ((sysinfo.Counter_sys_enabled != 0 || coreinfo[num12].CounterEnabled != 0) && coreinfo[num12].CounterEnabled == 1)
					{
						myOls.RdmsrTx(198u, ref num13, ref num14, threadAffinityMask4);
						coreinfo[num12].Register6_l = num14 | num13;
						myOls.RdmsrTx(195u, ref num13, ref num14, threadAffinityMask4);
						coreinfo[num12].Register3_l = num14 | num13;
						myOls.RdmsrTx(196u, ref num13, ref num14, threadAffinityMask4);
						coreinfo[num12].Register4_l = num14 | num13;
						myOls.RdmsrTx(194u, ref num13, ref num14, threadAffinityMask4);
						coreinfo[num12].Register2_l = num14 | num13;
						myOls.RdmsrTx(197u, ref num13, ref num14, threadAffinityMask4);
						coreinfo[num12].Register5_l = num14 | num13;
						myOls.RdmsrTx(193u, ref num13, ref num14, threadAffinityMask4);
						coreinfo[num12].Register1_l = num14 | num13;
						coreinfo[num12].Register6 = Math.Max(coreinfo[num12].Register6_l - coreinfo[num12].Register6_e, 0L);
						coreinfo[num12].Register5 = Math.Max(coreinfo[num12].Register5_l - coreinfo[num12].Register5_e, 0L);
						coreinfo[num12].Register4 = Math.Max(coreinfo[num12].Register4_l - coreinfo[num12].Register4_e, 0L);
						coreinfo[num12].Register3 = Math.Max(coreinfo[num12].Register3_l - coreinfo[num12].Register3_e, 0L);
						coreinfo[num12].Register2 = Math.Max(coreinfo[num12].Register2_l - coreinfo[num12].Register2_e, 0L);
						coreinfo[num12].Register1 = Math.Max(coreinfo[num12].Register1_l - coreinfo[num12].Register1_e, 0L);
						if (currentthread != num16 && num16 != 0)
						{
							findthreadinfo[num12].L4_miss += coreinfo[num12].Register3;
							findthreadinfo[num12].Ins_retire += coreinfo[num12].Register5;
							findthreadinfo[num12].PriorityAcc++;
							findthreadinfo[num12].avgruntime_total += datetime_elapsed[num12];
							if ((int)((uint)(1 << num12) & (affinitymask_big_phyx | affinitymask_big_smt)) > 0)
							{
								findthreadinfo[num12].L3_miss += coreinfo[num12].Register1 - coreinfo[num12].Register3;
								findthreadinfo[num12].CodeFootPrint_counter1 += coreinfo[num12].Register5;
								findthreadinfo[num12].L1_miss1 += coreinfo[num12].Register4;
								findthreadinfo[num12].L3_miss1 += coreinfo[num12].Register5;
							}
							if ((int)((uint)(1 << num12) & affinitymask_little) > 0)
							{
								findthreadinfo[num12].L3_miss += coreinfo[num12].Register1 + coreinfo[num12].Register2 + coreinfo[num12].Register3;
								findthreadinfo[num12].CodeFootPrint_counter1 += coreinfo[num12].Register5;
								findthreadinfo[num12].L1_miss1 += coreinfo[num12].Register4;
								findthreadinfo[num12].L3_miss1 += coreinfo[num12].Register5;
							}
							findthreadinfo[num12].IntVal = (DateTime.Now.Ticks - findthreadinfo[num12].DateTime4interval) / 10;
							findthreadinfo[num12].L2_miss += coreinfo[num12].Register6;
							try
							{
								IntPtr intPtr2 = OpenThread((ThreadAccess)96u, bInheritHandle: false, (uint)num15);
								if (intPtr2 != IntPtr.Zero)
								{
									thread_priority[num12] = GetThreadPriority(intPtr2);
									CloseHandle(intPtr2);
								}
								else
								{
									thread_priority[num12] = 0;
								}
							}
							catch
							{
								thread_priority[num12] = 0;
							}
							try
							{
								IntPtr intPtr3 = OpenProcess((ProcessAccess)1536u, bInheritHandle: false, (uint)num16);
								if (intPtr3 != IntPtr.Zero)
								{
									process_priority[num12] = GetPriorityClass(intPtr3);
									CloseHandle(intPtr3);
								}
								else
								{
									process_priority[num12] = 0;
								}
							}
							catch
							{
								process_priority[num12] = 0;
							}
							findthreadinfo[num12].Clock_big += ThreadPriorityMapper.GetFinalPriority(process_priority[num12], thread_priority[num12]);
							if (findthreadinfo[num12].PriorityAcc > 1000 || findthreadinfo[num12].Ins_retire > 300000)
							{
								findthreadinfo[num12].Ins_per_count = findthreadinfo[num12].CalcRatio(findthreadinfo[num12].Ins_retire, findthreadinfo[num12].PriorityAcc, findthreadinfo[num12].Ins_per_count);
								findthreadinfo[num12].UserModeRatio = (float)findthreadinfo[num12].L4_miss / (float)findthreadinfo[num12].Ins_retire;
								findthreadinfo[num12].Ins_big1 = findthreadinfo[num12].CalcRatio1(findthreadinfo[num12].L1_miss1, findthreadinfo[num12].L3_miss1, findthreadinfo[num12].Ins_big1);
								findthreadinfo[num12].CodeFootPrint = findthreadinfo[num12].CalcRatio(1000000 * findthreadinfo[num12].CodeFootPrint_counter1, findthreadinfo[num12].Ins_retire, findthreadinfo[num12].CodeFootPrint);
								findthreadinfo[num12].Ins_big = findthreadinfo[num12].CalcRatio1(findthreadinfo[num12].L3_miss, findthreadinfo[num12].CodeFootPrint_counter1, findthreadinfo[num12].Ins_big);
								findthreadinfo[num12].Ipc = findthreadinfo[num12].CalcRatio(findthreadinfo[num12].IntVal, findthreadinfo[num12].Count_sample1, findthreadinfo[num12].Ipc);
								findthreadinfo[num12].Clock = findthreadinfo[num12].CalcRatio1(findthreadinfo[num12].Ins_retire, findthreadinfo[num12].L2_miss, findthreadinfo[num12].Clock);
								findthreadinfo[num12].InsPressure = findthreadinfo[num12].CalcRatio1(findthreadinfo[num12].Clock_big, findthreadinfo[num12].PriorityAcc, findthreadinfo[num12].InsPressure);
								findthreadinfo[num12].avgruntime = findthreadinfo[num12].CalcRatio1(findthreadinfo[num12].WaitTime, findthreadinfo[num12].Count_sample1, findthreadinfo[num12].avgruntime);
								float[] array8 = new float[8]
								{
									findthreadinfo[num12].Ins_per_count,
									data.OldThreadPriority + 15,
									(float)findthreadinfo[num12].avgruntime,
									findthreadinfo[num12].Ipc,
									(float)findthreadinfo[num12].Clock,
									(float)findthreadinfo[num12].Ins_big,
									(float)findthreadinfo[num12].Ins_big1,
									num12
								};
								findthreadinfo[num12].PrevCoreType = transformerScheduler.Schedule(array8, coreFeatures, maxLP, num15, findthreadinfo[num12].PrevCoreType, num12);
								if (((uint)(1 << findthreadinfo[num12].PrevCoreType) & (affinitymask_big_phyx | affinitymask_big_smt)) == 0)
								{
									tempk++;
									findthreadinfo[num12].CoreType = 0;
								}
								else
								{
									findthreadinfo[num12].CoreType = 1;
									tempp++;
								}
								findthreadinfo[num12].Ins_retire = 0L;
								findthreadinfo[num12].PriorityAcc = 0L;
								findthreadinfo[num12].L3_miss1 = 0L;
								findthreadinfo[num12].L3_miss = 0L;
								findthreadinfo[num12].L1_miss1 = 0L;
								findthreadinfo[num12].L2_miss = 0L;
								findthreadinfo[num12].WaitTime = 0L;
								findthreadinfo[num12].CodeFootPrint_counter1 = 0L;
								findthreadinfo[num12].Count_sample1 = 0L;
								findthreadinfo[num12].Clock_big = 0L;
								findthreadinfo[num12].avgruntime_total = 0L;
								findthreadinfo[num12].DateTime4interval = DateTime.Now.Ticks;
							}
							if ((int)((uint)(1 << num12) & affinitymask_little) > 0)
							{
								findthreadinfo[num12].Count_internal1++;
								findthreadinfo[num12].ins_little += coreinfo[num12].Register5;
								findthreadinfo[num12].clock_little += coreinfo[num12].Register6;
								if (findthreadinfo[num12].ins_little > 300000 || findthreadinfo[num12].Count_internal1 > 1000)
								{
									findthreadinfo[num12].Clock_litte = findthreadinfo[num12].CalcRatio(100 * findthreadinfo[num12].ins_little, findthreadinfo[num12].clock_little, findthreadinfo[num12].Clock_litte);
									findthreadinfo[num12].ins_little = 0L;
									findthreadinfo[num12].clock_little = 0L;
									findthreadinfo[num12].Count_internal1 = 0L;
								}
							}
							if ((int)((uint)(1 << num12) & (affinitymask_big_phyx | affinitymask_big_smt)) > 0)
							{
								findthreadinfo[num12].Dummy += coreinfo[num12].Register5;
								findthreadinfo[num12].Count_internal2++;
								if ((findthreadinfo[num12].Dummy > 300000 || findthreadinfo[num12].Count_internal2 > 1000) && findthreadinfo[num12].L2_miss > 0)
								{
									findthreadinfo[num12].Dummy = 0L;
									findthreadinfo[num12].Count_internal2 = 0L;
								}
							}
						}
					}
					if (currentthread != num16 && num16 != 0)
					{
						float num37 = Math.Max(GetFactor(findthreadinfo[num12].Clock_litte), 0.5f);
						int num38 = currentefflvl;
						int num39 = currentperflvl;
						_ = currentsmtlvl;
						int num40 = 0;
						switch (findthreadinfo[num12].CoreType)
						{
						case 0:
							if (findthreadinfo[num12].Duration == 0L)
							{
								if (num38 >= findthreadinfo[num12].Efflvl)
								{
									findthreadinfo[num12].Efflvl = num38;
								}
								else
								{
									findthreadinfo[num12].Efflvl = ((findthreadinfo[num12].Efflvl > 0) ? (findthreadinfo[num12].Efflvl - 1) : 0);
								}
								findthreadinfo[num12].Efflvl = ((findthreadinfo[num12].Efflvl < level_nodes_l[0] - 1) ? ((int)(level_nodes_l[0] - 1)) : findthreadinfo[num12].Efflvl);
								if (findthreadinfo[num12].Efflvl > (int)(level_nodes_l[0] - 1))
								{
									L2Bresidency++;
								}
								else
								{
									Lresidency++;
								}
								findthreadinfo[num12].Groupinfo = Effgroup[findthreadinfo[num12].Efflvl];
								findthreadinfo[num12].Sched = 1;
								findthreadinfo[num12].Duration = 100000L;
								findthreadinfo[num12].CoreType = 0;
							}
							break;
						case 1:
							if (num39 > big_num - 1)
							{
								if (num39 > 3 + big_num)
								{
									B2L2Sresidency++;
								}
								else
								{
									B2Lresidency++;
								}
							}
							else
							{
								Bresidency++;
							}
							if (findthreadinfo[num12].Duration == 0L || num39 > findthreadinfo[num12].Perflvl)
							{
								if (num39 >= findthreadinfo[num12].Perflvl)
								{
									findthreadinfo[num12].Perflvl = num39;
								}
								else
								{
									findthreadinfo[num12].Perflvl = ((findthreadinfo[num12].Perflvl > 0) ? (findthreadinfo[num12].Perflvl - 1) : 0);
								}
								findthreadinfo[num12].Perflvl = (int)GetLevel(1, findthreadinfo[num12].Perflvl);
								findthreadinfo[num12].Groupinfo = Perfgroup[findthreadinfo[num12].Perflvl];
								findthreadinfo[num12].Sched = 1;
								findthreadinfo[num12].Duration = 100000L;
								findthreadinfo[num12].CoreType = 1;
							}
							break;
						}
						if (findthreadinfo[num12].Sched == 1)
						{
							try
							{
								IntPtr intPtr4 = OpenThread((ThreadAccess)96u, bInheritHandle: false, (uint)num15);
								if (intPtr4 != IntPtr.Zero)
								{
									if (SchedulerRuntime > 19)
									{
										SetThreadIdealProcessor(intPtr4, findthreadinfo[num12].PrevCoreType);
										if (findthreadinfo[num12].CoreType == 1)
										{
											SetThreadAffinityMask(intPtr4, (IntPtr)affinitymask_all);
										}
										else if (findthreadinfo[num12].Ins_per_count > 800000)
										{
											tempj++;
											SetThreadAffinityMask(intPtr4, (IntPtr)affinitymask_all);
										}
										else
										{
											SetThreadAffinityMask(intPtr4, (IntPtr)affinitymask_little);
										}
										CloseHandle(intPtr4);
									}
									else
									{
										SetThreadAffinityMask(intPtr4, (IntPtr)(1 << findthreadinfo[num12].PrevCoreType));
										SetThreadIdealProcessor(intPtr4, findthreadinfo[num12].PrevCoreType);
										CloseHandle(intPtr4);
									}
								}
							}
							catch
							{
							}
							findthreadinfo[num12].Sched = 0;
						}
					}
					if (sysinfo.Counter_sys_enabled == 1 || coreinfo[num12].CounterEnabled == 1)
					{
						myOls.RdmsrTx(198u, ref num13, ref num14, threadAffinityMask4);
						coreinfo[num12].Register6_l = num14 | num13;
						coreinfo[num12].Register6_e = coreinfo[num12].Register6_l;
						myOls.RdmsrTx(195u, ref num13, ref num14, threadAffinityMask4);
						coreinfo[num12].Register3_l = num14 | num13;
						coreinfo[num12].Register3_e = coreinfo[num12].Register3_l;
						myOls.RdmsrTx(196u, ref num13, ref num14, threadAffinityMask4);
						coreinfo[num12].Register4_l = num14 | num13;
						coreinfo[num12].Register4_e = coreinfo[num12].Register4_l;
						myOls.RdmsrTx(194u, ref num13, ref num14, threadAffinityMask4);
						coreinfo[num12].Register2_l = num14 | num13;
						coreinfo[num12].Register2_e = coreinfo[num12].Register2_l;
						myOls.RdmsrTx(197u, ref num13, ref num14, threadAffinityMask4);
						coreinfo[num12].Register5_l = num14 | num13;
						coreinfo[num12].Register5_e = coreinfo[num12].Register5_l;
						myOls.RdmsrTx(193u, ref num13, ref num14, threadAffinityMask4);
						coreinfo[num12].Register1_l = num14 | num13;
						coreinfo[num12].Register1_e = coreinfo[num12].Register1_l;
					}
					if (sysinfo.Counter_sys_enabled == 1 && coreinfo[num12].CounterEnabled == 0)
					{
						coreinfo[num12].CounterEnabled = 1;
					}
					else if (sysinfo.Counter_sys_enabled == 0 && coreinfo[num12].CounterEnabled == 1)
					{
						coreinfo[num12].CounterEnabled = 0;
					}
					oldthread_waittime[num12] = data.NewThreadWaitTime;
					if ((int)((uint)(1 << num12) & affinitymask_big_phyx) > 0 || (int)((uint)(1 << num12) & affinitymask_big_smt) > 0)
					{
						big_actual++;
					}
					else
					{
						little_actual++;
					}
					if (DateTime.Now.Ticks - sysinfo.Datetime > 10000000 && sysinfo.update)
					{
						sysinfo.update = false;
						myOls.RdmsrTx(1553u, ref num13, ref num14, threadAffinityMask4);
						sysinfo.total_energy_l = num14 | num13;
						sysinfo.total_energy = Math.Max(sysinfo.total_energy_l - sysinfo.total_energy_e, 0L);
						sysinfo.total_energy_e = sysinfo.total_energy_l;
						if (sysinfo.accQcount > 0 && sysinfo.total_instructions > 0)
						{
							transformerScheduler.UpdateTAT((float)sysinfo.accRewordPerS / (float)sysinfo.accQcount, -sysinfo.total_llcmiss / sysinfo.total_instructions);
							sysinfo.accQcount = 0L;
							sysinfo.accRewordPerS = 0L;
							sysinfo.total_energy = 0L;
							sysinfo.total_instructions = 0L;
							sysinfo.total_llcmiss = 0L;
						}
						sysinfo.total_runtime = 0L;
						sysinfo.Datetime = DateTime.Now.Ticks;
						sysinfo.update = true;
					}
				};
				traceEventSession.Source.Process();
			}
			void thread2()
			{
				System.Timers.Timer timer = new System.Timers.Timer(30.0);
				timer.Elapsed += OnTimedEvent;
				timer.Start();
			}
		}

		protected override void OnStop()
		{
			transformerScheduler.SaveModel("./scheduler_model.bin");
		}

		private void OnTimedEvent(object sender, ElapsedEventArgs e)
		{
			count_stat1++;
			if (count_stat1 > 32)
			{
				count_stat1 = 0L;
				counter_sys++;
				if (counter_sys % 4 < 3)
				{
					sysinfo.Counter_sys_enabled = 0;
				}
				else
				{
					sysinfo.Counter_sys_enabled = 1;
				}
				avg_ins_big = ((acc_ins_big_cnt > 0) ? (acc_ins_big / acc_ins_big_cnt) : avg_ins_big);
			}
			count_stat5++;
			if (count_stat5 > 3)
			{
				count_stat5 = 0L;
				(currentperflvl, currentefflvl) = TestAffinity4all(sysinfo.Availaff, sysinfo.Availaff1);
			}
			count_stat3++;
			if (count_stat3 > 3840)
			{
				avg_ipc_trigger = 0L;
				count_stat3 = 0L;
				GC.Collect();
				learner.SaveModel();
			}
			count_stat6++;
			if (count_stat6 > 1920)
			{
				count_stat6 = 0L;
			}
			count_stat7++;
			if (count_stat7 > 1920)
			{
				count_stat7 = 0L;
				SchedulerRuntime = transformerScheduler.GetRuntime();
				if (SchedulerRuntime > 19 && !sysinfo.IsModelSaved)
				{
					transformerScheduler.SaveModel("./scheduler_model.bin");
					sysinfo.IsModelSaved = true;
					switchvalue = 100L;
					transformerScheduler.SetLearningEnabled(enabled: false);
					transformerScheduler.SetTopK(1);
				}
				if (SchedulerRuntime >= 2 && SchedulerRuntime <= 19)
				{
					transformerScheduler.SetTopK(3);
				}
			}
			count_stat++;
			if (count_stat > 320)
			{
				count_stat = 0L;
				string path = "统计数据.txt";
				string contents = "统计数据" + Environment.NewLine + "tempp:" + tempp + Environment.NewLine + "tempk:" + tempk + Environment.NewLine + "人工调度override:" + tempj + Environment.NewLine + "神经网络统计信息1:" + transformerScheduler.GetStatistics(maxLP) + Environment.NewLine + "神经网络统计信息2:" + transformerScheduler.GetLearningReport() + Environment.NewLine + "神经网络统计信息3:" + transformerScheduler.GetAttentionHeadReport(maxLP) + Environment.NewLine + "实际分配大核:" + big_actual + Environment.NewLine + "实际分配小核:" + little_actual + Environment.NewLine + "大核高性能状态:" + perflevel1 + Environment.NewLine + "大核能效状态:" + perflevel2 + Environment.NewLine + "小核高性能状态:" + perflevel01 + Environment.NewLine + "小核能效状态:" + perflevel02;
				File.WriteAllText(path, contents);
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && components != null)
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void InitializeComponent()
		{
			components = new Container();
			base.ServiceName = "Service1";
		}
	}
}
