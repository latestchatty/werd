using Autofac;
using Common;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Werd.Common;
using Werd.DataModel;
using Werd.Managers;
using Werd.Settings;
using Werd.Views;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using IContainer = Autofac.IContainer;

namespace Werd.Controls
{
	public sealed partial class SingleThreadInlineControl : INotifyPropertyChanged
	{
		public event EventHandler<LinkClickedEventArgs> LinkClicked;

		public event EventHandler<ShellMessageEventArgs> ShellMessage;

		public bool TruncateLongThreads { get; set; } = false;

		private readonly ChattyManager _chattyManager;
		private readonly AuthenticationManager _authManager;
		private readonly IgnoreManager _ignoreManager;
		private readonly ThreadMarkManager _markManager;
		private readonly MessageManager _messageManager;
		private CoreWindow _keyBindWindow;
		private WebView _splitWebView;
		private readonly IContainer _container;

		private LatestChattySettings npcSettings;
		private Comment _selectedComment;

		private LatestChattySettings Settings
		{
			get => npcSettings;
			set => SetProperty(ref npcSettings, value);
		}
		public SingleThreadInlineControl()
		{
			InitializeComponent();
			_chattyManager = AppGlobal.Container.Resolve<ChattyManager>();
			Settings = AppGlobal.Container.Resolve<LatestChattySettings>();
			_authManager = AppGlobal.Container.Resolve<AuthenticationManager>();
			_ignoreManager = AppGlobal.Container.Resolve<IgnoreManager>();
			_markManager = AppGlobal.Container.Resolve<ThreadMarkManager>();
			_messageManager = AppGlobal.Container.Resolve<MessageManager>();
			_container = AppGlobal.Container;
		}

		public async Task Close()
		{
			var currentThread = DataContext as CommentThread;
			if (currentThread != null)
			{
				await _chattyManager.DeselectAllPostsForCommentThread(currentThread).ConfigureAwait(true);
			}
			CommentList.ItemsSource = null;
			if (_keyBindWindow != null)
			{
				_keyBindWindow.KeyDown -= SingleThreadInlineControl_KeyDown;
				_keyBindWindow.KeyUp -= SingleThreadInlineControl_KeyUp;
			}
			CloseWebView();
		}

		public void SelectPostId(int id)
		{
			var currentThread = DataContext as CommentThread;
			if (currentThread == null) return;
			var comment = currentThread.Comments.SingleOrDefault(c => c.Id == id);
			if (comment == null) return;
			CommentList.SelectedValue = comment;
		}

		#region Events
		private void ControlDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
		{
			var thread = args.NewValue as CommentThread;
			if (thread == null) return;
			//TODO: What was this trying to solve? if (thread == CurrentThread) return;
			var shownWebView = false;
			if (_selectedComment == null) _selectedComment = thread.Comments.FirstOrDefault();

			if (_keyBindWindow == null && !TruncateLongThreads) //Not sure what to do about hotkeys with the inline chatty yet.
			{
				_keyBindWindow = CoreWindow.GetForCurrentThread();
				_keyBindWindow.KeyDown += SingleThreadInlineControl_KeyDown;
				_keyBindWindow.KeyUp += SingleThreadInlineControl_KeyUp;
			}

			CommentList.ItemsSource = thread.Comments;

			shownWebView = ShowSplitWebViewIfNecessary();

			if (!TruncateLongThreads)
			{
				FindName(nameof(NavigationBarView));
			}

			if (!shownWebView)
			{
				CloseWebView();
				VisualStateManager.GoToState(this, "Default", false);
			}
		}

		private async void CollapseThreadClicked(object sender, RoutedEventArgs e)
		{
			var currentThread = DataContext as CommentThread;
			if (currentThread == null) return;
			await _markManager.MarkThread(currentThread.Id, currentThread.IsCollapsed ? MarkType.Unmarked : MarkType.Collapsed);
		}
		private async void PinThreadClicked(object sender, RoutedEventArgs e)
		{
			var currentThread = DataContext as CommentThread;
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
			var lv = sender as ListView;
			if (lv == null) return; //This would be bad.
			var currentThread = DataContext as CommentThread;
			if (currentThread == null) return;
			await _chattyManager.DeselectAllPostsForCommentThread(currentThread).ConfigureAwait(true);

			if (e.AddedItems.Count == 1)
			{
				var selectedItem = e.AddedItems[0] as Comment;
				if (selectedItem == null) return; //Bail, we don't know what to
												  //If the selection is a post other than the OP, untruncate the thread to prevent problems when truncated posts update.
				_selectedComment = selectedItem;
				await AppGlobal.DebugLog.AddMessage($"Selected comment - {selectedItem.Id} - {selectedItem.Preview}").ConfigureAwait(true);
				await _chattyManager.MarkCommentRead(selectedItem).ConfigureAwait(true);
				selectedItem.IsSelected = true;
				lv.UpdateLayout();
				lv.ScrollIntoView(selectedItem);
			}
		}

