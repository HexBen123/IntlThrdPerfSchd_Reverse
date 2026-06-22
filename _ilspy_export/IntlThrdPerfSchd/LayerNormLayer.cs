using System;
using System.Runtime.CompilerServices;
using SimdLibrary;

namespace IntlThrdPerfSchd;

public class LayerNormLayer
{
	public readonly float[] Gamma;

	public readonly float[] Beta;

	public readonly int Dim;

	public readonly float[] GammaGrads;

	public readonly float[] BetaGrads;

	public readonly float[] InputGrads;

	private float[] _cachedInput;

	private float[] _cachedMean;

	private float[] _cachedInvStd;

	private float[] _cachedXNorm;

	private float[] _tempBuf;

	private bool _cached;

	public LayerNormLayer(int dim)
	{
		Dim = dim;
		Gamma = new float[dim];
		Beta = new float[dim];
		GammaGrads = new float[dim];
		BetaGrads = new float[dim];
		InputGrads = new float[dim];
		_cachedInput = new float[dim];
		_cachedMean = new float[1];
		_cachedInvStd = new float[1];
		_cachedXNorm = new float[dim];
		_tempBuf = new float[dim];
		_cached = false;
		for (int i = 0; i < dim; i++)
		{
			Gamma[i] = 1f;
			Beta[i] = 0f;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Forward(ReadOnlySpan<float> input, Span<float> output)
	{
		int length = input.Length;
		float num = 0f;
		for (int i = 0; i < length; i++)
		{
			num += input[i];
		}
		num /= (float)length;
		_cachedMean[0] = num;
		float num2 = 0f;
		for (int j = 0; j < length; j++)
		{
			float num3 = input[j] - num;
			num2 += num3 * num3;
		}
		num2 /= (float)length;
		float num4 = 1f / (float)Math.Sqrt(num2 + 1E-05f);
		_cachedInvStd[0] = num4;
		input.CopyTo(_cachedInput);
		for (int k = 0; k < length; k++)
		{
			_cachedXNorm[k] = (input[k] - num) * num4;
			output[k] = _cachedXNorm[k] * Gamma[k] + Beta[k];
		}
		_cached = true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Backward(ReadOnlySpan<float> gradOutput, float learningRate = 0.001f)
	{
		if (_cached)
		{
			int dim = Dim;
			_ = _cachedMean[0];
			float invStd = _cachedInvStd[0];
			Span<float> result = new Span<float>(GammaGrads, 0, dim);
			Span<float> destination = new Span<float>(BetaGrads, 0, dim);
			MathHelper.MultiplyElementwise(gradOutput.Slice(0, dim), new ReadOnlySpan<float>(_cachedXNorm, 0, dim), result);
			gradOutput.Slice(0, dim).CopyTo(destination);
			MathHelper.MultiplyElementwise(gradOutput.Slice(0, dim), new ReadOnlySpan<float>(Gamma, 0, dim), _tempBuf);
			MathHelper.Sum(_tempBuf.AsSpan(0, dim));
			MathHelper.MultiplyElementwise(gradOutput.Slice(0, dim), new ReadOnlySpan<float>(Gamma, 0, dim), _tempBuf);
			VectorMathNew.ComputeLayerNormInputGrad(xNorm: new ReadOnlySpan<float>(_cachedXNorm, 0, dim), gradOutput: gradOutput.Slice(0, dim), gamma: new ReadOnlySpan<float>(Gamma, 0, dim), invStd: invStd, result: InputGrads);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ApplyGradients(float learningRate = 0.001f)
	{
		VectorMathNew.MultiplyScalarInPlace(GammaGrads.AsSpan(0, Dim), 0f - learningRate);
		VectorMathNew.Add(Gamma.AsSpan(0, Dim), GammaGrads.AsSpan(0, Dim), Gamma.AsSpan(0, Dim));
		VectorMathNew.MultiplyScalarInPlace(BetaGrads.AsSpan(0, Dim), 0f - learningRate);
		VectorMathNew.Add(Beta.AsSpan(0, Dim), BetaGrads.AsSpan(0, Dim), Beta.AsSpan(0, Dim));
		MathHelper.Clear(GammaGrads.AsSpan(0, Dim));
		MathHelper.Clear(BetaGrads.AsSpan(0, Dim));
	}
}
