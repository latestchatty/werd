using Autofac;
using Common;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Werd.Common;
using Werd.Controls;
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
using Windows.UI.Xaml.Media;
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
		ShellTabView _currentlyDisplayedView;
		CoreWindow _keyBindingWindow;
		readonly DispatcherTimer _popupTimer = new DispatcherTimer();
		DateTime _linkPopupExpireTime;
		TabViewItem _lastSelectedTab;

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

		private CortexManager npcCortexManager;
		public CortexManager CortexManager
		{
			get => npcCortexManager;
			set => SetProperty(ref npcCortexManager, value);
		}

		private AppSettings npcSettings;
		public AppSettings Settings
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
			Settings = _container.Resolve<AppSettings>();
			ChattyManager = _container.Resolve<ChattyManager>();
			ConnectionStatus = _container.Resolve<NetworkConnectionStatus>();
			CortexManager = _container.Resolve<CortexManager>();
			ConnectionStatus.PropertyChanged += ConnectionStatus_PropertyChanged;
			Settings.PropertyChanged += Settings_PropertyChanged;
			Application.Current.UnhandledException += UnhandledAppException;

			var titleBar = CoreApplication.GetCurrentView().TitleBar;
			titleBar.ExtendViewIntoTitleBar = true;
			titleBar.LayoutMetricsChanged += TitleBar_LayoutMetricsChanged;

			//Don't really need to unsubscribe to this because there's only ever one shell and it should last the lifetime of the application.
			CoreWindow.GetForCurrentThread().KeyDown += Shell_KeyDown;
			Window.Current.Activated += WindowActivated;

			FocusManager.GettingFocus += FocusManager_GettingFocus;
			FocusManager.LosingFocus += FocusManager_LosingFocus;

			LoadChattyTab();
			//NavigateToTag(initialNavigation).ConfigureAwait(true).GetAwaiter().GetResult();
		}

		private void ShellLoaded(object sender, RoutedEventArgs e)
		{
			SetupTitleBar();
		}

		private void TitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
		{
			SetupTitleBar(sender);
		}

		private void SetupTitleBar(CoreApplicationViewTitleBar sender = null, CoreWindowActivationState activaitonState = CoreWindowActivationState.PointerActivated)
		{
			if (sender is null) { sender = CoreApplication.GetCurrentView().TitleBar; }

			//Get the element in the tabview that will be the system drag handler.
			var tabContainerBackground = tabView.RecursiveFindControlNamed<Grid>("ShadowReceiver");
			tabContainerBackground.Background = new AcrylicBrush()
			{
				BackgroundSource = AcrylicBackgroundSource.HostBackdrop,
				TintColor = (Windows.UI.Color)Application.Current.Resources["SystemAccentColor"],
				Opacity = activaitonState == CoreWindowActivationState.Deactivated ? .1 : .5,
				FallbackColor = (Windows.UI.Color)Application.Current.Resources["SystemAccentColor"]
			};
			Window.Current.SetTitleBar(tabContainerBackground);

			HeaderTitlePadding.Width = sender.SystemOverlayLeftInset;
			FooterTitlePadding.Width = new GridLength(sender.SystemOverlayRightInset);

			// Can't get this to work, it would be nice if it did.
			//Brush headerForegroundBrush = new SolidColorBrush(Settings.Theme.WindowTitleForegroundColor);
			//if (activaitonState == CoreWindowActivationState.Deactivated)
			//{
			//	headerForegroundBrush = new SolidColorBrush(Settings.Theme.WindowTitleForegroundColorInactive);
			//}
			//Application.Current.Resources["TabViewItemHeaderForeground"] = headerForegroundBrush;
			//Application.Current.Resources["TabViewItemHeaderForegroundSelected"] = headerForegroundBrush;
			//NavView.Foreground = headerForegroundBrush;

			var titleBar = ApplicationView.GetForCurrentView().TitleBar;
			titleBar.ButtonBackgroundColor = titleBar.BackgroundColor = titleBar.InactiveBackgroundColor = titleBar.ButtonInactiveBackgroundColor = Windows.UI.Colors.Transparent;
			titleBar.ButtonForegroundColor = titleBar.ForegroundColor = Settings.Theme.WindowTitleForegroundColor;
			titleBar.InactiveForegroundColor = titleBar.ButtonInactiveForegroundColor = Settings.Theme.WindowTitleForegroundColorInactive;
		}

		private void LoadChattyTab()
		{
			var existingContent = ChattyTabItem.Content as Frame;
			if (existingContent != null)
			{
				var existingShellTabView = existingContent.Content as ShellTabView;
				if (existingShellTabView != null)
				{
					existingShellTabView.LinkClicked -= Sv_LinkClicked;
					existingShellTabView.ShellMessage -= Sv_ShellMessage;
				}
			}
			var f = new Frame();
			f.Navigate(Settings.UseMainDetail ? typeof(Chatty) : typeof(InlineChattyFast), new ChattyNavigationArgs(_container));
			var sv = f.Content as ShellTabView;
			sv.LinkClicked += Sv_LinkClicked;
			sv.ShellMessage += Sv_ShellMessage;
			sv.Shell = this;
			ChattyTabItem.Content = f;
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
			SetupTitleBar(activaitonState: e.WindowActivationState);
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

		public void NavigateToPage(Type page, object arguments, bool openInBackground = false)
		{
			if (!NavigationHelper.CanOpenMultipleInstances(page))
			{
				//Determine if we already have a tab of that type open
				foreach (var existing in tabView.TabItems)
				{
					if (((existing as TabViewItem)?.Content as Frame)?.Content.GetType() == page)
					{
						tabView.SelectedItem = existing;
						return; //Select the tab and bail.
					}
				}
			}
			var f = new Frame();
			f.Navigate(page, arguments);
			var tab = new TabViewItem
			{
				HeaderTemplate = (DataTemplate)this.Resources["TabHeaderTemplate"]
			};
			tab.Content = f;
			var sv = f.Content as ShellTabView;
			tabView.TabItems.Add(tab);
			if (sv != null)
			{
				tab.DataContext = sv;
				sv.LinkClicked += Sv_LinkClicked;
				sv.ShellMessage += Sv_ShellMessage;
				sv.Shell = this;
			}
			if (!openInBackground) { tabView.SelectedItem = tab; }
		}

		public async Task OpenThreadTab(int postId, bool openInBackground = false)
		{
			// This removes the thread from the active chatty.
			await ChattyManager.FindOrAddThreadByAnyPostId(postId, true).ConfigureAwait(true);

			NavigateToPage(typeof(SingleThreadView), new Tuple<IContainer, int, int>(_container, postId, postId), openInBackground);
		}

		private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName.Equals(nameof(AppSettings.ThemeName)))
			{
				SetupTitleBar();
			}

			if (e.PropertyName.Equals(nameof(AppSettings.UseMainDetail)))
			{
				LoadChattyTab();
			}
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
			ShowEmbeddedLink(e.Link, e.OpenInBackground);
		}

		private async void MainMenuItemClicked(object sender, RoutedEventArgs e)
		{
			if ((sender as FrameworkElement)?.Tag is null) return;
			await NavigateToTag((sender as FrameworkElement).Tag.ToString()).ConfigureAwait(true);
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
					NavigateToPage(typeof(CustomSearchWebView), new WebViewNavigationArgs(_container, new Uri("https://shacknews.com/search?q=&type=4")));
					break;
				case "MYPOSTSSEARCH":
					NavigateToPage(typeof(MyPostsSearchWebView), new WebViewNavigationArgs(_container, new Uri($"https://www.shacknews.com/search?chatty=1&type=4&chatty_term=&chatty_user={AuthManager.UserName}&chatty_author=&chatty_filter=all&result_sort=postdate_desc")));
					break;
				case "REPLIESTOMESEARCH":
					NavigateToPage(typeof(RepliesToMeSearchWebView), new WebViewNavigationArgs(_container, new Uri($"https://www.shacknews.com/search?chatty=1&type=4&chatty_term=&chatty_user=&chatty_author={AuthManager.UserName}&chatty_filter=all&result_sort=postdate_desc")));
					break;
				case "VANITYSEARCH":
					NavigateToPage(typeof(VanitySearchWebView), new WebViewNavigationArgs(_container, new Uri($"https://www.shacknews.com/search?chatty=1&type=4&chatty_term={AuthManager.UserName}&chatty_user=&chatty_author=&chatty_filter=all&result_sort=postdate_desc")));
					break;
				case "TAGS":
					NavigateToPage(typeof(TagsWebView), new WebViewNavigationArgs(_container, new Uri("https://www.shacknews.com/tags-user")));
					break;
				case "MODTOOLS":
					NavigateToPage(typeof(ModToolsWebView), new WebViewNavigationArgs(_container, new Uri("https://www.shacknews.com/moderators/ban-tool")));
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
					//NavigateToPage(typeof(CortexCreateWebView), new Views.NavigationArgs.WebViewNavigationArgs(_container, new Uri("https://www.shacknews.com/cortex/create")));
					await Launcher.LaunchUriAsync(new Uri("https://www.shacknews.com/cortex/create"));
					break;
				case "CORTEXFEED":
					NavigateToPage(typeof(CortexFeedWebView), new WebViewNavigationArgs(_container, new Uri("https://www.shacknews.com/cortex/my-feed")));
					break;
				case "CORTEXALLPOSTS":
					NavigateToPage(typeof(CortexAllPostsWebView), new WebViewNavigationArgs(_container, new Uri("https://www.shacknews.com/cortex/articles")));
					break;
				case "CORTEXMYPOSTS":
					NavigateToPage(typeof(CortexMyPostsWebView), new WebViewNavigationArgs(_container, new Uri("https://www.shacknews.com/cortex/my-articles")));
					break;
				case "CORTEXDRAFTS":
					//NavigateToPage(typeof(CortexDraftsWebView), new Views.NavigationArgs.WebViewNavigationArgs(_container, new Uri("https://www.shacknews.com/cortex/my-drafts")));
					await Launcher.LaunchUriAsync(new Uri("https://www.shacknews.com/cortex/my-drafts"));
					break;
				case "CORTEXFOLLOWING":
					NavigateToPage(typeof(CortexFollowingWebView), new WebViewNavigationArgs(_container, new Uri("https://www.shacknews.com/cortex/follow")));
					break;
				case "SETTINGS":
					NavigateToPage(typeof(SettingsView), _container);
					break;
			}
		}

		private async void ShowEmbeddedLink(Uri link, bool openInBackground = false)
		{
			await DebugLog.AddMessage($"Attempting to process url {link}").ConfigureAwait(true);
			link = await LaunchExternalAppOrGetEmbeddedUri(link).ConfigureAwait(true);
			if (link == null) //it was handled, no more to do.
			{
				return;
			}

			if (await LaunchShackThreadForUriIfNecessary(link, openInBackground).ConfigureAwait(true))
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

			if (!string.IsNullOrWhiteSpace(embeddedHtml))
			{
				NavigateToPage(typeof(ShackWebView), new WebViewNavigationArgs(_container, embeddedHtml), openInBackground);
			}
			else
			{
				NavigateToPage(typeof(ShackWebView), new WebViewNavigationArgs(_container, link), openInBackground);
			}
		}

		private void Shell_KeyDown(CoreWindow sender, KeyEventArgs args)
		{
			switch (args.VirtualKey)
			{
				case VirtualKey.Q:
					if (Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down) && Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down))
					{
						Flyout.SetAttachedFlyout((FrameworkElement)MenuButton, (Flyout)Resources["QuickSettingsFlyout"]);
						Flyout.ShowAttachedFlyout((FrameworkElement)MenuButton);
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

		private async Task<bool> LaunchShackThreadForUriIfNecessary(Uri link, bool openInBackground)
		{
			var postId = AppLaunchHelper.GetShackPostId(link);
			if (postId != null)
			{
				await OpenThreadTab(postId.Value, openInBackground).ConfigureAwait(false);
				return true;
			}
			return false;
		}

		private void CloseClipboardLinkPopupButtonClicked(object sender, RoutedEventArgs e)
		{
			LinkPopup.IsOpen = false;
		}

		private async void OpenClipboardLinkTapped(object sender, TappedRoutedEventArgs e)
		{
			if (Settings.LastClipboardPostId != 0)
			{
				await OpenThreadTab((int)Settings.LastClipboardPostId).ConfigureAwait(true);
				LinkPopup.IsOpen = false;
			}
		}

		//private void AddQuickSettingsToNav()
		//{
		//	CoreApplication.MainView.CoreWindow.Dispatcher.RunOnUiThreadAndWait(CoreDispatcherPriority.Low, () =>
		//	{
		//		var frameworkSettings = (FrameworkElement)TabHeaderButton;
		//		frameworkSettings.ContextFlyout = (Flyout)Resources["QuickSettingsFlyout"];
		//		ToolTipService.SetToolTip(frameworkSettings, "Settings\r\n\r\nPress Ctrl+Shift+Q or right click for quick settings.");
		//	}).ConfigureAwait(false).GetAwaiter().GetResult();
		//}


		private void NewTabKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
		{
			ShowNewTabFlyout();
		}

		private void CloseSelectedTabKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
		{
			var selectedTab = tabView.SelectedItem as TabViewItem;
			if (selectedTab is null) return;
			// Only remove the selected tab if it can be closed.
			if (selectedTab.IsClosable) CloseTab(selectedTab);
			args.Handled = true;
		}

		private void NavigateToNumberedTabKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
		{
			int tabToSelect = 0;

			switch (sender.Key)
			{
				case VirtualKey.Number1:
					tabToSelect = 0;
					break;
				case VirtualKey.Number2:
					tabToSelect = 1;
					break;
				case VirtualKey.Number3:
					tabToSelect = 2;
					break;
				case VirtualKey.Number4:
					tabToSelect = 3;
					break;
				case VirtualKey.Number5:
					tabToSelect = 4;
					break;
				case VirtualKey.Number6:
					tabToSelect = 5;
					break;
				case VirtualKey.Number7:
					tabToSelect = 6;
					break;
				case VirtualKey.Number8:
					tabToSelect = 7;
					break;
				case VirtualKey.Number9:
					// Select the last tab
					tabToSelect = tabView.TabItems.Count - 1;
					break;
			}

			// Only select the tab if it is in the list
			if (tabToSelect < tabView.TabItems.Count)
			{
				tabView.SelectedIndex = tabToSelect;
			}
		}

		private void AddTabClicked(TabView sender, object args)
		{
			ShowNewTabFlyout();
		}

		private async void TabSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.RemovedItems.Count > 1 || e.RemovedItems.Count > 1)
			{
				await DebugLog.AddMessage("More than one selected or deselected tab. Not good.").ConfigureAwait(true);
			}

			//Set everything to not having focus, then we'll re-enable just what does after.
			foreach (var stv in tabView.TabItems.Select(tvi => (((tvi as TabViewItem)?.Content as Frame)?.Content as SingleThreadView)))
			{
				if (stv is null) { continue; }
				stv.HasFocus = false;
			}

			foreach (var r in e.RemovedItems)
			{
				var rt = r as TabViewItem;
				if (rt is null) continue;
				_lastSelectedTab = rt;
				if ((rt.Content as Frame)?.Content is SingleThreadView sil)
				{
					await ChattyManager.MarkCommentThreadRead(sil.CommentThread).ConfigureAwait(true);
				}
			}

			foreach (var r in e.AddedItems)
			{
				var rt = r as TabViewItem;
				if (rt is null) continue;
				var sv = (rt.Content as Frame)?.Content as ShellTabView;
				if (sv != null)
				{
					sv.HasFocus = true;
				}
			}
		}

		private void CloseTabClicked(TabView sender, TabViewTabCloseRequestedEventArgs args)
		{
			CloseTab(args.Tab);
		}

		public void CloseTab(TabViewItem tab)
		{
			var sv = ((tab.Content as Frame)?.Content) as ShellTabView;
			if (sv != null)
			{
				sv.LinkClicked -= Sv_LinkClicked;
				sv.ShellMessage -= Sv_ShellMessage;
			}

			if (sv is ShackWebView)
			{
				((ShackWebView)sv).CloseWebView();
			}

			if (sv is SingleThreadView)
			{
				var thread = (sv as SingleThreadView).CommentThread;
				// This is dangerous to do in UI since something else could use this in the future but here we are and I just want tabs working.
				// Should probably do something similar to this with pinned stuff at some point too.
				if (!thread.IsPinned) thread.Invisible = false; // Since it's no longer open in a tab we can release it from the active chatty on the next refresh.
			}

			if (tabView.TabItems.Contains(_lastSelectedTab))
			{
				tabView.SelectedItem = _lastSelectedTab;
			}
			tabView.TabItems.Remove(tab);
		}

		private void ShowNewTabFlyout()
		{
			var button = tabView.FindDescendantByName("AddButton");
			var flyout = Resources["addTabFlyout"] as Flyout;
			flyout.ShowAt(button);
		}

		private async void SubmitAddThreadClicked(object sender, RoutedEventArgs _)
		{
			await AddTabViaTextBox(sender).ConfigureAwait(true);
		}

		private async void AddThreadTextBoxKeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key == VirtualKey.Enter)
			{
				await AddTabViaTextBox(sender).ConfigureAwait(true);
			}
		}

		private async Task AddTabViaTextBox(object sender)
		{
			try
			{
				SubmitAddThreadButton.IsEnabled = false;
				if (!int.TryParse(AddThreadTextBox.Text.Trim(), out int postId))
				{
					if (!ChattyHelper.TryGetThreadIdFromUrl(AddThreadTextBox.Text.Trim(), out postId))
					{
						try
						{
							ShowEmbeddedLink(await UriHelper.MakeWebViewSafeUriOrSearch(AddThreadTextBox.Text).ConfigureAwait(true));
						}
						catch (Exception ex)
						{
							await DebugLog.AddException(string.Empty, ex).ConfigureAwait(true);
							Sv_ShellMessage(this, new ShellMessageEventArgs("Error occurred adding tab: " + Environment.NewLine + ex.Message, ShellMessageType.Error));
						}
						AddThreadTextBox.Text = string.Empty;

						return;
					}
				}

				await OpenThreadTab(postId).ConfigureAwait(true);

				AddThreadTextBox.Text = string.Empty;
			}
			catch (Exception ex)
			{
				await DebugLog.AddException(string.Empty, ex).ConfigureAwait(true);
				Sv_ShellMessage(this, new ShellMessageEventArgs("Error occurred adding tabbed thread: " + Environment.NewLine + ex.Message, ShellMessageType.Error));
			}
			finally
			{
				var parentPopup = (sender as FrameworkElement)?.FindParent<Windows.UI.Xaml.Controls.Primitives.Popup>();
				if (parentPopup != null)
				{
					parentPopup.IsOpen = false;
				}
				SubmitAddThreadButton.IsEnabled = true;
			}
		}

		private async void NewMessagesButtonClicked(object sender, RoutedEventArgs e)
		{
			await NavigateToTag("message").ConfigureAwait(true);
		}
	}
}
