using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Autofac;
using Common;
using Latest_Chatty_8.Common;
using Latest_Chatty_8.Managers;
using Latest_Chatty_8.Networking;
using Latest_Chatty_8.Settings;
using Latest_Chatty_8.Views;
using Microsoft.HockeyApp;
//using MyToolkit.Multimedia;
using IContainer = Autofac.IContainer;
using Windows.UI.Xaml.Documents;

namespace Latest_Chatty_8
{
	public sealed partial class Shell : INotifyPropertyChanged
	{
		#region NPC
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
		///     value is optional and can be provided automatically when invoked from compilers that
		///     support CallerMemberName.</param>
		/// <returns>True if the value was changed, false if the existing value matched the
		/// desired value.</returns>
		private void SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
		{
			if (Equals(storage, value)) return;

			storage = value;
			OnPropertyChanged(propertyName);
		}

		/// <summary>
		/// Notifies listeners that a property value has changed.
		/// </summary>
		/// <param name="propertyName">Name of the property used to notify listeners.  This
		/// value is optional and can be provided automatically when invoked from compilers
		/// that support <see cref="CallerMemberNameAttribute"/>.</param>
		private void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			var eventHandler = PropertyChanged;
			if (eventHandler != null)
			{
				eventHandler(this, new PropertyChangedEventArgs(propertyName));
			}
		}
		#endregion

		private const int LINK_POPUP_TIMEOUT = 8000;

		#region Private Variables

		readonly IContainer _container;
		Uri _embeddedBrowserLink;
		ShellView _currentlyDisplayedView;
		CoreWindow _keyBindingWindow;
		WebView _embeddedBrowser;
		MediaElement _embeddedMediaPlayer;
		DispatcherTimer _popupTimer = new DispatcherTimer();
		DateTime _linkPopupExpireTime;
		int _lastClipboardThreadId;
		#endregion

		private string npcCurrentViewName = "";
		public string CurrentViewName
		{
			get => npcCurrentViewName;
			set => SetProperty(ref npcCurrentViewName, value);
		}


		private ChattyManager npcChattyManager;
		public ChattyManager ChattyManager
		{
			get => npcChattyManager;
			set => SetProperty(ref npcChattyManager, value);
		}

		private MessageManager npcMessageManager;
		public MessageManager MessageManager
		{
			get => npcMessageManager;
			set => SetProperty(ref npcMessageManager, value);
		}

		private AuthenticationManager npcAuthManager;
		public AuthenticationManager AuthManager
		{
			get => npcAuthManager;
			set => SetProperty(ref npcAuthManager, value);
		}

		private LatestChattySettings npcSettings;
		public LatestChattySettings Settings
		{
			get => npcSettings;
			set => SetProperty(ref npcSettings, value);
		}

		private NetworkConnectionStatus npcConnectionStatus;
		public NetworkConnectionStatus ConnectionStatus
		{
			get => npcConnectionStatus;
			set => SetProperty(ref npcConnectionStatus, value);
		}

		#region Constructor
		public Shell(Frame rootFrame, IContainer container)
		{
			InitializeComponent();

			ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(320, 320));

			if (rootFrame.Content is Chatty)
			{
				ChattyRadio.IsChecked = true;
				((Chatty)rootFrame.Content).LinkClicked += Sv_LinkClicked;
				((Chatty)rootFrame.Content).ShellMessage += Sv_ShellMessage;
			}
			//Needs an interface... yup. Some day.
			if (rootFrame.Content is InlineChattyFast)
			{
				ChattyRadio.IsChecked = true;
				((InlineChattyFast)rootFrame.Content).LinkClicked += Sv_LinkClicked;
				((InlineChattyFast)rootFrame.Content).ShellMessage += Sv_ShellMessage;
			}
			Splitter.Content = rootFrame;
			rootFrame.Navigated += FrameNavigatedTo;
			rootFrame.Navigating += FrameNavigating;
			_container = container;
			MessageManager = _container.Resolve<MessageManager>();
			AuthManager = _container.Resolve<AuthenticationManager>();
			Settings = _container.Resolve<LatestChattySettings>();
			ChattyManager = _container.Resolve<ChattyManager>();
			ConnectionStatus = _container.Resolve<NetworkConnectionStatus>();
			ConnectionStatus.PropertyChanged += ConnectionStatus_PropertyChanged;
			Settings.PropertyChanged += Settings_PropertyChanged;
			App.Current.UnhandledException += UnhandledAppException;

