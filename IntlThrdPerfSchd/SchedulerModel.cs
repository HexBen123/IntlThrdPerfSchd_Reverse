using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace IntlThrdPerfSchd
{

public class CircularBuffer<T>
{
	private readonly T[] _buffer;

	private readonly int _capacity;

	private int _head;

	private int _count;

	public int Count => _count;

	public CircularBuffer(int capacity)
	{
		_capacity = capacity;
		_buffer = new T[capacity];
		_head = 0;
		_count = 0;
	}

	public void Add(T item)
	{
		_buffer[_head] = item;
		_head = (_head + 1) % _capacity;
		if (_count < _capacity)
		{
			_count++;
		}
	}

	public T Get(int index)
	{
		if (index < 0 || index >= _count)
		{
			throw new ArgumentOutOfRangeException("index");
		}
		int actualIndex = (_head - _count + index + _capacity) % _capacity;
		return _buffer[actualIndex];
	}

	public Span<T> GetRecent(int n)
	{
		n = Math.Min(n, _count);
		return new Span<T>(_buffer, (_head - n + _capacity) % _capacity, n);
	}
}

public struct DecisionRecord
{
	public float[] ThreadFeatures;

	public float[][] CoreFeatures;

	public int NumCores;

	public int SelectedCore;

	public float Reward;

	public long Timestamp;

	public int PredictedCore;

	public int ActualCore;

	public bool IsValid;

	public DecisionRecord(int maxCores)
	{
		ThreadFeatures = new float[5];
		CoreFeatures = new float[maxCores][];
		for (int i = 0; i < maxCores; i++)
		{
			CoreFeatures[i] = new float[7];
		}
		NumCores = 0;
		SelectedCore = -1;
		Reward = 0f;
		Timestamp = 0L;
		PredictedCore = -1;
		ActualCore = -1;
		IsValid = true;
	}
}

public class SchedulerStatistics
{
	public long TotalDecisions;

	public long TotalInferenceTimeUs;

	public int[] CoreSelectionCounts;

	public float AvgInferenceTimeUs;

	public float RecentAvgReward;

	public int CacheHits;

	public int CacheMisses;

	public int LearningUpdates;

	public int ExperienceUsed;

	public int ExperienceSkipped;

	public float AvgLoss;

	public int MigrationCount;

	public float AvgTAT;

	public float MinTAT;

	public float MaxTAT;

	public long TotalTATSamples;

	public float RewardTrend;

	public float TATTrend;

	public float PolicyEntropy;

	public float PositiveRewardRatio;

	public float LastReward;

	public float LastTATDelta;

	private const int REWARD_HISTORY_SIZE = 100;

	private const int TAT_HISTORY_SIZE = 100;

	private readonly float[] _rewardHistory;

	private readonly float[] _tatHistory;

	private int _rewardIndex;

	private int _tatIndex;

	private int _rewardCount;

	private int _tatCount;

	private float _baselineTAT = 1000f;

	public SchedulerStatistics(int maxCores)
	{
		CoreSelectionCounts = new int[maxCores];
		_rewardHistory = new float[100];
		_tatHistory = new float[100];
	}

	public void RecordReward(float reward)
	{
		LastReward = reward;
		_rewardHistory[_rewardIndex] = reward;
		_rewardIndex = (_rewardIndex + 1) % 100;
		if (_rewardCount < 100)
		{
			_rewardCount++;
		}
		PositiveRewardRatio = UpdatePositiveRatio();
	}

	public void RecordTATDelta(float delta)
	{
		LastTATDelta = delta;
		_tatHistory[_tatIndex] = delta;
		_tatIndex = (_tatIndex + 1) % 100;
		if (_tatCount < 100)
		{
			_tatCount++;
		}
	}

	private float UpdatePositiveRatio()
	{
		if (_rewardCount == 0)
		{
			return 0f;
		}
		int positive = 0;
		for (int i = 0; i < _rewardCount; i++)
		{
			if (_rewardHistory[i] > 0f)
			{
				positive++;
			}
		}
		return (float)positive / (float)_rewardCount;
	}

	public void ComputeTrends()
	{
		if (_rewardCount >= 10)
		{
			RewardTrend = ComputeTrend(_rewardHistory, _rewardCount);
		}
		if (_tatCount >= 10)
		{
			TATTrend = ComputeTrend(_tatHistory, _tatCount);
		}
	}

	private float ComputeTrend(float[] history, int count)
	{
		float sumX = 0f;
		float sumY = 0f;
		float sumXY = 0f;
		float sumX2 = 0f;
		int startIdx = ((count >= 100) ? _rewardIndex : 0);
		for (int i = 0; i < count; i++)
		{
			float x = i;
			float y = history[(startIdx + i) % 100];
			sumX += x;
			sumY += y;
			sumXY += x * y;
			sumX2 += x * x;
		}
		float n = count;
		float denominator = n * sumX2 - sumX * sumX;
		if (Math.Abs(denominator) < 1E-08f)
		{
			return 0f;
		}
		return (n * sumXY - sumX * sumY) / denominator;
	}

	public string GetReport(int numCores, float explorationRate = 0.1f)
	{
		ComputeTrends();
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("=== Transformer Scheduler Statistics ===");
		sb.AppendLine($"Total Decisions: {TotalDecisions}");
		sb.AppendLine($"Learning Updates: {LearningUpdates}");
		sb.AppendLine($"Avg Inference Time: {((TotalDecisions > 0) ? ((float)TotalInferenceTimeUs / (float)TotalDecisions) : 0f):F2} μs");
		sb.AppendLine();
		sb.AppendLine("--- Reward & Learning ---");
		sb.AppendLine($"Last Reward: {LastReward:F6}");
		sb.AppendLine($"Recent Avg Reward: {RecentAvgReward:F6}");
		sb.AppendLine(string.Format("Reward Trend: {0:F6} ({1}{2:F2}%/step)", RewardTrend, (RewardTrend > 0f) ? "+" : "", RewardTrend * 100f));
		sb.AppendLine($"Positive Reward Ratio: {PositiveRewardRatio:P1}");
		sb.AppendLine($"Avg Loss: {AvgLoss:F6}");
		sb.AppendLine();
		sb.AppendLine("--- TAT Performance ---");
		sb.AppendLine($"Current TAT: {AvgTAT:F2}ms (Min={MinTAT:F2}, Max={MaxTAT:F2})");
		sb.AppendLine($"TAT Trend: {TATTrend:F4} ms/step");
		sb.AppendLine($"Last TAT Delta: {LastTATDelta:F2}ms");
		sb.AppendLine($"Baseline TAT: {GetBaselineTAT():F2}ms");
		sb.AppendLine();
		sb.AppendLine("--- Core Selection ---");
		sb.AppendLine($"Cache Hit Rate: {((CacheHits + CacheMisses > 0) ? ((float)CacheHits * 100f / (float)(CacheHits + CacheMisses)) : 0f):F1}%");
		sb.AppendLine($"Migrations: {MigrationCount} ({((TotalDecisions > 0) ? ((float)MigrationCount * 100f / (float)TotalDecisions) : 0f):F1}%)");
		sb.AppendLine($"Exploration Rate: {explorationRate:P1}");
		sb.AppendLine("Core Selection Distribution:");
		for (int i = 0; i < numCores; i++)
		{
			float pct = ((TotalDecisions > 0) ? ((float)CoreSelectionCounts[i] * 100f / (float)TotalDecisions) : 0f);
			sb.AppendLine($"  Core {i}: {CoreSelectionCounts[i]} ({pct:F1}%)");
		}
		return sb.ToString();
	}

	public float GetBaselineTAT()
	{
		return _baselineTAT;
	}

	public void UpdateBaselineTAT(float value)
	{
		_baselineTAT = value;
	}
}

public class TransformerScheduler
{
	private static readonly Random _random = new Random(42);

	private const int D_MODEL = 64;

	private const int N_HEAD = 8;

	private const int D_FF = 128;

	private const int MAX_CORES = 64;

	private const int THREAD_FEATURE_DIM = 5;

	private const int CORE_FEATURE_DIM = 7;

	private const int HISTORY_CAPACITY = 10000;

	private const long WINDOW_TICKS = 3000000000L;

	private const int NORMALIZATION_WINDOW_SIZE = 1000;

	private readonly LinearLayer _threadEmbedding;

	private readonly LinearLayer _coreEmbedding;

	private readonly TransformerEncoderLayer _encoder;

	private readonly MultiHeadAttention _crossAttention;

	private readonly LinearLayer _scoreHead;

	private readonly float[] _threadEmbed;

	private readonly float[] _threadEncoded;

	private readonly float[] _crossOutput;

	private readonly float[] _coreScore;

	private readonly float[] _attentionWeights;

	private readonly float[][] _coreEmbeddings;

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

	private float _lastTAT;

	private float _baselineTAT;

	private const float LEARNING_RATE = 0.0001f;

	private int _decisionsInCurrentSecond;

	private long _lastTATUpdateTick;

	private float _explorationRate = 0.1f;

	private float _minExplorationRate = 0.01f;

	private long _startTick;

	private const long EXPLORE_DECAY_DURATION = 18000000000L;

	private const long INITIAL_LEARNING_PHASE_MS = 600000L;

	private const long RAPID_LEARNING_PHASE_MS = 120000L;

	private const float RAPID_LEARNING_RATE = 0.1f;

	private const float INITIAL_LEARNING_RATE = 0.01f;

	private const float MIN_LEARNING_RATE = 0.0001f;

	private float _currentLearningRate = 0.1f;

	private const long LEARNING_RATE_DECAY_DURATION = 36000000000L;

	private const float INITIAL_EXPLORATION_RATE = 0.5f;

	private const long BATCH_TRAIN_INTERVAL = 600000000L;

	private long _lastBatchTrainTick;

	private const int BATCH_SAMPLE_SIZE = 1000;

	public SchedulerStatistics Statistics => _stats;

	public TransformerScheduler()
	{
		_threadEmbedding = new LinearLayer(5, 64);
		_coreEmbedding = new LinearLayer(7, 64);
		_encoder = new TransformerEncoderLayer(64, 8, 128);
		_crossAttention = new MultiHeadAttention(64, 8);
		_scoreHead = new LinearLayer(64, 1);
		_threadEmbed = new float[64];
		_threadEncoded = new float[64];
		_crossOutput = new float[64];
		_coreScore = new float[64];
		_attentionWeights = new float[64];
		_coreEmbeddings = new float[64][];
		_coreFeatureHash = new int[64];
		_coreCacheValid = new bool[64];
		for (int i = 0; i < 64; i++)
		{
			_coreEmbeddings[i] = new float[64];
			_coreCacheValid[i] = false;
		}
		_history = new CircularBuffer<DecisionRecord>(10000);
		_stats = new SchedulerStatistics(64);
		_currentRecord = new DecisionRecord(64);
		_threadFeatureMean = new float[5];
		_threadFeatureStd = new float[5];
		_coreFeatureMean = new float[7];
		_coreFeatureStd = new float[7];
		_threadFeatureWindow = new float[1000][];
		_coreFeatureWindow = new float[1000][];
		for (int j = 0; j < 1000; j++)
		{
			_threadFeatureWindow[j] = new float[5];
			_coreFeatureWindow[j] = new float[7];
		}
		_windowIndex = 0;
		_windowStartTick = DateTime.Now.Ticks;
		_normalizationReady = false;
		for (int k = 0; k < 5; k++)
		{
			_threadFeatureStd[k] = 1f;
		}
		for (int l = 0; l < 7; l++)
		{
			_coreFeatureStd[l] = 1f;
		}
		_tatHistory = new float[1000];
		_tatIndex = 0;
		_sumTAT = 0f;
		_lastTAT = 0f;
		_baselineTAT = 1000f;
		_startTick = DateTime.Now.Ticks;
		_lastBatchTrainTick = _startTick;
		_gradScoreHead = new float[1];
		_gradCrossOutput = new float[64];
		_gradThreadEncoded = new float[64];
		_gradThreadEmbed = new float[64];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int ComputeFeatureHash(ReadOnlySpan<float> features)
	{
		int hash = 0;
		for (int i = 0; i < features.Length; i++)
		{
			hash = hash * 31 + features[i].GetHashCode();
		}
		return hash;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void NormalizeFeatures(ReadOnlySpan<float> input, Span<float> output, ReadOnlySpan<float> mean, ReadOnlySpan<float> std)
	{
		int len = Math.Min(input.Length, Math.Min(output.Length, Math.Min(mean.Length, std.Length)));
		for (int i = 0; i < len; i++)
		{
			float effectiveStd = ((std[i] > 0.001f) ? std[i] : 1f);
			output[i] = (input[i] - mean[i]) / effectiveStd;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void UpdateNormalizationFixedWindow(ReadOnlySpan<float> threadFeatures, float[][] coreFeatures, int numCores)
	{
		long currentTick = DateTime.Now.Ticks;
		if (currentTick - _windowStartTick >= 3000000000u)
		{
			ComputeWindowStatistics();
			_windowStartTick = currentTick;
			_windowIndex = 0;
			_normalizationReady = true;
		}
		if (_windowIndex >= 1000)
		{
			return;
		}
		threadFeatures.CopyTo(_threadFeatureWindow[_windowIndex]);
		if (numCores > 0)
		{
			for (int i = 0; i < 7; i++)
			{
				float sum = 0f;
				for (int c = 0; c < numCores; c++)
				{
					sum += coreFeatures[c][i];
				}
				_coreFeatureWindow[_windowIndex][i] = sum / (float)numCores;
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
		Array.Clear(_threadFeatureMean, 0, 5);
		Array.Clear(_coreFeatureMean, 0, 7);
		Array.Clear(_threadFeatureStd, 0, 5);
		Array.Clear(_coreFeatureStd, 0, 7);
		for (int i = 0; i < _windowIndex; i++)
		{
			for (int j = 0; j < 5; j++)
			{
				_threadFeatureMean[j] += _threadFeatureWindow[i][j];
			}
			for (int k = 0; k < 7; k++)
			{
				_coreFeatureMean[k] += _coreFeatureWindow[i][k];
			}
		}
		float invN = 1f / (float)_windowIndex;
		for (int l = 0; l < 5; l++)
		{
			_threadFeatureMean[l] *= invN;
		}
		for (int m = 0; m < 7; m++)
		{
			_coreFeatureMean[m] *= invN;
		}
		for (int n = 0; n < _windowIndex; n++)
		{
			for (int num = 0; num < 5; num++)
			{
				float diff = _threadFeatureWindow[n][num] - _threadFeatureMean[num];
				_threadFeatureStd[num] += diff * diff;
			}
			for (int num2 = 0; num2 < 7; num2++)
			{
				float diff2 = _coreFeatureWindow[n][num2] - _coreFeatureMean[num2];
				_coreFeatureStd[num2] += diff2 * diff2;
			}
		}
		for (int num3 = 0; num3 < 5; num3++)
		{
			_threadFeatureStd[num3] = (float)Math.Sqrt(_threadFeatureStd[num3] / (float)_windowIndex + 1E-06f);
		}
		for (int num4 = 0; num4 < 7; num4++)
		{
			_coreFeatureStd[num4] = (float)Math.Sqrt(_coreFeatureStd[num4] / (float)_windowIndex + 1E-06f);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void UpdateCoreEmbedding(ReadOnlySpan<float> coreFeatures, int coreIndex, Span<float> normalized)
	{
		NormalizeFeatures(coreFeatures, normalized, _coreFeatureMean, _coreFeatureStd);
		_coreEmbedding.Forward(normalized, _coreEmbeddings[coreIndex]);
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
			if (coreFeatures[i] == null || coreFeatures[i].Length < 7)
			{
				object arg = i;
				float[] obj = coreFeatures[i];
				throw new ArgumentOutOfRangeException("coreFeatures", $"coreFeatures[{arg}].Length={((obj != null) ? obj.Length : (-1))} < CORE_FEATURE_DIM={7}");
			}
		}
		if (threadFeatures.Length < 5)
		{
			throw new ArgumentOutOfRangeException("threadFeatures", $"threadFeatures.Length={threadFeatures.Length} < THREAD_FEATURE_DIM={5}");
		}
		Stopwatch sw = Stopwatch.StartNew();
		UpdateNormalizationFixedWindow(threadFeatures, coreFeatures, numCores);
		Span<float> normalizedThread = stackalloc float[5];
		NormalizeFeatures(threadFeatures, normalizedThread, _threadFeatureMean, _threadFeatureStd);
		_threadEmbedding.Forward(normalizedThread, _threadEmbed);
		_encoder.Forward(_threadEmbed, _threadEncoded);
		Span<float> normalizedCore = stackalloc float[7];
		for (int j = 0; j < numCores; j++)
		{
			ReadOnlySpan<float> coreFeat = new ReadOnlySpan<float>(coreFeatures[j], 0, 7);
			int hash = ComputeFeatureHash(coreFeat);
			if (_coreFeatureHash[j] != hash || !_coreCacheValid[j])
			{
				UpdateCoreEmbedding(coreFeat, j, normalizedCore);
				_coreFeatureHash[j] = hash;
				_coreCacheValid[j] = true;
				_stats.CacheMisses++;
			}
			else
			{
				_stats.CacheHits++;
			}
		}
		Span<float> attentionWeights = _attentionWeights.AsSpan(0, numCores);
		_crossAttention.CrossAttention(_threadEncoded, _coreEmbeddings, _coreEmbeddings, _crossOutput, numCores, attentionWeights);
		float sumScores = 0f;
		for (int k = 0; k < numCores; k++)
		{
			_coreScore[k] = attentionWeights[k];
			sumScores += _coreScore[k];
		}
		int selectedCore = 0;
		float maxScore = float.MinValue;
		for (int l = 0; l < numCores; l++)
		{
			if (_coreScore[l] > maxScore)
			{
				maxScore = _coreScore[l];
				selectedCore = l;
			}
		}
		if (sumScores > 0f)
		{
			for (int m = 0; m < numCores; m++)
			{
				_coreScore[m] /= sumScores;
			}
		}
		long elapsedMs = (DateTime.Now.Ticks - _startTick) / 10000;
		if (elapsedMs < 120000)
		{
			_explorationRate = 0.5f;
		}
		else if (elapsedMs < 600000)
		{
			float progress = (float)(elapsedMs - 120000) / 480000f;
			_explorationRate = _minExplorationRate + (0.1f - _minExplorationRate) * (1f - progress);
		}
		else
		{
			_explorationRate = 0f;
		}
		if (_random.NextDouble() < (double)_explorationRate)
		{
			selectedCore = _random.Next(numCores);
		}
		long currentTick = DateTime.Now.Ticks;
		if (currentTick - _lastTATUpdateTick >= 10000000)
		{
			_decisionsInCurrentSecond = 0;
			_lastTATUpdateTick = currentTick;
		}
		_decisionsInCurrentSecond++;
		RecordDecision(threadFeatures, coreFeatures, numCores, selectedCore, predictedCore, actualCore);
		sw.Stop();
		_stats.TotalDecisions++;
		_stats.TotalInferenceTimeUs += sw.ElapsedTicks * 1000000 / Stopwatch.Frequency;
		_stats.CoreSelectionCounts[selectedCore]++;
		return selectedCore;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void UpdateTAT(float currentTAT)
	{
		UpdateTATInternal(currentTAT);
	}

	private void UpdateTATInternal(float currentTAT)
	{
		currentTAT /= 1000f;
		if (currentTAT < 0.01f || currentTAT > 100000f)
		{
			return;
		}
		long elapsedMs = (DateTime.Now.Ticks - _startTick) / 10000;
		if (elapsedMs < 120000)
		{
			_currentLearningRate = 0.1f;
		}
		else if (elapsedMs < 600000)
		{
			float progress = (float)(elapsedMs - 120000) / 480000f;
			_currentLearningRate = 0.0001f + 0.0099f * (1f - progress);
		}
		else
		{
			_currentLearningRate = 0.0001f;
		}
		float tatDelta = 0f;
		if (_stats.TotalTATSamples > 0)
		{
			tatDelta = currentTAT - _lastTAT;
			_stats.RecordTATDelta(tatDelta);
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
		float reward = 0f;
		if (_baselineTAT > 0.001f)
		{
			reward = (_baselineTAT - currentTAT) / _baselineTAT;
			reward = ((reward > 1f) ? 1f : ((reward < -1f) ? (-1f) : reward));
		}
		_stats.RecordReward(reward);
		_stats.RecentAvgReward = _stats.RecentAvgReward * 0.95f + reward * 0.05f;
		bool isInitialPhase = elapsedMs < 600000;
		float learningFactor = 1f;
		if (!isInitialPhase && currentTAT < 1f)
		{
			learningFactor = 0f;
		}
		else if (!isInitialPhase && currentTAT < 2f)
		{
			learningFactor = 0.2f;
		}
		int decisionsToLearn = Math.Min(_decisionsInCurrentSecond, _history.Count);
		int startIdx = _history.Count - decisionsToLearn;
		float batchLearningRate = _currentLearningRate * 0.3f * learningFactor;
		float totalWeight = 0f;
		float[] weights = new float[decisionsToLearn];
		for (int i = 0; i < decisionsToLearn; i++)
		{
			weights[i] = (float)Math.Exp(-0.5f * (float)(decisionsToLearn - 1 - i));
			totalWeight += weights[i];
		}
		float totalAdvantage = 0f;
		for (int j = 0; j < decisionsToLearn; j++)
		{
			int idx = startIdx + j;
			if (idx >= 0 && idx < _history.Count)
			{
				DecisionRecord record = _history.Get(idx);
				if (!isInitialPhase && !record.IsValid)
				{
					_stats.ExperienceSkipped++;
					continue;
				}
				float weightedReward = (record.Reward = reward * (weights[j] / totalWeight));
				totalAdvantage += Math.Abs(weightedReward);
				_gradScoreHead[0] = weightedReward;
				_scoreHead.Backward(_gradScoreHead, batchLearningRate);
				float[] gradCrossInput = _scoreHead.InputGrads;
				_crossAttention.Backward(gradCrossInput, batchLearningRate);
				float[] gradQuery = _crossAttention.GetQueryGradients();
				_encoder.Backward(gradQuery, batchLearningRate);
				float[] gradEncoded = _encoder.InputGradients;
				_threadEmbedding.Backward(gradEncoded, batchLearningRate);
				_stats.ExperienceUsed++;
				_stats.LearningUpdates++;
			}
		}
		_scoreHead.ApplyGradientsSGD(batchLearningRate);
		_threadEmbedding.ApplyGradientsSGD(batchLearningRate);
		_coreEmbedding.ApplyGradientsSGD(batchLearningRate);
		_crossAttention.ApplyGradients();
		_encoder.ApplyGradients();
		_stats.AvgLoss = _stats.AvgLoss * 0.9f + totalAdvantage / (float)Math.Max(decisionsToLearn, 1) * 0.1f;
		long currentTick = DateTime.Now.Ticks;
		if (currentTick - _lastBatchTrainTick >= 600000000)
		{
			PerformBatchTraining();
			_lastBatchTrainTick = currentTick;
		}
		_decisionsInCurrentSecond = 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void PerformBatchTraining()
	{
		if (_history.Count >= 10)
		{
			int sampleSize = Math.Min(1000, _history.Count);
			float batchLearningRate = _currentLearningRate;
			float totalLoss = 0f;
			for (int s = 0; s < sampleSize; s++)
			{
				int idx = _random.Next(_history.Count);
				float reward = _history.Get(idx).Reward;
				_gradScoreHead[0] = reward;
				_scoreHead.Backward(_gradScoreHead, batchLearningRate);
				float[] gradCrossInput = _scoreHead.InputGrads;
				_crossAttention.Backward(gradCrossInput, batchLearningRate);
				float[] gradQuery = _crossAttention.GetQueryGradients();
				_encoder.Backward(gradQuery, batchLearningRate);
				float[] gradEncoded = _encoder.InputGradients;
				_threadEmbedding.Backward(gradEncoded, batchLearningRate);
				totalLoss += Math.Abs(reward);
				_stats.LearningUpdates++;
			}
			_scoreHead.ApplyGradientsSGD(batchLearningRate);
			_threadEmbedding.ApplyGradientsSGD(batchLearningRate);
			_coreEmbedding.ApplyGradientsSGD(batchLearningRate);
			_crossAttention.ApplyGradients();
			_encoder.ApplyGradients();
			_stats.AvgLoss = _stats.AvgLoss * 0.95f + totalLoss / (float)sampleSize * 0.05f;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void RecordDecision(ReadOnlySpan<float> threadFeatures, float[][] coreFeatures, int numCores, int selectedCore, int predictedCore = -1, int actualCore = -1)
	{
		threadFeatures.CopyTo(_currentRecord.ThreadFeatures);
		_currentRecord.NumCores = numCores;
		for (int i = 0; i < numCores; i++)
		{
			Array.Copy(coreFeatures[i], _currentRecord.CoreFeatures[i], 7);
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

	public string GetStatistics()
	{
		long elapsedMs = (DateTime.Now.Ticks - _startTick) / 10000;
		bool isInitialPhase = elapsedMs < 600000;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine($"Runtime: {elapsedMs / 60000} min / {10L} min");
		stringBuilder.AppendLine("Learning Phase: " + (isInitialPhase ? "Initial (all experiences valid)" : "Stable (skip invalid experiences)"));
		stringBuilder.AppendLine();
		stringBuilder.Append(_stats.GetReport(64, _explorationRate));
		return stringBuilder.ToString();
	}

	public string GetRecentDecisions(int n = 10)
	{
		n = Math.Min(n, _history.Count);
		StringBuilder sb = new StringBuilder();
		sb.AppendLine($"=== Recent {n} Decisions ===");
		for (int i = 0; i < n; i++)
		{
			DecisionRecord record = _history.Get(n - 1 - i);
			sb.AppendLine($"[{i}] Core {record.SelectedCore} | " + $"Thread IPC: {record.ThreadFeatures[1]:F2} | " + $"Priority: {record.ThreadFeatures[2]:F2} | " + $"Reward: {record.Reward:F6}");
		}
		return sb.ToString();
	}

	public string GetAttentionHeadReport(int numCores)
	{
		StringBuilder sb = new StringBuilder();
		float[][] headWeights = _crossAttention.GetHeadAttentionWeights(numCores);
		sb.AppendLine("╔" + new string('═', 96) + "╗");
		sb.Append("║ Core   │");
		for (int h = 0; h < 8; h++)
		{
			sb.Append($"  H{h}    │");
		}
		sb.AppendLine("  Avg   ║");
		sb.Append("╟" + new string('─', 8) + "┼");
		for (int i = 0; i < 8; i++)
		{
			sb.Append(new string('─', 10) + "┼");
		}
		sb.AppendLine(new string('─', 8) + "╢");
		int[] maxCores = new int[8];
		float[] maxWeights = new float[8];
		for (int j = 0; j < 8; j++)
		{
			maxCores[j] = 0;
			maxWeights[j] = headWeights[j][0];
			for (int c = 1; c < numCores; c++)
			{
				if (headWeights[j][c] > maxWeights[j])
				{
					maxWeights[j] = headWeights[j][c];
					maxCores[j] = c;
				}
			}
		}
		for (int k = 0; k < numCores; k++)
		{
			sb.Append($"║ {k,-5} │");
			for (int l = 0; l < 8; l++)
			{
				string val = ((k == maxCores[l]) ? $"*{headWeights[l][k]:F4}" : $" {headWeights[l][k]:F4}");
				sb.Append($"{val,-8}│");
			}
			sb.AppendLine($" {_attentionWeights[k]:F4} ║");
		}
		sb.Append("╚" + new string('═', 8) + "╧");
		for (int m = 0; m < 8; m++)
		{
			sb.Append(new string('═', 10) + "╧");
		}
		sb.AppendLine(new string('═', 8) + "╝");
		sb.AppendLine();
		sb.AppendLine("Head Summary:");
		for (int n = 0; n < 8; n++)
		{
			float sum = 0f;
			for (int num = 0; num < numCores; num++)
			{
				sum += headWeights[n][num];
			}
			sb.AppendLine($"  H{n}: Max→Core{maxCores[n]} ({maxWeights[n]:F4}), Avg={sum / (float)numCores:F4}");
		}
		return sb.ToString();
	}

	public float[] GetLastAttentionWeights(int numCores)
	{
		float[] weights = new float[numCores];
		Array.Copy(_attentionWeights, weights, numCores);
		return weights;
	}

	public void SetNormalizationParams(float[] threadMean, float[] threadStd, float[] coreMean, float[] coreStd)
	{
		Array.Copy(threadMean, _threadFeatureMean, 5);
		Array.Copy(threadStd, _threadFeatureStd, 5);
		Array.Copy(coreMean, _coreFeatureMean, 7);
		Array.Copy(coreStd, _coreFeatureStd, 7);
		_normalizationReady = true;
	}

	public string ExportModel()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine($"Model Config: d_model={64}, n_head={8}, d_ff={128}");
		stringBuilder.AppendLine($"Thread Embedding: {5} -> {64}");
		stringBuilder.AppendLine($"Core Embedding: {7} -> {64}");
		stringBuilder.AppendLine($"Total Params: {CountParameters()}");
		return stringBuilder.ToString();
	}

	private int CountParameters()
	{
		return 0 + (_threadEmbedding.Weights.Length + _threadEmbedding.Bias.Length) + (_coreEmbedding.Weights.Length + _coreEmbedding.Bias.Length) + (_scoreHead.Weights.Length + _scoreHead.Bias.Length);
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
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("=== Neural Network Learning Report ===");
		sb.AppendLine();
		sb.AppendLine("--- Reward Metrics ---");
		sb.AppendLine($"Last Reward: {_stats.LastReward:F6}");
		sb.AppendLine($"Recent Avg Reward: {_stats.RecentAvgReward:F6}");
		sb.AppendLine(string.Format("Reward Trend: {0:F6} ({1}{2:F2}%/step)", _stats.RewardTrend, (_stats.RewardTrend > 0f) ? "+" : "", _stats.RewardTrend * 100f));
		sb.AppendLine($"Positive Reward Ratio: {_stats.PositiveRewardRatio:P1}");
		sb.AppendLine();
		sb.AppendLine("--- TAT Metrics ---");
		sb.AppendLine($"Current TAT: {_lastTAT:F2}ms");
		sb.AppendLine($"Baseline TAT: {_baselineTAT:F2}ms");
		sb.AppendLine($"Avg TAT: {_stats.AvgTAT:F2}ms");
		sb.AppendLine($"Min TAT: {_stats.MinTAT:F2}ms");
		sb.AppendLine($"Max TAT: {_stats.MaxTAT:F2}ms");
		sb.AppendLine($"TAT Trend: {_stats.TATTrend:F4} ms/step");
		sb.AppendLine($"Last TAT Delta: {_stats.LastTATDelta:F2}ms");
		sb.AppendLine();
		sb.AppendLine("--- Learning Status ---");
		sb.AppendLine($"Total Decisions: {_stats.TotalDecisions}");
		sb.AppendLine($"Learning Updates: {_stats.LearningUpdates}");
		sb.AppendLine($"Experience Used: {_stats.ExperienceUsed}");
		sb.AppendLine($"Experience Skipped: {_stats.ExperienceSkipped}");
		sb.AppendLine($"Learning Rate: {_currentLearningRate:F7}");
		sb.AppendLine($"Exploration Rate: {_explorationRate:P1}");
		sb.AppendLine($"Avg Loss: {_stats.AvgLoss:F6}");
		sb.AppendLine();
		float entropy = ComputePolicyEntropy();
		sb.AppendLine($"Policy Entropy: {entropy:F4} (max={((_stats.TotalDecisions > 0) ? Math.Log(_stats.CoreSelectionCounts.Length) : 0.0):F2})");
		if (_stats.RewardTrend > 0f && _stats.PositiveRewardRatio > 0.5f)
		{
			sb.AppendLine("Status: LEARNING IMPROVING");
		}
		else if (_stats.RewardTrend < -0.001f || _stats.PositiveRewardRatio < 0.3f)
		{
			sb.AppendLine("Status: NEEDS TUNING");
		}
		else
		{
			sb.AppendLine("Status: STABLE");
		}
		return sb.ToString();
	}

	private float ComputePolicyEntropy()
	{
		if (_stats.TotalDecisions == 0L)
		{
			return 0f;
		}
		float entropy = 0f;
		int activeCores = 0;
		for (int i = 0; i < _stats.CoreSelectionCounts.Length; i++)
		{
			if (_stats.CoreSelectionCounts[i] > 0)
			{
				activeCores++;
				float p = (float)_stats.CoreSelectionCounts[i] / (float)_stats.TotalDecisions;
				if (p > 1E-10f)
				{
					entropy -= p * (float)Math.Log(p);
				}
			}
		}
		return entropy;
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
		Array.Clear(_stats.CoreSelectionCounts, 0, _stats.CoreSelectionCounts.Length);
		_baselineTAT = 1000f;
		_sumTAT = 0f;
		_tatIndex = 0;
		_lastTAT = 0f;
		_windowIndex = 0;
		_windowStartTick = DateTime.Now.Ticks;
		_normalizationReady = false;
		Array.Clear(_tatHistory, 0, _tatHistory.Length);
	}
}
}
