using System;
using System.Runtime.CompilerServices;
using SimdLibrary;

namespace IntlThrdPerfSchd
{
	// Token: 0x0200001C RID: 28
	public class TransformerEncoderLayer
	{
		// Token: 0x1700002F RID: 47
		// (get) Token: 0x060001B4 RID: 436 RVA: 0x0001584A File Offset: 0x00013A4A
		public MultiHeadAttention SelfAttention
		{
			get
			{
				return this._selfAttention;
			}
		}

		// Token: 0x17000030 RID: 48
		// (get) Token: 0x060001B5 RID: 437 RVA: 0x00015852 File Offset: 0x00013A52
		public FeedForwardLayer FeedForward
		{
			get
			{
				return this._feedForward;
			}
		}

		// Token: 0x17000031 RID: 49
		// (get) Token: 0x060001B6 RID: 438 RVA: 0x0001585A File Offset: 0x00013A5A
		public LayerNormLayer Norm1
		{
			get
			{
				return this._norm1;
			}
		}

		// Token: 0x17000032 RID: 50
		// (get) Token: 0x060001B7 RID: 439 RVA: 0x00015862 File Offset: 0x00013A62
		public LayerNormLayer Norm2
		{
			get
			{
				return this._norm2;
			}
		}

		// Token: 0x060001B8 RID: 440 RVA: 0x0001586C File Offset: 0x00013A6C
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

		// Token: 0x060001B9 RID: 441 RVA: 0x00015918 File Offset: 0x00013B18
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe void Forward(ReadOnlySpan<float> input, Span<float> output)
		{
			input.CopyTo(this._residualInput);
			this._norm1.Forward(input, this._normOutput);
			this._feedForward.Forward(this._normOutput, this._ffnOutput);
			for (int i = 0; i < this._dModel; i++)
			{
				*output[i] = this._ffnOutput[i] + *input[i];
			}
			this._norm2.Forward(output, this._tempBuffer);
			this._tempBuffer.AsSpan<float>().CopyTo(output);
		}

		// Token: 0x060001BA RID: 442 RVA: 0x000159CC File Offset: 0x00013BCC
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Backward(ReadOnlySpan<float> gradOutput, float learningRate = 0.001f)
		{
			this._norm2.Backward(gradOutput, learningRate);
			float[] inputGrads = this._norm2.InputGrads;
			inputGrads.AsSpan<float>().CopyTo(this._gradFFN.AsSpan<float>());
			inputGrads.AsSpan<float>().CopyTo(this._gradResidual.AsSpan<float>());
			this._feedForward.Backward(this._gradFFN, learningRate);
			float[] inputGradients = this._feedForward.InputGradients;
			this._norm1.Backward(inputGradients, learningRate);
			float[] inputGrads2 = this._norm1.InputGrads;
			VectorMathNew.Add(this._gradResidual.AsSpan(0, this._dModel), inputGrads2, this._gradResidual.AsSpan(0, this._dModel));
			this._norm2.ApplyGradients(learningRate);
			this._norm1.ApplyGradients(learningRate);
		}

		// Token: 0x060001BB RID: 443 RVA: 0x00015AB0 File Offset: 0x00013CB0
		public void ApplyGradients(float learningRate = 0.001f)
		{
			this._feedForward.FC1.ApplyGradientsSGD(learningRate, 1f);
			this._feedForward.FC2.ApplyGradientsSGD(learningRate, 1f);
			this._norm1.ApplyGradients(learningRate);
			this._norm2.ApplyGradients(learningRate);
		}

		// Token: 0x17000033 RID: 51
		// (get) Token: 0x060001BC RID: 444 RVA: 0x00015B01 File Offset: 0x00013D01
		public float[] InputGradients
		{
			get
			{
				return this._gradResidual;
			}
		}

		// Token: 0x04000418 RID: 1048
		private readonly MultiHeadAttention _selfAttention;

		// Token: 0x04000419 RID: 1049
		private readonly FeedForwardLayer _feedForward;

		// Token: 0x0400041A RID: 1050
		private readonly LayerNormLayer _norm1;

		// Token: 0x0400041B RID: 1051
		private readonly LayerNormLayer _norm2;

		// Token: 0x0400041C RID: 1052
		private readonly int _dModel;

		// Token: 0x0400041D RID: 1053
		private readonly float[] _tempBuffer;

		// Token: 0x0400041E RID: 1054
		private readonly float[] _normOutput;

		// Token: 0x0400041F RID: 1055
		private readonly float[] _ffnOutput;

		// Token: 0x04000420 RID: 1056
		private readonly float[] _residualInput;

		// Token: 0x04000421 RID: 1057
		private readonly float[] _gradTemp;

		// Token: 0x04000422 RID: 1058
		private readonly float[] _gradNorm;

		// Token: 0x04000423 RID: 1059
		private readonly float[] _gradFFN;

		// Token: 0x04000424 RID: 1060
		private readonly float[] _gradResidual;
	}
}
