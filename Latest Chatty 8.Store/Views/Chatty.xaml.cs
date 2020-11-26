using Autofac;
using Common;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using Microsoft.UI.Xaml.Controls;
using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Werd.Common;
using Werd.Controls;
using Werd.DataModel;
using Werd.Managers;
using Werd.Settings;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using IContainer = Autofac.IContainer;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237
namespace Werd.Views
{
	/// <summary>
	/// A basic page that provides characteristics common to most applications.
	/// </summary>
	public sealed partial class Chatty
	{
		private CoreWindow _keyBindWindow;
		private bool _shortcutKeysEnabled = true;

		public override string ViewTitle => "Chatty";

		public override event EventHandler<LinkClickedEventArgs> LinkClicked;

		public override event EventHandler<ShellMessageEventArgs> ShellMessage;

		private IContainer _container;

		private LatestChattySettings npcSettings;
		private LatestChattySettings Settings
		{
			get => npcSettings;
			set => SetProperty(ref npcSettings, value);
		}

		private CommentThread npcSelectedThread;
		public CommentThread SelectedThread
		{
			get => npcSelectedThread;
			set => SetProperty(ref npcSelectedThread, value);
		}

		private bool npcShowSearch;
		private bool ShowSearch
		{
			get => npcShowSearch;
			set => SetProperty(ref npcShowSearch, value);
		}

		private CortexManager npcCortexManager;
		private CortexManager CortexManager
		{
			get => npcCortexManager;
			set => SetProperty(ref npcCortexManager, value);
		}

		public Chatty()
		{
			InitializeComponent();
		}

		private ChattyManager _chattyManager;
		public ChattyManager ChattyManager
		{
			get => _chattyManager;
			set => SetProperty(ref _chattyManager, value);
		}
		private ThreadMarkManager _markManager;
		private AuthenticationManager _authManager;
		private bool _preventNextThreadSelectionChangeFromMarkingRead;
		private IObservable<System.Reactive.EventPattern<TextChangedEventArgs>> _searchTextChangedEvent;
		private IDisposable _searchTextChangedSubscription;

		private async void ChattyListSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count == 1)
			{
				CommentThread ct = e.AddedItems[0] as CommentThread;
				if (ct == null) return;
				ThreadList.ScrollIntoView(ct);
			}

