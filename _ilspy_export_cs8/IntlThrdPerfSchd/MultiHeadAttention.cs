using System;
using System.Runtime.CompilerServices;
using SimdLibrary;

namespace IntlThrdPerfSchd
{
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
		public void CrossAttention(ReadOnlySpan<float> query, float[][] keys, float[][] values, Span<float> output, int numCores, Span<float> attentionWeights = default(Span<float>), float softmaxTemperature = 1f)
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
				int start = j * _dK;
				for (int k = 0; k < numCores; k++)
				{
					_attentionScores[j * 64 + k] = VectorMathNew.DotProduct(_qProj.AsSpan(start, _dK), keys[k].AsSpan(start, _dK)) / (float)Math.Sqrt(_dK);
				}
				int num = j * 64;
				if (softmaxTemperature != 1f)
				{
					float num2 = 1f / softmaxTemperature;
					for (int l = 0; l < numCores; l++)
					{
						_attentionScores[num + l] *= num2;
					}
				}
				ReadOnlySpan<float> input = new ReadOnlySpan<float>(_attentionScores, num, numCores);
				Span<float> span = new Span<float>(_attentionWeights, num, numCores);
				VectorMathNew.Softmax(input, span);
				VectorMathNew.BatchWeightedSum(span, values, _headOutputs.AsSpan(j * _dV, _dV), numCores, _dV, j * _dV);
			}
			for (int m = 0; m < _nHead; m++)
			{
				Buffer.BlockCopy(_headOutputs, m * _dV * 4, _concatHeads, m * _dV * 4, _dV * 4);
			}
			Wo.Forward(_concatHeads, output);
			if (!attentionWeights.IsEmpty)
			{
				Span<float> span2 = attentionWeights.Slice(0, numCores);
				VectorMathNew.Zero(span2);
				for (int n = 0; n < _nHead; n++)
				{
					int start2 = n * 64;
					Span<float> span3 = _attentionWeights.AsSpan(start2, numCores);
					VectorMathNew.Add(span2, span3, span2);
				}
				VectorMathNew.MultiplyScalarInPlace(span2, 1f / (float)_nHead);
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
				Span<float> span = _qProj.AsSpan(start, _dK);
				float num = VectorMathNew.DotProduct(right: _kProj.AsSpan(start, _dK), left: span) / (float)Math.Sqrt(_dK);
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
			if (!_cached || VectorMathNew.HasInvalidValues(gradOutput))
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
				VectorMathNew.BatchVectorsDotVector(b: new ReadOnlySpan<float>(inputGrads, num3, _dV), result: new Span<float>(_gradAttentionWeights, num4, 1), a: _cachedValues, numItems: 1, dim: _dV);
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
}
