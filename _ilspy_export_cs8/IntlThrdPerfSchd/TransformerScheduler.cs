using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using SimdLibrary;

namespace IntlThrdPerfSchd
{
	public class TransformerScheduler
	{
		private static readonly Random _random = new Random(42);

		private const int D_MODEL = 64;

		private const int N_HEAD = 4;

		private const int D_FF = 256;

		private const int NUM_CORE_ENCODER_LAYERS = 3;

		private const int NUM_CROSS_ATTN_LAYERS = 2;

		private const int MAX_CORES = 64;

		private const int THREAD_RAW_DIM = 6;

		private const int CORE_RAW_DIM = 7;

		private const int CORE_ID_EMBED_DIM = 4;

		private const int CORE_TYPE_EMBED_DIM = 4;

		private const int MAX_CORE_TYPES = 7;

		private const int THREAD_INPUT_DIM = 8;

		private const int CORE_INPUT_DIM = 9;

		private const int THREAD_FEATURE_DIM = 14;

		private const int CORE_FEATURE_DIM = 15;

		private const int HISTORY_CAPACITY = 10000;

		private const long WINDOW_TICKS = 600000000L;

		private const int NORMALIZATION_WINDOW_SIZE = 1000;

		private float ENERGY_WEIGHT;

		private float NEW_METRIC_WEIGHT;

		private float LOAD_BALANCE_WEIGHT = 0.3f;

		private float LOAD_BALANCE2_WEIGHT;

		private float EXTRA_METRIC_WEIGHT;

		private const float LOAD_BALANCE_DECAY = 1.5f;

		private readonly LinearLayer _threadEmbedding;

		private readonly LinearLayer _coreEmbedding;

		private readonly ThreadFFNBlock _threadEncoder;

		private readonly CoreTransformerEncoder[] _coreEncoders;

		private readonly MultiHeadAttention[] _crossAttentions;

		private readonly float[] _threadEmbed;

		private readonly float[] _threadEncoded;

		private readonly float[] _crossIntermediate;

		private readonly float[] _crossOutput;

		private readonly float[] _coreScore;

		private readonly float[] _attentionWeights;

		private readonly float[][] _coreEmbeddings;

		private readonly float[][] _coreEncoded;

		private readonly float[][] _coreIntermediate1;

		private readonly float[][] _coreIntermediate2;

		private int _batchFeatureHash;

		private readonly float[][] _coreInputs;

		private bool _batchCacheValid;

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

		private readonly float[] _gradCrossOutput;

		private readonly float[] _gradThreadEncoded;

		private readonly float[] _gradThreadEmbed;

		private readonly float[] _coreIdEmbedding;

		private readonly float[] _coreTypeEmbedding;

		private readonly float[] _coreIdEmbeddingGrad;

		private readonly float[] _coreTypeEmbeddingGrad;

		private readonly float[] _normThreadBuf;

		private readonly float[] _normCoreBuf;

		private readonly float[] _normCoreBwdBuf;

		private readonly float[] _gradCoreEncodedBuf;

		private readonly float[][] _gradCrossKVPerCore;

		private readonly float[] _coreEmbedWeightGradAccum;

		private readonly float[] _coreEmbedBiasGradAccum;

		private float _lastTAT;

		private float _baselineTAT;

		private float _baselineEnergy;

		private float _baselineNewMetric;

		private float _baselineLoadBalance;

		private float _baselineLoadBalance2;

		private float _baselineExtraMetric;

		private const float LEARNING_RATE = 0.0001f;

		private int _decisionsInCurrentSecond;

		private long _lastTATUpdateTick;

		private float _explorationRate;

		private float _minExplorationRate;

		public long _startTick;

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

		private const float INITIAL_SOFTMAX_TEMPERATURE = 2f;

		private const long TEMPERATURE_ANNEAL_MS = 120000L;

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

		private const int MODEL_VERSION = 11;

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
			_threadEmbedding = new LinearLayer(14, 64);
			_coreEmbedding = new LinearLayer(15, 64);
			_threadEncoder = new ThreadFFNBlock();
			_coreEncoders = new CoreTransformerEncoder[3];
			for (int i = 0; i < 3; i++)
			{
				_coreEncoders[i] = new CoreTransformerEncoder();
			}
			_crossAttentions = new MultiHeadAttention[2];
			for (int j = 0; j < 2; j++)
			{
				_crossAttentions[j] = new MultiHeadAttention(64, 4);
			}
			_threadEmbed = new float[64];
			_threadEncoded = new float[64];
			_crossIntermediate = new float[64];
			_crossOutput = new float[64];
			_coreScore = new float[64];
			_attentionWeights = new float[64];
			_coreEmbeddings = new float[64][];
			_coreEncoded = new float[64][];
			_coreInputs = new float[64][];
			_coreIntermediate1 = new float[64][];
			_coreIntermediate2 = new float[64][];
			for (int k = 0; k < 64; k++)
			{
				_coreEmbeddings[k] = new float[64];
				_coreEncoded[k] = new float[64];
				_coreInputs[k] = new float[15];
				_coreIntermediate1[k] = new float[64];
				_coreIntermediate2[k] = new float[64];
			}
			_batchFeatureHash = 0;
			_batchCacheValid = false;
			_history = new CircularBuffer<DecisionRecord>(10000);
			_stats = new SchedulerStatistics(64);
			_currentRecord = new DecisionRecord(64);
			_threadFeatureMean = new float[6];
			_threadFeatureStd = new float[6];
			_coreFeatureMean = new float[7];
			_coreFeatureStd = new float[7];
			_threadFeatureWindow = new float[1000][];
			_coreFeatureWindow = new float[1000][];
			for (int l = 0; l < 1000; l++)
			{
				_threadFeatureWindow[l] = new float[8];
				_coreFeatureWindow[l] = new float[9];
			}
			_windowIndex = 0;
			_windowStartTick = DateTime.Now.Ticks;
			_normalizationReady = false;
			for (int m = 0; m < 6; m++)
			{
				_threadFeatureStd[m] = 1f;
			}
			for (int n = 0; n < 7; n++)
			{
				_coreFeatureStd[n] = 1f;
			}
			_coreIdEmbedding = new float[256];
			MathHelper.InitEmbeddingOrthogonal(_coreIdEmbedding, 64, 4);
			_coreTypeEmbedding = new float[28];
			MathHelper.InitEmbeddingOrthogonal(_coreTypeEmbedding, 7, 4);
			_coreIdEmbeddingGrad = new float[256];
			_coreTypeEmbeddingGrad = new float[28];
			_tatHistory = new float[1000];
			_tatIndex = 0;
			_sumTAT = 0f;
			_lastTAT = 0f;
			_baselineTAT = 0f;
			_baselineEnergy = 0f;
			_baselineExtraMetric = 0f;
			_baselineLoadBalance = 0f;
			_baselineLoadBalance2 = 0f;
			_startTick = DateTime.Now.Ticks;
			_lastBatchTrainTick = _startTick;
			_gradCrossOutput = new float[64];
			_gradThreadEncoded = new float[64];
			_gradThreadEmbed = new float[64];
			_learningWeights = new float[10000];
			_reportMaxCores = new int[4];
			_reportMaxWeights = new float[4];
			_lastAttentionWeightsBuffer = new float[64];
			_normThreadBuf = new float[14];
			_normCoreBuf = new float[15];
			_normCoreBwdBuf = new float[15];
			_gradCoreEncodedBuf = new float[64];
			_gradCrossKVPerCore = new float[64][];
			for (int num = 0; num < 64; num++)
			{
				_gradCrossKVPerCore[num] = new float[64];
			}
			_coreEmbedWeightGradAccum = new float[960];
			_coreEmbedBiasGradAccum = new float[64];
		}

