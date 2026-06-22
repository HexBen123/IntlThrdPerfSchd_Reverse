using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using SimdLibrary;

namespace IntlThrdPerfSchd;

public class TransformerScheduler
{
	private static readonly Random _random = new Random(42);

	private const int D_MODEL = 64;

	private const int N_HEAD = 8;

	private const int D_FF = 128;

	private const int MAX_CORES = 64;

	private const int THREAD_RAW_DIM = 7;

	private const int CORE_RAW_DIM = 8;

	private const int CORE_ID_EMBED_DIM = 8;

	private const int THREAD_INPUT_DIM = 8;

	private const int CORE_INPUT_DIM = 9;

	private const int THREAD_FEATURE_DIM = 15;

	private const int CORE_FEATURE_DIM = 16;

	private const int HISTORY_CAPACITY = 10000;

	private const long WINDOW_TICKS = 600000000L;

	private const int NORMALIZATION_WINDOW_SIZE = 1000;

	private float ENERGY_WEIGHT;

	private float LOAD_BALANCE_WEIGHT = 0.3f;

	private const float LOAD_BALANCE_DECAY = 1.5f;

	private readonly LinearLayer _threadEmbedding;

	private readonly LinearLayer _coreEmbedding;

	private readonly ThreadTransformerEncoder _threadEncoder;

	private readonly CoreTransformerEncoder _coreEncoder;

	private readonly MultiHeadAttention _crossAttention;

	private readonly LinearLayer _scoreHead;

	private readonly float[] _threadEmbed;

	private readonly float[] _threadEncoded;

	private readonly float[] _crossOutput;

	private readonly float[] _coreScore;

	private readonly float[] _attentionWeights;

	private readonly float[][] _coreEmbeddings;

	private readonly float[][] _coreEncoded;

	private readonly int[] _coreFeatureHash;

	private readonly bool[] _coreCacheValid;

	private readonly CircularBuffer<DecisionRecord> _history;

	private readonly SchedulerStatistics _stats;

	private DecisionRecord _currentRecord;

	private readonly float[] _threadFeatureMean;

	private readonly float[] _threadFeatureStd;

	private readonly float[] _coreFeatureMean;

	private readonly float[] _coreFeatureStd;

	private readonly float[][] _threadFeatureWindow;

	private readonly float[][] _coreFeatureWindow;

	private int _windowIndex;

	private long _windowStartTick;

	private bool _normalizationReady;

	private readonly float[] _tatHistory;

	private int _tatIndex;

	private float _sumTAT;

	private const int TAT_HISTORY_SIZE = 1000;

	private readonly float[] _gradScoreHead;

	private readonly float[] _gradCrossOutput;

	private readonly float[] _gradThreadEncoded;

	private readonly float[] _gradThreadEmbed;

	private readonly float[] _coreIdEmbedding;

	private readonly float[] _normThreadBuf;

	private readonly float[] _normCoreBuf;

	private readonly float[] _normCoreBwdBuf;

	private readonly float[] _gradCoreEncodedBuf;

	private readonly float[] _scoreResultBuf;

	private float _lastTAT;

	private float _baselineTAT;

	private float _baselineEnergy;

	private float _baselineLoadBalance;

	private const float LEARNING_RATE = 0.0001f;

	private int _decisionsInCurrentSecond;

	private long _lastTATUpdateTick;

	private float _explorationRate;

	private float _minExplorationRate;

	private long _startTick;

	private const long INITIAL_LEARNING_PHASE_MS = 600000L;

	private const long RAPID_LEARNING_PHASE_MS = 120000L;

	private const float RAPID_LEARNING_RATE = 0.01f;

	private const float INITIAL_LEARNING_RATE = 0.01f;

	private const float MIN_LEARNING_RATE = 0.0001f;

	private const float MAX_LEARNING_RATE = 0.01f;

	private float _currentLearningRate = 0.01f;

	private bool _learningEnabled = true;

	private const long LEARNING_RATE_DECAY_DURATION = 36000000000L;

	private const float INITIAL_EXPLORATION_RATE = 0f;

	private const long BATCH_TRAIN_INTERVAL = 600000000L;

	private long _lastBatchTrainTick;

	private const int BATCH_SAMPLE_SIZE = 1000;

	private readonly float[] _learningWeights;

	private const int MAX_DECISIONS_PER_SECOND = 10000;

	private int _topK = 3;

	private readonly int[] _reportMaxCores;

	private readonly float[] _reportMaxWeights;

	private readonly float[] _lastAttentionWeightsBuffer;

	private const string MODEL_MAGIC = "TSC1";

	private const int MODEL_VERSION = 3;

	private const string DEFAULT_MODEL_PATH = "./scheduler_model.bin";

	public SchedulerStatistics Statistics => _stats;

	public void SetTopK(int k)
	{
		_topK = Math.Max(1, k);
	}

	public int GetTopK()
	{
		return _topK;
	}

	public void SetLearningEnabled(bool enabled)
	{
		_learningEnabled = enabled;
	}

	public bool GetLearningEnabled()
	{
		return _learningEnabled;
	}

	public TransformerScheduler()
	{
		_threadEmbedding = new LinearLayer(15, 64);
		_coreEmbedding = new LinearLayer(16, 64);
		_threadEncoder = new ThreadTransformerEncoder();
		_coreEncoder = new CoreTransformerEncoder();
		_crossAttention = new MultiHeadAttention(64, 8);
		_scoreHead = new LinearLayer(64, 1);
		_threadEmbed = new float[64];
		_threadEncoded = new float[64];
		_crossOutput = new float[64];
		_coreScore = new float[64];
		_attentionWeights = new float[64];
		_coreEmbeddings = new float[64][];
		_coreEncoded = new float[64][];
		_coreFeatureHash = new int[64];
		_coreCacheValid = new bool[64];
		for (int i = 0; i < 64; i++)
		{
			_coreEmbeddings[i] = new float[64];
			_coreEncoded[i] = new float[64];
			_coreCacheValid[i] = false;
		}
		_history = new CircularBuffer<DecisionRecord>(10000);
		_stats = new SchedulerStatistics(64);
		_currentRecord = new DecisionRecord(64);
		_threadFeatureMean = new float[7];
		_threadFeatureStd = new float[7];
		_coreFeatureMean = new float[8];
		_coreFeatureStd = new float[8];
		_threadFeatureWindow = new float[1000][];
		_coreFeatureWindow = new float[1000][];
		for (int j = 0; j < 1000; j++)
		{
			_threadFeatureWindow[j] = new float[8];
			_coreFeatureWindow[j] = new float[9];
		}
		_windowIndex = 0;
		_windowStartTick = DateTime.Now.Ticks;
		_normalizationReady = false;
		for (int k = 0; k < 7; k++)
		{
			_threadFeatureStd[k] = 1f;
		}
		for (int l = 0; l < 8; l++)
		{
			_coreFeatureStd[l] = 1f;
		}
		_coreIdEmbedding = new float[512];
		float num = (float)Math.Sqrt(0.25);
		for (int m = 0; m < _coreIdEmbedding.Length; m++)
		{
			_coreIdEmbedding[m] = (float)(_random.NextDouble() * 2.0 - 1.0) * num;
		}
		_tatHistory = new float[1000];
		_tatIndex = 0;
		_sumTAT = 0f;
		_lastTAT = 0f;
		_baselineTAT = 0f;
		_baselineEnergy = 0f;
		_startTick = DateTime.Now.Ticks;
		_lastBatchTrainTick = _startTick;
		_gradScoreHead = new float[1];
		_gradCrossOutput = new float[64];
		_gradThreadEncoded = new float[64];
		_gradThreadEmbed = new float[64];
		_learningWeights = new float[10000];
		_reportMaxCores = new int[8];
		_reportMaxWeights = new float[8];
		_lastAttentionWeightsBuffer = new float[64];
		_normThreadBuf = new float[15];
		_normCoreBuf = new float[16];
		_normCoreBwdBuf = new float[16];
		_gradCoreEncodedBuf = new float[64];
		_scoreResultBuf = new float[1];
	}

