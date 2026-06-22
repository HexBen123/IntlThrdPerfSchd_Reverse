using System;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace IntlThrdPerfSchd
{
	public class RealtimeScheduler : IDisposable
	{
		public const int ThreadFeatureDim = 5;

		public const int CoreFeatureDim = 7;

		public const int NumHeads = 8;

		public const int HeadDim = 16;

		public const int DModel = 128;

		public const int ThreadInstructions = 0;

		public const int ThreadIPC = 1;

		public const int ThreadPriority = 2;

		public const int ThreadLLCMissRate = 3;

		public const int ThreadBranchMispredRate = 4;

		public const int CoreUtilization = 0;

		public const int CoreAvgQueueExecTime = 1;

		public const int CoreQueueThreads = 2;

		public const int CoreLLCMissRate = 3;

		public const int CoreL1MissRate = 3;

		public const int CoreAvgIPC = 4;

		public const int CorePerformance = 5;

		public const int CorePriority = 6;

		private float[] _threadProjW;

		private float[] _threadProjB;

		private float[] _coreProjW;

		private float[] _coreProjB;

		private float[] _Wq;

		private float[] _Wk;

		private float[] _Wv;

		private float[] _Wo;

		private float[] _WoBias;

		private float[] _ffW1;

		private float[] _ffB1;

		private float[] _ffW2;

		private float[] _ffB2;

		private float[] _outputW;

		private float[] _outputB;

		private float _utilPenalty;

		private float _queuePenalty;

		private float[] _threadEmbed;

		private float[] _coreEmbed;

		private float[] _q;

		private float[] _k;

		private float[] _v;

		private float[] _attentionScores;

		private float[] _attentionOutput;

		private float[] _ffHidden;

		private float[] _ffOutput;

		private int _maxCores;

		private float[] _allCoreEmbeds;

		private float[] _allK;

		private float[] _allV;

		private float[] _allAttentionScores;

		private float[] _allHeadOutputs;

		private float[] _finalScores;

		private float[] _cachedThreadInput;

		private float[][] _cachedCoreInputs;

		private float[] _cachedAttentionScores;

		private float[] _cachedAttentionProbs;

		private float[] _cachedProjectedAttention;

		private float[] _cachedFfPreActivation;

		private int _cachedNumCores;

		private float[] _threadProjWGrad;

		private float[] _threadProjBGrad;

		private float[] _coreProjWGrad;

		private float[] _coreProjBGrad;

		private float[] _WqGrad;

		private float[] _WkGrad;

		private float[] _WvGrad;

		private float[] _WoGrad;

		private float[] _WoBiasGrad;

		private float[] _ffW1Grad;

		private float[] _ffB1Grad;

		private float[] _ffW2Grad;

		private float[] _ffB2Grad;

		private float[] _outputWGrad;

		private float[] _outputBGrad;

		private float _utilPenaltyGrad;

		private float _queuePenaltyGrad;

		private float[] _tempBuffer1;

		private float[] _tempBuffer2;

		private float[] _tempBuffer3;

		private float[] _tempGradScores;

		private float[] _tempGradAttentionProbs;

		private float[] _tempGradAllV;

		private float[] _tempGradAttentionScores;

		private float[] _tempGradQ;

		private float[] _tempGradAllK;

		private float[] _tempGradAllCoreEmbeds;

		private float[] _tempSoftmaxProbs;

		private long _inferenceCount;

		private double _totalInferenceTimeUs;

		private readonly object _statsLock = new object();

		private float _attentionTemperature = 3f;

		private float[] _gradFfOutput;

		private float[] _gradAttentionOutput;

		private float[] _gradAllHeadOutputs;

		private float[] _gradQ;

		private float[] _gradAllK;

		private float[] _gradAllV;

		private float[] _gradThreadEmbed;

		private float[] _gradAllCoreEmbeds;

		private bool _disposed;

		public float AttentionTemperature
		{
			get
			{
				return _attentionTemperature;
			}
			set
			{
				_attentionTemperature = Math.Max(0.1f, value);
			}
		}

		public float[][] GetRawAttentionScores(int numCores)
		{
			float[][] array = new float[8][];
			for (int i = 0; i < 8; i++)
			{
				array[i] = new float[numCores];
				for (int j = 0; j < numCores; j++)
				{
					array[i][j] = _cachedAttentionScores[i * _maxCores + j];
				}
			}
			return array;
		}

		public string GetAttentionScoreStats(int numCores)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("========== 注意力分数统计 ==========");
			for (int i = 0; i < 8; i++)
			{
				float num = float.MaxValue;
				float num2 = float.MinValue;
				float num3 = 0f;
				int num4 = 0;
				for (int j = 0; j < numCores; j++)
				{
					float num5 = _cachedAttentionScores[i * _maxCores + j];
					if (num5 < num)
					{
						num = num5;
					}
					if (num5 > num2)
					{
						num2 = num5;
						num4 = j;
					}
					num3 += num5;
				}
				float num6 = num3 / (float)numCores;
				float num7 = num2 - num;
				stringBuilder.AppendLine($"Head {i}: min={num:F2}, max={num2:F2} (C{num4}), avg={num6:F2}, range={num7:F2}");
			}
			stringBuilder.AppendLine($"温度参数: {_attentionTemperature:F2}");
			stringBuilder.AppendLine("====================================");
			return stringBuilder.ToString();
		}

		public RealtimeScheduler(int maxCores = 16)
		{
			if (maxCores <= 0)
			{
				throw new ArgumentException("maxCores must be positive", "maxCores");
			}
			_maxCores = maxCores;
			InitializeWeights();
			InitializeBuffers();
		}

		public void InitializeWeights()
		{
			float value = 0.01f;
			_threadProjW = InitializeUniform(128, 5, value);
			_threadProjB = new float[128];
			_coreProjW = InitializeUniform(128, 7, value);
			_coreProjB = new float[128];
			_Wq = InitializeUniform(128, 128, value);
			_Wk = InitializeUniform(128, 128, value);
			_Wv = InitializeUniform(128, 128, value);
			_Wo = InitializeUniform(128, 128, value);
			_WoBias = new float[128];
			int num = 512;
			_ffW1 = InitializeUniform(num, 128, value);
			_ffB1 = new float[num];
			_ffW2 = InitializeUniform(128, num, value);
			_ffB2 = new float[128];
			_outputW = InitializeUniform(1, 128, value);
			_outputB = new float[1];
			_utilPenalty = 0.5f;
			_queuePenalty = 0.1f;
		}

		private float[] InitializeUniform(int rows, int cols, float value)
		{
			float[] array = new float[rows * cols];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = value;
			}
			return array;
		}

		private void InitializeBuffers()
		{
			_threadEmbed = new float[128];
			_coreEmbed = new float[128];
			_q = new float[128];
			_k = new float[128];
			_v = new float[128];
			_attentionScores = new float[8];
			_attentionOutput = new float[128];
			_ffHidden = new float[512];
			_ffOutput = new float[128];
			_allCoreEmbeds = new float[_maxCores * 128];
			_allK = new float[_maxCores * 8 * 16];
			_allV = new float[_maxCores * 8 * 16];
			_allAttentionScores = new float[8 * _maxCores];
			_allHeadOutputs = new float[128];
			_finalScores = new float[_maxCores];
			_cachedCoreInputs = new float[_maxCores][];
			for (int i = 0; i < _maxCores; i++)
			{
				_cachedCoreInputs[i] = new float[7];
			}
			_cachedAttentionScores = new float[8 * _maxCores];
			_cachedAttentionProbs = new float[8 * _maxCores];
			_cachedProjectedAttention = new float[128];
			_cachedFfPreActivation = new float[512];
			_cachedThreadInput = new float[5];
			_threadProjWGrad = new float[640];
			_threadProjBGrad = new float[128];
			_coreProjWGrad = new float[896];
			_coreProjBGrad = new float[128];
			_WqGrad = new float[16384];
			_WkGrad = new float[16384];
			_WvGrad = new float[16384];
			_WoGrad = new float[16384];
			_WoBiasGrad = new float[128];
			_ffW1Grad = new float[65536];
			_ffB1Grad = new float[512];
			_ffW2Grad = new float[65536];
			_ffB2Grad = new float[128];
			_outputWGrad = new float[128];
			_outputBGrad = new float[1];
			_tempBuffer1 = new float[128];
			_tempBuffer2 = new float[512];
			_tempBuffer3 = new float[128];
			_tempGradScores = new float[_maxCores];
			_tempGradAttentionProbs = new float[8 * _maxCores];
			_tempGradAllV = new float[_maxCores * 8 * 16];
			_tempGradAttentionScores = new float[8 * _maxCores];
			_tempGradQ = new float[128];
			_tempGradAllK = new float[_maxCores * 8 * 16];
			_tempGradAllCoreEmbeds = new float[_maxCores * 128];
			_tempSoftmaxProbs = new float[_maxCores];
		}

		public int Predict(float[] threadFeatures, float[][] coreFeatures, int numCores)
		{
			if (threadFeatures == null || threadFeatures.Length != 5)
			{
				throw new ArgumentException($"Thread features must have {5} elements");
			}
			if (coreFeatures == null || coreFeatures.Length < numCores)
			{
				throw new ArgumentException("Not enough core features provided");
			}
			if (numCores <= 0 || numCores > _maxCores)
			{
				throw new ArgumentException($"numCores must be between 1 and {_maxCores}");
			}
			Stopwatch stopwatch = Stopwatch.StartNew();
			ProjectThread(threadFeatures);
			ComputeQ();
			for (int i = 0; i < numCores; i++)
			{
				int num = coreFeatures[i].Length;
				if (num >= 7)
				{
					Array.Copy(coreFeatures[i], _cachedCoreInputs[i], 7);
				}
				else
				{
					Array.Copy(coreFeatures[i], _cachedCoreInputs[i], num);
					for (int j = num; j < 7; j++)
					{
						_cachedCoreInputs[i][j] = ((j == 6) ? ((i < 8) ? 1f : 0f) : 0f);
					}
				}
				ProjectCore(_cachedCoreInputs[i], i);
			}
			ComputeAllKV(numCores);
			ComputeAttention(numCores);
			FeedForward();
			int result = ComputeFinalScores(numCores);
			stopwatch.Stop();
			lock (_statsLock)
			{
				_inferenceCount++;
				_totalInferenceTimeUs += stopwatch.Elapsed.TotalMilliseconds * 1000.0;
				return result;
			}
		}

		public float[] GetCoreEmbedding(int coreIdx)
		{
			if (coreIdx < 0 || coreIdx >= _maxCores)
			{
				throw new ArgumentException($"Core index must be between 0 and {_maxCores - 1}");
			}
			float[] array = new float[128];
			Array.Copy(_allCoreEmbeds, coreIdx * 128, array, 0, 128);
			return array;
		}

		public double GetAverageInferenceTimeUs()
		{
			lock (_statsLock)
			{
				return (_inferenceCount > 0) ? (_totalInferenceTimeUs / (double)_inferenceCount) : 0.0;
			}
		}

		public long GetInferenceCount()
		{
			lock (_statsLock)
			{
				return _inferenceCount;
			}
		}

		public float[][] GetAttentionWeights(int numCores)
		{
			float[][] array = new float[8][];
			for (int i = 0; i < 8; i++)
			{
				array[i] = new float[numCores];
				for (int j = 0; j < numCores; j++)
				{
					array[i][j] = _allAttentionScores[i * _maxCores + j];
				}
			}
			return array;
		}

		public (int bestCore, float[] probs) PredictWithCache(float[] threadFeatures, float[][] coreFeatures, int numCores)
		{
			if (threadFeatures == null || threadFeatures.Length != 5)
			{
				throw new ArgumentException($"Thread features must have {5} elements");
			}
			if (coreFeatures == null || coreFeatures.Length < numCores)
			{
				throw new ArgumentException("Not enough core features provided");
			}
			if (numCores <= 0 || numCores > _maxCores)
			{
				throw new ArgumentException($"numCores must be between 1 and {_maxCores}");
			}
			for (int i = 0; i < numCores; i++)
			{
				if (coreFeatures[i] == null)
				{
					throw new ArgumentException($"Core {i} features is null");
				}
			}
			Array.Copy(threadFeatures, _cachedThreadInput, 5);
			for (int j = 0; j < numCores; j++)
			{
				int num = coreFeatures[j].Length;
				if (num >= 7)
				{
					Array.Copy(coreFeatures[j], _cachedCoreInputs[j], 7);
					continue;
				}
				Array.Copy(coreFeatures[j], _cachedCoreInputs[j], num);
				for (int k = num; k < 7; k++)
				{
					_cachedCoreInputs[j][k] = ((k == 6) ? ((j < 8) ? 1f : 0f) : 0f);
				}
			}
			_cachedNumCores = numCores;
			ProjectThread(threadFeatures);
			ComputeQ();
			for (int l = 0; l < numCores; l++)
			{
				ProjectCore(_cachedCoreInputs[l], l);
			}
			ComputeAllKV(numCores);
			ComputeAttentionWithCache(numCores);
			FeedForwardWithCache();
			int item = ComputeFinalScores(numCores);
			float[] item2 = Softmax(_finalScores, 0, numCores);
			return (bestCore: item, probs: item2);
		}

		public void Backward(int selectedCore, float advantage)
		{
			if (_cachedNumCores == 0)
			{
				throw new InvalidOperationException("Must call PredictWithCache before Backward");
			}
			int cachedNumCores = _cachedNumCores;
			BackwardOutput(selectedCore, advantage, cachedNumCores);
			BackwardFeedForward();
			BackwardAttentionOutput();
			BackwardAttention(cachedNumCores);
			BackwardQKV(cachedNumCores);
			BackwardInputProjections(cachedNumCores);
		}

		public void ClearGradients()
		{
			Array.Clear(_threadProjWGrad, 0, _threadProjWGrad.Length);
			Array.Clear(_threadProjBGrad, 0, _threadProjBGrad.Length);
			Array.Clear(_coreProjWGrad, 0, _coreProjWGrad.Length);
			Array.Clear(_coreProjBGrad, 0, _coreProjBGrad.Length);
			Array.Clear(_WqGrad, 0, _WqGrad.Length);
			Array.Clear(_WkGrad, 0, _WkGrad.Length);
			Array.Clear(_WvGrad, 0, _WvGrad.Length);
			Array.Clear(_WoGrad, 0, _WoGrad.Length);
			Array.Clear(_WoBiasGrad, 0, _WoBiasGrad.Length);
			Array.Clear(_ffW1Grad, 0, _ffW1Grad.Length);
			Array.Clear(_ffB1Grad, 0, _ffB1Grad.Length);
			Array.Clear(_ffW2Grad, 0, _ffW2Grad.Length);
			Array.Clear(_ffB2Grad, 0, _ffB2Grad.Length);
			Array.Clear(_outputWGrad, 0, _outputWGrad.Length);
			Array.Clear(_outputBGrad, 0, _outputBGrad.Length);
			_utilPenaltyGrad = 0f;
			_queuePenaltyGrad = 0f;
		}

		public void ApplyGradients(float learningRate, int batchSize = 1)
		{
			float num = learningRate / (float)batchSize;
			float num2 = 1f;
			float num3 = 0f;
			num3 += ComputeNorm(_threadProjWGrad, _threadProjWGrad.Length);
			num3 += ComputeNorm(_threadProjBGrad, _threadProjBGrad.Length);
			num3 += ComputeNorm(_coreProjWGrad, _coreProjWGrad.Length);
			num3 += ComputeNorm(_coreProjBGrad, _coreProjBGrad.Length);
			num3 += ComputeNorm(_WqGrad, _WqGrad.Length);
			num3 += ComputeNorm(_WkGrad, _WkGrad.Length);
			num3 += ComputeNorm(_WvGrad, _WvGrad.Length);
			num3 += ComputeNorm(_WoGrad, _WoGrad.Length);
			num3 += ComputeNorm(_WoBiasGrad, _WoBiasGrad.Length);
			num3 += ComputeNorm(_ffW1Grad, _ffW1Grad.Length);
			num3 += ComputeNorm(_ffB1Grad, _ffB1Grad.Length);
			num3 += ComputeNorm(_ffW2Grad, _ffW2Grad.Length);
			num3 += ComputeNorm(_ffB2Grad, _ffB2Grad.Length);
			num3 += ComputeNorm(_outputWGrad, _outputWGrad.Length);
			num3 += ComputeNorm(_outputBGrad, _outputBGrad.Length);
			num3 = (float)Math.Sqrt(num3);
			float num4 = 1f;
			if (num3 > num2 && num3 > 0f && !float.IsNaN(num3) && !float.IsInfinity(num3))
			{
				num4 = num2 / num3;
			}
			float num5 = num * num4;
			AddScaledSafe(_threadProjW, _threadProjWGrad, num5, _threadProjW.Length);
			AddScaledSafe(_threadProjB, _threadProjBGrad, num5, _threadProjB.Length);
			AddScaledSafe(_coreProjW, _coreProjWGrad, num5, _coreProjW.Length);
			AddScaledSafe(_coreProjB, _coreProjBGrad, num5, _coreProjB.Length);
			AddScaledSafe(_Wq, _WqGrad, num5, _Wq.Length);
			AddScaledSafe(_Wk, _WkGrad, num5, _Wk.Length);
			AddScaledSafe(_Wv, _WvGrad, num5, _Wv.Length);
			AddScaledSafe(_Wo, _WoGrad, num5, _Wo.Length);
			AddScaledSafe(_WoBias, _WoBiasGrad, num5, _WoBias.Length);
			AddScaledSafe(_ffW1, _ffW1Grad, num5, _ffW1.Length);
			AddScaledSafe(_ffB1, _ffB1Grad, num5, _ffB1.Length);
			AddScaledSafe(_ffW2, _ffW2Grad, num5, _ffW2.Length);
			AddScaledSafe(_ffB2, _ffB2Grad, num5, _ffB2.Length);
			AddScaledSafe(_outputW, _outputWGrad, num5, _outputW.Length);
			AddScaledSafe(_outputB, _outputBGrad, num5, _outputB.Length);
			if (!float.IsNaN(_utilPenaltyGrad) && !float.IsInfinity(_utilPenaltyGrad))
			{
				_utilPenalty += num5 * _utilPenaltyGrad;
				_utilPenalty = Math.Max(0f, Math.Min(10f, _utilPenalty));
			}
			if (!float.IsNaN(_queuePenaltyGrad) && !float.IsInfinity(_queuePenaltyGrad))
			{
				_queuePenalty += num5 * _queuePenaltyGrad;
				_queuePenalty = Math.Max(0f, Math.Min(10f, _queuePenalty));
			}
		}

		private float ComputeNorm(float[] grad, int length)
		{
			float num = 0f;
			for (int i = 0; i < length; i++)
			{
				if (!float.IsNaN(grad[i]) && !float.IsInfinity(grad[i]))
				{
					num += grad[i] * grad[i];
				}
			}
			return num;
		}

		private void AddScaledSafe(float[] weights, float[] grads, float scale, int length)
		{
			for (int i = 0; i < length; i++)
			{
				if (!float.IsNaN(grads[i]) && !float.IsInfinity(grads[i]))
				{
					float num = weights[i] + scale * grads[i];
					if (!float.IsNaN(num) && !float.IsInfinity(num))
					{
						weights[i] = Math.Max(-100f, Math.Min(100f, num));
					}
				}
			}
		}

		private float[] NormalizeThreadFeatures(float[] features)
		{
			return new float[5]
			{
				(float)Math.Log(1.0 + (double)features[0]) / 20f,
				Math.Min(features[1] / 5f, 1f),
				Math.Min(features[2] / 32f, 1f),
				Math.Min(features[3], 1f),
				Math.Min(features[4], 1f)
			};
		}

		private float[] NormalizeCoreFeatures(float[] features)
		{
			return new float[7]
			{
				Math.Min(features[0], 1f),
				Math.Min(features[1] / 1000f, 1f),
				Math.Min(features[2] / 20f, 1f),
				Math.Min(features[3], 1f),
				Math.Min(features[4] / 5f, 1f),
				Math.Min(features[5] / 10000f, 1f),
				Math.Min(features[6], 1f)
			};
		}

		private void ProjectThread(float[] features)
		{
			float[] vector = NormalizeThreadFeatures(features);
			MatVecMul(_threadEmbed, _threadProjW, vector, 128, 5);
			VectorAdd(_threadEmbed, _threadProjB, 128);
		}

		private void ProjectCore(float[] features, int coreIdx)
		{
			float[] vector = NormalizeCoreFeatures(features);
			int num = coreIdx * 128;
			MatVecMul(_allCoreEmbeds, num, _coreProjW, vector, 128, 7);
			VectorAdd(_allCoreEmbeds, num, _coreProjB, 128);
		}

		private void ComputeQ()
		{
			MatVecMul(_q, _Wq, _threadEmbed, 128, 128);
		}

		private void ComputeAllKV(int numCores)
		{
			for (int i = 0; i < numCores; i++)
			{
				int vectorOffset = i * 128;
				int resultOffset = i * 8 * 16;
				int resultOffset2 = i * 8 * 16;
				MatVecMul(_allK, resultOffset, _Wk, _allCoreEmbeds, vectorOffset, 128, 128);
				MatVecMul(_allV, resultOffset2, _Wv, _allCoreEmbeds, vectorOffset, 128, 128);
			}
		}

		private void ComputeAttention(int numCores)
		{
			Array.Clear(_attentionOutput, 0, 128);
			Array.Clear(_allHeadOutputs, 0, 128);
			for (int i = 0; i < 8; i++)
			{
				int offsetA = i * 16;
				for (int j = 0; j < numCores; j++)
				{
					int offsetB = j * 8 * 16 + i * 16;
					float num = DotProduct(_q, offsetA, _allK, offsetB, 16);
					num /= (float)Math.Sqrt(16.0);
					_allAttentionScores[i * _maxCores + j] = num;
				}
				SoftMax(_allAttentionScores, i * _maxCores, numCores);
			}
			for (int k = 0; k < 8; k++)
			{
				for (int l = 0; l < numCores; l++)
				{
					float scalar = _allAttentionScores[k * _maxCores + l];
					int sourceOffset = l * 8 * 16 + k * 16;
					int resultOffset = k * 16;
					VectorScalarAdd(_allHeadOutputs, resultOffset, _allV, sourceOffset, scalar, 16);
				}
			}
			Array.Copy(_allHeadOutputs, _attentionOutput, 128);
			MatVecMul(_tempBuffer1, _Wo, _attentionOutput, 128, 128);
			VectorAdd(_tempBuffer1, _WoBias, 128);
			Array.Copy(_tempBuffer1, _attentionOutput, 128);
		}

		private void FeedForward()
		{
			MatVecMul(_ffHidden, _ffW1, _attentionOutput, 512, 128);
			VectorAdd(_ffHidden, _ffB1, 512);
			ReLU(_ffHidden, 512);
			MatVecMul(_ffOutput, _ffW2, _ffHidden, 128, 512);
			VectorAdd(_ffOutput, _ffB2, 128);
		}

		private int ComputeFinalScores(int numCores)
		{
			for (int i = 0; i < numCores; i++)
			{
				int num = i * 128;
				for (int j = 0; j < 128; j++)
				{
					_tempBuffer1[j] = _ffOutput[j] + _allCoreEmbeds[num + j];
				}
				float num2 = DotProduct(_outputW, 0, _tempBuffer1, 0, 128) + _outputB[0];
				float num3 = _cachedCoreInputs[i][0];
				num2 -= num3 * _utilPenalty;
				float num4 = _cachedCoreInputs[i][2];
				num2 -= num4 * _queuePenalty;
				_finalScores[i] = num2;
			}
			int result = 0;
			float num5 = _finalScores[0];
			for (int k = 1; k < numCores; k++)
			{
				if (_finalScores[k] > num5)
				{
					num5 = _finalScores[k];
					result = k;
				}
			}
			return result;
		}

		private void ComputeAttentionWithCache(int numCores)
		{
			Array.Clear(_attentionOutput, 0, 128);
			Array.Clear(_allHeadOutputs, 0, 128);
			for (int i = 0; i < 8; i++)
			{
				int offsetA = i * 16;
				for (int j = 0; j < numCores; j++)
				{
					int offsetB = j * 8 * 16 + i * 16;
					float num = DotProduct(_q, offsetA, _allK, offsetB, 16);
					num /= (float)Math.Sqrt(16.0);
					_cachedAttentionScores[i * _maxCores + j] = num;
					_allAttentionScores[i * _maxCores + j] = num;
				}
				SoftMax(_allAttentionScores, i * _maxCores, numCores);
				for (int k = 0; k < numCores; k++)
				{
					_cachedAttentionProbs[i * _maxCores + k] = _allAttentionScores[i * _maxCores + k];
				}
			}
			for (int l = 0; l < 8; l++)
			{
				for (int m = 0; m < numCores; m++)
				{
					float scalar = _allAttentionScores[l * _maxCores + m];
					int sourceOffset = m * 8 * 16 + l * 16;
					int resultOffset = l * 16;
					VectorScalarAdd(_allHeadOutputs, resultOffset, _allV, sourceOffset, scalar, 16);
				}
			}
			Array.Copy(_allHeadOutputs, _attentionOutput, 128);
			MatVecMul(_cachedProjectedAttention, _Wo, _attentionOutput, 128, 128);
			VectorAdd(_cachedProjectedAttention, _WoBias, 128);
			Array.Copy(_cachedProjectedAttention, _attentionOutput, 128);
		}

		private void FeedForwardWithCache()
		{
			MatVecMul(_cachedFfPreActivation, _ffW1, _attentionOutput, 512, 128);
			VectorAdd(_cachedFfPreActivation, _ffB1, 512);
			Array.Copy(_cachedFfPreActivation, _ffHidden, 512);
			ReLU(_ffHidden, 512);
			MatVecMul(_ffOutput, _ffW2, _ffHidden, 128, 512);
			VectorAdd(_ffOutput, _ffB2, 128);
		}

		private float[] Softmax(float[] scores, int offset, int length)
		{
			float[] tempSoftmaxProbs = _tempSoftmaxProbs;
			Array.Clear(tempSoftmaxProbs, 0, length);
			float num = scores[offset];
			for (int i = 1; i < length; i++)
			{
				if (scores[offset + i] > num)
				{
					num = scores[offset + i];
				}
			}
			float num2 = 1f / _attentionTemperature;
			float num3 = 0f;
			for (int j = 0; j < length; j++)
			{
				tempSoftmaxProbs[j] = (float)Math.Exp((scores[offset + j] - num) * num2);
				num3 += tempSoftmaxProbs[j];
			}
			float num4 = 1f / num3;
			for (int k = 0; k < length; k++)
			{
				tempSoftmaxProbs[k] *= num4;
			}
			return tempSoftmaxProbs;
		}

		private void BackwardOutput(int selectedCore, float advantage, int numCores)
		{
			float[] array = Softmax(_finalScores, 0, numCores);
			float[] tempGradScores = _tempGradScores;
			Array.Clear(tempGradScores, 0, numCores);
			for (int i = 0; i < numCores; i++)
			{
				tempGradScores[i] = array[i] * advantage;
			}
			tempGradScores[selectedCore] -= advantage;
			int num = selectedCore * 128;
			float[] tempBuffer = _tempBuffer1;
			for (int j = 0; j < 128; j++)
			{
				tempBuffer[j] = _ffOutput[j] + _allCoreEmbeds[num + j];
			}
			for (int k = 0; k < 128; k++)
			{
				_outputWGrad[k] += tempGradScores[selectedCore] * tempBuffer[k];
			}
			_outputBGrad[0] += tempGradScores[selectedCore];
			float[] tempGradAllCoreEmbeds = _tempGradAllCoreEmbeds;
			for (int l = 0; l < 128; l++)
			{
				tempGradAllCoreEmbeds[num + l] += tempGradScores[selectedCore] * _outputW[l];
			}
			float[] tempBuffer2 = _tempBuffer3;
			Array.Clear(tempBuffer2, 0, 128);
			for (int m = 0; m < 128; m++)
			{
				tempBuffer2[m] = tempGradScores[selectedCore] * _outputW[m];
			}
			float num2 = _cachedCoreInputs[selectedCore][0];
			_utilPenaltyGrad -= tempGradScores[selectedCore] * num2;
			float num3 = _cachedCoreInputs[selectedCore][2];
			_queuePenaltyGrad -= tempGradScores[selectedCore] * num3;
			BackwardFfOutput(tempBuffer2);
		}

		private void BackwardFfOutput(float[] gradOutput)
		{
			_gradFfOutput = gradOutput;
		}

		private void BackwardFeedForward()
		{
			float[] gradFfOutput = _gradFfOutput;
			for (int i = 0; i < 128; i++)
			{
				for (int j = 0; j < 512; j++)
				{
					_ffW2Grad[i * 512 + j] += gradFfOutput[i] * _ffHidden[j];
				}
			}
			for (int k = 0; k < 128; k++)
			{
				_ffB2Grad[k] += gradFfOutput[k];
			}
			float[] tempBuffer = _tempBuffer2;
			Array.Clear(tempBuffer, 0, 512);
			for (int l = 0; l < 512; l++)
			{
				for (int m = 0; m < 128; m++)
				{
					tempBuffer[l] += _ffW2[m * 512 + l] * gradFfOutput[m];
				}
			}
			for (int n = 0; n < 512; n++)
			{
				if (_cachedFfPreActivation[n] <= 0f)
				{
					tempBuffer[n] = 0f;
				}
			}
			for (int num = 0; num < 512; num++)
			{
				for (int num2 = 0; num2 < 128; num2++)
				{
					_ffW1Grad[num * 128 + num2] += tempBuffer[num] * _attentionOutput[num2];
				}
			}
			for (int num3 = 0; num3 < 512; num3++)
			{
				_ffB1Grad[num3] += tempBuffer[num3];
			}
			float[] tempBuffer2 = _tempBuffer1;
			Array.Clear(tempBuffer2, 0, 128);
			for (int num4 = 0; num4 < 128; num4++)
			{
				for (int num5 = 0; num5 < 512; num5++)
				{
					tempBuffer2[num4] += _ffW1[num5 * 128 + num4] * tempBuffer[num5];
				}
			}
			_gradAttentionOutput = tempBuffer2;
		}

		private void BackwardAttentionOutput()
		{
			float[] gradAttentionOutput = _gradAttentionOutput;
			for (int i = 0; i < 128; i++)
			{
				for (int j = 0; j < 128; j++)
				{
					_WoGrad[i * 128 + j] += gradAttentionOutput[i] * _allHeadOutputs[j];
				}
			}
			for (int k = 0; k < 128; k++)
			{
				_WoBiasGrad[k] += gradAttentionOutput[k];
			}
			float[] tempBuffer = _tempBuffer3;
			Array.Clear(tempBuffer, 0, 128);
			for (int l = 0; l < 128; l++)
			{
				for (int m = 0; m < 128; m++)
				{
					tempBuffer[l] += _Wo[m * 128 + l] * gradAttentionOutput[m];
				}
			}
			_gradAllHeadOutputs = tempBuffer;
		}

		private void BackwardAttention(int numCores)
		{
			float[] gradAllHeadOutputs = _gradAllHeadOutputs;
			float[] tempGradAttentionProbs = _tempGradAttentionProbs;
			float[] tempGradAllV = _tempGradAllV;
			Array.Clear(tempGradAttentionProbs, 0, 8 * _maxCores);
			Array.Clear(tempGradAllV, 0, _maxCores * 8 * 16);
			for (int i = 0; i < 8; i++)
			{
				int num = i * 16;
				for (int j = 0; j < numCores; j++)
				{
					int num2 = j * 8 * 16 + i * 16;
					float num3 = _cachedAttentionProbs[i * _maxCores + j];
					for (int k = 0; k < 16; k++)
					{
						tempGradAttentionProbs[i * _maxCores + j] += gradAllHeadOutputs[num + k] * _allV[num2 + k];
						tempGradAllV[num2 + k] += num3 * gradAllHeadOutputs[num + k];
					}
				}
			}
			float[] tempGradAttentionScores = _tempGradAttentionScores;
			Array.Clear(tempGradAttentionScores, 0, 8 * _maxCores);
			for (int l = 0; l < 8; l++)
			{
				float num4 = 0f;
				for (int m = 0; m < numCores; m++)
				{
					num4 += tempGradAttentionProbs[l * _maxCores + m] * _cachedAttentionProbs[l * _maxCores + m];
				}
				for (int n = 0; n < numCores; n++)
				{
					float num5 = _cachedAttentionProbs[l * _maxCores + n];
					tempGradAttentionScores[l * _maxCores + n] = num5 * (tempGradAttentionProbs[l * _maxCores + n] - num4);
				}
			}
			float num6 = 1f / (float)Math.Sqrt(16.0);
			float[] tempGradQ = _tempGradQ;
			float[] tempGradAllK = _tempGradAllK;
			Array.Clear(tempGradQ, 0, 128);
			Array.Clear(tempGradAllK, 0, _maxCores * 8 * 16);
			for (int num7 = 0; num7 < 8; num7++)
			{
				int num8 = num7 * 16;
				for (int num9 = 0; num9 < numCores; num9++)
				{
					int num10 = num9 * 8 * 16 + num7 * 16;
					float num11 = tempGradAttentionScores[num7 * _maxCores + num9] * num6;
					for (int num12 = 0; num12 < 16; num12++)
					{
						tempGradQ[num8 + num12] += num11 * _allK[num10 + num12];
						tempGradAllK[num10 + num12] += num11 * _q[num8 + num12];
					}
				}
			}
			_gradQ = tempGradQ;
			_gradAllK = tempGradAllK;
			_gradAllV = tempGradAllV;
		}

		private void BackwardQKV(int numCores)
		{
			float[] gradQ = _gradQ;
			for (int i = 0; i < 128; i++)
			{
				for (int j = 0; j < 128; j++)
				{
					_WqGrad[i * 128 + j] += gradQ[i] * _threadEmbed[j];
				}
			}
			float[] tempBuffer = _tempBuffer1;
			Array.Clear(tempBuffer, 0, 128);
			for (int k = 0; k < 128; k++)
			{
				for (int l = 0; l < 128; l++)
				{
					tempBuffer[k] += _Wq[l * 128 + k] * gradQ[l];
				}
			}
			float[] gradAllK = _gradAllK;
			float[] gradAllV = _gradAllV;
			float[] tempGradAllCoreEmbeds = _tempGradAllCoreEmbeds;
			Array.Clear(tempGradAllCoreEmbeds, 0, _maxCores * 128);
			for (int m = 0; m < numCores; m++)
			{
				int num = m * 128;
				int num2 = m * 8 * 16;
				for (int n = 0; n < 128; n++)
				{
					for (int num3 = 0; num3 < 128; num3++)
					{
						_WkGrad[n * 128 + num3] += gradAllK[num2 + n] * _allCoreEmbeds[num + num3];
						tempGradAllCoreEmbeds[num + num3] += _Wk[n * 128 + num3] * gradAllK[num2 + n];
					}
				}
				for (int num4 = 0; num4 < 128; num4++)
				{
					for (int num5 = 0; num5 < 128; num5++)
					{
						_WvGrad[num4 * 128 + num5] += gradAllV[num2 + num4] * _allCoreEmbeds[num + num5];
						tempGradAllCoreEmbeds[num + num5] += _Wv[num4 * 128 + num5] * gradAllV[num2 + num4];
					}
				}
			}
			_gradThreadEmbed = tempBuffer;
			_gradAllCoreEmbeds = tempGradAllCoreEmbeds;
		}

		private void BackwardInputProjections(int numCores)
		{
			float[] gradThreadEmbed = _gradThreadEmbed;
			float[] gradAllCoreEmbeds = _gradAllCoreEmbeds;
			for (int i = 0; i < 128; i++)
			{
				for (int j = 0; j < 5; j++)
				{
					_threadProjWGrad[i * 5 + j] += gradThreadEmbed[i] * _cachedThreadInput[j];
				}
				_threadProjBGrad[i] += gradThreadEmbed[i];
			}
			for (int k = 0; k < numCores; k++)
			{
				int num = k * 128;
				for (int l = 0; l < 128; l++)
				{
					for (int m = 0; m < 7; m++)
					{
						_coreProjWGrad[l * 7 + m] += gradAllCoreEmbeds[num + l] * _cachedCoreInputs[k][m];
					}
					_coreProjBGrad[l] += gradAllCoreEmbeds[num + l];
				}
			}
		}

		private static void AddScaled(float[] dest, float[] src, float scale, int length)
		{
			int count = Vector<float>.Count;
			Vector<float> vector = new Vector<float>(scale);
			int i;
			for (i = 0; i <= length - count; i += count)
			{
				Vector<float> vector2 = new Vector<float>(dest, i);
				Vector<float> vector3 = new Vector<float>(src, i);
				(vector2 + vector3 * vector).CopyTo(dest, i);
			}
			for (; i < length; i++)
			{
				dest[i] += src[i] * scale;
			}
		}

		private static void MatVecMul(float[] result, float[] matrix, float[] vector, int rows, int cols)
		{
			int count = Vector<float>.Count;
			for (int i = 0; i < rows; i++)
			{
				float num = 0f;
				int j = 0;
				if (cols >= count)
				{
					Vector<float> zero = Vector<float>.Zero;
					for (; j <= cols - count; j += count)
					{
						Vector<float> vector2 = new Vector<float>(matrix, i * cols + j);
						Vector<float> vector3 = new Vector<float>(vector, j);
						zero += vector2 * vector3;
					}
					num = Vector.Dot(zero, Vector<float>.One);
				}
				for (; j < cols; j++)
				{
					num += matrix[i * cols + j] * vector[j];
				}
				result[i] = num;
			}
		}

		private static void MatVecMul(float[] result, int resultOffset, float[] matrix, float[] vector, int rows, int cols)
		{
			int count = Vector<float>.Count;
			for (int i = 0; i < rows; i++)
			{
				float num = 0f;
				int j = 0;
				if (cols >= count)
				{
					Vector<float> zero = Vector<float>.Zero;
					for (; j <= cols - count; j += count)
					{
						Vector<float> vector2 = new Vector<float>(matrix, i * cols + j);
						Vector<float> vector3 = new Vector<float>(vector, j);
						zero += vector2 * vector3;
					}
					num = Vector.Dot(zero, Vector<float>.One);
				}
				for (; j < cols; j++)
				{
					num += matrix[i * cols + j] * vector[j];
				}
				result[resultOffset + i] = num;
			}
		}

		private static void MatVecMul(float[] result, int resultOffset, float[] matrix, float[] vector, int vectorOffset, int rows, int cols)
		{
			int count = Vector<float>.Count;
			for (int i = 0; i < rows; i++)
			{
				float num = 0f;
				int j = 0;
				if (cols >= count)
				{
					Vector<float> zero = Vector<float>.Zero;
					for (; j <= cols - count; j += count)
					{
						Vector<float> vector2 = new Vector<float>(matrix, i * cols + j);
						Vector<float> vector3 = new Vector<float>(vector, vectorOffset + j);
						zero += vector2 * vector3;
					}
					num = Vector.Dot(zero, Vector<float>.One);
				}
				for (; j < cols; j++)
				{
					num += matrix[i * cols + j] * vector[vectorOffset + j];
				}
				result[resultOffset + i] = num;
			}
		}

		private static void VectorAdd(float[] result, float[] bias, int length)
		{
			int count = Vector<float>.Count;
			int i;
			for (i = 0; i <= length - count; i += count)
			{
				Vector<float> vector = new Vector<float>(result, i);
				Vector<float> vector2 = new Vector<float>(bias, i);
				(vector + vector2).CopyTo(result, i);
			}
			for (; i < length; i++)
			{
				result[i] += bias[i];
			}
		}

		private static void VectorAdd(float[] result, int offset, float[] bias, int length)
		{
			int count = Vector<float>.Count;
			int i;
			for (i = 0; i <= length - count; i += count)
			{
				Vector<float> vector = new Vector<float>(result, offset + i);
				Vector<float> vector2 = new Vector<float>(bias, i);
				(vector + vector2).CopyTo(result, offset + i);
			}
			for (; i < length; i++)
			{
				result[offset + i] += bias[i];
			}
		}

		private static float DotProduct(float[] a, int offsetA, float[] b, int offsetB, int length)
		{
			int count = Vector<float>.Count;
			Vector<float> zero = Vector<float>.Zero;
			int i;
			for (i = 0; i <= length - count; i += count)
			{
				Vector<float> vector = new Vector<float>(a, offsetA + i);
				Vector<float> vector2 = new Vector<float>(b, offsetB + i);
				zero += vector * vector2;
			}
			float num = Vector.Dot(zero, Vector<float>.One);
			for (; i < length; i++)
			{
				num += a[offsetA + i] * b[offsetB + i];
			}
			return num;
		}

		private static void VectorScalarAdd(float[] result, int resultOffset, float[] source, int sourceOffset, float scalar, int length)
		{
			int count = Vector<float>.Count;
			Vector<float> vector = new Vector<float>(scalar);
			int i;
			for (i = 0; i <= length - count; i += count)
			{
				Vector<float> vector2 = new Vector<float>(result, resultOffset + i);
				Vector<float> vector3 = new Vector<float>(source, sourceOffset + i);
				(vector2 + vector3 * vector).CopyTo(result, resultOffset + i);
			}
			for (; i < length; i++)
			{
				result[resultOffset + i] += source[sourceOffset + i] * scalar;
			}
		}

		private static void ReLU(float[] data, int length)
		{
			int count = Vector<float>.Count;
			Vector<float> zero = Vector<float>.Zero;
			int i;
			for (i = 0; i <= length - count; i += count)
			{
				Vector.Max(new Vector<float>(data, i), zero).CopyTo(data, i);
			}
			for (; i < length; i++)
			{
				if (data[i] < 0f)
				{
					data[i] = 0f;
				}
			}
		}

		private void SoftMax(float[] data, int offset, int length)
		{
			float num = data[offset];
			for (int i = 1; i < length; i++)
			{
				if (data[offset + i] > num)
				{
					num = data[offset + i];
				}
			}
			float num2 = 1f / _attentionTemperature;
			float num3 = 0f;
			for (int j = 0; j < length; j++)
			{
				data[offset + j] = (float)Math.Exp((data[offset + j] - num) * num2);
				num3 += data[offset + j];
			}
			float num4 = 1f / num3;
			for (int k = 0; k < length; k++)
			{
				data[offset + k] *= num4;
			}
		}

		public float[] GetAllWeights()
		{
			float[] array = new float[_threadProjW.Length + _threadProjB.Length + _coreProjW.Length + _coreProjB.Length + _Wq.Length + _Wk.Length + _Wv.Length + _Wo.Length + _WoBias.Length + _ffW1.Length + _ffB1.Length + _ffW2.Length + _ffB2.Length + _outputW.Length + _outputB.Length + 2];
			int offset = 0;
			CopyToArray(array, ref offset, _threadProjW);
			CopyToArray(array, ref offset, _threadProjB);
			CopyToArray(array, ref offset, _coreProjW);
			CopyToArray(array, ref offset, _coreProjB);
			CopyToArray(array, ref offset, _Wq);
			CopyToArray(array, ref offset, _Wk);
			CopyToArray(array, ref offset, _Wv);
			CopyToArray(array, ref offset, _Wo);
			CopyToArray(array, ref offset, _WoBias);
			CopyToArray(array, ref offset, _ffW1);
			CopyToArray(array, ref offset, _ffB1);
			CopyToArray(array, ref offset, _ffW2);
			CopyToArray(array, ref offset, _ffB2);
			CopyToArray(array, ref offset, _outputW);
			CopyToArray(array, ref offset, _outputB);
			array[offset++] = _utilPenalty;
			array[offset++] = _queuePenalty;
			return array;
		}

		public void SetAllWeights(float[] weights)
		{
			int offset = 0;
			CopyFromArray(weights, ref offset, _threadProjW);
			CopyFromArray(weights, ref offset, _threadProjB);
			CopyFromArray(weights, ref offset, _coreProjW);
			CopyFromArray(weights, ref offset, _coreProjB);
			CopyFromArray(weights, ref offset, _Wq);
			CopyFromArray(weights, ref offset, _Wk);
			CopyFromArray(weights, ref offset, _Wv);
			CopyFromArray(weights, ref offset, _Wo);
			CopyFromArray(weights, ref offset, _WoBias);
			CopyFromArray(weights, ref offset, _ffW1);
			CopyFromArray(weights, ref offset, _ffB1);
			CopyFromArray(weights, ref offset, _ffW2);
			CopyFromArray(weights, ref offset, _ffB2);
			CopyFromArray(weights, ref offset, _outputW);
			CopyFromArray(weights, ref offset, _outputB);
			_utilPenalty = weights[offset++];
			_queuePenalty = weights[offset++];
		}

		public int GetWeightCount()
		{
			return _threadProjW.Length + _threadProjB.Length + _coreProjW.Length + _coreProjB.Length + _Wq.Length + _Wk.Length + _Wv.Length + _Wo.Length + _WoBias.Length + _ffW1.Length + _ffB1.Length + _ffW2.Length + _ffB2.Length + _outputW.Length + _outputB.Length + 2 + 128 + 1;
		}

		public void ApplyGradients(float[] gradients, float learningRate)
		{
			if (gradients.Length != GetWeightCount())
			{
				throw new ArgumentException("Gradient size mismatch");
			}
			float[] allWeights = GetAllWeights();
			for (int i = 0; i < allWeights.Length; i++)
			{
				allWeights[i] += learningRate * gradients[i];
			}
			SetAllWeights(allWeights);
		}

		private static void CopyToArray(float[] dest, ref int offset, float[] source)
		{
			Array.Copy(source, 0, dest, offset, source.Length);
			offset += source.Length;
		}

		private static void CopyFromArray(float[] source, ref int offset, float[] dest)
		{
			Array.Copy(source, offset, dest, 0, dest.Length);
			offset += dest.Length;
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				_disposed = true;
			}
		}

		~RealtimeScheduler()
		{
			Dispose(disposing: false);
		}
	}
}
