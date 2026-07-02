using System;
using System.Runtime.CompilerServices;
using SimdLibrary;

namespace IntlThrdPerfSchd
{
	// Token: 0x0200001F RID: 31
	public class CoreTransformerEncoder
	{
		// Token: 0x17000034 RID: 52
		// (get) Token: 0x060001CD RID: 461 RVA: 0x00017EE9 File Offset: 0x000160E9
		public MultiHeadAttention SelfAttention
		{
			get
			{
				return this._selfAttention;
			}
		}

		// Token: 0x17000035 RID: 53
		// (get) Token: 0x060001CE RID: 462 RVA: 0x00017EF1 File Offset: 0x000160F1
		public FeedForwardLayer FeedForward
		{
			get
			{
				return this._feedForward;
			}
		}

		// Token: 0x17000036 RID: 54
		// (get) Token: 0x060001CF RID: 463 RVA: 0x00017EF9 File Offset: 0x000160F9
		public LayerNormLayer Norm1
		{
			get
			{
				return this._norm1;
			}
		}

		// Token: 0x17000037 RID: 55
		// (get) Token: 0x060001D0 RID: 464 RVA: 0x00017F01 File Offset: 0x00016101
		public LayerNormLayer Norm2
		{
			get
			{
				return this._norm2;
			}
		}

		// Token: 0x060001D1 RID: 465 RVA: 0x00017F0C File Offset: 0x0001610C
		public CoreTransformerEncoder()
		{
			this._selfAttention = new MultiHeadAttention(64, 4);
			this._feedForward = new FeedForwardLayer(64, 256);
			this._norm1 = new LayerNormLayer(64);
			this._norm2 = new LayerNormLayer(64);
			this._selfAttnOutput = new float[64];
			this._norm1Output = new float[64];
			this._ffnOutput = new float[64];
			this._residual1 = new float[64];
			this._residual2 = new float[64];
			this._batchQProj = new float[64][];
			this._batchKProj = new float[64][];
			this._batchVProj = new float[64][];
			this._batchAttnOutputPerHead = new float[256][];
			this._batchAttnWeights = new float[4][];
			this._batchSelfAttnOutput = new float[64][];
			this._batchNorm1Output = new float[64][];
			this._batchFFNOutput = new float[64][];
			this._batchNorm1Input = new float[64][];
			this._batchPreNorm2Input = new float[64][];
			for (int i = 0; i < 64; i++)
			{
				this._batchQProj[i] = new float[64];
				this._batchKProj[i] = new float[64];
				this._batchVProj[i] = new float[64];
				this._batchSelfAttnOutput[i] = new float[64];
				this._batchNorm1Output[i] = new float[64];
				this._batchFFNOutput[i] = new float[64];
				this._batchNorm1Input[i] = new float[64];
				this._batchPreNorm2Input[i] = new float[64];
			}
			for (int j = 0; j < 4; j++)
			{
				this._batchAttnWeights[j] = new float[64];
			}
			for (int k = 0; k < 256; k++)
			{
				this._batchAttnOutputPerHead[k] = new float[16];
			}
			this._batchGradQProj = new float[64][];
			this._batchGradKProj = new float[64][];
			this._batchGradVProj = new float[64][];
			this._batchGradAttnScores = new float[4][];
			this._batchGradAttnWeights = new float[4][];
			this._batchGradConcatHeads = new float[64][];
			this._batchGradSelfAttnOutput = new float[64][];
			this._batchGradNorm1Input = new float[64][];
			this._batchGradFFNOutput = new float[64][];
			this._batchGradNorm1Output = new float[64][];
			this._batchGradInput = new float[64][];
			for (int l = 0; l < 64; l++)
			{
				this._batchGradQProj[l] = new float[64];
				this._batchGradKProj[l] = new float[64];
				this._batchGradVProj[l] = new float[64];
				this._batchGradConcatHeads[l] = new float[64];
				this._batchGradSelfAttnOutput[l] = new float[64];
				this._batchGradNorm1Input[l] = new float[64];
				this._batchGradFFNOutput[l] = new float[64];
				this._batchGradNorm1Output[l] = new float[64];
				this._batchGradInput[l] = new float[64];
			}
			for (int m = 0; m < 4; m++)
			{
				this._batchGradAttnScores[m] = new float[4096];
				this._batchGradAttnWeights[m] = new float[4096];
			}
			this._gradFFN = new float[64];
			this._gradSelfAttn = new float[64];
			this._gradNorm1Input = new float[64];
			this._gradResidual = new float[64];
			this._gradInput = new float[64];
			this._batchCachedNumCores = 0;
			this._batchCached = false;
		}