		private async void SingleThreadInlineControl_KeyUp(CoreWindow sender, KeyEventArgs args)
		{
			try
			{
				if (!AppGlobal.ShortcutKeysEnabled) //Not sure what to do about hotkeys with the inline chatty yet.
				{
					return;
				}

				switch (args.VirtualKey)
				{
					case VirtualKey.R:
						if (_selectedComment == null) return;
						_selectedComment.ShowReply = true;
						SetReplyFocus(_selectedComment);
						break;
				}
			}
			catch (Exception e)
			{
				await AppGlobal.DebugLog.AddException(string.Empty, e).ConfigureAwait(false);
				//(new Microsoft.ApplicationInsights.TelemetryClient()).TrackException(e, new Dictionary<string, string> { { "keyCode", args.VirtualKey.ToString() } });
			}

		}

		private async void SingleThreadInlineControl_KeyDown(CoreWindow sender, KeyEventArgs args)
		{
			try
			{
				if (!AppGlobal.ShortcutKeysEnabled) //Not sure what to do about hotkeys with the inline chatty yet.
				{
					return;
				}

				switch (args.VirtualKey)
				{
					case VirtualKey.A:
						if (_selectedComment == null) break;
						_selectedComment = await _chattyManager.SelectNextComment(_selectedComment.Thread, false, false).ConfigureAwait(true);
						CommentList.ScrollIntoView(_selectedComment);
						break;
					case VirtualKey.Z:
						if (_selectedComment == null) break;
						_selectedComment = await _chattyManager.SelectNextComment(_selectedComment.Thread, true, false).ConfigureAwait(true);
						CommentList.ScrollIntoView(_selectedComment);
						break;
				}
			}
			catch (Exception e)
			{
				await AppGlobal.DebugLog.AddException(string.Empty, e);
				//(new Microsoft.ApplicationInsights.TelemetryClient()).TrackException(e, new Dictionary<string, string> { { "keyCode", args.VirtualKey.ToString() } });
			}
		}

