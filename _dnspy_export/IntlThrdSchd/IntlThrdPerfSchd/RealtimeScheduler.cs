using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace IntlThrdPerfSchd
{
	// Token: 0x02000009 RID: 9
	public class RealtimeScheduler : IDisposable
	{
		// Token: 0x1700001D RID: 29
		// (get) Token: 0x06000075 RID: 117 RVA: 0x0000527D File Offset: 0x0000347D
		// (set) Token: 0x06000076 RID: 118 RVA: 0x00005285 File Offset: 0x00003485
		public float AttentionTemperature
		{
			get
			{
				return this._attentionTemperature;
			}
			set
			{
				this._attentionTemperature = Math.Max(0.1f, value);
			}
		}

		// Token: 0x06000077 RID: 119 RVA: 0x00005298 File Offset: 0x00003498
		public float[][] GetRawAttentionScores(int numCores)
		{
			float[][] array = new float[8][];
			for (int i = 0; i < 8; i++)
			{
				array[i] = new float[numCores];
				for (int j = 0; j < numCores; j++)
				{
					array[i][j] = this._cachedAttentionScores[i * this._maxCores + j];
				}
			}
			return array;
		}

		// Token: 0x06000078 RID: 120 RVA: 0x000052E4 File Offset: 0x000034E4
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
					float num5 = this._cachedAttentionScores[i * this._maxCores + j];
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
				stringBuilder.AppendLine(string.Format("Head {0}: min={1:F2}, max={2:F2} (C{3}), avg={4:F2}, range={5:F2}", new object[] { i, num, num2, num4, num6, num7 }));
			}
			stringBuilder.AppendLine(string.Format("温度参数: {0:F2}", this._attentionTemperature));
			stringBuilder.AppendLine("====================================");
			return stringBuilder.ToString();
		}

		// Token: 0x06000079 RID: 121 RVA: 0x000053F4 File Offset: 0x000035F4
		public RealtimeScheduler(int maxCores = 16)
		{
			if (maxCores <= 0)
			{
				throw new ArgumentException("maxCores must be positive", "maxCores");
			}
			this._maxCores = maxCores;
			this.InitializeWeights();
			this.InitializeBuffers();
		}

		// Token: 0x0600007A RID: 122 RVA: 0x00005444 File Offset: 0x00003644
		public void InitializeWeights()
		{
			float num = 0.01f;
			this._threadProjW = this.InitializeUniform(128, 5, num);
			this._threadProjB = new float[128];
			this._coreProjW = this.InitializeUniform(128, 7, num);
			this._coreProjB = new float[128];
			this._Wq = this.InitializeUniform(128, 128, num);
			this._Wk = this.InitializeUniform(128, 128, num);
			this._Wv = this.InitializeUniform(128, 128, num);
			this._Wo = this.InitializeUniform(128, 128, num);
			this._WoBias = new float[128];
			int num2 = 512;
			this._ffW1 = this.InitializeUniform(num2, 128, num);
			this._ffB1 = new float[num2];
			this._ffW2 = this.InitializeUniform(128, num2, num);
			this._ffB2 = new float[128];
			this._outputW = this.InitializeUniform(1, 128, num);
			this._outputB = new float[1];
			this._utilPenalty = 0.5f;
			this._queuePenalty = 0.1f;
		}

		// Token: 0x0600007B RID: 123 RVA: 0x00005588 File Offset: 0x00003788
		private float[] InitializeUniform(int rows, int cols, float value)
		{
			float[] array = new float[rows * cols];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = value;
			}
			return array;
		}

		// Token: 0x0600007C RID: 124 RVA: 0x000055B4 File Offset: 0x000037B4
		private void InitializeBuffers()
		{
			this._threadEmbed = new float[128];
			this._coreEmbed = new float[128];
			this._q = new float[128];
			this._k = new float[128];
			this._v = new float[128];
			this._attentionScores = new float[8];
			this._attentionOutput = new float[128];
			this._ffHidden = new float[512];
			this._ffOutput = new float[128];
			this._allCoreEmbeds = new float[this._maxCores * 128];
			this._allK = new float[this._maxCores * 8 * 16];
			this._allV = new float[this._maxCores * 8 * 16];
			this._allAttentionScores = new float[8 * this._maxCores];
			this._allHeadOutputs = new float[128];
			this._finalScores = new float[this._maxCores];
			this._cachedCoreInputs = new float[this._maxCores][];
			for (int i = 0; i < this._maxCores; i++)
			{
				this._cachedCoreInputs[i] = new float[7];
			}
			this._cachedAttentionScores = new float[8 * this._maxCores];
			this._cachedAttentionProbs = new float[8 * this._maxCores];
			this._cachedProjectedAttention = new float[128];
			this._cachedFfPreActivation = new float[512];
			this._cachedThreadInput = new float[5];
			this._threadProjWGrad = new float[640];
			this._threadProjBGrad = new float[128];
			this._coreProjWGrad = new float[896];
			this._coreProjBGrad = new float[128];
			this._WqGrad = new float[16384];
			this._WkGrad = new float[16384];
			this._WvGrad = new float[16384];
			this._WoGrad = new float[16384];
			this._WoBiasGrad = new float[128];
			this._ffW1Grad = new float[65536];
			this._ffB1Grad = new float[512];
			this._ffW2Grad = new float[65536];
			this._ffB2Grad = new float[128];
			this._outputWGrad = new float[128];
			this._outputBGrad = new float[1];
			this._tempBuffer1 = new float[128];
			this._tempBuffer2 = new float[512];
			this._tempBuffer3 = new float[128];
			this._tempGradScores = new float[this._maxCores];
			this._tempGradAttentionProbs = new float[8 * this._maxCores];
			this._tempGradAllV = new float[this._maxCores * 8 * 16];
			this._tempGradAttentionScores = new float[8 * this._maxCores];
			this._tempGradQ = new float[128];
			this._tempGradAllK = new float[this._maxCores * 8 * 16];
			this._tempGradAllCoreEmbeds = new float[this._maxCores * 128];
			this._tempSoftmaxProbs = new float[this._maxCores];
		}

		// Token: 0x0600007D RID: 125 RVA: 0x00005900 File Offset: 0x00003B00
		public int Predict(float[] threadFeatures, float[][] coreFeatures, int numCores)
		{
			if (threadFeatures == null || threadFeatures.Length != 5)
			{
				throw new ArgumentException(string.Format("Thread features must have {0} elements", 5));
			}
			if (coreFeatures == null || coreFeatures.Length < numCores)
			{
				throw new ArgumentException("Not enough core features provided");
			}
			if (numCores <= 0 || numCores > this._maxCores)
			{
				throw new ArgumentException(string.Format("numCores must be between 1 and {0}", this._maxCores));
			}
			Stopwatch stopwatch = Stopwatch.StartNew();
			this.ProjectThread(threadFeatures);
			this.ComputeQ();
			for (int i = 0; i < numCores; i++)
			{
				int num = coreFeatures[i].Length;
				if (num >= 7)
				{
					Array.Copy(coreFeatures[i], this._cachedCoreInputs[i], 7);
				}
				else
				{
					Array.Copy(coreFeatures[i], this._cachedCoreInputs[i], num);
					for (int j = num; j < 7; j++)
					{
						this._cachedCoreInputs[i][j] = ((j == 6) ? ((i < 8) ? 1f : 0f) : 0f);
					}
				}
				this.ProjectCore(this._cachedCoreInputs[i], i);
			}
			this.ComputeAllKV(numCores);
			this.ComputeAttention(numCores);
			this.FeedForward();
			int num2 = this.ComputeFinalScores(numCores);
			stopwatch.Stop();
			object statsLock = this._statsLock;
			lock (statsLock)
			{
				this._inferenceCount += 1L;
				this._totalInferenceTimeUs += stopwatch.Elapsed.TotalMilliseconds * 1000.0;
			}
			return num2;
		}

		// Token: 0x0600007E RID: 126 RVA: 0x00005A88 File Offset: 0x00003C88
		public float[] GetCoreEmbedding(int coreIdx)
		{
			if (coreIdx < 0 || coreIdx >= this._maxCores)
			{
				throw new ArgumentException(string.Format("Core index must be between 0 and {0}", this._maxCores - 1));
			}
			float[] array = new float[128];
			Array.Copy(this._allCoreEmbeds, coreIdx * 128, array, 0, 128);
			return array;
		}

		// Token: 0x0600007F RID: 127 RVA: 0x00005AE4 File Offset: 0x00003CE4
		public double GetAverageInferenceTimeUs()
		{
			object statsLock = this._statsLock;
			double num;
			lock (statsLock)
			{
				num = ((this._inferenceCount > 0L) ? (this._totalInferenceTimeUs / (double)this._inferenceCount) : 0.0);
			}
			return num;
		}

		// Token: 0x06000080 RID: 128 RVA: 0x00005B44 File Offset: 0x00003D44
		public long GetInferenceCount()
		{
			object statsLock = this._statsLock;
			long inferenceCount;
			lock (statsLock)
			{
				inferenceCount = this._inferenceCount;
			}
			return inferenceCount;
		}

		// Token: 0x06000081 RID: 129 RVA: 0x00005B88 File Offset: 0x00003D88
		public float[][] GetAttentionWeights(int numCores)
		{
			float[][] array = new float[8][];
			for (int i = 0; i < 8; i++)
			{
				array[i] = new float[numCores];
				for (int j = 0; j < numCores; j++)
				{
					array[i][j] = this._allAttentionScores[i * this._maxCores + j];
				}
			}
			return array;
		}

		// Token: 0x06000082 RID: 130 RVA: 0x00005BD4 File Offset: 0x00003DD4
		[return: TupleElementNames(new string[] { "bestCore", "probs" })]
		public ValueTuple<int, float[]> PredictWithCache(float[] threadFeatures, float[][] coreFeatures, int numCores)
		{
			if (threadFeatures == null || threadFeatures.Length != 5)
			{
				throw new ArgumentException(string.Format("Thread features must have {0} elements", 5));
			}
			if (coreFeatures == null || coreFeatures.Length < numCores)
			{
				throw new ArgumentException("Not enough core features provided");
			}
			if (numCores <= 0 || numCores > this._maxCores)
			{
				throw new ArgumentException(string.Format("numCores must be between 1 and {0}", this._maxCores));
			}
			for (int i = 0; i < numCores; i++)
			{
				if (coreFeatures[i] == null)
				{
					throw new ArgumentException(string.Format("Core {0} features is null", i));
				}
			}
			Array.Copy(threadFeatures, this._cachedThreadInput, 5);
			for (int j = 0; j < numCores; j++)
			{
				int num = coreFeatures[j].Length;
				if (num >= 7)
				{
					Array.Copy(coreFeatures[j], this._cachedCoreInputs[j], 7);
				}
				else
				{
					Array.Copy(coreFeatures[j], this._cachedCoreInputs[j], num);
					for (int k = num; k < 7; k++)
					{
						this._cachedCoreInputs[j][k] = ((k == 6) ? ((j < 8) ? 1f : 0f) : 0f);
					}
				}
			}
			this._cachedNumCores = numCores;
			this.ProjectThread(threadFeatures);
			this.ComputeQ();
			for (int l = 0; l < numCores; l++)
			{
				this.ProjectCore(this._cachedCoreInputs[l], l);
			}
			this.ComputeAllKV(numCores);
			this.ComputeAttentionWithCache(numCores);
			this.FeedForwardWithCache();
			int num2 = this.ComputeFinalScores(numCores);
			float[] array = this.Softmax(this._finalScores, 0, numCores);
			return new ValueTuple<int, float[]>(num2, array);
		}

		// Token: 0x06000083 RID: 131 RVA: 0x00005D48 File Offset: 0x00003F48
		public void Backward(int selectedCore, float advantage)
		{
			if (this._cachedNumCores == 0)
			{
				throw new InvalidOperationException("Must call PredictWithCache before Backward");
			}
			int cachedNumCores = this._cachedNumCores;
			this.BackwardOutput(selectedCore, advantage, cachedNumCores);
			this.BackwardFeedForward();
			this.BackwardAttentionOutput();
			this.BackwardAttention(cachedNumCores);
			this.BackwardQKV(cachedNumCores);
			this.BackwardInputProjections(cachedNumCores);
		}

		// Token: 0x06000084 RID: 132 RVA: 0x00005D9C File Offset: 0x00003F9C
		public void ClearGradients()
		{
			Array.Clear(this._threadProjWGrad, 0, this._threadProjWGrad.Length);
			Array.Clear(this._threadProjBGrad, 0, this._threadProjBGrad.Length);
			Array.Clear(this._coreProjWGrad, 0, this._coreProjWGrad.Length);
			Array.Clear(this._coreProjBGrad, 0, this._coreProjBGrad.Length);
			Array.Clear(this._WqGrad, 0, this._WqGrad.Length);
			Array.Clear(this._WkGrad, 0, this._WkGrad.Length);
			Array.Clear(this._WvGrad, 0, this._WvGrad.Length);
			Array.Clear(this._WoGrad, 0, this._WoGrad.Length);
			Array.Clear(this._WoBiasGrad, 0, this._WoBiasGrad.Length);
			Array.Clear(this._ffW1Grad, 0, this._ffW1Grad.Length);
			Array.Clear(this._ffB1Grad, 0, this._ffB1Grad.Length);
			Array.Clear(this._ffW2Grad, 0, this._ffW2Grad.Length);
			Array.Clear(this._ffB2Grad, 0, this._ffB2Grad.Length);
			Array.Clear(this._outputWGrad, 0, this._outputWGrad.Length);
			Array.Clear(this._outputBGrad, 0, this._outputBGrad.Length);
			this._utilPenaltyGrad = 0f;
			this._queuePenaltyGrad = 0f;
		}

		// Token: 0x06000085 RID: 133 RVA: 0x00005EEC File Offset: 0x000040EC
		public void ApplyGradients(float learningRate, int batchSize = 1)
		{
			float num = learningRate / (float)batchSize;
			float num2 = 1f;
			float num3 = 0f;
			num3 += this.ComputeNorm(this._threadProjWGrad, this._threadProjWGrad.Length);
			num3 += this.ComputeNorm(this._threadProjBGrad, this._threadProjBGrad.Length);
			num3 += this.ComputeNorm(this._coreProjWGrad, this._coreProjWGrad.Length);
			num3 += this.ComputeNorm(this._coreProjBGrad, this._coreProjBGrad.Length);
			num3 += this.ComputeNorm(this._WqGrad, this._WqGrad.Length);
			num3 += this.ComputeNorm(this._WkGrad, this._WkGrad.Length);
			num3 += this.ComputeNorm(this._WvGrad, this._WvGrad.Length);
			num3 += this.ComputeNorm(this._WoGrad, this._WoGrad.Length);
			num3 += this.ComputeNorm(this._WoBiasGrad, this._WoBiasGrad.Length);
			num3 += this.ComputeNorm(this._ffW1Grad, this._ffW1Grad.Length);
			num3 += this.ComputeNorm(this._ffB1Grad, this._ffB1Grad.Length);
			num3 += this.ComputeNorm(this._ffW2Grad, this._ffW2Grad.Length);
			num3 += this.ComputeNorm(this._ffB2Grad, this._ffB2Grad.Length);
			num3 += this.ComputeNorm(this._outputWGrad, this._outputWGrad.Length);
			num3 += this.ComputeNorm(this._outputBGrad, this._outputBGrad.Length);
			num3 = (float)Math.Sqrt((double)num3);
			float num4 = 1f;
			if (num3 > num2 && num3 > 0f && !float.IsNaN(num3) && !float.IsInfinity(num3))
			{
				num4 = num2 / num3;
			}
			float num5 = num * num4;
			this.AddScaledSafe(this._threadProjW, this._threadProjWGrad, num5, this._threadProjW.Length);
			this.AddScaledSafe(this._threadProjB, this._threadProjBGrad, num5, this._threadProjB.Length);
			this.AddScaledSafe(this._coreProjW, this._coreProjWGrad, num5, this._coreProjW.Length);
			this.AddScaledSafe(this._coreProjB, this._coreProjBGrad, num5, this._coreProjB.Length);
			this.AddScaledSafe(this._Wq, this._WqGrad, num5, this._Wq.Length);
			this.AddScaledSafe(this._Wk, this._WkGrad, num5, this._Wk.Length);
			this.AddScaledSafe(this._Wv, this._WvGrad, num5, this._Wv.Length);
			this.AddScaledSafe(this._Wo, this._WoGrad, num5, this._Wo.Length);
			this.AddScaledSafe(this._WoBias, this._WoBiasGrad, num5, this._WoBias.Length);
			this.AddScaledSafe(this._ffW1, this._ffW1Grad, num5, this._ffW1.Length);
			this.AddScaledSafe(this._ffB1, this._ffB1Grad, num5, this._ffB1.Length);
			this.AddScaledSafe(this._ffW2, this._ffW2Grad, num5, this._ffW2.Length);
			this.AddScaledSafe(this._ffB2, this._ffB2Grad, num5, this._ffB2.Length);
			this.AddScaledSafe(this._outputW, this._outputWGrad, num5, this._outputW.Length);
			this.AddScaledSafe(this._outputB, this._outputBGrad, num5, this._outputB.Length);
			if (!float.IsNaN(this._utilPenaltyGrad) && !float.IsInfinity(this._utilPenaltyGrad))
			{
				this._utilPenalty += num5 * this._utilPenaltyGrad;
				this._utilPenalty = Math.Max(0f, Math.Min(10f, this._utilPenalty));
			}
			if (!float.IsNaN(this._queuePenaltyGrad) && !float.IsInfinity(this._queuePenaltyGrad))
			{
				this._queuePenalty += num5 * this._queuePenaltyGrad;
				this._queuePenalty = Math.Max(0f, Math.Min(10f, this._queuePenalty));
			}
		}

		// Token: 0x06000086 RID: 134 RVA: 0x000062C8 File Offset: 0x000044C8
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

		// Token: 0x06000087 RID: 135 RVA: 0x00006308 File Offset: 0x00004508
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

		// Token: 0x06000088 RID: 136 RVA: 0x00006368 File Offset: 0x00004568
		private float[] NormalizeThreadFeatures(float[] features)
		{
			return new float[]
			{
				(float)Math.Log(1.0 + (double)features[0]) / 20f,
				Math.Min(features[1] / 5f, 1f),
				Math.Min(features[2] / 32f, 1f),
				Math.Min(features[3], 1f),
				Math.Min(features[4], 1f)
			};
		}

		// Token: 0x06000089 RID: 137 RVA: 0x000063E4 File Offset: 0x000045E4
		private float[] NormalizeCoreFeatures(float[] features)
		{
			return new float[]
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

		// Token: 0x0600008A RID: 138 RVA: 0x00006480 File Offset: 0x00004680
		private void ProjectThread(float[] features)
		{
			float[] array = this.NormalizeThreadFeatures(features);
			RealtimeScheduler.MatVecMul(this._threadEmbed, this._threadProjW, array, 128, 5);
			RealtimeScheduler.VectorAdd(this._threadEmbed, this._threadProjB, 128);
		}

		// Token: 0x0600008B RID: 139 RVA: 0x000064C4 File Offset: 0x000046C4
		private void ProjectCore(float[] features, int coreIdx)
		{
			float[] array = this.NormalizeCoreFeatures(features);
			int num = coreIdx * 128;
			RealtimeScheduler.MatVecMul(this._allCoreEmbeds, num, this._coreProjW, array, 128, 7);
			RealtimeScheduler.VectorAdd(this._allCoreEmbeds, num, this._coreProjB, 128);
		}

		// Token: 0x0600008C RID: 140 RVA: 0x00006511 File Offset: 0x00004711
		private void ComputeQ()
		{
			RealtimeScheduler.MatVecMul(this._q, this._Wq, this._threadEmbed, 128, 128);
		}

		// Token: 0x0600008D RID: 141 RVA: 0x00006534 File Offset: 0x00004734
		private void ComputeAllKV(int numCores)
		{
			for (int i = 0; i < numCores; i++)
			{
				int num = i * 128;
				int num2 = i * 8 * 16;
				int num3 = i * 8 * 16;
				RealtimeScheduler.MatVecMul(this._allK, num2, this._Wk, this._allCoreEmbeds, num, 128, 128);
				RealtimeScheduler.MatVecMul(this._allV, num3, this._Wv, this._allCoreEmbeds, num, 128, 128);
			}
		}

		// Token: 0x0600008E RID: 142 RVA: 0x000065AC File Offset: 0x000047AC
		private void ComputeAttention(int numCores)
		{
			Array.Clear(this._attentionOutput, 0, 128);
			Array.Clear(this._allHeadOutputs, 0, 128);
			for (int i = 0; i < 8; i++)
			{
				int num = i * 16;
				for (int j = 0; j < numCores; j++)
				{
					int num2 = j * 8 * 16 + i * 16;
					float num3 = RealtimeScheduler.DotProduct(this._q, num, this._allK, num2, 16);
					num3 /= (float)Math.Sqrt(16.0);
					this._allAttentionScores[i * this._maxCores + j] = num3;
				}
				this.SoftMax(this._allAttentionScores, i * this._maxCores, numCores);
			}
			for (int k = 0; k < 8; k++)
			{
				for (int l = 0; l < numCores; l++)
				{
					float num4 = this._allAttentionScores[k * this._maxCores + l];
					int num5 = l * 8 * 16 + k * 16;
					int num6 = k * 16;
					RealtimeScheduler.VectorScalarAdd(this._allHeadOutputs, num6, this._allV, num5, num4, 16);
				}
			}
			Array.Copy(this._allHeadOutputs, this._attentionOutput, 128);
			RealtimeScheduler.MatVecMul(this._tempBuffer1, this._Wo, this._attentionOutput, 128, 128);
			RealtimeScheduler.VectorAdd(this._tempBuffer1, this._WoBias, 128);
			Array.Copy(this._tempBuffer1, this._attentionOutput, 128);
		}

		// Token: 0x0600008F RID: 143 RVA: 0x00006720 File Offset: 0x00004920
		private void FeedForward()
		{
			RealtimeScheduler.MatVecMul(this._ffHidden, this._ffW1, this._attentionOutput, 512, 128);
			RealtimeScheduler.VectorAdd(this._ffHidden, this._ffB1, 512);
			RealtimeScheduler.ReLU(this._ffHidden, 512);
			RealtimeScheduler.MatVecMul(this._ffOutput, this._ffW2, this._ffHidden, 128, 512);
			RealtimeScheduler.VectorAdd(this._ffOutput, this._ffB2, 128);
		}

		// Token: 0x06000090 RID: 144 RVA: 0x000067AC File Offset: 0x000049AC
		private int ComputeFinalScores(int numCores)
		{
			for (int i = 0; i < numCores; i++)
			{
				int num = i * 128;
				for (int j = 0; j < 128; j++)
				{
					this._tempBuffer1[j] = this._ffOutput[j] + this._allCoreEmbeds[num + j];
				}
				float num2 = RealtimeScheduler.DotProduct(this._outputW, 0, this._tempBuffer1, 0, 128) + this._outputB[0];
				float num3 = this._cachedCoreInputs[i][0];
				num2 -= num3 * this._utilPenalty;
				float num4 = this._cachedCoreInputs[i][2];
				num2 -= num4 * this._queuePenalty;
				this._finalScores[i] = num2;
			}
			int num5 = 0;
			float num6 = this._finalScores[0];
			for (int k = 1; k < numCores; k++)
			{
				if (this._finalScores[k] > num6)
				{
					num6 = this._finalScores[k];
					num5 = k;
				}
			}
			return num5;
		}

		// Token: 0x06000091 RID: 145 RVA: 0x0000689C File Offset: 0x00004A9C
		private void ComputeAttentionWithCache(int numCores)
		{
			Array.Clear(this._attentionOutput, 0, 128);
			Array.Clear(this._allHeadOutputs, 0, 128);
			for (int i = 0; i < 8; i++)
			{
				int num = i * 16;
				for (int j = 0; j < numCores; j++)
				{
					int num2 = j * 8 * 16 + i * 16;
					float num3 = RealtimeScheduler.DotProduct(this._q, num, this._allK, num2, 16);
					num3 /= (float)Math.Sqrt(16.0);
					this._cachedAttentionScores[i * this._maxCores + j] = num3;
					this._allAttentionScores[i * this._maxCores + j] = num3;
				}
				this.SoftMax(this._allAttentionScores, i * this._maxCores, numCores);
				for (int k = 0; k < numCores; k++)
				{
					this._cachedAttentionProbs[i * this._maxCores + k] = this._allAttentionScores[i * this._maxCores + k];
				}
			}
			for (int l = 0; l < 8; l++)
			{
				for (int m = 0; m < numCores; m++)
				{
					float num4 = this._allAttentionScores[l * this._maxCores + m];
					int num5 = m * 8 * 16 + l * 16;
					int num6 = l * 16;
					RealtimeScheduler.VectorScalarAdd(this._allHeadOutputs, num6, this._allV, num5, num4, 16);
				}
			}
			Array.Copy(this._allHeadOutputs, this._attentionOutput, 128);
			RealtimeScheduler.MatVecMul(this._cachedProjectedAttention, this._Wo, this._attentionOutput, 128, 128);
			RealtimeScheduler.VectorAdd(this._cachedProjectedAttention, this._WoBias, 128);
			Array.Copy(this._cachedProjectedAttention, this._attentionOutput, 128);
		}

		// Token: 0x06000092 RID: 146 RVA: 0x00006A5C File Offset: 0x00004C5C
		private void FeedForwardWithCache()
		{
			RealtimeScheduler.MatVecMul(this._cachedFfPreActivation, this._ffW1, this._attentionOutput, 512, 128);
			RealtimeScheduler.VectorAdd(this._cachedFfPreActivation, this._ffB1, 512);
			Array.Copy(this._cachedFfPreActivation, this._ffHidden, 512);
			RealtimeScheduler.ReLU(this._ffHidden, 512);
			RealtimeScheduler.MatVecMul(this._ffOutput, this._ffW2, this._ffHidden, 128, 512);
			RealtimeScheduler.VectorAdd(this._ffOutput, this._ffB2, 128);
		}

		// Token: 0x06000093 RID: 147 RVA: 0x00006B00 File Offset: 0x00004D00
		private float[] Softmax(float[] scores, int offset, int length)
		{
			float[] tempSoftmaxProbs = this._tempSoftmaxProbs;
			Array.Clear(tempSoftmaxProbs, 0, length);
			float num = scores[offset];
			for (int i = 1; i < length; i++)
			{
				if (scores[offset + i] > num)
				{
					num = scores[offset + i];
				}
			}
			float num2 = 1f / this._attentionTemperature;
			float num3 = 0f;
			for (int j = 0; j < length; j++)
			{
				tempSoftmaxProbs[j] = (float)Math.Exp((double)((scores[offset + j] - num) * num2));
				num3 += tempSoftmaxProbs[j];
			}
			float num4 = 1f / num3;
			for (int k = 0; k < length; k++)
			{
				tempSoftmaxProbs[k] *= num4;
			}
			return tempSoftmaxProbs;
		}

		// Token: 0x06000094 RID: 148 RVA: 0x00006BA8 File Offset: 0x00004DA8
		private void BackwardOutput(int selectedCore, float advantage, int numCores)
		{
			float[] array = this.Softmax(this._finalScores, 0, numCores);
			float[] tempGradScores = this._tempGradScores;
			Array.Clear(tempGradScores, 0, numCores);
			for (int i = 0; i < numCores; i++)
			{
				tempGradScores[i] = array[i] * advantage;
			}
			tempGradScores[selectedCore] -= advantage;
			int num = selectedCore * 128;
			float[] tempBuffer = this._tempBuffer1;
			for (int j = 0; j < 128; j++)
			{
				tempBuffer[j] = this._ffOutput[j] + this._allCoreEmbeds[num + j];
			}
			for (int k = 0; k < 128; k++)
			{
				this._outputWGrad[k] += tempGradScores[selectedCore] * tempBuffer[k];
			}
			this._outputBGrad[0] += tempGradScores[selectedCore];
			float[] tempGradAllCoreEmbeds = this._tempGradAllCoreEmbeds;
			for (int l = 0; l < 128; l++)
			{
				tempGradAllCoreEmbeds[num + l] += tempGradScores[selectedCore] * this._outputW[l];
			}
			float[] tempBuffer2 = this._tempBuffer3;
			Array.Clear(tempBuffer2, 0, 128);
			for (int m = 0; m < 128; m++)
			{
				tempBuffer2[m] = tempGradScores[selectedCore] * this._outputW[m];
			}
			float num2 = this._cachedCoreInputs[selectedCore][0];
			this._utilPenaltyGrad -= tempGradScores[selectedCore] * num2;
			float num3 = this._cachedCoreInputs[selectedCore][2];
			this._queuePenaltyGrad -= tempGradScores[selectedCore] * num3;
			this.BackwardFfOutput(tempBuffer2);
		}

		// Token: 0x06000095 RID: 149 RVA: 0x00006D2E File Offset: 0x00004F2E
		private void BackwardFfOutput(float[] gradOutput)
		{
			this._gradFfOutput = gradOutput;
		}

		// Token: 0x06000096 RID: 150 RVA: 0x00006D38 File Offset: 0x00004F38
		private void BackwardFeedForward()
		{
			float[] gradFfOutput = this._gradFfOutput;
			for (int i = 0; i < 128; i++)
			{
				for (int j = 0; j < 512; j++)
				{
					this._ffW2Grad[i * 512 + j] += gradFfOutput[i] * this._ffHidden[j];
				}
			}
			for (int k = 0; k < 128; k++)
			{
				this._ffB2Grad[k] += gradFfOutput[k];
			}
			float[] tempBuffer = this._tempBuffer2;
			Array.Clear(tempBuffer, 0, 512);
			for (int l = 0; l < 512; l++)
			{
				for (int m = 0; m < 128; m++)
				{
					tempBuffer[l] += this._ffW2[m * 512 + l] * gradFfOutput[m];
				}
			}
			for (int n = 0; n < 512; n++)
			{
				if (this._cachedFfPreActivation[n] <= 0f)
				{
					tempBuffer[n] = 0f;
				}
			}
			for (int num = 0; num < 512; num++)
			{
				for (int num2 = 0; num2 < 128; num2++)
				{
					this._ffW1Grad[num * 128 + num2] += tempBuffer[num] * this._attentionOutput[num2];
				}
			}
			for (int num3 = 0; num3 < 512; num3++)
			{
				this._ffB1Grad[num3] += tempBuffer[num3];
			}
			float[] tempBuffer2 = this._tempBuffer1;
			Array.Clear(tempBuffer2, 0, 128);
			for (int num4 = 0; num4 < 128; num4++)
			{
				for (int num5 = 0; num5 < 512; num5++)
				{
					tempBuffer2[num4] += this._ffW1[num5 * 128 + num4] * tempBuffer[num5];
				}
			}
			this._gradAttentionOutput = tempBuffer2;
		}

		// Token: 0x06000097 RID: 151 RVA: 0x00006F28 File Offset: 0x00005128
		private void BackwardAttentionOutput()
		{
			float[] gradAttentionOutput = this._gradAttentionOutput;
			for (int i = 0; i < 128; i++)
			{
				for (int j = 0; j < 128; j++)
				{
					this._WoGrad[i * 128 + j] += gradAttentionOutput[i] * this._allHeadOutputs[j];
				}
			}
			for (int k = 0; k < 128; k++)
			{
				this._WoBiasGrad[k] += gradAttentionOutput[k];
			}
			float[] tempBuffer = this._tempBuffer3;
			Array.Clear(tempBuffer, 0, 128);
			for (int l = 0; l < 128; l++)
			{
				for (int m = 0; m < 128; m++)
				{
					tempBuffer[l] += this._Wo[m * 128 + l] * gradAttentionOutput[m];
				}
			}
			this._gradAllHeadOutputs = tempBuffer;
		}

		// Token: 0x06000098 RID: 152 RVA: 0x00007010 File Offset: 0x00005210
		private void BackwardAttention(int numCores)
		{
			float[] gradAllHeadOutputs = this._gradAllHeadOutputs;
			float[] tempGradAttentionProbs = this._tempGradAttentionProbs;
			float[] tempGradAllV = this._tempGradAllV;
			Array.Clear(tempGradAttentionProbs, 0, 8 * this._maxCores);
			Array.Clear(tempGradAllV, 0, this._maxCores * 8 * 16);
			for (int i = 0; i < 8; i++)
			{
				int num = i * 16;
				for (int j = 0; j < numCores; j++)
				{
					int num2 = j * 8 * 16 + i * 16;
					float num3 = this._cachedAttentionProbs[i * this._maxCores + j];
					for (int k = 0; k < 16; k++)
					{
						tempGradAttentionProbs[i * this._maxCores + j] += gradAllHeadOutputs[num + k] * this._allV[num2 + k];
						tempGradAllV[num2 + k] += num3 * gradAllHeadOutputs[num + k];
					}
				}
			}
			float[] tempGradAttentionScores = this._tempGradAttentionScores;
			Array.Clear(tempGradAttentionScores, 0, 8 * this._maxCores);
			for (int l = 0; l < 8; l++)
			{
				float num4 = 0f;
				for (int m = 0; m < numCores; m++)
				{
					num4 += tempGradAttentionProbs[l * this._maxCores + m] * this._cachedAttentionProbs[l * this._maxCores + m];
				}
				for (int n = 0; n < numCores; n++)
				{
					float num5 = this._cachedAttentionProbs[l * this._maxCores + n];
					tempGradAttentionScores[l * this._maxCores + n] = num5 * (tempGradAttentionProbs[l * this._maxCores + n] - num4);
				}
			}
			float num6 = 1f / (float)Math.Sqrt(16.0);
			float[] tempGradQ = this._tempGradQ;
			float[] tempGradAllK = this._tempGradAllK;
			Array.Clear(tempGradQ, 0, 128);
			Array.Clear(tempGradAllK, 0, this._maxCores * 8 * 16);
			for (int num7 = 0; num7 < 8; num7++)
			{
				int num8 = num7 * 16;
				for (int num9 = 0; num9 < numCores; num9++)
				{
					int num10 = num9 * 8 * 16 + num7 * 16;
					float num11 = tempGradAttentionScores[num7 * this._maxCores + num9] * num6;
					for (int num12 = 0; num12 < 16; num12++)
					{
						tempGradQ[num8 + num12] += num11 * this._allK[num10 + num12];
						tempGradAllK[num10 + num12] += num11 * this._q[num8 + num12];
					}
				}
			}
			this._gradQ = tempGradQ;
			this._gradAllK = tempGradAllK;
			this._gradAllV = tempGradAllV;
		}

		// Token: 0x06000099 RID: 153 RVA: 0x000072A8 File Offset: 0x000054A8
		private void BackwardQKV(int numCores)
		{
			float[] gradQ = this._gradQ;
			for (int i = 0; i < 128; i++)
			{
				for (int j = 0; j < 128; j++)
				{
					this._WqGrad[i * 128 + j] += gradQ[i] * this._threadEmbed[j];
				}
			}
			float[] tempBuffer = this._tempBuffer1;
			Array.Clear(tempBuffer, 0, 128);
			for (int k = 0; k < 128; k++)
			{
				for (int l = 0; l < 128; l++)
				{
					tempBuffer[k] += this._Wq[l * 128 + k] * gradQ[l];
				}
			}
			float[] gradAllK = this._gradAllK;
			float[] gradAllV = this._gradAllV;
			float[] tempGradAllCoreEmbeds = this._tempGradAllCoreEmbeds;
			Array.Clear(tempGradAllCoreEmbeds, 0, this._maxCores * 128);
			for (int m = 0; m < numCores; m++)
			{
				int num = m * 128;
				int num2 = m * 8 * 16;
				for (int n = 0; n < 128; n++)
				{
					for (int num3 = 0; num3 < 128; num3++)
					{
						this._WkGrad[n * 128 + num3] += gradAllK[num2 + n] * this._allCoreEmbeds[num + num3];
						tempGradAllCoreEmbeds[num + num3] += this._Wk[n * 128 + num3] * gradAllK[num2 + n];
					}
				}
				for (int num4 = 0; num4 < 128; num4++)
				{
					for (int num5 = 0; num5 < 128; num5++)
					{
						this._WvGrad[num4 * 128 + num5] += gradAllV[num2 + num4] * this._allCoreEmbeds[num + num5];
						tempGradAllCoreEmbeds[num + num5] += this._Wv[num4 * 128 + num5] * gradAllV[num2 + num4];
					}
				}
			}
			this._gradThreadEmbed = tempBuffer;
			this._gradAllCoreEmbeds = tempGradAllCoreEmbeds;
		}

		// Token: 0x0600009A RID: 154 RVA: 0x000074CC File Offset: 0x000056CC
		private void BackwardInputProjections(int numCores)
		{
			float[] gradThreadEmbed = this._gradThreadEmbed;
			float[] gradAllCoreEmbeds = this._gradAllCoreEmbeds;
			for (int i = 0; i < 128; i++)
			{
				for (int j = 0; j < 5; j++)
				{
					this._threadProjWGrad[i * 5 + j] += gradThreadEmbed[i] * this._cachedThreadInput[j];
				}
				this._threadProjBGrad[i] += gradThreadEmbed[i];
			}
			for (int k = 0; k < numCores; k++)
			{
				int num = k * 128;
				for (int l = 0; l < 128; l++)
				{
					for (int m = 0; m < 7; m++)
					{
						this._coreProjWGrad[l * 7 + m] += gradAllCoreEmbeds[num + l] * this._cachedCoreInputs[k][m];
					}
					this._coreProjBGrad[l] += gradAllCoreEmbeds[num + l];
				}
			}
		}

		// Token: 0x0600009B RID: 155 RVA: 0x000075B8 File Offset: 0x000057B8
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
			while (i < length)
			{
				dest[i] += src[i] * scale;
				i++;
			}
		}

		// Token: 0x0600009C RID: 156 RVA: 0x00007624 File Offset: 0x00005824
		private static void MatVecMul(float[] result, float[] matrix, float[] vector, int rows, int cols)
		{
			int count = Vector<float>.Count;
			for (int i = 0; i < rows; i++)
			{
				float num = 0f;
				int j = 0;
				if (cols >= count)
				{
					Vector<float> vector2 = Vector<float>.Zero;
					while (j <= cols - count)
					{
						Vector<float> vector3 = new Vector<float>(matrix, i * cols + j);
						Vector<float> vector4 = new Vector<float>(vector, j);
						vector2 += vector3 * vector4;
						j += count;
					}
					num = Vector.Dot<float>(vector2, Vector<float>.One);
				}
				while (j < cols)
				{
					num += matrix[i * cols + j] * vector[j];
					j++;
				}
				result[i] = num;
			}
		}

		// Token: 0x0600009D RID: 157 RVA: 0x000076B8 File Offset: 0x000058B8
		private static void MatVecMul(float[] result, int resultOffset, float[] matrix, float[] vector, int rows, int cols)
		{
			int count = Vector<float>.Count;
			for (int i = 0; i < rows; i++)
			{
				float num = 0f;
				int j = 0;
				if (cols >= count)
				{
					Vector<float> vector2 = Vector<float>.Zero;
					while (j <= cols - count)
					{
						Vector<float> vector3 = new Vector<float>(matrix, i * cols + j);
						Vector<float> vector4 = new Vector<float>(vector, j);
						vector2 += vector3 * vector4;
						j += count;
					}
					num = Vector.Dot<float>(vector2, Vector<float>.One);
				}
				while (j < cols)
				{
					num += matrix[i * cols + j] * vector[j];
					j++;
				}
				result[resultOffset + i] = num;
			}
		}

		// Token: 0x0600009E RID: 158 RVA: 0x00007750 File Offset: 0x00005950
		private static void MatVecMul(float[] result, int resultOffset, float[] matrix, float[] vector, int vectorOffset, int rows, int cols)
		{
			int count = Vector<float>.Count;
			for (int i = 0; i < rows; i++)
			{
				float num = 0f;
				int j = 0;
				if (cols >= count)
				{
					Vector<float> vector2 = Vector<float>.Zero;
					while (j <= cols - count)
					{
						Vector<float> vector3 = new Vector<float>(matrix, i * cols + j);
						Vector<float> vector4 = new Vector<float>(vector, vectorOffset + j);
						vector2 += vector3 * vector4;
						j += count;
					}
					num = Vector.Dot<float>(vector2, Vector<float>.One);
				}
				while (j < cols)
				{
					num += matrix[i * cols + j] * vector[vectorOffset + j];
					j++;
				}
				result[resultOffset + i] = num;
			}
		}

		// Token: 0x0600009F RID: 159 RVA: 0x000077F4 File Offset: 0x000059F4
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
			while (i < length)
			{
				result[i] += bias[i];
				i++;
			}
		}

		// Token: 0x060000A0 RID: 160 RVA: 0x00007850 File Offset: 0x00005A50
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
			while (i < length)
			{
				result[offset + i] += bias[i];
				i++;
			}
		}

		// Token: 0x060000A1 RID: 161 RVA: 0x000078B0 File Offset: 0x00005AB0
		private static float DotProduct(float[] a, int offsetA, float[] b, int offsetB, int length)
		{
			int count = Vector<float>.Count;
			Vector<float> vector = Vector<float>.Zero;
			int i;
			for (i = 0; i <= length - count; i += count)
			{
				Vector<float> vector2 = new Vector<float>(a, offsetA + i);
				Vector<float> vector3 = new Vector<float>(b, offsetB + i);
				vector += vector2 * vector3;
			}
			float num = Vector.Dot<float>(vector, Vector<float>.One);
			while (i < length)
			{
				num += a[offsetA + i] * b[offsetB + i];
				i++;
			}
			return num;
		}

		// Token: 0x060000A2 RID: 162 RVA: 0x00007924 File Offset: 0x00005B24
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
			while (i < length)
			{
				result[resultOffset + i] += source[sourceOffset + i] * scalar;
				i++;
			}
		}

		// Token: 0x060000A3 RID: 163 RVA: 0x0000799C File Offset: 0x00005B9C
		private static void ReLU(float[] data, int length)
		{
			int count = Vector<float>.Count;
			Vector<float> zero = Vector<float>.Zero;
			int i;
			for (i = 0; i <= length - count; i += count)
			{
				Vector.Max<float>(new Vector<float>(data, i), zero).CopyTo(data, i);
			}
			while (i < length)
			{
				if (data[i] < 0f)
				{
					data[i] = 0f;
				}
				i++;
			}
		}

		// Token: 0x060000A4 RID: 164 RVA: 0x000079F8 File Offset: 0x00005BF8
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
			float num2 = 1f / this._attentionTemperature;
			float num3 = 0f;
			for (int j = 0; j < length; j++)
			{
				data[offset + j] = (float)Math.Exp((double)((data[offset + j] - num) * num2));
				num3 += data[offset + j];
			}
			float num4 = 1f / num3;
			for (int k = 0; k < length; k++)
			{
				data[offset + k] *= num4;
			}
		}

		// Token: 0x060000A5 RID: 165 RVA: 0x00007A94 File Offset: 0x00005C94
		public float[] GetAllWeights()
		{
			float[] array = new float[this._threadProjW.Length + this._threadProjB.Length + this._coreProjW.Length + this._coreProjB.Length + this._Wq.Length + this._Wk.Length + this._Wv.Length + this._Wo.Length + this._WoBias.Length + this._ffW1.Length + this._ffB1.Length + this._ffW2.Length + this._ffB2.Length + this._outputW.Length + this._outputB.Length + 2];
			int num = 0;
			RealtimeScheduler.CopyToArray(array, ref num, this._threadProjW);
			RealtimeScheduler.CopyToArray(array, ref num, this._threadProjB);
			RealtimeScheduler.CopyToArray(array, ref num, this._coreProjW);
			RealtimeScheduler.CopyToArray(array, ref num, this._coreProjB);
			RealtimeScheduler.CopyToArray(array, ref num, this._Wq);
			RealtimeScheduler.CopyToArray(array, ref num, this._Wk);
			RealtimeScheduler.CopyToArray(array, ref num, this._Wv);
			RealtimeScheduler.CopyToArray(array, ref num, this._Wo);
			RealtimeScheduler.CopyToArray(array, ref num, this._WoBias);
			RealtimeScheduler.CopyToArray(array, ref num, this._ffW1);
			RealtimeScheduler.CopyToArray(array, ref num, this._ffB1);
			RealtimeScheduler.CopyToArray(array, ref num, this._ffW2);
			RealtimeScheduler.CopyToArray(array, ref num, this._ffB2);
			RealtimeScheduler.CopyToArray(array, ref num, this._outputW);
			RealtimeScheduler.CopyToArray(array, ref num, this._outputB);
			array[num++] = this._utilPenalty;
			array[num++] = this._queuePenalty;
			return array;
		}

		// Token: 0x060000A6 RID: 166 RVA: 0x00007C1C File Offset: 0x00005E1C
		public void SetAllWeights(float[] weights)
		{
			int num = 0;
			RealtimeScheduler.CopyFromArray(weights, ref num, this._threadProjW);
			RealtimeScheduler.CopyFromArray(weights, ref num, this._threadProjB);
			RealtimeScheduler.CopyFromArray(weights, ref num, this._coreProjW);
			RealtimeScheduler.CopyFromArray(weights, ref num, this._coreProjB);
			RealtimeScheduler.CopyFromArray(weights, ref num, this._Wq);
			RealtimeScheduler.CopyFromArray(weights, ref num, this._Wk);
			RealtimeScheduler.CopyFromArray(weights, ref num, this._Wv);
			RealtimeScheduler.CopyFromArray(weights, ref num, this._Wo);
			RealtimeScheduler.CopyFromArray(weights, ref num, this._WoBias);
			RealtimeScheduler.CopyFromArray(weights, ref num, this._ffW1);
			RealtimeScheduler.CopyFromArray(weights, ref num, this._ffB1);
			RealtimeScheduler.CopyFromArray(weights, ref num, this._ffW2);
			RealtimeScheduler.CopyFromArray(weights, ref num, this._ffB2);
			RealtimeScheduler.CopyFromArray(weights, ref num, this._outputW);
			RealtimeScheduler.CopyFromArray(weights, ref num, this._outputB);
			this._utilPenalty = weights[num++];
			this._queuePenalty = weights[num++];
		}

		// Token: 0x060000A7 RID: 167 RVA: 0x00007D18 File Offset: 0x00005F18
		public int GetWeightCount()
		{
			return this._threadProjW.Length + this._threadProjB.Length + this._coreProjW.Length + this._coreProjB.Length + this._Wq.Length + this._Wk.Length + this._Wv.Length + this._Wo.Length + this._WoBias.Length + this._ffW1.Length + this._ffB1.Length + this._ffW2.Length + this._ffB2.Length + this._outputW.Length + this._outputB.Length + 2 + 128 + 1;
		}

		// Token: 0x060000A8 RID: 168 RVA: 0x00007DB8 File Offset: 0x00005FB8
		public void ApplyGradients(float[] gradients, float learningRate)
		{
			if (gradients.Length != this.GetWeightCount())
			{
				throw new ArgumentException("Gradient size mismatch");
			}
			float[] allWeights = this.GetAllWeights();
			for (int i = 0; i < allWeights.Length; i++)
			{
				allWeights[i] += learningRate * gradients[i];
			}
			this.SetAllWeights(allWeights);
		}

		// Token: 0x060000A9 RID: 169 RVA: 0x00007E07 File Offset: 0x00006007
		private static void CopyToArray(float[] dest, ref int offset, float[] source)
		{
			Array.Copy(source, 0, dest, offset, source.Length);
			offset += source.Length;
		}

		// Token: 0x060000AA RID: 170 RVA: 0x00007E1E File Offset: 0x0000601E
		private static void CopyFromArray(float[] source, ref int offset, float[] dest)
		{
			Array.Copy(source, offset, dest, 0, dest.Length);
			offset += dest.Length;
		}

		// Token: 0x060000AB RID: 171 RVA: 0x00007E35 File Offset: 0x00006035
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Token: 0x060000AC RID: 172 RVA: 0x00007E44 File Offset: 0x00006044
		protected virtual void Dispose(bool disposing)
		{
			if (!this._disposed)
			{
				this._disposed = true;
			}
		}

		// Token: 0x060000AD RID: 173 RVA: 0x00007E58 File Offset: 0x00006058
		~RealtimeScheduler()
		{
			this.Dispose(false);
		}

		// Token: 0x04000072 RID: 114
		public const int ThreadFeatureDim = 5;

		// Token: 0x04000073 RID: 115
		public const int CoreFeatureDim = 7;

		// Token: 0x04000074 RID: 116
		public const int NumHeads = 8;

		// Token: 0x04000075 RID: 117
		public const int HeadDim = 16;

		// Token: 0x04000076 RID: 118
		public const int DModel = 128;

		// Token: 0x04000077 RID: 119
		public const int ThreadInstructions = 0;

		// Token: 0x04000078 RID: 120
		public const int ThreadIPC = 1;

		// Token: 0x04000079 RID: 121
		public const int ThreadPriority = 2;

		// Token: 0x0400007A RID: 122
		public const int ThreadLLCMissRate = 3;

		// Token: 0x0400007B RID: 123
		public const int ThreadBranchMispredRate = 4;

		// Token: 0x0400007C RID: 124
		public const int CoreUtilization = 0;

		// Token: 0x0400007D RID: 125
		public const int CoreAvgQueueExecTime = 1;

		// Token: 0x0400007E RID: 126
		public const int CoreQueueThreads = 2;

		// Token: 0x0400007F RID: 127
		public const int CoreLLCMissRate = 3;

		// Token: 0x04000080 RID: 128
		public const int CoreL1MissRate = 3;

		// Token: 0x04000081 RID: 129
		public const int CoreAvgIPC = 4;

		// Token: 0x04000082 RID: 130
		public const int CorePerformance = 5;

		// Token: 0x04000083 RID: 131
		public const int CorePriority = 6;

		// Token: 0x04000084 RID: 132
		private float[] _threadProjW;

		// Token: 0x04000085 RID: 133
		private float[] _threadProjB;

		// Token: 0x04000086 RID: 134
		private float[] _coreProjW;

		// Token: 0x04000087 RID: 135
		private float[] _coreProjB;

		// Token: 0x04000088 RID: 136
		private float[] _Wq;

		// Token: 0x04000089 RID: 137
		private float[] _Wk;

		// Token: 0x0400008A RID: 138
		private float[] _Wv;

		// Token: 0x0400008B RID: 139
		private float[] _Wo;

		// Token: 0x0400008C RID: 140
		private float[] _WoBias;

		// Token: 0x0400008D RID: 141
		private float[] _ffW1;

		// Token: 0x0400008E RID: 142
		private float[] _ffB1;

		// Token: 0x0400008F RID: 143
		private float[] _ffW2;

		// Token: 0x04000090 RID: 144
		private float[] _ffB2;

		// Token: 0x04000091 RID: 145
		private float[] _outputW;

		// Token: 0x04000092 RID: 146
		private float[] _outputB;

		// Token: 0x04000093 RID: 147
		private float _utilPenalty;

		// Token: 0x04000094 RID: 148
		private float _queuePenalty;

		// Token: 0x04000095 RID: 149
		private float[] _threadEmbed;

		// Token: 0x04000096 RID: 150
		private float[] _coreEmbed;

		// Token: 0x04000097 RID: 151
		private float[] _q;

		// Token: 0x04000098 RID: 152
		private float[] _k;

		// Token: 0x04000099 RID: 153
		private float[] _v;

		// Token: 0x0400009A RID: 154
		private float[] _attentionScores;

		// Token: 0x0400009B RID: 155
		private float[] _attentionOutput;

		// Token: 0x0400009C RID: 156
		private float[] _ffHidden;

		// Token: 0x0400009D RID: 157
		private float[] _ffOutput;

		// Token: 0x0400009E RID: 158
		private int _maxCores;

		// Token: 0x0400009F RID: 159
		private float[] _allCoreEmbeds;

		// Token: 0x040000A0 RID: 160
		private float[] _allK;

		// Token: 0x040000A1 RID: 161
		private float[] _allV;

		// Token: 0x040000A2 RID: 162
		private float[] _allAttentionScores;

		// Token: 0x040000A3 RID: 163
		private float[] _allHeadOutputs;

		// Token: 0x040000A4 RID: 164
		private float[] _finalScores;

		// Token: 0x040000A5 RID: 165
		private float[] _cachedThreadInput;

		// Token: 0x040000A6 RID: 166
		private float[][] _cachedCoreInputs;

		// Token: 0x040000A7 RID: 167
		private float[] _cachedAttentionScores;

		// Token: 0x040000A8 RID: 168
		private float[] _cachedAttentionProbs;

		// Token: 0x040000A9 RID: 169
		private float[] _cachedProjectedAttention;

		// Token: 0x040000AA RID: 170
		private float[] _cachedFfPreActivation;

		// Token: 0x040000AB RID: 171
		private int _cachedNumCores;

		// Token: 0x040000AC RID: 172
		private float[] _threadProjWGrad;

		// Token: 0x040000AD RID: 173
		private float[] _threadProjBGrad;

		// Token: 0x040000AE RID: 174
		private float[] _coreProjWGrad;

		// Token: 0x040000AF RID: 175
		private float[] _coreProjBGrad;

		// Token: 0x040000B0 RID: 176
		private float[] _WqGrad;

		// Token: 0x040000B1 RID: 177
		private float[] _WkGrad;

		// Token: 0x040000B2 RID: 178
		private float[] _WvGrad;

		// Token: 0x040000B3 RID: 179
		private float[] _WoGrad;

		// Token: 0x040000B4 RID: 180
		private float[] _WoBiasGrad;

		// Token: 0x040000B5 RID: 181
		private float[] _ffW1Grad;

		// Token: 0x040000B6 RID: 182
		private float[] _ffB1Grad;

		// Token: 0x040000B7 RID: 183
		private float[] _ffW2Grad;

		// Token: 0x040000B8 RID: 184
		private float[] _ffB2Grad;

		// Token: 0x040000B9 RID: 185
		private float[] _outputWGrad;

		// Token: 0x040000BA RID: 186
		private float[] _outputBGrad;

		// Token: 0x040000BB RID: 187
		private float _utilPenaltyGrad;

		// Token: 0x040000BC RID: 188
		private float _queuePenaltyGrad;

		// Token: 0x040000BD RID: 189
		private float[] _tempBuffer1;

		// Token: 0x040000BE RID: 190
		private float[] _tempBuffer2;

		// Token: 0x040000BF RID: 191
		private float[] _tempBuffer3;

		// Token: 0x040000C0 RID: 192
		private float[] _tempGradScores;

		// Token: 0x040000C1 RID: 193
		private float[] _tempGradAttentionProbs;

		// Token: 0x040000C2 RID: 194
		private float[] _tempGradAllV;

		// Token: 0x040000C3 RID: 195
		private float[] _tempGradAttentionScores;

		// Token: 0x040000C4 RID: 196
		private float[] _tempGradQ;

		// Token: 0x040000C5 RID: 197
		private float[] _tempGradAllK;

		// Token: 0x040000C6 RID: 198
		private float[] _tempGradAllCoreEmbeds;

		// Token: 0x040000C7 RID: 199
		private float[] _tempSoftmaxProbs;

		// Token: 0x040000C8 RID: 200
		private long _inferenceCount;

		// Token: 0x040000C9 RID: 201
		private double _totalInferenceTimeUs;

		// Token: 0x040000CA RID: 202
		private readonly object _statsLock = new object();

		// Token: 0x040000CB RID: 203
		private float _attentionTemperature = 3f;

		// Token: 0x040000CC RID: 204
		private float[] _gradFfOutput;

		// Token: 0x040000CD RID: 205
		private float[] _gradAttentionOutput;

		// Token: 0x040000CE RID: 206
		private float[] _gradAllHeadOutputs;

		// Token: 0x040000CF RID: 207
		private float[] _gradQ;

		// Token: 0x040000D0 RID: 208
		private float[] _gradAllK;

		// Token: 0x040000D1 RID: 209
		private float[] _gradAllV;

		// Token: 0x040000D2 RID: 210
		private float[] _gradThreadEmbed;

		// Token: 0x040000D3 RID: 211
		private float[] _gradAllCoreEmbeds;

		// Token: 0x040000D4 RID: 212
		private bool _disposed;
	}
}
