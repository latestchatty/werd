using Autofac;
using Common;
using Microsoft.Toolkit.Collections;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Werd.Common;
using Werd.Controls;
using Werd.DataModel;
using Werd.Managers;
using Werd.Settings;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
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

		public override string ViewTitle => "Chatty";

		public override event EventHandler<LinkClickedEventArgs> LinkClicked;

		public override event EventHandler<ShellMessageEventArgs> ShellMessage;


		private IContainer _container;
		private AuthenticationManager _authManager;
		private MessageManager _messageManager;
		private IgnoreManager _ignoreManager;
		private int _threadNavigationAnchorIndex = 0;
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

		//private async void ChattyListSelectionChanged(object sender, SelectionChangedEventArgs e)
		//{
		//	if (e.AddedItems.Count == 1)
		//	{
		//		CommentThread ct = e.AddedItems[0] as CommentThread;
		//		if (ct == null) return;

		//		if (visualState.CurrentState == VisualStatePhone)
		//		{
		//			SingleThreadControl.DataContext = null;
		//			await SingleThreadControl.Close();
		//			Frame.Navigate(typeof(SingleThreadView), new Tuple<IContainer, int, int>(_container, ct.Id, ct.Id));
		//		}
		//		else
		//		{
		//			SingleThreadControl.Initialize(_container);
		//			SingleThreadControl.DataContext = ct;
		//		}
		//		ThreadList.ScrollIntoView(ct);
		//	}

		//	if (e.RemovedItems.Count > 0)
		//	{
		//		CommentThread ct = e.RemovedItems[0] as CommentThread;
		//		await _chattyManager.MarkCommentThreadRead(ct);
		//	}
		//}

		#region Events

		private async void MarkAllRead(object sender, RoutedEventArgs e)
		{
			await _chattyManager.MarkAllVisibleCommentsRead();
			await AppGlobal.DebugLog.AddMessage("Chatty-MarkReadClicked");
		}

		private async void PinClicked(object sender, RoutedEventArgs e)
		{
			MenuFlyoutItem flyout = sender as MenuFlyoutItem;
			if (flyout == null) return;
			CommentThread commentThread = flyout.DataContext as CommentThread;
			if (commentThread == null) return;
			if (_markManager.GetMarkType(commentThread.Id) == MarkType.Pinned)
			{
				await AppGlobal.DebugLog.AddMessage("Chatty-PinClicked");
				await _markManager.MarkThread(commentThread.Id, MarkType.Unmarked);
			}
			else
			{
				await AppGlobal.DebugLog.AddMessage("Chatty-UnpinClicked");
				await _markManager.MarkThread(commentThread.Id, MarkType.Pinned);
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
				await AppGlobal.DebugLog.AddMessage("Chatty-CollapseClicked");
				await _markManager.MarkThread(commentThread.Id, MarkType.Unmarked);
			}
			else
			{
				await AppGlobal.DebugLog.AddMessage("Chatty-UncollapseClicked");
				await _markManager.MarkThread(commentThread.Id, MarkType.Collapsed);
			}
		}

		private async void MarkThreadReadClicked(object sender, RoutedEventArgs e)
		{
			MenuFlyoutItem flyout = sender as MenuFlyoutItem;
			if (flyout == null) return;
			CommentThread commentThread = flyout.DataContext as CommentThread;
			if (commentThread == null) return;
			await ChattyManager.MarkCommentThreadRead(commentThread);
		}
		#endregion


		private async void ReSortClicked(object sender, RoutedEventArgs e)
		{
			await AppGlobal.DebugLog.AddMessage("Chatty-ResortClicked");
			await ReSortChatty();
		}

		private async void ChattyPullRefresh(RefreshContainer sender, RefreshRequestedEventArgs args)
		{
			using (Windows.Foundation.Deferral _ = args.GetDeferral())
			{
				await ReSortChatty();
			}
		}

		private async Task ReSortChatty()
		{
			SelectedThread = null;
			//SingleThreadControl.DataContext = null;

			if (Settings.MarkReadOnSort)
			{
				await _chattyManager.MarkAllVisibleCommentsRead();
			}
			await _chattyManager.CleanupChattyList();
			if (ThreadList.Items != null && ThreadList.Items.Count > 0)
			{
				ThreadList.ScrollIntoView(ThreadList.Items[0]);
				SelectedComment = _chattyManager.GroupedChatty[0].Key.Comments[0];
				await _chattyManager.MarkCommentRead(SelectedComment).ConfigureAwait(false);
				_threadNavigationAnchorIndex = 0;
			}
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
			await AppGlobal.DebugLog.AddMessage("Chatty-Filter-" + tagName);
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
					await ChattyManager.SearchChatty(SearchTextBox.Text);
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
			await ChattyManager.FilterChatty(filter);
		}

		private async void SortChanged(object sender, SelectionChangedEventArgs e)
		{
			if (ChattyManager == null) return;
			if (e.AddedItems.Count != 1) return;
			ComboBoxItem item = e.AddedItems[0] as ComboBoxItem;
			if (item == null) return;
			ChattySortType sort;
			string tagName = item.Tag.ToString();
			await AppGlobal.DebugLog.AddMessage("Chatty-Sort-" + tagName);
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
			await ChattyManager.SortChatty(sort);
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
			ChattyManager.PropertyChanged += ChattyManager_PropertyChanged;
			Settings.PropertyChanged += Settings_PropertyChanged;
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

		private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName.Equals(nameof(Settings.UseSmoothScrolling), StringComparison.InvariantCulture))
			{
				SetListScrollViewerSmoothing();
			}
		}

		private void ChattyManager_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName.Equals(nameof(ChattyManager.IsFullUpdateHappening), StringComparison.InvariantCulture))
			{
				SetListScrollViewerSmoothing();
			}
		}

		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			base.OnNavigatingFrom(e);
			ChattyManager.PropertyChanged -= ChattyManager_PropertyChanged;
			Settings.PropertyChanged -= Settings_PropertyChanged;
			DisableShortcutKeys();
			if (_keyBindWindow != null)
			{
				_keyBindWindow.KeyDown -= Chatty_KeyDown;
				_keyBindWindow.KeyUp -= Chatty_KeyUp;
			}
			_searchTextChangedSubscription?.Dispose();
		}

		private bool _ctrlDown;
		private bool _shiftDown;

		private async void Chatty_KeyDown(CoreWindow sender, KeyEventArgs args)
		{
			try
			{
				if (!AppGlobal.ShortcutKeysEnabled)
				{
					return;
				}

				switch (args.VirtualKey)
				{
					case VirtualKey.Shift:
						_shiftDown = true;
						break;
					case VirtualKey.Control:
						_ctrlDown = true;
						break;
					case VirtualKey.A:
						if (SelectedComment == null) break;
						SelectedComment = await ChattyManager.SelectNextComment(SelectedComment.Thread, false, false);
						ThreadList.ScrollIntoView(SelectedComment);
						break;
					case VirtualKey.Z:
						if (SelectedComment == null) break;
						SelectedComment = await ChattyManager.SelectNextComment(SelectedComment.Thread, true, false);
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
					case VirtualKey.Space:
						PageUpOrDown(_shiftDown ? -1 : 1);
						args.Handled = true;
						break;
					case VirtualKey.T:
						if (SelectedComment == null) break;
						if (_shiftDown) await ChattyManager.MarkCommentThreadRead(SelectedComment.Thread);
						SelectedComment.Thread.TruncateThread = !SelectedComment.Thread.TruncateThread;
						if (SelectedComment.Thread.TruncateThread) SelectedComment = null;
						if (SelectedComment != null) ThreadList.ScrollIntoView(SelectedComment);
						break;
					case VirtualKey.F5:
						await ReSortChatty();
						break;
				}
			}
			catch (Exception)
			{
				//(new Microsoft.ApplicationInsights.TelemetryClient()).TrackException(e, new Dictionary<string, string> { { "keyCode", args.VirtualKey.ToString() } });
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
				if (!AppGlobal.ShortcutKeysEnabled)
				{
					//await Global.DebugLog.AddMessage($"{GetType().Name} - Suppressed KeyUp event.");
					return;
				}

				switch (args.VirtualKey)
				{
					case VirtualKey.Shift:
						_shiftDown = false;
						break;
					case VirtualKey.Control:
						_ctrlDown = false;
						break;
					case VirtualKey.N:
						if (_ctrlDown)
						{
							ShowNewRootPost();
						}
						break;
					case VirtualKey.R:
						if (SelectedComment == null) return;
						SelectedComment.ShowReply = true;
						SetReplyFocus(SelectedComment);
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
				await AppGlobal.DebugLog.AddException(string.Empty, e);
			}
		}

		#endregion

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

		private void TruncateUntruncateClicked(object sender, RoutedEventArgs e)
		{
			var currentThread = ((sender as FrameworkElement)?.DataContext as ReadOnlyObservableGroup<CommentThread, Comment>)?.Key;
			if (currentThread == null) currentThread = ((sender as FrameworkElement)?.DataContext as Comment)?.Thread;
			if (currentThread == null) return;
			currentThread.TruncateThread = !currentThread.TruncateThread;
			ThreadList.UpdateLayout();
		}
		private async void CollapseThreadClicked(object sender, RoutedEventArgs e)
		{
			var currentThread = ((sender as FrameworkElement)?.DataContext as ReadOnlyObservableGroup<CommentThread, Comment>)?.Key;
			if (currentThread == null) return;
			await _markManager.MarkThread(currentThread.Id, currentThread.IsCollapsed ? MarkType.Unmarked : MarkType.Collapsed);
		}
		private async void PinThreadClicked(object sender, RoutedEventArgs e)
		{
			var currentThread = ((sender as FrameworkElement)?.DataContext as ReadOnlyObservableGroup<CommentThread, Comment>)?.Key;
			if (currentThread == null) return;
			await _markManager.MarkThread(currentThread.Id, currentThread.IsPinned ? MarkType.Unmarked : MarkType.Pinned);
		}

		private async void ReportPostClicked(object sender, RoutedEventArgs e)
		{
			if (!_authManager.LoggedIn)
			{
				ShellMessage?.Invoke(this, new ShellMessageEventArgs("You must be logged in to report a post.", ShellMessageType.Error));
				return;
			}

			var dialog = new MessageDialog("Are you sure you want to report this post for violating community guidelines?");
			var comment = ((sender as FrameworkElement)?.DataContext as Comment);
			if (comment == null) return;
			dialog.Commands.Add(new UICommand("Yes", async _ =>
			{
				await _messageManager.SendMessage(
					"duke nuked",
					$"Reporting Post Id {comment.Id}",
					$"I am reporting the following post via the Werd in-app reporting feature.  Please take a look at it to ensure it meets community guidelines.  Thanks!  https://www.shacknews.com/chatty?id={comment.Id}#item_{comment.Id}");
				ShellMessage?.Invoke(this, new ShellMessageEventArgs("Post reported.", ShellMessageType.Message));
			}));
			dialog.Commands.Add(new UICommand("Cancel"));
			dialog.CancelCommandIndex = 1;
			dialog.DefaultCommandIndex = 1;
			await dialog.ShowAsync();
		}

		private async void SelectedItemChanged(object sender, SelectionChangedEventArgs e)
		{
			try
			{
				if (e.RemovedItems.Count == 1)
				{
					var oldItem = e.RemovedItems[0] as Comment;
					if (oldItem != null) oldItem.ShowReply = false;
				}
				//When a full update is happening, things will get added and removed but we don't want to do anything selectino related at that time.
				if (ChattyManager.IsFullUpdateHappening) return;
				var lv = sender as ListView;
				if (lv == null) return; //This would be bad.

				if (e.AddedItems.Count == 1)
				{
					var selectedItem = e.AddedItems[0] as Comment;
					SelectedComment = selectedItem;
					_threadNavigationAnchorIndex = _chattyManager.GroupedChatty.IndexOf(_chattyManager.GroupedChatty.First(x => x.Key.Id == SelectedComment.Thread.Id));
					if (selectedItem == null) return; //Bail, we don't know what to
					await _chattyManager.DeselectAllPostsForCommentThread(selectedItem.Thread).ConfigureAwait(true);

					//If the selection is a post other than the OP, untruncate the thread to prevent problems when truncated posts update.
					if (selectedItem.Thread.Id != selectedItem.Id && selectedItem.Thread.TruncateThread)
					{
						selectedItem.Thread.TruncateThread = false;
					}

					await _chattyManager.MarkCommentRead(selectedItem).ConfigureAwait(true);
					selectedItem.IsSelected = true;
					lv.UpdateLayout();
					lv.ScrollIntoView(selectedItem);
				}
			}
			catch { }
		}

		private void SearchAuthorClicked(object sender, RoutedEventArgs e)
		{
			var comment = ((sender as FrameworkElement)?.DataContext as Comment);
			if (comment == null) return;
			if (Window.Current.Content is Shell f)
			{
				f.NavigateToPage(
					typeof(SearchWebView),
					new Tuple<IContainer, Uri>
						(_container,
						new Uri($"https://www.shacknews.com/search?chatty=1&type=4&chatty_term=&chatty_user={Uri.EscapeUriString(comment.Author)}& chatty_author=&chatty_filter=all&result_sort=postdate_desc")
						)
				);
			}
		}

		private void SearchAuthorRepliesClicked(object sender, RoutedEventArgs e)
		{
			var comment = ((sender as FrameworkElement)?.DataContext as Comment);
			if (comment == null) return;
			if (Window.Current.Content is Shell f)
			{
				f.NavigateToPage(
					typeof(SearchWebView),
					new Tuple<IContainer, Uri>
						(_container,
						new Uri($"https://www.shacknews.com/search?chatty=1&type=4&chatty_term=&chatty_user=&chatty_author={Uri.EscapeUriString(comment.Author)}&chatty_filter=all&result_sort=postdate_desc")
						)
				);
			}
		}

		private void MessageAuthorClicked(object sender, RoutedEventArgs e)
		{
			var comment = ((sender as FrameworkElement)?.DataContext as Comment);
			if (comment == null) return;
			if (Window.Current.Content is Shell f)
			{
				f.NavigateToPage(typeof(Messages), new Tuple<IContainer, string>(_container, comment.Author));
			}
		}

		private async void IgnoreAuthorClicked(object sender, RoutedEventArgs e)
		{
			var comment = ((sender as FrameworkElement)?.DataContext as Comment);
			if (comment == null) return;
			var author = comment.Author;
			var dialog = new MessageDialog($"Are you sure you want to ignore posts from { author }?");
			dialog.Commands.Add(new UICommand("Ok", async a =>
			{
				await _ignoreManager.AddIgnoredUser(author).ConfigureAwait(true);
				_chattyManager.ScheduleImmediateFullChattyRefresh();
			}));
			dialog.Commands.Add(new UICommand("Cancel"));
			dialog.CancelCommandIndex = 1;
			dialog.DefaultCommandIndex = 1;
			await dialog.ShowAsync();
		}

		private void ViewAuthorModHistoryClicked(object sender, RoutedEventArgs e)
		{
			var comment = ((sender as FrameworkElement)?.DataContext as Comment);
			if (comment == null) return;
			var author = comment.Author;
			if (Window.Current.Content is Shell f)
			{
				f.NavigateToPage(typeof(ModToolsWebView), new Tuple<IContainer, Uri>(_container, new Uri($"https://www.shacknews.com/moderators/check?username={author}")));
			}
		}

		private async void LolPostClicked(object sender, RoutedEventArgs e)
		{
			var comment = ((sender as FrameworkElement)?.DataContext as Comment);
			if (comment == null) return;
			var mi = sender as MenuFlyoutItem;
			if (mi == null) return;
			try
			{
				mi.IsEnabled = false;
				var tag = mi?.Text;
				await comment.LolTag(tag);
				await AppGlobal.DebugLog.AddMessage("Chatty-LolTagged-" + tag);
			}
			catch (Exception ex)
			{
				await AppGlobal.DebugLog.AddException(string.Empty, ex);
				ShellMessage?.Invoke(this, new ShellMessageEventArgs("Problem tagging, try again later.", ShellMessageType.Error));
			}
			finally
			{
				mi.IsEnabled = true;
			}
		}


		private async Task ShowTaggers(Button button, int commentId)
		{
			try
			{
				if (button == null) return;
				button.IsEnabled = false;
				var tag = button.Tag as string;
				await AppGlobal.DebugLog.AddMessage("ViewedTagCount-" + tag).ConfigureAwait(true);
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
				await AppGlobal.DebugLog.AddException(string.Empty, ex).ConfigureAwait(true);
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
		}

		private void CopyPostLinkClicked(object sender, RoutedEventArgs e)
		{
			var button = sender as Button;
			if (button == null) return;
			var comment = ((sender as FrameworkElement)?.DataContext as Comment);
			if (comment == null) return;
			var dataPackage = new DataPackage();
			dataPackage.SetText($"http://www.shacknews.com/chatty?id={comment.Id}#item_{comment.Id}");
			Settings.LastClipboardPostId = comment.Id;
			Clipboard.SetContent(dataPackage);
			ShellMessage?.Invoke(this, new ShellMessageEventArgs("Link copied to clipboard."));
		}

		private async void MarkAllReadButtonClicked(object sender, RoutedEventArgs e)
		{
			var currentThread = ((sender as FrameworkElement)?.DataContext as ReadOnlyObservableGroup<CommentThread, Comment>)?.Key;
			if (currentThread == null) return;
			await _chattyManager.MarkCommentThreadRead(currentThread).ConfigureAwait(false);
		}

		private void ShowReplyClicked(object sender, RoutedEventArgs e)
		{
			var button = sender as Button;
			if (button == null) return;
			var comment = button.DataContext as Comment;
			if (comment == null) return;
			SelectedComment = comment;
			comment.ShowReply = true;
			replyControl.UpdateLayout();
			replyControl.SetFocus();
			//if (button.IsChecked.HasValue && button.IsChecked.Value) SetReplyFocus(comment);
		}
		private async void PreviewFlyoutOpened(object sender, object e)
		{
			var comment = (((sender as Flyout)?.Content as FrameworkElement)?.DataContext as Comment);
			if (comment == null) return;
			await _chattyManager.MarkCommentRead(comment).ConfigureAwait(true);
		}

		private async void ModeratePostClicked(object sender, RoutedEventArgs e)
		{
			var menuFlyoutItem = sender as MenuFlyoutItem;
			if (menuFlyoutItem is null) return;

			var comment = menuFlyoutItem.DataContext as Comment;
			if (comment is null) return;

			if (await comment.Moderate(menuFlyoutItem.Text).ConfigureAwait(true))
			{
				ShellMessage?.Invoke(this, new ShellMessageEventArgs("Post successfully moderated."));
			}
			else
			{
				ShellMessage?.Invoke(this, new ShellMessageEventArgs("Something went wrong while moderating. You probably don't have mod permissions. Stop it.", ShellMessageType.Error));
			}
		}

		private void RefreshSingleThreadClicked(object sender, RoutedEventArgs e)
		{
			var currentThread = ((sender as FrameworkElement)?.DataContext as ReadOnlyObservableGroup<CommentThread, Comment>)?.Key;
			if (currentThread == null) return;
			currentThread.ResyncGrouped();
		}
		private void CloseReplyClicked(object sender, RoutedEventArgs e)
		{
			// Long term - get rid of this. It's unecessary now.
			// Still need it because split view uses it.
			if (SelectedComment is null) return;
			SelectedComment.ShowReply = false;
		}
		private void ScrollToReplyPostClicked(object sender, RoutedEventArgs e)
		{
			if (SelectedComment is null) return;
			ThreadList.ScrollIntoView(SelectedComment, ScrollIntoViewAlignment.Leading);
		}
		#endregion

	}
}
