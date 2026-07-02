using System;
using System.Runtime.CompilerServices;
using SimdLibrary;

namespace IntlThrdPerfSchd
{
	public class CoreTransformerEncoder
	{
		private const int D_MODEL = 64;

		private const int D_FF = 256;

		private const int HEAD_COUNT = 4;

		private const int MAX_CORES = 64;

		private const float GRADIENT_CLIP_THRESHOLD = 1f;

		private readonly MultiHeadAttention _selfAttention;

		private readonly FeedForwardLayer _feedForward;

		private readonly LayerNormLayer _norm1;

		private readonly LayerNormLayer _norm2;

		private readonly float[] _selfAttnOutput;

		private readonly float[] _norm1Output;

		private readonly float[] _ffnOutput;

		private readonly float[] _residual1;

		private readonly float[] _residual2;

		private readonly float[][] _batchQProj;

		private readonly float[][] _batchKProj;

		private readonly float[][] _batchVProj;

		private readonly float[][] _batchAttnOutputPerHead;

		private readonly float[][] _batchAttnWeights;

		private readonly float[][] _batchSelfAttnOutput;

		private readonly float[][] _batchNorm1Output;

		private readonly float[][] _batchFFNOutput;

		private readonly float[][] _batchNorm1Input;

		private readonly float[][] _batchPreNorm2Input;

		private readonly float[][] _batchGradQProj;

		private readonly float[][] _batchGradKProj;

		private readonly float[][] _batchGradVProj;

		private readonly float[][] _batchGradAttnScores;

		private readonly float[][] _batchGradAttnWeights;

		private readonly float[][] _batchGradConcatHeads;

		private readonly float[][] _batchGradSelfAttnOutput;

		private readonly float[][] _batchGradNorm1Input;

		private readonly float[][] _batchGradFFNOutput;

		private readonly float[][] _batchGradNorm1Output;

		private readonly float[][] _batchGradInput;

		private readonly float[] _gradFFN;

		private readonly float[] _gradSelfAttn;

		private readonly float[] _gradNorm1Input;

		private readonly float[] _gradResidual;

		private readonly float[] _gradInput;

		private int _batchCachedNumCores;

		private bool _batchCached;

		public MultiHeadAttention SelfAttention => _selfAttention;

		public FeedForwardLayer FeedForward => _feedForward;

		public LayerNormLayer Norm1 => _norm1;

		public LayerNormLayer Norm2 => _norm2;

		public float[] InputGradients => _gradInput;

