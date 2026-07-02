using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using SimdLibrary;

namespace IntlThrdPerfSchd
{
	// Token: 0x02000010 RID: 16
	public class TransformerScheduler
	{
		// Token: 0x060000C3 RID: 195 RVA: 0x000086E9 File Offset: 0x000068E9
		public void SetTopK(int k)
		{
			this._topK = Math.Max(1, k);
		}

		// Token: 0x060000C4 RID: 196 RVA: 0x000086F8 File Offset: 0x000068F8
		public int GetTopK()
		{
			return this._topK;
		}

		// Token: 0x060000C5 RID: 197 RVA: 0x00008700 File Offset: 0x00006900
		public void SetLearningEnabled(bool enabled)
		{
			this._learningEnabled = enabled;
		}

		// Token: 0x060000C6 RID: 198 RVA: 0x00008709 File Offset: 0x00006909
		public bool GetLearningEnabled()
		{
			return this._learningEnabled;
		}

		// Token: 0x1700001F RID: 31
		// (get) Token: 0x060000C7 RID: 199 RVA: 0x00008711 File Offset: 0x00006911
		public SchedulerStatistics Statistics
		{
			get
			{
				return this._stats;
			}
		}

		// Token: 0x060000C8 RID: 200 RVA: 0x0000871C File Offset: 0x0000691C
		public TransformerScheduler()
		{
			this._threadEmbedding = new LinearLayer(14, 64);
			this._coreEmbedding = new LinearLayer(15, 64);
			this._threadEncoder = new ThreadFFNBlock();
			this._coreEncoders = new CoreTransformerEncoder[3];
			for (int i = 0; i < 3; i++)
			{
				this._coreEncoders[i] = new CoreTransformerEncoder();
			}
			this._crossAttentions = new MultiHeadAttention[2];
			for (int j = 0; j < 2; j++)
			{
				this._crossAttentions[j] = new MultiHeadAttention(64, 4);
			}
			this._threadEmbed = new float[64];
			this._threadEncoded = new float[64];
			this._crossIntermediate = new float[64];
			this._crossOutput = new float[64];
			this._coreScore = new float[64];
			this._attentionWeights = new float[64];
			this._coreEmbeddings = new float[64][];
			this._coreEncoded = new float[64][];
			this._coreInputs = new float[64][];
			this._coreIntermediate1 = new float[64][];
			this._coreIntermediate2 = new float[64][];
			for (int k = 0; k < 64; k++)
			{
				this._coreEmbeddings[k] = new float[64];
				this._coreEncoded[k] = new float[64];
				this._coreInputs[k] = new float[15];
				this._coreIntermediate1[k] = new float[64];
				this._coreIntermediate2[k] = new float[64];
			}
			this._batchFeatureHash = 0;
			this._batchCacheValid = false;
			this._history = new CircularBuffer<DecisionRecord>(10000);
			this._stats = new SchedulerStatistics(64);
			this._currentRecord = new DecisionRecord(64);
			this._threadFeatureMean = new float[6];
			this._threadFeatureStd = new float[6];
			this._coreFeatureMean = new float[7];
			this._coreFeatureStd = new float[7];
			this._threadFeatureWindow = new float[1000][];
			this._coreFeatureWindow = new float[1000][];
			for (int l = 0; l < 1000; l++)
			{
				this._threadFeatureWindow[l] = new float[8];
				this._coreFeatureWindow[l] = new float[9];
			}
			this._windowIndex = 0;
			this._windowStartTick = DateTime.Now.Ticks;
			this._normalizationReady = false;
			for (int m = 0; m < 6; m++)
			{
				this._threadFeatureStd[m] = 1f;
			}
			for (int n = 0; n < 7; n++)
			{
				this._coreFeatureStd[n] = 1f;
			}
			this._coreIdEmbedding = new float[256];
			MathHelper.InitEmbeddingOrthogonal(this._coreIdEmbedding, 64, 4);
			this._coreTypeEmbedding = new float[28];
			MathHelper.InitEmbeddingOrthogonal(this._coreTypeEmbedding, 7, 4);
			this._coreIdEmbeddingGrad = new float[256];
			this._coreTypeEmbeddingGrad = new float[28];
			this._tatHistory = new float[1000];
			this._tatIndex = 0;
			this._sumTAT = 0f;
			this._lastTAT = 0f;
			this._baselineTAT = 0f;
			this._baselineEnergy = 0f;
			this._baselineExtraMetric = 0f;
			this._baselineLoadBalance = 0f;
			this._baselineLoadBalance2 = 0f;
			this._startTick = DateTime.Now.Ticks;
			this._lastBatchTrainTick = this._startTick;
			this._gradCrossOutput = new float[64];
			this._gradThreadEncoded = new float[64];
			this._gradThreadEmbed = new float[64];
			this._learningWeights = new float[10000];
			this._reportMaxCores = new int[4];
			this._reportMaxWeights = new float[4];
			this._lastAttentionWeightsBuffer = new float[64];
			this._normThreadBuf = new float[14];
			this._normCoreBuf = new float[15];
			this._normCoreBwdBuf = new float[15];
			this._gradCoreEncodedBuf = new float[64];
			this._gradCrossKVPerCore = new float[64][];
			for (int num = 0; num < 64; num++)
			{
				this._gradCrossKVPerCore[num] = new float[64];
			}
			this._coreEmbedWeightGradAccum = new float[960];
			this._coreEmbedBiasGradAccum = new float[64];
		}

		// Token: 0x060000C9 RID: 201 RVA: 0x00008B78 File Offset: 0x00006D78
		public TransformerScheduler(string modelPath)
		{
			this._threadEmbedding = new LinearLayer(14, 64);
			this._coreEmbedding = new LinearLayer(15, 64);
			this._threadEncoder = new ThreadFFNBlock();
			this._coreEncoders = new CoreTransformerEncoder[3];
			for (int i = 0; i < 3; i++)
			{
				this._coreEncoders[i] = new CoreTransformerEncoder();
			}
			this._crossAttentions = new MultiHeadAttention[2];
			for (int j = 0; j < 2; j++)
			{
				this._crossAttentions[j] = new MultiHeadAttention(64, 4);
			}
			this._threadEmbed = new float[64];
			this._threadEncoded = new float[64];
			this._crossIntermediate = new float[64];
			this._crossOutput = new float[64];
			this._coreScore = new float[64];
			this._attentionWeights = new float[64];
			this._coreEmbeddings = new float[64][];
			this._coreEncoded = new float[64][];
			this._coreInputs = new float[64][];
			this._coreIntermediate1 = new float[64][];
			this._coreIntermediate2 = new float[64][];
			for (int k = 0; k < 64; k++)
			{
				this._coreEmbeddings[k] = new float[64];
				this._coreEncoded[k] = new float[64];
				this._coreInputs[k] = new float[15];
				this._coreIntermediate1[k] = new float[64];
				this._coreIntermediate2[k] = new float[64];
			}
			this._batchFeatureHash = 0;
			this._batchCacheValid = false;
			this._history = new CircularBuffer<DecisionRecord>(10000);
			this._stats = new SchedulerStatistics(64);
			this._currentRecord = new DecisionRecord(64);
			this._threadFeatureMean = new float[6];
			this._threadFeatureStd = new float[6];
			this._coreFeatureMean = new float[7];
			this._coreFeatureStd = new float[7];
			this._threadFeatureWindow = new float[1000][];
			this._coreFeatureWindow = new float[1000][];
			for (int l = 0; l < 1000; l++)
			{
				this._threadFeatureWindow[l] = new float[8];
				this._coreFeatureWindow[l] = new float[9];
			}
			this._windowIndex = 0;
			this._windowStartTick = DateTime.Now.Ticks;
			this._normalizationReady = false;
			for (int m = 0; m < 6; m++)
			{
				this._threadFeatureStd[m] = 1f;
			}
			for (int n = 0; n < 7; n++)
			{
				this._coreFeatureStd[n] = 1f;
			}
			this._coreIdEmbedding = new float[256];
			MathHelper.InitEmbeddingOrthogonal(this._coreIdEmbedding, 64, 4);
			this._coreTypeEmbedding = new float[28];
			MathHelper.InitEmbeddingOrthogonal(this._coreTypeEmbedding, 7, 4);
			this._coreIdEmbeddingGrad = new float[256];
			this._coreTypeEmbeddingGrad = new float[28];
			this._tatHistory = new float[1000];
			this._tatIndex = 0;
			this._sumTAT = 0f;
			this._lastTAT = 0f;
			this._baselineTAT = 0f;
			this._baselineEnergy = 0f;
			this._baselineExtraMetric = 0f;
			this._baselineLoadBalance = 0f;
			this._baselineLoadBalance2 = 0f;
			this._startTick = DateTime.Now.Ticks;
			this._lastBatchTrainTick = this._startTick;
			this._gradCrossOutput = new float[64];
			this._gradThreadEncoded = new float[64];
			this._gradThreadEmbed = new float[64];
			this._learningWeights = new float[10000];
			this._reportMaxCores = new int[4];
			this._reportMaxWeights = new float[4];
			this._lastAttentionWeightsBuffer = new float[64];
			this._normThreadBuf = new float[14];
			this._normCoreBuf = new float[15];
			this._normCoreBwdBuf = new float[15];
			this._gradCoreEncodedBuf = new float[64];
			this._gradCrossKVPerCore = new float[64][];
			for (int num = 0; num < 64; num++)
			{
				this._gradCrossKVPerCore[num] = new float[64];
			}
			this._coreEmbedWeightGradAccum = new float[960];
			this._coreEmbedBiasGradAccum = new float[64];
			if (!string.IsNullOrEmpty(modelPath) && File.Exists(modelPath))
			{
				this.LoadModel(modelPath);
			}
		}

		// Token: 0x060000CA RID: 202 RVA: 0x00008FEC File Offset: 0x000071EC
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe int ComputeFeatureHash(ReadOnlySpan<float> features)
		{
			int num = 0;
			for (int i = 0; i < features.Length; i++)
			{
				int num2 = num * 31;
				float num3 = *features[i];
				num = num2 + num3.GetHashCode();
			}
			return num;
		}

		// Token: 0x060000CB RID: 203 RVA: 0x00009028 File Offset: 0x00007228
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe void CopyCoreIdEmbedding(int coreId, Span<float> destination)
		{
			int num = coreId * 4;
			for (int i = 0; i < 4; i++)
			{
				*destination[i] = this._coreIdEmbedding[num + i];
			}
		}

		// Token: 0x060000CC RID: 204 RVA: 0x00009058 File Offset: 0x00007258
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe void CopyCoreTypeEmbedding(int coreType, Span<float> destination)
		{
			if (coreType < 0 || coreType >= 7)
			{
				coreType = 0;
			}
			int num = coreType * 4;
			for (int i = 0; i < 4; i++)
			{
				*destination[i] = this._coreTypeEmbedding[num + i];
			}
		}

		// Token: 0x060000CD RID: 205 RVA: 0x00009094 File Offset: 0x00007294
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe void BuildThreadInput(ReadOnlySpan<float> threadFeatures, Span<float> output)
		{
			this.NormalizeFeatures(threadFeatures.Slice(0, 6), output.Slice(0, 6), this._threadFeatureMean, this._threadFeatureStd);
			int num = (int)(*threadFeatures[6]);
			this.CopyCoreIdEmbedding(num, output.Slice(6));
			int num2 = (int)(*threadFeatures[7]);
			this.CopyCoreTypeEmbedding(num2, output.Slice(10));
		}

