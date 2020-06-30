using Autofac;
using Common;
using System;
using Werd.Common;
using Werd.Managers;
using Werd.Settings;
using Windows.UI.Xaml.Navigation;

namespace Werd.Views
{
	public sealed partial class NewRootPostView
	{
		public NewRootPostView()
		{
			InitializeComponent();
		}

		public override string ViewTitle => "New root post";

		public override event EventHandler<LinkClickedEventArgs> LinkClicked = delegate { };
		public override event EventHandler<ShellMessageEventArgs> ShellMessage = delegate { };

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			var container = e.Parameter as IContainer;
			PostControl.SetShared(container.Resolve<AuthenticationManager>(), container.Resolve<LatestChattySettings>(), container.Resolve<ChattyManager>());
			PostControl.Closed += PostControl_Closed;
			PostControl.UpdateLayout();
			PostControl.SetFocus();
		}

		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			base.OnNavigatingFrom(e);
			PostControl.Closed -= PostControl_Closed;
		}

		private void PostControl_Closed(object sender, EventArgs e)
		{
			if (Frame.CanGoBack)
			{
				Frame.GoBack();
			}
		}
	}
}
