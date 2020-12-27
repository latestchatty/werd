using Autofac;
using System;
using Werd.Common;
using Windows.UI.Xaml.Navigation;

namespace Werd.Views
{
	public sealed partial class NewRootPostView
	{
		public NewRootPostView()
		{
			InitializeComponent();
		}

		public override string ViewIcons { get => "\uE90A"; set { return; } }
		public override string ViewTitle { get => "New root post"; set { return; } }

		public override event EventHandler<LinkClickedEventArgs> LinkClicked = delegate { };
		public override event EventHandler<ShellMessageEventArgs> ShellMessage = delegate { };

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			var container = e.Parameter as IContainer;
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