		// Token: 0x060000CE RID: 206 RVA: 0x00009104 File Offset: 0x00007304
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe void BuildCoreInput(ReadOnlySpan<float> coreFeatures, Span<float> output)
		{
			this.NormalizeFeatures(coreFeatures.Slice(0, 7), output.Slice(0, 7), this._coreFeatureMean, this._coreFeatureStd);
			int num = (int)(*coreFeatures[7]);
			this.CopyCoreIdEmbedding(num, output.Slice(7));
			int num2 = (int)(*coreFeatures[8]);
			this.CopyCoreTypeEmbedding(num2, output.Slice(11));
		}

		// Token: 0x060000CF RID: 207 RVA: 0x00009174 File Offset: 0x00007374
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void ApplyEmbeddingGradients(float[] embedding, float[] gradBuffer, float learningRate)
		{
			float num = 0f;
			for (int i = 0; i < gradBuffer.Length; i++)
			{
				num += gradBuffer[i] * gradBuffer[i];
			}
			float num2 = (float)Math.Sqrt((double)num);
			float num3 = ((num2 > 1f) ? (1f / num2) : 1f);
			for (int j = 0; j < embedding.Length; j++)
			{
				float num4 = gradBuffer[j] * num3;
				embedding[j] -= learningRate * (num4 + 0.0001f * embedding[j]);
			}
		}

		// Token: 0x060000D0 RID: 208 RVA: 0x000091F4 File Offset: 0x000073F4
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe void NormalizeFeatures(ReadOnlySpan<float> input, Span<float> output, ReadOnlySpan<float> mean, ReadOnlySpan<float> std)
		{
			int num = Math.Min(input.Length, Math.Min(output.Length, Math.Min(mean.Length, std.Length)));
			for (int i = 0; i < num; i++)
			{
				float num2 = ((*std[i] > 0.001f) ? (*std[i]) : 1f);
				*output[i] = (*input[i] - *mean[i]) / num2;
			}
		}

		// Token: 0x060000D1 RID: 209 RVA: 0x00009278 File Offset: 0x00007478
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void UpdateNormalizationFixedWindow(ReadOnlySpan<float> threadFeatures, float[][] coreFeatures, int numCores)
		{
			long ticks = DateTime.Now.Ticks;
			if (ticks - this._windowStartTick >= 600000000L)
			{
				this.ComputeWindowStatistics();
				this._windowStartTick = ticks;
				this._windowIndex = 0;
				this._normalizationReady = true;
			}
			if (this._windowIndex < 1000)
			{
				threadFeatures.Slice(0, 8).CopyTo(this._threadFeatureWindow[this._windowIndex]);
				if (numCores > 0)
				{
					for (int i = 0; i < 9; i++)
					{
						float num = 0f;
						for (int j = 0; j < numCores; j++)
						{
							num += coreFeatures[j][i];
						}
						this._coreFeatureWindow[this._windowIndex][i] = num / (float)numCores;
					}
				}
				this._windowIndex++;
			}
		}

		// Token: 0x060000D2 RID: 210 RVA: 0x00009344 File Offset: 0x00007544
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void ComputeWindowStatistics()
		{
			if (this._windowIndex == 0)
			{
				return;
			}
			MathHelper.Clear(this._threadFeatureMean.AsSpan(0, 6));
			MathHelper.Clear(this._coreFeatureMean.AsSpan(0, 7));
			MathHelper.Clear(this._threadFeatureStd.AsSpan(0, 6));
			MathHelper.Clear(this._coreFeatureStd.AsSpan(0, 7));
			for (int i = 0; i < this._windowIndex; i++)
			{
				for (int j = 0; j < 6; j++)
				{
					this._threadFeatureMean[j] += this._threadFeatureWindow[i][j];
				}
				for (int k = 0; k < 7; k++)
				{
					this._coreFeatureMean[k] += this._coreFeatureWindow[i][k];
				}
			}
			float num = 1f / (float)this._windowIndex;
			for (int l = 0; l < 6; l++)
			{
				this._threadFeatureMean[l] *= num;
			}
			for (int m = 0; m < 7; m++)
			{
				this._coreFeatureMean[m] *= num;
			}
			for (int n = 0; n < this._windowIndex; n++)
			{
				for (int num2 = 0; num2 < 6; num2++)
				{
					float num3 = this._threadFeatureWindow[n][num2] - this._threadFeatureMean[num2];
					this._threadFeatureStd[num2] += num3 * num3;
				}
				for (int num4 = 0; num4 < 7; num4++)
				{
					float num5 = this._coreFeatureWindow[n][num4] - this._coreFeatureMean[num4];
					this._coreFeatureStd[num4] += num5 * num5;
				}
			}
			for (int num6 = 0; num6 < 6; num6++)
			{
				this._threadFeatureStd[num6] = (float)Math.Sqrt((double)(this._threadFeatureStd[num6] / (float)this._windowIndex + 1E-06f));
			}
			for (int num7 = 0; num7 < 7; num7++)
			{
				this._coreFeatureStd[num7] = (float)Math.Sqrt((double)(this._coreFeatureStd[num7] / (float)this._windowIndex + 1E-06f));
			}
		}

		// Token: 0x060000D3 RID: 211 RVA: 0x00009558 File Offset: 0x00007758
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void UpdateCoreEmbedding(ReadOnlySpan<float> coreFeatures, int coreIndex, Span<float> normalized, Span<float> encodedOutput)
		{
			this.BuildCoreInput(coreFeatures, normalized);
			this._coreEmbedding.Forward(normalized, this._coreEmbeddings[coreIndex]);
			this._coreEncoders[0].Forward(this._coreEmbeddings[coreIndex], this._coreIntermediate1[coreIndex]);
			this._coreEncoders[1].Forward(this._coreIntermediate1[coreIndex], this._coreIntermediate2[coreIndex]);
			this._coreEncoders[2].Forward(this._coreIntermediate2[coreIndex], encodedOutput);
		}

		// Token: 0x060000D4 RID: 212 RVA: 0x000095F5 File Offset: 0x000077F5
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ScheduleResult Schedule(ReadOnlySpan<float> threadFeatures, float[][] coreFeatures, int numCores)
		{
			return this.Schedule(threadFeatures, coreFeatures, numCores, -1, -1, -1);
		}

		// Token: 0x060000D5 RID: 213 RVA: 0x00009603 File Offset: 0x00007803
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int Schedule(ReadOnlySpan<float> threadFeatures, float[][] coreFeatures, int numCores, int predictedCore, int actualCore)
		{
			return this.Schedule(threadFeatures, coreFeatures, numCores, -1, predictedCore, actualCore).BestCoreIndex;
		}

		// Token: 0x060000D6 RID: 214 RVA: 0x00009618 File Offset: 0x00007818
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ScheduleResult Schedule(ReadOnlySpan<float> threadFeatures, float[][] coreFeatures, int numCores, int threadId, int predictedCore, int actualCore)
		{
			if (numCores <= 0 || numCores > 64)
			{
				throw new ArgumentOutOfRangeException("numCores", string.Format("numCores={0} must be in range [1, {1}]", numCores, 64));
			}
			if (coreFeatures == null || coreFeatures.Length < numCores)
			{
				throw new ArgumentOutOfRangeException("coreFeatures", string.Format("coreFeatures.Length={0} < numCores={1}", (coreFeatures != null) ? coreFeatures.Length : (-1), numCores));
			}
			for (int i = 0; i < numCores; i++)
			{
				if (coreFeatures[i] == null || coreFeatures[i].Length < 9)
				{
					string text = "coreFeatures";
					string text2 = "coreFeatures[{0}].Length={1} < CORE_INPUT_DIM={2}";
					object obj = i;
					float[] array = coreFeatures[i];
					throw new ArgumentOutOfRangeException(text, string.Format(text2, obj, (array != null) ? array.Length : (-1), 9));
				}
			}
			if (threadFeatures.Length < 8)
			{
				throw new ArgumentOutOfRangeException("threadFeatures", string.Format("threadFeatures.Length={0} < THREAD_INPUT_DIM={1}", threadFeatures.Length, 8));
			}
			Stopwatch stopwatch = Stopwatch.StartNew();
			long frequency = Stopwatch.Frequency;
			this.UpdateNormalizationFixedWindow(threadFeatures, coreFeatures, numCores);
			Stopwatch stopwatch2 = Stopwatch.StartNew();
			this.BuildThreadInput(threadFeatures, this._normThreadBuf);
			this._threadEmbedding.Forward(this._normThreadBuf, this._threadEmbed);
			this._threadEncoder.Forward(this._threadEmbed, this._threadEncoded);
			stopwatch2.Stop();
			long num = 0L;
			int num2 = 0;
			for (int j = 0; j < numCores; j++)
			{
				ReadOnlySpan<float> readOnlySpan = new ReadOnlySpan<float>(coreFeatures[j], 0, 9);
				num2 = num2 * 31 + this.ComputeFeatureHash(readOnlySpan);
			}
			if (this._batchFeatureHash != num2 || !this._batchCacheValid)
			{
				Stopwatch stopwatch3 = Stopwatch.StartNew();
				for (int k = 0; k < numCores; k++)
				{
					ReadOnlySpan<float> readOnlySpan2 = new ReadOnlySpan<float>(coreFeatures[k], 0, 9);
					this.BuildCoreInput(readOnlySpan2, this._coreInputs[k]);
					this._coreEmbedding.Forward(this._coreInputs[k], this._coreEmbeddings[k]);
				}
				this._coreEncoders[0].ForwardBatch(this._coreEmbeddings, numCores, this._coreIntermediate1);
				this._coreEncoders[1].ForwardBatch(this._coreIntermediate1, numCores, this._coreIntermediate2);
				this._coreEncoders[2].ForwardBatch(this._coreIntermediate2, numCores, this._coreEncoded);
				stopwatch3.Stop();
				num = stopwatch3.ElapsedTicks;
				this._batchFeatureHash = num2;
				this._batchCacheValid = true;
				this._stats.CacheMisses++;
			}
			else
			{
				this._stats.CacheHits++;
			}
			Stopwatch stopwatch4 = Stopwatch.StartNew();
			long num3 = (DateTime.Now.Ticks - this._startTick) / 10000L;
			float num4 = 1f;
			if (num3 < 120000L)
			{
				num4 = 1f + 1f * (1f - (float)num3 / 120000f);
			}
			this._crossAttentions[0].CrossAttention(this._threadEncoded, this._coreEncoded, this._coreEncoded, this._crossIntermediate, numCores, default(Span<float>), 1f);
			Span<float> span = this._attentionWeights.AsSpan(0, numCores);
			this._crossAttentions[1].CrossAttention(this._crossIntermediate, this._coreEncoded, this._coreEncoded, this._crossOutput, numCores, span, num4);
			stopwatch4.Stop();
			Span<float> span2 = this._coreScore.AsSpan(0, numCores);
			span.CopyTo(span2);
			float num5 = VectorMathNew.Sum(span2);
			int num6;
			IntPtr intPtr;
			this.SelectTopK(span2, this._topK, out num6, out intPtr);
			if (num5 > 0f)
			{
				MathHelper.Scale(span2, 1f / num5);
			}
			long num7 = (DateTime.Now.Ticks - this._startTick) / 10000L / 60000L;
			this._explorationRate = 0f;
			float positiveRewardRatio = this._stats.PositiveRewardRatio;
			float num8;
			if (num7 < 10L)
			{
				num8 = 0.01f;
			}
			else if (num7 < 17L)
			{
				num8 = 0.003f;
			}
			else if (num7 < 24L)
			{
				num8 = 0.001f;
			}
			else
			{
				num8 = 0.0001f;
			}
			this._currentLearningRate = num8;
			long ticks = DateTime.Now.Ticks;
			if (ticks - this._lastTATUpdateTick >= 10000000L)
			{
				this._decisionsInCurrentSecond = 0;
				this._lastTATUpdateTick = ticks;
			}
			this._decisionsInCurrentSecond++;
			this.RecordDecision(threadFeatures, coreFeatures, numCores, num6, predictedCore, actualCore);
			stopwatch.Stop();
			this._stats.TotalDecisions += 1L;
			long num9 = stopwatch.ElapsedTicks * 1000000L / frequency;
			this._stats.TotalInferenceTimeUs += num9;
			this._stats.TotalThreadEncodeTimeUs += stopwatch2.ElapsedTicks * 1000000L / frequency;
			this._stats.TotalCoreEncodeTimeUs += num * 1000000L / frequency;
			this._stats.TotalCrossAttnTimeUs += stopwatch4.ElapsedTicks * 1000000L / frequency;
			this._stats.CoreSelectionCounts[num6]++;
			return new ScheduleResult
			{
				BestCoreIndex = num6,
				AffinityMask = intPtr
			};
		}

