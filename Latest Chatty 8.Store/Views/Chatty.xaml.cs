using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Shared.Settings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Latest_Chatty_8.Common;
using Latest_Chatty_8.Shared;
using Windows.System;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237
namespace Latest_Chatty_8.Views
{
	/// <summary>
	/// A basic page that provides characteristics common to most applications.
	/// </summary>
	public sealed partial class Chatty : ShellView
	{
		public override string ViewTitle
		{
			get
			{
				return "Chatty";
			}
		}

		private CommentThread npcSelectedThread = null;
		public CommentThread SelectedThread
		{
			get { return this.npcSelectedThread; }
			set
			{
				if (this.SetProperty(ref this.npcSelectedThread, value))
				{
					var t = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
					{
						if (value?.Comments?.Count() > 0) this.commentList.SelectedIndex = 0;
					});
				}
			}
		}

		private IEnumerable<CommentThread> npcCommentThreads;
		public IEnumerable<CommentThread> CommentThreads
		{
			get { return this.npcCommentThreads; }
			set
			{
				this.SetProperty(ref this.npcCommentThreads, value);
			}
		}


		public Chatty()
		{
			this.InitializeComponent();
			this.CoreServices = CoreServices.Instance;
		}


		#region Thread View
		private int currentItemWidth;
        public Comment SelectedComment { get; private set; }
        private WebView currentWebView;
        //public AppBar AppBarToShow { get { return this.commentList.AppBarToShow; } set { this.commentList.AppBarToShow = value; } }

        private IEnumerable<Comment> currentComments;
        public IEnumerable<Comment> Comments
        {
            get { return this.currentComments; }
            set { this.SetProperty(ref this.currentComments, value); }
        }

		public CoreServices CoreServices { get; private set; }
		
        async private void SelectedItemChanged(object sender, SelectionChangedEventArgs e)
        {
			try
			{
				var lv = sender as ListView;
				if (lv == null) return; //This would be bad.
				this.SelectedComment = null;
				//this.SetFontSize();

				foreach (var notSelected in e.RemovedItems)
				{
					var unselectedComment = notSelected as Comment;
					if (unselectedComment == null) continue;
					var unselectedContainer = lv.ContainerFromItem(unselectedComment);
					if (unselectedContainer == null) continue;
					this.UpdateVisibility(unselectedContainer, true);
				}

				foreach (var added in e.AddedItems)
				{
					var selectedItem = added as Comment;
					if (selectedItem == null) return; //Bail, we don't know what to
					this.SelectedComment = selectedItem;
					CoreServices.Instance.ChattyManager.MarkCommentRead(this.SelectedThread, this.SelectedComment);
					var container = lv.ContainerFromItem(selectedItem);
					if (container == null) return; //Bail because the visual tree isn't created yet...
					var containerGrid = container.FindControlsNamed<Grid>("container").FirstOrDefault();

					this.currentItemWidth = (int)containerGrid.ActualWidth;// (int)(containerGrid.ActualWidth * ResolutionScaleConverter.ScaleFactor);

					System.Diagnostics.Debug.WriteLine("Width of web view container is {0}", this.currentItemWidth);
					var webView = container.FindControlsNamed<WebView>("bodyWebView").FirstOrDefault() as WebView;
					this.UpdateVisibility(container, false);
					UnbindEventHandlers();

					if (webView != null)
					{
						this.currentWebView = webView;
						webView.ScriptNotify += ScriptNotify;
						webView.NavigationCompleted += NavigationCompleted;
						webView.NavigationStarting += NavigatingWebView;
						webView.NavigateToString(
						@"<html xmlns='http://www.w3.org/1999/xhtml'>
						<head>
							<meta name='viewport' content='user-scalable=no'/>
							<style type='text/css'>" + WebBrowserHelper.CSS + @"</style>
							<script type='text/javascript'>
								function SetFontSize(size)
								{
									var html = document.getElementById('commentBody');
									html.style.fontSize=size+'pt';
								}
								function SetViewSize(size)
								{
									var html = document.getElementById('commentBody');
									html.style.width=size+'px';
								}
								function GetViewSize() {
									var html = document.getElementById('commentBody');
									var height = Math.max( html.clientHeight, html.scrollHeight, html.offsetHeight );
									return height.toString();
								}
								function loadImage(e, url) {
									 var img = new Image();
									 img.onload= function () {
										  e.onload=function() { window.external.notify('imageloaded'); };
										  e.src = img.src;
                                e.onclick=function(i) { var originalClassName = e.className; if(e.className == 'fullsize') { e.className = 'embedded'; } else { e.className = 'fullsize'; } window.external.notify('imageloaded|' + i.className + '|' + originalClassName); return false;};
									 };
									 img.src = url;
								}
							</script>
						</head>
						<body>
							<div id='commentBody' class='body'>" + selectedItem.Body.Replace("target=\"_blank\"", "") + @"</div>
						</body>
					</html>");
					}
					return;
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine("Exception in SelectedItemChanged {0}", ex);
				var msg = new MessageDialog(string.Format("Exception in SelectedItemChanged {0}", ex));
				await msg.ShowAsync();
				System.Diagnostics.Debugger.Break();
			}
        }

		private void UnbindEventHandlers()
		{
			if (this.currentWebView != null)
			{
				this.currentWebView.ScriptNotify -= ScriptNotify;
				this.currentWebView.NavigationStarting -= NavigatingWebView;
			}
		}

		private async void ScriptNotify(object s, NotifyEventArgs e)
        {
            var sender = s as WebView;

            if (e.Value.Contains("imageloaded"))
            {
                await ResizeWebView(sender);
            }
        }

        async private void NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            await ResizeWebView(sender);
            sender.NavigationCompleted -= NavigationCompleted;
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

		private async Task ResizeWebView(WebView wv)
        {
            //For some reason the WebView control *sometimes* has a width of NaN, or something small.
            //So we need to set it to what it's going to end up being in order for the text to render correctly.
            await wv.InvokeScriptAsync("eval", new string[] { string.Format("SetViewSize({0});", this.currentItemWidth) });
            var result = await wv.InvokeScriptAsync("eval", new string[] { "GetViewSize();" });
            int viewHeight;
            if (int.TryParse(result, out viewHeight))
            {
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(new CoreDispatcherPriority(), () =>
                {
                    wv.MinHeight = wv.Height = viewHeight;
                    //Scroll into view has to happen after height is set, set low dispatcher priority.
                    var t = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                    {
                        this.commentList.ScrollIntoView(this.commentList.SelectedItem);
                        wv.Focus(Windows.UI.Xaml.FocusState.Programmatic);
                    }
                    );
                }
                );
            }
        }

