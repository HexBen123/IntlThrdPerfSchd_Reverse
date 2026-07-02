using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace SimdLibrary
{
	// Token: 0x02000023 RID: 35
	public static class VectorMathNew
	{
		// Token: 0x1700003F RID: 63
		// (get) Token: 0x0600021E RID: 542 RVA: 0x0001CF33 File Offset: 0x0001B133
		public static bool IsSimdSupported
		{
			get
			{
				return Vector.IsHardwareAccelerated;
			}
		}

		// Token: 0x17000040 RID: 64
		// (get) Token: 0x0600021F RID: 543 RVA: 0x0001CF3A File Offset: 0x0001B13A
		public static int VectorSize
		{
			get
			{
				return Vector<float>.Count;
			}
		}

		// Token: 0x06000220 RID: 544 RVA: 0x0001CF44 File Offset: 0x0001B144
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void Add(ReadOnlySpan<float> left, ReadOnlySpan<float> right, Span<float> result)
		{
			if (left.Length != right.Length || left.Length != result.Length)
			{
				throw new ArgumentException("数组长度必须相同");
			}
			int length = left.Length;
			if (VectorMathNew.IsSimdSupported && length >= VectorMathNew.VectorSize)
			{
				int num = length - length % VectorMathNew.VectorSize;
				fixed (float* pinnableReference = left.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = right.GetPinnableReference())
					{
						float* ptr2 = pinnableReference2;
						fixed (float* pinnableReference3 = result.GetPinnableReference())
						{
							float* ptr3 = pinnableReference3;
							for (int i = 0; i < num; i += VectorMathNew.VectorSize)
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

		// Token: 0x06000221 RID: 545 RVA: 0x0001D07C File Offset: 0x0001B27C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void Subtract(ReadOnlySpan<float> left, ReadOnlySpan<float> right, Span<float> result)
		{
			if (left.Length != right.Length || left.Length != result.Length)
			{
				throw new ArgumentException("数组长度必须相同");
			}
			int length = left.Length;
			if (VectorMathNew.IsSimdSupported && length >= VectorMathNew.VectorSize)
			{
				int num = length - length % VectorMathNew.VectorSize;
				fixed (float* pinnableReference = left.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = right.GetPinnableReference())
					{
						float* ptr2 = pinnableReference2;
						fixed (float* pinnableReference3 = result.GetPinnableReference())
						{
							float* ptr3 = pinnableReference3;
							for (int i = 0; i < num; i += VectorMathNew.VectorSize)
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

		// Token: 0x06000222 RID: 546 RVA: 0x0001D1B4 File Offset: 0x0001B3B4
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void Multiply(ReadOnlySpan<float> left, ReadOnlySpan<float> right, Span<float> result)
		{
			if (left.Length != right.Length || left.Length != result.Length)
			{
				throw new ArgumentException("数组长度必须相同");
			}
			int length = left.Length;
			if (VectorMathNew.IsSimdSupported && length >= VectorMathNew.VectorSize)
			{
				int num = length - length % VectorMathNew.VectorSize;
				fixed (float* pinnableReference = left.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = right.GetPinnableReference())
					{
						float* ptr2 = pinnableReference2;
						fixed (float* pinnableReference3 = result.GetPinnableReference())
						{
							float* ptr3 = pinnableReference3;
							for (int i = 0; i < num; i += VectorMathNew.VectorSize)
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

		// Token: 0x06000223 RID: 547 RVA: 0x0001D2EC File Offset: 0x0001B4EC
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void Divide(ReadOnlySpan<float> left, ReadOnlySpan<float> right, Span<float> result)
		{
			if (left.Length != right.Length || left.Length != result.Length)
			{
				throw new ArgumentException("数组长度必须相同");
			}
			int length = left.Length;
			if (VectorMathNew.IsSimdSupported && length >= VectorMathNew.VectorSize)
			{
				int num = length - length % VectorMathNew.VectorSize;
				fixed (float* pinnableReference = left.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = right.GetPinnableReference())
					{
						float* ptr2 = pinnableReference2;
						fixed (float* pinnableReference3 = result.GetPinnableReference())
						{
							float* ptr3 = pinnableReference3;
							for (int i = 0; i < num; i += VectorMathNew.VectorSize)
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

		// Token: 0x06000224 RID: 548 RVA: 0x0001D424 File Offset: 0x0001B624
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void MultiplyScalar(ReadOnlySpan<float> array, float scalar, Span<float> result)
		{
			if (array.Length != result.Length)
			{
				throw new ArgumentException("数组长度必须相同");
			}
			int length = array.Length;
			if (VectorMathNew.IsSimdSupported && length >= VectorMathNew.VectorSize)
			{
				Vector<float> vector = new Vector<float>(scalar);
				int num = length - length % VectorMathNew.VectorSize;
				fixed (float* pinnableReference = array.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = result.GetPinnableReference())
					{
						float* ptr2 = pinnableReference2;
						for (int i = 0; i < num; i += VectorMathNew.VectorSize)
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

		// Token: 0x06000225 RID: 549 RVA: 0x0001D524 File Offset: 0x0001B724
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void MultiplyScalarInPlace(Span<float> array, float scalar)
		{
			int length = array.Length;
			if (VectorMathNew.IsSimdSupported && length >= VectorMathNew.VectorSize)
			{
				Vector<float> vector = new Vector<float>(scalar);
				int num = length - length % VectorMathNew.VectorSize;
				fixed (float* pinnableReference = array.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					for (int i = 0; i < num; i += VectorMathNew.VectorSize)
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

		// Token: 0x06000226 RID: 550 RVA: 0x0001D5E4 File Offset: 0x0001B7E4
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static float Sum(ReadOnlySpan<float> array)
		{
			int length = array.Length;
			if (length == 0)
			{
				return 0f;
			}
			float num = 0f;
			if (VectorMathNew.IsSimdSupported && length >= VectorMathNew.VectorSize)
			{
				Vector<float> vector = Vector<float>.Zero;
				int num2 = length - length % VectorMathNew.VectorSize;
				fixed (float* pinnableReference = array.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					for (int i = 0; i < num2; i += VectorMathNew.VectorSize)
					{
						vector += *(Vector<float>*)(ptr + i);
					}
				}
				for (int j = 0; j < VectorMathNew.VectorSize; j++)
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

		// Token: 0x06000227 RID: 551 RVA: 0x0001D6C4 File Offset: 0x0001B8C4
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static float DotProduct(ReadOnlySpan<float> left, ReadOnlySpan<float> right)
		{
			int length = left.Length;
			if (length != right.Length)
			{
				throw new ArgumentException("数组长度必须相同");
			}
			float num = 0f;
			if (VectorMathNew.IsSimdSupported && length >= VectorMathNew.VectorSize)
			{
				Vector<float> vector = Vector<float>.Zero;
				int num2 = length - length % VectorMathNew.VectorSize;
				fixed (float* pinnableReference = left.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = right.GetPinnableReference())
					{
						float* ptr2 = pinnableReference2;
						for (int i = 0; i < num2; i += VectorMathNew.VectorSize)
						{
							Vector<float> vector2 = *(Vector<float>*)(ptr + i);
							Vector<float> vector3 = *(Vector<float>*)(ptr2 + i);
							vector += vector2 * vector3;
						}
					}
				}
				for (int j = 0; j < VectorMathNew.VectorSize; j++)
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

		// Token: 0x06000228 RID: 552 RVA: 0x0001D7F4 File Offset: 0x0001B9F4
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static float EuclideanDistance(ReadOnlySpan<float> left, ReadOnlySpan<float> right)
		{
			int length = left.Length;
			if (length != right.Length)
			{
				throw new ArgumentException("数组长度必须相同");
			}
			float num = 0f;
			if (VectorMathNew.IsSimdSupported && length >= VectorMathNew.VectorSize)
			{
				Vector<float> vector = Vector<float>.Zero;
				int num2 = length - length % VectorMathNew.VectorSize;
				fixed (float* pinnableReference = left.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = right.GetPinnableReference())
					{
						float* ptr2 = pinnableReference2;
						for (int i = 0; i < num2; i += VectorMathNew.VectorSize)
						{
							Vector<float> vector2 = *(Vector<float>*)(ptr + i);
							Vector<float> vector3 = *(Vector<float>*)(ptr2 + i);
							Vector<float> vector4 = vector2 - vector3;
							vector += vector4 * vector4;
						}
					}
				}
				for (int j = 0; j < VectorMathNew.VectorSize; j++)
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

		// Token: 0x06000229 RID: 553 RVA: 0x0001D940 File Offset: 0x0001BB40
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void BatchMatrixVectorMultiply(float[][] inputs, ReadOnlySpan<float> weights, ReadOnlySpan<float> bias, float[][] outputs, int batchSize, int inDim, int outDim)
		{
			if (VectorMathNew.IsSimdSupported && inDim >= VectorMathNew.VectorSize)
			{
				int num = inDim - inDim % VectorMathNew.VectorSize;
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
								for (int k = 0; k < num; k += VectorMathNew.VectorSize)
								{
									Vector<float> vector2 = *(Vector<float>*)(ptr3 + k);
									Vector<float> vector3 = *(Vector<float>*)(ptr5 + k);
									vector += vector2 * vector3;
								}
								float num2 = 0f;
								for (int l = 0; l < VectorMathNew.VectorSize; l++)
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

		// Token: 0x0600022A RID: 554 RVA: 0x0001DB2C File Offset: 0x0001BD2C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static float Max(ReadOnlySpan<float> array)
		{
			int length = array.Length;
			if (length == 0)
			{
				throw new ArgumentException("数组不能为空");
			}
			float num = *array[0];
			if (VectorMathNew.IsSimdSupported && length >= VectorMathNew.VectorSize)
			{
				Vector<float> vector = new Vector<float>(float.MinValue);
				int num2 = length - length % VectorMathNew.VectorSize;
				fixed (float* pinnableReference = array.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					for (int i = 0; i < num2; i += VectorMathNew.VectorSize)
					{
						Vector<float> vector2 = *(Vector<float>*)(ptr + i);
						vector = Vector.Max<float>(vector, vector2);
					}
				}
				for (int j = 0; j < VectorMathNew.VectorSize; j++)
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

		// Token: 0x0600022B RID: 555 RVA: 0x0001DC40 File Offset: 0x0001BE40
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static float Min(ReadOnlySpan<float> array)
		{
			int length = array.Length;
			if (length == 0)
			{
				throw new ArgumentException("数组不能为空");
			}
			float num = *array[0];
			if (VectorMathNew.IsSimdSupported && length >= VectorMathNew.VectorSize)
			{
				Vector<float> vector = new Vector<float>(float.MaxValue);
				int num2 = length - length % VectorMathNew.VectorSize;
				fixed (float* pinnableReference = array.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					for (int i = 0; i < num2; i += VectorMathNew.VectorSize)
					{
						Vector<float> vector2 = *(Vector<float>*)(ptr + i);
						vector = Vector.Min<float>(vector, vector2);
					}
				}
				for (int j = 0; j < VectorMathNew.VectorSize; j++)
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

		// Token: 0x0600022C RID: 556 RVA: 0x0001DD54 File Offset: 0x0001BF54
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void ComputeMinMaxPerColumn(ReadOnlySpan<float> windowData, int rows, int cols, Span<float> min, Span<float> max)
		{
			if (rows == 0 || cols == 0)
			{
				return;
			}
			for (int i = 0; i < cols; i++)
			{
				*min[i] = *windowData[i];
				*max[i] = *windowData[i];
			}
			if (VectorMathNew.IsSimdSupported && cols >= VectorMathNew.VectorSize)
			{
				int num = cols - cols % VectorMathNew.VectorSize;
				fixed (float* ptr = min.GetPinnableReference())
				{
					float* ptr2 = ptr;
					fixed (float* pinnableReference = max.GetPinnableReference())
					{
						float* ptr3 = pinnableReference;
						for (int j = 1; j < rows; j++)
						{
							int num2 = j * cols;
							fixed (float* pinnableReference2 = windowData.GetPinnableReference())
							{
								float* ptr4 = pinnableReference2;
								for (int k = 0; k < num; k += VectorMathNew.VectorSize)
								{
									Vector<float> vector = *(Vector<float>*)(ptr4 + num2 + k);
									Vector<float> vector2 = *(Vector<float>*)(ptr2 + k);
									Vector<float> vector3 = *(Vector<float>*)(ptr3 + k);
									vector2 = Vector.Min<float>(vector2, vector);
									vector3 = Vector.Max<float>(vector3, vector);
									*(Vector<float>*)(ptr2 + k) = vector2;
									*(Vector<float>*)(ptr3 + k) = vector3;
								}
							}
							for (int l = num; l < cols; l++)
							{
								float num3 = *windowData[num2 + l];
								if (num3 < *min[l])
								{
									*min[l] = num3;
								}
								if (num3 > *max[l])
								{
									*max[l] = num3;
								}
							}
						}
						ptr = null;
					}
					return;
				}
			}
			for (int m = 1; m < rows; m++)
			{
				int num4 = m * cols;
				for (int n = 0; n < cols; n++)
				{
					float num5 = *windowData[num4 + n];
					if (num5 < *min[n])
					{
						*min[n] = num5;
					}
					if (num5 > *max[n])
					{
						*max[n] = num5;
					}
				}
			}
		}

		// Token: 0x0600022D RID: 557 RVA: 0x0001DF3B File Offset: 0x0001C13B
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Mean(ReadOnlySpan<float> array)
		{
			if (array.IsEmpty)
			{
				return 0f;
			}
			return VectorMathNew.Sum(array) / (float)array.Length;
		}

		// Token: 0x0600022E RID: 558 RVA: 0x0001DF5C File Offset: 0x0001C15C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static float Variance(ReadOnlySpan<float> array)
		{
			int length = array.Length;
			if (length == 0)
			{
				return 0f;
			}
			float num = VectorMathNew.Mean(array);
			float num2 = 0f;
			if (VectorMathNew.IsSimdSupported && length >= VectorMathNew.VectorSize)
			{
				Vector<float> vector = new Vector<float>(num);
				Vector<float> vector2 = Vector<float>.Zero;
				int num3 = length - length % VectorMathNew.VectorSize;
				fixed (float* pinnableReference = array.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					for (int i = 0; i < num3; i += VectorMathNew.VectorSize)
					{
						Vector<float> vector3 = *(Vector<float>*)(ptr + i) - vector;
						vector2 += vector3 * vector3;
					}
				}
				for (int j = 0; j < VectorMathNew.VectorSize; j++)
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

		// Token: 0x0600022F RID: 559 RVA: 0x0001E077 File Offset: 0x0001C277
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float StandardDeviation(ReadOnlySpan<float> array)
		{
			return (float)Math.Sqrt((double)VectorMathNew.Variance(array));
		}

		// Token: 0x06000230 RID: 560 RVA: 0x0001E088 File Offset: 0x0001C288
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

		// Token: 0x06000231 RID: 561 RVA: 0x0001E0E0 File Offset: 0x0001C2E0
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void Relu(ReadOnlySpan<float> input, Span<float> result)
		{
			if (input.Length != result.Length)
			{
				throw new ArgumentException("数组长度必须相同");
			}
			int length = input.Length;
			if (VectorMathNew.IsSimdSupported && length >= VectorMathNew.VectorSize)
			{
				Vector<float> zero = Vector<float>.Zero;
				int num = length - length % VectorMathNew.VectorSize;
				fixed (float* pinnableReference = input.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = result.GetPinnableReference())
					{
						float* ptr2 = pinnableReference2;
						for (int i = 0; i < num; i += VectorMathNew.VectorSize)
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

		// Token: 0x06000232 RID: 562 RVA: 0x0001E1EC File Offset: 0x0001C3EC
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void ReluGradient(ReadOnlySpan<float> input, ReadOnlySpan<float> gradOutput, Span<float> result)
		{
			if (input.Length != gradOutput.Length || input.Length != result.Length)
			{
				throw new ArgumentException("数组长度必须相同");
			}
			int length = input.Length;
			if (VectorMathNew.IsSimdSupported && length >= VectorMathNew.VectorSize)
			{
				Vector<float> zero = Vector<float>.Zero;
				int num = length - length % VectorMathNew.VectorSize;
				fixed (float* pinnableReference = input.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = gradOutput.GetPinnableReference())
					{
						float* ptr2 = pinnableReference2;
						fixed (float* pinnableReference3 = result.GetPinnableReference())
						{
							float* ptr3 = pinnableReference3;
							for (int i = 0; i < num; i += VectorMathNew.VectorSize)
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

		// Token: 0x06000233 RID: 563 RVA: 0x0001E358 File Offset: 0x0001C558
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void NormalizeMinMaxClamped(ReadOnlySpan<float> input, ReadOnlySpan<float> min, ReadOnlySpan<float> max, Span<float> output)
		{
			int num = Math.Min(input.Length, Math.Min(min.Length, Math.Min(max.Length, output.Length)));
			if (num == 0)
			{
				return;
			}
			if (VectorMathNew.IsSimdSupported && num >= VectorMathNew.VectorSize)
			{
				Vector<float> zero = Vector<float>.Zero;
				Vector<float> one = Vector<float>.One;
				Vector<float> vector = new Vector<float>(0.5f);
				Vector<float> vector2 = new Vector<float>(0.0001f);
				int num2 = num - num % VectorMathNew.VectorSize;
				fixed (float* pinnableReference = input.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = min.GetPinnableReference())
					{
						float* ptr2 = pinnableReference2;
						fixed (float* pinnableReference3 = max.GetPinnableReference())
						{
							float* ptr3 = pinnableReference3;
							fixed (float* pinnableReference4 = output.GetPinnableReference())
							{
								float* ptr4 = pinnableReference4;
								for (int i = 0; i < num2; i += VectorMathNew.VectorSize)
								{
									Vector<float> vector3 = *(Vector<float>*)(ptr + i);
									Vector<float> vector4 = *(Vector<float>*)(ptr2 + i);
									Vector<float> vector5 = *(Vector<float>*)(ptr3 + i) - vector4;
									Vector<int> vector6 = Vector.GreaterThan(Vector.Abs<float>(vector5), vector2);
									Vector<float> vector7 = (vector3 - vector4) / vector5;
									Vector<float> vector8 = Vector.Max<float>(zero, Vector.Min<float>(one, vector7));
									Vector<float> vector9 = Vector.ConditionalSelect(vector6, vector8, vector);
									*(Vector<float>*)(ptr4 + i) = vector9;
								}
							}
						}
					}
				}
				for (int j = num2; j < num; j++)
				{
					float num3 = *max[j] - *min[j];
					if (Math.Abs(num3) < 0.0001f)
					{
						*output[j] = 0.5f;
					}
					else
					{
						float num4 = (*input[j] - *min[j]) / num3;
						if (num4 < 0f)
						{
							num4 = 0f;
						}
						else if (num4 > 1f)
						{
							num4 = 1f;
						}
						*output[j] = num4;
					}
				}
				return;
			}
			for (int k = 0; k < num; k++)
			{
				float num5 = *max[k] - *min[k];
				if (Math.Abs(num5) < 0.0001f)
				{
					*output[k] = 0.5f;
				}
				else
				{
					float num6 = (*input[k] - *min[k]) / num5;
					if (num6 < 0f)
					{
						num6 = 0f;
					}
					else if (num6 > 1f)
					{
						num6 = 1f;
					}
					*output[k] = num6;
				}
			}
		}

		// Token: 0x06000234 RID: 564 RVA: 0x0001E5E4 File Offset: 0x0001C7E4
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void AddScalarInPlace(Span<float> array, float scalar)
		{
			int length = array.Length;
			if (VectorMathNew.IsSimdSupported && length >= VectorMathNew.VectorSize)
			{
				Vector<float> vector = new Vector<float>(scalar);
				int num = length - length % VectorMathNew.VectorSize;
				fixed (float* pinnableReference = array.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					for (int i = 0; i < num; i += VectorMathNew.VectorSize)
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

		// Token: 0x06000235 RID: 565 RVA: 0x0001E6A4 File Offset: 0x0001C8A4
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void AddScalarMultiplyInPlace(Span<float> result, ReadOnlySpan<float> a, float scalar)
		{
			int num = Math.Min(result.Length, a.Length);
			if (VectorMathNew.IsSimdSupported && num >= VectorMathNew.VectorSize)
			{
				Vector<float> vector = new Vector<float>(scalar);
				int num2 = num - num % VectorMathNew.VectorSize;
				fixed (float* pinnableReference = result.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = a.GetPinnableReference())
					{
						float* ptr2 = pinnableReference2;
						for (int i = 0; i < num2; i += VectorMathNew.VectorSize)
						{
							Vector<float> vector2 = *(Vector<float>*)(ptr + i);
							Vector<float> vector3 = *(Vector<float>*)(ptr2 + i);
							*(Vector<float>*)(ptr + i) = vector2 + vector3 * vector;
						}
					}
				}
				for (int j = num2; j < num; j++)
				{
					*result[j] += *a[j] * scalar;
				}
				return;
			}
			for (int k = 0; k < num; k++)
			{
				*result[k] += *a[k] * scalar;
			}
		}

		// Token: 0x06000236 RID: 566 RVA: 0x0001E7B0 File Offset: 0x0001C9B0
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void ComputeWeightGrad(ReadOnlySpan<float> gradOutput, ReadOnlySpan<float> input, Span<float> weightGrad, int outDim, int inDim)
		{
			if (VectorMathNew.IsSimdSupported && outDim >= VectorMathNew.VectorSize && inDim >= VectorMathNew.VectorSize)
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
							for (j = 0; j <= inDim - VectorMathNew.VectorSize; j += VectorMathNew.VectorSize)
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

		// Token: 0x06000237 RID: 567 RVA: 0x0001E8E8 File Offset: 0x0001CAE8
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void ComputeInputGrad(ReadOnlySpan<float> gradOutput, ReadOnlySpan<float> weights, Span<float> inputGrad, int outDim, int inDim)
		{
			if (VectorMathNew.IsSimdSupported && inDim >= VectorMathNew.VectorSize && outDim >= VectorMathNew.VectorSize)
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
							for (int j = 0; j <= inDim - VectorMathNew.VectorSize; j += VectorMathNew.VectorSize)
							{
								Vector<float> vector2 = *(Vector<float>*)(ptr + num2 + j);
								Vector<float> vector3 = *(Vector<float>*)(ptr2 + j);
								*(Vector<float>*)(ptr2 + j) = vector3 + vector2 * vector;
							}
							for (int k = inDim - inDim % VectorMathNew.VectorSize; k < inDim; k++)
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

		// Token: 0x06000238 RID: 568 RVA: 0x0001EA54 File Offset: 0x0001CC54
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
			if (VectorMathNew.IsSimdSupported && inDim >= VectorMathNew.VectorSize && outDim >= VectorMathNew.VectorSize)
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
									for (int k = 0; k <= inDim - VectorMathNew.VectorSize; k += VectorMathNew.VectorSize)
									{
										Vector<float> vector2 = *(Vector<float>*)(ptr + num5 + k);
										Vector<float> vector3 = *(Vector<float>*)(ptr2 + num3 + k);
										*(Vector<float>*)(ptr2 + num3 + k) = vector3 + vector2 * vector;
									}
									for (int l = inDim - inDim % VectorMathNew.VectorSize; l < inDim; l++)
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

		// Token: 0x06000239 RID: 569 RVA: 0x0001EC94 File Offset: 0x0001CE94
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void MultiplyElementwise(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> result)
		{
			int num = Math.Min(Math.Min(a.Length, b.Length), result.Length);
			if (VectorMathNew.IsSimdSupported && num >= VectorMathNew.VectorSize)
			{
				int num2 = num - num % VectorMathNew.VectorSize;
				fixed (float* pinnableReference = a.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = b.GetPinnableReference())
					{
						float* ptr2 = pinnableReference2;
						fixed (float* pinnableReference3 = result.GetPinnableReference())
						{
							float* ptr3 = pinnableReference3;
							for (int i = 0; i < num2; i += VectorMathNew.VectorSize)
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

		// Token: 0x0600023A RID: 570 RVA: 0x0001EDB8 File Offset: 0x0001CFB8
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void BatchDotProduct(ReadOnlySpan<float> query, float[][] keys, Span<float> scores, int numKeys, int dim)
		{
			if (VectorMathNew.IsSimdSupported && dim >= VectorMathNew.VectorSize)
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
						while (j <= dim - VectorMathNew.VectorSize)
						{
							Vector<float> vector = *(Vector<float>*)(ptr + j);
							Vector<float> vector2 = *(Vector<float>*)(ptr2 + j);
							Vector<float> vector3 = vector * vector2;
							for (int k = 0; k < VectorMathNew.VectorSize; k++)
							{
								num += vector3[k];
							}
							j += VectorMathNew.VectorSize;
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

		// Token: 0x0600023B RID: 571 RVA: 0x0001EEF2 File Offset: 0x0001D0F2
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void BatchWeightedSum(ReadOnlySpan<float> weights, float[][] values, Span<float> output, int numItems, int dim)
		{
			VectorMathNew.BatchWeightedSum(weights, values, output, numItems, dim, 0);
		}

		// Token: 0x0600023C RID: 572 RVA: 0x0001EF00 File Offset: 0x0001D100
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void BatchWeightedSum(ReadOnlySpan<float> weights, float[][] values, Span<float> output, int numItems, int dim, int offset)
		{
			if (VectorMathNew.IsSimdSupported && dim >= VectorMathNew.VectorSize)
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
							for (int j = 0; j <= dim - VectorMathNew.VectorSize; j += VectorMathNew.VectorSize)
							{
								Vector<float> vector2 = *(Vector<float>*)(ptr2 + offset + j);
								Vector<float> vector3 = *(Vector<float>*)(ptr + j);
								*(Vector<float>*)(ptr + j) = vector3 + vector2 * vector;
							}
							array = null;
							for (int k = dim - dim % VectorMathNew.VectorSize; k < dim; k++)
							{
								*output[k] += values[i][offset + k] * num;
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
					num2 += *weights[m] * values[m][offset + l];
				}
				*output[l] = num2;
			}
		}

		// Token: 0x0600023D RID: 573 RVA: 0x0001F06C File Offset: 0x0001D26C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void ComputeBatchWeightGrad(ReadOnlySpan<float> gradOutputs, float[][] inputs, Span<float> weightGrad, int batchSize, int outDim, int inDim)
		{
			if (VectorMathNew.IsSimdSupported && inDim >= VectorMathNew.VectorSize)
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
								for (int k = 0; k <= inDim - VectorMathNew.VectorSize; k += VectorMathNew.VectorSize)
								{
									Vector<float> vector = *(Vector<float>*)(ptr2 + k);
									Vector<float> vector2 = new Vector<float>(num2);
									Vector<float> vector3 = *(Vector<float>*)(ptr + num + k);
									*(Vector<float>*)(ptr + num + k) = vector3 + vector * vector2;
								}
								for (int l = inDim - inDim % VectorMathNew.VectorSize; l < inDim; l++)
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

		// Token: 0x0600023E RID: 574 RVA: 0x0001F210 File Offset: 0x0001D410
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void BatchWeightedDotProduct(ReadOnlySpan<float> weights, float[][] values, Span<float> result, int numItems, int dim)
		{
			if (VectorMathNew.IsSimdSupported && dim >= VectorMathNew.VectorSize)
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
							for (int j = 0; j <= dim - VectorMathNew.VectorSize; j += VectorMathNew.VectorSize)
							{
								Vector<float> vector = *(Vector<float>*)(ptr2 + j);
								Vector<float> vector2 = *(Vector<float>*)(ptr + j);
								*(Vector<float>*)(ptr + j) = vector2 + vector * new Vector<float>(num);
							}
							array = null;
							for (int k = dim - dim % VectorMathNew.VectorSize; k < dim; k++)
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

		// Token: 0x0600023F RID: 575 RVA: 0x0001F36C File Offset: 0x0001D56C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void BatchAccumulateWeighted(float[] weights, float[][] values, float[] output, int numItems, int dim, float scale = 1f)
		{
			if (VectorMathNew.IsSimdSupported && dim >= VectorMathNew.VectorSize)
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
							for (int j = 0; j <= dim - VectorMathNew.VectorSize; j += VectorMathNew.VectorSize)
							{
								Vector<float> vector = *(Vector<float>*)(ptr2 + j);
								Vector<float> vector2 = *(Vector<float>*)(ptr + j);
								*(Vector<float>*)(ptr + j) = vector2 + vector * new Vector<float>(num);
							}
							array2 = null;
							for (int k = dim - dim % VectorMathNew.VectorSize; k < dim; k++)
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

		// Token: 0x06000240 RID: 576 RVA: 0x0001F4BC File Offset: 0x0001D6BC
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void BatchQueryKeyDotProduct(ReadOnlySpan<float> query, float[][] keys, ReadOnlySpan<float> weights, Span<float> result, int numKeys, int dim)
		{
			if (VectorMathNew.IsSimdSupported && dim >= VectorMathNew.VectorSize)
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
								for (j = 0; j <= dim - VectorMathNew.VectorSize; j += VectorMathNew.VectorSize)
								{
									Vector<float> vector = *(Vector<float>*)(ptr + j);
									Vector<float> vector2 = *(Vector<float>*)(ptr3 + j);
									Vector<float> vector3 = vector * vector2;
									for (int k = 0; k < VectorMathNew.VectorSize; k++)
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

		// Token: 0x06000241 RID: 577 RVA: 0x0001F678 File Offset: 0x0001D878
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void BatchVectorsDotVector(float[][] a, ReadOnlySpan<float> b, Span<float> result, int numItems, int dim)
		{
			if (VectorMathNew.IsSimdSupported && dim >= VectorMathNew.VectorSize)
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
							for (j = 0; j <= dim - VectorMathNew.VectorSize; j += VectorMathNew.VectorSize)
							{
								Vector<float> vector = *(Vector<float>*)(ptr3 + j);
								Vector<float> vector2 = *(Vector<float>*)(ptr + j);
								Vector<float> vector3 = vector * vector2;
								for (int k = 0; k < VectorMathNew.VectorSize; k++)
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

		// Token: 0x06000242 RID: 578 RVA: 0x0001F7D4 File Offset: 0x0001D9D4
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void BatchMatrixVectorMultiply(ReadOnlySpan<float> matrices, ReadOnlySpan<float> vector, Span<float> results, int batchSize, int rowsPerMatrix, int cols)
		{
			if (VectorMathNew.IsSimdSupported && cols >= VectorMathNew.VectorSize && rowsPerMatrix >= VectorMathNew.VectorSize)
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
									for (k = 0; k <= cols - VectorMathNew.VectorSize; k += VectorMathNew.VectorSize)
									{
										Vector<float> vector2 = *(Vector<float>*)(ptr + num3 + k);
										Vector<float> vector3 = *(Vector<float>*)(ptr2 + k);
										Vector<float> vector4 = vector2 * vector3;
										for (int l = 0; l < VectorMathNew.VectorSize; l++)
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

		// Token: 0x06000243 RID: 579 RVA: 0x0001F9A0 File Offset: 0x0001DBA0
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void BatchMatrixVectorMultiplyWithBias(ReadOnlySpan<float> matrices, ReadOnlySpan<float> vector, ReadOnlySpan<float> bias, Span<float> results, int batchSize, int rowsPerMatrix, int cols)
		{
			if (VectorMathNew.IsSimdSupported && cols >= VectorMathNew.VectorSize && rowsPerMatrix >= VectorMathNew.VectorSize)
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
										for (k = 0; k <= cols - VectorMathNew.VectorSize; k += VectorMathNew.VectorSize)
										{
											Vector<float> vector2 = *(Vector<float>*)(ptr + num3 + k);
											Vector<float> vector3 = *(Vector<float>*)(ptr2 + k);
											Vector<float> vector4 = vector2 * vector3;
											for (int l = 0; l < VectorMathNew.VectorSize; l++)
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

		// Token: 0x06000244 RID: 580 RVA: 0x0001FBA0 File Offset: 0x0001DDA0
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void BatchTransposeMultiply(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> result, int batchSize, int rows, int cols)
		{
			if (VectorMathNew.IsSimdSupported && cols >= VectorMathNew.VectorSize && rows >= VectorMathNew.VectorSize)
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
										for (l = 0; l <= rows - VectorMathNew.VectorSize; l += VectorMathNew.VectorSize)
										{
											Vector<float> vector = *(Vector<float>*)(ptr + num4 + l * cols);
											Vector<float> vector2 = *(Vector<float>*)(ptr2 + num2 + l);
											Vector<float> vector3 = vector * vector2;
											for (int m = 0; m < VectorMathNew.VectorSize; m++)
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

		// Token: 0x06000245 RID: 581 RVA: 0x0001FDD0 File Offset: 0x0001DFD0
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void BatchMatMul(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> c, int batchSize, int m, int n, int k)
		{
			if (VectorMathNew.IsSimdSupported && k >= VectorMathNew.VectorSize)
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
										for (num5 = 0; num5 <= k - VectorMathNew.VectorSize; num5 += VectorMathNew.VectorSize)
										{
											Vector<float> vector = *(Vector<float>*)(ptr + num + j * k + num5);
											Vector<float> vector2 = *(Vector<float>*)(ptr2 + num2 + num5 * n + l);
											Vector<float> vector3 = vector * vector2;
											for (int num6 = 0; num6 < VectorMathNew.VectorSize; num6++)
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

		// Token: 0x06000246 RID: 582 RVA: 0x00020008 File Offset: 0x0001E208
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void BatchAdd(ReadOnlySpan<float> left, ReadOnlySpan<float> right, Span<float> results, int batchSize, int vectorSize)
		{
			int num = batchSize * vectorSize;
			if (VectorMathNew.IsSimdSupported && num >= VectorMathNew.VectorSize)
			{
				int num2 = num - num % VectorMathNew.VectorSize;
				fixed (float* pinnableReference = left.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = right.GetPinnableReference())
					{
						float* ptr2 = pinnableReference2;
						fixed (float* pinnableReference3 = results.GetPinnableReference())
						{
							float* ptr3 = pinnableReference3;
							for (int i = 0; i < num2; i += VectorMathNew.VectorSize)
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

		// Token: 0x06000247 RID: 583 RVA: 0x00020114 File Offset: 0x0001E314
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void BatchSoftmax(Span<float> values, int batchSize, int vectorSize)
		{
			if (VectorMathNew.IsSimdSupported && vectorSize >= VectorMathNew.VectorSize)
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
						while (l <= vectorSize - VectorMathNew.VectorSize)
						{
							Vector<float> vector2 = *(Vector<float>*)(ptr + num + l);
							*(Vector<float>*)(ptr + num + l) = vector2 * vector;
							l += VectorMathNew.VectorSize;
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

		// Token: 0x06000248 RID: 584 RVA: 0x0002031C File Offset: 0x0001E51C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void BatchLayerNorm(ReadOnlySpan<float> input, ReadOnlySpan<float> scale, ReadOnlySpan<float> bias, Span<float> output, int batchSize, int vectorSize)
		{
			if (VectorMathNew.IsSimdSupported && vectorSize >= VectorMathNew.VectorSize)
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
									while (l <= vectorSize - VectorMathNew.VectorSize)
									{
										Vector<float> vector2 = *(Vector<float>*)(ptr + num + l);
										Vector<float> vector3 = *(Vector<float>*)(ptr2 + l);
										Vector<float> vector4 = *(Vector<float>*)(ptr3 + l);
										Vector<float> vector5 = new Vector<float>(num2);
										*(Vector<float>*)(ptr4 + num + l) = vector3 * (vector2 - vector5) * vector + vector4;
										l += VectorMathNew.VectorSize;
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

		// Token: 0x06000249 RID: 585 RVA: 0x000205D8 File Offset: 0x0001E7D8
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void BatchRelu(ReadOnlySpan<float> input, Span<float> output, int batchSize, int vectorSize)
		{
			int num = batchSize * vectorSize;
			if (VectorMathNew.IsSimdSupported && num >= VectorMathNew.VectorSize)
			{
				Vector<float> zero = Vector<float>.Zero;
				int num2 = num - num % VectorMathNew.VectorSize;
				fixed (float* pinnableReference = input.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = output.GetPinnableReference())
					{
						float* ptr2 = pinnableReference2;
						for (int i = 0; i < num2; i += VectorMathNew.VectorSize)
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

		// Token: 0x0600024A RID: 586 RVA: 0x000206C8 File Offset: 0x0001E8C8
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void BatchScale(ReadOnlySpan<float> input, float scale, Span<float> output, int batchSize, int vectorSize)
		{
			int num = batchSize * vectorSize;
			if (VectorMathNew.IsSimdSupported && num >= VectorMathNew.VectorSize)
			{
				Vector<float> vector = new Vector<float>(scale);
				int num2 = num - num % VectorMathNew.VectorSize;
				fixed (float* pinnableReference = input.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = output.GetPinnableReference())
					{
						float* ptr2 = pinnableReference2;
						for (int i = 0; i < num2; i += VectorMathNew.VectorSize)
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

		// Token: 0x0600024B RID: 587 RVA: 0x000207A8 File Offset: 0x0001E9A8
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static int FindMaxIndex(ReadOnlySpan<float> values, out float maxValue)
		{
			int length = values.Length;
			if (length == 0)
			{
				maxValue = 0f;
				return -1;
			}
			if (VectorMathNew.IsSimdSupported && length >= VectorMathNew.VectorSize)
			{
				maxValue = float.MinValue;
				int num = 0;
				fixed (float* pinnableReference = values.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					int i;
					for (i = 0; i <= length - VectorMathNew.VectorSize; i += VectorMathNew.VectorSize)
					{
						Vector<float> vector = *(Vector<float>*)(ptr + i);
						for (int j = 0; j < VectorMathNew.VectorSize; j++)
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

		// Token: 0x0600024C RID: 588 RVA: 0x000208B8 File Offset: 0x0001EAB8
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

		// Token: 0x0600024D RID: 589 RVA: 0x00020918 File Offset: 0x0001EB18
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void Zero(Span<float> array)
		{
			int length = array.Length;
			if (length == 0)
			{
				return;
			}
			if (VectorMathNew.IsSimdSupported && length >= VectorMathNew.VectorSize)
			{
				int num = length - length % VectorMathNew.VectorSize;
				Vector<float> zero = Vector<float>.Zero;
				fixed (float* pinnableReference = array.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					for (int i = 0; i < num; i += VectorMathNew.VectorSize)
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

		// Token: 0x0600024E RID: 590 RVA: 0x000209C4 File Offset: 0x0001EBC4
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void Copy(ReadOnlySpan<float> source, Span<float> dest)
		{
			if (source.Length != dest.Length)
			{
				throw new ArgumentException("Length mismatch");
			}
			int length = source.Length;
			if (length == 0)
			{
				return;
			}
			if (VectorMathNew.IsSimdSupported && length >= VectorMathNew.VectorSize)
			{
				int num = length - length % VectorMathNew.VectorSize;
				fixed (float* pinnableReference = source.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = dest.GetPinnableReference())
					{
						float* ptr2 = pinnableReference2;
						for (int i = 0; i < num; i += VectorMathNew.VectorSize)
						{
							Vector<float> vector = *(Vector<float>*)(ptr + i);
							*(Vector<float>*)(ptr2 + i) = vector;
						}
					}
				}
				for (int j = num; j < length; j++)
				{
					*dest[j] = *source[j];
				}
				return;
			}
			for (int k = 0; k < length; k++)
			{
				*dest[k] = *source[k];
			}
		}

		// Token: 0x0600024F RID: 591 RVA: 0x00020AB4 File Offset: 0x0001ECB4
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static bool HasInvalidValues(ReadOnlySpan<float> array)
		{
			int length = array.Length;
			if (length == 0)
			{
				return false;
			}
			if (VectorMathNew.IsSimdSupported && length >= VectorMathNew.VectorSize)
			{
				int num = length - length % VectorMathNew.VectorSize;
				fixed (float* pinnableReference = array.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					for (int i = 0; i < num; i += VectorMathNew.VectorSize)
					{
						Vector<float> vector = *(Vector<float>*)(ptr + i);
						for (int j = 0; j < VectorMathNew.VectorSize; j++)
						{
							float num2 = vector[j];
							if (float.IsNaN(num2) || float.IsInfinity(num2))
							{
								return true;
							}
						}
					}
				}
				for (int k = num; k < length; k++)
				{
					if (float.IsNaN(*array[k]) || float.IsInfinity(*array[k]))
					{
						return true;
					}
				}
			}
			else
			{
				for (int l = 0; l < length; l++)
				{
					if (float.IsNaN(*array[l]) || float.IsInfinity(*array[l]))
					{
						return true;
					}
				}
			}
			return false;
		}

		// Token: 0x06000250 RID: 592 RVA: 0x00020BBB File Offset: 0x0001EDBB
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float EuclideanNorm(ReadOnlySpan<float> array)
		{
			return (float)Math.Sqrt((double)VectorMathNew.DotProduct(array, array));
		}

		// Token: 0x06000251 RID: 593 RVA: 0x00020BCC File Offset: 0x0001EDCC
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
				VectorMathNew.MultiplyElementwise(gradOutput, gamma, new Span<float>((void*)ptr, length));
				num2 = VectorMathNew.Sum(new ReadOnlySpan<float>((void*)ptr, length));
				float* ptr2 = stackalloc float[unchecked((UIntPtr)length) * 4];
				VectorMathNew.MultiplyElementwise(new ReadOnlySpan<float>((void*)ptr, length), xNorm, new Span<float>((void*)ptr2, length));
				num3 = VectorMathNew.Sum(new ReadOnlySpan<float>((void*)ptr2, length));
			}
			if (VectorMathNew.IsSimdSupported && length >= VectorMathNew.VectorSize)
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
								for (i = 0; i <= length - VectorMathNew.VectorSize; i += VectorMathNew.VectorSize)
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

		// Token: 0x06000252 RID: 594 RVA: 0x00020DC0 File Offset: 0x0001EFC0
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void Fill(Span<float> array, float value)
		{
			int length = array.Length;
			if (length == 0)
			{
				return;
			}
			if (VectorMathNew.IsSimdSupported && length >= VectorMathNew.VectorSize)
			{
				Vector<float> vector = new Vector<float>(value);
				int num = length - length % VectorMathNew.VectorSize;
				fixed (float* pinnableReference = array.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					for (int i = 0; i < num; i += VectorMathNew.VectorSize)
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

		// Token: 0x06000253 RID: 595 RVA: 0x00020E64 File Offset: 0x0001F064
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void ZeroInt(Span<int> array)
		{
			int length = array.Length;
			if (length == 0)
			{
				return;
			}
			if (VectorMathNew.IsSimdSupported && length >= Vector<int>.Count)
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

		// Token: 0x06000254 RID: 596 RVA: 0x00020F08 File Offset: 0x0001F108
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void FillInt(Span<int> array, int value)
		{
			int length = array.Length;
			if (length == 0)
			{
				return;
			}
			if (VectorMathNew.IsSimdSupported && length >= Vector<int>.Count)
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

		// Token: 0x06000255 RID: 597 RVA: 0x00020FAC File Offset: 0x0001F1AC
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void EluPlusOne(ReadOnlySpan<float> input, Span<float> output)
		{
			int length = input.Length;
			if (length != output.Length)
			{
				throw new ArgumentException("数组长度必须相同");
			}
			if (VectorMathNew.IsSimdSupported && length >= VectorMathNew.VectorSize)
			{
				Vector<float> vector = new Vector<float>(1f);
				Vector<float> zero = Vector<float>.Zero;
				int num = length - length % VectorMathNew.VectorSize;
				fixed (float* pinnableReference = input.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = output.GetPinnableReference())
					{
						float* ptr2 = pinnableReference2;
						for (int i = 0; i < num; i += VectorMathNew.VectorSize)
						{
							Vector<float> vector2 = *(Vector<float>*)(ptr + i);
							Vector<int> vector3 = Vector.GreaterThan(vector2, zero);
							Vector<float> vector4 = VectorMathNew.ExpVectorClamped(vector2);
							Vector<float> vector5 = vector2 + vector;
							Vector<float> vector6 = vector4 + vector;
							*(Vector<float>*)(ptr2 + i) = Vector.ConditionalSelect(vector3, vector5, vector6);
						}
					}
				}
				for (int j = num; j < length; j++)
				{
					float num2 = *input[j];
					if (num2 > 80f)
					{
						num2 = 80f;
					}
					else if (num2 < -80f)
					{
						num2 = -80f;
					}
					*output[j] = ((num2 > 0f) ? (num2 + 1f) : ((float)Math.Exp((double)num2) + 1f));
				}
				return;
			}
			for (int k = 0; k < length; k++)
			{
				float num3 = *input[k];
				if (num3 > 80f)
				{
					num3 = 80f;
				}
				else if (num3 < -80f)
				{
					num3 = -80f;
				}
				*output[k] = ((num3 > 0f) ? (num3 + 1f) : ((float)Math.Exp((double)num3) + 1f));
			}
		}

		// Token: 0x06000256 RID: 598 RVA: 0x0002115C File Offset: 0x0001F35C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe static Vector<float> ExpVectorClamped(Vector<float> v)
		{
			float* ptr;
			checked
			{
				ptr = stackalloc float[unchecked((UIntPtr)VectorMathNew.VectorSize) * 4];
			}
			for (int i = 0; i < VectorMathNew.VectorSize; i++)
			{
				float num = v[i];
				if (num > 80f)
				{
					num = 80f;
				}
				else if (num < -80f)
				{
					num = -80f;
				}
				ptr[i] = (float)Math.Exp((double)num);
			}
			return *(Vector<float>*)ptr;
		}

		// Token: 0x06000257 RID: 599 RVA: 0x000211C0 File Offset: 0x0001F3C0
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe static Vector<float> ExpVector(Vector<float> v)
		{
			float* ptr;
			checked
			{
				ptr = stackalloc float[unchecked((UIntPtr)VectorMathNew.VectorSize) * 4];
			}
			for (int i = 0; i < VectorMathNew.VectorSize; i++)
			{
				ptr[i] = (float)Math.Exp((double)v[i]);
			}
			return *(Vector<float>*)ptr;
		}

		// Token: 0x06000258 RID: 600 RVA: 0x00021204 File Offset: 0x0001F404
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void EluPlusOneBackward(ReadOnlySpan<float> input, ReadOnlySpan<float> gradOutput, Span<float> gradInput)
		{
			int length = input.Length;
			if (length != gradOutput.Length || length != gradInput.Length)
			{
				throw new ArgumentException("数组长度必须相同");
			}
			if (VectorMathNew.IsSimdSupported && length >= VectorMathNew.VectorSize)
			{
				Vector<float> zero = Vector<float>.Zero;
				int num = length - length % VectorMathNew.VectorSize;
				fixed (float* pinnableReference = input.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = gradOutput.GetPinnableReference())
					{
						float* ptr2 = pinnableReference2;
						fixed (float* pinnableReference3 = gradInput.GetPinnableReference())
						{
							float* ptr3 = pinnableReference3;
							for (int i = 0; i < num; i += VectorMathNew.VectorSize)
							{
								Vector<float> vector = *(Vector<float>*)(ptr + i);
								Vector<float> vector2 = *(Vector<float>*)(ptr2 + i);
								Vector<int> vector3 = Vector.GreaterThan(vector, zero);
								Vector<float> vector4 = VectorMathNew.ExpVector(vector);
								Vector<float> vector5 = vector2 * vector4;
								*(Vector<float>*)(ptr3 + i) = Vector.ConditionalSelect(vector3, vector2, vector5);
							}
						}
					}
				}
				for (int j = num; j < length; j++)
				{
					float num2 = *input[j];
					float num3 = *gradOutput[j];
					*gradInput[j] = ((num2 > 0f) ? num3 : (num3 * (float)Math.Exp((double)num2)));
				}
				return;
			}
			for (int k = 0; k < length; k++)
			{
				float num4 = *input[k];
				float num5 = *gradOutput[k];
				*gradInput[k] = ((num4 > 0f) ? num5 : (num5 * (float)Math.Exp((double)num4)));
			}
		}

		// Token: 0x06000259 RID: 601 RVA: 0x0002138C File Offset: 0x0001F58C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void ScaleAndAccumulate(ReadOnlySpan<float> src, Span<float> result, float scale)
		{
			int length = src.Length;
			if (length != result.Length)
			{
				throw new ArgumentException("数组长度必须相同");
			}
			if (VectorMathNew.IsSimdSupported && length >= VectorMathNew.VectorSize)
			{
				Vector<float> vector = new Vector<float>(scale);
				int num = length - length % VectorMathNew.VectorSize;
				fixed (float* pinnableReference = src.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = result.GetPinnableReference())
					{
						float* ptr2 = pinnableReference2;
						for (int i = 0; i < num; i += VectorMathNew.VectorSize)
						{
							Vector<float> vector2 = *(Vector<float>*)(ptr + i);
							Vector<float> vector3 = *(Vector<float>*)(ptr2 + i);
							*(Vector<float>*)(ptr2 + i) = vector3 + vector2 * vector;
						}
					}
				}
				for (int j = num; j < length; j++)
				{
					*result[j] += *src[j] * scale;
				}
				return;
			}
			for (int k = 0; k < length; k++)
			{
				*result[k] += *src[k] * scale;
			}
		}

		// Token: 0x0600025A RID: 602 RVA: 0x000214A0 File Offset: 0x0001F6A0
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void BatchReduceSum(float[][] inputs, Span<float> result, int numItems, int dim)
		{
			if (numItems == 0)
			{
				return;
			}
			VectorMathNew.Zero(result);
			if (VectorMathNew.IsSimdSupported && dim >= VectorMathNew.VectorSize)
			{
				fixed (float* pinnableReference = result.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					for (int i = 0; i < numItems; i++)
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
						for (int j = 0; j <= dim - VectorMathNew.VectorSize; j += VectorMathNew.VectorSize)
						{
							Vector<float> vector = *(Vector<float>*)(ptr2 + j);
							Vector<float> vector2 = *(Vector<float>*)(ptr + j);
							*(Vector<float>*)(ptr + j) = vector2 + vector;
						}
						for (int k = dim - dim % VectorMathNew.VectorSize; k < dim; k++)
						{
							*result[k] += inputs[i][k];
						}
						array = null;
					}
				}
				return;
			}
			for (int l = 0; l < numItems; l++)
			{
				for (int m = 0; m < dim; m++)
				{
					*result[m] += inputs[l][m];
				}
			}
		}

		// Token: 0x0600025B RID: 603 RVA: 0x000215B8 File Offset: 0x0001F7B8
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void BatchScaleAndAccumulateStrided(float[][] inputs, float[] scales, float[][] results, int numItems, int srcDim, int stride)
		{
			if (VectorMathNew.IsSimdSupported && stride >= VectorMathNew.VectorSize)
			{
				for (int i = 0; i < numItems; i++)
				{
					float num = scales[i];
					if (Math.Abs(num) >= 1E-10f)
					{
						Vector<float> vector = new Vector<float>(num);
						float[] array;
						float* ptr;
						if ((array = inputs[i]) == null || array.Length == 0)
						{
							ptr = null;
						}
						else
						{
							ptr = &array[0];
						}
						float[] array2;
						float* ptr2;
						if ((array2 = results[i]) == null || array2.Length == 0)
						{
							ptr2 = null;
						}
						else
						{
							ptr2 = &array2[0];
						}
						for (int j = 0; j <= srcDim - VectorMathNew.VectorSize; j += VectorMathNew.VectorSize)
						{
							Vector<float> vector2 = *(Vector<float>*)(ptr + j);
							Vector<float> vector3 = *(Vector<float>*)(ptr2 + j % stride);
							*(Vector<float>*)(ptr2 + j % stride) = vector3 + vector2 * vector;
						}
						for (int k = srcDim - srcDim % VectorMathNew.VectorSize; k < srcDim; k++)
						{
							results[i][k % stride] += inputs[i][k] * num;
						}
						array2 = null;
						array = null;
					}
				}
				return;
			}
			for (int l = 0; l < numItems; l++)
			{
				float num2 = scales[l];
				for (int m = 0; m < srcDim; m++)
				{
					results[l][m % stride] += inputs[l][m] * num2;
				}
			}
		}

		// Token: 0x0600025C RID: 604 RVA: 0x00021720 File Offset: 0x0001F920
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void BatchOuterProductAccumulate(float[] gradKV, float[][] vProjBatch, float[][] gradKPerCore, int numCores, int dModel, int dK)
		{
			Span<float> span = gradKV.AsSpan<float>();
			if (VectorMathNew.IsSimdSupported && dK >= VectorMathNew.VectorSize)
			{
				fixed (float* pinnableReference = span.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					for (int i = 0; i < numCores; i++)
					{
						float[] array;
						float* ptr2;
						if ((array = gradKPerCore[i]) == null || array.Length == 0)
						{
							ptr2 = null;
						}
						else
						{
							ptr2 = &array[0];
						}
						float[] array2;
						float* ptr3;
						if ((array2 = vProjBatch[i]) == null || array2.Length == 0)
						{
							ptr3 = null;
						}
						else
						{
							ptr3 = &array2[0];
						}
						for (int j = 0; j <= dModel - VectorMathNew.VectorSize; j += VectorMathNew.VectorSize)
						{
							Vector<float> vector = *(Vector<float>*)(ptr + j);
							Vector<float> vector2 = *(Vector<float>*)(ptr3 + j);
							int num = j % dK;
							Vector<float> vector3 = *(Vector<float>*)(ptr2 + num);
							*(Vector<float>*)(ptr2 + num) = vector3 + vector * vector2;
						}
						for (int k = dModel - dModel % VectorMathNew.VectorSize; k < dModel; k++)
						{
							gradKPerCore[i][k % dK] += gradKV[k] * vProjBatch[i][k];
						}
						array2 = null;
						array = null;
					}
				}
				return;
			}
			for (int l = 0; l < numCores; l++)
			{
				for (int m = 0; m < dModel; m++)
				{
					gradKPerCore[l][m % dK] += gradKV[m] * vProjBatch[l][m];
				}
			}
		}

		// Token: 0x0600025D RID: 605 RVA: 0x00021890 File Offset: 0x0001FA90
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void FusedMultiplyAddDivide(ReadOnlySpan<float> a, ReadOnlySpan<float> b, ReadOnlySpan<float> c, Span<float> output, float invScale)
		{
			int num = Math.Min(Math.Min(a.Length, b.Length), Math.Min(c.Length, output.Length));
			if (VectorMathNew.IsSimdSupported && num >= VectorMathNew.VectorSize)
			{
				Vector<float> vector = new Vector<float>(invScale);
				int num2 = num - num % VectorMathNew.VectorSize;
				fixed (float* pinnableReference = a.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = b.GetPinnableReference())
					{
						float* ptr2 = pinnableReference2;
						fixed (float* pinnableReference3 = c.GetPinnableReference())
						{
							float* ptr3 = pinnableReference3;
							fixed (float* pinnableReference4 = output.GetPinnableReference())
							{
								float* ptr4 = pinnableReference4;
								for (int i = 0; i < num2; i += VectorMathNew.VectorSize)
								{
									Vector<float> vector2 = *(Vector<float>*)(ptr + i);
									Vector<float> vector3 = *(Vector<float>*)(ptr2 + i);
									Vector<float> vector4 = *(Vector<float>*)(ptr3 + i);
									*(Vector<float>*)(ptr4 + i) = (vector2 * vector3 + vector4) * vector;
								}
							}
						}
					}
				}
				for (int j = num2; j < num; j++)
				{
					*output[j] = (*a[j] * *b[j] + *c[j]) * invScale;
				}
				return;
			}
			for (int k = 0; k < num; k++)
			{
				*output[k] = (*a[k] * *b[k] + *c[k]) * invScale;
			}
		}

		// Token: 0x0600025E RID: 606 RVA: 0x00021A18 File Offset: 0x0001FC18
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void BatchScaleAccumulate(float[][] inputs, ReadOnlySpan<float> scales, Span<float> result, int numItems, int dim)
		{
			if (VectorMathNew.IsSimdSupported && dim >= VectorMathNew.VectorSize)
			{
				VectorMathNew.Zero(result);
				for (int i = 0; i < numItems; i++)
				{
					float num = *scales[i];
					if (Math.Abs(num) >= 1E-10f)
					{
						Vector<float> vector = new Vector<float>(num);
						float[] array;
						float* ptr;
						if ((array = inputs[i]) == null || array.Length == 0)
						{
							ptr = null;
						}
						else
						{
							ptr = &array[0];
						}
						fixed (float* pinnableReference = result.GetPinnableReference())
						{
							float* ptr2 = pinnableReference;
							for (int j = 0; j <= dim - VectorMathNew.VectorSize; j += VectorMathNew.VectorSize)
							{
								Vector<float> vector2 = *(Vector<float>*)(ptr + j);
								Vector<float> vector3 = *(Vector<float>*)(ptr2 + j);
								*(Vector<float>*)(ptr2 + j) = vector3 + vector2 * vector;
							}
							for (int k = dim - dim % VectorMathNew.VectorSize; k < dim; k++)
							{
								*result[k] += inputs[i][k] * num;
							}
						}
						array = null;
					}
				}
				return;
			}
			for (int l = 0; l < numItems; l++)
			{
				float num2 = *scales[l];
				for (int m = 0; m < dim; m++)
				{
					*result[m] += inputs[l][m] * num2;
				}
			}
		}

		// Token: 0x0600025F RID: 607 RVA: 0x00021B70 File Offset: 0x0001FD70
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void Softmax(ReadOnlySpan<float> input, Span<float> output)
		{
			int length = input.Length;
			if (length != output.Length)
			{
				throw new ArgumentException("数组长度必须相同");
			}
			float num = VectorMathNew.Max(input);
			for (int i = 0; i < length; i++)
			{
				*output[i] = (float)Math.Exp((double)(*input[i] - num));
			}
			float num2 = VectorMathNew.Sum(output);
			float num3 = 1f / num2;
			VectorMathNew.MultiplyScalar(output, num3, output);
		}

		// Token: 0x06000260 RID: 608 RVA: 0x00021BF0 File Offset: 0x0001FDF0
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: TupleElementNames(new string[] { "mean", "invStd" })]
		public unsafe static ValueTuple<float, float> LayerNormForward(ReadOnlySpan<float> input, ReadOnlySpan<float> gamma, ReadOnlySpan<float> beta, Span<float> output, Span<float> xNorm, float epsilon = 1E-05f)
		{
			int length = input.Length;
			float num = VectorMathNew.Mean(input);
			float num2 = VectorMathNew.Variance(input);
			float num3 = 1f / (float)Math.Sqrt((double)(num2 + epsilon));
			if (VectorMathNew.IsSimdSupported && length >= VectorMathNew.VectorSize)
			{
				Vector<float> vector = new Vector<float>(num);
				Vector<float> vector2 = new Vector<float>(num3);
				int num4 = length - length % VectorMathNew.VectorSize;
				fixed (float* pinnableReference = input.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = gamma.GetPinnableReference())
					{
						float* ptr2 = pinnableReference2;
						fixed (float* pinnableReference3 = beta.GetPinnableReference())
						{
							float* ptr3 = pinnableReference3;
							fixed (float* pinnableReference4 = output.GetPinnableReference())
							{
								float* ptr4 = pinnableReference4;
								fixed (float* pinnableReference5 = xNorm.GetPinnableReference())
								{
									float* ptr5 = pinnableReference5;
									for (int i = 0; i < num4; i += VectorMathNew.VectorSize)
									{
										Vector<float> vector3 = *(Vector<float>*)(ptr + i);
										Vector<float> vector4 = *(Vector<float>*)(ptr2 + i);
										Vector<float> vector5 = *(Vector<float>*)(ptr3 + i);
										Vector<float> vector6 = (vector3 - vector) * vector2;
										*(Vector<float>*)(ptr5 + i) = vector6;
										*(Vector<float>*)(ptr4 + i) = vector4 * vector6 + vector5;
									}
								}
							}
						}
					}
				}
				for (int j = num4; j < length; j++)
				{
					float num5 = (*input[j] - num) * num3;
					*xNorm[j] = num5;
					*output[j] = *gamma[j] * num5 + *beta[j];
				}
			}
			else
			{
				for (int k = 0; k < length; k++)
				{
					float num6 = (*input[k] - num) * num3;
					*xNorm[k] = num6;
					*output[k] = *gamma[k] * num6 + *beta[k];
				}
			}
			return new ValueTuple<float, float>(num, num3);
		}

		// Token: 0x06000261 RID: 609 RVA: 0x00021DD4 File Offset: 0x0001FFD4
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void SoftmaxBackward(ReadOnlySpan<float> probs, ReadOnlySpan<float> gradOutput, Span<float> gradInput)
		{
			int length = probs.Length;
			if (length != gradOutput.Length || length != gradInput.Length)
			{
				throw new ArgumentException("数组长度必须相同");
			}
			float num = VectorMathNew.DotProduct(probs, gradOutput);
			if (VectorMathNew.IsSimdSupported && length >= VectorMathNew.VectorSize)
			{
				Vector<float> vector = new Vector<float>(num);
				int num2 = length - length % VectorMathNew.VectorSize;
				fixed (float* pinnableReference = probs.GetPinnableReference())
				{
					float* ptr = pinnableReference;
					fixed (float* pinnableReference2 = gradOutput.GetPinnableReference())
					{
						float* ptr2 = pinnableReference2;
						fixed (float* pinnableReference3 = gradInput.GetPinnableReference())
						{
							float* ptr3 = pinnableReference3;
							for (int i = 0; i < num2; i += VectorMathNew.VectorSize)
							{
								Vector<float> vector2 = *(Vector<float>*)(ptr + i);
								Vector<float> vector3 = *(Vector<float>*)(ptr2 + i);
								*(Vector<float>*)(ptr3 + i) = vector2 * (vector3 - vector);
							}
						}
					}
				}
				for (int j = num2; j < length; j++)
				{
					*gradInput[j] = *probs[j] * (*gradOutput[j] - num);
				}
				return;
			}
			for (int k = 0; k < length; k++)
			{
				*gradInput[k] = *probs[k] * (*gradOutput[k] - num);
			}
		}
	}
}