		// Token: 0x060000D7 RID: 215 RVA: 0x00009B86 File Offset: 0x00007D86
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void UpdateTAT(float currentTAT, float energyValue, float newMetricValue, float extraMetricValue, int little_num, int big_num)
		{
			this.UpdateTATInternal(currentTAT, energyValue, newMetricValue, extraMetricValue, little_num, big_num);
		}

		// Token: 0x060000D8 RID: 216 RVA: 0x00009B98 File Offset: 0x00007D98
		private void UpdateTATInternal(float currentTAT, float energyValue, float newMetricValue, float extraMetricValue, int little_num, int big_num)
		{
			currentTAT /= 1000f;
			if (currentTAT < 0.01f || currentTAT > 100000f)
			{
				return;
			}
			long num = (DateTime.Now.Ticks - this._startTick) / 10000L;
			if (this._stats.TotalTATSamples > 0L)
			{
				float num2 = currentTAT - this._lastTAT;
				this._stats.RecordTATDelta(num2);
			}
			this._tatHistory[this._tatIndex] = currentTAT;
			this._sumTAT += currentTAT;
			this._tatIndex = (this._tatIndex + 1) % 1000;
			if (this._stats.TotalTATSamples < 1000L)
			{
				this._stats.TotalTATSamples += 1L;
			}
			else
			{
				this._sumTAT -= this._tatHistory[this._tatIndex];
			}
			this._lastTAT = currentTAT;
			this._stats.AvgTAT = this._sumTAT / (float)this._stats.TotalTATSamples;
			if (this._stats.TotalTATSamples == 1L || currentTAT < this._stats.MinTAT)
			{
				this._stats.MinTAT = currentTAT;
			}
			if (this._stats.TotalTATSamples == 1L || currentTAT > this._stats.MaxTAT)
			{
				this._stats.MaxTAT = currentTAT;
			}
			if (this._stats.TotalTATSamples >= 10L)
			{
				if (this._baselineTAT < 0.001f)
				{
					this._baselineTAT = this._stats.AvgTAT;
				}
				else
				{
					this._baselineTAT = this._baselineTAT * 0.95f + currentTAT * 0.05f;
				}
				this._stats.UpdateBaselineTAT(this._baselineTAT);
			}
			float num3 = 0f;
			this._baselineTAT = 16f;
			if (this._baselineTAT > 0.001f)
			{
				num3 = 1f - (this._baselineTAT - currentTAT) * (this._baselineTAT - currentTAT) / (this._baselineTAT * this._baselineTAT);
				num3 = ((num3 > 1f) ? 1f : ((num3 < -1f) ? (-1f) : num3));
			}
			if (this._stats.TotalTATSamples >= 10L)
			{
				if (this._baselineEnergy < 0.001f)
				{
					this._baselineEnergy = energyValue;
				}
				else
				{
					this._baselineEnergy = this._baselineEnergy * 0.95f + energyValue * 0.05f;
				}
			}
			this._baselineEnergy = (float)little_num / (float)(little_num + big_num);
			float num4 = 0f;
			if (this._baselineEnergy > 0.001f)
			{
				num4 = (energyValue - this._baselineEnergy) / this._baselineEnergy;
				num4 = ((num4 > 1f) ? 1f : ((num4 < -1f) ? (-1f) : num4));
			}
			if (this._stats.TotalTATSamples >= 10L)
			{
				if (this._baselineNewMetric < 0.001f)
				{
					this._baselineNewMetric = newMetricValue;
				}
				else
				{
					this._baselineNewMetric = this._baselineNewMetric * 0.95f + newMetricValue * 0.05f;
				}
			}
			float num5 = 0f;
			if (this._baselineNewMetric > 0.001f)
			{
				num5 = (newMetricValue - this._baselineNewMetric) / this._baselineNewMetric;
				num5 = ((num5 > 1f) ? 1f : ((num5 < -1f) ? (-1f) : num5));
			}
			if (this._stats.TotalTATSamples >= 10L)
			{
				if (this._baselineExtraMetric < 0.001f)
				{
					this._baselineExtraMetric = extraMetricValue;
				}
				else
				{
					this._baselineExtraMetric = this._baselineExtraMetric * 0.95f + extraMetricValue * 0.05f;
				}
			}
			float num6 = 0f;
			if (this._baselineExtraMetric > 0.001f)
			{
				num6 = (extraMetricValue - this._baselineExtraMetric) / this._baselineExtraMetric;
				num6 = ((num6 > 1f) ? 1f : ((num6 < -1f) ? (-1f) : num6));
			}
			float num7 = 0f;
			if (this.LOAD_BALANCE_WEIGHT > 0f && this._currentRecord.NumCores > 0)
			{
				float num8 = this.ComputeLoadBalance(this._currentRecord.CoreFeatures, this._currentRecord.NumCores);
				if (this._stats.TotalTATSamples >= 10L)
				{
					if (this._baselineLoadBalance < 0.001f)
					{
						this._baselineLoadBalance = num8;
					}
					else
					{
						this._baselineLoadBalance = this._baselineLoadBalance * 0.95f + num8 * 0.05f;
					}
				}
				if (this._baselineLoadBalance > 0.001f)
				{
					num7 = (this._baselineLoadBalance - num8) / this._baselineLoadBalance;
					num7 = ((num7 > 1f) ? 1f : ((num7 < -1f) ? (-1f) : num7));
				}
				this._stats.LastLoadBalance = num8;
				this._stats.TotalLoadBalanceSamples += 1L;
				this._stats.AvgLoadBalance = (this._stats.AvgLoadBalance * (float)(this._stats.TotalLoadBalanceSamples - 1L) + num8) / (float)this._stats.TotalLoadBalanceSamples;
				if (this._stats.TotalLoadBalanceSamples == 1L || num8 < this._stats.MinLoadBalance)
				{
					this._stats.MinLoadBalance = num8;
				}
				if (this._stats.TotalLoadBalanceSamples == 1L || num8 > this._stats.MaxLoadBalance)
				{
					this._stats.MaxLoadBalance = num8;
				}
				this._stats.BaselineLoadBalance = this._baselineLoadBalance;
				this._stats.RecordLoadBalance(num8);
			}
			float num9 = 0f;
			if (this.LOAD_BALANCE2_WEIGHT > 0f && this._currentRecord.NumCores > 0)
			{
				float num10 = this.ComputeLoadBalance2(this._currentRecord.CoreFeatures, this._currentRecord.NumCores);
				if (this._stats.TotalTATSamples >= 10L)
				{
					if (this._baselineLoadBalance2 < 0.001f)
					{
						this._baselineLoadBalance2 = num10;
					}
					else
					{
						this._baselineLoadBalance2 = this._baselineLoadBalance2 * 0.95f + num10 * 0.05f;
					}
				}
				if (this._baselineLoadBalance2 > 0.001f)
				{
					num9 = (this._baselineLoadBalance2 - num10) / this._baselineLoadBalance2;
					num9 = ((num9 > 1f) ? 1f : ((num9 < -1f) ? (-1f) : num9));
				}
				this._stats.LastLoadBalance2 = num10;
				this._stats.TotalLoadBalance2Samples += 1L;
				this._stats.AvgLoadBalance2 = (this._stats.AvgLoadBalance2 * (float)(this._stats.TotalLoadBalance2Samples - 1L) + num10) / (float)this._stats.TotalLoadBalance2Samples;
				if (this._stats.TotalLoadBalance2Samples == 1L || num10 < this._stats.MinLoadBalance2)
				{
					this._stats.MinLoadBalance2 = num10;
				}
				if (this._stats.TotalLoadBalance2Samples == 1L || num10 > this._stats.MaxLoadBalance2)
				{
					this._stats.MaxLoadBalance2 = num10;
				}
				this._stats.BaselineLoadBalance2 = this._baselineLoadBalance2;
				this._stats.RecordLoadBalance2(num10);
			}
			long num11 = (DateTime.Now.Ticks - this._startTick) / 600000000L;
			if (num11 >= 0L && num11 < 18L)
			{
				this.LOAD_BALANCE_WEIGHT = 0f;
				this.LOAD_BALANCE2_WEIGHT = 0f;
				this.ENERGY_WEIGHT = 0f;
				this.NEW_METRIC_WEIGHT = 1f;
				this.EXTRA_METRIC_WEIGHT = 0f;
			}
			else
			{
				this.LOAD_BALANCE_WEIGHT = 0f;
				this.LOAD_BALANCE2_WEIGHT = 0f;
				this.ENERGY_WEIGHT = 0f;
				this.NEW_METRIC_WEIGHT = 1f;
				this.EXTRA_METRIC_WEIGHT = 0f;
			}
			float num12 = num3 * (1f - this.ENERGY_WEIGHT - this.LOAD_BALANCE_WEIGHT - this.LOAD_BALANCE2_WEIGHT - this.NEW_METRIC_WEIGHT - this.EXTRA_METRIC_WEIGHT) + num4 * this.ENERGY_WEIGHT + num7 * this.LOAD_BALANCE_WEIGHT + num9 * this.LOAD_BALANCE2_WEIGHT + num5 * this.NEW_METRIC_WEIGHT + num6 * this.EXTRA_METRIC_WEIGHT;
			this._stats.RecordReward(num12);
			this._stats.RecentAvgReward = this._stats.RecentAvgReward * 0.95f + num12 * 0.05f;
			this._stats.LastEnergy = energyValue;
			this._stats.TotalEnergySamples += 1L;
			this._stats.AvgEnergy = (this._stats.AvgEnergy * (float)(this._stats.TotalEnergySamples - 1L) + energyValue) / (float)this._stats.TotalEnergySamples;
			if (this._stats.TotalEnergySamples == 1L || energyValue < this._stats.MinEnergy)
			{
				this._stats.MinEnergy = energyValue;
			}
			if (this._stats.TotalEnergySamples == 1L || energyValue > this._stats.MaxEnergy)
			{
				this._stats.MaxEnergy = energyValue;
			}
			this._stats.BaselineEnergy = this._baselineEnergy;
			this._stats.RecordEnergy(energyValue);
			this._stats.LastNewMetric = newMetricValue;
			this._stats.TotalNewMetricSamples += 1L;
			this._stats.AvgNewMetric = (this._stats.AvgNewMetric * (float)(this._stats.TotalNewMetricSamples - 1L) + newMetricValue) / (float)this._stats.TotalNewMetricSamples;
			if (this._stats.TotalNewMetricSamples == 1L || newMetricValue < this._stats.MinNewMetric)
			{
				this._stats.MinNewMetric = newMetricValue;
			}
			if (this._stats.TotalNewMetricSamples == 1L || newMetricValue > this._stats.MaxNewMetric)
			{
				this._stats.MaxNewMetric = newMetricValue;
			}
			this._stats.BaselineNewMetric = this._baselineNewMetric;
			this._stats.RecordNewMetric(newMetricValue);
			this._stats.LastExtraMetric = extraMetricValue;
			this._stats.TotalExtraMetricSamples += 1L;
			this._stats.AvgExtraMetric = (this._stats.AvgExtraMetric * (float)(this._stats.TotalExtraMetricSamples - 1L) + extraMetricValue) / (float)this._stats.TotalExtraMetricSamples;
			if (this._stats.TotalExtraMetricSamples == 1L || extraMetricValue < this._stats.MinExtraMetric)
			{
				this._stats.MinExtraMetric = extraMetricValue;
			}
			if (this._stats.TotalExtraMetricSamples == 1L || extraMetricValue > this._stats.MaxExtraMetric)
			{
				this._stats.MaxExtraMetric = extraMetricValue;
			}
			this._stats.BaselineExtraMetric = this._baselineExtraMetric;
			this._stats.RecordExtraMetric(extraMetricValue);
			bool flag = num < 600000L;
			if ((!flag && currentTAT < 1f) || !flag)
			{
			}
			int num13 = Math.Min(this._decisionsInCurrentSecond, this._history.Count);
			int num14 = this._history.Count - num13;
			float currentLearningRate = this._currentLearningRate;
			float num15 = 0f;
			int num16 = Math.Min(num13, 10000);
			for (int i = 0; i < num16; i++)
			{
				this._learningWeights[i] = (float)Math.Exp((double)(-0.5f * (float)(num16 - 1 - i)));
				num15 += this._learningWeights[i];
			}
			float num17 = 0f;
			if (!this._learningEnabled)
			{
				this._stats.ExperienceSkipped += num16;
				return;
			}
			for (int j = 0; j < num16; j++)
			{
				int num18 = num14 + j;
				if (num18 >= 0 && num18 < this._history.Count)
				{
					DecisionRecord decisionRecord = this._history.Get(num18);
					if (!flag && !decisionRecord.IsValid)
					{
						this._stats.ExperienceSkipped++;
					}
					else
					{
						float num19 = num3 * (this._learningWeights[j] / num15);
						num17 += Math.Abs(num19);
						this.BuildThreadInput(decisionRecord.ThreadFeatures, this._normThreadBuf);
						this._threadEmbedding.Forward(this._normThreadBuf, this._threadEmbed);
						this._threadEncoder.Forward(this._threadEmbed, this._threadEncoded);
						for (int k = 0; k < decisionRecord.NumCores; k++)
						{
							ReadOnlySpan<float> readOnlySpan = new ReadOnlySpan<float>(decisionRecord.CoreFeatures[k], 0, 9);
							this.BuildCoreInput(readOnlySpan, this._coreInputs[k]);
							this._coreEmbedding.Forward(this._coreInputs[k], this._coreEmbeddings[k]);
						}
						this._coreEncoders[0].ForwardBatch(this._coreEmbeddings, decisionRecord.NumCores, this._coreIntermediate1);
						this._coreEncoders[1].ForwardBatch(this._coreIntermediate1, decisionRecord.NumCores, this._coreIntermediate2);
						this._coreEncoders[2].ForwardBatch(this._coreIntermediate2, decisionRecord.NumCores, this._coreEncoded);
						this._crossAttentions[0].CrossAttention(this._threadEncoded, this._coreEncoded, this._coreEncoded, this._crossIntermediate, decisionRecord.NumCores, default(Span<float>), 1f);
						Span<float> span = this._attentionWeights.AsSpan(0, decisionRecord.NumCores);
						this._crossAttentions[1].CrossAttention(this._crossIntermediate, this._coreEncoded, this._coreEncoded, this._crossOutput, decisionRecord.NumCores, span, 1f);
						Array.Clear(this._coreIdEmbeddingGrad, 0, this._coreIdEmbeddingGrad.Length);
						Array.Clear(this._coreTypeEmbeddingGrad, 0, this._coreTypeEmbeddingGrad.Length);
						for (int l = 0; l < 64; l++)
						{
							this._gradCrossOutput[l] = num19 * this._crossOutput[l];
						}
						this._crossAttentions[1].Backward(this._gradCrossOutput, currentLearningRate);
						float[] queryGradients = this._crossAttentions[1].GetQueryGradients();
						float[] valueGradients = this._crossAttentions[1].GetValueGradients();
						float[] keyGradients = this._crossAttentions[1].GetKeyGradients();
						this._crossAttentions[0].Backward(queryGradients, currentLearningRate);
						float[] queryGradients2 = this._crossAttentions[0].GetQueryGradients();
						float[] valueGradients2 = this._crossAttentions[0].GetValueGradients();
						float[] keyGradients2 = this._crossAttentions[0].GetKeyGradients();
						this._threadEncoder.Backward(queryGradients2, currentLearningRate);
						float[] inputGradients = this._threadEncoder.InputGradients;
						this._threadEmbedding.Backward(inputGradients, currentLearningRate, false);
						float[] inputGrads = this._threadEmbedding.InputGrads;
						int num20 = (int)decisionRecord.ThreadFeatures[6];
						if (num20 >= 0 && num20 < 64)
						{
							int num21 = num20 * 4;
							for (int m = 0; m < 4; m++)
							{
								this._coreIdEmbeddingGrad[num21 + m] += inputGrads[6 + m];
							}
						}
						int num22 = (int)decisionRecord.ThreadFeatures[7];
						if (num22 >= 0 && num22 < 7)
						{
							int num23 = num22 * 4;
							for (int n = 0; n < 4; n++)
							{
								this._coreTypeEmbeddingGrad[num23 + n] += inputGrads[10 + n];
							}
						}
						int cachedNumCores = this._crossAttentions[1].GetCachedNumCores();
						for (int num24 = 0; num24 < cachedNumCores; num24++)
						{
							for (int num25 = 0; num25 < 64; num25++)
							{
								this._gradCrossKVPerCore[num24][num25] = keyGradients2[num24 * 64 + num25] + valueGradients2[num24 * 64 + num25] + keyGradients[num24 * 64 + num25] + valueGradients[num24 * 64 + num25];
							}
						}
						this._coreEncoders[2].BackwardBatch(this._gradCrossKVPerCore, cachedNumCores, currentLearningRate);
						float[][] batchInputGradients = this._coreEncoders[2].GetBatchInputGradients();
						this._coreEncoders[1].BackwardBatch(batchInputGradients, cachedNumCores, currentLearningRate);
						float[][] batchInputGradients2 = this._coreEncoders[1].GetBatchInputGradients();
						this._coreEncoders[0].BackwardBatch(batchInputGradients2, cachedNumCores, currentLearningRate);
						float[][] batchInputGradients3 = this._coreEncoders[0].GetBatchInputGradients();
						Array.Clear(this._coreEmbedWeightGradAccum, 0, this._coreEmbedWeightGradAccum.Length);
						Array.Clear(this._coreEmbedBiasGradAccum, 0, this._coreEmbedBiasGradAccum.Length);
						for (int num26 = 0; num26 < cachedNumCores; num26++)
						{
							ReadOnlySpan<float> readOnlySpan2 = new ReadOnlySpan<float>(decisionRecord.CoreFeatures[num26], 0, 9);
							this.BuildCoreInput(readOnlySpan2, this._normCoreBwdBuf);
							this._coreEmbedding.Forward(this._normCoreBwdBuf, this._coreEmbeddings[num26]);
							float[] array = batchInputGradients3[num26];
							this._coreEmbedding.Backward(array, currentLearningRate, false);
							float[] weightGrads = this._coreEmbedding.WeightGrads;
							for (int num27 = 0; num27 < this._coreEmbedWeightGradAccum.Length; num27++)
							{
								this._coreEmbedWeightGradAccum[num27] += weightGrads[num27];
							}
							float[] biasGrads = this._coreEmbedding.BiasGrads;
							for (int num28 = 0; num28 < this._coreEmbedBiasGradAccum.Length; num28++)
							{
								this._coreEmbedBiasGradAccum[num28] += biasGrads[num28];
							}
							float[] inputGrads2 = this._coreEmbedding.InputGrads;
							int num29 = (int)decisionRecord.CoreFeatures[num26][7];
							if (num29 >= 0 && num29 < 64)
							{
								int num30 = num29 * 4;
								for (int num31 = 0; num31 < 4; num31++)
								{
									this._coreIdEmbeddingGrad[num30 + num31] += inputGrads2[7 + num31];
								}
							}
							int num32 = (int)decisionRecord.CoreFeatures[num26][8];
							if (num32 >= 0 && num32 < 7)
							{
								int num33 = num32 * 4;
								for (int num34 = 0; num34 < 4; num34++)
								{
									this._coreTypeEmbeddingGrad[num33 + num34] += inputGrads2[11 + num34];
								}
							}
						}
						Array.Copy(this._coreEmbedWeightGradAccum, this._coreEmbedding.WeightGrads, this._coreEmbedWeightGradAccum.Length);
						Array.Copy(this._coreEmbedBiasGradAccum, this._coreEmbedding.BiasGrads, this._coreEmbedBiasGradAccum.Length);
						this._threadEmbedding.ApplyGradientsSGD(currentLearningRate, 1f);
						this._coreEmbedding.ApplyGradientsSGD(currentLearningRate, 1f);
						for (int num35 = 0; num35 < 2; num35++)
						{
							this._crossAttentions[num35].ApplyGradients(0.001f);
						}
						this._threadEncoder.ApplyGradients(currentLearningRate);
						for (int num36 = 0; num36 < 3; num36++)
						{
							this._coreEncoders[num36].ApplyGradients(currentLearningRate);
						}
						this.ApplyEmbeddingGradients(this._coreIdEmbedding, this._coreIdEmbeddingGrad, currentLearningRate);
						this.ApplyEmbeddingGradients(this._coreTypeEmbedding, this._coreTypeEmbeddingGrad, currentLearningRate);
						this._stats.ExperienceUsed++;
						this._stats.LearningUpdates++;
					}
				}
			}
			this._stats.AvgLoss = this._stats.AvgLoss * 0.9f + num17 / (float)Math.Max(num16, 1) * 0.1f;
			this._decisionsInCurrentSecond = 0;
		}

