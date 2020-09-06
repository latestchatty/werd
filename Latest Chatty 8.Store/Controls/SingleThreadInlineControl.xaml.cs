﻿using Autofac;
using Common;
using Microsoft.Toolkit.Collections;
using Microsoft.Toolkit.Uwp.UI.Animations;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Werd.Common;
using Werd.DataModel;
using Werd.Managers;
using Werd.Settings;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using IContainer = Autofac.IContainer;

namespace Werd.Controls
{
	public sealed partial class SingleThreadInlineControl : INotifyPropertyChanged
	{
		public event EventHandler<LinkClickedEventArgs> LinkClicked;

		public event EventHandler<ShellMessageEventArgs> ShellMessage;

		public bool TruncateLongThreads { get; set; }

		public bool ShortcutKeysEnabled { get; set; } = true;

		private readonly ChattyManager _chattyManager;
		private readonly AuthenticationManager _authManager;
		private readonly IgnoreManager _ignoreManager;
		private readonly ThreadMarkManager _markManager;
		private readonly MessageManager _messageManager;
		private CoreWindow _keyBindWindow;
		private WebView _splitWebView;
		private readonly IContainer _container;
		bool _scrollingDown;
		Windows.UI.Xaml.Controls.Primitives.ScrollBar _threadListScrollBar;

		private LatestChattySettings npcSettings;
		private Comment _selectedComment;
		private Comment SelectedComment
		{
			get => _selectedComment;
			set
			{
				DebugLog.AddMessage($"{nameof(SingleThreadInlineControl)} - selecting thread id {value?.Id}").ConfigureAwait(true).GetAwaiter().GetResult();
				SetProperty(ref _selectedComment, value);
			}
		}
		private readonly CollectionViewSource GroupedChattyView;
		private readonly ObservableGroupedCollection<CommentThread, Comment> _groupedCommentCollection = new ObservableGroupedCollection<CommentThread, Comment>();
		private readonly ReadOnlyObservableGroupedCollection<CommentThread, Comment> GroupedCommentCollection;

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
			GroupedCommentCollection = new ReadOnlyObservableGroupedCollection<CommentThread, Comment>(_groupedCommentCollection);
			GroupedChattyView = new CollectionViewSource
			{
				IsSourceGrouped = true,
				Source = GroupedCommentCollection
			};
		}

