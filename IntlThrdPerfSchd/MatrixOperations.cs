using System;

namespace SimdLibrary
{

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
				int rowStart = i * cols;
				result[i] = VectorMathNew.DotProduct(matrix.Slice(rowStart, cols), vector);
			}
			return;
		}
		for (int j = 0; j < rows; j++)
		{
			float sum = 0f;
			int rowStart2 = j * cols;
			for (int k = 0; k < cols; k++)
			{
				sum += matrix[rowStart2 + k] * vector[k];
			}
			result[j] = sum;
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
			for (int c = 0; c < numCores; c++)
			{
				int vecStart = c * cols;
				int resStart = c * rowsPerCore;
				for (int r = 0; r < rowsPerCore; r++)
				{
					int matStart = (c * rowsPerCore + r) * cols;
					results[resStart + r] = VectorMathNew.DotProduct(matrices.Slice(matStart, cols), vectors.Slice(vecStart, cols));
				}
			}
			return;
		}
		for (int i = 0; i < numCores; i++)
		{
			int vecStart2 = i * cols;
			int resStart2 = i * rowsPerCore;
			for (int j = 0; j < rowsPerCore; j++)
			{
				int matStart2 = (i * rowsPerCore + j) * cols;
				float sum = 0f;
				for (int k = 0; k < cols; k++)
				{
					sum += matrices[matStart2 + k] * vectors[vecStart2 + k];
				}
				results[resStart2 + j] = sum;
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
			for (int k = 0; k < batchSize; k++)
			{
				MatrixVectorMultiply(vector: new ReadOnlySpan<float>(inputs[k], 0, inDim), result: new Span<float>(outputs[k], 0, outDim), matrix: weights, rows: outDim, cols: inDim);
				for (int j = 0; j < outDim; j++)
				{
					outputs[k][j] += bias[j];
				}
			}
			return;
		}
		for (int i = 0; i < batchSize; i++)
		{
			for (int l = 0; l < outDim; l++)
			{
				float sum = 0f;
				for (int m = 0; m < inDim; m++)
				{
					sum += inputs[i][m] * weights[l * inDim + m];
				}
				outputs[i][l] = sum + bias[l];
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
}