	public TransformerScheduler(string modelPath)
	{
		_threadEmbedding = new LinearLayer(15, 64);
		_coreEmbedding = new LinearLayer(16, 64);
		_threadEncoder = new ThreadTransformerEncoder();
		_coreEncoder = new CoreTransformerEncoder();
		_crossAttention = new MultiHeadAttention(64, 8);
		_scoreHead = new LinearLayer(64, 1);
		_threadEmbed = new float[64];
		_threadEncoded = new float[64];
		_crossOutput = new float[64];
		_coreScore = new float[64];
		_attentionWeights = new float[64];
		_coreEmbeddings = new float[64][];
		_coreEncoded = new float[64][];
		_coreFeatureHash = new int[64];
		_coreCacheValid = new bool[64];
		for (int i = 0; i < 64; i++)
		{
			_coreEmbeddings[i] = new float[64];
			_coreEncoded[i] = new float[64];
			_coreCacheValid[i] = false;
		}
		_history = new CircularBuffer<DecisionRecord>(10000);
		_stats = new SchedulerStatistics(64);
		_currentRecord = new DecisionRecord(64);
		_threadFeatureMean = new float[7];
		_threadFeatureStd = new float[7];
		_coreFeatureMean = new float[8];
		_coreFeatureStd = new float[8];
		_threadFeatureWindow = new float[1000][];
		_coreFeatureWindow = new float[1000][];
		for (int j = 0; j < 1000; j++)
		{
			_threadFeatureWindow[j] = new float[8];
			_coreFeatureWindow[j] = new float[9];
		}
		_windowIndex = 0;
		_windowStartTick = DateTime.Now.Ticks;
		_normalizationReady = false;
		for (int k = 0; k < 7; k++)
		{
			_threadFeatureStd[k] = 1f;
		}
		for (int l = 0; l < 8; l++)
		{
			_coreFeatureStd[l] = 1f;
		}
		_coreIdEmbedding = new float[512];
		float num = (float)Math.Sqrt(0.25);
		for (int m = 0; m < _coreIdEmbedding.Length; m++)
		{
			_coreIdEmbedding[m] = (float)(_random.NextDouble() * 2.0 - 1.0) * num;
		}
		_tatHistory = new float[1000];
		_tatIndex = 0;
		_sumTAT = 0f;
		_lastTAT = 0f;
		_baselineTAT = 0f;
		_baselineEnergy = 0f;
		_startTick = DateTime.Now.Ticks;
		_lastBatchTrainTick = _startTick;
		_gradScoreHead = new float[1];
		_gradCrossOutput = new float[64];
		_gradThreadEncoded = new float[64];
		_gradThreadEmbed = new float[64];
		_learningWeights = new float[10000];
		_reportMaxCores = new int[8];
		_reportMaxWeights = new float[8];
		_lastAttentionWeightsBuffer = new float[64];
		_normThreadBuf = new float[15];
		_normCoreBuf = new float[16];
		_normCoreBwdBuf = new float[16];
		_gradCoreEncodedBuf = new float[64];
		_scoreResultBuf = new float[1];
		if (!string.IsNullOrEmpty(modelPath) && File.Exists(modelPath))
		{
			LoadModel(modelPath);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int ComputeFeatureHash(ReadOnlySpan<float> features)
	{
		int num = 0;
		for (int i = 0; i < features.Length; i++)
		{
			num = num * 31 + features[i].GetHashCode();
		}
		return num;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void CopyCoreIdEmbedding(int coreId, Span<float> destination)
	{
		int num = coreId * 8;
		for (int i = 0; i < 8; i++)
		{
			destination[i] = _coreIdEmbedding[num + i];
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void BuildThreadInput(ReadOnlySpan<float> threadFeatures, Span<float> output)
	{
		NormalizeFeatures(threadFeatures.Slice(0, 7), output.Slice(0, 7), _threadFeatureMean, _threadFeatureStd);
		int coreId = (int)threadFeatures[7];
		CopyCoreIdEmbedding(coreId, output.Slice(7));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void BuildCoreInput(ReadOnlySpan<float> coreFeatures, Span<float> output)
	{
		NormalizeFeatures(coreFeatures.Slice(0, 8), output.Slice(0, 8), _coreFeatureMean, _coreFeatureStd);
		int coreId = (int)coreFeatures[8];
		CopyCoreIdEmbedding(coreId, output.Slice(8));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void NormalizeFeatures(ReadOnlySpan<float> input, Span<float> output, ReadOnlySpan<float> mean, ReadOnlySpan<float> std)
	{
		int num = Math.Min(input.Length, Math.Min(output.Length, Math.Min(mean.Length, std.Length)));
		for (int i = 0; i < num; i++)
		{
			float num2 = ((std[i] > 0.001f) ? std[i] : 1f);
			output[i] = (input[i] - mean[i]) / num2;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void UpdateNormalizationFixedWindow(ReadOnlySpan<float> threadFeatures, float[][] coreFeatures, int numCores)
	{
		long ticks = DateTime.Now.Ticks;
		if (ticks - _windowStartTick >= 600000000)
		{
			ComputeWindowStatistics();
			_windowStartTick = ticks;
			_windowIndex = 0;
			_normalizationReady = true;
		}
		if (_windowIndex >= 1000)
		{
			return;
		}
		threadFeatures.Slice(0, 8).CopyTo(_threadFeatureWindow[_windowIndex]);
		if (numCores > 0)
		{
			for (int i = 0; i < 9; i++)
			{
				float num = 0f;
				for (int j = 0; j < numCores; j++)
				{
					num += coreFeatures[j][i];
				}
				_coreFeatureWindow[_windowIndex][i] = num / (float)numCores;
			}
		}
		_windowIndex++;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void ComputeWindowStatistics()
	{
		if (_windowIndex == 0)
		{
			return;
		}
		MathHelper.Clear(_threadFeatureMean.AsSpan(0, 7));
		MathHelper.Clear(_coreFeatureMean.AsSpan(0, 8));
		MathHelper.Clear(_threadFeatureStd.AsSpan(0, 7));
		MathHelper.Clear(_coreFeatureStd.AsSpan(0, 8));
		for (int i = 0; i < _windowIndex; i++)
		{
			for (int j = 0; j < 7; j++)
			{
				_threadFeatureMean[j] += _threadFeatureWindow[i][j];
			}
			for (int k = 0; k < 8; k++)
			{
				_coreFeatureMean[k] += _coreFeatureWindow[i][k];
			}
		}
		float num = 1f / (float)_windowIndex;
		for (int l = 0; l < 7; l++)
		{
			_threadFeatureMean[l] *= num;
		}
		for (int m = 0; m < 8; m++)
		{
			_coreFeatureMean[m] *= num;
		}
		for (int n = 0; n < _windowIndex; n++)
		{
			for (int num2 = 0; num2 < 7; num2++)
			{
				float num3 = _threadFeatureWindow[n][num2] - _threadFeatureMean[num2];
				_threadFeatureStd[num2] += num3 * num3;
			}
			for (int num4 = 0; num4 < 8; num4++)
			{
				float num5 = _coreFeatureWindow[n][num4] - _coreFeatureMean[num4];
				_coreFeatureStd[num4] += num5 * num5;
			}
		}
		for (int num6 = 0; num6 < 7; num6++)
		{
			_threadFeatureStd[num6] = (float)Math.Sqrt(_threadFeatureStd[num6] / (float)_windowIndex + 1E-06f);
		}
		for (int num7 = 0; num7 < 8; num7++)
		{
			_coreFeatureStd[num7] = (float)Math.Sqrt(_coreFeatureStd[num7] / (float)_windowIndex + 1E-06f);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void UpdateCoreEmbedding(ReadOnlySpan<float> coreFeatures, int coreIndex, Span<float> normalized, Span<float> encodedOutput)
	{
		BuildCoreInput(coreFeatures, normalized);
		_coreEmbedding.Forward(normalized, _coreEmbeddings[coreIndex]);
		_coreEncoder.Forward(_coreEmbeddings[coreIndex], encodedOutput);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int Schedule(ReadOnlySpan<float> threadFeatures, float[][] coreFeatures, int numCores)
	{
		return Schedule(threadFeatures, coreFeatures, numCores, -1, -1, -1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int Schedule(ReadOnlySpan<float> threadFeatures, float[][] coreFeatures, int numCores, int predictedCore, int actualCore)
	{
		return Schedule(threadFeatures, coreFeatures, numCores, -1, predictedCore, actualCore);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int Schedule(ReadOnlySpan<float> threadFeatures, float[][] coreFeatures, int numCores, int threadId, int predictedCore, int actualCore)
	{
		if (numCores <= 0 || numCores > 64)
		{
			throw new ArgumentOutOfRangeException("numCores", $"numCores={numCores} must be in range [1, {64}]");
		}
		if (coreFeatures == null || coreFeatures.Length < numCores)
		{
			throw new ArgumentOutOfRangeException("coreFeatures", $"coreFeatures.Length={((coreFeatures != null) ? coreFeatures.Length : (-1))} < numCores={numCores}");
		}
		for (int i = 0; i < numCores; i++)
		{
			if (coreFeatures[i] == null || coreFeatures[i].Length < 9)
			{
				object arg = i;
				float[] obj = coreFeatures[i];
				throw new ArgumentOutOfRangeException("coreFeatures", $"coreFeatures[{arg}].Length={((obj != null) ? obj.Length : (-1))} < CORE_INPUT_DIM={9}");
			}
		}
		if (threadFeatures.Length < 8)
		{
			throw new ArgumentOutOfRangeException("threadFeatures", $"threadFeatures.Length={threadFeatures.Length} < THREAD_INPUT_DIM={8}");
		}
		Stopwatch stopwatch = Stopwatch.StartNew();
		UpdateNormalizationFixedWindow(threadFeatures, coreFeatures, numCores);
		BuildThreadInput(threadFeatures, _normThreadBuf);
		_threadEmbedding.Forward(_normThreadBuf, _threadEmbed);
		_threadEncoder.Forward(_threadEmbed, _threadEncoded);
		for (int j = 0; j < numCores; j++)
		{
			ReadOnlySpan<float> readOnlySpan = new ReadOnlySpan<float>(coreFeatures[j], 0, 9);
			int num = ComputeFeatureHash(readOnlySpan);
			if (_coreFeatureHash[j] != num || !_coreCacheValid[j])
			{
				BuildCoreInput(readOnlySpan, _normCoreBuf);
				_coreEmbedding.Forward(_normCoreBuf, _coreEmbeddings[j]);
				_coreEncoder.Forward(_coreEmbeddings[j], _coreEncoded[j].AsSpan());
				_coreFeatureHash[j] = num;
				_coreCacheValid[j] = true;
				_stats.CacheMisses++;
			}
			else
			{
				_stats.CacheHits++;
			}
		}
		Span<float> attentionWeights = _attentionWeights.AsSpan(0, numCores);
		_crossAttention.CrossAttention(_threadEncoded, _coreEncoded, _coreEncoded, _crossOutput, numCores, attentionWeights);
		Span<float> span = _coreScore.AsSpan(0, numCores);
		attentionWeights.CopyTo(span);
		float num2 = VectorMathNew.Sum(span);
		int num3 = SelectTopKRandom(span, _topK);
		if (num2 > 0f)
		{
			MathHelper.Scale(span, 1f / num2);
		}
		long num4 = (DateTime.Now.Ticks - _startTick) / 10000 / 60000;
		_explorationRate = 0f;
		_ = _stats.PositiveRewardRatio;
		float currentLearningRate = ((num4 < 6) ? 0.01f : ((num4 >= 12) ? 0.0001f : 0.001f));
		_currentLearningRate = currentLearningRate;
		if (_random.NextDouble() < (double)_explorationRate)
		{
			num3 = _random.Next(numCores);
		}
		long ticks = DateTime.Now.Ticks;
		if (ticks - _lastTATUpdateTick >= 10000000)
		{
			_decisionsInCurrentSecond = 0;
			_lastTATUpdateTick = ticks;
		}
		_decisionsInCurrentSecond++;
		RecordDecision(threadFeatures, coreFeatures, numCores, num3, predictedCore, actualCore);
		stopwatch.Stop();
		_stats.TotalDecisions++;
		_stats.TotalInferenceTimeUs += stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency;
		_stats.CoreSelectionCounts[num3]++;
		return num3;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void UpdateTAT(float currentTAT, float energyValue)
	{
		UpdateTATInternal(currentTAT, energyValue);
	}

	private void UpdateTATInternal(float currentTAT, float energyValue)
	{
		currentTAT /= 1000f;
		if (currentTAT < 0.01f || currentTAT > 100000f)
		{
			return;
		}
		long num = (DateTime.Now.Ticks - _startTick) / 10000;
		float num2 = 0f;
		if (_stats.TotalTATSamples > 0)
		{
			num2 = currentTAT - _lastTAT;
			_stats.RecordTATDelta(num2);
		}
		_tatHistory[_tatIndex] = currentTAT;
		_sumTAT += currentTAT;
		_tatIndex = (_tatIndex + 1) % 1000;
		if (_stats.TotalTATSamples < 1000)
		{
			_stats.TotalTATSamples++;
		}
		else
		{
			_sumTAT -= _tatHistory[_tatIndex];
		}
		_lastTAT = currentTAT;
		_stats.AvgTAT = _sumTAT / (float)_stats.TotalTATSamples;
		if (_stats.TotalTATSamples == 1 || currentTAT < _stats.MinTAT)
		{
			_stats.MinTAT = currentTAT;
		}
		if (_stats.TotalTATSamples == 1 || currentTAT > _stats.MaxTAT)
		{
			_stats.MaxTAT = currentTAT;
		}
		if (_stats.TotalTATSamples >= 10)
		{
			if (_baselineTAT < 0.001f)
			{
				_baselineTAT = _stats.AvgTAT;
			}
			else
			{
				_baselineTAT = _baselineTAT * 0.95f + currentTAT * 0.05f;
			}
			_stats.UpdateBaselineTAT(_baselineTAT);
		}
		float num3 = 0f;
		if (_baselineTAT > 0.001f)
		{
			num3 = (_baselineTAT - currentTAT) / _baselineTAT;
			num3 = ((num3 > 1f) ? 1f : ((num3 < -1f) ? (-1f) : num3));
		}
		if (_stats.TotalTATSamples >= 10)
		{
			if (_baselineEnergy < 0.001f)
			{
				_baselineEnergy = energyValue;
			}
			else
			{
				_baselineEnergy = _baselineEnergy * 0.95f + energyValue * 0.05f;
			}
		}
		float num4 = 0f;
		if (_baselineEnergy > 0.001f)
		{
			num4 = (energyValue - _baselineEnergy) / _baselineEnergy;
			num4 = ((num4 > 1f) ? 1f : ((num4 < -1f) ? (-1f) : num4));
		}
		if (LOAD_BALANCE_WEIGHT > 0f && _currentRecord.NumCores > 0)
		{
			float num5 = ComputeLoadBalance(_currentRecord.CoreFeatures, _currentRecord.NumCores);
			if (_stats.TotalTATSamples >= 10)
			{
				if (_baselineLoadBalance < 0.001f)
				{
					_baselineLoadBalance = num5;
				}
				else
				{
					_baselineLoadBalance = _baselineLoadBalance * 0.95f + num5 * 0.05f;
				}
			}
		}
		float num6 = 0f;
		if (LOAD_BALANCE_WEIGHT > 0f && _currentRecord.NumCores > 0)
		{
			float num7 = ComputeLoadBalance(_currentRecord.CoreFeatures, _currentRecord.NumCores);
			if (_baselineLoadBalance > 0.001f)
			{
				float num8 = (num7 - _baselineLoadBalance) / _baselineLoadBalance;
				num6 = ((!(num8 > 0f)) ? num8 : (0f - (float)Math.Exp(1.5f * num8)));
				num6 = Math.Max(num6, -10f);
			}
		}
		long num9 = (DateTime.Now.Ticks - _startTick) / 600000000;
		if (num9 >= 0 && num9 < 6)
		{
			LOAD_BALANCE_WEIGHT = 0f;
			ENERGY_WEIGHT = 0f;
		}
		else if (num9 >= 6 && num9 < 12)
		{
			LOAD_BALANCE_WEIGHT = 0f;
			ENERGY_WEIGHT = 0.4f;
		}
		else
		{
			LOAD_BALANCE_WEIGHT = 0.3f;
			ENERGY_WEIGHT = 0.3f;
		}
		float num10 = num3 * (1f - ENERGY_WEIGHT - LOAD_BALANCE_WEIGHT) + num4 * ENERGY_WEIGHT + num6 * LOAD_BALANCE_WEIGHT;
		_stats.RecordReward(num10);
		_stats.RecentAvgReward = _stats.RecentAvgReward * 0.95f + num10 * 0.05f;
		_stats.LastEnergy = energyValue;
		_stats.TotalEnergySamples++;
		_stats.AvgEnergy = (_stats.AvgEnergy * (float)(_stats.TotalEnergySamples - 1) + energyValue) / (float)_stats.TotalEnergySamples;
		if (_stats.TotalEnergySamples == 1 || energyValue < _stats.MinEnergy)
		{
			_stats.MinEnergy = energyValue;
		}
		if (_stats.TotalEnergySamples == 1 || energyValue > _stats.MaxEnergy)
		{
			_stats.MaxEnergy = energyValue;
		}
		_stats.BaselineEnergy = _baselineEnergy;
		_stats.RecordEnergy(energyValue);
		bool flag = num < 600000;
		if ((flag || !(currentTAT < 1f)) && !flag)
		{
			_ = 2f;
		}
		int num11 = Math.Min(_decisionsInCurrentSecond, _history.Count);
		int num12 = _history.Count - num11;
		float currentLearningRate = _currentLearningRate;
		float num13 = 0f;
		int num14 = Math.Min(num11, 10000);
		for (int i = 0; i < num14; i++)
		{
			_learningWeights[i] = (float)Math.Exp(-0.5f * (float)(num14 - 1 - i));
			num13 += _learningWeights[i];
		}
		float num15 = 0f;
		if (!_learningEnabled)
		{
			_stats.ExperienceSkipped += num14;
			return;
		}
		for (int j = 0; j < num14; j++)
		{
			int num16 = num12 + j;
			if (num16 < 0 || num16 >= _history.Count)
			{
				continue;
			}
			DecisionRecord decisionRecord = _history.Get(num16);
			if (!flag && !decisionRecord.IsValid)
			{
				_stats.ExperienceSkipped++;
				continue;
			}
			float num17 = num3 * (_learningWeights[j] / num13);
			num15 += Math.Abs(num17);
			BuildThreadInput(decisionRecord.ThreadFeatures, _normThreadBuf);
			_threadEmbedding.Forward(_normThreadBuf, _threadEmbed);
			_threadEncoder.Forward(_threadEmbed, _threadEncoded);
			for (int k = 0; k < decisionRecord.NumCores; k++)
			{
				ReadOnlySpan<float> coreFeatures = new ReadOnlySpan<float>(decisionRecord.CoreFeatures[k], 0, 9);
				BuildCoreInput(coreFeatures, _normCoreBuf);
				_coreEmbedding.Forward(_normCoreBuf, _coreEmbeddings[k]);
				_coreEncoder.Forward(_coreEmbeddings[k], _coreEncoded[k]);
			}
			Span<float> attentionWeights = _attentionWeights.AsSpan(0, decisionRecord.NumCores);
			_crossAttention.CrossAttention(_threadEncoded, _coreEncoded, _coreEncoded, _crossOutput, decisionRecord.NumCores, attentionWeights);
			_scoreHead.Forward(_crossOutput, _scoreResultBuf);
			_gradScoreHead[0] = num17;
			_scoreHead.Backward(_gradScoreHead, currentLearningRate);
			float[] inputGrads = _scoreHead.InputGrads;
			_crossAttention.Backward(inputGrads, currentLearningRate);
			float[] queryGradients = _crossAttention.GetQueryGradients();
			_threadEncoder.Backward(queryGradients, currentLearningRate);
			float[] inputGradients = _threadEncoder.InputGradients;
			_threadEmbedding.Backward(inputGradients, currentLearningRate);
			float[] valueGradients = _crossAttention.GetValueGradients();
			float[] keyGradients = _crossAttention.GetKeyGradients();
			int cachedNumCores = _crossAttention.GetCachedNumCores();
			bool flag2 = true;
			for (int l = 0; l < cachedNumCores; l++)
			{
				for (int m = 0; m < 64; m++)
				{
					_gradCoreEncodedBuf[m] = keyGradients[l * 64 + m] + valueGradients[l * 64 + m];
				}
				ReadOnlySpan<float> coreFeatures2 = new ReadOnlySpan<float>(decisionRecord.CoreFeatures[l], 0, 9);
				BuildCoreInput(coreFeatures2, _normCoreBwdBuf);
				_coreEmbedding.Forward(_normCoreBwdBuf, _coreEmbeddings[l]);
				_coreEncoder.Forward(_coreEmbeddings[l], _coreEncoded[l]);
				_coreEncoder.Backward(_gradCoreEncodedBuf, currentLearningRate);
				_coreEncoder.ApplyGradients(currentLearningRate);
				float[] inputGradients2 = _coreEncoder.InputGradients;
				if (flag2)
				{
					_coreEmbedding.Backward(inputGradients2, currentLearningRate);
					flag2 = false;
				}
				else
				{
					_coreEmbedding.Backward(inputGradients2, currentLearningRate, accumulateGrad: true);
				}
			}
			_scoreHead.ApplyGradientsSGD(currentLearningRate);
			_threadEmbedding.ApplyGradientsSGD(currentLearningRate);
			_coreEmbedding.ApplyGradientsSGD(currentLearningRate);
			_crossAttention.ApplyGradients();
			_threadEncoder.ApplyGradients(currentLearningRate);
			_coreEncoder.ApplyGradients(currentLearningRate);
			_stats.ExperienceUsed++;
			_stats.LearningUpdates++;
		}
		_stats.AvgLoss = _stats.AvgLoss * 0.9f + num15 / (float)Math.Max(num14, 1) * 0.1f;
		_decisionsInCurrentSecond = 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void PerformBatchTraining()
	{
		if (!_learningEnabled || _history.Count < 10)
		{
			return;
		}
		int num = Math.Min(1000, _history.Count);
		float currentLearningRate = _currentLearningRate;
		float num2 = 0f;
		for (int i = 0; i < num; i++)
		{
			int index = _random.Next(_history.Count);
			DecisionRecord decisionRecord = _history.Get(index);
			float reward = decisionRecord.Reward;
			_gradScoreHead[0] = reward;
			_scoreHead.Backward(_gradScoreHead, currentLearningRate);
			float[] inputGrads = _scoreHead.InputGrads;
			_crossAttention.Backward(inputGrads, currentLearningRate);
			float[] queryGradients = _crossAttention.GetQueryGradients();
			_threadEncoder.Backward(queryGradients, currentLearningRate);
			float[] inputGradients = _threadEncoder.InputGradients;
			_threadEmbedding.Backward(inputGradients, currentLearningRate);
			float[] valueGradients = _crossAttention.GetValueGradients();
			float[] keyGradients = _crossAttention.GetKeyGradients();
			int cachedNumCores = _crossAttention.GetCachedNumCores();
			bool flag = true;
			for (int j = 0; j < cachedNumCores; j++)
			{
				for (int k = 0; k < 64; k++)
				{
					_gradCoreEncodedBuf[k] = keyGradients[j * 64 + k] + valueGradients[j * 64 + k];
				}
				ReadOnlySpan<float> coreFeatures = new ReadOnlySpan<float>(decisionRecord.CoreFeatures[j], 0, 9);
				BuildCoreInput(coreFeatures, _normCoreBwdBuf);
				_coreEmbedding.Forward(_normCoreBwdBuf, _coreEmbeddings[j]);
				_coreEncoder.Forward(_coreEmbeddings[j], _coreEncoded[j]);
				_coreEncoder.Backward(_gradCoreEncodedBuf, currentLearningRate);
				_coreEncoder.ApplyGradients(currentLearningRate);
				float[] inputGradients2 = _coreEncoder.InputGradients;
				if (flag)
				{
					_coreEmbedding.Backward(inputGradients2, currentLearningRate);
					flag = false;
				}
				else
				{
					_coreEmbedding.Backward(inputGradients2, currentLearningRate, accumulateGrad: true);
				}
			}
			num2 += Math.Abs(reward);
			_stats.LearningUpdates++;
		}
		_scoreHead.ApplyGradientsSGD(currentLearningRate);
		_threadEmbedding.ApplyGradientsSGD(currentLearningRate);
		_coreEmbedding.ApplyGradientsSGD(currentLearningRate);
		_crossAttention.ApplyGradients();
		_threadEncoder.ApplyGradients(currentLearningRate);
		_coreEncoder.ApplyGradients(currentLearningRate);
		_stats.AvgLoss = _stats.AvgLoss * 0.95f + num2 / (float)num * 0.05f;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void RecordDecision(ReadOnlySpan<float> threadFeatures, float[][] coreFeatures, int numCores, int selectedCore, int predictedCore = -1, int actualCore = -1)
	{
		threadFeatures.Slice(0, 8).CopyTo(_currentRecord.ThreadFeatures);
		_currentRecord.NumCores = numCores;
		for (int i = 0; i < numCores; i++)
		{
			Array.Copy(coreFeatures[i], _currentRecord.CoreFeatures[i], 9);
		}
		_currentRecord.SelectedCore = selectedCore;
		_currentRecord.Timestamp = DateTime.Now.Ticks;
		_currentRecord.PredictedCore = predictedCore;
		_currentRecord.ActualCore = actualCore;
		if (predictedCore >= 0 && actualCore >= 0 && predictedCore != actualCore)
		{
			_currentRecord.IsValid = false;
		}
		else
		{
			_currentRecord.IsValid = true;
		}
		_history.Add(_currentRecord);
	}

	public long GetRuntime()
	{
		return (DateTime.Now.Ticks - _startTick) / 600000000;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int SelectTopKRandom(ReadOnlySpan<float> scores, int topK)
	{
		int length = scores.Length;
		if (length <= 0)
		{
			return 0;
		}
		if (length == 1)
		{
			return 0;
		}
		if (topK >= length)
		{
			topK = length;
		}
		float num = float.MinValue;
		int num2 = topK;
		for (int i = 0; i < num2 && i < length; i++)
		{
			if (scores[i] > num)
			{
				num = scores[i];
			}
		}
		for (int j = num2; j < length; j++)
		{
			if (scores[j] > num)
			{
				num = scores[j];
			}
		}
		int num3 = 0;
		for (int k = 0; k < length; k++)
		{
			if (scores[k] >= num - 1E-06f)
			{
				num3++;
			}
		}
		int num4 = _random.Next(num3);
		for (int l = 0; l < length; l++)
		{
			if (scores[l] >= num - 1E-06f)
			{
				if (num4 == 0)
				{
					return l;
				}
				num4--;
			}
		}
		return 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private float ComputeLoadBalance(float[][] coreFeatures, int numCores)
	{
		if (numCores <= 1)
		{
			return 0f;
		}
		float num = 0f;
		for (int i = 0; i < numCores; i++)
		{
			num += coreFeatures[i][2];
		}
		float num2 = num / (float)numCores;
		float num3 = 0f;
		for (int j = 0; j < numCores; j++)
		{
			float num4 = coreFeatures[j][2] - num2;
			num3 += num4 * num4;
		}
		return (float)Math.Sqrt(num3 / (float)numCores);
	}

	public string GetStatistics(int numcores)
	{
		long num = (DateTime.Now.Ticks - _startTick) / 10000;
		bool flag = num < 600000;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine($"Runtime: {num / 60000} min / {10L} min");
		stringBuilder.AppendLine("Learning Phase: " + (flag ? "Initial (all experiences valid)" : "Stable (skip invalid experiences)"));
		stringBuilder.AppendLine();
		stringBuilder.Append(_stats.GetReport(numcores, _explorationRate));
		return stringBuilder.ToString();
	}

	public string GetRecentDecisions(int n = 10)
	{
		n = Math.Min(n, _history.Count);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine($"=== Recent {n} Decisions ===");
		for (int i = 0; i < n; i++)
		{
			DecisionRecord decisionRecord = _history.Get(n - 1 - i);
			stringBuilder.AppendLine($"[{i}] Core {decisionRecord.SelectedCore} | " + $"Thread IPC: {decisionRecord.ThreadFeatures[1]:F2} | " + $"Priority: {decisionRecord.ThreadFeatures[2]:F2} | " + $"Reward: {decisionRecord.Reward:F6}");
		}
		return stringBuilder.ToString();
	}

	public string GetAttentionHeadReport(int numCores)
	{
		StringBuilder stringBuilder = new StringBuilder();
		float[][] headAttentionWeights = _crossAttention.GetHeadAttentionWeights(numCores);
		stringBuilder.AppendLine("╔" + new string('═', 96) + "╗");
		stringBuilder.Append("║ Core   │");
		for (int i = 0; i < 8; i++)
		{
			stringBuilder.Append($"  H{i}    │");
		}
		stringBuilder.AppendLine("  Avg   ║");
		stringBuilder.Append("╟" + new string('─', 8) + "┼");
		for (int j = 0; j < 8; j++)
		{
			stringBuilder.Append(new string('─', 10) + "┼");
		}
		stringBuilder.AppendLine(new string('─', 8) + "╢");
		for (int k = 0; k < 8; k++)
		{
			_reportMaxCores[k] = 0;
			_reportMaxWeights[k] = headAttentionWeights[k][0];
			for (int l = 1; l < numCores; l++)
			{
				if (headAttentionWeights[k][l] > _reportMaxWeights[k])
				{
					_reportMaxWeights[k] = headAttentionWeights[k][l];
					_reportMaxCores[k] = l;
				}
			}
		}
		for (int m = 0; m < numCores; m++)
		{
			stringBuilder.Append($"║ {m,-5} │");
			for (int n = 0; n < 8; n++)
			{
				string arg = ((m == _reportMaxCores[n]) ? $"*{headAttentionWeights[n][m]:F4}" : $" {headAttentionWeights[n][m]:F4}");
				stringBuilder.Append($"{arg,-8}│");
			}
			stringBuilder.AppendLine($" {_attentionWeights[m]:F4} ║");
		}
		stringBuilder.Append("╚" + new string('═', 8) + "╧");
		for (int num = 0; num < 8; num++)
		{
			stringBuilder.Append(new string('═', 10) + "╧");
		}
		stringBuilder.AppendLine(new string('═', 8) + "╝");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("Head Summary:");
		for (int num2 = 0; num2 < 8; num2++)
		{
			float num3 = 0f;
			for (int num4 = 0; num4 < numCores; num4++)
			{
				num3 += headAttentionWeights[num2][num4];
			}
			stringBuilder.AppendLine($"  H{num2}: Max→Core{_reportMaxCores[num2]} ({_reportMaxWeights[num2]:F4}), Avg={num3 / (float)numCores:F4}");
		}
		return stringBuilder.ToString();
	}

	public float[] GetLastAttentionWeights(int numCores)
	{
		for (int i = 0; i < numCores; i++)
		{
			_lastAttentionWeightsBuffer[i] = _attentionWeights[i];
		}
		return _lastAttentionWeightsBuffer;
	}

	public void SetNormalizationParams(float[] threadMean, float[] threadStd, float[] coreMean, float[] coreStd)
	{
		Array.Copy(threadMean, _threadFeatureMean, 7);
		Array.Copy(threadStd, _threadFeatureStd, 7);
		Array.Copy(coreMean, _coreFeatureMean, 8);
		Array.Copy(coreStd, _coreFeatureStd, 8);
		_normalizationReady = true;
	}

	public string ExportModel()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine($"Model Config: d_model={64}, n_head={8}, d_ff={128}");
		stringBuilder.AppendLine($"Thread Embedding: {15} -> {64}");
		stringBuilder.AppendLine($"Core Embedding: {16} -> {64}");
		stringBuilder.AppendLine($"Total Params: {CountParameters()}");
		return stringBuilder.ToString();
	}

	private int CountParameters()
	{
		return 0 + (_threadEmbedding.Weights.Length + _threadEmbedding.Bias.Length) + (_coreEmbedding.Weights.Length + _coreEmbedding.Bias.Length) + (_scoreHead.Weights.Length + _scoreHead.Bias.Length) + _coreIdEmbedding.Length;
	}

	public float GetCurrentTAT()
	{
		return _lastTAT;
	}

	public float GetBaselineTAT()
	{
		return _baselineTAT;
	}

	public float GetRecentAvgReward()
	{
		return _stats.RecentAvgReward;
	}

	public float GetExplorationRate()
	{
		return _explorationRate;
	}

	public float GetLearningRate()
	{
		return _currentLearningRate;
	}

	public void SetExplorationRate(float rate)
	{
		_explorationRate = Math.Max(rate, _minExplorationRate);
	}

	public void SetMinExplorationRate(float rate)
	{
		_minExplorationRate = Math.Max(rate, 0.001f);
	}

	public void SetExplorationDecayMinutes(int minutes)
	{
	}

	public string GetLearningReport()
	{
		_stats.ComputeTrends();
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("=== Neural Network Learning Report ===");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("--- Reward Metrics ---");
		stringBuilder.AppendLine($"Last Reward: {_stats.LastReward:F6}");
		stringBuilder.AppendLine($"Recent Avg Reward: {_stats.RecentAvgReward:F6}");
		stringBuilder.AppendLine(string.Format("Reward Trend: {0:F6} ({1}{2:F2}%/step)", _stats.RewardTrend, (_stats.RewardTrend > 0f) ? "+" : "", _stats.RewardTrend * 100f));
		stringBuilder.AppendLine($"Positive Reward Ratio: {_stats.PositiveRewardRatio:P1}");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("--- TAT Metrics ---");
		stringBuilder.AppendLine($"Current TAT: {_lastTAT:F2}ms");
		stringBuilder.AppendLine($"Baseline TAT: {_baselineTAT:F2}ms");
		stringBuilder.AppendLine($"Avg TAT: {_stats.AvgTAT:F2}ms");
		stringBuilder.AppendLine($"Min TAT: {_stats.MinTAT:F2}ms");
		stringBuilder.AppendLine($"Max TAT: {_stats.MaxTAT:F2}ms");
		stringBuilder.AppendLine($"TAT Trend: {_stats.TATTrend:F4} ms/step");
		stringBuilder.AppendLine($"Last TAT Delta: {_stats.LastTATDelta:F2}ms");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("--- Energy Metrics ---");
		stringBuilder.AppendLine($"Current Energy: {_stats.LastEnergy:F2}");
		stringBuilder.AppendLine($"Baseline Energy: {_baselineEnergy:F2}");
		stringBuilder.AppendLine($"Avg Energy: {_stats.AvgEnergy:F2}");
		stringBuilder.AppendLine($"Min Energy: {_stats.MinEnergy:F2}");
		stringBuilder.AppendLine($"Max Energy: {_stats.MaxEnergy:F2}");
		stringBuilder.AppendLine($"Energy Trend: {_stats.EnergyTrend:F4} /step");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("--- Learning Status ---");
		stringBuilder.AppendLine($"Total Decisions: {_stats.TotalDecisions}");
		stringBuilder.AppendLine($"Learning Updates: {_stats.LearningUpdates}");
		stringBuilder.AppendLine($"Experience Used: {_stats.ExperienceUsed}");
		stringBuilder.AppendLine($"Experience Skipped: {_stats.ExperienceSkipped}");
		stringBuilder.AppendLine($"Learning Rate: {_currentLearningRate:F7}");
		stringBuilder.AppendLine($"Exploration Rate: {_explorationRate:P1}");
		stringBuilder.AppendLine($"Avg Loss: {_stats.AvgLoss:F6}");
		stringBuilder.AppendLine();
		float num = ComputePolicyEntropy();
		stringBuilder.AppendLine($"Policy Entropy: {num:F4} (max={((_stats.TotalDecisions > 0) ? Math.Log(_stats.CoreSelectionCounts.Length) : 0.0):F2})");
		if (_stats.RewardTrend > 0f && _stats.PositiveRewardRatio > 0.5f)
		{
			stringBuilder.AppendLine("Status: LEARNING IMPROVING");
		}
		else if (_stats.RewardTrend < -0.001f || _stats.PositiveRewardRatio < 0.3f)
		{
			stringBuilder.AppendLine("Status: NEEDS TUNING");
		}
		else
		{
			stringBuilder.AppendLine("Status: STABLE");
		}
		return stringBuilder.ToString();
	}

	public float ComputePolicyEntropy()
	{
		if (_stats.TotalDecisions == 0L)
		{
			return 0f;
		}
		float num = 0f;
		int num2 = 0;
		for (int i = 0; i < _stats.CoreSelectionCounts.Length; i++)
		{
			if (_stats.CoreSelectionCounts[i] > 0)
			{
				num2++;
				float num3 = (float)_stats.CoreSelectionCounts[i] / (float)_stats.TotalDecisions;
				if (num3 > 1E-10f)
				{
					num -= num3 * (float)Math.Log(num3);
				}
			}
		}
		return num;
	}

	public void ResetStatistics()
	{
		_stats.TotalDecisions = 0L;
		_stats.TotalInferenceTimeUs = 0L;
		_stats.LearningUpdates = 0;
		_stats.ExperienceUsed = 0;
		_stats.ExperienceSkipped = 0;
		_stats.AvgLoss = 0f;
		_stats.RecentAvgReward = 0f;
		_stats.MigrationCount = 0;
		_stats.CacheHits = 0;
		_stats.CacheMisses = 0;
		_stats.TotalTATSamples = 0L;
		_stats.AvgTAT = 0f;
		_stats.MinTAT = 0f;
		_stats.MaxTAT = 0f;
		_stats.RewardTrend = 0f;
		_stats.TATTrend = 0f;
		_stats.PositiveRewardRatio = 0f;
		_stats.LastReward = 0f;
		_stats.LastTATDelta = 0f;
		_stats.TotalEnergySamples = 0L;
		_stats.AvgEnergy = 0f;
		_stats.MinEnergy = 0f;
		_stats.MaxEnergy = 0f;
		_stats.BaselineEnergy = 0f;
		_stats.EnergyTrend = 0f;
		_stats.LastEnergy = 0f;
		MathHelper.Clear(_stats.CoreSelectionCounts.AsSpan());
		_baselineTAT = 0f;
		_baselineEnergy = 0f;
		_sumTAT = 0f;
		_tatIndex = 0;
		_lastTAT = 0f;
		_windowIndex = 0;
		_windowStartTick = DateTime.Now.Ticks;
		_normalizationReady = false;
		MathHelper.Clear(_tatHistory.AsSpan());
	}

	public void SaveModel(string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			path = "./scheduler_model.bin";
		}
		using BinaryWriter binaryWriter = new BinaryWriter(File.OpenWrite(path));
		binaryWriter.Write("TSC1");
		binaryWriter.Write(3);
		binaryWriter.Write(64);
		binaryWriter.Write(8);
		binaryWriter.Write(128);
		binaryWriter.Write(64);
		binaryWriter.Write(15);
		binaryWriter.Write(16);
		WriteLayer(binaryWriter, _threadEmbedding);
		WriteLayer(binaryWriter, _coreEmbedding);
		WriteLayer(binaryWriter, _scoreHead);
		WriteAttention(binaryWriter, _crossAttention);
		WriteEncoder(binaryWriter, _threadEncoder);
		WriteEncoder(binaryWriter, _coreEncoder);
		for (int i = 0; i < 7; i++)
		{
			binaryWriter.Write(_threadFeatureMean[i]);
		}
		for (int j = 0; j < 7; j++)
		{
			binaryWriter.Write(_threadFeatureStd[j]);
		}
		for (int k = 0; k < 8; k++)
		{
			binaryWriter.Write(_coreFeatureMean[k]);
		}
		for (int l = 0; l < 8; l++)
		{
			binaryWriter.Write(_coreFeatureStd[l]);
		}
		for (int m = 0; m < _coreIdEmbedding.Length; m++)
		{
			binaryWriter.Write(_coreIdEmbedding[m]);
		}
		binaryWriter.Write(_startTick);
	}

	public bool LoadModel(string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			path = "./scheduler_model.bin";
		}
		if (!File.Exists(path))
		{
			return false;
		}
		try
		{
			using BinaryReader binaryReader = new BinaryReader(File.OpenRead(path));
			if (binaryReader.ReadString() != "TSC1")
			{
				return false;
			}
			int num = binaryReader.ReadInt32();
			if (num != 3)
			{
				return false;
			}
			int num2 = binaryReader.ReadInt32();
			int num3 = binaryReader.ReadInt32();
			int num4 = binaryReader.ReadInt32();
			if (num2 != 64 || num3 != 8 || num4 != 128)
			{
				return false;
			}
			int num5 = binaryReader.ReadInt32();
			int num6 = binaryReader.ReadInt32();
			int num7 = binaryReader.ReadInt32();
			if (num5 != 64 || num6 != 15 || num7 != 16)
			{
				return false;
			}
			ReadLayer(binaryReader, _threadEmbedding);
			ReadLayer(binaryReader, _coreEmbedding);
			ReadLayer(binaryReader, _scoreHead);
			ReadAttention(binaryReader, _crossAttention);
			ReadEncoder(binaryReader, _threadEncoder);
			ReadEncoder(binaryReader, _coreEncoder);
			for (int i = 0; i < 7; i++)
			{
				_threadFeatureMean[i] = binaryReader.ReadSingle();
			}
			for (int j = 0; j < 7; j++)
			{
				_threadFeatureStd[j] = binaryReader.ReadSingle();
			}
			for (int k = 0; k < 8; k++)
			{
				_coreFeatureMean[k] = binaryReader.ReadSingle();
			}
			for (int l = 0; l < 8; l++)
			{
				_coreFeatureStd[l] = binaryReader.ReadSingle();
			}
			for (int m = 0; m < _coreIdEmbedding.Length; m++)
			{
				_coreIdEmbedding[m] = binaryReader.ReadSingle();
			}
			if (num >= 2)
			{
				_startTick = binaryReader.ReadInt64();
			}
			_normalizationReady = true;
			return true;
		}
		catch
		{
			return false;
		}
	}

	private void WriteLayer(BinaryWriter writer, LinearLayer layer)
	{
		for (int i = 0; i < layer.Weights.Length; i++)
		{
			writer.Write(layer.Weights[i]);
		}
		for (int j = 0; j < layer.Bias.Length; j++)
		{
			writer.Write(layer.Bias[j]);
		}
	}

	private void ReadLayer(BinaryReader reader, LinearLayer layer)
	{
		for (int i = 0; i < layer.Weights.Length; i++)
		{
			layer.Weights[i] = reader.ReadSingle();
		}
		for (int j = 0; j < layer.Bias.Length; j++)
		{
			layer.Bias[j] = reader.ReadSingle();
		}
	}

	private void WriteAttention(BinaryWriter writer, MultiHeadAttention attention)
	{
		WriteLayer(writer, attention.Wq);
		WriteLayer(writer, attention.Wk);
		WriteLayer(writer, attention.Wv);
		WriteLayer(writer, attention.Wo);
	}

	private void ReadAttention(BinaryReader reader, MultiHeadAttention attention)
	{
		ReadLayer(reader, attention.Wq);
		ReadLayer(reader, attention.Wk);
		ReadLayer(reader, attention.Wv);
		ReadLayer(reader, attention.Wo);
	}

	private void WriteEncoder(BinaryWriter writer, CoreTransformerEncoder encoder)
	{
		WriteAttention(writer, encoder.SelfAttention);
		WriteLayer(writer, encoder.FeedForward.FC1);
		WriteLayer(writer, encoder.FeedForward.FC2);
		for (int i = 0; i < encoder.Norm1.Gamma.Length; i++)
		{
			writer.Write(encoder.Norm1.Gamma[i]);
		}
		for (int j = 0; j < encoder.Norm1.Beta.Length; j++)
		{
			writer.Write(encoder.Norm1.Beta[j]);
		}
		for (int k = 0; k < encoder.Norm2.Gamma.Length; k++)
		{
			writer.Write(encoder.Norm2.Gamma[k]);
		}
		for (int l = 0; l < encoder.Norm2.Beta.Length; l++)
		{
			writer.Write(encoder.Norm2.Beta[l]);
		}
	}

	private void ReadEncoder(BinaryReader reader, CoreTransformerEncoder encoder)
	{
		ReadAttention(reader, encoder.SelfAttention);
		ReadLayer(reader, encoder.FeedForward.FC1);
		ReadLayer(reader, encoder.FeedForward.FC2);
		for (int i = 0; i < encoder.Norm1.Gamma.Length; i++)
		{
			encoder.Norm1.Gamma[i] = reader.ReadSingle();
		}
		for (int j = 0; j < encoder.Norm1.Beta.Length; j++)
		{
			encoder.Norm1.Beta[j] = reader.ReadSingle();
		}
		for (int k = 0; k < encoder.Norm2.Gamma.Length; k++)
		{
			encoder.Norm2.Gamma[k] = reader.ReadSingle();
		}
		for (int l = 0; l < encoder.Norm2.Beta.Length; l++)
		{
			encoder.Norm2.Beta[l] = reader.ReadSingle();
		}
	}

	private void WriteEncoder(BinaryWriter writer, ThreadTransformerEncoder encoder)
	{
		WriteAttention(writer, encoder.SelfAttention);
		WriteLayer(writer, encoder.FeedForward.FC1);
		WriteLayer(writer, encoder.FeedForward.FC2);
		for (int i = 0; i < encoder.Norm1.Gamma.Length; i++)
		{
			writer.Write(encoder.Norm1.Gamma[i]);
		}
		for (int j = 0; j < encoder.Norm1.Beta.Length; j++)
		{
			writer.Write(encoder.Norm1.Beta[j]);
		}
		for (int k = 0; k < encoder.Norm2.Gamma.Length; k++)
		{
			writer.Write(encoder.Norm2.Gamma[k]);
		}
		for (int l = 0; l < encoder.Norm2.Beta.Length; l++)
		{
			writer.Write(encoder.Norm2.Beta[l]);
		}
	}

	private void ReadEncoder(BinaryReader reader, ThreadTransformerEncoder encoder)
	{
		ReadAttention(reader, encoder.SelfAttention);
		ReadLayer(reader, encoder.FeedForward.FC1);
		ReadLayer(reader, encoder.FeedForward.FC2);
		for (int i = 0; i < encoder.Norm1.Gamma.Length; i++)
		{
			encoder.Norm1.Gamma[i] = reader.ReadSingle();
		}
		for (int j = 0; j < encoder.Norm1.Beta.Length; j++)
		{
			encoder.Norm1.Beta[j] = reader.ReadSingle();
		}
		for (int k = 0; k < encoder.Norm2.Gamma.Length; k++)
		{
			encoder.Norm2.Gamma[k] = reader.ReadSingle();
		}
		for (int l = 0; l < encoder.Norm2.Beta.Length; l++)
		{
			encoder.Norm2.Beta[l] = reader.ReadSingle();
		}
	}
}
