using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace SimdLibrary
{
	// Token: 0x02000020 RID: 32
	public static class VectorMath
	{
		// Token: 0x1700003E RID: 62
		// (get) Token: 0x060001DC RID: 476 RVA: 0x00016729 File Offset: 0x00014929
		public static bool IsSimdSupported
		{
			get
			{
				return Vector.IsHardwareAccelerated;
			}
		}

		// Token: 0x1700003F RID: 63
		// (get) Token: 0x060001DD RID: 477 RVA: 0x00016730 File Offset: 0x00014930
		public static int VectorSize
		{
			get
			{
				return Vector<float>.Count;
			}
		}

		// Token: 0x060001DE RID: 478 RVA: 0x00016738 File Offset: 0x00014938
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void Add(ReadOnlySpan<float> left, ReadOnlySpan<float> right, Span<float> result)
		{
			if (left.Length != right.Length || left.Length != result.Length)
			{
				throw new ArgumentException("数组长度必须相同");
			}
			int length = left.Length;
			if (VectorMath.IsSimdSupported && length >= VectorMath.VectorSize)
			{
				int num = length - length % VectorMath.VectorSize;
				fixed (float* pinnableReference = left.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = right.GetPinnableReference())
					{
						float* ptr2 = pinnableReference2;
						fixed (float* pinnableReference3 = result.GetPinnableReference())
						{
							float* ptr3 = pinnableReference3;
							for (int i = 0; i < num; i += VectorMath.VectorSize)
							{
								Vector<float> vector = *(Vector<float>*)(ptr + i);
								Vector<float> vector2 = *(Vector<float>*)(ptr2 + i);
								*(Vector<float>*)(ptr3 + i) = vector + vector2;
							}
						}
					}
				}
				for (int j = num; j < length; j++)
				{
					*result[j] = *left[j] + *right[j];
				}
				return;
			}
			for (int k = 0; k < length; k++)
			{
				*result[k] = *left[k] + *right[k];
			}
		}

		// Token: 0x060001DF RID: 479 RVA: 0x00016870 File Offset: 0x00014A70
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void Subtract(ReadOnlySpan<float> left, ReadOnlySpan<float> right, Span<float> result)
		{
			if (left.Length != right.Length || left.Length != result.Length)
			{
				throw new ArgumentException("数组长度必须相同");
			}
			int length = left.Length;
			if (VectorMath.IsSimdSupported && length >= VectorMath.VectorSize)
			{
				int num = length - length % VectorMath.VectorSize;
				fixed (float* pinnableReference = left.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = right.GetPinnableReference())
					{
						float* ptr2 = pinnableReference2;
						fixed (float* pinnableReference3 = result.GetPinnableReference())
						{
							float* ptr3 = pinnableReference3;
							for (int i = 0; i < num; i += VectorMath.VectorSize)
							{
								Vector<float> vector = *(Vector<float>*)(ptr + i);
								Vector<float> vector2 = *(Vector<float>*)(ptr2 + i);
								*(Vector<float>*)(ptr3 + i) = vector - vector2;
							}
						}
					}
				}
				for (int j = num; j < length; j++)
				{
					*result[j] = *left[j] - *right[j];
				}
				return;
			}
			for (int k = 0; k < length; k++)
			{
				*result[k] = *left[k] - *right[k];
			}
		}

		// Token: 0x060001E0 RID: 480 RVA: 0x000169A8 File Offset: 0x00014BA8
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void Multiply(ReadOnlySpan<float> left, ReadOnlySpan<float> right, Span<float> result)
		{
			if (left.Length != right.Length || left.Length != result.Length)
			{
				throw new ArgumentException("数组长度必须相同");
			}
			int length = left.Length;
			if (VectorMath.IsSimdSupported && length >= VectorMath.VectorSize)
			{
				int num = length - length % VectorMath.VectorSize;
				fixed (float* pinnableReference = left.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = right.GetPinnableReference())
					{
						float* ptr2 = pinnableReference2;
						fixed (float* pinnableReference3 = result.GetPinnableReference())
						{
							float* ptr3 = pinnableReference3;
							for (int i = 0; i < num; i += VectorMath.VectorSize)
							{
								Vector<float> vector = *(Vector<float>*)(ptr + i);
								Vector<float> vector2 = *(Vector<float>*)(ptr2 + i);
								*(Vector<float>*)(ptr3 + i) = vector * vector2;
							}
						}
					}
				}
				for (int j = num; j < length; j++)
				{
					*result[j] = *left[j] * *right[j];
				}
				return;
			}
			for (int k = 0; k < length; k++)
			{
				*result[k] = *left[k] * *right[k];
			}
		}

		// Token: 0x060001E1 RID: 481 RVA: 0x00016AE0 File Offset: 0x00014CE0
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void Divide(ReadOnlySpan<float> left, ReadOnlySpan<float> right, Span<float> result)
		{
			if (left.Length != right.Length || left.Length != result.Length)
			{
				throw new ArgumentException("数组长度必须相同");
			}
			int length = left.Length;
			if (VectorMath.IsSimdSupported && length >= VectorMath.VectorSize)
			{
				int num = length - length % VectorMath.VectorSize;
				fixed (float* pinnableReference = left.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = right.GetPinnableReference())
					{
						float* ptr2 = pinnableReference2;
						fixed (float* pinnableReference3 = result.GetPinnableReference())
						{
							float* ptr3 = pinnableReference3;
							for (int i = 0; i < num; i += VectorMath.VectorSize)
							{
								Vector<float> vector = *(Vector<float>*)(ptr + i);
								Vector<float> vector2 = *(Vector<float>*)(ptr2 + i);
								*(Vector<float>*)(ptr3 + i) = vector / vector2;
							}
						}
					}
				}
				for (int j = num; j < length; j++)
				{
					*result[j] = *left[j] / *right[j];
				}
				return;
			}
			for (int k = 0; k < length; k++)
			{
				*result[k] = *left[k] / *right[k];
			}
		}

		// Token: 0x060001E2 RID: 482 RVA: 0x00016C18 File Offset: 0x00014E18
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void MultiplyScalar(ReadOnlySpan<float> array, float scalar, Span<float> result)
		{
			if (array.Length != result.Length)
			{
				throw new ArgumentException("数组长度必须相同");
			}
			int length = array.Length;
			if (VectorMath.IsSimdSupported && length >= VectorMath.VectorSize)
			{
				Vector<float> vector = new Vector<float>(scalar);
				int num = length - length % VectorMath.VectorSize;
				fixed (float* pinnableReference = array.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = result.GetPinnableReference())
					{
						float* ptr2 = pinnableReference2;
						for (int i = 0; i < num; i += VectorMath.VectorSize)
						{
							Vector<float> vector2 = *(Vector<float>*)(ptr + i);
							*(Vector<float>*)(ptr2 + i) = vector2 * vector;
						}
					}
				}
				for (int j = num; j < length; j++)
				{
					*result[j] = *array[j] * scalar;
				}
				return;
			}
			for (int k = 0; k < length; k++)
			{
				*result[k] = *array[k] * scalar;
			}
		}

		// Token: 0x060001E3 RID: 483 RVA: 0x00016D18 File Offset: 0x00014F18
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void MultiplyScalarInPlace(Span<float> array, float scalar)
		{
			int length = array.Length;
			if (VectorMath.IsSimdSupported && length >= VectorMath.VectorSize)
			{
				Vector<float> vector = new Vector<float>(scalar);
				int num = length - length % VectorMath.VectorSize;
				fixed (float* pinnableReference = array.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					for (int i = 0; i < num; i += VectorMath.VectorSize)
					{
						Vector<float> vector2 = *(Vector<float>*)(ptr + i);
						*(Vector<float>*)(ptr + i) = vector2 * vector;
					}
				}
				for (int j = num; j < length; j++)
				{
					*array[j] *= scalar;
				}
				return;
			}
			for (int k = 0; k < length; k++)
			{
				*array[k] *= scalar;
			}
		}

		// Token: 0x060001E4 RID: 484 RVA: 0x00016DD8 File Offset: 0x00014FD8
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static float Sum(ReadOnlySpan<float> array)
		{
			int length = array.Length;
			if (length == 0)
			{
				return 0f;
			}
			float num = 0f;
			if (VectorMath.IsSimdSupported && length >= VectorMath.VectorSize)
			{
				Vector<float> vector = Vector<float>.Zero;
				int num2 = length - length % VectorMath.VectorSize;
				fixed (float* pinnableReference = array.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					for (int i = 0; i < num2; i += VectorMath.VectorSize)
					{
						vector += *(Vector<float>*)(ptr + i);
					}
				}
				for (int j = 0; j < VectorMath.VectorSize; j++)
				{
					num += vector[j];
				}
				for (int k = num2; k < length; k++)
				{
					num += *array[k];
				}
			}
			else
			{
				for (int l = 0; l < length; l++)
				{
					num += *array[l];
				}
			}
			return num;
		}

		// Token: 0x060001E5 RID: 485 RVA: 0x00016EB8 File Offset: 0x000150B8
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static float DotProduct(ReadOnlySpan<float> left, ReadOnlySpan<float> right)
		{
			int length = left.Length;
			if (length != right.Length)
			{
				throw new ArgumentException("数组长度必须相同");
			}
			float num = 0f;
			if (VectorMath.IsSimdSupported && length >= VectorMath.VectorSize)
			{
				Vector<float> vector = Vector<float>.Zero;
				int num2 = length - length % VectorMath.VectorSize;
				fixed (float* pinnableReference = left.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = right.GetPinnableReference())
					{
						float* ptr2 = pinnableReference2;
						for (int i = 0; i < num2; i += VectorMath.VectorSize)
						{
							Vector<float> vector2 = *(Vector<float>*)(ptr + i);
							Vector<float> vector3 = *(Vector<float>*)(ptr2 + i);
							vector += vector2 * vector3;
						}
					}
				}
				for (int j = 0; j < VectorMath.VectorSize; j++)
				{
					num += vector[j];
				}
				for (int k = num2; k < length; k++)
				{
					num += *left[k] * *right[k];
				}
			}
			else
			{
				for (int l = 0; l < length; l++)
				{
					num += *left[l] * *right[l];
				}
			}
			return num;
		}

		// Token: 0x060001E6 RID: 486 RVA: 0x00016FE8 File Offset: 0x000151E8
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static float EuclideanDistance(ReadOnlySpan<float> left, ReadOnlySpan<float> right)
		{
			int length = left.Length;
			if (length != right.Length)
			{
				throw new ArgumentException("数组长度必须相同");
			}
			float num = 0f;
			if (VectorMath.IsSimdSupported && length >= VectorMath.VectorSize)
			{
				Vector<float> vector = Vector<float>.Zero;
				int num2 = length - length % VectorMath.VectorSize;
				fixed (float* pinnableReference = left.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = right.GetPinnableReference())
					{
						float* ptr2 = pinnableReference2;
						for (int i = 0; i < num2; i += VectorMath.VectorSize)
						{
							Vector<float> vector2 = *(Vector<float>*)(ptr + i);
							Vector<float> vector3 = *(Vector<float>*)(ptr2 + i);
							Vector<float> vector4 = vector2 - vector3;
							vector += vector4 * vector4;
						}
					}
				}
				for (int j = 0; j < VectorMath.VectorSize; j++)
				{
					num += vector[j];
				}
				for (int k = num2; k < length; k++)
				{
					float num3 = *left[k] - *right[k];
					num += num3 * num3;
				}
			}
			else
			{
				for (int l = 0; l < length; l++)
				{
					float num4 = *left[l] - *right[l];
					num += num4 * num4;
				}
			}
			return (float)Math.Sqrt((double)num);
		}

		// Token: 0x060001E7 RID: 487 RVA: 0x00017134 File Offset: 0x00015334
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void BatchMatrixVectorMultiply(float[][] inputs, ReadOnlySpan<float> weights, ReadOnlySpan<float> bias, float[][] outputs, int batchSize, int inDim, int outDim)
		{
			if (VectorMath.IsSimdSupported && inDim >= VectorMath.VectorSize)
			{
				int num = inDim - inDim % VectorMath.VectorSize;
				fixed (float* pinnableReference = weights.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = bias.GetPinnableReference())
					{
						float* ptr2 = pinnableReference2;
						for (int i = 0; i < batchSize; i++)
						{
							float[] array;
							float* ptr3;
							if ((array = inputs[i]) == null || array.Length == 0)
							{
								ptr3 = null;
							}
							else
							{
								ptr3 = &array[0];
							}
							float[] array2;
							float* ptr4;
							if ((array2 = outputs[i]) == null || array2.Length == 0)
							{
								ptr4 = null;
							}
							else
							{
								ptr4 = &array2[0];
							}
							for (int j = 0; j < outDim; j++)
							{
								float* ptr5 = ptr + j * inDim;
								Vector<float> vector = Vector<float>.Zero;
								for (int k = 0; k < num; k += VectorMath.VectorSize)
								{
									Vector<float> vector2 = *(Vector<float>*)(ptr3 + k);
									Vector<float> vector3 = *(Vector<float>*)(ptr5 + k);
									vector += vector2 * vector3;
								}
								float num2 = 0f;
								for (int l = 0; l < VectorMath.VectorSize; l++)
								{
									num2 += vector[l];
								}
								for (int m = num; m < inDim; m++)
								{
									num2 += ptr3[m] * ptr5[m];
								}
								ptr4[j] = num2 + ptr2[j];
							}
							array2 = null;
							array = null;
						}
					}
				}
				return;
			}
			for (int n = 0; n < batchSize; n++)
			{
				for (int num3 = 0; num3 < outDim; num3++)
				{
					float num4 = 0f;
					for (int num5 = 0; num5 < inDim; num5++)
					{
						num4 += inputs[n][num5] * *weights[num3 * inDim + num5];
					}
					outputs[n][num3] = num4 + *bias[num3];
				}
			}
		}

		// Token: 0x060001E8 RID: 488 RVA: 0x00017320 File Offset: 0x00015520
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static float Max(ReadOnlySpan<float> array)
		{
			int length = array.Length;
			if (length == 0)
			{
				throw new ArgumentException("数组不能为空");
			}
			float num = *array[0];
			if (VectorMath.IsSimdSupported && length >= VectorMath.VectorSize)
			{
				Vector<float> vector = new Vector<float>(float.MinValue);
				int num2 = length - length % VectorMath.VectorSize;
				fixed (float* pinnableReference = array.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					for (int i = 0; i < num2; i += VectorMath.VectorSize)
					{
						Vector<float> vector2 = *(Vector<float>*)(ptr + i);
						vector = Vector.Max<float>(vector, vector2);
					}
				}
				for (int j = 0; j < VectorMath.VectorSize; j++)
				{
					if (vector[j] > num)
					{
						num = vector[j];
					}
				}
				for (int k = num2; k < length; k++)
				{
					if (*array[k] > num)
					{
						num = *array[k];
					}
				}
			}
			else
			{
				for (int l = 1; l < length; l++)
				{
					if (*array[l] > num)
					{
						num = *array[l];
					}
				}
			}
			return num;
		}

		// Token: 0x060001E9 RID: 489 RVA: 0x00017434 File Offset: 0x00015634
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static float Min(ReadOnlySpan<float> array)
		{
			int length = array.Length;
			if (length == 0)
			{
				throw new ArgumentException("数组不能为空");
			}
			float num = *array[0];
			if (VectorMath.IsSimdSupported && length >= VectorMath.VectorSize)
			{
				Vector<float> vector = new Vector<float>(float.MaxValue);
				int num2 = length - length % VectorMath.VectorSize;
				fixed (float* pinnableReference = array.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					for (int i = 0; i < num2; i += VectorMath.VectorSize)
					{
						Vector<float> vector2 = *(Vector<float>*)(ptr + i);
						vector = Vector.Min<float>(vector, vector2);
					}
				}
				for (int j = 0; j < VectorMath.VectorSize; j++)
				{
					if (vector[j] < num)
					{
						num = vector[j];
					}
				}
				for (int k = num2; k < length; k++)
				{
					if (*array[k] < num)
					{
						num = *array[k];
					}
				}
			}
			else
			{
				for (int l = 1; l < length; l++)
				{
					if (*array[l] < num)
					{
						num = *array[l];
					}
				}
			}
			return num;
		}

		// Token: 0x060001EA RID: 490 RVA: 0x00017547 File Offset: 0x00015747
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Mean(ReadOnlySpan<float> array)
		{
			if (array.IsEmpty)
			{
				return 0f;
			}
			return VectorMath.Sum(array) / (float)array.Length;
		}

		// Token: 0x060001EB RID: 491 RVA: 0x00017568 File Offset: 0x00015768
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static float Variance(ReadOnlySpan<float> array)
		{
			int length = array.Length;
			if (length == 0)
			{
				return 0f;
			}
			float num = VectorMath.Mean(array);
			float num2 = 0f;
			if (VectorMath.IsSimdSupported && length >= VectorMath.VectorSize)
			{
				Vector<float> vector = new Vector<float>(num);
				Vector<float> vector2 = Vector<float>.Zero;
				int num3 = length - length % VectorMath.VectorSize;
				fixed (float* pinnableReference = array.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					for (int i = 0; i < num3; i += VectorMath.VectorSize)
					{
						Vector<float> vector3 = *(Vector<float>*)(ptr + i) - vector;
						vector2 += vector3 * vector3;
					}
				}
				for (int j = 0; j < VectorMath.VectorSize; j++)
				{
					num2 += vector2[j];
				}
				for (int k = num3; k < length; k++)
				{
					float num4 = *array[k] - num;
					num2 += num4 * num4;
				}
			}
			else
			{
				for (int l = 0; l < length; l++)
				{
					float num5 = *array[l] - num;
					num2 += num5 * num5;
				}
			}
			return num2 / (float)length;
		}

		// Token: 0x060001EC RID: 492 RVA: 0x00017683 File Offset: 0x00015883
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float StandardDeviation(ReadOnlySpan<float> array)
		{
			return (float)Math.Sqrt((double)VectorMath.Variance(array));
		}

		// Token: 0x060001ED RID: 493 RVA: 0x00017694 File Offset: 0x00015894
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void Exp(ReadOnlySpan<float> input, Span<float> result)
		{
			if (input.Length != result.Length)
			{
				throw new ArgumentException("数组长度必须相同");
			}
			int length = input.Length;
			for (int i = 0; i < length; i++)
			{
				*result[i] = (float)Math.Exp((double)(*input[i]));
			}
		}

		// Token: 0x060001EE RID: 494 RVA: 0x000176EC File Offset: 0x000158EC
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void Relu(ReadOnlySpan<float> input, Span<float> result)
		{
			if (input.Length != result.Length)
			{
				throw new ArgumentException("数组长度必须相同");
			}
			int length = input.Length;
			if (VectorMath.IsSimdSupported && length >= VectorMath.VectorSize)
			{
				Vector<float> zero = Vector<float>.Zero;
				int num = length - length % VectorMath.VectorSize;
				fixed (float* pinnableReference = input.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = result.GetPinnableReference())
					{
						float* ptr2 = pinnableReference2;
						for (int i = 0; i < num; i += VectorMath.VectorSize)
						{
							Vector<float> vector = *(Vector<float>*)(ptr + i);
							*(Vector<float>*)(ptr2 + i) = Vector.Max<float>(zero, vector);
						}
					}
				}
				for (int j = num; j < length; j++)
				{
					*result[j] = Math.Max(0f, *input[j]);
				}
				return;
			}
			for (int k = 0; k < length; k++)
			{
				*result[k] = Math.Max(0f, *input[k]);
			}
		}

		// Token: 0x060001EF RID: 495 RVA: 0x000177F8 File Offset: 0x000159F8
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void ReluGradient(ReadOnlySpan<float> input, ReadOnlySpan<float> gradOutput, Span<float> result)
		{
			if (input.Length != gradOutput.Length || input.Length != result.Length)
			{
				throw new ArgumentException("数组长度必须相同");
			}
			int length = input.Length;
			if (VectorMath.IsSimdSupported && length >= VectorMath.VectorSize)
			{
				Vector<float> zero = Vector<float>.Zero;
				int num = length - length % VectorMath.VectorSize;
				fixed (float* pinnableReference = input.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = gradOutput.GetPinnableReference())
					{
						float* ptr2 = pinnableReference2;
						fixed (float* pinnableReference3 = result.GetPinnableReference())
						{
							float* ptr3 = pinnableReference3;
							for (int i = 0; i < num; i += VectorMath.VectorSize)
							{
								Vector<float> vector = *(Vector<float>*)(ptr + i);
								Vector<float> vector2 = *(Vector<float>*)(ptr2 + i);
								Vector<float> vector3 = Vector.ConditionalSelect(Vector.GreaterThan(vector, zero), Vector<float>.One, zero);
								*(Vector<float>*)(ptr3 + i) = vector3 * vector2;
							}
						}
					}
				}
				for (int j = num; j < length; j++)
				{
					*result[j] = ((*input[j] > 0f) ? (*gradOutput[j]) : 0f);
				}
				return;
			}
			for (int k = 0; k < length; k++)
			{
				*result[k] = ((*input[k] > 0f) ? (*gradOutput[k]) : 0f);
			}
		}

		// Token: 0x060001F0 RID: 496 RVA: 0x00017964 File Offset: 0x00015B64
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void AddScalarInPlace(Span<float> array, float scalar)
		{
			int length = array.Length;
			if (VectorMath.IsSimdSupported && length >= VectorMath.VectorSize)
			{
				Vector<float> vector = new Vector<float>(scalar);
				int num = length - length % VectorMath.VectorSize;
				fixed (float* pinnableReference = array.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					for (int i = 0; i < num; i += VectorMath.VectorSize)
					{
						Vector<float> vector2 = *(Vector<float>*)(ptr + i);
						*(Vector<float>*)(ptr + i) = vector2 + vector;
					}
				}
				for (int j = num; j < length; j++)
				{
					*array[j] += scalar;
				}
				return;
			}
			for (int k = 0; k < length; k++)
			{
				*array[k] += scalar;
			}
		}

		// Token: 0x060001F1 RID: 497 RVA: 0x00017A24 File Offset: 0x00015C24
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void ComputeWeightGrad(ReadOnlySpan<float> gradOutput, ReadOnlySpan<float> input, Span<float> weightGrad, int outDim, int inDim)
		{
			if (VectorMath.IsSimdSupported && outDim >= VectorMath.VectorSize && inDim >= VectorMath.VectorSize)
			{
				fixed (float* pinnableReference = input.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = weightGrad.GetPinnableReference())
					{
						float* ptr2 = pinnableReference2;
						for (int i = 0; i < outDim; i++)
						{
							float num = *gradOutput[i];
							Vector<float> vector = new Vector<float>(num);
							int num2 = i * inDim;
							int j;
							for (j = 0; j <= inDim - VectorMath.VectorSize; j += VectorMath.VectorSize)
							{
								Vector<float> vector2 = *(Vector<float>*)(ptr + j);
								*(Vector<float>*)(ptr2 + num2 + j) = vector2 * vector;
							}
							while (j < inDim)
							{
								*weightGrad[num2 + j] = num * *input[j];
								j++;
							}
						}
					}
				}
				return;
			}
			for (int k = 0; k < outDim; k++)
			{
				float num3 = *gradOutput[k];
				int num4 = k * inDim;
				for (int l = 0; l < inDim; l++)
				{
					*weightGrad[num4 + l] = num3 * *input[l];
				}
			}
		}

		// Token: 0x060001F2 RID: 498 RVA: 0x00017B5C File Offset: 0x00015D5C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void ComputeInputGrad(ReadOnlySpan<float> gradOutput, ReadOnlySpan<float> weights, Span<float> inputGrad, int outDim, int inDim)
		{
			if (VectorMath.IsSimdSupported && inDim >= VectorMath.VectorSize && outDim >= VectorMath.VectorSize)
			{
				inputGrad.Clear();
				fixed (float* pinnableReference = weights.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = inputGrad.GetPinnableReference())
					{
						float* ptr2 = pinnableReference2;
						for (int i = 0; i < outDim; i++)
						{
							float num = *gradOutput[i];
							int num2 = i * inDim;
							Vector<float> vector = new Vector<float>(num);
							for (int j = 0; j <= inDim - VectorMath.VectorSize; j += VectorMath.VectorSize)
							{
								Vector<float> vector2 = *(Vector<float>*)(ptr + num2 + j);
								Vector<float> vector3 = *(Vector<float>*)(ptr2 + j);
								*(Vector<float>*)(ptr2 + j) = vector3 + vector2 * vector;
							}
							for (int k = inDim - inDim % VectorMath.VectorSize; k < inDim; k++)
							{
								*inputGrad[k] += *weights[num2 + k] * num;
							}
						}
					}
				}
				return;
			}
			for (int l = 0; l < inDim; l++)
			{
				float num3 = 0f;
				for (int m = 0; m < outDim; m++)
				{
					num3 += *gradOutput[m] * *weights[m * inDim + l];
				}
				*inputGrad[l] = num3;
			}
		}

		// Token: 0x060001F3 RID: 499 RVA: 0x00017CC8 File Offset: 0x00015EC8
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void BatchComputeInputGrad(ReadOnlySpan<float> gradOutputs, ReadOnlySpan<float> weights, Span<float> inputGrads, int numCores, int outDim, int inDim)
		{
			if (gradOutputs.Length != numCores * outDim)
			{
				throw new ArgumentException("梯度输出大小不匹配");
			}
			if (weights.Length != numCores * outDim * inDim)
			{
				throw new ArgumentException("权重总大小不匹配");
			}
			if (inputGrads.Length != numCores * inDim)
			{
				throw new ArgumentException("输入梯度总大小不匹配");
			}
			if (VectorMath.IsSimdSupported && inDim >= VectorMath.VectorSize && outDim >= VectorMath.VectorSize)
			{
				inputGrads.Clear();
				fixed (float* pinnableReference = weights.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = gradOutputs.GetPinnableReference())
					{
						fixed (float* pinnableReference3 = inputGrads.GetPinnableReference())
						{
							float* ptr2 = pinnableReference3;
							for (int i = 0; i < numCores; i++)
							{
								int num = i * outDim * inDim;
								int num2 = i * outDim;
								int num3 = i * inDim;
								for (int j = 0; j < outDim; j++)
								{
									float num4 = *gradOutputs[num2 + j];
									int num5 = num + j * inDim;
									Vector<float> vector = new Vector<float>(num4);
									for (int k = 0; k <= inDim - VectorMath.VectorSize; k += VectorMath.VectorSize)
									{
										Vector<float> vector2 = *(Vector<float>*)(ptr + num5 + k);
										Vector<float> vector3 = *(Vector<float>*)(ptr2 + num3 + k);
										*(Vector<float>*)(ptr2 + num3 + k) = vector3 + vector2 * vector;
									}
									for (int l = inDim - inDim % VectorMath.VectorSize; l < inDim; l++)
									{
										*inputGrads[num3 + l] += *weights[num5 + l] * num4;
									}
								}
							}
						}
					}
				}
				return;
			}
			for (int m = 0; m < numCores; m++)
			{
				int num6 = m * outDim * inDim;
				int num7 = m * outDim;
				int num8 = m * inDim;
				for (int n = 0; n < inDim; n++)
				{
					float num9 = 0f;
					for (int num10 = 0; num10 < outDim; num10++)
					{
						num9 += *gradOutputs[num7 + num10] * *weights[num6 + num10 * inDim + n];
					}
					*inputGrads[num8 + n] = num9;
				}
			}
		}

		// Token: 0x060001F4 RID: 500 RVA: 0x00017F08 File Offset: 0x00016108
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void MultiplyElementwise(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> result)
		{
			int num = Math.Min(Math.Min(a.Length, b.Length), result.Length);
			if (VectorMath.IsSimdSupported && num >= VectorMath.VectorSize)
			{
				int num2 = num - num % VectorMath.VectorSize;
				fixed (float* pinnableReference = a.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = b.GetPinnableReference())
					{
						float* ptr2 = pinnableReference2;
						fixed (float* pinnableReference3 = result.GetPinnableReference())
						{
							float* ptr3 = pinnableReference3;
							for (int i = 0; i < num2; i += VectorMath.VectorSize)
							{
								Vector<float> vector = *(Vector<float>*)(ptr + i);
								Vector<float> vector2 = *(Vector<float>*)(ptr2 + i);
								*(Vector<float>*)(ptr3 + i) = vector * vector2;
							}
						}
					}
				}
				for (int j = num2; j < num; j++)
				{
					*result[j] = *a[j] * *b[j];
				}
				return;
			}
			for (int k = 0; k < num; k++)
			{
				*result[k] = *a[k] * *b[k];
			}
		}

		// Token: 0x060001F5 RID: 501 RVA: 0x0001802C File Offset: 0x0001622C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void BatchDotProduct(ReadOnlySpan<float> query, float[][] keys, Span<float> scores, int numKeys, int dim)
		{
			if (VectorMath.IsSimdSupported && dim >= VectorMath.VectorSize)
			{
				for (int i = 0; i < numKeys; i++)
				{
					float num = 0f;
					int j = 0;
					fixed (float* pinnableReference = query.GetPinnableReference())
					{
						float* ptr = pinnableReference;
						float[] array;
						float* ptr2;
						if ((array = keys[i]) == null || array.Length == 0)
						{
							ptr2 = null;
						}
						else
						{
							ptr2 = &array[0];
						}
						while (j <= dim - VectorMath.VectorSize)
						{
							Vector<float> vector = *(Vector<float>*)(ptr + j);
							Vector<float> vector2 = *(Vector<float>*)(ptr2 + j);
							Vector<float> vector3 = vector * vector2;
							for (int k = 0; k < VectorMath.VectorSize; k++)
							{
								num += vector3[k];
							}
							j += VectorMath.VectorSize;
						}
						array = null;
					}
					while (j < dim)
					{
						num += *query[j] * keys[i][j];
						j++;
					}
					*scores[i] = num;
				}
				return;
			}
			for (int l = 0; l < numKeys; l++)
			{
				float num2 = 0f;
				for (int m = 0; m < dim; m++)
				{
					num2 += *query[m] * keys[l][m];
				}
				*scores[l] = num2;
			}
		}

		// Token: 0x060001F6 RID: 502 RVA: 0x00018168 File Offset: 0x00016368
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void BatchWeightedSum(ReadOnlySpan<float> weights, float[][] values, Span<float> output, int numItems, int dim)
		{
			if (VectorMath.IsSimdSupported && dim >= VectorMath.VectorSize)
			{
				output.Clear();
				fixed (float* pinnableReference = output.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					for (int i = 0; i < numItems; i++)
					{
						float num = *weights[i];
						if (Math.Abs(num) >= 1E-10f)
						{
							Vector<float> vector = new Vector<float>(num);
							float[] array;
							float* ptr2;
							if ((array = values[i]) == null || array.Length == 0)
							{
								ptr2 = null;
							}
							else
							{
								ptr2 = &array[0];
							}
							for (int j = 0; j <= dim - VectorMath.VectorSize; j += VectorMath.VectorSize)
							{
								Vector<float> vector2 = *(Vector<float>*)(ptr2 + j);
								Vector<float> vector3 = *(Vector<float>*)(ptr + j);
								*(Vector<float>*)(ptr + j) = vector3 + vector2 * vector;
							}
							array = null;
							for (int k = dim - dim % VectorMath.VectorSize; k < dim; k++)
							{
								*output[k] += values[i][k] * num;
							}
						}
					}
				}
				return;
			}
			for (int l = 0; l < dim; l++)
			{
				float num2 = 0f;
				for (int m = 0; m < numItems; m++)
				{
					num2 += *weights[m] * values[m][l];
				}
				*output[l] = num2;
			}
		}

		// Token: 0x060001F7 RID: 503 RVA: 0x000182C8 File Offset: 0x000164C8
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void ComputeBatchWeightGrad(ReadOnlySpan<float> gradOutputs, float[][] inputs, Span<float> weightGrad, int batchSize, int outDim, int inDim)
		{
			if (VectorMath.IsSimdSupported && inDim >= VectorMath.VectorSize)
			{
				fixed (float* pinnableReference = weightGrad.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					for (int i = 0; i < batchSize; i++)
					{
						float[] array;
						float* ptr2;
						if ((array = inputs[i]) == null || array.Length == 0)
						{
							ptr2 = null;
						}
						else
						{
							ptr2 = &array[0];
						}
						for (int j = 0; j < outDim; j++)
						{
							int num = j * inDim;
							float num2 = *gradOutputs[i * outDim + j];
							if (Math.Abs(num2) >= 1E-10f)
							{
								for (int k = 0; k <= inDim - VectorMath.VectorSize; k += VectorMath.VectorSize)
								{
									Vector<float> vector = *(Vector<float>*)(ptr2 + k);
									Vector<float> vector2 = new Vector<float>(num2);
									Vector<float> vector3 = *(Vector<float>*)(ptr + num + k);
									*(Vector<float>*)(ptr + num + k) = vector3 + vector * vector2;
								}
								for (int l = inDim - inDim % VectorMath.VectorSize; l < inDim; l++)
								{
									*weightGrad[num + l] += ptr2[l] * num2;
								}
							}
						}
						array = null;
					}
				}
				return;
			}
			for (int m = 0; m < batchSize; m++)
			{
				for (int n = 0; n < outDim; n++)
				{
					float num3 = *gradOutputs[m * outDim + n];
					for (int num4 = 0; num4 < inDim; num4++)
					{
						*weightGrad[n * inDim + num4] += inputs[m][num4] * num3;
					}
				}
			}
		}

		// Token: 0x060001F8 RID: 504 RVA: 0x0001846C File Offset: 0x0001666C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void BatchWeightedDotProduct(ReadOnlySpan<float> weights, float[][] values, Span<float> result, int numItems, int dim)
		{
			if (VectorMath.IsSimdSupported && dim >= VectorMath.VectorSize)
			{
				result.Clear();
				fixed (float* pinnableReference = result.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					for (int i = 0; i < numItems; i++)
					{
						float num = *weights[i];
						if (Math.Abs(num) >= 1E-10f)
						{
							float[] array;
							float* ptr2;
							if ((array = values[i]) == null || array.Length == 0)
							{
								ptr2 = null;
							}
							else
							{
								ptr2 = &array[0];
							}
							for (int j = 0; j <= dim - VectorMath.VectorSize; j += VectorMath.VectorSize)
							{
								Vector<float> vector = *(Vector<float>*)(ptr2 + j);
								Vector<float> vector2 = *(Vector<float>*)(ptr + j);
								*(Vector<float>*)(ptr + j) = vector2 + vector * new Vector<float>(num);
							}
							array = null;
							for (int k = dim - dim % VectorMath.VectorSize; k < dim; k++)
							{
								*result[k] += values[i][k] * num;
							}
						}
					}
				}
				return;
			}
			for (int l = 0; l < dim; l++)
			{
				float num2 = 0f;
				for (int m = 0; m < numItems; m++)
				{
					num2 += *weights[m] * values[m][l];
				}
				*result[l] = num2;
			}
		}

		// Token: 0x060001F9 RID: 505 RVA: 0x000185C8 File Offset: 0x000167C8
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void BatchAccumulateWeighted(float[] weights, float[][] values, float[] output, int numItems, int dim, float scale = 1f)
		{
			if (VectorMath.IsSimdSupported && dim >= VectorMath.VectorSize)
			{
				fixed (float[] array = output)
				{
					float* ptr;
					if (output == null || array.Length == 0)
					{
						ptr = null;
					}
					else
					{
						ptr = &array[0];
					}
					for (int i = 0; i < numItems; i++)
					{
						float num = weights[i] * scale;
						if (Math.Abs(num) >= 1E-10f)
						{
							float[] array2;
							float* ptr2;
							if ((array2 = values[i]) == null || array2.Length == 0)
							{
								ptr2 = null;
							}
							else
							{
								ptr2 = &array2[0];
							}
							for (int j = 0; j <= dim - VectorMath.VectorSize; j += VectorMath.VectorSize)
							{
								Vector<float> vector = *(Vector<float>*)(ptr2 + j);
								Vector<float> vector2 = *(Vector<float>*)(ptr + j);
								*(Vector<float>*)(ptr + j) = vector2 + vector * new Vector<float>(num);
							}
							array2 = null;
							for (int k = dim - dim % VectorMath.VectorSize; k < dim; k++)
							{
								output[k] += values[i][k] * num;
							}
						}
					}
				}
				return;
			}
			for (int l = 0; l < numItems; l++)
			{
				float num2 = weights[l] * scale;
				for (int m = 0; m < dim; m++)
				{
					output[m] += values[l][m] * num2;
				}
			}
		}

		// Token: 0x060001FA RID: 506 RVA: 0x00018718 File Offset: 0x00016918
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void BatchQueryKeyDotProduct(ReadOnlySpan<float> query, float[][] keys, ReadOnlySpan<float> weights, Span<float> result, int numKeys, int dim)
		{
			if (VectorMath.IsSimdSupported && dim >= VectorMath.VectorSize)
			{
				fixed (float* pinnableReference = query.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = result.GetPinnableReference())
					{
						float* ptr2 = pinnableReference2;
						for (int i = 0; i < numKeys; i++)
						{
							float num = *weights[i];
							if (Math.Abs(num) < 1E-10f)
							{
								ptr2[i] = 0f;
							}
							else
							{
								float[] array;
								float* ptr3;
								if ((array = keys[i]) == null || array.Length == 0)
								{
									ptr3 = null;
								}
								else
								{
									ptr3 = &array[0];
								}
								float num2 = 0f;
								int j;
								for (j = 0; j <= dim - VectorMath.VectorSize; j += VectorMath.VectorSize)
								{
									Vector<float> vector = *(Vector<float>*)(ptr + j);
									Vector<float> vector2 = *(Vector<float>*)(ptr3 + j);
									Vector<float> vector3 = vector * vector2;
									for (int k = 0; k < VectorMath.VectorSize; k++)
									{
										num2 += vector3[k];
									}
								}
								while (j < dim)
								{
									num2 += *query[j] * keys[i][j];
									j++;
								}
								ptr2[i] = num2 * num;
								array = null;
							}
						}
					}
				}
				return;
			}
			for (int l = 0; l < numKeys; l++)
			{
				float num3 = *weights[l];
				if (Math.Abs(num3) < 1E-10f)
				{
					*result[l] = 0f;
				}
				else
				{
					float num4 = 0f;
					for (int m = 0; m < dim; m++)
					{
						num4 += *query[m] * keys[l][m];
					}
					*result[l] = num4 * num3;
				}
			}
		}

		// Token: 0x060001FB RID: 507 RVA: 0x000188D4 File Offset: 0x00016AD4
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void BatchVectorsDotVector(float[][] a, ReadOnlySpan<float> b, Span<float> result, int numItems, int dim)
		{
			if (VectorMath.IsSimdSupported && dim >= VectorMath.VectorSize)
			{
				fixed (float* pinnableReference = b.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = result.GetPinnableReference())
					{
						float* ptr2 = pinnableReference2;
						for (int i = 0; i < numItems; i++)
						{
							float[] array;
							float* ptr3;
							if ((array = a[i]) == null || array.Length == 0)
							{
								ptr3 = null;
							}
							else
							{
								ptr3 = &array[0];
							}
							float num = 0f;
							int j;
							for (j = 0; j <= dim - VectorMath.VectorSize; j += VectorMath.VectorSize)
							{
								Vector<float> vector = *(Vector<float>*)(ptr3 + j);
								Vector<float> vector2 = *(Vector<float>*)(ptr + j);
								Vector<float> vector3 = vector * vector2;
								for (int k = 0; k < VectorMath.VectorSize; k++)
								{
									num += vector3[k];
								}
							}
							while (j < dim)
							{
								num += a[i][j] * *b[j];
								j++;
							}
							ptr2[i] = num;
							array = null;
						}
					}
				}
				return;
			}
			for (int l = 0; l < numItems; l++)
			{
				float num2 = 0f;
				for (int m = 0; m < dim; m++)
				{
					num2 += a[l][m] * *b[m];
				}
				*result[l] = num2;
			}
		}

		// Token: 0x060001FC RID: 508 RVA: 0x00018A30 File Offset: 0x00016C30
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void BatchMatrixVectorMultiply(ReadOnlySpan<float> matrices, ReadOnlySpan<float> vector, Span<float> results, int batchSize, int rowsPerMatrix, int cols)
		{
			if (VectorMath.IsSimdSupported && cols >= VectorMath.VectorSize && rowsPerMatrix >= VectorMath.VectorSize)
			{
				fixed (float* pinnableReference = matrices.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = vector.GetPinnableReference())
					{
						float* ptr2 = pinnableReference2;
						fixed (float* pinnableReference3 = results.GetPinnableReference())
						{
							for (int i = 0; i < batchSize; i++)
							{
								int num = i * rowsPerMatrix * cols;
								int num2 = i * rowsPerMatrix;
								for (int j = 0; j < rowsPerMatrix; j++)
								{
									int num3 = num + j * cols;
									float num4 = 0f;
									int k;
									for (k = 0; k <= cols - VectorMath.VectorSize; k += VectorMath.VectorSize)
									{
										Vector<float> vector2 = *(Vector<float>*)(ptr + num3 + k);
										Vector<float> vector3 = *(Vector<float>*)(ptr2 + k);
										Vector<float> vector4 = vector2 * vector3;
										for (int l = 0; l < VectorMath.VectorSize; l++)
										{
											num4 += vector4[l];
										}
									}
									while (k < cols)
									{
										num4 += *matrices[num3 + k] * *vector[k];
										k++;
									}
									*results[num2 + j] = num4;
								}
							}
						}
					}
				}
				return;
			}
			for (int m = 0; m < batchSize; m++)
			{
				int num5 = m * rowsPerMatrix * cols;
				int num6 = m * rowsPerMatrix;
				for (int n = 0; n < rowsPerMatrix; n++)
				{
					float num7 = 0f;
					int num8 = num5 + n * cols;
					for (int num9 = 0; num9 < cols; num9++)
					{
						num7 += *matrices[num8 + num9] * *vector[num9];
					}
					*results[num6 + n] = num7;
				}
			}
		}

		// Token: 0x060001FD RID: 509 RVA: 0x00018BFC File Offset: 0x00016DFC
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void BatchMatrixVectorMultiplyWithBias(ReadOnlySpan<float> matrices, ReadOnlySpan<float> vector, ReadOnlySpan<float> bias, Span<float> results, int batchSize, int rowsPerMatrix, int cols)
		{
			if (VectorMath.IsSimdSupported && cols >= VectorMath.VectorSize && rowsPerMatrix >= VectorMath.VectorSize)
			{
				fixed (float* pinnableReference = matrices.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = vector.GetPinnableReference())
					{
						float* ptr2 = pinnableReference2;
						fixed (float* pinnableReference3 = bias.GetPinnableReference())
						{
							fixed (float* pinnableReference4 = results.GetPinnableReference())
							{
								for (int i = 0; i < batchSize; i++)
								{
									int num = i * rowsPerMatrix * cols;
									int num2 = i * rowsPerMatrix;
									for (int j = 0; j < rowsPerMatrix; j++)
									{
										int num3 = num + j * cols;
										float num4 = 0f;
										int k;
										for (k = 0; k <= cols - VectorMath.VectorSize; k += VectorMath.VectorSize)
										{
											Vector<float> vector2 = *(Vector<float>*)(ptr + num3 + k);
											Vector<float> vector3 = *(Vector<float>*)(ptr2 + k);
											Vector<float> vector4 = vector2 * vector3;
											for (int l = 0; l < VectorMath.VectorSize; l++)
											{
												num4 += vector4[l];
											}
										}
										while (k < cols)
										{
											num4 += *matrices[num3 + k] * *vector[k];
											k++;
										}
										*results[num2 + j] = num4 + *bias[num2 + j];
									}
								}
							}
						}
					}
				}
				return;
			}
			for (int m = 0; m < batchSize; m++)
			{
				int num5 = m * rowsPerMatrix * cols;
				int num6 = m * rowsPerMatrix;
				for (int n = 0; n < rowsPerMatrix; n++)
				{
					float num7 = 0f;
					int num8 = num5 + n * cols;
					for (int num9 = 0; num9 < cols; num9++)
					{
						num7 += *matrices[num8 + num9] * *vector[num9];
					}
					*results[num6 + n] = num7 + *bias[num6 + n];
				}
			}
		}

		// Token: 0x060001FE RID: 510 RVA: 0x00018DFC File Offset: 0x00016FFC
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void BatchTransposeMultiply(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> result, int batchSize, int rows, int cols)
		{
			if (VectorMath.IsSimdSupported && cols >= VectorMath.VectorSize && rows >= VectorMath.VectorSize)
			{
				fixed (float* pinnableReference = a.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = b.GetPinnableReference())
					{
						float* ptr2 = pinnableReference2;
						fixed (float* pinnableReference3 = result.GetPinnableReference())
						{
							for (int i = 0; i < batchSize; i++)
							{
								int num = i * rows * cols;
								int num2 = i * cols;
								int num3 = i * rows * cols;
								for (int j = 0; j < rows; j++)
								{
									for (int k = 0; k < cols; k++)
									{
										int num4 = num + j * cols + k;
										float num5 = 0f;
										int l;
										for (l = 0; l <= rows - VectorMath.VectorSize; l += VectorMath.VectorSize)
										{
											Vector<float> vector = *(Vector<float>*)(ptr + num4 + l * cols);
											Vector<float> vector2 = *(Vector<float>*)(ptr2 + num2 + l);
											Vector<float> vector3 = vector * vector2;
											for (int m = 0; m < VectorMath.VectorSize; m++)
											{
												num5 += vector3[m];
											}
										}
										while (l < rows)
										{
											num5 += *a[num + l * cols + k] * *b[num2 + l];
											l++;
										}
										*result[num3 + j * cols + k] = num5;
									}
								}
							}
						}
					}
				}
				return;
			}
			for (int n = 0; n < batchSize; n++)
			{
				int num6 = n * rows * cols;
				int num7 = n * cols;
				int num8 = n * rows * cols;
				for (int num9 = 0; num9 < rows; num9++)
				{
					for (int num10 = 0; num10 < cols; num10++)
					{
						float num11 = 0f;
						for (int num12 = 0; num12 < rows; num12++)
						{
							num11 += *a[num6 + num12 * cols + num10] * *b[num7 + num12];
						}
						*result[num8 + num9 * cols + num10] = num11;
					}
				}
			}
		}

		// Token: 0x060001FF RID: 511 RVA: 0x0001902C File Offset: 0x0001722C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void BatchMatMul(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> c, int batchSize, int m, int n, int k)
		{
			if (VectorMath.IsSimdSupported && k >= VectorMath.VectorSize)
			{
				fixed (float* pinnableReference = a.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = b.GetPinnableReference())
					{
						float* ptr2 = pinnableReference2;
						fixed (float* pinnableReference3 = c.GetPinnableReference())
						{
							for (int i = 0; i < batchSize; i++)
							{
								int num = i * m * k;
								int num2 = i * k * n;
								int num3 = i * m * n;
								for (int j = 0; j < m; j++)
								{
									for (int l = 0; l < n; l++)
									{
										float num4 = 0f;
										int num5;
										for (num5 = 0; num5 <= k - VectorMath.VectorSize; num5 += VectorMath.VectorSize)
										{
											Vector<float> vector = *(Vector<float>*)(ptr + num + j * k + num5);
											Vector<float> vector2 = *(Vector<float>*)(ptr2 + num2 + num5 * n + l);
											Vector<float> vector3 = vector * vector2;
											for (int num6 = 0; num6 < VectorMath.VectorSize; num6++)
											{
												num4 += vector3[num6];
											}
										}
										while (num5 < k)
										{
											num4 += *a[num + j * k + num5] * *b[num2 + num5 * n + l];
											num5++;
										}
										*c[num3 + j * n + l] = num4;
									}
								}
							}
						}
					}
				}
				return;
			}
			for (int num7 = 0; num7 < batchSize; num7++)
			{
				int num8 = num7 * m * k;
				int num9 = num7 * k * n;
				int num10 = num7 * m * n;
				for (int num11 = 0; num11 < m; num11++)
				{
					for (int num12 = 0; num12 < n; num12++)
					{
						float num13 = 0f;
						for (int num14 = 0; num14 < k; num14++)
						{
							num13 += *a[num8 + num11 * k + num14] * *b[num9 + num14 * n + num12];
						}
						*c[num10 + num11 * n + num12] = num13;
					}
				}
			}
		}

		// Token: 0x06000200 RID: 512 RVA: 0x00019264 File Offset: 0x00017464
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void BatchAdd(ReadOnlySpan<float> left, ReadOnlySpan<float> right, Span<float> results, int batchSize, int vectorSize)
		{
			int num = batchSize * vectorSize;
			if (VectorMath.IsSimdSupported && num >= VectorMath.VectorSize)
			{
				int num2 = num - num % VectorMath.VectorSize;
				fixed (float* pinnableReference = left.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = right.GetPinnableReference())
					{
						float* ptr2 = pinnableReference2;
						fixed (float* pinnableReference3 = results.GetPinnableReference())
						{
							float* ptr3 = pinnableReference3;
							for (int i = 0; i < num2; i += VectorMath.VectorSize)
							{
								Vector<float> vector = *(Vector<float>*)(ptr + i);
								Vector<float> vector2 = *(Vector<float>*)(ptr2 + i);
								*(Vector<float>*)(ptr3 + i) = vector + vector2;
							}
						}
					}
				}
				for (int j = num2; j < num; j++)
				{
					*results[j] = *left[j] + *right[j];
				}
				return;
			}
			for (int k = 0; k < num; k++)
			{
				*results[k] = *left[k] + *right[k];
			}
		}

		// Token: 0x06000201 RID: 513 RVA: 0x00019370 File Offset: 0x00017570
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void BatchSoftmax(Span<float> values, int batchSize, int vectorSize)
		{
			if (VectorMath.IsSimdSupported && vectorSize >= VectorMath.VectorSize)
			{
				for (int i = 0; i < batchSize; i++)
				{
					int num = i * vectorSize;
					float num2 = float.MinValue;
					for (int j = 0; j < vectorSize; j++)
					{
						if (*values[num + j] > num2)
						{
							num2 = *values[num + j];
						}
					}
					float num3 = 0f;
					for (int k = 0; k < vectorSize; k++)
					{
						*values[num + k] = (float)Math.Exp((double)(*values[num + k] - num2));
						num3 += *values[num + k];
					}
					float num4 = 1f / num3;
					Vector<float> vector = new Vector<float>(num4);
					int l = 0;
					fixed (float* pinnableReference = values.GetPinnableReference())
					{
						float* ptr = pinnableReference;
						while (l <= vectorSize - VectorMath.VectorSize)
						{
							Vector<float> vector2 = *(Vector<float>*)(ptr + num + l);
							*(Vector<float>*)(ptr + num + l) = vector2 * vector;
							l += VectorMath.VectorSize;
						}
					}
					while (l < vectorSize)
					{
						*values[num + l] *= num4;
						l++;
					}
				}
				return;
			}
			for (int m = 0; m < batchSize; m++)
			{
				int num5 = m * vectorSize;
				float num6 = float.MinValue;
				for (int n = 0; n < vectorSize; n++)
				{
					if (*values[num5 + n] > num6)
					{
						num6 = *values[num5 + n];
					}
				}
				float num7 = 0f;
				for (int num8 = 0; num8 < vectorSize; num8++)
				{
					*values[num5 + num8] = (float)Math.Exp((double)(*values[num5 + num8] - num6));
					num7 += *values[num5 + num8];
				}
				float num9 = 1f / num7;
				for (int num10 = 0; num10 < vectorSize; num10++)
				{
					*values[num5 + num10] *= num9;
				}
			}
		}

		// Token: 0x06000202 RID: 514 RVA: 0x00019578 File Offset: 0x00017778
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void BatchLayerNorm(ReadOnlySpan<float> input, ReadOnlySpan<float> scale, ReadOnlySpan<float> bias, Span<float> output, int batchSize, int vectorSize)
		{
			if (VectorMath.IsSimdSupported && vectorSize >= VectorMath.VectorSize)
			{
				for (int i = 0; i < batchSize; i++)
				{
					int num = i * vectorSize;
					float num2 = 0f;
					for (int j = 0; j < vectorSize; j++)
					{
						num2 += *input[num + j];
					}
					num2 /= (float)vectorSize;
					float num3 = 0f;
					for (int k = 0; k < vectorSize; k++)
					{
						float num4 = *input[num + k] - num2;
						num3 += num4 * num4;
					}
					num3 /= (float)vectorSize;
					float num5 = (float)Math.Sqrt((double)(num3 + 1E-08f));
					float num6 = 1f / num5;
					Vector<float> vector = new Vector<float>(num6);
					int l = 0;
					fixed (float* pinnableReference = input.GetPinnableReference())
					{
						float* ptr = pinnableReference;
						fixed (float* pinnableReference2 = scale.GetPinnableReference())
						{
							float* ptr2 = pinnableReference2;
							fixed (float* pinnableReference3 = bias.GetPinnableReference())
							{
								float* ptr3 = pinnableReference3;
								fixed (float* pinnableReference4 = output.GetPinnableReference())
								{
									float* ptr4 = pinnableReference4;
									while (l <= vectorSize - VectorMath.VectorSize)
									{
										Vector<float> vector2 = *(Vector<float>*)(ptr + num + l);
										Vector<float> vector3 = *(Vector<float>*)(ptr2 + l);
										Vector<float> vector4 = *(Vector<float>*)(ptr3 + l);
										Vector<float> vector5 = new Vector<float>(num2);
										*(Vector<float>*)(ptr4 + num + l) = vector3 * (vector2 - vector5) * vector + vector4;
										l += VectorMath.VectorSize;
									}
								}
							}
						}
					}
					while (l < vectorSize)
					{
						*output[num + l] = *scale[l] * (*input[num + l] - num2) * num6 + *bias[l];
						l++;
					}
				}
				return;
			}
			for (int m = 0; m < batchSize; m++)
			{
				int num7 = m * vectorSize;
				float num8 = 0f;
				for (int n = 0; n < vectorSize; n++)
				{
					num8 += *input[num7 + n];
				}
				num8 /= (float)vectorSize;
				float num9 = 0f;
				for (int num10 = 0; num10 < vectorSize; num10++)
				{
					float num11 = *input[num7 + num10] - num8;
					num9 += num11 * num11;
				}
				num9 /= (float)vectorSize;
				float num12 = (float)Math.Sqrt((double)(num9 + 1E-08f));
				float num13 = 1f / num12;
				for (int num14 = 0; num14 < vectorSize; num14++)
				{
					*output[num7 + num14] = *scale[num14] * (*input[num7 + num14] - num8) * num13 + *bias[num14];
				}
			}
		}

		// Token: 0x06000203 RID: 515 RVA: 0x00019834 File Offset: 0x00017A34
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void BatchRelu(ReadOnlySpan<float> input, Span<float> output, int batchSize, int vectorSize)
		{
			int num = batchSize * vectorSize;
			if (VectorMath.IsSimdSupported && num >= VectorMath.VectorSize)
			{
				Vector<float> zero = Vector<float>.Zero;
				int num2 = num - num % VectorMath.VectorSize;
				fixed (float* pinnableReference = input.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = output.GetPinnableReference())
					{
						float* ptr2 = pinnableReference2;
						for (int i = 0; i < num2; i += VectorMath.VectorSize)
						{
							Vector<float> vector = *(Vector<float>*)(ptr + i);
							*(Vector<float>*)(ptr2 + i) = Vector.Max<float>(zero, vector);
						}
					}
				}
				for (int j = num2; j < num; j++)
				{
					*output[j] = Math.Max(0f, *input[j]);
				}
				return;
			}
			for (int k = 0; k < num; k++)
			{
				*output[k] = Math.Max(0f, *input[k]);
			}
		}

		// Token: 0x06000204 RID: 516 RVA: 0x00019924 File Offset: 0x00017B24
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void BatchScale(ReadOnlySpan<float> input, float scale, Span<float> output, int batchSize, int vectorSize)
		{
			int num = batchSize * vectorSize;
			if (VectorMath.IsSimdSupported && num >= VectorMath.VectorSize)
			{
				Vector<float> vector = new Vector<float>(scale);
				int num2 = num - num % VectorMath.VectorSize;
				fixed (float* pinnableReference = input.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = output.GetPinnableReference())
					{
						float* ptr2 = pinnableReference2;
						for (int i = 0; i < num2; i += VectorMath.VectorSize)
						{
							Vector<float> vector2 = *(Vector<float>*)(ptr + i);
							*(Vector<float>*)(ptr2 + i) = vector2 * vector;
						}
					}
				}
				for (int j = num2; j < num; j++)
				{
					*output[j] = *input[j] * scale;
				}
				return;
			}
			for (int k = 0; k < num; k++)
			{
				*output[k] = *input[k] * scale;
			}
		}

		// Token: 0x06000205 RID: 517 RVA: 0x00019A04 File Offset: 0x00017C04
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static int FindMaxIndex(ReadOnlySpan<float> values, out float maxValue)
		{
			int length = values.Length;
			if (length == 0)
			{
				maxValue = 0f;
				return -1;
			}
			if (VectorMath.IsSimdSupported && length >= VectorMath.VectorSize)
			{
				maxValue = float.MinValue;
				int num = 0;
				fixed (float* pinnableReference = values.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					int i;
					for (i = 0; i <= length - VectorMath.VectorSize; i += VectorMath.VectorSize)
					{
						Vector<float> vector = *(Vector<float>*)(ptr + i);
						for (int j = 0; j < VectorMath.VectorSize; j++)
						{
							float num2 = vector[j];
							if (num2 > maxValue)
							{
								maxValue = num2;
								num = i + j;
							}
						}
					}
					while (i < length)
					{
						if (*values[i] > maxValue)
						{
							maxValue = *values[i];
							num = i;
						}
						i++;
					}
				}
				return num;
			}
			maxValue = *values[0];
			int num3 = 0;
			for (int k = 1; k < length; k++)
			{
				if (*values[k] > maxValue)
				{
					maxValue = *values[k];
					num3 = k;
				}
			}
			return num3;
		}

		// Token: 0x06000206 RID: 518 RVA: 0x00019B14 File Offset: 0x00017D14
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void BatchNormalize(Span<float> values)
		{
			float num = 0f;
			for (int i = 0; i < values.Length; i++)
			{
				num += *values[i];
			}
			if (num > 0f)
			{
				float num2 = 1f / num;
				for (int j = 0; j < values.Length; j++)
				{
					*values[j] *= num2;
				}
			}
		}

		// Token: 0x06000207 RID: 519 RVA: 0x00019B74 File Offset: 0x00017D74
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void Zero(Span<float> array)
		{
			int length = array.Length;
			if (length == 0)
			{
				return;
			}
			if (VectorMath.IsSimdSupported && length >= VectorMath.VectorSize)
			{
				int num = length - length % VectorMath.VectorSize;
				Vector<float> zero = Vector<float>.Zero;
				fixed (float* pinnableReference = array.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					for (int i = 0; i < num; i += VectorMath.VectorSize)
					{
						*(Vector<float>*)(ptr + i) = zero;
					}
				}
				for (int j = num; j < length; j++)
				{
					*array[j] = 0f;
				}
				return;
			}
			for (int k = 0; k < length; k++)
			{
				*array[k] = 0f;
			}
		}

		// Token: 0x06000208 RID: 520 RVA: 0x00019C20 File Offset: 0x00017E20
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void ComputeLayerNormInputGrad(ReadOnlySpan<float> gradOutput, ReadOnlySpan<float> gamma, ReadOnlySpan<float> xNorm, float invStd, Span<float> result)
		{
			int length = result.Length;
			float num = 1f / (float)length;
			float num2;
			float num3;
			checked
			{
				float* ptr = stackalloc float[unchecked((UIntPtr)length) * 4];
				VectorMath.MultiplyElementwise(gradOutput, gamma, new Span<float>((void*)ptr, length));
				num2 = VectorMath.Sum(new ReadOnlySpan<float>((void*)ptr, length));
				float* ptr2 = stackalloc float[unchecked((UIntPtr)length) * 4];
				VectorMath.MultiplyElementwise(new ReadOnlySpan<float>((void*)ptr, length), xNorm, new Span<float>((void*)ptr2, length));
				num3 = VectorMath.Sum(new ReadOnlySpan<float>((void*)ptr2, length));
			}
			if (VectorMath.IsSimdSupported && length >= VectorMath.VectorSize)
			{
				Vector<float> vector = new Vector<float>(invStd * num);
				Vector<float> vector2 = new Vector<float>(num2);
				Vector<float> vector3 = new Vector<float>(num3);
				fixed (float* pinnableReference = result.GetPinnableReference())
				{
					float* ptr3 = pinnableReference;
					fixed (float* pinnableReference2 = gamma.GetPinnableReference())
					{
						float* ptr4 = pinnableReference2;
						fixed (float* pinnableReference3 = xNorm.GetPinnableReference())
						{
							float* ptr5 = pinnableReference3;
							fixed (float* pinnableReference4 = gradOutput.GetPinnableReference())
							{
								float* ptr6 = pinnableReference4;
								int i;
								for (i = 0; i <= length - VectorMath.VectorSize; i += VectorMath.VectorSize)
								{
									Vector<float> vector4 = *(Vector<float>*)(ptr4 + i);
									Vector<float> vector5 = *(Vector<float>*)(ptr5 + i);
									Vector<float> vector6 = *(Vector<float>*)(ptr6 + i);
									*(Vector<float>*)(ptr3 + i) = vector * vector4 * (vector6 * new Vector<float>((float)length) - vector2 - vector5 * vector3);
								}
								while (i < length)
								{
									*result[i] = invStd * num * *gamma[i] * ((float)length * *gradOutput[i] - num2 - *xNorm[i] * num3);
									i++;
								}
							}
						}
					}
				}
				return;
			}
			for (int j = 0; j < length; j++)
			{
				*result[j] = invStd * num * *gamma[j] * ((float)length * *gradOutput[j] - num2 - *xNorm[j] * num3);
			}
		}

		// Token: 0x06000209 RID: 521 RVA: 0x00019E14 File Offset: 0x00018014
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void Fill(Span<float> array, float value)
		{
			int length = array.Length;
			if (length == 0)
			{
				return;
			}
			if (VectorMath.IsSimdSupported && length >= VectorMath.VectorSize)
			{
				Vector<float> vector = new Vector<float>(value);
				int num = length - length % VectorMath.VectorSize;
				fixed (float* pinnableReference = array.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					for (int i = 0; i < num; i += VectorMath.VectorSize)
					{
						*(Vector<float>*)(ptr + i) = vector;
					}
				}
				for (int j = num; j < length; j++)
				{
					*array[j] = value;
				}
				return;
			}
			for (int k = 0; k < length; k++)
			{
				*array[k] = value;
			}
		}

		// Token: 0x0600020A RID: 522 RVA: 0x00019EB8 File Offset: 0x000180B8
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void ZeroInt(Span<int> array)
		{
			int length = array.Length;
			if (length == 0)
			{
				return;
			}
			if (VectorMath.IsSimdSupported && length >= Vector<int>.Count)
			{
				int num = length - length % Vector<int>.Count;
				Vector<int> zero = Vector<int>.Zero;
				fixed (int* pinnableReference = array.GetPinnableReference())
				{
					int* ptr = pinnableReference;
					for (int i = 0; i < num; i += Vector<int>.Count)
					{
						*(Vector<int>*)(ptr + i) = zero;
					}
				}
				for (int j = num; j < length; j++)
				{
					*array[j] = 0;
				}
				return;
			}
			for (int k = 0; k < length; k++)
			{
				*array[k] = 0;
			}
		}

		// Token: 0x0600020B RID: 523 RVA: 0x00019F5C File Offset: 0x0001815C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void FillInt(Span<int> array, int value)
		{
			int length = array.Length;
			if (length == 0)
			{
				return;
			}
			if (VectorMath.IsSimdSupported && length >= Vector<int>.Count)
			{
				Vector<int> vector = new Vector<int>(value);
				int num = length - length % Vector<int>.Count;
				fixed (int* pinnableReference = array.GetPinnableReference())
				{
					int* ptr = pinnableReference;
					for (int i = 0; i < num; i += Vector<int>.Count)
					{
						*(Vector<int>*)(ptr + i) = vector;
					}
				}
				for (int j = num; j < length; j++)
				{
					*array[j] = value;
				}
				return;
			}
			for (int k = 0; k < length; k++)
			{
				*array[k] = value;
			}
		}
	}
}
