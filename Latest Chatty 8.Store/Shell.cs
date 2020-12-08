using Autofac;
using Common;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Werd.Common;
using Werd.Managers;
using Werd.Networking;
using Werd.Settings;
using Werd.Views;
using Werd.Views.NavigationArgs;
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
using IContainer = Autofac.IContainer;

namespace Werd
{
	//Hiding shell probably isn't great, but it's not like I'm using it, so meh?
#pragma warning disable CA1724
	public sealed partial class Shell : INotifyPropertyChanged
#pragma warning restore CA1724
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
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion

		private const int LINK_POPUP_TIMEOUT = 10000;

		#region Private Variables

		readonly IContainer _container;
		Uri _embeddedBrowserLink;
		ShellView _currentlyDisplayedView;
		CoreWindow _keyBindingWindow;
		WebView _embeddedBrowser;
		MediaElement _embeddedMediaPlayer;
		readonly DispatcherTimer _popupTimer = new DispatcherTimer();
		DateTime _linkPopupExpireTime;

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
		public Shell(string initialNavigation, IContainer container)
		{
			InitializeComponent();

			ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(400, 400));
			_container = container;
			MessageManager = _container.Resolve<MessageManager>();
			AuthManager = _container.Resolve<AuthenticationManager>();
			Settings = _container.Resolve<LatestChattySettings>();
			ChattyManager = _container.Resolve<ChattyManager>();
			ConnectionStatus = _container.Resolve<NetworkConnectionStatus>();
			ConnectionStatus.PropertyChanged += ConnectionStatus_PropertyChanged;
			Settings.PropertyChanged += Settings_PropertyChanged;
			Application.Current.UnhandledException += UnhandledAppException;

			SetThemeColor();

			//Don't really need to unsubscribe to this because there's only ever one shell and it should last the lifetime of the application.
			CoreWindow.GetForCurrentThread().KeyDown += Shell_KeyDown;
			Window.Current.Activated += WindowActivated;
			SystemNavigationManager.GetForCurrentView().BackRequested +=
				async (o, a) =>
				{
					await DebugLog.AddMessage("Shell-HardwareBackButtonPressed").ConfigureAwait(true);
					a.Handled = await NavigateBack().ConfigureAwait(true);
				};
			CoreWindow.GetForCurrentThread().PointerPressed += async (sender, args) =>
			{
				if (args.CurrentPoint.Properties.IsXButton1Pressed) args.Handled = await NavigateBack().ConfigureAwait(true);
			};

			FocusManager.GettingFocus += FocusManager_GettingFocus;
			FocusManager.LosingFocus += FocusManager_LosingFocus;