			SetThemeColor();

			var sv = rootFrame.Content as ShellView;
			if (sv != null)
			{
				_currentlyDisplayedView = sv;
				SetCaptionFromFrame(sv);
			}

			Window.Current.Activated += WindowActivated;
			SystemNavigationManager.GetForCurrentView().BackRequested += (
				(o, a) =>
				{
					HockeyClient.Current.TrackEvent("Shell-HardwareBackButtonPressed");

					a.Handled = NavigateBack();
				});
			CoreWindow.GetForCurrentThread().PointerPressed += (sender, args) =>
			{
				if (args.CurrentPoint.Properties.IsXButton1Pressed) args.Handled = NavigateBack();
			};
		}

		private void UnhandledAppException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
		{
			Sv_ShellMessage(this,
				new ShellMessageEventArgs("Uh oh. Things may not work right from this point forward. We don't know what happened."
				+ Environment.NewLine + "Restarting the application may help."
				+ Environment.NewLine
				+ Environment.NewLine + "Here's some info that means nothing to you:"
				+ Environment.NewLine + e.Message
				+ Environment.NewLine + e.Exception.StackTrace,
				ShellMessageType.Error));
			e.Handled = true;
		}

		private bool NavigateBack()
		{
			var handled = false;
			if (_embeddedBrowserLink != null)
			{
				if (EmbeddedViewer.Visibility == Visibility.Visible)
				{
					if (_embeddedBrowser.CanGoBack)
					{
						_embeddedBrowser.GoBack();
					}
					else
					{
						CloseEmbeddedBrowser();
					}
					handled = true;
				}
			}
			if (!handled)
			{
				handled = GoBack();
			}

			return handled;
		}

		private void ConnectionStatus_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			var status = sender as NetworkConnectionStatus;
			if (status == null) return;
			if (ConnectionIndicatorButton == null || ConnectionIndicatorButton.Flyout == null) return;
			if (!status.IsConnected)
			{
				ConnectionIndicatorButton.Flyout.ShowAt(ConnectionIndicatorButton);
			}
			else
			{
				ConnectionIndicatorButton.Flyout.Hide();
			}
		}
		#endregion

		private async void WindowActivated(object sender, WindowActivatedEventArgs e)
		{
			await ShowChattyClipboardLinkOpen(e);
		}

		private async Task ShowChattyClipboardLinkOpen(WindowActivatedEventArgs e)
		{
			if (e.WindowActivationState == CoreWindowActivationState.Deactivated) { return; }

			try
			{
				DataPackageView dataPackageView = Clipboard.GetContent();
				if (dataPackageView.Contains(StandardDataFormats.Text))
				{
					string text = await dataPackageView.GetTextAsync();
					if (ChattyHelper.TryGetThreadIdFromUrl(text, out var threadId))
					{
						if (threadId != _lastClipboardThreadId)
						{
							Debug.WriteLine($"Parsed threadId {threadId} from clipboard.");
							_lastClipboardThreadId = threadId;
							LinkPopup.IsOpen = true;
							_popupTimer.Stop();
							_linkPopupExpireTime = DateTime.Now.AddMilliseconds(LINK_POPUP_TIMEOUT);
							_popupTimer.Interval = TimeSpan.FromMilliseconds(30);
							LinkPopupTimer.Value = 100;
							_popupTimer.Tick += (_, __) =>
							{
								var remaining = _linkPopupExpireTime.Subtract(DateTime.Now).TotalMilliseconds;
								if (remaining <= 0)
								{
									LinkPopup.IsOpen = false;
									_popupTimer.Stop();
								}
								else
								{
									LinkPopupTimer.Value = Math.Max(((double)remaining / LINK_POPUP_TIMEOUT) * 100, 0);
								}
							};
							_popupTimer.Start();
						}
					}
				}
			}
			catch
			{
				// ignored
			} //Had an exception where data in clipboard was invalid. Ultimately if this doesn't work, who cares.
		}

		public void NavigateToPage(Type page, object arguments)
		{
			var f = Splitter.Content as Frame;
			if (f == null) return;

			f.Navigate(page, arguments);

			BurguerToggle.IsChecked = false;
		}

		private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName.Equals(nameof(LatestChattySettings.ThemeName)))
			{
				SetThemeColor();
			}
		}
		private void FrameNavigating(object sender, NavigatingCancelEventArgs e)
		{
			if (_currentlyDisplayedView != null)
			{
				_currentlyDisplayedView.LinkClicked -= Sv_LinkClicked;
				_currentlyDisplayedView.ShellMessage -= Sv_ShellMessage;
				_currentlyDisplayedView = null;
			}
		}

		private void FrameNavigatedTo(object sender, NavigationEventArgs e)
		{
			var sv = e.Content as ShellView;
			if (sv != null)
			{
				_currentlyDisplayedView = sv;
				sv.LinkClicked += Sv_LinkClicked;
				sv.ShellMessage += Sv_ShellMessage;
				SetCaptionFromFrame(sv);
			}

			foreach (var rb in this.AllChildren<RadioButton>().Where(b => b.GroupName.Equals("NavGroup")))
			{
				rb.IsChecked = false;
			}

			if (e.Content is Chatty || e.Content is InlineChattyFast)
			{
				ChattyRadio.IsChecked = true;
			}
			else if (e.Content is SettingsView)
			{
				SettingsRadio.IsChecked = true;
			}
			else if (e.Content is Messages)
			{
				MessagesRadio.IsChecked = true;
			}
			else if (e.Content is Help)
			{
				HelpRadio.IsChecked = true;
			}
			//else if (this.tagRadio.IsChecked.HasValue && this.tagRadio.IsChecked.Value)
			//{
			//	f.Navigate(typeof(TagView), this.container);
			//}
#if DEBUG
			else if (e.Content is DeveloperView)
			{
				DeveloperRadio.IsChecked = true;
			}
#endif
			var f = Splitter.Content as Frame;
			SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = f != null && f.CanGoBack ? AppViewBackButtonVisibility.Visible : AppViewBackButtonVisibility.Collapsed;
		}

		private async void Sv_ShellMessage(object sender, ShellMessageEventArgs e)
		{
			await CoreApplication.MainView.CoreWindow.Dispatcher.RunOnUiThreadAndWait(CoreDispatcherPriority.Normal, () =>
			{
				FindName("MessageContainer");
			});
			PopupMessage.ShowMessage(e);
		}

		private void Sv_LinkClicked(object sender, LinkClickedEventArgs e)
		{
			ShowEmbeddedLink(e.Link);
		}

		private void ClickedNav(object sender, RoutedEventArgs e)
		{
			if (ChattyRadio.IsChecked.HasValue && ChattyRadio.IsChecked.Value)
			{
				NavigateToPage(Settings.UseMainDetail ? typeof(Chatty) : typeof(InlineChattyFast), _container);
			}
			else if (SettingsRadio.IsChecked.HasValue && SettingsRadio.IsChecked.Value)
			{
				NavigateToPage(typeof(SettingsView), _container);
			}
			else if (MessagesRadio.IsChecked.HasValue && MessagesRadio.IsChecked.Value)
			{
				NavigateToPage(typeof(Messages), new Tuple<IContainer, string>(_container, null));
			}
			else if (HelpRadio.IsChecked.HasValue && HelpRadio.IsChecked.Value)
			{
				NavigateToPage(typeof(Help), new Tuple<IContainer, bool>(_container, false));
			}
			else if (SearchRadio.IsChecked.HasValue && SearchRadio.IsChecked.Value)
			{
				NavigateToPage(typeof(ShackWebView), new Tuple<IContainer, Uri>(_container, new Uri("https://shacknews.com/search?q=&type=4")));
			}
			else if (TagRadio.IsChecked.HasValue && TagRadio.IsChecked.Value)
			{
				NavigateToPage(typeof(ShackWebView), new Tuple<IContainer, Uri>(_container, new Uri("https://www.shacknews.com/tags-user")));
			}
			else if (PinnedRadio.IsChecked.HasValue && PinnedRadio.IsChecked.Value)
			{
				NavigateToPage(typeof(PinnedThreadsView), _container);
			}
			else if (DeveloperRadio.IsChecked.HasValue && DeveloperRadio.IsChecked.Value)
			{
				NavigateToPage(typeof(DeveloperView), _container);
			}
		}

		private void SetCaptionFromFrame(ShellView sv)
		{
			CurrentViewName = sv.ViewTitle;
		}

		private void SetThemeColor()
		{
			var titleBar = ApplicationView.GetForCurrentView().TitleBar;
			titleBar.ButtonBackgroundColor = titleBar.BackgroundColor = titleBar.InactiveBackgroundColor = titleBar.ButtonInactiveBackgroundColor = Settings.Theme.WindowTitleBackgroundColor;
			titleBar.ButtonForegroundColor = titleBar.ForegroundColor = Settings.Theme.WindowTitleForegroundColor;
			titleBar.InactiveForegroundColor = titleBar.ButtonInactiveForegroundColor = Settings.Theme.WindowTitleForegroundColorInactive;
		}

		public bool CanGoBack => Splitter.Content != null && ((Frame)Splitter.Content).CanGoBack;

		public bool GoBack()
		{
			var f = Splitter.Content as Frame;
			if (f != null && f.CanGoBack)
			{
				f.GoBack();
				return true;
			}

			return false;
		}

		private async void ShowEmbeddedLink(Uri link)
		{
			//if (await ShowEmbeddedMediaIfNecessary(link))
			//{
			//	return;
			//}

			link = await LaunchExternalAppOrGetEmbeddedUri(link);
			if (link == null) //it was handled, no more to do.
			{
				return;
			}

			if (LaunchShackThreadForUriIfNecessary(link))
			{
				return;
			}

			var embeddedHtml = EmbedHelper.GetEmbedHtml(link);

			if (string.IsNullOrWhiteSpace(embeddedHtml) && !Settings.OpenUnknownLinksInEmbeddedBrowser)
			{
				//Don't want to use the embedded browser, ever.
				await Launcher.LaunchUriAsync(link);
				return;
			}

			FindName("EmbeddedViewer");
			HockeyClient.Current.TrackEvent("ShellEmbeddedBrowserShown");
			_embeddedBrowser = new WebView(WebViewExecutionMode.SeparateThread);
			EmbeddedBrowserContainer.Children.Add(_embeddedBrowser);
			EmbeddedViewer.Visibility = Visibility.Visible;
			_embeddedBrowserLink = link;
			_keyBindingWindow = CoreWindow.GetForCurrentThread();
			_keyBindingWindow.KeyDown += Shell_KeyDown;
			_embeddedBrowser.NavigationStarting += EmbeddedBrowser_NavigationStarting;
			_embeddedBrowser.NavigationCompleted += EmbeddedBrowser_NavigationCompleted;
			if (!string.IsNullOrWhiteSpace(embeddedHtml))
			{
				_embeddedBrowser.NavigateToString(embeddedHtml);
			}
			else
			{
				_embeddedBrowser.Navigate(link);
			}
		}

		private void EmbeddedBrowser_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
		{
			BrowserLoadingIndicator.Visibility = Visibility.Visible;
			BrowserLoadingIndicator.IsActive = true;
		}

		private void EmbeddedBrowser_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
		{
			BrowserLoadingIndicator.IsActive = false;
			BrowserLoadingIndicator.Visibility = Visibility.Collapsed;
		}

		private void Shell_KeyDown(CoreWindow sender, KeyEventArgs args)
		{
			switch (args.VirtualKey)
			{
				case VirtualKey.Escape:
					if (EmbeddedViewer.Visibility == Visibility.Visible)
					{
						CloseEmbeddedBrowser();
					}
					break;
			}
		}

		private async Task<Uri> LaunchExternalAppOrGetEmbeddedUri(Uri link)
		{
			var launchUri = AppLaunchHelper.GetAppLaunchUri(Settings, link);
			if (launchUri.uri != null && !launchUri.openInEmbeddedBrowser)
			{
				await Launcher.LaunchUriAsync(launchUri.uri);
				return null;
			}
			return launchUri.uri;
		}

		//private async Task<bool> ShowEmbeddedMediaIfNecessary(Uri link)
		//{
		//	try
		//	{
		//		if (Settings.ExternalYoutubeApp.Type == ExternalYoutubeAppType.InternalMediaPlayer)
		//		{
		//			var id = AppLaunchHelper.GetYoutubeId(link);
		//			if (!string.IsNullOrWhiteSpace(id))
		//			{
		//				var videoUrl = await YouTube.GetVideoUriAsync(id, Settings.EmbeddedYouTubeResolution.Quality);
		//				FindName("EmbeddedViewer");
		//				_embeddedMediaPlayer = new MediaElement();
		//				_embeddedMediaPlayer.AutoPlay = false;
		//				_embeddedMediaPlayer.AreTransportControlsEnabled = true;
		//				_embeddedMediaPlayer.Source = videoUrl.Uri;
		//				EmbeddedBrowserContainer.Children.Add(_embeddedMediaPlayer);
		//				EmbeddedViewer.Visibility = Visibility.Visible;
		//				_embeddedBrowserLink = link;
		//				_keyBindingWindow = CoreWindow.GetForCurrentThread();
		//				_keyBindingWindow.KeyDown += Shell_KeyDown;
		//				return true;
		//			}
		//		}
		//	}
		//	catch
		//	{
		//		// ignored
		//	}

		//	return false;
		//}

		private bool LaunchShackThreadForUriIfNecessary(Uri link)
		{
			var postId = AppLaunchHelper.GetShackPostId(link);
			if (postId != null)
			{
				NavigateToPage(typeof(SingleThreadView), new Tuple<IContainer, int, int>(_container, postId.Value, postId.Value));
				return true;
			}
			return false;
		}

		private void EmbeddedCloseClicked(object sender, RoutedEventArgs e)
		{
			CloseEmbeddedBrowser();
		}

		private void CloseEmbeddedBrowser()
		{
			HockeyClient.Current.TrackEvent("ShellEmbeddedBrowserClosed");
			_keyBindingWindow.KeyDown -= Shell_KeyDown;
			if (_embeddedBrowser != null)
			{
				_embeddedBrowser.NavigationStarting -= EmbeddedBrowser_NavigationStarting;
				_embeddedBrowser.NavigationCompleted -= EmbeddedBrowser_NavigationCompleted;
				_embeddedBrowser.Stop();
				_embeddedBrowser.NavigateToString("");
			}
			if (_embeddedMediaPlayer != null)
			{
				_embeddedMediaPlayer.Stop();
				_embeddedMediaPlayer.Source = null;
				_embeddedMediaPlayer = null;
			}
			EmbeddedViewer.Visibility = Visibility.Collapsed;
			EmbeddedBrowserContainer.Children.Clear();
			_embeddedBrowser = null;
			_embeddedBrowserLink = null;
		}

		private async void EmbeddedBrowserClicked(object sender, RoutedEventArgs e)
		{
			if (_embeddedBrowserLink != null)
			{
				HockeyClient.Current.TrackEvent("ShellEmbeddedBrowserShowFullBrowser");
				await Launcher.LaunchUriAsync(_embeddedBrowserLink);
				CloseEmbeddedBrowser();
			}
		}

		private void CloseClipboardLinkPopupButtonClicked(object sender, RoutedEventArgs e)
		{
			LinkPopup.IsOpen = false;
		}

		private void OpenClipboardLinkTapped(object sender, TappedRoutedEventArgs e)
		{
			if (_lastClipboardThreadId != 0)
			{
				NavigateToPage(typeof(SingleThreadView), new Tuple<IContainer, int, int>(_container, _lastClipboardThreadId, _lastClipboardThreadId));
				LinkPopup.IsOpen = false;
			}
		}
	}
}
