using Latest_Chatty_8.Common;
using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Networking;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace Latest_Chatty_8.Views
{
	/// <summary>
	/// A basic page that provides characteristics common to most applications.
	/// </summary>
	public sealed partial class ThreadView : Latest_Chatty_8.Common.LayoutAwarePage
	{
		#region Private Variables
		private readonly ObservableCollection<Comment> chattyComments;
		private readonly WebViewBrush bigViewBrush = new WebViewBrush() { SourceName = "fullSizeWebViewer" };
		private readonly WebViewBrush miniViewBrush = new WebViewBrush() { SourceName = "miniWebViewer" };
		/// <summary>
		/// Used to prevent recursive calls to hiding the webview, since we're hiding it on a background thread.
		/// </summary>
		private bool hidingWebView = false;

		//Don't really need this, but it'll make it easier than sifting through the persisted comment collection.
		private int rootCommentId;
		private bool settingsVisible;
		private Comment RootComment
		{
			get { return this.chattyComments.SingleOrDefault(c => c.Id == this.rootCommentId); }
		}

		#endregion

		#region Constructor
		public ThreadView()
		{
			this.InitializeComponent();
			this.chattyComments = new ObservableCollection<Comment>();
			this.DefaultViewModel["Comments"] = this.chattyComments;
			this.miniWebViewBrushContainer.Fill = miniViewBrush;
			this.webViewBrushContainer.Fill = bigViewBrush;

			this.commentList.SelectionChanged += CommentSelectionChanged;
			this.fullSizeWebViewer.LoadCompleted += (a, b) => this.BrowserLoaded();
			this.miniWebViewer.LoadCompleted += (a, b) => this.BrowserLoaded();
			Window.Current.SizeChanged += (a, b) => this.LoadHTMLForSelectedComment();
		}
		#endregion

		#region Overrides
		async protected override Task<bool> CorePageKeyActivated(CoreDispatcher sender, AcceleratorKeyEventArgs args)
		{
			base.CorePageKeyActivated(sender, args);
			//If it's not a key down event, we don't care about it.
			if (args.EventType != CoreAcceleratorKeyEventType.SystemKeyDown &&
				 args.EventType != CoreAcceleratorKeyEventType.KeyDown)
			{
				return true;
			}

			switch (args.VirtualKey)
			{
				case Windows.System.VirtualKey.A:
					this.GoToPreviousComment();
					break;
				case Windows.System.VirtualKey.Z:
					this.GoToNextComment();
					break;
				case Windows.System.VirtualKey.P:
					this.TogglePin();
					break;
				case Windows.System.VirtualKey.R:
					await this.ReplyToSelectedComment();
					break;
				case Windows.System.VirtualKey.F5:
					await this.RefreshThisThread();
					break;
				case Windows.System.VirtualKey.Back:
					this.Frame.GoBack();
					break;

				default:
					break;
			}
			return true;
		}

		async protected override void SettingsShown()
		{
			base.SettingsShown();
			this.settingsVisible = true;
			await this.ShowWebBrush();
		}

		protected override void SettingsDismissed()
		{
			base.SettingsDismissed();
			this.settingsVisible = false;
			this.ShowWebView();
		}

		#endregion

		#region Events
		private void WindowSizeChanged(object sender, WindowSizeChangedEventArgs e)
		{
			this.LayoutUI();
		}

		private void PinClicked(object sender, RoutedEventArgs e)
		{
			var comment = this.chattyComments.First();
			comment.IsPinned = true;
		}

		private void UnPinClicked(object sender, RoutedEventArgs e)
		{
			var comment = this.chattyComments.First();
			comment.IsPinned = false;
		}

		private void PreviousPostClicked(object sender, RoutedEventArgs e)
		{
			this.GoToPreviousComment();
		}

		private void NextPostClicked(object sender, RoutedEventArgs e)
		{
			this.GoToNextComment();
		}

		private void BrowserLoaded()
		{
			System.Diagnostics.Debug.WriteLine("Browser Loaded...");
			this.ShowCorrectControls();
		}

		private void CommentSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			this.hidingWebView = false;
			this.LoadHTMLForSelectedComment();
		}

		async private void MousePointerMoved(object sender, PointerRoutedEventArgs e)
		{
			if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
			{
				//If we're moving the mouse pointer, we don't need these.
				if (this.nextPrevButtonGrid.Visibility == Visibility.Visible)
				{
					this.nextPrevButtonGrid.Visibility = Visibility.Collapsed;
				}

				if (this.hidingWebView)
					return;

				if (((ApplicationView.Value != ApplicationViewState.Snapped) &&
						!RectHelper.Contains(new Rect(new Point(0, 0), this.webViewBrushContainer.RenderSize), e.GetCurrentPoint(this.webViewBrushContainer).RawPosition)) ||
					((ApplicationView.Value == ApplicationViewState.Snapped) &&
						!RectHelper.Contains(new Rect(new Point(0, 0), this.miniWebViewBrushContainer.RenderSize), e.GetCurrentPoint(this.miniWebViewBrushContainer).RawPosition)))
				{
					await this.ShowWebBrush();
				}
			}
		}

		private void PointerEnteredViewBrush(object sender, PointerRoutedEventArgs e)
		{
			if (this.settingsVisible) return;

			this.ShowWebView();
		}

		async private void RefreshClicked(object sender, RoutedEventArgs e)
		{
			await this.RefreshThisThread();
		}

		async private void ReplyClicked(object sender, RoutedEventArgs e)
		{
			await this.ReplyToSelectedComment();
		}
		#endregion

		#region Load and Save State
		protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
		{
			Window.Current.SizeChanged += WindowSizeChanged;
			var threadId = (int)navigationParameter;
			List<Comment> comment = null;
			int selectedCommentId = threadId;

			if (pageState != null)
			{
				if (pageState.ContainsKey("RootCommentID"))
				{
					var persistedCommentId = (int)pageState["RootCommentID"];

					if (threadId == persistedCommentId)
					{
						//If we didn't post a comment, we can use the cache.  Otherwise we need to refresh.
						if (pageState.ContainsKey("Comments") && !CoreServices.Instance.PostedAComment)
						{
							comment = (List<Comment>)pageState["Comments"];
							this.bottomBar.DataContext = comment.First();
						}
						if (pageState.ContainsKey("SelectedComment"))
						{
							selectedCommentId = ((Comment)pageState["SelectedComment"]).Id;
						}
					}
				}
			}

			this.rootCommentId = threadId;
			this.RefreshThread(comment, selectedCommentId);
		}

		protected override void SaveState(Dictionary<String, Object> pageState)
		{
			Window.Current.SizeChanged -= WindowSizeChanged;
			pageState.Add("Comments", this.chattyComments.ToList());
			pageState.Add("SelectedComment", commentList.SelectedItem as Comment);
			pageState.Add("RootCommentID", this.rootCommentId);
		}

		#endregion

		#region Private Helpers

		//TODO: Respond to moving from left to right side while remaining snapped.
		private void LayoutUI()
		{
			if (Windows.UI.ViewManagement.ApplicationView.Value == Windows.UI.ViewManagement.ApplicationViewState.Snapped)
			{
				if (Window.Current.Bounds.Left == 0) //Snapped Left side.
				{
					this.nextPrevButtonGrid.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Left;
					return;
				}
			}

			this.nextPrevButtonGrid.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Right;
		} 

		async private void ShowCorrectControls()
		{
			await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
			{
				var fullView = ApplicationView.Value != ApplicationViewState.Snapped ? Visibility.Visible : Visibility.Collapsed;
				var miniView = ApplicationView.Value == ApplicationViewState.Snapped ? Visibility.Visible : Visibility.Collapsed;
				//this.commentList.Visibility = fullView;
				this.commentSection.Visibility = fullView;
				this.fullSizeWebViewer.Visibility = fullView;
				this.webViewBrushContainer.Visibility = fullView;
				this.miniWebViewer.Visibility = miniView;
				this.miniCommentSection.Visibility = miniView;
				this.miniWebViewBrushContainer.Visibility = miniView;
				this.commentList.ScrollIntoView(this.commentList.SelectedItem);
				this.Focus(FocusState.Programmatic);
			});
		}

		private void LoadHTMLForSelectedComment()
		{
			if (ApplicationView.Value == ApplicationViewState.Snapped)
			{
				var comment = this.commentList.SelectedItem as Comment;
				if (comment != null)
				{
					this.miniWebViewer.NavigateToString(WebBrowserHelper.CommentHTMLTemplate.Replace("$$CSS$$", WebBrowserHelper.MiniCSS).Replace("$$BODY$$", comment.Body));
				}
			}
			else
			{
				var comment = this.commentList.SelectedItem as Comment;
				if (comment != null)
				{
					this.fullSizeWebViewer.NavigateToString(WebBrowserHelper.CommentHTMLTemplate.Replace("$$CSS$$", WebBrowserHelper.FullSizeCSS).Replace("$$BODY$$", comment.Body));
				}
			}
		}

		private void GoToNextComment()
		{
			var listToChange = this.commentList;

			if (listToChange.Items.Count == 0)
			{
				return;
			}
			if (listToChange.SelectedIndex >= listToChange.Items.Count - 1)
			{
				listToChange.SelectedIndex = 0;
			}
			else
			{
				listToChange.SelectedIndex++;
			}
			listToChange.ScrollIntoView(listToChange.SelectedItem, ScrollIntoViewAlignment.Leading);
		}

		private void GoToPreviousComment()
		{
			var listToChange = this.commentList;

			if (listToChange.Items.Count == 0)
			{
				return;
			}
			if (listToChange.SelectedIndex <= 0)
			{
				listToChange.SelectedIndex = listToChange.Items.Count - 1;
			}
			else
			{
				listToChange.SelectedIndex--;
			}
			listToChange.ScrollIntoView(listToChange.SelectedItem, ScrollIntoViewAlignment.Leading);
		}

		async private Task RefreshThisThread()
		{
			var selectedComment = commentList.SelectedItem as Comment;
			this.RefreshThread(null, selectedComment == null ? 0 : selectedComment.Id);
		}

		async private Task ReplyToSelectedComment()
		{
			if (!CoreServices.Instance.LoggedIn)
			{
				var dialog = new MessageDialog("You must login before you can post.  Login information can be set in the application settings.");
				await dialog.ShowAsync();
				return;
			}
			var selectedComment = commentList.SelectedItem as Comment;
			if (selectedComment != null)
			{
				this.Frame.Navigate(typeof(ReplyToCommentView), selectedComment);
			}
		}

		//This is a bit weird... passing in comments and using those, otherwise refreshing... weird.
		async private void RefreshThread(List<Comment> comments, int currentSelectedCommentId)
		{
			this.loadingBar.IsIndeterminate = true;
			this.loadingBar.Visibility = Windows.UI.Xaml.Visibility.Visible;

			if (comments == null)
			{
				var rootComment = await CommentDownloader.GetComment(this.rootCommentId);
				comments = rootComment.FlattenedComments.ToList();
			}

			this.chattyComments.Clear();
			foreach (var c in comments)
			{
				this.chattyComments.Add(c);
			}

			if (currentSelectedCommentId != 0)
			{
				await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
				{
					this.commentList.SelectedItem = this.chattyComments.Single(c => c.Id == currentSelectedCommentId);
					this.commentList.ScrollIntoView(this.commentList.SelectedItem, ScrollIntoViewAlignment.Leading);
				});
			}
			else
			{
				this.commentList.SelectedItem = comments.FirstOrDefault();
			}

			this.bottomBar.DataContext = this.chattyComments.First();

			this.loadingBar.IsIndeterminate = false;
			this.loadingBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
		}

		private void ShowWebView()
		{
			this.hidingWebView = false;
			if (ApplicationView.Value != ApplicationViewState.Snapped)
			{
				System.Diagnostics.Debug.WriteLine("Full Web View Visible.");
				this.fullSizeWebViewer.Visibility = Windows.UI.Xaml.Visibility.Visible;
			}
			else
			{
				System.Diagnostics.Debug.WriteLine("Mini Web View Visible.");
				this.miniWebViewer.Visibility = Windows.UI.Xaml.Visibility.Visible;
			}
		}

		private void TogglePin()
		{
			var c = this.chattyComments.First();
			c.IsPinned = !c.IsPinned;
		}

		async private Task ShowWebBrush()
		{
			this.hidingWebView = true;
			if (this.fullSizeWebViewer.Visibility == Windows.UI.Xaml.Visibility.Visible)
			{
				System.Diagnostics.Debug.WriteLine("Full Web Brush Visible");
				this.bigViewBrush.Redraw();
				await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
				{
					this.fullSizeWebViewer.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
				});
			}
			if (this.miniWebViewer.Visibility == Windows.UI.Xaml.Visibility.Visible)
			{
				System.Diagnostics.Debug.WriteLine("Mini Web Brush Visible.");
				this.miniViewBrush.Redraw();
				await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
				{
					this.miniWebViewer.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
				});
			}
		}
		#endregion
	}
}
