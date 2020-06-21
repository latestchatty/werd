using Autofac;
using Common;
using Latest_Chatty_8.Common;
using Latest_Chatty_8.Controls;
using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Managers;
using Latest_Chatty_8.Settings;
using Microsoft.HockeyApp;
using Microsoft.Toolkit.Collections;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using IContainer = Autofac.IContainer;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237
namespace Latest_Chatty_8.Views
{
	/// <summary>
	/// A basic page that provides characteristics common to most applications.
	/// </summary>
	public sealed partial class InlineChattyFast
	{
		private const double SwipeThreshold = 110;
		private bool? _swipingLeft;

		private CoreWindow _keyBindWindow;

		public override string ViewTitle => "Chatty (Inline Fast)";

		public override event EventHandler<LinkClickedEventArgs> LinkClicked;

		public override event EventHandler<ShellMessageEventArgs> ShellMessage;

		private IContainer _container;
		private AuthenticationManager _authManager;
		private MessageManager _messageManager;
		private IgnoreManager _ignoreManager;

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
			set
			{
				if (SetProperty(ref npcSelectedThread, value))
				{
					//var t = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunOnUIThreadAndWait(CoreDispatcherPriority.Low, () =>
					//{
					//	if (value?.Comments?.Count() > 0) this.commentList.SelectedIndex = 0;
					//});
				}
			}
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
			HockeyClient.Current.TrackEvent("Chatty-MarkReadClicked");
		}

