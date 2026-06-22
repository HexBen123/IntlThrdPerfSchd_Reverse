using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace SimdLibrary
{

public static class VectorMath
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
			int vectorCount = length - length % VectorSize;
			fixed (float* leftPtr = left)
			{
				fixed (float* rightPtr = right)
				{
					fixed (float* resultPtr = result)
					{
						for (int i = 0; i < vectorCount; i += VectorSize)
						{
							Vector<float> vLeft = *(Vector<float>*)(leftPtr + i);
							Vector<float> vRight = *(Vector<float>*)(rightPtr + i);
							*(Vector<float>*)(resultPtr + i) = vLeft + vRight;
						}
					}
				}
			}
			for (int j = vectorCount; j < length; j++)
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
			int vectorCount = length - length % VectorSize;
			fixed (float* leftPtr = left)
			{
				fixed (float* rightPtr = right)
				{
					fixed (float* resultPtr = result)
					{
						for (int i = 0; i < vectorCount; i += VectorSize)
						{
							Vector<float> vLeft = *(Vector<float>*)(leftPtr + i);
							Vector<float> vRight = *(Vector<float>*)(rightPtr + i);
							*(Vector<float>*)(resultPtr + i) = vLeft - vRight;
						}
					}
				}
			}
			for (int j = vectorCount; j < length; j++)
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
			int vectorCount = length - length % VectorSize;
			fixed (float* leftPtr = left)
			{
				fixed (float* rightPtr = right)
				{
					fixed (float* resultPtr = result)
					{
						for (int i = 0; i < vectorCount; i += VectorSize)
						{
							Vector<float> vLeft = *(Vector<float>*)(leftPtr + i);
							Vector<float> vRight = *(Vector<float>*)(rightPtr + i);
							*(Vector<float>*)(resultPtr + i) = vLeft * vRight;
						}
					}
				}
			}
			for (int j = vectorCount; j < length; j++)
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
			int vectorCount = length - length % VectorSize;
			fixed (float* leftPtr = left)
			{
				fixed (float* rightPtr = right)
				{
					fixed (float* resultPtr = result)
					{
						for (int i = 0; i < vectorCount; i += VectorSize)
						{
							Vector<float> vLeft = *(Vector<float>*)(leftPtr + i);
							Vector<float> vRight = *(Vector<float>*)(rightPtr + i);
							*(Vector<float>*)(resultPtr + i) = vLeft / vRight;
						}
					}
				}
			}
			for (int j = vectorCount; j < length; j++)
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
			Vector<float> vScalar = new Vector<float>(scalar);
			int vectorCount = length - length % VectorSize;
			fixed (float* arrayPtr = array)
			{
				fixed (float* resultPtr = result)
				{
					for (int i = 0; i < vectorCount; i += VectorSize)
					{
						Vector<float> vArray = *(Vector<float>*)(arrayPtr + i);
						*(Vector<float>*)(resultPtr + i) = vArray * vScalar;
					}
				}
			}
			for (int j = vectorCount; j < length; j++)
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
			Vector<float> vScalar = new Vector<float>(scalar);
			int vectorCount = length - length % VectorSize;
			fixed (float* arrayPtr = array)
			{
				for (int i = 0; i < vectorCount; i += VectorSize)
				{
					Vector<float> vArray = *(Vector<float>*)(arrayPtr + i);
					*(Vector<float>*)(arrayPtr + i) = vArray * vScalar;
				}
			}
			for (int j = vectorCount; j < length; j++)
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
		float sum = 0f;
		if (IsSimdSupported && length >= VectorSize)
		{
			Vector<float> vSum = Vector<float>.Zero;
			int vectorCount = length - length % VectorSize;
			fixed (float* arrayPtr = array)
			{
				for (int i = 0; i < vectorCount; i += VectorSize)
				{
					vSum += *(Vector<float>*)(arrayPtr + i);
				}
			}
			for (int j = 0; j < VectorSize; j++)
			{
				sum += vSum[j];
			}
			for (int k = vectorCount; k < length; k++)
			{
				sum += array[k];
			}
		}
		else
		{
			for (int l = 0; l < length; l++)
			{
				sum += array[l];
			}
		}
		return sum;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static float DotProduct(ReadOnlySpan<float> left, ReadOnlySpan<float> right)
	{
		int length = left.Length;
		if (length != right.Length)
		{
			throw new ArgumentException("数组长度必须相同");
		}
		float result = 0f;
		if (IsSimdSupported && length >= VectorSize)
		{
			Vector<float> vSum = Vector<float>.Zero;
			int vectorCount = length - length % VectorSize;
			fixed (float* leftPtr = left)
			{
				fixed (float* rightPtr = right)
				{
					for (int i = 0; i < vectorCount; i += VectorSize)
					{
						Vector<float> vLeft = *(Vector<float>*)(leftPtr + i);
						Vector<float> vRight = *(Vector<float>*)(rightPtr + i);
						vSum += vLeft * vRight;
					}
				}
			}
			for (int j = 0; j < VectorSize; j++)
			{
				result += vSum[j];
			}
			for (int k = vectorCount; k < length; k++)
			{
				result += left[k] * right[k];
			}
		}
		else
		{
			for (int l = 0; l < length; l++)
			{
				result += left[l] * right[l];
			}
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static float EuclideanDistance(ReadOnlySpan<float> left, ReadOnlySpan<float> right)
	{
		int length = left.Length;
		if (length != right.Length)
		{
			throw new ArgumentException("数组长度必须相同");
		}
		float sumSquares = 0f;
		if (IsSimdSupported && length >= VectorSize)
		{
			Vector<float> vSum = Vector<float>.Zero;
			int vectorCount = length - length % VectorSize;
			fixed (float* leftPtr = left)
			{
				fixed (float* rightPtr = right)
				{
					for (int i = 0; i < vectorCount; i += VectorSize)
					{
						Vector<float> vector = *(Vector<float>*)(leftPtr + i);
						Vector<float> vRight = *(Vector<float>*)(rightPtr + i);
						Vector<float> diff = vector - vRight;
						vSum += diff * diff;
					}
				}
			}
			for (int j = 0; j < VectorSize; j++)
			{
				sumSquares += vSum[j];
			}
			for (int k = vectorCount; k < length; k++)
			{
				float diff2 = left[k] - right[k];
				sumSquares += diff2 * diff2;
			}
		}
		else
		{
			for (int l = 0; l < length; l++)
			{
				float diff3 = left[l] - right[l];
				sumSquares += diff3 * diff3;
			}
		}
		return (float)Math.Sqrt(sumSquares);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float L2Norm(ReadOnlySpan<float> array)
	{
		return (float)Math.Sqrt(DotProduct(array, array));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static float Max(ReadOnlySpan<float> array)
	{
		int length = array.Length;
		if (length == 0)
		{
			throw new ArgumentException("数组不能为空");
		}
		float max = array[0];
		if (IsSimdSupported && length >= VectorSize)
		{
			Vector<float> vMax = new Vector<float>(float.MinValue);
			int vectorCount = length - length % VectorSize;
			fixed (float* arrayPtr = array)
			{
				for (int i = 0; i < vectorCount; i += VectorSize)
				{
					Vector<float> vCurrent = *(Vector<float>*)(arrayPtr + i);
					vMax = Vector.Max(vMax, vCurrent);
				}
			}
			for (int j = 0; j < VectorSize; j++)
			{
				if (vMax[j] > max)
				{
					max = vMax[j];
				}
			}
			for (int k = vectorCount; k < length; k++)
			{
				if (array[k] > max)
				{
					max = array[k];
				}
			}
		}
		else
		{
			for (int l = 1; l < length; l++)
			{
				if (array[l] > max)
				{
					max = array[l];
				}
			}
		}
		return max;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static float Min(ReadOnlySpan<float> array)
	{
		int length = array.Length;
		if (length == 0)
		{
			throw new ArgumentException("数组不能为空");
		}
		float min = array[0];
		if (IsSimdSupported && length >= VectorSize)
		{
			Vector<float> vMin = new Vector<float>(float.MaxValue);
			int vectorCount = length - length % VectorSize;
			fixed (float* arrayPtr = array)
			{
				for (int i = 0; i < vectorCount; i += VectorSize)
				{
					Vector<float> vCurrent = *(Vector<float>*)(arrayPtr + i);
					vMin = Vector.Min(vMin, vCurrent);
				}
			}
			for (int j = 0; j < VectorSize; j++)
			{
				if (vMin[j] < min)
				{
					min = vMin[j];
				}
			}
			for (int k = vectorCount; k < length; k++)
			{
				if (array[k] < min)
				{
					min = array[k];
				}
			}
		}
		else
		{
			for (int l = 1; l < length; l++)
			{
				if (array[l] < min)
				{
					min = array[l];
				}
			}
		}
		return min;
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
		float mean = Mean(array);
		float sumSquares = 0f;
		if (IsSimdSupported && length >= VectorSize)
		{
			Vector<float> vMean = new Vector<float>(mean);
			Vector<float> vSum = Vector<float>.Zero;
			int vectorCount = length - length % VectorSize;
			fixed (float* arrayPtr = array)
			{
				for (int i = 0; i < vectorCount; i += VectorSize)
				{
					Vector<float> diff = *(Vector<float>*)(arrayPtr + i) - vMean;
					vSum += diff * diff;
				}
			}
			for (int j = 0; j < VectorSize; j++)
			{
				sumSquares += vSum[j];
			}
			for (int k = vectorCount; k < length; k++)
			{
				float diff2 = array[k] - mean;
				sumSquares += diff2 * diff2;
			}
		}
		else
		{
			for (int l = 0; l < length; l++)
			{
				float diff3 = array[l] - mean;
				sumSquares += diff3 * diff3;
			}
		}
		return sumSquares / (float)length;
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
			Vector<float> vZero = Vector<float>.Zero;
			int vectorCount = length - length % VectorSize;
			fixed (float* inputPtr = input)
			{
				fixed (float* resultPtr = result)
				{
					for (int i = 0; i < vectorCount; i += VectorSize)
					{
						Vector<float> vInput = *(Vector<float>*)(inputPtr + i);
						*(Vector<float>*)(resultPtr + i) = Vector.Max(vZero, vInput);
					}
				}
			}
			for (int j = vectorCount; j < length; j++)
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
			Vector<float> vZero = Vector<float>.Zero;
			int vectorCount = length - length % VectorSize;
			fixed (float* inputPtr = input)
			{
				fixed (float* gradPtr = gradOutput)
				{
					fixed (float* resultPtr = result)
					{
						for (int i = 0; i < vectorCount; i += VectorSize)
						{
							Vector<float> left = *(Vector<float>*)(inputPtr + i);
							Vector<float> vGrad = *(Vector<float>*)(gradPtr + i);
							Vector<float> vMask = Vector.ConditionalSelect(Vector.GreaterThan(left, vZero), Vector<float>.One, vZero);
							*(Vector<float>*)(resultPtr + i) = vMask * vGrad;
						}
					}
				}
			}
			for (int j = vectorCount; j < length; j++)
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
	public unsafe static void AddScalarInPlace(Span<float> array, float scalar)
	{
		int length = array.Length;
		if (IsSimdSupported && length >= VectorSize)
		{
			Vector<float> vScalar = new Vector<float>(scalar);
			int vectorCount = length - length % VectorSize;
			fixed (float* arrayPtr = array)
			{
				for (int i = 0; i < vectorCount; i += VectorSize)
				{
					Vector<float> vArray = *(Vector<float>*)(arrayPtr + i);
					*(Vector<float>*)(arrayPtr + i) = vArray + vScalar;
				}
			}
			for (int j = vectorCount; j < length; j++)
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
	public unsafe static void ComputeWeightGrad(ReadOnlySpan<float> gradOutput, ReadOnlySpan<float> input, Span<float> weightGrad, int outDim, int inDim)
	{
		if (IsSimdSupported && outDim >= VectorSize && inDim >= VectorSize)
		{
			fixed (float* inputPtr = input)
			{
				fixed (float* weightGradPtr = weightGrad)
				{
					for (int j = 0; j < outDim; j++)
					{
						float go = gradOutput[j];
						Vector<float> vGo = new Vector<float>(go);
						int rowStart = j * inDim;
						int i;
						for (i = 0; i <= inDim - VectorSize; i += VectorSize)
						{
							Vector<float> vInput = *(Vector<float>*)(inputPtr + i);
							*(Vector<float>*)(weightGradPtr + rowStart + i) = vInput * vGo;
						}
						for (; i < inDim; i++)
						{
							weightGrad[rowStart + i] = go * input[i];
						}
					}
				}
			}
			return;
		}
		for (int k = 0; k < outDim; k++)
		{
			float go2 = gradOutput[k];
			int rowStart2 = k * inDim;
			for (int l = 0; l < inDim; l++)
			{
				weightGrad[rowStart2 + l] = go2 * input[l];
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void ComputeInputGrad(ReadOnlySpan<float> gradOutput, ReadOnlySpan<float> weights, Span<float> inputGrad, int outDim, int inDim)
	{
		if (IsSimdSupported && inDim >= VectorSize && outDim >= VectorSize)
		{
			inputGrad.Clear();
			fixed (float* weightsPtr = weights)
			{
				fixed (float* inputGradPtr = inputGrad)
				{
					for (int j = 0; j < outDim; j++)
					{
						float go = gradOutput[j];
						int rowStart = j * inDim;
						Vector<float> vGo = new Vector<float>(go);
						for (int i = 0; i <= inDim - VectorSize; i += VectorSize)
						{
							Vector<float> vWeight = *(Vector<float>*)(weightsPtr + rowStart + i);
							Vector<float> vInputGrad = *(Vector<float>*)(inputGradPtr + i);
							*(Vector<float>*)(inputGradPtr + i) = vInputGrad + vWeight * vGo;
						}
						for (int k = inDim - inDim % VectorSize; k < inDim; k++)
						{
							inputGrad[k] += weights[rowStart + k] * go;
						}
					}
				}
			}
			return;
		}
		for (int l = 0; l < inDim; l++)
		{
			float sum = 0f;
			for (int m = 0; m < outDim; m++)
			{
				sum += gradOutput[m] * weights[m * inDim + l];
			}
			inputGrad[l] = sum;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void MultiplyElementwise(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> result)
	{
		int length = Math.Min(Math.Min(a.Length, b.Length), result.Length);
		if (IsSimdSupported && length >= VectorSize)
		{
			int vectorCount = length - length % VectorSize;
			fixed (float* aPtr = a)
			{
				fixed (float* bPtr = b)
				{
					fixed (float* resultPtr = result)
					{
						for (int i = 0; i < vectorCount; i += VectorSize)
						{
							Vector<float> vA = *(Vector<float>*)(aPtr + i);
							Vector<float> vB = *(Vector<float>*)(bPtr + i);
							*(Vector<float>*)(resultPtr + i) = vA * vB;
						}
					}
				}
			}
			for (int j = vectorCount; j < length; j++)
			{
				result[j] = a[j] * b[j];
			}
		}
		else
		{
			for (int k = 0; k < length; k++)
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
			for (int k = 0; k < numKeys; k++)
			{
				float sum = 0f;
				int d = 0;
				fixed (float* queryPtr = query)
				{
					fixed (float* keyPtr = keys[k])
					{
						for (; d <= dim - VectorSize; d += VectorSize)
						{
							Vector<float> vector = *(Vector<float>*)(queryPtr + d);
							Vector<float> vKey = *(Vector<float>*)(keyPtr + d);
							Vector<float> vMul = vector * vKey;
							for (int vi = 0; vi < VectorSize; vi++)
							{
								sum += vMul[vi];
							}
						}
					}
				}
				for (; d < dim; d++)
				{
					sum += query[d] * keys[k][d];
				}
				scores[k] = sum;
			}
			return;
		}
		for (int i = 0; i < numKeys; i++)
		{
			float sum2 = 0f;
			for (int j = 0; j < dim; j++)
			{
				sum2 += query[j] * keys[i][j];
			}
			scores[i] = sum2;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void BatchWeightedSum(ReadOnlySpan<float> weights, float[][] values, Span<float> output, int numItems, int dim)
	{
		if (IsSimdSupported && dim >= VectorSize)
		{
			output.Clear();
			fixed (float* outputPtr = output)
			{
				for (int k = 0; k < numItems; k++)
				{
					float w = weights[k];
					if (Math.Abs(w) < 1E-10f)
					{
						continue;
					}
					Vector<float> vW = new Vector<float>(w);
					fixed (float* valuePtr = values[k])
					{
						for (int d = 0; d <= dim - VectorSize; d += VectorSize)
						{
							Vector<float> vValue = *(Vector<float>*)(valuePtr + d);
							Vector<float> vOutput = *(Vector<float>*)(outputPtr + d);
							*(Vector<float>*)(outputPtr + d) = vOutput + vValue * vW;
						}
					}
					for (int i = dim - dim % VectorSize; i < dim; i++)
					{
						output[i] += values[k][i] * w;
					}
				}
			}
			return;
		}
		for (int j = 0; j < dim; j++)
		{
			float sum = 0f;
			for (int l = 0; l < numItems; l++)
			{
				sum += weights[l] * values[l][j];
			}
			output[j] = sum;
		}
	}
}
}
