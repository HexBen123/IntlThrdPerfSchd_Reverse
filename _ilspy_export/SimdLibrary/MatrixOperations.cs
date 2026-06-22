using System;

namespace SimdLibrary;

public static class MatrixOperations
{
	public static void MatrixVectorMultiply(ReadOnlySpan<float> matrix, ReadOnlySpan<float> vector, Span<float> result, int rows, int cols)
	{
		if (matrix.Length != rows * cols)
		{
			throw new ArgumentException("矩阵大小与行列数不匹配");
		}
		if (vector.Length != cols)
		{
			throw new ArgumentException("向量长度必须等于矩阵列数");
		}
		if (result.Length != rows)
		{
			throw new ArgumentException("结果向量长度必须等于矩阵行数");
		}
		if (VectorMathNew.IsSimdSupported && cols >= VectorMathNew.VectorSize)
		{
			for (int i = 0; i < rows; i++)
			{
				int start = i * cols;
				result[i] = VectorMathNew.DotProduct(matrix.Slice(start, cols), vector);
			}
			return;
		}
		for (int j = 0; j < rows; j++)
		{
			float num = 0f;
			int num2 = j * cols;
			for (int k = 0; k < cols; k++)
			{
				num += matrix[num2 + k] * vector[k];
			}
			result[j] = num;
		}
	}

	public static void BatchMatrixVectorMultiply(ReadOnlySpan<float> matrices, ReadOnlySpan<float> vectors, Span<float> results, int numCores, int rowsPerCore, int cols)
	{
		if (matrices.Length != numCores * rowsPerCore * cols)
		{
			throw new ArgumentException("矩阵总大小不匹配");
		}
		if (vectors.Length != numCores * cols)
		{
			throw new ArgumentException("向量总大小不匹配");
		}
		if (results.Length != numCores * rowsPerCore)
		{
			throw new ArgumentException("结果总大小不匹配");
		}
		if (VectorMathNew.IsSimdSupported && cols >= VectorMathNew.VectorSize)
		{
			for (int i = 0; i < numCores; i++)
			{
				int start = i * cols;
				int num = i * rowsPerCore;
				for (int j = 0; j < rowsPerCore; j++)
				{
					int start2 = (i * rowsPerCore + j) * cols;
					results[num + j] = VectorMathNew.DotProduct(matrices.Slice(start2, cols), vectors.Slice(start, cols));
				}
			}
			return;
		}
		for (int k = 0; k < numCores; k++)
		{
			int num2 = k * cols;
			int num3 = k * rowsPerCore;
			for (int l = 0; l < rowsPerCore; l++)
			{
				int num4 = (k * rowsPerCore + l) * cols;
				float num5 = 0f;
				for (int m = 0; m < cols; m++)
				{
					num5 += matrices[num4 + m] * vectors[num2 + m];
				}
				results[num3 + l] = num5;
			}
		}
	}

	public static void Transpose(ReadOnlySpan<float> matrix, Span<float> result, int rows, int cols)
	{
		if (matrix.Length != rows * cols || result.Length != cols * rows)
		{
			throw new ArgumentException("矩阵大小与行列数不匹配");
		}
		for (int i = 0; i < rows; i++)
		{
			for (int j = 0; j < cols; j++)
			{
				result[j * rows + i] = matrix[i * cols + j];
			}
		}
	}

	public static void BatchLinear(float[][] inputs, ReadOnlySpan<float> weights, ReadOnlySpan<float> bias, float[][] outputs, int batchSize, int inDim, int outDim)
	{
		if (VectorMathNew.IsSimdSupported && inDim >= VectorMathNew.VectorSize)
		{
			for (int i = 0; i < batchSize; i++)
			{
				MatrixVectorMultiply(vector: new ReadOnlySpan<float>(inputs[i], 0, inDim), result: new Span<float>(outputs[i], 0, outDim), matrix: weights, rows: outDim, cols: inDim);
				for (int j = 0; j < outDim; j++)
				{
					outputs[i][j] += bias[j];
				}
			}
			return;
		}
		for (int k = 0; k < batchSize; k++)
		{
			for (int l = 0; l < outDim; l++)
			{
				float num = 0f;
				for (int m = 0; m < inDim; m++)
				{
					num += inputs[k][m] * weights[l * inDim + m];
				}
				outputs[k][l] = num + bias[l];
			}
		}
	}

	public static void Add(ReadOnlySpan<float> left, ReadOnlySpan<float> right, Span<float> result)
	{
		VectorMathNew.Add(left, right, result);
	}

	public static void Subtract(ReadOnlySpan<float> left, ReadOnlySpan<float> right, Span<float> result)
	{
		VectorMathNew.Subtract(left, right, result);
	}

	public static void MultiplyScalar(ReadOnlySpan<float> matrix, float scalar, Span<float> result)
	{
		VectorMathNew.MultiplyScalar(matrix, scalar, result);
	}
}
