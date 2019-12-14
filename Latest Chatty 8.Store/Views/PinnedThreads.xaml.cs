using System;
using Windows.UI.Xaml.Navigation;
using Autofac;
using Common;
using Latest_Chatty_8.Common;
using Latest_Chatty_8.Managers;
using Latest_Chatty_8.Networking;
using Latest_Chatty_8.Settings;
using Latest_Chatty_8.DataModel;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

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
		private SeenPostsManager _seenPostsManager;
		private AuthenticationManager _authManager;
		private UserFlairManager _flairManager;
		private IgnoreManager _ignoreManager;
		private ChattyManager _chattyManager;

		public PinnedThreadsView()
		{
			InitializeComponent();
		}

		protected async override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			var container = e.Parameter as IContainer;
			this._container = container;
			this.Settings = container.Resolve<LatestChattySettings>();
			this._markManager = container.Resolve<ThreadMarkManager>();
			_seenPostsManager = container.Resolve<SeenPostsManager>();
			_authManager = container.Resolve<AuthenticationManager>();
			_flairManager = container.Resolve<UserFlairManager>();
			_ignoreManager = container.Resolve<IgnoreManager>();
			//_chattyManager = container.Resolve<ChattyManager>();
			await LoadThreads();
			//SingleThreadControl.Initialize(container);
			//var commentThread = await JsonDownloader.Download(Locations.GetThread + "?id=" + "34139993");
			//var parsedThread = (await CommentDownloader.TryParseThread(commentThread["threads"][0], 0, _seenPostsManager, _authManager, _settings, _markManager, _flairManager, _ignoreManager));
			//parsedThread.RecalculateDepthIndicators();
			//SingleThreadControl.DataContext = parsedThread;
		}

		private async Task LoadThreads()
		{
			ThreadsRefreshing = true;
			SingleThreadControl.DataContext = null;
			var threadIds = await _markManager.GetAllMarkedThreadsOfType(MarkType.Pinned);
			PinnedThreads.Clear();
			foreach (var threadId in threadIds)
			{
				PinnedThreads.Add(await CommentDownloader.TryDownloadThreadById(threadId, _seenPostsManager, _authManager, Settings, _markManager, _flairManager, _ignoreManager));
			}
			ThreadsRefreshing = false;
		}

		private void ListSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count == 1)
			{
				CommentThread ct = e.AddedItems[0] as CommentThread;
				if (ct == null) return;

				//if (visualState.CurrentState == VisualStatePhone)
				//{
				//	SingleThreadControl.DataContext = null;
				//	await SingleThreadControl.Close();
				//	Frame.Navigate(typeof(SingleThreadView), new Tuple<IContainer, int, int>(_container, ct.Id, ct.Id));
				//}
				//else
				//{
					SingleThreadControl.Initialize(_container);
					SingleThreadControl.DataContext = ct;
				//}
				ThreadList.ScrollIntoView(ct);
			}

			//if (e.RemovedItems.Count > 0)
			//{
			//	CommentThread ct = e.RemovedItems[0] as CommentThread;
			//	await _chattyManager.MarkCommentThreadRead(ct);
			//}
		}

		private void ThreadSwiped(object sender, Controls.ThreadSwipeEventArgs e)
		{

		}

		private async void PullToRefresh(object sender, RefreshRequestedEventArgs e) => await LoadThreads();

		private void InlineControlLinkClicked(object sender, LinkClickedEventArgs e) => LinkClicked?.Invoke(sender, e);

		private void InlineControlShellMessage(object sender, ShellMessageEventArgs e) => ShellMessage?.Invoke(sender, e);
	}
}
