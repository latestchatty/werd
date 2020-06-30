using Autofac;
using System;
using Werd.Common;
using Werd.Managers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;

namespace Werd.Views
{
	public sealed partial class SingleThreadView
	{
		public override string ViewTitle => "Single Thread";

		public override event EventHandler<LinkClickedEventArgs> LinkClicked;
		public override event EventHandler<ShellMessageEventArgs> ShellMessage;

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
				var thread = await chattyManager.FindOrAddThreadByAnyPostId(navArg.Item2);
				if (thread == null)
				{
					ShellMessage?.Invoke(this,
						new ShellMessageEventArgs($"Couldn't load thread for id {navArg.Item2}.",
							ShellMessageType.Error));
				}
				ThreadView.DataContext = thread;
			}

			if (navArg != null) ThreadView.SelectPostId(navArg.Item3);
			LoadingBar.Visibility = Visibility.Collapsed;
			LoadingBar.IsActive = false;
		}

		protected async override void OnNavigatedFrom(NavigationEventArgs e)
		{
			base.OnNavigatedFrom(e);
			await ThreadView.Close();
		}

		private void InlineLinkClicked(object sender, LinkClickedEventArgs e)
		{
			if (LinkClicked != null)
			{
				LinkClicked(sender, e);
			}
		}

		private void InlineShellMessage(object sender, ShellMessageEventArgs e)
		{
			if (ShellMessage != null)
			{
				ShellMessage(sender, e);
			}
		}
	}
}
