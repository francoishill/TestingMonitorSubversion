﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using SharedClasses;
using Microsoft.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Interop;

namespace TestingMonitorSubversion
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		ObservableCollection<MonitoredCategory> monitoredList = new ObservableCollection<MonitoredCategory>();
		System.Windows.Forms.Timer timer;

		public MainWindow()
		{
			InitializeComponent();
			WindowMessagesInterop.InitializeClientMessages();
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
			source.AddHook(WndProc);
		}

		private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			WindowMessagesInterop.MessageTypes mt;
			WindowMessagesInterop.ClientHandleMessage(msg, wParam, lParam, out mt);
			if (mt == WindowMessagesInterop.MessageTypes.Show)
				ShowForm();
			else if (mt == WindowMessagesInterop.MessageTypes.Hide)
				this.Hide();
			else if (mt == WindowMessagesInterop.MessageTypes.Close)
				this.Close();
			return IntPtr.Zero;
		}

		private void ShowForm()
		{
			this.Show();
			this.Activate();
			if (this.WindowState == System.Windows.WindowState.Minimized)
				this.WindowState = System.Windows.WindowState.Normal;
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			trayIcon.BalloonTipClicked += delegate
			{
				ShowForm();
			};

			ObservableCollection<MonitoredDirectory> tmpList = new ObservableCollection<MonitoredDirectory>();
			tmpList.Add(new MonitoredDirectory(@"C:\Programming\Wadiso6\Wadiso6Lib"));
			tmpList.Add(new MonitoredDirectory(@"C:\Programming\GLSCore"));
			tmpList.Add(new MonitoredDirectory(@"C:\Programming\GLSCore6"));
			tmpList.Add(new MonitoredDirectory(@"C:\Programming\Sewsan4\SewsanLib"));
			tmpList.Add(new MonitoredDirectory(@"C:\Programming\Sewsan6\Sewsan6Lib"));
			tmpList.Add(new MonitoredDirectory(@"C:\Programming\Wadiso5\W5Source"));
			monitoredList.Add(new MonitoredCategory("Work", tmpList));

			tmpList = new ObservableCollection<MonitoredDirectory>();
			tmpList.Add(new MonitoredDirectory(@"C:\Users\francois\Documents\Visual Studio 2010\Projects\SharedClasses"));
			tmpList.Add(new MonitoredDirectory(@"C:\Users\francois\Documents\Visual Studio 2010\Projects\TestingSharedClasses"));
			tmpList.Add(new MonitoredDirectory(@"C:\Users\francois\Documents\Visual Studio 2010\Projects\QuickAccess"));
			tmpList.Add(new MonitoredDirectory(@"C:\Users\francois\Documents\Visual Studio 2010\Projects\TestingMonitorSubversion"));
			tmpList.Add(new MonitoredDirectory(@"C:\Users\francois\Documents\Visual Studio 2010\Projects\GenericTextFunctions"));
			monitoredList.Add(new MonitoredCategory("Personal", tmpList));

			treeViewMonitoredDirectories.ItemsSource = monitoredList;
			treeViewMonitoredDirectories.UpdateLayout();

			timer = new System.Windows.Forms.Timer();
			TimerInterval = 1;
			timer.Tick += new EventHandler(timer_Tick);
			timer.Start();
		}
		private bool InitialShortTimeChanged = false;

		public event PropertyChangedEventHandler  PropertyChanged = new PropertyChangedEventHandler(delegate { });
		public void OnPropertyChanged(string propertyName) { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }

		public int TimerInterval
		{
			get { if (timer == null) return 0; else return timer.Interval; }
			set { timer.Interval = value; OnPropertyChanged("TimerInterval"); }
		}

		private string[] FilterList_StartsWith = new string[] { "Performing status on external item", "X       ", "Status against revision" };

		private bool IsBusyChecking = false;
		private void timer_Tick(object sender, EventArgs e)
		{
			if (!InitialShortTimeChanged)
			{
				TimerInterval = (int)TimeSpan.FromHours(2).TotalMilliseconds;
				InitialShortTimeChanged = true;
			}

			CheckNow();
		}

		private void CheckNow()
		{
			if (!IsBusyChecking)
			{
				buttonCheckNow.IsEnabled = false;

				IsBusyChecking = true;
				progessBar1.Visibility = System.Windows.Visibility.Visible;
				this.UpdateLayout();
				progessBar1.UpdateLayout();

				foreach (MonitoredCategory cat in monitoredList)
					foreach (MonitoredDirectory md in cat.MonitoredDirectories)
						md.BrushType = BrushTypeEnum.Default;

				bool AnyChangesFound = false;
				foreach (MonitoredCategory cat in monitoredList)
				{
					List<string> ChangedDirectories = new List<string>();

					ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
					{
						Parallel.ForEach(
							cat.MonitoredDirectories,
							(md) =>
							{
								md.Status = "";
								/*
								SubversionCommand.Commit ? "commit -m\"" + logmessage + "\" \"" + tmpFolder + "\""
									: svnCommand == SubversionCommand.Update ? "update \"" + tmpFolder + "\""
									: svnCommand == SubversionCommand.Status ? "status --show-updates \"" + tmpFolder + "\""
									: svnCommand == SubversionCommand.StatusLocal ? "status \"" + tmpFolder + "\""
									: "";
								*/
								Process proc = Process.Start(new ProcessStartInfo(@"C:\Program Files\TortoiseSVN\bin\svn.exe", "status --show-updates \"" + md.Directory + "\"")
								{
									RedirectStandardError = true,
									RedirectStandardOutput = true,
									CreateNoWindow = true,
									UseShellExecute = false
								});
								proc.OutputDataReceived += (snder, evtargs) =>
								{
									bool MustAdd = true;
									if (string.IsNullOrWhiteSpace(evtargs.Data))
										MustAdd = false;
									else
									{
										foreach (string s in FilterList_StartsWith)
											if (evtargs.Data.StartsWith(s, StringComparison.InvariantCultureIgnoreCase))
												MustAdd = false;
									}
									if (MustAdd)
									{
										if (!ChangedDirectories.Contains(md.Directory))
										{
											AnyChangesFound = true;
											ChangedDirectories.Add(md.Directory);
										}

										string strtoadd = evtargs.Data;
										if (strtoadd.Contains(md.Directory))
											strtoadd = strtoadd.Replace(md.Directory, "...");
										md.Status += (md.Status.Length > 0 ? Environment.NewLine : "") + strtoadd;
									}
								};
								proc.ErrorDataReceived += (snder, evtargs) =>
								{
									if (!string.IsNullOrWhiteSpace(evtargs.Data))
										md.Status += (md.Status.Length > 0 ? Environment.NewLine : "") + "Error: " + evtargs.Data;
								};
								proc.BeginErrorReadLine();
								proc.BeginOutputReadLine();

								proc.WaitForExit();

								md.BrushType = string.IsNullOrWhiteSpace(md.Status) ? BrushTypeEnum.Success : BrushTypeEnum.Error;
							});
					});

					if (ChangedDirectories.Count > 0)
						trayIcon.ShowBalloonTip(
							3000,
							string.Format("Changes: {0}", cat.CategoryName),
							string.Join(Environment.NewLine, ChangedDirectories.Select(d => d.Split('\\')[d.Split('\\').Length - 1])),
							System.Windows.Forms.ToolTipIcon.Warning);
					ChangedDirectories.Clear();
					ChangedDirectories = null;
				}
				if (!AnyChangesFound)
					trayIcon.ShowBalloonTip(2000, "No changes", "No subversion changes in any category", System.Windows.Forms.ToolTipIcon.Info);

				progessBar1.Visibility = System.Windows.Visibility.Collapsed;
				buttonCheckNow.IsEnabled = true;
				IsBusyChecking = false;
			}
		}

		private void Border_PreviewMouseLeftButtonDown_1(object sender, MouseButtonEventArgs e)
		{
			this.DragMove();
		}

		private void buttonMinimize_Click(object sender, RoutedEventArgs e)
		{
			//this.WindowState = System.Windows.WindowState.Minimized;
			this.Hide();
		}

		private void buttonClose_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		private void OnNotificationAreaIconDoubleClick(object sender, MouseButtonEventArgs e)
		{
			ShowForm();
		}

		private void OnMenuItemExitClick(object sender, EventArgs e)
		{
			this.Close();
		}

		private void OnMenuItemShowClick(object sender, EventArgs e)
		{
			ShowForm();
		}

		private void buttonCheckNow_Click(object sender, RoutedEventArgs e)
		{
			CheckNow();
		}

		private void OnMenuItemCheckNowClick(object sender, EventArgs e)
		{
			CheckNow();
		}

		private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ClickCount == 2)
			{
				MonitoredDirectory md = (sender as Border).DataContext as MonitoredDirectory;
				this.WindowState = System.Windows.WindowState.Minimized;
				Process.Start("explorer", "/select, " + md.Directory);
			}
		}
	}

	public class MonitoredCategory : INotifyPropertyChanged
	{
		public string CategoryName { get; private set; }
		public ObservableCollection<MonitoredDirectory> MonitoredDirectories { get; private set; }
		private bool _isexpanded;
		public bool IsExpanded { get { return _isexpanded; } set { _isexpanded = value; OnPropertyChanged("IsExpanded"); } }

		public MonitoredCategory(string CategoryName, ObservableCollection<MonitoredDirectory> MonitoredDirectories)
		{
			this.CategoryName = CategoryName;
			this.MonitoredDirectories = MonitoredDirectories;
			this.IsExpanded = true;
		}

		public event PropertyChangedEventHandler  PropertyChanged = new PropertyChangedEventHandler(delegate { });
		public void OnPropertyChanged(string propertyName) { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }
	}

	public enum BrushTypeEnum { Default, Error, Success };
	public class MonitoredDirectory : INotifyPropertyChanged
	{
		private string _directory;
		public string Directory { get { return _directory; } private set { _directory = value; OnPropertyChanged("Directory"); } }
		private string _status;
		public string Status { get { return _status; } set { _status = value; OnPropertyChanged("Status"); } }
		private BrushTypeEnum _brushtype;
		public BrushTypeEnum BrushType { get { return _brushtype; } set { _brushtype = value; OnPropertyChanged("BrushType"); } }

		public MonitoredDirectory(string Directory)
		{
			this.Directory = Directory;
			this.BrushType = BrushTypeEnum.Success;
		}

		public event PropertyChangedEventHandler  PropertyChanged = new PropertyChangedEventHandler(delegate { });
		public void OnPropertyChanged(string propertyName) { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }
	}

	public static class MyBrushes
	{
		private static Brush _errorbrush;
		public static Brush ErrorBrush
		{
			get
			{
				if (_errorbrush == null)
					_errorbrush = new LinearGradientBrush(
						new GradientStopCollection(new GradientStop[]
						{ 
							new GradientStop(new Color(){ A = 255, R = 40, G = 0, B = 0 }, 0),
							new GradientStop(new Color(){ A = 255, R = 80, G = 0, B = 0 }, 0.75),
							new GradientStop(new Color(){ A = 255, R = 55, G = 0, B = 0 }, 1),
						}),
						new Point(0, 0),
						new Point(0, 1));
				return _errorbrush;
			}
		}

		private static Brush _successbrush;
		public static Brush SuccessBrush
		{
			get
			{
				if (_successbrush == null)
					_successbrush = new LinearGradientBrush(
						new GradientStopCollection(new GradientStop[]
						{ 
							//new GradientStop(new Color(){ A = 30, R = 0, G = 240, B = 0 }, 0),
							//new GradientStop(new Color(){ A = 30, R = 0, G = 220, B = 0 }, 0),
							//new GradientStop(new Color(){ A = 30, R = 0, G = 255, B = 0 }, 0),
							new GradientStop(new Color(){ A = 255, R = 0, G = 40, B = 0 }, 0),
							new GradientStop(new Color(){ A = 255, R = 0, G = 80, B = 0 }, 0.75),
							new GradientStop(new Color(){ A = 255, R = 0, G = 55, B = 0 }, 1),
						}),
						new Point(0, 0),
						new Point(0, 1));
				return _successbrush;
			}
		}
	}

	#region Converters
	public class BrushTypeToBrushConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (!(value is BrushTypeEnum))
				return MyBrushes.SuccessBrush;
			BrushTypeEnum bt = (BrushTypeEnum)value;
			switch (bt)
			{
				case BrushTypeEnum.Error: return MyBrushes.ErrorBrush;
				case BrushTypeEnum.Success: return MyBrushes.SuccessBrush;
				case BrushTypeEnum.Default: return Brushes.Transparent;
				default: return Brushes.Transparent;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class BrushTypeToForegroundConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (!(value is BrushTypeEnum))
				return Colors.Black;
			BrushTypeEnum bt = (BrushTypeEnum)value;
			switch (bt)
			{
				case BrushTypeEnum.Error: return Brushes.LightGray;
				case BrushTypeEnum.Success: return Brushes.LightGray;
				case BrushTypeEnum.Default: return Brushes.Black;
				default: return Brushes.Black;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
	#endregion
}
