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
		private string _viewTitle = "Web";
		public override string ViewTitle
		{
			get => _viewTitle;
			set => SetProperty(ref _viewTitle, value);
		}

		public override event EventHandler<LinkClickedEventArgs> LinkClicked;
		public override event EventHandler<ShellMessageEventArgs> ShellMessage = delegate { }; //Unused

		private IContainer _container;
		private WebView _webView;

		private Uri _baseUri;
		private Uri BaseUri
		{
			get => _baseUri;
			set => SetProperty(ref _baseUri, value);
		}
		private AuthenticationManager _authManager;

		public ShackWebView()
		{
			InitializeComponent();
		}

		protected override async void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			_webView = new WebView(WebViewExecutionMode.SeparateThread);
			_webView.NavigationStarting += WebView_NavigationStarting;
			_webView.NavigationCompleted += WebView_NavigationCompleted;
			webHolder.Children.Add(_webView);
			var param = e.Parameter as NavigationArgs.WebViewNavigationArgs;
			_container = param.Container;
			BaseUri = param.NavigationUrl;
			_authManager = _container.Resolve<AuthenticationManager>();
			if (e.NavigationMode == NavigationMode.New)
			{
				if (_baseUri != null)
				{
					await _webView.NavigateWithShackLogin(_baseUri, _authManager).ConfigureAwait(true);
				}
				else
				{
					_webView.NavigateToString(param.NavigationString);
				}
			}
			else
			{
				// Navigate back one in the stack since we would have navigated to an empty string when leaving last time.
				if (_webView.CanGoBack) _webView.GoBack();
			}
			this.Bindings.Update();
		}

		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			base.OnNavigatingFrom(e);

			//Navigate to an empty string to stop any A/V, and free up resources.
			_webView.NavigateToString(""); 
		}

		private async void WebView_NavigationStarting(WebView wv, WebViewNavigationStartingEventArgs args)
		{
			await DebugLog.AddMessage($"Navigating to {args.Uri}").ConfigureAwait(true);
			if (args.Uri is null) return;
			SetViewTitle(wv.DocumentTitle);
			var postId = AppLaunchHelper.GetShackPostId(args.Uri);
			if (postId != null)
			{
				LinkClicked?.Invoke(this, new LinkClickedEventArgs(new Uri($"https://shacknews.com/chatty?id={postId.Value}")));
				args.Cancel = true;
			}
		}

		private void BackClicked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			if (this._webView.CanGoBack)
			{
				this._webView.GoBack();
			}
		}

		private async void HomeClicked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			await _webView.NavigateWithShackLogin(BaseUri, _authManager).ConfigureAwait(false);
		}

		private async void WebView_NavigationCompleted(WebView wv, WebViewNavigationCompletedEventArgs args)
		{
			if (args.Uri != null)
			{
				SetViewTitle(wv.DocumentTitle);
				if (args.Uri.Host.Contains("shacknews.com", StringComparison.Ordinal))
				{
					var ret =
					await this._webView.InvokeScriptAsync("eval", new[]
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

		public void CloseWebView()
		{
			_webView.NavigateToString("");
			webHolder.Children.Clear();
		}

		private void SetViewTitle(string title)
		{
			if(string.IsNullOrWhiteSpace(title))
			{
				ViewTitle = "Web";
			}
			else
			{
				ViewTitle = title;
			}
		}
	}
}
