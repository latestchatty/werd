using Autofac;
using Common;
using Werd.Common;
using System;
using Windows.UI.Xaml.Navigation;

namespace Werd.Views
{
	public partial class ShackWebView
	{
		public override string ViewTitle => "Search";

		public override event EventHandler<LinkClickedEventArgs> LinkClicked = delegate { }; //Unused
		public override event EventHandler<ShellMessageEventArgs> ShellMessage = delegate { }; //Unused

		private IContainer _container;
		private Uri _baseUri;
		private AuthenticationManager _authManager;

		public ShackWebView()
		{
			InitializeComponent();
		}

		protected override async void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			var param = e.Parameter as Tuple<IContainer, Uri>;
			_container = param.Item1;
			_baseUri = param.Item2;
			_authManager = _container.Resolve<AuthenticationManager>();
			if (e.NavigationMode == NavigationMode.New)
			{
				await this.web.NavigateWithShackLogin(_baseUri, _authManager);
			}
		}

		private async void WebView_NavigationStarting(Windows.UI.Xaml.Controls.WebView sender, Windows.UI.Xaml.Controls.WebViewNavigationStartingEventArgs args)
		{
			await AppGlobal.DebugLog.AddMessage($"Navigating to {args.Uri.ToString()}");
			var postId = AppLaunchHelper.GetShackPostId(args.Uri);
			if (postId != null)
			{
				Frame.Navigate(typeof(SingleThreadView), new Tuple<IContainer, int, int>(_container, postId.Value, postId.Value));
				args.Cancel = true;
			}
		}

		private void BackClicked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			if (this.web.CanGoBack)
			{
				this.web.GoBack();
			}
		}

		private async void HomeClicked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			await this.web.NavigateWithShackLogin(_baseUri, _authManager);
		}

		private async void web_NavigationCompleted(Windows.UI.Xaml.Controls.WebView sender, Windows.UI.Xaml.Controls.WebViewNavigationCompletedEventArgs args)
		{
			var ret =
			await this.web.InvokeScriptAsync("eval", new[]
			{
				@"(function()
                {
                    function updateHrefs() {
                        var hyperlinks = document.getElementsByClassName('permalink');
                        for(var i = 0; i < hyperlinks.length; i++)
                        {
                            hyperlinks[i].setAttribute('target', '_self');
                        }
                    }

                    var target = document.getElementById('page');
                    if(target !== undefined) {
                        const observer = new MutationObserver(updateHrefs);
                        observer.observe(target, { childList: true, subtree: true });
                    }   
                })()"
			});
		}
	}
}
