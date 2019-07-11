
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Core;
using Windows.System;
using Windows.System.Profile;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Autofac;
using Common;
using Latest_Chatty_8.Managers;
using Latest_Chatty_8.Networking;
using Latest_Chatty_8.Settings;
using Latest_Chatty_8.Views;
using Microsoft.HockeyApp;
using Microsoft.Services.Store.Engagement;
using Tasks;
using AuthenticationManager = Common.AuthenticationManager;

// The Split App template is documented at http://go.microsoft.com/fwlink/?LinkId=234228

namespace Latest_Chatty_8
{
	/// <summary>
	/// Provides application-specific behavior to supplement the default Application class.
	/// </summary>
	sealed partial class App
	{
		private AuthenticationManager _authManager;
		private LatestChattySettings _settings;
		private ChattyManager _chattyManager;
		private CloudSyncManager _cloudSyncManager;
		private MessageManager _messageManager;
		private INotificationManager _notificationManager;
		private NetworkConnectionStatus _networkConnectionStatus;
		private AvailableTagsManager _availableTagsManager;
		private IContainer _container;

		/// <summary>
		/// Initializes the singleton Application object.  This is the first line of authored code
		/// executed, and as such is the logical equivalent of main() or WinMain().
		/// </summary>
		public App()
		{
			//var apiKey = HockeyAppHelpers.GetAPIKey().Result;
			//if (!apiKey.Equals("REPLACEME"))
			//{
			//	HockeyClient.Current.Configure(apiKey);
			//}
			InitializeComponent();

			//This enables the notification queue on the tile so we can cycle replies.
			TileUpdateManager.CreateTileUpdaterForApplication().EnableNotificationQueue(true);

			//Disable mouse mode.  Will require a lot of extra stuff.
			//this.RequiresPointerMode = Windows.UI.Xaml.ApplicationRequiresPointerMode.WhenRequested;

			Suspending += OnSuspending;
			Resuming += OnResuming;
			//DebugSettings.BindingFailed += DebugSettings_BindingFailedAsync;
			//DebugSettings.IsTextPerformanceVisualizationEnabled = true;
		}

		//private async void DebugSettings_BindingFailedAsync(object sender, BindingFailedEventArgs e)
		//{
		//	await new MessageDialog(e.Message).ShowAsync();
		//}

		//private async Task<bool> IsInternetAvailableAsync()
		//{
		//	//Ping the API with a light request to make sure Internets works.
		//	var req = HttpWebRequest.CreateHttp(Locations.GetNewestEventId);

		//	try
		//	{
		//		using (var res = await req.GetResponseAsync())
		//		{
		//			req.Abort();
		//		}
		//		return true;
		//	}
		//	catch (Exception)
		//	{
		//		req.Abort();
		//		return false;
		//	}
		//}
		
		/// <summary>
		/// Invoked when the application is launched normally by the end user.  Other entry points
		/// will be used when the application is launched to open a specific file, to display
		/// search results, and so forth.
		/// </summary>
		/// <param name="args">Details about the launch request and process.</param>
		protected async override void OnLaunched(LaunchActivatedEventArgs args)
		{
			Debug.WriteLine("OnLaunched...");
			//App.Current.UnhandledException += OnUnhandledException;

			if (_container == null)
			{
				AppModuleBuilder builder = new AppModuleBuilder();
				_container = builder.BuildContainer();
				_authManager = _container.Resolve<AuthenticationManager>();
				_chattyManager = _container.Resolve<ChattyManager>();
				_settings = _container.Resolve<LatestChattySettings>();
				_cloudSyncManager = _container.Resolve<CloudSyncManager>();
				_messageManager = _container.Resolve<MessageManager>();
				_notificationManager = _container.Resolve<INotificationManager>();
				_networkConnectionStatus = _container.Resolve<NetworkConnectionStatus>();
				_availableTagsManager = _container.Resolve<AvailableTagsManager>();
			}

			var shell = Window.Current.Content as Shell;

			if (shell == null || shell.Content == null)
			{
				shell = CreateNewShell();
			}

			if (_chattyManager.ShouldFullRefresh())
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

			if (IsXbox())
			{
				//Draw to screen bounds in Xbox One
				ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseCoreWindow);
			}

			await RegisterBackgroundTasks();

			await _networkConnectionStatus.WaitForNetworkConnection();//Make sure we're connected to the interwebs before proceeding.

