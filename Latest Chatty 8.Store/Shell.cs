using Autofac;
using Latest_Chatty_8.Common;
using Latest_Chatty_8.Managers;
using Latest_Chatty_8.Settings;
using Latest_Chatty_8.Views;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Latest_Chatty_8
{
	public sealed partial class Shell : Page, INotifyPropertyChanged
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

		#region Private Variables
		IContainer container;
		Uri embeddedBrowserLink;
		ShellView currentlyDisplayedView;
		CoreWindow keyBindingWindow;
		WebView embeddedBrowser;
		MediaElement embeddedMediaPlayer;
		int lastClipboardThreadId;
		Regex urlParserRegex = new Regex(@"https?://(www.)?shacknews\.com\/chatty\?.*id=(?<id>\d*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		#endregion

		private string npcCurrentViewName = "";
		public string CurrentViewName
		{
			get { return npcCurrentViewName; }
			set { this.SetProperty(ref this.npcCurrentViewName, value); }
		}


		private ChattyManager npcChattyManager;
		public ChattyManager ChattyManager
		{
			get { return this.npcChattyManager; }
			set { this.SetProperty(ref this.npcChattyManager, value); }
		}

		private MessageManager npcMessageManager;
		public MessageManager MessageManager
		{
			get { return this.npcMessageManager; }
			set { this.SetProperty(ref this.npcMessageManager, value); }
		}

		private AuthenticationManager npcAuthManager;
		public AuthenticationManager AuthManager
		{
			get { return this.npcAuthManager; }
			set { this.SetProperty(ref this.npcAuthManager, value); }
		}

		private LatestChattySettings npcSettings;
		public LatestChattySettings Settings
		{
			get { return this.npcSettings; }
			set { this.SetProperty(ref this.npcSettings, value); }
		}

		#region Constructor
		public Shell(Frame rootFrame, IContainer container)
		{
			this.InitializeComponent();

			ApplicationView.GetForCurrentView().SetPreferredMinSize(new Windows.Foundation.Size(320, 320));

			if (rootFrame.Content is Chatty)
			{
				this.chattyRadio.IsChecked = true;
				((Chatty)rootFrame.Content).LinkClicked += Sv_LinkClicked;
				((Chatty)rootFrame.Content).ShellMessage += Sv_ShellMessage;
			}
			this.splitter.Content = rootFrame;
			rootFrame.Navigated += FrameNavigatedTo;
			rootFrame.Navigating += FrameNavigating;
			this.container = container;
			this.MessageManager = this.container.Resolve<MessageManager>();
			this.AuthManager = this.container.Resolve<AuthenticationManager>();
			this.Settings = this.container.Resolve<LatestChattySettings>();
			this.ChattyManager = this.container.Resolve<ChattyManager>();
			this.Settings.PropertyChanged += Settings_PropertyChanged;

			this.SetThemeColor();

			var sv = rootFrame.Content as ShellView;
			if (sv != null)
			{
				this.currentlyDisplayedView = sv;
				SetCaptionFromFrame(sv);
			}

			Windows.UI.Xaml.Window.Current.Activated += WindowActivated;
			Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested += (
				(o, a) =>
				{
					Microsoft.HockeyApp.HockeyClient.Current.TrackEvent("Shell-HardwareBackButtonPressed");
					var handled = false;
					if (this.embeddedBrowserLink != null)
					{
						if (this.embeddedViewer.Visibility == Windows.UI.Xaml.Visibility.Visible)
						{
							if (this.embeddedBrowser.CanGoBack)
							{
								this.embeddedBrowser.GoBack();
							}
							else
							{
								this.CloseEmbeddedBrowser();
							}
							handled = true;
						}
					}
					if (!handled)
					{
						handled = this.GoBack();
					}
					a.Handled = handled;
				});

#if DEBUG
			this.developerRadio.Visibility = Windows.UI.Xaml.Visibility.Visible;
			this.developerRadio.IsEnabled = true;
#endif
		}
		#endregion

		private async void WindowActivated(object sender, WindowActivatedEventArgs e)
		{
			await ShowChattyClipboardLinkOpen(e);
		}

		private async Task ShowChattyClipboardLinkOpen(WindowActivatedEventArgs e)
		{
			if (e.WindowActivationState == CoreWindowActivationState.Deactivated) { return; }

			DataPackageView dataPackageView = Clipboard.GetContent();
			if (dataPackageView.Contains(StandardDataFormats.Text))
			{
				string text = await dataPackageView.GetTextAsync();
				if (!string.IsNullOrWhiteSpace(text))
				{
					var match = this.urlParserRegex.Match(text);
					if (match.Success)
					{
						int threadId;
						if (int.TryParse(match.Groups["id"].Value, out threadId))
						{
							if (threadId != this.lastClipboardThreadId)
							{
								System.Diagnostics.Debug.WriteLine($"Parsed threadId {threadId} from clipboard.");
								this.lastClipboardThreadId = threadId;
								this.linkPopup.IsOpen = true;
							}
						}
					}
				}
			}
		}

		public void NavigateToPage(Type page, object arguments)
		{
			var f = this.splitter.Content as Frame;
			if (f == null) return;

			f.Navigate(page, arguments);

			this.BurguerToggle.IsChecked = false;
		}

		private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName.Equals(nameof(LatestChattySettings.ThemeName)))
			{
				this.SetThemeColor();
			}
		}
		private void FrameNavigating(object sender, NavigatingCancelEventArgs e)
		{
			if (this.currentlyDisplayedView != null)
			{
				this.currentlyDisplayedView.LinkClicked -= Sv_LinkClicked;
				this.currentlyDisplayedView.ShellMessage -= Sv_ShellMessage;
				this.currentlyDisplayedView = null;
			}
		}

		private void FrameNavigatedTo(object sender, NavigationEventArgs e)
		{
			var sv = e.Content as ShellView;
			if (sv != null)
			{
				this.currentlyDisplayedView = sv;
				sv.LinkClicked += Sv_LinkClicked;
				sv.ShellMessage += Sv_ShellMessage;
				SetCaptionFromFrame(sv);
			}

			foreach (var rb in this.AllChildren<RadioButton>().Where(b => b.GroupName.Equals("NavGroup")))
			{
				rb.IsChecked = false;
			}

			if (e.Content is Chatty)
			{
				this.chattyRadio.IsChecked = true;
			}
			else if (e.Content is SettingsView)
			{
				this.settingsRadio.IsChecked = true;
			}
			else if (e.Content is Messages)
			{
				this.messagesRadio.IsChecked = true;
			}
			else if (e.Content is Help)
			{
				this.helpRadio.IsChecked = true;
			}
			//else if (this.tagRadio.IsChecked.HasValue && this.tagRadio.IsChecked.Value)
			//{
			//	f.Navigate(typeof(TagView), this.container);
			//}
#if DEBUG
			else if (e.Content is DeveloperView)
			{
				this.developerRadio.IsChecked = true;
			}
#endif
			var f = this.splitter.Content as Frame;
			SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = f.CanGoBack ? AppViewBackButtonVisibility.Visible : AppViewBackButtonVisibility.Collapsed;
		}

		private async void Sv_ShellMessage(object sender, ShellMessageEventArgs e)
		{
			await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunOnUIThreadAndWait(CoreDispatcherPriority.Normal, () =>
			{
				this.FindName("messageContainer");
			});
			this.popupMessage.ShowMessage(e);
		}

		private void Sv_LinkClicked(object sender, LinkClickedEventArgs e)
		{
			this.ShowEmbeddedLink(e.Link);
		}

		private void ClickedNav(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			if (this.chattyRadio.IsChecked.HasValue && this.chattyRadio.IsChecked.Value)
			{
				this.NavigateToPage(typeof(Chatty), this.container);
			}
			else if (this.settingsRadio.IsChecked.HasValue && this.settingsRadio.IsChecked.Value)
			{
				this.NavigateToPage(typeof(SettingsView), this.container);
			}
			else if (this.messagesRadio.IsChecked.HasValue && this.messagesRadio.IsChecked.Value)
			{
				this.NavigateToPage(typeof(Messages), new Tuple<IContainer, string>(this.container, null));
			}
			else if (this.helpRadio.IsChecked.HasValue && this.helpRadio.IsChecked.Value)
			{
				this.NavigateToPage(typeof(Help), new Tuple<IContainer, bool>(this.container, false));
			}
			//else if (this.tagRadio.IsChecked.HasValue && this.tagRadio.IsChecked.Value)
			//{
			//	f.Navigate(typeof(TagView), this.container);
			//}
#if DEBUG
			else if (this.developerRadio.IsChecked.HasValue && this.developerRadio.IsChecked.Value)
			{
				this.NavigateToPage(typeof(DeveloperView), this.container);
			}
#endif
		}

		private void SetCaptionFromFrame(ShellView sv)
		{
			this.CurrentViewName = sv.ViewTitle;
		}

		private void SetThemeColor()
		{
			var titleBar = ApplicationView.GetForCurrentView().TitleBar;
			titleBar.ButtonBackgroundColor = titleBar.BackgroundColor = titleBar.InactiveBackgroundColor = titleBar.ButtonInactiveBackgroundColor = this.Settings.Theme.WindowTitleBackgroundColor;
			titleBar.ButtonForegroundColor = titleBar.ForegroundColor = this.Settings.Theme.WindowTitleForegroundColor;
			titleBar.InactiveForegroundColor = titleBar.ButtonInactiveForegroundColor = this.Settings.Theme.WindowTitleForegroundColorInactive;
		}

		public bool CanGoBack
		{
			get { return ((Frame)this.splitter.Content).CanGoBack; }
		}
		public bool GoBack()
		{
			var f = this.splitter.Content as Frame;
			if (f.CanGoBack)
			{
				f.GoBack();
				return true;
			}

			return false;
		}

		private async void ShowEmbeddedLink(Uri link)
		{
			if (await this.ShowEmbeddedMediaIfNecessary(link))
			{
				return;
			}

			if (await this.LaunchExternalAppForUrlHandlerIfNecessary(link))
			{
				return;
			}

			if (this.LaunchShackThreadForUriIfNecessary(link))
			{
				return;
			}

			var embeddedHtml = EmbedHelper.GetEmbedHtml(link);

			if (string.IsNullOrWhiteSpace(embeddedHtml) && !this.Settings.OpenUnknownLinksInEmbeddedBrowser)
			{
				//Don't want to use the embedded browser, ever.
				await Launcher.LaunchUriAsync(link);
				return;
			}

			this.FindName("embeddedViewer");
			Microsoft.HockeyApp.HockeyClient.Current.TrackEvent("ShellEmbeddedBrowserShown");
			this.embeddedBrowser = new WebView(WebViewExecutionMode.SeparateThread);
			this.embeddedBrowserContainer.Children.Add(this.embeddedBrowser);
			this.embeddedViewer.Visibility = Windows.UI.Xaml.Visibility.Visible;
			this.embeddedBrowserLink = link;
			this.keyBindingWindow = CoreWindow.GetForCurrentThread();
			this.keyBindingWindow.KeyDown += Shell_KeyDown;
			this.embeddedBrowser.NavigationStarting += EmbeddedBrowser_NavigationStarting;
			this.embeddedBrowser.NavigationCompleted += EmbeddedBrowser_NavigationCompleted;
			if (!string.IsNullOrWhiteSpace(embeddedHtml))
			{
				this.embeddedBrowser.NavigateToString(embeddedHtml);
			}
			else
			{
				this.embeddedBrowser.Navigate(link);
			}
		}

		private void EmbeddedBrowser_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
		{
			this.browserLoadingIndicator.Visibility = Windows.UI.Xaml.Visibility.Visible;
			this.browserLoadingIndicator.IsActive = true;
		}

		private void EmbeddedBrowser_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
		{
			this.browserLoadingIndicator.IsActive = false;
			this.browserLoadingIndicator.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
		}

		private void Shell_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
		{
			switch (args.VirtualKey)
			{
				case VirtualKey.Escape:
					if (this.embeddedViewer.Visibility == Windows.UI.Xaml.Visibility.Visible)
					{
						this.CloseEmbeddedBrowser();
					}
					break;
				default:
					break;
			}
		}

		private async Task<bool> LaunchExternalAppForUrlHandlerIfNecessary(Uri link)
		{
			var launchUri = AppLaunchHelper.GetAppLaunchUri(this.Settings, link);
			if (launchUri != null)
			{
				await Launcher.LaunchUriAsync(launchUri);
				return true;
			}
			return false;
		}

		private async Task<bool> ShowEmbeddedMediaIfNecessary(Uri link)
		{
			try
			{
				if (this.Settings.ExternalYoutubeApp.Type == ExternalYoutubeAppType.InternalMediaPlayer)
				{
					var id = AppLaunchHelper.GetYoutubeId(link);
					if (!string.IsNullOrWhiteSpace(id))
					{
						var videoUrl = await MyToolkit.Multimedia.YouTube.GetVideoUriAsync(id, this.Settings.EmbeddedYouTubeResolution.Quality);
						this.FindName("embeddedViewer");
						this.embeddedMediaPlayer = new MediaElement();
						this.embeddedMediaPlayer.AutoPlay = false;
						this.embeddedMediaPlayer.AreTransportControlsEnabled = true;
						this.embeddedMediaPlayer.Source = videoUrl.Uri;
						this.embeddedBrowserContainer.Children.Add(this.embeddedMediaPlayer);
						this.embeddedViewer.Visibility = Windows.UI.Xaml.Visibility.Visible;
						this.embeddedBrowserLink = link;
						this.keyBindingWindow = CoreWindow.GetForCurrentThread();
						this.keyBindingWindow.KeyDown += Shell_KeyDown;
						return true;
					}
				}
			}
			catch { }
			return false;
		}

		private bool LaunchShackThreadForUriIfNecessary(Uri link)
		{
			var postId = AppLaunchHelper.GetShackPostId(link);
			if (postId != null)
			{
				this.NavigateToPage(typeof(SingleThreadView), new Tuple<IContainer, int, int>(this.container, postId.Value, postId.Value));
				return true;
			}
			return false;
		}

		private void EmbeddedCloseClicked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			this.CloseEmbeddedBrowser();
		}

		private void CloseEmbeddedBrowser()
		{
			Microsoft.HockeyApp.HockeyClient.Current.TrackEvent("ShellEmbeddedBrowserClosed");
			this.keyBindingWindow.KeyDown -= Shell_KeyDown;
			if (this.embeddedBrowser != null)
			{
				this.embeddedBrowser.NavigationStarting -= EmbeddedBrowser_NavigationStarting;
				this.embeddedBrowser.NavigationCompleted -= EmbeddedBrowser_NavigationCompleted;
				this.embeddedBrowser.Stop();
				this.embeddedBrowser.NavigateToString("");
			}
			if (this.embeddedMediaPlayer != null)
			{
				this.embeddedMediaPlayer.Stop();
				this.embeddedMediaPlayer.Source = null;
				this.embeddedMediaPlayer = null;
			}
			this.embeddedViewer.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
			this.embeddedBrowserContainer.Children.Clear();
			this.embeddedBrowser = null;
			this.embeddedBrowserLink = null;
		}

		private async void EmbeddedBrowserClicked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			if (this.embeddedBrowserLink != null)
			{
				Microsoft.HockeyApp.HockeyClient.Current.TrackEvent("ShellEmbeddedBrowserShowFullBrowser");
				await Windows.System.Launcher.LaunchUriAsync(this.embeddedBrowserLink);
				this.CloseEmbeddedBrowser();
			}
		}

		private void CloseClipboardLinkPopupButtonClicked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			this.linkPopup.IsOpen = false;
		}

		private void OpenClipboardLinkTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
		{
			if (this.lastClipboardThreadId != 0)
			{
				this.NavigateToPage(typeof(SingleThreadView), new Tuple<IContainer, int, int>(this.container, this.lastClipboardThreadId, this.lastClipboardThreadId));
				this.linkPopup.IsOpen = false;
			}
		}
	}
}
