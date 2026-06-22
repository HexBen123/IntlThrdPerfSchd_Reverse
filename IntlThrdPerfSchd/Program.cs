using System.ServiceProcess;

namespace IntlThrdPerfSchd
{
	internal static class Program
	{
		private static void Main()
		{
			ServiceBase.Run(new ServiceBase[1]
			{
				new Service1()
			});
		}
	}
}
