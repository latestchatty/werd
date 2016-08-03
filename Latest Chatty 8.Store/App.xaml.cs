
using Autofac;
using Latest_Chatty_8.Common;
using Latest_Chatty_8.Settings;
using Latest_Chatty_8.Views;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Networking.Connectivity;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Reflection;
using Windows.ApplicationModel.Background;
using System.Linq;
using Latest_Chatty_8.Managers;

// The Split App template is documented at http://go.microsoft.com/fwlink/?LinkId=234228

namespace Latest_Chatty_8
{
	/// <summary>
	/// Provides application-specific behavior to supplement the default Application class.
	/// </summary>
	sealed partial class App : Application
	{
		private CancellationTokenSource networkStatusDialogToken = null;
		private AuthenticationManager authManager;
		private LatestChattySettings settings;
		private ChattyManager chattyManager;
		private CloudSyncManager cloudSyncManager;
		private MessageManager messageManager;
		private INotificationManager notificationManager;
		private IContainer container;

		/// <summary>
		/// Initializes the singleton Application object.  This is the first line of authored code
		/// executed, and as such is the logical equivalent of main() or WinMain().
		/// </summary>
		public App()
		{
			var apiKey = HockeyAppHelpers.GetAPIKey().Result;
			if (!apiKey.Equals("REPLACEME"))
			{
				Microsoft.HockeyApp.HockeyClient.Current.Configure(apiKey);
			}
			this.InitializeComponent();

			//This enables the notification queue on the tile so we can cycle replies.
			TileUpdateManager.CreateTileUpdaterForApplication().EnableNotificationQueue(true);
			this.Suspending += OnSuspending;
			this.Resuming += OnResuming;
			NetworkInformation.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;
			DebugSettings.BindingFailed += DebugSettings_BindingFailed;
			//DebugSettings.IsTextPerformanceVisualizationEnabled = true;
		}

		private async void DebugSettings_BindingFailed(object sender, BindingFailedEventArgs e)
		{
			await new MessageDialog(e.Message).ShowAsync();
		}

		private async Task<bool> IsInternetAvailable()
		{
			//Ping the API with a light request to make sure Internets works.
			var req = System.Net.HttpWebRequest.CreateHttp(Networking.Locations.GetNewestEventId);

			try
			{
				using (var res = await req.GetResponseAsync())
				{
					req.Abort();
				}
				return true;
			}
			catch (Exception)
			{
				req.Abort();
				return false;
			}
		}

		protected override void OnActivated(IActivatedEventArgs args)
		{
			base.OnActivated(args);
		}
		/// <summary>
		/// Invoked when the application is launched normally by the end user.  Other entry points
		/// will be used when the application is launched to open a specific file, to display
		/// search results, and so forth.
		/// </summary>
		/// <param name="args">Details about the launch request and process.</param>
		protected async override void OnLaunched(LaunchActivatedEventArgs args)
		{
			System.Diagnostics.Debug.WriteLine("OnLaunched...");
			//App.Current.UnhandledException += OnUnhandledException;

			if (this.container == null)
			{
				AppModuleBuilder builder = new AppModuleBuilder();
				this.container = builder.BuildContainer();
				this.authManager = this.container.Resolve<AuthenticationManager>();
				this.chattyManager = this.container.Resolve<ChattyManager>();
				this.settings = this.container.Resolve<LatestChattySettings>();
				this.cloudSyncManager = this.container.Resolve<CloudSyncManager>();
				this.messageManager = this.container.Resolve<MessageManager>();
				this.notificationManager = this.container.Resolve<INotificationManager>();
			}

			var shell = Window.Current.Content as Shell;

			if (shell == null || shell.Content == null)
			{
				shell = CreateNewShell();
			}

			if (this.chattyManager.ShouldFullRefresh())
			{
				//Reset the navigation stack and return to the main page because we're going to refresh everything
				while (shell.CanGoBack)
				{
					shell.GoBack();
				}
			}

			Window.Current.Content = shell;

			//Ensure the current window is active - Must be called within 15 seconds of launching or app will be terminated.
			Window.Current.Activate();

			await RegisterBackgroundTask();

			await this.EnsureNetworkConnection(); //Make sure we're connected to the interwebs before proceeding.

			//Loading this stuff after activating the window shouldn't be a problem, things will just appear as necessary.
			await this.authManager.Initialize();
			System.Diagnostics.Debug.WriteLine("Completed login.");
			await this.cloudSyncManager.Initialize();
			System.Diagnostics.Debug.WriteLine("Done initializing cloud sync.");
			this.messageManager.Start();
			this.chattyManager.StartAutoChattyRefresh();

			if (!string.IsNullOrWhiteSpace(args.Arguments))
			{
				//"goToPost?postId=34793445"
				if (args.Arguments.StartsWith("goToPost?postId="))
				{
					var postId = int.Parse(args.Arguments.Replace("goToPost?postId=", ""));
					shell.NavigateToPage(typeof(SingleThreadView), new Tuple<IContainer, int, int>(this.container, postId, postId));
				}
			}

			await this.notificationManager.ReRegisterForNotifications();
			await this.notificationManager.Resume();
			await this.MaybeShowRating();
			await this.MaybeShowMercury();
			this.SetUpLiveTile();

		}

