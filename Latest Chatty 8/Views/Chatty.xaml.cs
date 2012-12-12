using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Networking;
using Latest_Chatty_8.Settings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ApplicationSettings;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace Latest_Chatty_8.Views
{
	/// <summary>
	/// A basic page that provides characteristics common to most applications.
	/// </summary>
	public sealed partial class Chatty : Latest_Chatty_8.Common.LayoutAwarePage
	{

		private readonly ObservableCollection<Comment> chattyComments;
		private readonly ObservableCollection<Comment> threadComments;
		private readonly WebViewBrush viewBrush;
		private Comment navigatingToComment;
		/// <summary>
		/// Used to prevent recursive calls to hiding the webview, since we're hiding it on a background thread.
		/// </summary>
		private bool hidingWebView = false;
		private bool settingsVisible = false;
		private bool returnedFromPosting = false;

		public Chatty()
		{
			this.InitializeComponent();
			this.chattyComments = new ObservableCollection<Comment>();
			this.threadComments = new ObservableCollection<Comment>();
			this.DefaultViewModel["ChattyComments"] = this.chattyComments;
			this.DefaultViewModel["ThreadComments"] = this.threadComments;
			this.chattyCommentList.SelectionChanged += ChattyCommentListSelectionChanged;
			this.bottomBar.DataContext = null;
			this.viewBrush = new WebViewBrush() { SourceName = "web" };
			this.webViewBrushContainer.Fill = this.viewBrush;
			this.threadCommentList.SelectionChanged += (a, b) => this.hidingWebView = false;
			this.web.LoadCompleted += (a, b) => WebPageLoaded();
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
		async protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
		{
			//This means we went forward into a sub view and posted a comment while we were there.
			this.returnedFromPosting = (this.Frame.CanGoForward && (CoreServices.Instance.PostedAComment));
			CoreServices.Instance.PostedAComment = false;
			this.navigatingToComment = null;


			if (pageState != null)
			{
				if (pageState.ContainsKey("ChattyComments"))
				{
					var ps = pageState["ChattyComments"] as List<Comment>;
					if (ps != null)
					{
						foreach (var c in ps)
						{
							this.chattyComments.Add(c);
						}
					}
				}
				//Reset the focus to the thread we were viewing.
				if (pageState.ContainsKey("SelectedChattyComment"))
				{
					var ps = pageState["SelectedChattyComment"] as Comment;
					if (ps != null)
					{
						var newSelectedComment = this.chattyComments.SingleOrDefault(c => c.Id == ps.Id);
						if (newSelectedComment != null)
						{
							await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
								{
									if (this.chattyCommentList.Visibility == Windows.UI.Xaml.Visibility.Visible)
									{
										this.chattyCommentList.SelectedItem = newSelectedComment;
										this.chattyCommentList.ScrollIntoView(newSelectedComment);
									}
									else
									{
										this.chattyCommentListSnapped.ScrollIntoView(newSelectedComment);
									}
								});
						}
					}
				}
			}

			if (this.chattyComments.Count == 0)
			{
				this.RefreshChattyComments();
			}
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

		async private void RefreshChattyComments()
		{
			this.SetLoading();
			CoreServices.Instance.ClearAndRegisterForNotifications();
			var comments = await CommentDownloader.GetChattyRootComments();
			this.chattyComments.Clear();
			this.threadComments.Clear();
			foreach (var c in comments)
			{
				this.chattyComments.Add(c);
			}
			this.UnsetLoading();
		}

		async void ChattyCommentListSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			this.GetSelectedThread();
			await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
			{
				this.inlineThreadView.Visibility = Windows.UI.Xaml.Visibility.Visible;
			});
		}

		async private void WebPageLoaded()
		{
			await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
				{
					this.Focus(FocusState.Programmatic);
				});
		}

		private bool shiftDown = false;
		protected override void CorePageKeyUp(CoreWindow sender, KeyEventArgs args)
		{
			if (args.VirtualKey == Windows.System.VirtualKey.Shift)
			{
				shiftDown = false;
			}
		}

		protected override void CorePageKeyDown(CoreWindow sender, KeyEventArgs args)
		{
			base.CorePageKeyDown(sender, args);

			var listToChange = shiftDown ? this.chattyCommentList : this.threadCommentList;

			switch (args.VirtualKey)
			{
				case Windows.System.VirtualKey.Shift:
					shiftDown = true;
					break;

				case Windows.System.VirtualKey.Z:
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
					break;

				case Windows.System.VirtualKey.A:
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
					break;

				case Windows.System.VirtualKey.P:
					this.TogglePin();
					break;

				case Windows.System.VirtualKey.R:
					this.ReplyToThread();
					break;

				case Windows.System.VirtualKey.F5:
					this.RefreshChattyComments();
					break;

				case Windows.System.VirtualKey.Back:
					this.Frame.GoBack();
					break;
			}
		}

		/// <summary>
		/// Preserves state associated with this page in case the application is suspended or the
		/// page is discarded from the navigation cache.  Values must conform to the serialization
		/// requirements of <see cref="SuspensionManager.SessionState"/>.
		/// </summary>
		/// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
		protected override void SaveState(Dictionary<String, Object> pageState)
		{
			pageState["ChattyComments"] = this.chattyComments.ToList();
			//TODO: work with things based on visibility...
			//TODO: These probably should be the same control and just styled differently.
			if (this.chattyCommentList.Visibility == Windows.UI.Xaml.Visibility.Visible)
			{
				pageState["SelectedChattyComment"] = this.chattyCommentList.SelectedItem as Comment;
			}
			else
			{
				pageState["SelectedChattyComment"] = this.navigatingToComment;
			}
			pageState["ThreadComments"] = this.threadComments.ToList();
			pageState["SelectedThreadComment"] = this.threadCommentList.SelectedItem as Comment;
		}

		async private void GetSelectedThread()
		{
			this.hidingWebView = false;
			var selectedChattyComment = this.chattyCommentList.SelectedItem as Comment;
			this.bottomBar.DataContext = null;
			if (selectedChattyComment != null)
			{
				this.SetLoading();
				var rootComment = await CommentDownloader.GetComment(selectedChattyComment.Id);

				if (this.returnedFromPosting)
				{
					var threadLocation = this.chattyComments.IndexOf(selectedChattyComment);
					this.chattyComments.Remove(selectedChattyComment);
					this.chattyComments.Insert(threadLocation, rootComment);
					this.returnedFromPosting = false;
				}

				this.threadComments.Clear();
				foreach (var c in rootComment.FlattenedComments.ToList())
				{
					this.threadComments.Add(c);
				}

				this.threadCommentList.SelectedItem = rootComment;
				await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
				{
					this.threadCommentList.ScrollIntoView(rootComment, ScrollIntoViewAlignment.Leading);
				});

				this.bottomBar.DataContext = rootComment;
				//This seems hacky - I should be able to do this with binding...
				this.replyButtonSection.Visibility = Windows.UI.Xaml.Visibility.Visible;
				this.UnsetLoading();
			}
			else
			{
				this.replyButtonSection.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
			}
		}

		private void SetLoading()
		{
			this.loadingBar.IsIndeterminate = true;
			this.loadingBar.Visibility = Visibility.Visible;
		}

		private void UnsetLoading()
		{
			this.loadingBar.IsIndeterminate = false;
			this.loadingBar.Visibility = Visibility.Collapsed;
		}

		private void SnappedCommentListItemClicked(object sender, ItemClickEventArgs e)
		{
			var clickedComment = e.ClickedItem as Comment;
			if (clickedComment != null)
			{
				this.navigatingToComment = clickedComment;
				this.Frame.Navigate(typeof(ThreadView), clickedComment.Id);
			}
		}

		private void PinClicked(object sender, RoutedEventArgs e)
		{
			var comment = this.chattyCommentList.SelectedItem as Comment;
			if (comment != null)
			{
				comment.IsPinned = true;
			}
		}

		private void UnPinClicked(object sender, RoutedEventArgs e)
		{
			var comment = this.chattyCommentList.SelectedItem as Comment;
			if (comment != null)
			{
				comment.IsPinned = true;
			}
		}

		private void TogglePin()
		{
			var comment = this.chattyCommentList.SelectedItem as Comment;
			if (comment != null)
			{
				comment.IsPinned = !comment.IsPinned;
			}
		}

		async private void ReplyClicked(object sender, RoutedEventArgs e)
		{
			await this.ReplyToThread();
		}

		async private Task ReplyToThread()
		{
			if (this.inlineThreadView.Visibility != Windows.UI.Xaml.Visibility.Visible)
			{
				return;
			}

			if (!CoreServices.Instance.LoginVerified)
			{
				var dialog = new MessageDialog("You must login before you can post.  Login information can be set in the application settings.");
				await dialog.ShowAsync();
				return;
			}
			var comment = this.threadCommentList.SelectedItem as Comment;
			if (comment != null)
			{
				this.Frame.Navigate(typeof(ReplyToCommentView), comment);
			}
		}

		private void RefreshClicked(object sender, RoutedEventArgs e)
		{
			this.RefreshChattyComments();
		}

		async private void MousePointerMoved(object sender, PointerRoutedEventArgs e)
		{
			if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
			{
				var coords = e.GetCurrentPoint(this.webViewBrushContainer);
				if (!RectHelper.Contains(new Rect(new Point(0, 0), this.webViewBrushContainer.RenderSize), coords.RawPosition))
				{
					if (this.web.Visibility == Windows.UI.Xaml.Visibility.Visible)
					{
						if (hidingWebView)
							return;
						await this.ShowWebBrush();
					}
				}
			}
		}

		private void PointerEnteredViewBrush(object sender, PointerRoutedEventArgs e)
		{
			//If we're using a mouse, and settings aren't visible, replace the brush with the view.
			if ((e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
				&& !this.settingsVisible)
			{
				this.ShowWebView();
			}
		}

		private void NewRootPostClicked(object sender, RoutedEventArgs e)
		{
			this.Frame.Navigate(typeof(ReplyToCommentView));
		}

		async private Task ShowWebBrush()
		{
			hidingWebView = true;
			System.Diagnostics.Debug.WriteLine("Replacing WebView with Brush.");
			this.viewBrush.Redraw();
			//Hiding the browser with low priority seems to give a chance to draw the frame and gets rid of flickering.
			await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
			{
				this.web.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
			});
		}

		private void ShowWebView()
		{
			hidingWebView = false;
			System.Diagnostics.Debug.WriteLine("Replacing brush with view.");
			this.web.Visibility = Windows.UI.Xaml.Visibility.Visible;
		}
	}
}