		// Token: 0x060001D2 RID: 466 RVA: 0x0001828C File Offset: 0x0001648C
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

		// Token: 0x060001D3 RID: 467 RVA: 0x00018368 File Offset: 0x00016568
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ForwardBatch(float[][] inputs, int numCores, float[][] outputs)
		{
			int num = 16;
			int num2 = 16;
			float num3 = 1f / (float)Math.Sqrt((double)num);
			for (int i = 0; i < numCores; i++)
			{
				this._selfAttention.Wq.Forward(inputs[i], this._batchQProj[i]);
				this._selfAttention.Wk.Forward(inputs[i], this._batchKProj[i]);
				this._selfAttention.Wv.Forward(inputs[i], this._batchVProj[i]);
			}
			for (int j = 0; j < 4; j++)
			{
				int num4 = j * num;
				for (int k = 0; k < numCores; k++)
				{
					for (int l = 0; l < numCores; l++)
					{
						float num5 = VectorMathNew.DotProduct(this._batchQProj[k].AsSpan(num4, num), this._batchKProj[l].AsSpan(num4, num)) * num3;
						this._batchAttnWeights[j][l] = num5;
					}
					Span<float> span = this._batchAttnWeights[j].AsSpan(0, numCores);
					VectorMathNew.Softmax(span, span);
					span.CopyTo(this._batchGradAttnScores[j].AsSpan(k * 64, numCores));
					int num6 = j * numCores + k;
					VectorMathNew.BatchWeightedSum(span, this._batchVProj, this._batchAttnOutputPerHead[num6].AsSpan(0, num2), numCores, num2, num4);
				}
			}
			for (int m = 0; m < numCores; m++)
			{
				float[] array = this._batchSelfAttnOutput[m];
				for (int n = 0; n < 4; n++)
				{
					int num7 = n * numCores + m;
					int num8 = n * num2;
					for (int num9 = 0; num9 < num2; num9++)
					{
						array[num8 + num9] = this._batchAttnOutputPerHead[num7][num9];
					}
				}
				this._selfAttention.Wo.Forward(array, this._batchSelfAttnOutput[m]);
			}
			for (int num10 = 0; num10 < numCores; num10++)
			{
				VectorMathNew.Add(inputs[num10], this._batchSelfAttnOutput[num10], this._batchNorm1Input[num10]);
				this._norm1.Forward(this._batchNorm1Input[num10], this._batchNorm1Output[num10]);
				this._feedForward.Forward(this._batchNorm1Output[num10], this._batchFFNOutput[num10]);
				VectorMathNew.Add(this._batchNorm1Output[num10], this._batchFFNOutput[num10], this._batchPreNorm2Input[num10]);
				this._norm2.Forward(this._batchPreNorm2Input[num10], outputs[num10]);
			}
			this._batchCachedNumCores = numCores;
			this._batchCached = true;
		}

		// Token: 0x060001D4 RID: 468 RVA: 0x00018664 File Offset: 0x00016864
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
			VectorMathNew.Add(this._gradResidual, inputGradients, this._gradResidual);
			this._norm1.Backward(this._gradResidual, learningRate, false);
			float[] inputGrads2 = this._norm1.InputGrads;
			VectorMathNew.Copy(inputGrads2, this._gradSelfAttn);
			this.ClipGradientIfNeeded(this._gradSelfAttn);
			this._selfAttention.SelfBackward(this._gradSelfAttn, learningRate);
			float[] selfInputGradients = this._selfAttention.GetSelfInputGradients();
			VectorMathNew.Add(inputGrads2, selfInputGradients, this._gradInput);
			this._norm2.ApplyGradients(learningRate);
			this._norm1.ApplyGradients(learningRate);
		}

