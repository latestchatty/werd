using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Networking;
using Latest_Chatty_8.Settings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
	public sealed partial class Chatty : Latest_Chatty_8.Common.LayoutAwarePage, INotifyPropertyChanged
	{
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

		#region Private Variables
		//private VirtualizableCommentList chattyComments;
		#endregion

		public bool IsLoading { get { return false; } }

		private Comment npcSelectedComment;
		public Comment SelectedComment
		{
			get { return this.npcSelectedComment; }
			set { this.SetProperty(ref this.npcSelectedComment, value); }
		}

		private ReadOnlyObservableCollection<Comment> npcChattyComments;
		public ReadOnlyObservableCollection<Comment> ChattyComments
		{
			get { return this.npcChattyComments; }
			set
			{
				this.SetProperty(ref this.npcChattyComments, value);
			}
		}
		
		#region Constructor
		public Chatty()
		{
			this.InitializeComponent();
			this.chattyCommentList.AppBarToShow = this.bottomBar;
			this.selectedThreadView.AppBarToShow = this.bottomBar;
		}

		private bool updating = false;
		private object locker = new object();
		//:TODO: Figure out the order here.  We want to prevent selecting stuff when we bump it to the top.
		private void ChattyUpdated(object sender, NotifyCollectionChangedEventArgs e)
		{
			//lock (this.locker)
			//{
			//	if (this.currentlySelectedComment != null && !this.updating)
			//	{
			//		this.updating = true;
			//		//Make sure the comment is still in the chatty before we select it.
			//		var commentToSelect = CoreServices.Instance.Chatty.SingleOrDefault(c => c.Id == this.currentlySelectedComment.Id);
			//		if (commentToSelect != null)
			//		{
			//			var t = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
			//			{
			//				lock (this.locker)
			//				{
			//					this.chattyCommentList.SelectedItem = commentToSelect;
			//					this.updating = false;
			//				}
			//			});
			//		}
			//	}
			//}
		}

		private void ChattyListSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			Windows.UI.Xaml.Visibility vis = Windows.UI.Xaml.Visibility.Collapsed;
			try
			{
				if (e.AddedItems.Count > 0)
				{
					var comment = e.AddedItems[0] as Comment;

					vis = Windows.UI.Xaml.Visibility.Visible;
				}
			}
			catch { }
			finally
			{
				this.threadAppBar.Visibility = vis;
			}
		}

		~Chatty()
		{
			//LatestChattySettings.Instance.PropertyChanged -= SettingChanged;
		}
		#endregion

		#region Load and Save State
		async protected override void OnNavigatedTo(Windows.UI.Xaml.Navigation.NavigationEventArgs e)
		{
 			 base.OnNavigatedTo(e);
			 this.ChattyComments = CoreServices.Instance.Chatty;
		}
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

		async private void RefreshChattyClicked(object sender, RoutedEventArgs e)
		{
			int selectedId = -1;
			if (this.chattyCommentList.SelectedItem != null)
			{
				var comment = this.chattyCommentList.SelectedItem as Comment;
				if (comment != null)
				{
					selectedId = comment.Id;
				}
			}

			await CoreServices.Instance.RefreshChatty();
			var focusedComment = CoreServices.Instance.Chatty.FirstOrDefault(c => c.Id == selectedId);
			if (focusedComment != null)
			{
				var t = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
				 {
					 this.chattyCommentList.SelectedItem = focusedComment;
					 this.chattyCommentList.ScrollIntoView(focusedComment);
				 });
			}
			//this.chattyCommentList.ScrollIntoView(this.chattyCommentList.Items[0]);
			//this.chattyComments.Clear();
		}
		
		private void NewRootPostClicked(object sender, RoutedEventArgs e)
		{
			this.Frame.Navigate(typeof(ReplyToCommentView));
		}
		#endregion

		#region Private Helpers

		//private void SetSplitHeight()
		//{
		//	 this.threadCommentList.Height = Window.Current.CoreWindow.Bounds.Height * (LatestChattySettings.Instance.SplitPercent / 100.0);
		//}

		async private Task ReplyToThread()
		{
			//if (this.inlineThreadView.Visibility != Windows.UI.Xaml.Visibility.Visible)
			//{
			//	return;
			//}

			if (!CoreServices.Instance.LoggedIn)
			{
				var dialog = new MessageDialog("You must login before you can post.  Login information can be set in the application settings.");
				await dialog.ShowAsync();
				return;
			}

			var comment = this.selectedThreadView.SelectedComment as Comment;
			if (comment != null && this.SelectedComment != null)
			{
				this.Frame.Navigate(typeof(ReplyToCommentView), new ReplyNavParameter(comment, this.SelectedComment));
			}
		}

		#endregion
	}
}
