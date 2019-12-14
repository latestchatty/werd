using System;
using Windows.UI.Xaml.Navigation;
using Autofac;
using Common;
using Latest_Chatty_8.Common;
using Latest_Chatty_8.Managers;
using Latest_Chatty_8.Settings;
using Latest_Chatty_8.DataModel;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using System.Linq;
using Windows.UI.Core;
using Windows.UI.Xaml;
using System.Diagnostics;
using Windows.System;
using Windows.UI.Xaml.Controls.Primitives;

namespace Latest_Chatty_8.Views
{
	public sealed partial class PinnedThreadsView
	{
		public override string ViewTitle => "Pinned Threads";

		public override event EventHandler<LinkClickedEventArgs> LinkClicked = delegate { }; //Unused
		public override event EventHandler<ShellMessageEventArgs> ShellMessage = delegate { }; //Unused

		private CommentThread _selectedThread;
		public CommentThread SelectedThread
		{
			get => _selectedThread;
			set => SetProperty(ref _selectedThread, value);
		}

		private bool _threadsRefreshing;

		private ObservableCollection<CommentThread> PinnedThreads = new ObservableCollection<CommentThread>();

		private bool ThreadsRefreshing
		{
			get => _threadsRefreshing;
			set => this.SetProperty(ref _threadsRefreshing, value);
		}

		private IContainer _container;

		private LatestChattySettings Settings { get; set; }

		private ThreadMarkManager _markManager;
		private ChattyManager _chattyManager;
		private CoreWindow _keyBindWindow;
		private ChattySwipeOperation _swipeOp;
		private ChattySwipeOperation SwipeOp
		{
			get => _swipeOp;
			set => this.SetProperty(ref _swipeOp, value);
		}

		public PinnedThreadsView()
		{
			InitializeComponent();
		}

		#region Load and Save State

		protected async override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			var container = e.Parameter as IContainer;
			_container = container;
			Settings = container.Resolve<LatestChattySettings>();
			SwipeOp = Settings.ChattySwipeOperations.First(op => op.Type == ChattySwipeOperationType.Pin);
			_markManager = container.Resolve<ThreadMarkManager>();
			_chattyManager = container.Resolve<ChattyManager>();
			_keyBindWindow = CoreWindow.GetForCurrentThread();
			_keyBindWindow.KeyDown += Chatty_KeyDown;
			//EnableShortcutKeys();
			if (Settings.DisableSplitView)
			{
				VisualStateManager.GoToState(this, "VisualStatePhone", false);
			}
			visualState.CurrentStateChanging += VisualState_CurrentStateChanging;
			if (visualState.CurrentState == VisualStatePhone)
			{
				ThreadList.SelectNone();
			}
			await LoadThreads();
		}

		private void VisualState_CurrentStateChanging(object sender, VisualStateChangedEventArgs e)
		{
			if (e.NewState.Name == "VisualStatePhone")
			{
				ThreadList.SelectNone();
			}
			if (Settings.DisableSplitView)
			{
				VisualStateManager.GoToState(e.Control, "VisualStatePhone", false);
			}
		}

		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			base.OnNavigatingFrom(e);
			visualState.CurrentStateChanging -= VisualState_CurrentStateChanging;
			//DisableShortcutKeys();
			if (_keyBindWindow != null)
			{
				_keyBindWindow.KeyDown -= Chatty_KeyDown;
			}
		}

		private async void Chatty_KeyDown(CoreWindow sender, KeyEventArgs args)
		{
			try
			{
				if (!SingleThreadControl.ShortcutKeysEnabled)
				{
					Debug.WriteLine($"{GetType().Name} - Suppressed KeyDown event.");
					return;
				}

				switch (args.VirtualKey)
				{
					case VirtualKey.J:
						if (visualState.CurrentState != VisualStatePhone)
						{
							ThreadList.SelectPreviousThread();
						}
						break;
					case VirtualKey.K:
						if (visualState.CurrentState != VisualStatePhone)
						{
							ThreadList.SelectNextThread();
						}
						break;
					case VirtualKey.P:
						if (visualState.CurrentState != VisualStatePhone)
						{
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

		#endregion

		private async Task LoadThreads()
		{
			ThreadsRefreshing = true;
			SingleThreadControl.DataContext = null;
			var threadIds = await _markManager.GetAllMarkedThreadsOfType(MarkType.Pinned);
			PinnedThreads.Clear();
			foreach (var threadId in threadIds)
			{
				PinnedThreads.Add(await _chattyManager.FindOrAddThreadByAnyPostId(threadId));
			}
			ThreadsRefreshing = false;
		}

		private async void ListSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count == 1)
			{
				CommentThread ct = e.AddedItems[0] as CommentThread;
				if (visualState.CurrentState == VisualStatePhone)
				{
					SingleThreadControl.DataContext = null;
					await SingleThreadControl.Close();
					Frame.Navigate(typeof(SingleThreadView), new Tuple<IContainer, int, int>(_container, ct.Id, ct.Id));
				}
				else
				{
					if (ct == null) return;
					SingleThreadControl.Initialize(_container);
					SingleThreadControl.DataContext = ct;
					ThreadList.ScrollIntoView(ct);
				}
			}

			if (e.RemovedItems.Count > 0)
			{
				CommentThread ct = e.RemovedItems[0] as CommentThread;
				await _chattyManager.MarkCommentThreadRead(ct);
			}
		}

		private async void ThreadSwiped(object sender, Controls.ThreadSwipeEventArgs e)
		{
			if (e.Operation.Type != ChattySwipeOperationType.Pin) return;

			var ct = e.Thread;
			MarkType currentMark = _markManager.GetMarkType(ct.Id);
			if (currentMark != MarkType.Pinned)
			{
				await _markManager.MarkThread(ct.Id, MarkType.Pinned);
			}
			else if (currentMark == MarkType.Pinned)
			{
				await _markManager.MarkThread(ct.Id, MarkType.Unmarked);
			}
			await LoadThreads();
		}

		private async void PullToRefresh(object sender, RefreshRequestedEventArgs e) => await LoadThreads();

		private void InlineControlLinkClicked(object sender, LinkClickedEventArgs e) => LinkClicked?.Invoke(sender, e);

		private void InlineControlShellMessage(object sender, ShellMessageEventArgs e) => ShellMessage?.Invoke(sender, e);

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

				var threadId = await Networking.CommentDownloader.GetParentPostId(postId);

				if (_markManager.GetMarkType(threadId) == MarkType.Pinned) return;

				await _markManager.MarkThread(threadId, MarkType.Pinned);
				AddThreadButton.Flyout?.Hide();
				await LoadThreads();
			}
			finally
			{
				SubmitAddThreadButton.IsEnabled = true;
			}
		}
	}
}
