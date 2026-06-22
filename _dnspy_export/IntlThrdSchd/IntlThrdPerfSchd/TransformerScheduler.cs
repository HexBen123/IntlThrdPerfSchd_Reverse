using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using SimdLibrary;

namespace IntlThrdPerfSchd
{
	// Token: 0x0200000E RID: 14
	public class TransformerScheduler
	{
		// Token: 0x060000BF RID: 191 RVA: 0x00008426 File Offset: 0x00006626
		public void SetTopK(int k)
		{
			this._topK = Math.Max(1, k);
		}

		// Token: 0x060000C0 RID: 192 RVA: 0x00008435 File Offset: 0x00006635
		public int GetTopK()
		{
			return this._topK;
		}

		// Token: 0x060000C1 RID: 193 RVA: 0x0000843D File Offset: 0x0000663D
		public void SetLearningEnabled(bool enabled)
		{
			this._learningEnabled = enabled;
		}

		// Token: 0x060000C2 RID: 194 RVA: 0x00008446 File Offset: 0x00006646
		public bool GetLearningEnabled()
		{
			return this._learningEnabled;
		}

		// Token: 0x1700001F RID: 31
		// (get) Token: 0x060000C3 RID: 195 RVA: 0x0000844E File Offset: 0x0000664E
		public SchedulerStatistics Statistics
		{
			get
			{
				return this._stats;
			}
		}

		// Token: 0x060000C4 RID: 196 RVA: 0x00008458 File Offset: 0x00006658
		public TransformerScheduler()
		{
			this._threadEmbedding = new LinearLayer(15, 64);
			this._coreEmbedding = new LinearLayer(16, 64);
			this._threadEncoder = new ThreadTransformerEncoder();
			this._coreEncoder = new CoreTransformerEncoder();
			this._crossAttention = new MultiHeadAttention(64, 8);
			this._scoreHead = new LinearLayer(64, 1);
			this._threadEmbed = new float[64];
			this._threadEncoded = new float[64];
			this._crossOutput = new float[64];
			this._coreScore = new float[64];
			this._attentionWeights = new float[64];
			this._coreEmbeddings = new float[64][];
			this._coreEncoded = new float[64][];
			this._coreFeatureHash = new int[64];
			this._coreCacheValid = new bool[64];
			for (int i = 0; i < 64; i++)
			{
				this._coreEmbeddings[i] = new float[64];
				this._coreEncoded[i] = new float[64];
				this._coreCacheValid[i] = false;
			}
			this._history = new CircularBuffer<DecisionRecord>(10000);
			this._stats = new SchedulerStatistics(64);
			this._currentRecord = new DecisionRecord(64);
			this._threadFeatureMean = new float[7];
			this._threadFeatureStd = new float[7];
			this._coreFeatureMean = new float[8];
			this._coreFeatureStd = new float[8];
			this._threadFeatureWindow = new float[1000][];
			this._coreFeatureWindow = new float[1000][];
			for (int j = 0; j < 1000; j++)
			{
				this._threadFeatureWindow[j] = new float[8];
				this._coreFeatureWindow[j] = new float[9];
			}
			this._windowIndex = 0;
			this._windowStartTick = DateTime.Now.Ticks;
			this._normalizationReady = false;
			for (int k = 0; k < 7; k++)
			{
				this._threadFeatureStd[k] = 1f;
			}
			for (int l = 0; l < 8; l++)
			{
				this._coreFeatureStd[l] = 1f;
			}
			this._coreIdEmbedding = new float[512];
			float num = (float)Math.Sqrt(0.25);
			for (int m = 0; m < this._coreIdEmbedding.Length; m++)
			{
				this._coreIdEmbedding[m] = (float)(TransformerScheduler._random.NextDouble() * 2.0 - 1.0) * num;
			}
			this._tatHistory = new float[1000];
			this._tatIndex = 0;
			this._sumTAT = 0f;
			this._lastTAT = 0f;
			this._baselineTAT = 0f;
			this._baselineEnergy = 0f;
			this._startTick = DateTime.Now.Ticks;
			this._lastBatchTrainTick = this._startTick;
			this._gradScoreHead = new float[1];
			this._gradCrossOutput = new float[64];
			this._gradThreadEncoded = new float[64];
			this._gradThreadEmbed = new float[64];
			this._learningWeights = new float[10000];
			this._reportMaxCores = new int[8];
			this._reportMaxWeights = new float[8];
			this._lastAttentionWeightsBuffer = new float[64];
			this._normThreadBuf = new float[15];
			this._normCoreBuf = new float[16];
			this._normCoreBwdBuf = new float[16];
			this._gradCoreEncodedBuf = new float[64];
			this._scoreResultBuf = new float[1];
		}

