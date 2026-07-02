using System;
using System.Runtime.CompilerServices;
using SimdLibrary;

namespace IntlThrdPerfSchd
{
	// Token: 0x02000019 RID: 25
	public static class MathHelper
	{
		// Token: 0x06000199 RID: 409 RVA: 0x00016190 File Offset: 0x00014390
		public static void InitWeights(float[] weights, int fanIn, int fanOut)
		{
			for (int i = 0; i < weights.Length; i++)
			{
				weights[i] = (float)(MathHelper._rng.NextDouble() * 2.0 - 1.0);
			}
			int num = Math.Min(fanOut, fanIn);
			for (int j = 0; j < fanOut; j++)
			{
				int num2 = 0;
				while (num2 < j && num2 < num)
				{
					float num3 = 0f;
					for (int k = 0; k < fanIn; k++)
					{
						num3 += weights[num2 * fanIn + k] * weights[j * fanIn + k];
					}
					for (int l = 0; l < fanIn; l++)
					{
						weights[j * fanIn + l] -= num3 * weights[num2 * fanIn + l];
					}
					num2++;
				}
				float num4 = 0f;
				for (int m = 0; m < fanIn; m++)
				{
					num4 += weights[j * fanIn + m] * weights[j * fanIn + m];
				}
				num4 = (float)Math.Sqrt((double)num4);
				if (num4 > 1E-06f)
				{
					float num5 = 1f / num4;
					for (int n = 0; n < fanIn; n++)
					{
						weights[j * fanIn + n] *= num5;
					}
				}
			}
			float num6 = (float)Math.Sqrt(2.0 / (double)fanIn);
			for (int num7 = 0; num7 < weights.Length; num7++)
			{
				weights[num7] *= num6;
			}
		}

		// Token: 0x0600019A RID: 410 RVA: 0x00016308 File Offset: 0x00014508
		public static void InitEmbeddingOrthogonal(float[] embeddings, int numVectors, int dim)
		{
			for (int i = 0; i < embeddings.Length; i++)
			{
				embeddings[i] = (float)(MathHelper._rng.NextDouble() * 2.0 - 1.0);
			}
			int num = Math.Min(numVectors, dim);
			for (int j = 0; j < numVectors; j++)
			{
				int num2 = 0;
				while (num2 < j && num2 < num)
				{
					float num3 = 0f;
					for (int k = 0; k < dim; k++)
					{
						num3 += embeddings[num2 * dim + k] * embeddings[j * dim + k];
					}
					for (int l = 0; l < dim; l++)
					{
						embeddings[j * dim + l] -= num3 * embeddings[num2 * dim + l];
					}
					num2++;
				}
				float num4 = 0f;
				for (int m = 0; m < dim; m++)
				{
					num4 += embeddings[j * dim + m] * embeddings[j * dim + m];
				}
				num4 = (float)Math.Sqrt((double)num4);
				if (num4 > 1E-06f)
				{
					float num5 = 1f / num4;
					for (int n = 0; n < dim; n++)
					{
						embeddings[j * dim + n] *= num5;
					}
				}
				float num6 = (float)Math.Sqrt(2.0 / (double)dim);
				for (int num7 = 0; num7 < dim; num7++)
				{
					embeddings[j * dim + num7] *= num6;
				}
			}
		}

		// Token: 0x0600019B RID: 411 RVA: 0x0001646A File Offset: 0x0001466A
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Add(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> result)
		{
			VectorMathNew.Add(a, b, result);
		}

		// Token: 0x0600019C RID: 412 RVA: 0x00016474 File Offset: 0x00014674
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Scale(Span<float> data, float scale)
		{
			VectorMathNew.MultiplyScalar(data, scale, data);
		}

		// Token: 0x0600019D RID: 413 RVA: 0x00016484 File Offset: 0x00014684
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float DotProduct(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
		{
			int num = Math.Min(a.Length, b.Length);
			return VectorMathNew.DotProduct(a.Slice(0, num), b.Slice(0, num));
		}

		// Token: 0x0600019E RID: 414 RVA: 0x000164BC File Offset: 0x000146BC
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Softmax(ReadOnlySpan<float> input, Span<float> output)
		{
			VectorMathNew.Softmax(input, output);
		}

		// Token: 0x0600019F RID: 415 RVA: 0x000164C8 File Offset: 0x000146C8
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LayerNorm(ReadOnlySpan<float> input, ReadOnlySpan<float> gamma, ReadOnlySpan<float> beta, Span<float> output, float epsilon = 1E-05f)
		{
			float[] array = new float[input.Length];
			VectorMathNew.LayerNormForward(input, gamma, beta, output, array, epsilon);
		}

		// Token: 0x060001A0 RID: 416 RVA: 0x000164F4 File Offset: 0x000146F4
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Linear(ReadOnlySpan<float> input, ReadOnlySpan<float> weights, ReadOnlySpan<float> bias, Span<float> result, int inDim, int outDim)
		{
			MatrixOperations.MatrixVectorMultiply(weights, input, result, outDim, inDim);
			VectorMathNew.Add(result, bias.Slice(0, outDim), result);
		}

		// Token: 0x060001A1 RID: 417 RVA: 0x00016518 File Offset: 0x00014718
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void ComputeWeightGrad(ReadOnlySpan<float> gradOutput, ReadOnlySpan<float> input, Span<float> weightGrad, int outDim, int inDim, bool accumulate = false)
		{
			for (int i = 0; i < outDim; i++)
			{
				float num = *gradOutput[i];
				int num2 = i * inDim;
				if (accumulate)
				{
					for (int j = 0; j < inDim; j++)
					{
						*weightGrad[num2 + j] += num * *input[j];
					}
				}
				else
				{
					for (int k = 0; k < inDim; k++)
					{
						*weightGrad[num2 + k] = num * *input[k];
					}
				}
			}
		}

		// Token: 0x060001A2 RID: 418 RVA: 0x00016598 File Offset: 0x00014798
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void ComputeInputGrad(ReadOnlySpan<float> gradOutput, ReadOnlySpan<float> weights, Span<float> inputGrad, int outDim, int inDim)
		{
			for (int i = 0; i < inDim; i++)
			{
				float num = 0f;
				for (int j = 0; j < outDim; j++)
				{
					num += *gradOutput[j] * *weights[j * inDim + i];
				}
				*inputGrad[i] = num;
			}
		}

		// Token: 0x060001A3 RID: 419 RVA: 0x000165E9 File Offset: 0x000147E9
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void MultiplyElementwise(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> result)
		{
			VectorMathNew.MultiplyElementwise(a, b, result);
		}

		// Token: 0x060001A4 RID: 420 RVA: 0x000165F3 File Offset: 0x000147F3
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Sum(ReadOnlySpan<float> array)
		{
			return VectorMathNew.Sum(array);
		}

		// Token: 0x060001A5 RID: 421 RVA: 0x000165FB File Offset: 0x000147FB
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Clear(Span<float> array)
		{
			VectorMathNew.Zero(array);
		}

		// Token: 0x060001A6 RID: 422 RVA: 0x00016603 File Offset: 0x00014803
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Clear(Span<int> array)
		{
			VectorMathNew.ZeroInt(array);
		}

		// Token: 0x04000439 RID: 1081
		public const int SIMD_SIZE = 8;

		// Token: 0x0400043A RID: 1082
		private static readonly Random _rng = new Random(42);
	}
}
