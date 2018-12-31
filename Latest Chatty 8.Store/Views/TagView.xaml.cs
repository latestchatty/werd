using System;
using Windows.UI.Xaml.Navigation;
using Autofac;
using Common;
using Latest_Chatty_8.Common;
using Latest_Chatty_8.Managers;
using Latest_Chatty_8.Networking;
using Latest_Chatty_8.Settings;

namespace Latest_Chatty_8.Views
{
	public sealed partial class TagView
	{
		private SeenPostsManager _seenPostsManager;
		private AuthenticationManager _authManager;
		private LatestChattySettings _settings;
		private ThreadMarkManager _markManager;
		private UserFlairManager _flairManager;
		private IgnoreManager _ignoreManager;

		public override string ViewTitle => "Tags";

		public override event EventHandler<LinkClickedEventArgs> LinkClicked = delegate { }; //Unused
		public override event EventHandler<ShellMessageEventArgs> ShellMessage = delegate { }; //Unused

		public TagView()
		{
			InitializeComponent();
		}

		protected async override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			var container = e.Parameter as IContainer;
			_seenPostsManager = container.Resolve<SeenPostsManager>();
			_authManager = container.Resolve<AuthenticationManager>();
			_settings = container.Resolve<LatestChattySettings>();
			_markManager = container.Resolve<ThreadMarkManager>();
			_flairManager = container.Resolve<UserFlairManager>();
			_ignoreManager = container.Resolve<IgnoreManager>();
			SingleThreadControl.Initialize(container);
			var commentThread = await JsonDownloader.Download(Locations.GetThread + "?id=" + "34139993");
			var parsedThread = (await CommentDownloader.TryParseThread(commentThread["threads"][0], 0, _seenPostsManager, _authManager, _settings, _markManager, _flairManager, _ignoreManager));
			parsedThread.RecalculateDepthIndicators();
			SingleThreadControl.DataContext = parsedThread;
		}
	}
}