		private async void PinClicked(object sender, RoutedEventArgs e)
		{
			MenuFlyoutItem flyout = sender as MenuFlyoutItem;
			if (flyout == null) return;
			CommentThread commentThread = flyout.DataContext as CommentThread;
			if (commentThread == null) return;
			if (_markManager.GetMarkType(commentThread.Id) == MarkType.Pinned)
			{
				HockeyClient.Current.TrackEvent("Chatty-PinClicked");
				await _markManager.MarkThread(commentThread.Id, MarkType.Unmarked);
			}
			else
			{
				HockeyClient.Current.TrackEvent("Chatty-UnpinClicked");
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
				HockeyClient.Current.TrackEvent("Chatty-CollapseClicked");
				await _markManager.MarkThread(commentThread.Id, MarkType.Unmarked);
			}
			else
			{
				HockeyClient.Current.TrackEvent("Chatty-UncollapseClicked");
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

		//private async void ChattyManager_PropertyChanged(object sender, PropertyChangedEventArgs e)
		//{
		//	if (e.PropertyName.Equals(nameof(ChattyManager.IsFullUpdateHappening)))
		//	{
		//		if (!ChattyManager.IsFullUpdateHappening)
		//		{
		//			//SingleThreadControl.DataContext = null;
		//			//await SingleThreadControl.Close();
		//			var chatty = ChattyManager.Chatty.ToList();

		//			GroupedChatty.Clear();
		//			foreach (var thread in chatty)
		//			{
		//				var g = new ObservableGroup<CommentThread, Comment>(thread, thread.Comments);
		//				GroupedChatty.Add(g);
		//			}
		//		}
		//	}
		//}
		#endregion


		private async void ReSortClicked(object sender, RoutedEventArgs e)
		{
			HockeyClient.Current.TrackEvent("Chatty-ResortClicked");
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
			}
		}

		private async void SearchTextChanged(object sender, TextChangedEventArgs e)
		{
			if (ShowSearch)
			{
				TextBox searchTextBox = sender as TextBox;
				if (searchTextBox == null) return;
				await ChattyManager.SearchChatty(searchTextBox.Text);
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

		private void ThreadListRightHeld(object sender, HoldingRoutedEventArgs e)
		{
			FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
		}
		private void ThreadListRightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
		}

		private async void FilterChanged(object sender, SelectionChangedEventArgs e)
		{
			if (ChattyManager == null) return;
			if (e.AddedItems.Count != 1) return;
			ComboBoxItem item = e.AddedItems[0] as ComboBoxItem;
			if (item == null) return;
			ChattyFilterType filter;
			string tagName = item.Tag.ToString();
			HockeyClient.Current.TrackEvent("Chatty-Filter-" + tagName);
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
			HockeyClient.Current.TrackEvent("Chatty-Sort-" + tagName);
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
			//ChattyManager.PropertyChanged += ChattyManager_PropertyChanged;
			EnableShortcutKeys();

			GroupedChattyView = new CollectionViewSource
			{
				IsSourceGrouped = true,
				Source = ChattyManager.GroupedChatty
			};
		}

		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			base.OnNavigatingFrom(e);
			//ChattyManager.PropertyChanged -= ChattyManager_PropertyChanged;
			DisableShortcutKeys();
			if (_keyBindWindow != null)
			{
				_keyBindWindow.KeyDown -= Chatty_KeyDown;
				_keyBindWindow.KeyUp -= Chatty_KeyUp;
			}
		}

		private bool _ctrlDown;

		private async void Chatty_KeyDown(CoreWindow sender, KeyEventArgs args)
		{
			try
			{
				if (!Global.ShortcutKeysEnabled)
				{
					Debug.WriteLine($"{GetType().Name} - Suppressed KeyDown event.");
					return;
				}

				switch (args.VirtualKey)
				{
					case VirtualKey.Control:
						_ctrlDown = true;
						break;
					case VirtualKey.F5:
						HockeyClient.Current.TrackEvent("Chatty-F5Pressed");
						await ReSortChatty();
						break;
				}
				Debug.WriteLine($"{GetType().Name} - KeyDown event for {args.VirtualKey}");
			}
			catch (Exception)
			{
				//(new Microsoft.ApplicationInsights.TelemetryClient()).TrackException(e, new Dictionary<string, string> { { "keyCode", args.VirtualKey.ToString() } });
			}
		}

		private void Chatty_KeyUp(CoreWindow sender, KeyEventArgs args)
		{
			try
			{
				if (!Global.ShortcutKeysEnabled)
				{
					Debug.WriteLine($"{GetType().Name} - Suppressed KeyUp event.");
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
							HockeyClient.Current.TrackEvent("Chatty-CtrlNPressed");
							ShowNewRootPost();
						}
						break;
					default:
						switch ((int)args.VirtualKey)
						{
							case 191:
								HockeyClient.Current.TrackEvent("Chatty-SlashPressed");

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
				Debug.WriteLine($"{GetType().Name} - KeyUp event for {args.VirtualKey}");
			}
			catch (Exception)
			{
				//(new Microsoft.ApplicationInsights.TelemetryClient()).TrackException(e, new Dictionary<string, string> { { "keyCode", args.VirtualKey.ToString() } });
			}
		}

		#endregion

		//private void NewRootPostControl_ShellMessage(object sender, ShellMessageEventArgs e)
		//{
		//	if (ShellMessage != null)
		//	{
		//		ShellMessage(sender, e);
		//	}
		//}

		private void ShowNewRootPost()
		{
			Frame.Navigate(typeof(NewRootPostView), _container);
		}

		private void DisableShortcutKeys()
		{
			Global.ShortcutKeysEnabled = false;
		}

		private void EnableShortcutKeys()
		{
			Global.ShortcutKeysEnabled = true;
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

		#region Swipe Gestures
		private void ChattyListManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
		{
			Grid grid = sender as Grid;
			if (grid == null) return;

			Grid container = grid.FindFirstControlNamed<Grid>("previewContainer");
			if (container == null) return;

			Grid swipeContainer = grid.FindName("swipeContainer") as Grid;
			if (swipeContainer != null)
			{
				swipeContainer.Visibility = Visibility.Visible;
			}

			container.Background = (Brush)Resources["ApplicationPageBackgroundThemeBrush"];
			_swipingLeft = null;
		}

		private async void ChattyListManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
		{
			Grid grid = sender as Grid;
			if (grid == null) return;

			Grid container = grid.FindFirstControlNamed<Grid>("previewContainer");
			if (container == null) return;

			Grid swipeContainer = grid.FindName("swipeContainer") as Grid;
			if (swipeContainer != null) swipeContainer.Visibility = Visibility.Collapsed;

			CommentThread ct = container.DataContext as CommentThread;
			if (ct == null) return;
			MarkType currentMark = _markManager.GetMarkType(ct.Id);
			e.Handled = false;
			Debug.WriteLine("Completed manipulation {0},{1}", e.Cumulative.Translation.X, e.Cumulative.Translation.Y);

			bool completedSwipe = Math.Abs(e.Cumulative.Translation.X) > SwipeThreshold;
			ChattySwipeOperation operation = e.Cumulative.Translation.X > 0 ? Settings.ChattyRightSwipeAction : Settings.ChattyLeftSwipeAction;

			if (completedSwipe)
			{
				switch (operation.Type)
				{
					case ChattySwipeOperationType.Collapse:

						if (currentMark != MarkType.Collapsed)
						{
							await _markManager.MarkThread(ct.Id, MarkType.Collapsed);
						}
						else if (currentMark == MarkType.Collapsed)
						{
							await _markManager.MarkThread(ct.Id, MarkType.Unmarked);
						}
						e.Handled = true;
						break;
					case ChattySwipeOperationType.Pin:
						if (currentMark != MarkType.Pinned)
						{
							await _markManager.MarkThread(ct.Id, MarkType.Pinned);
						}
						else if (currentMark == MarkType.Pinned)
						{
							await _markManager.MarkThread(ct.Id, MarkType.Unmarked);
						}
						e.Handled = true;
						break;
					case ChattySwipeOperationType.MarkRead:
						await ChattyManager.MarkCommentThreadRead(ct);
						e.Handled = true;
						break;
				}
			}

			TranslateTransform transform = container.RenderTransform as TranslateTransform;
			if (transform != null) transform.X = 0;
			container.Background = new SolidColorBrush(Colors.Transparent);
			_swipingLeft = null;
		}

		private void ChattyListManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
		{
			Grid grid = sender as Grid;
			if (grid == null) return;

			Grid container = grid.FindFirstControlNamed<Grid>("previewContainer");
			if (container == null) return;

			StackPanel swipeContainer = grid.FindFirstControlNamed<StackPanel>("swipeTextContainer");
			if (swipeContainer == null) return;

			TranslateTransform swipeIconTransform = swipeContainer.RenderTransform as TranslateTransform;

			TranslateTransform transform = container.RenderTransform as TranslateTransform;
			double cumulativeX = e.Cumulative.Translation.X;
			bool showRight = (cumulativeX < 0);

			if (!_swipingLeft.HasValue || _swipingLeft != showRight)
			{
				CommentThread commentThread = grid.DataContext as CommentThread;
				if (commentThread == null) return;

				TextBlock swipeIcon = grid.FindFirstControlNamed<TextBlock>("swipeIcon");
				if (swipeIcon == null) return;
				TextBlock swipeText = grid.FindFirstControlNamed<TextBlock>("swipeText");
				if (swipeText == null) return;

				ChattySwipeOperation op = showRight ? Settings.ChattyLeftSwipeAction : Settings.ChattyRightSwipeAction;

				swipeIcon.Text = op.Icon;
				swipeText.Text = op.DisplayName;
				swipeContainer.FlowDirection = showRight ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
				_swipingLeft = showRight;
			}

			if (transform != null) transform.X = cumulativeX;
			if (swipeIconTransform == null) return;
			if (Math.Abs(cumulativeX) < SwipeThreshold)
			{
				swipeIconTransform.X = showRight ? -(cumulativeX * .3) : cumulativeX * .3;
			}
			else
			{
				swipeIconTransform.X = 15;
			}
		}

		#endregion

		private void GoToChattyTopClicked(object sender, RoutedEventArgs e)
		{
			if (ThreadList.Items != null && ThreadList.Items.Count > 0)
			{
				ThreadList.ScrollIntoView(ThreadList.Items[0]);
			}
		}

		private void InlineControlLinkClicked(object sender, LinkClickedEventArgs e)
		{
			if (LinkClicked != null)
			{
				LinkClicked(sender, e);
			}
		}

		private void InlineControlShellMessage(object sender, ShellMessageEventArgs e)
		{
			if (ShellMessage != null)
			{
				ShellMessage(sender, e);
			}
		}

		private void TruncateUntruncateClicked(object sender, RoutedEventArgs e)
		{
			var currentThread = ((sender as FrameworkElement)?.DataContext as ReadOnlyObservableGroup<CommentThread, Comment>)?.Key;
			if (currentThread == null) return;
			currentThread.TruncateThread = !currentThread.TruncateThread;
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
				//When a full update is happening, things will get added and removed but we don't want to do anything selectino related at that time.
				if (ChattyManager.IsFullUpdateHappening) return;
				var lv = sender as ListView;
				if (lv == null) return; //This would be bad.

				if (e.AddedItems.Count == 1)
				{
					var selectedItem = e.AddedItems[0] as Comment;
					if (selectedItem == null) return; //Bail, we don't know what to
					await _chattyManager.DeselectAllPostsForCommentThread(selectedItem.Thread);

					//If the selection is a post other than the OP, untruncate the thread to prevent problems when truncated posts update.
					if (selectedItem.Thread.Id != selectedItem.Id && selectedItem.Thread.TruncateThread)
					{
						selectedItem.Thread.TruncateThread = false;
					}

					await _chattyManager.MarkCommentRead(selectedItem.Thread, selectedItem);
					selectedItem.IsSelected = true;
					lv.UpdateLayout();
					lv.ScrollIntoView(selectedItem);
				}
			}
			catch { }
		}

		private void SingleThreadInlineControl_KeyUp(CoreWindow sender, KeyEventArgs args)
		{
			try
			{
				if (!Global.ShortcutKeysEnabled) //Not sure what to do about hotkeys with the inline chatty yet.
				{
					Debug.WriteLine($"{GetType().Name} - Suppressed KeyUp event.");
					return;
				}

				switch (args.VirtualKey)
				{
					case VirtualKey.R:
						break;
						//if (_selectedComment == null) return;
						//var controlContainer = CommentList.ContainerFromItem(_selectedComment);
						//var button = controlContainer.FindFirstControlNamed<ToggleButton>("showReply");
						//if (button == null) return;
						//HockeyClient.Current.TrackEvent("Chatty-RPressed");
						//button.IsChecked = true;
						//ShowHideReply();
						//break;
				}
				Debug.WriteLine($"{GetType().Name} - KeyUp event for {args.VirtualKey}");
			}
			catch (Exception)
			{
				//(new Microsoft.ApplicationInsights.TelemetryClient()).TrackException(e, new Dictionary<string, string> { { "keyCode", args.VirtualKey.ToString() } });
			}

		}

		private void SingleThreadInlineControl_KeyDown(CoreWindow sender, KeyEventArgs args)
		{
			try
			{
				if (!Global.ShortcutKeysEnabled) //Not sure what to do about hotkeys with the inline chatty yet.
				{
					Debug.WriteLine($"{GetType().Name} - Suppressed KeyDown event.");
					return;
				}

				switch (args.VirtualKey)
				{
					case VirtualKey.A:
						HockeyClient.Current.TrackEvent("Chatty-APressed");
						//MoveToPreviousPost();
						break;

					case VirtualKey.Z:
						HockeyClient.Current.TrackEvent("Chatty-ZPressed");
						//MoveToNextPost();
						break;
				}
				Debug.WriteLine($"{GetType().Name} - KeyDown event for {args.VirtualKey}");
			}
			catch (Exception)
			{
				//(new Microsoft.ApplicationInsights.TelemetryClient()).TrackException(e, new Dictionary<string, string> { { "keyCode", args.VirtualKey.ToString() } });
			}
		}

		private void SearchAuthorClicked(object sender, RoutedEventArgs e)
		{
			var comment = ((sender as FrameworkElement)?.DataContext as Comment);
			if (comment == null) return;
			if (Window.Current.Content is Shell f)
			{
				f.NavigateToPage(
					typeof(ShackWebView),
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
					typeof(ShackWebView),
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
				await _ignoreManager.AddIgnoredUser(author);
				ShellMessage?.Invoke(this, new ShellMessageEventArgs($"Posts from { author } will be ignored when the app is restarted."));
			}));
			dialog.Commands.Add(new UICommand("Cancel"));
			dialog.CancelCommandIndex = 1;
			dialog.DefaultCommandIndex = 1;
			await dialog.ShowAsync();
		}

		private async void LolPostClicked(object sender, RoutedEventArgs e)
		{
			var comment = ((sender as FrameworkElement)?.DataContext as Comment);
			if (comment == null) return;
			var controlContainer = ThreadList.ContainerFromItem(comment);
			if (controlContainer != null)
			{
				var tagButton = controlContainer.FindFirstControlNamed<Button>("tagButton");
				if (tagButton == null) return;

				tagButton.IsEnabled = false;
				try
				{
					var mi = sender as MenuFlyoutItem;
					var tag = mi?.Text;
					await comment.LolTag(tag);
					HockeyClient.Current.TrackEvent("Chatty-LolTagged-" + tag);
				}
				catch (Exception)
				{
					//(new Microsoft.ApplicationInsights.TelemetryClient()).TrackException(ex);
					if (ShellMessage != null)
					{
						ShellMessage(this, new ShellMessageEventArgs("Problem tagging, try again later.", ShellMessageType.Error));
					}
				}
				finally
				{
					tagButton.IsEnabled = true;
				}
			}
		}

		private async void LolTagTapped(object sender, TappedRoutedEventArgs e)
		{
			Button s = null;
			try
			{
				s = sender as Button;
				if (s == null) return;
				s.IsEnabled = false;
				var tag = s.Tag as string;
				HockeyClient.Current.TrackEvent("ViewedTagCount-" + tag);
				var lolUrl = Locations.GetLolTaggersUrl((s.DataContext as Comment).Id, tag);
				var response = await JsonDownloader.DownloadObject(lolUrl);
				var names = string.Join(Environment.NewLine, response["data"][0]["usernames"].Select(a => a.ToString()).OrderBy(a => a));
				var flyout = new Flyout();
				var tb = new TextBlock();
				tb.Text = names;
				flyout.Content = tb;
				flyout.ShowAt(s);
			}
			catch (Exception)
			{
				//(new TelemetryClient()).TrackException(ex);
				ShellMessage?.Invoke(this, new ShellMessageEventArgs("Error retrieving taggers. Try again later.", ShellMessageType.Error));
			}
			finally
			{
				if (s != null)
				{
					s.IsEnabled = true;
				}
			}
		}

		private void ShowReplyClicked(object sender, RoutedEventArgs e)
		{
			ShowHideReply(sender);
		}

		private void ReplyControl_TextBoxLostFocus(object sender, EventArgs e)
		{
			Global.ShortcutKeysEnabled = true;
		}

		private void ReplyControl_TextBoxGotFocus(object sender, EventArgs e)
		{
			Global.ShortcutKeysEnabled = false;
		}

		private void ReplyControl_ShellMessage(object sender, ShellMessageEventArgs args)
		{
			if (ShellMessage != null)
			{
				ShellMessage(sender, args);
			}
		}

		private void ReplyControl_Closed(object sender, EventArgs e)
		{
			Global.ShortcutKeysEnabled = true;
		}

		private void CopyPostLinkClicked(object sender, RoutedEventArgs e)
		{
			var button = sender as Button;
			if (button == null) return;
			var comment = ((sender as FrameworkElement)?.DataContext as Comment);
			if (comment == null) return;
			var dataPackage = new DataPackage();
			dataPackage.SetText(string.Format("http://www.shacknews.com/chatty?id={0}#item_{0}", comment.Id));
			Clipboard.SetContent(dataPackage);
			ShellMessage?.Invoke(this, new ShellMessageEventArgs("Link copied to clipboard."));
		}

		private void RichPostLinkClicked(object sender, LinkClickedEventArgs e)
		{
			LinkClicked?.Invoke(sender, e);
		}
		private void RichPostShellMessage(object sender, ShellMessageEventArgs e)
		{
			ShellMessage?.Invoke(sender, e);
		}

		private void PreviousNavigationButtonClicked(object sender, RoutedEventArgs e)
		{
			//MoveToPreviousPost();
		}

		private void NextNavigationButtonClicked(object sender, RoutedEventArgs e)
		{
			//MoveToNextPost();
		}

		private async void MarkAllReadButtonClicked(object sender, RoutedEventArgs e)
		{
			var currentThread = ((sender as FrameworkElement)?.DataContext as ReadOnlyObservableGroup<CommentThread, Comment>)?.Key;
			if (currentThread == null) return;
			await _chattyManager.MarkCommentThreadRead(currentThread);
		}
		#endregion

		private void ShowHideReply(object sender)
		{
			return;
			//TODO: Hotkey??
			DependencyObject controlContainer = null;
			if (sender == null) return;
			var comment = ((sender as FrameworkElement)?.DataContext as Comment);
			if (comment == null)
			{
				//If it's the root post, it'll be a different data context.
				var currentThread = ((sender as FrameworkElement)?.DataContext as ReadOnlyObservableGroup<CommentThread, Comment>)?.Key;
				if (currentThread == null) return;
				//TODO - Realize the reply controls for root posts.
				//comment = currentThread.Comments.First();
				//controlContainer = (sender as FrameworkElement)?.FindFirstParentControlNamed<Grid>("HeaderContainer");
			}
			else
			{
				controlContainer = ThreadList.ContainerFromItem(comment);
			}

			if (controlContainer == null) return;
			var button = controlContainer.FindFirstControlNamed<ToggleButton>("showReply");
			if (button == null) return;
			var commentSection = controlContainer.FindFirstControlNamed<Grid>("commentSection");
			if (commentSection == null) return;
			commentSection.FindName("replyArea"); //Lazy load
			var replyControl = commentSection.FindFirstControlNamed<PostContol>("replyControl");
			if (replyControl == null) return;
			if (button.IsChecked.HasValue && button.IsChecked.Value)
			{
				replyControl.Visibility = Visibility.Visible;
				replyControl.SetShared(_authManager, Settings, _chattyManager);
				replyControl.SetFocus();
				replyControl.Closed += ReplyControl_Closed;
				replyControl.TextBoxGotFocus += ReplyControl_TextBoxGotFocus;
				replyControl.TextBoxLostFocus += ReplyControl_TextBoxLostFocus;
				replyControl.ShellMessage += ReplyControl_ShellMessage;
				replyControl.UpdateLayout();
				ThreadList.ScrollIntoView(comment);
			}
			else
			{
				Global.ShortcutKeysEnabled = true;
				replyControl.Closed -= ReplyControl_Closed;
				replyControl.TextBoxGotFocus -= ReplyControl_TextBoxGotFocus;
				replyControl.TextBoxLostFocus -= ReplyControl_TextBoxLostFocus;
				replyControl.ShellMessage -= ReplyControl_ShellMessage;
			}
		}
	}
}
