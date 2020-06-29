using Autofac;
using Common;
using Latest_Chatty_8.Common;
using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Managers;
using Latest_Chatty_8.Networking;
//using MyToolkit.Input;
using Latest_Chatty_8.Settings;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;


// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Latest_Chatty_8.Controls
{
	public sealed partial class PostContol : INotifyPropertyChanged
	{
		public event EventHandler Closed;
		public event EventHandler TextBoxGotFocus;
		public event EventHandler TextBoxLostFocus;
		public event EventHandler<ShellMessageEventArgs> ShellMessage;

		private readonly ChattyManager _chattyManager;

		private AuthenticationManager npcAuthManager;
		private AuthenticationManager AuthManager
		{
			get => npcAuthManager;
			set => SetProperty(ref npcAuthManager, value);
		}

		private LatestChattySettings settings;
		private LatestChattySettings Settings
		{
			get => settings;
			set => SetProperty(ref settings, value);
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

		private bool npcTemplatesProcessing = true;

		private bool TemplatesProcessing
		{
			get => npcTemplatesProcessing;
			set => SetProperty(ref npcTemplatesProcessing, value);
		}

		private bool npcSaveNewTemplateVisible = false;

		private bool SaveNewTemplateVisible
		{
			get => npcSaveNewTemplateVisible;
			set => SetProperty(ref npcSaveNewTemplateVisible, value);
		}

		private readonly ObservableCollection<KeyValuePair<string, string>> Templates = new ObservableCollection<KeyValuePair<string, string>>();

		public PostContol()
		{
			InitializeComponent();
			AuthManager = Global.Container.Resolve<AuthenticationManager>();
			Settings = Global.Settings;
			_chattyManager = Global.Container.Resolve<ChattyManager>();
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

				await Global.DebugLog.AddMessage("Submit clicked.");

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
						_chattyManager.ScheduleImmediateChattyRefresh();
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
			if (Closed != null)
			{
				Closed(this, EventArgs.Empty);
			}
			var comment = this.DataContext as Comment;
			if (comment != null) comment.ShowReply = false;
		}

		public void SetShared(AuthenticationManager authManager, LatestChattySettings settings, ChattyManager chattyManager)
		{

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
				//var photoUrl = await ChattyPics.UploadPhotoUsingPicker();
				var photoUrl = await Imgur.UploadPhotoUsingPicker();
				await AddReplyTextAtSelection(photoUrl);
			}
			finally
			{
				await EnableDisableReplyArea(true);
			}
		}

		private async Task EnableDisableReplyArea(bool enable)
		{

			await Global.DebugLog.AddMessage("Showing overlay.");
			await CoreApplication.MainView.CoreWindow.Dispatcher.RunOnUiThreadAndWait(CoreDispatcherPriority.High, () =>
			{
				ReplyOverlay.Visibility = enable ? Visibility.Collapsed : Visibility.Visible;
			});

		}

		private void TagButtonClicked(object sender, RoutedEventArgs e)
		{
			var btn = (Button)sender;
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
			if (PreviewButton.IsChecked.HasValue && PreviewButton.IsChecked.Value) PreviewControl.LoadPostPreview(ReplyText.Text);
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
			if (Window.Current.CoreWindow.GetKeyState(VirtualKey.Control) == CoreVirtualKeyStates.Down && e.Key == VirtualKey.Enter)
			{
				e.Handled = true;
				await SubmitPost();
			}
		}

		private void ReplyGotFocus(object sender, RoutedEventArgs e)
		{
			TextBoxGotFocus?.Invoke(this, EventArgs.Empty);
		}

		private void ReplyLostFocus(object sender, RoutedEventArgs e)
		{
			TextBoxLostFocus?.Invoke(this, EventArgs.Empty);
		}
		private void PreviewButtonClicked(object sender, RoutedEventArgs e)
		{
			if (PreviewButton.IsChecked.HasValue && PreviewButton.IsChecked.Value)
			{
				PreviewControl.LoadPostPreview(ReplyText.Text);
			}
		}
		private void PinMarkupClicked(object sender, RoutedEventArgs e)
		{
			Settings.PinMarkup = !Settings.PinMarkup;
			ColorPickerButton.Flyout?.Hide();
		}

		private async void TemplateClicked(object sender, RoutedEventArgs e)
		{
			TemplatesProcessing = true;
			try
			{
				var templates = await Settings.GetTemplatePosts();
				PopulateTemplatesFromDictionary(templates);
			}
			catch (Exception ex)
			{
				await Global.DebugLog.AddException(string.Empty, ex);
				ShellMessage?.Invoke(this, new ShellMessageEventArgs("Error retrieving templates.", ShellMessageType.Error));
			}
			finally
			{
				TemplatesProcessing = false;
			}
		}

		private async void SaveCurrentPostClicked(object sender, RoutedEventArgs e)
		{
			TemplatesProcessing = true;
			try
			{
				var templates = await Settings.GetTemplatePosts();
				if (templates == null) templates = new Dictionary<string, string>();
				templates.Add(TemplateName.Text, ReplyText.Text);
				await Settings.SetTemplatePosts(templates);
				PopulateTemplatesFromDictionary(templates);
				TemplateName.Text = "";
				SaveNewTemplateVisible = false;
			}
			catch (Exception ex)
			{
				await Global.DebugLog.AddException(string.Empty, ex);
				ShellMessage?.Invoke(this, new ShellMessageEventArgs("Error occurred saving item.", ShellMessageType.Error));
			}
			finally
			{
				TemplatesProcessing = false;
			}
		}

		private void PopulateTemplatesFromDictionary(Dictionary<string, string> templates)
		{
			Templates.Clear();
			if (templates == null) return;

			foreach (var template in templates)
			{
				Templates.Add(new KeyValuePair<string, string>(template.Key, template.Value));
			}
		}

		private async void RemoveTemplateItemClicked(object sender, RoutedEventArgs e)
		{
			TemplatesProcessing = true;
			try
			{
				var itemToRemove = (KeyValuePair<string, string>)(sender as Button)?.DataContext;
				var templates = await Settings.GetTemplatePosts();
				if (templates == null) return;
				templates.Remove(itemToRemove.Key);
				await Settings.SetTemplatePosts(templates);
				PopulateTemplatesFromDictionary(templates);
			}
			catch (Exception ex)
			{
				await Global.DebugLog.AddException(string.Empty, ex);
				ShellMessage?.Invoke(this, new ShellMessageEventArgs("Error occurred removing item.", ShellMessageType.Error));
			}
			finally
			{
				TemplatesProcessing = false;
			}
		}

		private void SeletedTemplate(object sender, SelectionChangedEventArgs e)
		{
			try
			{
				if (e.AddedItems != null && e.AddedItems.Count > 0)
				{
					var selectedItem = (KeyValuePair<string, string>)e.AddedItems[0];
					ReplyText.Text += selectedItem.Value;
					TemplateItems.SelectedIndex = -1;
				}
			}
			catch { }
		}

		private void SaveCurrentPostCancelled(object sender, RoutedEventArgs e)
		{
			SaveNewTemplateVisible = false;
		}

		private void SaveNewTemplateClicked(object sender, RoutedEventArgs e)
		{
			SaveNewTemplateVisible = true;
			TemplateName.Focus(FocusState.Keyboard);
		}

		private async void ReplyPasted(object sender, TextControlPasteEventArgs e)
		{
			DataPackageView dataPackageView = Clipboard.GetContent();
			if (dataPackageView.Contains(StandardDataFormats.Bitmap))
			{
				try
				{
					await EnableDisableReplyArea(false);
					var bmp = await dataPackageView.GetBitmapAsync();
					using (var iraswct = await bmp.OpenReadAsync())
					{
						var buffer = new Windows.Storage.Streams.Buffer(Convert.ToUInt32(iraswct.Size));
						var iBuffer = await iraswct.ReadAsync(buffer, buffer.Capacity, Windows.Storage.Streams.InputStreamOptions.None);
						var storageFolder = Windows.Storage.ApplicationData.Current.TemporaryFolder;
						var fileName = Guid.NewGuid();
						var file = await storageFolder.CreateFileAsync(fileName.ToString(), Windows.Storage.CreationCollisionOption.ReplaceExisting);
						await Windows.Storage.FileIO.WriteBufferAsync(file, iBuffer);
						var imgUrl = await Imgur.UploadPhoto(file);
						await AddReplyTextAtSelection(imgUrl);
						await file.DeleteAsync();
					}
					e.Handled = true;
				}
				catch (Exception ex)
				{
					await Global.DebugLog.AddException(string.Empty, ex);
					ShellMessage(this, new ShellMessageEventArgs("Error occurred uploading file. Make sure the image format is supported by your PC.", ShellMessageType.Error));
				}
				finally
				{
					await EnableDisableReplyArea(true);
				}
			}
		}

		private async Task AddReplyTextAtSelection(string text)
		{
			await Dispatcher.RunOnUiThreadAndWait(CoreDispatcherPriority.Low, () =>
			{
				var builder = new StringBuilder();
				var startLocation = ReplyText.SelectionStart;
				if (startLocation < 0)
				{
					builder.Append(text);
				}
				else
				{
					builder.Append(ReplyText.Text.Substring(0, startLocation));
					builder.Append(text);
					builder.Append(ReplyText.Text.Substring(startLocation));
				}
				ReplyText.Text = builder.ToString();
				ReplyText.SelectionStart = startLocation + text.Length;
			});
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
