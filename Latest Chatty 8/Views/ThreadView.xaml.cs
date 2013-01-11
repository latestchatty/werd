using Latest_Chatty_8.Common;
using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Networking;
using Latest_Chatty_8.Settings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace Latest_Chatty_8.Views
{
	/// <summary>
	/// A basic page that provides characteristics common to most applications.
	/// </summary>
	public sealed partial class ThreadView : Latest_Chatty_8.Common.LayoutAwarePage, INotifyPropertyChanged
	{
        private bool npcIsExpired;
        public bool IsExpired
        {
            get { return npcIsExpired; }
            set { this.SetProperty(ref this.npcIsExpired, value); }
        }

		#region Private Variables
		private readonly ObservableCollection<Comment> chattyComments;
		
        private readonly WebViewBrush bigViewBrush = new WebViewBrush() { SourceName = "fullSizeWebViewer" };
		/// <summary>
		/// Used to prevent recursive calls to hiding the webview, since we're hiding it on a background thread.
		/// </summary>
		private bool hidingWebView = false;
        private bool settingsVisible;
        private bool animatingButtons;

		//Don't really need this, but it'll make it easier than sifting through the persisted comment collection.
		private int rootCommentId;
		private Comment RootComment
		{
			get { return this.chattyComments.SingleOrDefault(c => c.Id == this.rootCommentId); }
		}

		#endregion

		#region Constructor
		public ThreadView()
		{
			this.InitializeComponent();

			LatestChattySettings.Instance.PropertyChanged += SettingChanged;
			Window.Current.SizeChanged += WindowSizeChanged;
            this.commentList.SelectionChanged += CommentSelectionChanged;
            this.fullSizeWebViewer.LoadCompleted += BrowserLoaded; 
            
            this.chattyComments = new ObservableCollection<Comment>();
			this.DefaultViewModel["Comments"] = this.chattyComments;

			this.commentList.AppBarToShow = this.BottomAppBar;

			this.SetSplitHeight();
		}

        ~ThreadView()
        {
            Window.Current.SizeChanged -= WindowSizeChanged;
            LatestChattySettings.Instance.PropertyChanged -= SettingChanged;
            this.commentList.SelectionChanged -= CommentSelectionChanged;
            this.fullSizeWebViewer.LoadCompleted -= BrowserLoaded;
        }
		#endregion

		#region Overrides
		async protected override Task<bool> CorePageKeyActivated(CoreDispatcher sender, AcceleratorKeyEventArgs args)
		{
			base.CorePageKeyActivated(sender, args);
			//If it's not a key down event, we don't care about it.
			if (args.EventType == CoreAcceleratorKeyEventType.SystemKeyDown ||
				 args.EventType == CoreAcceleratorKeyEventType.KeyDown)
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
				case Windows.System.VirtualKey.F5:
					await this.RefreshThisThread();
					break;
				case Windows.System.VirtualKey.Back:
					this.Frame.GoBack();
					break;

				default:
					break;
			}

			//Don't reply unless it's on keyup, to prevent the key up event from going to the reply page.
			if (args.EventType == CoreAcceleratorKeyEventType.KeyUp)
			{
				if (args.VirtualKey == VirtualKey.R)
				{
					await this.ReplyToSelectedComment();
				}
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

		private void SettingChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "SplitPercent")
			{
				this.SetSplitHeight();
				this.webViewBrushContainer.Fill = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Transparent);
			}
		}

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

        private void BrowserLoaded(object sender, Windows.UI.Xaml.Navigation.NavigationEventArgs e)
		{
			System.Diagnostics.Debug.WriteLine("Browser Loaded...");
			this.ShowCorrectControls();
		}

		private void CommentSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			this.hidingWebView = false;
			this.LayoutUI();
		}

		async private void MousePointerMoved(object sender, PointerRoutedEventArgs e)
		{
			if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
			{
				//If we're moving the mouse pointer, we don't need these.
				if (!this.animatingButtons && this.nextPrevButtonGrid.Visibility == Visibility.Visible)
				{
					this.animatingButtons = true;
					var storyboard = new Storyboard();
					var animation = new DoubleAnimation();
					animation.Duration = new Duration(TimeSpan.FromMilliseconds(500));
					animation.To = 0;
					animation.EnableDependentAnimation = true;
					Storyboard.SetTarget(animation, this.nextPrevButtonGrid);
					Storyboard.SetTargetProperty(animation, "Width");
					storyboard.Children.Add(animation);
					storyboard.Completed += (a, b) =>
					{
						this.animatingButtons = false;
						this.nextPrevButtonGrid.Visibility = Visibility.Collapsed;
					};
					storyboard.Begin();
				}

				if (this.hidingWebView)
					return;

				if (((ApplicationView.Value != ApplicationViewState.Snapped) &&
						!RectHelper.Contains(new Rect(new Point(0, 0), this.webViewBrushContainer.RenderSize), e.GetCurrentPoint(this.webViewBrushContainer).RawPosition)))
				{
					await this.ShowWebBrush();
				}
			}
		}

		//Prevent de-selection when right clicking.
		private void CommentListRightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			e.Handled = true;
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
			pageState.Add("Comments", this.chattyComments.ToList());
			pageState.Add("SelectedComment", commentList.SelectedItem as Comment);
			pageState.Add("RootCommentID", this.rootCommentId);
		}

		#endregion

		#region Private Helpers
		private void SetSplitHeight()
		{
			this.commentList.Height = Window.Current.CoreWindow.Bounds.Height * (LatestChattySettings.Instance.SplitPercent / 100.0);
		}

		//TODO: Respond to moving from left to right side while remaining snapped.
		private void LayoutUI()
		{
			//var comment = this.commentList.SelectedItem as Comment;
			if (Windows.UI.ViewManagement.ApplicationView.Value == Windows.UI.ViewManagement.ApplicationViewState.Snapped)
			{
				WebBrowserBinding.SetFontSize(this.fullSizeWebViewer, 10);
				if (Window.Current.Bounds.Left == 0) //Snapped Left side.
				{
					this.nextPrevButtonGrid.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Left;
					return;
				}
			}

			if (Windows.UI.ViewManagement.ApplicationView.Value != Windows.UI.ViewManagement.ApplicationViewState.Snapped)
			{
				WebBrowserBinding.SetFontSize(this.fullSizeWebViewer, 14);
			}
			this.nextPrevButtonGrid.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Right;
		}

		async private void ShowCorrectControls()
		{
			await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
			{
				this.commentList.ScrollIntoView(this.commentList.SelectedItem);
				this.bottomBar.Focus(FocusState.Programmatic);
			});
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
			listToChange.ScrollIntoView(listToChange.SelectedItem);
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
			listToChange.ScrollIntoView(listToChange.SelectedItem);
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
				this.Frame.Navigate(typeof(ReplyToCommentView), new ReplyNavParameter(selectedComment, commentList.Items.First() as Comment));
			}
		}

		//This is a bit weird... passing in comments and using those, otherwise refreshing... weird.
		async private void RefreshThread(List<Comment> comments, int currentSelectedCommentId)
		{
			this.loadingBar.IsIndeterminate = true;
			this.loadingBar.Visibility = Windows.UI.Xaml.Visibility.Visible;

			if (comments == null)
			{
				var c = await CommentDownloader.GetComment(this.rootCommentId);
				comments = c.FlattenedComments.ToList();
			}

            var rootComment = comments.First();

            this.IsExpired = (rootComment.Date.AddHours(18).ToUniversalTime() < DateTime.UtcNow);
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

			this.bottomBar.DataContext = rootComment;

			this.LayoutUI();

			this.loadingBar.IsIndeterminate = false;
			this.loadingBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
		}

		private void ShowWebView()
		{
			this.hidingWebView = false;

			System.Diagnostics.Debug.WriteLine("Full Web View Visible.");
			this.fullSizeWebViewer.Visibility = Windows.UI.Xaml.Visibility.Visible;

		}

		private void TogglePin()
		{
			var c = this.chattyComments.First();
			c.IsPinned = !c.IsPinned;
		}

		async private Task ShowWebBrush()
		{
			this.hidingWebView = true;

			System.Diagnostics.Debug.WriteLine("Full Web Brush Visible");
			this.webViewBrushContainer.Fill = bigViewBrush;
			this.bigViewBrush.Redraw();
			await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
			{
				this.fullSizeWebViewer.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
			});

		}
		#endregion

        #region Bindable Base
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
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] String propertyName = null)
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
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var eventHandler = this.PropertyChanged;
            if (eventHandler != null)
            {
                eventHandler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
    }
}