		public TransformerScheduler(string modelPath)
		{
			_threadEmbedding = new LinearLayer(14, 64);
			_coreEmbedding = new LinearLayer(15, 64);
			_threadEncoder = new ThreadFFNBlock();
			_coreEncoders = new CoreTransformerEncoder[3];
			for (int i = 0; i < 3; i++)
			{
				_coreEncoders[i] = new CoreTransformerEncoder();
			}
			_crossAttentions = new MultiHeadAttention[2];
			for (int j = 0; j < 2; j++)
			{
				_crossAttentions[j] = new MultiHeadAttention(64, 4);
			}
			_threadEmbed = new float[64];
			_threadEncoded = new float[64];
			_crossIntermediate = new float[64];
			_crossOutput = new float[64];
			_coreScore = new float[64];
			_attentionWeights = new float[64];
			_coreEmbeddings = new float[64][];
			_coreEncoded = new float[64][];
			_coreInputs = new float[64][];
			_coreIntermediate1 = new float[64][];
			_coreIntermediate2 = new float[64][];
			for (int k = 0; k < 64; k++)
			{
				_coreEmbeddings[k] = new float[64];
				_coreEncoded[k] = new float[64];
				_coreInputs[k] = new float[15];
				_coreIntermediate1[k] = new float[64];
				_coreIntermediate2[k] = new float[64];
			}
			_batchFeatureHash = 0;
			_batchCacheValid = false;
			_history = new CircularBuffer<DecisionRecord>(10000);
			_stats = new SchedulerStatistics(64);
			_currentRecord = new DecisionRecord(64);
			_threadFeatureMean = new float[6];
			_threadFeatureStd = new float[6];
			_coreFeatureMean = new float[7];
			_coreFeatureStd = new float[7];
			_threadFeatureWindow = new float[1000][];
			_coreFeatureWindow = new float[1000][];
			for (int l = 0; l < 1000; l++)
			{
				_threadFeatureWindow[l] = new float[8];
				_coreFeatureWindow[l] = new float[9];
			}
			_windowIndex = 0;
			_windowStartTick = DateTime.Now.Ticks;
			_normalizationReady = false;
			for (int m = 0; m < 6; m++)
			{
				_threadFeatureStd[m] = 1f;
			}
			for (int n = 0; n < 7; n++)
			{
				_coreFeatureStd[n] = 1f;
			}
			_coreIdEmbedding = new float[256];
			MathHelper.InitEmbeddingOrthogonal(_coreIdEmbedding, 64, 4);
			_coreTypeEmbedding = new float[28];
			MathHelper.InitEmbeddingOrthogonal(_coreTypeEmbedding, 7, 4);
			_coreIdEmbeddingGrad = new float[256];
			_coreTypeEmbeddingGrad = new float[28];
			_tatHistory = new float[1000];
			_tatIndex = 0;
			_sumTAT = 0f;
			_lastTAT = 0f;
			_baselineTAT = 0f;
			_baselineEnergy = 0f;
			_baselineExtraMetric = 0f;
			_baselineLoadBalance = 0f;
			_baselineLoadBalance2 = 0f;
			_startTick = DateTime.Now.Ticks;
			_lastBatchTrainTick = _startTick;
			_gradCrossOutput = new float[64];
			_gradThreadEncoded = new float[64];
			_gradThreadEmbed = new float[64];
			_learningWeights = new float[10000];
			_reportMaxCores = new int[4];
			_reportMaxWeights = new float[4];
			_lastAttentionWeightsBuffer = new float[64];
			_normThreadBuf = new float[14];
			_normCoreBuf = new float[15];
			_normCoreBwdBuf = new float[15];
			_gradCoreEncodedBuf = new float[64];
			_gradCrossKVPerCore = new float[64][];
			for (int num = 0; num < 64; num++)
			{
				_gradCrossKVPerCore[num] = new float[64];
			}
			_coreEmbedWeightGradAccum = new float[960];
			_coreEmbedBiasGradAccum = new float[64];
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
			int num = coreId * 4;
			for (int i = 0; i < 4; i++)
			{
				destination[i] = _coreIdEmbedding[num + i];
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void CopyCoreTypeEmbedding(int coreType, Span<float> destination)
		{
			if (coreType < 0 || coreType >= 7)
			{
				coreType = 0;
			}
			int num = coreType * 4;
			for (int i = 0; i < 4; i++)
			{
				destination[i] = _coreTypeEmbedding[num + i];
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void BuildThreadInput(ReadOnlySpan<float> threadFeatures, Span<float> output)
		{
			NormalizeFeatures(threadFeatures.Slice(0, 6), output.Slice(0, 6), _threadFeatureMean, _threadFeatureStd);
			int coreId = (int)threadFeatures[6];
			CopyCoreIdEmbedding(coreId, output.Slice(6));
			int coreType = (int)threadFeatures[7];
			CopyCoreTypeEmbedding(coreType, output.Slice(10));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void BuildCoreInput(ReadOnlySpan<float> coreFeatures, Span<float> output)
		{
			NormalizeFeatures(coreFeatures.Slice(0, 7), output.Slice(0, 7), _coreFeatureMean, _coreFeatureStd);
			int coreId = (int)coreFeatures[7];
			CopyCoreIdEmbedding(coreId, output.Slice(7));
			int coreType = (int)coreFeatures[8];
			CopyCoreTypeEmbedding(coreType, output.Slice(11));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void ApplyEmbeddingGradients(float[] embedding, float[] gradBuffer, float learningRate)
		{
			float num = 0f;
			for (int i = 0; i < gradBuffer.Length; i++)
			{
				num += gradBuffer[i] * gradBuffer[i];
			}
			float num2 = (float)Math.Sqrt(num);
			float num3 = ((num2 > 1f) ? (1f / num2) : 1f);
			for (int j = 0; j < embedding.Length; j++)
			{
				float num4 = gradBuffer[j] * num3;
				embedding[j] -= learningRate * (num4 + 0.0001f * embedding[j]);
			}
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
			MathHelper.Clear(_threadFeatureMean.AsSpan(0, 6));
			MathHelper.Clear(_coreFeatureMean.AsSpan(0, 7));
			MathHelper.Clear(_threadFeatureStd.AsSpan(0, 6));
			MathHelper.Clear(_coreFeatureStd.AsSpan(0, 7));
			for (int i = 0; i < _windowIndex; i++)
			{
				for (int j = 0; j < 6; j++)
				{
					_threadFeatureMean[j] += _threadFeatureWindow[i][j];
				}
				for (int k = 0; k < 7; k++)
				{
					_coreFeatureMean[k] += _coreFeatureWindow[i][k];
				}
			}
			float num = 1f / (float)_windowIndex;
			for (int l = 0; l < 6; l++)
			{
				_threadFeatureMean[l] *= num;
			}
			for (int m = 0; m < 7; m++)
			{
				_coreFeatureMean[m] *= num;
			}
			for (int n = 0; n < _windowIndex; n++)
			{
				for (int num2 = 0; num2 < 6; num2++)
				{
					float num3 = _threadFeatureWindow[n][num2] - _threadFeatureMean[num2];
					_threadFeatureStd[num2] += num3 * num3;
				}
				for (int num4 = 0; num4 < 7; num4++)
				{
					float num5 = _coreFeatureWindow[n][num4] - _coreFeatureMean[num4];
					_coreFeatureStd[num4] += num5 * num5;
				}
			}
			for (int num6 = 0; num6 < 6; num6++)
			{
				_threadFeatureStd[num6] = (float)Math.Sqrt(_threadFeatureStd[num6] / (float)_windowIndex + 1E-06f);
			}
			for (int num7 = 0; num7 < 7; num7++)
			{
				_coreFeatureStd[num7] = (float)Math.Sqrt(_coreFeatureStd[num7] / (float)_windowIndex + 1E-06f);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void UpdateCoreEmbedding(ReadOnlySpan<float> coreFeatures, int coreIndex, Span<float> normalized, Span<float> encodedOutput)
		{
			BuildCoreInput(coreFeatures, normalized);
			_coreEmbedding.Forward(normalized, _coreEmbeddings[coreIndex]);
			_coreEncoders[0].Forward(_coreEmbeddings[coreIndex], _coreIntermediate1[coreIndex]);
			_coreEncoders[1].Forward(_coreIntermediate1[coreIndex], _coreIntermediate2[coreIndex]);
			_coreEncoders[2].Forward(_coreIntermediate2[coreIndex], encodedOutput);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ScheduleResult Schedule(ReadOnlySpan<float> threadFeatures, float[][] coreFeatures, int numCores)
		{
			return Schedule(threadFeatures, coreFeatures, numCores, -1, -1, -1);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int Schedule(ReadOnlySpan<float> threadFeatures, float[][] coreFeatures, int numCores, int predictedCore, int actualCore)
		{
			return Schedule(threadFeatures, coreFeatures, numCores, -1, predictedCore, actualCore).BestCoreIndex;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ScheduleResult Schedule(ReadOnlySpan<float> threadFeatures, float[][] coreFeatures, int numCores, int threadId, int predictedCore, int actualCore)
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
			long frequency = Stopwatch.Frequency;
			UpdateNormalizationFixedWindow(threadFeatures, coreFeatures, numCores);
			Stopwatch stopwatch2 = Stopwatch.StartNew();
			BuildThreadInput(threadFeatures, _normThreadBuf);
			_threadEmbedding.Forward(_normThreadBuf, _threadEmbed);
			_threadEncoder.Forward(_threadEmbed, _threadEncoded);
			stopwatch2.Stop();
			long num = 0L;
			int num2 = 0;
			for (int j = 0; j < numCores; j++)
			{
				ReadOnlySpan<float> features = new ReadOnlySpan<float>(coreFeatures[j], 0, 9);
				num2 = num2 * 31 + ComputeFeatureHash(features);
			}
			if (_batchFeatureHash != num2 || !_batchCacheValid)
			{
				Stopwatch stopwatch3 = Stopwatch.StartNew();
				for (int k = 0; k < numCores; k++)
				{
					ReadOnlySpan<float> coreFeatures2 = new ReadOnlySpan<float>(coreFeatures[k], 0, 9);
					BuildCoreInput(coreFeatures2, _coreInputs[k]);
					_coreEmbedding.Forward(_coreInputs[k], _coreEmbeddings[k]);
				}
				_coreEncoders[0].ForwardBatch(_coreEmbeddings, numCores, _coreIntermediate1);
				_coreEncoders[1].ForwardBatch(_coreIntermediate1, numCores, _coreIntermediate2);
				_coreEncoders[2].ForwardBatch(_coreIntermediate2, numCores, _coreEncoded);
				stopwatch3.Stop();
				num = stopwatch3.ElapsedTicks;
				_batchFeatureHash = num2;
				_batchCacheValid = true;
				_stats.CacheMisses++;
			}
			else
			{
				_stats.CacheHits++;
			}
			Stopwatch stopwatch4 = Stopwatch.StartNew();
			long num3 = (DateTime.Now.Ticks - _startTick) / 10000;
			float softmaxTemperature = 1f;
			if (num3 < 120000)
			{
				softmaxTemperature = 1f + 1f * (1f - (float)num3 / 120000f);
			}
			_crossAttentions[0].CrossAttention(_threadEncoded, _coreEncoded, _coreEncoded, _crossIntermediate, numCores);
			Span<float> attentionWeights = _attentionWeights.AsSpan(0, numCores);
			_crossAttentions[1].CrossAttention(_crossIntermediate, _coreEncoded, _coreEncoded, _crossOutput, numCores, attentionWeights, softmaxTemperature);
			stopwatch4.Stop();
			Span<float> span = _coreScore.AsSpan(0, numCores);
			attentionWeights.CopyTo(span);
			float num4 = VectorMathNew.Sum(span);
			SelectTopK(span, _topK, out var bestCoreIndex, out var affinityMask);
			if (num4 > 0f)
			{
				MathHelper.Scale(span, 1f / num4);
			}
			long num5 = (DateTime.Now.Ticks - _startTick) / 10000 / 60000;
			_explorationRate = 0f;
			_ = _stats.PositiveRewardRatio;
			float currentLearningRate = ((num5 < 10) ? 0.01f : ((num5 < 17) ? 0.003f : ((num5 >= 24) ? 0.0001f : 0.001f)));
			_currentLearningRate = currentLearningRate;
			long ticks = DateTime.Now.Ticks;
			if (ticks - _lastTATUpdateTick >= 10000000)
			{
				_decisionsInCurrentSecond = 0;
				_lastTATUpdateTick = ticks;
			}
			_decisionsInCurrentSecond++;
			RecordDecision(threadFeatures, coreFeatures, numCores, bestCoreIndex, predictedCore, actualCore);
			stopwatch.Stop();
			_stats.TotalDecisions++;
			long num6 = stopwatch.ElapsedTicks * 1000000 / frequency;
			_stats.TotalInferenceTimeUs += num6;
			_stats.TotalThreadEncodeTimeUs += stopwatch2.ElapsedTicks * 1000000 / frequency;
			_stats.TotalCoreEncodeTimeUs += num * 1000000 / frequency;
			_stats.TotalCrossAttnTimeUs += stopwatch4.ElapsedTicks * 1000000 / frequency;
			_stats.CoreSelectionCounts[bestCoreIndex]++;
			return new ScheduleResult
			{
				BestCoreIndex = bestCoreIndex,
				AffinityMask = affinityMask
			};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void UpdateTAT(float currentTAT, float energyValue, float newMetricValue, float extraMetricValue, int little_num, int big_num)
		{
			UpdateTATInternal(currentTAT, energyValue, newMetricValue, extraMetricValue, little_num, big_num);
		}

		private void UpdateTATInternal(float currentTAT, float energyValue, float newMetricValue, float extraMetricValue, int little_num, int big_num)
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
			_baselineTAT = 16f;
			if (_baselineTAT > 0.001f)
			{
				num3 = 1f - (_baselineTAT - currentTAT) * (_baselineTAT - currentTAT) / (_baselineTAT * _baselineTAT);
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
			_baselineEnergy = (float)little_num / (float)(little_num + big_num);
			float num4 = 0f;
			if (_baselineEnergy > 0.001f)
			{
				num4 = (energyValue - _baselineEnergy) / _baselineEnergy;
				num4 = ((num4 > 1f) ? 1f : ((num4 < -1f) ? (-1f) : num4));
			}
			if (_stats.TotalTATSamples >= 10)
			{
				if (_baselineNewMetric < 0.001f)
				{
					_baselineNewMetric = newMetricValue;
				}
				else
				{
					_baselineNewMetric = _baselineNewMetric * 0.95f + newMetricValue * 0.05f;
				}
			}
			float num5 = 0f;
			if (_baselineNewMetric > 0.001f)
			{
				num5 = (newMetricValue - _baselineNewMetric) / _baselineNewMetric;
				num5 = ((num5 > 1f) ? 1f : ((num5 < -1f) ? (-1f) : num5));
			}
			if (_stats.TotalTATSamples >= 10)
			{
				if (_baselineExtraMetric < 0.001f)
				{
					_baselineExtraMetric = extraMetricValue;
				}
				else
				{
					_baselineExtraMetric = _baselineExtraMetric * 0.95f + extraMetricValue * 0.05f;
				}
			}
			float num6 = 0f;
			if (_baselineExtraMetric > 0.001f)
			{
				num6 = (extraMetricValue - _baselineExtraMetric) / _baselineExtraMetric;
				num6 = ((num6 > 1f) ? 1f : ((num6 < -1f) ? (-1f) : num6));
			}
			float num7 = 0f;
			float num8 = 0f;
			if (LOAD_BALANCE_WEIGHT > 0f && _currentRecord.NumCores > 0)
			{
				num8 = ComputeLoadBalance(_currentRecord.CoreFeatures, _currentRecord.NumCores);
				if (_stats.TotalTATSamples >= 10)
				{
					if (_baselineLoadBalance < 0.001f)
					{
						_baselineLoadBalance = num8;
					}
					else
					{
						_baselineLoadBalance = _baselineLoadBalance * 0.95f + num8 * 0.05f;
					}
				}
				if (_baselineLoadBalance > 0.001f)
				{
					num7 = (_baselineLoadBalance - num8) / _baselineLoadBalance;
					num7 = ((num7 > 1f) ? 1f : ((num7 < -1f) ? (-1f) : num7));
				}
				_stats.LastLoadBalance = num8;
				_stats.TotalLoadBalanceSamples++;
				_stats.AvgLoadBalance = (_stats.AvgLoadBalance * (float)(_stats.TotalLoadBalanceSamples - 1) + num8) / (float)_stats.TotalLoadBalanceSamples;
				if (_stats.TotalLoadBalanceSamples == 1 || num8 < _stats.MinLoadBalance)
				{
					_stats.MinLoadBalance = num8;
				}
				if (_stats.TotalLoadBalanceSamples == 1 || num8 > _stats.MaxLoadBalance)
				{
					_stats.MaxLoadBalance = num8;
				}
				_stats.BaselineLoadBalance = _baselineLoadBalance;
				_stats.RecordLoadBalance(num8);
			}
			float num9 = 0f;
			float num10 = 0f;
			if (LOAD_BALANCE2_WEIGHT > 0f && _currentRecord.NumCores > 0)
			{
				num10 = ComputeLoadBalance2(_currentRecord.CoreFeatures, _currentRecord.NumCores);
				if (_stats.TotalTATSamples >= 10)
				{
					if (_baselineLoadBalance2 < 0.001f)
					{
						_baselineLoadBalance2 = num10;
					}
					else
					{
						_baselineLoadBalance2 = _baselineLoadBalance2 * 0.95f + num10 * 0.05f;
					}
				}
				if (_baselineLoadBalance2 > 0.001f)
				{
					num9 = (_baselineLoadBalance2 - num10) / _baselineLoadBalance2;
					num9 = ((num9 > 1f) ? 1f : ((num9 < -1f) ? (-1f) : num9));
				}
				_stats.LastLoadBalance2 = num10;
				_stats.TotalLoadBalance2Samples++;
				_stats.AvgLoadBalance2 = (_stats.AvgLoadBalance2 * (float)(_stats.TotalLoadBalance2Samples - 1) + num10) / (float)_stats.TotalLoadBalance2Samples;
				if (_stats.TotalLoadBalance2Samples == 1 || num10 < _stats.MinLoadBalance2)
				{
					_stats.MinLoadBalance2 = num10;
				}
				if (_stats.TotalLoadBalance2Samples == 1 || num10 > _stats.MaxLoadBalance2)
				{
					_stats.MaxLoadBalance2 = num10;
				}
				_stats.BaselineLoadBalance2 = _baselineLoadBalance2;
				_stats.RecordLoadBalance2(num10);
			}
			long num11 = (DateTime.Now.Ticks - _startTick) / 600000000;
			if (num11 >= 0 && num11 < 18)
			{
				LOAD_BALANCE_WEIGHT = 0f;
				LOAD_BALANCE2_WEIGHT = 0f;
				ENERGY_WEIGHT = 0f;
				NEW_METRIC_WEIGHT = 1f;
				EXTRA_METRIC_WEIGHT = 0f;
			}
			else
			{
				LOAD_BALANCE_WEIGHT = 0f;
				LOAD_BALANCE2_WEIGHT = 0f;
				ENERGY_WEIGHT = 0f;
				NEW_METRIC_WEIGHT = 1f;
				EXTRA_METRIC_WEIGHT = 0f;
			}
			float num12 = num3 * (1f - ENERGY_WEIGHT - LOAD_BALANCE_WEIGHT - LOAD_BALANCE2_WEIGHT - NEW_METRIC_WEIGHT - EXTRA_METRIC_WEIGHT) + num4 * ENERGY_WEIGHT + num7 * LOAD_BALANCE_WEIGHT + num9 * LOAD_BALANCE2_WEIGHT + num5 * NEW_METRIC_WEIGHT + num6 * EXTRA_METRIC_WEIGHT;
			_stats.RecordReward(num12);
			_stats.RecentAvgReward = _stats.RecentAvgReward * 0.95f + num12 * 0.05f;
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
			_stats.LastNewMetric = newMetricValue;
			_stats.TotalNewMetricSamples++;
			_stats.AvgNewMetric = (_stats.AvgNewMetric * (float)(_stats.TotalNewMetricSamples - 1) + newMetricValue) / (float)_stats.TotalNewMetricSamples;
			if (_stats.TotalNewMetricSamples == 1 || newMetricValue < _stats.MinNewMetric)
			{
				_stats.MinNewMetric = newMetricValue;
			}
			if (_stats.TotalNewMetricSamples == 1 || newMetricValue > _stats.MaxNewMetric)
			{
				_stats.MaxNewMetric = newMetricValue;
			}
			_stats.BaselineNewMetric = _baselineNewMetric;
			_stats.RecordNewMetric(newMetricValue);
			_stats.LastExtraMetric = extraMetricValue;
			_stats.TotalExtraMetricSamples++;
			_stats.AvgExtraMetric = (_stats.AvgExtraMetric * (float)(_stats.TotalExtraMetricSamples - 1) + extraMetricValue) / (float)_stats.TotalExtraMetricSamples;
			if (_stats.TotalExtraMetricSamples == 1 || extraMetricValue < _stats.MinExtraMetric)
			{
				_stats.MinExtraMetric = extraMetricValue;
			}
			if (_stats.TotalExtraMetricSamples == 1 || extraMetricValue > _stats.MaxExtraMetric)
			{
				_stats.MaxExtraMetric = extraMetricValue;
			}
			_stats.BaselineExtraMetric = _baselineExtraMetric;
			_stats.RecordExtraMetric(extraMetricValue);
			bool flag = num < 600000;
			if ((flag || !(currentTAT < 1f)) && !flag)
			{
				_ = 2f;
			}
			int num13 = Math.Min(_decisionsInCurrentSecond, _history.Count);
			int num14 = _history.Count - num13;
			float currentLearningRate = _currentLearningRate;
			float num15 = 0f;
			int num16 = Math.Min(num13, 10000);
			for (int i = 0; i < num16; i++)
			{
				_learningWeights[i] = (float)Math.Exp(-0.5f * (float)(num16 - 1 - i));
				num15 += _learningWeights[i];
			}
			float num17 = 0f;
			if (!_learningEnabled)
			{
				_stats.ExperienceSkipped += num16;
				return;
			}
			for (int j = 0; j < num16; j++)
			{
				int num18 = num14 + j;
				if (num18 < 0 || num18 >= _history.Count)
				{
					continue;
				}
				DecisionRecord decisionRecord = _history.Get(num18);
				if (!flag && !decisionRecord.IsValid)
				{
					_stats.ExperienceSkipped++;
					continue;
				}
				float num19 = num3 * (_learningWeights[j] / num15);
				num17 += Math.Abs(num19);
				BuildThreadInput(decisionRecord.ThreadFeatures, _normThreadBuf);
				_threadEmbedding.Forward(_normThreadBuf, _threadEmbed);
				_threadEncoder.Forward(_threadEmbed, _threadEncoded);
				for (int k = 0; k < decisionRecord.NumCores; k++)
				{
					ReadOnlySpan<float> coreFeatures = new ReadOnlySpan<float>(decisionRecord.CoreFeatures[k], 0, 9);
					BuildCoreInput(coreFeatures, _coreInputs[k]);
					_coreEmbedding.Forward(_coreInputs[k], _coreEmbeddings[k]);
				}
				_coreEncoders[0].ForwardBatch(_coreEmbeddings, decisionRecord.NumCores, _coreIntermediate1);
				_coreEncoders[1].ForwardBatch(_coreIntermediate1, decisionRecord.NumCores, _coreIntermediate2);
				_coreEncoders[2].ForwardBatch(_coreIntermediate2, decisionRecord.NumCores, _coreEncoded);
				_crossAttentions[0].CrossAttention(_threadEncoded, _coreEncoded, _coreEncoded, _crossIntermediate, decisionRecord.NumCores);
				Span<float> attentionWeights = _attentionWeights.AsSpan(0, decisionRecord.NumCores);
				_crossAttentions[1].CrossAttention(_crossIntermediate, _coreEncoded, _coreEncoded, _crossOutput, decisionRecord.NumCores, attentionWeights);
				Array.Clear(_coreIdEmbeddingGrad, 0, _coreIdEmbeddingGrad.Length);
				Array.Clear(_coreTypeEmbeddingGrad, 0, _coreTypeEmbeddingGrad.Length);
				for (int l = 0; l < 64; l++)
				{
					_gradCrossOutput[l] = num19 * _crossOutput[l];
				}
				_crossAttentions[1].Backward(_gradCrossOutput, currentLearningRate);
				float[] queryGradients = _crossAttentions[1].GetQueryGradients();
				float[] valueGradients = _crossAttentions[1].GetValueGradients();
				float[] keyGradients = _crossAttentions[1].GetKeyGradients();
				_crossAttentions[0].Backward(queryGradients, currentLearningRate);
				float[] queryGradients2 = _crossAttentions[0].GetQueryGradients();
				float[] valueGradients2 = _crossAttentions[0].GetValueGradients();
				float[] keyGradients2 = _crossAttentions[0].GetKeyGradients();
				_threadEncoder.Backward(queryGradients2, currentLearningRate);
				float[] inputGradients = _threadEncoder.InputGradients;
				_threadEmbedding.Backward(inputGradients, currentLearningRate);
				float[] inputGrads = _threadEmbedding.InputGrads;
				int num20 = (int)decisionRecord.ThreadFeatures[6];
				if (num20 >= 0 && num20 < 64)
				{
					int num21 = num20 * 4;
					for (int m = 0; m < 4; m++)
					{
						_coreIdEmbeddingGrad[num21 + m] += inputGrads[6 + m];
					}
				}
				int num22 = (int)decisionRecord.ThreadFeatures[7];
				if (num22 >= 0 && num22 < 7)
				{
					int num23 = num22 * 4;
					for (int n = 0; n < 4; n++)
					{
						_coreTypeEmbeddingGrad[num23 + n] += inputGrads[10 + n];
					}
				}
				int cachedNumCores = _crossAttentions[1].GetCachedNumCores();
				for (int num24 = 0; num24 < cachedNumCores; num24++)
				{
					for (int num25 = 0; num25 < 64; num25++)
					{
						_gradCrossKVPerCore[num24][num25] = keyGradients2[num24 * 64 + num25] + valueGradients2[num24 * 64 + num25] + keyGradients[num24 * 64 + num25] + valueGradients[num24 * 64 + num25];
					}
				}
				_coreEncoders[2].BackwardBatch(_gradCrossKVPerCore, cachedNumCores, currentLearningRate);
				float[][] batchInputGradients = _coreEncoders[2].GetBatchInputGradients();
				_coreEncoders[1].BackwardBatch(batchInputGradients, cachedNumCores, currentLearningRate);
				float[][] batchInputGradients2 = _coreEncoders[1].GetBatchInputGradients();
				_coreEncoders[0].BackwardBatch(batchInputGradients2, cachedNumCores, currentLearningRate);
				float[][] batchInputGradients3 = _coreEncoders[0].GetBatchInputGradients();
				Array.Clear(_coreEmbedWeightGradAccum, 0, _coreEmbedWeightGradAccum.Length);
				Array.Clear(_coreEmbedBiasGradAccum, 0, _coreEmbedBiasGradAccum.Length);
				for (int num26 = 0; num26 < cachedNumCores; num26++)
				{
					ReadOnlySpan<float> coreFeatures2 = new ReadOnlySpan<float>(decisionRecord.CoreFeatures[num26], 0, 9);
					BuildCoreInput(coreFeatures2, _normCoreBwdBuf);
					_coreEmbedding.Forward(_normCoreBwdBuf, _coreEmbeddings[num26]);
					float[] array = batchInputGradients3[num26];
					_coreEmbedding.Backward(array, currentLearningRate);
					float[] weightGrads = _coreEmbedding.WeightGrads;
					for (int num27 = 0; num27 < _coreEmbedWeightGradAccum.Length; num27++)
					{
						_coreEmbedWeightGradAccum[num27] += weightGrads[num27];
					}
					float[] biasGrads = _coreEmbedding.BiasGrads;
					for (int num28 = 0; num28 < _coreEmbedBiasGradAccum.Length; num28++)
					{
						_coreEmbedBiasGradAccum[num28] += biasGrads[num28];
					}
					float[] inputGrads2 = _coreEmbedding.InputGrads;
					int num29 = (int)decisionRecord.CoreFeatures[num26][7];
					if (num29 >= 0 && num29 < 64)
					{
						int num30 = num29 * 4;
						for (int num31 = 0; num31 < 4; num31++)
						{
							_coreIdEmbeddingGrad[num30 + num31] += inputGrads2[7 + num31];
						}
					}
					int num32 = (int)decisionRecord.CoreFeatures[num26][8];
					if (num32 >= 0 && num32 < 7)
					{
						int num33 = num32 * 4;
						for (int num34 = 0; num34 < 4; num34++)
						{
							_coreTypeEmbeddingGrad[num33 + num34] += inputGrads2[11 + num34];
						}
					}
				}
				Array.Copy(_coreEmbedWeightGradAccum, _coreEmbedding.WeightGrads, _coreEmbedWeightGradAccum.Length);
				Array.Copy(_coreEmbedBiasGradAccum, _coreEmbedding.BiasGrads, _coreEmbedBiasGradAccum.Length);
				_threadEmbedding.ApplyGradientsSGD(currentLearningRate);
				_coreEmbedding.ApplyGradientsSGD(currentLearningRate);
				for (int num35 = 0; num35 < 2; num35++)
				{
					_crossAttentions[num35].ApplyGradients();
				}
				_threadEncoder.ApplyGradients(currentLearningRate);
				for (int num36 = 0; num36 < 3; num36++)
				{
					_coreEncoders[num36].ApplyGradients(currentLearningRate);
				}
				ApplyEmbeddingGradients(_coreIdEmbedding, _coreIdEmbeddingGrad, currentLearningRate);
				ApplyEmbeddingGradients(_coreTypeEmbedding, _coreTypeEmbeddingGrad, currentLearningRate);
				_stats.ExperienceUsed++;
				_stats.LearningUpdates++;
			}
			_stats.AvgLoss = _stats.AvgLoss * 0.9f + num17 / (float)Math.Max(num16, 1) * 0.1f;
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
				Array.Clear(_coreIdEmbeddingGrad, 0, _coreIdEmbeddingGrad.Length);
				Array.Clear(_coreTypeEmbeddingGrad, 0, _coreTypeEmbeddingGrad.Length);
				BuildThreadInput(decisionRecord.ThreadFeatures, _normThreadBuf);
				_threadEmbedding.Forward(_normThreadBuf, _threadEmbed);
				_threadEncoder.Forward(_threadEmbed, _threadEncoded);
				for (int j = 0; j < decisionRecord.NumCores; j++)
				{
					ReadOnlySpan<float> coreFeatures = new ReadOnlySpan<float>(decisionRecord.CoreFeatures[j], 0, 9);
					BuildCoreInput(coreFeatures, _coreInputs[j]);
					_coreEmbedding.Forward(_coreInputs[j], _coreEmbeddings[j]);
				}
				_coreEncoders[0].ForwardBatch(_coreEmbeddings, decisionRecord.NumCores, _coreIntermediate1);
				_coreEncoders[1].ForwardBatch(_coreIntermediate1, decisionRecord.NumCores, _coreIntermediate2);
				_coreEncoders[2].ForwardBatch(_coreIntermediate2, decisionRecord.NumCores, _coreEncoded);
				_crossAttentions[0].CrossAttention(_threadEncoded, _coreEncoded, _coreEncoded, _crossIntermediate, decisionRecord.NumCores);
				Span<float> attentionWeights = _attentionWeights.AsSpan(0, decisionRecord.NumCores);
				_crossAttentions[1].CrossAttention(_crossIntermediate, _coreEncoded, _coreEncoded, _crossOutput, decisionRecord.NumCores, attentionWeights);
				for (int k = 0; k < 64; k++)
				{
					_gradCrossOutput[k] = reward * _crossOutput[k];
				}
				_crossAttentions[1].Backward(_gradCrossOutput, currentLearningRate);
				float[] queryGradients = _crossAttentions[1].GetQueryGradients();
				float[] valueGradients = _crossAttentions[1].GetValueGradients();
				float[] keyGradients = _crossAttentions[1].GetKeyGradients();
				_crossAttentions[0].Backward(queryGradients, currentLearningRate);
				float[] queryGradients2 = _crossAttentions[0].GetQueryGradients();
				float[] valueGradients2 = _crossAttentions[0].GetValueGradients();
				float[] keyGradients2 = _crossAttentions[0].GetKeyGradients();
				_threadEncoder.Backward(queryGradients2, currentLearningRate);
				float[] inputGradients = _threadEncoder.InputGradients;
				_threadEmbedding.Backward(inputGradients, currentLearningRate);
				float[] inputGrads = _threadEmbedding.InputGrads;
				int num3 = (int)decisionRecord.ThreadFeatures[6];
				if (num3 >= 0 && num3 < 64)
				{
					int num4 = num3 * 4;
					for (int l = 0; l < 4; l++)
					{
						_coreIdEmbeddingGrad[num4 + l] += inputGrads[6 + l];
					}
				}
				float[] inputGrads2 = _threadEmbedding.InputGrads;
				int num5 = (int)decisionRecord.ThreadFeatures[7];
				if (num5 >= 0 && num5 < 7)
				{
					int num6 = num5 * 4;
					for (int m = 0; m < 4; m++)
					{
						_coreTypeEmbeddingGrad[num6 + m] += inputGrads2[10 + m];
					}
				}
				int cachedNumCores = _crossAttentions[1].GetCachedNumCores();
				for (int n = 0; n < cachedNumCores; n++)
				{
					for (int num7 = 0; num7 < 64; num7++)
					{
						_gradCrossKVPerCore[n][num7] = keyGradients2[n * 64 + num7] + valueGradients2[n * 64 + num7] + keyGradients[n * 64 + num7] + valueGradients[n * 64 + num7];
					}
				}
				_coreEncoders[2].BackwardBatch(_gradCrossKVPerCore, cachedNumCores, currentLearningRate);
				float[][] batchInputGradients = _coreEncoders[2].GetBatchInputGradients();
				_coreEncoders[1].BackwardBatch(batchInputGradients, cachedNumCores, currentLearningRate);
				float[][] batchInputGradients2 = _coreEncoders[1].GetBatchInputGradients();
				_coreEncoders[0].BackwardBatch(batchInputGradients2, cachedNumCores, currentLearningRate);
				float[][] batchInputGradients3 = _coreEncoders[0].GetBatchInputGradients();
				for (int num8 = 0; num8 < cachedNumCores; num8++)
				{
					ReadOnlySpan<float> coreFeatures2 = new ReadOnlySpan<float>(decisionRecord.CoreFeatures[num8], 0, 9);
					BuildCoreInput(coreFeatures2, _normCoreBwdBuf);
					_coreEmbedding.Forward(_normCoreBwdBuf, _coreEmbeddings[num8]);
					float[] array = batchInputGradients3[num8];
					_coreEmbedding.Backward(array, currentLearningRate);
					float[] weightGrads = _coreEmbedding.WeightGrads;
					for (int num9 = 0; num9 < _coreEmbedWeightGradAccum.Length; num9++)
					{
						_coreEmbedWeightGradAccum[num9] += weightGrads[num9];
					}
					float[] biasGrads = _coreEmbedding.BiasGrads;
					for (int num10 = 0; num10 < _coreEmbedBiasGradAccum.Length; num10++)
					{
						_coreEmbedBiasGradAccum[num10] += biasGrads[num10];
					}
					float[] inputGrads3 = _coreEmbedding.InputGrads;
					int num11 = (int)decisionRecord.CoreFeatures[num8][7];
					if (num11 >= 0 && num11 < 64)
					{
						int num12 = num11 * 4;
						for (int num13 = 0; num13 < 4; num13++)
						{
							_coreIdEmbeddingGrad[num12 + num13] += inputGrads3[7 + num13];
						}
					}
					float[] inputGrads4 = _coreEmbedding.InputGrads;
					int num14 = (int)decisionRecord.CoreFeatures[num8][8];
					if (num14 >= 0 && num14 < 7)
					{
						int num15 = num14 * 4;
						for (int num16 = 0; num16 < 4; num16++)
						{
							_coreTypeEmbeddingGrad[num15 + num16] += inputGrads4[11 + num16];
						}
					}
				}
				Array.Copy(_coreEmbedWeightGradAccum, _coreEmbedding.WeightGrads, _coreEmbedWeightGradAccum.Length);
				Array.Copy(_coreEmbedBiasGradAccum, _coreEmbedding.BiasGrads, _coreEmbedBiasGradAccum.Length);
				num2 += Math.Abs(reward);
				_stats.LearningUpdates++;
			}
			_threadEmbedding.ApplyGradientsSGD(currentLearningRate);
			_coreEmbedding.ApplyGradientsSGD(currentLearningRate);
			for (int num17 = 0; num17 < 2; num17++)
			{
				_crossAttentions[num17].ApplyGradients();
			}
			_threadEncoder.ApplyGradients(currentLearningRate);
			for (int num18 = 0; num18 < 3; num18++)
			{
				_coreEncoders[num18].ApplyGradients(currentLearningRate);
			}
			ApplyEmbeddingGradients(_coreIdEmbedding, _coreIdEmbeddingGrad, currentLearningRate);
			ApplyEmbeddingGradients(_coreTypeEmbedding, _coreTypeEmbeddingGrad, currentLearningRate);
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
		private void SelectTopK(ReadOnlySpan<float> scores, int topK, out int bestCoreIndex, out IntPtr affinityMask)
		{
			int length = scores.Length;
			if (length <= 0)
			{
				bestCoreIndex = 0;
				affinityMask = IntPtr.Zero;
				return;
			}
			if (length == 1)
			{
				bestCoreIndex = 0;
				affinityMask = (IntPtr)1L;
				return;
			}
			if (topK >= length)
			{
				topK = length;
			}
			Span<float> span = stackalloc float[length];
			scores.CopyTo(span);
			Span<int> span2 = stackalloc int[topK];
			for (int i = 0; i < topK; i++)
			{
				float num = VectorMathNew.Max(span);
				int num2 = 0;
				for (int j = 0; j < length; j++)
				{
					if (span[j] >= num - 1E-06f)
					{
						num2 = j;
						break;
					}
				}
				span2[i] = num2;
				span[num2] = float.NegativeInfinity;
			}
			bestCoreIndex = span2[0];
			ulong num3 = 0uL;
			for (int k = 0; k < topK; k++)
			{
				num3 |= (ulong)(1L << span2[k]);
			}
			affinityMask = (IntPtr)(long)num3;
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
				num += coreFeatures[i][4];
			}
			float num2 = num / (float)numCores;
			float num3 = 0f;
			for (int j = 0; j < numCores; j++)
			{
				float num4 = coreFeatures[j][4] - num2;
				num3 += num4 * num4;
			}
			return (float)Math.Sqrt(num3 / (float)numCores);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private float ComputeLoadBalance2(float[][] coreFeatures, int numCores)
		{
			if (numCores <= 1)
			{
				return 0f;
			}
			float num = 0f;
			for (int i = 0; i < numCores; i++)
			{
				num += coreFeatures[i][3];
			}
			float num2 = num / (float)numCores;
			float num3 = 0f;
			for (int j = 0; j < numCores; j++)
			{
				float num4 = coreFeatures[j][3] - num2;
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
			stringBuilder.AppendLine($"Model: d_model={64}, n_head={4}, d_ff={256}, core_encoder_layers={3}, cross_attn_layers={2}");
			float num2 = 1f;
			if (num < 120000)
			{
				num2 = 1f + 1f * (1f - (float)num / 120000f);
			}
			stringBuilder.AppendLine(string.Format("Softmax Temperature: {0:F2} {1}", num2, (num < 120000) ? "(annealing)" : "(stable)"));
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
			float[][] headAttentionWeights = _crossAttentions[1].GetHeadAttentionWeights(numCores);
			stringBuilder.AppendLine("╔" + new string('═', 52) + "╗");
			stringBuilder.Append("║ Core   │");
			for (int i = 0; i < 4; i++)
			{
				stringBuilder.Append($"  H{i}    │");
			}
			stringBuilder.AppendLine("  Avg   ║");
			stringBuilder.Append("╟" + new string('─', 8) + "┼");
			for (int j = 0; j < 4; j++)
			{
				stringBuilder.Append(new string('─', 10) + "┼");
			}
			stringBuilder.AppendLine(new string('─', 8) + "╢");
			for (int k = 0; k < 4; k++)
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
				for (int n = 0; n < 4; n++)
				{
					string arg = ((m == _reportMaxCores[n]) ? $"*{headAttentionWeights[n][m]:F4}" : $" {headAttentionWeights[n][m]:F4}");
					stringBuilder.Append($"{arg,-8}│");
				}
				stringBuilder.AppendLine($" {_attentionWeights[m]:F4} ║");
			}
			stringBuilder.Append("╚" + new string('═', 8) + "╧");
			for (int num = 0; num < 4; num++)
			{
				stringBuilder.Append(new string('═', 10) + "╧");
			}
			stringBuilder.AppendLine(new string('═', 8) + "╝");
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("Head Summary:");
			for (int num2 = 0; num2 < 4; num2++)
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
			Array.Copy(threadMean, _threadFeatureMean, 6);
			Array.Copy(threadStd, _threadFeatureStd, 6);
			Array.Copy(coreMean, _coreFeatureMean, 7);
			Array.Copy(coreStd, _coreFeatureStd, 7);
			_normalizationReady = true;
		}

		public string ExportModel()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine($"Model Config: d_model={64}, n_head={4}, d_ff={256}");
			stringBuilder.AppendLine($"Thread Embedding: {14} -> {64}");
			stringBuilder.AppendLine($"Core Embedding: {15} -> {64}");
			stringBuilder.AppendLine($"Total Params: {CountParameters()}");
			return stringBuilder.ToString();
		}

		private int CountParameters()
		{
			int num = 0;
			num += _threadEmbedding.Weights.Length + _threadEmbedding.Bias.Length;
			num += _coreEmbedding.Weights.Length + _coreEmbedding.Bias.Length;
			num += _coreIdEmbedding.Length;
			num += _coreTypeEmbedding.Length;
			for (int i = 0; i < 3; i++)
			{
				CoreTransformerEncoder coreTransformerEncoder = _coreEncoders[i];
				num += coreTransformerEncoder.SelfAttention.Wq.Weights.Length + coreTransformerEncoder.SelfAttention.Wq.Bias.Length;
				num += coreTransformerEncoder.SelfAttention.Wk.Weights.Length + coreTransformerEncoder.SelfAttention.Wk.Bias.Length;
				num += coreTransformerEncoder.SelfAttention.Wv.Weights.Length + coreTransformerEncoder.SelfAttention.Wv.Bias.Length;
				num += coreTransformerEncoder.SelfAttention.Wo.Weights.Length + coreTransformerEncoder.SelfAttention.Wo.Bias.Length;
				num += coreTransformerEncoder.FeedForward.FC1.Weights.Length + coreTransformerEncoder.FeedForward.FC1.Bias.Length;
				num += coreTransformerEncoder.FeedForward.FC2.Weights.Length + coreTransformerEncoder.FeedForward.FC2.Bias.Length;
				num += coreTransformerEncoder.Norm1.Gamma.Length + coreTransformerEncoder.Norm1.Beta.Length;
				num += coreTransformerEncoder.Norm2.Gamma.Length + coreTransformerEncoder.Norm2.Beta.Length;
			}
			for (int j = 0; j < 2; j++)
			{
				num += _crossAttentions[j].Wq.Weights.Length + _crossAttentions[j].Wq.Bias.Length;
				num += _crossAttentions[j].Wk.Weights.Length + _crossAttentions[j].Wk.Bias.Length;
				num += _crossAttentions[j].Wv.Weights.Length + _crossAttentions[j].Wv.Bias.Length;
				num += _crossAttentions[j].Wo.Weights.Length + _crossAttentions[j].Wo.Bias.Length;
			}
			num += _threadEncoder.FeedForward.FC1.Weights.Length + _threadEncoder.FeedForward.FC1.Bias.Length;
			num += _threadEncoder.FeedForward.FC2.Weights.Length + _threadEncoder.FeedForward.FC2.Bias.Length;
			num += _threadEncoder.Norm1.Gamma.Length + _threadEncoder.Norm1.Beta.Length;
			return num + (_threadEncoder.Norm2.Gamma.Length + _threadEncoder.Norm2.Beta.Length);
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
			stringBuilder.AppendLine("--- 微架构可比压力系数（越低越好） ---");
			stringBuilder.AppendLine($"Current value: {0f - _stats.LastNewMetric:F2}");
			stringBuilder.AppendLine($"Baseline value: {0f - _baselineNewMetric:F2}");
			stringBuilder.AppendLine($"Avg value: {0f - _stats.AvgNewMetric:F2}");
			stringBuilder.AppendLine($"Min value: {0f - _stats.MaxNewMetric:F2}");
			stringBuilder.AppendLine($"Max value: {0f - _stats.MinNewMetric:F2}");
			stringBuilder.AppendLine($"Value Trend: {0f - _stats.NewMetricTrend:F3} /step");
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("--- 平均IPC---");
			stringBuilder.AppendLine($"Current value: {_stats.LastExtraMetric:F2}");
			stringBuilder.AppendLine($"Baseline value: {_baselineExtraMetric:F2}");
			stringBuilder.AppendLine($"Avg value: {_stats.AvgExtraMetric:F2}");
			stringBuilder.AppendLine($"Min value: {_stats.MinExtraMetric:F2}");
			stringBuilder.AppendLine($"Max value: {_stats.MaxExtraMetric:F2}");
			stringBuilder.AppendLine($"Value Trend: {_stats.ExtraMetricTrend:F3} /step");
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
			_stats.TotalThreadEncodeTimeUs = 0L;
			_stats.TotalCoreEncodeTimeUs = 0L;
			_stats.TotalCrossAttnTimeUs = 0L;
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
			_stats.TotalNewMetricSamples = 0L;
			_stats.AvgNewMetric = 0f;
			_stats.MinNewMetric = 0f;
			_stats.MaxNewMetric = 0f;
			_stats.BaselineNewMetric = 0f;
			_stats.NewMetricTrend = 0f;
			_stats.LastNewMetric = 0f;
			_stats.TotalExtraMetricSamples = 0L;
			_stats.AvgExtraMetric = 0f;
			_stats.MinExtraMetric = 0f;
			_stats.MaxExtraMetric = 0f;
			_stats.BaselineExtraMetric = 0f;
			_stats.ExtraMetricTrend = 0f;
			_stats.LastExtraMetric = 0f;
			_stats.TotalLoadBalanceSamples = 0L;
			_stats.AvgLoadBalance = 0f;
			_stats.MinLoadBalance = 0f;
			_stats.MaxLoadBalance = 0f;
			_stats.BaselineLoadBalance = 0f;
			_stats.LoadBalanceTrend = 0f;
			_stats.LastLoadBalance = 0f;
			_stats.TotalLoadBalance2Samples = 0L;
			_stats.AvgLoadBalance2 = 0f;
			_stats.MinLoadBalance2 = 0f;
			_stats.MaxLoadBalance2 = 0f;
			_stats.BaselineLoadBalance2 = 0f;
			_stats.LoadBalance2Trend = 0f;
			_stats.LastLoadBalance2 = 0f;
			MathHelper.Clear(_stats.CoreSelectionCounts.AsSpan());
			_baselineTAT = 0f;
			_baselineEnergy = 0f;
			_baselineNewMetric = 0f;
			_baselineExtraMetric = 0f;
			_baselineLoadBalance = 0f;
			_baselineLoadBalance2 = 0f;
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
			binaryWriter.Write(11);
			binaryWriter.Write(64);
			binaryWriter.Write(4);
			binaryWriter.Write(256);
			binaryWriter.Write(64);
			binaryWriter.Write(14);
			binaryWriter.Write(15);
			binaryWriter.Write(3);
			binaryWriter.Write(2);
			WriteLayer(binaryWriter, _threadEmbedding);
			WriteLayer(binaryWriter, _coreEmbedding);
			for (int i = 0; i < 2; i++)
			{
				WriteAttention(binaryWriter, _crossAttentions[i]);
			}
			WriteFFNBlock(binaryWriter, _threadEncoder);
			for (int j = 0; j < 3; j++)
			{
				WriteCoreEncoder(binaryWriter, _coreEncoders[j]);
			}
			for (int k = 0; k < 6; k++)
			{
				binaryWriter.Write(_threadFeatureMean[k]);
			}
			for (int l = 0; l < 6; l++)
			{
				binaryWriter.Write(_threadFeatureStd[l]);
			}
			for (int m = 0; m < 7; m++)
			{
				binaryWriter.Write(_coreFeatureMean[m]);
			}
			for (int n = 0; n < 7; n++)
			{
				binaryWriter.Write(_coreFeatureStd[n]);
			}
			for (int num = 0; num < _coreIdEmbedding.Length; num++)
			{
				binaryWriter.Write(_coreIdEmbedding[num]);
			}
			for (int num2 = 0; num2 < _coreTypeEmbedding.Length; num2++)
			{
				binaryWriter.Write(_coreTypeEmbedding[num2]);
			}
			binaryWriter.Write(_startTick);
			binaryWriter.Write(_baselineExtraMetric);
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
				if (num != 11)
				{
					return false;
				}
				int num2 = binaryReader.ReadInt32();
				int num3 = binaryReader.ReadInt32();
				int num4 = binaryReader.ReadInt32();
				if (num2 != 64 || num3 != 4 || num4 != 256)
				{
					return false;
				}
				int num5 = binaryReader.ReadInt32();
				int num6 = binaryReader.ReadInt32();
				int num7 = binaryReader.ReadInt32();
				int num8 = binaryReader.ReadInt32();
				if (num5 != 64 || num6 != 14 || num7 != 15 || num8 != 3)
				{
					return false;
				}
				if (binaryReader.ReadInt32() != 2)
				{
					return false;
				}
				ReadLayer(binaryReader, _threadEmbedding);
				ReadLayer(binaryReader, _coreEmbedding);
				for (int i = 0; i < 2; i++)
				{
					ReadAttention(binaryReader, _crossAttentions[i]);
				}
				ReadFFNBlock(binaryReader, _threadEncoder);
				for (int j = 0; j < 3; j++)
				{
					ReadCoreEncoder(binaryReader, _coreEncoders[j]);
				}
				for (int k = 0; k < 6; k++)
				{
					_threadFeatureMean[k] = binaryReader.ReadSingle();
				}
				for (int l = 0; l < 6; l++)
				{
					_threadFeatureStd[l] = binaryReader.ReadSingle();
				}
				for (int m = 0; m < 7; m++)
				{
					_coreFeatureMean[m] = binaryReader.ReadSingle();
				}
				for (int n = 0; n < 7; n++)
				{
					_coreFeatureStd[n] = binaryReader.ReadSingle();
				}
				for (int num9 = 0; num9 < _coreIdEmbedding.Length; num9++)
				{
					_coreIdEmbedding[num9] = binaryReader.ReadSingle();
				}
				if (num >= 5)
				{
					for (int num10 = 0; num10 < _coreTypeEmbedding.Length; num10++)
					{
						_coreTypeEmbedding[num10] = binaryReader.ReadSingle();
					}
				}
				if (num >= 2)
				{
					_startTick = binaryReader.ReadInt64();
				}
				if (num >= 11)
				{
					_baselineExtraMetric = binaryReader.ReadSingle();
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

		private void WriteFFNBlock(BinaryWriter writer, ThreadFFNBlock block)
		{
			WriteLayer(writer, block.FeedForward.FC1);
			WriteLayer(writer, block.FeedForward.FC2);
			for (int i = 0; i < block.Norm1.Gamma.Length; i++)
			{
				writer.Write(block.Norm1.Gamma[i]);
			}
			for (int j = 0; j < block.Norm1.Beta.Length; j++)
			{
				writer.Write(block.Norm1.Beta[j]);
			}
			for (int k = 0; k < block.Norm2.Gamma.Length; k++)
			{
				writer.Write(block.Norm2.Gamma[k]);
			}
			for (int l = 0; l < block.Norm2.Beta.Length; l++)
			{
				writer.Write(block.Norm2.Beta[l]);
			}
		}

		private void ReadFFNBlock(BinaryReader reader, ThreadFFNBlock block)
		{
			ReadLayer(reader, block.FeedForward.FC1);
			ReadLayer(reader, block.FeedForward.FC2);
			for (int i = 0; i < block.Norm1.Gamma.Length; i++)
			{
				block.Norm1.Gamma[i] = reader.ReadSingle();
			}
			for (int j = 0; j < block.Norm1.Beta.Length; j++)
			{
				block.Norm1.Beta[j] = reader.ReadSingle();
			}
			for (int k = 0; k < block.Norm2.Gamma.Length; k++)
			{
				block.Norm2.Gamma[k] = reader.ReadSingle();
			}
			for (int l = 0; l < block.Norm2.Beta.Length; l++)
			{
				block.Norm2.Beta[l] = reader.ReadSingle();
			}
		}

		private void WriteCoreEncoder(BinaryWriter writer, CoreTransformerEncoder encoder)
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

		private void ReadCoreEncoder(BinaryReader reader, CoreTransformerEncoder encoder)
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
}
