using System;
using System.Runtime.CompilerServices;
using SimdLibrary;

namespace IntlThrdPerfSchd
{
	public class LinearLayer
	{
		public readonly float[] Weights;

		public readonly float[] Bias;

		public readonly int InDim;

		public readonly int OutDim;

		public readonly float[] WeightGrads;

		public readonly float[] BiasGrads;

		public readonly float[] InputGrads;

		public readonly float[] WeightMom;

		public readonly float[] BiasMom;

		private float[] _cachedInput;

		private bool _inputCached;

		private readonly float[] _tempInputGrad;

		public LinearLayer(int inDim, int outDim)
		{
			InDim = inDim;
			OutDim = outDim;
			Weights = new float[outDim * inDim];
			Bias = new float[outDim];
			WeightGrads = new float[outDim * inDim];
			BiasGrads = new float[outDim];
			InputGrads = new float[inDim];
			WeightMom = new float[outDim * inDim];
			BiasMom = new float[outDim];
			_cachedInput = new float[inDim];
			_tempInputGrad = new float[inDim];
			_inputCached = false;
			MathHelper.InitWeights(Weights, inDim, OutDim);
			MathHelper.Clear(Bias.AsSpan(0, OutDim));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Forward(ReadOnlySpan<float> input, Span<float> output)
		{
			input.CopyTo(_cachedInput);
			_inputCached = true;
			MathHelper.Linear(input, Weights, Bias, output, InDim, OutDim);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Backward(ReadOnlySpan<float> gradOutput, float learningRate = 0.001f, bool accumulateGrad = false)
		{
			if (_inputCached)
			{
				MathHelper.ComputeWeightGrad(gradOutput, _cachedInput, WeightGrads, OutDim, InDim, accumulateGrad);
				if (accumulateGrad)
				{
					VectorMathNew.Add(BiasGrads.AsSpan(0, OutDim), gradOutput, BiasGrads.AsSpan(0, OutDim));
				}
				else
				{
					gradOutput.CopyTo(BiasGrads.AsSpan(0, OutDim));
				}
				if (accumulateGrad)
				{
					MathHelper.ComputeInputGrad(gradOutput, Weights, _tempInputGrad, OutDim, InDim);
					VectorMathNew.Add(InputGrads.AsSpan(0, InDim), _tempInputGrad.AsSpan(0, InDim), InputGrads.AsSpan(0, InDim));
				}
				else
				{
					MathHelper.ComputeInputGrad(gradOutput, Weights, InputGrads, OutDim, InDim);
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ApplyGradients(float learningRate = 0.001f, float beta1 = 0.9f, float beta2 = 0.999f, float epsilon = 1E-08f, int t = 1)
		{
			int num = OutDim * InDim;
			for (int i = 0; i < num; i++)
			{
				float num2 = WeightGrads[i];
				WeightMom[i] = beta1 * WeightMom[i] + (1f - beta1) * num2;
				float num3 = beta2 * 0f + (1f - beta2) * num2 * num2;
				float num4 = WeightMom[i] / (1f - (float)Math.Pow(beta1, t));
				float num5 = num3 / (1f - (float)Math.Pow(beta2, t));
				Weights[i] -= learningRate * num4 / ((float)Math.Sqrt(num5) + epsilon);
			}
			for (int j = 0; j < OutDim; j++)
			{
				float num6 = BiasGrads[j];
				BiasMom[j] = beta1 * BiasMom[j] + (1f - beta1) * num6;
				float num7 = beta2 * 0f + (1f - beta2) * num6 * num6;
				float num8 = BiasMom[j] / (1f - (float)Math.Pow(beta1, t));
				float num9 = num7 / (1f - (float)Math.Pow(beta2, t));
				Bias[j] -= learningRate * num8 / ((float)Math.Sqrt(num9) + epsilon);
			}
			MathHelper.Clear(WeightGrads.AsSpan(0, num));
			MathHelper.Clear(BiasGrads.AsSpan(0, OutDim));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ApplyGradientsSGD(float learningRate = 0.001f, float maxGradNorm = 1f)
		{
			int length = OutDim * InDim;
			Span<float> span = WeightGrads.AsSpan(0, length);
			float num = VectorMathNew.EuclideanNorm(span);
			if (num > maxGradNorm)
			{
				float scalar = maxGradNorm / num;
				VectorMathNew.MultiplyScalarInPlace(span, scalar);
			}
			Span<float> span2 = BiasGrads.AsSpan(0, OutDim);
			float num2 = VectorMathNew.EuclideanNorm(span2);
			if (num2 > maxGradNorm)
			{
				float scalar2 = maxGradNorm / num2;
				VectorMathNew.MultiplyScalarInPlace(span2, scalar2);
			}
			Span<float> span3 = Weights.AsSpan(0, length);
			VectorMathNew.MultiplyScalarInPlace(span, 0f - learningRate);
			VectorMathNew.Add(span3, span, span3);
			VectorMathNew.MultiplyScalarInPlace(span2, 0f - learningRate);
			VectorMathNew.Add(Bias.AsSpan(0, OutDim), span2, Bias.AsSpan(0, OutDim));
			MathHelper.Clear(WeightGrads.AsSpan(0, length));
			MathHelper.Clear(BiasGrads.AsSpan(0, OutDim));
		}
	}
}
