using Latest_Chatty_8.Common;
using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Settings;
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

// The Split App template is documented at http://go.microsoft.com/fwlink/?LinkId=234228

namespace Latest_Chatty_8
{
	/// <summary>
	/// Provides application-specific behavior to supplement the default Application class.
	/// </summary>
	sealed partial class App : Application
	{
		Popup settingsPopup;
		Rect windowBounds;

		/// <summary>
		/// Occurs when a settings dialog is shown.
		/// </summary>
		public event EventHandler OnSettingsShown;
		/// <summary>
		/// Occurs when a settings dialog is dismissed.
		/// </summary>
		public event EventHandler OnSettingsDismissed;

		/// <summary>
		/// Initializes the singleton Application object.  This is the first line of authored code
		/// executed, and as such is the logical equivalent of main() or WinMain().
		/// </summary>
		public App()
		{
			this.InitializeComponent();
			//This enables the notification queue on the tile so we can cycle replies.
			TileUpdateManager.CreateTileUpdaterForApplication().EnableNotificationQueue(true);
			this.Suspending += OnSuspending;
			this.Resuming += OnResuming;
			//Add types to the suspension manager so it can serialize them.
			SuspensionManager.KnownTypes.Add(typeof(NewsStory));
			SuspensionManager.KnownTypes.Add(typeof(List<NewsStory>));
			SuspensionManager.KnownTypes.Add(typeof(Comment));
			SuspensionManager.KnownTypes.Add(typeof(List<Comment>));
			SuspensionManager.KnownTypes.Add(typeof(int));
		}

		async private void OnResuming(object sender, object e)
		{
			//Happens when resuming
			await Window.Current.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
			await CoreServices.Instance.Resume());
		}

		async protected override void OnActivated(IActivatedEventArgs args)
		{
			base.OnActivated(args);
			//Happens when resuming from suspended?
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
			catch (Exception e)
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
		protected override async void OnLaunched(LaunchActivatedEventArgs args)
		{
			App.Current.UnhandledException += OnUnhandledException;
			var profile = Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile();
			if (profile == null)
			{
				Window.Current.Activate();
				var message = new MessageDialog("This application requires an active Internet connection.  Please check your connection and launch the application again.", "The tubes are clogged!");
				await message.ShowAsync();
				Application.Current.Exit();
				return;
			}

			Window.Current.SizeChanged += OnWindowSizeChanged;
			OnWindowSizeChanged(null, null);
			LatestChattySettings.Instance.CreateInstance();
			await CoreServices.Instance.Resume();
			await CoreServices.Instance.ClearTile(true);

			SettingsPane.GetForCurrentView().CommandsRequested += SettingsRequested;
			Frame rootFrame = Window.Current.Content as Frame;

			// Do not repeat app initialization when the Window already has content,
			// just ensure that the window is active

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

				// Place the frame in the current Window
				Window.Current.Content = rootFrame;
			}
			if (rootFrame.Content == null)
			{
				// When the navigation stack isn't restored navigate to the first page,
				// configuring the new page by passing required information as a navigation
				// parameter
				if (!rootFrame.Navigate(typeof(MainPage), "AllGroups"))
				{
					throw new Exception("Failed to create initial page");
				}
			}
			// Ensure the current window is active
			Window.Current.Activate();
		}