		// Token: 0x060000C5 RID: 197 RVA: 0x000087F8 File Offset: 0x000069F8
		public TransformerScheduler(string modelPath)
		{
			this._threadEmbedding = new LinearLayer(15, 64);
			this._coreEmbedding = new LinearLayer(16, 64);
			this._threadEncoder = new ThreadTransformerEncoder();
			this._coreEncoder = new CoreTransformerEncoder();
			this._crossAttention = new MultiHeadAttention(64, 8);
			this._scoreHead = new LinearLayer(64, 1);
			this._threadEmbed = new float[64];
			this._threadEncoded = new float[64];
			this._crossOutput = new float[64];
			this._coreScore = new float[64];
			this._attentionWeights = new float[64];
			this._coreEmbeddings = new float[64][];
			this._coreEncoded = new float[64][];
			this._coreFeatureHash = new int[64];
			this._coreCacheValid = new bool[64];
			for (int i = 0; i < 64; i++)
			{
				this._coreEmbeddings[i] = new float[64];
				this._coreEncoded[i] = new float[64];
				this._coreCacheValid[i] = false;
			}
			this._history = new CircularBuffer<DecisionRecord>(10000);
			this._stats = new SchedulerStatistics(64);
			this._currentRecord = new DecisionRecord(64);
			this._threadFeatureMean = new float[7];
			this._threadFeatureStd = new float[7];
			this._coreFeatureMean = new float[8];
			this._coreFeatureStd = new float[8];
			this._threadFeatureWindow = new float[1000][];
			this._coreFeatureWindow = new float[1000][];
			for (int j = 0; j < 1000; j++)
			{
				this._threadFeatureWindow[j] = new float[8];
				this._coreFeatureWindow[j] = new float[9];
			}
			this._windowIndex = 0;
			this._windowStartTick = DateTime.Now.Ticks;
			this._normalizationReady = false;
			for (int k = 0; k < 7; k++)
			{
				this._threadFeatureStd[k] = 1f;
			}
			for (int l = 0; l < 8; l++)
			{
				this._coreFeatureStd[l] = 1f;
			}
			this._coreIdEmbedding = new float[512];
			float num = (float)Math.Sqrt(0.25);
			for (int m = 0; m < this._coreIdEmbedding.Length; m++)
			{
				this._coreIdEmbedding[m] = (float)(TransformerScheduler._random.NextDouble() * 2.0 - 1.0) * num;
			}
			this._tatHistory = new float[1000];
			this._tatIndex = 0;
			this._sumTAT = 0f;
			this._lastTAT = 0f;
			this._baselineTAT = 0f;
			this._baselineEnergy = 0f;
			this._startTick = DateTime.Now.Ticks;
			this._lastBatchTrainTick = this._startTick;
			this._gradScoreHead = new float[1];
			this._gradCrossOutput = new float[64];
			this._gradThreadEncoded = new float[64];
			this._gradThreadEmbed = new float[64];
			this._learningWeights = new float[10000];
			this._reportMaxCores = new int[8];
			this._reportMaxWeights = new float[8];
			this._lastAttentionWeightsBuffer = new float[64];
			this._normThreadBuf = new float[15];
			this._normCoreBuf = new float[16];
			this._normCoreBwdBuf = new float[16];
			this._gradCoreEncodedBuf = new float[64];
			this._scoreResultBuf = new float[1];
			if (!string.IsNullOrEmpty(modelPath) && File.Exists(modelPath))
			{
				this.LoadModel(modelPath);
			}
		}

		// Token: 0x060000C6 RID: 198 RVA: 0x00008BB0 File Offset: 0x00006DB0
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

		// Token: 0x060000C7 RID: 199 RVA: 0x00008BEC File Offset: 0x00006DEC
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe void CopyCoreIdEmbedding(int coreId, Span<float> destination)
		{
			int num = coreId * 8;
			for (int i = 0; i < 8; i++)
			{
				*destination[i] = this._coreIdEmbedding[num + i];
			}
		}

		// Token: 0x060000C8 RID: 200 RVA: 0x00008C1C File Offset: 0x00006E1C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe void BuildThreadInput(ReadOnlySpan<float> threadFeatures, Span<float> output)
		{
			this.NormalizeFeatures(threadFeatures.Slice(0, 7), output.Slice(0, 7), this._threadFeatureMean, this._threadFeatureStd);
			int num = (int)(*threadFeatures[7]);
			this.CopyCoreIdEmbedding(num, output.Slice(7));
		}

		// Token: 0x060000C9 RID: 201 RVA: 0x00008C74 File Offset: 0x00006E74
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe void BuildCoreInput(ReadOnlySpan<float> coreFeatures, Span<float> output)
		{
			this.NormalizeFeatures(coreFeatures.Slice(0, 8), output.Slice(0, 8), this._coreFeatureMean, this._coreFeatureStd);
			int num = (int)(*coreFeatures[8]);
			this.CopyCoreIdEmbedding(num, output.Slice(8));
		}

		// Token: 0x060000CA RID: 202 RVA: 0x00008CCC File Offset: 0x00006ECC
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

		// Token: 0x060000CB RID: 203 RVA: 0x00008D50 File Offset: 0x00006F50
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

		// Token: 0x060000CC RID: 204 RVA: 0x00008E1C File Offset: 0x0000701C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void ComputeWindowStatistics()
		{
			if (this._windowIndex == 0)
			{
				return;
			}
			MathHelper.Clear(this._threadFeatureMean.AsSpan(0, 7));
			MathHelper.Clear(this._coreFeatureMean.AsSpan(0, 8));
			MathHelper.Clear(this._threadFeatureStd.AsSpan(0, 7));
			MathHelper.Clear(this._coreFeatureStd.AsSpan(0, 8));
			for (int i = 0; i < this._windowIndex; i++)
			{
				for (int j = 0; j < 7; j++)
				{
					this._threadFeatureMean[j] += this._threadFeatureWindow[i][j];
				}
				for (int k = 0; k < 8; k++)
				{
					this._coreFeatureMean[k] += this._coreFeatureWindow[i][k];
				}
			}
			float num = 1f / (float)this._windowIndex;
			for (int l = 0; l < 7; l++)
			{
				this._threadFeatureMean[l] *= num;
			}
			for (int m = 0; m < 8; m++)
			{
				this._coreFeatureMean[m] *= num;
			}
			for (int n = 0; n < this._windowIndex; n++)
			{
				for (int num2 = 0; num2 < 7; num2++)
				{
					float num3 = this._threadFeatureWindow[n][num2] - this._threadFeatureMean[num2];
					this._threadFeatureStd[num2] += num3 * num3;
				}
				for (int num4 = 0; num4 < 8; num4++)
				{
					float num5 = this._coreFeatureWindow[n][num4] - this._coreFeatureMean[num4];
					this._coreFeatureStd[num4] += num5 * num5;
				}
			}
			for (int num6 = 0; num6 < 7; num6++)
			{
				this._threadFeatureStd[num6] = (float)Math.Sqrt((double)(this._threadFeatureStd[num6] / (float)this._windowIndex + 1E-06f));
			}
			for (int num7 = 0; num7 < 8; num7++)
			{
				this._coreFeatureStd[num7] = (float)Math.Sqrt((double)(this._coreFeatureStd[num7] / (float)this._windowIndex + 1E-06f));
			}
		}

		// Token: 0x060000CD RID: 205 RVA: 0x00009030 File Offset: 0x00007230
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void UpdateCoreEmbedding(ReadOnlySpan<float> coreFeatures, int coreIndex, Span<float> normalized, Span<float> encodedOutput)
		{
			this.BuildCoreInput(coreFeatures, normalized);
			this._coreEmbedding.Forward(normalized, this._coreEmbeddings[coreIndex]);
			this._coreEncoder.Forward(this._coreEmbeddings[coreIndex], encodedOutput);
		}

