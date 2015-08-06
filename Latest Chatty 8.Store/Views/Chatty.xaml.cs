using Autofac;
using Latest_Chatty_8.Common;
using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Settings;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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

		private LatestChattySettings npcSettings = null;
		private LatestChattySettings Settings
		{
			get { return this.npcSettings; }
            set { this.SetProperty(ref this.npcSettings, value); }
		}

		private CommentThread npcSelectedThread = null;
		public CommentThread SelectedThread
		{
			get { return this.npcSelectedThread; }
			set
			{
				if (this.SetProperty(ref this.npcSelectedThread, value))
				{
					//var t = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
					//{
					//	if (value?.Comments?.Count() > 0) this.commentList.SelectedIndex = 0;
					//});
				}
			}
		}

		private bool npcShowSearch = false;
		private bool ShowSearch
		{
			get { return this.npcShowSearch; }
			set { this.SetProperty(ref this.npcShowSearch, value); }
		}


		public Chatty()
		{
			this.InitializeComponent();
		}


		#region Thread View
		private int currentItemWidth;
		public Comment SelectedComment { get; private set; }
		private WebView currentWebView;
		//public AppBar AppBarToShow { get { return this.commentList.AppBarToShow; } set { this.commentList.AppBarToShow = value; } }

		private ChattyManager chattyManager;
		public ChattyManager ChattyManager
		{
			get { return this.chattyManager; }
			set { this.SetProperty(ref this.chattyManager, value); }
		}
		private ThreadMarkManager markManager;
		private AuthenticationManager authManager;
		private Controls.PostContol currentReplyControl;

		private string imageUrlForContextMenu;


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
					await this.chattyManager.MarkCommentRead(this.SelectedThread, this.SelectedComment);
					var container = lv.ContainerFromItem(selectedItem);
					if (container == null) return; //Bail because the visual tree isn't created yet...
					var containerGrid = container.FindControlsNamed<Grid>("container").FirstOrDefault();

					this.currentItemWidth = (int)containerGrid.ActualWidth;// (int)(containerGrid.ActualWidth * ResolutionScaleConverter.ScaleFactor);

					//HACK - This seems to work fine in desktops without doing any math.  Phone doesn't seem to work right and requires taking scaling into account - even then it doesn't seem to work exactly right.
					//this.currentItemWidth = (int)(containerGrid.ActualWidth * Windows.Graphics.Display.DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel);
					System.Diagnostics.Debug.WriteLine("Scale is {0}", Windows.Graphics.Display.DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel);
					System.Diagnostics.Debug.WriteLine("Width of web view container is {0}", this.currentItemWidth);
					var webView = container.FindControlsNamed<WebView>("bodyWebView").FirstOrDefault() as WebView;
					this.UpdateVisibility(container, false);
					UnbindEventHandlers();

					this.currentReplyControl = container.FindControlsNamed<Latest_Chatty_8.Controls.PostContol>("replyArea").FirstOrDefault();

					if (webView != null)
					{
						this.currentWebView = webView;
						webView.ScriptNotify += ScriptNotify;
						webView.NavigationCompleted += NavigationCompleted;
						webView.NavigationStarting += NavigatingWebView;
						webView.NavigateToString(WebBrowserHelper.GetPostHtml(this.SelectedComment.Body));
					}
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

		private IEnumerable<FrameworkElement> PostControl(string arg)
		{
			throw new NotImplementedException();
		}

		private void UnbindEventHandlers()
		{
			if (this.currentWebView != null)
			{
				this.currentWebView.ScriptNotify -= ScriptNotify;
				this.currentWebView.NavigationStarting -= NavigatingWebView;
			}
		}

		async private void ScriptNotify(object s, NotifyEventArgs e)
		{
			var sender = s as WebView;
			var jsonEventData = JToken.Parse(e.Value);

			if (jsonEventData["eventName"].ToString().Equals("imageloaded"))
			{
				await ResizeWebView(sender);
			}
			else if (jsonEventData["eventName"].ToString().Equals("rightClickedImage"))
			{
				this.imageUrlForContextMenu = jsonEventData["eventData"]["url"].ToString();
				Windows.UI.Xaml.Controls.Primitives.FlyoutBase.ShowAttachedFlyout(sender);
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

		async private Task ResizeWebView(WebView wv)
		{
			//For some reason the WebView control *sometimes* has a width of NaN, or something small.
			//So we need to set it to what it's going to end up being in order for the text to render correctly.
			await wv.InvokeScriptAsync("eval", new string[] { string.Format("SetViewSize({0});", this.currentItemWidth) });
			var result = await wv.InvokeScriptAsync("eval", new string[] { "GetViewSize();" });
			int viewHeight;
			if (int.TryParse(result, out viewHeight))
			{
				//viewHeight = (int)(viewHeight / Windows.Graphics.Display.DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel);
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

		async private void lolPostClicked(object sender, RoutedEventArgs e)
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


		async private void ChattyListSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.RemovedItems.Count > 0)
			{
				var ct = e.RemovedItems[0] as CommentThread;
				await this.chattyManager.MarkCommentThreadRead(ct);
			}

			if (e.AddedItems.Count > 0)
			{
				var ct = e.AddedItems[0] as CommentThread;
				this.commentList.ItemsSource = ct.Comments;
				this.commentList.UpdateLayout();
				this.commentList.SelectedIndex = 0;
			}
		}

		#region Events

		async private void MarkAllRead(object sender, RoutedEventArgs e)
		{
			await this.chattyManager.MarkAllVisibleCommentsRead();
		}

		async private void PinClicked(object sender, RoutedEventArgs e)
		{
			var flyout = sender as MenuFlyoutItem;
			if (flyout == null) return;
			var commentThread = flyout.DataContext as CommentThread;
			if (commentThread == null) return;
			if (this.markManager.GetMarkType(commentThread.Id) == MarkType.Pinned)
			{
				await this.markManager.MarkThread(commentThread.Id, MarkType.Unmarked);
			}
			else
			{
				await this.markManager.MarkThread(commentThread.Id, MarkType.Pinned);
			}
		}

		async private void CollapseClicked(object sender, RoutedEventArgs e)
		{
			var flyout = sender as MenuFlyoutItem;
			if (flyout == null) return;
			var commentThread = flyout.DataContext as CommentThread;
			if (commentThread == null) return;
			if (this.markManager.GetMarkType(commentThread.Id) == MarkType.Collapsed)
			{
				await this.markManager.MarkThread(commentThread.Id, MarkType.Unmarked);
			}
			else
			{
				await this.markManager.MarkThread(commentThread.Id, MarkType.Collapsed);
			}
		}

		async private void OpenImageInBrowserClicked(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(this.imageUrlForContextMenu)) return;
			await Launcher.LaunchUriAsync(new Uri(this.imageUrlForContextMenu));
		}

		private void CopyImageLinkClicked(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(this.imageUrlForContextMenu)) return;
			var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
			dataPackage.SetText(this.imageUrlForContextMenu);
			Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
		}
		#endregion


		async private void ReSortClicked(object sender, RoutedEventArgs e)
		{
			if (this.Settings.MarkReadOnSort)
			{
				await this.chattyManager.MarkAllVisibleCommentsRead();
			}
			await this.chattyManager.CleanupChattyList();
			if (this.chattyCommentList.Items.Count > 0)
			{
				this.chattyCommentList.ScrollIntoView(this.chattyCommentList.Items[0]);
			}
		}

		async private void SearchTextChanged(object sender, TextChangedEventArgs e)
		{
			var searchTextBox = sender as TextBox;
			await this.ChattyManager.SearchChatty(searchTextBox.Text);
		}

		private void ShowReplyClicked(object sender, RoutedEventArgs e)
		{
			var button = sender as Windows.UI.Xaml.Controls.Primitives.ToggleButton;
			if (button == null) return;
			if (button.IsChecked.HasValue && button.IsChecked.Value)
			{
				this.currentReplyControl.SetAuthenticationManager(this.authManager);
				this.currentReplyControl.SetFocus();
			}
		}

		private void NewRootPostButtonClicked(object sender, RoutedEventArgs e)
		{
			this.newRootPostControl.SetAuthenticationManager(this.authManager);
			this.newRootPostControl.SetFocus();
		}

		private void ThreadListRightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
		{
			Windows.UI.Xaml.Controls.Primitives.FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
		}

		private void CopyPostLinkClicked(object sender, RoutedEventArgs e)
		{
			var button = sender as Button;
			if (button == null) return;
			var comment = button.DataContext as Comment;
			if (comment == null) return;
			var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
			dataPackage.SetText(string.Format("http://www.shacknews.com/chatty?id={0}#item_{0}", comment.Id));
			Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
		}

		async private void FilterChanged(object sender, SelectionChangedEventArgs e)
		{
			if (this.ChattyManager == null) return;
			if (e.AddedItems.Count != 1) return;
			var item = e.AddedItems[0] as ComboBoxItem;
			if (item == null) return;
			ChattyFilterType filter;
			switch (item.Tag.ToString())
			{
				case "new":
					filter = ChattyFilterType.New;
					break;
				case "has replies":
					filter = ChattyFilterType.HasReplies;
					break;
				case "participated":
					filter = ChattyFilterType.Participated;
					break;
				case "search":
					this.ShowSearch = true;
					await this.ChattyManager.SearchChatty(this.searchTextBox.Text);
					this.searchTextBox.Focus(FocusState.Programmatic);
					return;
				case "collapsed":
					filter = ChattyFilterType.Collapsed;
					break;
				case "pinned":
					filter = ChattyFilterType.Pinned;
					break;
				default:
					filter = ChattyFilterType.All;
					break;
			}
			this.ShowSearch = false;
			await this.ChattyManager.FilterChatty(filter);
		}

		async private void SortChanged(object sender, SelectionChangedEventArgs e)
		{
			if (this.ChattyManager == null) return;
			if (e.AddedItems.Count != 1) return;
			var item = e.AddedItems[0] as ComboBoxItem;
			if (item == null) return;
			ChattySortType sort;
			switch (item.Tag.ToString())
			{
				case "inf":
					sort = ChattySortType.Inf;
					break;
				case "lol":
					sort = ChattySortType.Lol;
					break;
				case "mostreplies":
					sort = ChattySortType.ReplyCount;
					break;
				case "hasnewreplies":
					sort = ChattySortType.HasNewReplies;
					break;
				case "participated":
					sort = ChattySortType.Participated;
					break;
				default:
					sort = ChattySortType.Default;
					break;
			}
			await this.ChattyManager.SortChatty(sort);
		}

		#region Load and Save State
		protected override void OnNavigatedTo(Windows.UI.Xaml.Navigation.NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			var container = e.Parameter as Autofac.IContainer;
			this.authManager = container.Resolve<AuthenticationManager>();
			this.ChattyManager = container.Resolve<ChattyManager>();
			this.markManager = container.Resolve<ThreadMarkManager>();
			this.Settings = container.Resolve<LatestChattySettings>();
		}

		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			base.OnNavigatingFrom(e);
			if (this.currentWebView != null)
			{
				this.UnbindEventHandlers();
			}
		}




		#endregion
	}
}
