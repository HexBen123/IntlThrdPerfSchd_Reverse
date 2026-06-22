using System;
using System.ServiceProcess;

namespace IntlThrdPerfSchd
{
	// Token: 0x02000007 RID: 7
	internal static class Program
	{
		// Token: 0x0600006F RID: 111 RVA: 0x0000516B File Offset: 0x0000336B
		private static void Main()
		{
			ServiceBase.Run(new ServiceBase[]
			{
				new Service1()
			});
		}
	}
}
