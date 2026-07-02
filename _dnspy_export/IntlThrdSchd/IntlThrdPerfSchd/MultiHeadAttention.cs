using System;
using System.Runtime.CompilerServices;
using SimdLibrary;

namespace IntlThrdPerfSchd
{
	// Token: 0x0200001C RID: 28
	public class MultiHeadAttention
	{
		// Token: 0x1700002B RID: 43
		// (get) Token: 0x060001B1 RID: 433 RVA: 0x00016E1B File Offset: 0x0001501B
		public int NumHeads
		{
			get
			{
				return this._nHead;
			}
		}

		// Token: 0x060001B2 RID: 434 RVA: 0x00016E24 File Offset: 0x00015024
		public float[][] GetHeadAttentionWeights(int numCores)
		{
			float[][] array = new float[this._nHead][];
			for (int i = 0; i < this._nHead; i++)
			{
				array[i] = new float[numCores];
				for (int j = 0; j < numCores; j++)
				{
					array[i][j] = this._attentionWeights[i * 64 + j];
				}
			}
			return array;
		}

		// Token: 0x060001B3 RID: 435 RVA: 0x00016E78 File Offset: 0x00015078
		public MultiHeadAttention(int dModel, int nHead)
		{
			this._dModel = dModel;
			this._nHead = nHead;
			this._dK = dModel / nHead;
			this._dV = dModel / nHead;
			this.Wq = new LinearLayer(dModel, dModel);
			this.Wk = new LinearLayer(dModel, dModel);
			this.Wv = new LinearLayer(dModel, dModel);
			this.Wo = new LinearLayer(dModel, dModel);
			this._qProj = new float[dModel];
			this._kProj = new float[dModel];
			this._vProj = new float[dModel];
			this._attentionScores = new float[nHead * 64];
			this._attentionWeights = new float[nHead * 64];
			this._headOutputs = new float[nHead * this._dV];
			this._concatHeads = new float[dModel];
			this._cachedKeys = new float[64][];
			this._cachedValues = new float[64][];
			for (int i = 0; i < 64; i++)
			{
				this._cachedKeys[i] = new float[dModel];
				this._cachedValues[i] = new float[dModel];
			}
			this._cachedQProj = new float[dModel];
			this._cachedQuery = new float[dModel];
			this._cachedNumCores = 0;
			this._cached = false;
			this._gradQProj = new float[dModel];
			this._gradConcatHeads = new float[dModel];
			this._gradAttentionWeights = new float[nHead * 64];
			this._gradAttentionScores = new float[nHead * 64];
			this._gradHeadOutputs = new float[nHead * this._dV];
			this._gradKeys = new float[dModel * 64];
			this._gradValues = new float[dModel * 64];
		}

		// Token: 0x060001B4 RID: 436 RVA: 0x00017010 File Offset: 0x00015210
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void CrossAttention(ReadOnlySpan<float> query, float[][] keys, float[][] values, Span<float> output, int numCores, Span<float> attentionWeights = default(Span<float>), float softmaxTemperature = 1f)
		{
			query.CopyTo(this._cachedQuery);
			for (int i = 0; i < numCores; i++)
			{
				Array.Copy(keys[i], this._cachedKeys[i], this._dModel);
				Array.Copy(values[i], this._cachedValues[i], this._dModel);
			}
			this._cachedNumCores = numCores;
			this.Wq.Forward(query, this._qProj);
			Array.Copy(this._qProj, this._cachedQProj, this._dModel);
			for (int j = 0; j < this._nHead; j++)
			{
				int num = j * this._dK;
				for (int k = 0; k < numCores; k++)
				{
					this._attentionScores[j * 64 + k] = VectorMathNew.DotProduct(this._qProj.AsSpan(num, this._dK), keys[k].AsSpan(num, this._dK)) / (float)Math.Sqrt((double)this._dK);
				}
				int num2 = j * 64;
				if (softmaxTemperature != 1f)
				{
					float num3 = 1f / softmaxTemperature;
					for (int l = 0; l < numCores; l++)
					{
						this._attentionScores[num2 + l] *= num3;
					}
				}
				ReadOnlySpan<float> readOnlySpan = new ReadOnlySpan<float>(this._attentionScores, num2, numCores);
				Span<float> span = new Span<float>(this._attentionWeights, num2, numCores);
				VectorMathNew.Softmax(readOnlySpan, span);
				VectorMathNew.BatchWeightedSum(span, values, this._headOutputs.AsSpan(j * this._dV, this._dV), numCores, this._dV, j * this._dV);
			}
			for (int m = 0; m < this._nHead; m++)
			{
				Buffer.BlockCopy(this._headOutputs, m * this._dV * 4, this._concatHeads, m * this._dV * 4, this._dV * 4);
			}
			this.Wo.Forward(this._concatHeads, output);
			if (!attentionWeights.IsEmpty)
			{
				Span<float> span2 = attentionWeights.Slice(0, numCores);
				VectorMathNew.Zero(span2);
				for (int n = 0; n < this._nHead; n++)
				{
					int num4 = n * 64;
					Span<float> span3 = this._attentionWeights.AsSpan(num4, numCores);
					VectorMathNew.Add(span2, span3, span2);
				}
				VectorMathNew.MultiplyScalarInPlace(span2, 1f / (float)this._nHead);
			}
			this._cached = true;
		}