        public void UpdateVisibility(DependencyObject container, bool previewMode)
        {
            var children = container.AllChildren<Grid>();
            var previewGrid = children.FirstOrDefault(c => c.Name == "preview");
            if (previewGrid != null)
            {
                previewGrid.Visibility = previewMode ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
            }
            var fullView = children.FirstOrDefault(c => c.Name == "commentSection");
            if (fullView != null)
            {
                fullView.Visibility = previewMode ? Windows.UI.Xaml.Visibility.Collapsed : Windows.UI.Xaml.Visibility.Visible;
            }
        }

        private async void lolPostClicked(object sender, RoutedEventArgs e)
        {
            if (this.SelectedComment == null) return;
            var controlContainer = this.commentList.ContainerFromItem(this.SelectedComment);
            if (controlContainer != null)
            {
                var tagButton = controlContainer.FindControlsNamed<Button>("tagButton").FirstOrDefault();
                if (tagButton == null) return;

                tagButton.IsEnabled = false;
                try
                {
                    var mi = sender as MenuFlyoutItem;
                    var tag = mi.Text;
                    await this.SelectedComment.LolTag(tag);
                }
                finally
                {
                    tagButton.IsEnabled = true;
                }
            }
        }
        #endregion


		private void ChattyListSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			try
			{
				if (e.RemovedItems.Count > 0)
				{
					var ct = e.RemovedItems[0] as CommentThread;
					CoreServices.Instance.ChattyManager.MarkCommentThreadRead(ct);
				}
			}
			catch
			{ }
			finally
			{
				//this.selectedThreadView.Visibility = vis;
			}
		}
		
		#region Events
		
		private void MarkAllRead(object sender, RoutedEventArgs e)
		{
			CoreServices.Instance.ChattyManager.MarkAllCommentsRead();
		}

		async private void PinClicked(object sender, RoutedEventArgs e)
		{
			if (this.SelectedThread != null)
			{
				await CoreServices.Instance.PinManager.PinThread(this.SelectedThread.Id);
				//await CoreServices.Instance.GetPinnedPosts();
			}
		}

		async private void UnPinClicked(object sender, RoutedEventArgs e)
		{
			if (this.SelectedThread != null)
			{
				await CoreServices.Instance.PinManager.UnPinThread(this.SelectedThread.Id);
				//await CoreServices.Instance.GetPinnedPosts();
			}
		}


		#endregion

		#region Private Helpers
	
		async private void FilterChatty()
		{
			var selectedItem = this.filterCombo.SelectedValue as ComboBoxItem;

			var filtername = selectedItem != null ? selectedItem.Content as string : "";
			//CoreServices.ChattyManager.FilterChatty();
		}

		#endregion

		private async void ReSortClicked(object sender, RoutedEventArgs e)
		{
			await CoreServices.Instance.ChattyManager.CleanupChattyList();
			this.FilterChatty();
			this.chattyCommentList.ScrollIntoView(CoreServices.Instance.ChattyManager.Chatty.First(), ScrollIntoViewAlignment.Leading);
		}

		private void SearchTextChanged(object sender, TextChangedEventArgs e)
		{
			//var searchTextBox = sender as TextBox;
			//if (!string.IsNullOrWhiteSpace(searchTextBox.Text))
			//{
			//    this.searchType.Visibility = Windows.UI.Xaml.Visibility.Visible;
			//}
			//else
			//{
			//    this.searchType.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
			//}
		}
		
		private void FilterChanged(object sender, SelectionChangedEventArgs e)
		{
			if (this.filterCombo != null)
			{
				this.FilterChatty();
			}
		}

		#region Load and Save State
		async protected override void OnNavigatedTo(Windows.UI.Xaml.Navigation.NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			//await LatestChattySettings.Instance.LoadLongRunningSettings();
			await CoreServices.Instance.Initialize();
			//:TODO: RE-enable pinned posts loading here.
			//await LatestChattySettings.Instance();

			await CoreServices.Instance.ClearTile(true);
			//this.CommentThreads = CoreServices.Instance.Chatty;
			//this.chattyControl.LoadChatty();
		}

		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			base.OnNavigatingFrom(e);
			if(this.currentWebView != null)
			{
				this.UnbindEventHandlers();
			}
		}


		#endregion

	}
}
