using Autofac;
using System;
using Werd.Common;
using Werd.DataModel;
using Werd.Managers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Werd.Views
{
	public sealed partial class SingleThreadView
	{

		public override Brush IconColor { get => (SolidColorBrush)Application.Current.Resources["ThemeHighlight"]; }
		private string _viewTitle = "Single Thread";
		public override string ViewTitle { get => _viewTitle; set => SetProperty(ref _viewTitle, value); }

		private string _viewIcons = "";
		public override string ViewIcons { get => _viewIcons; set => SetProperty(ref _viewIcons, value); }

		public override event EventHandler<LinkClickedEventArgs> LinkClicked;
		public override event EventHandler<ShellMessageEventArgs> ShellMessage;
		public CommentThread CommentThread { get; private set; }

		public SingleThreadView()
		{
			InitializeComponent();
		}

		protected async override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			LoadingBar.Visibility = Visibility.Visible;
			LoadingBar.IsActive = true;
			var navArg = e.Parameter as Tuple<IContainer, int, int>;
			if (navArg == null)
			{
				if (Frame.CanGoBack)
				{
					Frame.GoBack();
				}
			}
			var chattyManager = navArg?.Item1.Resolve<ChattyManager>();

			if (chattyManager != null)
			{
				var thread = await chattyManager.FindOrAddThreadByAnyPostId(navArg.Item2).ConfigureAwait(true);
				if (thread == null)
				{
					ShellMessage?.Invoke(this,
						new ShellMessageEventArgs($"Couldn't load thread for id {navArg.Item2}.",
							ShellMessageType.Error));
				}
				ThreadView.DataContext = thread;
				CommentThread = thread;
				thread.PropertyChanged += Thread_PropertyChanged;
				ViewTitle = thread.Comments[0].Preview;
			}

			if (navArg != null) ThreadView.SelectPostId(navArg.Item3);
			LoadingBar.Visibility = Visibility.Collapsed;
			LoadingBar.IsActive = false;
		}

		private void Thread_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName.Equals(nameof(CommentThread.HasNewReplies), StringComparison.Ordinal)
				|| e.PropertyName.Equals(nameof(CommentThread.HasNewRepliesToUser), StringComparison.Ordinal))
			{
				SetViewIcons();
			}
		}

		protected async override void OnNavigatedFrom(NavigationEventArgs e)
		{
			base.OnNavigatedFrom(e);
			await ThreadView.Close().ConfigureAwait(true);
		}

		private void ThreadView_LinkClicked(object sender, LinkClickedEventArgs e)
		{
			LinkClicked?.Invoke(sender, e);
		}

		private void ThreadView_ShellMessage(object sender, ShellMessageEventArgs e)
		{
			ShellMessage?.Invoke(sender, e);
		}

		private void SetViewIcons()
		{
			ViewIcons = (CommentThread.HasNewReplies ? "\uE735 " : "") + (CommentThread.HasNewRepliesToUser ? "\uE8BD " : "");
		}
	}
}
