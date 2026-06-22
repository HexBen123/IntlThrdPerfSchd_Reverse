using System;
using System.Runtime.CompilerServices;
using SimdLibrary;

namespace IntlThrdPerfSchd
{
	public class TransformerEncoderLayer
	{
		private readonly MultiHeadAttention _selfAttention;

		private readonly FeedForwardLayer _feedForward;

		private readonly LayerNormLayer _norm1;

		private readonly LayerNormLayer _norm2;

		private readonly int _dModel;

		private readonly float[] _tempBuffer;

		private readonly float[] _normOutput;

		private readonly float[] _ffnOutput;

		private readonly float[] _residualInput;

		private readonly float[] _gradTemp;

		private readonly float[] _gradNorm;

		private readonly float[] _gradFFN;

		private readonly float[] _gradResidual;

		public MultiHeadAttention SelfAttention => _selfAttention;

		public FeedForwardLayer FeedForward => _feedForward;

		public LayerNormLayer Norm1 => _norm1;

		public LayerNormLayer Norm2 => _norm2;

		public float[] InputGradients => _gradResidual;

		public TransformerEncoderLayer(int dModel, int nHead, int dFF = 256)
		{
			_dModel = dModel;
			_selfAttention = new MultiHeadAttention(dModel, nHead);
			_feedForward = new FeedForwardLayer(dModel, dFF);
			_norm1 = new LayerNormLayer(dModel);
			_norm2 = new LayerNormLayer(dModel);
			_tempBuffer = new float[dModel];
			_normOutput = new float[dModel];
			_ffnOutput = new float[dModel];
			_residualInput = new float[dModel];
			_gradTemp = new float[dModel];
			_gradNorm = new float[dModel];
			_gradFFN = new float[dModel];
			_gradResidual = new float[dModel];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Forward(ReadOnlySpan<float> input, Span<float> output)
		{
			input.CopyTo(_residualInput);
			_norm1.Forward(input, _normOutput);
			_feedForward.Forward(_normOutput, _ffnOutput);
			for (int i = 0; i < _dModel; i++)
			{
				output[i] = _ffnOutput[i] + input[i];
			}
			_norm2.Forward(output, _tempBuffer);
			_tempBuffer.AsSpan().CopyTo(output);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Backward(ReadOnlySpan<float> gradOutput, float learningRate = 0.001f)
		{
			_norm2.Backward(gradOutput, learningRate);
			float[] inputGrads = _norm2.InputGrads;
			inputGrads.AsSpan().CopyTo(_gradFFN.AsSpan());
			inputGrads.AsSpan().CopyTo(_gradResidual.AsSpan());
			_feedForward.Backward(_gradFFN, learningRate);
			float[] inputGradients = _feedForward.InputGradients;
			_norm1.Backward(inputGradients, learningRate);
			float[] inputGrads2 = _norm1.InputGrads;
			VectorMathNew.Add(_gradResidual.AsSpan(0, _dModel), inputGrads2, _gradResidual.AsSpan(0, _dModel));
			_norm2.ApplyGradients(learningRate);
			_norm1.ApplyGradients(learningRate);
		}

		public void ApplyGradients(float learningRate = 0.001f)
		{
			_feedForward.FC1.ApplyGradientsSGD(learningRate);
			_feedForward.FC2.ApplyGradientsSGD(learningRate);
			_norm1.ApplyGradients(learningRate);
			_norm2.ApplyGradients(learningRate);
		}
	}
}
