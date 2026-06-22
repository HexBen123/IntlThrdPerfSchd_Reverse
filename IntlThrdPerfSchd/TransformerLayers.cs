using System;
using System.Runtime.CompilerServices;
using SimdLibrary;

namespace IntlThrdPerfSchd
{
	public static class MathHelper
	{
		public const int SIMD_SIZE = 8;

		private static readonly Random _rng = new Random(42);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void InitWeights(float[] weights, int fanIn, int fanOut)
		{
			float num = (float)Math.Sqrt(2.0 / (double)(fanIn + fanOut));
			for (int i = 0; i < weights.Length; i++)
			{
				weights[i] = (float)(_rng.NextDouble() * 2.0 - 1.0) * num;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Add(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> result)
		{
			VectorMathNew.Add(a, b, result);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Scale(Span<float> data, float scale)
		{
			VectorMathNew.MultiplyScalar(data, scale, data);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float DotProduct(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
		{
			int length = Math.Min(a.Length, b.Length);
			return VectorMathNew.DotProduct(a.Slice(0, length), b.Slice(0, length));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Softmax(ReadOnlySpan<float> input, Span<float> output)
		{
			float num = VectorMathNew.Max(input);
			float num2 = 0f;
			for (int i = 0; i < input.Length; i++)
			{
				output[i] = (float)Math.Exp(input[i] - num);
				num2 += output[i];
			}
			float scale = 1f / num2;
			Scale(output, scale);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LayerNorm(ReadOnlySpan<float> input, ReadOnlySpan<float> gamma, ReadOnlySpan<float> beta, Span<float> output, float epsilon = 1E-05f)
		{
			int length = input.Length;
			float num = VectorMathNew.Mean(input);
			float num2 = VectorMathNew.Variance(input);
			float num3 = 1f / (float)Math.Sqrt(num2 + epsilon);
			for (int i = 0; i < length; i++)
			{
				output[i] = (input[i] - num) * num3 * gamma[i] + beta[i];
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Linear(ReadOnlySpan<float> input, ReadOnlySpan<float> weights, ReadOnlySpan<float> bias, Span<float> result, int inDim, int outDim)
		{
			MatrixOperations.MatrixVectorMultiply(weights, input, result, outDim, inDim);
			for (int i = 0; i < outDim; i++)
			{
				result[i] += bias[i];
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ComputeWeightGrad(ReadOnlySpan<float> gradOutput, ReadOnlySpan<float> input, Span<float> weightGrad, int outDim, int inDim, bool accumulate = false)
		{
			for (int i = 0; i < outDim; i++)
			{
				float num = gradOutput[i];
				int num2 = i * inDim;
				if (accumulate)
				{
					for (int j = 0; j < inDim; j++)
					{
						weightGrad[num2 + j] += num * input[j];
					}
				}
				else
				{
					for (int k = 0; k < inDim; k++)
					{
						weightGrad[num2 + k] = num * input[k];
					}
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ComputeInputGrad(ReadOnlySpan<float> gradOutput, ReadOnlySpan<float> weights, Span<float> inputGrad, int outDim, int inDim)
		{
			for (int i = 0; i < inDim; i++)
			{
				float num = 0f;
				for (int j = 0; j < outDim; j++)
				{
					num += gradOutput[j] * weights[j * inDim + i];
				}
				inputGrad[i] = num;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void MultiplyElementwise(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> result)
		{
			int num = Math.Min(Math.Min(a.Length, b.Length), result.Length);
			for (int i = 0; i < num; i++)
			{
				result[i] = a[i] * b[i];
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Sum(ReadOnlySpan<float> array)
		{
			float num = 0f;
			for (int i = 0; i < array.Length; i++)
			{
				num += array[i];
			}
			return num;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Clear(Span<float> array)
		{
			VectorMathNew.Zero(array);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Clear(Span<int> array)
		{
			VectorMathNew.ZeroInt(array);
		}
	}

	public class LinearLayer
	{
		public readonly float[] Weights;

		public readonly float[] Bias;

		public readonly int InDim;

		public readonly int OutDim;

		public readonly float[] WeightGrads;

		public readonly float[] BiasGrads;

		public readonly float[] InputGrads;

		public readonly float[] WeightMom;

		public readonly float[] BiasMom;

		private float[] _cachedInput;

		private bool _inputCached;

		private readonly float[] _tempInputGrad;

		public LinearLayer(int inDim, int outDim)
		{
			InDim = inDim;
			OutDim = outDim;
			Weights = new float[outDim * inDim];
			Bias = new float[outDim];
			WeightGrads = new float[outDim * inDim];
			BiasGrads = new float[outDim];
			InputGrads = new float[inDim];
			WeightMom = new float[outDim * inDim];
			BiasMom = new float[outDim];
			_cachedInput = new float[inDim];
			_tempInputGrad = new float[inDim];
			_inputCached = false;
			MathHelper.InitWeights(Weights, inDim, OutDim);
			MathHelper.Clear(Bias.AsSpan(0, OutDim));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Forward(ReadOnlySpan<float> input, Span<float> output)
		{
			input.CopyTo(_cachedInput);
			_inputCached = true;
			MathHelper.Linear(input, Weights, Bias, output, InDim, OutDim);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Backward(ReadOnlySpan<float> gradOutput, float learningRate = 0.001f, bool accumulateGrad = false)
		{
			if (!_inputCached)
			{
				return;
			}
			MathHelper.ComputeWeightGrad(gradOutput, _cachedInput, WeightGrads, OutDim, InDim, accumulateGrad);
			if (accumulateGrad)
			{
				VectorMathNew.Add(BiasGrads.AsSpan(0, OutDim), gradOutput, BiasGrads.AsSpan(0, OutDim));
			}
			else
			{
				gradOutput.CopyTo(BiasGrads.AsSpan(0, OutDim));
			}
			if (accumulateGrad)
			{
				MathHelper.ComputeInputGrad(gradOutput, Weights, _tempInputGrad, OutDim, InDim);
				VectorMathNew.Add(InputGrads.AsSpan(0, InDim), _tempInputGrad.AsSpan(0, InDim), InputGrads.AsSpan(0, InDim));
			}
			else
			{
				MathHelper.ComputeInputGrad(gradOutput, Weights, InputGrads, OutDim, InDim);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ApplyGradients(float learningRate = 0.001f, float beta1 = 0.9f, float beta2 = 0.999f, float epsilon = 1E-08f, int t = 1)
		{
			int num = OutDim * InDim;
			for (int i = 0; i < num; i++)
			{
				float num2 = WeightGrads[i];
				WeightMom[i] = beta1 * WeightMom[i] + (1f - beta1) * num2;
				float num3 = beta2 * 0f + (1f - beta2) * num2 * num2;
				float num4 = WeightMom[i] / (1f - (float)Math.Pow(beta1, t));
				float num5 = num3 / (1f - (float)Math.Pow(beta2, t));
				Weights[i] -= learningRate * num4 / ((float)Math.Sqrt(num5) + epsilon);
			}
			for (int j = 0; j < OutDim; j++)
			{
				float num6 = BiasGrads[j];
				BiasMom[j] = beta1 * BiasMom[j] + (1f - beta1) * num6;
				float num7 = beta2 * 0f + (1f - beta2) * num6 * num6;
				float num8 = BiasMom[j] / (1f - (float)Math.Pow(beta1, t));
				float num9 = num7 / (1f - (float)Math.Pow(beta2, t));
				Bias[j] -= learningRate * num8 / ((float)Math.Sqrt(num9) + epsilon);
			}
			MathHelper.Clear(WeightGrads.AsSpan(0, num));
			MathHelper.Clear(BiasGrads.AsSpan(0, OutDim));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ApplyGradientsSGD(float learningRate = 0.001f, float maxGradNorm = 1f)
		{
			int length = OutDim * InDim;
			Span<float> span = WeightGrads.AsSpan(0, length);
			float num = VectorMathNew.EuclideanNorm(span);
			if (num > maxGradNorm)
			{
				float scalar = maxGradNorm / num;
				VectorMathNew.MultiplyScalarInPlace(span, scalar);
			}
			Span<float> span2 = BiasGrads.AsSpan(0, OutDim);
			float num2 = VectorMathNew.EuclideanNorm(span2);
			if (num2 > maxGradNorm)
			{
				float scalar2 = maxGradNorm / num2;
				VectorMathNew.MultiplyScalarInPlace(span2, scalar2);
			}
			Span<float> span3 = Weights.AsSpan(0, length);
			VectorMathNew.MultiplyScalarInPlace(span, -learningRate);
			VectorMathNew.Add(span3, span, span3);
			VectorMathNew.MultiplyScalarInPlace(span2, -learningRate);
			VectorMathNew.Add(Bias.AsSpan(0, OutDim), span2, Bias.AsSpan(0, OutDim));
			MathHelper.Clear(WeightGrads.AsSpan(0, length));
			MathHelper.Clear(BiasGrads.AsSpan(0, OutDim));
		}
	}

	public class LayerNormLayer
	{
		public readonly float[] Gamma;

		public readonly float[] Beta;

		public readonly int Dim;

		public readonly float[] GammaGrads;

		public readonly float[] BetaGrads;

		public readonly float[] InputGrads;

		private float[] _cachedInput;

		private float[] _cachedMean;

		private float[] _cachedInvStd;

		private float[] _cachedXNorm;

		private float[] _tempBuf;

		private bool _cached;

		public LayerNormLayer(int dim)
		{
			Dim = dim;
			Gamma = new float[dim];
			Beta = new float[dim];
			GammaGrads = new float[dim];
			BetaGrads = new float[dim];
			InputGrads = new float[dim];
			_cachedInput = new float[dim];
			_cachedMean = new float[1];
			_cachedInvStd = new float[1];
			_cachedXNorm = new float[dim];
			_tempBuf = new float[dim];
			_cached = false;
			for (int i = 0; i < dim; i++)
			{
				Gamma[i] = 1f;
				Beta[i] = 0f;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Forward(ReadOnlySpan<float> input, Span<float> output)
		{
			int length = input.Length;
			float num = 0f;
			for (int i = 0; i < length; i++)
			{
				num += input[i];
			}
			num /= (float)length;
			_cachedMean[0] = num;
			float num2 = 0f;
			for (int j = 0; j < length; j++)
			{
				float num3 = input[j] - num;
				num2 += num3 * num3;
			}
			num2 /= (float)length;
			float num4 = 1f / (float)Math.Sqrt(num2 + 1E-05f);
			_cachedInvStd[0] = num4;
			input.CopyTo(_cachedInput);
			for (int k = 0; k < length; k++)
			{
				_cachedXNorm[k] = (input[k] - num) * num4;
				output[k] = _cachedXNorm[k] * Gamma[k] + Beta[k];
			}
			_cached = true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Backward(ReadOnlySpan<float> gradOutput, float learningRate = 0.001f)
		{
			if (_cached)
			{
				goto IL_Cached;
			}
			return;
		IL_Cached:
			int dim = Dim;
			_ = _cachedMean[0];
			float invStd = _cachedInvStd[0];
			Span<float> result = new Span<float>(GammaGrads, 0, dim);
			Span<float> destination = new Span<float>(BetaGrads, 0, dim);
			MathHelper.MultiplyElementwise(gradOutput.Slice(0, dim), new ReadOnlySpan<float>(_cachedXNorm, 0, dim), result);
			ReadOnlySpan<float> readOnlySpan = gradOutput.Slice(0, dim);
			readOnlySpan.CopyTo(destination);
			MathHelper.MultiplyElementwise(gradOutput.Slice(0, dim), new ReadOnlySpan<float>(Gamma, 0, dim), _tempBuf);
			MathHelper.Sum(_tempBuf.AsSpan(0, dim));
			MathHelper.MultiplyElementwise(gradOutput.Slice(0, dim), new ReadOnlySpan<float>(Gamma, 0, dim), _tempBuf);
			VectorMathNew.ComputeLayerNormInputGrad(xNorm: new ReadOnlySpan<float>(_cachedXNorm, 0, dim), gradOutput: gradOutput.Slice(0, dim), gamma: new ReadOnlySpan<float>(Gamma, 0, dim), invStd: invStd, result: InputGrads);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ApplyGradients(float learningRate = 0.001f)
		{
			VectorMathNew.MultiplyScalarInPlace(GammaGrads.AsSpan(0, Dim), -learningRate);
			VectorMathNew.Add(Gamma.AsSpan(0, Dim), GammaGrads.AsSpan(0, Dim), Gamma.AsSpan(0, Dim));
			VectorMathNew.MultiplyScalarInPlace(BetaGrads.AsSpan(0, Dim), -learningRate);
			VectorMathNew.Add(Beta.AsSpan(0, Dim), BetaGrads.AsSpan(0, Dim), Beta.AsSpan(0, Dim));
			MathHelper.Clear(GammaGrads.AsSpan(0, Dim));
			MathHelper.Clear(BetaGrads.AsSpan(0, Dim));
		}
	}

	public class MultiHeadAttention
	{
		private const int MAX_CORES = 64;

		private const float GRADIENT_CLIP_THRESHOLD = 1f;

		private readonly int _dModel;

		private readonly int _nHead;

		private readonly int _dK;

		private readonly int _dV;

		public readonly LinearLayer Wq;

		public readonly LinearLayer Wk;

		public readonly LinearLayer Wv;

		public readonly LinearLayer Wo;

		private readonly float[] _qProj;

		private readonly float[] _kProj;

		private readonly float[] _vProj;

		private readonly float[] _attentionScores;

		private readonly float[] _attentionWeights;

		private readonly float[] _headOutputs;

		private readonly float[] _concatHeads;

		private readonly float[][] _cachedKeys;

		private readonly float[][] _cachedValues;

		private readonly float[] _cachedQProj;

		private readonly float[] _cachedQuery;

		private int _cachedNumCores;

		private bool _cached;

		private readonly float[] _gradQProj;

		private readonly float[] _gradConcatHeads;

		private readonly float[] _gradAttentionWeights;

		private readonly float[] _gradAttentionScores;

		private readonly float[] _gradHeadOutputs;

		private readonly float[] _gradKeys;

		private readonly float[] _gradValues;

		public int NumHeads => _nHead;

		public float[][] GetHeadAttentionWeights(int numCores)
		{
			float[][] array = new float[_nHead][];
			for (int i = 0; i < _nHead; i++)
			{
				array[i] = new float[numCores];
				for (int j = 0; j < numCores; j++)
				{
					array[i][j] = _attentionWeights[i * 64 + j];
				}
			}
			return array;
		}

		public MultiHeadAttention(int dModel, int nHead)
		{
			_dModel = dModel;
			_nHead = nHead;
			_dK = dModel / nHead;
			_dV = dModel / nHead;
			Wq = new LinearLayer(dModel, dModel);
			Wk = new LinearLayer(dModel, dModel);
			Wv = new LinearLayer(dModel, dModel);
			Wo = new LinearLayer(dModel, dModel);
			_qProj = new float[dModel];
			_kProj = new float[dModel];
			_vProj = new float[dModel];
			_attentionScores = new float[nHead * 64];
			_attentionWeights = new float[nHead * 64];
			_headOutputs = new float[nHead * _dV];
			_concatHeads = new float[dModel];
			_cachedKeys = new float[64][];
			_cachedValues = new float[64][];
			for (int i = 0; i < 64; i++)
			{
				_cachedKeys[i] = new float[dModel];
				_cachedValues[i] = new float[dModel];
			}
			_cachedQProj = new float[dModel];
			_cachedQuery = new float[dModel];
			_cachedNumCores = 0;
			_cached = false;
			_gradQProj = new float[dModel];
			_gradConcatHeads = new float[dModel];
			_gradAttentionWeights = new float[nHead * 64];
			_gradAttentionScores = new float[nHead * 64];
			_gradHeadOutputs = new float[nHead * _dV];
			_gradKeys = new float[dModel * 64];
			_gradValues = new float[dModel * 64];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void CrossAttention(ReadOnlySpan<float> query, float[][] keys, float[][] values, Span<float> output, int numCores, Span<float> attentionWeights = default(Span<float>))
		{
			query.CopyTo(_cachedQuery);
			for (int i = 0; i < numCores; i++)
			{
				Array.Copy(keys[i], _cachedKeys[i], _dModel);
				Array.Copy(values[i], _cachedValues[i], _dModel);
			}
			_cachedNumCores = numCores;
			Wq.Forward(query, _qProj);
			Array.Copy(_qProj, _cachedQProj, _dModel);
			for (int j = 0; j < _nHead; j++)
			{
				int num = j * _dK;
				for (int k = 0; k < numCores; k++)
				{
					float num2 = 0f;
					for (int l = 0; l < _dK; l++)
					{
						num2 += _qProj[num + l] * keys[k][num + l];
					}
					_attentionScores[j * 64 + k] = num2 / (float)Math.Sqrt(_dK);
				}
				int num3 = j * 64;
				MathHelper.Softmax(new ReadOnlySpan<float>(_attentionScores, num3, numCores), new Span<float>(_attentionWeights, num3, numCores));
				for (int m = 0; m < _dV; m++)
				{
					float num4 = 0f;
					for (int n = 0; n < numCores; n++)
					{
						num4 += _attentionWeights[num3 + n] * values[n][j * _dV + m];
					}
					_headOutputs[j * _dV + m] = num4;
				}
			}
			for (int num5 = 0; num5 < _nHead; num5++)
			{
				Buffer.BlockCopy(_headOutputs, num5 * _dV * 4, _concatHeads, num5 * _dV * 4, _dV * 4);
			}
			Wo.Forward(_concatHeads, output);
			if (!attentionWeights.IsEmpty)
			{
				Span<float> span = attentionWeights.Slice(0, numCores);
				VectorMathNew.Zero(span);
				for (int num6 = 0; num6 < _nHead; num6++)
				{
					int start = num6 * 64;
					Span<float> span2 = _attentionWeights.AsSpan(start, numCores);
					VectorMathNew.Add(span, span2, span);
				}
				VectorMathNew.MultiplyScalarInPlace(span, 1f / (float)_nHead);
			}
			_cached = true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Backward(ReadOnlySpan<float> gradOutput, float learningRate = 0.001f)
		{
			if (!_cached)
			{
				return;
			}
			if (VectorMathNew.HasInvalidValues(gradOutput))
			{
				return;
			}
			int num = Math.Min(gradOutput.Length, _dModel);
			for (int i = 0; i < num; i++)
			{
				_gradConcatHeads[i] = gradOutput[i];
			}
			float num2 = VectorMathNew.EuclideanNorm(_gradConcatHeads.AsSpan(0, num));
			if (num2 > 1f)
			{
				float scalar = 1f / num2;
				VectorMathNew.MultiplyScalarInPlace(_gradConcatHeads.AsSpan(0, num), scalar);
			}
			Array.Clear(_gradKeys, 0, _dModel * 64);
			Array.Clear(_gradValues, 0, _dModel * 64);
			Wo.Backward(_gradConcatHeads.AsSpan(0, _dModel), learningRate);
			float[] inputGrads = Wo.InputGrads;
			for (int j = 0; j < _nHead; j++)
			{
				int num3 = j * _dV;
				int num4 = j * 64;
				ReadOnlySpan<float> b = new ReadOnlySpan<float>(inputGrads, num3, _dV);
				Span<float> span = new Span<float>(_gradAttentionWeights, num4, _cachedNumCores);
				VectorMathNew.BatchVectorsDotVector(_cachedValues, b, span, _cachedNumCores, _dV);
				float num5 = VectorMathNew.DotProduct(new ReadOnlySpan<float>(_attentionWeights, num4, _cachedNumCores), span);
				for (int k = 0; k < _cachedNumCores; k++)
				{
					float num6 = _attentionWeights[num4 + k];
					_gradAttentionScores[num4 + k] = num6 * (_gradAttentionWeights[num4 + k] - num5);
				}
				float num7 = 1f / (float)Math.Sqrt(_dK);
				VectorMathNew.BatchWeightedDotProduct(new ReadOnlySpan<float>(_gradAttentionScores, num4, _cachedNumCores), _cachedKeys, _gradQProj.AsSpan(num3, _dK), _cachedNumCores, _dK);
				VectorMathNew.MultiplyScalarInPlace(_gradQProj.AsSpan(num3, _dK), num7);
				for (int l = 0; l < _cachedNumCores; l++)
				{
					float num8 = _gradAttentionScores[num4 + l] * num7;
					int num9 = l * _dModel;
					for (int m = 0; m < _dK; m++)
					{
						_gradKeys[num9 + num3 + m] += num8 * _cachedQuery[num3 + m];
					}
				}
				for (int n = 0; n < _cachedNumCores; n++)
				{
					float num10 = _attentionWeights[num4 + n];
					int num11 = n * _dModel;
					int num12 = num3;
					for (int num13 = 0; num13 < _dV; num13++)
					{
						_gradValues[num11 + num12 + num13] += num10 * inputGrads[num12 + num13];
					}
				}
			}
			Wq.Backward(_gradQProj, learningRate);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SelfForward(ReadOnlySpan<float> input, Span<float> output)
		{
			input.CopyTo(_cachedQuery);
			Wq.Forward(input, _qProj);
			Wk.Forward(input, _kProj);
			Wv.Forward(input, _vProj);
			Array.Copy(_qProj, _cachedQProj, _dModel);
			for (int i = 0; i < 1; i++)
			{
				Array.Copy(_kProj, _cachedKeys[i], _dModel);
				Array.Copy(_vProj, _cachedValues[i], _dModel);
			}
			_cachedNumCores = 1;
			for (int j = 0; j < _nHead; j++)
			{
				int start = j * _dK;
				Span<float> span;
				float num = VectorMathNew.DotProduct(_qProj.AsSpan(start, _dK), span = _kProj.AsSpan(start, _dK)) / (float)Math.Sqrt(_dK);
				_attentionScores[j * 64] = num;
				_attentionWeights[j * 64] = 1f;
				Span<float> span2 = _vProj.AsSpan(start, _dV);
				VectorMathNew.Copy(dest: _headOutputs.AsSpan(j * _dV, _dV), source: span2);
			}
			for (int k = 0; k < _nHead; k++)
			{
				Buffer.BlockCopy(_headOutputs, k * _dV * 4, _concatHeads, k * _dV * 4, _dV * 4);
			}
			Wo.Forward(_concatHeads, output);
			_cached = true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SelfBackward(ReadOnlySpan<float> gradOutput, float learningRate = 0.001f)
		{
			if (!_cached)
			{
				return;
			}
			if (VectorMathNew.HasInvalidValues(gradOutput))
			{
				return;
			}
			int num = Math.Min(gradOutput.Length, _dModel);
			for (int i = 0; i < num; i++)
			{
				_gradConcatHeads[i] = gradOutput[i];
			}
			float num2 = VectorMathNew.EuclideanNorm(_gradConcatHeads.AsSpan(0, num));
			if (num2 > 1f)
			{
				float scalar = 1f / num2;
				VectorMathNew.MultiplyScalarInPlace(_gradConcatHeads.AsSpan(0, num), scalar);
			}
			Array.Clear(_gradKeys, 0, _dModel);
			Array.Clear(_gradValues, 0, _dModel);
			Wo.Backward(_gradConcatHeads.AsSpan(0, _dModel), learningRate);
			float[] inputGrads = Wo.InputGrads;
			for (int j = 0; j < _nHead; j++)
			{
				int num3 = j * _dV;
				int num4 = j * 64;
				ReadOnlySpan<float> readOnlySpan = new ReadOnlySpan<float>(inputGrads, num3, _dV);
				Span<float> span = new Span<float>(_gradAttentionWeights, num4, 1);
				VectorMathNew.BatchVectorsDotVector(_cachedValues, readOnlySpan, span, 1, _dV);
				float num5 = 0f;
				for (int k = 0; k < _dV; k++)
				{
					num5 += inputGrads[num3 + k];
				}
				_gradAttentionScores[num4] = num5;
				float num6 = 1f / (float)Math.Sqrt(_dK);
				float num7 = _gradAttentionScores[num4] * num6;
				for (int l = 0; l < _dK; l++)
				{
					_gradQProj[num3 + l] = num7 * _cachedKeys[0][num3 + l];
				}
				for (int m = 0; m < _dK; m++)
				{
					_gradKeys[num3 + m] += num7 * _cachedQuery[num3 + m];
				}
				float num8 = _attentionWeights[num4];
				for (int n = 0; n < _dV; n++)
				{
					_gradValues[num3 + n] += num8 * inputGrads[num3 + n];
				}
			}
			Wq.Backward(_gradQProj, learningRate);
			Wk.Backward(new ReadOnlySpan<float>(_gradKeys, 0, _dModel), learningRate);
			Wv.Backward(new ReadOnlySpan<float>(_gradValues, 0, _dModel), learningRate);
		}

		public float[] GetSelfInputGradients()
		{
			float[] inputGrads = Wq.InputGrads;
			float[] inputGrads2 = Wk.InputGrads;
			float[] inputGrads3 = Wv.InputGrads;
			for (int i = 0; i < _dModel; i++)
			{
				_gradQProj[i] = inputGrads[i] + inputGrads2[i] + inputGrads3[i];
			}
			return _gradQProj;
		}

		public void ApplyGradients(float learningRate = 0.001f)
		{
			Wo.ApplyGradientsSGD(learningRate);
			Wq.ApplyGradientsSGD(learningRate);
			Wk.ApplyGradientsSGD(learningRate);
			Wv.ApplyGradientsSGD(learningRate);
		}

		public float[] GetQueryGradients()
		{
			return Wq.InputGrads;
		}

		public float[] GetKeyGradients()
		{
			return _gradKeys;
		}

		public float[] GetValueGradients()
		{
			return _gradValues;
		}

		public int GetCachedNumCores()
		{
			return _cachedNumCores;
		}
	}

	public class FeedForwardLayer
	{
		private readonly LinearLayer _fc1;

		private readonly LinearLayer _fc2;

		private readonly int _dModel;

		private readonly int _dFF;

		private readonly float[] _hidden;

		private readonly float[] _hiddenAfterRelu;

		private readonly float[] _gradHidden;

		private readonly float[] _gradInput;

		public LinearLayer FC1 => _fc1;

		public LinearLayer FC2 => _fc2;

		public float[] InputGradients => _fc1.InputGrads;

		public FeedForwardLayer(int dModel, int dFF = 256)
		{
			_dModel = dModel;
			_dFF = dFF;
			_fc1 = new LinearLayer(dModel, dFF);
			_fc2 = new LinearLayer(dFF, dModel);
			_hidden = new float[dFF];
			_hiddenAfterRelu = new float[dFF];
			_gradHidden = new float[dFF];
			_gradInput = new float[dModel];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Forward(ReadOnlySpan<float> input, Span<float> output)
		{
			_fc1.Forward(input, _hidden);
			for (int i = 0; i < _dFF; i++)
			{
				_hiddenAfterRelu[i] = Math.Max(0f, _hidden[i]);
			}
			_fc2.Forward(_hiddenAfterRelu, output);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Backward(ReadOnlySpan<float> gradOutput, float learningRate = 0.001f)
		{
			_fc2.Backward(gradOutput, learningRate);
			float[] inputGrads = _fc2.InputGrads;
			VectorMathNew.ReluGradient(new ReadOnlySpan<float>(_hidden, 0, _dFF), new ReadOnlySpan<float>(inputGrads, 0, _dFF), new Span<float>(_gradHidden, 0, _dFF));
			_fc1.Backward(_gradHidden, learningRate);
		}
	}

	public class TransformerEncoderLayer
	{
		private readonly MultiHeadAttention _selfAttention;

		private readonly FeedForwardLayer _feedForward;

		private readonly LayerNormLayer _norm1;

		private readonly LayerNormLayer _norm2;

		private readonly int _dModel;

		private readonly float[] _tempBuffer;

		private readonly float[] _normOutput;

		private readonly float[] _ffnOutput;

		private readonly float[] _residualInput;

		private readonly float[] _gradTemp;

		private readonly float[] _gradNorm;

		private readonly float[] _gradFFN;

		private readonly float[] _gradResidual;

		public MultiHeadAttention SelfAttention => _selfAttention;

		public FeedForwardLayer FeedForward => _feedForward;

		public LayerNormLayer Norm1 => _norm1;

		public LayerNormLayer Norm2 => _norm2;

		public float[] InputGradients => _gradResidual;

		public TransformerEncoderLayer(int dModel, int nHead, int dFF = 256)
		{
			_dModel = dModel;
			_selfAttention = new MultiHeadAttention(dModel, nHead);
			_feedForward = new FeedForwardLayer(dModel, dFF);
			_norm1 = new LayerNormLayer(dModel);
			_norm2 = new LayerNormLayer(dModel);
			_tempBuffer = new float[dModel];
			_normOutput = new float[dModel];
			_ffnOutput = new float[dModel];
			_residualInput = new float[dModel];
			_gradTemp = new float[dModel];
			_gradNorm = new float[dModel];
			_gradFFN = new float[dModel];
			_gradResidual = new float[dModel];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Forward(ReadOnlySpan<float> input, Span<float> output)
		{
			input.CopyTo(_residualInput);
			_norm1.Forward(input, _normOutput);
			_feedForward.Forward(_normOutput, _ffnOutput);
			for (int i = 0; i < _dModel; i++)
			{
				output[i] = _ffnOutput[i] + input[i];
			}
			_norm2.Forward(output, _tempBuffer);
			_tempBuffer.AsSpan().CopyTo(output);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Backward(ReadOnlySpan<float> gradOutput, float learningRate = 0.001f)
		{
			_norm2.Backward(gradOutput, learningRate);
			float[] inputGrads = _norm2.InputGrads;
			inputGrads.AsSpan().CopyTo(_gradFFN.AsSpan());
			inputGrads.AsSpan().CopyTo(_gradResidual.AsSpan());
			_feedForward.Backward(_gradFFN, learningRate);
			float[] inputGradients = _feedForward.InputGradients;
			_norm1.Backward(inputGradients, learningRate);
			float[] inputGrads2 = _norm1.InputGrads;
			VectorMathNew.Add(_gradResidual.AsSpan(0, _dModel), inputGrads2, _gradResidual.AsSpan(0, _dModel));
			_norm2.ApplyGradients(learningRate);
			_norm1.ApplyGradients(learningRate);
		}

		public void ApplyGradients(float learningRate = 0.001f)
		{
			_feedForward.FC1.ApplyGradientsSGD(learningRate);
			_feedForward.FC2.ApplyGradientsSGD(learningRate);
			_norm1.ApplyGradients(learningRate);
			_norm2.ApplyGradients(learningRate);
		}
	}

	public class CoreTransformerEncoder
	{
		private const int D_MODEL = 64;

		private const int D_FF = 128;

		private const int HEAD_COUNT = 4;

		private const float GRADIENT_CLIP_THRESHOLD = 1f;

		private readonly MultiHeadAttention _selfAttention;

		private readonly FeedForwardLayer _feedForward;

		private readonly LayerNormLayer _norm1;

		private readonly LayerNormLayer _norm2;

		private readonly float[] _selfAttnOutput;

		private readonly float[] _norm1Output;

		private readonly float[] _ffnOutput;

		private readonly float[] _residual1;

		private readonly float[] _residual2;

		private readonly float[] _gradFFN;

		private readonly float[] _gradSelfAttn;

		private readonly float[] _gradNorm1Input;

		private readonly float[] _gradResidual;

		private readonly float[] _gradInput;

		public MultiHeadAttention SelfAttention => _selfAttention;

		public FeedForwardLayer FeedForward => _feedForward;

		public LayerNormLayer Norm1 => _norm1;

		public LayerNormLayer Norm2 => _norm2;

		public float[] InputGradients => _gradInput;

		public CoreTransformerEncoder()
		{
			_selfAttention = new MultiHeadAttention(64, 4);
			_feedForward = new FeedForwardLayer(64, 128);
			_norm1 = new LayerNormLayer(64);
			_norm2 = new LayerNormLayer(64);
			_selfAttnOutput = new float[64];
			_norm1Output = new float[64];
			_ffnOutput = new float[64];
			_residual1 = new float[64];
			_residual2 = new float[64];
			_gradFFN = new float[64];
			_gradSelfAttn = new float[64];
			_gradNorm1Input = new float[64];
			_gradResidual = new float[64];
			_gradInput = new float[64];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Forward(ReadOnlySpan<float> input, Span<float> output)
		{
			VectorMathNew.Copy(input, _residual1);
			_selfAttention.SelfForward(input, _selfAttnOutput);
			VectorMathNew.Add(input, _selfAttnOutput, _norm1Output);
			_norm1.Forward(_norm1Output, _norm1Output);
			VectorMathNew.Copy(_norm1Output, _residual2);
			_feedForward.Forward(_norm1Output, _ffnOutput);
			VectorMathNew.Add(_residual2, _ffnOutput, output);
			_norm2.Forward(output, output);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Backward(ReadOnlySpan<float> gradOutput, float learningRate = 0.001f)
		{
			if (VectorMathNew.HasInvalidValues(gradOutput))
			{
				return;
			}
			_norm2.Backward(gradOutput, learningRate);
			float[] inputGrads = _norm2.InputGrads;
			VectorMathNew.Copy(inputGrads, _gradFFN);
			VectorMathNew.Copy(inputGrads, _gradResidual);
			_feedForward.Backward(_gradFFN, learningRate);
			float[] inputGradients = _feedForward.InputGradients;
			VectorMathNew.Add(_gradResidual, inputGradients, _gradResidual);
			_norm1.Backward(_gradResidual, learningRate);
			float[] inputGrads2 = _norm1.InputGrads;
			VectorMathNew.Copy(inputGrads2, _gradSelfAttn);
			ClipGradientIfNeeded(_gradSelfAttn);
			_selfAttention.SelfBackward(_gradSelfAttn, learningRate);
			float[] selfInputGradients = _selfAttention.GetSelfInputGradients();
			VectorMathNew.Add(inputGrads2, selfInputGradients, _gradInput);
			_norm2.ApplyGradients(learningRate);
			_norm1.ApplyGradients(learningRate);
		}

		public void ApplyGradients(float learningRate = 0.001f)
		{
			if (!AreWeightsValid())
			{
				return;
			}
			_selfAttention.ApplyGradients(learningRate);
			_feedForward.FC1.ApplyGradientsSGD(learningRate);
			_feedForward.FC2.ApplyGradientsSGD(learningRate);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void ClipGradientIfNeeded(Span<float> grad)
		{
			float num = VectorMathNew.EuclideanNorm(grad);
			if (num > 1f)
			{
				float scalar = 1f / num;
				VectorMathNew.MultiplyScalarInPlace(grad, scalar);
			}
		}

		private bool AreWeightsValid()
		{
			if (!IsWeightsValid(_selfAttention.Wq.Weights))
			{
				return false;
			}
			if (!IsWeightsValid(_selfAttention.Wk.Weights))
			{
				return false;
			}
			if (!IsWeightsValid(_selfAttention.Wv.Weights))
			{
				return false;
			}
			if (!IsWeightsValid(_selfAttention.Wo.Weights))
			{
				return false;
			}
			if (!IsWeightsValid(_feedForward.FC1.Weights))
			{
				return false;
			}
			if (!IsWeightsValid(_feedForward.FC2.Weights))
			{
				return false;
			}
			return true;
		}

		private bool IsWeightsValid(float[] weights)
		{
			return !VectorMathNew.HasInvalidValues(weights.AsSpan());
		}
	}

	public class ThreadTransformerEncoder
	{
		private const int D_MODEL = 64;

		private const int D_FF = 128;

		private const int HEAD_COUNT = 4;

		private const float GRADIENT_CLIP_THRESHOLD = 1f;

		private readonly MultiHeadAttention _selfAttention;

		private readonly FeedForwardLayer _feedForward;

		private readonly LayerNormLayer _norm1;

		private readonly LayerNormLayer _norm2;

		private readonly float[] _selfAttnOutput;

		private readonly float[] _norm1Output;

		private readonly float[] _ffnOutput;

		private readonly float[] _residual1;

		private readonly float[] _residual2;

		private readonly float[] _gradFFN;

		private readonly float[] _gradSelfAttn;

		private readonly float[] _gradNorm1Input;

		private readonly float[] _gradResidual;

		private readonly float[] _gradInput;

		public MultiHeadAttention SelfAttention => _selfAttention;

		public FeedForwardLayer FeedForward => _feedForward;

		public LayerNormLayer Norm1 => _norm1;

		public LayerNormLayer Norm2 => _norm2;

		public float[] InputGradients => _gradInput;

		public ThreadTransformerEncoder()
		{
			_selfAttention = new MultiHeadAttention(64, 4);
			_feedForward = new FeedForwardLayer(64, 128);
			_norm1 = new LayerNormLayer(64);
			_norm2 = new LayerNormLayer(64);
			_selfAttnOutput = new float[64];
			_norm1Output = new float[64];
			_ffnOutput = new float[64];
			_residual1 = new float[64];
			_residual2 = new float[64];
			_gradFFN = new float[64];
			_gradSelfAttn = new float[64];
			_gradNorm1Input = new float[64];
			_gradResidual = new float[64];
			_gradInput = new float[64];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Forward(ReadOnlySpan<float> input, Span<float> output)
		{
			VectorMathNew.Copy(input, _residual1);
			_selfAttention.SelfForward(input, _selfAttnOutput);
			VectorMathNew.Add(input, _selfAttnOutput, _norm1Output);
			_norm1.Forward(_norm1Output, _norm1Output);
			VectorMathNew.Copy(_norm1Output, _residual2);
			_feedForward.Forward(_norm1Output, _ffnOutput);
			VectorMathNew.Add(_residual2, _ffnOutput, output);
			_norm2.Forward(output, output);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Backward(ReadOnlySpan<float> gradOutput, float learningRate = 0.001f)
		{
			if (VectorMathNew.HasInvalidValues(gradOutput))
			{
				return;
			}
			_norm2.Backward(gradOutput, learningRate);
			float[] inputGrads = _norm2.InputGrads;
			VectorMathNew.Copy(inputGrads, _gradFFN);
			VectorMathNew.Copy(inputGrads, _gradResidual);
			_feedForward.Backward(_gradFFN, learningRate);
			float[] inputGradients = _feedForward.InputGradients;
			VectorMathNew.Add(_gradResidual, inputGradients, _gradResidual);
			_norm1.Backward(_gradResidual, learningRate);
			float[] inputGrads2 = _norm1.InputGrads;
			VectorMathNew.Copy(inputGrads2, _gradSelfAttn);
			ClipGradientIfNeeded(_gradSelfAttn);
			_selfAttention.SelfBackward(_gradSelfAttn, learningRate);
			float[] selfInputGradients = _selfAttention.GetSelfInputGradients();
			VectorMathNew.Add(inputGrads2, selfInputGradients, _gradInput);
			_norm2.ApplyGradients(learningRate);
			_norm1.ApplyGradients(learningRate);
		}

		public void ApplyGradients(float learningRate = 0.001f)
		{
			if (!AreWeightsValid())
			{
				return;
			}
			_selfAttention.ApplyGradients(learningRate);
			_feedForward.FC1.ApplyGradientsSGD(learningRate);
			_feedForward.FC2.ApplyGradientsSGD(learningRate);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void ClipGradientIfNeeded(Span<float> grad)
		{
			float num = VectorMathNew.EuclideanNorm(grad);
			if (num > 1f)
			{
				float scalar = 1f / num;
				VectorMathNew.MultiplyScalarInPlace(grad, scalar);
			}
		}

		private bool AreWeightsValid()
		{
			if (!IsWeightsValid(_selfAttention.Wq.Weights))
			{
				return false;
			}
			if (!IsWeightsValid(_selfAttention.Wk.Weights))
			{
				return false;
			}
			if (!IsWeightsValid(_selfAttention.Wv.Weights))
			{
				return false;
			}
			if (!IsWeightsValid(_selfAttention.Wo.Weights))
			{
				return false;
			}
			if (!IsWeightsValid(_feedForward.FC1.Weights))
			{
				return false;
			}
			if (!IsWeightsValid(_feedForward.FC2.Weights))
			{
				return false;
			}
			return true;
		}

		private bool IsWeightsValid(float[] weights)
		{
			return !VectorMathNew.HasInvalidValues(weights.AsSpan());
		}
	}
}
