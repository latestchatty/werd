using Latest_Chatty_8.Common;
using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Networking;
using Latest_Chatty_8.Settings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Popups;
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

		private ListView CurrentViewedList
		{
			get { return (ApplicationView.Value != ApplicationViewState.Snapped ? this.commentList : this.miniCommentList); }
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
			Windows.UI.Core.CoreWindow.GetForCurrentThread().KeyDown += WindowKeyDown; 
			Window.Current.SizeChanged += (a, b) => this.LoadHTMLForSelectedComment();
		}

		async private void WindowKeyDown(object sender, KeyEventArgs e)
		{
			var viewedList = this.CurrentViewedList;
			if (viewedList != null)
			{
				if (e.VirtualKey == Windows.System.VirtualKey.Z)
				{
					if (viewedList.SelectedIndex >= 0)
					{
						if (viewedList.SelectedIndex >= viewedList.Items.Count - 1)
						{
							viewedList.SelectedIndex = 0;
						}
						else
						{
							viewedList.SelectedIndex++;
						}
					}
				}
				else if (e.VirtualKey == Windows.System.VirtualKey.A)
				{
					if (viewedList.SelectedIndex <= 0)
					{
						viewedList.SelectedIndex = viewedList.Items.Count - 1;
					}
					else
					{
						viewedList.SelectedIndex--;
					}
				}
				viewedList.ScrollIntoView(viewedList.SelectedItem, ScrollIntoViewAlignment.Leading);
			}

			switch (e.VirtualKey)
			{
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
		}

		private void BrowserLoaded()
		{
			System.Diagnostics.Debug.WriteLine("Browser Loaded...");
			this.ShowCorrectControls();
		}

		async private void ShowCorrectControls()
		{
			await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
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
				var viewedList = this.CurrentViewedList;
				viewedList.ScrollIntoView(viewedList.SelectedItem);
				this.Focus(FocusState.Programmatic);
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
			Windows.UI.Core.CoreWindow.GetForCurrentThread().KeyDown -= WindowKeyDown;
		}

		async private void MousePointerMoved(object sender, PointerRoutedEventArgs e)
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

		async private void RefreshClicked(object sender, RoutedEventArgs e)
		{
			await this.RefreshThisThread();
		}

		async private Task RefreshThisThread()
		{
			var selectedComment = commentList.SelectedItem as Comment;
			this.RefreshThread(null, selectedComment == null ? 0 : selectedComment.Id);
		}

		async private void ReplyClicked(object sender, RoutedEventArgs e)
		{
			await this.ReplyToSelectedComment();
		}

		async private Task ReplyToSelectedComment()
		{
			if (!CoreServices.Instance.LoginVerified)
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

		private void TogglePin()
		{
			this.RootComment.IsPinned = !this.RootComment.IsPinned;
		}
	}
}
