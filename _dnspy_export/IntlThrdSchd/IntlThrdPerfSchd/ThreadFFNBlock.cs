using System;
using System.Runtime.CompilerServices;
using SimdLibrary;

namespace IntlThrdPerfSchd
{
	// Token: 0x02000020 RID: 32
	public class ThreadFFNBlock
	{
		// Token: 0x17000039 RID: 57
		// (get) Token: 0x060001DC RID: 476 RVA: 0x00019037 File Offset: 0x00017237
		public FeedForwardLayer FeedForward
		{
			get
			{
				return this._feedForward;
			}
		}

		// Token: 0x1700003A RID: 58
		// (get) Token: 0x060001DD RID: 477 RVA: 0x0001903F File Offset: 0x0001723F
		public LayerNormLayer Norm1
		{
			get
			{
				return this._norm1;
			}
		}

		// Token: 0x1700003B RID: 59
		// (get) Token: 0x060001DE RID: 478 RVA: 0x00019047 File Offset: 0x00017247
		public LayerNormLayer Norm2
		{
			get
			{
				return this._norm2;
			}
		}

		// Token: 0x060001DF RID: 479 RVA: 0x00019050 File Offset: 0x00017250
		public ThreadFFNBlock()
		{
			this._feedForward = new FeedForwardLayer(64, 256);
			this._norm1 = new LayerNormLayer(64);
			this._norm2 = new LayerNormLayer(64);
			this._norm1Output = new float[64];
			this._ffnOutput = new float[64];
			this._gradFFN = new float[64];
			this._gradResidual = new float[64];
			this._gradInput = new float[64];
		}

		// Token: 0x060001E0 RID: 480 RVA: 0x000190D0 File Offset: 0x000172D0
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Forward(ReadOnlySpan<float> input, Span<float> output)
		{
			this._norm1.Forward(input, this._norm1Output);
			this._feedForward.Forward(this._norm1Output, this._ffnOutput);
			VectorMathNew.Add(input, this._ffnOutput, output);
			this._norm2.Forward(output, output);
		}

		// Token: 0x060001E1 RID: 481 RVA: 0x0001913C File Offset: 0x0001733C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Backward(ReadOnlySpan<float> gradOutput, float learningRate = 0.001f)
		{
			if (VectorMathNew.HasInvalidValues(gradOutput))
			{
				return;
			}
			this._norm2.Backward(gradOutput, learningRate, false);
			float[] inputGrads = this._norm2.InputGrads;
			VectorMathNew.Copy(inputGrads, this._gradFFN);
			VectorMathNew.Copy(inputGrads, this._gradResidual);
			this._feedForward.Backward(this._gradFFN, learningRate);
			float[] inputGradients = this._feedForward.InputGradients;
			this._norm1.Backward(inputGradients, learningRate, false);
			float[] inputGrads2 = this._norm1.InputGrads;
			VectorMathNew.Add(this._gradResidual, inputGrads2, this._gradInput);
			this.ClipGradientIfNeeded(this._gradInput);
			this._norm2.ApplyGradients(learningRate);
			this._norm1.ApplyGradients(learningRate);
		}

		// Token: 0x060001E2 RID: 482 RVA: 0x00019222 File Offset: 0x00017422
		public void ApplyGradients(float learningRate = 0.001f)
		{
			if (!this.AreWeightsValid())
			{
				return;
			}
			this._feedForward.FC1.ApplyGradientsSGD(learningRate, 1f);
			this._feedForward.FC2.ApplyGradientsSGD(learningRate, 1f);
		}

		// Token: 0x060001E3 RID: 483 RVA: 0x0001925C File Offset: 0x0001745C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void ClipGradientIfNeeded(Span<float> grad)
		{
			float num = VectorMathNew.EuclideanNorm(grad);
			if (num > 1f)
			{
				float num2 = 1f / num;
				VectorMathNew.MultiplyScalarInPlace(grad, num2);
			}
		}

		// Token: 0x060001E4 RID: 484 RVA: 0x0001928C File Offset: 0x0001748C
		private bool AreWeightsValid()
		{
			return this.IsWeightsValid(this._feedForward.FC1.Weights) && this.IsWeightsValid(this._feedForward.FC2.Weights);
		}

		// Token: 0x060001E5 RID: 485 RVA: 0x000192C3 File Offset: 0x000174C3
		private bool IsWeightsValid(float[] weights)
		{
			return !VectorMathNew.HasInvalidValues(weights.AsSpan<float>());
		}

		// Token: 0x1700003C RID: 60
		// (get) Token: 0x060001E6 RID: 486 RVA: 0x000192D8 File Offset: 0x000174D8
		public float[] InputGradients
		{
			get
			{
				return this._gradInput;
			}
		}

		// Token: 0x040004B0 RID: 1200
		private const int D_MODEL = 64;

		// Token: 0x040004B1 RID: 1201
		private const int D_FF = 256;

		// Token: 0x040004B2 RID: 1202
		private const float GRADIENT_CLIP_THRESHOLD = 1f;

		// Token: 0x040004B3 RID: 1203
		private readonly FeedForwardLayer _feedForward;

		// Token: 0x040004B4 RID: 1204
		private readonly LayerNormLayer _norm1;

		// Token: 0x040004B5 RID: 1205
		private readonly LayerNormLayer _norm2;

		// Token: 0x040004B6 RID: 1206
		private readonly float[] _norm1Output;

		// Token: 0x040004B7 RID: 1207
		private readonly float[] _ffnOutput;

		// Token: 0x040004B8 RID: 1208
		private readonly float[] _gradFFN;

		// Token: 0x040004B9 RID: 1209
		private readonly float[] _gradResidual;

		// Token: 0x040004BA RID: 1210
		private readonly float[] _gradInput;
	}
}
