using System;
using System.Runtime.CompilerServices;
using SimdLibrary;

namespace IntlThrdPerfSchd
{
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
			input.CopyTo(_cachedInput);
			var (num, num2) = VectorMathNew.LayerNormForward(input, Gamma, Beta, output, _cachedXNorm);
			_cachedMean[0] = num;
			_cachedInvStd[0] = num2;
			_cached = true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Backward(ReadOnlySpan<float> gradOutput, float learningRate = 0.001f, bool accumulateGrad = false)
		{
			if (!_cached)
			{
				return;
			}
			int dim = Dim;
			_ = _cachedMean[0];
			float invStd = _cachedInvStd[0];
			if (accumulateGrad)
			{
				for (int i = 0; i < dim; i++)
				{
					GammaGrads[i] += gradOutput[i] * _cachedXNorm[i];
					BetaGrads[i] += gradOutput[i];
				}
			}
			else
			{
				Span<float> result = new Span<float>(GammaGrads, 0, dim);
				Span<float> destination = new Span<float>(BetaGrads, 0, dim);
				MathHelper.MultiplyElementwise(gradOutput.Slice(0, dim), new ReadOnlySpan<float>(_cachedXNorm, 0, dim), result);
				gradOutput.Slice(0, dim).CopyTo(destination);
			}
			VectorMathNew.ComputeLayerNormInputGrad(gradOutput.Slice(0, dim), new ReadOnlySpan<float>(Gamma, 0, dim), new ReadOnlySpan<float>(_cachedXNorm, 0, dim), invStd, InputGrads);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ApplyGradients(float learningRate = 0.001f)
		{
			VectorMathNew.MultiplyScalarInPlace(GammaGrads.AsSpan(0, Dim), -learningRate);
			VectorMathNew.Add(Gamma.AsSpan(0, Dim), GammaGrads.AsSpan(0, Dim), Gamma.AsSpan(0, Dim));
			VectorMathNew.MultiplyScalarInPlace(BetaGrads.AsSpan(0, Dim), -learningRate);
			VectorMathNew.Add(Beta.AsSpan(0, Dim), BetaGrads.AsSpan(0, Dim), Beta.AsSpan(0, Dim));
			MathHelper.Clear(GammaGrads.AsSpan(0, Dim));
			MathHelper.Clear(BetaGrads.AsSpan(0, Dim));
		}
	}
}
