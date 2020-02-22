using System;
using Windows.UI.Xaml.Navigation;
using Autofac;
using Latest_Chatty_8.Common;

namespace Latest_Chatty_8.Views
{
	public sealed partial class SearchView
	{
		public override string ViewTitle => "Search";

		public override event EventHandler<LinkClickedEventArgs> LinkClicked = delegate { }; //Unused
		public override event EventHandler<ShellMessageEventArgs> ShellMessage = delegate { }; //Unused

		private IContainer _container;
	
		public SearchView()
		{
			InitializeComponent();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			_container = e.Parameter as IContainer;
		}

		private void WebView_NavigationStarting(Windows.UI.Xaml.Controls.WebView sender, Windows.UI.Xaml.Controls.WebViewNavigationStartingEventArgs args)
		{
			System.Diagnostics.Debug.WriteLine($"Navigating to {args.Uri.ToString()}");
			var postId = AppLaunchHelper.GetShackPostId(args.Uri);
			if (postId != null)
			{
				Frame.Navigate(typeof(SingleThreadView), new Tuple<IContainer, int, int>(_container, postId.Value, postId.Value));
				args.Cancel = true;
			}
		}

		private void BackClicked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			if(this.web.CanGoBack)
			{
				this.web.GoBack();
			}
		}

		private void HomeClicked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			this.web.Navigate(new Uri("https://shacknews.com/search?q=&type=4"));
		}
	}
}
