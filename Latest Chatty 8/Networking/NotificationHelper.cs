//TODO: Implement Notifications
using System;
using System.Windows;
using System.Windows.Input;
using System.Text;
using LatestChatty.Settings;
using Windows.Networking.PushNotifications;
using Latest_Chatty_8.Settings;
using System.Net.Http;
using System.Threading.Tasks;

namespace Latest_Chatty_8.Networking
{
	public enum NotificationType
	{
		None = 0,
		PhoneTile = 1,
		PhoneTileAndToast = 2,
		StoreNotify = 3
	}

	public static class NotificationHelper
	{
		private const string ServiceHostName = "shacknotify.bit-shift.com";
		//private const string ServiceHostName = "10.0.0.235";
		//Prod
		//private const int ServicePort = 12243;
		//Dev
		private const int ServicePort = 12253;

		private static PushNotificationChannel channel;

		public static event EventHandler<PushNotificationReceivedEventArgs> NotificationRecieved;

		#region Register
		async public static Task UnRegisterNotifications()
		{
			try
			{
				var uriBuilder = new UriBuilder(
					"https",
					ServiceHostName,
					ServicePort,
					"users/" + LatestChattySettings.Instance.Username.ToLowerInvariant(),
					"?deviceId=" + LatestChattySettings.Instance.NotificationID);

				var result = await (new HttpClient()).DeleteAsync(uriBuilder.Uri);
			}
			catch { }
		}

		//<summary>
		//Unbinds, closes, and rebinds notification channel.
		//</summary>
		async public static void ReRegisterForNotifications()
		{
			await UnRegisterNotifications();
			await RegisterForNotifications();
		}

		//<summary>
		//Registers for notifications if not already registered.
		//</summary>
		async public static Task RegisterForNotifications()
		{
			if (!LatestChattySettings.Instance.EnableNotifications) return;

			channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();

			channel.PushNotificationReceived += (o, e) =>
			{
				if (NotificationHelper.NotificationRecieved != null)
				{
					NotificationHelper.NotificationRecieved(null, e);
				}
			};
			NotifyServerOfUriChange();
		}
		#endregion

		#region Helper Methods

		async private static void NotifyServerOfUriChange()
		{
			var uriBuilder = new UriBuilder(
				"https",
				ServiceHostName,
				ServicePort,
				"users/" + LatestChattySettings.Instance.Username.ToLowerInvariant(),
				"?deviceId=" + LatestChattySettings.Instance.NotificationID +
					"&notificationType=" + (int)NotificationType.StoreNotify);

			var result = await (new HttpClient()).PostAsync(uriBuilder.Uri, new StringContent(channel.Uri.ToString()));
		}
		#endregion
	}
}