		// Token: 0x060000D9 RID: 217 RVA: 0x0000AE88 File Offset: 0x00009088
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void PerformBatchTraining()
		{
			if (!this._learningEnabled)
			{
				return;
			}
			if (this._history.Count < 10)
			{
				return;
			}
			int num = Math.Min(1000, this._history.Count);
			float currentLearningRate = this._currentLearningRate;
			float num2 = 0f;
			for (int i = 0; i < num; i++)
			{
				int num3 = TransformerScheduler._random.Next(this._history.Count);
				DecisionRecord decisionRecord = this._history.Get(num3);
				float reward = decisionRecord.Reward;
				Array.Clear(this._coreIdEmbeddingGrad, 0, this._coreIdEmbeddingGrad.Length);
				Array.Clear(this._coreTypeEmbeddingGrad, 0, this._coreTypeEmbeddingGrad.Length);
				this.BuildThreadInput(decisionRecord.ThreadFeatures, this._normThreadBuf);
				this._threadEmbedding.Forward(this._normThreadBuf, this._threadEmbed);
				this._threadEncoder.Forward(this._threadEmbed, this._threadEncoded);
				for (int j = 0; j < decisionRecord.NumCores; j++)
				{
					ReadOnlySpan<float> readOnlySpan = new ReadOnlySpan<float>(decisionRecord.CoreFeatures[j], 0, 9);
					this.BuildCoreInput(readOnlySpan, this._coreInputs[j]);
					this._coreEmbedding.Forward(this._coreInputs[j], this._coreEmbeddings[j]);
				}
				this._coreEncoders[0].ForwardBatch(this._coreEmbeddings, decisionRecord.NumCores, this._coreIntermediate1);
				this._coreEncoders[1].ForwardBatch(this._coreIntermediate1, decisionRecord.NumCores, this._coreIntermediate2);
				this._coreEncoders[2].ForwardBatch(this._coreIntermediate2, decisionRecord.NumCores, this._coreEncoded);
				this._crossAttentions[0].CrossAttention(this._threadEncoded, this._coreEncoded, this._coreEncoded, this._crossIntermediate, decisionRecord.NumCores, default(Span<float>), 1f);
				Span<float> span = this._attentionWeights.AsSpan(0, decisionRecord.NumCores);
				this._crossAttentions[1].CrossAttention(this._crossIntermediate, this._coreEncoded, this._coreEncoded, this._crossOutput, decisionRecord.NumCores, span, 1f);
				for (int k = 0; k < 64; k++)
				{
					this._gradCrossOutput[k] = reward * this._crossOutput[k];
				}
				this._crossAttentions[1].Backward(this._gradCrossOutput, currentLearningRate);
				float[] queryGradients = this._crossAttentions[1].GetQueryGradients();
				float[] valueGradients = this._crossAttentions[1].GetValueGradients();
				float[] keyGradients = this._crossAttentions[1].GetKeyGradients();
				this._crossAttentions[0].Backward(queryGradients, currentLearningRate);
				float[] queryGradients2 = this._crossAttentions[0].GetQueryGradients();
				float[] valueGradients2 = this._crossAttentions[0].GetValueGradients();
				float[] keyGradients2 = this._crossAttentions[0].GetKeyGradients();
				this._threadEncoder.Backward(queryGradients2, currentLearningRate);
				float[] inputGradients = this._threadEncoder.InputGradients;
				this._threadEmbedding.Backward(inputGradients, currentLearningRate, false);
				float[] inputGrads = this._threadEmbedding.InputGrads;
				int num4 = (int)decisionRecord.ThreadFeatures[6];
				if (num4 >= 0 && num4 < 64)
				{
					int num5 = num4 * 4;
					for (int l = 0; l < 4; l++)
					{
						this._coreIdEmbeddingGrad[num5 + l] += inputGrads[6 + l];
					}
				}
				float[] inputGrads2 = this._threadEmbedding.InputGrads;
				int num6 = (int)decisionRecord.ThreadFeatures[7];
				if (num6 >= 0 && num6 < 7)
				{
					int num7 = num6 * 4;
					for (int m = 0; m < 4; m++)
					{
						this._coreTypeEmbeddingGrad[num7 + m] += inputGrads2[10 + m];
					}
				}
				int cachedNumCores = this._crossAttentions[1].GetCachedNumCores();
				for (int n = 0; n < cachedNumCores; n++)
				{
					for (int num8 = 0; num8 < 64; num8++)
					{
						this._gradCrossKVPerCore[n][num8] = keyGradients2[n * 64 + num8] + valueGradients2[n * 64 + num8] + keyGradients[n * 64 + num8] + valueGradients[n * 64 + num8];
					}
				}
				this._coreEncoders[2].BackwardBatch(this._gradCrossKVPerCore, cachedNumCores, currentLearningRate);
				float[][] batchInputGradients = this._coreEncoders[2].GetBatchInputGradients();
				this._coreEncoders[1].BackwardBatch(batchInputGradients, cachedNumCores, currentLearningRate);
				float[][] batchInputGradients2 = this._coreEncoders[1].GetBatchInputGradients();
				this._coreEncoders[0].BackwardBatch(batchInputGradients2, cachedNumCores, currentLearningRate);
				float[][] batchInputGradients3 = this._coreEncoders[0].GetBatchInputGradients();
				for (int num9 = 0; num9 < cachedNumCores; num9++)
				{
					ReadOnlySpan<float> readOnlySpan2 = new ReadOnlySpan<float>(decisionRecord.CoreFeatures[num9], 0, 9);
					this.BuildCoreInput(readOnlySpan2, this._normCoreBwdBuf);
					this._coreEmbedding.Forward(this._normCoreBwdBuf, this._coreEmbeddings[num9]);
					float[] array = batchInputGradients3[num9];
					this._coreEmbedding.Backward(array, currentLearningRate, false);
					float[] weightGrads = this._coreEmbedding.WeightGrads;
					for (int num10 = 0; num10 < this._coreEmbedWeightGradAccum.Length; num10++)
					{
						this._coreEmbedWeightGradAccum[num10] += weightGrads[num10];
					}
					float[] biasGrads = this._coreEmbedding.BiasGrads;
					for (int num11 = 0; num11 < this._coreEmbedBiasGradAccum.Length; num11++)
					{
						this._coreEmbedBiasGradAccum[num11] += biasGrads[num11];
					}
					float[] inputGrads3 = this._coreEmbedding.InputGrads;
					int num12 = (int)decisionRecord.CoreFeatures[num9][7];
					if (num12 >= 0 && num12 < 64)
					{
						int num13 = num12 * 4;
						for (int num14 = 0; num14 < 4; num14++)
						{
							this._coreIdEmbeddingGrad[num13 + num14] += inputGrads3[7 + num14];
						}
					}
					float[] inputGrads4 = this._coreEmbedding.InputGrads;
					int num15 = (int)decisionRecord.CoreFeatures[num9][8];
					if (num15 >= 0 && num15 < 7)
					{
						int num16 = num15 * 4;
						for (int num17 = 0; num17 < 4; num17++)
						{
							this._coreTypeEmbeddingGrad[num16 + num17] += inputGrads4[11 + num17];
						}
					}
				}
				Array.Copy(this._coreEmbedWeightGradAccum, this._coreEmbedding.WeightGrads, this._coreEmbedWeightGradAccum.Length);
				Array.Copy(this._coreEmbedBiasGradAccum, this._coreEmbedding.BiasGrads, this._coreEmbedBiasGradAccum.Length);
				num2 += Math.Abs(reward);
				this._stats.LearningUpdates++;
			}
			this._threadEmbedding.ApplyGradientsSGD(currentLearningRate, 1f);
			this._coreEmbedding.ApplyGradientsSGD(currentLearningRate, 1f);
			for (int num18 = 0; num18 < 2; num18++)
			{
				this._crossAttentions[num18].ApplyGradients(0.001f);
			}
			this._threadEncoder.ApplyGradients(currentLearningRate);
			for (int num19 = 0; num19 < 3; num19++)
			{
				this._coreEncoders[num19].ApplyGradients(currentLearningRate);
			}
			this.ApplyEmbeddingGradients(this._coreIdEmbedding, this._coreIdEmbeddingGrad, currentLearningRate);
			this.ApplyEmbeddingGradients(this._coreTypeEmbedding, this._coreTypeEmbeddingGrad, currentLearningRate);
			this._stats.AvgLoss = this._stats.AvgLoss * 0.95f + num2 / (float)num * 0.05f;
		}

