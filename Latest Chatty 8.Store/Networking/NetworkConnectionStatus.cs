using Common;
using Latest_Chatty_8.Common;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Networking.Connectivity;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Latest_Chatty_8.Networking
{
	public class NetworkConnectionStatus : BindableBase
	{
		private bool _isConnected;
		public bool IsConnected
		{
			get => _isConnected;
			set => SetProperty(ref _isConnected, value);
		}

		private bool _isWinChattyConnected;
		public bool IsWinChattyConnected
		{
			get => _isWinChattyConnected;
			set => SetProperty(ref _isWinChattyConnected, value);
		}

		private bool _isNotifyConnected;
		public bool IsNotifyConnected
		{
			get => _isNotifyConnected;
			set => SetProperty(ref _isNotifyConnected, value);
		}

		private string _messageDetails;
		public string MessageDetails
		{
			get => _messageDetails;
			set => SetProperty(ref _messageDetails, value);
		}

		public NetworkConnectionStatus()
		{
			NetworkInformation.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;
		}

		private async void NetworkInformation_NetworkStatusChanged(object sender)
		{
			await CheckNetworkStatus();
		}

		public async Task WaitForNetworkConnection()
		{
			while (!(await CheckNetworkStatus()))
			{
				Debug.WriteLine("Attempting network status detection.");
				await Task.Delay(5000);
			}
		}

		private async Task<bool> CheckNetworkStatus()
		{
			NetworkInformation.NetworkStatusChanged -= NetworkInformation_NetworkStatusChanged;
			try
			{
				var winchattyConnected = true;
				var notifyConnected = true;
				var profile = NetworkInformation.GetInternetConnectionProfile();
				var messageBuilder = new StringBuilder();
				if (profile == null)
				{
					messageBuilder.AppendLine("• Network connection not available.");
					messageBuilder.AppendLine();
				}
				else
				{
					//We have a network connection, let's make sure the APIs are accessible.
					var latestEventJson = await JsonDownloader.Download(Locations.GetNewestEventId);
					if (latestEventJson == null)
					{
						winchattyConnected = false;
						messageBuilder.AppendLine("• Cannot access winchatty (" + (new Uri(Locations.ServiceHost)).Host + ")");
						messageBuilder.AppendLine();
					}
					var result = await JsonDownloader.Download(Locations.NotificationTest);
					if (result == null)
					{
						notifyConnected = false;
						messageBuilder.AppendLine("• Cannot access notification (" + (new Uri(Locations.NotificationBase)).Host + ")");
						messageBuilder.AppendLine();
					}
				}
				if (messageBuilder.Length > 0)
				{
					messageBuilder.AppendLine("Some functionality may be unavailable until these issues are resolved.");
				}
				await SetStatus(winchattyConnected, notifyConnected, messageBuilder.ToString());
				//We can get by if we can't access notification api.  All we really ned is winchatty.
				return messageBuilder.Length == 0 || winchattyConnected;
			}
			finally
			{
				NetworkInformation.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;
			}
		}

		private async Task SetStatus(bool winChattyConnected, bool notifyConnected, string message)
		{
			CoreDispatcher dispatcher;
			if (Window.Current != null)
			{
				dispatcher = Window.Current.Dispatcher;
			}
			else
			{
				dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;
			}
			await dispatcher.RunOnUiThreadAndWait(CoreDispatcherPriority.Normal, () =>
			{
				IsConnected = message.Length == 0;
				IsWinChattyConnected = winChattyConnected;
				IsNotifyConnected = notifyConnected;
				MessageDetails = message;
			});
		}
	}
}