			//Loading this stuff after activating the window shouldn't be a problem, things will just appear as necessary.
			//await _availableTagsManager.Initialize();
			await _authManager.Initialize();
			Debug.WriteLine("Completed login.");
			await _cloudSyncManager.Initialize();
			Debug.WriteLine("Done initializing cloud sync.");
			_messageManager.Start();
			_chattyManager.StartAutoChattyRefresh();

			if (!string.IsNullOrWhiteSpace(args.Arguments))
			{
				//"goToPost?postId=34793445"
				if (args.Arguments.StartsWith("goToPost?postId="))
				{
					var postId = int.Parse(args.Arguments.Replace("goToPost?postId=", ""));
					shell.NavigateToPage(typeof(SingleThreadView), new Tuple<IContainer, int, int>(_container, postId, postId));
				}
			}

			await _notificationManager.ReRegisterForNotifications();
			await MaybeShowRating();
			await MaybeShowMercury();
			SetUpLiveTile();

		}

		private static bool IsXbox()
		{
			/* According to https://msdn.microsoft.com/en-us/library/windows/apps/windows.system.profile.analyticsversioninfo.devicefamily.aspx
			 * AnalyticsInfo...DeviceFamily shouldn't be used because it could change over time.
			 * However, according to https://github.com/Microsoft/AppDevXbox and other Microsoft blogs, this is exactly what they're doing.
			 * So with that said, we're going to go with it.  It seems to be the best way to determine whether or not Xbox specific code is needed.
			 */
			return AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox";
		}

		private static async Task RegisterBackgroundTasks()
		{
			await RegisterNotificationReplyTask();
			await RegisterUnreadNotificationTask();
		}

		private static async Task RegisterNotificationReplyTask()
		{
			try
			{
				var _ = await BackgroundExecutionManager.RequestAccessAsync();
				var backgroundTaskName = nameof(NotificationBackgroundTaskHandler);
				var bgTask = BackgroundTaskRegistration.AllTasks.Values.FirstOrDefault(t => t.Name.Equals(backgroundTaskName));
				if (bgTask != null)
				{
					bgTask.Unregister(true);
				}
				if (!BackgroundTaskRegistration.AllTasks.Any(i => i.Value.Name.Equals(backgroundTaskName)))
				{
					var backgroundBuilder = new BackgroundTaskBuilder
					{
						Name = backgroundTaskName,
						TaskEntryPoint = typeof(NotificationBackgroundTaskHandler).FullName
					};
					backgroundBuilder.SetTrigger(new ToastNotificationActionTrigger());
					var unused = backgroundBuilder.Register();
				}
			}
			catch
			{
				//There seem to be exceptions in this method on Xbox One.
				// We don't need it because Xbox One doesn't support toast notifications at this point
				// so we don't need the background agent to handle replies.
				// We'll just swallow the exception on Xbox. Otherwise we're going to throw so it gets caught and reported.
				if (!IsXbox()) throw;
			}
		}

		private static async Task RegisterUnreadNotificationTask()
		{
			try
			{
				var _ = await BackgroundExecutionManager.RequestAccessAsync();
				var backgroundTaskName = nameof(UnreadMessageNotifier);
				var bgTask = BackgroundTaskRegistration.AllTasks.Values.FirstOrDefault(t => t.Name.Equals(backgroundTaskName));
				if (bgTask != null)
				{
					bgTask.Unregister(true);
				}
				if (!BackgroundTaskRegistration.AllTasks.Any(i => i.Value.Name.Equals(backgroundTaskName)))
				{
					var backgroundBuilder = new BackgroundTaskBuilder
					{
						Name = backgroundTaskName,
						TaskEntryPoint = typeof(UnreadMessageNotifier).FullName
					};
					backgroundBuilder.SetTrigger(new TimeTrigger(30, false));
					var unused = backgroundBuilder.Register();
				}
			}
			catch
			{
				//There seem to be exceptions in this method on Xbox One.
				// We don't need it because Xbox One doesn't support toast notifications at this point
				// so we don't need the background agent to handle replies.
				// We'll just swallow the exception on Xbox. Otherwise we're going to throw so it gets caught and reported.
				if (!IsXbox()) throw;
			}
		}

