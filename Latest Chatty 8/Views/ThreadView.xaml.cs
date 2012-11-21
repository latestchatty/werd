using Latest_Chatty_8.Common;
using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Networking;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WinRTXamlToolkit.Controls;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace Latest_Chatty_8.Views
{
	/// <summary>
	/// A basic page that provides characteristics common to most applications.
	/// </summary>
	public sealed partial class ThreadView : Latest_Chatty_8.Common.LayoutAwarePage
	{
		private readonly ObservableCollection<Comment> chattyComments;
		private readonly WebViewBrush bigViewBrush = new WebViewBrush() { SourceName = "fullSizeWebViewer" };
		private readonly WebViewBrush miniViewBrush = new WebViewBrush() { SourceName = "miniWebViewer" };
		/// <summary>
		/// Used to prevent recursive calls to hiding the webview, since we're hiding it on a background thread.
		/// </summary>
		private bool hidingWebView = false;

		//Don't really need this, but it'll make it easier than sifting through the persisted comment collection.
		private int rootCommentId;
		private Comment RootComment
		{
			get { return this.chattyComments.SingleOrDefault(c => c.Id == this.rootCommentId); }
		}

		public ThreadView()
		{
			this.InitializeComponent();
			this.chattyComments = new ObservableCollection<Comment>();
			this.DefaultViewModel["Comments"] = this.chattyComments;
			this.miniWebViewBrushContainer.Fill = miniViewBrush;
			this.webViewBrushContainer.Fill = bigViewBrush;

			this.commentList.SelectionChanged += CommentSelectionChanged;
			this.miniCommentList.SelectionChanged += CommentSelectionChanged;
			this.fullSizeWebViewer.LoadCompleted += (a, b) => this.BrowserLoaded();
			this.miniWebViewer.LoadCompleted += (a, b) => this.BrowserLoaded();
			Window.Current.SizeChanged += (a, b) => this.LoadHTMLForSelectedComment();

		}

		private void BrowserLoaded()
		{
			System.Diagnostics.Debug.WriteLine("Browser Loaded...");
			this.ShowCorrectControls();
		}

		private void ShowCorrectControls()
		{
			Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
			{
				var fullView = ApplicationView.Value != ApplicationViewState.Snapped ? Visibility.Visible : Visibility.Collapsed;
				var miniView = ApplicationView.Value == ApplicationViewState.Snapped ? Visibility.Visible : Visibility.Collapsed;
				this.commentList.Visibility = fullView;
				this.commentSection.Visibility = fullView;
				this.fullSizeWebViewer.Visibility = fullView;
				this.webViewBrushContainer.Visibility = fullView;
				this.miniWebViewer.Visibility = miniView;
				this.miniCommentList.Visibility = miniView;
				this.miniCommentSection.Visibility = miniView;
				this.miniWebViewBrushContainer.Visibility = miniView;
				if (ApplicationView.Value != ApplicationViewState.Snapped)
				{
					this.commentList.ScrollIntoView(this.commentList.SelectedItem);
				}
				else
				{
					this.miniCommentList.ScrollIntoView(this.miniCommentList.SelectedItem);
				}
			});
		}

		private void CommentSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			this.hidingWebView = false;
			this.LoadHTMLForSelectedComment();
		}

		private void LoadHTMLForSelectedComment()
		{
			if (ApplicationView.Value == ApplicationViewState.Snapped)
			{
				var comment = this.miniCommentList.SelectedItem as Comment;
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
		/// <summary>
		/// Populates the page with content passed during navigation.  Any saved state is also
		/// provided when recreating a page from a prior session.
		/// </summary>
		/// <param name="navigationParameter">The parameter value passed to
		/// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
		/// </param>
		/// <param name="pageState">A dictionary of state preserved by this page during an earlier
		/// session.  This will be null the first time a page is visited.</param>
		protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
		{
			CoreServices.Instance.ReturningFromThreadView = true;
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
							this.bottomBar.DataContext = this.RootComment;
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

		/// <summary>
		/// Preserves state associated with this page in case the application is suspended or the
		/// page is discarded from the navigation cache.  Values must conform to the serialization
		/// requirements of <see cref="SuspensionManager.SessionState"/>.
		/// </summary>
		/// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
		protected override void SaveState(Dictionary<String, Object> pageState)
		{
			pageState.Add("Comments", this.chattyComments.ToList());
			pageState.Add("SelectedComment", commentList.SelectedItem as Comment);
			pageState.Add("RootCommentID", this.rootCommentId);
		}

		private void PointerMoved(object sender, PointerRoutedEventArgs e)
		{
			if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
			{
				if (this.hidingWebView)
					return;

				if (((ApplicationView.Value != ApplicationViewState.Snapped) &&
						!RectHelper.Contains(new Rect(new Point(0, 0), this.webViewBrushContainer.RenderSize), e.GetCurrentPoint(this.webViewBrushContainer).RawPosition)) ||
					((ApplicationView.Value == ApplicationViewState.Snapped) &&
						!RectHelper.Contains(new Rect(new Point(0, 0), this.miniWebViewBrushContainer.RenderSize), e.GetCurrentPoint(this.miniWebViewBrushContainer).RawPosition)))
				{
					this.hidingWebView = true;
					if (this.fullSizeWebViewer.Visibility == Windows.UI.Xaml.Visibility.Visible)
					{
						System.Diagnostics.Debug.WriteLine("Full Web Brush Visible");
						this.bigViewBrush.Redraw();
						Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
						{
							this.fullSizeWebViewer.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
						});
					}
					if (this.miniWebViewer.Visibility == Windows.UI.Xaml.Visibility.Visible)
					{
						System.Diagnostics.Debug.WriteLine("Mini Web Brush Visible.");
						this.miniViewBrush.Redraw();
						Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
						{
							this.miniWebViewer.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
						});
					}
				}
			}
		}

		private void PointerEnteredViewBrush(object sender, PointerRoutedEventArgs e)
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

		private void RefreshClicked(object sender, RoutedEventArgs e)
		{
			var selectedComment = commentList.SelectedItem as Comment;
			this.RefreshThread(null, selectedComment == null ? 0 : selectedComment.Id);
		}

		private void ReplyClicked(object sender, RoutedEventArgs e)
		{
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
				Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
					{
						this.commentList.SelectedItem = this.chattyComments.Single(c => c.Id == currentSelectedCommentId);
						this.commentList.ScrollIntoView(this.commentList.SelectedItem, ScrollIntoViewAlignment.Leading);
					});
			}
			else
			{
				this.commentList.SelectedItem = comments.FirstOrDefault();
			}

			this.bottomBar.DataContext = this.RootComment;

			this.loadingBar.IsIndeterminate = false;
			this.loadingBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
		}

		private void PinClicked(object sender, RoutedEventArgs e)
		{
			var comment = this.RootComment;
			comment.IsPinned = true;
		}

		private void UnPinClicked(object sender, RoutedEventArgs e)
		{
			var comment = this.RootComment;
			comment.IsPinned = false;
		}

	}
}