		async private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			Window.Current.Activate();
			var message = new MessageDialog("We encountered a problem that we never expected! If you'd be so kind as to send us a friendly correspondence upon your return about what you were doing when this happened, we would be most greatful!", "Well that's not good.");
			await message.ShowAsync();
		}

		async private void SettingsRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
		{
			args.Request.ApplicationCommands.Add(new SettingsCommand("MainSettings", "Settings", (x) =>
			{
				if (this.OnSettingsShown != null)
				{
					this.OnSettingsShown(this, EventArgs.Empty);
				}

				settingsPopup = new Popup();
				settingsPopup.Closed += popup_Closed;
				Window.Current.Activated += OnWindowActivated;
				settingsPopup.IsLightDismissEnabled = true;
				settingsPopup.Width = 346;
				settingsPopup.Height = this.windowBounds.Height;

				settingsPopup.ChildTransitions = new TransitionCollection();
				settingsPopup.ChildTransitions.Add(new PaneThemeTransition()
				{
					Edge = (SettingsPane.Edge == SettingsEdgeLocation.Right) ?
							 EdgeTransitionLocation.Right :
							 EdgeTransitionLocation.Left
				});

				var settingsControl = new Latest_Chatty_8.Settings.MainSettings(LatestChattySettings.Instance);
				settingsControl.Width = settingsPopup.Width;
				settingsControl.Height = windowBounds.Height;
				settingsPopup.SetValue(Canvas.LeftProperty, windowBounds.Width - settingsPopup.Width);
				settingsPopup.SetValue(Canvas.TopProperty, 0);
				settingsPopup.Child = settingsControl;
				settingsPopup.IsOpen = true;
				settingsControl.Initialize();
			}));

			args.Request.ApplicationCommands.Add(new SettingsCommand("PrivacySettings", "Privacy and Sync", (x) =>
			{
				if (this.OnSettingsShown != null)
				{
					this.OnSettingsShown(this, EventArgs.Empty);
				}

				settingsPopup = new Popup();
				settingsPopup.Closed += popup_Closed;
				Window.Current.Activated += OnWindowActivated;
				settingsPopup.IsLightDismissEnabled = true;
				settingsPopup.Width = 346;
				settingsPopup.Height = this.windowBounds.Height;

				settingsPopup.ChildTransitions = new TransitionCollection();
				settingsPopup.ChildTransitions.Add(new PaneThemeTransition()
				{
					Edge = (SettingsPane.Edge == SettingsEdgeLocation.Right) ?
							 EdgeTransitionLocation.Right :
							 EdgeTransitionLocation.Left
				});

				var settingsControl = new Latest_Chatty_8.Settings.PrivacySettings(LatestChattySettings.Instance);
				settingsControl.Width = settingsPopup.Width;
				settingsControl.Height = windowBounds.Height;
				settingsPopup.SetValue(Canvas.LeftProperty, windowBounds.Width - settingsPopup.Width);
				settingsPopup.SetValue(Canvas.TopProperty, 0);
				settingsPopup.Child = settingsControl;
				settingsPopup.IsOpen = true;
				settingsControl.Initialize();
			}));

			args.Request.ApplicationCommands.Add(new SettingsCommand("HelpSettings", "Help", (x) =>
				{
					if (Window.Current == null) { return; }

					var frame = Window.Current.Content as Frame;

					if (frame != null)
					{
						frame.Navigate(typeof(Latest_Chatty_8.Views.Help), null);
						Window.Current.Content = frame;
						Window.Current.Activate();
					}
				}));
		}

		void popup_Closed(object sender, object e)
		{
			if (this.OnSettingsDismissed != null)
			{
				this.OnSettingsDismissed(this, EventArgs.Empty);
			}
			Window.Current.Activated -= OnWindowActivated;
		}

		void OnWindowActivated(object sender, Windows.UI.Core.WindowActivatedEventArgs e)
		{
			if (e.WindowActivationState == Windows.UI.Core.CoreWindowActivationState.Deactivated)
			{
				this.settingsPopup.IsOpen = false;
			}
		}

		void OnWindowSizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
		{
			this.windowBounds = Window.Current.Bounds;
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

		/// <summary>
		/// Invoked when the application is activated to display search results.
		/// </summary>
		/// <param name="args">Details about the activation request.</param>
		protected async override void OnSearchActivated(Windows.ApplicationModel.Activation.SearchActivatedEventArgs args)
		{
			// event in OnWindowCreated to speed up searches once the application is already running

			// If the Window isn't already using Frame navigation, insert our own Frame
			var previousContent = Window.Current.Content;
			var frame = previousContent as Frame;

			// If the app does not contain a top-level frame, it is possible that this 
			// is the initial launch of the app. Typically this method and OnLaunched 
			// in App.xaml.cs can call a common method.
			if (frame == null)
			{
				// Create a Frame to act as the navigation context and associate it with
				// a SuspensionManager key
				frame = new Frame();
				Latest_Chatty_8.Common.SuspensionManager.RegisterFrame(frame, "AppFrame");

				if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
				{
					// Restore the saved session state only when appropriate
					try
					{
						await Latest_Chatty_8.Common.SuspensionManager.RestoreAsync();
					}
					catch (Latest_Chatty_8.Common.SuspensionManagerException)
					{
						//Something went wrong restoring state.
						//Assume there is no state and continue
					}
				}
			}

			frame.Navigate(typeof(Search), args.QueryText);
			Window.Current.Content = frame;

			// Ensure the current window is active
			Window.Current.Activate();
		}
	}
}