		// Token: 0x060001D5 RID: 469 RVA: 0x000187A4 File Offset: 0x000169A4
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void BackwardBatch(float[][] gradOutputs, int numCores, float learningRate = 0.001f)
		{
			if (!this._batchCached || numCores != this._batchCachedNumCores)
			{
				return;
			}
			int num = 16;
			int num2 = 16;
			float num3 = 1f / (float)Math.Sqrt((double)num);
			for (int i = 0; i < numCores; i++)
			{
				this._norm2.Backward(gradOutputs[i], learningRate, i > 0);
				float[] inputGrads = this._norm2.InputGrads;
				VectorMathNew.Copy(inputGrads, this._batchGradFFNOutput[i]);
				VectorMathNew.Copy(inputGrads, this._batchGradNorm1Output[i]);
				this._feedForward.Backward(this._batchGradFFNOutput[i], learningRate);
				float[] inputGradients = this._feedForward.InputGradients;
				VectorMathNew.Add(this._batchGradNorm1Output[i], inputGradients, this._batchGradNorm1Output[i]);
				this._norm1.Backward(this._batchGradNorm1Output[i], learningRate, i > 0);
				float[] inputGrads2 = this._norm1.InputGrads;
				VectorMathNew.Copy(inputGrads2, this._batchGradSelfAttnOutput[i]);
				VectorMathNew.Copy(inputGrads2, this._batchGradInput[i]);
			}
			this._norm2.ApplyGradients(learningRate);
			this._norm1.ApplyGradients(learningRate);
			for (int j = 0; j < numCores; j++)
			{
				this._selfAttention.Wo.Backward(this._batchGradSelfAttnOutput[j], learningRate, false);
				this._selfAttention.Wo.InputGrads.AsSpan(0, 64).CopyTo(this._batchGradConcatHeads[j]);
			}
			for (int k = 0; k < numCores; k++)
			{
				VectorMathNew.Zero(this._batchGradQProj[k]);
				VectorMathNew.Zero(this._batchGradKProj[k]);
				VectorMathNew.Zero(this._batchGradVProj[k]);
			}
			for (int l = 0; l < 4; l++)
			{
				int num4 = l * num;
				for (int m = 0; m < numCores; m++)
				{
					for (int n = 0; n < numCores; n++)
					{
						this._batchGradAttnWeights[l][m * 64 + n] = VectorMathNew.DotProduct(this._batchGradConcatHeads[m].AsSpan(num4, num2), this._batchVProj[n].AsSpan(num4, num2));
					}
					Span<float> span = this._batchGradAttnScores[l].AsSpan(m * 64, numCores);
					Span<float> span2 = this._batchGradAttnWeights[l].AsSpan(m * 64, numCores);
					VectorMathNew.SoftmaxBackward(span, span2, span2);
					span2.CopyTo(this._batchGradAttnScores[l].AsSpan(m * 64, numCores));
					for (int num5 = 0; num5 < numCores; num5++)
					{
						float num6 = this._batchGradAttnScores[l][m * 64 + num5] * num3;
						Span<float> span3 = this._batchKProj[num5].AsSpan(num4, num);
						VectorMathNew.AddScalarMultiplyInPlace(this._batchGradQProj[m].AsSpan(num4, num), span3, num6);
					}
					for (int num7 = 0; num7 < numCores; num7++)
					{
						float num8 = this._batchGradAttnScores[l][m * 64 + num7] * num3;
						Span<float> span4 = this._batchQProj[m].AsSpan(num4, num);
						VectorMathNew.AddScalarMultiplyInPlace(this._batchGradKProj[num7].AsSpan(num4, num), span4, num8);
					}
				}
			}
			for (int num9 = 0; num9 < numCores; num9++)
			{
				VectorMathNew.Zero(this._batchGradVProj[num9]);
			}
			for (int num10 = 0; num10 < 4; num10++)
			{
				int num11 = num10 * num;
				float[] array = this._batchGradAttnWeights[num10];
				for (int num12 = 0; num12 < numCores; num12++)
				{
					for (int num13 = 0; num13 < numCores; num13++)
					{
						array[num12 * 64 + num13] = VectorMathNew.DotProduct(this._batchQProj[num12].AsSpan(num11, num), this._batchKProj[num13].AsSpan(num11, num)) * num3;
					}
					Span<float> span5 = array.AsSpan(num12 * 64, numCores);
					VectorMathNew.Softmax(span5, span5);
					for (int num14 = 0; num14 < numCores; num14++)
					{
						float num15 = array[num12 * 64 + num14];
						Span<float> span6 = this._batchGradConcatHeads[num12].AsSpan(num11, num2);
						VectorMathNew.AddScalarMultiplyInPlace(this._batchGradVProj[num14].AsSpan(num11, num2), span6, num15);
					}
				}
			}
			for (int num16 = 0; num16 < numCores; num16++)
			{
				this.ClipGradientIfNeeded(this._batchGradQProj[num16]);
				this.ClipGradientIfNeeded(this._batchGradKProj[num16]);
				this.ClipGradientIfNeeded(this._batchGradVProj[num16]);
			}
			bool flag = true;
			for (int num17 = 0; num17 < numCores; num17++)
			{
				if (flag)
				{
					this._selfAttention.Wq.Backward(this._batchGradQProj[num17], learningRate, false);
					this._selfAttention.Wk.Backward(this._batchGradKProj[num17], learningRate, false);
					this._selfAttention.Wv.Backward(this._batchGradVProj[num17], learningRate, false);
					flag = false;
				}
				else
				{
					this._selfAttention.Wq.Backward(this._batchGradQProj[num17], learningRate, true);
					this._selfAttention.Wk.Backward(this._batchGradKProj[num17], learningRate, true);
					this._selfAttention.Wv.Backward(this._batchGradVProj[num17], learningRate, true);
				}
			}
			for (int num18 = 0; num18 < numCores; num18++)
			{
			}
			for (int num19 = 0; num19 < numCores; num19++)
			{
				float[] array2 = this._batchGradSelfAttnOutput[num19];
				MathHelper.ComputeInputGrad(this._batchGradQProj[num19], this._selfAttention.Wq.Weights, array2, 64, 64);
				VectorMathNew.Add(this._batchGradInput[num19], array2, this._batchGradInput[num19]);
				MathHelper.ComputeInputGrad(this._batchGradKProj[num19], this._selfAttention.Wk.Weights, array2, 64, 64);
				VectorMathNew.Add(this._batchGradInput[num19], array2, this._batchGradInput[num19]);
				MathHelper.ComputeInputGrad(this._batchGradVProj[num19], this._selfAttention.Wv.Weights, array2, 64, 64);
				VectorMathNew.Add(this._batchGradInput[num19], array2, this._batchGradInput[num19]);
			}
			for (int num20 = 0; num20 < numCores; num20++)
			{
				this.ClipGradientIfNeeded(this._batchGradInput[num20]);
			}
		}

