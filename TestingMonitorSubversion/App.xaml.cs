using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

namespace TestingMonitorSubversion
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			SharedClasses.AutoUpdating.CheckForUpdates_ExceptionHandler();
			//SharedClasses.AutoUpdating.CheckForUpdates(null, null);

			base.OnStartup(e);

			TestingMonitorSubversion.MainWindow mw = new MainWindow();
			mw.ShowDialog();
		}
	}
}
