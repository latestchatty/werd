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
using Werd.Views.NavigationArgs;
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
	/// <summary>
	/// A basic page that provides characteristics common to most applications.
	/// </summary>
	public sealed partial class InlineChattyFast
	{
		private CoreWindow _keyBindWindow;

		public override string ViewIcons { get => "\uE90A"; set { return; } }
		public override string ViewTitle { get => "Chatty"; set { return; } }

		public override event EventHandler<LinkClickedEventArgs> LinkClicked;

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
		private AppSettings npcSettings;
		private AppSettings Settings
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

		private CortexManager npcCortexManager;
		private CortexManager CortexManager
		{
			get => npcCortexManager;
			set => SetProperty(ref npcCortexManager, value);
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
			if (Settings.MarkReadOnSort)
			{
				await _chattyManager.MarkAllVisibleCommentsRead().ConfigureAwait(true);
			}
			await _chattyManager.CleanupChattyList().ConfigureAwait(true);
			if (ThreadList.Items != null && ThreadList.Items.Count > 0)
			{
				ThreadList.ScrollIntoView(ThreadList.Items[0]);
				SelectedComment = _chattyManager.GroupedChatty[0].Key.Comments[0];
				await _chattyManager.MarkCommentRead(SelectedComment).ConfigureAwait(true);
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
				case "hot":
					sort = ChattySortType.Hot;
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
			var args = e.Parameter as ChattyNavigationArgs;
			_container = args.Container;
			_container.Resolve<AuthenticationManager>();
			ChattyManager = _container.Resolve<ChattyManager>();
			_markManager = _container.Resolve<ThreadMarkManager>();
			Settings = _container.Resolve<AppSettings>();
			_authManager = _container.Resolve<AuthenticationManager>();
			_messageManager = _container.Resolve<MessageManager>();
			_ignoreManager = _container.Resolve<IgnoreManager>();
			CortexManager = _container.Resolve<CortexManager>();
			_container.Resolve<INotificationManager>();
			_keyBindWindow = CoreWindow.GetForCurrentThread();
			_keyBindWindow.KeyDown += Chatty_KeyDown;
			_keyBindWindow.KeyUp += Chatty_KeyUp;
			PageRoot.SizeChanged += PageRoot_SizeChanged;
			Settings.PropertyChanged += Settings_PropertyChanged;
			ChattyManager.PropertyChanged += ChattyManager_PropertyChanged;
			AppGlobal.ShortcutKeysEnabled = true;

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
				replyBox.MinHeight = replyBox.MaxHeight = PageRoot.ActualHeight - ChattyCommandBarGroup.ActualHeight - 20;
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
					case VirtualKey.F:
						if (Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down)) { ShowSearchBox(); }
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
					case VirtualKey.F5:
						await ReSortChatty().ConfigureAwait(true);
						break;

					case VirtualKey.R:
						if (SelectedComment == null) return;
						ShowReplyForComment(SelectedComment);
						break;
					default:
						switch ((int)args.VirtualKey)
						{
							case 191:
								ShowSearchBox();
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

		private void ShowSearchBox()
		{
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
		}

		private void SetReplyFocus(Comment comment)
		{
			ThreadList.ContainerFromItem(comment)?.FindFirstControlNamed<PostContol>("replyControl")?.SetFocus();
		}

		private void ShowNewRootPost()
		{
			this.Shell.NavigateToPage(typeof(NewRootPostView), _container);
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

		#region Events

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


		private void ReplyControl_Closed(object sender, EventArgs e)
		{
			replyBox.Opacity = 0;
		}

		private void AddTabThreadClicked(object sender, AddThreadTabEventArgs e)
		{
			LinkClicked?.Invoke(sender, new LinkClickedEventArgs(new Uri($"https://shacknews.com/chatty?id={e.Thread.Id}"), e.AddInBackground));
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

		private void HandleLinkClicked(object sender, LinkClickedEventArgs e)
		{
			LinkClicked?.Invoke(sender, e);
		}


		private void ThreadListLoaded(object sender, RoutedEventArgs e)
		{
			RebindThreadList();
		}

		private void RebindThreadList()
		{
			SetListScrollViewerSmoothing();
		}
		private void ListPointerEntered(object sender, PointerRoutedEventArgs e)
		{
			RebindThreadList();
		}

		private void UntruncateClicked(object sender, CommentEventArgs e)
		{
			e.Comment.Thread.TruncateThread = false;
			RebindThreadList();
		}
		#endregion

	}
}
