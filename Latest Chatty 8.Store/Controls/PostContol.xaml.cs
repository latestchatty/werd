using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Common;
using Latest_Chatty_8.Common;
using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Networking;
using Microsoft.HockeyApp;
using MyToolkit.Input;


// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Latest_Chatty_8.Controls
{
	public sealed partial class PostContol : INotifyPropertyChanged
	{
		public event EventHandler Closed;
		public event EventHandler TextBoxGotFocus;
		public event EventHandler TextBoxLostFocus;
		public event EventHandler<ShellMessageEventArgs> ShellMessage;

		private AuthenticationManager npcAuthManager;
		private AuthenticationManager AuthManager
		{
			get => npcAuthManager;
			set => SetProperty(ref npcAuthManager, value);
		}

		private bool npcCanPost;
		private bool CanPost
		{
			get => npcCanPost;
			set => SetProperty(ref npcCanPost, value);
		}

		private bool npcLongPost;

		private bool LongPost
		{
			get => npcLongPost;
			set => SetProperty(ref npcLongPost, value);
		}

		public PostContol()
		{
			InitializeComponent();
		}

		private async void SubmitPostButtonClicked(object sender, RoutedEventArgs e)
		{
			await SubmitPost();
		}

		private async Task SubmitPost()
		{
			if (!PostButton.IsEnabled) return;
			PostButton.IsEnabled = false;
			try
			{
				var comment = DataContext as Comment;

				Debug.WriteLine("Submit clicked.");

				await EnableDisableReplyArea(false);

				var replyText = ReplyText.Text;
				var success = false;
				var message = string.Empty;

				try
				{
					if (comment == null)
					{
						var resultTuple = await ChattyHelper.PostRootComment(replyText, npcAuthManager);
						success = resultTuple.Item1;
						message = resultTuple.Item2;
					}
					else
					{
						var resultTuple = await comment.ReplyToComment(replyText, npcAuthManager);
						success = resultTuple.Item1;
						message = resultTuple.Item2;
					}

					if (success)
					{
						CloseControl();
					}
				}
				catch (Exception)
				{
					//HOCKEYAPP: Swallowing an exception and I'll never know about it because HA can't track exceptions that aren't thrown.  But in this case, the worst thing that happens is the post doesn't go through so actually crashing the app is a terrible UX.
					//var tc = new Microsoft.ApplicationInsights.TelemetryClient();
					//tc.TrackException(ex, new Dictionary<string, string> { { "ReplyText", ReplyText }, { "replyingToId", comment == null ? "root" : comment.Id.ToString() } });
				}
				if (!success)
				{
					if (ShellMessage != null)
					{
						ShellMessage(this, new ShellMessageEventArgs(message, ShellMessageType.Error));
					}
				}
			}
			finally
			{
				PostButton.IsEnabled = true;
				await EnableDisableReplyArea(true);
			}
		}

		private void CloseControl()
		{
			ReplyText.Text = "";
			Visibility = Visibility.Collapsed;
			if (Closed != null)
			{
				Closed(this, EventArgs.Empty);
			}
		}

		public void SetAuthenticationManager(AuthenticationManager authManager)
		{
			AuthManager = authManager;
		}

		public void SetFocus()
		{
			ReplyText.Focus(FocusState.Programmatic);
		}

		private async void AttachClicked(object sender, RoutedEventArgs e)
		{
			await EnableDisableReplyArea(false);

			try
			{
				var photoUrl = await ChattyPics.UploadPhotoUsingPicker();
				await Dispatcher.RunOnUiThreadAndWait(CoreDispatcherPriority.Low, () =>
				{
					var builder = new StringBuilder();
					var startLocation = ReplyText.SelectionStart;
					if (startLocation < 0)
					{
						builder.Append(photoUrl);
					}
					else
					{
						builder.Append(ReplyText.Text.Substring(0, startLocation));
						builder.Append(photoUrl);
						builder.Append(ReplyText.Text.Substring(startLocation));
					}
					ReplyText.Text = builder.ToString();
				});
				HockeyClient.Current.TrackEvent("AttachedPhoto");
			}
			finally
			{
				await EnableDisableReplyArea(true);
			}
		}

		private async Task EnableDisableReplyArea(bool enable)
		{

			Debug.WriteLine("Showing overlay.");
			await CoreApplication.MainView.CoreWindow.Dispatcher.RunOnUiThreadAndWait(CoreDispatcherPriority.High, () =>
			{
				ReplyOverlay.Visibility = enable ? Visibility.Collapsed : Visibility.Visible;
			});

		}

		private void TagButtonClicked(object sender, RoutedEventArgs e)
		{
			var btn = (Button) sender;
			HockeyClient.Current.TrackEvent($"FormatTagApplied - {btn.Tag}");
			if (ReplyText.SelectionLength > 0)
			{
				var selectionStart = ReplyText.SelectionStart;
				var selectionLength = ReplyText.SelectionLength;
				var tagLength = btn.Tag.ToString().IndexOf(".", StringComparison.Ordinal);
				var specialCharacter = Encoding.UTF8.GetChars(new byte[] { 2 }).First().ToString();
				var text = ReplyText.Text.Replace(Environment.NewLine, specialCharacter);
				var before = text.Substring(0, selectionStart);
				var after = text.Substring(selectionStart + selectionLength);
				ReplyText.Text = (before + btn.Tag.ToString().Replace("...", ReplyText.SelectedText) + after).Replace(specialCharacter, Environment.NewLine);
				ReplyText.SelectionStart = selectionStart + tagLength;
				ReplyText.SelectionLength = selectionLength;
			}
			else
			{
				var startPosition = ReplyText.SelectionStart;
				var tagLength = btn.Tag.ToString().Replace("...", " ").Length / 2;
				ReplyText.Text = ReplyText.Text.Insert(startPosition, btn.Tag.ToString().Replace("...", ""));
				ReplyText.SelectionStart = startPosition + tagLength;
			}
			ColorPickerButton.Flyout?.Hide();
			ReplyText.Focus(FocusState.Programmatic);
		}

		private void PostTextChanged(object sender, TextChangedEventArgs e)
		{
			CanPost = ReplyText.Text.Length > 5;
			LongPost = ((DataContext as Comment) == null) && (ReplyText.Text.Length > 1000 || ReplyText.Text.CountOccurrences(Environment.NewLine) > 10);
		}

		private async void ReplyKeyUp(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key == VirtualKey.Escape)
			{
				if (ReplyText.Text.Length > 0)
				{
					var dialog = new MessageDialog("Are you sure you want to close this post without submitting?");
					dialog.Commands.Add(new UICommand("Ok", a => CloseControl()));
					dialog.Commands.Add(new UICommand("Cancel"));
					dialog.CancelCommandIndex = 1;
					dialog.DefaultCommandIndex = 1;
					await dialog.ShowAsync();
				}
				else
				{
					CloseControl();
				}
			}
		}

		private async void PreviewReplyTextOnKeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (Keyboard.IsControlKeyDown && e.Key == VirtualKey.Enter)
			{
				e.Handled = true;
				await SubmitPost();
			}
		}

		private void ReplyGotFocus(object sender, RoutedEventArgs e)
		{
			if (TextBoxGotFocus != null)
			{
				TextBoxGotFocus(this, EventArgs.Empty);
			}
		}

		private void ReplyLostFocus(object sender, RoutedEventArgs e)
		{
			if (TextBoxLostFocus != null)
			{
				TextBoxLostFocus(this, EventArgs.Empty);
			}
		}
		private void PreviewButtonClicked(object sender, RoutedEventArgs e)
		{
			if (PreviewButton.IsChecked.HasValue && PreviewButton.IsChecked.Value)
			{
				PreviewControl.LoadPostPreview(ReplyText.Text);
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
		///     value is optional and can be provided automatically when invoked from compilers that
		///     support CallerMemberName.</param>
		/// <returns>True if the value was changed, false if the existing value matched the
		/// desired value.</returns>
		private void SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
		{
			if (Equals(storage, value)) return;

			storage = value;
			OnPropertyChanged(propertyName);
		}

		/// <summary>
		/// Notifies listeners that a property value has changed.
		/// </summary>
		/// <param name="propertyName">Name of the property used to notify listeners.  This
		/// value is optional and can be provided automatically when invoked from compilers
		/// that support <see cref="CallerMemberNameAttribute"/>.</param>
		private void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			var eventHandler = PropertyChanged;
			if (eventHandler != null)
			{
				eventHandler(this, new PropertyChangedEventArgs(propertyName));
			}
		}
		#endregion
	}
}
