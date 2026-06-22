using System;
using System.Runtime.CompilerServices;
using SimdLibrary;

namespace IntlThrdPerfSchd
{
	// Token: 0x0200001B RID: 27
	public class FeedForwardLayer
	{
		// Token: 0x1700002C RID: 44
		// (get) Token: 0x060001AE RID: 430 RVA: 0x000156E2 File Offset: 0x000138E2
		public LinearLayer FC1
		{
			get
			{
				return this._fc1;
			}
		}

		// Token: 0x1700002D RID: 45
		// (get) Token: 0x060001AF RID: 431 RVA: 0x000156EA File Offset: 0x000138EA
		public LinearLayer FC2
		{
			get
			{
				return this._fc2;
			}
		}

		// Token: 0x060001B0 RID: 432 RVA: 0x000156F4 File Offset: 0x000138F4
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

		// Token: 0x060001B1 RID: 433 RVA: 0x00015760 File Offset: 0x00013960
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Forward(ReadOnlySpan<float> input, Span<float> output)
		{
			this._fc1.Forward(input, this._hidden);
			for (int i = 0; i < this._dFF; i++)
			{
				this._hiddenAfterRelu[i] = Math.Max(0f, this._hidden[i]);
			}
			this._fc2.Forward(this._hiddenAfterRelu, output);
		}

		// Token: 0x060001B2 RID: 434 RVA: 0x000157C8 File Offset: 0x000139C8
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Backward(ReadOnlySpan<float> gradOutput, float learningRate = 0.001f)
		{
			this._fc2.Backward(gradOutput, learningRate, false);
			float[] inputGrads = this._fc2.InputGrads;
			VectorMathNew.ReluGradient(new ReadOnlySpan<float>(this._hidden, 0, this._dFF), new ReadOnlySpan<float>(inputGrads, 0, this._dFF), new Span<float>(this._gradHidden, 0, this._dFF));
			this._fc1.Backward(this._gradHidden, learningRate, false);
		}

		// Token: 0x1700002E RID: 46
		// (get) Token: 0x060001B3 RID: 435 RVA: 0x0001583D File Offset: 0x00013A3D
		public float[] InputGradients
		{
			get
			{
				return this._fc1.InputGrads;
			}
		}

		// Token: 0x04000410 RID: 1040
		private readonly LinearLayer _fc1;

		// Token: 0x04000411 RID: 1041
		private readonly LinearLayer _fc2;

		// Token: 0x04000412 RID: 1042
		private readonly int _dModel;

		// Token: 0x04000413 RID: 1043
		private readonly int _dFF;

		// Token: 0x04000414 RID: 1044
		private readonly float[] _hidden;

		// Token: 0x04000415 RID: 1045
		private readonly float[] _hiddenAfterRelu;

		// Token: 0x04000416 RID: 1046
		private readonly float[] _gradHidden;

		// Token: 0x04000417 RID: 1047
		private readonly float[] _gradInput;
	}
}
