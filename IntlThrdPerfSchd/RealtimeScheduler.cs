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
		float[][] scores = new float[8][];
		for (int h = 0; h < 8; h++)
		{
			scores[h] = new float[numCores];
			for (int c = 0; c < numCores; c++)
			{
				scores[h][c] = _cachedAttentionScores[h * _maxCores + c];
			}
		}
		return scores;
	}

	public string GetAttentionScoreStats(int numCores)
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("========== 注意力分数统计 ==========");
		for (int h = 0; h < 8; h++)
		{
			float min = float.MaxValue;
			float max = float.MinValue;
			float sum = 0f;
			int maxIdx = 0;
			for (int c = 0; c < numCores; c++)
			{
				float score = _cachedAttentionScores[h * _maxCores + c];
				if (score < min)
				{
					min = score;
				}
				if (score > max)
				{
					max = score;
					maxIdx = c;
				}
				sum += score;
			}
			float avg = sum / (float)numCores;
			float range = max - min;
			sb.AppendLine($"Head {h}: min={min:F2}, max={max:F2} (C{maxIdx}), avg={avg:F2}, range={range:F2}");
		}
		sb.AppendLine($"温度参数: {_attentionTemperature:F2}");
		sb.AppendLine("====================================");
		return sb.ToString();
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
		float smallValue = 0.01f;
		_threadProjW = InitializeUniform(128, 5, smallValue);
		_threadProjB = new float[128];
		_coreProjW = InitializeUniform(128, 7, smallValue);
		_coreProjB = new float[128];
		_Wq = InitializeUniform(128, 128, smallValue);
		_Wk = InitializeUniform(128, 128, smallValue);
		_Wv = InitializeUniform(128, 128, smallValue);
		_Wo = InitializeUniform(128, 128, smallValue);
		_WoBias = new float[128];
		int ffHidden = 512;
		_ffW1 = InitializeUniform(ffHidden, 128, smallValue);
		_ffB1 = new float[ffHidden];
		_ffW2 = InitializeUniform(128, ffHidden, smallValue);
		_ffB2 = new float[128];
		_outputW = InitializeUniform(1, 128, smallValue);
		_outputB = new float[1];
		_utilPenalty = 0.5f;
		_queuePenalty = 0.1f;
	}

	private float[] InitializeUniform(int rows, int cols, float value)
	{
		float[] matrix = new float[rows * cols];
		for (int i = 0; i < matrix.Length; i++)
		{
			matrix[i] = value;
		}
		return matrix;
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
		Stopwatch sw = Stopwatch.StartNew();
		ProjectThread(threadFeatures);
		ComputeQ();
		for (int c = 0; c < numCores; c++)
		{
			int inputLen = coreFeatures[c].Length;
			if (inputLen >= 7)
			{
				Array.Copy(coreFeatures[c], _cachedCoreInputs[c], 7);
			}
			else
			{
				Array.Copy(coreFeatures[c], _cachedCoreInputs[c], inputLen);
				for (int j = inputLen; j < 7; j++)
				{
					_cachedCoreInputs[c][j] = ((j == 6) ? ((c < 8) ? 1f : 0f) : 0f);
				}
			}
			ProjectCore(_cachedCoreInputs[c], c);
		}
		ComputeAllKV(numCores);
		ComputeAttention(numCores);
		FeedForward();
		int bestCore = ComputeFinalScores(numCores);
		sw.Stop();
		lock (_statsLock)
		{
			_inferenceCount++;
			_totalInferenceTimeUs += sw.Elapsed.TotalMilliseconds * 1000.0;
			return bestCore;
		}
	}

	public float[] GetCoreEmbedding(int coreIdx)
	{
		if (coreIdx < 0 || coreIdx >= _maxCores)
		{
			throw new ArgumentException($"Core index must be between 0 and {_maxCores - 1}");
		}
		float[] embed = new float[128];
		Array.Copy(_allCoreEmbeds, coreIdx * 128, embed, 0, 128);
		return embed;
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
		float[][] weights = new float[8][];
		for (int h = 0; h < 8; h++)
		{
			weights[h] = new float[numCores];
			for (int c = 0; c < numCores; c++)
			{
				weights[h][c] = _allAttentionScores[h * _maxCores + c];
			}
		}
		return weights;
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
		for (int c = 0; c < numCores; c++)
		{
			if (coreFeatures[c] == null)
			{
				throw new ArgumentException($"Core {c} features is null");
			}
		}
		Array.Copy(threadFeatures, _cachedThreadInput, 5);
		for (int i = 0; i < numCores; i++)
		{
			int inputLen = coreFeatures[i].Length;
			if (inputLen >= 7)
			{
				Array.Copy(coreFeatures[i], _cachedCoreInputs[i], 7);
				continue;
			}
			Array.Copy(coreFeatures[i], _cachedCoreInputs[i], inputLen);
			for (int j = inputLen; j < 7; j++)
			{
				_cachedCoreInputs[i][j] = ((j == 6) ? ((i < 8) ? 1f : 0f) : 0f);
			}
		}
		_cachedNumCores = numCores;
		ProjectThread(threadFeatures);
		ComputeQ();
		for (int k = 0; k < numCores; k++)
		{
			ProjectCore(_cachedCoreInputs[k], k);
		}
		ComputeAllKV(numCores);
		ComputeAttentionWithCache(numCores);
		FeedForwardWithCache();
		int item = ComputeFinalScores(numCores);
		float[] probs = Softmax(_finalScores, 0, numCores);
		return (bestCore: item, probs: probs);
	}

	public void Backward(int selectedCore, float advantage)
	{
		if (_cachedNumCores == 0)
		{
			throw new InvalidOperationException("Must call PredictWithCache before Backward");
		}
		int numCores = _cachedNumCores;
		BackwardOutput(selectedCore, advantage, numCores);
		BackwardFeedForward();
		BackwardAttentionOutput();
		BackwardAttention(numCores);
		BackwardQKV(numCores);
		BackwardInputProjections(numCores);
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
		float maxGradNorm = 1f;
		float totalNorm = 0f;
		totalNorm += ComputeNorm(_threadProjWGrad, _threadProjWGrad.Length);
		totalNorm += ComputeNorm(_threadProjBGrad, _threadProjBGrad.Length);
		totalNorm += ComputeNorm(_coreProjWGrad, _coreProjWGrad.Length);
		totalNorm += ComputeNorm(_coreProjBGrad, _coreProjBGrad.Length);
		totalNorm += ComputeNorm(_WqGrad, _WqGrad.Length);
		totalNorm += ComputeNorm(_WkGrad, _WkGrad.Length);
		totalNorm += ComputeNorm(_WvGrad, _WvGrad.Length);
		totalNorm += ComputeNorm(_WoGrad, _WoGrad.Length);
		totalNorm += ComputeNorm(_WoBiasGrad, _WoBiasGrad.Length);
		totalNorm += ComputeNorm(_ffW1Grad, _ffW1Grad.Length);
		totalNorm += ComputeNorm(_ffB1Grad, _ffB1Grad.Length);
		totalNorm += ComputeNorm(_ffW2Grad, _ffW2Grad.Length);
		totalNorm += ComputeNorm(_ffB2Grad, _ffB2Grad.Length);
		totalNorm += ComputeNorm(_outputWGrad, _outputWGrad.Length);
		totalNorm += ComputeNorm(_outputBGrad, _outputBGrad.Length);
		totalNorm = (float)Math.Sqrt(totalNorm);
		float clipScale = 1f;
		if (totalNorm > maxGradNorm && totalNorm > 0f && !float.IsNaN(totalNorm) && !float.IsInfinity(totalNorm))
		{
			clipScale = maxGradNorm / totalNorm;
		}
		float finalScale = num * clipScale;
		AddScaledSafe(_threadProjW, _threadProjWGrad, finalScale, _threadProjW.Length);
		AddScaledSafe(_threadProjB, _threadProjBGrad, finalScale, _threadProjB.Length);
		AddScaledSafe(_coreProjW, _coreProjWGrad, finalScale, _coreProjW.Length);
		AddScaledSafe(_coreProjB, _coreProjBGrad, finalScale, _coreProjB.Length);
		AddScaledSafe(_Wq, _WqGrad, finalScale, _Wq.Length);
		AddScaledSafe(_Wk, _WkGrad, finalScale, _Wk.Length);
		AddScaledSafe(_Wv, _WvGrad, finalScale, _Wv.Length);
		AddScaledSafe(_Wo, _WoGrad, finalScale, _Wo.Length);
		AddScaledSafe(_WoBias, _WoBiasGrad, finalScale, _WoBias.Length);
		AddScaledSafe(_ffW1, _ffW1Grad, finalScale, _ffW1.Length);
		AddScaledSafe(_ffB1, _ffB1Grad, finalScale, _ffB1.Length);
		AddScaledSafe(_ffW2, _ffW2Grad, finalScale, _ffW2.Length);
		AddScaledSafe(_ffB2, _ffB2Grad, finalScale, _ffB2.Length);
		AddScaledSafe(_outputW, _outputWGrad, finalScale, _outputW.Length);
		AddScaledSafe(_outputB, _outputBGrad, finalScale, _outputB.Length);
		if (!float.IsNaN(_utilPenaltyGrad) && !float.IsInfinity(_utilPenaltyGrad))
		{
			_utilPenalty += finalScale * _utilPenaltyGrad;
			_utilPenalty = Math.Max(0f, Math.Min(10f, _utilPenalty));
		}
		if (!float.IsNaN(_queuePenaltyGrad) && !float.IsInfinity(_queuePenaltyGrad))
		{
			_queuePenalty += finalScale * _queuePenaltyGrad;
			_queuePenalty = Math.Max(0f, Math.Min(10f, _queuePenalty));
		}
	}

	private float ComputeNorm(float[] grad, int length)
	{
		float sum = 0f;
		for (int i = 0; i < length; i++)
		{
			if (!float.IsNaN(grad[i]) && !float.IsInfinity(grad[i]))
			{
				sum += grad[i] * grad[i];
			}
		}
		return sum;
	}

	private void AddScaledSafe(float[] weights, float[] grads, float scale, int length)
	{
		for (int i = 0; i < length; i++)
		{
			if (!float.IsNaN(grads[i]) && !float.IsInfinity(grads[i]))
			{
				float newVal = weights[i] + scale * grads[i];
				if (!float.IsNaN(newVal) && !float.IsInfinity(newVal))
				{
					weights[i] = Math.Max(-100f, Math.Min(100f, newVal));
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
		float[] normalized = NormalizeThreadFeatures(features);
		MatVecMul(_threadEmbed, _threadProjW, normalized, 128, 5);
		VectorAdd(_threadEmbed, _threadProjB, 128);
	}

	private void ProjectCore(float[] features, int coreIdx)
	{
		float[] normalized = NormalizeCoreFeatures(features);
		int offset = coreIdx * 128;
		MatVecMul(_allCoreEmbeds, offset, _coreProjW, normalized, 128, 7);
		VectorAdd(_allCoreEmbeds, offset, _coreProjB, 128);
	}

	private void ComputeQ()
	{
		MatVecMul(_q, _Wq, _threadEmbed, 128, 128);
	}

	private void ComputeAllKV(int numCores)
	{
		for (int c = 0; c < numCores; c++)
		{
			int coreOffset = c * 128;
			int kOffset = c * 8 * 16;
			int vOffset = c * 8 * 16;
			MatVecMul(_allK, kOffset, _Wk, _allCoreEmbeds, coreOffset, 128, 128);
			MatVecMul(_allV, vOffset, _Wv, _allCoreEmbeds, coreOffset, 128, 128);
		}
	}

	private void ComputeAttention(int numCores)
	{
		Array.Clear(_attentionOutput, 0, 128);
		Array.Clear(_allHeadOutputs, 0, 128);
		for (int h = 0; h < 8; h++)
		{
			int qOffset = h * 16;
			for (int c = 0; c < numCores; c++)
			{
				int kOffset = c * 8 * 16 + h * 16;
				float score = DotProduct(_q, qOffset, _allK, kOffset, 16);
				score /= (float)Math.Sqrt(16.0);
				_allAttentionScores[h * _maxCores + c] = score;
			}
			SoftMax(_allAttentionScores, h * _maxCores, numCores);
		}
		for (int i = 0; i < 8; i++)
		{
			for (int j = 0; j < numCores; j++)
			{
				float weight = _allAttentionScores[i * _maxCores + j];
				int vOffset = j * 8 * 16 + i * 16;
				int outOffset = i * 16;
				VectorScalarAdd(_allHeadOutputs, outOffset, _allV, vOffset, weight, 16);
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
		for (int c = 0; c < numCores; c++)
		{
			int coreOffset = c * 128;
			for (int i = 0; i < 128; i++)
			{
				_tempBuffer1[i] = _ffOutput[i] + _allCoreEmbeds[coreOffset + i];
			}
			float score = DotProduct(_outputW, 0, _tempBuffer1, 0, 128) + _outputB[0];
			float utilization = _cachedCoreInputs[c][0];
			score -= utilization * _utilPenalty;
			float queueThreads = _cachedCoreInputs[c][2];
			score -= queueThreads * _queuePenalty;
			_finalScores[c] = score;
		}
		int bestCore = 0;
		float maxScore = _finalScores[0];
		for (int j = 1; j < numCores; j++)
		{
			if (_finalScores[j] > maxScore)
			{
				maxScore = _finalScores[j];
				bestCore = j;
			}
		}
		return bestCore;
	}

	private void ComputeAttentionWithCache(int numCores)
	{
		Array.Clear(_attentionOutput, 0, 128);
		Array.Clear(_allHeadOutputs, 0, 128);
		for (int h = 0; h < 8; h++)
		{
			int qOffset = h * 16;
			for (int c = 0; c < numCores; c++)
			{
				int kOffset = c * 8 * 16 + h * 16;
				float score = DotProduct(_q, qOffset, _allK, kOffset, 16);
				score /= (float)Math.Sqrt(16.0);
				_cachedAttentionScores[h * _maxCores + c] = score;
				_allAttentionScores[h * _maxCores + c] = score;
			}
			SoftMax(_allAttentionScores, h * _maxCores, numCores);
			for (int i = 0; i < numCores; i++)
			{
				_cachedAttentionProbs[h * _maxCores + i] = _allAttentionScores[h * _maxCores + i];
			}
		}
		for (int j = 0; j < 8; j++)
		{
			for (int k = 0; k < numCores; k++)
			{
				float weight = _allAttentionScores[j * _maxCores + k];
				int vOffset = k * 8 * 16 + j * 16;
				int outOffset = j * 16;
				VectorScalarAdd(_allHeadOutputs, outOffset, _allV, vOffset, weight, 16);
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
		float[] probs = _tempSoftmaxProbs;
		Array.Clear(probs, 0, length);
		float maxVal = scores[offset];
		for (int i = 1; i < length; i++)
		{
			if (scores[offset + i] > maxVal)
			{
				maxVal = scores[offset + i];
			}
		}
		float invTemp = 1f / _attentionTemperature;
		float sum = 0f;
		for (int j = 0; j < length; j++)
		{
			probs[j] = (float)Math.Exp((scores[offset + j] - maxVal) * invTemp);
			sum += probs[j];
		}
		float invSum = 1f / sum;
		for (int k = 0; k < length; k++)
		{
			probs[k] *= invSum;
		}
		return probs;
	}

	private void BackwardOutput(int selectedCore, float advantage, int numCores)
	{
		float[] probs = Softmax(_finalScores, 0, numCores);
		float[] gradScores = _tempGradScores;
		Array.Clear(gradScores, 0, numCores);
		for (int i = 0; i < numCores; i++)
		{
			gradScores[i] = probs[i] * advantage;
		}
		gradScores[selectedCore] -= advantage;
		int coreOffset = selectedCore * 128;
		float[] combined = _tempBuffer1;
		for (int j = 0; j < 128; j++)
		{
			combined[j] = _ffOutput[j] + _allCoreEmbeds[coreOffset + j];
		}
		for (int k = 0; k < 128; k++)
		{
			_outputWGrad[k] += gradScores[selectedCore] * combined[k];
		}
		_outputBGrad[0] += gradScores[selectedCore];
		float[] gradCoreEmbed = _tempGradAllCoreEmbeds;
		for (int l = 0; l < 128; l++)
		{
			gradCoreEmbed[coreOffset + l] += gradScores[selectedCore] * _outputW[l];
		}
		float[] gradFfOutput = _tempBuffer3;
		Array.Clear(gradFfOutput, 0, 128);
		for (int m = 0; m < 128; m++)
		{
			gradFfOutput[m] = gradScores[selectedCore] * _outputW[m];
		}
		float utilization = _cachedCoreInputs[selectedCore][0];
		_utilPenaltyGrad -= gradScores[selectedCore] * utilization;
		float queueThreads = _cachedCoreInputs[selectedCore][2];
		_queuePenaltyGrad -= gradScores[selectedCore] * queueThreads;
		BackwardFfOutput(gradFfOutput);
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
		float[] gradFfHidden = _tempBuffer2;
		Array.Clear(gradFfHidden, 0, 512);
		for (int l = 0; l < 512; l++)
		{
			for (int m = 0; m < 128; m++)
			{
				gradFfHidden[l] += _ffW2[m * 512 + l] * gradFfOutput[m];
			}
		}
		for (int n = 0; n < 512; n++)
		{
			if (_cachedFfPreActivation[n] <= 0f)
			{
				gradFfHidden[n] = 0f;
			}
		}
		for (int num = 0; num < 512; num++)
		{
			for (int num2 = 0; num2 < 128; num2++)
			{
				_ffW1Grad[num * 128 + num2] += gradFfHidden[num] * _attentionOutput[num2];
			}
		}
		for (int num3 = 0; num3 < 512; num3++)
		{
			_ffB1Grad[num3] += gradFfHidden[num3];
		}
		float[] gradAttentionOutput = _tempBuffer1;
		Array.Clear(gradAttentionOutput, 0, 128);
		for (int num4 = 0; num4 < 128; num4++)
		{
			for (int num5 = 0; num5 < 512; num5++)
			{
				gradAttentionOutput[num4] += _ffW1[num5 * 128 + num4] * gradFfHidden[num5];
			}
		}
		_gradAttentionOutput = gradAttentionOutput;
	}

	private void BackwardAttentionOutput()
	{
		float[] gradOutput = _gradAttentionOutput;
		for (int i = 0; i < 128; i++)
		{
			for (int j = 0; j < 128; j++)
			{
				_WoGrad[i * 128 + j] += gradOutput[i] * _allHeadOutputs[j];
			}
		}
		for (int k = 0; k < 128; k++)
		{
			_WoBiasGrad[k] += gradOutput[k];
		}
		float[] gradAllHeadOutputs = _tempBuffer3;
		Array.Clear(gradAllHeadOutputs, 0, 128);
		for (int l = 0; l < 128; l++)
		{
			for (int m = 0; m < 128; m++)
			{
				gradAllHeadOutputs[l] += _Wo[m * 128 + l] * gradOutput[m];
			}
		}
		_gradAllHeadOutputs = gradAllHeadOutputs;
	}

	private void BackwardAttention(int numCores)
	{
		float[] gradAllHeadOutputs = _gradAllHeadOutputs;
		float[] gradAttentionProbs = _tempGradAttentionProbs;
		float[] gradAllV = _tempGradAllV;
		Array.Clear(gradAttentionProbs, 0, 8 * _maxCores);
		Array.Clear(gradAllV, 0, _maxCores * 8 * 16);
		for (int h = 0; h < 8; h++)
		{
			int outOffset = h * 16;
			for (int c = 0; c < numCores; c++)
			{
				int vOffset = c * 8 * 16 + h * 16;
				float prob = _cachedAttentionProbs[h * _maxCores + c];
				for (int k = 0; k < 16; k++)
				{
					gradAttentionProbs[h * _maxCores + c] += gradAllHeadOutputs[outOffset + k] * _allV[vOffset + k];
					gradAllV[vOffset + k] += prob * gradAllHeadOutputs[outOffset + k];
				}
			}
		}
		float[] gradAttentionScores = _tempGradAttentionScores;
		Array.Clear(gradAttentionScores, 0, 8 * _maxCores);
		for (int i = 0; i < 8; i++)
		{
			float sum = 0f;
			for (int j = 0; j < numCores; j++)
			{
				sum += gradAttentionProbs[i * _maxCores + j] * _cachedAttentionProbs[i * _maxCores + j];
			}
			for (int l = 0; l < numCores; l++)
			{
				float prob2 = _cachedAttentionProbs[i * _maxCores + l];
				gradAttentionScores[i * _maxCores + l] = prob2 * (gradAttentionProbs[i * _maxCores + l] - sum);
			}
		}
		float scale = 1f / (float)Math.Sqrt(16.0);
		float[] gradQ = _tempGradQ;
		float[] gradAllK = _tempGradAllK;
		Array.Clear(gradQ, 0, 128);
		Array.Clear(gradAllK, 0, _maxCores * 8 * 16);
		for (int m = 0; m < 8; m++)
		{
			int qOffset = m * 16;
			for (int n = 0; n < numCores; n++)
			{
				int kOffset = n * 8 * 16 + m * 16;
				float grad = gradAttentionScores[m * _maxCores + n] * scale;
				for (int num = 0; num < 16; num++)
				{
					gradQ[qOffset + num] += grad * _allK[kOffset + num];
					gradAllK[kOffset + num] += grad * _q[qOffset + num];
				}
			}
		}
		_gradQ = gradQ;
		_gradAllK = gradAllK;
		_gradAllV = gradAllV;
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
		float[] gradThreadEmbed = _tempBuffer1;
		Array.Clear(gradThreadEmbed, 0, 128);
		for (int k = 0; k < 128; k++)
		{
			for (int l = 0; l < 128; l++)
			{
				gradThreadEmbed[k] += _Wq[l * 128 + k] * gradQ[l];
			}
		}
		float[] gradAllK = _gradAllK;
		float[] gradAllV = _gradAllV;
		float[] gradAllCoreEmbeds = _tempGradAllCoreEmbeds;
		Array.Clear(gradAllCoreEmbeds, 0, _maxCores * 128);
		for (int c = 0; c < numCores; c++)
		{
			int coreOffset = c * 128;
			int kvOffset = c * 8 * 16;
			for (int m = 0; m < 128; m++)
			{
				for (int n = 0; n < 128; n++)
				{
					_WkGrad[m * 128 + n] += gradAllK[kvOffset + m] * _allCoreEmbeds[coreOffset + n];
					gradAllCoreEmbeds[coreOffset + n] += _Wk[m * 128 + n] * gradAllK[kvOffset + m];
				}
			}
			for (int num = 0; num < 128; num++)
			{
				for (int num2 = 0; num2 < 128; num2++)
				{
					_WvGrad[num * 128 + num2] += gradAllV[kvOffset + num] * _allCoreEmbeds[coreOffset + num2];
					gradAllCoreEmbeds[coreOffset + num2] += _Wv[num * 128 + num2] * gradAllV[kvOffset + num];
				}
			}
		}
		_gradThreadEmbed = gradThreadEmbed;
		_gradAllCoreEmbeds = gradAllCoreEmbeds;
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
		for (int c = 0; c < numCores; c++)
		{
			int coreOffset = c * 128;
			for (int k = 0; k < 128; k++)
			{
				for (int l = 0; l < 7; l++)
				{
					_coreProjWGrad[k * 7 + l] += gradAllCoreEmbeds[coreOffset + k] * _cachedCoreInputs[c][l];
				}
				_coreProjBGrad[k] += gradAllCoreEmbeds[coreOffset + k];
			}
		}
	}

	private static void AddScaled(float[] dest, float[] src, float scale, int length)
	{
		int vectorWidth = Vector<float>.Count;
		Vector<float> scaleVec = new Vector<float>(scale);
		int i;
		for (i = 0; i <= length - vectorWidth; i += vectorWidth)
		{
			Vector<float> vector = new Vector<float>(dest, i);
			Vector<float> s = new Vector<float>(src, i);
			(vector + s * scaleVec).CopyTo(dest, i);
		}
		for (; i < length; i++)
		{
			dest[i] += src[i] * scale;
		}
	}

	private static void MatVecMul(float[] result, float[] matrix, float[] vector, int rows, int cols)
	{
		int vectorWidth = Vector<float>.Count;
		for (int i = 0; i < rows; i++)
		{
			float sum = 0f;
			int j = 0;
			if (cols >= vectorWidth)
			{
				Vector<float> sumVec = Vector<float>.Zero;
				for (; j <= cols - vectorWidth; j += vectorWidth)
				{
					Vector<float> rowVec = new Vector<float>(matrix, i * cols + j);
					Vector<float> vec = new Vector<float>(vector, j);
					sumVec += rowVec * vec;
				}
				sum = Vector.Dot(sumVec, Vector<float>.One);
			}
			for (; j < cols; j++)
			{
				sum += matrix[i * cols + j] * vector[j];
			}
			result[i] = sum;
		}
	}

	private static void MatVecMul(float[] result, int resultOffset, float[] matrix, float[] vector, int rows, int cols)
	{
		int vectorWidth = Vector<float>.Count;
		for (int i = 0; i < rows; i++)
		{
			float sum = 0f;
			int j = 0;
			if (cols >= vectorWidth)
			{
				Vector<float> sumVec = Vector<float>.Zero;
				for (; j <= cols - vectorWidth; j += vectorWidth)
				{
					Vector<float> rowVec = new Vector<float>(matrix, i * cols + j);
					Vector<float> vec = new Vector<float>(vector, j);
					sumVec += rowVec * vec;
				}
				sum = Vector.Dot(sumVec, Vector<float>.One);
			}
			for (; j < cols; j++)
			{
				sum += matrix[i * cols + j] * vector[j];
			}
			result[resultOffset + i] = sum;
		}
	}

	private static void MatVecMul(float[] result, int resultOffset, float[] matrix, float[] vector, int vectorOffset, int rows, int cols)
	{
		int vectorWidth = Vector<float>.Count;
		for (int i = 0; i < rows; i++)
		{
			float sum = 0f;
			int j = 0;
			if (cols >= vectorWidth)
			{
				Vector<float> sumVec = Vector<float>.Zero;
				for (; j <= cols - vectorWidth; j += vectorWidth)
				{
					Vector<float> rowVec = new Vector<float>(matrix, i * cols + j);
					Vector<float> vec = new Vector<float>(vector, vectorOffset + j);
					sumVec += rowVec * vec;
				}
				sum = Vector.Dot(sumVec, Vector<float>.One);
			}
			for (; j < cols; j++)
			{
				sum += matrix[i * cols + j] * vector[vectorOffset + j];
			}
			result[resultOffset + i] = sum;
		}
	}

	private static void VectorAdd(float[] result, float[] bias, int length)
	{
		int vectorWidth = Vector<float>.Count;
		int i;
		for (i = 0; i <= length - vectorWidth; i += vectorWidth)
		{
			Vector<float> vector = new Vector<float>(result, i);
			Vector<float> b = new Vector<float>(bias, i);
			(vector + b).CopyTo(result, i);
		}
		for (; i < length; i++)
		{
			result[i] += bias[i];
		}
	}

	private static void VectorAdd(float[] result, int offset, float[] bias, int length)
	{
		int vectorWidth = Vector<float>.Count;
		int i;
		for (i = 0; i <= length - vectorWidth; i += vectorWidth)
		{
			Vector<float> vector = new Vector<float>(result, offset + i);
			Vector<float> b = new Vector<float>(bias, i);
			(vector + b).CopyTo(result, offset + i);
		}
		for (; i < length; i++)
		{
			result[offset + i] += bias[i];
		}
	}

	private static float DotProduct(float[] a, int offsetA, float[] b, int offsetB, int length)
	{
		int vectorWidth = Vector<float>.Count;
		Vector<float> sumVec = Vector<float>.Zero;
		int i;
		for (i = 0; i <= length - vectorWidth; i += vectorWidth)
		{
			Vector<float> va = new Vector<float>(a, offsetA + i);
			Vector<float> vb = new Vector<float>(b, offsetB + i);
			sumVec += va * vb;
		}
		float sum = Vector.Dot(sumVec, Vector<float>.One);
		for (; i < length; i++)
		{
			sum += a[offsetA + i] * b[offsetB + i];
		}
		return sum;
	}

	private static void VectorScalarAdd(float[] result, int resultOffset, float[] source, int sourceOffset, float scalar, int length)
	{
		int vectorWidth = Vector<float>.Count;
		Vector<float> scalarVec = new Vector<float>(scalar);
		int i;
		for (i = 0; i <= length - vectorWidth; i += vectorWidth)
		{
			Vector<float> vector = new Vector<float>(result, resultOffset + i);
			Vector<float> s = new Vector<float>(source, sourceOffset + i);
			(vector + s * scalarVec).CopyTo(result, resultOffset + i);
		}
		for (; i < length; i++)
		{
			result[resultOffset + i] += source[sourceOffset + i] * scalar;
		}
	}

	private static void ReLU(float[] data, int length)
	{
		int vectorWidth = Vector<float>.Count;
		Vector<float> zeroVec = Vector<float>.Zero;
		int i;
		for (i = 0; i <= length - vectorWidth; i += vectorWidth)
		{
			Vector.Max(new Vector<float>(data, i), zeroVec).CopyTo(data, i);
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
		float maxVal = data[offset];
		for (int i = 1; i < length; i++)
		{
			if (data[offset + i] > maxVal)
			{
				maxVal = data[offset + i];
			}
		}
		float invTemp = 1f / _attentionTemperature;
		float sum = 0f;
		for (int j = 0; j < length; j++)
		{
			data[offset + j] = (float)Math.Exp((data[offset + j] - maxVal) * invTemp);
			sum += data[offset + j];
		}
		float invSum = 1f / sum;
		for (int k = 0; k < length; k++)
		{
			data[offset + k] *= invSum;
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
		float[] currentWeights = GetAllWeights();
		for (int i = 0; i < currentWeights.Length; i++)
		{
			currentWeights[i] += learningRate * gradients[i];
		}
		SetAllWeights(currentWeights);
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

internal static class RandomExtensions
{
	public static double NextGaussian(this Random random)
	{
		double u1 = 1.0 - random.NextDouble();
		double u2 = 1.0 - random.NextDouble();
		return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(Math.PI * 2.0 * u2);
	}
}
}
