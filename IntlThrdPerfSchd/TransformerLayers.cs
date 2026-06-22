using SimdLibrary;
using System;
using System.Runtime.CompilerServices;

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
		VectorMath.Add(a, b, result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Scale(Span<float> data, float scale)
	{
		VectorMath.MultiplyScalar(data, scale, data);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float DotProduct(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
	{
		int length = Math.Min(a.Length, b.Length);
		return VectorMath.DotProduct(a.Slice(0, length), b.Slice(0, length));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Softmax(ReadOnlySpan<float> input, Span<float> output)
	{
		float max = VectorMath.Max(input);
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
		float mean = VectorMath.Mean(input);
		float variance = VectorMath.Variance(input);
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
		VectorMath.ComputeWeightGrad(gradOutput, input, weightGrad, outDim, inDim);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ComputeInputGrad(ReadOnlySpan<float> gradOutput, ReadOnlySpan<float> weights, Span<float> inputGrad, int outDim, int inDim)
	{
		VectorMath.ComputeInputGrad(gradOutput, weights, inputGrad, outDim, inDim);
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
		Array.Clear(Bias, 0, outDim);
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
		MathHelper.ComputeWeightGrad(gradOutput, _cachedInput, WeightGrads, OutDim, InDim);
		if (accumulateGrad)
		{
			for (int j = 0; j < OutDim; j++)
			{
				BiasGrads[j] += gradOutput[j];
			}
		}
		else
		{
			for (int i = 0; i < OutDim; i++)
			{
				BiasGrads[i] = gradOutput[i];
			}
		}
		MathHelper.ComputeInputGrad(gradOutput, Weights, InputGrads, OutDim, InDim);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ApplyGradients(float learningRate = 0.001f, float beta1 = 0.9f, float beta2 = 0.999f, float epsilon = 1E-08f, int t = 1)
	{
		int count = OutDim * InDim;
		for (int i = 0; i < count; i++)
		{
			float grad = WeightGrads[i];
			WeightMom[i] = beta1 * WeightMom[i] + (1f - beta1) * grad;
			float num = beta2 * WeightMom[i] + (1f - beta2) * grad * grad;
			float mHat = WeightMom[i] / (1f - (float)Math.Pow(beta1, t));
			float vHat = num / (1f - (float)Math.Pow(beta2, t));
			Weights[i] -= learningRate * mHat / ((float)Math.Sqrt(vHat) + epsilon);
		}
		for (int j = 0; j < OutDim; j++)
		{
			float grad2 = BiasGrads[j];
			BiasMom[j] = beta1 * BiasMom[j] + (1f - beta1) * grad2;
			float num2 = beta2 * BiasMom[j] + (1f - beta2) * grad2 * grad2;
			float mHat2 = BiasMom[j] / (1f - (float)Math.Pow(beta1, t));
			float vHat2 = num2 / (1f - (float)Math.Pow(beta2, t));
			Bias[j] -= learningRate * mHat2 / ((float)Math.Sqrt(vHat2) + epsilon);
		}
		Array.Clear(WeightGrads, 0, count);
		Array.Clear(BiasGrads, 0, OutDim);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ApplyGradientsSGD(float learningRate = 0.001f)
	{
		int count = OutDim * InDim;
		for (int i = 0; i < count; i++)
		{
			Weights[i] -= learningRate * WeightGrads[i];
		}
		for (int j = 0; j < OutDim; j++)
		{
			Bias[j] -= learningRate * BiasGrads[j];
		}
		Array.Clear(WeightGrads, 0, count);
		Array.Clear(BiasGrads, 0, OutDim);
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

	private float[] _tempBuffer;

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
		_tempBuffer = new float[dim];
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
			float num = _cachedInvStd[0];
			ReadOnlySpan<float> gradOutputSpan = gradOutput.Slice(0, d);
			ReadOnlySpan<float> xNormSpan = new ReadOnlySpan<float>(_cachedXNorm, 0, d);
			Span<float> gammaGradSpan = new Span<float>(GammaGrads, 0, d);
			Span<float> betaGradSpan = new Span<float>(BetaGrads, 0, d);
			MathHelper.MultiplyElementwise(gradOutputSpan, xNormSpan, gammaGradSpan);
			gradOutputSpan.CopyTo(betaGradSpan);
			MathHelper.MultiplyElementwise(gradOutputSpan, new ReadOnlySpan<float>(Gamma, 0, d), new Span<float>(_tempBuffer, 0, d));
			float sum1 = MathHelper.Sum(new ReadOnlySpan<float>(_tempBuffer, 0, d));
			MathHelper.MultiplyElementwise(gradOutputSpan, new ReadOnlySpan<float>(Gamma, 0, d), new Span<float>(_tempBuffer, 0, d));
			MathHelper.MultiplyElementwise(new ReadOnlySpan<float>(_tempBuffer, 0, d), xNormSpan, new Span<float>(_tempBuffer, 0, d));
			float sum2 = MathHelper.Sum(new ReadOnlySpan<float>(_tempBuffer, 0, d));
			float factor = num / (float)d;
			for (int i = 0; i < d; i++)
			{
				InputGrads[i] = factor * ((float)d * gradOutputSpan[i] - sum1 - xNormSpan[i] * sum2);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ApplyGradients(float learningRate = 0.001f)
	{
		for (int i = 0; i < Dim; i++)
		{
			Gamma[i] -= learningRate * GammaGrads[i];
			Beta[i] -= learningRate * BetaGrads[i];
		}
		Array.Clear(GammaGrads, 0, Dim);
		Array.Clear(BetaGrads, 0, Dim);
	}
}

public class MultiHeadAttention
{
	private const int MAX_CORES = 64;

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
			for (int m = 0; m < numCores; m++)
			{
				float avgWeight = 0f;
				for (int n = 0; n < _nHead; n++)
				{
					avgWeight += _attentionWeights[n * 64 + m];
				}
				attentionWeights[m] = avgWeight / (float)_nHead;
			}
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
		Wo.Backward(gradOutput, learningRate);
		float[] gradConcat = Wo.InputGrads;
		for (int h = 0; h < _nHead; h++)
		{
			int headOffset = h * _dV;
			int scoreOffset = h * 64;
			for (int c = 0; c < _cachedNumCores; c++)
			{
				float grad = 0f;
				int vOffset = headOffset;
				for (int d = 0; d < _dV; d++)
				{
					grad += gradConcat[vOffset + d] * _cachedValues[c][vOffset + d];
				}
				_gradAttentionWeights[scoreOffset + c] = grad;
			}
			ReadOnlySpan<float> a = new ReadOnlySpan<float>(_attentionWeights, scoreOffset, _cachedNumCores);
			Span<float> gradAttnWeightsSpan = new Span<float>(_gradAttentionWeights, scoreOffset, _cachedNumCores);
			Span<float> gradScoresSpan = new Span<float>(_gradAttentionScores, scoreOffset, _cachedNumCores);
			MathHelper.MultiplyElementwise(a, gradAttnWeightsSpan, gradScoresSpan);
			float sumProbGrad = MathHelper.Sum(gradScoresSpan);
			for (int i = 0; i < _cachedNumCores; i++)
			{
				float prob = _attentionWeights[scoreOffset + i];
				_gradAttentionScores[scoreOffset + i] = prob * (_gradAttentionScores[scoreOffset + i] - sumProbGrad);
			}
			float scale = 1f / (float)Math.Sqrt(_dK);
			for (int j = 0; j < _dK; j++)
			{
				float grad2 = 0f;
				for (int k = 0; k < _cachedNumCores; k++)
				{
					grad2 += _gradAttentionScores[scoreOffset + k] * _cachedKeys[k][headOffset + j] * scale;
				}
				_gradQProj[headOffset + j] = grad2;
			}
		}
		Wq.Backward(_gradQProj, learningRate);
	}

	public void ApplyGradients(float learningRate = 0.001f)
	{
		Wo.ApplyGradientsSGD(learningRate);
		Wq.ApplyGradientsSGD(learningRate);
	}

	public float[] GetQueryGradients()
	{
		return Wq.InputGrads;
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
		for (int i = 0; i < _dFF; i++)
		{
			_gradHidden[i] = ((_hidden[i] > 0f) ? gradHiddenAfterRelu[i] : 0f);
		}
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
		float[] gradAfterNorm2 = _norm2.InputGrads;
		for (int i = 0; i < _dModel; i++)
		{
			_gradFFN[i] = gradAfterNorm2[i];
			_gradResidual[i] = gradAfterNorm2[i];
		}
		_feedForward.Backward(_gradFFN, learningRate);
		float[] gradNormOutput = _feedForward.InputGradients;
		_norm1.Backward(gradNormOutput, learningRate);
		float[] gradNorm1Input = _norm1.InputGrads;
		for (int j = 0; j < _dModel; j++)
		{
			_gradResidual[j] += gradNorm1Input[j];
		}
		_norm2.ApplyGradients(learningRate);
		_norm1.ApplyGradients(learningRate);
		_selfAttention.ApplyGradients(learningRate);
	}

	public void ApplyGradients(float learningRate = 0.001f)
	{
		_feedForward.FC1.ApplyGradientsSGD(learningRate);
		_feedForward.FC2.ApplyGradientsSGD(learningRate);
		_norm1.ApplyGradients(learningRate);
		_norm2.ApplyGradients(learningRate);
	}
}
}
