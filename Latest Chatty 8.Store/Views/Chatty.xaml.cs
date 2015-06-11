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

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237
namespace Latest_Chatty_8.Views
{
	/// <summary>
	/// A basic page that provides characteristics common to most applications.
	/// </summary>
	public sealed partial class Chatty : Page, INotifyPropertyChanged
	{
		#region NPC
		/// <summary>
		/// Multicast event for property change notifications.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Checks if a property already matches a desired value.  Sets the property and
		/// notifies listeners only when necessary.
		/// </summary>
		/// <typeparam name="T">Type of the property.</typeparam>
		/// <param name="storage">Reference to a property with both getter and setter.</param>
		/// <param name="value">Desired value for the property.</param>
		/// <param name="propertyName">Name of the property used to notify listeners.  This
		/// value is optional and can be provided automatically when invoked from compilers that
		/// support CallerMemberName.</param>
		/// <returns>True if the value was changed, false if the existing value matched the
		/// desired value.</returns>
		private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] String propertyName = null)
		{
			if (object.Equals(storage, value)) return false;

			storage = value;
			this.OnPropertyChanged(propertyName);
			return true;
		}

		/// <summary>
		/// Notifies listeners that a property value has changed.
		/// </summary>
		/// <param name="propertyName">Name of the property used to notify listeners.  This
		/// value is optional and can be provided automatically when invoked from compilers
		/// that support <see cref="CallerMemberNameAttribute"/>.</param>
		private void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			var eventHandler = this.PropertyChanged;
			if (eventHandler != null)
			{
				eventHandler(this, new PropertyChangedEventArgs(propertyName));
			}
		}
        #endregion

        #region Thread View
        private const int NormalWebFontSize = 14;
        private int currentItemWidth;
        public Comment SelectedComment { get; private set; }
        private WebView currentWebView;
        private int webFontSize = 14;
        //public AppBar AppBarToShow { get { return this.commentList.AppBarToShow; } set { this.commentList.AppBarToShow = value; } }

        private IEnumerable<Comment> currentComments;
        public IEnumerable<Comment> Comments
        {
            get { return this.currentComments; }
            set { this.SetProperty(ref this.currentComments, value); }
        }

        //private CommentThread thread;
        //public CommentThread Thread
        //{
        //    get { return this.thread; }
        //    set { this.SetProperty(ref this.thread, value); }
        //}
        
        private void WindowSizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            //HACK: This would be better to be based on the control size, not the window size.
            //CoreServices.Instance.ShowAuthor = e.Size.Width > 500;
            if (this.currentWebView != null)
            {
                //Selecting the comment again will cause the web view to redraw itself, making sure everything fits in the current display.
                //:HACK: This is kinda crappy.  I should probably handle this better.
                var selectedIndex = this.commentList.SelectedIndex;
                this.commentList.SelectedIndex = -1;
                this.commentList.SelectedIndex = selectedIndex;
            }
        }

        private void SetFontSize()
        {
            return;
            //Minimum size is 320px.
            //Size that we want max font is 600px.
            //Minimum font size is 9pt.  Max is 14pt.
            var fontScale = (600 - Window.Current.Bounds.Width) / 280;
            if (fontScale <= 1 && fontScale > 0)
            {
                this.webFontSize = (int)(NormalWebFontSize - (5 * fontScale));
            }
            else
            {
                this.webFontSize = NormalWebFontSize;
            }
        }

        //private void DataContextUpdated(FrameworkElement sender, DataContextChangedEventArgs args)
        //{
        //    var commentThread = args.NewValue as CommentThread;


        //    if (commentThread != null)
        //    {
        //        this.Thread = commentThread;
        //        this.Comments = commentThread.Comments;

        //        //:TODO: Can this be removed?  It should be handled by binding and updates to the models.
        //        ////Any time we view a thread, we check to see if we've seen a post before.
        //        ////If we have, make sure it's not marked as new.
        //        ////If we haven't, add it to the list of comments we've seen, but leave it marked as new.
        //        //foreach (var c in commentThread.Comments)
        //        //{
        //        //	if (CoreServices.Instance.SeenPosts.Contains(c.Id))
        //        //	{
        //        //		c.IsNew = false;
        //        //	}
        //        //	else
        //        //	{
        //        //		CoreServices.Instance.SeenPosts.Add(c.Id);
        //        //		c.IsNew = true;
        //        //	}
        //        //}

        //        var t = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
        //        {
        //            if (commentThread.Comments.Count() > 0) this.commentList.SelectedIndex = 0;
        //        });
        //    }
        //    this.selectedThreadView.DataContext = this;
        //}

        async private void SelectedItemChanged(object sender, SelectionChangedEventArgs e)
        {
            var lv = sender as ListView;
            if (lv == null) return; //This would be bad.
            this.SelectedComment = null;
            this.SetFontSize();

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
                await CoreServices.Instance.MarkCommentRead(this.SelectedThread, this.SelectedComment);
                var container = lv.ContainerFromItem(selectedItem);
                if (container == null) return; //Bail because the visual tree isn't created yet...
                var containerGrid = container.FindControlsNamed<Grid>("container").FirstOrDefault();

                this.currentItemWidth = (int)containerGrid.ActualWidth;// (int)(containerGrid.ActualWidth * ResolutionScaleConverter.ScaleFactor);

                System.Diagnostics.Debug.WriteLine("Width of web view container is {0}", this.currentItemWidth);
                var webView = container.FindControlsNamed<WebView>("bodyWebView").FirstOrDefault() as WebView;
                webView.ScriptNotify += ScriptNotify;
                this.UpdateVisibility(container, false);

                if (webView != null)
                {
                    webView.NavigationStarting += (o, a) => { return; };

                    this.currentWebView = webView;
                    webView.NavigationCompleted += NavigationCompleted;
                    webView.NavigateToString(
                    @"<html xmlns='http://www.w3.org/1999/xhtml'>
						<head>
							<meta name='viewport' content='user-scalable=no'/>
							<style type='text/css'>" + WebBrowserHelper.CSS.Replace("$$$FONTSIZE$$$", this.webFontSize.ToString()) + @"</style>
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
							<div id='commentBody' class='body'>" + selectedItem.Body + @"</div>
						</body>
					</html>");
                }
                return;
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

        #region Private Variables
        #endregion

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
                        if (value.Comments.Count() > 0) this.commentList.SelectedIndex = 0;
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
			this.SizeChanged += Chatty_SizeChanged;
			this.chattyCommentList.SelectionChanged += ChattyListSelectionChanged;
			this.lastUpdateTime.DataContext = CoreServices.Instance;
			this.fullRefreshProgress.DataContext = CoreServices.Instance;
			LatestChattySettings.Instance.PropertyChanged += SettingsChanged;
            Window.Current.SizeChanged += WindowSizeChanged;
        }

        public void LoadChatty()
		{
			var col = CoreServices.Instance.Chatty as INotifyCollectionChanged;
			col.CollectionChanged += ChattyChanged;
			FilterChatty();
			this.sortThreadsButton.DataContext = CoreServices.Instance;
			this.SelectedThread = CoreServices.Instance.Chatty.FirstOrDefault();
		}

		private void SettingsChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName.Equals("ShowRightChattyList"))
			{
				UpdateUI(Window.Current.Bounds.Width);
			}
		}

		void Chatty_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			UpdateUI(e.NewSize.Width);
		}

		private async void ChattyListSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			try
			{
				if (e.RemovedItems.Count > 0)
				{
					var ct = e.RemovedItems[0] as CommentThread;
					await CoreServices.Instance.MarkCommentThreadRead(ct);
				}
			}
			catch
			{ }
			finally
			{
				//this.selectedThreadView.Visibility = vis;
			}
		}

		#region Overrides
		//async protected override void SettingsShown()
		//{
		//	 base.SettingsShown();
		//	 this.settingsVisible = true;
		//	 await this.ShowWebBrush();
		//}

		//protected override void SettingsDismissed()
		//{
		//	 base.SettingsDismissed();
		//	 this.settingsVisible = false;
		//	 this.ShowWebView();
		//}

		//async protected override Task<bool> CorePageKeyActivated(CoreDispatcher sender, AcceleratorKeyEventArgs args)
		//{
		//	 base.CorePageKeyActivated(sender, args);
		//	 //If it's not a key down event, we don't care about it.
		//	 if (args.EventType == CoreAcceleratorKeyEventType.SystemKeyDown ||
		//			args.EventType == CoreAcceleratorKeyEventType.KeyDown)
		//	 {
		//		  var shiftDown = (Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
		//		  var ctrlDown = (Window.Current.CoreWindow.GetKeyState(VirtualKey.Control) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
		//		  switch (args.VirtualKey)
		//		  {
		//				case Windows.System.VirtualKey.Z:
		//					 this.GoToNextComment(shiftDown);
		//					 break;

		//				case Windows.System.VirtualKey.A:
		//					 this.GoToPreviousComment(shiftDown);
		//					 break;

		//				case Windows.System.VirtualKey.P:
		//					 this.TogglePin();
		//					 break;

		//				case Windows.System.VirtualKey.F5:
		//					 if (ctrlDown)
		//					 {
		//						  this.chattyComments.Clear();
		//					 }
		//					 else
		//					 {
		//						  await this.GetSelectedThread();
		//					 }
		//					 break;

		//				case Windows.System.VirtualKey.Back:
		//					 this.Frame.GoBack();
		//					 break;
		//		  }
		//	 }
		//	 //Don't reply unless it's on keyup, to prevent the key up event from going to the reply page.
		//	 if (args.EventType == CoreAcceleratorKeyEventType.KeyUp)
		//	 {
		//		  if (args.VirtualKey == VirtualKey.R)
		//		  {
		//				await this.ReplyToThread();
		//		  }
		//	 }
		//	 return true;
		//}

		#endregion

		#region Events

		private void ChattyChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			FilterChatty();
		}

		async private void MarkAllReadThread(object sender, RoutedEventArgs e)
		{
			if (this.SelectedThread != null)
			{
				await CoreServices.Instance.MarkCommentThreadRead(this.SelectedThread);
			}
		}

		async private void MarkAllRead(object sender, RoutedEventArgs e)
		{
			await CoreServices.Instance.MarkAllCommentsRead();
		}

		async private void PinClicked(object sender, RoutedEventArgs e)
		{
			if (this.SelectedThread != null)
			{
				await CoreServices.Instance.PinThread(this.SelectedThread.Id);
				//await CoreServices.Instance.GetPinnedPosts();
			}
		}

		async private void UnPinClicked(object sender, RoutedEventArgs e)
		{
			if (this.SelectedThread != null)
			{
				await CoreServices.Instance.UnPinThread(this.SelectedThread.Id);
				//await CoreServices.Instance.GetPinnedPosts();
			}
		}


		#endregion

		#region Private Helpers
		private void UpdateUI(double width)
		{
			if (width < 800)
			{
				this.chattyListGroup.MaxWidth = Double.PositiveInfinity;
				Grid.SetRow(this.chattyListGroup, 1);
				Grid.SetRowSpan(this.chattyListGroup, 1);
				Grid.SetColumn(this.chattyListGroup, 2);
				Grid.SetRow(this.divider, 2);
				Grid.SetRowSpan(this.divider, 1);
				Grid.SetColumn(this.divider, 2);
				this.divider.Width = Double.NaN;
				this.divider.Height = 7;
				this.lastUpdateTime.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
				Grid.SetRow(this.selectedThreadView, 3);
				Grid.SetRowSpan(this.selectedThreadView, 1);
				//this.header.Height = 90;
				//if (width < 600)
				//{
				//    this.pageTitle.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
				//}
				//else
				//{
				//    this.pageTitle.Visibility = Windows.UI.Xaml.Visibility.Visible;
				//}
			}
			else
			{
				if (width < 900)
				{
					this.chattyListGroup.MaxWidth = 320;
				}
				else
				{
					this.chattyListGroup.MaxWidth = 400;
				}
				Grid.SetRow(this.chattyListGroup, 1);
				Grid.SetRowSpan(this.chattyListGroup, 3);
				Grid.SetColumn(this.chattyListGroup, LatestChattySettings.Instance.ShowRightChattyList ? 4 : 0);
				Grid.SetRow(this.divider, 0);
				Grid.SetRowSpan(this.divider, 4);
				Grid.SetColumn(this.divider, LatestChattySettings.Instance.ShowRightChattyList ? 3 : 1);
				this.lastUpdateTime.Visibility = Windows.UI.Xaml.Visibility.Visible;
				this.divider.Width = 7;
				this.divider.Height = Double.NaN;
				Grid.SetRow(this.selectedThreadView, 0);
				Grid.SetRowSpan(this.selectedThreadView, 4);
				//this.header.Height = 140;
				//this.pageTitle.Visibility = Windows.UI.Xaml.Visibility.Visible;
			}
			//this.chattyAppBar.HorizontalAlignment = LatestChattySettings.Instance.ShowRightChattyList ? HorizontalAlignment.Right : HorizontalAlignment.Left;
			//this.threadAppBar.HorizontalAlignment = LatestChattySettings.Instance.ShowRightChattyList ? HorizontalAlignment.Left : HorizontalAlignment.Right;
		}

		async private void FilterChatty()
		{
			var selectedItem = this.filterCombo.SelectedValue as ComboBoxItem;

			var filtername = selectedItem != null ? selectedItem.Content as string : "";

			//TODO: reader locks??
			switch (filtername)
			{
				case "participated":
					this.CommentThreads = CoreServices.Instance.Chatty.Where(c => c.UserParticipated);
					break;
				case "has replies":
					this.CommentThreads = CoreServices.Instance.Chatty.Where(c => c.HasRepliesToUser);
					break;
				case "new":
					this.CommentThreads = CoreServices.Instance.Chatty.Where(c => c.HasNewReplies);
					break;
				default:
					//By default show everything.
					this.CommentThreads = CoreServices.Instance.Chatty;
					break;
			}
		}

		#endregion

		private void ReSortClicked(object sender, RoutedEventArgs e)
		{
			CoreServices.Instance.CleanupChattyList();
			this.FilterChatty();
			this.chattyCommentList.ScrollIntoView(CoreServices.Instance.Chatty.First(c => !c.IsPinned), ScrollIntoViewAlignment.Leading);
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

		private void SearchButtonClicked(object sender, RoutedEventArgs e)
		{

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
			this.LoadChatty();
		}




        #endregion
		
    }
}