		private static async Task RegisterBackgroundTask()
		{
			var backgroundAccessStatus = await BackgroundExecutionManager.RequestAccessAsync();
			var backgroundTaskName = nameof(Tasks.NotificationBackgroundTaskHandler);
			var bgTask = BackgroundTaskRegistration.AllTasks.Values.FirstOrDefault(t => t.Name.Equals(backgroundTaskName));
			if (bgTask != null)
			{
				bgTask.Unregister(true);
			}
			if (!BackgroundTaskRegistration.AllTasks.Any(i => i.Value.Name.Equals(backgroundTaskName)))
			{
				var backgroundBuilder = new BackgroundTaskBuilder()
				{
					Name = backgroundTaskName,
					TaskEntryPoint = typeof(Tasks.NotificationBackgroundTaskHandler).FullName
				};
				backgroundBuilder.SetTrigger(new ToastNotificationActionTrigger());
				var registration = backgroundBuilder.Register();
			}
		}

		private Shell CreateNewShell()
		{
			var rootFrame = new Frame();
#if !DEBUG
			//If this is the first time they've installed the app, don't show update info.
			if (this.settings.IsUpdateInfoAvailable && !this.settings.LocalFirstRun)
			{
				rootFrame.Navigate(typeof(Help), new Tuple<IContainer, bool>(this.container, true));
			}
			else
			{
#endif
			rootFrame.Navigate(typeof(Chatty), this.container);
#if !DEBUG
			}
#endif
			this.settings.LocalFirstRun = false;
			return new Shell(rootFrame, this.container);
		}

		//private async void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
		//{
		//	Window.Current.Activate();
		//	var message = new MessageDialog("We encountered a problem that we never expected! If you'd be so kind as to send us a friendly correspondence upon your return about what you were doing when this happened, we would be most greatful!", "Well that's not good.");
		//	await message.ShowAsync();
		//}

		/// <summary>
		/// Invoked when application execution is being suspended.  Application state is saved
		/// without knowing whether the application will be terminated or resumed with the contents
		/// of memory still intact.
		/// </summary>
		/// <param name="sender">The source of the suspend request.</param>
		/// <param name="e">Details about the suspend request.</param>
		private async void OnSuspending(object sender, SuspendingEventArgs e)
		{
			var deferral = e.SuspendingOperation.GetDeferral();
			try
			{
				//var timer = new TelemetryTimer("App-Suspending");
				//timer.Start();
				System.Diagnostics.Debug.WriteLine("Suspending - Timeout in {0}ms", (e.SuspendingOperation.Deadline.Ticks - DateTime.Now.Ticks) / TimeSpan.TicksPerMillisecond);
				this.chattyManager.StopAutoChattyRefresh();
				await this.cloudSyncManager.Suspend();
				this.messageManager.Stop();
				//timer.Stop();
			}
			finally
			{
				deferral.Complete();
			}
		}

		private async void OnResuming(object sender, object e)
		{
			//var timer = new TelemetryTimer("App-Resuming");
			//timer.Start();
			if (this.chattyManager.ShouldFullRefresh())
			{
				//Reset the navigation stack and return to the main page because we're going to refresh everything
				var shell = Window.Current.Content as Shell;
				while (shell.CanGoBack)
				{
					shell.GoBack();
				}
			}
			await this.EnsureNetworkConnection(); //Make sure we're connected to the interwebs before proceeding.
			await this.authManager.Initialize();
			await this.cloudSyncManager.Initialize();
			await this.notificationManager.Resume();
			this.messageManager.Start();
			this.chattyManager.StartAutoChattyRefresh();
			this.SetUpLiveTile();
			//timer.Stop();
		}

		async void NetworkInformation_NetworkStatusChanged(object sender)
		{
			await this.EnsureNetworkConnection();
		}

		public async Task EnsureNetworkConnection()
		{
			if (this.networkStatusDialogToken == null)
			{
				while (!(await this.CheckNetworkStatus()))
				{
					System.Diagnostics.Debug.WriteLine("Attempting network status detection.");
					await Task.Delay(1000);
				}
				this.networkStatusDialogToken = null;
			}
		}