		// Token: 0x060000CE RID: 206 RVA: 0x0000907D File Offset: 0x0000727D
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int Schedule(ReadOnlySpan<float> threadFeatures, float[][] coreFeatures, int numCores)
		{
			return this.Schedule(threadFeatures, coreFeatures, numCores, -1, -1, -1);
		}

		// Token: 0x060000CF RID: 207 RVA: 0x0000908B File Offset: 0x0000728B
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int Schedule(ReadOnlySpan<float> threadFeatures, float[][] coreFeatures, int numCores, int predictedCore, int actualCore)
		{
			return this.Schedule(threadFeatures, coreFeatures, numCores, -1, predictedCore, actualCore);
		}

		// Token: 0x060000D0 RID: 208 RVA: 0x0000909C File Offset: 0x0000729C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int Schedule(ReadOnlySpan<float> threadFeatures, float[][] coreFeatures, int numCores, int threadId, int predictedCore, int actualCore)
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
			this.UpdateNormalizationFixedWindow(threadFeatures, coreFeatures, numCores);
			this.BuildThreadInput(threadFeatures, this._normThreadBuf);
			this._threadEmbedding.Forward(this._normThreadBuf, this._threadEmbed);
			this._threadEncoder.Forward(this._threadEmbed, this._threadEncoded);
			for (int j = 0; j < numCores; j++)
			{
				ReadOnlySpan<float> readOnlySpan = new ReadOnlySpan<float>(coreFeatures[j], 0, 9);
				int num = this.ComputeFeatureHash(readOnlySpan);
				if (this._coreFeatureHash[j] != num || !this._coreCacheValid[j])
				{
					this.BuildCoreInput(readOnlySpan, this._normCoreBuf);
					this._coreEmbedding.Forward(this._normCoreBuf, this._coreEmbeddings[j]);
					this._coreEncoder.Forward(this._coreEmbeddings[j], this._coreEncoded[j].AsSpan<float>());
					this._coreFeatureHash[j] = num;
					this._coreCacheValid[j] = true;
					this._stats.CacheMisses++;
				}
				else
				{
					this._stats.CacheHits++;
				}
			}
			Span<float> span = this._attentionWeights.AsSpan(0, numCores);
			this._crossAttention.CrossAttention(this._threadEncoded, this._coreEncoded, this._coreEncoded, this._crossOutput, numCores, span);
			Span<float> span2 = this._coreScore.AsSpan(0, numCores);
			span.CopyTo(span2);
			float num2 = VectorMathNew.Sum(span2);
			int num3 = this.SelectTopKRandom(span2, this._topK);
			if (num2 > 0f)
			{
				MathHelper.Scale(span2, 1f / num2);
			}
			long num4 = (DateTime.Now.Ticks - this._startTick) / 10000L / 60000L;
			this._explorationRate = 0f;
			float positiveRewardRatio = this._stats.PositiveRewardRatio;
			float num5;
			if (num4 < 6L)
			{
				num5 = 0.01f;
			}
			else if (num4 < 12L)
			{
				num5 = 0.001f;
			}
			else
			{
				num5 = 0.0001f;
			}
			this._currentLearningRate = num5;
			if (TransformerScheduler._random.NextDouble() < (double)this._explorationRate)
			{
				num3 = TransformerScheduler._random.Next(numCores);
			}
			long ticks = DateTime.Now.Ticks;
			if (ticks - this._lastTATUpdateTick >= 10000000L)
			{
				this._decisionsInCurrentSecond = 0;
				this._lastTATUpdateTick = ticks;
			}
			this._decisionsInCurrentSecond++;
			this.RecordDecision(threadFeatures, coreFeatures, numCores, num3, predictedCore, actualCore);
			stopwatch.Stop();
			this._stats.TotalDecisions += 1L;
			this._stats.TotalInferenceTimeUs += stopwatch.ElapsedTicks * 1000000L / Stopwatch.Frequency;
			this._stats.CoreSelectionCounts[num3]++;
			return num3;
		}

		// Token: 0x060000D1 RID: 209 RVA: 0x0000948A File Offset: 0x0000768A
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void UpdateTAT(float currentTAT, float energyValue)
		{
			this.UpdateTATInternal(currentTAT, energyValue);
		}

