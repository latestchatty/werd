using Latest_Chatty_8.Common;
using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Networking;
using Latest_Chatty_8.Settings;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;


// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Latest_Chatty_8.Controls
{
	public sealed partial class PostContol : UserControl, INotifyPropertyChanged
	{
		private AuthenticationManager npcAuthManager;
		private AuthenticationManager AuthManager
		{
			get { return this.npcAuthManager; }
			set { this.SetProperty(ref this.npcAuthManager, value); }
		}

		private bool npcCanPost = false;
		private bool CanPost
		{
			get { return this.npcCanPost; }
			set { this.SetProperty(ref this.npcCanPost, value); }
		}

		public PostContol()
		{
			this.InitializeComponent();
		}

		async private void SubmitPostButtonClicked(object sender, RoutedEventArgs e)
		{
			var comment = this.DataContext as Comment;

			System.Diagnostics.Debug.WriteLine("Submit clicked.");

			await EnableDisableReplyArea(false);

			if (comment == null)
			{
				await ChattyHelper.PostRootComment(this.replyText.Text, this.npcAuthManager);
			}
			else
			{
				await comment.ReplyToComment(this.replyText.Text, this.npcAuthManager);
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

		public void SetAuthenticationManager(AuthenticationManager authManager)
		{
			this.AuthManager = authManager;
		}

		public void SetFocus()
		{
			this.replyText.Focus(FocusState.Programmatic);
		}

		async private void AttachClicked(object sender, RoutedEventArgs e)
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
				await this.EnableDisableReplyArea(true);
			}
		}

		async private Task EnableDisableReplyArea(bool enable)
		{

			System.Diagnostics.Debug.WriteLine("Showing overlay.");
			await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
			{
				this.replyOverlay.Visibility = enable ? Visibility.Collapsed : Visibility.Visible;
			});

		}

		private void TagButtonClicked(object sender, RoutedEventArgs e)
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
			this.colorPickerButton.Flyout.Hide();
			this.replyText.Focus(FocusState.Programmatic);
		}

		private void PostTextChanged(object sender, TextChangedEventArgs e)
		{
			this.CanPost = this.replyText.Text.Length > 5;
		}

		#region NPC
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
		private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] String propertyName = null)
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
		private void OnPropertyChanged([CallerMemberName] string propertyName = null)
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