		// Token: 0x060001B5 RID: 437 RVA: 0x0001728C File Offset: 0x0001548C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe void Backward(ReadOnlySpan<float> gradOutput, float learningRate = 0.001f)
		{
			if (!this._cached)
			{
				return;
			}
			if (VectorMathNew.HasInvalidValues(gradOutput))
			{
				return;
			}
			int num = Math.Min(gradOutput.Length, this._dModel);
			for (int i = 0; i < num; i++)
			{
				this._gradConcatHeads[i] = *gradOutput[i];
			}
			float num2 = VectorMathNew.EuclideanNorm(this._gradConcatHeads.AsSpan(0, num));
			if (num2 > 1f)
			{
				float num3 = 1f / num2;
				VectorMathNew.MultiplyScalarInPlace(this._gradConcatHeads.AsSpan(0, num), num3);
			}
			Array.Clear(this._gradKeys, 0, this._dModel * 64);
			Array.Clear(this._gradValues, 0, this._dModel * 64);
			this.Wo.Backward(this._gradConcatHeads.AsSpan(0, this._dModel), learningRate, false);
			float[] inputGrads = this.Wo.InputGrads;
			for (int j = 0; j < this._nHead; j++)
			{
				int num4 = j * this._dV;
				int num5 = j * 64;
				ReadOnlySpan<float> readOnlySpan = new ReadOnlySpan<float>(inputGrads, num4, this._dV);
				Span<float> span = new Span<float>(this._gradAttentionWeights, num5, this._cachedNumCores);
				VectorMathNew.BatchVectorsDotVector(this._cachedValues, readOnlySpan, span, this._cachedNumCores, this._dV);
				float num6 = VectorMathNew.DotProduct(new ReadOnlySpan<float>(this._attentionWeights, num5, this._cachedNumCores), span);
				for (int k = 0; k < this._cachedNumCores; k++)
				{
					float num7 = this._attentionWeights[num5 + k];
					this._gradAttentionScores[num5 + k] = num7 * (this._gradAttentionWeights[num5 + k] - num6);
				}
				float num8 = 1f / (float)Math.Sqrt((double)this._dK);
				VectorMathNew.BatchWeightedDotProduct(new ReadOnlySpan<float>(this._gradAttentionScores, num5, this._cachedNumCores), this._cachedKeys, this._gradQProj.AsSpan(num4, this._dK), this._cachedNumCores, this._dK);
				VectorMathNew.MultiplyScalarInPlace(this._gradQProj.AsSpan(num4, this._dK), num8);
				for (int l = 0; l < this._cachedNumCores; l++)
				{
					float num9 = this._gradAttentionScores[num5 + l] * num8;
					int num10 = l * this._dModel;
					for (int m = 0; m < this._dK; m++)
					{
						this._gradKeys[num10 + num4 + m] += num9 * this._cachedQuery[num4 + m];
					}
				}
				for (int n = 0; n < this._cachedNumCores; n++)
				{
					float num11 = this._attentionWeights[num5 + n];
					int num12 = n * this._dModel;
					int num13 = num4;
					for (int num14 = 0; num14 < this._dV; num14++)
					{
						this._gradValues[num12 + num13 + num14] += num11 * inputGrads[num13 + num14];
					}
				}
			}
			this.Wq.Backward(this._gradQProj, learningRate, false);
		}

