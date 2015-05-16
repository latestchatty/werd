using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Shared.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Latest_Chatty_8.Common;
using Latest_Chatty_8.Shared.Networking;
using Latest_Chatty_8.Shared;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Latest_Chatty_8.Controls
{
	public sealed partial class PostContol : UserControl
	{
		public PostContol()
		{
			this.InitializeComponent();
		}

		private async void SubmitPostButtonClicked(object sender, RoutedEventArgs e)
		{
			var comment = this.DataContext as Comment;

			System.Diagnostics.Debug.WriteLine("Submit clicked.");

			await EnableDisableReplyArea(false);

			if (comment == null)
			{
				await ChattyHelper.PostRootComment(this.replyText.Text);
			}
			else
			{
				await comment.ReplyToComment(this.replyText.Text);
			}

			//if (LatestChattySettings.Instance.AutoPinOnReply)
			//{
			//	//Add the post to pinned in the background.
			//	var res = CoreServices.Instance.PinThread(this.navParam.CommentThread.Id);
			//}

			//var showReplyButton = controlContainer.FindControlsNamed<ToggleButton>("showReply").FirstOrDefault();
			//showReplyButton.IsChecked = false;
			this.replyText.Text = "";
			await EnableDisableReplyArea(true);
			this.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
		}

		private async void AttachClicked(object sender, RoutedEventArgs e)
		{
			await this.EnableDisableReplyArea(false);

			try
			{
				var photoUrl = await ChattyPics.UploadPhotoUsingPicker();
				await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
				{
					this.replyText.Text += photoUrl;
				});
			}
			finally
			{
				this.EnableDisableReplyArea(true);
			}
		}

		private async Task EnableDisableReplyArea(bool enable)
		{

			System.Diagnostics.Debug.WriteLine("Showing overlay.");
			await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
			{
				this.replyOverlay.Visibility = enable ? Visibility.Collapsed : Visibility.Visible;
			});

		}

		private async void TagButtonClicked(object sender, RoutedEventArgs e)
		{
			var btn = sender as Button;
			if(this.replyText.SelectionLength > 0)
			{
				var specialCharacter = System.Text.Encoding.UTF8.GetChars(new byte[] { 2 }).First().ToString();
				var text = this.replyText.Text.Replace(Environment.NewLine, specialCharacter);
				var before = text.Substring(0, this.replyText.SelectionStart);
				var after = text.Substring(this.replyText.SelectionStart + this.replyText.SelectionLength);
				this.replyText.Text = (before + btn.Tag.ToString().Replace("...", this.replyText.SelectedText) + after).Replace(specialCharacter, Environment.NewLine);
			}
			else
			{
				var startPosition = this.replyText.SelectionStart;
				var tagLength = btn.Tag.ToString().Replace("...", " ").Length / 2;
				this.replyText.Text = this.replyText.Text.Insert(startPosition, btn.Tag.ToString().Replace("...", ""));
				this.replyText.SelectionStart = startPosition + tagLength;
			}
			this.replyText.Focus(FocusState.Programmatic);
		}
	}
}
