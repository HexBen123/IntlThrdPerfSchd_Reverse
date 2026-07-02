using System;
using System.Runtime.CompilerServices;
using SimdLibrary;

namespace IntlThrdPerfSchd;

public class FeedForwardLayer
{
	private readonly LinearLayer _fc1;

	private readonly LinearLayer _fc2;

	private readonly int _dModel;

	private readonly int _dFF;

	private readonly float[] _hidden;

	private readonly float[] _hiddenAfterRelu;

	private readonly float[] _gradHidden;

	private readonly float[] _gradInput;

	public LinearLayer FC1 => _fc1;

	public LinearLayer FC2 => _fc2;

	public float[] InputGradients => _fc1.InputGrads;

	public FeedForwardLayer(int dModel, int dFF = 256)
	{
		_dModel = dModel;
		_dFF = dFF;
		_fc1 = new LinearLayer(dModel, dFF);
		_fc2 = new LinearLayer(dFF, dModel);
		_hidden = new float[dFF];
		_hiddenAfterRelu = new float[dFF];
		_gradHidden = new float[dFF];
		_gradInput = new float[dModel];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Forward(ReadOnlySpan<float> input, Span<float> output)
	{
		_fc1.Forward(input, _hidden);
		VectorMathNew.Relu(_hidden, _hiddenAfterRelu);
		_fc2.Forward(_hiddenAfterRelu, output);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Backward(ReadOnlySpan<float> gradOutput, float learningRate = 0.001f)
	{
		_fc2.Backward(gradOutput, learningRate);
		float[] inputGrads = _fc2.InputGrads;
		VectorMathNew.ReluGradient(new ReadOnlySpan<float>(_hidden, 0, _dFF), new ReadOnlySpan<float>(inputGrads, 0, _dFF), new Span<float>(_gradHidden, 0, _dFF));
		_fc1.Backward(_gradHidden, learningRate);
	}
}