		// Token: 0x060000D2 RID: 210 RVA: 0x00009494 File Offset: 0x00007694
		private void UpdateTATInternal(float currentTAT, float energyValue)
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
			if (this._baselineTAT > 0.001f)
			{
				num3 = (this._baselineTAT - currentTAT) / this._baselineTAT;
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
			float num4 = 0f;
			if (this._baselineEnergy > 0.001f)
			{
				num4 = (energyValue - this._baselineEnergy) / this._baselineEnergy;
				num4 = ((num4 > 1f) ? 1f : ((num4 < -1f) ? (-1f) : num4));
			}
			if (this.LOAD_BALANCE_WEIGHT > 0f && this._currentRecord.NumCores > 0)
			{
				float num5 = this.ComputeLoadBalance(this._currentRecord.CoreFeatures, this._currentRecord.NumCores);
				if (this._stats.TotalTATSamples >= 10L)
				{
					if (this._baselineLoadBalance < 0.001f)
					{
						this._baselineLoadBalance = num5;
					}
					else
					{
						this._baselineLoadBalance = this._baselineLoadBalance * 0.95f + num5 * 0.05f;
					}
				}
			}
			float num6 = 0f;
			if (this.LOAD_BALANCE_WEIGHT > 0f && this._currentRecord.NumCores > 0)
			{
				float num7 = this.ComputeLoadBalance(this._currentRecord.CoreFeatures, this._currentRecord.NumCores);
				if (this._baselineLoadBalance > 0.001f)
				{
					float num8 = (num7 - this._baselineLoadBalance) / this._baselineLoadBalance;
					if (num8 > 0f)
					{
						num6 = -(float)Math.Exp((double)(1.5f * num8));
					}
					else
					{
						num6 = num8;
					}
					num6 = Math.Max(num6, -10f);
				}
			}
			long num9 = (DateTime.Now.Ticks - this._startTick) / 600000000L;
			if (num9 >= 0L && num9 < 6L)
			{
				this.LOAD_BALANCE_WEIGHT = 0f;
				this.ENERGY_WEIGHT = 0f;
			}
			else if (num9 >= 6L && num9 < 12L)
			{
				this.LOAD_BALANCE_WEIGHT = 0f;
				this.ENERGY_WEIGHT = 0.4f;
			}
			else
			{
				this.LOAD_BALANCE_WEIGHT = 0.3f;
				this.ENERGY_WEIGHT = 0.3f;
			}
			float num10 = num3 * (1f - this.ENERGY_WEIGHT - this.LOAD_BALANCE_WEIGHT) + num4 * this.ENERGY_WEIGHT + num6 * this.LOAD_BALANCE_WEIGHT;
			this._stats.RecordReward(num10);
			this._stats.RecentAvgReward = this._stats.RecentAvgReward * 0.95f + num10 * 0.05f;
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
			bool flag = num < 600000L;
			if ((!flag && currentTAT < 1f) || !flag)
			{
			}
			int num11 = Math.Min(this._decisionsInCurrentSecond, this._history.Count);
			int num12 = this._history.Count - num11;
			float currentLearningRate = this._currentLearningRate;
			float num13 = 0f;
			int num14 = Math.Min(num11, 10000);
			for (int i = 0; i < num14; i++)
			{
				this._learningWeights[i] = (float)Math.Exp((double)(-0.5f * (float)(num14 - 1 - i)));
				num13 += this._learningWeights[i];
			}
			float num15 = 0f;
			if (!this._learningEnabled)
			{
				this._stats.ExperienceSkipped += num14;
				return;
			}
			for (int j = 0; j < num14; j++)
			{
				int num16 = num12 + j;
				if (num16 >= 0 && num16 < this._history.Count)
				{
					DecisionRecord decisionRecord = this._history.Get(num16);
					if (!flag && !decisionRecord.IsValid)
					{
						this._stats.ExperienceSkipped++;
					}
					else
					{
						float num17 = num3 * (this._learningWeights[j] / num13);
						num15 += Math.Abs(num17);
						this.BuildThreadInput(decisionRecord.ThreadFeatures, this._normThreadBuf);
						this._threadEmbedding.Forward(this._normThreadBuf, this._threadEmbed);
						this._threadEncoder.Forward(this._threadEmbed, this._threadEncoded);
						for (int k = 0; k < decisionRecord.NumCores; k++)
						{
							ReadOnlySpan<float> readOnlySpan = new ReadOnlySpan<float>(decisionRecord.CoreFeatures[k], 0, 9);
							this.BuildCoreInput(readOnlySpan, this._normCoreBuf);
							this._coreEmbedding.Forward(this._normCoreBuf, this._coreEmbeddings[k]);
							this._coreEncoder.Forward(this._coreEmbeddings[k], this._coreEncoded[k]);
						}
						Span<float> span = this._attentionWeights.AsSpan(0, decisionRecord.NumCores);
						this._crossAttention.CrossAttention(this._threadEncoded, this._coreEncoded, this._coreEncoded, this._crossOutput, decisionRecord.NumCores, span);
						this._scoreHead.Forward(this._crossOutput, this._scoreResultBuf);
						this._gradScoreHead[0] = num17;
						this._scoreHead.Backward(this._gradScoreHead, currentLearningRate, false);
						float[] inputGrads = this._scoreHead.InputGrads;
						this._crossAttention.Backward(inputGrads, currentLearningRate);
						float[] queryGradients = this._crossAttention.GetQueryGradients();
						this._threadEncoder.Backward(queryGradients, currentLearningRate);
						float[] inputGradients = this._threadEncoder.InputGradients;
						this._threadEmbedding.Backward(inputGradients, currentLearningRate, false);
						float[] valueGradients = this._crossAttention.GetValueGradients();
						float[] keyGradients = this._crossAttention.GetKeyGradients();
						int cachedNumCores = this._crossAttention.GetCachedNumCores();
						bool flag2 = true;
						for (int l = 0; l < cachedNumCores; l++)
						{
							for (int m = 0; m < 64; m++)
							{
								this._gradCoreEncodedBuf[m] = keyGradients[l * 64 + m] + valueGradients[l * 64 + m];
							}
							ReadOnlySpan<float> readOnlySpan2 = new ReadOnlySpan<float>(decisionRecord.CoreFeatures[l], 0, 9);
							this.BuildCoreInput(readOnlySpan2, this._normCoreBwdBuf);
							this._coreEmbedding.Forward(this._normCoreBwdBuf, this._coreEmbeddings[l]);
							this._coreEncoder.Forward(this._coreEmbeddings[l], this._coreEncoded[l]);
							this._coreEncoder.Backward(this._gradCoreEncodedBuf, currentLearningRate);
							this._coreEncoder.ApplyGradients(currentLearningRate);
							float[] inputGradients2 = this._coreEncoder.InputGradients;
							if (flag2)
							{
								this._coreEmbedding.Backward(inputGradients2, currentLearningRate, false);
								flag2 = false;
							}
							else
							{
								this._coreEmbedding.Backward(inputGradients2, currentLearningRate, true);
							}
						}
						this._scoreHead.ApplyGradientsSGD(currentLearningRate, 1f);
						this._threadEmbedding.ApplyGradientsSGD(currentLearningRate, 1f);
						this._coreEmbedding.ApplyGradientsSGD(currentLearningRate, 1f);
						this._crossAttention.ApplyGradients(0.001f);
						this._threadEncoder.ApplyGradients(currentLearningRate);
						this._coreEncoder.ApplyGradients(currentLearningRate);
						this._stats.ExperienceUsed++;
						this._stats.LearningUpdates++;
					}
				}
			}
			this._stats.AvgLoss = this._stats.AvgLoss * 0.9f + num15 / (float)Math.Max(num14, 1) * 0.1f;
			this._decisionsInCurrentSecond = 0;
		}

