using System;
using System.Runtime.CompilerServices;

namespace IntlThrdPerfSchd
{
	// Token: 0x02000005 RID: 5
	public static class GradientHelper
	{
		// Token: 0x06000018 RID: 24 RVA: 0x00002E88 File Offset: 0x00001088
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void SoftmaxCrossEntropyGradient(ReadOnlySpan<float> probs, int target, Span<float> grad)
		{
			for (int i = 0; i < probs.Length; i++)
			{
				*grad[i] = *probs[i] - ((i == target) ? 1f : 0f);
			}
		}

		// Token: 0x06000019 RID: 25 RVA: 0x00002ECC File Offset: 0x000010CC
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void ClipGradient(Span<float> grad, float maxNorm)
		{
			float num = 0f;
			for (int i = 0; i < grad.Length; i++)
			{
				num += *grad[i] * *grad[i];
			}
			num = (float)Math.Sqrt((double)num);
			if (num > maxNorm)
			{
				float num2 = maxNorm / num;
				MathHelper.Scale(grad, num2);
			}
		}

		// Token: 0x0600001A RID: 26 RVA: 0x00002F1F File Offset: 0x0000111F
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float ComputeAdvantage(float reward, float value, float nextValue, float gamma, float done)
		{
			return reward + gamma * nextValue * (1f - done) - value;
		}
	}
}
