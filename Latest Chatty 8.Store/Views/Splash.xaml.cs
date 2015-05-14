using Latest_Chatty_8.Shared;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Networking.Connectivity;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Latest_Chatty_8.Views
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class Splash : INotifyPropertyChanged
	{
		private CancellationTokenSource networkStatusDialogToken = null;

		internal Rect splashImageRect; // Rect to store splash screen image coordinates.
		internal bool dismissed = false;	// Variable to track splash screen dismissal status.
		internal Frame rootFrame;

		private SplashScreen splash;  // Variable to hold the splash screen object.

		private string npcLoadStatus;
		public string LoadStatus
		{
			get { return npcLoadStatus; }
			set { this.SetProperty(ref this.npcLoadStatus, value); }
		}

		public Splash(SplashScreen splashscreen, bool loadState)
		{
			this.InitializeComponent();
			NetworkInformation.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;

			this.DataContext = this;

			// Listen for window resize events to reposition the extended splash screen image accordingly.
			// This is important to ensure that the extended splash screen is formatted properly in response to snapping, unsnapping, rotation, etc...
			Window.Current.SizeChanged += ExtendedSplash_OnResize;

			splash = splashscreen;

			if (splash != null)
			{
				// Retrieve the window coordinates of the splash screen image.
				splashImageRect = splash.ImageLocation;
				PositionImage();
			}


			// Create a Frame to act as the navigation context
			rootFrame = new Frame();

			// Restore the saved session state if necessary
			RestoreStateAsync(loadState);
		}

		async void RestoreStateAsync(bool loadState)
		{
			await this.EnsureNetworkConnection();

			if (loadState)
				await SuspensionManager.RestoreAsync();
			this.LoadStatus = "Lamp...";
			//await LatestChattySettings.Instance.LoadLongRunningSettings();
			await CoreServices.Instance.Initialize();
			this.LoadStatus = "Sand...";
			//:TODO: RE-enable pinned posts loading here.
			//await LatestChattySettings.Instance();
			this.LoadStatus = "Lime!";

			// Navigate to mainpage
			rootFrame.Navigate(typeof(Chatty));

			// Set extended splash info on main page
			//((MainPage)rootFrame.Content).SetExtendedSplashInfo(splashImageRect, dismissed);

			// Place the frame in the current Window
			Window.Current.Content = rootFrame;
		}

		// Position the extended splash screen image in the same location as the system splash screen image.
		void PositionImage()
		{
			this.splashImage.SetValue(Canvas.LeftProperty, splashImageRect.X);
			this.splashImage.SetValue(Canvas.TopProperty, splashImageRect.Y);
			this.splashImage.Height = splashImageRect.Height;
			this.splashImage.Width = splashImageRect.Width;
		}

		void ExtendedSplash_OnResize(Object sender, WindowSizeChangedEventArgs e)
		{
			// Safely update the extended splash screen image coordinates. This function will be fired in response to snapping, unsnapping, rotation, etc...
			if (splash != null)
			{
				// Update the coordinates of the splash screen image.
				splashImageRect = splash.ImageLocation;
				PositionImage();
			}
		}

		#region Network Status
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
		#endregion
		#region Notify Property Changed
		/// <summary>
		/// Multicast event for property change notifications.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Checks if a property already matches a desired value.  Sets the property and
		/// notifies listeners only when necessary.
		/// </summary>
		/// <typeparam name="T">Type of the property.</typeparam>
		/// <param name="storage">Reference to a property with both getter and setter.</param>
		/// <param name="value">Desired value for the property.</param>
		/// <param name="propertyName">Name of the property used to notify listeners.  This
		/// value is optional and can be provided automatically when invoked from compilers that
		/// support CallerMemberName.</param>
		/// <returns>True if the value was changed, false if the existing value matched the
		/// desired value.</returns>
		private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] String propertyName = null)
		{
			if (object.Equals(storage, value)) return false;

			storage = value;
			this.OnPropertyChanged(propertyName);
			return true;
		}

		/// <summary>
		/// Notifies listeners that a property value has changed.
		/// </summary>
		/// <param name="propertyName">Name of the property used to notify listeners.  This
		/// value is optional and can be provided automatically when invoked from compilers
		/// that support <see cref="CallerMemberNameAttribute"/>.</param>
		private void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			var eventHandler = this.PropertyChanged;
			if (eventHandler != null)
			{
				eventHandler(this, new PropertyChangedEventArgs(propertyName));
			}
		}
		#endregion
	}
}
