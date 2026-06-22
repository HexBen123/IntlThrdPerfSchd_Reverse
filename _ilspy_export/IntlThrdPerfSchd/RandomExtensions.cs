using System;

namespace IntlThrdPerfSchd;

internal static class RandomExtensions
{
	public static double NextGaussian(this Random random)
	{
		double d = 1.0 - random.NextDouble();
		double num = 1.0 - random.NextDouble();
		return Math.Sqrt(-2.0 * Math.Log(d)) * Math.Sin(Math.PI * 2.0 * num);
	}
}
