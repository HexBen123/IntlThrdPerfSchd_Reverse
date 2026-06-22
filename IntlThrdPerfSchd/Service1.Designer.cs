using System.ComponentModel;

namespace IntlThrdPerfSchd
{

public partial class Service1
{
	private IContainer components;

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
		components = new Container();
		base.ServiceName = "Service1";
	}
}
}