		// Token: 0x060001B6 RID: 438 RVA: 0x0001759C File Offset: 0x0001579C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SelfForward(ReadOnlySpan<float> input, Span<float> output)
		{
			input.CopyTo(this._cachedQuery);
			this.Wq.Forward(input, this._qProj);
			this.Wk.Forward(input, this._kProj);
			this.Wv.Forward(input, this._vProj);
			Array.Copy(this._qProj, this._cachedQProj, this._dModel);
			for (int i = 0; i < 1; i++)
			{
				Array.Copy(this._kProj, this._cachedKeys[i], this._dModel);
				Array.Copy(this._vProj, this._cachedValues[i], this._dModel);
			}
			this._cachedNumCores = 1;
			for (int j = 0; j < this._nHead; j++)
			{
				int num = j * this._dK;
				Span<float> span = this._qProj.AsSpan(num, this._dK);
				Span<float> span2 = this._kProj.AsSpan(num, this._dK);
				float num2 = VectorMathNew.DotProduct(span, span2) / (float)Math.Sqrt((double)this._dK);
				this._attentionScores[j * 64] = num2;
				this._attentionWeights[j * 64] = 1f;
				Span<float> span3 = this._vProj.AsSpan(num, this._dV);
				Span<float> span4 = this._headOutputs.AsSpan(j * this._dV, this._dV);
				VectorMathNew.Copy(span3, span4);
			}
			for (int k = 0; k < this._nHead; k++)
			{
				Buffer.BlockCopy(this._headOutputs, k * this._dV * 4, this._concatHeads, k * this._dV * 4, this._dV * 4);
			}
			this.Wo.Forward(this._concatHeads, output);
			this._cached = true;
		}

		// Token: 0x060001B7 RID: 439 RVA: 0x0001777C File Offset: 0x0001597C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe void SelfBackward(ReadOnlySpan<float> gradOutput, float learningRate = 0.001f)
		{
			if (!this._cached)
			{
				return;
			}
			if (VectorMathNew.HasInvalidValues(gradOutput))
			{
				return;
			}
			int num = Math.Min(gradOutput.Length, this._dModel);
			for (int i = 0; i < num; i++)
			{
				this._gradConcatHeads[i] = *gradOutput[i];
			}
			float num2 = VectorMathNew.EuclideanNorm(this._gradConcatHeads.AsSpan(0, num));
			if (num2 > 1f)
			{
				float num3 = 1f / num2;
				VectorMathNew.MultiplyScalarInPlace(this._gradConcatHeads.AsSpan(0, num), num3);
			}
			Array.Clear(this._gradKeys, 0, this._dModel);
			Array.Clear(this._gradValues, 0, this._dModel);
			this.Wo.Backward(this._gradConcatHeads.AsSpan(0, this._dModel), learningRate, false);
			float[] inputGrads = this.Wo.InputGrads;
			for (int j = 0; j < this._nHead; j++)
			{
				int num4 = j * this._dV;
				int num5 = j * 64;
				ReadOnlySpan<float> readOnlySpan = new ReadOnlySpan<float>(inputGrads, num4, this._dV);
				Span<float> span = new Span<float>(this._gradAttentionWeights, num5, 1);
				VectorMathNew.BatchVectorsDotVector(this._cachedValues, readOnlySpan, span, 1, this._dV);
				float num6 = 0f;
				for (int k = 0; k < this._dV; k++)
				{
					num6 += inputGrads[num4 + k];
				}
				this._gradAttentionScores[num5] = num6;
				float num7 = 1f / (float)Math.Sqrt((double)this._dK);
				float num8 = this._gradAttentionScores[num5] * num7;
				for (int l = 0; l < this._dK; l++)
				{
					this._gradQProj[num4 + l] = num8 * this._cachedKeys[0][num4 + l];
				}
				for (int m = 0; m < this._dK; m++)
				{
					this._gradKeys[num4 + m] += num8 * this._cachedQuery[num4 + m];
				}
				float num9 = this._attentionWeights[num5];
				for (int n = 0; n < this._dV; n++)
				{
					this._gradValues[num4 + n] += num9 * inputGrads[num4 + n];
				}
			}
			this.Wq.Backward(this._gradQProj, learningRate, false);
			this.Wk.Backward(new ReadOnlySpan<float>(this._gradKeys, 0, this._dModel), learningRate, false);
			this.Wv.Backward(new ReadOnlySpan<float>(this._gradValues, 0, this._dModel), learningRate, false);
		}