		// Token: 0x060000DA RID: 218 RVA: 0x0000B610 File Offset: 0x00009810
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void RecordDecision(ReadOnlySpan<float> threadFeatures, float[][] coreFeatures, int numCores, int selectedCore, int predictedCore = -1, int actualCore = -1)
		{
			threadFeatures.Slice(0, 8).CopyTo(this._currentRecord.ThreadFeatures);
			this._currentRecord.NumCores = numCores;
			for (int i = 0; i < numCores; i++)
			{
				Array.Copy(coreFeatures[i], this._currentRecord.CoreFeatures[i], 9);
			}
			this._currentRecord.SelectedCore = selectedCore;
			this._currentRecord.Timestamp = DateTime.Now.Ticks;
			this._currentRecord.PredictedCore = predictedCore;
			this._currentRecord.ActualCore = actualCore;
			if (predictedCore >= 0 && actualCore >= 0 && predictedCore != actualCore)
			{
				this._currentRecord.IsValid = false;
			}
			else
			{
				this._currentRecord.IsValid = true;
			}
			this._history.Add(this._currentRecord);
		}

		// Token: 0x060000DB RID: 219 RVA: 0x0000B6E8 File Offset: 0x000098E8
		public long GetRuntime()
		{
			return (DateTime.Now.Ticks - this._startTick) / 600000000L;
		}

		// Token: 0x060000DC RID: 220 RVA: 0x0000B710 File Offset: 0x00009910
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe void SelectTopK(ReadOnlySpan<float> scores, int topK, out int bestCoreIndex, out IntPtr affinityMask)
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
			int num = length;
			Span<float> span;
			Span<int> span2;
			checked
			{
				span = new Span<float>(stackalloc byte[unchecked((UIntPtr)num) * 4], num);
				scores.CopyTo(span);
				num = topK;
				span2 = new Span<int>(stackalloc byte[unchecked((UIntPtr)num) * 4], num);
			}
			for (int i = 0; i < topK; i++)
			{
				float num2 = VectorMathNew.Max(span);
				int num3 = 0;
				for (int j = 0; j < length; j++)
				{
					if (*span[j] >= num2 - 1E-06f)
					{
						num3 = j;
						break;
					}
				}
				*span2[i] = num3;
				*span[num3] = float.NegativeInfinity;
			}
			bestCoreIndex = *span2[0];
			ulong num4 = 0UL;
			for (int k = 0; k < topK; k++)
			{
				num4 |= 1UL << *span2[k];
			}
			affinityMask = (IntPtr)((long)num4);
		}

