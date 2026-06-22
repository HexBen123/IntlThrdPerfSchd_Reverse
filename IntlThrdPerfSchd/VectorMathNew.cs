using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace SimdLibrary
{

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
	public unsafe static void BatchMatrixVectorMultiply(float[][] inputs, ReadOnlySpan<float> weights, ReadOnlySpan<float> bias, float[][] outputs, int batchSize, int inDim, int outDim)
	{
		if (IsSimdSupported && inDim >= VectorSize)
		{
			int vectorCount = inDim - inDim % VectorSize;
			fixed (float* weightsPtr = weights)
			{
				fixed (float* biasPtr = bias)
				{
					for (int k = 0; k < batchSize; k++)
					{
						fixed (float* inputPtr = inputs[k])
						{
							fixed (float* outputPtr = outputs[k])
							{
								for (int j = 0; j < outDim; j++)
								{
									float* weightRow = weightsPtr + j * inDim;
									Vector<float> vSum = Vector<float>.Zero;
									for (int d = 0; d < vectorCount; d += VectorSize)
									{
										Vector<float> vInput = *(Vector<float>*)(inputPtr + d);
										Vector<float> vWeight = *(Vector<float>*)(weightRow + d);
										vSum += vInput * vWeight;
									}
									float sum = 0f;
									for (int vi = 0; vi < VectorSize; vi++)
									{
										sum += vSum[vi];
									}
									for (int i = vectorCount; i < inDim; i++)
									{
										sum += inputPtr[i] * weightRow[i];
									}
									outputPtr[j] = sum + biasPtr[j];
								}
							}
						}
					}
				}
			}
			return;
		}
		for (int l = 0; l < batchSize; l++)
		{
			for (int m = 0; m < outDim; m++)
			{
				float sum2 = 0f;
				for (int n = 0; n < inDim; n++)
				{
					sum2 += inputs[l][n] * weights[m * inDim + n];
				}
				outputs[l][m] = sum2 + bias[m];
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
			fixed (float* weightsPtr = weights)
			{
				fixed (float* ptr = gradOutputs)
				{
					fixed (float* inputGradsPtr = inputGrads)
					{
						for (int c = 0; c < numCores; c++)
						{
							int weightOffset = c * outDim * inDim;
							int gradOffset = c * outDim;
							int inputOffset = c * inDim;
							for (int j = 0; j < outDim; j++)
							{
								float go = gradOutputs[gradOffset + j];
								int rowStart = weightOffset + j * inDim;
								Vector<float> vGo = new Vector<float>(go);
								for (int i = 0; i <= inDim - VectorSize; i += VectorSize)
								{
									Vector<float> vWeight = *(Vector<float>*)(weightsPtr + rowStart + i);
									Vector<float> vInputGrad = *(Vector<float>*)(inputGradsPtr + inputOffset + i);
									*(Vector<float>*)(inputGradsPtr + inputOffset + i) = vInputGrad + vWeight * vGo;
								}
								for (int k = inDim - inDim % VectorSize; k < inDim; k++)
								{
									inputGrads[inputOffset + k] += weights[rowStart + k] * go;
								}
							}
						}
					}
				}
			}
			return;
		}
		for (int l = 0; l < numCores; l++)
		{
			int weightOffset2 = l * outDim * inDim;
			int gradOffset2 = l * outDim;
			int inputOffset2 = l * inDim;
			for (int m = 0; m < inDim; m++)
			{
				float sum = 0f;
				for (int n = 0; n < outDim; n++)
				{
					sum += gradOutputs[gradOffset2 + n] * weights[weightOffset2 + n * inDim + m];
				}
				inputGrads[inputOffset2 + m] = sum;
			}
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void ComputeBatchWeightGrad(ReadOnlySpan<float> gradOutputs, float[][] inputs, Span<float> weightGrad, int batchSize, int outDim, int inDim)
	{
		if (IsSimdSupported && inDim >= VectorSize)
		{
			fixed (float* weightGradPtr = weightGrad)
			{
				for (int k = 0; k < batchSize; k++)
				{
					fixed (float* inputPtr = inputs[k])
					{
						for (int j = 0; j < outDim; j++)
						{
							int gradOffset = j * inDim;
							float go_j = gradOutputs[k * outDim + j];
							if (!(Math.Abs(go_j) < 1E-10f))
							{
								for (int d = 0; d <= inDim - VectorSize; d += VectorSize)
								{
									Vector<float> vInput = *(Vector<float>*)(inputPtr + d);
									Vector<float> vGrad = new Vector<float>(go_j);
									Vector<float> vWeightGrad = *(Vector<float>*)(weightGradPtr + gradOffset + d);
									*(Vector<float>*)(weightGradPtr + gradOffset + d) = vWeightGrad + vInput * vGrad;
								}
								for (int i = inDim - inDim % VectorSize; i < inDim; i++)
								{
									weightGrad[gradOffset + i] += inputPtr[i] * go_j;
								}
							}
						}
					}
				}
			}
			return;
		}
		for (int l = 0; l < batchSize; l++)
		{
			for (int m = 0; m < outDim; m++)
			{
				float go_j2 = gradOutputs[l * outDim + m];
				for (int n = 0; n < inDim; n++)
				{
					weightGrad[m * inDim + n] += inputs[l][n] * go_j2;
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
			fixed (float* resultPtr = result)
			{
				for (int k = 0; k < numItems; k++)
				{
					float w = weights[k];
					if (Math.Abs(w) < 1E-10f)
					{
						continue;
					}
					fixed (float* valuePtr = values[k])
					{
						for (int d = 0; d <= dim - VectorSize; d += VectorSize)
						{
							Vector<float> vValue = *(Vector<float>*)(valuePtr + d);
							Vector<float> vResult = *(Vector<float>*)(resultPtr + d);
							*(Vector<float>*)(resultPtr + d) = vResult + vValue * new Vector<float>(w);
						}
					}
					for (int i = dim - dim % VectorSize; i < dim; i++)
					{
						result[i] += values[k][i] * w;
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
			result[j] = sum;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void BatchAccumulateWeighted(float[] weights, float[][] values, float[] output, int numItems, int dim, float scale = 1f)
	{
		if (IsSimdSupported && dim >= VectorSize)
		{
			fixed (float* outputPtr = output)
			{
				for (int k = 0; k < numItems; k++)
				{
					float w = weights[k] * scale;
					if (Math.Abs(w) < 1E-10f)
					{
						continue;
					}
					fixed (float* valuePtr = values[k])
					{
						for (int d = 0; d <= dim - VectorSize; d += VectorSize)
						{
							Vector<float> vValue = *(Vector<float>*)(valuePtr + d);
							Vector<float> vOutput = *(Vector<float>*)(outputPtr + d);
							*(Vector<float>*)(outputPtr + d) = vOutput + vValue * new Vector<float>(w);
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
		for (int j = 0; j < numItems; j++)
		{
			float w2 = weights[j] * scale;
			for (int l = 0; l < dim; l++)
			{
				output[l] += values[j][l] * w2;
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void BatchQueryKeyDotProduct(ReadOnlySpan<float> query, float[][] keys, ReadOnlySpan<float> weights, Span<float> result, int numKeys, int dim)
	{
		if (IsSimdSupported && dim >= VectorSize)
		{
			fixed (float* queryPtr = query)
			{
				fixed (float* resultPtr = result)
				{
					for (int k = 0; k < numKeys; k++)
					{
						float w = weights[k];
						if (Math.Abs(w) < 1E-10f)
						{
							resultPtr[k] = 0f;
							continue;
						}
						fixed (float* keyPtr = keys[k])
						{
							float sum = 0f;
							int d;
							for (d = 0; d <= dim - VectorSize; d += VectorSize)
							{
								Vector<float> vector = *(Vector<float>*)(queryPtr + d);
								Vector<float> vKey = *(Vector<float>*)(keyPtr + d);
								Vector<float> vMul = vector * vKey;
								for (int vi = 0; vi < VectorSize; vi++)
								{
									sum += vMul[vi];
								}
							}
							for (; d < dim; d++)
							{
								sum += query[d] * keys[k][d];
							}
							resultPtr[k] = sum * w;
						}
					}
				}
			}
			return;
		}
		for (int i = 0; i < numKeys; i++)
		{
			float w2 = weights[i];
			if (Math.Abs(w2) < 1E-10f)
			{
				result[i] = 0f;
				continue;
			}
			float sum2 = 0f;
			for (int j = 0; j < dim; j++)
			{
				sum2 += query[j] * keys[i][j];
			}
			result[i] = sum2 * w2;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void BatchVectorsDotVector(float[][] a, ReadOnlySpan<float> b, Span<float> result, int numItems, int dim)
	{
		if (IsSimdSupported && dim >= VectorSize)
		{
			fixed (float* bPtr = b)
			{
				fixed (float* resultPtr = result)
				{
					for (int k = 0; k < numItems; k++)
					{
						fixed (float* aPtr = a[k])
						{
							float sum = 0f;
							int d;
							for (d = 0; d <= dim - VectorSize; d += VectorSize)
							{
								Vector<float> vector = *(Vector<float>*)(aPtr + d);
								Vector<float> vB = *(Vector<float>*)(bPtr + d);
								Vector<float> vMul = vector * vB;
								for (int vi = 0; vi < VectorSize; vi++)
								{
									sum += vMul[vi];
								}
							}
							for (; d < dim; d++)
							{
								sum += a[k][d] * b[d];
							}
							resultPtr[k] = sum;
						}
					}
				}
			}
			return;
		}
		for (int i = 0; i < numItems; i++)
		{
			float sum2 = 0f;
			for (int j = 0; j < dim; j++)
			{
				sum2 += a[i][j] * b[j];
			}
			result[i] = sum2;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void BatchMatrixVectorMultiply(ReadOnlySpan<float> matrices, ReadOnlySpan<float> vector, Span<float> results, int batchSize, int rowsPerMatrix, int cols)
	{
		if (IsSimdSupported && cols >= VectorSize && rowsPerMatrix >= VectorSize)
		{
			fixed (float* matricesPtr = matrices)
			{
				fixed (float* vectorPtr = vector)
				{
					fixed (float* ptr = results)
					{
						for (int c = 0; c < batchSize; c++)
						{
							int matOffset = c * rowsPerMatrix * cols;
							int resOffset = c * rowsPerMatrix;
							for (int r = 0; r < rowsPerMatrix; r++)
							{
								int rowStart = matOffset + r * cols;
								float sum = 0f;
								int d;
								for (d = 0; d <= cols - VectorSize; d += VectorSize)
								{
									Vector<float> vector2 = *(Vector<float>*)(matricesPtr + rowStart + d);
									Vector<float> vVec = *(Vector<float>*)(vectorPtr + d);
									Vector<float> vMul = vector2 * vVec;
									for (int vi = 0; vi < VectorSize; vi++)
									{
										sum += vMul[vi];
									}
								}
								for (; d < cols; d++)
								{
									sum += matrices[rowStart + d] * vector[d];
								}
								results[resOffset + r] = sum;
							}
						}
					}
				}
			}
			return;
		}
		for (int i = 0; i < batchSize; i++)
		{
			int matOffset2 = i * rowsPerMatrix * cols;
			int resOffset2 = i * rowsPerMatrix;
			for (int j = 0; j < rowsPerMatrix; j++)
			{
				float sum2 = 0f;
				int rowStart2 = matOffset2 + j * cols;
				for (int k = 0; k < cols; k++)
				{
					sum2 += matrices[rowStart2 + k] * vector[k];
				}
				results[resOffset2 + j] = sum2;
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void BatchMatrixVectorMultiplyWithBias(ReadOnlySpan<float> matrices, ReadOnlySpan<float> vector, ReadOnlySpan<float> bias, Span<float> results, int batchSize, int rowsPerMatrix, int cols)
	{
		if (IsSimdSupported && cols >= VectorSize && rowsPerMatrix >= VectorSize)
		{
			fixed (float* matricesPtr = matrices)
			{
				fixed (float* vectorPtr = vector)
				{
					fixed (float* ptr = bias)
					{
						fixed (float* ptr2 = results)
						{
							for (int c = 0; c < batchSize; c++)
							{
								int matOffset = c * rowsPerMatrix * cols;
								int resOffset = c * rowsPerMatrix;
								for (int r = 0; r < rowsPerMatrix; r++)
								{
									int rowStart = matOffset + r * cols;
									float sum = 0f;
									int d;
									for (d = 0; d <= cols - VectorSize; d += VectorSize)
									{
										Vector<float> vector2 = *(Vector<float>*)(matricesPtr + rowStart + d);
										Vector<float> vVec = *(Vector<float>*)(vectorPtr + d);
										Vector<float> vMul = vector2 * vVec;
										for (int vi = 0; vi < VectorSize; vi++)
										{
											sum += vMul[vi];
										}
									}
									for (; d < cols; d++)
									{
										sum += matrices[rowStart + d] * vector[d];
									}
									results[resOffset + r] = sum + bias[resOffset + r];
								}
							}
						}
					}
				}
			}
			return;
		}
		for (int i = 0; i < batchSize; i++)
		{
			int matOffset2 = i * rowsPerMatrix * cols;
			int resOffset2 = i * rowsPerMatrix;
			for (int j = 0; j < rowsPerMatrix; j++)
			{
				float sum2 = 0f;
				int rowStart2 = matOffset2 + j * cols;
				for (int k = 0; k < cols; k++)
				{
					sum2 += matrices[rowStart2 + k] * vector[k];
				}
				results[resOffset2 + j] = sum2 + bias[resOffset2 + j];
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void BatchTransposeMultiply(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> result, int batchSize, int rows, int cols)
	{
		if (IsSimdSupported && cols >= VectorSize && rows >= VectorSize)
		{
			fixed (float* aPtr = a)
			{
				fixed (float* bPtr = b)
				{
					fixed (float* ptr = result)
					{
						for (int c = 0; c < batchSize; c++)
						{
							int aOffset = c * rows * cols;
							int bOffset = c * cols;
							int resOffset = c * rows * cols;
							for (int i = 0; i < rows; i++)
							{
								for (int j = 0; j < cols; j++)
								{
									int aIndex = aOffset + i * cols + j;
									float sum = 0f;
									int k;
									for (k = 0; k <= rows - VectorSize; k += VectorSize)
									{
										Vector<float> vector = *(Vector<float>*)(aPtr + aIndex + k * cols);
										Vector<float> vB = *(Vector<float>*)(bPtr + bOffset + k);
										Vector<float> vMul = vector * vB;
										for (int vi = 0; vi < VectorSize; vi++)
										{
											sum += vMul[vi];
										}
									}
									for (; k < rows; k++)
									{
										sum += a[aOffset + k * cols + j] * b[bOffset + k];
									}
									result[resOffset + i * cols + j] = sum;
								}
							}
						}
					}
				}
			}
			return;
		}
		for (int l = 0; l < batchSize; l++)
		{
			int aOffset2 = l * rows * cols;
			int bOffset2 = l * cols;
			int resOffset2 = l * rows * cols;
			for (int m = 0; m < rows; m++)
			{
				for (int n = 0; n < cols; n++)
				{
					float sum2 = 0f;
					for (int num = 0; num < rows; num++)
					{
						sum2 += a[aOffset2 + num * cols + n] * b[bOffset2 + num];
					}
					result[resOffset2 + m * cols + n] = sum2;
				}
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void BatchMatMul(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> c, int batchSize, int m, int n, int k)
	{
		if (IsSimdSupported && k >= VectorSize)
		{
			fixed (float* aPtr = a)
			{
				fixed (float* bPtr = b)
				{
					fixed (float* ptr = c)
					{
						for (int idx = 0; idx < batchSize; idx++)
						{
							int aOffset = idx * m * k;
							int bOffset = idx * k * n;
							int cOffset = idx * m * n;
							for (int i = 0; i < m; i++)
							{
								for (int j = 0; j < n; j++)
								{
									float sum = 0f;
									int ki;
									for (ki = 0; ki <= k - VectorSize; ki += VectorSize)
									{
										Vector<float> vector = *(Vector<float>*)(aPtr + aOffset + i * k + ki);
										Vector<float> vB = *(Vector<float>*)(bPtr + bOffset + ki * n + j);
										Vector<float> vMul = vector * vB;
										for (int vi = 0; vi < VectorSize; vi++)
										{
											sum += vMul[vi];
										}
									}
									for (; ki < k; ki++)
									{
										sum += a[aOffset + i * k + ki] * b[bOffset + ki * n + j];
									}
									c[cOffset + i * n + j] = sum;
								}
							}
						}
					}
				}
			}
			return;
		}
		for (int l = 0; l < batchSize; l++)
		{
			int aOffset2 = l * m * k;
			int bOffset2 = l * k * n;
			int cOffset2 = l * m * n;
			for (int num = 0; num < m; num++)
			{
				for (int num2 = 0; num2 < n; num2++)
				{
					float sum2 = 0f;
					for (int num3 = 0; num3 < k; num3++)
					{
						sum2 += a[aOffset2 + num * k + num3] * b[bOffset2 + num3 * n + num2];
					}
					c[cOffset2 + num * n + num2] = sum2;
				}
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void BatchAdd(ReadOnlySpan<float> left, ReadOnlySpan<float> right, Span<float> results, int batchSize, int vectorSize)
	{
		int totalLen = batchSize * vectorSize;
		if (IsSimdSupported && totalLen >= VectorSize)
		{
			int vectorCount = totalLen - totalLen % VectorSize;
			fixed (float* leftPtr = left)
			{
				fixed (float* rightPtr = right)
				{
					fixed (float* resultsPtr = results)
					{
						for (int i = 0; i < vectorCount; i += VectorSize)
						{
							Vector<float> vLeft = *(Vector<float>*)(leftPtr + i);
							Vector<float> vRight = *(Vector<float>*)(rightPtr + i);
							*(Vector<float>*)(resultsPtr + i) = vLeft + vRight;
						}
					}
				}
			}
			for (int j = vectorCount; j < totalLen; j++)
			{
				results[j] = left[j] + right[j];
			}
		}
		else
		{
			for (int k = 0; k < totalLen; k++)
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
			for (int c = 0; c < batchSize; c++)
			{
				int offset = c * vectorSize;
				float maxVal = float.MinValue;
				for (int i = 0; i < vectorSize; i++)
				{
					if (values[offset + i] > maxVal)
					{
						maxVal = values[offset + i];
					}
				}
				float sumExp = 0f;
				for (int j = 0; j < vectorSize; j++)
				{
					values[offset + j] = (float)Math.Exp(values[offset + j] - maxVal);
					sumExp += values[offset + j];
				}
				float invSum = 1f / sumExp;
				Vector<float> vInvSum = new Vector<float>(invSum);
				int iVec = 0;
				fixed (float* valuesPtr = values)
				{
					for (; iVec <= vectorSize - VectorSize; iVec += VectorSize)
					{
						Vector<float> v = *(Vector<float>*)(valuesPtr + offset + iVec);
						*(Vector<float>*)(valuesPtr + offset + iVec) = v * vInvSum;
					}
				}
				for (; iVec < vectorSize; iVec++)
				{
					values[offset + iVec] *= invSum;
				}
			}
			return;
		}
		for (int k = 0; k < batchSize; k++)
		{
			int offset2 = k * vectorSize;
			float maxVal2 = float.MinValue;
			for (int l = 0; l < vectorSize; l++)
			{
				if (values[offset2 + l] > maxVal2)
				{
					maxVal2 = values[offset2 + l];
				}
			}
			float sumExp2 = 0f;
			for (int m = 0; m < vectorSize; m++)
			{
				values[offset2 + m] = (float)Math.Exp(values[offset2 + m] - maxVal2);
				sumExp2 += values[offset2 + m];
			}
			float invSum2 = 1f / sumExp2;
			for (int n = 0; n < vectorSize; n++)
			{
				values[offset2 + n] *= invSum2;
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void BatchLayerNorm(ReadOnlySpan<float> input, ReadOnlySpan<float> scale, ReadOnlySpan<float> bias, Span<float> output, int batchSize, int vectorSize)
	{
		if (IsSimdSupported && vectorSize >= VectorSize)
		{
			for (int c = 0; c < batchSize; c++)
			{
				int offset = c * vectorSize;
				float mean = 0f;
				for (int i = 0; i < vectorSize; i++)
				{
					mean += input[offset + i];
				}
				mean /= (float)vectorSize;
				float variance = 0f;
				for (int j = 0; j < vectorSize; j++)
				{
					float diff = input[offset + j] - mean;
					variance += diff * diff;
				}
				variance /= (float)vectorSize;
				float std = (float)Math.Sqrt(variance + 1E-08f);
				float invStd = 1f / std;
				Vector<float> vInvStd = new Vector<float>(invStd);
				int iVec = 0;
				fixed (float* inputPtr = input)
				{
					fixed (float* scalePtr = scale)
					{
						fixed (float* biasPtr = bias)
						{
							fixed (float* outputPtr = output)
							{
								for (; iVec <= vectorSize - VectorSize; iVec += VectorSize)
								{
									Vector<float> vInput = *(Vector<float>*)(inputPtr + offset + iVec);
									Vector<float> vScale = *(Vector<float>*)(scalePtr + iVec);
									Vector<float> vBias = *(Vector<float>*)(biasPtr + iVec);
									Vector<float> vMean = new Vector<float>(mean);
									*(Vector<float>*)(outputPtr + offset + iVec) = vScale * (vInput - vMean) * vInvStd + vBias;
								}
							}
						}
					}
				}
				for (; iVec < vectorSize; iVec++)
				{
					output[offset + iVec] = scale[iVec] * (input[offset + iVec] - mean) * invStd + bias[iVec];
				}
			}
			return;
		}
		for (int k = 0; k < batchSize; k++)
		{
			int offset2 = k * vectorSize;
			float mean2 = 0f;
			for (int l = 0; l < vectorSize; l++)
			{
				mean2 += input[offset2 + l];
			}
			mean2 /= (float)vectorSize;
			float variance2 = 0f;
			for (int m = 0; m < vectorSize; m++)
			{
				float diff2 = input[offset2 + m] - mean2;
				variance2 += diff2 * diff2;
			}
			variance2 /= (float)vectorSize;
			float std2 = (float)Math.Sqrt(variance2 + 1E-08f);
			float invStd2 = 1f / std2;
			for (int n = 0; n < vectorSize; n++)
			{
				output[offset2 + n] = scale[n] * (input[offset2 + n] - mean2) * invStd2 + bias[n];
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void BatchRelu(ReadOnlySpan<float> input, Span<float> output, int batchSize, int vectorSize)
	{
		int totalLen = batchSize * vectorSize;
		if (IsSimdSupported && totalLen >= VectorSize)
		{
			Vector<float> vZero = Vector<float>.Zero;
			int vectorCount = totalLen - totalLen % VectorSize;
			fixed (float* inputPtr = input)
			{
				fixed (float* outputPtr = output)
				{
					for (int i = 0; i < vectorCount; i += VectorSize)
					{
						Vector<float> vInput = *(Vector<float>*)(inputPtr + i);
						*(Vector<float>*)(outputPtr + i) = Vector.Max(vZero, vInput);
					}
				}
			}
			for (int j = vectorCount; j < totalLen; j++)
			{
				output[j] = Math.Max(0f, input[j]);
			}
		}
		else
		{
			for (int k = 0; k < totalLen; k++)
			{
				output[k] = Math.Max(0f, input[k]);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void BatchScale(ReadOnlySpan<float> input, float scale, Span<float> output, int batchSize, int vectorSize)
	{
		int totalLen = batchSize * vectorSize;
		if (IsSimdSupported && totalLen >= VectorSize)
		{
			Vector<float> vScale = new Vector<float>(scale);
			int vectorCount = totalLen - totalLen % VectorSize;
			fixed (float* inputPtr = input)
			{
				fixed (float* outputPtr = output)
				{
					for (int i = 0; i < vectorCount; i += VectorSize)
					{
						Vector<float> vInput = *(Vector<float>*)(inputPtr + i);
						*(Vector<float>*)(outputPtr + i) = vInput * vScale;
					}
				}
			}
			for (int j = vectorCount; j < totalLen; j++)
			{
				output[j] = input[j] * scale;
			}
		}
		else
		{
			for (int k = 0; k < totalLen; k++)
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
			int maxIndex = 0;
			fixed (float* valuesPtr = values)
			{
				int i;
				for (i = 0; i <= length - VectorSize; i += VectorSize)
				{
					Vector<float> v = *(Vector<float>*)(valuesPtr + i);
					for (int vi = 0; vi < VectorSize; vi++)
					{
						float vx = v[vi];
						if (vx > maxValue)
						{
							maxValue = vx;
							maxIndex = i + vi;
						}
					}
				}
				for (; i < length; i++)
				{
					if (values[i] > maxValue)
					{
						maxValue = values[i];
						maxIndex = i;
					}
				}
			}
			return maxIndex;
		}
		maxValue = values[0];
		int maxIndex2 = 0;
		for (int j = 1; j < length; j++)
		{
			if (values[j] > maxValue)
			{
				maxValue = values[j];
				maxIndex2 = j;
			}
		}
		return maxIndex2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void BatchNormalize(Span<float> values)
	{
		float sum = 0f;
		for (int i = 0; i < values.Length; i++)
		{
			sum += values[i];
		}
		if (sum > 0f)
		{
			float invSum = 1f / sum;
			for (int j = 0; j < values.Length; j++)
			{
				values[j] *= invSum;
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
			int vectorCount = length - length % VectorSize;
			Vector<float> vZero = Vector<float>.Zero;
			fixed (float* arrayPtr = array)
			{
				for (int i = 0; i < vectorCount; i += VectorSize)
				{
					*(Vector<float>*)(arrayPtr + i) = vZero;
				}
			}
			for (int j = vectorCount; j < length; j++)
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
			int vectorCount = length - length % VectorSize;
			fixed (float* srcPtr = source)
			{
				fixed (float* destPtr = dest)
				{
					for (int i = 0; i < vectorCount; i += VectorSize)
					{
						Vector<float> v = *(Vector<float>*)(srcPtr + i);
						*(Vector<float>*)(destPtr + i) = v;
					}
				}
			}
			for (int j = vectorCount; j < length; j++)
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
			int vectorCount = length - length % VectorSize;
			fixed (float* arrayPtr = array)
			{
				for (int i = 0; i < vectorCount; i += VectorSize)
				{
					Vector<float> v = *(Vector<float>*)(arrayPtr + i);
					for (int vi = 0; vi < VectorSize; vi++)
					{
						float val = v[vi];
						if (float.IsNaN(val) || float.IsInfinity(val))
						{
							return true;
						}
					}
				}
			}
			for (int j = vectorCount; j < length; j++)
			{
				if (float.IsNaN(array[j]) || float.IsInfinity(array[j]))
				{
					return true;
				}
			}
		}
		else
		{
			for (int k = 0; k < length; k++)
			{
				if (float.IsNaN(array[k]) || float.IsInfinity(array[k]))
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
		int d = result.Length;
		float invD = 1f / (float)d;
		float* temp = stackalloc float[d];
		MultiplyElementwise(gradOutput, gamma, new Span<float>(temp, d));
		float sum1 = Sum(new ReadOnlySpan<float>(temp, d));
		float* temp2 = stackalloc float[d];
		MultiplyElementwise(new ReadOnlySpan<float>(temp, d), xNorm, new Span<float>(temp2, d));
		float sum2 = Sum(new ReadOnlySpan<float>(temp2, d));
		if (IsSimdSupported && d >= VectorSize)
		{
			Vector<float> vInvStdD = new Vector<float>(invStd * invD);
			Vector<float> vSum1 = new Vector<float>(sum1);
			Vector<float> vSum2 = new Vector<float>(sum2);
			fixed (float* resultPtr = result)
			{
				fixed (float* gammaPtr = gamma)
				{
					fixed (float* xNormPtr = xNorm)
					{
						fixed (float* gradOutputPtr = gradOutput)
						{
							int i;
							for (i = 0; i <= d - VectorSize; i += VectorSize)
							{
								Vector<float> vGamma = *(Vector<float>*)(gammaPtr + i);
								Vector<float> vXNorm = *(Vector<float>*)(xNormPtr + i);
								Vector<float> vGradOut = *(Vector<float>*)(gradOutputPtr + i);
								*(Vector<float>*)(resultPtr + i) = vInvStdD * vGamma * (vGradOut * new Vector<float>(d) - vSum1 - vXNorm * vSum2);
							}
							for (; i < d; i++)
							{
								result[i] = invStd * invD * gamma[i] * ((float)d * gradOutput[i] - sum1 - xNorm[i] * sum2);
							}
						}
					}
				}
			}
		}
		else
		{
			for (int j = 0; j < d; j++)
			{
				result[j] = invStd * invD * gamma[j] * ((float)d * gradOutput[j] - sum1 - xNorm[j] * sum2);
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
			Vector<float> vValue = new Vector<float>(value);
			int vectorCount = length - length % VectorSize;
			fixed (float* arrayPtr = array)
			{
				for (int i = 0; i < vectorCount; i += VectorSize)
				{
					*(Vector<float>*)(arrayPtr + i) = vValue;
				}
			}
			for (int j = vectorCount; j < length; j++)
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
			int vectorCount = length - length % Vector<int>.Count;
			Vector<int> vZero = Vector<int>.Zero;
			fixed (int* arrayPtr = array)
			{
				for (int i = 0; i < vectorCount; i += Vector<int>.Count)
				{
					*(Vector<int>*)(arrayPtr + i) = vZero;
				}
			}
			for (int j = vectorCount; j < length; j++)
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
			Vector<int> vValue = new Vector<int>(value);
			int vectorCount = length - length % Vector<int>.Count;
			fixed (int* arrayPtr = array)
			{
				for (int i = 0; i < vectorCount; i += Vector<int>.Count)
				{
					*(Vector<int>*)(arrayPtr + i) = vValue;
				}
			}
			for (int j = vectorCount; j < length; j++)
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
			Vector<float> vOne = new Vector<float>(1f);
			Vector<float> vZero = Vector<float>.Zero;
			int vectorCount = length - length % VectorSize;
			fixed (float* inputPtr = input)
			{
				fixed (float* outputPtr = output)
				{
					for (int i = 0; i < vectorCount; i += VectorSize)
					{
						Vector<float> vector = *(Vector<float>*)(inputPtr + i);
						Vector<int> vMask = Vector.GreaterThan(vector, vZero);
						Vector<float> vExp = ExpVectorClamped(vector);
						Vector<float> vPlusOne = vector + vOne;
						Vector<float> vExpPlusOne = vExp + vOne;
						*(Vector<float>*)(outputPtr + i) = Vector.ConditionalSelect(vMask, vPlusOne, vExpPlusOne);
					}
				}
			}
			for (int j = vectorCount; j < length; j++)
			{
				float x = input[j];
				if (x > 80f)
				{
					x = 80f;
				}
				else if (x < -80f)
				{
					x = -80f;
				}
				output[j] = ((x > 0f) ? (x + 1f) : ((float)Math.Exp(x) + 1f));
			}
			return;
		}
		for (int k = 0; k < length; k++)
		{
			float x2 = input[k];
			if (x2 > 80f)
			{
				x2 = 80f;
			}
			else if (x2 < -80f)
			{
				x2 = -80f;
			}
			output[k] = ((x2 > 0f) ? (x2 + 1f) : ((float)Math.Exp(x2) + 1f));
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static Vector<float> ExpVectorClamped(Vector<float> v)
	{
		float* values = stackalloc float[VectorSize];
		for (int i = 0; i < VectorSize; i++)
		{
			float x = v[i];
			if (x > 80f)
			{
				x = 80f;
			}
			else if (x < -80f)
			{
				x = -80f;
			}
			values[i] = (float)Math.Exp(x);
		}
		return *(Vector<float>*)values;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static Vector<float> ExpVector(Vector<float> v)
	{
		float* values = stackalloc float[VectorSize];
		for (int i = 0; i < VectorSize; i++)
		{
			values[i] = (float)Math.Exp(v[i]);
		}
		return *(Vector<float>*)values;
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
			Vector<float> vZero = Vector<float>.Zero;
			int vectorCount = length - length % VectorSize;
			fixed (float* inputPtr = input)
			{
				fixed (float* gradOutputPtr = gradOutput)
				{
					fixed (float* gradInputPtr = gradInput)
					{
						for (int i = 0; i < vectorCount; i += VectorSize)
						{
							Vector<float> vector = *(Vector<float>*)(inputPtr + i);
							Vector<float> vGrad = *(Vector<float>*)(gradOutputPtr + i);
							Vector<int> vMask = Vector.GreaterThan(vector, vZero);
							Vector<float> vExp = ExpVector(vector);
							Vector<float> vGradMasked = vGrad * vExp;
							*(Vector<float>*)(gradInputPtr + i) = Vector.ConditionalSelect(vMask, vGrad, vGradMasked);
						}
					}
				}
			}
			for (int j = vectorCount; j < length; j++)
			{
				float x = input[j];
				float go = gradOutput[j];
				gradInput[j] = ((x > 0f) ? go : (go * (float)Math.Exp(x)));
			}
		}
		else
		{
			for (int k = 0; k < length; k++)
			{
				float x2 = input[k];
				float go2 = gradOutput[k];
				gradInput[k] = ((x2 > 0f) ? go2 : (go2 * (float)Math.Exp(x2)));
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
			Vector<float> vScale = new Vector<float>(scale);
			int vectorCount = length - length % VectorSize;
			fixed (float* srcPtr = src)
			{
				fixed (float* resultPtr = result)
				{
					for (int i = 0; i < vectorCount; i += VectorSize)
					{
						Vector<float> vSrc = *(Vector<float>*)(srcPtr + i);
						Vector<float> vResult = *(Vector<float>*)(resultPtr + i);
						*(Vector<float>*)(resultPtr + i) = vResult + vSrc * vScale;
					}
				}
			}
			for (int j = vectorCount; j < length; j++)
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
			fixed (float* resultPtr = result)
			{
				for (int k = 0; k < numItems; k++)
				{
					fixed (float* inputPtr = inputs[k])
					{
						for (int d = 0; d <= dim - VectorSize; d += VectorSize)
						{
							Vector<float> vInput = *(Vector<float>*)(inputPtr + d);
							Vector<float> vResult = *(Vector<float>*)(resultPtr + d);
							*(Vector<float>*)(resultPtr + d) = vResult + vInput;
						}
						for (int i = dim - dim % VectorSize; i < dim; i++)
						{
							result[i] += inputs[k][i];
						}
					}
				}
			}
			return;
		}
		for (int j = 0; j < numItems; j++)
		{
			for (int l = 0; l < dim; l++)
			{
				result[l] += inputs[j][l];
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void BatchScaleAndAccumulateStrided(float[][] inputs, float[] scales, float[][] results, int numItems, int srcDim, int stride)
	{
		if (IsSimdSupported && stride >= VectorSize)
		{
			for (int c = 0; c < numItems; c++)
			{
				float scale = scales[c];
				if (Math.Abs(scale) < 1E-10f)
				{
					continue;
				}
				Vector<float> vScale = new Vector<float>(scale);
				fixed (float* inputPtr = inputs[c])
				{
					fixed (float* resultPtr = results[c])
					{
						for (int d = 0; d <= srcDim - VectorSize; d += VectorSize)
						{
							Vector<float> vInput = *(Vector<float>*)(inputPtr + d);
							Vector<float> vResult = *(Vector<float>*)(resultPtr + d % stride);
							*(Vector<float>*)(resultPtr + d % stride) = vResult + vInput * vScale;
						}
						for (int i = srcDim - srcDim % VectorSize; i < srcDim; i++)
						{
							results[c][i % stride] += inputs[c][i] * scale;
						}
					}
				}
			}
			return;
		}
		for (int j = 0; j < numItems; j++)
		{
			float scale2 = scales[j];
			for (int k = 0; k < srcDim; k++)
			{
				results[j][k % stride] += inputs[j][k] * scale2;
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void BatchOuterProductAccumulate(float[] gradKV, float[][] vProjBatch, float[][] gradKPerCore, int numCores, int dModel, int dK)
	{
		Span<float> gradKVSpan = gradKV.AsSpan();
		if (IsSimdSupported && dK >= VectorSize)
		{
			fixed (float* gradKVPtr = gradKVSpan)
			{
				for (int c = 0; c < numCores; c++)
				{
					fixed (float* gradKPtr = gradKPerCore[c])
					{
						fixed (float* vProjPtr = vProjBatch[c])
						{
							for (int d = 0; d <= dModel - VectorSize; d += VectorSize)
							{
								Vector<float> vGradKV = *(Vector<float>*)(gradKVPtr + d);
								Vector<float> vVProj = *(Vector<float>*)(vProjPtr + d);
								int strideIdx = d % dK;
								Vector<float> vGradK = *(Vector<float>*)(gradKPtr + strideIdx);
								*(Vector<float>*)(gradKPtr + strideIdx) = vGradK + vGradKV * vVProj;
							}
							for (int i = dModel - dModel % VectorSize; i < dModel; i++)
							{
								gradKPerCore[c][i % dK] += gradKV[i] * vProjBatch[c][i];
							}
						}
					}
				}
			}
			return;
		}
		for (int j = 0; j < numCores; j++)
		{
			for (int k = 0; k < dModel; k++)
			{
				gradKPerCore[j][k % dK] += gradKV[k] * vProjBatch[j][k];
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void FusedMultiplyAddDivide(ReadOnlySpan<float> a, ReadOnlySpan<float> b, ReadOnlySpan<float> c, Span<float> output, float invScale)
	{
		int length = Math.Min(Math.Min(a.Length, b.Length), Math.Min(c.Length, output.Length));
		if (IsSimdSupported && length >= VectorSize)
		{
			Vector<float> vInvScale = new Vector<float>(invScale);
			int vectorCount = length - length % VectorSize;
			fixed (float* aPtr = a)
			{
				fixed (float* bPtr = b)
				{
					fixed (float* cPtr = c)
					{
						fixed (float* outputPtr = output)
						{
							for (int i = 0; i < vectorCount; i += VectorSize)
							{
								Vector<float> vA = *(Vector<float>*)(aPtr + i);
								Vector<float> vB = *(Vector<float>*)(bPtr + i);
								Vector<float> vC = *(Vector<float>*)(cPtr + i);
								*(Vector<float>*)(outputPtr + i) = (vA * vB + vC) * vInvScale;
							}
						}
					}
				}
			}
			for (int j = vectorCount; j < length; j++)
			{
				output[j] = (a[j] * b[j] + c[j]) * invScale;
			}
		}
		else
		{
			for (int k = 0; k < length; k++)
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
			for (int c = 0; c < numItems; c++)
			{
				float scale = scales[c];
				if (Math.Abs(scale) < 1E-10f)
				{
					continue;
				}
				Vector<float> vScale = new Vector<float>(scale);
				fixed (float* inputPtr = inputs[c])
				{
					fixed (float* resultPtr = result)
					{
						for (int d = 0; d <= dim - VectorSize; d += VectorSize)
						{
							Vector<float> vInput = *(Vector<float>*)(inputPtr + d);
							Vector<float> vResult = *(Vector<float>*)(resultPtr + d);
							*(Vector<float>*)(resultPtr + d) = vResult + vInput * vScale;
						}
						for (int i = dim - dim % VectorSize; i < dim; i++)
						{
							result[i] += inputs[c][i] * scale;
						}
					}
				}
			}
			return;
		}
		for (int j = 0; j < numItems; j++)
		{
			float scale2 = scales[j];
			for (int k = 0; k < dim; k++)
			{
				result[k] += inputs[j][k] * scale2;
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static void SoftmaxBackward(ReadOnlySpan<float> probs, ReadOnlySpan<float> gradOutput, Span<float> gradInput)
	{
		int length = probs.Length;
		if (length != gradOutput.Length || length != gradInput.Length)
		{
			throw new ArgumentException("数组长度必须相同");
		}
		float sumProbGrad = DotProduct(probs, gradOutput);
		if (IsSimdSupported && length >= VectorSize)
		{
			Vector<float> vSumProbGrad = new Vector<float>(sumProbGrad);
			int vectorCount = length - length % VectorSize;
			fixed (float* probsPtr = probs)
			{
				fixed (float* gradOutputPtr = gradOutput)
				{
					fixed (float* gradInputPtr = gradInput)
					{
						for (int i = 0; i < vectorCount; i += VectorSize)
						{
							Vector<float> vProb = *(Vector<float>*)(probsPtr + i);
							Vector<float> vGradOut = *(Vector<float>*)(gradOutputPtr + i);
							*(Vector<float>*)(gradInputPtr + i) = vProb * (vGradOut - vSumProbGrad);
						}
					}
				}
			}
			for (int j = vectorCount; j < length; j++)
			{
				gradInput[j] = probs[j] * (gradOutput[j] - sumProbGrad);
			}
		}
		else
		{
			for (int k = 0; k < length; k++)
			{
				gradInput[k] = probs[k] * (gradOutput[k] - sumProbGrad);
			}
		}
	}
}
}