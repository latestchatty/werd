using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Werd.Common;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Werd.Controls
{
	public sealed partial class PopupMessage
	{
		readonly Queue<ShellMessageEventArgs> _shellMessages = new Queue<ShellMessageEventArgs>();
		CancellationTokenSource _dismissSource;
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
					try
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
						if (message.Message is null) return;
						//Increase the length that long messages stay on the screen.  Show for a minimum of 2 seconds no matter the length.
						timeout = Math.Max(2000, (int)(timeout * Math.Max((message.Message.Length / 50f), 1)));
						await CoreApplication.MainView.CoreWindow.Dispatcher.RunOnUiThreadAndWait(CoreDispatcherPriority.Normal, () =>
						{
							_dismissSource = new CancellationTokenSource();
							BackColor = message.Type == ShellMessageType.Message ?
								(Brush)Application.Current.Resources["SystemControlAcrylicElementBrush"]
								: (Brush)this.Resources["ErrorBrush"];
							ShellMessage.Text = message.Message;
							this.Bindings.Update();
							Visibility = Visibility.Visible;
							this.Opacity = 1;
							//this.Fade(1).Start();
						}).ConfigureAwait(false);

						try
						{
							await Task.Delay(timeout, _dismissSource.Token).ConfigureAwait(false);
						}
						catch (TaskCanceledException) { }

						await CoreApplication.MainView.CoreWindow.Dispatcher.RunOnUiThreadAndWaitAsync(CoreDispatcherPriority.Normal, async () =>
						{
							this.Opacity = 0;
							//await this.Fade(0).StartAsync().ConfigureAwait(true);
							ShellMessage.Text = string.Empty;
							Visibility = Visibility.Collapsed;
						}).ConfigureAwait(false);
					}
					finally
					{
						_dismissSource?.Dispose();
					}
				}

				_messageShown = false;
			}
		}

		private void CloseMessageClicked(object sender, RoutedEventArgs e)
		{
			_dismissSource?.Cancel();
		}
	}
}
