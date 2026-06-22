using System;
using System.Runtime.CompilerServices;
using SimdLibrary;

namespace IntlThrdPerfSchd
{
	// Token: 0x02000018 RID: 24
	public class LinearLayer
	{
		// Token: 0x06000198 RID: 408 RVA: 0x00014184 File Offset: 0x00012384
		public LinearLayer(int inDim, int outDim)
		{
			this.InDim = inDim;
			this.OutDim = outDim;
			this.Weights = new float[outDim * inDim];
			this.Bias = new float[outDim];
			this.WeightGrads = new float[outDim * inDim];
			this.BiasGrads = new float[outDim];
			this.InputGrads = new float[inDim];
			this.WeightMom = new float[outDim * inDim];
			this.BiasMom = new float[outDim];
			this._cachedInput = new float[inDim];
			this._tempInputGrad = new float[inDim];
			this._inputCached = false;
			MathHelper.InitWeights(this.Weights, inDim, this.OutDim);
			MathHelper.Clear(this.Bias.AsSpan(0, this.OutDim));
		}

		// Token: 0x06000199 RID: 409 RVA: 0x00014248 File Offset: 0x00012448
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Forward(ReadOnlySpan<float> input, Span<float> output)
		{
			input.CopyTo(this._cachedInput);
			this._inputCached = true;
			MathHelper.Linear(input, this.Weights, this.Bias, output, this.InDim, this.OutDim);
		}

		// Token: 0x0600019A RID: 410 RVA: 0x00014298 File Offset: 0x00012498
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Backward(ReadOnlySpan<float> gradOutput, float learningRate = 0.001f, bool accumulateGrad = false)
		{
			if (!this._inputCached)
			{
				return;
			}
			MathHelper.ComputeWeightGrad(gradOutput, this._cachedInput, this.WeightGrads, this.OutDim, this.InDim, accumulateGrad);
			if (accumulateGrad)
			{
				VectorMathNew.Add(this.BiasGrads.AsSpan(0, this.OutDim), gradOutput, this.BiasGrads.AsSpan(0, this.OutDim));
			}
			else
			{
				gradOutput.CopyTo(this.BiasGrads.AsSpan(0, this.OutDim));
			}
			if (accumulateGrad)
			{
				MathHelper.ComputeInputGrad(gradOutput, this.Weights, this._tempInputGrad, this.OutDim, this.InDim);
				VectorMathNew.Add(this.InputGrads.AsSpan(0, this.InDim), this._tempInputGrad.AsSpan(0, this.InDim), this.InputGrads.AsSpan(0, this.InDim));
				return;
			}
			MathHelper.ComputeInputGrad(gradOutput, this.Weights, this.InputGrads, this.OutDim, this.InDim);
		}

		// Token: 0x0600019B RID: 411 RVA: 0x000143C0 File Offset: 0x000125C0
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ApplyGradients(float learningRate = 0.001f, float beta1 = 0.9f, float beta2 = 0.999f, float epsilon = 1E-08f, int t = 1)
		{
			int num = this.OutDim * this.InDim;
			for (int i = 0; i < num; i++)
			{
				float num2 = this.WeightGrads[i];
				this.WeightMom[i] = beta1 * this.WeightMom[i] + (1f - beta1) * num2;
				float num3 = beta2 * 0f + (1f - beta2) * num2 * num2;
				float num4 = this.WeightMom[i] / (1f - (float)Math.Pow((double)beta1, (double)t));
				float num5 = num3 / (1f - (float)Math.Pow((double)beta2, (double)t));
				this.Weights[i] -= learningRate * num4 / ((float)Math.Sqrt((double)num5) + epsilon);
			}
			for (int j = 0; j < this.OutDim; j++)
			{
				float num6 = this.BiasGrads[j];
				this.BiasMom[j] = beta1 * this.BiasMom[j] + (1f - beta1) * num6;
				float num7 = beta2 * 0f + (1f - beta2) * num6 * num6;
				float num8 = this.BiasMom[j] / (1f - (float)Math.Pow((double)beta1, (double)t));
				float num9 = num7 / (1f - (float)Math.Pow((double)beta2, (double)t));
				this.Bias[j] -= learningRate * num8 / ((float)Math.Sqrt((double)num9) + epsilon);
			}
			MathHelper.Clear(this.WeightGrads.AsSpan(0, num));
			MathHelper.Clear(this.BiasGrads.AsSpan(0, this.OutDim));
		}

		// Token: 0x0600019C RID: 412 RVA: 0x0001454C File Offset: 0x0001274C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ApplyGradientsSGD(float learningRate = 0.001f, float maxGradNorm = 1f)
		{
			int num = this.OutDim * this.InDim;
			Span<float> span = this.WeightGrads.AsSpan(0, num);
			float num2 = VectorMathNew.EuclideanNorm(span);
			if (num2 > maxGradNorm)
			{
				float num3 = maxGradNorm / num2;
				VectorMathNew.MultiplyScalarInPlace(span, num3);
			}
			Span<float> span2 = this.BiasGrads.AsSpan(0, this.OutDim);
			float num4 = VectorMathNew.EuclideanNorm(span2);
			if (num4 > maxGradNorm)
			{
				float num5 = maxGradNorm / num4;
				VectorMathNew.MultiplyScalarInPlace(span2, num5);
			}
			Span<float> span3 = this.Weights.AsSpan(0, num);
			VectorMathNew.MultiplyScalarInPlace(span, -learningRate);
			VectorMathNew.Add(span3, span, span3);
			VectorMathNew.MultiplyScalarInPlace(span2, -learningRate);
			VectorMathNew.Add(this.Bias.AsSpan(0, this.OutDim), span2, this.Bias.AsSpan(0, this.OutDim));
			MathHelper.Clear(this.WeightGrads.AsSpan(0, num));
			MathHelper.Clear(this.BiasGrads.AsSpan(0, this.OutDim));
		}

		// Token: 0x040003DA RID: 986
		public readonly float[] Weights;

		// Token: 0x040003DB RID: 987
		public readonly float[] Bias;

		// Token: 0x040003DC RID: 988
		public readonly int InDim;

		// Token: 0x040003DD RID: 989
		public readonly int OutDim;

		// Token: 0x040003DE RID: 990
		public readonly float[] WeightGrads;

		// Token: 0x040003DF RID: 991
		public readonly float[] BiasGrads;

		// Token: 0x040003E0 RID: 992
		public readonly float[] InputGrads;

		// Token: 0x040003E1 RID: 993
		public readonly float[] WeightMom;

		// Token: 0x040003E2 RID: 994
		public readonly float[] BiasMom;

		// Token: 0x040003E3 RID: 995
		private float[] _cachedInput;

		// Token: 0x040003E4 RID: 996
		private bool _inputCached;

		// Token: 0x040003E5 RID: 997
		private readonly float[] _tempInputGrad;
	}
}
