using System;
using System.ServiceProcess;

namespace IntlThrdPerfSchd
{
	// Token: 0x02000004 RID: 4
	internal static class Program
	{
		// Token: 0x06000023 RID: 35 RVA: 0x000049C0 File Offset: 0x00002BC0
		private static void Main()
		{
			ServiceBase.Run(new ServiceBase[]
			{
				new Service1()
			});
		}
	}
}
