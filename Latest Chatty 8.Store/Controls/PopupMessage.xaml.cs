using Latest_Chatty_8.Common;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace Latest_Chatty_8.Controls
{
	public sealed partial class PopupMessage : UserControl
	{
		Queue<ShellMessageEventArgs> shellMessages = new Queue<ShellMessageEventArgs>();
		bool messageShown = false;

		public PopupMessage()
		{
			this.InitializeComponent();
		}

		public void ShowMessage(ShellMessageEventArgs message)
		{
			this.shellMessages.Enqueue(message);
			if (!this.messageShown)
			{
				Task.Run(DisplayShellMessage);
			}
		}

		private async Task DisplayShellMessage()
		{
			//Display the message.  Otherwise, it'll be displayed later when the current one is hidden.
			if (!this.messageShown)
			{
				this.messageShown = true;
				while (this.shellMessages.Count > 0)
				{
					var message = this.shellMessages.Dequeue();
					var timeout = 2000;
					switch (message.Type)
					{
						case ShellMessageType.Error:
							timeout = 5000;
							break;
						case ShellMessageType.Message:
							break;
						default:
							break;
					}
					//Increase the length that long messages stay on the screen.  Show for a minimum of 2 seconds no matter the length.
					timeout = Math.Max(2000, (int)(timeout * Math.Max((message.Message.Length / 50f), 1)));
					//TODO: Storyboard fading.
					await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunOnUIThreadAndWait(CoreDispatcherPriority.Normal, () =>
					{
						this.shellMessage.Text = message.Message;
						this.Visibility = Windows.UI.Xaml.Visibility.Visible;
					});

					await Task.Delay(timeout);

					await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunOnUIThreadAndWait(CoreDispatcherPriority.Normal, () =>
					{
						//TODO: Fadeout storyboard
						this.shellMessage.Text = string.Empty;
						this.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
					});
				};
				this.messageShown = false;
			}
		}
	}
}
