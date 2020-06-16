using Autofac;
using Common;
using Latest_Chatty_8.Common;
using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Managers;
using Latest_Chatty_8.Settings;
using Microsoft.HockeyApp;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
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

		#region Thread View

		#endregion

		private async void ChattyListSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count == 1)
			{
				CommentThread ct = e.AddedItems[0] as CommentThread;
				if (ct == null) return;

				if (visualState.CurrentState == VisualStatePhone)
				{
					SingleThreadControl.DataContext = null;
					await SingleThreadControl.Close();
					Frame.Navigate(typeof(SingleThreadView), new Tuple<IContainer, int, int>(_container, ct.Id, ct.Id));
				}
				else
				{
					SingleThreadControl.Initialize(_container);
					SingleThreadControl.DataContext = ct;
				}
				ThreadList.ScrollIntoView(ct);
			}

			if (e.RemovedItems.Count > 0)
			{
				CommentThread ct = e.RemovedItems[0] as CommentThread;
				await _chattyManager.MarkCommentThreadRead(ct);
			}
		}

		#region Events

		private async void MarkAllRead(object sender, RoutedEventArgs e)
		{
			await _chattyManager.MarkAllVisibleCommentsRead();
		}

		private async void PinClicked(object sender, RoutedEventArgs e)
		{
			MenuFlyoutItem flyout = sender as MenuFlyoutItem;
			if (flyout == null) return;
			CommentThread commentThread = flyout.DataContext as CommentThread;
			if (commentThread == null) return;
			if (_markManager.GetMarkType(commentThread.Id) == MarkType.Pinned)
			{
				await _markManager.MarkThread(commentThread.Id, MarkType.Unmarked);
			}
			else
			{
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
				await _markManager.MarkThread(commentThread.Id, MarkType.Unmarked);
			}
			else
			{
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

		private async void ChattyManager_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName.Equals(nameof(ChattyManager.IsFullUpdateHappening)))
			{
				if (ChattyManager.IsFullUpdateHappening)
				{
					SingleThreadControl.DataContext = null;
					await SingleThreadControl.Close();
				}
			}
		}
		#endregion


		private async void ReSortClicked(object sender, RoutedEventArgs e)
		{
			HockeyClient.Current.TrackEvent("Chatty-ResortClicked");
			await ReSortChatty();
		}

		private async Task ReSortChatty()
		{
			//TODO: Pin - SelectedThread = null;
			SingleThreadControl.DataContext = null;

			if (Settings.MarkReadOnSort)
			{
				await _chattyManager.MarkAllVisibleCommentsRead();
			}
			await _chattyManager.CleanupChattyList();
			ThreadList.ScrollToTop();
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
			_container.Resolve<INotificationManager>();
			_keyBindWindow = CoreWindow.GetForCurrentThread();
			_keyBindWindow.KeyDown += Chatty_KeyDown;
			_keyBindWindow.KeyUp += Chatty_KeyUp;
			ChattyManager.PropertyChanged += ChattyManager_PropertyChanged;
			EnableShortcutKeys();
			if (Settings.DisableSplitView)
			{
				VisualStateManager.GoToState(this, "VisualStatePhone", false);
			}
			visualState.CurrentStateChanging += VisualState_CurrentStateChanging;
			if (visualState.CurrentState == VisualStatePhone)
			{
				ThreadList.SelectNone();
			}
		}

		private void VisualState_CurrentStateChanging(object sender, VisualStateChangedEventArgs e)
		{
			if (Settings.DisableSplitView)
			{
				VisualStateManager.GoToState(e.Control, "VisualStatePhone", false);
			}
		}

		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			base.OnNavigatingFrom(e);
			ChattyManager.PropertyChanged -= ChattyManager_PropertyChanged;
			visualState.CurrentStateChanging -= VisualState_CurrentStateChanging;
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
					case VirtualKey.J:
						if (visualState.CurrentState != VisualStatePhone && !_ctrlDown)
						{
							ThreadList.SelectPreviousThread();
						}
						break;
					case VirtualKey.K:
						if (visualState.CurrentState != VisualStatePhone && !_ctrlDown)
						{
							ThreadList.SelectNextThread();
						}
						break;
					case VirtualKey.P:
						if (visualState.CurrentState != VisualStatePhone && !_ctrlDown)
						{
							HockeyClient.Current.TrackEvent("Chatty-PPressed");
							if (SelectedThread != null)
							{
								await _markManager.MarkThread(SelectedThread.Id, _markManager.GetMarkType(SelectedThread.Id) != MarkType.Pinned ? MarkType.Pinned : MarkType.Unmarked);
							}
						}
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

		private void SearchTextBoxLostFocus(object sender, RoutedEventArgs e)
		{
			EnableShortcutKeys();
		}

		private void SearchTextBoxGotFocus(object sender, RoutedEventArgs e)
		{
			DisableShortcutKeys();
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

		private async void ThreadSwiped(object sender, Controls.ThreadSwipeEventArgs e)
		{
			var ct = e.Thread;
			MarkType currentMark = _markManager.GetMarkType(ct.Id);
			switch (e.Operation.Type)
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
					break;
				case ChattySwipeOperationType.MarkRead:
					await ChattyManager.MarkCommentThreadRead(ct);
					break;
			}
		}

		private async void ChattyPullRefresh(object sender, RefreshRequestedEventArgs e)
		{
			using (Windows.Foundation.Deferral _ = e.GetDeferral())
			{
				await ReSortChatty();
			}
		}
	}
}
