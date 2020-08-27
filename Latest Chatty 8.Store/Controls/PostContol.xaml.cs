using Autofac;
using Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Werd.Common;
using Werd.DataModel;
using Werd.Managers;
using Werd.Networking;
//using MyToolkit.Input;
using Werd.Settings;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;


// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Werd.Controls
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
			AuthManager = AppGlobal.Container.Resolve<AuthenticationManager>();
			Settings = AppGlobal.Settings;
			_chattyManager = AppGlobal.Container.Resolve<ChattyManager>();
		}

		private void UserControl_DataContextChanged(FrameworkElement _, DataContextChangedEventArgs _1)
		{
			var comment = (DataContext as Comment);
			if (comment is null)
			{
				ReplyText.Text = string.Empty;
			}
			else
			{
				ReplyText.Text = comment.PendingReplyText ?? string.Empty;
			}
		}

		private async void SubmitPostButtonClicked(object sender, RoutedEventArgs e)
		{
			await SubmitPost().ConfigureAwait(true);
		}

		private async Task SubmitPost()
		{
			if (!PostButton.IsEnabled) return;
			PostButton.IsEnabled = false;
			try
			{
				var comment = DataContext as Comment;

				await AppGlobal.DebugLog.AddMessage("Submit clicked.").ConfigureAwait(true);

				await EnableDisableReplyArea(false).ConfigureAwait(true);

				var replyText = ReplyText.Text;
				var success = false;
				var message = string.Empty;

				try
				{
					if (comment == null)
					{
						var resultTuple = await ChattyHelper.PostRootComment(replyText, npcAuthManager).ConfigureAwait(true);
						success = resultTuple.Item1;
						message = resultTuple.Item2;
					}
					else
					{
						var resultTuple = await comment.ReplyToComment(replyText, npcAuthManager).ConfigureAwait(true);
						success = resultTuple.Item1;
						message = resultTuple.Item2;
					}

					if (success)
					{
						if (comment != null) comment.PendingReplyText = string.Empty;
						CloseControl();
					}
				}
				catch (Exception ex)
				{
					await AppGlobal.DebugLog.AddException("Error submitting post.", ex).ConfigureAwait(true);
				}
				if (!success)
				{
					ShellMessage?.Invoke(this, new ShellMessageEventArgs(message, ShellMessageType.Error));
				}
			}
			finally
			{
				PostButton.IsEnabled = true;
				await EnableDisableReplyArea(true).ConfigureAwait(true);
			}
		}

		private void CloseControl()
		{
			ReplyText.Text = "";
			Closed?.Invoke(this, EventArgs.Empty);
			var comment = this.DataContext as Comment;
			if (comment != null) comment.ShowReply = false;
		}

		public void SetFocus()
		{
			ReplyText.Focus(FocusState.Programmatic);
		}

		private async void AttachClicked(object sender, RoutedEventArgs e)
		{
			await EnableDisableReplyArea(false).ConfigureAwait(true);

			try
			{
				//var photoUrl = await ChattyPics.UploadPhotoUsingPicker();
				var photoUrl = await Imgur.UploadPhotoUsingPicker().ConfigureAwait(true);
				await AddReplyTextAtSelection(photoUrl).ConfigureAwait(true);
			}
			finally
			{
				await EnableDisableReplyArea(true).ConfigureAwait(true);
			}
		}

		private async Task EnableDisableReplyArea(bool enable)
		{
			await CoreApplication.MainView.CoreWindow.Dispatcher.RunOnUiThreadAndWait(CoreDispatcherPriority.High, () =>
			{
				ReplyOverlay.Visibility = enable ? Visibility.Collapsed : Visibility.Visible;
			}).ConfigureAwait(false);
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
				var text = ReplyText.Text.Replace(Environment.NewLine, specialCharacter, StringComparison.Ordinal);
				var before = text.Substring(0, selectionStart);
				var after = text.Substring(selectionStart + selectionLength);
				ReplyText.Text = (before + btn.Tag.ToString().Replace("...", ReplyText.SelectedText, StringComparison.Ordinal) + after).Replace(specialCharacter, Environment.NewLine, StringComparison.Ordinal);
				ReplyText.SelectionStart = selectionStart + tagLength;
				ReplyText.SelectionLength = selectionLength;
			}
			else
			{
				var startPosition = ReplyText.SelectionStart;
				var tagLength = btn.Tag.ToString().Replace("...", " ", StringComparison.Ordinal).Length / 2;
				ReplyText.Text = ReplyText.Text.Insert(startPosition, btn.Tag.ToString().Replace("...", "", StringComparison.Ordinal));
				ReplyText.SelectionStart = startPosition + tagLength;
			}
			ColorPickerButton.Flyout?.Hide();
			ReplyText.Focus(FocusState.Programmatic);
		}

		private void PostTextChanged(object sender, TextChangedEventArgs e)
		{
			CanPost = ReplyText.Text.Length > 5;
			var comment = (DataContext as Comment);
			LongPost = (comment == null) && (ReplyText.Text.Length > 1000 || ReplyText.Text.CountOccurrences(Environment.NewLine) > 10);
			if (comment != null) comment.PendingReplyText = ReplyText.Text;
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
				await SubmitPost().ConfigureAwait(true);
			}
		}

		private void ReplyKeyDown(object sender, KeyRoutedEventArgs e)
		{
			// Prevent focus from shifting since this is usually in a listview. We don't want up/down events to bubble up. Just stay within the editor.
			if (e.Key == VirtualKey.Up || e.Key == VirtualKey.Down)
			{
				e.Handled = true;
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
			await SetTemplatesProcessing(true).ConfigureAwait(false);
			try
			{
				var templates = await Settings.GetTemplatePosts().ConfigureAwait(false);
				await PopulateTemplatesFromDictionary(templates).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				await AppGlobal.DebugLog.AddException(string.Empty, ex).ConfigureAwait(false);
				ShellMessage?.Invoke(this, new ShellMessageEventArgs("Error retrieving templates.", ShellMessageType.Error));
			}
			finally
			{
				await SetTemplatesProcessing(false).ConfigureAwait(false);
			}
		}

		private async void SaveCurrentPostClicked(object sender, RoutedEventArgs e)
		{
			await SetTemplatesProcessing(true).ConfigureAwait(false);
			try
			{
				var templates = await Settings.GetTemplatePosts().ConfigureAwait(true);
				if (templates == null) templates = new Dictionary<string, string>();
				templates.Add(TemplateName.Text, ReplyText.Text);
				await Settings.SetTemplatePosts(templates).ConfigureAwait(false);
				await PopulateTemplatesFromDictionary(templates).ConfigureAwait(false);
				await CoreApplication.MainView.CoreWindow.Dispatcher.RunOnUiThreadAndWait(CoreDispatcherPriority.Normal, () =>
				{
					TemplateName.Text = "";
					SaveNewTemplateVisible = false;
				}).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				await AppGlobal.DebugLog.AddException(string.Empty, ex).ConfigureAwait(false);
				ShellMessage?.Invoke(this, new ShellMessageEventArgs("Error occurred saving item.", ShellMessageType.Error));
			}
			finally
			{
				await SetTemplatesProcessing(false).ConfigureAwait(false);
			}
		}

		private async Task PopulateTemplatesFromDictionary(Dictionary<string, string> templates)
		{
			await CoreApplication.MainView.CoreWindow.Dispatcher.RunOnUiThreadAndWait(CoreDispatcherPriority.Normal, () =>
			{
				Templates.Clear();
				if (templates == null) return;

				foreach (var template in templates)
				{
					Templates.Add(new KeyValuePair<string, string>(template.Key, template.Value));
				}
			}).ConfigureAwait(false);
		}

		private async void RemoveTemplateItemClicked(object sender, RoutedEventArgs e)
		{
			await SetTemplatesProcessing(true).ConfigureAwait(false);
			try
			{
				var itemToRemove = (KeyValuePair<string, string>)(sender as Button)?.DataContext;
				var templates = await Settings.GetTemplatePosts().ConfigureAwait(false);
				if (templates == null) return;
				templates.Remove(itemToRemove.Key);
				await Settings.SetTemplatePosts(templates).ConfigureAwait(false);
				await PopulateTemplatesFromDictionary(templates).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				await AppGlobal.DebugLog.AddException(string.Empty, ex).ConfigureAwait(false);
				ShellMessage?.Invoke(this, new ShellMessageEventArgs("Error occurred removing item.", ShellMessageType.Error));
			}
			finally
			{
				await SetTemplatesProcessing(false).ConfigureAwait(false);
			}
		}

		private async Task SetTemplatesProcessing(bool value)
		{
			if (Dispatcher.HasThreadAccess)
			{
				TemplatesProcessing = value;
				return;
			}
			else
			{
				await Dispatcher.RunOnUiThreadAndWait(CoreDispatcherPriority.Normal, async () =>
				{
					await SetTemplatesProcessing(value).ConfigureAwait(false);
				}).ConfigureAwait(false);
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
			e.Handled = await PasteImage().ConfigureAwait(true);
		}

		private void ReplyTextUnloaded(object sender, RoutedEventArgs e)
		{
			ReplyText.ContextFlyout.Opening -= ReplyTextMenuOpening;
			ReplyText.SelectionFlyout.Opening -= ReplyTextMenuOpening;
		}

		private void ReplyTextLoaded(object sender, RoutedEventArgs e)
		{
			ReplyText.ContextFlyout.Opening += ReplyTextMenuOpening;
			ReplyText.SelectionFlyout.Opening += ReplyTextMenuOpening;
		}

		private void ReplyTextMenuOpening(object sender, object e)
		{
			var flyout = (sender as CommandBarFlyout);
			if (!(flyout.Target == ReplyText)) return;

			if (!Clipboard.GetContent().Contains(StandardDataFormats.Bitmap)) return;

			var cmd = new StandardUICommand(StandardUICommandKind.Paste)
			{
				IconSource = new SymbolIconSource() { Symbol = Symbol.Pictures },
				Description = "Paste Image"
			};
			cmd.ExecuteRequested += async (_, __) => await PasteImage().ConfigureAwait(false);
			var button = new AppBarButton()
			{
				Command = cmd
			};

			flyout.PrimaryCommands.Add(button);
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
			}).ConfigureAwait(false);
		}

		private async Task<bool> PasteImage()
		{
			var success = false;
			DataPackageView dataPackageView = Clipboard.GetContent();
			if (dataPackageView.Contains(StandardDataFormats.Bitmap))
			{
				try
				{
					await EnableDisableReplyArea(false).ConfigureAwait(true);
					var bmp = await dataPackageView.GetBitmapAsync();
					using (var iraswct = await bmp.OpenReadAsync())
					{
						var buffer = new Windows.Storage.Streams.Buffer(Convert.ToUInt32(iraswct.Size));
						var iBuffer = await iraswct.ReadAsync(buffer, buffer.Capacity, Windows.Storage.Streams.InputStreamOptions.None);
						var storageFolder = Windows.Storage.ApplicationData.Current.TemporaryFolder;
						var fileName = Guid.NewGuid();
						var file = await storageFolder.CreateFileAsync(fileName.ToString(), Windows.Storage.CreationCollisionOption.ReplaceExisting);
						await Windows.Storage.FileIO.WriteBufferAsync(file, iBuffer);
						var imgUrl = await Imgur.UploadPhoto(file).ConfigureAwait(true);
						await AddReplyTextAtSelection(imgUrl).ConfigureAwait(true);
						await file.DeleteAsync();
					}
					success = true;
				}
				catch (Exception ex)
				{
					await AppGlobal.DebugLog.AddException(string.Empty, ex).ConfigureAwait(false);
					ShellMessage(this, new ShellMessageEventArgs("Error occurred uploading file. Make sure the image format is supported by your PC.", ShellMessageType.Error));
				}
				finally
				{
					await EnableDisableReplyArea(true).ConfigureAwait(false);
				}
			}
			return success;
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
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}




		#endregion

	}
}
