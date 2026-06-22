using System;
using System.Runtime.CompilerServices;
using SimdLibrary;

namespace IntlThrdPerfSchd
{
	// Token: 0x0200001D RID: 29
	public class CoreTransformerEncoder
	{
		// Token: 0x17000034 RID: 52
		// (get) Token: 0x060001BD RID: 445 RVA: 0x00015B09 File Offset: 0x00013D09
		public MultiHeadAttention SelfAttention
		{
			get
			{
				return this._selfAttention;
			}
		}

		// Token: 0x17000035 RID: 53
		// (get) Token: 0x060001BE RID: 446 RVA: 0x00015B11 File Offset: 0x00013D11
		public FeedForwardLayer FeedForward
		{
			get
			{
				return this._feedForward;
			}
		}

		// Token: 0x17000036 RID: 54
		// (get) Token: 0x060001BF RID: 447 RVA: 0x00015B19 File Offset: 0x00013D19
		public LayerNormLayer Norm1
		{
			get
			{
				return this._norm1;
			}
		}

		// Token: 0x17000037 RID: 55
		// (get) Token: 0x060001C0 RID: 448 RVA: 0x00015B21 File Offset: 0x00013D21
		public LayerNormLayer Norm2
		{
			get
			{
				return this._norm2;
			}
		}

		// Token: 0x060001C1 RID: 449 RVA: 0x00015B2C File Offset: 0x00013D2C
		public CoreTransformerEncoder()
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

		// Token: 0x060001C2 RID: 450 RVA: 0x00015BFC File Offset: 0x00013DFC
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

		// Token: 0x060001C3 RID: 451 RVA: 0x00015CD8 File Offset: 0x00013ED8
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

		// Token: 0x060001C4 RID: 452 RVA: 0x00015E14 File Offset: 0x00014014
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

		// Token: 0x060001C5 RID: 453 RVA: 0x00015E64 File Offset: 0x00014064
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

		// Token: 0x060001C6 RID: 454 RVA: 0x00015E94 File Offset: 0x00014094
		private bool AreWeightsValid()
		{
			return this.IsWeightsValid(this._selfAttention.Wq.Weights) && this.IsWeightsValid(this._selfAttention.Wk.Weights) && this.IsWeightsValid(this._selfAttention.Wv.Weights) && this.IsWeightsValid(this._selfAttention.Wo.Weights) && this.IsWeightsValid(this._feedForward.FC1.Weights) && this.IsWeightsValid(this._feedForward.FC2.Weights);
		}

		// Token: 0x060001C7 RID: 455 RVA: 0x00015F3E File Offset: 0x0001413E
		private bool IsWeightsValid(float[] weights)
		{
			return !VectorMathNew.HasInvalidValues(weights.AsSpan<float>());
		}

		// Token: 0x17000038 RID: 56
		// (get) Token: 0x060001C8 RID: 456 RVA: 0x00015F53 File Offset: 0x00014153
		public float[] InputGradients
		{
			get
			{
				return this._gradInput;
			}
		}

		// Token: 0x04000425 RID: 1061
		private const int D_MODEL = 64;

		// Token: 0x04000426 RID: 1062
		private const int D_FF = 128;

		// Token: 0x04000427 RID: 1063
		private const int HEAD_COUNT = 4;

		// Token: 0x04000428 RID: 1064
		private const float GRADIENT_CLIP_THRESHOLD = 1f;

		// Token: 0x04000429 RID: 1065
		private readonly MultiHeadAttention _selfAttention;

		// Token: 0x0400042A RID: 1066
		private readonly FeedForwardLayer _feedForward;

		// Token: 0x0400042B RID: 1067
		private readonly LayerNormLayer _norm1;

		// Token: 0x0400042C RID: 1068
		private readonly LayerNormLayer _norm2;

		// Token: 0x0400042D RID: 1069
		private readonly float[] _selfAttnOutput;

		// Token: 0x0400042E RID: 1070
		private readonly float[] _norm1Output;

		// Token: 0x0400042F RID: 1071
		private readonly float[] _ffnOutput;

		// Token: 0x04000430 RID: 1072
		private readonly float[] _residual1;

		// Token: 0x04000431 RID: 1073
		private readonly float[] _residual2;

		// Token: 0x04000432 RID: 1074
		private readonly float[] _gradFFN;

		// Token: 0x04000433 RID: 1075
		private readonly float[] _gradSelfAttn;

		// Token: 0x04000434 RID: 1076
		private readonly float[] _gradNorm1Input;

		// Token: 0x04000435 RID: 1077
		private readonly float[] _gradResidual;

		// Token: 0x04000436 RID: 1078
		private readonly float[] _gradInput;
	}
}
