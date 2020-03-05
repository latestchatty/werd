using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Latest_Chatty_8.Common;
using Windows.UI.Xaml.Media;
using System.ComponentModel;
using Common;
using System.Runtime.CompilerServices;

namespace Latest_Chatty_8.Controls
{
	public sealed partial class PopupMessage : INotifyPropertyChanged
	{
		#region NPC
		public event PropertyChangedEventHandler PropertyChanged;

		private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
		{
			if (Equals(storage, value)) return false;

			storage = value;
			OnPropertyChanged(propertyName);
			return true;
		}
		private void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		} 
		#endregion

		readonly Queue<ShellMessageEventArgs> _shellMessages = new Queue<ShellMessageEventArgs>();
		bool _messageShown;

		Brush _backColor;

		private Brush BackColor {
			get { return _backColor; }
			set { SetProperty(ref _backColor, value); }
		}

		public PopupMessage()
		{
			InitializeComponent();
		}

		public void ShowMessage(ShellMessageEventArgs message)
		{
			_shellMessages.Enqueue(message);
			if (!_messageShown)
			{
				Task.Run(DisplayShellMessage);
			}
		}

		private async Task DisplayShellMessage()
		{
			//Display the message.  Otherwise, it'll be displayed later when the current one is hidden.
			if (!_messageShown)
			{
				_messageShown = true;
				while (_shellMessages.Count > 0)
				{
					var message = _shellMessages.Dequeue();
					var timeout = 2000;
					switch (message.Type)
					{
						case ShellMessageType.Error:
							timeout = 5000;
							break;
						case ShellMessageType.Message:
							break;
					}
					//Increase the length that long messages stay on the screen.  Show for a minimum of 2 seconds no matter the length.
					timeout = Math.Max(2000, (int)(timeout * Math.Max((message.Message.Length / 50f), 1)));
					//TODO: Storyboard fading.
					await CoreApplication.MainView.CoreWindow.Dispatcher.RunOnUiThreadAndWait(CoreDispatcherPriority.Normal, () =>
					{
						BackColor = message.Type == ShellMessageType.Message ?
							new SolidColorBrush(Windows.UI.Colors.Black)
							: new SolidColorBrush(Windows.UI.Colors.OrangeRed);
						ShellMessage.Text = message.Message;
						Visibility = Visibility.Visible;
					});

					await Task.Delay(timeout);

					await CoreApplication.MainView.CoreWindow.Dispatcher.RunOnUiThreadAndWait(CoreDispatcherPriority.Normal, () =>
					{
						//TODO: Fadeout storyboard
						ShellMessage.Text = string.Empty;
						Visibility = Visibility.Collapsed;
					});
				}

				_messageShown = false;
			}
		}
	}
}
