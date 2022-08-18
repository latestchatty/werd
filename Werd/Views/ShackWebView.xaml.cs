using Autofac;
using Common;
using Microsoft.Web.WebView2.Core;
using System;
using Werd.Common;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;

namespace Werd.Views
{
	public partial class ShackWebView
	{
		public override string ViewIcons { get => "\uEB41"; set { return; } }
		private string _viewTitle = "Web";
		public override string ViewTitle
		{
			get => _viewTitle;
			set => SetProperty(ref _viewTitle, value);
		}

		private bool _isLoading;
		private bool IsLoading
		{
			get => _isLoading;
			set => SetProperty(ref _isLoading, value);
		}

		public override event EventHandler<LinkClickedEventArgs> LinkClicked;
		public override event EventHandler<ShellMessageEventArgs> ShellMessage = delegate { }; //Unused

		private IContainer _container;
		//private WebView2 _webView;

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
			await _webView.EnsureCoreWebView2Async(); 
			_webView.CoreWebView2.NavigationStarting += WebView_NavigationStarting;
			_webView.CoreWebView2.NavigationCompleted += WebView_NavigationCompleted;
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
			CloseWebView();			
		}

		private void BackClicked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			if (_webView.CanGoBack)
			{
				_webView.GoBack();
			}
		}

		private void ForwardClicked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			if (_webView.CanGoForward) { _webView.GoForward(); }
		}

		private async void OpenInNewWindowClicked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			await Windows.System.Launcher.LaunchUriAsync(_webView.Source);
		}

		private async void UrlKeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
		{
			if (e.Key == Windows.System.VirtualKey.Enter)
			{
				try
				{
					IsLoading = true;
					_webView.CoreWebView2.Navigate((await UriHelper.MakeWebViewSafeUriOrSearch(urlText.Text).ConfigureAwait(true)).ToString());
				}
				catch
				{
					IsLoading = false;
					throw;
				}
			}
		}
		private async void HomeClicked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			await _webView.NavigateWithShackLogin(BaseUri, _authManager).ConfigureAwait(false);
		}
		private void FocusAddressBarAccelerator(Windows.UI.Xaml.Input.KeyboardAccelerator sender, Windows.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
		{
			urlText.SelectAll();
			urlText.Focus(Windows.UI.Xaml.FocusState.Programmatic);
		}

		private void StopClicked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			_webView.CoreWebView2.Stop();
		}

		private void RefreshClicked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			_webView.CoreWebView2.Reload();
		}

		private async void WebView_NavigationStarting(CoreWebView2 wv, CoreWebView2NavigationStartingEventArgs args)
		{
			await DebugLog.AddMessage($"Navigating to {args.Uri}").ConfigureAwait(true);
			IsLoading = true;
			if (args.Uri is null) return;
			var postId = AppLaunchHelper.GetShackPostId(new Uri(args.Uri));
			if (postId != null)
			{
				LinkClicked?.Invoke(this, new LinkClickedEventArgs(new Uri($"https://shacknews.com/chatty?id={postId.Value}")));
				args.Cancel = true;
				IsLoading = false;
				return;
			}
			SetViewTitle(wv.DocumentTitle);
			urlText.Text = args.Uri.ToString();
		}

		private async void WebView_NavigationCompleted(CoreWebView2 wv, CoreWebView2NavigationCompletedEventArgs args)
		{
			IsLoading = false;
			if (wv.Source != null)
			{
				SetViewTitle(wv.DocumentTitle);
				urlText.Text = wv.Source.ToString();
				if (new Uri(wv.Source).Host.Contains("shacknews.com", StringComparison.Ordinal))
				{
					var ret =
					await this._webView.CoreWebView2.ExecuteScriptAsync(
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
                })()");
				}
			}
		}

		public void CloseWebView()
		{
			_webView.NavigateToString("");
			// For some reason the cursor can get stuck when closing a webview without resetting it.
			Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 0);
			webHolder.Children.Clear();
			_webView.Close();
		}

		private void SetViewTitle(string title)
		{
			if (string.IsNullOrWhiteSpace(title))
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
