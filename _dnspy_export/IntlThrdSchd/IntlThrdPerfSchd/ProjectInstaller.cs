using System;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace IntlThrdPerfSchd
{
	// Token: 0x02000008 RID: 8
	[RunInstaller(true)]
	public class ProjectInstaller : Installer
	{
		// Token: 0x06000070 RID: 112 RVA: 0x00005180 File Offset: 0x00003380
		public ProjectInstaller()
		{
			this.InitializeComponent();
		}

		// Token: 0x06000071 RID: 113 RVA: 0x0000518E File Offset: 0x0000338E
		private void serviceInstaller1_AfterInstall(object sender, InstallEventArgs e)
		{
		}

		// Token: 0x06000072 RID: 114 RVA: 0x00005190 File Offset: 0x00003390
		private void serviceInstaller2_AfterInstall(object sender, InstallEventArgs e)
		{
		}

		// Token: 0x06000073 RID: 115 RVA: 0x00005192 File Offset: 0x00003392
		protected override void Dispose(bool disposing)
		{
			if (disposing && this.components != null)
			{
				this.components.Dispose();
			}
			base.Dispose(disposing);
		}

		// Token: 0x06000074 RID: 116 RVA: 0x000051B4 File Offset: 0x000033B4
		private void InitializeComponent()
		{
			this.serviceProcessInstaller1 = new ServiceProcessInstaller();
			this.serviceInstaller1 = new ServiceInstaller();
			this.serviceProcessInstaller1.Account = ServiceAccount.LocalSystem;
			this.serviceProcessInstaller1.Password = null;
			this.serviceProcessInstaller1.Username = null;
			this.serviceInstaller1.DelayedAutoStart = true;
			this.serviceInstaller1.Description = "基于线程的调度器";
			this.serviceInstaller1.DisplayName = "IntlThrdSchd";
			this.serviceInstaller1.ServiceName = "IntlThrdSchd";
			this.serviceInstaller1.StartType = ServiceStartMode.Automatic;
			this.serviceInstaller1.AfterInstall += this.serviceInstaller1_AfterInstall;
			base.Installers.AddRange(new Installer[] { this.serviceProcessInstaller1, this.serviceInstaller1 });
		}

		// Token: 0x0400006F RID: 111
		private IContainer components;

		// Token: 0x04000070 RID: 112
		private ServiceProcessInstaller serviceProcessInstaller1;

		// Token: 0x04000071 RID: 113
		private ServiceInstaller serviceInstaller1;
	}
}