		//private void CurrentWebView_Resized(object sender, EventArgs e)
		//{
		//	CommentList.ScrollIntoView(CommentList.SelectedItem);
		//}

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
			var controlContainer = CommentList.ContainerFromItem(comment);
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
				}
				catch (Exception ex)
				{
					await AppGlobal.DebugLog.AddException(string.Empty, ex);
					ShellMessage?.Invoke(this, new ShellMessageEventArgs("Problem tagging, try again later.", ShellMessageType.Error));
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
				var lolUrl = Locations.GetLolTaggersUrl((s.DataContext as Comment).Id, tag);
				var response = await JsonDownloader.DownloadObject(lolUrl);
				var names = string.Join(Environment.NewLine, response["data"][0]["usernames"].Select(a => a.ToString()).OrderBy(a => a));
				var flyout = new Flyout();
				var tb = new TextBlock();
				tb.Text = names;
				flyout.Content = tb;
				flyout.ShowAt(s);
			}
			catch (Exception ex)
			{
				await AppGlobal.DebugLog.AddException(string.Empty, ex);
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

		private void ReplyControl_TextBoxLostFocus(object sender, EventArgs e)
		{
			AppGlobal.ShortcutKeysEnabled = true;
		}

		private void ReplyControl_TextBoxGotFocus(object sender, EventArgs e)
		{
			AppGlobal.ShortcutKeysEnabled = false;
		}

		private void ReplyControl_ShellMessage(object sender, ShellMessageEventArgs args)
		{
			ShellMessage?.Invoke(sender, args);
		}

		private void ReplyControl_Closed(object sender, EventArgs e)
		{
			AppGlobal.ShortcutKeysEnabled = true;
		}

		private void CopyPostLinkClicked(object sender, RoutedEventArgs e)
		{
			var button = sender as Button;
			if (button == null) return;
			var comment = button.DataContext as Comment;
			if (comment == null) return;
			var dataPackage = new DataPackage();
			dataPackage.SetText($"http://www.shacknews.com/chatty?id={comment.Id}#item_{comment.Id}");
			Settings.LastClipboardPostId = comment.Id;
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
			MoveToPreviousPost();
		}

		private void NextNavigationButtonClicked(object sender, RoutedEventArgs e)
		{
			MoveToNextPost();
		}

		private async void MarkAllReadButtonClicked(object sender, RoutedEventArgs e)
		{
			var currentThread = DataContext as CommentThread;
			if (currentThread == null) return;
			await _chattyManager.MarkCommentThreadRead(currentThread);
		}

		private void ToggleShowReplyClicked(object sender, RoutedEventArgs e)
		{
			var button = sender as CustomToggleButton;
			if (button == null) return;
			var comment = button.DataContext as Comment;
			if (comment == null) return;
			if (button.IsChecked.HasValue && button.IsChecked.Value) SetReplyFocus(comment);
		}

		private async void PreviewFlyoutOpened(object sender, object e)
		{
			var comment = (((sender as Flyout)?.Content as FrameworkElement)?.DataContext as Comment);
			if (comment == null) return;
			await _chattyManager.MarkCommentRead(comment).ConfigureAwait(true);
		}
		private void PreviewEffectiveViewportChanged(FrameworkElement sender, EffectiveViewportChangedEventArgs args)
		{
			if (sender.DataContext == null) return;
			//There's probably a more efficient way to do this, but at least this only updates if things are within the scrolling viewport.
			if (args.BringIntoViewDistanceY < sender.ActualHeight)
			{
				var container = CommentList.ContainerFromItem(sender.DataContext) as FrameworkElement;
				if (container == null) return;
				var previewBlock = container.FindFirstControlNamed<TextBlock>("PreviewTextBlock");
				var depthImage = container.FindFirstControlNamed<Image>("Depth");
				var authorBlock = container.FindFirstControlNamed<StackPanel>("AuthorPanel");
				previewBlock.MaxWidth = Math.Max(container.ActualWidth - depthImage.ActualWidth - authorBlock.ActualWidth - 32, 0);
				//Global.DebugLog.AddMessage($"{(sender.DataContext as Comment).Preview}");
			}
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

		#endregion

		#region Helpers
		private void SetReplyFocus(Comment comment)
		{
			var container = CommentList.ContainerFromItem(comment);
			var reply = container?.FindFirstControlNamed<PostContol>("replyControl");
			reply?.SetFocus();
		}

		private bool ShowSplitWebViewIfNecessary()
		{
			if (TruncateLongThreads) return false; //If it'w truncated, it's inline. Don't show the split view.
			var shownWebView = false;
			if (!Settings.DisableNewsSplitView)
			{
				var currentThread = DataContext as CommentThread;
				if (currentThread == null) return false;
				var firstComment = currentThread.Comments.FirstOrDefault();
				try
				{
					if (firstComment != null)
					{
						if (firstComment.AuthorType == AuthorType.Shacknews)
						{
							//Find the first href.
							var find = "<a target=\"_blank\" href=\"";
							var urlStart = firstComment.Body.IndexOf(find, StringComparison.Ordinal);
							var urlEnd = firstComment.Body.IndexOf("\"", urlStart + find.Length, StringComparison.Ordinal);
							if (urlStart > 0 && urlEnd > 0)
							{
								var storyUrl = new Uri(firstComment.Body.Substring(urlStart + find.Length, urlEnd - (urlStart + find.Length)));
								FindName(nameof(WebViewContainer)); //Realize the container since it's deferred.
								VisualStateManager.GoToState(this, "WebViewShown", false);
								_splitWebView = new WebView(WebViewExecutionMode.SeparateThread);
								Grid.SetRow(_splitWebView, 0);
								WebViewContainer.Children.Add(_splitWebView);
								_splitWebView.Navigate(storyUrl);
								shownWebView = true;
							}
						}
					}
				}
				catch
				{
					// ignored
				}
			}

			return shownWebView;
		}

		private void CloseWebView()
		{
			if (_splitWebView != null)
			{
				_splitWebView.Stop();
				_splitWebView.NavigateToString("");
				WebViewContainer.Children.Remove(_splitWebView);
				_splitWebView = null;
			}
		}

		private async void MoveToPreviousPost()
		{
			var currentThread = DataContext as CommentThread;
			if (currentThread is null) return;
			await _chattyManager.SelectNextComment(currentThread, false, false).ConfigureAwait(true);
		}

		private async void MoveToNextPost()
		{
			var currentThread = DataContext as CommentThread;
			if (currentThread is null) return;
			await _chattyManager.SelectNextComment(currentThread, true, false).ConfigureAwait(true);
		}
		#endregion

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


	}
}
