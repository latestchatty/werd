
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

		/// <summary>
		/// Initializes the singleton Application object.  This is the first line of authored code
		/// executed, and as such is the logical equivalent of main() or WinMain().
		/// </summary>
		public App()
		{
			Microsoft.ApplicationInsights.WindowsAppInitializer.InitializeAsync();

			this.InitializeComponent();

			//This enables the notification queue on the tile so we can cycle replies.
			TileUpdateManager.CreateTileUpdaterForApplication().EnableNotificationQueue(true);
			this.Suspending += OnSuspending;
			this.Resuming += OnResuming;
			NetworkInformation.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;
			DebugSettings.BindingFailed += DebugSettings_BindingFailed;
		}

		async private void DebugSettings_BindingFailed(object sender, BindingFailedEventArgs e)
		{
			await new MessageDialog(e.Message).ShowAsync();
		}

		async private Task<bool> IsInternetAvailable()
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

		/// <summary>
		/// Invoked when the application is launched normally by the end user.  Other entry points
		/// will be used when the application is launched to open a specific file, to display
		/// search results, and so forth.
		/// </summary>
		/// <param name="args">Details about the launch request and process.</param>
		async protected override void OnLaunched(LaunchActivatedEventArgs args)
		{
			System.Diagnostics.Debug.WriteLine("OnLaunched...");
			//App.Current.UnhandledException += OnUnhandledException;

			AppModuleBuilder builder = new AppModuleBuilder();
			var container = builder.BuildContainer();
			this.authManager = container.Resolve<AuthenticationManager>();
			this.chattyManager = container.Resolve<ChattyManager>();
			this.settings = container.Resolve<LatestChattySettings>();
			this.cloudSyncManager = container.Resolve<CloudSyncManager>();
			this.messageManager = container.Resolve<MessageManager>();

			Frame rootFrame = Window.Current.Content as Frame;

			if (rootFrame == null)
			{
				// Create a Frame to act as the navigation context and navigate to the first page
				rootFrame = new Frame();
			}
			if (rootFrame.Content == null)
			{
				// When the navigation stack isn't restored navigate to the first page,
				// configuring the new page by passing required information as a navigation
				// parameter
				if (!rootFrame.Navigate(typeof(Chatty), container))
				{
					throw new Exception("Failed to create initial page");
				}
			}

			var shell = new Shell(rootFrame, container);
			Window.Current.Content = shell;
			//Ensure the current window is active - Must be called within 15 seconds of launching or app will be terminated.
			Window.Current.Activate();

			await this.EnsureNetworkConnection(); //Make sure we're connected to the interwebs before proceeding.

			//Loading this stuff after activating the window shouldn't be a problem, things will just appear as necessary.
			await this.authManager.Initialize();
			await this.cloudSyncManager.Initialize();
			this.messageManager.Start();
			this.chattyManager.StartAutoChattyRefresh();
		}

		//async private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
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
		async private void OnSuspending(object sender, SuspendingEventArgs e)
		{
			var deferral = e.SuspendingOperation.GetDeferral();
			System.Diagnostics.Debug.WriteLine("Suspending - Timeout in {0}ms", (e.SuspendingOperation.Deadline.Ticks - DateTime.Now.Ticks) / TimeSpan.TicksPerMillisecond);
			this.chattyManager.StopAutoChattyRefresh();
			await this.cloudSyncManager.Suspend();
			this.messageManager.Stop();
			deferral.Complete();
		}

		async private void OnResuming(object sender, object e)
		{
			await this.authManager.Initialize();
			await this.cloudSyncManager.Initialize();
			this.messageManager.Start();
			this.chattyManager.StartAutoChattyRefresh();
		}

		async void NetworkInformation_NetworkStatusChanged(object sender)
		{
			await this.EnsureNetworkConnection();
		}

		async public Task EnsureNetworkConnection()
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

		async public Task<bool> CheckNetworkStatus()
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
						await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
						{
							try
							{
								System.Diagnostics.Debug.WriteLine("Showing network error dialog.");
								var tc = new Microsoft.ApplicationInsights.TelemetryClient();
								tc.TrackEvent("LostInternetConnection");
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
	}
}
