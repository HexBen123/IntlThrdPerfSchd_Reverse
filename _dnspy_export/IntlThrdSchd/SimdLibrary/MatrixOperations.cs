using System;

namespace SimdLibrary
{
	// Token: 0x0200001F RID: 31
	public static class MatrixOperations
	{
		// Token: 0x060001D5 RID: 469 RVA: 0x000163AC File Offset: 0x000145AC
		public unsafe static void MatrixVectorMultiply(ReadOnlySpan<float> matrix, ReadOnlySpan<float> vector, Span<float> result, int rows, int cols)
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
					int num = i * cols;
					*result[i] = VectorMathNew.DotProduct(matrix.Slice(num, cols), vector);
				}
				return;
			}
			for (int j = 0; j < rows; j++)
			{
				float num2 = 0f;
				int num3 = j * cols;
				for (int k = 0; k < cols; k++)
				{
					num2 += *matrix[num3 + k] * *vector[k];
				}
				*result[j] = num2;
			}
		}

		// Token: 0x060001D6 RID: 470 RVA: 0x00016488 File Offset: 0x00014688
		public unsafe static void BatchMatrixVectorMultiply(ReadOnlySpan<float> matrices, ReadOnlySpan<float> vectors, Span<float> results, int numCores, int rowsPerCore, int cols)
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
					int num = i * cols;
					int num2 = i * rowsPerCore;
					for (int j = 0; j < rowsPerCore; j++)
					{
						int num3 = (i * rowsPerCore + j) * cols;
						*results[num2 + j] = VectorMathNew.DotProduct(matrices.Slice(num3, cols), vectors.Slice(num, cols));
					}
				}
				return;
			}
			for (int k = 0; k < numCores; k++)
			{
				int num4 = k * cols;
				int num5 = k * rowsPerCore;
				for (int l = 0; l < rowsPerCore; l++)
				{
					int num6 = (k * rowsPerCore + l) * cols;
					float num7 = 0f;
					for (int m = 0; m < cols; m++)
					{
						num7 += *matrices[num6 + m] * *vectors[num4 + m];
					}
					*results[num5 + l] = num7;
				}
			}
		}

		// Token: 0x060001D7 RID: 471 RVA: 0x000165C8 File Offset: 0x000147C8
		public unsafe static void Transpose(ReadOnlySpan<float> matrix, Span<float> result, int rows, int cols)
		{
			if (matrix.Length != rows * cols || result.Length != cols * rows)
			{
				throw new ArgumentException("矩阵大小与行列数不匹配");
			}
			for (int i = 0; i < rows; i++)
			{
				for (int j = 0; j < cols; j++)
				{
					*result[j * rows + i] = *matrix[i * cols + j];
				}
			}
		}

		// Token: 0x060001D8 RID: 472 RVA: 0x0001662C File Offset: 0x0001482C
		public unsafe static void BatchLinear(float[][] inputs, ReadOnlySpan<float> weights, ReadOnlySpan<float> bias, float[][] outputs, int batchSize, int inDim, int outDim)
		{
			if (VectorMathNew.IsSimdSupported && inDim >= VectorMathNew.VectorSize)
			{
				for (int i = 0; i < batchSize; i++)
				{
					ReadOnlySpan<float> readOnlySpan = new ReadOnlySpan<float>(inputs[i], 0, inDim);
					Span<float> span = new Span<float>(outputs[i], 0, outDim);
					MatrixOperations.MatrixVectorMultiply(weights, readOnlySpan, span, outDim, inDim);
					for (int j = 0; j < outDim; j++)
					{
						outputs[i][j] += *bias[j];
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
						num += inputs[k][m] * *weights[l * inDim + m];
					}
					outputs[k][l] = num + *bias[l];
				}
			}
		}

		// Token: 0x060001D9 RID: 473 RVA: 0x0001670B File Offset: 0x0001490B
		public static void Add(ReadOnlySpan<float> left, ReadOnlySpan<float> right, Span<float> result)
		{
			VectorMathNew.Add(left, right, result);
		}

		// Token: 0x060001DA RID: 474 RVA: 0x00016715 File Offset: 0x00014915
		public static void Subtract(ReadOnlySpan<float> left, ReadOnlySpan<float> right, Span<float> result)
		{
			VectorMathNew.Subtract(left, right, result);
		}

		// Token: 0x060001DB RID: 475 RVA: 0x0001671F File Offset: 0x0001491F
		public static void MultiplyScalar(ReadOnlySpan<float> matrix, float scalar, Span<float> result)
		{
			VectorMathNew.MultiplyScalar(matrix, scalar, result);
		}
	}
}
