using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace SimdLibrary;

public static class VectorMathNew
{
	public static bool IsSimdSupported => Vector.IsHardwareAccelerated;

	public static int VectorSize => Vector<float>.Count;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void Add(ReadOnlySpan<float> left, ReadOnlySpan<float> right, Span<float> result)
	{
		if (left.Length != right.Length || left.Length != result.Length)
		{
			throw new ArgumentException("数组长度必须相同");
		}
		int length = left.Length;
		if (IsSimdSupported && length >= VectorSize)
		{
			int num = length - length % VectorSize;
			fixed (float* ptr = left)
			{
				fixed (float* ptr2 = right)
				{
					fixed (float* ptr3 = result)
					{
						for (int i = 0; i < num; i += VectorSize)
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
				result[j] = left[j] + right[j];
			}
		}
		else
		{
			for (int k = 0; k < length; k++)
			{
				result[k] = left[k] + right[k];
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void Subtract(ReadOnlySpan<float> left, ReadOnlySpan<float> right, Span<float> result)
	{
		if (left.Length != right.Length || left.Length != result.Length)
		{
			throw new ArgumentException("数组长度必须相同");
		}
		int length = left.Length;
		if (IsSimdSupported && length >= VectorSize)
		{
			int num = length - length % VectorSize;
			fixed (float* ptr = left)
			{
				fixed (float* ptr2 = right)
				{
					fixed (float* ptr3 = result)
					{
						for (int i = 0; i < num; i += VectorSize)
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
				result[j] = left[j] - right[j];
			}
		}
		else
		{
			for (int k = 0; k < length; k++)
			{
				result[k] = left[k] - right[k];
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void Multiply(ReadOnlySpan<float> left, ReadOnlySpan<float> right, Span<float> result)
	{
		if (left.Length != right.Length || left.Length != result.Length)
		{
			throw new ArgumentException("数组长度必须相同");
		}
		int length = left.Length;
		if (IsSimdSupported && length >= VectorSize)
		{
			int num = length - length % VectorSize;
			fixed (float* ptr = left)
			{
				fixed (float* ptr2 = right)
				{
					fixed (float* ptr3 = result)
					{
						for (int i = 0; i < num; i += VectorSize)
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
				result[j] = left[j] * right[j];
			}
		}
		else
		{
			for (int k = 0; k < length; k++)
			{
				result[k] = left[k] * right[k];
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void Divide(ReadOnlySpan<float> left, ReadOnlySpan<float> right, Span<float> result)
	{
		if (left.Length != right.Length || left.Length != result.Length)
		{
			throw new ArgumentException("数组长度必须相同");
		}
		int length = left.Length;
		if (IsSimdSupported && length >= VectorSize)
		{
			int num = length - length % VectorSize;
			fixed (float* ptr = left)
			{
				fixed (float* ptr2 = right)
				{
					fixed (float* ptr3 = result)
					{
						for (int i = 0; i < num; i += VectorSize)
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
				result[j] = left[j] / right[j];
			}
		}
		else
		{
			for (int k = 0; k < length; k++)
			{
				result[k] = left[k] / right[k];
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void MultiplyScalar(ReadOnlySpan<float> array, float scalar, Span<float> result)
	{
		if (array.Length != result.Length)
		{
			throw new ArgumentException("数组长度必须相同");
		}
		int length = array.Length;
		if (IsSimdSupported && length >= VectorSize)
		{
			Vector<float> vector = new Vector<float>(scalar);
			int num = length - length % VectorSize;
			fixed (float* ptr = array)
			{
				fixed (float* ptr2 = result)
				{
					for (int i = 0; i < num; i += VectorSize)
					{
						Vector<float> vector2 = *(Vector<float>*)(ptr + i);
						*(Vector<float>*)(ptr2 + i) = vector2 * vector;
					}
				}
			}
			for (int j = num; j < length; j++)
			{
				result[j] = array[j] * scalar;
			}
		}
		else
		{
			for (int k = 0; k < length; k++)
			{
				result[k] = array[k] * scalar;
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void MultiplyScalarInPlace(Span<float> array, float scalar)
	{
		int length = array.Length;
		if (IsSimdSupported && length >= VectorSize)
		{
			Vector<float> vector = new Vector<float>(scalar);
			int num = length - length % VectorSize;
			fixed (float* ptr = array)
			{
				for (int i = 0; i < num; i += VectorSize)
				{
					Vector<float> vector2 = *(Vector<float>*)(ptr + i);
					*(Vector<float>*)(ptr + i) = vector2 * vector;
				}
			}
			for (int j = num; j < length; j++)
			{
				array[j] *= scalar;
			}
		}
		else
		{
			for (int k = 0; k < length; k++)
			{
				array[k] *= scalar;
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static float Sum(ReadOnlySpan<float> array)
	{
		int length = array.Length;
		if (length == 0)
		{
			return 0f;
		}
		float num = 0f;
		if (IsSimdSupported && length >= VectorSize)
		{
			Vector<float> zero = Vector<float>.Zero;
			int num2 = length - length % VectorSize;
			fixed (float* ptr = array)
			{
				for (int i = 0; i < num2; i += VectorSize)
				{
					zero += *(Vector<float>*)(ptr + i);
				}
			}
			for (int j = 0; j < VectorSize; j++)
			{
				num += zero[j];
			}
			for (int k = num2; k < length; k++)
			{
				num += array[k];
			}
		}
		else
		{
			for (int l = 0; l < length; l++)
			{
				num += array[l];
			}
		}
		return num;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static float DotProduct(ReadOnlySpan<float> left, ReadOnlySpan<float> right)
	{
		int length = left.Length;
		if (length != right.Length)
		{
			throw new ArgumentException("数组长度必须相同");
		}
		float num = 0f;
		if (IsSimdSupported && length >= VectorSize)
		{
			Vector<float> zero = Vector<float>.Zero;
			int num2 = length - length % VectorSize;
			fixed (float* ptr = left)
			{
				fixed (float* ptr2 = right)
				{
					for (int i = 0; i < num2; i += VectorSize)
					{
						Vector<float> vector = *(Vector<float>*)(ptr + i);
						Vector<float> vector2 = *(Vector<float>*)(ptr2 + i);
						zero += vector * vector2;
					}
				}
			}
			for (int j = 0; j < VectorSize; j++)
			{
				num += zero[j];
			}
			for (int k = num2; k < length; k++)
			{
				num += left[k] * right[k];
			}
		}
		else
		{
			for (int l = 0; l < length; l++)
			{
				num += left[l] * right[l];
			}
		}
		return num;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static float EuclideanDistance(ReadOnlySpan<float> left, ReadOnlySpan<float> right)
	{
		int length = left.Length;
		if (length != right.Length)
		{
			throw new ArgumentException("数组长度必须相同");
		}
		float num = 0f;
		if (IsSimdSupported && length >= VectorSize)
		{
			Vector<float> zero = Vector<float>.Zero;
			int num2 = length - length % VectorSize;
			fixed (float* ptr = left)
			{
				fixed (float* ptr2 = right)
				{
					for (int i = 0; i < num2; i += VectorSize)
					{
						Vector<float> vector = *(Vector<float>*)(ptr + i);
						Vector<float> vector2 = *(Vector<float>*)(ptr2 + i);
						Vector<float> vector3 = vector - vector2;
						zero += vector3 * vector3;
					}
				}
			}
			for (int j = 0; j < VectorSize; j++)
			{
				num += zero[j];
			}
			for (int k = num2; k < length; k++)
			{
				float num3 = left[k] - right[k];
				num += num3 * num3;
			}
		}
		else
		{
			for (int l = 0; l < length; l++)
			{
				float num4 = left[l] - right[l];
				num += num4 * num4;
			}
		}
		return (float)Math.Sqrt(num);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void BatchMatrixVectorMultiply(float[][] inputs, ReadOnlySpan<float> weights, ReadOnlySpan<float> bias, float[][] outputs, int batchSize, int inDim, int outDim)
	{
		if (IsSimdSupported && inDim >= VectorSize)
		{
			int num = inDim - inDim % VectorSize;
			fixed (float* ptr = weights)
			{
				fixed (float* ptr2 = bias)
				{
					for (int i = 0; i < batchSize; i++)
					{
						fixed (float* ptr3 = inputs[i])
						{
							fixed (float* ptr4 = outputs[i])
							{
								for (int j = 0; j < outDim; j++)
								{
									float* ptr5 = ptr + j * inDim;
									Vector<float> zero = Vector<float>.Zero;
									for (int k = 0; k < num; k += VectorSize)
									{
										Vector<float> vector = *(Vector<float>*)(ptr3 + k);
										Vector<float> vector2 = *(Vector<float>*)(ptr5 + k);
										zero += vector * vector2;
									}
									float num2 = 0f;
									for (int l = 0; l < VectorSize; l++)
									{
										num2 += zero[l];
									}
									for (int m = num; m < inDim; m++)
									{
										num2 += ptr3[m] * ptr5[m];
									}
									ptr4[j] = num2 + ptr2[j];
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
			for (int num3 = 0; num3 < outDim; num3++)
			{
				float num4 = 0f;
				for (int num5 = 0; num5 < inDim; num5++)
				{
					num4 += inputs[n][num5] * weights[num3 * inDim + num5];
				}
				outputs[n][num3] = num4 + bias[num3];
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static float Max(ReadOnlySpan<float> array)
	{
		int length = array.Length;
		if (length == 0)
		{
			throw new ArgumentException("数组不能为空");
		}
		float num = array[0];
		if (IsSimdSupported && length >= VectorSize)
		{
			Vector<float> left = new Vector<float>(float.MinValue);
			int num2 = length - length % VectorSize;
			fixed (float* ptr = array)
			{
				for (int i = 0; i < num2; i += VectorSize)
				{
					Vector<float> right = *(Vector<float>*)(ptr + i);
					left = Vector.Max(left, right);
				}
			}
			for (int j = 0; j < VectorSize; j++)
			{
				if (left[j] > num)
				{
					num = left[j];
				}
			}
			for (int k = num2; k < length; k++)
			{
				if (array[k] > num)
				{
					num = array[k];
				}
			}
		}
		else
		{
			for (int l = 1; l < length; l++)
			{
				if (array[l] > num)
				{
					num = array[l];
				}
			}
		}
		return num;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static float Min(ReadOnlySpan<float> array)
	{
		int length = array.Length;
		if (length == 0)
		{
			throw new ArgumentException("数组不能为空");
		}
		float num = array[0];
		if (IsSimdSupported && length >= VectorSize)
		{
			Vector<float> left = new Vector<float>(float.MaxValue);
			int num2 = length - length % VectorSize;
			fixed (float* ptr = array)
			{
				for (int i = 0; i < num2; i += VectorSize)
				{
					Vector<float> right = *(Vector<float>*)(ptr + i);
					left = Vector.Min(left, right);
				}
			}
			for (int j = 0; j < VectorSize; j++)
			{
				if (left[j] < num)
				{
					num = left[j];
				}
			}
			for (int k = num2; k < length; k++)
			{
				if (array[k] < num)
				{
					num = array[k];
				}
			}
		}
		else
		{
			for (int l = 1; l < length; l++)
			{
				if (array[l] < num)
				{
					num = array[l];
				}
			}
		}
		return num;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void ComputeMinMaxPerColumn(ReadOnlySpan<float> windowData, int rows, int cols, Span<float> min, Span<float> max)
	{
		if (rows == 0 || cols == 0)
		{
			return;
		}
		for (int i = 0; i < cols; i++)
		{
			min[i] = windowData[i];
			max[i] = windowData[i];
		}
		if (IsSimdSupported && cols >= VectorSize)
		{
			int num = cols - cols % VectorSize;
			fixed (float* ptr = min)
			{
				fixed (float* ptr2 = max)
				{
					for (int j = 1; j < rows; j++)
					{
						int num2 = j * cols;
						fixed (float* ptr3 = windowData)
						{
							for (int k = 0; k < num; k += VectorSize)
							{
								Vector<float> right = *(Vector<float>*)(ptr3 + num2 + k);
								Vector<float> left = *(Vector<float>*)(ptr + k);
								Vector<float> left2 = *(Vector<float>*)(ptr2 + k);
								left = Vector.Min(left, right);
								left2 = Vector.Max(left2, right);
								*(Vector<float>*)(ptr + k) = left;
								*(Vector<float>*)(ptr2 + k) = left2;
							}
						}
						for (int l = num; l < cols; l++)
						{
							float num3 = windowData[num2 + l];
							if (num3 < min[l])
							{
								min[l] = num3;
							}
							if (num3 > max[l])
							{
								max[l] = num3;
							}
						}
					}
				}
			}
			return;
		}
		for (int m = 1; m < rows; m++)
		{
			int num4 = m * cols;
			for (int n = 0; n < cols; n++)
			{
				float num5 = windowData[num4 + n];
				if (num5 < min[n])
				{
					min[n] = num5;
				}
				if (num5 > max[n])
				{
					max[n] = num5;
				}
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float Mean(ReadOnlySpan<float> array)
	{
		if (array.IsEmpty)
		{
			return 0f;
		}
		return Sum(array) / (float)array.Length;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static float Variance(ReadOnlySpan<float> array)
	{
		int length = array.Length;
		if (length == 0)
		{
			return 0f;
		}
		float num = Mean(array);
		float num2 = 0f;
		if (IsSimdSupported && length >= VectorSize)
		{
			Vector<float> vector = new Vector<float>(num);
			Vector<float> zero = Vector<float>.Zero;
			int num3 = length - length % VectorSize;
			fixed (float* ptr = array)
			{
				for (int i = 0; i < num3; i += VectorSize)
				{
					Vector<float> vector2 = *(Vector<float>*)(ptr + i) - vector;
					zero += vector2 * vector2;
				}
			}
			for (int j = 0; j < VectorSize; j++)
			{
				num2 += zero[j];
			}
			for (int k = num3; k < length; k++)
			{
				float num4 = array[k] - num;
				num2 += num4 * num4;
			}
		}
		else
		{
			for (int l = 0; l < length; l++)
			{
				float num5 = array[l] - num;
				num2 += num5 * num5;
			}
		}
		return num2 / (float)length;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float StandardDeviation(ReadOnlySpan<float> array)
	{
		return (float)Math.Sqrt(Variance(array));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Exp(ReadOnlySpan<float> input, Span<float> result)
	{
		if (input.Length != result.Length)
		{
			throw new ArgumentException("数组长度必须相同");
		}
		int length = input.Length;
		for (int i = 0; i < length; i++)
		{
			result[i] = (float)Math.Exp(input[i]);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void Relu(ReadOnlySpan<float> input, Span<float> result)
	{
		if (input.Length != result.Length)
		{
			throw new ArgumentException("数组长度必须相同");
		}
		int length = input.Length;
		if (IsSimdSupported && length >= VectorSize)
		{
			Vector<float> zero = Vector<float>.Zero;
			int num = length - length % VectorSize;
			fixed (float* ptr = input)
			{
				fixed (float* ptr2 = result)
				{
					for (int i = 0; i < num; i += VectorSize)
					{
						Vector<float> right = *(Vector<float>*)(ptr + i);
						*(Vector<float>*)(ptr2 + i) = Vector.Max(zero, right);
					}
				}
			}
			for (int j = num; j < length; j++)
			{
				result[j] = Math.Max(0f, input[j]);
			}
		}
		else
		{
			for (int k = 0; k < length; k++)
			{
				result[k] = Math.Max(0f, input[k]);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void ReluGradient(ReadOnlySpan<float> input, ReadOnlySpan<float> gradOutput, Span<float> result)
	{
		if (input.Length != gradOutput.Length || input.Length != result.Length)
		{
			throw new ArgumentException("数组长度必须相同");
		}
		int length = input.Length;
		if (IsSimdSupported && length >= VectorSize)
		{
			Vector<float> zero = Vector<float>.Zero;
			int num = length - length % VectorSize;
			fixed (float* ptr = input)
			{
				fixed (float* ptr2 = gradOutput)
				{
					fixed (float* ptr3 = result)
					{
						for (int i = 0; i < num; i += VectorSize)
						{
							Vector<float> left = *(Vector<float>*)(ptr + i);
							Vector<float> vector = *(Vector<float>*)(ptr2 + i);
							Vector<float> vector2 = Vector.ConditionalSelect(Vector.GreaterThan(left, zero), Vector<float>.One, zero);
							*(Vector<float>*)(ptr3 + i) = vector2 * vector;
						}
					}
				}
			}
			for (int j = num; j < length; j++)
			{
				result[j] = ((input[j] > 0f) ? gradOutput[j] : 0f);
			}
		}
		else
		{
			for (int k = 0; k < length; k++)
			{
				result[k] = ((input[k] > 0f) ? gradOutput[k] : 0f);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void NormalizeMinMaxClamped(ReadOnlySpan<float> input, ReadOnlySpan<float> min, ReadOnlySpan<float> max, Span<float> output)
	{
		int num = Math.Min(input.Length, Math.Min(min.Length, Math.Min(max.Length, output.Length)));
		if (num == 0)
		{
			return;
		}
		if (IsSimdSupported && num >= VectorSize)
		{
			Vector<float> zero = Vector<float>.Zero;
			Vector<float> one = Vector<float>.One;
			Vector<float> right = new Vector<float>(0.5f);
			Vector<float> right2 = new Vector<float>(0.0001f);
			int num2 = num - num % VectorSize;
			fixed (float* ptr = input)
			{
				fixed (float* ptr2 = min)
				{
					fixed (float* ptr3 = max)
					{
						fixed (float* ptr4 = output)
						{
							for (int i = 0; i < num2; i += VectorSize)
							{
								Vector<float> vector = *(Vector<float>*)(ptr + i);
								Vector<float> vector2 = *(Vector<float>*)(ptr2 + i);
								Vector<float> vector3 = *(Vector<float>*)(ptr3 + i) - vector2;
								Vector<int> condition = Vector.GreaterThan(Vector.Abs(vector3), right2);
								Vector<float> right3 = (vector - vector2) / vector3;
								Vector<float> left = Vector.Max(zero, Vector.Min(one, right3));
								Vector<float> vector4 = Vector.ConditionalSelect(condition, left, right);
								*(Vector<float>*)(ptr4 + i) = vector4;
							}
						}
					}
				}
			}
			for (int j = num2; j < num; j++)
			{
				float num3 = max[j] - min[j];
				if (Math.Abs(num3) < 0.0001f)
				{
					output[j] = 0.5f;
					continue;
				}
				float num4 = (input[j] - min[j]) / num3;
				if (num4 < 0f)
				{
					num4 = 0f;
				}
				else if (num4 > 1f)
				{
					num4 = 1f;
				}
				output[j] = num4;
			}
			return;
		}
		for (int k = 0; k < num; k++)
		{
			float num5 = max[k] - min[k];
			if (Math.Abs(num5) < 0.0001f)
			{
				output[k] = 0.5f;
				continue;
			}
			float num6 = (input[k] - min[k]) / num5;
			if (num6 < 0f)
			{
				num6 = 0f;
			}
			else if (num6 > 1f)
			{
				num6 = 1f;
			}
			output[k] = num6;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void AddScalarInPlace(Span<float> array, float scalar)
	{
		int length = array.Length;
		if (IsSimdSupported && length >= VectorSize)
		{
			Vector<float> vector = new Vector<float>(scalar);
			int num = length - length % VectorSize;
			fixed (float* ptr = array)
			{
				for (int i = 0; i < num; i += VectorSize)
				{
					Vector<float> vector2 = *(Vector<float>*)(ptr + i);
					*(Vector<float>*)(ptr + i) = vector2 + vector;
				}
			}
			for (int j = num; j < length; j++)
			{
				array[j] += scalar;
			}
		}
		else
		{
			for (int k = 0; k < length; k++)
			{
				array[k] += scalar;
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void AddScalarMultiplyInPlace(Span<float> result, ReadOnlySpan<float> a, float scalar)
	{
		int num = Math.Min(result.Length, a.Length);
		if (IsSimdSupported && num >= VectorSize)
		{
			Vector<float> vector = new Vector<float>(scalar);
			int num2 = num - num % VectorSize;
			fixed (float* ptr = result)
			{
				fixed (float* ptr2 = a)
				{
					for (int i = 0; i < num2; i += VectorSize)
					{
						Vector<float> vector2 = *(Vector<float>*)(ptr + i);
						Vector<float> vector3 = *(Vector<float>*)(ptr2 + i);
						*(Vector<float>*)(ptr + i) = vector2 + vector3 * vector;
					}
				}
			}
			for (int j = num2; j < num; j++)
			{
				result[j] += a[j] * scalar;
			}
		}
		else
		{
			for (int k = 0; k < num; k++)
			{
				result[k] += a[k] * scalar;
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void ComputeWeightGrad(ReadOnlySpan<float> gradOutput, ReadOnlySpan<float> input, Span<float> weightGrad, int outDim, int inDim)
	{
		if (IsSimdSupported && outDim >= VectorSize && inDim >= VectorSize)
		{
			fixed (float* ptr = input)
			{
				fixed (float* ptr2 = weightGrad)
				{
					for (int i = 0; i < outDim; i++)
					{
						float num = gradOutput[i];
						Vector<float> vector = new Vector<float>(num);
						int num2 = i * inDim;
						int j;
						for (j = 0; j <= inDim - VectorSize; j += VectorSize)
						{
							Vector<float> vector2 = *(Vector<float>*)(ptr + j);
							*(Vector<float>*)(ptr2 + num2 + j) = vector2 * vector;
						}
						for (; j < inDim; j++)
						{
							weightGrad[num2 + j] = num * input[j];
						}
					}
				}
			}
			return;
		}
		for (int k = 0; k < outDim; k++)
		{
			float num3 = gradOutput[k];
			int num4 = k * inDim;
			for (int l = 0; l < inDim; l++)
			{
				weightGrad[num4 + l] = num3 * input[l];
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void ComputeInputGrad(ReadOnlySpan<float> gradOutput, ReadOnlySpan<float> weights, Span<float> inputGrad, int outDim, int inDim)
	{
		if (IsSimdSupported && inDim >= VectorSize && outDim >= VectorSize)
		{
			inputGrad.Clear();
			fixed (float* ptr = weights)
			{
				fixed (float* ptr2 = inputGrad)
				{
					for (int i = 0; i < outDim; i++)
					{
						float num = gradOutput[i];
						int num2 = i * inDim;
						Vector<float> vector = new Vector<float>(num);
						for (int j = 0; j <= inDim - VectorSize; j += VectorSize)
						{
							Vector<float> vector2 = *(Vector<float>*)(ptr + num2 + j);
							Vector<float> vector3 = *(Vector<float>*)(ptr2 + j);
							*(Vector<float>*)(ptr2 + j) = vector3 + vector2 * vector;
						}
						for (int k = inDim - inDim % VectorSize; k < inDim; k++)
						{
							inputGrad[k] += weights[num2 + k] * num;
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
				num3 += gradOutput[m] * weights[m * inDim + l];
			}
			inputGrad[l] = num3;
		}
	}

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
		if (IsSimdSupported && inDim >= VectorSize && outDim >= VectorSize)
		{
			inputGrads.Clear();
			fixed (float* ptr = weights)
			{
				fixed (float* ptr2 = gradOutputs)
				{
					fixed (float* ptr3 = inputGrads)
					{
						for (int i = 0; i < numCores; i++)
						{
							int num = i * outDim * inDim;
							int num2 = i * outDim;
							int num3 = i * inDim;
							for (int j = 0; j < outDim; j++)
							{
								float num4 = gradOutputs[num2 + j];
								int num5 = num + j * inDim;
								Vector<float> vector = new Vector<float>(num4);
								for (int k = 0; k <= inDim - VectorSize; k += VectorSize)
								{
									Vector<float> vector2 = *(Vector<float>*)(ptr + num5 + k);
									Vector<float> vector3 = *(Vector<float>*)(ptr3 + num3 + k);
									*(Vector<float>*)(ptr3 + num3 + k) = vector3 + vector2 * vector;
								}
								for (int l = inDim - inDim % VectorSize; l < inDim; l++)
								{
									inputGrads[num3 + l] += weights[num5 + l] * num4;
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
					num9 += gradOutputs[num7 + num10] * weights[num6 + num10 * inDim + n];
				}
				inputGrads[num8 + n] = num9;
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void MultiplyElementwise(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> result)
	{
		int num = Math.Min(Math.Min(a.Length, b.Length), result.Length);
		if (IsSimdSupported && num >= VectorSize)
		{
			int num2 = num - num % VectorSize;
			fixed (float* ptr = a)
			{
				fixed (float* ptr2 = b)
				{
					fixed (float* ptr3 = result)
					{
						for (int i = 0; i < num2; i += VectorSize)
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
				result[j] = a[j] * b[j];
			}
		}
		else
		{
			for (int k = 0; k < num; k++)
			{
				result[k] = a[k] * b[k];
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void BatchDotProduct(ReadOnlySpan<float> query, float[][] keys, Span<float> scores, int numKeys, int dim)
	{
		if (IsSimdSupported && dim >= VectorSize)
		{
			for (int i = 0; i < numKeys; i++)
			{
				float num = 0f;
				int j = 0;
				fixed (float* ptr = query)
				{
					fixed (float* ptr2 = keys[i])
					{
						for (; j <= dim - VectorSize; j += VectorSize)
						{
							Vector<float> vector = *(Vector<float>*)(ptr + j);
							Vector<float> vector2 = *(Vector<float>*)(ptr2 + j);
							Vector<float> vector3 = vector * vector2;
							for (int k = 0; k < VectorSize; k++)
							{
								num += vector3[k];
							}
						}
					}
				}
				for (; j < dim; j++)
				{
					num += query[j] * keys[i][j];
				}
				scores[i] = num;
			}
			return;
		}
		for (int l = 0; l < numKeys; l++)
		{
			float num2 = 0f;
			for (int m = 0; m < dim; m++)
			{
				num2 += query[m] * keys[l][m];
			}
			scores[l] = num2;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void BatchWeightedSum(ReadOnlySpan<float> weights, float[][] values, Span<float> output, int numItems, int dim)
	{
		BatchWeightedSum(weights, values, output, numItems, dim, 0);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void BatchWeightedSum(ReadOnlySpan<float> weights, float[][] values, Span<float> output, int numItems, int dim, int offset)
	{
		if (IsSimdSupported && dim >= VectorSize)
		{
			output.Clear();
			fixed (float* ptr = output)
			{
				for (int i = 0; i < numItems; i++)
				{
					float num = weights[i];
					if (Math.Abs(num) < 1E-10f)
					{
						continue;
					}
					Vector<float> vector = new Vector<float>(num);
					fixed (float* ptr2 = values[i])
					{
						for (int j = 0; j <= dim - VectorSize; j += VectorSize)
						{
							Vector<float> vector2 = *(Vector<float>*)(ptr2 + offset + j);
							Vector<float> vector3 = *(Vector<float>*)(ptr + j);
							*(Vector<float>*)(ptr + j) = vector3 + vector2 * vector;
						}
					}
					for (int k = dim - dim % VectorSize; k < dim; k++)
					{
						output[k] += values[i][offset + k] * num;
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
				num2 += weights[m] * values[m][offset + l];
			}
			output[l] = num2;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void ComputeBatchWeightGrad(ReadOnlySpan<float> gradOutputs, float[][] inputs, Span<float> weightGrad, int batchSize, int outDim, int inDim)
	{
		if (IsSimdSupported && inDim >= VectorSize)
		{
			fixed (float* ptr = weightGrad)
			{
				for (int i = 0; i < batchSize; i++)
				{
					fixed (float* ptr2 = inputs[i])
					{
						for (int j = 0; j < outDim; j++)
						{
							int num = j * inDim;
							float num2 = gradOutputs[i * outDim + j];
							if (!(Math.Abs(num2) < 1E-10f))
							{
								for (int k = 0; k <= inDim - VectorSize; k += VectorSize)
								{
									Vector<float> vector = *(Vector<float>*)(ptr2 + k);
									Vector<float> vector2 = new Vector<float>(num2);
									Vector<float> vector3 = *(Vector<float>*)(ptr + num + k);
									*(Vector<float>*)(ptr + num + k) = vector3 + vector * vector2;
								}
								for (int l = inDim - inDim % VectorSize; l < inDim; l++)
								{
									weightGrad[num + l] += ptr2[l] * num2;
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
			for (int n = 0; n < outDim; n++)
			{
				float num3 = gradOutputs[m * outDim + n];
				for (int num4 = 0; num4 < inDim; num4++)
				{
					weightGrad[n * inDim + num4] += inputs[m][num4] * num3;
				}
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void BatchWeightedDotProduct(ReadOnlySpan<float> weights, float[][] values, Span<float> result, int numItems, int dim)
	{
		if (IsSimdSupported && dim >= VectorSize)
		{
			result.Clear();
			fixed (float* ptr = result)
			{
				for (int i = 0; i < numItems; i++)
				{
					float num = weights[i];
					if (Math.Abs(num) < 1E-10f)
					{
						continue;
					}
					fixed (float* ptr2 = values[i])
					{
						for (int j = 0; j <= dim - VectorSize; j += VectorSize)
						{
							Vector<float> vector = *(Vector<float>*)(ptr2 + j);
							Vector<float> vector2 = *(Vector<float>*)(ptr + j);
							*(Vector<float>*)(ptr + j) = vector2 + vector * new Vector<float>(num);
						}
					}
					for (int k = dim - dim % VectorSize; k < dim; k++)
					{
						result[k] += values[i][k] * num;
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
				num2 += weights[m] * values[m][l];
			}
			result[l] = num2;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void BatchAccumulateWeighted(float[] weights, float[][] values, float[] output, int numItems, int dim, float scale = 1f)
	{
		if (IsSimdSupported && dim >= VectorSize)
		{
			fixed (float* ptr = output)
			{
				for (int i = 0; i < numItems; i++)
				{
					float num = weights[i] * scale;
					if (Math.Abs(num) < 1E-10f)
					{
						continue;
					}
					fixed (float* ptr2 = values[i])
					{
						for (int j = 0; j <= dim - VectorSize; j += VectorSize)
						{
							Vector<float> vector = *(Vector<float>*)(ptr2 + j);
							Vector<float> vector2 = *(Vector<float>*)(ptr + j);
							*(Vector<float>*)(ptr + j) = vector2 + vector * new Vector<float>(num);
						}
					}
					for (int k = dim - dim % VectorSize; k < dim; k++)
					{
						output[k] += values[i][k] * num;
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void BatchQueryKeyDotProduct(ReadOnlySpan<float> query, float[][] keys, ReadOnlySpan<float> weights, Span<float> result, int numKeys, int dim)
	{
		if (IsSimdSupported && dim >= VectorSize)
		{
			fixed (float* ptr = query)
			{
				fixed (float* ptr2 = result)
				{
					for (int i = 0; i < numKeys; i++)
					{
						float num = weights[i];
						if (Math.Abs(num) < 1E-10f)
						{
							ptr2[i] = 0f;
							continue;
						}
						fixed (float* ptr3 = keys[i])
						{
							float num2 = 0f;
							int j;
							for (j = 0; j <= dim - VectorSize; j += VectorSize)
							{
								Vector<float> vector = *(Vector<float>*)(ptr + j);
								Vector<float> vector2 = *(Vector<float>*)(ptr3 + j);
								Vector<float> vector3 = vector * vector2;
								for (int k = 0; k < VectorSize; k++)
								{
									num2 += vector3[k];
								}
							}
							for (; j < dim; j++)
							{
								num2 += query[j] * keys[i][j];
							}
							ptr2[i] = num2 * num;
						}
					}
				}
			}
			return;
		}
		for (int l = 0; l < numKeys; l++)
		{
			float num3 = weights[l];
			if (Math.Abs(num3) < 1E-10f)
			{
				result[l] = 0f;
				continue;
			}
			float num4 = 0f;
			for (int m = 0; m < dim; m++)
			{
				num4 += query[m] * keys[l][m];
			}
			result[l] = num4 * num3;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void BatchVectorsDotVector(float[][] a, ReadOnlySpan<float> b, Span<float> result, int numItems, int dim)
	{
		if (IsSimdSupported && dim >= VectorSize)
		{
			fixed (float* ptr = b)
			{
				fixed (float* ptr2 = result)
				{
					for (int i = 0; i < numItems; i++)
					{
						fixed (float* ptr3 = a[i])
						{
							float num = 0f;
							int j;
							for (j = 0; j <= dim - VectorSize; j += VectorSize)
							{
								Vector<float> vector = *(Vector<float>*)(ptr3 + j);
								Vector<float> vector2 = *(Vector<float>*)(ptr + j);
								Vector<float> vector3 = vector * vector2;
								for (int k = 0; k < VectorSize; k++)
								{
									num += vector3[k];
								}
							}
							for (; j < dim; j++)
							{
								num += a[i][j] * b[j];
							}
							ptr2[i] = num;
						}
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
				num2 += a[l][m] * b[m];
			}
			result[l] = num2;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void BatchMatrixVectorMultiply(ReadOnlySpan<float> matrices, ReadOnlySpan<float> vector, Span<float> results, int batchSize, int rowsPerMatrix, int cols)
	{
		if (IsSimdSupported && cols >= VectorSize && rowsPerMatrix >= VectorSize)
		{
			fixed (float* ptr = matrices)
			{
				fixed (float* ptr2 = vector)
				{
					fixed (float* ptr3 = results)
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
								for (k = 0; k <= cols - VectorSize; k += VectorSize)
								{
									Vector<float> vector2 = *(Vector<float>*)(ptr + num3 + k);
									Vector<float> vector3 = *(Vector<float>*)(ptr2 + k);
									Vector<float> vector4 = vector2 * vector3;
									for (int l = 0; l < VectorSize; l++)
									{
										num4 += vector4[l];
									}
								}
								for (; k < cols; k++)
								{
									num4 += matrices[num3 + k] * vector[k];
								}
								results[num2 + j] = num4;
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
					num7 += matrices[num8 + num9] * vector[num9];
				}
				results[num6 + n] = num7;
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void BatchMatrixVectorMultiplyWithBias(ReadOnlySpan<float> matrices, ReadOnlySpan<float> vector, ReadOnlySpan<float> bias, Span<float> results, int batchSize, int rowsPerMatrix, int cols)
	{
		if (IsSimdSupported && cols >= VectorSize && rowsPerMatrix >= VectorSize)
		{
			fixed (float* ptr = matrices)
			{
				fixed (float* ptr2 = vector)
				{
					fixed (float* ptr3 = bias)
					{
						fixed (float* ptr4 = results)
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
									for (k = 0; k <= cols - VectorSize; k += VectorSize)
									{
										Vector<float> vector2 = *(Vector<float>*)(ptr + num3 + k);
										Vector<float> vector3 = *(Vector<float>*)(ptr2 + k);
										Vector<float> vector4 = vector2 * vector3;
										for (int l = 0; l < VectorSize; l++)
										{
											num4 += vector4[l];
										}
									}
									for (; k < cols; k++)
									{
										num4 += matrices[num3 + k] * vector[k];
									}
									results[num2 + j] = num4 + bias[num2 + j];
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
					num7 += matrices[num8 + num9] * vector[num9];
				}
				results[num6 + n] = num7 + bias[num6 + n];
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void BatchTransposeMultiply(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> result, int batchSize, int rows, int cols)
	{
		if (IsSimdSupported && cols >= VectorSize && rows >= VectorSize)
		{
			fixed (float* ptr = a)
			{
				fixed (float* ptr2 = b)
				{
					fixed (float* ptr3 = result)
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
									for (l = 0; l <= rows - VectorSize; l += VectorSize)
									{
										Vector<float> vector = *(Vector<float>*)(ptr + num4 + l * cols);
										Vector<float> vector2 = *(Vector<float>*)(ptr2 + num2 + l);
										Vector<float> vector3 = vector * vector2;
										for (int m = 0; m < VectorSize; m++)
										{
											num5 += vector3[m];
										}
									}
									for (; l < rows; l++)
									{
										num5 += a[num + l * cols + k] * b[num2 + l];
									}
									result[num3 + j * cols + k] = num5;
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
						num11 += a[num6 + num12 * cols + num10] * b[num7 + num12];
					}
					result[num8 + num9 * cols + num10] = num11;
				}
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void BatchMatMul(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> c, int batchSize, int m, int n, int k)
	{
		if (IsSimdSupported && k >= VectorSize)
		{
			fixed (float* ptr = a)
			{
				fixed (float* ptr2 = b)
				{
					fixed (float* ptr3 = c)
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
									for (num5 = 0; num5 <= k - VectorSize; num5 += VectorSize)
									{
										Vector<float> vector = *(Vector<float>*)(ptr + num + j * k + num5);
										Vector<float> vector2 = *(Vector<float>*)(ptr2 + num2 + num5 * n + l);
										Vector<float> vector3 = vector * vector2;
										for (int num6 = 0; num6 < VectorSize; num6++)
										{
											num4 += vector3[num6];
										}
									}
									for (; num5 < k; num5++)
									{
										num4 += a[num + j * k + num5] * b[num2 + num5 * n + l];
									}
									c[num3 + j * n + l] = num4;
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
						num13 += a[num8 + num11 * k + num14] * b[num9 + num14 * n + num12];
					}
					c[num10 + num11 * n + num12] = num13;
				}
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void BatchAdd(ReadOnlySpan<float> left, ReadOnlySpan<float> right, Span<float> results, int batchSize, int vectorSize)
	{
		int num = batchSize * vectorSize;
		if (IsSimdSupported && num >= VectorSize)
		{
			int num2 = num - num % VectorSize;
			fixed (float* ptr = left)
			{
				fixed (float* ptr2 = right)
				{
					fixed (float* ptr3 = results)
					{
						for (int i = 0; i < num2; i += VectorSize)
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
				results[j] = left[j] + right[j];
			}
		}
		else
		{
			for (int k = 0; k < num; k++)
			{
				results[k] = left[k] + right[k];
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void BatchSoftmax(Span<float> values, int batchSize, int vectorSize)
	{
		if (IsSimdSupported && vectorSize >= VectorSize)
		{
			for (int i = 0; i < batchSize; i++)
			{
				int num = i * vectorSize;
				float num2 = float.MinValue;
				for (int j = 0; j < vectorSize; j++)
				{
					if (values[num + j] > num2)
					{
						num2 = values[num + j];
					}
				}
				float num3 = 0f;
				for (int k = 0; k < vectorSize; k++)
				{
					values[num + k] = (float)Math.Exp(values[num + k] - num2);
					num3 += values[num + k];
				}
				float num4 = 1f / num3;
				Vector<float> vector = new Vector<float>(num4);
				int l = 0;
				fixed (float* ptr = values)
				{
					for (; l <= vectorSize - VectorSize; l += VectorSize)
					{
						Vector<float> vector2 = *(Vector<float>*)(ptr + num + l);
						*(Vector<float>*)(ptr + num + l) = vector2 * vector;
					}
				}
				for (; l < vectorSize; l++)
				{
					values[num + l] *= num4;
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
				if (values[num5 + n] > num6)
				{
					num6 = values[num5 + n];
				}
			}
			float num7 = 0f;
			for (int num8 = 0; num8 < vectorSize; num8++)
			{
				values[num5 + num8] = (float)Math.Exp(values[num5 + num8] - num6);
				num7 += values[num5 + num8];
			}
			float num9 = 1f / num7;
			for (int num10 = 0; num10 < vectorSize; num10++)
			{
				values[num5 + num10] *= num9;
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void BatchLayerNorm(ReadOnlySpan<float> input, ReadOnlySpan<float> scale, ReadOnlySpan<float> bias, Span<float> output, int batchSize, int vectorSize)
	{
		if (IsSimdSupported && vectorSize >= VectorSize)
		{
			for (int i = 0; i < batchSize; i++)
			{
				int num = i * vectorSize;
				float num2 = 0f;
				for (int j = 0; j < vectorSize; j++)
				{
					num2 += input[num + j];
				}
				num2 /= (float)vectorSize;
				float num3 = 0f;
				for (int k = 0; k < vectorSize; k++)
				{
					float num4 = input[num + k] - num2;
					num3 += num4 * num4;
				}
				num3 /= (float)vectorSize;
				float num5 = (float)Math.Sqrt(num3 + 1E-08f);
				float num6 = 1f / num5;
				Vector<float> vector = new Vector<float>(num6);
				int l = 0;
				fixed (float* ptr = input)
				{
					fixed (float* ptr2 = scale)
					{
						fixed (float* ptr3 = bias)
						{
							fixed (float* ptr4 = output)
							{
								for (; l <= vectorSize - VectorSize; l += VectorSize)
								{
									Vector<float> vector2 = *(Vector<float>*)(ptr + num + l);
									Vector<float> vector3 = *(Vector<float>*)(ptr2 + l);
									Vector<float> vector4 = *(Vector<float>*)(ptr3 + l);
									Vector<float> vector5 = new Vector<float>(num2);
									*(Vector<float>*)(ptr4 + num + l) = vector3 * (vector2 - vector5) * vector + vector4;
								}
							}
						}
					}
				}
				for (; l < vectorSize; l++)
				{
					output[num + l] = scale[l] * (input[num + l] - num2) * num6 + bias[l];
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
				num8 += input[num7 + n];
			}
			num8 /= (float)vectorSize;
			float num9 = 0f;
			for (int num10 = 0; num10 < vectorSize; num10++)
			{
				float num11 = input[num7 + num10] - num8;
				num9 += num11 * num11;
			}
			num9 /= (float)vectorSize;
			float num12 = (float)Math.Sqrt(num9 + 1E-08f);
			float num13 = 1f / num12;
			for (int num14 = 0; num14 < vectorSize; num14++)
			{
				output[num7 + num14] = scale[num14] * (input[num7 + num14] - num8) * num13 + bias[num14];
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void BatchRelu(ReadOnlySpan<float> input, Span<float> output, int batchSize, int vectorSize)
	{
		int num = batchSize * vectorSize;
		if (IsSimdSupported && num >= VectorSize)
		{
			Vector<float> zero = Vector<float>.Zero;
			int num2 = num - num % VectorSize;
			fixed (float* ptr = input)
			{
				fixed (float* ptr2 = output)
				{
					for (int i = 0; i < num2; i += VectorSize)
					{
						Vector<float> right = *(Vector<float>*)(ptr + i);
						*(Vector<float>*)(ptr2 + i) = Vector.Max(zero, right);
					}
				}
			}
			for (int j = num2; j < num; j++)
			{
				output[j] = Math.Max(0f, input[j]);
			}
		}
		else
		{
			for (int k = 0; k < num; k++)
			{
				output[k] = Math.Max(0f, input[k]);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void BatchScale(ReadOnlySpan<float> input, float scale, Span<float> output, int batchSize, int vectorSize)
	{
		int num = batchSize * vectorSize;
		if (IsSimdSupported && num >= VectorSize)
		{
			Vector<float> vector = new Vector<float>(scale);
			int num2 = num - num % VectorSize;
			fixed (float* ptr = input)
			{
				fixed (float* ptr2 = output)
				{
					for (int i = 0; i < num2; i += VectorSize)
					{
						Vector<float> vector2 = *(Vector<float>*)(ptr + i);
						*(Vector<float>*)(ptr2 + i) = vector2 * vector;
					}
				}
			}
			for (int j = num2; j < num; j++)
			{
				output[j] = input[j] * scale;
			}
		}
		else
		{
			for (int k = 0; k < num; k++)
			{
				output[k] = input[k] * scale;
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static int FindMaxIndex(ReadOnlySpan<float> values, out float maxValue)
	{
		int length = values.Length;
		if (length == 0)
		{
			maxValue = 0f;
			return -1;
		}
		if (IsSimdSupported && length >= VectorSize)
		{
			maxValue = float.MinValue;
			int result = 0;
			fixed (float* ptr = values)
			{
				int i;
				for (i = 0; i <= length - VectorSize; i += VectorSize)
				{
					Vector<float> vector = *(Vector<float>*)(ptr + i);
					for (int j = 0; j < VectorSize; j++)
					{
						float num = vector[j];
						if (num > maxValue)
						{
							maxValue = num;
							result = i + j;
						}
					}
				}
				for (; i < length; i++)
				{
					if (values[i] > maxValue)
					{
						maxValue = values[i];
						result = i;
					}
				}
			}
			return result;
		}
		maxValue = values[0];
		int result2 = 0;
		for (int k = 1; k < length; k++)
		{
			if (values[k] > maxValue)
			{
				maxValue = values[k];
				result2 = k;
			}
		}
		return result2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void BatchNormalize(Span<float> values)
	{
		float num = 0f;
		for (int i = 0; i < values.Length; i++)
		{
			num += values[i];
		}
		if (num > 0f)
		{
			float num2 = 1f / num;
			for (int j = 0; j < values.Length; j++)
			{
				values[j] *= num2;
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void Zero(Span<float> array)
	{
		int length = array.Length;
		if (length == 0)
		{
			return;
		}
		if (IsSimdSupported && length >= VectorSize)
		{
			int num = length - length % VectorSize;
			Vector<float> zero = Vector<float>.Zero;
			fixed (float* ptr = array)
			{
				for (int i = 0; i < num; i += VectorSize)
				{
					*(Vector<float>*)(ptr + i) = zero;
				}
			}
			for (int j = num; j < length; j++)
			{
				array[j] = 0f;
			}
		}
		else
		{
			for (int k = 0; k < length; k++)
			{
				array[k] = 0f;
			}
		}
	}

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
		if (IsSimdSupported && length >= VectorSize)
		{
			int num = length - length % VectorSize;
			fixed (float* ptr = source)
			{
				fixed (float* ptr2 = dest)
				{
					for (int i = 0; i < num; i += VectorSize)
					{
						Vector<float> vector = *(Vector<float>*)(ptr + i);
						*(Vector<float>*)(ptr2 + i) = vector;
					}
				}
			}
			for (int j = num; j < length; j++)
			{
				dest[j] = source[j];
			}
		}
		else
		{
			for (int k = 0; k < length; k++)
			{
				dest[k] = source[k];
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static bool HasInvalidValues(ReadOnlySpan<float> array)
	{
		int length = array.Length;
		if (length == 0)
		{
			return false;
		}
		if (IsSimdSupported && length >= VectorSize)
		{
			int num = length - length % VectorSize;
			fixed (float* ptr = array)
			{
				for (int i = 0; i < num; i += VectorSize)
				{
					Vector<float> vector = *(Vector<float>*)(ptr + i);
					for (int j = 0; j < VectorSize; j++)
					{
						float f = vector[j];
						if (float.IsNaN(f) || float.IsInfinity(f))
						{
							return true;
						}
					}
				}
			}
			for (int k = num; k < length; k++)
			{
				if (float.IsNaN(array[k]) || float.IsInfinity(array[k]))
				{
					return true;
				}
			}
		}
		else
		{
			for (int l = 0; l < length; l++)
			{
				if (float.IsNaN(array[l]) || float.IsInfinity(array[l]))
				{
					return true;
				}
			}
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float EuclideanNorm(ReadOnlySpan<float> array)
	{
		return (float)Math.Sqrt(DotProduct(array, array));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void ComputeLayerNormInputGrad(ReadOnlySpan<float> gradOutput, ReadOnlySpan<float> gamma, ReadOnlySpan<float> xNorm, float invStd, Span<float> result)
	{
		int length = result.Length;
		float num = 1f / (float)length;
		float* pointer = stackalloc float[length];
		MultiplyElementwise(gradOutput, gamma, new Span<float>(pointer, length));
		float num2 = Sum(new ReadOnlySpan<float>(pointer, length));
		float* pointer2 = stackalloc float[length];
		MultiplyElementwise(new ReadOnlySpan<float>(pointer, length), xNorm, new Span<float>(pointer2, length));
		float num3 = Sum(new ReadOnlySpan<float>(pointer2, length));
		if (IsSimdSupported && length >= VectorSize)
		{
			Vector<float> vector = new Vector<float>(invStd * num);
			Vector<float> vector2 = new Vector<float>(num2);
			Vector<float> vector3 = new Vector<float>(num3);
			fixed (float* ptr = result)
			{
				fixed (float* ptr2 = gamma)
				{
					fixed (float* ptr3 = xNorm)
					{
						fixed (float* ptr4 = gradOutput)
						{
							int i;
							for (i = 0; i <= length - VectorSize; i += VectorSize)
							{
								Vector<float> vector4 = *(Vector<float>*)(ptr2 + i);
								Vector<float> vector5 = *(Vector<float>*)(ptr3 + i);
								Vector<float> vector6 = *(Vector<float>*)(ptr4 + i);
								*(Vector<float>*)(ptr + i) = vector * vector4 * (vector6 * new Vector<float>(length) - vector2 - vector5 * vector3);
							}
							for (; i < length; i++)
							{
								result[i] = invStd * num * gamma[i] * ((float)length * gradOutput[i] - num2 - xNorm[i] * num3);
							}
						}
					}
				}
			}
		}
		else
		{
			for (int j = 0; j < length; j++)
			{
				result[j] = invStd * num * gamma[j] * ((float)length * gradOutput[j] - num2 - xNorm[j] * num3);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void Fill(Span<float> array, float value)
	{
		int length = array.Length;
		if (length == 0)
		{
			return;
		}
		if (IsSimdSupported && length >= VectorSize)
		{
			Vector<float> vector = new Vector<float>(value);
			int num = length - length % VectorSize;
			fixed (float* ptr = array)
			{
				for (int i = 0; i < num; i += VectorSize)
				{
					*(Vector<float>*)(ptr + i) = vector;
				}
			}
			for (int j = num; j < length; j++)
			{
				array[j] = value;
			}
		}
		else
		{
			for (int k = 0; k < length; k++)
			{
				array[k] = value;
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void ZeroInt(Span<int> array)
	{
		int length = array.Length;
		if (length == 0)
		{
			return;
		}
		if (IsSimdSupported && length >= Vector<int>.Count)
		{
			int num = length - length % Vector<int>.Count;
			Vector<int> zero = Vector<int>.Zero;
			fixed (int* ptr = array)
			{
				for (int i = 0; i < num; i += Vector<int>.Count)
				{
					*(Vector<int>*)(ptr + i) = zero;
				}
			}
			for (int j = num; j < length; j++)
			{
				array[j] = 0;
			}
		}
		else
		{
			for (int k = 0; k < length; k++)
			{
				array[k] = 0;
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void FillInt(Span<int> array, int value)
	{
		int length = array.Length;
		if (length == 0)
		{
			return;
		}
		if (IsSimdSupported && length >= Vector<int>.Count)
		{
			Vector<int> vector = new Vector<int>(value);
			int num = length - length % Vector<int>.Count;
			fixed (int* ptr = array)
			{
				for (int i = 0; i < num; i += Vector<int>.Count)
				{
					*(Vector<int>*)(ptr + i) = vector;
				}
			}
			for (int j = num; j < length; j++)
			{
				array[j] = value;
			}
		}
		else
		{
			for (int k = 0; k < length; k++)
			{
				array[k] = value;
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void EluPlusOne(ReadOnlySpan<float> input, Span<float> output)
	{
		int length = input.Length;
		if (length != output.Length)
		{
			throw new ArgumentException("数组长度必须相同");
		}
		if (IsSimdSupported && length >= VectorSize)
		{
			Vector<float> vector = new Vector<float>(1f);
			Vector<float> zero = Vector<float>.Zero;
			int num = length - length % VectorSize;
			fixed (float* ptr = input)
			{
				fixed (float* ptr2 = output)
				{
					for (int i = 0; i < num; i += VectorSize)
					{
						Vector<float> vector2 = *(Vector<float>*)(ptr + i);
						Vector<int> condition = Vector.GreaterThan(vector2, zero);
						Vector<float> vector3 = ExpVectorClamped(vector2);
						Vector<float> left = vector2 + vector;
						Vector<float> right = vector3 + vector;
						*(Vector<float>*)(ptr2 + i) = Vector.ConditionalSelect(condition, left, right);
					}
				}
			}
			for (int j = num; j < length; j++)
			{
				float num2 = input[j];
				if (num2 > 80f)
				{
					num2 = 80f;
				}
				else if (num2 < -80f)
				{
					num2 = -80f;
				}
				output[j] = ((num2 > 0f) ? (num2 + 1f) : ((float)Math.Exp(num2) + 1f));
			}
			return;
		}
		for (int k = 0; k < length; k++)
		{
			float num3 = input[k];
			if (num3 > 80f)
			{
				num3 = 80f;
			}
			else if (num3 < -80f)
			{
				num3 = -80f;
			}
			output[k] = ((num3 > 0f) ? (num3 + 1f) : ((float)Math.Exp(num3) + 1f));
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static Vector<float> ExpVectorClamped(Vector<float> v)
	{
		float* ptr = stackalloc float[VectorSize];
		for (int i = 0; i < VectorSize; i++)
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
			ptr[i] = (float)Math.Exp(num);
		}
		return *(Vector<float>*)ptr;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static Vector<float> ExpVector(Vector<float> v)
	{
		float* ptr = stackalloc float[VectorSize];
		for (int i = 0; i < VectorSize; i++)
		{
			ptr[i] = (float)Math.Exp(v[i]);
		}
		return *(Vector<float>*)ptr;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void EluPlusOneBackward(ReadOnlySpan<float> input, ReadOnlySpan<float> gradOutput, Span<float> gradInput)
	{
		int length = input.Length;
		if (length != gradOutput.Length || length != gradInput.Length)
		{
			throw new ArgumentException("数组长度必须相同");
		}
		if (IsSimdSupported && length >= VectorSize)
		{
			Vector<float> zero = Vector<float>.Zero;
			int num = length - length % VectorSize;
			fixed (float* ptr = input)
			{
				fixed (float* ptr2 = gradOutput)
				{
					fixed (float* ptr3 = gradInput)
					{
						for (int i = 0; i < num; i += VectorSize)
						{
							Vector<float> vector = *(Vector<float>*)(ptr + i);
							Vector<float> vector2 = *(Vector<float>*)(ptr2 + i);
							Vector<int> condition = Vector.GreaterThan(vector, zero);
							Vector<float> vector3 = ExpVector(vector);
							Vector<float> right = vector2 * vector3;
							*(Vector<float>*)(ptr3 + i) = Vector.ConditionalSelect(condition, vector2, right);
						}
					}
				}
			}
			for (int j = num; j < length; j++)
			{
				float num2 = input[j];
				float num3 = gradOutput[j];
				gradInput[j] = ((num2 > 0f) ? num3 : (num3 * (float)Math.Exp(num2)));
			}
		}
		else
		{
			for (int k = 0; k < length; k++)
			{
				float num4 = input[k];
				float num5 = gradOutput[k];
				gradInput[k] = ((num4 > 0f) ? num5 : (num5 * (float)Math.Exp(num4)));
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void ScaleAndAccumulate(ReadOnlySpan<float> src, Span<float> result, float scale)
	{
		int length = src.Length;
		if (length != result.Length)
		{
			throw new ArgumentException("数组长度必须相同");
		}
		if (IsSimdSupported && length >= VectorSize)
		{
			Vector<float> vector = new Vector<float>(scale);
			int num = length - length % VectorSize;
			fixed (float* ptr = src)
			{
				fixed (float* ptr2 = result)
				{
					for (int i = 0; i < num; i += VectorSize)
					{
						Vector<float> vector2 = *(Vector<float>*)(ptr + i);
						Vector<float> vector3 = *(Vector<float>*)(ptr2 + i);
						*(Vector<float>*)(ptr2 + i) = vector3 + vector2 * vector;
					}
				}
			}
			for (int j = num; j < length; j++)
			{
				result[j] += src[j] * scale;
			}
		}
		else
		{
			for (int k = 0; k < length; k++)
			{
				result[k] += src[k] * scale;
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void BatchReduceSum(float[][] inputs, Span<float> result, int numItems, int dim)
	{
		if (numItems == 0)
		{
			return;
		}
		Zero(result);
		if (IsSimdSupported && dim >= VectorSize)
		{
			fixed (float* ptr = result)
			{
				for (int i = 0; i < numItems; i++)
				{
					fixed (float* ptr2 = inputs[i])
					{
						for (int j = 0; j <= dim - VectorSize; j += VectorSize)
						{
							Vector<float> vector = *(Vector<float>*)(ptr2 + j);
							Vector<float> vector2 = *(Vector<float>*)(ptr + j);
							*(Vector<float>*)(ptr + j) = vector2 + vector;
						}
						for (int k = dim - dim % VectorSize; k < dim; k++)
						{
							result[k] += inputs[i][k];
						}
					}
				}
			}
			return;
		}
		for (int l = 0; l < numItems; l++)
		{
			for (int m = 0; m < dim; m++)
			{
				result[m] += inputs[l][m];
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void BatchScaleAndAccumulateStrided(float[][] inputs, float[] scales, float[][] results, int numItems, int srcDim, int stride)
	{
		if (IsSimdSupported && stride >= VectorSize)
		{
			for (int i = 0; i < numItems; i++)
			{
				float num = scales[i];
				if (Math.Abs(num) < 1E-10f)
				{
					continue;
				}
				Vector<float> vector = new Vector<float>(num);
				fixed (float* ptr = inputs[i])
				{
					fixed (float* ptr2 = results[i])
					{
						for (int j = 0; j <= srcDim - VectorSize; j += VectorSize)
						{
							Vector<float> vector2 = *(Vector<float>*)(ptr + j);
							Vector<float> vector3 = *(Vector<float>*)(ptr2 + j % stride);
							*(Vector<float>*)(ptr2 + j % stride) = vector3 + vector2 * vector;
						}
						for (int k = srcDim - srcDim % VectorSize; k < srcDim; k++)
						{
							results[i][k % stride] += inputs[i][k] * num;
						}
					}
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void BatchOuterProductAccumulate(float[] gradKV, float[][] vProjBatch, float[][] gradKPerCore, int numCores, int dModel, int dK)
	{
		Span<float> span = gradKV.AsSpan();
		if (IsSimdSupported && dK >= VectorSize)
		{
			fixed (float* ptr = span)
			{
				for (int i = 0; i < numCores; i++)
				{
					fixed (float* ptr2 = gradKPerCore[i])
					{
						fixed (float* ptr3 = vProjBatch[i])
						{
							for (int j = 0; j <= dModel - VectorSize; j += VectorSize)
							{
								Vector<float> vector = *(Vector<float>*)(ptr + j);
								Vector<float> vector2 = *(Vector<float>*)(ptr3 + j);
								int num = j % dK;
								Vector<float> vector3 = *(Vector<float>*)(ptr2 + num);
								*(Vector<float>*)(ptr2 + num) = vector3 + vector * vector2;
							}
							for (int k = dModel - dModel % VectorSize; k < dModel; k++)
							{
								gradKPerCore[i][k % dK] += gradKV[k] * vProjBatch[i][k];
							}
						}
					}
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void FusedMultiplyAddDivide(ReadOnlySpan<float> a, ReadOnlySpan<float> b, ReadOnlySpan<float> c, Span<float> output, float invScale)
	{
		int num = Math.Min(Math.Min(a.Length, b.Length), Math.Min(c.Length, output.Length));
		if (IsSimdSupported && num >= VectorSize)
		{
			Vector<float> vector = new Vector<float>(invScale);
			int num2 = num - num % VectorSize;
			fixed (float* ptr = a)
			{
				fixed (float* ptr2 = b)
				{
					fixed (float* ptr3 = c)
					{
						fixed (float* ptr4 = output)
						{
							for (int i = 0; i < num2; i += VectorSize)
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
				output[j] = (a[j] * b[j] + c[j]) * invScale;
			}
		}
		else
		{
			for (int k = 0; k < num; k++)
			{
				output[k] = (a[k] * b[k] + c[k]) * invScale;
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void BatchScaleAccumulate(float[][] inputs, ReadOnlySpan<float> scales, Span<float> result, int numItems, int dim)
	{
		if (IsSimdSupported && dim >= VectorSize)
		{
			Zero(result);
			for (int i = 0; i < numItems; i++)
			{
				float num = scales[i];
				if (Math.Abs(num) < 1E-10f)
				{
					continue;
				}
				Vector<float> vector = new Vector<float>(num);
				fixed (float* ptr = inputs[i])
				{
					fixed (float* ptr2 = result)
					{
						for (int j = 0; j <= dim - VectorSize; j += VectorSize)
						{
							Vector<float> vector2 = *(Vector<float>*)(ptr + j);
							Vector<float> vector3 = *(Vector<float>*)(ptr2 + j);
							*(Vector<float>*)(ptr2 + j) = vector3 + vector2 * vector;
						}
						for (int k = dim - dim % VectorSize; k < dim; k++)
						{
							result[k] += inputs[i][k] * num;
						}
					}
				}
			}
			return;
		}
		for (int l = 0; l < numItems; l++)
		{
			float num2 = scales[l];
			for (int m = 0; m < dim; m++)
			{
				result[m] += inputs[l][m] * num2;
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Softmax(ReadOnlySpan<float> input, Span<float> output)
	{
		int length = input.Length;
		if (length != output.Length)
		{
			throw new ArgumentException("数组长度必须相同");
		}
		float num = Max(input);
		for (int i = 0; i < length; i++)
		{
			output[i] = (float)Math.Exp(input[i] - num);
		}
		float num2 = Sum(output);
		float scalar = 1f / num2;
		MultiplyScalar(output, scalar, output);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static (float mean, float invStd) LayerNormForward(ReadOnlySpan<float> input, ReadOnlySpan<float> gamma, ReadOnlySpan<float> beta, Span<float> output, Span<float> xNorm, float epsilon = 1E-05f)
	{
		int length = input.Length;
		float num = Mean(input);
		float num2 = Variance(input);
		float num3 = 1f / (float)Math.Sqrt(num2 + epsilon);
		if (IsSimdSupported && length >= VectorSize)
		{
			Vector<float> vector = new Vector<float>(num);
			Vector<float> vector2 = new Vector<float>(num3);
			int num4 = length - length % VectorSize;
			fixed (float* ptr = input)
			{
				fixed (float* ptr2 = gamma)
				{
					fixed (float* ptr3 = beta)
					{
						fixed (float* ptr4 = output)
						{
							fixed (float* ptr5 = xNorm)
							{
								for (int i = 0; i < num4; i += VectorSize)
								{
									Vector<float> vector3 = *(Vector<float>*)(ptr + i);
									Vector<float> vector4 = *(Vector<float>*)(ptr2 + i);
									Vector<float> vector5 = *(Vector<float>*)(ptr3 + i);
									*(Vector<float>*)(ptr4 + i) = vector4 * (*(Vector<float>*)(ptr5 + i) = (vector3 - vector) * vector2) + vector5;
								}
							}
						}
					}
				}
			}
			for (int j = num4; j < length; j++)
			{
				float num5 = (input[j] - num) * num3;
				xNorm[j] = num5;
				output[j] = gamma[j] * num5 + beta[j];
			}
		}
		else
		{
			for (int k = 0; k < length; k++)
			{
				float num6 = (input[k] - num) * num3;
				xNorm[k] = num6;
				output[k] = gamma[k] * num6 + beta[k];
			}
		}
		return (mean: num, invStd: num3);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void SoftmaxBackward(ReadOnlySpan<float> probs, ReadOnlySpan<float> gradOutput, Span<float> gradInput)
	{
		int length = probs.Length;
		if (length != gradOutput.Length || length != gradInput.Length)
		{
			throw new ArgumentException("数组长度必须相同");
		}
		float num = DotProduct(probs, gradOutput);
		if (IsSimdSupported && length >= VectorSize)
		{
			Vector<float> vector = new Vector<float>(num);
			int num2 = length - length % VectorSize;
			fixed (float* ptr = probs)
			{
				fixed (float* ptr2 = gradOutput)
				{
					fixed (float* ptr3 = gradInput)
					{
						for (int i = 0; i < num2; i += VectorSize)
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
				gradInput[j] = probs[j] * (gradOutput[j] - num);
			}
		}
		else
		{
			for (int k = 0; k < length; k++)
			{
				gradInput[k] = probs[k] * (gradOutput[k] - num);
			}
		}
	}
}