			NavigateToTag(initialNavigation).ConfigureAwait(true).GetAwaiter().GetResult();
		}

		private void FocusManager_LosingFocus(object sender, LosingFocusEventArgs e)
		{
			if (e.NewFocusedElement is TextBox) AppGlobal.ShortcutKeysEnabled = false;
		}

		private void FocusManager_GettingFocus(object sender, GettingFocusEventArgs e)
		{
			if (e.OldFocusedElement is TextBox) AppGlobal.ShortcutKeysEnabled = true;
		}



		//private async void FocusManager_LosingFocus(object sender, LosingFocusEventArgs e)
		//{
		//	await DebugLog.AddMessage($"LostFocus: CorId [{e.CorrelationId}] - NewElement [{e.NewFocusedElement?.GetType().Name}] LastElement [{e.OldFocusedElement?.GetType().Name}] State [{e.FocusState}] InputDevice [{e.InputDevice}]").ConfigureAwait(true);

		//}

		private void UnhandledAppException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
		{
			//Tooltips are throwing exceptions when the control they're bound to goes away.
			// This isn't detrimental to the application functionality so... ignore them.
			var stackTrace = e.Exception.StackTrace;
			if (!e.Message.StartsWith("The text associated with this error code could not be found.", StringComparison.InvariantCulture))
			{
				Sv_ShellMessage(this,
					new ShellMessageEventArgs("Uh oh. Things may not work right from this point forward. We don't know what happened."
					+ Environment.NewLine + "Restarting the application may help."
					+ Environment.NewLine + "Message: " + e.Message,
					ShellMessageType.Error));
			}
			Task.Run(() => DebugLog.AddMessage($"UNHANDLED EXCEPTION: {e.Message + Environment.NewLine + stackTrace}"));
			e.Handled = true;
		}

		private async Task<bool> NavigateBack()
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
						await CloseEmbeddedBrowser().ConfigureAwait(true);
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
			if (!status.IsConnected)
			{
				Sv_ShellMessage(this, new ShellMessageEventArgs(status.MessageDetails, ShellMessageType.Error));
			}
		}
		#endregion

		private async void WindowActivated(object sender, WindowActivatedEventArgs e)
		{
			await ShowChattyClipboardLinkOpen(e).ConfigureAwait(true);
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
						if (threadId != Settings.LastClipboardPostId)
						{
							await DebugLog.AddMessage($"Parsed threadId {threadId} from clipboard.").ConfigureAwait(true);
							Settings.LastClipboardPostId = threadId;
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
									LinkPopupTimer.Value = Math.Max((double)remaining / LINK_POPUP_TIMEOUT * 100, 0);
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

		public void NavigateToPage(Type page, object arguments, bool forceNav = false)
		{
			if (navigationFrame.CurrentSourcePageType != page || forceNav)
			{
				navigationFrame.Navigate(page, arguments);
			}
		}
		public void OpenThreadTab(int postId)
		{
			if (navigationFrame.CurrentSourcePageType != typeof(Chatty) && navigationFrame.CurrentSourcePageType != typeof(InlineChattyFast))
			{
				NavigateToPage(Settings.UseMainDetail ? typeof(Chatty) : typeof(InlineChattyFast), new ChattyNavigationArgs(_container) { OpenPostInTabId = postId });
			}
			else
			{
				_currentlyDisplayedView.ShellTabOpenRequest(postId);
			}
		}

		private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName.Equals(nameof(LatestChattySettings.ThemeName), StringComparison.InvariantCulture))
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

		private async void FrameNavigatedTo(object sender, NavigationEventArgs e)
		{
			var sv = e.Content as ShellView;
			if (sv != null)
			{
				_currentlyDisplayedView = sv;
				sv.LinkClicked += Sv_LinkClicked;
				sv.ShellMessage += Sv_ShellMessage;
				SetCaptionFromFrame(sv);
			}

			NavView.IsBackEnabled = CanGoBack;

			await DebugLog.AddMessage($"Shell navigated to {e.Content.GetType().Name}").ConfigureAwait(true);

			if (e.Content is Chatty || e.Content is InlineChattyFast)
			{
				SelectFromTag("chatty", e.Content);
			}
			else if (e.Content is PinnedThreadsView)
			{
				SelectFromTag("pinned", e.Content);
			}
			else if (e.Content is CustomSearchWebView)
			{
				SelectFromTag("search", e.Content);
			}
			else if (e.Content is VanitySearchWebView)
			{
				SelectFromTag("vanitysearch", e.Content);
			}
			else if (e.Content is MyPostsSearchWebView)
			{
				SelectFromTag("mypostssearch", e.Content);
			}
			else if (e.Content is RepliesToMeSearchWebView)
			{
				SelectFromTag("repliestomesearch", e.Content);
			}
			else if (e.Content is TagsWebView)
			{
				SelectFromTag("tags", e.Content);
			}
			else if (e.Content is SettingsView)
			{
				NavView.SelectedItem = NavView.SettingsItem;
			}
			else if (e.Content is Messages)
			{
				SelectFromTag("message", e.Content);
			}
			else if (e.Content is Help)
			{
				SelectFromTag("help", e.Content);
			}
			else if (e.Content is DeveloperView)
			{
				SelectFromTag("devtools", e.Content);
			}
			else if (e.Content is ModToolsWebView)
			{
				SelectFromTag("modtools", e.Content);
			}
		}

		private void SelectFromTag(string tag, object _)
		{

			NavView.SelectedItem = NavView.MenuItems
				.OfType<Microsoft.UI.Xaml.Controls.NavigationViewItem>()
				.SelectMany(nvi => nvi.MenuItems.OfType<Microsoft.UI.Xaml.Controls.NavigationViewItem>().Union(new [] { nvi }))
				.FirstOrDefault(item => item.Tag == null ? false : item.Tag.ToString().Equals(tag, StringComparison.OrdinalIgnoreCase));
			// This doesn't work. Seems like something the nav control should handle anyway and is ultimately a MUXC bug.
			//if (o is SearchWebView)
			//{
			//	SearchParentMenuItem.IsChildSelected = true;
			//}
		}

		private async void Sv_ShellMessage(object sender, ShellMessageEventArgs e)
		{
			await CoreApplication.MainView.CoreWindow.Dispatcher.RunOnUiThreadAndWait(CoreDispatcherPriority.Normal, () =>
			{
				FindName("MessageContainer");
			}).ConfigureAwait(true);
			PopupMessage.ShowMessage(e);
		}

		private void Sv_LinkClicked(object sender, LinkClickedEventArgs e)
		{
			ShowEmbeddedLink(e.Link);
		}

		private async void ClickedNav(Microsoft.UI.Xaml.Controls.NavigationView _, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
		{
			if (args.IsSettingsInvoked)
			{
				NavigateToPage(typeof(SettingsView), _container);
				return;
			}
			if (args.InvokedItemContainer?.Tag is null) return;
			await NavigateToTag(args.InvokedItemContainer.Tag.ToString()).ConfigureAwait(true);
		}

		private async Task NavigateToTag(string tag)
		{
			switch (tag.ToUpperInvariant())
			{
				default:
				case "CHATTY":
					NavigateToPage(Settings.UseMainDetail ? typeof(Chatty) : typeof(InlineChattyFast), new ChattyNavigationArgs(_container));
					break;
				case "PINNED":
					NavigateToPage(typeof(PinnedThreadsView), _container);
					break;
				case "SEARCH":
					NavigateToPage(typeof(CustomSearchWebView), new Tuple<IContainer, Uri>(_container, new Uri("https://shacknews.com/search?q=&type=4")), true);
					break;
				case "MYPOSTSSEARCH":
					NavigateToPage(typeof(MyPostsSearchWebView), new Tuple<IContainer, Uri>(_container, new Uri($"https://www.shacknews.com/search?chatty=1&type=4&chatty_term=&chatty_user={AuthManager.UserName}&chatty_author=&chatty_filter=all&result_sort=postdate_desc")), true);
					break;
				case "REPLIESTOMESEARCH":
					NavigateToPage(typeof(RepliesToMeSearchWebView), new Tuple<IContainer, Uri>(_container, new Uri($"https://www.shacknews.com/search?chatty=1&type=4&chatty_term=&chatty_user=&chatty_author={AuthManager.UserName}&chatty_filter=all&result_sort=postdate_desc")), true);
					break;
				case "VANITYSEARCH":
					NavigateToPage(typeof(VanitySearchWebView), new Tuple<IContainer, Uri>(_container, new Uri($"https://www.shacknews.com/search?chatty=1&type=4&chatty_term={AuthManager.UserName}&chatty_user=&chatty_author=&chatty_filter=all&result_sort=postdate_desc")), true);
					break;
				case "TAGS":
					NavigateToPage(typeof(TagsWebView), new Tuple<IContainer, Uri>(_container, new Uri("https://www.shacknews.com/tags-user")));
					break;
				case "MODTOOLS":
					NavigateToPage(typeof(ModToolsWebView), new Tuple<IContainer, Uri>(_container, new Uri("https://www.shacknews.com/moderators/ban-tool")));
					break;
				case "DEVTOOLS":
					NavigateToPage(typeof(DeveloperView), _container);
					break;
				case "HELP":
					NavigateToPage(typeof(Help), new Tuple<IContainer, bool>(_container, false));
					break;
				case "CHANGELOG":
					NavigateToPage(typeof(Help), new Tuple<IContainer, bool>(_container, true));
					break;
				case "MESSAGE":
					NavigateToPage(typeof(Messages), new Tuple<IContainer, string>(_container, null));
					break;
				case "CORTEXCREATE":
					//NavigateToPage(typeof(CortexCreateWebView), new Tuple<IContainer, Uri>(_container, new Uri("https://www.shacknews.com/cortex/create")));
					await Launcher.LaunchUriAsync(new Uri("https://www.shacknews.com/cortex/create"));
					break;
				case "CORTEXFEED":
					NavigateToPage(typeof(CortexFeedWebView), new Tuple<IContainer, Uri>(_container, new Uri("https://www.shacknews.com/cortex/my-feed")));
					break;
				case "CORTEXALLPOSTS":
					NavigateToPage(typeof(CortexAllPostsWebView), new Tuple<IContainer, Uri>(_container, new Uri("https://www.shacknews.com/cortex/articles")));
					break;
				case "CORTEXMYPOSTS":
					NavigateToPage(typeof(CortexMyPostsWebView), new Tuple<IContainer, Uri>(_container, new Uri("https://www.shacknews.com/cortex/my-articles")));
					break;
				case "CORTEXDRAFTS":
					//NavigateToPage(typeof(CortexDraftsWebView), new Tuple<IContainer, Uri>(_container, new Uri("https://www.shacknews.com/cortex/my-drafts")));
					await Launcher.LaunchUriAsync(new Uri("https://www.shacknews.com/cortex/my-drafts"));
					break;
				case "CORTEXFOLLOWING":
					NavigateToPage(typeof(CortexFollowingWebView), new Tuple<IContainer, Uri>(_container, new Uri("https://www.shacknews.com/cortex/follow")));
					break;
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

		public bool CanGoBack => navigationFrame.Content != null && navigationFrame.CanGoBack;

		public bool GoBack()
		{
			var f = navigationFrame;
			if (f != null && f.CanGoBack)
			{
				f.GoBack();
				return true;
			}

			return false;
		}

		private async void ShowEmbeddedLink(Uri link)
		{
			link = await LaunchExternalAppOrGetEmbeddedUri(link).ConfigureAwait(true);
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
			await DebugLog.AddMessage("ShellEmbeddedBrowserShown").ConfigureAwait(true);
			_embeddedBrowser = new WebView(WebViewExecutionMode.SeparateThread);
			EmbeddedBrowserContainer.Children.Add(_embeddedBrowser);
			EmbeddedViewer.Visibility = Visibility.Visible;
			_embeddedBrowserLink = link;
			_keyBindingWindow = CoreWindow.GetForCurrentThread();
			_keyBindingWindow.KeyDown += WebViewDismissKeyHandler;
			_embeddedBrowser.NavigationStarting += EmbeddedBrowser_NavigationStarting;
			_embeddedBrowser.NavigationCompleted += EmbeddedBrowser_NavigationCompleted;
			if (!string.IsNullOrWhiteSpace(embeddedHtml))
			{
				_embeddedBrowser.NavigateToString(embeddedHtml);
			}
			else
			{
				if (link.Host.Contains("shacknews.com", StringComparison.Ordinal))
				{
					await _embeddedBrowser.NavigateWithShackLogin(link, AuthManager).ConfigureAwait(true);
				}
				else
				{
					_embeddedBrowser.Navigate(link);
				}
			}
		}

		private async void EmbeddedBrowser_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
		{
			BrowserLoadingIndicator.Visibility = Visibility.Visible;
			BrowserLoadingIndicator.IsActive = true;
			if (args.Uri is null) return;

			var postId = AppLaunchHelper.GetShackPostId(args.Uri);
			if (postId != null)
			{
				await CloseEmbeddedBrowser().ConfigureAwait(true);
				OpenThreadTab(postId.Value);
				args.Cancel = true;
			}
		}

		private async void EmbeddedBrowser_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
		{
			if (args.Uri != null && args.Uri.Host.Contains("shacknews.com", StringComparison.Ordinal))
			{
				var ret =
				await sender.InvokeScriptAsync("eval", new[]
				{
							@"(function()
								 {
									  function updateHrefs() {
											var hyperlinks = document.getElementsByClassName('permalink');
											for(var i = 0; i < hyperlinks.length; i++)
											{
												 hyperlinks[i].setAttribute('target', '_self');
											}
									  }

									  var target = document.getElementById('page');
									  if(target !== undefined) {
											const observer = new MutationObserver(updateHrefs);
											observer.observe(target, { childList: true, subtree: true });
									  }   
								 })()"
				});
			}
			BrowserLoadingIndicator.IsActive = false;
			BrowserLoadingIndicator.Visibility = Visibility.Collapsed;
		}

		private void Shell_KeyDown(CoreWindow sender, KeyEventArgs args)
		{
			switch (args.VirtualKey)
			{
				case VirtualKey.Q:
					if (Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down) && Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down))
					{
						Flyout.SetAttachedFlyout((FrameworkElement)NavView.SettingsItem, (Flyout)Resources["QuickSettingsFlyout"]);
						Flyout.ShowAttachedFlyout((FrameworkElement)NavView.SettingsItem);
					}
					break;
			}
		}

		private async void WebViewDismissKeyHandler(CoreWindow sender, KeyEventArgs args)
		{
			switch (args.VirtualKey)
			{
				case VirtualKey.Escape:
					if (EmbeddedViewer.Visibility == Visibility.Visible)
					{
						await CloseEmbeddedBrowser().ConfigureAwait(false);
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

		private bool LaunchShackThreadForUriIfNecessary(Uri link)
		{
			var postId = AppLaunchHelper.GetShackPostId(link);
			if (postId != null)
			{
				OpenThreadTab(postId.Value);
				return true;
			}
			return false;
		}

		private async void EmbeddedCloseClicked(object sender, RoutedEventArgs e)
		{
			await CloseEmbeddedBrowser().ConfigureAwait(false);
		}

		private async Task CloseEmbeddedBrowser()
		{
			await DebugLog.AddMessage("ShellEmbeddedBrowserClosed").ConfigureAwait(true);
			_keyBindingWindow.KeyDown -= WebViewDismissKeyHandler;
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
				await DebugLog.AddMessage("ShellEmbeddedBrowserShowFullBrowser").ConfigureAwait(true);
				await Launcher.LaunchUriAsync(_embeddedBrowserLink);
				await CloseEmbeddedBrowser().ConfigureAwait(true);
			}
		}

		private void CloseClipboardLinkPopupButtonClicked(object sender, RoutedEventArgs e)
		{
			LinkPopup.IsOpen = false;
		}

		private void OpenClipboardLinkTapped(object sender, TappedRoutedEventArgs e)
		{
			if (Settings.LastClipboardPostId != 0)
			{
				OpenThreadTab((int)Settings.LastClipboardPostId);
				LinkPopup.IsOpen = false;
			}
		}

		private void NavView_BackRequested(Microsoft.UI.Xaml.Controls.NavigationView _, Microsoft.UI.Xaml.Controls.NavigationViewBackRequestedEventArgs _1)
		{
			NavigateBack().ConfigureAwait(false);
		}

		private void AddQuickSettingsToNav()
		{
			CoreApplication.MainView.CoreWindow.Dispatcher.RunOnUiThreadAndWait(CoreDispatcherPriority.Low, () =>
			{
				var frameworkSettings = (FrameworkElement)NavView.SettingsItem;
				frameworkSettings.ContextFlyout = (Flyout)Resources["QuickSettingsFlyout"];
				ToolTipService.SetToolTip(frameworkSettings, "Settings\r\n\r\nPress Ctrl+Shift+Q or right click for quick settings.");
			}).ConfigureAwait(false).GetAwaiter().GetResult();
		}

		private void NavViewLoaded(object _, RoutedEventArgs _1)
		{
			AddQuickSettingsToNav();
			//This is really sketchy.
			// There doesn't seem to be an event that I can reliably hook into when the displaymode changes (or ViewState)
			// that guarantees the new settings item will be shown by the time the code runs.
			// So, we're just going to wait and hope it's been added within 500ms.
			// Low chance that the user is able to resize the window and then invoke quick settings that fast.
			NavView.DisplayModeChanged += (_, _1) => Task.Run(() => { Task.Delay(500); AddQuickSettingsToNav(); });
		}
	}
}
