using System;
using System.Runtime.CompilerServices;
using SimdLibrary;

namespace IntlThrdPerfSchd
{
	// Token: 0x02000017 RID: 23
	public static class MathHelper
	{
		// Token: 0x0600018A RID: 394 RVA: 0x00013E4C File Offset: 0x0001204C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void InitWeights(float[] weights, int fanIn, int fanOut)
		{
			float num = (float)Math.Sqrt(2.0 / (double)(fanIn + fanOut));
			for (int i = 0; i < weights.Length; i++)
			{
				weights[i] = (float)(MathHelper._rng.NextDouble() * 2.0 - 1.0) * num;
			}
		}

		// Token: 0x0600018B RID: 395 RVA: 0x00013EA0 File Offset: 0x000120A0
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Add(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> result)
		{
			VectorMathNew.Add(a, b, result);
		}

		// Token: 0x0600018C RID: 396 RVA: 0x00013EAA File Offset: 0x000120AA
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Scale(Span<float> data, float scale)
		{
			VectorMathNew.MultiplyScalar(data, scale, data);
		}

		// Token: 0x0600018D RID: 397 RVA: 0x00013EBC File Offset: 0x000120BC
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float DotProduct(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
		{
			int num = Math.Min(a.Length, b.Length);
			return VectorMathNew.DotProduct(a.Slice(0, num), b.Slice(0, num));
		}

		// Token: 0x0600018E RID: 398 RVA: 0x00013EF4 File Offset: 0x000120F4
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void Softmax(ReadOnlySpan<float> input, Span<float> output)
		{
			float num = VectorMathNew.Max(input);
			float num2 = 0f;
			for (int i = 0; i < input.Length; i++)
			{
				*output[i] = (float)Math.Exp((double)(*input[i] - num));
				num2 += *output[i];
			}
			float num3 = 1f / num2;
			MathHelper.Scale(output, num3);
		}

		// Token: 0x0600018F RID: 399 RVA: 0x00013F58 File Offset: 0x00012158
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void LayerNorm(ReadOnlySpan<float> input, ReadOnlySpan<float> gamma, ReadOnlySpan<float> beta, Span<float> output, float epsilon = 1E-05f)
		{
			int length = input.Length;
			float num = VectorMathNew.Mean(input);
			float num2 = VectorMathNew.Variance(input);
			float num3 = 1f / (float)Math.Sqrt((double)(num2 + epsilon));
			for (int i = 0; i < length; i++)
			{
				*output[i] = (*input[i] - num) * num3 * *gamma[i] + *beta[i];
			}
		}

		// Token: 0x06000190 RID: 400 RVA: 0x00013FCC File Offset: 0x000121CC
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void Linear(ReadOnlySpan<float> input, ReadOnlySpan<float> weights, ReadOnlySpan<float> bias, Span<float> result, int inDim, int outDim)
		{
			MatrixOperations.MatrixVectorMultiply(weights, input, result, outDim, inDim);
			for (int i = 0; i < outDim; i++)
			{
				*result[i] += *bias[i];
			}
		}

		// Token: 0x06000191 RID: 401 RVA: 0x00014008 File Offset: 0x00012208
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

		// Token: 0x06000192 RID: 402 RVA: 0x00014088 File Offset: 0x00012288
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

		// Token: 0x06000193 RID: 403 RVA: 0x000140DC File Offset: 0x000122DC
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void MultiplyElementwise(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> result)
		{
			int num = Math.Min(Math.Min(a.Length, b.Length), result.Length);
			for (int i = 0; i < num; i++)
			{
				*result[i] = *a[i] * *b[i];
			}
		}

		// Token: 0x06000194 RID: 404 RVA: 0x00014134 File Offset: 0x00012334
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static float Sum(ReadOnlySpan<float> array)
		{
			float num = 0f;
			for (int i = 0; i < array.Length; i++)
			{
				num += *array[i];
			}
			return num;
		}

		// Token: 0x06000195 RID: 405 RVA: 0x00014166 File Offset: 0x00012366
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Clear(Span<float> array)
		{
			VectorMathNew.Zero(array);
		}

		// Token: 0x06000196 RID: 406 RVA: 0x0001416E File Offset: 0x0001236E
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Clear(Span<int> array)
		{
			VectorMathNew.ZeroInt(array);
		}

		// Token: 0x040003D8 RID: 984
		public const int SIMD_SIZE = 8;

		// Token: 0x040003D9 RID: 985
		private static readonly Random _rng = new Random(42);
	}
}