		// Token: 0x060000D3 RID: 211 RVA: 0x00009EE0 File Offset: 0x000080E0
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
				this._gradScoreHead[0] = reward;
				this._scoreHead.Backward(this._gradScoreHead, currentLearningRate, false);
				float[] inputGrads = this._scoreHead.InputGrads;
				this._crossAttention.Backward(inputGrads, currentLearningRate);
				float[] queryGradients = this._crossAttention.GetQueryGradients();
				this._threadEncoder.Backward(queryGradients, currentLearningRate);
				float[] inputGradients = this._threadEncoder.InputGradients;
				this._threadEmbedding.Backward(inputGradients, currentLearningRate, false);
				float[] valueGradients = this._crossAttention.GetValueGradients();
				float[] keyGradients = this._crossAttention.GetKeyGradients();
				int cachedNumCores = this._crossAttention.GetCachedNumCores();
				bool flag = true;
				for (int j = 0; j < cachedNumCores; j++)
				{
					for (int k = 0; k < 64; k++)
					{
						this._gradCoreEncodedBuf[k] = keyGradients[j * 64 + k] + valueGradients[j * 64 + k];
					}
					ReadOnlySpan<float> readOnlySpan = new ReadOnlySpan<float>(decisionRecord.CoreFeatures[j], 0, 9);
					this.BuildCoreInput(readOnlySpan, this._normCoreBwdBuf);
					this._coreEmbedding.Forward(this._normCoreBwdBuf, this._coreEmbeddings[j]);
					this._coreEncoder.Forward(this._coreEmbeddings[j], this._coreEncoded[j]);
					this._coreEncoder.Backward(this._gradCoreEncodedBuf, currentLearningRate);
					this._coreEncoder.ApplyGradients(currentLearningRate);
					float[] inputGradients2 = this._coreEncoder.InputGradients;
					if (flag)
					{
						this._coreEmbedding.Backward(inputGradients2, currentLearningRate, false);
						flag = false;
					}
					else
					{
						this._coreEmbedding.Backward(inputGradients2, currentLearningRate, true);
					}
				}
				num2 += Math.Abs(reward);
				this._stats.LearningUpdates++;
			}
			this._scoreHead.ApplyGradientsSGD(currentLearningRate, 1f);
			this._threadEmbedding.ApplyGradientsSGD(currentLearningRate, 1f);
			this._coreEmbedding.ApplyGradientsSGD(currentLearningRate, 1f);
			this._crossAttention.ApplyGradients(0.001f);
			this._threadEncoder.ApplyGradients(currentLearningRate);
			this._coreEncoder.ApplyGradients(currentLearningRate);
			this._stats.AvgLoss = this._stats.AvgLoss * 0.95f + num2 / (float)num * 0.05f;
		}

		// Token: 0x060000D4 RID: 212 RVA: 0x0000A1D4 File Offset: 0x000083D4
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

		// Token: 0x060000D5 RID: 213 RVA: 0x0000A2AC File Offset: 0x000084AC
		public long GetRuntime()
		{
			return (DateTime.Now.Ticks - this._startTick) / 600000000L;
		}

		// Token: 0x060000D6 RID: 214 RVA: 0x0000A2D4 File Offset: 0x000084D4
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe int SelectTopKRandom(ReadOnlySpan<float> scores, int topK)
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
			int num3 = 0;
			while (num3 < num2 && num3 < length)
			{
				if (*scores[num3] > num)
				{
					num = *scores[num3];
				}
				num3++;
			}
			for (int i = num2; i < length; i++)
			{
				if (*scores[i] > num)
				{
					num = *scores[i];
				}
			}
			int num4 = 0;
			for (int j = 0; j < length; j++)
			{
				if (*scores[j] >= num - 1E-06f)
				{
					num4++;
				}
			}
			int num5 = TransformerScheduler._random.Next(num4);
			for (int k = 0; k < length; k++)
			{
				if (*scores[k] >= num - 1E-06f)
				{
					if (num5 == 0)
					{
						return k;
					}
					num5--;
				}
			}
			return 0;
		}

		// Token: 0x060000D7 RID: 215 RVA: 0x0000A3C0 File Offset: 0x000085C0
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
			return (float)Math.Sqrt((double)(num3 / (float)numCores));
		}

		// Token: 0x060000D8 RID: 216 RVA: 0x0000A42C File Offset: 0x0000862C
		public string GetStatistics(int numcores)
		{
			long num = (DateTime.Now.Ticks - this._startTick) / 10000L;
			bool flag = num < 600000L;
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(string.Format("Runtime: {0} min / {1} min", num / 60000L, 10L));
			stringBuilder.AppendLine("Learning Phase: " + (flag ? "Initial (all experiences valid)" : "Stable (skip invalid experiences)"));
			stringBuilder.AppendLine();
			stringBuilder.Append(this._stats.GetReport(numcores, this._explorationRate));
			return stringBuilder.ToString();
		}

		// Token: 0x060000D9 RID: 217 RVA: 0x0000A4D0 File Offset: 0x000086D0
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

		// Token: 0x060000DA RID: 218 RVA: 0x0000A5A0 File Offset: 0x000087A0
		public string GetAttentionHeadReport(int numCores)
		{
			StringBuilder stringBuilder = new StringBuilder();
			float[][] headAttentionWeights = this._crossAttention.GetHeadAttentionWeights(numCores);
			stringBuilder.AppendLine("╔" + new string('═', 96) + "╗");
			stringBuilder.Append("║ Core   │");
			for (int i = 0; i < 8; i++)
			{
				stringBuilder.Append(string.Format("  H{0}    │", i));
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
				for (int n = 0; n < 8; n++)
				{
					string text = ((m == this._reportMaxCores[n]) ? string.Format("*{0:F4}", headAttentionWeights[n][m]) : string.Format(" {0:F4}", headAttentionWeights[n][m]));
					stringBuilder.Append(string.Format("{0,-8}│", text));
				}
				stringBuilder.AppendLine(string.Format(" {0:F4} ║", this._attentionWeights[m]));
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

		// Token: 0x060000DB RID: 219 RVA: 0x0000A8A0 File Offset: 0x00008AA0
		public float[] GetLastAttentionWeights(int numCores)
		{
			for (int i = 0; i < numCores; i++)
			{
				this._lastAttentionWeightsBuffer[i] = this._attentionWeights[i];
			}
			return this._lastAttentionWeightsBuffer;
		}

		// Token: 0x060000DC RID: 220 RVA: 0x0000A8CF File Offset: 0x00008ACF
		public void SetNormalizationParams(float[] threadMean, float[] threadStd, float[] coreMean, float[] coreStd)
		{
			Array.Copy(threadMean, this._threadFeatureMean, 7);
			Array.Copy(threadStd, this._threadFeatureStd, 7);
			Array.Copy(coreMean, this._coreFeatureMean, 8);
			Array.Copy(coreStd, this._coreFeatureStd, 8);
			this._normalizationReady = true;
		}

		// Token: 0x060000DD RID: 221 RVA: 0x0000A910 File Offset: 0x00008B10
		public string ExportModel()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(string.Format("Model Config: d_model={0}, n_head={1}, d_ff={2}", 64, 8, 128));
			stringBuilder.AppendLine(string.Format("Thread Embedding: {0} -> {1}", 15, 64));
			stringBuilder.AppendLine(string.Format("Core Embedding: {0} -> {1}", 16, 64));
			stringBuilder.AppendLine(string.Format("Total Params: {0}", this.CountParameters()));
			return stringBuilder.ToString();
		}

		// Token: 0x060000DE RID: 222 RVA: 0x0000A9AC File Offset: 0x00008BAC
		private int CountParameters()
		{
			return 0 + (this._threadEmbedding.Weights.Length + this._threadEmbedding.Bias.Length) + (this._coreEmbedding.Weights.Length + this._coreEmbedding.Bias.Length) + (this._scoreHead.Weights.Length + this._scoreHead.Bias.Length) + this._coreIdEmbedding.Length;
		}

		// Token: 0x060000DF RID: 223 RVA: 0x0000AA17 File Offset: 0x00008C17
		public float GetCurrentTAT()
		{
			return this._lastTAT;
		}

		// Token: 0x060000E0 RID: 224 RVA: 0x0000AA1F File Offset: 0x00008C1F
		public float GetBaselineTAT()
		{
			return this._baselineTAT;
		}

		// Token: 0x060000E1 RID: 225 RVA: 0x0000AA27 File Offset: 0x00008C27
		public float GetRecentAvgReward()
		{
			return this._stats.RecentAvgReward;
		}

		// Token: 0x060000E2 RID: 226 RVA: 0x0000AA34 File Offset: 0x00008C34
		public float GetExplorationRate()
		{
			return this._explorationRate;
		}

		// Token: 0x060000E3 RID: 227 RVA: 0x0000AA3C File Offset: 0x00008C3C
		public float GetLearningRate()
		{
			return this._currentLearningRate;
		}

		// Token: 0x060000E4 RID: 228 RVA: 0x0000AA44 File Offset: 0x00008C44
		public void SetExplorationRate(float rate)
		{
			this._explorationRate = Math.Max(rate, this._minExplorationRate);
		}

		// Token: 0x060000E5 RID: 229 RVA: 0x0000AA58 File Offset: 0x00008C58
		public void SetMinExplorationRate(float rate)
		{
			this._minExplorationRate = Math.Max(rate, 0.001f);
		}

		// Token: 0x060000E6 RID: 230 RVA: 0x0000AA6B File Offset: 0x00008C6B
		public void SetExplorationDecayMinutes(int minutes)
		{
		}

		// Token: 0x060000E7 RID: 231 RVA: 0x0000AA70 File Offset: 0x00008C70
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
			stringBuilder.AppendLine("--- Energy Metrics ---");
			stringBuilder.AppendLine(string.Format("Current Energy: {0:F2}", this._stats.LastEnergy));
			stringBuilder.AppendLine(string.Format("Baseline Energy: {0:F2}", this._baselineEnergy));
			stringBuilder.AppendLine(string.Format("Avg Energy: {0:F2}", this._stats.AvgEnergy));
			stringBuilder.AppendLine(string.Format("Min Energy: {0:F2}", this._stats.MinEnergy));
			stringBuilder.AppendLine(string.Format("Max Energy: {0:F2}", this._stats.MaxEnergy));
			stringBuilder.AppendLine(string.Format("Energy Trend: {0:F4} /step", this._stats.EnergyTrend));
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

		// Token: 0x060000E8 RID: 232 RVA: 0x0000AEE8 File Offset: 0x000090E8
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

		// Token: 0x060000E9 RID: 233 RVA: 0x0000AF70 File Offset: 0x00009170
		public void ResetStatistics()
		{
			this._stats.TotalDecisions = 0L;
			this._stats.TotalInferenceTimeUs = 0L;
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
			MathHelper.Clear(this._stats.CoreSelectionCounts.AsSpan<int>());
			this._baselineTAT = 0f;
			this._baselineEnergy = 0f;
			this._sumTAT = 0f;
			this._tatIndex = 0;
			this._lastTAT = 0f;
			this._windowIndex = 0;
			this._windowStartTick = DateTime.Now.Ticks;
			this._normalizationReady = false;
			MathHelper.Clear(this._tatHistory.AsSpan<float>());
		}

		// Token: 0x060000EA RID: 234 RVA: 0x0000B174 File Offset: 0x00009374
		public void SaveModel(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				path = "./scheduler_model.bin";
			}
			using (BinaryWriter binaryWriter = new BinaryWriter(File.OpenWrite(path)))
			{
				binaryWriter.Write("TSC1");
				binaryWriter.Write(3);
				binaryWriter.Write(64);
				binaryWriter.Write(8);
				binaryWriter.Write(128);
				binaryWriter.Write(64);
				binaryWriter.Write(15);
				binaryWriter.Write(16);
				this.WriteLayer(binaryWriter, this._threadEmbedding);
				this.WriteLayer(binaryWriter, this._coreEmbedding);
				this.WriteLayer(binaryWriter, this._scoreHead);
				this.WriteAttention(binaryWriter, this._crossAttention);
				this.WriteEncoder(binaryWriter, this._threadEncoder);
				this.WriteEncoder(binaryWriter, this._coreEncoder);
				for (int i = 0; i < 7; i++)
				{
					binaryWriter.Write(this._threadFeatureMean[i]);
				}
				for (int j = 0; j < 7; j++)
				{
					binaryWriter.Write(this._threadFeatureStd[j]);
				}
				for (int k = 0; k < 8; k++)
				{
					binaryWriter.Write(this._coreFeatureMean[k]);
				}
				for (int l = 0; l < 8; l++)
				{
					binaryWriter.Write(this._coreFeatureStd[l]);
				}
				for (int m = 0; m < this._coreIdEmbedding.Length; m++)
				{
					binaryWriter.Write(this._coreIdEmbedding[m]);
				}
				binaryWriter.Write(this._startTick);
			}
		}

		// Token: 0x060000EB RID: 235 RVA: 0x0000B2F8 File Offset: 0x000094F8
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
						if (num != 3)
						{
							flag = false;
						}
						else
						{
							int num2 = binaryReader.ReadInt32();
							int num3 = binaryReader.ReadInt32();
							int num4 = binaryReader.ReadInt32();
							if (num2 != 64 || num3 != 8 || num4 != 128)
							{
								flag = false;
							}
							else
							{
								int num5 = binaryReader.ReadInt32();
								int num6 = binaryReader.ReadInt32();
								int num7 = binaryReader.ReadInt32();
								if (num5 != 64 || num6 != 15 || num7 != 16)
								{
									flag = false;
								}
								else
								{
									this.ReadLayer(binaryReader, this._threadEmbedding);
									this.ReadLayer(binaryReader, this._coreEmbedding);
									this.ReadLayer(binaryReader, this._scoreHead);
									this.ReadAttention(binaryReader, this._crossAttention);
									this.ReadEncoder(binaryReader, this._threadEncoder);
									this.ReadEncoder(binaryReader, this._coreEncoder);
									for (int i = 0; i < 7; i++)
									{
										this._threadFeatureMean[i] = binaryReader.ReadSingle();
									}
									for (int j = 0; j < 7; j++)
									{
										this._threadFeatureStd[j] = binaryReader.ReadSingle();
									}
									for (int k = 0; k < 8; k++)
									{
										this._coreFeatureMean[k] = binaryReader.ReadSingle();
									}
									for (int l = 0; l < 8; l++)
									{
										this._coreFeatureStd[l] = binaryReader.ReadSingle();
									}
									for (int m = 0; m < this._coreIdEmbedding.Length; m++)
									{
										this._coreIdEmbedding[m] = binaryReader.ReadSingle();
									}
									if (num >= 2)
									{
										this._startTick = binaryReader.ReadInt64();
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

		// Token: 0x060000EC RID: 236 RVA: 0x0000B504 File Offset: 0x00009704
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

		// Token: 0x060000ED RID: 237 RVA: 0x0000B554 File Offset: 0x00009754
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

		// Token: 0x060000EE RID: 238 RVA: 0x0000B5A3 File Offset: 0x000097A3
		private void WriteAttention(BinaryWriter writer, MultiHeadAttention attention)
		{
			this.WriteLayer(writer, attention.Wq);
			this.WriteLayer(writer, attention.Wk);
			this.WriteLayer(writer, attention.Wv);
			this.WriteLayer(writer, attention.Wo);
		}

		// Token: 0x060000EF RID: 239 RVA: 0x0000B5D9 File Offset: 0x000097D9
		private void ReadAttention(BinaryReader reader, MultiHeadAttention attention)
		{
			this.ReadLayer(reader, attention.Wq);
			this.ReadLayer(reader, attention.Wk);
			this.ReadLayer(reader, attention.Wv);
			this.ReadLayer(reader, attention.Wo);
		}

		// Token: 0x060000F0 RID: 240 RVA: 0x0000B610 File Offset: 0x00009810
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

		// Token: 0x060000F1 RID: 241 RVA: 0x0000B6FC File Offset: 0x000098FC
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

		// Token: 0x060000F2 RID: 242 RVA: 0x0000B7E8 File Offset: 0x000099E8
		private void WriteEncoder(BinaryWriter writer, ThreadTransformerEncoder encoder)
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

		// Token: 0x060000F3 RID: 243 RVA: 0x0000B8D4 File Offset: 0x00009AD4
		private void ReadEncoder(BinaryReader reader, ThreadTransformerEncoder encoder)
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

		// Token: 0x0400010C RID: 268
		private static readonly Random _random = new Random(42);

		// Token: 0x0400010D RID: 269
		private const int D_MODEL = 64;

		// Token: 0x0400010E RID: 270
		private const int N_HEAD = 8;

		// Token: 0x0400010F RID: 271
		private const int D_FF = 128;

		// Token: 0x04000110 RID: 272
		private const int MAX_CORES = 64;

		// Token: 0x04000111 RID: 273
		private const int THREAD_RAW_DIM = 7;

		// Token: 0x04000112 RID: 274
		private const int CORE_RAW_DIM = 8;

		// Token: 0x04000113 RID: 275
		private const int CORE_ID_EMBED_DIM = 8;

		// Token: 0x04000114 RID: 276
		private const int THREAD_INPUT_DIM = 8;

		// Token: 0x04000115 RID: 277
		private const int CORE_INPUT_DIM = 9;

		// Token: 0x04000116 RID: 278
		private const int THREAD_FEATURE_DIM = 15;

		// Token: 0x04000117 RID: 279
		private const int CORE_FEATURE_DIM = 16;

		// Token: 0x04000118 RID: 280
		private const int HISTORY_CAPACITY = 10000;

		// Token: 0x04000119 RID: 281
		private const long WINDOW_TICKS = 600000000L;

		// Token: 0x0400011A RID: 282
		private const int NORMALIZATION_WINDOW_SIZE = 1000;

		// Token: 0x0400011B RID: 283
		private float ENERGY_WEIGHT;

		// Token: 0x0400011C RID: 284
		private float LOAD_BALANCE_WEIGHT = 0.3f;

		// Token: 0x0400011D RID: 285
		private const float LOAD_BALANCE_DECAY = 1.5f;

		// Token: 0x0400011E RID: 286
		private readonly LinearLayer _threadEmbedding;

		// Token: 0x0400011F RID: 287
		private readonly LinearLayer _coreEmbedding;

		// Token: 0x04000120 RID: 288
		private readonly ThreadTransformerEncoder _threadEncoder;

		// Token: 0x04000121 RID: 289
		private readonly CoreTransformerEncoder _coreEncoder;

		// Token: 0x04000122 RID: 290
		private readonly MultiHeadAttention _crossAttention;

		// Token: 0x04000123 RID: 291
		private readonly LinearLayer _scoreHead;

		// Token: 0x04000124 RID: 292
		private readonly float[] _threadEmbed;

		// Token: 0x04000125 RID: 293
		private readonly float[] _threadEncoded;

		// Token: 0x04000126 RID: 294
		private readonly float[] _crossOutput;

		// Token: 0x04000127 RID: 295
		private readonly float[] _coreScore;

		// Token: 0x04000128 RID: 296
		private readonly float[] _attentionWeights;

		// Token: 0x04000129 RID: 297
		private readonly float[][] _coreEmbeddings;

		// Token: 0x0400012A RID: 298
		private readonly float[][] _coreEncoded;

		// Token: 0x0400012B RID: 299
		private readonly int[] _coreFeatureHash;

		// Token: 0x0400012C RID: 300
		private readonly bool[] _coreCacheValid;

		// Token: 0x0400012D RID: 301
		private readonly CircularBuffer<DecisionRecord> _history;

		// Token: 0x0400012E RID: 302
		private readonly SchedulerStatistics _stats;

		// Token: 0x0400012F RID: 303
		private DecisionRecord _currentRecord;

		// Token: 0x04000130 RID: 304
		private readonly float[] _threadFeatureMean;

		// Token: 0x04000131 RID: 305
		private readonly float[] _threadFeatureStd;

		// Token: 0x04000132 RID: 306
		private readonly float[] _coreFeatureMean;

		// Token: 0x04000133 RID: 307
		private readonly float[] _coreFeatureStd;

		// Token: 0x04000134 RID: 308
		private readonly float[][] _threadFeatureWindow;

		// Token: 0x04000135 RID: 309
		private readonly float[][] _coreFeatureWindow;

		// Token: 0x04000136 RID: 310
		private int _windowIndex;

		// Token: 0x04000137 RID: 311
		private long _windowStartTick;

		// Token: 0x04000138 RID: 312
		private bool _normalizationReady;

		// Token: 0x04000139 RID: 313
		private readonly float[] _tatHistory;

		// Token: 0x0400013A RID: 314
		private int _tatIndex;

		// Token: 0x0400013B RID: 315
		private float _sumTAT;

		// Token: 0x0400013C RID: 316
		private const int TAT_HISTORY_SIZE = 1000;

		// Token: 0x0400013D RID: 317
		private readonly float[] _gradScoreHead;

		// Token: 0x0400013E RID: 318
		private readonly float[] _gradCrossOutput;

		// Token: 0x0400013F RID: 319
		private readonly float[] _gradThreadEncoded;

		// Token: 0x04000140 RID: 320
		private readonly float[] _gradThreadEmbed;

		// Token: 0x04000141 RID: 321
		private readonly float[] _coreIdEmbedding;

		// Token: 0x04000142 RID: 322
		private readonly float[] _normThreadBuf;

		// Token: 0x04000143 RID: 323
		private readonly float[] _normCoreBuf;

		// Token: 0x04000144 RID: 324
		private readonly float[] _normCoreBwdBuf;

		// Token: 0x04000145 RID: 325
		private readonly float[] _gradCoreEncodedBuf;

		// Token: 0x04000146 RID: 326
		private readonly float[] _scoreResultBuf;

		// Token: 0x04000147 RID: 327
		private float _lastTAT;

		// Token: 0x04000148 RID: 328
		private float _baselineTAT;

		// Token: 0x04000149 RID: 329
		private float _baselineEnergy;

		// Token: 0x0400014A RID: 330
		private float _baselineLoadBalance;

		// Token: 0x0400014B RID: 331
		private const float LEARNING_RATE = 0.0001f;

		// Token: 0x0400014C RID: 332
		private int _decisionsInCurrentSecond;

		// Token: 0x0400014D RID: 333
		private long _lastTATUpdateTick;

		// Token: 0x0400014E RID: 334
		private float _explorationRate;

		// Token: 0x0400014F RID: 335
		private float _minExplorationRate;

		// Token: 0x04000150 RID: 336
		private long _startTick;

		// Token: 0x04000151 RID: 337
		private const long INITIAL_LEARNING_PHASE_MS = 600000L;

		// Token: 0x04000152 RID: 338
		private const long RAPID_LEARNING_PHASE_MS = 120000L;

		// Token: 0x04000153 RID: 339
		private const float RAPID_LEARNING_RATE = 0.01f;

		// Token: 0x04000154 RID: 340
		private const float INITIAL_LEARNING_RATE = 0.01f;

		// Token: 0x04000155 RID: 341
		private const float MIN_LEARNING_RATE = 0.0001f;

		// Token: 0x04000156 RID: 342
		private const float MAX_LEARNING_RATE = 0.01f;

		// Token: 0x04000157 RID: 343
		private float _currentLearningRate = 0.01f;

		// Token: 0x04000158 RID: 344
		private bool _learningEnabled = true;

		// Token: 0x04000159 RID: 345
		private const long LEARNING_RATE_DECAY_DURATION = 36000000000L;

		// Token: 0x0400015A RID: 346
		private const float INITIAL_EXPLORATION_RATE = 0f;

		// Token: 0x0400015B RID: 347
		private const long BATCH_TRAIN_INTERVAL = 600000000L;

		// Token: 0x0400015C RID: 348
		private long _lastBatchTrainTick;

		// Token: 0x0400015D RID: 349
		private const int BATCH_SAMPLE_SIZE = 1000;

		// Token: 0x0400015E RID: 350
		private readonly float[] _learningWeights;

		// Token: 0x0400015F RID: 351
		private const int MAX_DECISIONS_PER_SECOND = 10000;

		// Token: 0x04000160 RID: 352
		private int _topK = 3;

		// Token: 0x04000161 RID: 353
		private readonly int[] _reportMaxCores;

		// Token: 0x04000162 RID: 354
		private readonly float[] _reportMaxWeights;

		// Token: 0x04000163 RID: 355
		private readonly float[] _lastAttentionWeightsBuffer;

		// Token: 0x04000164 RID: 356
		private const string MODEL_MAGIC = "TSC1";

		// Token: 0x04000165 RID: 357
		private const int MODEL_VERSION = 3;

		// Token: 0x04000166 RID: 358
		private const string DEFAULT_MODEL_PATH = "./scheduler_model.bin";
	}
}
