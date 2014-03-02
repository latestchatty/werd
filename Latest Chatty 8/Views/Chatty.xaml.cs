using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Networking;
using Latest_Chatty_8.Settings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237
namespace Latest_Chatty_8.Views
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class Chatty : Latest_Chatty_8.Common.LayoutAwarePage
    {

        #region Private Variables
        private VirtualizableCommentList chattyComments;
        private readonly ObservableCollection<Comment> threadComments;
        private readonly WebViewBrush viewBrush;
        private Comment navigatingToComment;
        /// <summary>
        /// Used to prevent recursive calls to hiding the webview, since we're hiding it on a background thread.
        /// </summary>
        private bool hidingWebView = false;
        private bool settingsVisible = false;
        #endregion

		  public bool IsLoading { get { return false; } }

        #region Constructor
        public Chatty()
        {
            this.InitializeComponent();
//            LatestChattySettings.Instance.PropertyChanged += SettingChanged;
            //this.threadComments = new ObservableCollection<Comment>();
				this.DefaultViewModel["ChattyComments"] = CoreServices.Instance.Chatty;

            //this.chattyCommentList.DataFetchSize = 2;
            //this.chattyCommentList.IncrementalLoadingThreshold = 1;

				//this.bottomBar.DataContext = null;
				//this.viewBrush = new WebViewBrush() { SourceName = "web" };
				//this.threadCommentList.SelectionChanged += (a, b) => this.hidingWebView = false;
				//this.web.LoadCompleted += (a, b) => WebPageLoaded();
				//this.SetSplitHeight();
				//this.chattyCommentList.AppBarToShow = this.BottomAppBar;
				//this.threadCommentList.AppBarToShow = this.BottomAppBar;
        }

        ~Chatty()
        {
				//LatestChattySettings.Instance.PropertyChanged -= SettingChanged;
        }
        #endregion

        #region Load and Save State
		  //async protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
		  //{
		  //	 this.chattyCommentList.SelectionChanged -= ChattyCommentListSelectionChanged;
		  //	 //This means we went forward into a sub view and posted a comment while we were there.
		  //	 CoreServices.Instance.PostedAComment = false;
		  //	 this.navigatingToComment = null;

		  //	 if (pageState != null)
		  //	 {
		  //		  if (pageState.ContainsKey("ChattyComments"))
		  //		  {
		  //				var ps = pageState["ChattyComments"] as VirtualizableCommentList;
		  //				if (ps != null)
		  //				{
		  //					 this.chattyComments = ps;
		  //					 this.DefaultViewModel["ChattyComments"] = this.chattyComments;
		  //				}
		  //				//Reset the focus to the thread we were viewing.
		  //				if (pageState.ContainsKey("SelectedChattyComment"))
		  //				{
		  //					 var selectedComment = pageState["SelectedChattyComment"] as Comment;
		  //					 if (selectedComment != null)
		  //					 {
		  //						  var newSelectedComment = this.chattyComments.SingleOrDefault(c => c.Id == selectedComment.Id);
		  //						  if (newSelectedComment != null)
		  //						  {
		  //								await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, async () =>
		  //								{
		  //									 this.chattyCommentList.SelectedItem = newSelectedComment;
		  //									 if (Windows.UI.ViewManagement.ApplicationView.Value != ApplicationViewState.Snapped)
		  //									 {
		  //										  await this.GetSelectedThread();
		  //									 }
		  //									 this.chattyCommentList.ScrollIntoView(newSelectedComment);   
		  //								});
		  //								this.chattyCommentList.SelectionChanged += ChattyCommentListSelectionChanged;
		  //						  }
		  //					 }
		  //				}
		  //		  }
		  //	 }

		  //	 if (this.chattyComments == null)
		  //	 {
		  //		  this.chattyComments = new VirtualizableCommentList();
		  //		  this.DefaultViewModel["ChattyComments"] = this.chattyComments;
		  //		  System.Diagnostics.Debug.WriteLine("Binding Selection On New.");
		  //		  this.chattyCommentList.SelectionChanged += ChattyCommentListSelectionChanged;
		  //	 }
		  //}

		  //protected override void SaveState(Dictionary<String, Object> pageState)
		  //{

		  //	 pageState["ChattyComments"] = this.chattyComments;
		  //	 pageState["SelectedChattyComment"] = this.chattyCommentList.SelectedItem as Comment;
		  //}
        #endregion

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
		  //private void SettingChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		  //{
		  //	 if (e.PropertyName == "SplitPercent")
		  //	 {
		  //		  this.SetSplitHeight();
		  //		  this.webViewBrushContainer.Fill = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Transparent);
		  //	 }
		  //}

		  //async void ChattyCommentListSelectionChanged(object sender, SelectionChangedEventArgs e)
		  //{
		  //	 if (Windows.UI.ViewManagement.ApplicationView.Value == Windows.UI.ViewManagement.ApplicationViewState.Snapped)
		  //	 {
		  //		  if (e.AddedItems.Count > 0)
		  //		  {
		  //				var clickedComment = e.AddedItems.First() as Comment;
		  //				if (clickedComment != null)
		  //				{
		  //					 this.navigatingToComment = clickedComment;
		  //					 this.Frame.Navigate(typeof(ThreadView), clickedComment.Id);
		  //				}
		  //		  }
		  //	 }
		  //	 else
		  //	 {
		  //		  await this.GetSelectedThread();
		  //	 }
		  //}

		  //async private void WebPageLoaded()
		  //{
		  //	 await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
		  //		  {
		  //				this.bottomBar.Focus(FocusState.Programmatic);
		  //		  });
		  //}

		  //private void PreviousPostClicked(object sender, RoutedEventArgs e)
		  //{
		  //	 this.GoToPreviousComment(false);
		  //}

		  //private void NextPostClicked(object sender, RoutedEventArgs e)
		  //{
		  //	 this.GoToNextComment(false);
		  //}

		  //private void PinClicked(object sender, RoutedEventArgs e)
		  //{
		  //	 var comment = this.threadComments.First();
		  //	 if (comment != null)
		  //	 {
		  //		  comment.IsPinned = true;
		  //	 }
		  //}

		  //private void UnPinClicked(object sender, RoutedEventArgs e)
		  //{
		  //	 var comment = this.threadComments.First();
		  //	 if (comment != null)
		  //	 {
		  //		  comment.IsPinned = true;
		  //	 }
		  //}

		  //private void TogglePin()
		  //{
		  //	 var comment = this.threadComments.First();
		  //	 if (comment != null)
		  //	 {
		  //		  comment.IsPinned = !comment.IsPinned;
		  //	 }
		  //}

		  //async private void ReplyClicked(object sender, RoutedEventArgs e)
		  //{
		  //	 await this.ReplyToThread();
		  //}

		  //private void RefreshChattyClicked(object sender, RoutedEventArgs e)
		  //{
		  //	 this.chattyCommentList.ScrollIntoView(this.chattyCommentList.Items[0]);
		  //	 this.chattyComments.Clear();
		  //}


		  //async private void RefreshThreadClicked(object sender, RoutedEventArgs e)
		  //{
		  //	 await this.GetSelectedThread();
		  //}

		  //async private void MousePointerMoved(object sender, PointerRoutedEventArgs e)
		  //{
		  //	 if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
		  //	 {
		  //		  //If we're moving the mouse pointer, we don't need these.
		  //		  if (!this.animatingButtons && this.nextPrevButtonGrid.Visibility == Visibility.Visible)
		  //		  {
		  //				this.animatingButtons = true;
		  //				var storyboard = new Storyboard();
		  //				var animation = new DoubleAnimation();
		  //				animation.Duration = new Duration(TimeSpan.FromMilliseconds(500));
		  //				animation.To = 0;
		  //				animation.EnableDependentAnimation = true;
		  //				Storyboard.SetTarget(animation, this.nextPrevButtonGrid);
		  //				Storyboard.SetTargetProperty(animation, "Width");
		  //				storyboard.Children.Add(animation);
		  //				storyboard.Completed += (a, b) =>
		  //				{
		  //					 this.animatingButtons = false;
		  //					 this.nextPrevButtonGrid.Visibility = Visibility.Collapsed;
		  //				};
		  //				storyboard.Begin();
		  //		  }
		  //		  var coords = e.GetCurrentPoint(this.webViewBrushContainer);
		  //		  if (!RectHelper.Contains(new Rect(new Point(0, 0), this.webViewBrushContainer.RenderSize), coords.RawPosition))
		  //		  {
		  //				if (this.web.Visibility == Windows.UI.Xaml.Visibility.Visible)
		  //				{
		  //					 if (hidingWebView)
		  //						  return;
		  //					 await this.ShowWebBrush();
		  //				}
		  //		  }
		  //	 }
		  //}

		  //private void PointerEnteredViewBrush(object sender, PointerRoutedEventArgs e)
		  //{
		  //	 //If we're using a mouse, and settings aren't visible, replace the brush with the view.
		  //	 if ((e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
		  //		  && !this.settingsVisible)
		  //	 {
		  //		  this.ShowWebView();
		  //	 }
		  //}

		  //private void NewRootPostClicked(object sender, RoutedEventArgs e)
		  //{
		  //	 this.Frame.Navigate(typeof(ReplyToCommentView));
		  //}
        #endregion

        #region Private Helpers

		  //private void SetSplitHeight()
		  //{
		  //	 this.threadCommentList.Height = Window.Current.CoreWindow.Bounds.Height * (LatestChattySettings.Instance.SplitPercent / 100.0);
		  //}

		  //async private Task ReplyToThread()
		  //{
		  //	 if (this.inlineThreadView.Visibility != Windows.UI.Xaml.Visibility.Visible)
		  //	 {
		  //		  return;
		  //	 }

		  //	 if (!CoreServices.Instance.LoggedIn)
		  //	 {
		  //		  var dialog = new MessageDialog("You must login before you can post.  Login information can be set in the application settings.");
		  //		  await dialog.ShowAsync();
		  //		  return;
		  //	 }
		  //	 var comment = this.threadCommentList.SelectedItem as Comment;
		  //	 if (comment != null)
		  //	 {
		  //		  this.Frame.Navigate(typeof(ReplyToCommentView), new ReplyNavParameter(comment, this.threadComments.First()));
		  //	 }
		  //}

		  //async private Task ShowWebBrush()
		  //{
		  //	 hidingWebView = true;
		  //	 System.Diagnostics.Debug.WriteLine("Replacing WebView with Brush.");
		  //	 this.webViewBrushContainer.Fill = this.viewBrush;
		  //	 this.viewBrush.Redraw();
		  //	 //Hiding the browser with low priority seems to give a chance to draw the frame and gets rid of flickering.
		  //	 await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
		  //	 {
		  //		  this.web.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
		  //	 });
		  //}

		  //private void ShowWebView()
		  //{
		  //	 hidingWebView = false;
		  //	 System.Diagnostics.Debug.WriteLine("Replacing brush with view.");
		  //	 this.web.Visibility = Windows.UI.Xaml.Visibility.Visible;
		  //}

		  //private void GoToNextComment(bool shiftDown)
		  //{
		  //	 //If we're already loading, wait until that's finished.
		  //	 if (shiftDown && this.loadingThread)
		  //	 {
		  //		  return;
		  //	 }
		  //	 var listToChange = shiftDown ? this.chattyCommentList : this.threadCommentList;

		  //	 if (listToChange.Items.Count == 0)
		  //	 {
		  //		  return;
		  //	 }
		  //	 if (listToChange.SelectedIndex >= listToChange.Items.Count - 1)
		  //	 {
		  //		  listToChange.SelectedIndex = 0;
		  //	 }
		  //	 else
		  //	 {
		  //		  listToChange.SelectedIndex++;
		  //	 }
		  //	 listToChange.ScrollIntoView(listToChange.SelectedItem);
		  //}

		  //private void GoToPreviousComment(bool shiftDown)
		  //{
		  //	 //If we're already loading, wait until that's finished.
		  //	 if (shiftDown && this.loadingThread)
		  //	 {
		  //		  return;
		  //	 }
		  //	 var listToChange = shiftDown ? this.chattyCommentList : this.threadCommentList;

		  //	 if (listToChange.Items.Count == 0)
		  //	 {
		  //		  return;
		  //	 }
		  //	 if (listToChange.SelectedIndex <= 0)
		  //	 {
		  //		  listToChange.SelectedIndex = listToChange.Items.Count - 1;
		  //	 }
		  //	 else
		  //	 {
		  //		  listToChange.SelectedIndex--;
		  //	 }
		  //	 listToChange.ScrollIntoView(listToChange.SelectedItem);
		  //}

		  //bool loadingThread = false;
		  //private bool animatingButtons;

		  //async private Task GetSelectedThread()
		  //{
		  //	 if (this.loadingThread) return;
		  //	 this.loadingThread = true;
		  //	 this.DefaultViewModel["CanSelect"] = false;
		  //	 this.webViewBrushContainer.Fill = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Transparent);
		  //	 var errorMessage = string.Empty;

		  //	 try
		  //	 {
		  //		  this.hidingWebView = false;
		  //		  this.replyButtonSection.Visibility = Visibility.Collapsed;
		  //		  var selectedChattyComment = this.chattyCommentList.SelectedItem as Comment;
		  //		  if (selectedChattyComment != null)
		  //		  {
		  //				this.bottomBar.DataContext = null;
		  //				this.SetLoading();
		  //				var rootComment = await CommentDownloader.GetComment(selectedChattyComment.Id);
                    
		  //				if (rootComment != null)
		  //				{
		  //					 var threadLocation = this.chattyComments.IndexOf(selectedChattyComment);
		  //					 //this.chattyComments[threadLocation] = rootComment;
		  //					 this.threadComments.Clear();
		  //					 foreach (var c in rootComment.FlattenedComments.ToList())
		  //					 {
		  //						  this.threadComments.Add(c);
		  //					 }
		  //					 this.threadCommentList.SelectedItem = rootComment;
		  //					 this.threadCommentList.ScrollIntoView(rootComment, ScrollIntoViewAlignment.Leading);
                        
		  //					 //This seems hacky - I should be able to do this with binding...
		  //					 this.replyButtonSection.Visibility = Windows.UI.Xaml.Visibility.Visible;

		  //					 this.bottomBar.DataContext = this.threadComments.First();

		  //					 this.inlineThreadView.Visibility = Windows.UI.Xaml.Visibility.Visible;
		  //				}
		  //		  }
		  //		  else
		  //		  {
		  //				this.replyButtonSection.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
		  //				this.inlineThreadView.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
		  //		  }
		  //	 }
		  //	 catch (Exception e)
		  //	 {
		  //		  errorMessage = "There was a problem getting the comment";
		  //	 }
		  //	 finally
		  //	 {
		  //		  this.loadingThread = false;
		  //		  this.DefaultViewModel["CanSelect"] = true;
		  //		  this.UnsetLoading();
		  //	 }
		  //	 if (!string.IsNullOrEmpty(errorMessage))
		  //	 {
		  //		  var dlg = new MessageDialog(errorMessage, "Uh oh");
		  //		  await dlg.ShowAsync();
		  //	 }
		  //}

		  //private void SetLoading()
		  //{
		  //	 this.loadingBar.IsIndeterminate = true;
		  //	 this.loadingBar.Visibility = Visibility.Visible;
		  //}

		  //private void UnsetLoading()
		  //{
		  //	 this.loadingBar.IsIndeterminate = false;
		  //	 this.loadingBar.Visibility = Visibility.Collapsed;
		  //}
        #endregion
    }
}
