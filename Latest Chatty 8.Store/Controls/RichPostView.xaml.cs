using Latest_Chatty_8.Settings;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace Latest_Chatty_8.Controls
{
	public sealed partial class RichPostView : UserControl
	{
		private string imageUrlForContextMenu;
		private LatestChattySettings Settings;

		public event EventHandler Resized;

		public RichPostView()
		{
			this.InitializeComponent();
		}

		#region Public Methods
		public void Close()
		{
			this.SizeChanged -= RichPostView_SizeChanged;
			this.bodyWebView.ScriptNotify -= ScriptNotify;
			this.bodyWebView.NavigationStarting -= NavigatingWebView;
			this.bodyWebView.NavigateToString("<html></html>"); //This will force any embedded videos to be stopped.
		}

		public void LoadPost(string v, LatestChattySettings settings)
		{
			this.Settings = settings;
			this.bodyWebView.ScriptNotify += ScriptNotify;
			this.bodyWebView.NavigationCompleted += NavigationCompleted;
			this.bodyWebView.NavigationStarting += NavigatingWebView;
			this.SizeChanged += RichPostView_SizeChanged;
			this.bodyWebView.NavigateToString(v);
		}
		#endregion

		#region Web View Events
		async private void ScriptNotify(object s, NotifyEventArgs e)
		{
			var jsonEventData = JToken.Parse(e.Value);

			var eventName = jsonEventData["eventName"].ToString();

			System.Diagnostics.Debug.WriteLine(string.Format("JavaScript event from WebView: {0}", eventName));

			if (eventName.Equals("resizeRequired"))
			{
				await ResizeWebView();
			}
			else if (eventName.Equals("rightClickedImage"))
			{
				this.imageUrlForContextMenu = jsonEventData["eventData"]["url"].ToString();
				FlyoutBase.ShowAttachedFlyout(s as WebView);
			}
#if DEBUG
			else if (eventName.Equals("debug"))
			{
				System.Diagnostics.Debug.WriteLine("=======Begin JS Debug Event======={0}{1}{0}=======End JS Debug Event=======", Environment.NewLine, jsonEventData["eventData"].ToString());
			}
#endif
			else if (eventName.Equals("error"))
			{
				System.Diagnostics.Debug.WriteLine("!!!!!!!Begin JS Error!!!!!!!{0}{1}{0}!!!!!!!End JS Error!!!!!!!", Environment.NewLine, jsonEventData["eventData"].ToString());
				(new Microsoft.ApplicationInsights.TelemetryClient()).TrackEvent("Chatty-JSError", new Dictionary<string, string> { { "eventData", jsonEventData["eventData"].ToString() } });
			}
			else if (eventName.Equals("externalYoutube"))
			{
				var videoId = jsonEventData["eventData"]["ytId"].ToString();
				await Launcher.LaunchUriAsync(new Uri(string.Format(this.Settings.ExternalYoutubeApp.UriFormat, videoId)));
			}
		}

		async private void NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
		{
			sender.NavigationCompleted -= NavigationCompleted;
			await ResizeWebView();
		}

		async private void NavigatingWebView(WebView sender, WebViewNavigationStartingEventArgs args)
		{
			//NavigateToString will not have a uri, so if a WebView is trying to navigate somewhere with a URI, we want to run it in a new browser window.
			//We have to handle navigation like this because if a link has target="_blank" in it, the application will crash entirely when clicking on that link.
			//Maybe this will be fixed in an updated SDK, but for now it is what it is.
			if (args.Uri != null)
			{
				args.Cancel = true;
				await Launcher.LaunchUriAsync(args.Uri);
			}
		}
		#endregion

		#region Flyout Events
		async private void OpenImageInBrowserClicked(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(this.imageUrlForContextMenu)) return;
			await Launcher.LaunchUriAsync(new Uri(this.imageUrlForContextMenu));
			(new Microsoft.ApplicationInsights.TelemetryClient()).TrackEvent("Chatty-OpenImageInBrowserClicked");
		}

		private void CopyImageLinkClicked(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(this.imageUrlForContextMenu)) return;
			var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
			dataPackage.SetText(this.imageUrlForContextMenu);
			Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
			(new Microsoft.ApplicationInsights.TelemetryClient()).TrackEvent("Chatty-CopyImageLinkClicked");
		}
		#endregion

		#region Private Helpers
		async private Task ResizeWebView()
		{
			await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
			{
				try
				{
					await this.bodyWebView.InvokeScriptAsync("eval", new string[] { $"SetViewSize({this.container.ActualWidth});" });
					var result = await this.bodyWebView.InvokeScriptAsync("eval", new string[] { "GetViewSize();" });
					int viewHeight;
					if (int.TryParse(result, out viewHeight))
					{
						System.Diagnostics.Debug.WriteLine("WebView Height is {0}", viewHeight);
						this.bodyWebView.MinHeight = this.bodyWebView.Height = viewHeight;
						this.bodyWebView.UpdateLayout();
						if (this.Resized != null)
						{
							this.Resized(this, EventArgs.Empty);
						}
					}
				}
				catch (Exception ex)
				{
					//If we happen to call Close while this thread is waiting for the UI to become available, the InvokeScript calls are going to fail.
					//This could be safeguarded with locks, but I think the easier way to handle it is to just swallow the exception that gets thrown.
					//Rather than block up the UI thread with all that synchronizing.
					//This is bad, mmkay?  Mmmkay.
					System.Diagnostics.Debug.WriteLine("Exception occurred while resizing {0}", ex);
				}
			});
		}

		async private void RichPostView_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			await this.ResizeWebView();
		}
		#endregion
	}
}
