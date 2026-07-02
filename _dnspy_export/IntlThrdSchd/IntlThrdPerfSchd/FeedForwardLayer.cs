using System;
using System.Runtime.CompilerServices;
using SimdLibrary;

namespace IntlThrdPerfSchd
{
	// Token: 0x0200001D RID: 29
	public class FeedForwardLayer
	{
		// Token: 0x1700002C RID: 44
		// (get) Token: 0x060001BE RID: 446 RVA: 0x00017AE6 File Offset: 0x00015CE6
		public LinearLayer FC1
		{
			get
			{
				return this._fc1;
			}
		}

		// Token: 0x1700002D RID: 45
		// (get) Token: 0x060001BF RID: 447 RVA: 0x00017AEE File Offset: 0x00015CEE
		public LinearLayer FC2
		{
			get
			{
				return this._fc2;
			}
		}

		// Token: 0x060001C0 RID: 448 RVA: 0x00017AF8 File Offset: 0x00015CF8
		public FeedForwardLayer(int dModel, int dFF = 256)
		{
			this._dModel = dModel;
			this._dFF = dFF;
			this._fc1 = new LinearLayer(dModel, dFF);
			this._fc2 = new LinearLayer(dFF, dModel);
			this._hidden = new float[dFF];
			this._hiddenAfterRelu = new float[dFF];
			this._gradHidden = new float[dFF];
			this._gradInput = new float[dModel];
		}

		// Token: 0x060001C1 RID: 449 RVA: 0x00017B64 File Offset: 0x00015D64
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Forward(ReadOnlySpan<float> input, Span<float> output)
		{
			this._fc1.Forward(input, this._hidden);
			VectorMathNew.Relu(this._hidden, this._hiddenAfterRelu);
			this._fc2.Forward(this._hiddenAfterRelu, output);
		}

		// Token: 0x060001C2 RID: 450 RVA: 0x00017BBC File Offset: 0x00015DBC
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Backward(ReadOnlySpan<float> gradOutput, float learningRate = 0.001f)
		{
			this._fc2.Backward(gradOutput, learningRate, false);
			float[] inputGrads = this._fc2.InputGrads;
			VectorMathNew.ReluGradient(new ReadOnlySpan<float>(this._hidden, 0, this._dFF), new ReadOnlySpan<float>(inputGrads, 0, this._dFF), new Span<float>(this._gradHidden, 0, this._dFF));
			this._fc1.Backward(this._gradHidden, learningRate, false);
		}

		// Token: 0x1700002E RID: 46
		// (get) Token: 0x060001C3 RID: 451 RVA: 0x00017C31 File Offset: 0x00015E31
		public float[] InputGradients
		{
			get
			{
				return this._fc1.InputGrads;
			}
		}

		// Token: 0x04000471 RID: 1137
		private readonly LinearLayer _fc1;

		// Token: 0x04000472 RID: 1138
		private readonly LinearLayer _fc2;

		// Token: 0x04000473 RID: 1139
		private readonly int _dModel;

		// Token: 0x04000474 RID: 1140
		private readonly int _dFF;

		// Token: 0x04000475 RID: 1141
		private readonly float[] _hidden;

		// Token: 0x04000476 RID: 1142
		private readonly float[] _hiddenAfterRelu;

		// Token: 0x04000477 RID: 1143
		private readonly float[] _gradHidden;

		// Token: 0x04000478 RID: 1144
		private readonly float[] _gradInput;
	}
}
