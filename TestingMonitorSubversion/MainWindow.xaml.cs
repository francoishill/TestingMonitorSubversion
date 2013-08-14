using System;
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
//using Microsoft.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Interop;
using System.IO;
using System.Collections.Concurrent;
using System.Threading;

namespace TestingMonitorSubversion
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		ObservableCollection<MonitoredCategory> monitoredList = new ObservableCollection<MonitoredCategory>();
		System.Windows.Forms.Timer timer;
		//NamedPipesInterop.NamedPipeClient pipeclient;

		public MainWindow()
		{
			InitializeComponent();
			//StartPipeClient();
			//WindowMessagesInterop.InitializeClientMessages();

			/*var client = new SvnClient();
			var path = @"C:\Programming\Wadiso6\Wadiso6Lib";
			//Collection<SvnListChangeListEventArgs> list;
			//client.GetChangeList(path, out list);
			//Collection<SvnLogEventArgs> list;
			//client.GetLog(path, out list);
			Collection<SvnStatusEventArgs> list;
			client.GetStatus(path, out list);*/
		}

		/*private void StartPipeClient()
		{
			pipeclient = NamedPipesInterop.NamedPipeClient.StartNewPipeClient(
				ActionOnError: (e) => { Console.WriteLine("Error occured: " + e.GetException().Message); },
				ActionOnMessageReceived: (m) =>
				{
					if (m.MessageType == PipeMessageTypes.AcknowledgeClientRegistration)
						Console.WriteLine("Client successfully registered.");
					else
					{
						if (m.MessageType == PipeMessageTypes.Show)
							Dispatcher.BeginInvoke((Action)delegate { ShowForm(); });
						else if (m.MessageType == PipeMessageTypes.Hide)
							Dispatcher.BeginInvoke((Action)delegate { this.Hide(); });
						else if (m.MessageType == PipeMessageTypes.Close)
							Dispatcher.BeginInvoke((Action)delegate { this.Close(); });
					}
				});
			this.Closing += delegate { if (pipeclient != null) { pipeclient.ForceCancelRetryLoop = true; } };
		}*/

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
			source.AddHook(WndProc);

			var handle = new WindowInteropHelper(Application.Current.MainWindow).Handle;
			if (!Win32Api.RegisterHotKey(handle, Win32Api.Hotkey1, Win32Api.MOD_WIN, (int)System.Windows.Forms.Keys.V))
				UserMessages.ShowWarningMessage("TestingMonitorSubversion could not register hotkey WinKey + V");
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
			else if (msg == Win32Api.WM_HOTKEY)
			{
				if (wParam == new IntPtr(Win32Api.Hotkey1))
				{
					if (this.Visibility != System.Windows.Visibility.Visible)
						ShowForm();
					else
						this.Hide();
				}
			}
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

			//ObservableCollection<MonitoredDirectory> tmpList = new ObservableCollection<MonitoredDirectory>();

			//tmpList.Add(new MonitoredDirectory(@"C:\Programming\Wadiso6\Wadiso6Lib"));
			//tmpList.Add(new MonitoredDirectory(@"C:\Programming\GLSCore"));
			//tmpList.Add(new MonitoredDirectory(@"C:\Programming\GLSCore6"));
			//tmpList.Add(new MonitoredDirectory(@"C:\Programming\Sewsan4\SewsanLib"));
			//tmpList.Add(new MonitoredDirectory(@"C:\Programming\Sewsan6\Sewsan6Lib"));
			//tmpList.Add(new MonitoredDirectory(@"C:\Programming\Wadiso5\W5Source"));
			//monitoredList.Add(new MonitoredCategory("Work", tmpList));

			//Work|C:\Programming\Wadiso6\Wadiso6Lib,C:\Programming\GLSCore,C:\Programming\GLSCore6,C:\Programming\Sewsan4\SewsanLib,C:\Programming\Sewsan6\Sewsan6Lib,C:\Programming\Wadiso5\W5Source|Personal|C:\Users\francois\Documents\Visual Studio 2010\Projects\SharedClasses,C:\Users\francois\Documents\Visual Studio 2010\Projects\TestingSharedClasses,C:\Users\francois\Documents\Visual Studio 2010\Projects\QuickAccess,C:\Users\francois\Documents\Visual Studio 2010\Projects\TestingMonitorSubversion,C:\Users\francois\Documents\Visual Studio 2010\Projects\GenericTextFunctions
			GenericSettings.EnsureAllSettingsAreInitialized();
			var dict = GlobalSettings.SubversionSettings.Instance.GetGroupedMonitoredList();
			foreach (string cat in dict.Keys)
			{
				if (cat.Equals("AllInDir", StringComparison.InvariantCultureIgnoreCase))
				{
					var dir = dict[cat][0];
					if (!Directory.Exists(dir))
						UserMessages.ShowWarningMessage("Cannot use 'allindir' mode, directory not found: " + dir);
					else
					{
						ObservableCollection<MonitoredDirectory> tmpList = new ObservableCollection<MonitoredDirectory>();
						foreach (string subdir in Directory.GetDirectories(dir))
							if (DirIsValidSvnPath(subdir) || DirIsValidGitPath(subdir))
								tmpList.Add(new MonitoredDirectory(subdir));
						monitoredList.Add(new MonitoredCategory(cat, tmpList));
					}
				}
				else
				{
					ObservableCollection<MonitoredDirectory> tmpList = new ObservableCollection<MonitoredDirectory>();
					foreach (string item in dict[cat])
						tmpList.Add(new MonitoredDirectory(item));
					monitoredList.Add(new MonitoredCategory(cat, new ObservableCollection<MonitoredDirectory>(tmpList.OrderBy(md => md.Directory))));
				}
			}

			//tmpList = new ObservableCollection<MonitoredDirectory>();
			//tmpList.Add(new MonitoredDirectory(@"C:\Users\francois\Documents\Visual Studio 2010\Projects\SharedClasses"));
			//tmpList.Add(new MonitoredDirectory(@"C:\Users\francois\Documents\Visual Studio 2010\Projects\TestingSharedClasses"));
			//tmpList.Add(new MonitoredDirectory(@"C:\Users\francois\Documents\Visual Studio 2010\Projects\QuickAccess"));
			//tmpList.Add(new MonitoredDirectory(@"C:\Users\francois\Documents\Visual Studio 2010\Projects\TestingMonitorSubversion"));
			//tmpList.Add(new MonitoredDirectory(@"C:\Users\francois\Documents\Visual Studio 2010\Projects\GenericTextFunctions"));
			//monitoredList.Add(new MonitoredCategory("Personal", tmpList));

			treeViewMonitoredDirectories.ItemsSource = monitoredList;
			treeViewMonitoredDirectories.UpdateLayout();

			timer = new System.Windows.Forms.Timer();
			TimerInterval = 1;
			timer.Tick += new EventHandler(timer_Tick);
			timer.Start();
		}
		private bool InitialShortTimeChanged = false;

		private bool DirIsValidSvnPath(string dir)
		{
			if (!Directory.Exists(dir))
				return false;
			return Directory.Exists(System.IO.Path.Combine(dir, ".svn"));
		}

		private bool DirIsValidGitPath(string dir)
		{
			if (!Directory.Exists(dir))
				return false;
			return Directory.Exists(System.IO.Path.Combine(dir, ".git"));
		}

		public event PropertyChangedEventHandler PropertyChanged = new PropertyChangedEventHandler(delegate { });
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

		private bool IsSvnInstalled() { return File.Exists(TortoiseProcInterop.cSvnPath); }
		private bool IsGitInstalled() { return File.Exists(TortoiseProcInterop.cGitPath); }

		object lockObj = new object();
		ConcurrentDictionary<MonitoredCategory, int> counterForCategories = new ConcurrentDictionary<MonitoredCategory, int>();
		private void CheckNow(MonitoredDirectory checkOnlyThisDirectory = null)
		{
			labelStatus.Content = null;
			labelStatus.ToolTip = null;
			labelStatus.Foreground = Brushes.Black;

			if (!IsSvnInstalled() && !IsGitInstalled())
			{
				labelStatus.Content = "Cannot use any Svn/Git functionality, exe files missing";
				labelStatus.ToolTip = TortoiseProcInterop.cSvnPath + Environment.NewLine + TortoiseProcInterop.cGitPath;
				labelStatus.Foreground = Brushes.Red;
			}
			else if (!IsSvnInstalled())
			{
				labelStatus.Content = "Cannot use any Svn functionality, file missing: " + TortoiseProcInterop.cSvnPath;
				labelStatus.ToolTip = labelStatus.Content;
				labelStatus.Foreground = Brushes.Red;
			}
			else if (!IsGitInstalled())
			{
				labelStatus.Content = "Cannot use any Git functionality, file missing: " + TortoiseProcInterop.cGitPath;
				labelStatus.ToolTip = labelStatus.Content;
				labelStatus.Foreground = Brushes.Red;
			}
			else
			{
				labelStatus.Content = "Both Svn & Git is installed";
				labelStatus.ToolTip = TortoiseProcInterop.cSvnPath + Environment.NewLine + TortoiseProcInterop.cGitPath;
				labelStatus.Foreground = Brushes.Green;
			}

			if (!IsBusyChecking)
			{
				buttonCheckNow.IsEnabled = false;

				IsBusyChecking = true;
				counterForCategories.Clear();

				progessBar1.Visibility = System.Windows.Visibility.Visible;
				this.UpdateLayout();
				progessBar1.UpdateLayout();

				foreach (MonitoredCategory cat in monitoredList)
				{
					while (!counterForCategories.TryAdd(cat, 0)) Thread.Sleep(100);
					foreach (MonitoredDirectory md in cat.MonitoredDirectories)
						if (checkOnlyThisDirectory == null || md == checkOnlyThisDirectory)
							md.BrushType = BrushTypeEnum.Default;
				}

				List<string> ChangedDirectories = new List<string>();
				foreach (MonitoredCategory category in monitoredList)
				{
					ThreadingInterop.PerformOneArgFunctionSeperateThread<MonitoredCategory>(
						(cat) =>
						{
							int tmpint;
							while (!counterForCategories.TryGetValue(cat, out tmpint)) Thread.Sleep(100);
							cat.SubItemsSummary = string.Format(" ({0}/{1})", tmpint, cat.MonitoredDirectories.Count);
							/*Parallel.ForEach(
								cat.MonitoredDirectories,
								(md) =>*/
							foreach (var md in cat.MonitoredDirectories)
							{
								if (checkOnlyThisDirectory != null && md != checkOnlyThisDirectory)
									continue;

								md.Status = "";

								if (DirIsValidSvnPath(md.Directory))
								{
									if (!IsSvnInstalled())
										return;
								}
								else if (DirIsValidGitPath(md.Directory))
								{
									if (!IsGitInstalled())
										return;
									Environment.CurrentDirectory = md.Directory;
								}

								Process proc = new Process();
								proc.StartInfo =
									DirIsValidSvnPath(md.Directory)
									? new ProcessStartInfo(TortoiseProcInterop.cSvnPath, "status --show-updates \"" + md.Directory + "\"")
									: new ProcessStartInfo(TortoiseProcInterop.cGitPath, "status --short");
								proc.StartInfo.RedirectStandardError = true;
								proc.StartInfo.RedirectStandardOutput = true;
								proc.StartInfo.CreateNoWindow = true;
								proc.StartInfo.UseShellExecute = false;

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
											ChangedDirectories.Add(md.Directory);

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
								proc.Start();
								proc.BeginErrorReadLine();
								proc.BeginOutputReadLine();

								proc.WaitForExit();


								proc.Dispose();
								proc = null;

								md.BrushType = string.IsNullOrWhiteSpace(md.Status) ? BrushTypeEnum.Success : BrushTypeEnum.Error;

								lock (lockObj)
								{
									int outint;
									while (!counterForCategories.TryGetValue(cat, out outint)) Thread.Sleep(100);
									int outint2 = outint + 1;
									while (!counterForCategories.TryUpdate(cat, outint2, outint)) Thread.Sleep(100);
									cat.SubItemsSummary = string.Format(" ({0}/{1})", outint2, cat.MonitoredDirectories.Count);
								}
							};//);
						},
						category,
						true);
				}
				if (null == checkOnlyThisDirectory)
				{
					if (ChangedDirectories.Count > 0)
						trayIcon.ShowBalloonTip(
							3000,
							"Subversion changes",//string.Format("Changes: {0}", cat.CategoryName),
							string.Join(Environment.NewLine, ChangedDirectories.Select(d => d.Split('\\')[d.Split('\\').Length - 1])),
							System.Windows.Forms.ToolTipIcon.Warning);
					else
						trayIcon.ShowBalloonTip(2000, "No changes", "No subversion changes in any category", System.Windows.Forms.ToolTipIcon.Info);
				}
				ChangedDirectories.Clear();
				ChangedDirectories = null;

				progessBar1.Visibility = System.Windows.Visibility.Collapsed;
				buttonCheckNow.IsEnabled = true;
				IsBusyChecking = false;
			}
		}

		private void Border_PreviewMouseLeftButtonDown_1(object sender, MouseButtonEventArgs e)
		{
			if (Mouse.RightButton == MouseButtonState.Pressed)
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

		private void OnNotificationArayIconMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (this.IsVisible)
				this.Hide();
			else
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

		private void MenuItemSvnShowLogDialog_Click(object sender, EventArgs e)
		{
			MenuItem mi = sender as MenuItem;
			MonitoredDirectory md = mi.DataContext as MonitoredDirectory;
			ContextMenu cm = mi.Parent as ContextMenu;
			if (cm == null) return;
			cm.IsOpen = false;

			ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
			{
				if (DirIsValidSvnPath(md.Directory))
					TortoiseProcInterop.Subversion_StartTortoiseProc(TortoiseProcInterop.TortoiseSvnCommands.Log, md.Directory)
						.WaitForExit();
				if (DirIsValidGitPath(md.Directory))
					TortoiseProcInterop.Git_StartTortoiseProc(TortoiseProcInterop.TortoiseGitCommands.Log, md.Directory, true);
				Dispatcher.BeginInvoke((Action)delegate { CheckNow(md); });
			});
		}

		private void MenuItemSvnLoadLog_Click(object sender, EventArgs e)
		{
			MenuItem mi = sender as MenuItem;
			MonitoredDirectory md = mi.DataContext as MonitoredDirectory;
			ContextMenu cm = mi.Parent as ContextMenu;
			if (cm == null) return;
			cm.IsOpen = false;

			ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
			{
				string log = "";

				if (DirIsValidSvnPath(md.Directory))
				{
					if (!IsSvnInstalled())
						return;
				}
				else if (DirIsValidGitPath(md.Directory))
				{
					if (!IsGitInstalled())
						return;
					Environment.CurrentDirectory = md.Directory;
				}

				Process proc = Process.Start(
					DirIsValidSvnPath(md.Directory)
					? new ProcessStartInfo(TortoiseProcInterop.cSvnPath, "log -l 15 \"" + md.Directory + "\"")
					: new ProcessStartInfo(TortoiseProcInterop.cGitPath, "log -l 15")
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
						string strtoadd = evtargs.Data;
						if (strtoadd.Contains(md.Directory))
							strtoadd = strtoadd.Replace(md.Directory, "...");
						log += (log.Length > 0 ? Environment.NewLine : "") + strtoadd;
					}
				};
				proc.ErrorDataReceived += (snder, evtargs) =>
				{
					if (!string.IsNullOrWhiteSpace(evtargs.Data))
						log += (log.Length > 0 ? Environment.NewLine : "") + "Error: " + evtargs.Data;
				};
				proc.BeginErrorReadLine();
				proc.BeginOutputReadLine();

				proc.WaitForExit();
				proc.Dispose();
				proc = null;

				Dispatcher.BeginInvoke((Action)delegate
				{
					textBoxLog.Text = log;
				});
			});
		}

		private void MenuItemSvnUpdate_Click(object sender, EventArgs e)
		{
			MenuItem mi = sender as MenuItem;
			MonitoredDirectory md = mi.DataContext as MonitoredDirectory;
			ContextMenu cm = mi.Parent as ContextMenu;
			if (cm == null) return;
			cm.IsOpen = false;

			ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
			{
				if (DirIsValidSvnPath(md.Directory))
					TortoiseProcInterop.Subversion_StartTortoiseProc(TortoiseProcInterop.TortoiseSvnCommands.Update, md.Directory)
						.WaitForExit();
				if (DirIsValidGitPath(md.Directory))
					TortoiseProcInterop.Git_StartTortoiseProc(TortoiseProcInterop.TortoiseGitCommands.Pull, md.Directory, true);
				Dispatcher.BeginInvoke((Action)delegate { CheckNow(md); });
			});
		}

		private void MenuItemSvnCommit_Click(object sender, EventArgs e)
		{
			MenuItem mi = sender as MenuItem;
			MonitoredDirectory md = mi.DataContext as MonitoredDirectory;
			ContextMenu cm = mi.Parent as ContextMenu;
			if (cm == null) return;
			cm.IsOpen = false;

			ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
			{
				if (DirIsValidSvnPath(md.Directory))
					TortoiseProcInterop.Subversion_StartTortoiseProc(TortoiseProcInterop.TortoiseSvnCommands.Commit, md.Directory)
						.WaitForExit();
				if (DirIsValidGitPath(md.Directory))
					TortoiseProcInterop.Git_StartTortoiseProc(TortoiseProcInterop.TortoiseGitCommands.Commit, md.Directory, true);
				Dispatcher.BeginInvoke((Action)delegate { CheckNow(md); });
			});
		}

		private void MenuItemGitPush_Click(object sender, RoutedEventArgs e)
		{
			MenuItem mi = sender as MenuItem;
			MonitoredDirectory md = mi.DataContext as MonitoredDirectory;
			ContextMenu cm = mi.Parent as ContextMenu;
			if (cm == null) return;
			cm.IsOpen = false;

			if (DirIsValidSvnPath(md.Directory))
				return;

			ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
			{
				TortoiseProcInterop.Git_StartTortoiseProc(TortoiseProcInterop.TortoiseGitCommands.Push, md.Directory, true);
				Dispatcher.BeginInvoke((Action)delegate { CheckNow(md); });
			});
		}

		private void MenuItemCheckOnlyThisDirectoy_Click(object sender, EventArgs e)
		{
			MonitoredDirectory md = (sender as MenuItem).DataContext as MonitoredDirectory;
			CheckNow(md);
		}

		private void treeViewMonitoredDirectories_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			MonitoredDirectory md = e.NewValue as MonitoredDirectory;
			if (md == null)
				return;
			textBoxLog.Text = null;
		}

		private void OnContextMenuOpened(object sender, RoutedEventArgs e)
		{
			textBoxLog.Text = null;
		}

		private void labelAbout_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			AboutWindow2.ShowAboutWindow(new System.Collections.ObjectModel.ObservableCollection<DisplayItem>()
			{
				new DisplayItem("Author", "Francois Hill"),
				new DisplayItem("Icon(s) obtained from", null)
			});
		}
	}

	public class MonitoredCategory : INotifyPropertyChanged
	{
		public string CategoryName { get; private set; }
		public ObservableCollection<MonitoredDirectory> MonitoredDirectories { get; private set; }
		private string _subitemssummary;
		public string SubItemsSummary { get { return _subitemssummary; } set { _subitemssummary = value; OnPropertyChanged("SubItemsSummary"); } }

		private bool _isexpanded;
		public bool IsExpanded { get { return _isexpanded; } set { _isexpanded = value; OnPropertyChanged("IsExpanded"); } }
		public BrushTypeEnum _brushtype;
		public BrushTypeEnum BrushType { get { return _brushtype; } set { _brushtype = value; OnPropertyChanged("BrushType"); } }

		public MonitoredCategory(string CategoryName, ObservableCollection<MonitoredDirectory> MonitoredDirectories)
		{
			this.CategoryName = CategoryName;
			this.SubItemsSummary = "";
			this.MonitoredDirectories = MonitoredDirectories;
			this.IsExpanded = true;
			this.BrushType = BrushTypeEnum.Success;
			foreach (var mondir in MonitoredDirectories)
				mondir.PropertyChanged += (sn, ev) =>
				{
					if (ev.PropertyName.Equals("BrushType", StringComparison.InvariantCultureIgnoreCase))
					{
						this.BrushType = BrushTypeEnum.Success;
						foreach (var mdir in MonitoredDirectories)
							if (mdir.BrushType == BrushTypeEnum.Error)
								this.BrushType = BrushTypeEnum.Error;
					}
				};
		}

		public event PropertyChangedEventHandler PropertyChanged = new PropertyChangedEventHandler(delegate { });
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

		public event PropertyChangedEventHandler PropertyChanged = new PropertyChangedEventHandler(delegate { });
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
