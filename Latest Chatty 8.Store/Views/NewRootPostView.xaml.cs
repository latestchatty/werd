using Autofac;
using Latest_Chatty_8.Common;
using System;
using Windows.UI.Xaml.Navigation;

namespace Latest_Chatty_8.Views
{
	public sealed partial class NewRootPostView : ShellView
	{
		public NewRootPostView()
		{
			this.InitializeComponent();
		}

		public override string ViewTitle
		{
			get
			{
				return "New root post";
			}
		}

		public override event EventHandler<LinkClickedEventArgs> LinkClicked = delegate { };
		public override event EventHandler<ShellMessageEventArgs> ShellMessage = delegate { };

		protected async override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			var container = e.Parameter as IContainer;
			this.postControl.SetAuthenticationManager(container.Resolve<AuthenticationManager>());
			this.postControl.Closed += PostControl_Closed;
		}

		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			base.OnNavigatingFrom(e);
			this.postControl.Closed -= PostControl_Closed;
		}

		private void PostControl_Closed(object sender, EventArgs e)
		{
			if(this.Frame.CanGoBack)
			{
				this.Frame.GoBack();
			}
		}
	}
}
