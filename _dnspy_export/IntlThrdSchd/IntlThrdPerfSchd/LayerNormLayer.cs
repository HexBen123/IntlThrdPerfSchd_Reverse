using System;
using System.Runtime.CompilerServices;
using SimdLibrary;

namespace IntlThrdPerfSchd
{
	// Token: 0x02000019 RID: 25
	public class LayerNormLayer
	{
		// Token: 0x0600019D RID: 413 RVA: 0x00014658 File Offset: 0x00012858
		public LayerNormLayer(int dim)
		{
			this.Dim = dim;
			this.Gamma = new float[dim];
			this.Beta = new float[dim];
			this.GammaGrads = new float[dim];
			this.BetaGrads = new float[dim];
			this.InputGrads = new float[dim];
			this._cachedInput = new float[dim];
			this._cachedMean = new float[1];
			this._cachedInvStd = new float[1];
			this._cachedXNorm = new float[dim];
			this._tempBuf = new float[dim];
			this._cached = false;
			for (int i = 0; i < dim; i++)
			{
				this.Gamma[i] = 1f;
				this.Beta[i] = 0f;
			}
		}

		// Token: 0x0600019E RID: 414 RVA: 0x00014718 File Offset: 0x00012918
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe void Forward(ReadOnlySpan<float> input, Span<float> output)
		{
			int length = input.Length;
			float num = 0f;
			for (int i = 0; i < length; i++)
			{
				num += *input[i];
			}
			num /= (float)length;
			this._cachedMean[0] = num;
			float num2 = 0f;
			for (int j = 0; j < length; j++)
			{
				float num3 = *input[j] - num;
				num2 += num3 * num3;
			}
			num2 /= (float)length;
			float num4 = 1f / (float)Math.Sqrt((double)(num2 + 1E-05f));
			this._cachedInvStd[0] = num4;
			input.CopyTo(this._cachedInput);
			for (int k = 0; k < length; k++)
			{
				this._cachedXNorm[k] = (*input[k] - num) * num4;
				*output[k] = this._cachedXNorm[k] * this.Gamma[k] + this.Beta[k];
			}
			this._cached = true;
		}

		// Token: 0x0600019F RID: 415 RVA: 0x00014814 File Offset: 0x00012A14
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Backward(ReadOnlySpan<float> gradOutput, float learningRate = 0.001f)
		{
			if (!this._cached)
			{
				return;
			}
			int dim = this.Dim;
			float num = this._cachedMean[0];
			float num2 = this._cachedInvStd[0];
			Span<float> span = new Span<float>(this.GammaGrads, 0, dim);
			Span<float> span2 = new Span<float>(this.BetaGrads, 0, dim);
			MathHelper.MultiplyElementwise(gradOutput.Slice(0, dim), new ReadOnlySpan<float>(this._cachedXNorm, 0, dim), span);
			gradOutput.Slice(0, dim).CopyTo(span2);
			MathHelper.MultiplyElementwise(gradOutput.Slice(0, dim), new ReadOnlySpan<float>(this.Gamma, 0, dim), this._tempBuf);
			MathHelper.Sum(this._tempBuf.AsSpan(0, dim));
			MathHelper.MultiplyElementwise(gradOutput.Slice(0, dim), new ReadOnlySpan<float>(this.Gamma, 0, dim), this._tempBuf);
			ReadOnlySpan<float> readOnlySpan = new ReadOnlySpan<float>(this._cachedXNorm, 0, dim);
			VectorMathNew.ComputeLayerNormInputGrad(gradOutput.Slice(0, dim), new ReadOnlySpan<float>(this.Gamma, 0, dim), readOnlySpan, num2, this.InputGrads);
		}

		// Token: 0x060001A0 RID: 416 RVA: 0x0001492C File Offset: 0x00012B2C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ApplyGradients(float learningRate = 0.001f)
		{
			VectorMathNew.MultiplyScalarInPlace(this.GammaGrads.AsSpan(0, this.Dim), -learningRate);
			VectorMathNew.Add(this.Gamma.AsSpan(0, this.Dim), this.GammaGrads.AsSpan(0, this.Dim), this.Gamma.AsSpan(0, this.Dim));
			VectorMathNew.MultiplyScalarInPlace(this.BetaGrads.AsSpan(0, this.Dim), -learningRate);
			VectorMathNew.Add(this.Beta.AsSpan(0, this.Dim), this.BetaGrads.AsSpan(0, this.Dim), this.Beta.AsSpan(0, this.Dim));
			MathHelper.Clear(this.GammaGrads.AsSpan(0, this.Dim));
			MathHelper.Clear(this.BetaGrads.AsSpan(0, this.Dim));
		}

		// Token: 0x040003E6 RID: 998
		public readonly float[] Gamma;

		// Token: 0x040003E7 RID: 999
		public readonly float[] Beta;

		// Token: 0x040003E8 RID: 1000
		public readonly int Dim;

		// Token: 0x040003E9 RID: 1001
		public readonly float[] GammaGrads;

		// Token: 0x040003EA RID: 1002
		public readonly float[] BetaGrads;

		// Token: 0x040003EB RID: 1003
		public readonly float[] InputGrads;

		// Token: 0x040003EC RID: 1004
		private float[] _cachedInput;

		// Token: 0x040003ED RID: 1005
		private float[] _cachedMean;

		// Token: 0x040003EE RID: 1006
		private float[] _cachedInvStd;

		// Token: 0x040003EF RID: 1007
		private float[] _cachedXNorm;

		// Token: 0x040003F0 RID: 1008
		private float[] _tempBuf;

		// Token: 0x040003F1 RID: 1009
		private bool _cached;
	}
}