		// Token: 0x060001D6 RID: 470 RVA: 0x00018EE6 File Offset: 0x000170E6
		public float[][] GetBatchInputGradients()
		{
			return this._batchGradInput;
		}

		// Token: 0x060001D7 RID: 471 RVA: 0x00018EF0 File Offset: 0x000170F0
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

		// Token: 0x060001D8 RID: 472 RVA: 0x00018F40 File Offset: 0x00017140
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

		// Token: 0x060001D9 RID: 473 RVA: 0x00018F70 File Offset: 0x00017170
		private bool AreWeightsValid()
		{
			return this.IsWeightsValid(this._selfAttention.Wq.Weights) && this.IsWeightsValid(this._selfAttention.Wk.Weights) && this.IsWeightsValid(this._selfAttention.Wv.Weights) && this.IsWeightsValid(this._selfAttention.Wo.Weights) && this.IsWeightsValid(this._feedForward.FC1.Weights) && this.IsWeightsValid(this._feedForward.FC2.Weights);
		}

		// Token: 0x060001DA RID: 474 RVA: 0x0001901A File Offset: 0x0001721A
		private bool IsWeightsValid(float[] weights)
		{
			return !VectorMathNew.HasInvalidValues(weights.AsSpan<float>());
		}

		// Token: 0x17000038 RID: 56
		// (get) Token: 0x060001DB RID: 475 RVA: 0x0001902F File Offset: 0x0001722F
		public float[] InputGradients
		{
			get
			{
				return this._gradInput;
			}
		}

