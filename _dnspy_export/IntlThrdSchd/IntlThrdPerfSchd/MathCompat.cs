using System;

namespace IntlThrdPerfSchd
{
	// Token: 0x02000004 RID: 4
	internal static class MathCompat
	{
		// Token: 0x06000017 RID: 23 RVA: 0x00002E79 File Offset: 0x00001079
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
}
