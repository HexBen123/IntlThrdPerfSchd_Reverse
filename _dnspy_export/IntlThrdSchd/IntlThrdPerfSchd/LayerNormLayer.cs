using System;
using System.Runtime.CompilerServices;
using SimdLibrary;

namespace IntlThrdPerfSchd
{
	// Token: 0x0200001B RID: 27
	public class LayerNormLayer
	{
		// Token: 0x060001AD RID: 429 RVA: 0x00016AF0 File Offset: 0x00014CF0
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

		// Token: 0x060001AE RID: 430 RVA: 0x00016BB0 File Offset: 0x00014DB0
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Forward(ReadOnlySpan<float> input, Span<float> output)
		{
			input.CopyTo(this._cachedInput);
			ValueTuple<float, float> valueTuple = VectorMathNew.LayerNormForward(input, this.Gamma, this.Beta, output, this._cachedXNorm, 1E-05f);
			float item = valueTuple.Item1;
			float item2 = valueTuple.Item2;
			this._cachedMean[0] = item;
			this._cachedInvStd[0] = item2;
			this._cached = true;
		}

		// Token: 0x060001AF RID: 431 RVA: 0x00016C24 File Offset: 0x00014E24
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe void Backward(ReadOnlySpan<float> gradOutput, float learningRate = 0.001f, bool accumulateGrad = false)
		{
			if (!this._cached)
			{
				return;
			}
			int dim = this.Dim;
			float num = this._cachedMean[0];
			float num2 = this._cachedInvStd[0];
			if (accumulateGrad)
			{
				for (int i = 0; i < dim; i++)
				{
					this.GammaGrads[i] += *gradOutput[i] * this._cachedXNorm[i];
					this.BetaGrads[i] += *gradOutput[i];
				}
			}
			else
			{
				Span<float> span = new Span<float>(this.GammaGrads, 0, dim);
				Span<float> span2 = new Span<float>(this.BetaGrads, 0, dim);
				MathHelper.MultiplyElementwise(gradOutput.Slice(0, dim), new ReadOnlySpan<float>(this._cachedXNorm, 0, dim), span);
				gradOutput.Slice(0, dim).CopyTo(span2);
			}
			VectorMathNew.ComputeLayerNormInputGrad(gradOutput.Slice(0, dim), new ReadOnlySpan<float>(this.Gamma, 0, dim), new ReadOnlySpan<float>(this._cachedXNorm, 0, dim), num2, this.InputGrads);
		}

		// Token: 0x060001B0 RID: 432 RVA: 0x00016D24 File Offset: 0x00014F24
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

		// Token: 0x04000447 RID: 1095
		public readonly float[] Gamma;

		// Token: 0x04000448 RID: 1096
		public readonly float[] Beta;

		// Token: 0x04000449 RID: 1097
		public readonly int Dim;

		// Token: 0x0400044A RID: 1098
		public readonly float[] GammaGrads;

		// Token: 0x0400044B RID: 1099
		public readonly float[] BetaGrads;

		// Token: 0x0400044C RID: 1100
		public readonly float[] InputGrads;

		// Token: 0x0400044D RID: 1101
		private float[] _cachedInput;

		// Token: 0x0400044E RID: 1102
		private float[] _cachedMean;

		// Token: 0x0400044F RID: 1103
		private float[] _cachedInvStd;

		// Token: 0x04000450 RID: 1104
		private float[] _cachedXNorm;

		// Token: 0x04000451 RID: 1105
		private float[] _tempBuf;

		// Token: 0x04000452 RID: 1106
		private bool _cached;
	}
}