		public async Task<bool> CheckNetworkStatus()
		{
			NetworkInformation.NetworkStatusChanged -= NetworkInformation_NetworkStatusChanged;
			try
			{
				var profile = Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile();
				if (profile == null)
				{
					if (this.networkStatusDialogToken == null)
					{
						this.networkStatusDialogToken = new CancellationTokenSource();
						CoreDispatcher dispatcher = null;
						if (Window.Current != null)
						{
							dispatcher = Window.Current.Dispatcher;
						}
						else
						{
							dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;
						}
						await dispatcher.RunOnUIThreadAndWait(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
						{
							try
							{
								System.Diagnostics.Debug.WriteLine("Showing network error dialog.");
								Microsoft.HockeyApp.HockeyClient.Current.TrackEvent("LostInternetConnection");
								CoreApplication.MainView.CoreWindow.Activate();
								var message = new MessageDialog("This application requires an active Internet connection.  This dialog will close automatically when the Internet connection is restored.  If it doesn't, click close to try again.", "The tubes are clogged!");
								await message.ShowAsync().AsTask(this.networkStatusDialogToken.Token);
								this.networkStatusDialogToken = null;
							}
							//Canceled - dismissed since we got the connection back.
							catch (OperationCanceledException)
							{ }
						});
					}
					return false;
				}
				else
				{
					if (this.networkStatusDialogToken != null)
					{
						this.networkStatusDialogToken.Cancel();
					}
				}
				return true;
			}
			finally
			{
				NetworkInformation.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;
			}
		}

		private async Task MaybeShowMercury()
		{
			if ((this.settings.LaunchCount >= 20 && !this.settings.SeenMercuryBlast)) //|| System.Diagnostics.Debugger.IsAttached)
			{
				this.settings.SeenMercuryBlast = true;
				CoreApplication.MainView.CoreWindow.Activate();
				var tc = Microsoft.HockeyApp.HockeyClient.Current;
				var dialog = new MessageDialog("Shacknews depends on revenue from advertisements. While this app is free, shacknews gets no revenue from it's usage. We urge you to help support shacknews by subscribing to their Mercury service.", "Would you like to support shacknews?");

				dialog.Commands.Add(new UICommand("Yes!", async (a) =>
				{
					tc.TrackEvent("AcceptedMercuryInfo");
					var d2 = new MessageDialog("Clicking next will take you to the shacknews settings page. You must be logged in to your account on the site. From there, click on the 'Mercury' link and fill out the form.", "Instructions");
					d2.Commands.Add(new UICommand("Next", async (b) =>
					{
						await Windows.System.Launcher.LaunchUriAsync(new Uri(@"https://www.shacknews.com/settings"));
					}));
					await d2.ShowAsync();
				}));

				dialog.Commands.Add(new UICommand("No Thanks", (a) =>
				{
					tc.TrackEvent("DeclienedMercury");
				}));

				await dialog.ShowAsync();
			}
		}

		private async Task MaybeShowRating()
		{
			this.settings.LaunchCount++;
			if (this.settings.LaunchCount == 3)//|| System.Diagnostics.Debugger.IsAttached)
			{
				CoreApplication.MainView.CoreWindow.Activate();
				var tc = Microsoft.HockeyApp.HockeyClient.Current;
				var dialog = new MessageDialog("Would you kindly rate this app?", "Rate this thang!");

				dialog.Commands.Add(new UICommand("Yes!", async (a) =>
				{
					tc.TrackEvent("AcceptedRating");
					await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-windows-store://review/?ProductId=9WZDNCRDKLBD"));
				}));

				dialog.Commands.Add(new UICommand("Nope :(", async (a) =>
				{
					tc.TrackEvent("DeclinedRating");

					var feedbackDialog = new MessageDialog("Would you like to provide feedback via email?", "Last question, promise!");
					feedbackDialog.Commands.Add(new UICommand("Yes", async (b) =>
					{
						tc.TrackEvent("AcceptedFeedback");
						var assemblyName = new AssemblyName(typeof(App).GetTypeInfo().Assembly.FullName);
						await Windows.System.Launcher.LaunchUriAsync(new Uri(string.Format("mailto:support@bit-shift.com?subject=Feedback for {0} v{1}", assemblyName.Name, assemblyName.Version.ToString())));
					}));

					feedbackDialog.Commands.Add(new UICommand("No. Seriously, leave me alone!", (b) =>
					{
						tc.TrackEvent("DeclinedFeedback");
					}));

					await feedbackDialog.ShowAsync();
				}));

				await dialog.ShowAsync();
			}
		}

		private void SetUpLiveTile()
		{
			var updater = TileUpdateManager.CreateTileUpdaterForApplication();
			updater.EnableNotificationQueue(false);
			updater.StartPeriodicUpdate(new Uri("https://shacknotify.bit-shift.com/tileContent"), PeriodicUpdateRecurrence.HalfHour);
		}
	}
}
