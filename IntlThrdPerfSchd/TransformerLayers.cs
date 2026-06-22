using SimdLibrary;
using System.Runtime.CompilerServices;
using System;

namespace IntlThrdPerfSchd
{

public static class MathHelper
{
	public const int SIMD_SIZE = 8;

	private static readonly Random _rng = new Random(42);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void InitWeights(float[] weights, int fanIn, int fanOut)
	{
		float scale = (float)Math.Sqrt(2.0 / (double)(fanIn + fanOut));
		for (int i = 0; i < weights.Length; i++)
		{
			weights[i] = (float)(_rng.NextDouble() * 2.0 - 1.0) * scale;
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
		float max = VectorMathNew.Max(input);
		float sum = 0f;
		for (int i = 0; i < input.Length; i++)
		{
			output[i] = (float)Math.Exp(input[i] - max);
			sum += output[i];
		}
		float invSum = 1f / sum;
		Scale(output, invSum);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void LayerNorm(ReadOnlySpan<float> input, ReadOnlySpan<float> gamma, ReadOnlySpan<float> beta, Span<float> output, float epsilon = 1E-05f)
	{
		int d = input.Length;
		float mean = VectorMathNew.Mean(input);
		float variance = VectorMathNew.Variance(input);
		float invStd = 1f / (float)Math.Sqrt(variance + epsilon);
		for (int i = 0; i < d; i++)
		{
			output[i] = (input[i] - mean) * invStd * gamma[i] + beta[i];
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Linear(ReadOnlySpan<float> input, ReadOnlySpan<float> weights, ReadOnlySpan<float> bias, Span<float> result, int inDim, int outDim)
	{
		MatrixOperations.MatrixVectorMultiply(weights, input, result, outDim, inDim);
		for (int j = 0; j < outDim; j++)
		{
			result[j] += bias[j];
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ComputeWeightGrad(ReadOnlySpan<float> gradOutput, ReadOnlySpan<float> input, Span<float> weightGrad, int outDim, int inDim)
	{
		for (int j = 0; j < outDim; j++)
		{
			float go = gradOutput[j];
			int rowStart = j * inDim;
			for (int i = 0; i < inDim; i++)
			{
				weightGrad[rowStart + i] = go * input[i];
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ComputeInputGrad(ReadOnlySpan<float> gradOutput, ReadOnlySpan<float> weights, Span<float> inputGrad, int outDim, int inDim)
	{
		for (int i = 0; i < inDim; i++)
		{
			float sum = 0f;
			for (int j = 0; j < outDim; j++)
			{
				sum += gradOutput[j] * weights[j * inDim + i];
			}
			inputGrad[i] = sum;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void MultiplyElementwise(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> result)
	{
		int length = Math.Min(Math.Min(a.Length, b.Length), result.Length);
		for (int i = 0; i < length; i++)
		{
			result[i] = a[i] * b[i];
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float Sum(ReadOnlySpan<float> array)
	{
		float sum = 0f;
		for (int i = 0; i < array.Length; i++)
		{
			sum += array[i];
		}
		return sum;
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
		_inputCached = false;
		MathHelper.InitWeights(Weights, inDim, outDim);
		MathHelper.Clear(Bias.AsSpan(0, outDim));
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
		if (_inputCached)
		{
			MathHelper.ComputeWeightGrad(gradOutput, _cachedInput, WeightGrads, OutDim, InDim);
			if (accumulateGrad)
			{
				VectorMathNew.Add(BiasGrads.AsSpan(0, OutDim), gradOutput, BiasGrads.AsSpan(0, OutDim));
			}
			else
			{
				gradOutput.CopyTo(BiasGrads.AsSpan(0, OutDim));
			}
			MathHelper.ComputeInputGrad(gradOutput, Weights, InputGrads, OutDim, InDim);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ApplyGradients(float learningRate = 0.001f, float beta1 = 0.9f, float beta2 = 0.999f, float epsilon = 1E-08f, int t = 1)
	{
		int count = OutDim * InDim;
		for (int i = 0; i < count; i++)
		{
			float grad = WeightGrads[i];
			WeightMom[i] = beta1 * WeightMom[i] + (1f - beta1) * grad;
			float num = beta2 * 0f + (1f - beta2) * grad * grad;
			float mHat = WeightMom[i] / (1f - (float)Math.Pow(beta1, t));
			float vHat = num / (1f - (float)Math.Pow(beta2, t));
			Weights[i] -= learningRate * mHat / ((float)Math.Sqrt(vHat) + epsilon);
		}
		for (int j = 0; j < OutDim; j++)
		{
			float grad2 = BiasGrads[j];
			BiasMom[j] = beta1 * BiasMom[j] + (1f - beta1) * grad2;
			float num2 = beta2 * 0f + (1f - beta2) * grad2 * grad2;
			float mHat2 = BiasMom[j] / (1f - (float)Math.Pow(beta1, t));
			float vHat2 = num2 / (1f - (float)Math.Pow(beta2, t));
			Bias[j] -= learningRate * mHat2 / ((float)Math.Sqrt(vHat2) + epsilon);
		}
		MathHelper.Clear(WeightGrads.AsSpan(0, count));
		MathHelper.Clear(BiasGrads.AsSpan(0, OutDim));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ApplyGradientsSGD(float learningRate = 0.001f)
	{
		int count = OutDim * InDim;
		Span<float> weightGradSlice = WeightGrads.AsSpan(0, count);
		Span<float> weightSlice = Weights.AsSpan(0, count);
		VectorMathNew.MultiplyScalarInPlace(weightGradSlice, 0f - learningRate);
		VectorMathNew.Add(weightSlice, weightGradSlice, weightSlice);
		VectorMathNew.MultiplyScalarInPlace(BiasGrads.AsSpan(0, OutDim), 0f - learningRate);
		VectorMathNew.Add(Bias.AsSpan(0, OutDim), BiasGrads.AsSpan(0, OutDim), Bias.AsSpan(0, OutDim));
		MathHelper.Clear(WeightGrads.AsSpan(0, count));
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
		int d = input.Length;
		float mean = 0f;
		for (int i = 0; i < d; i++)
		{
			mean += input[i];
		}
		mean /= (float)d;
		_cachedMean[0] = mean;
		float var = 0f;
		for (int j = 0; j < d; j++)
		{
			float diff = input[j] - mean;
			var += diff * diff;
		}
		var /= (float)d;
		float invStd = 1f / (float)Math.Sqrt(var + 1E-05f);
		_cachedInvStd[0] = invStd;
		input.CopyTo(_cachedInput);
		for (int k = 0; k < d; k++)
		{
			_cachedXNorm[k] = (input[k] - mean) * invStd;
			output[k] = _cachedXNorm[k] * Gamma[k] + Beta[k];
		}
		_cached = true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Backward(ReadOnlySpan<float> gradOutput, float learningRate = 0.001f)
	{
		if (_cached)
		{
			int d = Dim;
			_ = _cachedMean[0];
			float invStd = _cachedInvStd[0];
			Span<float> gammaGradSpan = new Span<float>(GammaGrads, 0, d);
			Span<float> betaGradSpan = new Span<float>(BetaGrads, 0, d);
			MathHelper.MultiplyElementwise(gradOutput.Slice(0, d), new ReadOnlySpan<float>(_cachedXNorm, 0, d), gammaGradSpan);
			gradOutput.Slice(0, d).CopyTo(betaGradSpan);
			Span<float> temp = stackalloc float[d];
			MathHelper.MultiplyElementwise(gradOutput.Slice(0, d), new ReadOnlySpan<float>(Gamma, 0, d), temp);
			MathHelper.Sum(temp);
			MathHelper.MultiplyElementwise(gradOutput.Slice(0, d), new ReadOnlySpan<float>(Gamma, 0, d), temp);
			VectorMathNew.ComputeLayerNormInputGrad(xNorm: new ReadOnlySpan<float>(_cachedXNorm, 0, d), gradOutput: gradOutput.Slice(0, d), gamma: new ReadOnlySpan<float>(Gamma, 0, d), invStd: invStd, result: InputGrads);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ApplyGradients(float learningRate = 0.001f)
	{
		VectorMathNew.MultiplyScalarInPlace(GammaGrads.AsSpan(0, Dim), 0f - learningRate);
		VectorMathNew.Add(Gamma.AsSpan(0, Dim), GammaGrads.AsSpan(0, Dim), Gamma.AsSpan(0, Dim));
		VectorMathNew.MultiplyScalarInPlace(BetaGrads.AsSpan(0, Dim), 0f - learningRate);
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
		float[][] weights = new float[_nHead][];
		for (int h = 0; h < _nHead; h++)
		{
			weights[h] = new float[numCores];
			for (int c = 0; c < numCores; c++)
			{
				weights[h][c] = _attentionWeights[h * 64 + c];
			}
		}
		return weights;
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
		for (int c = 0; c < numCores; c++)
		{
			Array.Copy(keys[c], _cachedKeys[c], _dModel);
			Array.Copy(values[c], _cachedValues[c], _dModel);
		}
		_cachedNumCores = numCores;
		Wq.Forward(query, _qProj);
		Array.Copy(_qProj, _cachedQProj, _dModel);
		for (int h = 0; h < _nHead; h++)
		{
			int headOffset = h * _dK;
			for (int i = 0; i < numCores; i++)
			{
				float score = 0f;
				for (int d = 0; d < _dK; d++)
				{
					score += _qProj[headOffset + d] * keys[i][headOffset + d];
				}
				_attentionScores[h * 64 + i] = score / (float)Math.Sqrt(_dK);
			}
			int scoreOffset = h * 64;
			MathHelper.Softmax(new ReadOnlySpan<float>(_attentionScores, scoreOffset, numCores), new Span<float>(_attentionWeights, scoreOffset, numCores));
			for (int j = 0; j < _dV; j++)
			{
				float sum = 0f;
				for (int k = 0; k < numCores; k++)
				{
					sum += _attentionWeights[scoreOffset + k] * values[k][h * _dV + j];
				}
				_headOutputs[h * _dV + j] = sum;
			}
		}
		for (int l = 0; l < _nHead; l++)
		{
			Buffer.BlockCopy(_headOutputs, l * _dV * 4, _concatHeads, l * _dV * 4, _dV * 4);
		}
		Wo.Forward(_concatHeads, output);
		if (!attentionWeights.IsEmpty)
		{
			Span<float> attentionWeightsSlice = attentionWeights.Slice(0, numCores);
			VectorMathNew.Zero(attentionWeightsSlice);
			for (int m = 0; m < _nHead; m++)
			{
				int scoreOffset2 = m * 64;
				Span<float> headWeights = _attentionWeights.AsSpan(scoreOffset2, numCores);
				VectorMathNew.Add(attentionWeightsSlice, headWeights, attentionWeightsSlice);
			}
			VectorMathNew.MultiplyScalarInPlace(attentionWeightsSlice, 1f / (float)_nHead);
		}
		_cached = true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Backward(ReadOnlySpan<float> gradOutput, float learningRate = 0.001f)
	{
		if (!_cached || VectorMathNew.HasInvalidValues(gradOutput))
		{
			return;
		}
		int copyLen = Math.Min(gradOutput.Length, _dModel);
		for (int i = 0; i < copyLen; i++)
		{
			_gradConcatHeads[i] = gradOutput[i];
		}
		float norm = VectorMathNew.EuclideanNorm(_gradConcatHeads.AsSpan(0, copyLen));
		if (norm > 1f)
		{
			float scale = 1f / norm;
			VectorMathNew.MultiplyScalarInPlace(_gradConcatHeads.AsSpan(0, copyLen), scale);
		}
		Array.Clear(_gradKeys, 0, _dModel * 64);
		Array.Clear(_gradValues, 0, _dModel * 64);
		Wo.Backward(_gradConcatHeads.AsSpan(0, _dModel), learningRate);
		float[] gradConcat = Wo.InputGrads;
		for (int h = 0; h < _nHead; h++)
		{
			int headOffset = h * _dV;
			int scoreOffset = h * 64;
			ReadOnlySpan<float> gradConcatSlice = new ReadOnlySpan<float>(gradConcat, headOffset, _dV);
			Span<float> gradAttentionSlice = new Span<float>(_gradAttentionWeights, scoreOffset, _cachedNumCores);
			VectorMathNew.BatchVectorsDotVector(_cachedValues, gradConcatSlice, gradAttentionSlice, _cachedNumCores, _dV);
			float sumProbGrad = VectorMathNew.DotProduct(new ReadOnlySpan<float>(_attentionWeights, scoreOffset, _cachedNumCores), gradAttentionSlice);
			for (int c = 0; c < _cachedNumCores; c++)
			{
				float prob = _attentionWeights[scoreOffset + c];
				_gradAttentionScores[scoreOffset + c] = prob * (_gradAttentionWeights[scoreOffset + c] - sumProbGrad);
			}
			float scale2 = 1f / (float)Math.Sqrt(_dK);
			VectorMathNew.BatchWeightedDotProduct(new ReadOnlySpan<float>(_gradAttentionScores, scoreOffset, _cachedNumCores), _cachedKeys, _gradQProj.AsSpan(headOffset, _dK), _cachedNumCores, _dK);
			VectorMathNew.MultiplyScalarInPlace(_gradQProj.AsSpan(headOffset, _dK), scale2);
			for (int j = 0; j < _cachedNumCores; j++)
			{
				float gs = _gradAttentionScores[scoreOffset + j] * scale2;
				int keyOffset = j * _dModel;
				for (int d = 0; d < _dK; d++)
				{
					_gradKeys[keyOffset + headOffset + d] += gs * _cachedQuery[headOffset + d];
				}
			}
			for (int k = 0; k < _cachedNumCores; k++)
			{
				float attnWeight = _attentionWeights[scoreOffset + k];
				int valueOffset = k * _dModel;
				int gradOffset = headOffset;
				for (int l = 0; l < _dV; l++)
				{
					_gradValues[valueOffset + gradOffset + l] += attnWeight * gradConcat[gradOffset + l];
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
		for (int c = 0; c < 1; c++)
		{
			Array.Copy(_kProj, _cachedKeys[c], _dModel);
			Array.Copy(_vProj, _cachedValues[c], _dModel);
		}
		_cachedNumCores = 1;
		for (int h = 0; h < _nHead; h++)
		{
			int headOffset = h * _dK;
			Span<float> span = _qProj.AsSpan(headOffset, _dK);
			float score = VectorMathNew.DotProduct(right: _kProj.AsSpan(headOffset, _dK), left: span) / (float)Math.Sqrt(_dK);
			_attentionScores[h * 64] = score;
			_attentionWeights[h * 64] = 1f;
			Span<float> span2 = _vProj.AsSpan(headOffset, _dV);
			VectorMathNew.Copy(dest: _headOutputs.AsSpan(h * _dV, _dV), source: span2);
		}
		for (int i = 0; i < _nHead; i++)
		{
			Buffer.BlockCopy(_headOutputs, i * _dV * 4, _concatHeads, i * _dV * 4, _dV * 4);
		}
		Wo.Forward(_concatHeads, output);
		_cached = true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SelfBackward(ReadOnlySpan<float> gradOutput, float learningRate = 0.001f)
	{
		if (!_cached || VectorMathNew.HasInvalidValues(gradOutput))
		{
			return;
		}
		int copyLen = Math.Min(gradOutput.Length, _dModel);
		for (int i = 0; i < copyLen; i++)
		{
			_gradConcatHeads[i] = gradOutput[i];
		}
		float norm = VectorMathNew.EuclideanNorm(_gradConcatHeads.AsSpan(0, copyLen));
		if (norm > 1f)
		{
			float scale = 1f / norm;
			VectorMathNew.MultiplyScalarInPlace(_gradConcatHeads.AsSpan(0, copyLen), scale);
		}
		Array.Clear(_gradKeys, 0, _dModel);
		Array.Clear(_gradValues, 0, _dModel);
		Wo.Backward(_gradConcatHeads.AsSpan(0, _dModel), learningRate);
		float[] gradConcat = Wo.InputGrads;
		for (int h = 0; h < _nHead; h++)
		{
			int headOffset = h * _dV;
			int scoreOffset = h * 64;
			VectorMathNew.BatchVectorsDotVector(b: new ReadOnlySpan<float>(gradConcat, headOffset, _dV), result: new Span<float>(_gradAttentionWeights, scoreOffset, 1), a: _cachedValues, numItems: 1, dim: _dV);
			float sumGrad = 0f;
			for (int d = 0; d < _dV; d++)
			{
				sumGrad += gradConcat[headOffset + d];
			}
			_gradAttentionScores[scoreOffset] = sumGrad;
			float scale2 = 1f / (float)Math.Sqrt(_dK);
			float gs = _gradAttentionScores[scoreOffset] * scale2;
			for (int j = 0; j < _dK; j++)
			{
				_gradQProj[headOffset + j] = gs * _cachedKeys[0][headOffset + j];
			}
			for (int k = 0; k < _dK; k++)
			{
				_gradKeys[headOffset + k] += gs * _cachedQuery[headOffset + k];
			}
			float attnWeight = _attentionWeights[scoreOffset];
			for (int l = 0; l < _dV; l++)
			{
				_gradValues[headOffset + l] += attnWeight * gradConcat[headOffset + l];
			}
		}
		Wq.Backward(_gradQProj, learningRate);
		Wk.Backward(_gradQProj, learningRate);
		Wv.Backward(_gradQProj, learningRate);
	}

	public float[] GetSelfInputGradients()
	{
		float[] gradQ = Wq.InputGrads;
		float[] gradK = Wk.InputGrads;
		float[] gradV = Wv.InputGrads;
		for (int i = 0; i < _dModel; i++)
		{
			_gradQProj[i] = gradQ[i] + gradK[i] + gradV[i];
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
		float[] gradHiddenAfterRelu = _fc2.InputGrads;
		VectorMathNew.ReluGradient(new ReadOnlySpan<float>(_hidden, 0, _dFF), new ReadOnlySpan<float>(gradHiddenAfterRelu, 0, _dFF), new Span<float>(_gradHidden, 0, _dFF));
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
		float[] gradNormOutput = _feedForward.InputGradients;
		_norm1.Backward(gradNormOutput, learningRate);
		float[] gradInput = _norm1.InputGrads;
		VectorMathNew.Add(_gradResidual.AsSpan(0, _dModel), gradInput, _gradResidual.AsSpan(0, _dModel));
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
		if (!VectorMathNew.HasInvalidValues(gradOutput))
		{
			_norm2.Backward(gradOutput, learningRate);
			float[] inputGrads = _norm2.InputGrads;
			VectorMathNew.Copy(inputGrads, _gradFFN);
			VectorMathNew.Copy(inputGrads, _gradResidual);
			_feedForward.Backward(_gradFFN, learningRate);
			float[] gradNorm1Output = _feedForward.InputGradients;
			_norm1.Backward(gradNorm1Output, learningRate);
			float[] gradSelfAttn = _norm1.InputGrads;
			VectorMathNew.Add(_gradResidual, gradSelfAttn, _gradResidual);
			ClipGradientIfNeeded(_gradResidual);
			_selfAttention.SelfBackward(_gradResidual, learningRate);
			VectorMathNew.Copy(_selfAttention.GetSelfInputGradients(), _gradInput);
			_norm2.ApplyGradients(learningRate);
			_norm1.ApplyGradients(learningRate);
		}
	}

	public void ApplyGradients(float learningRate = 0.001f)
	{
		if (AreWeightsValid())
		{
			_selfAttention.ApplyGradients(learningRate);
			_feedForward.FC1.ApplyGradientsSGD(learningRate);
			_feedForward.FC2.ApplyGradientsSGD(learningRate);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void ClipGradientIfNeeded(Span<float> grad)
	{
		float norm = VectorMathNew.EuclideanNorm(grad);
		if (norm > 1f)
		{
			float scale = 1f / norm;
			VectorMathNew.MultiplyScalarInPlace(grad, scale);
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

	private const int HEAD_COUNT = 2;

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

	public float[] InputGradients => _gradInput;

	public ThreadTransformerEncoder()
	{
		_selfAttention = new MultiHeadAttention(64, 2);
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
		if (!VectorMathNew.HasInvalidValues(gradOutput))
		{
			_norm2.Backward(gradOutput, learningRate);
			float[] inputGrads = _norm2.InputGrads;
			VectorMathNew.Copy(inputGrads, _gradFFN);
			VectorMathNew.Copy(inputGrads, _gradResidual);
			_feedForward.Backward(_gradFFN, learningRate);
			float[] gradNorm1Output = _feedForward.InputGradients;
			_norm1.Backward(gradNorm1Output, learningRate);
			float[] gradSelfAttn = _norm1.InputGrads;
			VectorMathNew.Add(_gradResidual, gradSelfAttn, _gradResidual);
			ClipGradientIfNeeded(_gradResidual);
			_selfAttention.SelfBackward(_gradResidual, learningRate);
			VectorMathNew.Copy(_selfAttention.GetSelfInputGradients(), _gradInput);
			_norm2.ApplyGradients(learningRate);
			_norm1.ApplyGradients(learningRate);
		}
	}

	public void ApplyGradients(float learningRate = 0.001f)
	{
		if (AreWeightsValid())
		{
			_selfAttention.ApplyGradients(learningRate);
			_feedForward.FC1.ApplyGradientsSGD(learningRate);
			_feedForward.FC2.ApplyGradientsSGD(learningRate);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void ClipGradientIfNeeded(Span<float> grad)
	{
		float norm = VectorMathNew.EuclideanNorm(grad);
		if (norm > 1f)
		{
			float scale = 1f / norm;
			VectorMathNew.MultiplyScalarInPlace(grad, scale);
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