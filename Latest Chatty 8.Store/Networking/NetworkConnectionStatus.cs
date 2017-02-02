using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Windows.Networking.Connectivity;
using System.Threading.Tasks;
using System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.ApplicationModel.Core;
using Latest_Chatty_8.Common;

namespace Latest_Chatty_8.Networking
{
	public class NetworkConnectionStatus : BindableBase
	{
		private bool isConnected;
		public bool IsConnected
		{
			get { return this.isConnected; }
			set { this.SetProperty(ref this.isConnected, value); }
		}

		private bool isWinChattyConnected;
		public bool IsWinChattyConnected
		{
			get { return this.isWinChattyConnected; }
			set { this.SetProperty(ref this.isWinChattyConnected, value); }
		}

		private bool isNotifyConnected;
		public bool IsNotifyConnected
		{
			get { return this.isNotifyConnected; }
			set { this.SetProperty(ref this.isNotifyConnected, value); }
		}

		private string messageDetails;
		public string MessageDetails
		{
			get { return this.messageDetails; }
			set { this.SetProperty(ref this.messageDetails, value); }
		}

		public NetworkConnectionStatus()
		{
			NetworkInformation.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;
		}

		private async void NetworkInformation_NetworkStatusChanged(object sender)
		{
			await this.CheckNetworkStatus();
		}

		public async Task WaitForNetworkConnection()
		{
			while (!(await this.CheckNetworkStatus()))
			{
				System.Diagnostics.Debug.WriteLine("Attempting network status detection.");
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
					var latestEventJson = await JSONDownloader.Download(Locations.GetNewestEventId);
					if (latestEventJson == null)
					{
						winchattyConnected = false;
						messageBuilder.AppendLine("• Cannot access winchatty (" + (new Uri(Locations.ServiceHost)).Host + ")");
						messageBuilder.AppendLine();
					}
					var result = await JSONDownloader.Download(Locations.NotificationTest);
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
			CoreDispatcher dispatcher = null;
			if (Window.Current != null)
			{
				dispatcher = Window.Current.Dispatcher;
			}
			else
			{
				dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;
			}
			await dispatcher.RunOnUIThreadAndWait(CoreDispatcherPriority.Normal, () =>
			{
				this.IsConnected = message.Length == 0;
				this.IsWinChattyConnected = winChattyConnected;
				this.IsNotifyConnected = notifyConnected;
				this.MessageDetails = message;
			});
		}
	}
}
