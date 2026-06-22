using System;
using System.Runtime.CompilerServices;
using SimdLibrary;

namespace IntlThrdPerfSchd
{
	// Token: 0x0200001E RID: 30
	public class ThreadTransformerEncoder
	{
		// Token: 0x17000039 RID: 57
		// (get) Token: 0x060001C9 RID: 457 RVA: 0x00015F5B File Offset: 0x0001415B
		public MultiHeadAttention SelfAttention
		{
			get
			{
				return this._selfAttention;
			}
		}

		// Token: 0x1700003A RID: 58
		// (get) Token: 0x060001CA RID: 458 RVA: 0x00015F63 File Offset: 0x00014163
		public FeedForwardLayer FeedForward
		{
			get
			{
				return this._feedForward;
			}
		}

		// Token: 0x1700003B RID: 59
		// (get) Token: 0x060001CB RID: 459 RVA: 0x00015F6B File Offset: 0x0001416B
		public LayerNormLayer Norm1
		{
			get
			{
				return this._norm1;
			}
		}

		// Token: 0x1700003C RID: 60
		// (get) Token: 0x060001CC RID: 460 RVA: 0x00015F73 File Offset: 0x00014173
		public LayerNormLayer Norm2
		{
			get
			{
				return this._norm2;
			}
		}

		// Token: 0x060001CD RID: 461 RVA: 0x00015F7C File Offset: 0x0001417C
		public ThreadTransformerEncoder()
		{
			this._selfAttention = new MultiHeadAttention(64, 4);
			this._feedForward = new FeedForwardLayer(64, 128);
			this._norm1 = new LayerNormLayer(64);
			this._norm2 = new LayerNormLayer(64);
			this._selfAttnOutput = new float[64];
			this._norm1Output = new float[64];
			this._ffnOutput = new float[64];
			this._residual1 = new float[64];
			this._residual2 = new float[64];
			this._gradFFN = new float[64];
			this._gradSelfAttn = new float[64];
			this._gradNorm1Input = new float[64];
			this._gradResidual = new float[64];
			this._gradInput = new float[64];
		}

		// Token: 0x060001CE RID: 462 RVA: 0x0001604C File Offset: 0x0001424C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Forward(ReadOnlySpan<float> input, Span<float> output)
		{
			VectorMathNew.Copy(input, this._residual1);
			this._selfAttention.SelfForward(input, this._selfAttnOutput);
			VectorMathNew.Add(input, this._selfAttnOutput, this._norm1Output);
			this._norm1.Forward(this._norm1Output, this._norm1Output);
			VectorMathNew.Copy(this._norm1Output, this._residual2);
			this._feedForward.Forward(this._norm1Output, this._ffnOutput);
			VectorMathNew.Add(this._residual2, this._ffnOutput, output);
			this._norm2.Forward(output, output);
		}

		// Token: 0x060001CF RID: 463 RVA: 0x00016128 File Offset: 0x00014328
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Backward(ReadOnlySpan<float> gradOutput, float learningRate = 0.001f)
		{
			if (VectorMathNew.HasInvalidValues(gradOutput))
			{
				return;
			}
			this._norm2.Backward(gradOutput, learningRate);
			float[] inputGrads = this._norm2.InputGrads;
			VectorMathNew.Copy(inputGrads, this._gradFFN);
			VectorMathNew.Copy(inputGrads, this._gradResidual);
			this._feedForward.Backward(this._gradFFN, learningRate);
			float[] inputGradients = this._feedForward.InputGradients;
			VectorMathNew.Add(this._gradResidual, inputGradients, this._gradResidual);
			this._norm1.Backward(this._gradResidual, learningRate);
			float[] inputGrads2 = this._norm1.InputGrads;
			VectorMathNew.Copy(inputGrads2, this._gradSelfAttn);
			this.ClipGradientIfNeeded(this._gradSelfAttn);
			this._selfAttention.SelfBackward(this._gradSelfAttn, learningRate);
			float[] selfInputGradients = this._selfAttention.GetSelfInputGradients();
			VectorMathNew.Add(inputGrads2, selfInputGradients, this._gradInput);
			this._norm2.ApplyGradients(learningRate);
			this._norm1.ApplyGradients(learningRate);
		}

		// Token: 0x060001D0 RID: 464 RVA: 0x00016264 File Offset: 0x00014464
		public void ApplyGradients(float learningRate = 0.001f)
		{
			if (!this.AreWeightsValid())
			{
				return;
			}
			this._selfAttention.ApplyGradients(learningRate);
			this._feedForward.FC1.ApplyGradientsSGD(learningRate, 1f);
			this._feedForward.FC2.ApplyGradientsSGD(learningRate, 1f);
		}

		// Token: 0x060001D1 RID: 465 RVA: 0x000162B4 File Offset: 0x000144B4
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

		// Token: 0x060001D2 RID: 466 RVA: 0x000162E4 File Offset: 0x000144E4
		private bool AreWeightsValid()
		{
			return this.IsWeightsValid(this._selfAttention.Wq.Weights) && this.IsWeightsValid(this._selfAttention.Wk.Weights) && this.IsWeightsValid(this._selfAttention.Wv.Weights) && this.IsWeightsValid(this._selfAttention.Wo.Weights) && this.IsWeightsValid(this._feedForward.FC1.Weights) && this.IsWeightsValid(this._feedForward.FC2.Weights);
		}

		// Token: 0x060001D3 RID: 467 RVA: 0x0001638E File Offset: 0x0001458E
		private bool IsWeightsValid(float[] weights)
		{
			return !VectorMathNew.HasInvalidValues(weights.AsSpan<float>());
		}

		// Token: 0x1700003D RID: 61
		// (get) Token: 0x060001D4 RID: 468 RVA: 0x000163A3 File Offset: 0x000145A3
		public float[] InputGradients
		{
			get
			{
				return this._gradInput;
			}
		}

		// Token: 0x04000437 RID: 1079
		private const int D_MODEL = 64;

		// Token: 0x04000438 RID: 1080
		private const int D_FF = 128;

		// Token: 0x04000439 RID: 1081
		private const int HEAD_COUNT = 4;

		// Token: 0x0400043A RID: 1082
		private const float GRADIENT_CLIP_THRESHOLD = 1f;

		// Token: 0x0400043B RID: 1083
		private readonly MultiHeadAttention _selfAttention;

		// Token: 0x0400043C RID: 1084
		private readonly FeedForwardLayer _feedForward;

		// Token: 0x0400043D RID: 1085
		private readonly LayerNormLayer _norm1;

		// Token: 0x0400043E RID: 1086
		private readonly LayerNormLayer _norm2;

		// Token: 0x0400043F RID: 1087
		private readonly float[] _selfAttnOutput;

		// Token: 0x04000440 RID: 1088
		private readonly float[] _norm1Output;

		// Token: 0x04000441 RID: 1089
		private readonly float[] _ffnOutput;

		// Token: 0x04000442 RID: 1090
		private readonly float[] _residual1;

		// Token: 0x04000443 RID: 1091
		private readonly float[] _residual2;

		// Token: 0x04000444 RID: 1092
		private readonly float[] _gradFFN;

		// Token: 0x04000445 RID: 1093
		private readonly float[] _gradSelfAttn;

		// Token: 0x04000446 RID: 1094
		private readonly float[] _gradNorm1Input;

		// Token: 0x04000447 RID: 1095
		private readonly float[] _gradResidual;

		// Token: 0x04000448 RID: 1096
		private readonly float[] _gradInput;
	}
}
