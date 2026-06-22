using System;
using System.Runtime.CompilerServices;
using SimdLibrary;

namespace IntlThrdPerfSchd
{
	public class ThreadTransformerEncoder
	{
		private const int D_MODEL = 64;

		private const int D_FF = 128;

		private const int HEAD_COUNT = 4;

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

		private readonly float[] _gradFFN;

		private readonly float[] _gradSelfAttn;

		private readonly float[] _gradNorm1Input;

		private readonly float[] _gradResidual;

		private readonly float[] _gradInput;

		public MultiHeadAttention SelfAttention => _selfAttention;

		public FeedForwardLayer FeedForward => _feedForward;

		public LayerNormLayer Norm1 => _norm1;

		public LayerNormLayer Norm2 => _norm2;

		public float[] InputGradients => _gradInput;

		public ThreadTransformerEncoder()
		{
			_selfAttention = new MultiHeadAttention(64, 4);
			_feedForward = new FeedForwardLayer(64, 128);
			_norm1 = new LayerNormLayer(64);
			_norm2 = new LayerNormLayer(64);
			_selfAttnOutput = new float[64];
			_norm1Output = new float[64];
			_ffnOutput = new float[64];
			_residual1 = new float[64];
			_residual2 = new float[64];
			_gradFFN = new float[64];
			_gradSelfAttn = new float[64];
			_gradNorm1Input = new float[64];
			_gradResidual = new float[64];
			_gradInput = new float[64];
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
