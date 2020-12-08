using Autofac;
using Common;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Werd.Common;
using Werd.DataModel;
using Werd.Managers;
using Werd.Settings;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Werd.Views
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

		private readonly ObservableCollection<CommentThread> PinnedThreads = new ObservableCollection<CommentThread>();

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
			_markManager = container.Resolve<ThreadMarkManager>();
			_chattyManager = container.Resolve<ChattyManager>();
			_keyBindWindow = CoreWindow.GetForCurrentThread();
			_keyBindWindow.KeyDown += Chatty_KeyDown;
			AppGlobal.ShortcutKeysEnabled = true;
			await LoadThreads().ConfigureAwait(true);
		}

		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			base.OnNavigatingFrom(e);
			if (_keyBindWindow != null)
			{
				_keyBindWindow.KeyDown -= Chatty_KeyDown;
			}
		}

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
					case VirtualKey.J:
						ThreadList.SelectPreviousThread();
						break;
					case VirtualKey.K:
						ThreadList.SelectNextThread();
						break;
					case VirtualKey.P:
						if (SelectedThread != null)
						{
							await _markManager.MarkThread(SelectedThread.Id, _markManager.GetMarkType(SelectedThread.Id) != MarkType.Pinned ? MarkType.Pinned : MarkType.Unmarked).ConfigureAwait(true);
						}
						break;
				}
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
			var threadIds = (await _markManager.GetAllMarkedThreadsOfType(MarkType.Pinned).ConfigureAwait(true)).OrderByDescending(t => t);
			PinnedThreads.Clear();
			foreach (var threadId in threadIds)
			{
				PinnedThreads.Add(await _chattyManager.FindOrAddThreadByAnyPostId(threadId).ConfigureAwait(true));
			}
			ThreadsRefreshing = false;
		}

		private async void ListSelectionChanged(object sender, SelectionChangedEventArgs e)
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
				await _chattyManager.MarkCommentThreadRead(ct).ConfigureAwait(true);
			}
		}

		private async void ThreadSwiped(object sender, Controls.ThreadSwipeEventArgs e)
		{
			if ((e.Operation != ChattySwipeOperationType.Pin) && (e.Operation != ChattySwipeOperationType.Collapse)) return;
			await LoadThreads().ConfigureAwait(false);
		}

		private async void PullToRefresh(object sender, RefreshRequestedEventArgs e) => await LoadThreads().ConfigureAwait(true);

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

				var threadId = await Networking.CommentDownloader.GetRootPostId(postId).ConfigureAwait(true);

				if (_markManager.GetMarkType(threadId) == MarkType.Pinned) return;

				await _markManager.MarkThread(threadId, MarkType.Pinned).ConfigureAwait(true);
				AddThreadButton.Flyout?.Hide();
				await LoadThreads().ConfigureAwait(true);
			}
			catch (Exception ex)
			{
				await DebugLog.AddException(string.Empty, ex).ConfigureAwait(true);
				ShellMessage?.Invoke(this, new ShellMessageEventArgs("Error occurred adding pinned thread: " + Environment.NewLine + ex.Message, ShellMessageType.Error));
			}
			finally
			{
				SubmitAddThreadButton.IsEnabled = true;
			}
		}
	}
}
