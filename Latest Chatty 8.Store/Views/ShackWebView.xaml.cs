using Autofac;
using Common;
using System;
using Werd.Common;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Werd.Views
{
	public partial class ShackWebView
	{
		public override string ViewTitle => "Search";

		public override event EventHandler<LinkClickedEventArgs> LinkClicked;
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
				await web.NavigateWithShackLogin(_baseUri, _authManager).ConfigureAwait(false);
			}
			else
			{
				// Navigate back one in the stack since we would have navigated to an empty string when leaving last time.
				if (web.CanGoBack) web.GoBack();
			}
		}

		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			base.OnNavigatingFrom(e);

			//Navigate to an empty string to stop any A/V, and free up resources.
			web.NavigateToString(""); 
		}

		private async void WebView_NavigationStarting(Windows.UI.Xaml.Controls.WebView _, Windows.UI.Xaml.Controls.WebViewNavigationStartingEventArgs args)
		{
			await DebugLog.AddMessage($"Navigating to {args.Uri}").ConfigureAwait(true);
			if (args.Uri is null) return;
			var postId = AppLaunchHelper.GetShackPostId(args.Uri);
			if (postId != null)
			{
				LinkClicked?.Invoke(this, new LinkClickedEventArgs(new Uri($"https://shacknews.com/chatty?id={postId.Value}")));
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
			await web.NavigateWithShackLogin(_baseUri, _authManager).ConfigureAwait(false);
		}

		private async void web_NavigationCompleted(Windows.UI.Xaml.Controls.WebView _, Windows.UI.Xaml.Controls.WebViewNavigationCompletedEventArgs args)
		{
			if (args.Uri is null) return;

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