		public async Task Close()
		{
			_commentsCurrentlyInView.Clear();
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
				_keyBindWindow = null;
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
		private void ControlDataContextChanged(FrameworkElement _, DataContextChangedEventArgs args)
		{
			DebugLog.AddMessage($"{nameof(SingleThreadInlineControl)} - starting data context change").ConfigureAwait(true).GetAwaiter().GetResult();
			var thread = args.NewValue as CommentThread;
			if (thread == null)
			{
				DebugLog.AddMessage("arg is null").ConfigureAwait(true).GetAwaiter().GetResult();
				return;
			}

			DebugLog.AddMessage($"Changing to thread id {thread.Id}").ConfigureAwait(true).GetAwaiter().GetResult();
			_groupedCommentCollection.Clear();
			_groupedCommentCollection.Add(thread.CompleteCommentsGroup);
			_commentsCurrentlyInView.Clear();
			CommentList.ItemsSource = GroupedChattyView.View;

			//TODO: What was this trying to solve? if (thread == CurrentThread) return;
			var shownWebView = false;
			SelectedComment = thread.Comments.FirstOrDefault();

			if (_keyBindWindow == null && !TruncateLongThreads) //Not sure what to do about hotkeys with the inline chatty yet.
			{
				_keyBindWindow = CoreWindow.GetForCurrentThread();
				_keyBindWindow.KeyDown += SingleThreadInlineControl_KeyDown;
				_keyBindWindow.KeyUp += SingleThreadInlineControl_KeyUp;
			}

			shownWebView = ShowSplitWebViewIfNecessary();

			if (!shownWebView)
			{
				CloseWebView();
				VisualStateManager.GoToState(this, "Default", false);
			}
		}

		private async void SingleThreadInlineControl_KeyUp(CoreWindow sender, KeyEventArgs args)
		{
			try
			{
				if (!AppGlobal.ShortcutKeysEnabled || !ShortcutKeysEnabled) //Not sure what to do about hotkeys with the inline chatty yet.
				{
					//await DebugLog.AddMessage($"Keypress suppressed G:{AppGlobal.ShortcutKeysEnabled} L:{ShortcutKeysEnabled}").ConfigureAwait(true);
					return;
				}

				switch (args.VirtualKey)
				{
					case VirtualKey.R:
						if (SelectedComment == null) return;
						ShowReplyForComment(SelectedComment);
						break;
				}
			}
			catch (Exception e)
			{
				await DebugLog.AddException(string.Empty, e).ConfigureAwait(false);
				//(new Microsoft.ApplicationInsights.TelemetryClient()).TrackException(e, new Dictionary<string, string> { { "keyCode", args.VirtualKey.ToString() } });
			}

		}

		private async void SingleThreadInlineControl_KeyDown(CoreWindow sender, KeyEventArgs args)
		{
			try
			{
				if (!AppGlobal.ShortcutKeysEnabled || !ShortcutKeysEnabled)
				{
					//await DebugLog.AddMessage($"Keypress suppressed G:{AppGlobal.ShortcutKeysEnabled} L:{ShortcutKeysEnabled}").ConfigureAwait(true);
					return;
				}

				switch (args.VirtualKey)
				{
					case VirtualKey.A:
						if (SelectedComment == null) break;
						SelectedComment = await _chattyManager.SelectNextComment(SelectedComment.Thread, false, TruncateLongThreads).ConfigureAwait(true);
						CommentList.ScrollIntoView(SelectedComment);
						break;
					case VirtualKey.Z:
						if (SelectedComment == null) break;
						SelectedComment = await _chattyManager.SelectNextComment(SelectedComment.Thread, true, TruncateLongThreads).ConfigureAwait(true);
						CommentList.ScrollIntoView(SelectedComment);
						break;
				}
			}
			catch (Exception e)
			{
				await DebugLog.AddException(string.Empty, e).ConfigureAwait(false);
				//(new Microsoft.ApplicationInsights.TelemetryClient()).TrackException(e, new Dictionary<string, string> { { "keyCode", args.VirtualKey.ToString() } });
			}
		}

		//private void CurrentWebView_Resized(object sender, EventArgs e)
		//{
		//	CommentList.ScrollIntoView(CommentList.SelectedItem);
		//}

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

		private async void CommentList_ItemClick(object sender, ItemClickEventArgs e)
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
					if (_chattyManager.IsFullUpdateHappening) return;
					var lv = sender as ListView;
					if (lv == null) return; //This would be bad.

					SelectedComment = comment;
					if (comment == null) return; //Bail, we don't know what to
					await _chattyManager.DeselectAllPostsForCommentThread(comment.Thread).ConfigureAwait(true);

					//If the selection is a post other than the OP, untruncate the thread to prevent problems when truncated posts update.
					if (comment.Thread.Id != comment.Id && comment.Thread.TruncateThread)
					{
						comment.Thread.TruncateThread = false;
					}

					comment.IsSelected = true;
					lv.UpdateLayout();
					lv.ScrollIntoView(comment);
				}
			}
			catch { }
		}

		private void PostListViewItem_ShowReply(object sender, CommentEventArgs e)
		{
			ShowReplyForComment(e.Comment);
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			this.SizeChanged += ControlSizeChanged;
			Settings.PropertyChanged += Settings_PropertyChanged;
		}

		private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName.Equals(nameof(Settings.UseSmoothScrolling), StringComparison.InvariantCulture))
			{
				SetListScrollViewerSmoothing();
			}
		}

		private void ControlSizeChanged(object sender, SizeChangedEventArgs e)
		{
			SetReplyBounds();
		}

		private void UserControl_Unloaded(object sender, RoutedEventArgs e)
		{
			this.SizeChanged -= ControlSizeChanged;
			Settings.PropertyChanged -= Settings_PropertyChanged;
		}

		private void ToggleLargeReply(object sender, RoutedEventArgs e)
		{
			Settings.LargeReply = !Settings.LargeReply;
			SetReplyBounds();
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
			CommentList.ScrollIntoView(SelectedComment, ScrollIntoViewAlignment.Leading);
		}

		Dictionary<int, Comment> _commentsCurrentlyInView = new Dictionary<int, Comment>();

		private async void ListViewItemViewportChanged(FrameworkElement sender, EffectiveViewportChangedEventArgs args)
		{
			if (!AppGlobal.Settings.MarkCommentReadDuringScroll) return;

			var comment = sender.DataContext as Comment;
			if (comment is null) return;
			if (args.BringIntoViewDistanceY < sender.ActualHeight)
			{
				if (!_commentsCurrentlyInView.ContainsKey(comment.Id)) _commentsCurrentlyInView.Add(comment.Id, comment);
			}
			else
			{
				if (_commentsCurrentlyInView.Remove(comment.Id))
				{
					//If we're not scrolling down, don't mark it read.
					if (!_scrollingDown) return;
					//It was in the collection, now it's not. Mark it read.
					await _chattyManager.MarkCommentRead(comment).ConfigureAwait(false);
				}
			}
		}


		private void CommentListLoaded(object sender, RoutedEventArgs e)
		{
			SetListScrollViewerSmoothing();
			_threadListScrollBar = CommentList.FindDescendant<Windows.UI.Xaml.Controls.Primitives.ScrollBar>();
			_threadListScrollBar.ValueChanged += ScrollBar_ValueChanged;
		}

		private void ScrollBar_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
		{
			_scrollingDown = e.OldValue < e.NewValue;
		}

		private void CommentListUnloaded(object sender, RoutedEventArgs e)
		{
			if (_threadListScrollBar != null)
			{
				_threadListScrollBar.ValueChanged -= ScrollBar_ValueChanged;
			}
		}
		#endregion

		#region Helpers
		private void SetListScrollViewerSmoothing()
		{
			var scrollViewer = CommentList.FindDescendant<ScrollViewer>();
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

		private void SetReplyBounds()
		{
			if (replyBox is null) return;

			var windowSize = new Size(this.ActualWidth, this.ActualHeight);
			if (Settings.LargeReply)
			{
				replyBox.MinHeight = replyBox.MaxHeight = this.ActualHeight - NavigationBar.ActualHeight - 20;
				replyBox.MinWidth = replyBox.MaxWidth = this.ActualWidth - 20;
			}
			else
			{
				replyBox.MinHeight = replyBox.MaxHeight = windowSize.Height / 1.75;
				replyBox.MinWidth = replyBox.MaxWidth = windowSize.Width / 1.5;
				if (windowSize.Height < 600) replyBox.MaxHeight = double.PositiveInfinity;
				if (windowSize.Width < 900) replyBox.MaxWidth = double.PositiveInfinity;
			}
		}

		private bool ShowSplitWebViewIfNecessary()
		{
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
								_splitWebView = new WebView(WebViewExecutionMode.SeparateThread);
								Grid.SetRow(_splitWebView, 0);
								WebViewContainer.Children.Add(_splitWebView);
								_splitWebView.Navigate(storyUrl);
								VisualStateManager.GoToState(this, "WebViewShown", false);
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
			await _chattyManager.SelectNextComment(currentThread, false, TruncateLongThreads).ConfigureAwait(true);
		}

		private async void MoveToNextPost()
		{
			var currentThread = DataContext as CommentThread;
			if (currentThread is null) return;
			await _chattyManager.SelectNextComment(currentThread, true, TruncateLongThreads).ConfigureAwait(true);
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
