using System.Runtime.CompilerServices;
using System;

namespace IntlThrdPerfSchd
{

internal static class MathCompat
{
	public static float Clamp(float value, float min, float max)
	{
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}
}

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
		float norm = 0f;
		for (int i = 0; i < grad.Length; i++)
		{
			norm += grad[i] * grad[i];
		}
		norm = (float)Math.Sqrt(norm);
		if (norm > maxNorm)
		{
			float scale = maxNorm / norm;
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