using Latest_Chatty_8.Shared;
using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Shared.Settings;
using Latest_Chatty_8.Views;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.UI.ApplicationSettings;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media.Animation;
using BugSense;
using BugSense.Model;
using Windows.Networking.Connectivity;
using System.Threading;
using Windows.UI.Core;
using Windows.ApplicationModel.Core;

// The Split App template is documented at http://go.microsoft.com/fwlink/?LinkId=234228

namespace Latest_Chatty_8
{
	/// <summary>
	/// Provides application-specific behavior to supplement the default Application class.
	/// </summary>
	sealed partial class App : Application
	{
		private CancellationTokenSource networkStatusDialogToken = null;

		/// <summary>
		/// Initializes the singleton Application object.  This is the first line of authored code
		/// executed, and as such is the logical equivalent of main() or WinMain().
		/// </summary>
		public App()
		{
			this.InitializeComponent();
			BugSense.BugSenseHandler.Instance.InitAndStartSession(new ExceptionManager(Current), "w8cb9742");

			//This enables the notification queue on the tile so we can cycle replies.
			TileUpdateManager.CreateTileUpdaterForApplication().EnableNotificationQueue(true);
			this.Suspending += OnSuspending;
			this.Resuming += OnResuming;
			NetworkInformation.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;
			//Add types to the suspension manager so it can serialize them.
			SuspensionManager.KnownTypes.Add(typeof(Comment));
			SuspensionManager.KnownTypes.Add(typeof(List<Comment>));
			SuspensionManager.KnownTypes.Add(typeof(int));
			SuspensionManager.KnownTypes.Add(typeof(AuthorType));
//			SuspensionManager.KnownTypes.Add(typeof(Latest_Chatty_8.Views.ReplyNavParameter));
		}

		async private void OnResuming(object sender, object e)
		{
			await CoreServices.Instance.Resume();
		}

		async private Task<bool> IsInternetAvailable()
		{
			var req = System.Net.HttpWebRequest.CreateHttp("http://www.microsoft.com");

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
		protected async override void OnLaunched(LaunchActivatedEventArgs args)
		{
			System.Diagnostics.Debug.WriteLine("OnLaunched...");
			App.Current.UnhandledException += OnUnhandledException;

			LatestChattySettings.Instance.CreateInstance();
			Frame rootFrame = Window.Current.Content as Frame;

			if (rootFrame == null)
			{
				// Create a Frame to act as the navigation context and navigate to the first page
				rootFrame = new Frame();
				//Associate the frame with a SuspensionManager key
				SuspensionManager.RegisterFrame(rootFrame, "AppFrame");

				if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
				{
					// Restore the saved session state only when appropriate
					try
					{
						await SuspensionManager.RestoreAsync();
					}
					catch (SuspensionManagerException)
					{
						//Something went wrong restoring state.
						//Assume there is no state and continue
					}
				}
			}
			if (rootFrame.Content == null)
			{
				// When the navigation stack isn't restored navigate to the first page,
				// configuring the new page by passing required information as a navigation
				// parameter
				if (!rootFrame.Navigate(typeof(Chatty)))
				{
					throw new Exception("Failed to create initial page");
				}
			}

			var shell = new Shell(rootFrame);
			Window.Current.Content = shell;
			//Ensure the current window is active
			Window.Current.Activate();
		}

		async private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			Window.Current.Activate();
			var message = new MessageDialog("We encountered a problem that we never expected! If you'd be so kind as to send us a friendly correspondence upon your return about what you were doing when this happened, we would be most greatful!", "Well that's not good.");
			await message.ShowAsync();
		}

		/// <summary>
		/// Invoked when application execution is being suspended.  Application state is saved
		/// without knowing whether the application will be terminated or resumed with the contents
		/// of memory still intact.
		/// </summary>
		/// <param name="sender">The source of the suspend request.</param>
		/// <param name="e">Details about the suspend request.</param>
		private async void OnSuspending(object sender, SuspendingEventArgs e)
		{
			System.Diagnostics.Debug.WriteLine("OnSuspending...");
			var deferral = e.SuspendingOperation.GetDeferral();
			try
			{
				await SuspensionManager.SaveAsync();
			}
			catch { System.Diagnostics.Debug.Assert(false); }
			try
			{
				await CoreServices.Instance.Suspend();
			}
			catch (Exception)
			{
				System.Diagnostics.Debug.WriteLine("blah");
			}
			deferral.Complete();
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
								//TODO: This will probably fail.  Don't think there is a core window yet, we're just initializing the app at this point if we didn't start with a network connection.
								System.Diagnostics.Debug.WriteLine("Showing network error dialog.");
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
