using System;
using System.Runtime.CompilerServices;
using SimdLibrary;

namespace IntlThrdPerfSchd;

public class ThreadFFNBlock
{
	private const int D_MODEL = 64;

	private const int D_FF = 256;

	private const float GRADIENT_CLIP_THRESHOLD = 1f;

	private readonly FeedForwardLayer _feedForward;

	private readonly LayerNormLayer _norm1;

	private readonly LayerNormLayer _norm2;

	private readonly float[] _norm1Output;

	private readonly float[] _ffnOutput;

	private readonly float[] _gradFFN;

	private readonly float[] _gradResidual;

	private readonly float[] _gradInput;

	public FeedForwardLayer FeedForward => _feedForward;

	public LayerNormLayer Norm1 => _norm1;

	public LayerNormLayer Norm2 => _norm2;

	public float[] InputGradients => _gradInput;

	public ThreadFFNBlock()
	{
		_feedForward = new FeedForwardLayer(64);
		_norm1 = new LayerNormLayer(64);
		_norm2 = new LayerNormLayer(64);
		_norm1Output = new float[64];
		_ffnOutput = new float[64];
		_gradFFN = new float[64];
		_gradResidual = new float[64];
		_gradInput = new float[64];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Forward(ReadOnlySpan<float> input, Span<float> output)
	{
		_norm1.Forward(input, _norm1Output);
		_feedForward.Forward(_norm1Output, _ffnOutput);
		VectorMathNew.Add(input, _ffnOutput, output);
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
			_norm1.Backward(inputGradients, learningRate);
			float[] inputGrads2 = _norm1.InputGrads;
			VectorMathNew.Add(_gradResidual, inputGrads2, _gradInput);
			ClipGradientIfNeeded(_gradInput);
			_norm2.ApplyGradients(learningRate);
			_norm1.ApplyGradients(learningRate);
		}
	}

	public void ApplyGradients(float learningRate = 0.001f)
	{
		if (AreWeightsValid())
		{
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
