using System;

namespace IntlThrdPerfSchd
{
	// Token: 0x0200000A RID: 10
	internal static class RandomExtensions
	{
		// Token: 0x060000AE RID: 174 RVA: 0x00007E88 File Offset: 0x00006088
		public static double NextGaussian(this Random random)
		{
			double num = 1.0 - random.NextDouble();
			double num2 = 1.0 - random.NextDouble();
			return Math.Sqrt(-2.0 * Math.Log(num)) * Math.Sin(6.283185307179586 * num2);
		}
	}
}
