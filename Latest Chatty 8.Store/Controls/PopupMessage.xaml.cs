using Latest_Chatty_8.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Latest_Chatty_8.Controls
{
	public sealed partial class PopupMessage
	{
		readonly Queue<ShellMessageEventArgs> _shellMessages = new Queue<ShellMessageEventArgs>();
		bool _messageShown;

		private Brush BackColor;

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
						Bindings.Update();
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
