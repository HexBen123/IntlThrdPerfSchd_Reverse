using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace IntlThrdPerfSchd
{
	[RunInstaller(true)]
	public class ProjectInstaller : Installer
	{
		private IContainer components;

		private ServiceProcessInstaller serviceProcessInstaller1;

		private ServiceInstaller serviceInstaller1;

		public ProjectInstaller()
		{
			InitializeComponent();
		}

		private void serviceInstaller1_AfterInstall(object sender, InstallEventArgs e)
		{
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && components != null)
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void InitializeComponent()
		{
			serviceProcessInstaller1 = new ServiceProcessInstaller();
			serviceInstaller1 = new ServiceInstaller();
			serviceProcessInstaller1.Account = ServiceAccount.LocalSystem;
			serviceProcessInstaller1.Password = null;
			serviceProcessInstaller1.Username = null;
			serviceInstaller1.Description = "基于线程的调度器";
			serviceInstaller1.DisplayName = "IntlThrdSchd";
			serviceInstaller1.ServiceName = "IntlThrdSchd";
			serviceInstaller1.StartType = ServiceStartMode.Automatic;
			serviceInstaller1.AfterInstall += serviceInstaller1_AfterInstall;
			base.Installers.AddRange(new Installer[2] { serviceProcessInstaller1, serviceInstaller1 });
		}
	}
}