			if (e.RemovedItems.Count > 0)
			{
				CommentThread ct = e.RemovedItems[0] as CommentThread;
				//This is really janky and doesn't take threading or anything into account.
				// It will probably work in most cases, though.
				if (!_preventNextThreadSelectionChangeFromMarkingRead)
				{
					await _chattyManager.MarkCommentThreadRead(ct).ConfigureAwait(false);
				}
				_preventNextThreadSelectionChangeFromMarkingRead = false;
				await _chattyManager.DeselectAllPostsForCommentThread(ct).ConfigureAwait(false);
			}
		}
		private async void MarkAllRead(object sender, RoutedEventArgs e)
		{
			await _chattyManager.MarkAllVisibleCommentsRead().ConfigureAwait(false);
		}

		private async void PinClicked(object sender, RoutedEventArgs e)
		{
			MenuFlyoutItem flyout = sender as MenuFlyoutItem;
			if (flyout == null) return;
			CommentThread commentThread = flyout.DataContext as CommentThread;
			if (commentThread == null) return;
			if (_markManager.GetMarkType(commentThread.Id) == MarkType.Pinned)
			{
				await _markManager.MarkThread(commentThread.Id, MarkType.Unmarked).ConfigureAwait(true);
			}
			else
			{
				await _markManager.MarkThread(commentThread.Id, MarkType.Pinned).ConfigureAwait(true);
			}
		}

		private async void CollapseClicked(object sender, RoutedEventArgs e)
		{
			MenuFlyoutItem flyout = sender as MenuFlyoutItem;
			if (flyout == null) return;
			CommentThread commentThread = flyout.DataContext as CommentThread;
			if (commentThread == null) return;
			if (_markManager.GetMarkType(commentThread.Id) == MarkType.Collapsed)
			{
				await _markManager.MarkThread(commentThread.Id, MarkType.Unmarked).ConfigureAwait(true);
			}
			else
			{
				await _markManager.MarkThread(commentThread.Id, MarkType.Collapsed).ConfigureAwait(true);
			}
		}

		private async void MarkThreadReadClicked(object sender, RoutedEventArgs e)
		{
			MenuFlyoutItem flyout = sender as MenuFlyoutItem;
			if (flyout == null) return;
			CommentThread commentThread = flyout.DataContext as CommentThread;
			if (commentThread == null) return;
			await ChattyManager.MarkCommentThreadRead(commentThread).ConfigureAwait(true);
		}

		private async void ChattyManager_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName.Equals(nameof(ChattyManager.IsFullUpdateHappening), StringComparison.Ordinal))
			{
				if (ChattyManager.IsFullUpdateHappening)
				{
					ThreadList.SelectNone();
					await SingleThreadControl.Close().ConfigureAwait(true);
				}
			}
		}

		private async void ReSortClicked(object sender, RoutedEventArgs e)
		{
			await DebugLog.AddMessage("Chatty-ResortClicked").ConfigureAwait(true);
			await ReSortChatty().ConfigureAwait(true);
		}

		private async Task ReSortChatty()
		{
			//TODO: Pin - SelectedThread = null;
			ThreadList.SelectNone();
			await SingleThreadControl.Close().ConfigureAwait(true);

			if (Settings.MarkReadOnSort)
			{
				await _chattyManager.MarkAllVisibleCommentsRead().ConfigureAwait(true);
			}
			await _chattyManager.CleanupChattyList().ConfigureAwait(true);
			ThreadList.ScrollToTop();
		}

		private void SearchKeyUp(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key == VirtualKey.Escape)
			{
				ShowSearch = false;
				if (FilterCombo.Items != null)
				{
					foreach (object item in FilterCombo.Items)
					{
						ComboBoxItem i = item as ComboBoxItem;
						if (i?.Tag != null && i.Tag.ToString().Equals("all", StringComparison.OrdinalIgnoreCase))
						{
							FilterCombo.SelectedItem = i;
							break;
						}
					}
				}

				SearchTextBox.Text = String.Empty;
			}
		}

		private void NewRootPostButtonClicked(object sender, RoutedEventArgs e)
		{
			ShowNewRootPost();
		}

		private async void FilterChanged(object sender, SelectionChangedEventArgs e)
		{
			if (ChattyManager == null) return;
			if (e.AddedItems.Count != 1) return;
			ComboBoxItem item = e.AddedItems[0] as ComboBoxItem;
			if (item == null) return;
			ChattyFilterType filter;
			string tagName = item.Tag.ToString();
			await DebugLog.AddMessage("Chatty-Filter-" + tagName).ConfigureAwait(true);
			switch (tagName)
			{
				case "news":
					filter = ChattyFilterType.News;
					break;
				case "new":
					filter = ChattyFilterType.New;
					break;
				case "has replies":
					filter = ChattyFilterType.HasReplies;
					break;
				case "participated":
					filter = ChattyFilterType.Participated;
					break;
				case "search":
					ShowSearch = true;
					await ChattyManager.SearchChatty(SearchTextBox.Text).ConfigureAwait(true);
					SearchTextBox.Focus(FocusState.Programmatic);
					return;
				case "collapsed":
					filter = ChattyFilterType.Collapsed;
					break;
				case "pinned":
					filter = ChattyFilterType.Pinned;
					break;
				case "cortex":
					filter = ChattyFilterType.Cortex;
					break;
				default:
					filter = ChattyFilterType.All;
					break;
			}
			ShowSearch = false;
			await ChattyManager.FilterChatty(filter).ConfigureAwait(true);
		}

		private async void SortChanged(object sender, SelectionChangedEventArgs e)
		{
			if (ChattyManager == null) return;
			if (e.AddedItems.Count != 1) return;
			ComboBoxItem item = e.AddedItems[0] as ComboBoxItem;
			if (item == null) return;
			ChattySortType sort;
			string tagName = item.Tag.ToString();
			await DebugLog.AddMessage("Chatty-Sort-" + tagName).ConfigureAwait(true);
			switch (tagName)
			{
				case "inf":
					sort = ChattySortType.Inf;
					break;
				case "lol":
					sort = ChattySortType.Lol;
					break;
				case "mostreplies":
					sort = ChattySortType.ReplyCount;
					break;
				case "hasnewreplies":
					sort = ChattySortType.HasNewReplies;
					break;
				case "participated":
					sort = ChattySortType.Participated;
					break;
				default:
					sort = ChattySortType.Default;
					break;
			}
			await ChattyManager.SortChatty(sort).ConfigureAwait(true);
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			_container = e.Parameter as IContainer;
			_container.Resolve<AuthenticationManager>();
			ChattyManager = _container.Resolve<ChattyManager>();
			_markManager = _container.Resolve<ThreadMarkManager>();
			Settings = _container.Resolve<LatestChattySettings>();
			CortexManager = _container.Resolve<CortexManager>();
			_authManager = _container.Resolve<AuthenticationManager>();
			_container.Resolve<INotificationManager>();
			_keyBindWindow = CoreWindow.GetForCurrentThread();
			_keyBindWindow.KeyDown += Chatty_KeyDown;
			_keyBindWindow.KeyUp += Chatty_KeyUp;
			_searchTextChangedEvent = Observable.FromEventPattern<TextChangedEventHandler, TextChangedEventArgs>(h => SearchTextBox.TextChanged += h, h => SearchTextBox.TextChanged -= h);
			//Debounce filter changes otherwise the UI gets bogged down
			_searchTextChangedSubscription = _searchTextChangedEvent
				.Select(_ => SearchTextBox.Text)
				.DistinctUntilChanged()
				.Throttle(TimeSpan.FromSeconds(.5))
				.Select(s =>
					Observable.FromAsync(async () =>
						await Dispatcher.RunOnUiThreadAndWait(CoreDispatcherPriority.Normal, () =>
							ChattyManager.SearchChatty(s).ConfigureAwait(false).GetAwaiter().GetResult()).ConfigureAwait(false)))
				.Concat()
				.Subscribe();
			ChattyManager.PropertyChanged += ChattyManager_PropertyChanged;
			EnableShortcutKeys();
		}

		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			base.OnNavigatingFrom(e);
			ChattyManager.PropertyChanged -= ChattyManager_PropertyChanged;
			DisableShortcutKeys();
			if (_keyBindWindow != null)
			{
				_keyBindWindow.KeyDown -= Chatty_KeyDown;
				_keyBindWindow.KeyUp -= Chatty_KeyUp;
			}

			_searchTextChangedSubscription?.Dispose();
		}

		private bool _ctrlDown;

		private async void Chatty_KeyDown(CoreWindow sender, KeyEventArgs args)
		{
			try
			{
				if (!AppGlobal.ShortcutKeysEnabled || !_shortcutKeysEnabled)
				{
					return;
				}

				switch (args.VirtualKey)
				{
					case VirtualKey.Control:
						_ctrlDown = true;
						break;
					case VirtualKey.F5:
						await DebugLog.AddMessage("Chatty-F5Pressed").ConfigureAwait(true);
						await ReSortChatty().ConfigureAwait(true);
						break;
					case VirtualKey.J:
						if (!_ctrlDown)
						{
							ThreadList.SelectPreviousThread();
						}
						break;
					case VirtualKey.K:
						if (!_ctrlDown)
						{
							ThreadList.SelectNextThread();
						}
						break;
					case VirtualKey.P:
						if (!_ctrlDown)
						{
							await DebugLog.AddMessage("Chatty-PPressed").ConfigureAwait(true);
							if (SelectedThread != null)
							{
								await _markManager.MarkThread(SelectedThread.Id, _markManager.GetMarkType(SelectedThread.Id) != MarkType.Pinned ? MarkType.Pinned : MarkType.Unmarked).ConfigureAwait(true);
							}
						}
						break;
				}
			}
			catch (Exception e)
			{
				await DebugLog.AddException($"Exception for key {args.VirtualKey}", e).ConfigureAwait(false);
			}
		}

		private async void Chatty_KeyUp(CoreWindow sender, KeyEventArgs args)
		{
			try
			{
				if (!AppGlobal.ShortcutKeysEnabled || !_shortcutKeysEnabled)
				{
					return;
				}

				switch (args.VirtualKey)
				{
					case VirtualKey.Control:
						_ctrlDown = false;
						break;
					case VirtualKey.N:
						if (_ctrlDown)
						{
							ShowNewRootPost();
						}
						break;
					default:
						switch ((int)args.VirtualKey)
						{
							case 191:
								if (ShowSearch)
								{
									SearchTextBox.Focus(FocusState.Programmatic);
								}
								else
								{
									if (FilterCombo.Items != null)
									{
										foreach (object item in FilterCombo.Items)
										{
											ComboBoxItem i = item as ComboBoxItem;
											if (i != null && (i.Tag != null && i.Tag.ToString().Equals("search",
																  StringComparison.OrdinalIgnoreCase)))
											{
												FilterCombo.SelectedItem = i;
												break;
											}
										}
									}
								}
								break;
						}
						break;
				}
			}
			catch (Exception e)
			{
				await DebugLog.AddException(string.Empty, e).ConfigureAwait(false);
			}
		}

		private void ShowNewRootPost()
		{
			Frame.Navigate(typeof(NewRootPostView), _container);
		}

		private void DisableShortcutKeys()
		{
			AppGlobal.ShortcutKeysEnabled = false;
		}

		private void EnableShortcutKeys()
		{
			AppGlobal.ShortcutKeysEnabled = true;
		}

		private void SearchTextBoxLostFocus(object sender, RoutedEventArgs e)
		{
			EnableShortcutKeys();
		}

		private void SearchTextBoxGotFocus(object sender, RoutedEventArgs e)
		{
			DisableShortcutKeys();
		}

		private void InlineControlShellMessage(object sender, ShellMessageEventArgs e)
		{
			ShellMessage?.Invoke(sender, e);
		}

		private async void ChattyPullRefresh(object sender, Windows.UI.Xaml.Controls.RefreshRequestedEventArgs e)
		{
			using (Windows.Foundation.Deferral _ = e.GetDeferral())
			{
				await ReSortChatty().ConfigureAwait(true);
			}
		}

		#region Tabs
		private async Task AddTabByPostId(int postId, bool selectNewTab = true)
		{
			//When we add it as a tab, it's going to get removed from the active chatty, which will cause a selection change
			// So we'll prevent the thread from being marked read since we might just be opening it to read at a later time without having
			// actually read everything.
			_preventNextThreadSelectionChangeFromMarkingRead = true;
			var thread = await ChattyManager.FindOrAddThreadByAnyPostId(postId, true).ConfigureAwait(true);
			if (thread == null)
			{
				ShellMessage?.Invoke(this,
					new ShellMessageEventArgs($"Couldn't load thread for id {postId}.",
						ShellMessageType.Error));
				return;
			}

			var tab = new Microsoft.UI.Xaml.Controls.TabViewItem()
			{
				HeaderTemplate = (DataTemplate)this.Resources["TabHeaderTemplate"]
			};

			var singleThreadControl = new SingleThreadInlineControl();
			singleThreadControl.Margin = new Thickness(12, 12, 0, 0);
			singleThreadControl.DataContext = thread;
			singleThreadControl.LinkClicked += HandleLinkClicked;
			singleThreadControl.ShellMessage += ShellMessage;
			singleThreadControl.HorizontalAlignment = HorizontalAlignment.Stretch;
			singleThreadControl.VerticalAlignment = VerticalAlignment.Stretch;
			singleThreadControl.ShortcutKeysEnabled = false; // Disable by default until it gets focus.

			tab.Content = singleThreadControl;

			tab.DataContext = thread;
			tabView.TabItems.Add(tab);
			if (selectNewTab)
			{
				tabView.SelectedItem = tab;
			}
			await DebugLog.AddMessage($"Adding tab for post {postId}").ConfigureAwait(false);
		}

		private void ShowNewTabFlyout()
		{
			var button = tabView.FindDescendantByName("AddButton");
			var flyout = Resources["addTabFlyout"] as Flyout;
			flyout.ShowAt(button);
		}

		private void CloseTab(TabViewItem tab)
		{
			var content = tab.Content as SingleThreadInlineControl;
			if (content is null) return;
			//Unnecessary since it's xaml now?
			content.ShellMessage -= ShellMessage;
			content.LinkClicked -= HandleLinkClicked;
			tabView.TabItems.Remove(tab);
			var thread = content.DataContext as CommentThread;
			if (thread != null)
			{
				// This is dangerous to do in UI since something else could use this in the future but here we are and I just want tabs working.
				// Should probably do something similar to this with pinned stuff at some point too.
				if (!thread.IsPinned) thread.Invisible = false; // Since it's no longer open in a tab we can release it from the active chatty on the next refresh.
			}
		}

		private async void SubmitAddThreadClicked(object sender, RoutedEventArgs e)
		{
			try
			{
				SubmitAddThreadButton.IsEnabled = false;
				if (!int.TryParse(AddThreadTextBox.Text.Trim(), out int postId))
				{
					if (!ChattyHelper.TryGetThreadIdFromUrl(AddThreadTextBox.Text.Trim(), out postId))
					{
						return;
					}
				}

				await AddTabByPostId(postId).ConfigureAwait(true);

				AddThreadTextBox.Text = string.Empty;
			}
			catch (Exception ex)
			{
				await DebugLog.AddException(string.Empty, ex).ConfigureAwait(true);
				ShellMessage?.Invoke(this, new ShellMessageEventArgs("Error occurred adding tabbed thread: " + Environment.NewLine + ex.Message, ShellMessageType.Error));
			}
			finally
			{
				SubmitAddThreadButton.IsEnabled = true;
			}
		}

		private void AddTabClicked(Microsoft.UI.Xaml.Controls.TabView _, object _1)
		{
			ShowNewTabFlyout();
		}

		private void CloseTabClicked(Microsoft.UI.Xaml.Controls.TabView _, Microsoft.UI.Xaml.Controls.TabViewTabCloseRequestedEventArgs args)
		{
			CloseTab(args.Tab);
		}

		private async void TabSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			foreach (var r in e.RemovedItems)
			{
				var rt = r as TabViewItem;
				if (rt is null) continue;
				var sil = rt.Content as SingleThreadInlineControl;
				if (sil is null)
				{
					if (rt.Content is Grid) _shortcutKeysEnabled = false;
					continue;
				}
				var commentThread = sil.DataContext as CommentThread;
				if (commentThread != null)
				{
					await _chattyManager.MarkCommentThreadRead(commentThread).ConfigureAwait(true);
				}
				sil.ShortcutKeysEnabled = false;
			}
			foreach (var r in e.AddedItems)
			{
				var rt = r as TabViewItem;
				if (rt is null) continue;
				var sil = rt.Content as SingleThreadInlineControl;
				if (sil is null)
				{
					if (rt.Content is Grid) _shortcutKeysEnabled = true;
					continue;
				}
				sil.ShortcutKeysEnabled = true;
			}
		}

		private void NewTabKeyboardAccelerator_Invoked(KeyboardAccelerator _, KeyboardAcceleratorInvokedEventArgs _1)
		{
			ShowNewTabFlyout();
		}

		private void CloseSelectedTabKeyboardAccelerator_Invoked(KeyboardAccelerator _, KeyboardAcceleratorInvokedEventArgs args)
		{
			var selectedTab = tabView.SelectedItem as TabViewItem;
			if (selectedTab is null) return;
			// Only remove the selected tab if it can be closed.
			if (selectedTab.IsClosable) CloseTab(selectedTab);
			args.Handled = true;
		}

		private void NavigateToNumberedTabKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs _)
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

		private async void HandleLinkClicked(object sender, LinkClickedEventArgs e)
		{
			var focusNewTab = !Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			if (ChattyHelper.TryGetThreadIdFromUrl(e.Link.ToString(), out var postId))
			{
				await AddTabByPostId(postId, focusNewTab).ConfigureAwait(false);
			}
			else
			{
				LinkClicked?.Invoke(sender, e);
			}
		}

		private async void AddThreadTabClicked(object sender, AddThreadTabEventArgs e)
		{
			await AddTabByPostId(e.Thread.Id, !e.AddInBackground).ConfigureAwait(true);
			await SingleThreadControl.Close().ConfigureAwait(true);
		}
		#endregion
	}
}
