using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Networking;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace Latest_Chatty_8.Views
{
	/// <summary>
	/// A basic page that provides characteristics common to most applications.
	/// </summary>
	public sealed partial class Chatty : Latest_Chatty_8.Common.LayoutAwarePage
	{

		#region Private Variables
		private readonly VirtualizableCommentList chattyComments;
		private readonly ObservableCollection<Comment> threadComments;
		private readonly WebViewBrush viewBrush;
		private Comment navigatingToComment;
		/// <summary>
		/// Used to prevent recursive calls to hiding the webview, since we're hiding it on a background thread.
		/// </summary>
		private bool hidingWebView = false;
		private bool settingsVisible = false;
		private bool loadingFromSavedState = false;
		#endregion

		#region Constructor
		public Chatty()
		{
			this.InitializeComponent();
			this.chattyComments = new VirtualizableCommentList();
			this.threadComments = new ObservableCollection<Comment>();
			this.DefaultViewModel["ChattyComments"] = this.chattyComments;
			this.DefaultViewModel["ThreadComments"] = this.threadComments;
			this.chattyCommentList.SelectionChanged += ChattyCommentListSelectionChanged;
			this.bottomBar.DataContext = null;
			this.viewBrush = new WebViewBrush() { SourceName = "web" };
			this.webViewBrushContainer.Fill = this.viewBrush;
			this.threadCommentList.SelectionChanged += (a, b) => this.hidingWebView = false;
			this.web.LoadCompleted += (a, b) => WebPageLoaded();
			this.chattyCommentList.DataFetchSize = 2;
			this.chattyCommentList.IncrementalLoadingThreshold = 1;
		}
		#endregion

		#region Load and Save State
		async protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
		{
			//This means we went forward into a sub view and posted a comment while we were there.
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
									this.loadingFromSavedState = true;
									this.chattyCommentList.SelectedItem = newSelectedComment;
									this.chattyCommentList.ScrollIntoView(newSelectedComment);
									this.loadingFromSavedState = false;
								});
						}
					}
				}
			}
		}

		protected override void SaveState(Dictionary<String, Object> pageState)
		{
			pageState["ChattyComments"] = this.chattyComments.ToList();
			pageState["SelectedChattyComment"] = this.chattyCommentList.SelectedItem as Comment;
			pageState["ThreadComments"] = this.threadComments.ToList();
			pageState["SelectedThreadComment"] = this.threadCommentList.SelectedItem as Comment;
		}
		#endregion

		#region Overrides
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

		async protected override Task<bool> CorePageKeyActivated(CoreDispatcher sender, AcceleratorKeyEventArgs args)
		{
			base.CorePageKeyActivated(sender, args);
			//If it's not a key down event, we don't care about it.
			if (args.EventType == CoreAcceleratorKeyEventType.SystemKeyDown ||
				 args.EventType == CoreAcceleratorKeyEventType.KeyDown)
			{
				var shiftDown = (Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
				var ctrlDown = (Window.Current.CoreWindow.GetKeyState(VirtualKey.Control) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
				switch (args.VirtualKey)
				{
					case Windows.System.VirtualKey.Z:
						this.GoToNextComment(shiftDown);
						break;

					case Windows.System.VirtualKey.A:
						this.GoToPreviousComment(shiftDown);
						break;

					case Windows.System.VirtualKey.P:
						this.TogglePin();
						break;

					case Windows.System.VirtualKey.F5:
						if (ctrlDown)
						{
							this.chattyComments.Clear();
						}
						else
						{
							this.GetSelectedThread();
						}
						break;

					case Windows.System.VirtualKey.Back:
						this.Frame.GoBack();
						break;
				}
			}
			//Don't reply unless it's on keyup, to prevent the key up event from going to the reply page.
			if (args.EventType == CoreAcceleratorKeyEventType.KeyUp)
			{
				if (args.VirtualKey == VirtualKey.R)
				{
					await this.ReplyToThread();
				}
			}
			return true;
		}

		#endregion

		#region Events
		async void ChattyCommentListSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (Windows.UI.ViewManagement.ApplicationView.Value == Windows.UI.ViewManagement.ApplicationViewState.Snapped)
			{
				if (this.loadingFromSavedState) return;
				var clickedComment = e.AddedItems.First() as Comment;
				if (clickedComment != null)
				{
					this.navigatingToComment = clickedComment;
					this.Frame.Navigate(typeof(ThreadView), clickedComment.Id);
				}
			}
			else
			{
				this.GetSelectedThread();
				await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
				{
					this.inlineThreadView.Visibility = Windows.UI.Xaml.Visibility.Visible;
				});
			}
		}

		async private void WebPageLoaded()
		{
			await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
				{
					this.Focus(FocusState.Programmatic);
				});
		}

		private void PreviousPostClicked(object sender, RoutedEventArgs e)
		{
			this.GoToPreviousComment(false);
		}

		private void NextPostClicked(object sender, RoutedEventArgs e)
		{
			this.GoToNextComment(false);
		}

		private void PinClicked(object sender, RoutedEventArgs e)
		{
			var comment = this.threadComments.First();
			if (comment != null)
			{
				comment.IsPinned = true;
			}
		}

		private void UnPinClicked(object sender, RoutedEventArgs e)
		{
			var comment = this.threadComments.First();
			if (comment != null)
			{
				comment.IsPinned = true;
			}
		}

		private void TogglePin()
		{
			var comment = this.threadComments.First();
			if (comment != null)
			{
				comment.IsPinned = !comment.IsPinned;
			}
		}

		async private void ReplyClicked(object sender, RoutedEventArgs e)
		{
			await this.ReplyToThread();
		}

		private void RefreshClicked(object sender, RoutedEventArgs e)
		{
			this.chattyCommentList.ScrollIntoView(this.chattyCommentList.Items[0]);
			this.chattyComments.Clear();
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
		#endregion

		#region Private Helpers
		async private Task ReplyToThread()
		{
			if (this.inlineThreadView.Visibility != Windows.UI.Xaml.Visibility.Visible)
			{
				return;
			}

			if (!CoreServices.Instance.LoggedIn)
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

		private void GoToNextComment(bool shiftDown)
		{
			var listToChange = shiftDown ? this.chattyCommentList : this.threadCommentList;

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
			listToChange.ScrollIntoView(listToChange.SelectedItem);
		}

		private void GoToPreviousComment(bool shiftDown)
		{
			var listToChange = shiftDown ? this.chattyCommentList : this.threadCommentList;

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
			listToChange.ScrollIntoView(listToChange.SelectedItem);
		}

		bool loadingThread = false;
		async private void GetSelectedThread()
		{
			if (this.loadingThread) return;
			this.loadingThread = true;
			try
			{
				this.hidingWebView = false;
				var selectedChattyComment = this.chattyCommentList.SelectedItem as Comment;
				if (selectedChattyComment != null)
				{
					this.bottomBar.DataContext = null;
					this.SetLoading();
					var rootComment = await CommentDownloader.GetComment(selectedChattyComment.Id);

					var threadLocation = this.chattyComments.IndexOf(selectedChattyComment);
					this.chattyComments[threadLocation] = rootComment;

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

					//This seems hacky - I should be able to do this with binding...
					this.replyButtonSection.Visibility = Windows.UI.Xaml.Visibility.Visible;

					this.bottomBar.DataContext = this.threadComments.First();
				}
				else
				{
					this.replyButtonSection.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
				}
			}
			catch (Exception e)
			{
				var dlg = new MessageDialog("There was a problem getting the comment", "Uh oh");
				dlg.ShowAsync();
			}
			finally
			{
				this.loadingThread = false;
				this.UnsetLoading();
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
		//async private void RefreshChattyComments()
		//{
		//	this.SetLoading();
		//	CoreServices.Instance.ClearTileAndRegisterForNotifications();
		//	//var comments = (await CommentDownloader.GetChattyRootComments(1)).ToList();
		//	this.chattyComments.Clear();
		//	this.threadComments.Clear();
		//	//foreach (var c in comments)
		//	//{
		//	//	this.chattyComments.Add(c);
		//	//}
		//	this.UnsetLoading();
		//} 
		#endregion

	}
}
