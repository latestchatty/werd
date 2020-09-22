using Autofac;
using Common;
using Microsoft.Toolkit.Collections;
using Microsoft.Toolkit.Extensions;
using Microsoft.Toolkit.Uwp.UI.Animations;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Werd.Common;
using Werd.Controls;
using Werd.DataModel;
using Werd.Managers;
using Werd.Settings;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using IContainer = Autofac.IContainer;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237
namespace Werd.Views
{
	internal static class InlineChattyXamlHelpers
	{
		internal static string GetTabPreviewText(string preview)
		{
			return preview.Truncate(30, true);
		}

		internal static string GetTabIcons(bool hasNewReplies, bool hasNewRepliesToUser)
		{
			return (hasNewReplies ? "\uE735 " : "") + (hasNewRepliesToUser ? "\uE8BD " : "");
		}
	}
	/// <summary>
	/// A basic page that provides characteristics common to most applications.
	/// </summary>
	public sealed partial class InlineChattyFast
	{
		private CoreWindow _keyBindWindow;
		bool _scrollingDown;
		Windows.UI.Xaml.Controls.Primitives.ScrollBar _threadListScrollBar;

		public override string ViewTitle => "Chatty";

		public override event EventHandler<Common.LinkClickedEventArgs> LinkClicked;

		public override event EventHandler<ShellMessageEventArgs> ShellMessage;


		private IContainer _container;
		private AuthenticationManager _authManager;
		private MessageManager _messageManager;
		private IgnoreManager _ignoreManager;
		private int _threadNavigationAnchorIndex;
		private IObservable<System.Reactive.EventPattern<TextChangedEventArgs>> _searchTextChangedEvent;
		private IDisposable _searchTextChangedSubscription;

		private Comment _selectedComment;
		private Comment SelectedComment
		{
			get => _selectedComment;
			set => SetProperty(ref _selectedComment, value);
		}
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

		CollectionViewSource GroupedChattyView;

		public InlineChattyFast()
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

		private bool _shortcutKeysEnabled = true;

		private async void MarkAllRead(object sender, RoutedEventArgs e)
		{
			await _chattyManager.MarkAllVisibleCommentsRead().ConfigureAwait(true);
			await DebugLog.AddMessage("Chatty-MarkReadClicked").ConfigureAwait(true);
		}

		private async void ReSortClicked(object sender, RoutedEventArgs e)
		{
			await DebugLog.AddMessage("Chatty-ResortClicked").ConfigureAwait(true);
			await ReSortChatty().ConfigureAwait(true);
		}

		private async void ChattyPullRefresh(Windows.UI.Xaml.Controls.RefreshContainer _, Windows.UI.Xaml.Controls.RefreshRequestedEventArgs args)
		{
			using (Deferral _1 = args.GetDeferral())
			{
				await ReSortChatty().ConfigureAwait(true);
			}
		}

		private async Task ReSortChatty()
		{
			SelectedThread = null;
			//SingleThreadControl.DataContext = null;

			_commentsCurrentlyInView.Clear();
			if (Settings.MarkReadOnSort)
			{
				await _chattyManager.MarkAllVisibleCommentsRead().ConfigureAwait(true);
			}
			await _chattyManager.CleanupChattyList().ConfigureAwait(true);
			if (ThreadList.Items != null && ThreadList.Items.Count > 0)
			{
				ThreadList.ScrollIntoView(ThreadList.Items[0]);
				SelectedComment = _chattyManager.GroupedChatty[0].Key.Comments[0];
				await _chattyManager.MarkCommentRead(SelectedComment).ConfigureAwait(false);
				_threadNavigationAnchorIndex = 0;
			}
			RebindThreadList();
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

		#region Load and Save State
		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			_container = e.Parameter as IContainer;
			_container.Resolve<AuthenticationManager>();
			ChattyManager = _container.Resolve<ChattyManager>();
			_markManager = _container.Resolve<ThreadMarkManager>();
			Settings = _container.Resolve<LatestChattySettings>();
			_authManager = _container.Resolve<AuthenticationManager>();
			_messageManager = _container.Resolve<MessageManager>();
			_ignoreManager = _container.Resolve<IgnoreManager>();
			_container.Resolve<INotificationManager>();
			_keyBindWindow = CoreWindow.GetForCurrentThread();
			_keyBindWindow.KeyDown += Chatty_KeyDown;
			_keyBindWindow.KeyUp += Chatty_KeyUp;
			PageRoot.SizeChanged += PageRoot_SizeChanged;
			Settings.PropertyChanged += Settings_PropertyChanged;
			ChattyManager.PropertyChanged += ChattyManager_PropertyChanged;
			EnableShortcutKeys();

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

			GroupedChattyView = new CollectionViewSource
			{
				IsSourceGrouped = true,
				Source = ChattyManager.GroupedChatty
			};
		}

		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			base.OnNavigatingFrom(e);
			Settings.PropertyChanged -= Settings_PropertyChanged;
			PageRoot.SizeChanged -= PageRoot_SizeChanged;
			ChattyManager.PropertyChanged -= ChattyManager_PropertyChanged;
			DisableShortcutKeys();
			if (_keyBindWindow != null)
			{
				_keyBindWindow.KeyDown -= Chatty_KeyDown;
				_keyBindWindow.KeyUp -= Chatty_KeyUp;
			}
			_searchTextChangedSubscription?.Dispose();
		}