		// Token: 0x060000DD RID: 221 RVA: 0x0000B81C File Offset: 0x00009A1C
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
			return (float)Math.Sqrt((double)(num3 / (float)numCores));
		}

		// Token: 0x060000DE RID: 222 RVA: 0x0000B888 File Offset: 0x00009A88
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
			return (float)Math.Sqrt((double)(num3 / (float)numCores));
		}

		// Token: 0x060000DF RID: 223 RVA: 0x0000B8F4 File Offset: 0x00009AF4
		public string GetStatistics(int numcores)
		{
			long num = (DateTime.Now.Ticks - this._startTick) / 10000L;
			bool flag = num < 600000L;
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(string.Format("Runtime: {0} min / {1} min", num / 60000L, 10L));
			stringBuilder.AppendLine(string.Format("Model: d_model={0}, n_head={1}, d_ff={2}, core_encoder_layers={3}, cross_attn_layers={4}", new object[] { 64, 4, 256, 3, 2 }));
			float num2 = 1f;
			if (num < 120000L)
			{
				num2 = 1f + 1f * (1f - (float)num / 120000f);
			}
			stringBuilder.AppendLine(string.Format("Softmax Temperature: {0:F2} {1}", num2, (num < 120000L) ? "(annealing)" : "(stable)"));
			stringBuilder.AppendLine("Learning Phase: " + (flag ? "Initial (all experiences valid)" : "Stable (skip invalid experiences)"));
			stringBuilder.AppendLine();
			stringBuilder.Append(this._stats.GetReport(numcores, this._explorationRate));
			return stringBuilder.ToString();
		}

		// Token: 0x060000E0 RID: 224 RVA: 0x0000BA3C File Offset: 0x00009C3C
		public string GetRecentDecisions(int n = 10)
		{
			n = Math.Min(n, this._history.Count);
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(string.Format("=== Recent {0} Decisions ===", n));
			for (int i = 0; i < n; i++)
			{
				DecisionRecord decisionRecord = this._history.Get(n - 1 - i);
				stringBuilder.AppendLine(string.Format("[{0}] Core {1} | ", i, decisionRecord.SelectedCore) + string.Format("Thread IPC: {0:F2} | ", decisionRecord.ThreadFeatures[1]) + string.Format("Priority: {0:F2} | ", decisionRecord.ThreadFeatures[2]) + string.Format("Reward: {0:F6}", decisionRecord.Reward));
			}
			return stringBuilder.ToString();
		}

		// Token: 0x060000E1 RID: 225 RVA: 0x0000BB0C File Offset: 0x00009D0C
		public string GetAttentionHeadReport(int numCores)
		{
			StringBuilder stringBuilder = new StringBuilder();
			float[][] headAttentionWeights = this._crossAttentions[1].GetHeadAttentionWeights(numCores);
			stringBuilder.AppendLine("╔" + new string('═', 52) + "╗");
			stringBuilder.Append("║ Core   │");
			for (int i = 0; i < 4; i++)
			{
				stringBuilder.Append(string.Format("  H{0}    │", i));
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
				this._reportMaxCores[k] = 0;
				this._reportMaxWeights[k] = headAttentionWeights[k][0];
				for (int l = 1; l < numCores; l++)
				{
					if (headAttentionWeights[k][l] > this._reportMaxWeights[k])
					{
						this._reportMaxWeights[k] = headAttentionWeights[k][l];
						this._reportMaxCores[k] = l;
					}
				}
			}
			for (int m = 0; m < numCores; m++)
			{
				stringBuilder.Append(string.Format("║ {0,-5} │", m));
				for (int n = 0; n < 4; n++)
				{
					string text = ((m == this._reportMaxCores[n]) ? string.Format("*{0:F4}", headAttentionWeights[n][m]) : string.Format(" {0:F4}", headAttentionWeights[n][m]));
					stringBuilder.Append(string.Format("{0,-8}│", text));
				}
				stringBuilder.AppendLine(string.Format(" {0:F4} ║", this._attentionWeights[m]));
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
				stringBuilder.AppendLine(string.Format("  H{0}: Max→Core{1} ({2:F4}), Avg={3:F4}", new object[]
				{
					num2,
					this._reportMaxCores[num2],
					this._reportMaxWeights[num2],
					num3 / (float)numCores
				}));
			}
			return stringBuilder.ToString();
		}

		// Token: 0x060000E2 RID: 226 RVA: 0x0000BE0C File Offset: 0x0000A00C
		public float[] GetLastAttentionWeights(int numCores)
		{
			for (int i = 0; i < numCores; i++)
			{
				this._lastAttentionWeightsBuffer[i] = this._attentionWeights[i];
			}
			return this._lastAttentionWeightsBuffer;
		}

		// Token: 0x060000E3 RID: 227 RVA: 0x0000BE3B File Offset: 0x0000A03B
		public void SetNormalizationParams(float[] threadMean, float[] threadStd, float[] coreMean, float[] coreStd)
		{
			Array.Copy(threadMean, this._threadFeatureMean, 6);
			Array.Copy(threadStd, this._threadFeatureStd, 6);
			Array.Copy(coreMean, this._coreFeatureMean, 7);
			Array.Copy(coreStd, this._coreFeatureStd, 7);
			this._normalizationReady = true;
		}

		// Token: 0x060000E4 RID: 228 RVA: 0x0000BE7C File Offset: 0x0000A07C
		public string ExportModel()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(string.Format("Model Config: d_model={0}, n_head={1}, d_ff={2}", 64, 4, 256));
			stringBuilder.AppendLine(string.Format("Thread Embedding: {0} -> {1}", 14, 64));
			stringBuilder.AppendLine(string.Format("Core Embedding: {0} -> {1}", 15, 64));
			stringBuilder.AppendLine(string.Format("Total Params: {0}", this.CountParameters()));
			return stringBuilder.ToString();
		}

		// Token: 0x060000E5 RID: 229 RVA: 0x0000BF18 File Offset: 0x0000A118
		private int CountParameters()
		{
			int num = 0;
			num += this._threadEmbedding.Weights.Length + this._threadEmbedding.Bias.Length;
			num += this._coreEmbedding.Weights.Length + this._coreEmbedding.Bias.Length;
			num += this._coreIdEmbedding.Length;
			num += this._coreTypeEmbedding.Length;
			for (int i = 0; i < 3; i++)
			{
				CoreTransformerEncoder coreTransformerEncoder = this._coreEncoders[i];
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
				num += this._crossAttentions[j].Wq.Weights.Length + this._crossAttentions[j].Wq.Bias.Length;
				num += this._crossAttentions[j].Wk.Weights.Length + this._crossAttentions[j].Wk.Bias.Length;
				num += this._crossAttentions[j].Wv.Weights.Length + this._crossAttentions[j].Wv.Bias.Length;
				num += this._crossAttentions[j].Wo.Weights.Length + this._crossAttentions[j].Wo.Bias.Length;
			}
			num += this._threadEncoder.FeedForward.FC1.Weights.Length + this._threadEncoder.FeedForward.FC1.Bias.Length;
			num += this._threadEncoder.FeedForward.FC2.Weights.Length + this._threadEncoder.FeedForward.FC2.Bias.Length;
			num += this._threadEncoder.Norm1.Gamma.Length + this._threadEncoder.Norm1.Beta.Length;
			return num + (this._threadEncoder.Norm2.Gamma.Length + this._threadEncoder.Norm2.Beta.Length);
		}

		// Token: 0x060000E6 RID: 230 RVA: 0x0000C237 File Offset: 0x0000A437
		public float GetCurrentTAT()
		{
			return this._lastTAT;
		}

		// Token: 0x060000E7 RID: 231 RVA: 0x0000C23F File Offset: 0x0000A43F
		public float GetBaselineTAT()
		{
			return this._baselineTAT;
		}

		// Token: 0x060000E8 RID: 232 RVA: 0x0000C247 File Offset: 0x0000A447
		public float GetRecentAvgReward()
		{
			return this._stats.RecentAvgReward;
		}

		// Token: 0x060000E9 RID: 233 RVA: 0x0000C254 File Offset: 0x0000A454
		public float GetExplorationRate()
		{
			return this._explorationRate;
		}

		// Token: 0x060000EA RID: 234 RVA: 0x0000C25C File Offset: 0x0000A45C
		public float GetLearningRate()
		{
			return this._currentLearningRate;
		}

		// Token: 0x060000EB RID: 235 RVA: 0x0000C264 File Offset: 0x0000A464
		public void SetExplorationRate(float rate)
		{
			this._explorationRate = Math.Max(rate, this._minExplorationRate);
		}

		// Token: 0x060000EC RID: 236 RVA: 0x0000C278 File Offset: 0x0000A478
		public void SetMinExplorationRate(float rate)
		{
			this._minExplorationRate = Math.Max(rate, 0.001f);
		}

		// Token: 0x060000ED RID: 237 RVA: 0x0000C28B File Offset: 0x0000A48B
		public void SetExplorationDecayMinutes(int minutes)
		{
		}

		// Token: 0x060000EE RID: 238 RVA: 0x0000C290 File Offset: 0x0000A490
		public string GetLearningReport()
		{
			this._stats.ComputeTrends();
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("=== Neural Network Learning Report ===");
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("--- Reward Metrics ---");
			stringBuilder.AppendLine(string.Format("Last Reward: {0:F6}", this._stats.LastReward));
			stringBuilder.AppendLine(string.Format("Recent Avg Reward: {0:F6}", this._stats.RecentAvgReward));
			stringBuilder.AppendLine(string.Format("Reward Trend: {0:F6} ({1}{2:F2}%/step)", this._stats.RewardTrend, (this._stats.RewardTrend > 0f) ? "+" : "", this._stats.RewardTrend * 100f));
			stringBuilder.AppendLine(string.Format("Positive Reward Ratio: {0:P1}", this._stats.PositiveRewardRatio));
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("--- TAT Metrics ---");
			stringBuilder.AppendLine(string.Format("Current TAT: {0:F2}ms", this._lastTAT));
			stringBuilder.AppendLine(string.Format("Baseline TAT: {0:F2}ms", this._baselineTAT));
			stringBuilder.AppendLine(string.Format("Avg TAT: {0:F2}ms", this._stats.AvgTAT));
			stringBuilder.AppendLine(string.Format("Min TAT: {0:F2}ms", this._stats.MinTAT));
			stringBuilder.AppendLine(string.Format("Max TAT: {0:F2}ms", this._stats.MaxTAT));
			stringBuilder.AppendLine(string.Format("TAT Trend: {0:F4} ms/step", this._stats.TATTrend));
			stringBuilder.AppendLine(string.Format("Last TAT Delta: {0:F2}ms", this._stats.LastTATDelta));
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("--- 微架构可比压力系数（越低越好） ---");
			stringBuilder.AppendLine(string.Format("Current value: {0:F2}", -this._stats.LastNewMetric));
			stringBuilder.AppendLine(string.Format("Baseline value: {0:F2}", -this._baselineNewMetric));
			stringBuilder.AppendLine(string.Format("Avg value: {0:F2}", -this._stats.AvgNewMetric));
			stringBuilder.AppendLine(string.Format("Min value: {0:F2}", -this._stats.MaxNewMetric));
			stringBuilder.AppendLine(string.Format("Max value: {0:F2}", -this._stats.MinNewMetric));
			stringBuilder.AppendLine(string.Format("Value Trend: {0:F3} /step", -this._stats.NewMetricTrend));
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("--- 平均IPC---");
			stringBuilder.AppendLine(string.Format("Current value: {0:F2}", this._stats.LastExtraMetric));
			stringBuilder.AppendLine(string.Format("Baseline value: {0:F2}", this._baselineExtraMetric));
			stringBuilder.AppendLine(string.Format("Avg value: {0:F2}", this._stats.AvgExtraMetric));
			stringBuilder.AppendLine(string.Format("Min value: {0:F2}", this._stats.MinExtraMetric));
			stringBuilder.AppendLine(string.Format("Max value: {0:F2}", this._stats.MaxExtraMetric));
			stringBuilder.AppendLine(string.Format("Value Trend: {0:F3} /step", this._stats.ExtraMetricTrend));
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("--- Learning Status ---");
			stringBuilder.AppendLine(string.Format("Total Decisions: {0}", this._stats.TotalDecisions));
			stringBuilder.AppendLine(string.Format("Learning Updates: {0}", this._stats.LearningUpdates));
			stringBuilder.AppendLine(string.Format("Experience Used: {0}", this._stats.ExperienceUsed));
			stringBuilder.AppendLine(string.Format("Experience Skipped: {0}", this._stats.ExperienceSkipped));
			stringBuilder.AppendLine(string.Format("Learning Rate: {0:F7}", this._currentLearningRate));
			stringBuilder.AppendLine(string.Format("Exploration Rate: {0:P1}", this._explorationRate));
			stringBuilder.AppendLine(string.Format("Avg Loss: {0:F6}", this._stats.AvgLoss));
			stringBuilder.AppendLine();
			float num = this.ComputePolicyEntropy();
			stringBuilder.AppendLine(string.Format("Policy Entropy: {0:F4} (max={1:F2})", num, (this._stats.TotalDecisions > 0L) ? Math.Log((double)this._stats.CoreSelectionCounts.Length) : 0.0));
			if (this._stats.RewardTrend > 0f && this._stats.PositiveRewardRatio > 0.5f)
			{
				stringBuilder.AppendLine("Status: LEARNING IMPROVING");
			}
			else if (this._stats.RewardTrend < -0.001f || this._stats.PositiveRewardRatio < 0.3f)
			{
				stringBuilder.AppendLine("Status: NEEDS TUNING");
			}
			else
			{
				stringBuilder.AppendLine("Status: STABLE");
			}
			return stringBuilder.ToString();
		}

		// Token: 0x060000EF RID: 239 RVA: 0x0000C7E0 File Offset: 0x0000A9E0
		public float ComputePolicyEntropy()
		{
			if (this._stats.TotalDecisions == 0L)
			{
				return 0f;
			}
			float num = 0f;
			int num2 = 0;
			for (int i = 0; i < this._stats.CoreSelectionCounts.Length; i++)
			{
				if (this._stats.CoreSelectionCounts[i] > 0)
				{
					num2++;
					float num3 = (float)this._stats.CoreSelectionCounts[i] / (float)this._stats.TotalDecisions;
					if (num3 > 1E-10f)
					{
						num -= num3 * (float)Math.Log((double)num3);
					}
				}
			}
			return num;
		}

		// Token: 0x060000F0 RID: 240 RVA: 0x0000C868 File Offset: 0x0000AA68
		public void ResetStatistics()
		{
			this._stats.TotalDecisions = 0L;
			this._stats.TotalInferenceTimeUs = 0L;
			this._stats.TotalThreadEncodeTimeUs = 0L;
			this._stats.TotalCoreEncodeTimeUs = 0L;
			this._stats.TotalCrossAttnTimeUs = 0L;
			this._stats.LearningUpdates = 0;
			this._stats.ExperienceUsed = 0;
			this._stats.ExperienceSkipped = 0;
			this._stats.AvgLoss = 0f;
			this._stats.RecentAvgReward = 0f;
			this._stats.MigrationCount = 0;
			this._stats.CacheHits = 0;
			this._stats.CacheMisses = 0;
			this._stats.TotalTATSamples = 0L;
			this._stats.AvgTAT = 0f;
			this._stats.MinTAT = 0f;
			this._stats.MaxTAT = 0f;
			this._stats.RewardTrend = 0f;
			this._stats.TATTrend = 0f;
			this._stats.PositiveRewardRatio = 0f;
			this._stats.LastReward = 0f;
			this._stats.LastTATDelta = 0f;
			this._stats.TotalEnergySamples = 0L;
			this._stats.AvgEnergy = 0f;
			this._stats.MinEnergy = 0f;
			this._stats.MaxEnergy = 0f;
			this._stats.BaselineEnergy = 0f;
			this._stats.EnergyTrend = 0f;
			this._stats.LastEnergy = 0f;
			this._stats.TotalNewMetricSamples = 0L;
			this._stats.AvgNewMetric = 0f;
			this._stats.MinNewMetric = 0f;
			this._stats.MaxNewMetric = 0f;
			this._stats.BaselineNewMetric = 0f;
			this._stats.NewMetricTrend = 0f;
			this._stats.LastNewMetric = 0f;
			this._stats.TotalExtraMetricSamples = 0L;
			this._stats.AvgExtraMetric = 0f;
			this._stats.MinExtraMetric = 0f;
			this._stats.MaxExtraMetric = 0f;
			this._stats.BaselineExtraMetric = 0f;
			this._stats.ExtraMetricTrend = 0f;
			this._stats.LastExtraMetric = 0f;
			this._stats.TotalLoadBalanceSamples = 0L;
			this._stats.AvgLoadBalance = 0f;
			this._stats.MinLoadBalance = 0f;
			this._stats.MaxLoadBalance = 0f;
			this._stats.BaselineLoadBalance = 0f;
			this._stats.LoadBalanceTrend = 0f;
			this._stats.LastLoadBalance = 0f;
			this._stats.TotalLoadBalance2Samples = 0L;
			this._stats.AvgLoadBalance2 = 0f;
			this._stats.MinLoadBalance2 = 0f;
			this._stats.MaxLoadBalance2 = 0f;
			this._stats.BaselineLoadBalance2 = 0f;
			this._stats.LoadBalance2Trend = 0f;
			this._stats.LastLoadBalance2 = 0f;
			MathHelper.Clear(this._stats.CoreSelectionCounts.AsSpan<int>());
			this._baselineTAT = 0f;
			this._baselineEnergy = 0f;
			this._baselineNewMetric = 0f;
			this._baselineExtraMetric = 0f;
			this._baselineLoadBalance = 0f;
			this._baselineLoadBalance2 = 0f;
			this._sumTAT = 0f;
			this._tatIndex = 0;
			this._lastTAT = 0f;
			this._windowIndex = 0;
			this._windowStartTick = DateTime.Now.Ticks;
			this._normalizationReady = false;
			MathHelper.Clear(this._tatHistory.AsSpan<float>());
		}

		// Token: 0x060000F1 RID: 241 RVA: 0x0000CC74 File Offset: 0x0000AE74
		public void SaveModel(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				path = "./scheduler_model.bin";
			}
			using (BinaryWriter binaryWriter = new BinaryWriter(File.OpenWrite(path)))
			{
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
				this.WriteLayer(binaryWriter, this._threadEmbedding);
				this.WriteLayer(binaryWriter, this._coreEmbedding);
				for (int i = 0; i < 2; i++)
				{
					this.WriteAttention(binaryWriter, this._crossAttentions[i]);
				}
				this.WriteFFNBlock(binaryWriter, this._threadEncoder);
				for (int j = 0; j < 3; j++)
				{
					this.WriteCoreEncoder(binaryWriter, this._coreEncoders[j]);
				}
				for (int k = 0; k < 6; k++)
				{
					binaryWriter.Write(this._threadFeatureMean[k]);
				}
				for (int l = 0; l < 6; l++)
				{
					binaryWriter.Write(this._threadFeatureStd[l]);
				}
				for (int m = 0; m < 7; m++)
				{
					binaryWriter.Write(this._coreFeatureMean[m]);
				}
				for (int n = 0; n < 7; n++)
				{
					binaryWriter.Write(this._coreFeatureStd[n]);
				}
				for (int num = 0; num < this._coreIdEmbedding.Length; num++)
				{
					binaryWriter.Write(this._coreIdEmbedding[num]);
				}
				for (int num2 = 0; num2 < this._coreTypeEmbedding.Length; num2++)
				{
					binaryWriter.Write(this._coreTypeEmbedding[num2]);
				}
				binaryWriter.Write(this._startTick);
				binaryWriter.Write(this._baselineExtraMetric);
			}
		}

		// Token: 0x060000F2 RID: 242 RVA: 0x0000CE50 File Offset: 0x0000B050
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
			bool flag;
			try
			{
				using (BinaryReader binaryReader = new BinaryReader(File.OpenRead(path)))
				{
					if (binaryReader.ReadString() != "TSC1")
					{
						flag = false;
					}
					else
					{
						int num = binaryReader.ReadInt32();
						if (num != 11)
						{
							flag = false;
						}
						else
						{
							int num2 = binaryReader.ReadInt32();
							int num3 = binaryReader.ReadInt32();
							int num4 = binaryReader.ReadInt32();
							if (num2 != 64 || num3 != 4 || num4 != 256)
							{
								flag = false;
							}
							else
							{
								int num5 = binaryReader.ReadInt32();
								int num6 = binaryReader.ReadInt32();
								int num7 = binaryReader.ReadInt32();
								int num8 = binaryReader.ReadInt32();
								if (num5 != 64 || num6 != 14 || num7 != 15 || num8 != 3)
								{
									flag = false;
								}
								else if (binaryReader.ReadInt32() != 2)
								{
									flag = false;
								}
								else
								{
									this.ReadLayer(binaryReader, this._threadEmbedding);
									this.ReadLayer(binaryReader, this._coreEmbedding);
									for (int i = 0; i < 2; i++)
									{
										this.ReadAttention(binaryReader, this._crossAttentions[i]);
									}
									this.ReadFFNBlock(binaryReader, this._threadEncoder);
									for (int j = 0; j < 3; j++)
									{
										this.ReadCoreEncoder(binaryReader, this._coreEncoders[j]);
									}
									for (int k = 0; k < 6; k++)
									{
										this._threadFeatureMean[k] = binaryReader.ReadSingle();
									}
									for (int l = 0; l < 6; l++)
									{
										this._threadFeatureStd[l] = binaryReader.ReadSingle();
									}
									for (int m = 0; m < 7; m++)
									{
										this._coreFeatureMean[m] = binaryReader.ReadSingle();
									}
									for (int n = 0; n < 7; n++)
									{
										this._coreFeatureStd[n] = binaryReader.ReadSingle();
									}
									for (int num9 = 0; num9 < this._coreIdEmbedding.Length; num9++)
									{
										this._coreIdEmbedding[num9] = binaryReader.ReadSingle();
									}
									if (num >= 5)
									{
										for (int num10 = 0; num10 < this._coreTypeEmbedding.Length; num10++)
										{
											this._coreTypeEmbedding[num10] = binaryReader.ReadSingle();
										}
									}
									if (num >= 2)
									{
										this._startTick = binaryReader.ReadInt64();
									}
									if (num >= 11)
									{
										this._baselineExtraMetric = binaryReader.ReadSingle();
									}
									this._normalizationReady = true;
									flag = true;
								}
							}
						}
					}
				}
			}
			catch
			{
				flag = false;
			}
			return flag;
		}

		// Token: 0x060000F3 RID: 243 RVA: 0x0000D0D0 File Offset: 0x0000B2D0
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

		// Token: 0x060000F4 RID: 244 RVA: 0x0000D120 File Offset: 0x0000B320
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

		// Token: 0x060000F5 RID: 245 RVA: 0x0000D16F File Offset: 0x0000B36F
		private void WriteAttention(BinaryWriter writer, MultiHeadAttention attention)
		{
			this.WriteLayer(writer, attention.Wq);
			this.WriteLayer(writer, attention.Wk);
			this.WriteLayer(writer, attention.Wv);
			this.WriteLayer(writer, attention.Wo);
		}

		// Token: 0x060000F6 RID: 246 RVA: 0x0000D1A5 File Offset: 0x0000B3A5
		private void ReadAttention(BinaryReader reader, MultiHeadAttention attention)
		{
			this.ReadLayer(reader, attention.Wq);
			this.ReadLayer(reader, attention.Wk);
			this.ReadLayer(reader, attention.Wv);
			this.ReadLayer(reader, attention.Wo);
		}

		// Token: 0x060000F7 RID: 247 RVA: 0x0000D1DC File Offset: 0x0000B3DC
		private void WriteEncoder(BinaryWriter writer, CoreTransformerEncoder encoder)
		{
			this.WriteAttention(writer, encoder.SelfAttention);
			this.WriteLayer(writer, encoder.FeedForward.FC1);
			this.WriteLayer(writer, encoder.FeedForward.FC2);
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

		// Token: 0x060000F8 RID: 248 RVA: 0x0000D2C8 File Offset: 0x0000B4C8
		private void ReadEncoder(BinaryReader reader, CoreTransformerEncoder encoder)
		{
			this.ReadAttention(reader, encoder.SelfAttention);
			this.ReadLayer(reader, encoder.FeedForward.FC1);
			this.ReadLayer(reader, encoder.FeedForward.FC2);
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

		// Token: 0x060000F9 RID: 249 RVA: 0x0000D3B4 File Offset: 0x0000B5B4
		private void WriteFFNBlock(BinaryWriter writer, ThreadFFNBlock block)
		{
			this.WriteLayer(writer, block.FeedForward.FC1);
			this.WriteLayer(writer, block.FeedForward.FC2);
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

		// Token: 0x060000FA RID: 250 RVA: 0x0000D494 File Offset: 0x0000B694
		private void ReadFFNBlock(BinaryReader reader, ThreadFFNBlock block)
		{
			this.ReadLayer(reader, block.FeedForward.FC1);
			this.ReadLayer(reader, block.FeedForward.FC2);
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

		// Token: 0x060000FB RID: 251 RVA: 0x0000D574 File Offset: 0x0000B774
		private void WriteCoreEncoder(BinaryWriter writer, CoreTransformerEncoder encoder)
		{
			this.WriteAttention(writer, encoder.SelfAttention);
			this.WriteLayer(writer, encoder.FeedForward.FC1);
			this.WriteLayer(writer, encoder.FeedForward.FC2);
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

		// Token: 0x060000FC RID: 252 RVA: 0x0000D660 File Offset: 0x0000B860
		private void ReadCoreEncoder(BinaryReader reader, CoreTransformerEncoder encoder)
		{
			this.ReadAttention(reader, encoder.SelfAttention);
			this.ReadLayer(reader, encoder.FeedForward.FC1);
			this.ReadLayer(reader, encoder.FeedForward.FC2);
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

		// Token: 0x04000145 RID: 325
		private static readonly Random _random = new Random(42);

		// Token: 0x04000146 RID: 326
		private const int D_MODEL = 64;

		// Token: 0x04000147 RID: 327
		private const int N_HEAD = 4;

		// Token: 0x04000148 RID: 328
		private const int D_FF = 256;

		// Token: 0x04000149 RID: 329
		private const int NUM_CORE_ENCODER_LAYERS = 3;

		// Token: 0x0400014A RID: 330
		private const int NUM_CROSS_ATTN_LAYERS = 2;

		// Token: 0x0400014B RID: 331
		private const int MAX_CORES = 64;

		// Token: 0x0400014C RID: 332
		private const int THREAD_RAW_DIM = 6;

		// Token: 0x0400014D RID: 333
		private const int CORE_RAW_DIM = 7;

		// Token: 0x0400014E RID: 334
		private const int CORE_ID_EMBED_DIM = 4;

		// Token: 0x0400014F RID: 335
		private const int CORE_TYPE_EMBED_DIM = 4;

		// Token: 0x04000150 RID: 336
		private const int MAX_CORE_TYPES = 7;

		// Token: 0x04000151 RID: 337
		private const int THREAD_INPUT_DIM = 8;

		// Token: 0x04000152 RID: 338
		private const int CORE_INPUT_DIM = 9;

		// Token: 0x04000153 RID: 339
		private const int THREAD_FEATURE_DIM = 14;

		// Token: 0x04000154 RID: 340
		private const int CORE_FEATURE_DIM = 15;

		// Token: 0x04000155 RID: 341
		private const int HISTORY_CAPACITY = 10000;

		// Token: 0x04000156 RID: 342
		private const long WINDOW_TICKS = 600000000L;

		// Token: 0x04000157 RID: 343
		private const int NORMALIZATION_WINDOW_SIZE = 1000;

		// Token: 0x04000158 RID: 344
		private float ENERGY_WEIGHT;

		// Token: 0x04000159 RID: 345
		private float NEW_METRIC_WEIGHT;

		// Token: 0x0400015A RID: 346
		private float LOAD_BALANCE_WEIGHT = 0.3f;

		// Token: 0x0400015B RID: 347
		private float LOAD_BALANCE2_WEIGHT;

		// Token: 0x0400015C RID: 348
		private float EXTRA_METRIC_WEIGHT;

		// Token: 0x0400015D RID: 349
		private const float LOAD_BALANCE_DECAY = 1.5f;

		// Token: 0x0400015E RID: 350
		private readonly LinearLayer _threadEmbedding;

		// Token: 0x0400015F RID: 351
		private readonly LinearLayer _coreEmbedding;

		// Token: 0x04000160 RID: 352
		private readonly ThreadFFNBlock _threadEncoder;

		// Token: 0x04000161 RID: 353
		private readonly CoreTransformerEncoder[] _coreEncoders;

		// Token: 0x04000162 RID: 354
		private readonly MultiHeadAttention[] _crossAttentions;

		// Token: 0x04000163 RID: 355
		private readonly float[] _threadEmbed;

		// Token: 0x04000164 RID: 356
		private readonly float[] _threadEncoded;

		// Token: 0x04000165 RID: 357
		private readonly float[] _crossIntermediate;

		// Token: 0x04000166 RID: 358
		private readonly float[] _crossOutput;

		// Token: 0x04000167 RID: 359
		private readonly float[] _coreScore;

		// Token: 0x04000168 RID: 360
		private readonly float[] _attentionWeights;

		// Token: 0x04000169 RID: 361
		private readonly float[][] _coreEmbeddings;

		// Token: 0x0400016A RID: 362
		private readonly float[][] _coreEncoded;

		// Token: 0x0400016B RID: 363
		private readonly float[][] _coreIntermediate1;

		// Token: 0x0400016C RID: 364
		private readonly float[][] _coreIntermediate2;

		// Token: 0x0400016D RID: 365
		private int _batchFeatureHash;

		// Token: 0x0400016E RID: 366
		private readonly float[][] _coreInputs;

		// Token: 0x0400016F RID: 367
		private bool _batchCacheValid;

		// Token: 0x04000170 RID: 368
		private readonly CircularBuffer<DecisionRecord> _history;

		// Token: 0x04000171 RID: 369
		private readonly SchedulerStatistics _stats;

		// Token: 0x04000172 RID: 370
		private DecisionRecord _currentRecord;

		// Token: 0x04000173 RID: 371
		private readonly float[] _threadFeatureMean;

		// Token: 0x04000174 RID: 372
		private readonly float[] _threadFeatureStd;

		// Token: 0x04000175 RID: 373
		private readonly float[] _coreFeatureMean;

		// Token: 0x04000176 RID: 374
		private readonly float[] _coreFeatureStd;

		// Token: 0x04000177 RID: 375
		private readonly float[][] _threadFeatureWindow;

		// Token: 0x04000178 RID: 376
		private readonly float[][] _coreFeatureWindow;

		// Token: 0x04000179 RID: 377
		private int _windowIndex;

		// Token: 0x0400017A RID: 378
		private long _windowStartTick;

		// Token: 0x0400017B RID: 379
		private bool _normalizationReady;

		// Token: 0x0400017C RID: 380
		private readonly float[] _tatHistory;

		// Token: 0x0400017D RID: 381
		private int _tatIndex;

		// Token: 0x0400017E RID: 382
		private float _sumTAT;

		// Token: 0x0400017F RID: 383
		private const int TAT_HISTORY_SIZE = 1000;

		// Token: 0x04000180 RID: 384
		private readonly float[] _gradCrossOutput;

		// Token: 0x04000181 RID: 385
		private readonly float[] _gradThreadEncoded;

		// Token: 0x04000182 RID: 386
		private readonly float[] _gradThreadEmbed;

		// Token: 0x04000183 RID: 387
		private readonly float[] _coreIdEmbedding;

		// Token: 0x04000184 RID: 388
		private readonly float[] _coreTypeEmbedding;

		// Token: 0x04000185 RID: 389
		private readonly float[] _coreIdEmbeddingGrad;

		// Token: 0x04000186 RID: 390
		private readonly float[] _coreTypeEmbeddingGrad;

		// Token: 0x04000187 RID: 391
		private readonly float[] _normThreadBuf;

		// Token: 0x04000188 RID: 392
		private readonly float[] _normCoreBuf;

		// Token: 0x04000189 RID: 393
		private readonly float[] _normCoreBwdBuf;

		// Token: 0x0400018A RID: 394
		private readonly float[] _gradCoreEncodedBuf;

		// Token: 0x0400018B RID: 395
		private readonly float[][] _gradCrossKVPerCore;

		// Token: 0x0400018C RID: 396
		private readonly float[] _coreEmbedWeightGradAccum;

		// Token: 0x0400018D RID: 397
		private readonly float[] _coreEmbedBiasGradAccum;

		// Token: 0x0400018E RID: 398
		private float _lastTAT;

		// Token: 0x0400018F RID: 399
		private float _baselineTAT;

		// Token: 0x04000190 RID: 400
		private float _baselineEnergy;

		// Token: 0x04000191 RID: 401
		private float _baselineNewMetric;

		// Token: 0x04000192 RID: 402
		private float _baselineLoadBalance;

		// Token: 0x04000193 RID: 403
		private float _baselineLoadBalance2;

		// Token: 0x04000194 RID: 404
		private float _baselineExtraMetric;

		// Token: 0x04000195 RID: 405
		private const float LEARNING_RATE = 0.0001f;

		// Token: 0x04000196 RID: 406
		private int _decisionsInCurrentSecond;

		// Token: 0x04000197 RID: 407
		private long _lastTATUpdateTick;

		// Token: 0x04000198 RID: 408
		private float _explorationRate;

		// Token: 0x04000199 RID: 409
		private float _minExplorationRate;

		// Token: 0x0400019A RID: 410
		public long _startTick;

		// Token: 0x0400019B RID: 411
		private const long INITIAL_LEARNING_PHASE_MS = 600000L;

		// Token: 0x0400019C RID: 412
		private const long RAPID_LEARNING_PHASE_MS = 120000L;

		// Token: 0x0400019D RID: 413
		private const float RAPID_LEARNING_RATE = 0.01f;

		// Token: 0x0400019E RID: 414
		private const float INITIAL_LEARNING_RATE = 0.01f;

		// Token: 0x0400019F RID: 415
		private const float MIN_LEARNING_RATE = 0.0001f;

		// Token: 0x040001A0 RID: 416
		private const float MAX_LEARNING_RATE = 0.01f;

		// Token: 0x040001A1 RID: 417
		private float _currentLearningRate = 0.01f;

		// Token: 0x040001A2 RID: 418
		private bool _learningEnabled = true;

		// Token: 0x040001A3 RID: 419
		private const long LEARNING_RATE_DECAY_DURATION = 36000000000L;

		// Token: 0x040001A4 RID: 420
		private const float INITIAL_EXPLORATION_RATE = 0f;

		// Token: 0x040001A5 RID: 421
		private const float INITIAL_SOFTMAX_TEMPERATURE = 2f;

		// Token: 0x040001A6 RID: 422
		private const long TEMPERATURE_ANNEAL_MS = 120000L;

		// Token: 0x040001A7 RID: 423
		private const long BATCH_TRAIN_INTERVAL = 600000000L;

		// Token: 0x040001A8 RID: 424
		private long _lastBatchTrainTick;

		// Token: 0x040001A9 RID: 425
		private const int BATCH_SAMPLE_SIZE = 1000;

		// Token: 0x040001AA RID: 426
		private readonly float[] _learningWeights;

		// Token: 0x040001AB RID: 427
		private const int MAX_DECISIONS_PER_SECOND = 10000;

		// Token: 0x040001AC RID: 428
		private int _topK = 3;

		// Token: 0x040001AD RID: 429
		private readonly int[] _reportMaxCores;

		// Token: 0x040001AE RID: 430
		private readonly float[] _reportMaxWeights;

		// Token: 0x040001AF RID: 431
		private readonly float[] _lastAttentionWeightsBuffer;

		// Token: 0x040001B0 RID: 432
		private const string MODEL_MAGIC = "TSC1";

		// Token: 0x040001B1 RID: 433
		private const int MODEL_VERSION = 11;

		// Token: 0x040001B2 RID: 434
		private const string DEFAULT_MODEL_PATH = "./scheduler_model.bin";
	}
}
