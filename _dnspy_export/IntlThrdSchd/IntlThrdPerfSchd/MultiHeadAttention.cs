using System;
using System.Runtime.CompilerServices;
using SimdLibrary;

namespace IntlThrdPerfSchd
{
	// Token: 0x0200001A RID: 26
	public class MultiHeadAttention
	{
		// Token: 0x1700002B RID: 43
		// (get) Token: 0x060001A1 RID: 417 RVA: 0x00014A23 File Offset: 0x00012C23
		public int NumHeads
		{
			get
			{
				return this._nHead;
			}
		}

		// Token: 0x060001A2 RID: 418 RVA: 0x00014A2C File Offset: 0x00012C2C
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

		// Token: 0x060001A3 RID: 419 RVA: 0x00014A80 File Offset: 0x00012C80
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

		// Token: 0x060001A4 RID: 420 RVA: 0x00014C18 File Offset: 0x00012E18
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void CrossAttention(ReadOnlySpan<float> query, float[][] keys, float[][] values, Span<float> output, int numCores, Span<float> attentionWeights = default(Span<float>))
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
					float num2 = 0f;
					for (int l = 0; l < this._dK; l++)
					{
						num2 += this._qProj[num + l] * keys[k][num + l];
					}
					this._attentionScores[j * 64 + k] = num2 / (float)Math.Sqrt((double)this._dK);
				}
				int num3 = j * 64;
				MathHelper.Softmax(new ReadOnlySpan<float>(this._attentionScores, num3, numCores), new Span<float>(this._attentionWeights, num3, numCores));
				for (int m = 0; m < this._dV; m++)
				{
					float num4 = 0f;
					for (int n = 0; n < numCores; n++)
					{
						num4 += this._attentionWeights[num3 + n] * values[n][j * this._dV + m];
					}
					this._headOutputs[j * this._dV + m] = num4;
				}
			}
			for (int num5 = 0; num5 < this._nHead; num5++)
			{
				Buffer.BlockCopy(this._headOutputs, num5 * this._dV * 4, this._concatHeads, num5 * this._dV * 4, this._dV * 4);
			}
			this.Wo.Forward(this._concatHeads, output);
			if (!attentionWeights.IsEmpty)
			{
				Span<float> span = attentionWeights.Slice(0, numCores);
				VectorMathNew.Zero(span);
				for (int num6 = 0; num6 < this._nHead; num6++)
				{
					int num7 = num6 * 64;
					Span<float> span2 = this._attentionWeights.AsSpan(num7, numCores);
					VectorMathNew.Add(span, span2, span);
				}
				VectorMathNew.MultiplyScalarInPlace(span, 1f / (float)this._nHead);
			}
			this._cached = true;
		}

		// Token: 0x060001A5 RID: 421 RVA: 0x00014E88 File Offset: 0x00013088
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

		// Token: 0x060001A6 RID: 422 RVA: 0x00015198 File Offset: 0x00013398
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

		// Token: 0x060001A7 RID: 423 RVA: 0x00015378 File Offset: 0x00013578
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

		// Token: 0x060001A8 RID: 424 RVA: 0x00015610 File Offset: 0x00013810
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

		// Token: 0x060001A9 RID: 425 RVA: 0x0001566C File Offset: 0x0001386C
		public void ApplyGradients(float learningRate = 0.001f)
		{
			this.Wo.ApplyGradientsSGD(learningRate, 1f);
			this.Wq.ApplyGradientsSGD(learningRate, 1f);
			this.Wk.ApplyGradientsSGD(learningRate, 1f);
			this.Wv.ApplyGradientsSGD(learningRate, 1f);
		}

		// Token: 0x060001AA RID: 426 RVA: 0x000156BD File Offset: 0x000138BD
		public float[] GetQueryGradients()
		{
			return this.Wq.InputGrads;
		}

		// Token: 0x060001AB RID: 427 RVA: 0x000156CA File Offset: 0x000138CA
		public float[] GetKeyGradients()
		{
			return this._gradKeys;
		}

		// Token: 0x060001AC RID: 428 RVA: 0x000156D2 File Offset: 0x000138D2
		public float[] GetValueGradients()
		{
			return this._gradValues;
		}

		// Token: 0x060001AD RID: 429 RVA: 0x000156DA File Offset: 0x000138DA
		public int GetCachedNumCores()
		{
			return this._cachedNumCores;
		}

		// Token: 0x040003F2 RID: 1010
		private const int MAX_CORES = 64;

		// Token: 0x040003F3 RID: 1011
		private const float GRADIENT_CLIP_THRESHOLD = 1f;

		// Token: 0x040003F4 RID: 1012
		private readonly int _dModel;

		// Token: 0x040003F5 RID: 1013
		private readonly int _nHead;

		// Token: 0x040003F6 RID: 1014
		private readonly int _dK;

		// Token: 0x040003F7 RID: 1015
		private readonly int _dV;

		// Token: 0x040003F8 RID: 1016
		public readonly LinearLayer Wq;

		// Token: 0x040003F9 RID: 1017
		public readonly LinearLayer Wk;

		// Token: 0x040003FA RID: 1018
		public readonly LinearLayer Wv;

		// Token: 0x040003FB RID: 1019
		public readonly LinearLayer Wo;

		// Token: 0x040003FC RID: 1020
		private readonly float[] _qProj;

		// Token: 0x040003FD RID: 1021
		private readonly float[] _kProj;

		// Token: 0x040003FE RID: 1022
		private readonly float[] _vProj;

		// Token: 0x040003FF RID: 1023
		private readonly float[] _attentionScores;

		// Token: 0x04000400 RID: 1024
		private readonly float[] _attentionWeights;

		// Token: 0x04000401 RID: 1025
		private readonly float[] _headOutputs;

		// Token: 0x04000402 RID: 1026
		private readonly float[] _concatHeads;

		// Token: 0x04000403 RID: 1027
		private readonly float[][] _cachedKeys;

		// Token: 0x04000404 RID: 1028
		private readonly float[][] _cachedValues;

		// Token: 0x04000405 RID: 1029
		private readonly float[] _cachedQProj;

		// Token: 0x04000406 RID: 1030
		private readonly float[] _cachedQuery;

		// Token: 0x04000407 RID: 1031
		private int _cachedNumCores;

		// Token: 0x04000408 RID: 1032
		private bool _cached;

		// Token: 0x04000409 RID: 1033
		private readonly float[] _gradQProj;

		// Token: 0x0400040A RID: 1034
		private readonly float[] _gradConcatHeads;

		// Token: 0x0400040B RID: 1035
		private readonly float[] _gradAttentionWeights;

		// Token: 0x0400040C RID: 1036
		private readonly float[] _gradAttentionScores;

		// Token: 0x0400040D RID: 1037
		private readonly float[] _gradHeadOutputs;

		// Token: 0x0400040E RID: 1038
		private readonly float[] _gradKeys;

		// Token: 0x0400040F RID: 1039
		private readonly float[] _gradValues;
	}
}
