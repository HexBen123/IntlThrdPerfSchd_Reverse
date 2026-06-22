using System;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace IntlThrdPerfSchd
{
	// Token: 0x02000002 RID: 2
	[RunInstaller(true)]
	public class ProjectInstaller : Installer
	{
		// Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
		public ProjectInstaller()
		{
			this.InitializeComponent();
		}

		// Token: 0x06000002 RID: 2 RVA: 0x0000205E File Offset: 0x0000025E
		private void serviceInstaller1_AfterInstall(object sender, InstallEventArgs e)
		{
		}

		// Token: 0x06000003 RID: 3 RVA: 0x00002060 File Offset: 0x00000260
		protected override void Dispose(bool disposing)
		{
			if (disposing && this.components != null)
			{
				this.components.Dispose();
			}
			base.Dispose(disposing);
		}

		// Token: 0x06000004 RID: 4 RVA: 0x00002080 File Offset: 0x00000280
		private void InitializeComponent()
		{
			this.serviceProcessInstaller1 = new ServiceProcessInstaller();
			this.serviceInstaller1 = new ServiceInstaller();
			this.serviceProcessInstaller1.Account = ServiceAccount.LocalSystem;
			this.serviceProcessInstaller1.Password = null;
			this.serviceProcessInstaller1.Username = null;
			this.serviceInstaller1.Description = "基于线程的调度器";
			this.serviceInstaller1.DisplayName = "IntlThrdSchd";
			this.serviceInstaller1.ServiceName = "IntlThrdSchd";
			this.serviceInstaller1.StartType = ServiceStartMode.Automatic;
			this.serviceInstaller1.AfterInstall += this.serviceInstaller1_AfterInstall;
			base.Installers.AddRange(new Installer[] { this.serviceProcessInstaller1, this.serviceInstaller1 });
		}

		// Token: 0x04000001 RID: 1
		private IContainer components;

		// Token: 0x04000002 RID: 2
		private ServiceProcessInstaller serviceProcessInstaller1;

		// Token: 0x04000003 RID: 3
		private ServiceInstaller serviceInstaller1;
	}
}