		private Shell CreateNewShell()
		{
			var rootFrame = new Frame();
#if !DEBUG
			//If this is the first time they've installed the app, don't show update info.
			if (_settings.IsUpdateInfoAvailable && !_settings.LocalFirstRun)
			{
				rootFrame.Navigate(typeof(Help), new Tuple<IContainer, bool>(_container, true));
			}
			else
			{
#endif
				rootFrame.Navigate(typeof(InlineChatty), _container);
#if !DEBUG
			}
#endif
			_settings.LocalFirstRun = false;
			return new Shell(rootFrame, _container);
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
				Debug.WriteLine("Suspending - Timeout in {0}ms", (e.SuspendingOperation.Deadline.Ticks - DateTime.Now.Ticks) / TimeSpan.TicksPerMillisecond);
				_chattyManager.StopAutoChattyRefresh();
				await _cloudSyncManager.Suspend();
				_messageManager.Stop();
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
			if (_chattyManager.ShouldFullRefresh())
			{
				//Reset the navigation stack and return to the main page because we're going to refresh everything
				var shell = Window.Current.Content as Shell;
				while (shell != null && shell.CanGoBack)
				{
					shell.GoBack();
				}
			}
			await _networkConnectionStatus.WaitForNetworkConnection(); //Make sure we're connected to the interwebs before proceeding.
			await _authManager.Initialize();
			await _cloudSyncManager.Initialize();
			_messageManager.Start();
			_chattyManager.StartAutoChattyRefresh();
			SetUpLiveTile();
			//timer.Stop();
		}

		private async Task MaybeShowMercury()
		{
			if ((_settings.LaunchCount >= 20 && !_settings.SeenMercuryBlast)) //|| System.Diagnostics.Debugger.IsAttached)
			{
				_settings.SeenMercuryBlast = true;
				CoreApplication.MainView.CoreWindow.Activate();
				var tc = HockeyClient.Current;
				var dialog = new MessageDialog("Shacknews depends on revenue from advertisements. While this app is free, shacknews gets no revenue from it's usage. We urge you to help support shacknews by subscribing to their Mercury service.", "Would you like to support shacknews?");

				dialog.Commands.Add(new UICommand("Yes!", async a =>
				{
					tc.TrackEvent("AcceptedMercuryInfo");
					var d2 = new MessageDialog("Clicking next will take you to the shacknews settings page. You must be logged in to your account on the site. From there, click on the 'Mercury' link and fill out the form.", "Instructions");
					d2.Commands.Add(new UICommand("Next", async b =>
					{
						await Launcher.LaunchUriAsync(new Uri(@"https://www.shacknews.com/settings"));
					}));
					await d2.ShowAsync();
				}));

				dialog.Commands.Add(new UICommand("No Thanks", a =>
				{
					tc.TrackEvent("DeclienedMercury");
				}));

				await dialog.ShowAsync();
			}
		}

		private async Task MaybeShowRating()
		{
			_settings.LaunchCount++;
			if (_settings.LaunchCount == 3)// || System.Diagnostics.Debugger.IsAttached)
			{
				CoreApplication.MainView.CoreWindow.Activate();
				var tc = HockeyClient.Current;
				var dialog = new MessageDialog("Would you kindly rate this app?", "Rate this thang!");

				dialog.Commands.Add(new UICommand("Yes!", async a =>
				{
					tc.TrackEvent("AcceptedRating");
					await Launcher.LaunchUriAsync(new Uri("ms-windows-store://review/?ProductId=9WZDNCRDKLBD"));
				}));

				dialog.Commands.Add(new UICommand("Nope :(", async a =>
				{
					tc.TrackEvent("DeclinedRating");

					var feedbackDialog = new MessageDialog("Would you like to provide feedback so we can make the app better?", "Last question, promise!");
					feedbackDialog.Commands.Add(new UICommand("Yes", async b =>
					{
						tc.TrackEvent("AcceptedFeedback");
						if (StoreServicesFeedbackLauncher.IsSupported())
						{
							var launcher = StoreServicesFeedbackLauncher.GetDefault();
							await launcher.LaunchAsync();
						}
						else
						{
							var assemblyName = new AssemblyName(typeof(App).GetTypeInfo().Assembly.FullName);
							await Launcher.LaunchUriAsync(new Uri(string.Format("mailto:support@bit-shift.com?subject=Feedback for {0} v{1}", assemblyName.Name, assemblyName.Version.ToString())));
						}
					}));

					feedbackDialog.Commands.Add(new UICommand("No. Seriously, leave me alone!", b =>
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