		public CoreTransformerEncoder()
		{
			_selfAttention = new MultiHeadAttention(64, 4);
			_feedForward = new FeedForwardLayer(64);
			_norm1 = new LayerNormLayer(64);
			_norm2 = new LayerNormLayer(64);
			_selfAttnOutput = new float[64];
			_norm1Output = new float[64];
			_ffnOutput = new float[64];
			_residual1 = new float[64];
			_residual2 = new float[64];
			_batchQProj = new float[64][];
			_batchKProj = new float[64][];
			_batchVProj = new float[64][];
			_batchAttnOutputPerHead = new float[256][];
			_batchAttnWeights = new float[4][];
			_batchSelfAttnOutput = new float[64][];
			_batchNorm1Output = new float[64][];
			_batchFFNOutput = new float[64][];
			_batchNorm1Input = new float[64][];
			_batchPreNorm2Input = new float[64][];
			for (int i = 0; i < 64; i++)
			{
				_batchQProj[i] = new float[64];
				_batchKProj[i] = new float[64];
				_batchVProj[i] = new float[64];
				_batchSelfAttnOutput[i] = new float[64];
				_batchNorm1Output[i] = new float[64];
				_batchFFNOutput[i] = new float[64];
				_batchNorm1Input[i] = new float[64];
				_batchPreNorm2Input[i] = new float[64];
			}
			for (int j = 0; j < 4; j++)
			{
				_batchAttnWeights[j] = new float[64];
			}
			for (int k = 0; k < 256; k++)
			{
				_batchAttnOutputPerHead[k] = new float[16];
			}
			_batchGradQProj = new float[64][];
			_batchGradKProj = new float[64][];
			_batchGradVProj = new float[64][];
			_batchGradAttnScores = new float[4][];
			_batchGradAttnWeights = new float[4][];
			_batchGradConcatHeads = new float[64][];
			_batchGradSelfAttnOutput = new float[64][];
			_batchGradNorm1Input = new float[64][];
			_batchGradFFNOutput = new float[64][];
			_batchGradNorm1Output = new float[64][];
			_batchGradInput = new float[64][];
			for (int l = 0; l < 64; l++)
			{
				_batchGradQProj[l] = new float[64];
				_batchGradKProj[l] = new float[64];
				_batchGradVProj[l] = new float[64];
				_batchGradConcatHeads[l] = new float[64];
				_batchGradSelfAttnOutput[l] = new float[64];
				_batchGradNorm1Input[l] = new float[64];
				_batchGradFFNOutput[l] = new float[64];
				_batchGradNorm1Output[l] = new float[64];
				_batchGradInput[l] = new float[64];
			}
			for (int m = 0; m < 4; m++)
			{
				_batchGradAttnScores[m] = new float[4096];
				_batchGradAttnWeights[m] = new float[4096];
			}
			_gradFFN = new float[64];
			_gradSelfAttn = new float[64];
			_gradNorm1Input = new float[64];
			_gradResidual = new float[64];
			_gradInput = new float[64];
			_batchCachedNumCores = 0;
			_batchCached = false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Forward(ReadOnlySpan<float> input, Span<float> output)
		{
			VectorMathNew.Copy(input, _residual1);
			_selfAttention.SelfForward(input, _selfAttnOutput);
			VectorMathNew.Add(input, _selfAttnOutput, _norm1Output);
			_norm1.Forward(_norm1Output, _norm1Output);
			VectorMathNew.Copy(_norm1Output, _residual2);
			_feedForward.Forward(_norm1Output, _ffnOutput);
			VectorMathNew.Add(_residual2, _ffnOutput, output);
			_norm2.Forward(output, output);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ForwardBatch(float[][] inputs, int numCores, float[][] outputs)
		{
			int num = 16;
			int num2 = 16;
			float num3 = 1f / (float)Math.Sqrt(num);
			for (int i = 0; i < numCores; i++)
			{
				_selfAttention.Wq.Forward(inputs[i], _batchQProj[i]);
				_selfAttention.Wk.Forward(inputs[i], _batchKProj[i]);
				_selfAttention.Wv.Forward(inputs[i], _batchVProj[i]);
			}
			for (int j = 0; j < 4; j++)
			{
				int num4 = j * num;
				for (int k = 0; k < numCores; k++)
				{
					for (int l = 0; l < numCores; l++)
					{
						float num5 = VectorMathNew.DotProduct(_batchQProj[k].AsSpan(num4, num), _batchKProj[l].AsSpan(num4, num)) * num3;
						_batchAttnWeights[j][l] = num5;
					}
					Span<float> span = _batchAttnWeights[j].AsSpan(0, numCores);
					VectorMathNew.Softmax(span, span);
					span.CopyTo(_batchGradAttnScores[j].AsSpan(k * 64, numCores));
					int num6 = j * numCores + k;
					VectorMathNew.BatchWeightedSum(span, _batchVProj, _batchAttnOutputPerHead[num6].AsSpan(0, num2), numCores, num2, num4);
				}
			}
			for (int m = 0; m < numCores; m++)
			{
				float[] array = _batchSelfAttnOutput[m];
				for (int n = 0; n < 4; n++)
				{
					int num7 = n * numCores + m;
					int num8 = n * num2;
					for (int num9 = 0; num9 < num2; num9++)
					{
						array[num8 + num9] = _batchAttnOutputPerHead[num7][num9];
					}
				}
				_selfAttention.Wo.Forward(array, _batchSelfAttnOutput[m]);
			}
			for (int num10 = 0; num10 < numCores; num10++)
			{
				VectorMathNew.Add(inputs[num10], _batchSelfAttnOutput[num10], _batchNorm1Input[num10]);
				_norm1.Forward(_batchNorm1Input[num10], _batchNorm1Output[num10]);
				_feedForward.Forward(_batchNorm1Output[num10], _batchFFNOutput[num10]);
				VectorMathNew.Add(_batchNorm1Output[num10], _batchFFNOutput[num10], _batchPreNorm2Input[num10]);
				_norm2.Forward(_batchPreNorm2Input[num10], outputs[num10]);
			}
			_batchCachedNumCores = numCores;
			_batchCached = true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Backward(ReadOnlySpan<float> gradOutput, float learningRate = 0.001f)
		{
			if (!VectorMathNew.HasInvalidValues(gradOutput))
			{
				_norm2.Backward(gradOutput, learningRate);
				float[] inputGrads = _norm2.InputGrads;
				VectorMathNew.Copy(inputGrads, _gradFFN);
				VectorMathNew.Copy(inputGrads, _gradResidual);
				_feedForward.Backward(_gradFFN, learningRate);
				float[] inputGradients = _feedForward.InputGradients;
				VectorMathNew.Add(_gradResidual, inputGradients, _gradResidual);
				_norm1.Backward(_gradResidual, learningRate);
				float[] inputGrads2 = _norm1.InputGrads;
				VectorMathNew.Copy(inputGrads2, _gradSelfAttn);
				ClipGradientIfNeeded(_gradSelfAttn);
				_selfAttention.SelfBackward(_gradSelfAttn, learningRate);
				VectorMathNew.Add(right: _selfAttention.GetSelfInputGradients(), left: inputGrads2, result: _gradInput);
				_norm2.ApplyGradients(learningRate);
				_norm1.ApplyGradients(learningRate);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void BackwardBatch(float[][] gradOutputs, int numCores, float learningRate = 0.001f)
		{
			if (!_batchCached || numCores != _batchCachedNumCores)
			{
				return;
			}
			int num = 16;
			int length = 16;
			float num2 = 1f / (float)Math.Sqrt(num);
			for (int i = 0; i < numCores; i++)
			{
				_norm2.Backward(gradOutputs[i], learningRate, i > 0);
				float[] inputGrads = _norm2.InputGrads;
				VectorMathNew.Copy(inputGrads, _batchGradFFNOutput[i]);
				VectorMathNew.Copy(inputGrads, _batchGradNorm1Output[i]);
				_feedForward.Backward(_batchGradFFNOutput[i], learningRate);
				float[] inputGradients = _feedForward.InputGradients;
				VectorMathNew.Add(_batchGradNorm1Output[i], inputGradients, _batchGradNorm1Output[i]);
				_norm1.Backward(_batchGradNorm1Output[i], learningRate, i > 0);
				float[] inputGrads2 = _norm1.InputGrads;
				VectorMathNew.Copy(inputGrads2, _batchGradSelfAttnOutput[i]);
				VectorMathNew.Copy(inputGrads2, _batchGradInput[i]);
			}
			_norm2.ApplyGradients(learningRate);
			_norm1.ApplyGradients(learningRate);
			for (int j = 0; j < numCores; j++)
			{
				_selfAttention.Wo.Backward(_batchGradSelfAttnOutput[j], learningRate);
				_selfAttention.Wo.InputGrads.AsSpan(0, 64).CopyTo(_batchGradConcatHeads[j]);
			}
			for (int k = 0; k < numCores; k++)
			{
				VectorMathNew.Zero(_batchGradQProj[k]);
				VectorMathNew.Zero(_batchGradKProj[k]);
				VectorMathNew.Zero(_batchGradVProj[k]);
			}
			for (int l = 0; l < 4; l++)
			{
				int start = l * num;
				for (int m = 0; m < numCores; m++)
				{
					for (int n = 0; n < numCores; n++)
					{
						_batchGradAttnWeights[l][m * 64 + n] = VectorMathNew.DotProduct(_batchGradConcatHeads[m].AsSpan(start, length), _batchVProj[n].AsSpan(start, length));
					}
					Span<float> span = _batchGradAttnScores[l].AsSpan(m * 64, numCores);
					Span<float> span2 = _batchGradAttnWeights[l].AsSpan(m * 64, numCores);
					VectorMathNew.SoftmaxBackward(span, span2, span2);
					span2.CopyTo(_batchGradAttnScores[l].AsSpan(m * 64, numCores));
					for (int num3 = 0; num3 < numCores; num3++)
					{
						float scalar = _batchGradAttnScores[l][m * 64 + num3] * num2;
						Span<float> span3 = _batchKProj[num3].AsSpan(start, num);
						VectorMathNew.AddScalarMultiplyInPlace(_batchGradQProj[m].AsSpan(start, num), span3, scalar);
					}
					for (int num4 = 0; num4 < numCores; num4++)
					{
						float scalar2 = _batchGradAttnScores[l][m * 64 + num4] * num2;
						Span<float> span4 = _batchQProj[m].AsSpan(start, num);
						VectorMathNew.AddScalarMultiplyInPlace(_batchGradKProj[num4].AsSpan(start, num), span4, scalar2);
					}
				}
			}
			for (int num5 = 0; num5 < numCores; num5++)
			{
				VectorMathNew.Zero(_batchGradVProj[num5]);
			}
			for (int num6 = 0; num6 < 4; num6++)
			{
				int start2 = num6 * num;
				float[] array = _batchGradAttnWeights[num6];
				for (int num7 = 0; num7 < numCores; num7++)
				{
					for (int num8 = 0; num8 < numCores; num8++)
					{
						array[num7 * 64 + num8] = VectorMathNew.DotProduct(_batchQProj[num7].AsSpan(start2, num), _batchKProj[num8].AsSpan(start2, num)) * num2;
					}
					Span<float> span5 = array.AsSpan(num7 * 64, numCores);
					VectorMathNew.Softmax(span5, span5);
					for (int num9 = 0; num9 < numCores; num9++)
					{
						float scalar3 = array[num7 * 64 + num9];
						Span<float> span6 = _batchGradConcatHeads[num7].AsSpan(start2, length);
						VectorMathNew.AddScalarMultiplyInPlace(_batchGradVProj[num9].AsSpan(start2, length), span6, scalar3);
					}
				}
			}
			for (int num10 = 0; num10 < numCores; num10++)
			{
				ClipGradientIfNeeded(_batchGradQProj[num10]);
				ClipGradientIfNeeded(_batchGradKProj[num10]);
				ClipGradientIfNeeded(_batchGradVProj[num10]);
			}
			bool flag = true;
			for (int num11 = 0; num11 < numCores; num11++)
			{
				if (flag)
				{
					_selfAttention.Wq.Backward(_batchGradQProj[num11], learningRate);
					_selfAttention.Wk.Backward(_batchGradKProj[num11], learningRate);
					_selfAttention.Wv.Backward(_batchGradVProj[num11], learningRate);
					flag = false;
				}
				else
				{
					_selfAttention.Wq.Backward(_batchGradQProj[num11], learningRate, accumulateGrad: true);
					_selfAttention.Wk.Backward(_batchGradKProj[num11], learningRate, accumulateGrad: true);
					_selfAttention.Wv.Backward(_batchGradVProj[num11], learningRate, accumulateGrad: true);
				}
			}
			for (int num12 = 0; num12 < numCores; num12++)
			{
			}
			for (int num13 = 0; num13 < numCores; num13++)
			{
				float[] array2 = _batchGradSelfAttnOutput[num13];
				MathHelper.ComputeInputGrad(_batchGradQProj[num13], _selfAttention.Wq.Weights, array2, 64, 64);
				VectorMathNew.Add(_batchGradInput[num13], array2, _batchGradInput[num13]);
				MathHelper.ComputeInputGrad(_batchGradKProj[num13], _selfAttention.Wk.Weights, array2, 64, 64);
				VectorMathNew.Add(_batchGradInput[num13], array2, _batchGradInput[num13]);
				MathHelper.ComputeInputGrad(_batchGradVProj[num13], _selfAttention.Wv.Weights, array2, 64, 64);
				VectorMathNew.Add(_batchGradInput[num13], array2, _batchGradInput[num13]);
			}
			for (int num14 = 0; num14 < numCores; num14++)
			{
				ClipGradientIfNeeded(_batchGradInput[num14]);
			}
		}

		public float[][] GetBatchInputGradients()
		{
			return _batchGradInput;
		}

		public void ApplyGradients(float learningRate = 0.001f)
		{
			if (AreWeightsValid())
			{
				_selfAttention.ApplyGradients(learningRate);
				_feedForward.FC1.ApplyGradientsSGD(learningRate);
				_feedForward.FC2.ApplyGradientsSGD(learningRate);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void ClipGradientIfNeeded(Span<float> grad)
		{
			float num = VectorMathNew.EuclideanNorm(grad);
			if (num > 1f)
			{
				float scalar = 1f / num;
				VectorMathNew.MultiplyScalarInPlace(grad, scalar);
			}
		}

		private bool AreWeightsValid()
		{
			if (!IsWeightsValid(_selfAttention.Wq.Weights))
			{
				return false;
			}
			if (!IsWeightsValid(_selfAttention.Wk.Weights))
			{
				return false;
			}
			if (!IsWeightsValid(_selfAttention.Wv.Weights))
			{
				return false;
			}
			if (!IsWeightsValid(_selfAttention.Wo.Weights))
			{
				return false;
			}
			if (!IsWeightsValid(_feedForward.FC1.Weights))
			{
				return false;
			}
			if (!IsWeightsValid(_feedForward.FC2.Weights))
			{
				return false;
			}
			return true;
		}

		private bool IsWeightsValid(float[] weights)
		{
			return !VectorMathNew.HasInvalidValues(weights.AsSpan());
		}
	}
}
