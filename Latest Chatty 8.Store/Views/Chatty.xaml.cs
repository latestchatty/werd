using Autofac;
using Common;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Werd.Common;
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
				await _chattyManager.MarkCommentThreadRead(ct).ConfigureAwait(false);
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
			if (e.PropertyName.Equals(nameof(ChattyManager.IsFullUpdateHappening)))
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
			await AppGlobal.DebugLog.AddMessage("Chatty-ResortClicked").ConfigureAwait(true);
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

		private async void SearchTextChanged(object sender, TextChangedEventArgs e)
		{
			if (ShowSearch)
			{
				TextBox searchTextBox = sender as TextBox;
				if (searchTextBox == null) return;
				await ChattyManager.SearchChatty(searchTextBox.Text).ConfigureAwait(true);
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
			await AppGlobal.DebugLog.AddMessage("Chatty-Filter-" + tagName).ConfigureAwait(true);
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
			await AppGlobal.DebugLog.AddMessage("Chatty-Sort-" + tagName).ConfigureAwait(true);
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
			_container.Resolve<INotificationManager>();
			_keyBindWindow = CoreWindow.GetForCurrentThread();
			_keyBindWindow.KeyDown += Chatty_KeyDown;
			_keyBindWindow.KeyUp += Chatty_KeyUp;
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
		}

		private bool _ctrlDown;

		private async void Chatty_KeyDown(CoreWindow sender, KeyEventArgs args)
		{
			try
			{
				if (!AppGlobal.ShortcutKeysEnabled)
				{
					await AppGlobal.DebugLog.AddMessage($"{GetType().Name} - Suppressed KeyDown event.").ConfigureAwait(true);
					return;
				}

				switch (args.VirtualKey)
				{
					case VirtualKey.Control:
						_ctrlDown = true;
						break;
					case VirtualKey.F5:
						await AppGlobal.DebugLog.AddMessage("Chatty-F5Pressed").ConfigureAwait(true);
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
							await AppGlobal.DebugLog.AddMessage("Chatty-PPressed").ConfigureAwait(true);
							if (SelectedThread != null)
							{
								await _markManager.MarkThread(SelectedThread.Id, _markManager.GetMarkType(SelectedThread.Id) != MarkType.Pinned ? MarkType.Pinned : MarkType.Unmarked).ConfigureAwait(true);
							}
						}
						break;
				}
				await AppGlobal.DebugLog.AddMessage($"{GetType().Name} - KeyDown event for {args.VirtualKey}").ConfigureAwait(true);
			}
			catch (Exception)
			{
				//(new Microsoft.ApplicationInsights.TelemetryClient()).TrackException(e, new Dictionary<string, string> { { "keyCode", args.VirtualKey.ToString() } });
			}
		}

		private async void Chatty_KeyUp(CoreWindow sender, KeyEventArgs args)
		{
			try
			{
				if (!AppGlobal.ShortcutKeysEnabled)
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
				await AppGlobal.DebugLog.AddException(string.Empty, e).ConfigureAwait(true);
				//(new Microsoft.ApplicationInsights.TelemetryClient()).TrackException(e, new Dictionary<string, string> { { "keyCode", args.VirtualKey.ToString() } });
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

		private async void ChattyPullRefresh(object sender, RefreshRequestedEventArgs e)
		{
			using (Windows.Foundation.Deferral _ = e.GetDeferral())
			{
				await ReSortChatty().ConfigureAwait(true);
			}
		}
	}
}