		// Token: 0x04000486 RID: 1158
		private const int D_MODEL = 64;

		// Token: 0x04000487 RID: 1159
		private const int D_FF = 256;

		// Token: 0x04000488 RID: 1160
		private const int HEAD_COUNT = 4;

		// Token: 0x04000489 RID: 1161
		private const int MAX_CORES = 64;

		// Token: 0x0400048A RID: 1162
		private const float GRADIENT_CLIP_THRESHOLD = 1f;

		// Token: 0x0400048B RID: 1163
		private readonly MultiHeadAttention _selfAttention;

		// Token: 0x0400048C RID: 1164
		private readonly FeedForwardLayer _feedForward;

		// Token: 0x0400048D RID: 1165
		private readonly LayerNormLayer _norm1;

		// Token: 0x0400048E RID: 1166
		private readonly LayerNormLayer _norm2;

		// Token: 0x0400048F RID: 1167
		private readonly float[] _selfAttnOutput;

		// Token: 0x04000490 RID: 1168
		private readonly float[] _norm1Output;

		// Token: 0x04000491 RID: 1169
		private readonly float[] _ffnOutput;

		// Token: 0x04000492 RID: 1170
		private readonly float[] _residual1;

		// Token: 0x04000493 RID: 1171
		private readonly float[] _residual2;

		// Token: 0x04000494 RID: 1172
		private readonly float[][] _batchQProj;

		// Token: 0x04000495 RID: 1173
		private readonly float[][] _batchKProj;

		// Token: 0x04000496 RID: 1174
		private readonly float[][] _batchVProj;

		// Token: 0x04000497 RID: 1175
		private readonly float[][] _batchAttnOutputPerHead;

		// Token: 0x04000498 RID: 1176
		private readonly float[][] _batchAttnWeights;

		// Token: 0x04000499 RID: 1177
		private readonly float[][] _batchSelfAttnOutput;

		// Token: 0x0400049A RID: 1178
		private readonly float[][] _batchNorm1Output;

		// Token: 0x0400049B RID: 1179
		private readonly float[][] _batchFFNOutput;

		// Token: 0x0400049C RID: 1180
		private readonly float[][] _batchNorm1Input;

		// Token: 0x0400049D RID: 1181
		private readonly float[][] _batchPreNorm2Input;

		// Token: 0x0400049E RID: 1182
		private readonly float[][] _batchGradQProj;

		// Token: 0x0400049F RID: 1183
		private readonly float[][] _batchGradKProj;

		// Token: 0x040004A0 RID: 1184
		private readonly float[][] _batchGradVProj;

		// Token: 0x040004A1 RID: 1185
		private readonly float[][] _batchGradAttnScores;

		// Token: 0x040004A2 RID: 1186
		private readonly float[][] _batchGradAttnWeights;

		// Token: 0x040004A3 RID: 1187
		private readonly float[][] _batchGradConcatHeads;

		// Token: 0x040004A4 RID: 1188
		private readonly float[][] _batchGradSelfAttnOutput;

		// Token: 0x040004A5 RID: 1189
		private readonly float[][] _batchGradNorm1Input;

		// Token: 0x040004A6 RID: 1190
		private readonly float[][] _batchGradFFNOutput;

		// Token: 0x040004A7 RID: 1191
		private readonly float[][] _batchGradNorm1Output;

		// Token: 0x040004A8 RID: 1192
		private readonly float[][] _batchGradInput;

		// Token: 0x040004A9 RID: 1193
		private readonly float[] _gradFFN;

		// Token: 0x040004AA RID: 1194
		private readonly float[] _gradSelfAttn;

		// Token: 0x040004AB RID: 1195
		private readonly float[] _gradNorm1Input;

		// Token: 0x040004AC RID: 1196
		private readonly float[] _gradResidual;

		// Token: 0x040004AD RID: 1197
		private readonly float[] _gradInput;

		// Token: 0x040004AE RID: 1198
		private int _batchCachedNumCores;

		// Token: 0x040004AF RID: 1199
		private bool _batchCached;
	}
}
