using System;
using System.Runtime.CompilerServices;
using SimdLibrary;

namespace IntlThrdPerfSchd
{
	public static class MathHelper
	{
		public const int SIMD_SIZE = 8;

		private static readonly Random _rng = new Random(42);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void InitWeights(float[] weights, int fanIn, int fanOut)
		{
			float num = (float)Math.Sqrt(2.0 / (double)(fanIn + fanOut));
			for (int i = 0; i < weights.Length; i++)
			{
				weights[i] = (float)(_rng.NextDouble() * 2.0 - 1.0) * num;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Add(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> result)
		{
			VectorMathNew.Add(a, b, result);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Scale(Span<float> data, float scale)
		{
			VectorMathNew.MultiplyScalar(data, scale, data);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float DotProduct(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
		{
			int length = Math.Min(a.Length, b.Length);
			return VectorMathNew.DotProduct(a.Slice(0, length), b.Slice(0, length));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Softmax(ReadOnlySpan<float> input, Span<float> output)
		{
			float num = VectorMathNew.Max(input);
			float num2 = 0f;
			for (int i = 0; i < input.Length; i++)
			{
				output[i] = (float)Math.Exp(input[i] - num);
				num2 += output[i];
			}
			float scale = 1f / num2;
			Scale(output, scale);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LayerNorm(ReadOnlySpan<float> input, ReadOnlySpan<float> gamma, ReadOnlySpan<float> beta, Span<float> output, float epsilon = 1E-05f)
		{
			int length = input.Length;
			float num = VectorMathNew.Mean(input);
			float num2 = VectorMathNew.Variance(input);
			float num3 = 1f / (float)Math.Sqrt(num2 + epsilon);
			for (int i = 0; i < length; i++)
			{
				output[i] = (input[i] - num) * num3 * gamma[i] + beta[i];
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Linear(ReadOnlySpan<float> input, ReadOnlySpan<float> weights, ReadOnlySpan<float> bias, Span<float> result, int inDim, int outDim)
		{
			MatrixOperations.MatrixVectorMultiply(weights, input, result, outDim, inDim);
			for (int i = 0; i < outDim; i++)
			{
				result[i] += bias[i];
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ComputeWeightGrad(ReadOnlySpan<float> gradOutput, ReadOnlySpan<float> input, Span<float> weightGrad, int outDim, int inDim, bool accumulate = false)
		{
			for (int i = 0; i < outDim; i++)
			{
				float num = gradOutput[i];
				int num2 = i * inDim;
				if (accumulate)
				{
					for (int j = 0; j < inDim; j++)
					{
						weightGrad[num2 + j] += num * input[j];
					}
				}
				else
				{
					for (int k = 0; k < inDim; k++)
					{
						weightGrad[num2 + k] = num * input[k];
					}
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ComputeInputGrad(ReadOnlySpan<float> gradOutput, ReadOnlySpan<float> weights, Span<float> inputGrad, int outDim, int inDim)
		{
			for (int i = 0; i < inDim; i++)
			{
				float num = 0f;
				for (int j = 0; j < outDim; j++)
				{
					num += gradOutput[j] * weights[j * inDim + i];
				}
				inputGrad[i] = num;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void MultiplyElementwise(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> result)
		{
			int num = Math.Min(Math.Min(a.Length, b.Length), result.Length);
			for (int i = 0; i < num; i++)
			{
				result[i] = a[i] * b[i];
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Sum(ReadOnlySpan<float> array)
		{
			float num = 0f;
			for (int i = 0; i < array.Length; i++)
			{
				num += array[i];
			}
			return num;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Clear(Span<float> array)
		{
			VectorMathNew.Zero(array);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Clear(Span<int> array)
		{
			VectorMathNew.ZeroInt(array);
		}
	}
}
