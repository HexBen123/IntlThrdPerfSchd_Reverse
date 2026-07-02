using System;
using System.Runtime.CompilerServices;

namespace IntlThrdPerfSchd
{
	public static class GradientHelper
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void SoftmaxCrossEntropyGradient(ReadOnlySpan<float> probs, int target, Span<float> grad)
		{
			for (int i = 0; i < probs.Length; i++)
			{
				grad[i] = probs[i] - ((i == target) ? 1f : 0f);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ClipGradient(Span<float> grad, float maxNorm)
		{
			float num = 0f;
			for (int i = 0; i < grad.Length; i++)
			{
				num += grad[i] * grad[i];
			}
			num = (float)Math.Sqrt(num);
			if (num > maxNorm)
			{
				float scale = maxNorm / num;
				MathHelper.Scale(grad, scale);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float ComputeAdvantage(float reward, float value, float nextValue, float gamma, float done)
		{
			return reward + gamma * nextValue * (1f - done) - value;
		}
	}
}
