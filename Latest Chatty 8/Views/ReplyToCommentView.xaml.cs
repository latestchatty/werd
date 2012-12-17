using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Networking;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace Latest_Chatty_8.Views
{
	/// <summary>
	/// A basic page that provides characteristics common to most applications.
	/// </summary>
	public sealed partial class ReplyToCommentView : Latest_Chatty_8.Common.LayoutAwarePage
	{
		#region Private Variables
		private Comment replyToComment;
		private bool ctrlPressed = false;
		
		#endregion

		#region Constructor
		public ReplyToCommentView()
		{
			this.InitializeComponent();
		} 
		#endregion

		#region Events
		private void WindowSizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
		{
			this.LayoutUI();
		}

		async private void SendButtonClicked(object sender, RoutedEventArgs e)
		{
			await this.SendReply();
		}

		async private void AttachClicked(object sender, RoutedEventArgs e)
		{
			if (Windows.UI.ViewManagement.ApplicationView.Value == Windows.UI.ViewManagement.ApplicationViewState.Snapped)
			{
				var dialog = new MessageDialog("Can't attach photos in snapped view.");
				return;
			}

			this.progress.IsIndeterminate = true;
			this.progress.Visibility = Windows.UI.Xaml.Visibility.Visible;
			this.postButton.IsEnabled = false;
			this.attachButton.IsEnabled = false;

			try
			{
				var photoUrl = await ChattyPics.UploadPhoto();
				await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
				{
					this.replyText.Text += photoUrl;
				});
			}
			finally
			{
				this.progress.IsIndeterminate = false;
				this.progress.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
				this.postButton.IsEnabled = true;
				this.attachButton.IsEnabled = true;
			}
		}
		#endregion

		#region Overrides
		async protected override Task<bool> CorePageKeyActivated(CoreDispatcher sender, AcceleratorKeyEventArgs args)
		{
			await base.CorePageKeyActivated(sender, args);
			//If it's not a key down event, we don't care about it.
			if (args.EventType != CoreAcceleratorKeyEventType.SystemKeyDown &&
				 args.EventType != CoreAcceleratorKeyEventType.KeyDown)
			{
				return true;
			}

			var ctrlDown = (Window.Current.CoreWindow.GetKeyState(VirtualKey.Control) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
			switch (args.VirtualKey)
			{
				case Windows.System.VirtualKey.Enter:
					if (ctrlPressed)
					{
						this.SendReply();
					}
					break;
				default:
					break;
			}
			return true;
		} 
		#endregion

		#region Load and Save State
		protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
		{
			Window.Current.SizeChanged += WindowSizeChanged;
			this.LayoutUI();
			this.replyToComment = navigationParameter as Comment;
			if (replyToComment != null)
			{
				this.DefaultViewModel["ReplyToComment"] = this.replyToComment;
			}
			else
			{
				//Making a root post.  We don't need this.
				this.commentBrowser.Visibility = Visibility.Collapsed;
				this.replyGrid.RowDefinitions.RemoveAt(0);
			}
		}

		protected override void SaveState(Dictionary<String, Object> pageState)
		{
			Window.Current.SizeChanged -= WindowSizeChanged;
		} 
		#endregion

		#region Private Helpers
		async private Task SendReply()
		{
			var button = postButton;

			if (this.replyText.Text.Length <= 5)
			{
				var dlg = new Windows.UI.Popups.MessageDialog("Post something longer.");
				await dlg.ShowAsync();
				return;
			}

			this.backButton.Focus(Windows.UI.Xaml.FocusState.Programmatic);
			button.IsEnabled = false;

			this.progress.IsIndeterminate = true;
			this.progress.Visibility = Windows.UI.Xaml.Visibility.Visible;

			var content = this.replyText.Text;

			var encodedBody = Uri.EscapeUriString(content);
			content = "body=" + encodedBody;
			//If we're not replying to a comment, we're root chatty posting.
			if (this.replyToComment != null)
			{
				content += "&parent_id=" + this.replyToComment.Id;
			}

			await POSTHelper.Send(Locations.PostUrl, content, true);

			this.progress.IsIndeterminate = false;
			this.progress.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
			button.IsEnabled = true;

			CoreServices.Instance.PostedAComment = true;
			this.Frame.GoBack();
		}

		private void LayoutUI()
		{
			if (Windows.UI.ViewManagement.ApplicationView.Value == Windows.UI.ViewManagement.ApplicationViewState.Snapped)
			{
				if (Window.Current.Bounds.Left == 0) //Snapped Left side.
				{
					this.postButton.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Left;
					return;
				}
			}

			this.postButton.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Right;
		} 
		#endregion
	}
}
