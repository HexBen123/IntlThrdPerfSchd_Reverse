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
		if (VectorMath.IsSimdSupported && cols >= VectorMath.VectorSize)
		{
			for (int i = 0; i < rows; i++)
			{
				int rowStart = i * cols;
				result[i] = VectorMath.DotProduct(matrix.Slice(rowStart, cols), vector);
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

	public static void Add(ReadOnlySpan<float> left, ReadOnlySpan<float> right, Span<float> result)
	{
		VectorMath.Add(left, right, result);
	}

	public static void Subtract(ReadOnlySpan<float> left, ReadOnlySpan<float> right, Span<float> result)
	{
		VectorMath.Subtract(left, right, result);
	}

	public static void MultiplyScalar(ReadOnlySpan<float> matrix, float scalar, Span<float> result)
	{
		VectorMath.MultiplyScalar(matrix, scalar, result);
	}
}
}
