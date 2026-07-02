using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace IntlThrdPerfSchd
{
	public class OnlineLearningManager
	{
		public class EnergyWindow
		{
			public DateTime StartTime;

			public DateTime EndTime;

			public List<RewardWindow> SubWindows = new List<RewardWindow>();

			public float EnergyEfficiency;

			public bool IsComplete;
		}

		public class RewardWindow
		{
			public DateTime StartTime;

			public DateTime EndTime;

			public float[][] StartState;

			public float[][] EndState;

			public List<SchedulingRecord> Decisions = new List<SchedulingRecord>();

			public float ImmediateReward;

			public float EnergyReward;

			public bool HasImmediateReward;

			public bool HasEnergyReward;
		}

		public class SchedulingRecord
		{
			public float[] ThreadFeatures;

			public float[][] CoreFeatures;

			public int SelectedCore;

			public DateTime Timestamp;

			public float ActionProb;

			public int ThreadId;

			public int? PreviousCore;

			public float ImmediatePortion;

			public float EnergyPortion;

			public float TimeWeight;

			public float SelectedCoreLLCMissRate;

			public float SelectedCoreQueueThreads;

			public float SelectedCoreQueueExecTime;

			public float SelectedCoreL1MissRateBefore;

			public float SelectedCoreL1MissRateAfter;

			public float SelectedCoreIPCBefore;

			public float SelectedCoreIPCAfter;

			public float PreviousIPC;

			public bool HasPreviousIPC;

			public float ExecutionTime;

			public float ActualIPC;

			public float CacheMissRate;

			public int? ActualCore;

			public bool HasExecutionResult;

			public float TotalReward => ImmediatePortion + EnergyPortion;
		}

		public class LearningStats
		{
			public int TotalDecisions;

			public int TotalUpdates;

			public float AverageImmediateReward;

			public float AverageEnergyReward;

			public int PendingRecords;

			public int ModelVersion;

			public int PendingEnergyWindows;

			public float AverageReward => (AverageImmediateReward + AverageEnergyReward) / 2f;
		}

		public enum ImmediateRewardMode
		{
			ExecutionBased,
			CoreStateBased,
			Combined
		}

		private readonly RealtimeScheduler _scheduler;

		private readonly List<SchedulingRecord> _allRecords;

		private readonly List<RewardWindow> _rewardWindows;

		private readonly List<EnergyWindow> _energyWindows;

		private readonly Dictionary<int, int> _threadLastCore;

		private readonly Dictionary<int, float> _threadLastIPC;

		private int _numCores;

		private bool _initialized;

		private RewardWindow _currentRewardWindow;

		private EnergyWindow _currentEnergyWindow;

		private DateTime _initializationTime;

		private bool _disableExploration;

		private int _totalDecisions;

		private int _totalUpdates;

		private float _totalImmediateReward;

		private float _totalEnergyReward;

		private int _energyRewardCount;

		private int _modelVersion;

		private Random _random;

		private int[] _coreSelectionCounts;

		private const string DefaultModelFileName = "scheduler_model.bin";

		public float LearningRate { get; set; } = 0.003f;

		public int BatchSize { get; set; } = 30;

		public int MaxTrainingBatchSize { get; set; } = 20;

		public int MaxRecords { get; set; } = 10000;

		public float RewardWindowMs { get; set; } = 30f;

		public float EnergyWindowSeconds { get; set; } = 1f;

		public float EpsilonStart { get; set; } = 0.05f;

		public float EpsilonEnd { get; set; } = 0.01f;

		public int EpsilonDecaySteps { get; set; } = 10000;

		public float ImmediateRewardWeight { get; set; }

		public float EnergyRewardWeight { get; set; } = 1f;

		public float IPCImprovementRewardWeight { get; set; } = 2f;

		public float OptimalCoreRewardWeight { get; set; }

		public float BaseRewardValue { get; set; } = 0.5f;

		public ImmediateRewardMode CurrentImmediateRewardMode { get; set; } = ImmediateRewardMode.Combined;

		public float CoreStateWeight { get; set; } = 0.5f;

		public float ExecutionWeight { get; set; } = 0.5f;

		public float LLCMissRateRewardWeight { get; set; } = 0.5f;

		public float QueueThreadsRewardWeight { get; set; } = 0.5f;

		public float LoadBalanceRewardWeight { get; set; } = 0.2f;

		public float StabilityRewardWeight { get; set; } = 0.15f;

		public float CoreConsistencyPenalty { get; set; } = 0.3f;

		public bool EnableImmediateReward { get; set; }

		public bool EnableEnergyReward { get; set; } = true;

		public float AttentionTemperature
		{
			get
			{
				return _scheduler.AttentionTemperature;
			}
			set
			{
				_scheduler.AttentionTemperature = value;
			}
		}

		public double ExplorationDurationMinutes { get; set; } = 10.0;

		public bool IsExploring
		{
			get
			{
				if (_disableExploration)
				{
					return false;
				}
				return (DateTime.Now - _initializationTime).TotalMinutes < ExplorationDurationMinutes;
			}
		}

		public OnlineLearningManager(RealtimeScheduler scheduler, int numCores, int seed = 42)
		{
			_scheduler = scheduler ?? throw new ArgumentNullException("scheduler");
			_numCores = numCores;
			_allRecords = new List<SchedulingRecord>();
			_rewardWindows = new List<RewardWindow>();
			_energyWindows = new List<EnergyWindow>();
			_random = new Random(seed);
			_coreSelectionCounts = new int[numCores];
			_initialized = true;
			_initializationTime = DateTime.Now;
			InitializeWindows();
		}

		public OnlineLearningManager(RealtimeScheduler scheduler, int seed = 42)
		{
			_scheduler = scheduler ?? throw new ArgumentNullException("scheduler");
			_allRecords = new List<SchedulingRecord>();
			_rewardWindows = new List<RewardWindow>();
			_energyWindows = new List<EnergyWindow>();
			_threadLastCore = new Dictionary<int, int>();
			_threadLastIPC = new Dictionary<int, float>();
			_random = new Random(seed);
			_initializationTime = DateTime.Now;
		}

		public void SetNumCores(int numCores)
		{
			if (_initialized)
			{
				throw new InvalidOperationException("OnlineLearningManager already initialized");
			}
			if (numCores <= 0)
			{
				throw new ArgumentException("numCores must be positive", "numCores");
			}
			_numCores = numCores;
			_coreSelectionCounts = new int[numCores];
			_initialized = true;
			InitializeWindows();
		}

		public bool IsInitialized => _initialized;

		private void InitializeWindows()
		{
			DateTime now = DateTime.Now;
			_currentEnergyWindow = new EnergyWindow
			{
				StartTime = now,
				EndTime = now.AddSeconds(EnergyWindowSeconds)
			};
			_energyWindows.Add(_currentEnergyWindow);
			_currentRewardWindow = new RewardWindow
			{
				StartTime = now,
				EndTime = now.AddMilliseconds(RewardWindowMs)
			};
			_rewardWindows.Add(_currentRewardWindow);
			_currentEnergyWindow.SubWindows.Add(_currentRewardWindow);
		}

		public int Schedule(float[] threadFeatures, float[][] coreFeatures, int threadId = 0)
		{
			if (!_initialized)
			{
				throw new InvalidOperationException("OnlineLearningManager not initialized. Call SetNumCores first.");
			}
			CheckAndRotateWindows();
			float[] actionProbs = GetActionProbs(threadFeatures, coreFeatures);
			float currentEpsilon = GetCurrentEpsilon();
			int num;
			float actionProb;
			if (_random.NextDouble() < (double)currentEpsilon)
			{
				num = _random.Next(_numCores);
				actionProb = 1f / (float)_numCores;
			}
			else
			{
				num = 0;
				float num2 = actionProbs[0];
				for (int i = 1; i < _numCores; i++)
				{
					if (actionProbs[i] > num2)
					{
						num2 = actionProbs[i];
						num = i;
					}
				}
				actionProb = actionProbs[num];
			}
			int? previousCore = null;
			if (threadId != 0 && _threadLastCore.TryGetValue(threadId, out var value))
			{
				previousCore = value;
			}
			float previousIPC = 0f;
			bool hasPreviousIPC = false;
			if (threadId != 0 && _threadLastIPC.TryGetValue(threadId, out var value2))
			{
				previousIPC = value2;
				hasPreviousIPC = true;
			}
			SchedulingRecord item = new SchedulingRecord
			{
				ThreadFeatures = (float[])threadFeatures.Clone(),
				CoreFeatures = CloneCoreFeatures(coreFeatures),
				SelectedCore = num,
				Timestamp = DateTime.Now,
				ActionProb = actionProb,
				ThreadId = threadId,
				PreviousCore = previousCore,
				PreviousIPC = previousIPC,
				HasPreviousIPC = hasPreviousIPC,
				SelectedCoreLLCMissRate = coreFeatures[num][3],
				SelectedCoreQueueThreads = coreFeatures[num][2],
				SelectedCoreQueueExecTime = coreFeatures[num][1],
				SelectedCoreL1MissRateBefore = coreFeatures[num][3],
				SelectedCoreIPCBefore = coreFeatures[num][4]
			};
			if (threadId != 0)
			{
				_threadLastCore[threadId] = num;
			}
			lock (_allRecords)
			{
				_allRecords.Add(item);
				_currentRewardWindow.Decisions.Add(item);
				while (_allRecords.Count > MaxRecords)
				{
					_allRecords.RemoveAt(0);
				}
			}
			_totalDecisions++;
			_coreSelectionCounts[num]++;
			return num;
		}

		public void OnThreadComplete(int decisionCore, float executionTime, float actualIPC, float cacheMissRate, int? actualCore = null, float coreL1MissRateAfter = 0f, float coreIPCAfter = 0f)
		{
			SchedulingRecord schedulingRecord = null;
			lock (_allRecords)
			{
				for (int num = _allRecords.Count - 1; num >= 0; num--)
				{
					if (_allRecords[num].SelectedCore == decisionCore && !_allRecords[num].HasExecutionResult)
					{
						schedulingRecord = _allRecords[num];
						break;
					}
				}
				if (schedulingRecord == null)
				{
					for (int num2 = _allRecords.Count - 1; num2 >= 0; num2--)
					{
						if (!_allRecords[num2].HasExecutionResult)
						{
							schedulingRecord = _allRecords[num2];
							_ = schedulingRecord.SelectedCore;
							break;
						}
					}
				}
			}
			if (schedulingRecord == null)
			{
				return;
			}
			bool flag = actualCore.HasValue && actualCore.Value != schedulingRecord.SelectedCore;
			schedulingRecord.ExecutionTime = executionTime;
			schedulingRecord.ActualIPC = actualIPC;
			schedulingRecord.CacheMissRate = cacheMissRate;
			schedulingRecord.ActualCore = actualCore;
			schedulingRecord.SelectedCoreL1MissRateAfter = (flag ? schedulingRecord.SelectedCoreL1MissRateBefore : coreL1MissRateAfter);
			schedulingRecord.SelectedCoreIPCAfter = (flag ? schedulingRecord.SelectedCoreIPCBefore : coreIPCAfter);
			schedulingRecord.HasExecutionResult = true;
			if (schedulingRecord.ThreadId != 0)
			{
				_threadLastIPC[schedulingRecord.ThreadId] = actualIPC;
			}
			if (EnableImmediateReward)
			{
				float num3 = CalculateIPCImprovementReward(actualIPC, schedulingRecord.PreviousIPC, schedulingRecord.HasPreviousIPC, BaseRewardValue);
				schedulingRecord.ImmediatePortion = ImmediateRewardWeight * num3;
				_totalImmediateReward += schedulingRecord.ImmediatePortion;
				if (flag)
				{
					schedulingRecord.ImmediatePortion *= 1f - CoreConsistencyPenalty;
				}
			}
			else if (EnableEnergyReward)
			{
				schedulingRecord.ImmediatePortion = BaseRewardValue;
			}
		}

		private float CalculateIPCImprovementReward(float actualIPC, float previousIPC, bool hasPreviousIPC, float baseReward)
		{
			if (!hasPreviousIPC)
			{
				return baseReward;
			}
			float num = (actualIPC - previousIPC) / Math.Max(previousIPC, 0.001f);
			float num2;
			float num3;
			if (num > 0f)
			{
				num2 = Math.Min(num * 1f, 1f);
				return Math.Min(baseReward + num2, 1f);
			}
			num3 = Math.Abs(num) * 1f;
			return Math.Max(baseReward - num3, 0f);
		}

		public bool UpdateEnergyReward(float energyEfficiency, bool forceUpdate = false)
		{
			if (float.IsNaN(energyEfficiency) || float.IsInfinity(energyEfficiency))
			{
				return false;
			}
			if (EnableEnergyReward)
			{
				_currentEnergyWindow.EnergyEfficiency = energyEfficiency;
				_currentEnergyWindow.IsComplete = true;
				DistributeEnergyReward(_currentEnergyWindow, energyEfficiency);
				DateTime now = DateTime.Now;
				_currentEnergyWindow = new EnergyWindow
				{
					StartTime = now,
					EndTime = now.AddSeconds(EnergyWindowSeconds)
				};
				_energyWindows.Add(_currentEnergyWindow);
			}
			int num = 0;
			lock (_allRecords)
			{
				foreach (SchedulingRecord allRecord in _allRecords)
				{
					if (allRecord.HasExecutionResult)
					{
						num++;
					}
				}
				if (num >= BatchSize || forceUpdate)
				{
					UpdateModel();
					return true;
				}
			}
			return false;
		}

		public bool UpdateReward(float energyEfficiency, bool forceUpdate = false)
		{
			return UpdateEnergyReward(energyEfficiency, forceUpdate);
		}

		public LearningStats GetStats()
		{
			int num = 0;
			int num2 = 0;
			List<SchedulingRecord> allRecords = _allRecords;
			lock (allRecords)
			{
				foreach (SchedulingRecord allRecord in _allRecords)
				{
					if (allRecord.HasExecutionResult)
					{
						num2++;
					}
					else
					{
						num++;
					}
				}
			}
			LearningStats learningStats = new LearningStats();
			learningStats.TotalDecisions = _totalDecisions;
			learningStats.TotalUpdates = _totalUpdates;
			learningStats.AverageImmediateReward = ((_totalDecisions > 0) ? (_totalImmediateReward / (float)_totalDecisions) : 0f);
			learningStats.AverageEnergyReward = ((_energyRewardCount > 0) ? (_totalEnergyReward / (float)_energyRewardCount) : 0f);
			learningStats.PendingRecords = num;
			learningStats.ModelVersion = _modelVersion;
			learningStats.PendingEnergyWindows = _energyWindows.Count((EnergyWindow w) => !w.IsComplete);
			return learningStats;
		}

		public string GetNetworkStatsString()
		{
			if (!_initialized)
			{
				return "OnlineLearningManager not initialized";
			}
			StringBuilder stringBuilder = new StringBuilder();
			LearningStats stats = GetStats();
			stringBuilder.AppendLine("========== 网络学习统计 ==========");
			stringBuilder.AppendLine($"模型版本: {stats.ModelVersion}");
			stringBuilder.AppendLine($"总决策数: {stats.TotalDecisions}");
			stringBuilder.AppendLine($"总更新数: {stats.TotalUpdates}");
			stringBuilder.AppendLine($"待处理记录: {stats.PendingRecords}");
			TimeSpan timeSpan = DateTime.Now - _initializationTime;
			double num = Math.Max(0.0, ExplorationDurationMinutes - timeSpan.TotalMinutes);
			stringBuilder.AppendLine($"当前探索率(Epsilon): {GetCurrentEpsilon():F4}");
			stringBuilder.AppendLine("探索状态: " + (IsExploring ? $"进行中 (剩余 {num:F1} 分钟)" : "已结束"));
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("---------- 奖励统计 ----------");
			stringBuilder.AppendLine($"平均即时奖励: {stats.AverageImmediateReward:F4}");
			stringBuilder.AppendLine($"平均能效奖励: {stats.AverageEnergyReward:F4}");
			stringBuilder.AppendLine($"平均总奖励: {stats.AverageReward:F4}");
			stringBuilder.AppendLine($"即时奖励权重: {ImmediateRewardWeight:F2}");
			stringBuilder.AppendLine($"能效奖励权重: {EnergyRewardWeight:F2}");
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("---------- 网络配置 ----------");
			stringBuilder.AppendLine($"核心数: {_numCores}");
			stringBuilder.AppendLine($"dModel: {128}");
			stringBuilder.AppendLine($"注意力头数: {8}");
			stringBuilder.AppendLine($"头维度: {16}");
			stringBuilder.AppendLine($"学习率: {LearningRate:F6}");
			stringBuilder.AppendLine($"批次大小: {BatchSize}");
			stringBuilder.AppendLine($"注意力温度: {AttentionTemperature:F2}");
			stringBuilder.AppendLine();
			float[] allWeights = _scheduler.GetAllWeights();
			float num2 = 0f;
			float num3 = float.MinValue;
			float num4 = float.MaxValue;
			int num5 = 0;
			int num6 = 0;
			float[] array = allWeights;
			foreach (float num7 in array)
			{
				if (float.IsNaN(num7))
				{
					num5++;
					continue;
				}
				if (float.IsInfinity(num7))
				{
					num6++;
					continue;
				}
				num2 += num7;
				if (num7 > num3)
				{
					num3 = num7;
				}
				if (num7 < num4)
				{
					num4 = num7;
				}
			}
			stringBuilder.AppendLine("---------- 权重统计 ----------");
			stringBuilder.AppendLine($"总权重数: {allWeights.Length}");
			if (num5 > 0 || num6 > 0)
			{
				stringBuilder.AppendLine($"[警告] NaN数量: {num5}, Infinity数量: {num6}");
				stringBuilder.AppendLine("[建议] 模型已损坏，请重新初始化或加载备份模型");
			}
			else
			{
				stringBuilder.AppendLine($"权重均值: {num2 / (float)allWeights.Length:F6}");
				stringBuilder.AppendLine($"权重最大: {num3:F6}");
				stringBuilder.AppendLine($"权重最小: {num4:F6}");
			}
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("---------- 注意力头说明 ----------");
			stringBuilder.AppendLine("Head 0 (性能匹配): 关注IPC、指令数、优先级");
			stringBuilder.AppendLine("Head 1 (缓存匹配): 关注LLC miss、分支预测");
			stringBuilder.AppendLine("Head 2 (负载均衡): 关注队列深度、利用率");
			stringBuilder.AppendLine("Head 3 (能效优化): 关注功耗相关特征");
			stringBuilder.AppendLine("Head 4 (内存带宽): 关注内存访问模式");
			stringBuilder.AppendLine("Head 5 (计算密集): 关注计算密集型任务");
			stringBuilder.AppendLine("Head 6 (IO密集): 关注IO等待特征");
			stringBuilder.AppendLine("Head 7 (综合评估): 综合多维度特征");
			stringBuilder.AppendLine("================================");
			return stringBuilder.ToString();
		}

		public string GetCriticStatsString()
		{
			if (!_initialized)
			{
				return "OnlineLearningManager not initialized";
			}
			StringBuilder stringBuilder = new StringBuilder();
			GetStats();
			stringBuilder.AppendLine("================================");
			return stringBuilder.ToString();
		}

		public string GetLastAttentionWeightsString()
		{
			if (!_initialized)
			{
				return "OnlineLearningManager not initialized";
			}
			StringBuilder stringBuilder = new StringBuilder();
			float[][] attentionWeights = _scheduler.GetAttentionWeights(_numCores);
			stringBuilder.AppendLine("========== 注意力权重分布 ==========");
			string[] array = new string[8] { "性能匹配", "缓存匹配", "负载均衡", "能效优化", "内存带宽", "计算密集", "IO密集", "综合评估" };
			for (int i = 0; i < 8; i++)
			{
				stringBuilder.AppendLine($"--- Head {i} ({array[i]}) ---");
				float num = 0f;
				float num2 = float.MinValue;
				int num3 = 0;
				for (int j = 0; j < _numCores; j++)
				{
					num += attentionWeights[i][j];
					if (attentionWeights[i][j] > num2)
					{
						num2 = attentionWeights[i][j];
						num3 = j;
					}
				}
				stringBuilder.Append("大核[0-7]: ");
				for (int k = 0; k < Math.Min(8, _numCores); k++)
				{
					stringBuilder.Append($"C{k}={attentionWeights[i][k]:F3} ");
				}
				stringBuilder.AppendLine();
				if (_numCores > 8)
				{
					stringBuilder.Append("小核[8+]: ");
					for (int l = 8; l < _numCores; l++)
					{
						stringBuilder.Append($"C{l}={attentionWeights[i][l]:F3} ");
						if ((l - 7) % 8 == 0 && l < _numCores - 1)
						{
							stringBuilder.AppendLine();
						}
						if (l > 8 && (l - 7) % 8 == 0)
						{
							stringBuilder.Append("         ");
						}
					}
					stringBuilder.AppendLine();
				}
				stringBuilder.AppendLine($"最大权重核心: C{num3} ({num2:F4}), 权重和: {num:F4}");
				stringBuilder.AppendLine();
			}
			stringBuilder.AppendLine("--- 聚合概率分布 ---");
			float[] array2 = new float[_numCores];
			for (int m = 0; m < _numCores; m++)
			{
				for (int n = 0; n < 8; n++)
				{
					array2[m] += attentionWeights[n][m];
				}
			}
			float num4 = 0f;
			float[] array3 = array2;
			foreach (float num6 in array3)
			{
				num4 += num6;
			}
			if (num4 > 0f)
			{
				for (int num7 = 0; num7 < _numCores; num7++)
				{
					array2[num7] /= num4;
				}
			}
			stringBuilder.Append("大核[0-7]: ");
			for (int num8 = 0; num8 < Math.Min(8, _numCores); num8++)
			{
				stringBuilder.Append($"C{num8}={array2[num8] * 100f:F1}% ");
			}
			stringBuilder.AppendLine();
			if (_numCores > 8)
			{
				stringBuilder.Append("小核[8+]: ");
				for (int num9 = 8; num9 < _numCores; num9++)
				{
					stringBuilder.Append($"C{num9}={array2[num9] * 100f:F1}% ");
					if ((num9 - 7) % 8 == 0 && num9 < _numCores - 1)
					{
						stringBuilder.AppendLine();
					}
					if (num9 > 8 && (num9 - 7) % 8 == 0)
					{
						stringBuilder.Append("         ");
					}
				}
				stringBuilder.AppendLine();
			}
			int num10 = 0;
			float num11 = array2[0];
			for (int num12 = 1; num12 < _numCores; num12++)
			{
				if (array2[num12] > num11)
				{
					num11 = array2[num12];
					num10 = num12;
				}
			}
			stringBuilder.AppendLine($"推荐核心: C{num10} (概率: {num11 * 100f:F1}%)");
			stringBuilder.AppendLine("====================================");
			return stringBuilder.ToString();
		}

		public string GetAttentionScoreStats()
		{
			if (!_initialized)
			{
				return "OnlineLearningManager not initialized";
			}
			return _scheduler.GetAttentionScoreStats(_numCores);
		}

		private void CheckAndRotateWindows()
		{
			DateTime now = DateTime.Now;
			if (now >= _currentRewardWindow.EndTime)
			{
				_currentRewardWindow.EndState = CaptureCoreState();
				_currentRewardWindow = new RewardWindow
				{
					StartTime = now,
					EndTime = now.AddMilliseconds(RewardWindowMs)
				};
				_rewardWindows.Add(_currentRewardWindow);
				_currentEnergyWindow.SubWindows.Add(_currentRewardWindow);
			}
		}

		private float[][] CaptureCoreState()
		{
			return new float[_numCores][];
		}

		private void DistributeEnergyReward(EnergyWindow energyWindow, float totalEnergyReward)
		{
			List<SchedulingRecord> list = new List<SchedulingRecord>();
			float val;
			float num;
			float[] array;
			float num2;
			lock (_allRecords)
			{
				foreach (SchedulingRecord allRecord in _allRecords)
				{
					if (allRecord.HasExecutionResult && allRecord.EnergyPortion == 0f)
					{
						list.Add(allRecord);
					}
				}
			}
			if (list.Count == 0)
			{
				return;
			}
			if (float.IsNaN(totalEnergyReward) || float.IsInfinity(totalEnergyReward))
			{
				return;
			}
			val = (totalEnergyReward - -100f) / 200f;
			val = Math.Max(0f, Math.Min(1f, val));
			num = 0f;
			array = new float[list.Count];
			for (int i = 0; i < list.Count; i++)
			{
				array[i] = 1f;
				list[i].TimeWeight = array[i];
				num += array[i];
			}
			num2 = EnergyRewardWeight * val * (float)list.Count;
			for (int j = 0; j < list.Count; j++)
			{
				list[j].EnergyPortion = array[j] / num * num2;
				_totalEnergyReward += list[j].EnergyPortion;
				_energyRewardCount++;
			}
		}

		private float[] GetActionProbs(float[] threadFeatures, float[][] coreFeatures)
		{
			_scheduler.Predict(threadFeatures, coreFeatures, _numCores);
			float[][] attentionWeights = _scheduler.GetAttentionWeights(_numCores);
			float[] array = new float[_numCores];
			for (int i = 0; i < _numCores; i++)
			{
				for (int j = 0; j < 8; j++)
				{
					array[i] += attentionWeights[j][i];
				}
			}
			float num = 0f;
			for (int k = 0; k < _numCores; k++)
			{
				num += array[k];
			}
			if (num > 0f)
			{
				for (int l = 0; l < _numCores; l++)
				{
					array[l] /= num;
				}
			}
			else
			{
				for (int m = 0; m < _numCores; m++)
				{
					array[m] = 1f / (float)_numCores;
				}
			}
			return array;
		}

		private float[] Softmax(float[] scores)
		{
			float[] array = new float[scores.Length];
			float num = scores[0];
			for (int i = 1; i < scores.Length; i++)
			{
				if (scores[i] > num)
				{
					num = scores[i];
				}
			}
			float num2 = 0f;
			for (int j = 0; j < scores.Length; j++)
			{
				array[j] = (float)Math.Exp(scores[j] - num);
				num2 += array[j];
			}
			for (int k = 0; k < scores.Length; k++)
			{
				array[k] /= num2;
			}
			return array;
		}

		private float GetCurrentEpsilon()
		{
			TimeSpan elapsed;
			float num;
			if (_disableExploration)
			{
				return 0f;
			}
			elapsed = DateTime.Now - _initializationTime;
			if (elapsed.TotalMinutes >= ExplorationDurationMinutes)
			{
				return 0f;
			}
			if (_totalDecisions >= EpsilonDecaySteps)
			{
				return EpsilonEnd;
			}
			num = (float)_totalDecisions / (float)EpsilonDecaySteps;
			return EpsilonStart + (EpsilonEnd - EpsilonStart) * num;
		}

		private void UpdateModel()
		{
			if (_allRecords.Count == 0)
			{
				return;
			}
			List<SchedulingRecord> list = new List<SchedulingRecord>();
			lock (_allRecords)
			{
				foreach (SchedulingRecord allRecord in _allRecords)
				{
					if (allRecord.HasExecutionResult)
					{
						list.Add(allRecord);
					}
				}
			}
			if (list.Count == 0)
			{
				return;
			}
			if (list.Count > MaxTrainingBatchSize)
			{
				list = list.GetRange(list.Count - MaxTrainingBatchSize, MaxTrainingBatchSize);
			}
			float num = 0f;
			foreach (SchedulingRecord item in list)
			{
				num += item.TotalReward;
			}
			num /= (float)list.Count;
			_scheduler.ClearGradients();
			foreach (SchedulingRecord item2 in list)
			{
				_scheduler.PredictWithCache(item2.ThreadFeatures, item2.CoreFeatures, _numCores);
				float advantage = item2.TotalReward - num;
				_scheduler.Backward(item2.SelectedCore, advantage);
			}
			_scheduler.ApplyGradients(LearningRate, list.Count);
			lock (_allRecords)
			{
				if (EnableEnergyReward)
				{
					_allRecords.RemoveAll((SchedulingRecord r) => r.EnergyPortion > 0f);
				}
				else
				{
					HashSet<SchedulingRecord> trainedSet = new HashSet<SchedulingRecord>(list);
					_allRecords.RemoveAll((SchedulingRecord r) => trainedSet.Contains(r));
				}
			}
			CleanupOldWindows();
			_totalUpdates++;
			_modelVersion++;
		}

		private void CleanupOldWindows()
		{
			int num = 5;
			while (_energyWindows.Count > num)
			{
				if (_energyWindows[0].IsComplete)
				{
					_energyWindows.RemoveAt(0);
				}
				else
				{
					break;
				}
			}
			while (_rewardWindows.Count > num * (int)(EnergyWindowSeconds * 1000f / RewardWindowMs))
			{
				_rewardWindows.RemoveAt(0);
			}
		}

		private float[][] CloneCoreFeatures(float[][] features)
		{
			float[][] array = new float[features.Length][];
			for (int i = 0; i < features.Length; i++)
			{
				array[i] = (float[])features[i].Clone();
			}
			return array;
		}

		public bool IsModelCorrupted()
		{
			float[] allWeights = _scheduler.GetAllWeights();
			foreach (float f in allWeights)
			{
				if (float.IsNaN(f) || float.IsInfinity(f))
				{
					return true;
				}
			}
			return false;
		}

		public void ResetModel()
		{
			_scheduler.InitializeWeights();
			_modelVersion++;
			lock (_allRecords)
			{
				_allRecords.Clear();
			}
			_rewardWindows.Clear();
			_energyWindows.Clear();
			_totalDecisions = 0;
			_totalUpdates = 0;
			_totalImmediateReward = 0f;
			_totalEnergyReward = 0f;
			_energyRewardCount = 0;
			Array.Clear(_coreSelectionCounts, 0, _coreSelectionCounts.Length);
			InitializeWindows();
		}

		public void SaveModel()
		{
			SaveModel("scheduler_model.bin");
		}

		public void SaveModel(string filePath)
		{
			float[] allWeights = _scheduler.GetAllWeights();
			using BinaryWriter binaryWriter = new BinaryWriter(File.Open(filePath, FileMode.Create));
			binaryWriter.Write(2);
			binaryWriter.Write(allWeights.Length);
			float[] array = allWeights;
			foreach (float value in array)
			{
				binaryWriter.Write(value);
			}
			bool value2 = !IsExploring;
			binaryWriter.Write(value2);
			binaryWriter.Write(_initializationTime.ToBinary());
			binaryWriter.Write(ExplorationDurationMinutes);
		}

		public void LoadModel()
		{
			LoadModel("scheduler_model.bin");
		}

		public void LoadModel(string filePath)
		{
			if (!File.Exists(filePath))
			{
				_scheduler.InitializeWeights();
				_modelVersion++;
				_initializationTime = DateTime.Now;
				_disableExploration = false;
				return;
			}
			using BinaryReader binaryReader = new BinaryReader(File.Open(filePath, FileMode.Open));
			int num = binaryReader.ReadInt32();
			int num2 = binaryReader.ReadInt32();
			int weightCount = _scheduler.GetWeightCount();
			if (num2 != weightCount)
			{
				_scheduler.InitializeWeights();
				_modelVersion++;
				_initializationTime = DateTime.Now;
				_disableExploration = false;
				return;
			}
			float[] array = new float[num2];
			for (int i = 0; i < num2; i++)
			{
				array[i] = binaryReader.ReadSingle();
			}
			_scheduler.SetAllWeights(array);
			_modelVersion++;
			if (num >= 2)
			{
				_disableExploration = binaryReader.ReadBoolean();
				long dateData = binaryReader.ReadInt64();
				_initializationTime = DateTime.FromBinary(dateData);
				ExplorationDurationMinutes = binaryReader.ReadDouble();
				if (!_disableExploration && (DateTime.Now - _initializationTime).TotalMinutes >= ExplorationDurationMinutes)
				{
					_disableExploration = true;
				}
			}
			else
			{
				_initializationTime = DateTime.Now;
				_disableExploration = false;
			}
		}

		public string GetCoreSelectionDistribution()
		{
			int totalDecisions = _totalDecisions;
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("========== 核心选择分布 ==========");
			stringBuilder.AppendLine($"总决策数: {totalDecisions}");
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("--- 大核 [0-7] ---");
			int num = 0;
			for (int i = 0; i < Math.Min(8, _numCores); i++)
			{
				double num2 = ((totalDecisions > 0) ? (100.0 * (double)_coreSelectionCounts[i] / (double)totalDecisions) : 0.0);
				stringBuilder.AppendLine($"C{i}: {_coreSelectionCounts[i],5} 次 ({num2,5:F1}%)");
				num += _coreSelectionCounts[i];
			}
			double num3 = ((totalDecisions > 0) ? (100.0 * (double)num / (double)totalDecisions) : 0.0);
			stringBuilder.AppendLine($"大核合计: {num} 次 ({num3:F1}%)");
			if (_numCores > 8)
			{
				stringBuilder.AppendLine();
				stringBuilder.AppendLine("--- 小核 [8+] ---");
				int num4 = 0;
				for (int j = 8; j < _numCores; j++)
				{
					double num5 = ((totalDecisions > 0) ? (100.0 * (double)_coreSelectionCounts[j] / (double)totalDecisions) : 0.0);
					stringBuilder.AppendLine($"C{j}: {_coreSelectionCounts[j],5} 次 ({num5,5:F1}%)");
					num4 += _coreSelectionCounts[j];
				}
				double num6 = ((totalDecisions > 0) ? (100.0 * (double)num4 / (double)totalDecisions) : 0.0);
				stringBuilder.AppendLine($"小核合计: {num4} 次 ({num6:F1}%)");
			}
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("--- 负载均衡指标 ---");
			double num7 = 0.0;
			for (int k = 0; k < _numCores; k++)
			{
				double num8 = ((totalDecisions > 0) ? ((double)_coreSelectionCounts[k] / (double)totalDecisions) : 0.0);
				if (num8 > 0.0)
				{
					num7 -= num8 * Math.Log(num8, 2.0);
				}
			}
			double num9 = Math.Log(_numCores, 2.0);
			double num10 = ((num9 > 0.0) ? (num7 / num9) : 0.0);
			stringBuilder.AppendLine($"熵值: {num7:F3} / {num9:F3} (最大熵)");
			stringBuilder.AppendLine($"均衡度: {num10 * 100.0:F1}% (100% = 完美均衡)");
			stringBuilder.AppendLine("================================");
			return stringBuilder.ToString();
		}
	}
}
