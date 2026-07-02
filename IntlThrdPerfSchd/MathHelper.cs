using System;
using System.Runtime.CompilerServices;
using SimdLibrary;

namespace IntlThrdPerfSchd
{
	public static class MathHelper
	{
		public const int SIMD_SIZE = 8;

		private static readonly Random _rng = new Random(42);

		public static void InitWeights(float[] weights, int fanIn, int fanOut)
		{
			int rows = fanOut;
			int cols = fanIn;
			for (int i = 0; i < weights.Length; i++)
			{
				weights[i] = (float)(_rng.NextDouble() * 2.0 - 1.0);
			}
			int num = Math.Min(rows, cols);
			for (int j = 0; j < rows; j++)
			{
				float num4;
				int num2 = 0;
				while (num2 < j && num2 < num)
				{
					float num3 = 0f;
					for (int k = 0; k < cols; k++)
					{
						num3 += weights[num2 * cols + k] * weights[j * cols + k];
					}
					for (int l = 0; l < cols; l++)
					{
						weights[j * cols + l] -= num3 * weights[num2 * cols + l];
					}
					num2++;
				}
				num4 = 0f;
				for (int m = 0; m < cols; m++)
				{
					num4 += weights[j * cols + m] * weights[j * cols + m];
				}
				num4 = (float)Math.Sqrt((double)num4);
				if (num4 > 1E-06f)
				{
					float num5 = 1f / num4;
					for (int n = 0; n < cols; n++)
					{
						weights[j * cols + n] *= num5;
					}
				}
			}
			float num6 = (float)Math.Sqrt(2.0 / (double)fanIn);
			for (int num7 = 0; num7 < weights.Length; num7++)
			{
				weights[num7] *= num6;
			}
		}

		public static void InitEmbeddingOrthogonal(float[] embeddings, int numVectors, int dim)
		{
			for (int i = 0; i < embeddings.Length; i++)
			{
				embeddings[i] = (float)(_rng.NextDouble() * 2.0 - 1.0);
			}
			int num = Math.Min(numVectors, dim);
			for (int j = 0; j < numVectors; j++)
			{
				for (int k = 0; k < j && k < num; k++)
				{
					float num2 = 0f;
					for (int l = 0; l < dim; l++)
					{
						num2 += embeddings[k * dim + l] * embeddings[j * dim + l];
					}
					for (int m = 0; m < dim; m++)
					{
						embeddings[j * dim + m] -= num2 * embeddings[k * dim + m];
					}
				}
				float num3 = 0f;
				for (int n = 0; n < dim; n++)
				{
					num3 += embeddings[j * dim + n] * embeddings[j * dim + n];
				}
				num3 = (float)Math.Sqrt(num3);
				if (num3 > 1E-06f)
				{
					float num4 = 1f / num3;
					for (int num5 = 0; num5 < dim; num5++)
					{
						embeddings[j * dim + num5] *= num4;
					}
				}
				float num6 = (float)Math.Sqrt(2.0 / (double)dim);
				for (int num7 = 0; num7 < dim; num7++)
				{
					embeddings[j * dim + num7] *= num6;
				}
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
			VectorMathNew.Softmax(input, output);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LayerNorm(ReadOnlySpan<float> input, ReadOnlySpan<float> gamma, ReadOnlySpan<float> beta, Span<float> output, float epsilon = 1E-05f)
		{
			float[] array = new float[input.Length];
			VectorMathNew.LayerNormForward(input, gamma, beta, output, array, epsilon);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Linear(ReadOnlySpan<float> input, ReadOnlySpan<float> weights, ReadOnlySpan<float> bias, Span<float> result, int inDim, int outDim)
		{
			MatrixOperations.MatrixVectorMultiply(weights, input, result, outDim, inDim);
			VectorMathNew.Add(result, bias.Slice(0, outDim), result);
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
			VectorMathNew.MultiplyElementwise(a, b, result);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Sum(ReadOnlySpan<float> array)
		{
			return VectorMathNew.Sum(array);
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