		// Token: 0x060001B8 RID: 440 RVA: 0x00017A14 File Offset: 0x00015C14
		public float[] GetSelfInputGradients()
		{
			float[] inputGrads = this.Wq.InputGrads;
			float[] inputGrads2 = this.Wk.InputGrads;
			float[] inputGrads3 = this.Wv.InputGrads;
			for (int i = 0; i < this._dModel; i++)
			{
				this._gradQProj[i] = inputGrads[i] + inputGrads2[i] + inputGrads3[i];
			}
			return this._gradQProj;
		}

		// Token: 0x060001B9 RID: 441 RVA: 0x00017A70 File Offset: 0x00015C70
		public void ApplyGradients(float learningRate = 0.001f)
		{
			this.Wo.ApplyGradientsSGD(learningRate, 1f);
			this.Wq.ApplyGradientsSGD(learningRate, 1f);
			this.Wk.ApplyGradientsSGD(learningRate, 1f);
			this.Wv.ApplyGradientsSGD(learningRate, 1f);
		}

		// Token: 0x060001BA RID: 442 RVA: 0x00017AC1 File Offset: 0x00015CC1
		public float[] GetQueryGradients()
		{
			return this.Wq.InputGrads;
		}

		// Token: 0x060001BB RID: 443 RVA: 0x00017ACE File Offset: 0x00015CCE
		public float[] GetKeyGradients()
		{
			return this._gradKeys;
		}

		// Token: 0x060001BC RID: 444 RVA: 0x00017AD6 File Offset: 0x00015CD6
		public float[] GetValueGradients()
		{
			return this._gradValues;
		}

		// Token: 0x060001BD RID: 445 RVA: 0x00017ADE File Offset: 0x00015CDE
		public int GetCachedNumCores()
		{
			return this._cachedNumCores;
		}

		// Token: 0x04000453 RID: 1107
		private const int MAX_CORES = 64;

		// Token: 0x04000454 RID: 1108
		private const float GRADIENT_CLIP_THRESHOLD = 1f;

		// Token: 0x04000455 RID: 1109
		private readonly int _dModel;

		// Token: 0x04000456 RID: 1110
		private readonly int _nHead;

		// Token: 0x04000457 RID: 1111
		private readonly int _dK;

		// Token: 0x04000458 RID: 1112
		private readonly int _dV;

		// Token: 0x04000459 RID: 1113
		public readonly LinearLayer Wq;

		// Token: 0x0400045A RID: 1114
		public readonly LinearLayer Wk;

		// Token: 0x0400045B RID: 1115
		public readonly LinearLayer Wv;

		// Token: 0x0400045C RID: 1116
		public readonly LinearLayer Wo;

		// Token: 0x0400045D RID: 1117
		private readonly float[] _qProj;

		// Token: 0x0400045E RID: 1118
		private readonly float[] _kProj;

		// Token: 0x0400045F RID: 1119
		private readonly float[] _vProj;

		// Token: 0x04000460 RID: 1120
		private readonly float[] _attentionScores;

		// Token: 0x04000461 RID: 1121
		private readonly float[] _attentionWeights;

		// Token: 0x04000462 RID: 1122
		private readonly float[] _headOutputs;

		// Token: 0x04000463 RID: 1123
		private readonly float[] _concatHeads;

		// Token: 0x04000464 RID: 1124
		private readonly float[][] _cachedKeys;

		// Token: 0x04000465 RID: 1125
		private readonly float[][] _cachedValues;

		// Token: 0x04000466 RID: 1126
		private readonly float[] _cachedQProj;

		// Token: 0x04000467 RID: 1127
		private readonly float[] _cachedQuery;

		// Token: 0x04000468 RID: 1128
		private int _cachedNumCores;

		// Token: 0x04000469 RID: 1129
		private bool _cached;

		// Token: 0x0400046A RID: 1130
		private readonly float[] _gradQProj;

		// Token: 0x0400046B RID: 1131
		private readonly float[] _gradConcatHeads;

		// Token: 0x0400046C RID: 1132
		private readonly float[] _gradAttentionWeights;

		// Token: 0x0400046D RID: 1133
		private readonly float[] _gradAttentionScores;

		// Token: 0x0400046E RID: 1134
		private readonly float[] _gradHeadOutputs;

		// Token: 0x0400046F RID: 1135
		private readonly float[] _gradKeys;

		// Token: 0x04000470 RID: 1136
		private readonly float[] _gradValues;
	}
}
