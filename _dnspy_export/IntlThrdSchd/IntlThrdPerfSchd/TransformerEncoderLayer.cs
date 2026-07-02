using System;
using System.Runtime.CompilerServices;
using SimdLibrary;

namespace IntlThrdPerfSchd
{
	// Token: 0x0200001E RID: 30
	public class TransformerEncoderLayer
	{
		// Token: 0x1700002F RID: 47
		// (get) Token: 0x060001C4 RID: 452 RVA: 0x00017C3E File Offset: 0x00015E3E
		public MultiHeadAttention SelfAttention
		{
			get
			{
				return this._selfAttention;
			}
		}

		// Token: 0x17000030 RID: 48
		// (get) Token: 0x060001C5 RID: 453 RVA: 0x00017C46 File Offset: 0x00015E46
		public FeedForwardLayer FeedForward
		{
			get
			{
				return this._feedForward;
			}
		}

		// Token: 0x17000031 RID: 49
		// (get) Token: 0x060001C6 RID: 454 RVA: 0x00017C4E File Offset: 0x00015E4E
		public LayerNormLayer Norm1
		{
			get
			{
				return this._norm1;
			}
		}

		// Token: 0x17000032 RID: 50
		// (get) Token: 0x060001C7 RID: 455 RVA: 0x00017C56 File Offset: 0x00015E56
		public LayerNormLayer Norm2
		{
			get
			{
				return this._norm2;
			}
		}

		// Token: 0x060001C8 RID: 456 RVA: 0x00017C60 File Offset: 0x00015E60
		public TransformerEncoderLayer(int dModel, int nHead, int dFF = 256)
		{
			this._dModel = dModel;
			this._selfAttention = new MultiHeadAttention(dModel, nHead);
			this._feedForward = new FeedForwardLayer(dModel, dFF);
			this._norm1 = new LayerNormLayer(dModel);
			this._norm2 = new LayerNormLayer(dModel);
			this._tempBuffer = new float[dModel];
			this._normOutput = new float[dModel];
			this._ffnOutput = new float[dModel];
			this._residualInput = new float[dModel];
			this._gradTemp = new float[dModel];
			this._gradNorm = new float[dModel];
			this._gradFFN = new float[dModel];
			this._gradResidual = new float[dModel];
		}

		// Token: 0x060001C9 RID: 457 RVA: 0x00017D0C File Offset: 0x00015F0C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Forward(ReadOnlySpan<float> input, Span<float> output)
		{
			input.CopyTo(this._residualInput);
			this._norm1.Forward(input, this._normOutput);
			this._feedForward.Forward(this._normOutput, this._ffnOutput);
			VectorMathNew.Add(this._ffnOutput, input, output);
			this._norm2.Forward(output, this._tempBuffer);
			this._tempBuffer.AsSpan<float>().CopyTo(output);
		}

		// Token: 0x060001CA RID: 458 RVA: 0x00017DA8 File Offset: 0x00015FA8
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Backward(ReadOnlySpan<float> gradOutput, float learningRate = 0.001f)
		{
			this._norm2.Backward(gradOutput, learningRate, false);
			float[] inputGrads = this._norm2.InputGrads;
			inputGrads.AsSpan<float>().CopyTo(this._gradFFN.AsSpan<float>());
			inputGrads.AsSpan<float>().CopyTo(this._gradResidual.AsSpan<float>());
			this._feedForward.Backward(this._gradFFN, learningRate);
			float[] inputGradients = this._feedForward.InputGradients;
			this._norm1.Backward(inputGradients, learningRate, false);
			float[] inputGrads2 = this._norm1.InputGrads;
			VectorMathNew.Add(this._gradResidual.AsSpan(0, this._dModel), inputGrads2, this._gradResidual.AsSpan(0, this._dModel));
			this._norm2.ApplyGradients(learningRate);
			this._norm1.ApplyGradients(learningRate);
		}

		// Token: 0x060001CB RID: 459 RVA: 0x00017E90 File Offset: 0x00016090
		public void ApplyGradients(float learningRate = 0.001f)
		{
			this._feedForward.FC1.ApplyGradientsSGD(learningRate, 1f);
			this._feedForward.FC2.ApplyGradientsSGD(learningRate, 1f);
			this._norm1.ApplyGradients(learningRate);
			this._norm2.ApplyGradients(learningRate);
		}

		// Token: 0x17000033 RID: 51
		// (get) Token: 0x060001CC RID: 460 RVA: 0x00017EE1 File Offset: 0x000160E1
		public float[] InputGradients
		{
			get
			{
				return this._gradResidual;
			}
		}

		// Token: 0x04000479 RID: 1145
		private readonly MultiHeadAttention _selfAttention;

		// Token: 0x0400047A RID: 1146
		private readonly FeedForwardLayer _feedForward;

		// Token: 0x0400047B RID: 1147
		private readonly LayerNormLayer _norm1;

		// Token: 0x0400047C RID: 1148
		private readonly LayerNormLayer _norm2;

		// Token: 0x0400047D RID: 1149
		private readonly int _dModel;

		// Token: 0x0400047E RID: 1150
		private readonly float[] _tempBuffer;

		// Token: 0x0400047F RID: 1151
		private readonly float[] _normOutput;

		// Token: 0x04000480 RID: 1152
		private readonly float[] _ffnOutput;

		// Token: 0x04000481 RID: 1153
		private readonly float[] _residualInput;

		// Token: 0x04000482 RID: 1154
		private readonly float[] _gradTemp;

		// Token: 0x04000483 RID: 1155
		private readonly float[] _gradNorm;

		// Token: 0x04000484 RID: 1156
		private readonly float[] _gradFFN;

		// Token: 0x04000485 RID: 1157
		private readonly float[] _gradResidual;
	}
}
