using Latest_Chatty_8.Common;
using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Networking;
using Latest_Chatty_8.Settings;
using Microsoft.ApplicationInsights;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;


// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Latest_Chatty_8.Controls
{
	public sealed partial class PostContol : UserControl, INotifyPropertyChanged
	{
		public event EventHandler Closed;
		public event EventHandler TextBoxGotFocus;
		public event EventHandler TextBoxLostFocus;
		public event EventHandler<ShellMessageEventArgs> ShellMessage;

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

		private bool npcLongPost = false;
		private bool LongPost
		{
			get { return this.npcLongPost; }
			set { this.SetProperty(ref this.npcLongPost, value); }
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

			var replyText = this.replyText.Text;
			var success = false;
			var message = string.Empty;

			try
			{
				if (comment == null)
				{
					var resultTuple = await ChattyHelper.PostRootComment(replyText, this.npcAuthManager);
					success = resultTuple.Item1;
					message = resultTuple.Item2;
				}
				else
				{
					var resultTuple = await comment.ReplyToComment(replyText, this.npcAuthManager);
					success = resultTuple.Item1;
					message = resultTuple.Item2;
				}

				if (success)
				{
					this.CloseControl();
				}
			}
			catch (Exception ex)
			{
				var tc = new Microsoft.ApplicationInsights.TelemetryClient();
				tc.TrackException(ex, new Dictionary<string, string> { { "replyText", replyText }, { "replyingToId", comment == null ? "root" : comment.Id.ToString() } });
			}
			if (!success)
			{
				if (this.ShellMessage != null)
				{
					this.ShellMessage(this, new ShellMessageEventArgs(message, ShellMessageType.Error));
				}
			}

			await EnableDisableReplyArea(true);
		}

		private void CloseControl()
		{
			this.replyText.Text = "";
			this.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
			if (this.Closed != null)
			{
				this.Closed(this, EventArgs.Empty);
			}
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
					var builder = new StringBuilder();
					var startLocation = this.replyText.SelectionStart;
					if (startLocation < 0)
					{
						builder.Append(photoUrl);
					}
					else
					{
						builder.Append(this.replyText.Text.Substring(0, startLocation));
						builder.Append(photoUrl);
						builder.Append(this.replyText.Text.Substring(startLocation));
					}
					this.replyText.Text = builder.ToString();
				});
				(new TelemetryClient()).TrackEvent("AttachedPhoto");
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
			(new TelemetryClient()).TrackEvent("FormatTagApplied", new Dictionary<string, string> { { "tag", btn.Tag.ToString() } });
			if (this.replyText.SelectionLength > 0)
			{
				var selectionStart = this.replyText.SelectionStart;
				var selectionLength = this.replyText.SelectionLength;
				var tagLength = btn.Tag.ToString().IndexOf(".");
				var specialCharacter = System.Text.Encoding.UTF8.GetChars(new byte[] { 2 }).First().ToString();
				var text = this.replyText.Text.Replace(Environment.NewLine, specialCharacter);
				var before = text.Substring(0, selectionStart);
				var after = text.Substring(selectionStart + selectionLength);
				this.replyText.Text = (before + btn.Tag.ToString().Replace("...", this.replyText.SelectedText) + after).Replace(specialCharacter, Environment.NewLine);
				this.replyText.SelectionStart = selectionStart + tagLength;
				this.replyText.SelectionLength = selectionLength;
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
			this.LongPost = ((this.DataContext as Comment) == null) && (this.replyText.Text.Length > 1000 || this.replyText.Text.CountOccurrences(Environment.NewLine) > 10);
		}

		async private void ReplyKeyUp(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
		{
			if (e.Key == Windows.System.VirtualKey.Escape)
			{
				if (this.replyText.Text.Length > 0)
				{
					var dialog = new Windows.UI.Popups.MessageDialog("Are you sure you want to close this post without submitting?");
					dialog.Commands.Add(new Windows.UI.Popups.UICommand("Ok", (a) => this.CloseControl()));
					dialog.Commands.Add(new Windows.UI.Popups.UICommand("Cancel"));
					dialog.CancelCommandIndex = 1;
					dialog.DefaultCommandIndex = 1;
					await dialog.ShowAsync();
				}
				else
				{
					this.CloseControl();
				}
			}
		}

		private void ReplyGotFocus(object sender, RoutedEventArgs e)
		{
			if (this.TextBoxGotFocus != null)
			{
				this.TextBoxGotFocus(this, EventArgs.Empty);
			}
		}

		private void ReplyLostFocus(object sender, RoutedEventArgs e)
		{
			if (this.TextBoxLostFocus != null)
			{
				this.TextBoxLostFocus(this, EventArgs.Empty);
			}
		}
		private void PreviewButtonClicked(object sender, RoutedEventArgs e)
		{
			if (this.previewButton.IsChecked.HasValue && this.previewButton.IsChecked.Value)
			{
				this.previewControl.LoadPostPreview(this.replyText.Text);
			}
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