		private void ChattyManager_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName.Equals(nameof(ChattyManager.IsFullUpdateHappening), StringComparison.Ordinal))
			{
				RebindThreadList();
			}
		}

		private void PageRoot_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			SetReplyBounds();
		}

		private void SetReplyBounds()
		{
			if (replyBox is null) return;

			var windowSize = new Size(Window.Current.Bounds.Width, Window.Current.Bounds.Height);
			if (Settings.LargeReply)
			{
				replyBox.MinHeight = replyBox.MaxHeight = PageRoot.ActualHeight - ChattyTabItem.ActualHeight - ChattyCommandBarGroup.ActualHeight - 20;
				replyBox.MinWidth = replyBox.MaxWidth = PageRoot.ActualWidth - 20;
			}
			else
			{
				replyBox.MinHeight = replyBox.MaxHeight = windowSize.Height / 1.75;
				replyBox.MinWidth = replyBox.MaxWidth = windowSize.Width / 2;
				if (windowSize.Height < 600) replyBox.MaxHeight = double.PositiveInfinity;
				if (windowSize.Width < 900) replyBox.MaxWidth = double.PositiveInfinity;
			}
		}

		private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName.Equals(nameof(Settings.UseSmoothScrolling), StringComparison.InvariantCulture))
			{
				SetListScrollViewerSmoothing();
			}
		}



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
					case VirtualKey.A:
						if (SelectedComment == null) break;
						SelectedComment = await ChattyManager.SelectNextComment(SelectedComment.Thread, false, true).ConfigureAwait(true);
						ThreadList.ScrollIntoView(SelectedComment);
						break;
					case VirtualKey.Z:
						if (SelectedComment == null) break;
						SelectedComment = await ChattyManager.SelectNextComment(SelectedComment.Thread, true, true).ConfigureAwait(true);
						ThreadList.ScrollIntoView(SelectedComment);
						break;
					case VirtualKey.J:
						_threadNavigationAnchorIndex--;
						if (_threadNavigationAnchorIndex < 0) _threadNavigationAnchorIndex = _chattyManager.GroupedChatty.Count - 1;
						ThreadList.ScrollIntoView(_chattyManager.GroupedChatty[_threadNavigationAnchorIndex], ScrollIntoViewAlignment.Leading);
						SelectedComment = _chattyManager.GroupedChatty[_threadNavigationAnchorIndex].Key.Comments[0];
						break;
					case VirtualKey.K:
						_threadNavigationAnchorIndex++;
						if (_threadNavigationAnchorIndex > _chattyManager.GroupedChatty.Count - 1) _threadNavigationAnchorIndex = 0;
						ThreadList.ScrollIntoView(_chattyManager.GroupedChatty[_threadNavigationAnchorIndex], ScrollIntoViewAlignment.Leading);
						SelectedComment = _chattyManager.GroupedChatty[_threadNavigationAnchorIndex].Key.Comments[0];
						break;
					case VirtualKey.PageUp:
						PageUpOrDown(-1);
						args.Handled = true;
						break;
					case VirtualKey.PageDown:
						PageUpOrDown(1);
						args.Handled = true;
						break;
					case VirtualKey.T:
						if (SelectedComment == null) break;
						if (Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down)) await ChattyManager.MarkCommentThreadRead(SelectedComment.Thread).ConfigureAwait(true);
						SelectedComment.Thread.TruncateThread = !SelectedComment.Thread.TruncateThread;
						if (SelectedComment.Thread.TruncateThread) SelectedComment = null;
						if (SelectedComment != null) ThreadList.ScrollIntoView(SelectedComment);
						break;
				}
			}
			catch (Exception ex)
			{
				await DebugLog.AddException($"Keydown exception for {args.VirtualKey}", ex).ConfigureAwait(false);
			}
		}

		private void PageUpOrDown(int direction)
		{
			var scrollViewer = ThreadList.FindDescendant<ScrollViewer>();
			if (scrollViewer != null) scrollViewer.ChangeView(null, scrollViewer.VerticalOffset + direction * scrollViewer.ViewportHeight * 0.9, null);
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
					case VirtualKey.R:
						if (SelectedComment == null) return;
						ShowReplyForComment(SelectedComment);
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
			catch (Exception ex)
			{
				await DebugLog.AddException($"Keyup exception for {args.VirtualKey}", ex).ConfigureAwait(false);
			}
		}

		#endregion

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

		private void SetReplyFocus(Comment comment)
		{
			ThreadList.ContainerFromItem(comment)?.FindFirstControlNamed<PostContol>("replyControl")?.SetFocus();
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

		private void SetListScrollViewerSmoothing()
		{
			var scrollViewer = ThreadList.FindDescendant<ScrollViewer>();
			if (scrollViewer != null) scrollViewer.IsScrollInertiaEnabled = AppGlobal.Settings.UseSmoothScrolling;
		}

		private void ShowReplyForComment(Comment comment)
		{
			SelectedComment = comment;
			comment.ShowReply = true;
			SetReplyBounds();
			replyControl.UpdateLayout();
			replyControl.SetFocus();
			replyBox.Fade(1, 250).Start();
			DebugLog.AddMessage($"Showing reply for post {comment.Id}").ConfigureAwait(false).GetAwaiter().GetResult();
		}

		private async Task AddTabByPostId(int postId, bool selectNewTab = true)
		{
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

		#region Events

		private void SearchTextBoxLostFocus(object sender, RoutedEventArgs e)
		{
			EnableShortcutKeys();
		}

		private void SearchTextBoxGotFocus(object sender, RoutedEventArgs e)
		{
			DisableShortcutKeys();
		}

		private void GoToChattyTopClicked(object sender, RoutedEventArgs e)
		{
			if (ThreadList.Items != null && ThreadList.Items.Count > 0)
			{
				ThreadList.ScrollIntoView(ThreadList.Items[0]);
			}
		}

		private async void ThreadList_ItemClick(object sender, ItemClickEventArgs e)
		{
			try
			{
				var comment = e.ClickedItem as Comment;
				if (comment is null) return;

				await _chattyManager.MarkCommentRead(comment).ConfigureAwait(true);

				if (Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down))
				{
					ShowReplyForComment(comment);
					return;
				}
				else
				{
					if (SelectedComment != null) SelectedComment.ShowReply = false;

					//When a full update is happening, things will get added and removed but we don't want to do anything selectino related at that time.
					if (ChattyManager.IsFullUpdateHappening) return;
					var lv = sender as ListView;
					if (lv == null) return; //This would be bad.

					SelectedComment = comment;
					_threadNavigationAnchorIndex = _chattyManager.GroupedChatty.IndexOf(_chattyManager.GroupedChatty.First(x => x.Key.Id == SelectedComment.Thread.Id));
					if (comment == null) return; //Bail, we don't know what to
					await _chattyManager.DeselectAllPostsForCommentThread(comment.Thread).ConfigureAwait(true);

					//If the selection is a post other than the OP, untruncate the thread to prevent problems when truncated posts update.
					if (comment.Thread.Id != comment.Id && comment.Thread.TruncateThread)
					{
						UnBindThreadList();
						comment.Thread.TruncateThread = false;
						RebindThreadList();
					}

					comment.IsSelected = true;
					lv.UpdateLayout();
					lv.ScrollIntoView(comment);
				}
			}
			catch (Exception ex)
			{
				await DebugLog.AddException(string.Empty, ex).ConfigureAwait(false);
			}
		}

		private async Task ShowTaggers(Button button, int commentId)
		{
			try
			{
				if (button == null) return;
				button.IsEnabled = false;
				var tag = button.Tag as string;
				await DebugLog.AddMessage("ViewedTagCount-" + tag).ConfigureAwait(true);
				var lolUrl = Locations.GetLolTaggersUrl(commentId, tag);
				var response = await JsonDownloader.DownloadObject(lolUrl).ConfigureAwait(true);
				var names = string.Join(Environment.NewLine, response["data"][0]["usernames"].Select(a => a.ToString()).OrderBy(a => a));
				var flyout = new Flyout();
				var tb = new TextBlock();
				tb.Text = names;
				flyout.Content = tb;
				flyout.ShowAt(button);
			}
			catch (Exception ex)
			{
				await DebugLog.AddException(string.Empty, ex).ConfigureAwait(true);
				ShellMessage?.Invoke(this, new ShellMessageEventArgs("Error retrieving taggers. Try again later.", ShellMessageType.Error));
			}
			finally
			{
				if (button != null)
				{
					button.IsEnabled = true;
				}
			}
		}
		private async void LolTagTapped(object sender, TappedRoutedEventArgs e)
		{
			var b = sender as Button;
			var id = (b?.DataContext as Comment)?.Id;
			if (b == null || id == null) return;
			await ShowTaggers(b, id.Value).ConfigureAwait(true);
		}

		private void ReplyControl_TextBoxLostFocus(object sender, EventArgs e)
		{
			AppGlobal.ShortcutKeysEnabled = true;
		}

		private void ReplyControl_TextBoxGotFocus(object sender, EventArgs e)
		{
			AppGlobal.ShortcutKeysEnabled = false;
		}

		private void ReplyControl_Closed(object sender, EventArgs e)
		{
			AppGlobal.ShortcutKeysEnabled = true;
			replyBox.Opacity = 0;
		}

		private async void AddTabThreadClicked(object sender, AddThreadTabEventArgs e)
		{
			await AddTabByPostId(e.Thread.Id, !e.AddInBackground).ConfigureAwait(false);
		}

		private void ShowReplyClicked(object sender, CommentEventArgs e)
		{
			ShowReplyForComment(e.Comment);
		}

		private void CloseReplyClicked(object sender, RoutedEventArgs e)
		{
			// Long term - get rid of this. It's unecessary now.
			// Still need it because split view uses it.
			if (SelectedComment is null) return;
			SelectedComment.ShowReply = false;
			replyBox.Opacity = 0;
		}
		private void ScrollToReplyPostClicked(object sender, RoutedEventArgs e)
		{
			if (SelectedComment is null) return;
			ThreadList.ScrollIntoView(SelectedComment, ScrollIntoViewAlignment.Leading);
		}

		private void ToggleLargeReply(object sender, RoutedEventArgs e)
		{
			Settings.LargeReply = !Settings.LargeReply;
			SetReplyBounds();
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

		Dictionary<int, Comment> _commentsCurrentlyInView = new Dictionary<int, Comment>();

		private async void ListViewItemViewportChanged(FrameworkElement sender, EffectiveViewportChangedEventArgs args)
		{
			if (!AppGlobal.Settings.MarkCommentReadDuringScroll) return;

			var comment = sender.DataContext as Comment;
			if (comment is null) return;
			if (args.BringIntoViewDistanceY < sender.ActualHeight)
			{
				//System.Diagnostics.Debug.WriteLine($"Item: {comment.Preview.Truncate(25)} has {sender.ActualHeight - args.BringIntoViewDistanceY} pixels within the viewport");
				if (!_commentsCurrentlyInView.ContainsKey(comment.Id)) _commentsCurrentlyInView.Add(comment.Id, comment);
			}
			else
			{
				//System.Diagnostics.Debug.WriteLine($"Item: {comment.Preview.Truncate(25)} has {args.BringIntoViewDistanceY - sender.ActualHeight} pixels to go before it is even partially visible");
				if (_commentsCurrentlyInView.Remove(comment.Id))
				{
					//If we're not scrolling down, don't mark it read.
					if (!_scrollingDown) return;
					//It was in the collection, now it's not. Mark it read.
					await _chattyManager.MarkCommentRead(comment).ConfigureAwait(false);
				}
			}
		}

		private void ThreadListLoaded(object sender, RoutedEventArgs e)
		{
			RebindThreadList();
		}

		private void RebindThreadList()
		{
			SetListScrollViewerSmoothing();
			UnBindThreadList();
			_threadListScrollBar = ThreadList.FindDescendant<Windows.UI.Xaml.Controls.Primitives.ScrollBar>();
			_threadListScrollBar.ValueChanged += ScrollBar_ValueChanged;
		}

		private void UnBindThreadList()
		{
			if (_threadListScrollBar != null)
			{
				_threadListScrollBar.ValueChanged -= ScrollBar_ValueChanged;
			}
		}

		private void ScrollBar_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
		{
			_scrollingDown = e.OldValue < e.NewValue;
			System.Diagnostics.Debug.WriteLine($"Setting Scroll Direction: {_scrollingDown}");
		}

		private void ThreadListUnloaded(object sender, RoutedEventArgs e)
		{
			UnBindThreadList();
		}
		private void ListPointerEntered(object sender, PointerRoutedEventArgs e)
		{
			RebindThreadList();
		}

		private void UntruncateClicked(object sender, CommentEventArgs e)
		{
			UnBindThreadList();
			e.Comment.Thread.TruncateThread = false;
			RebindThreadList();
		}
		#endregion

	}
}
