using System;
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

	//TODO: Notifications should send Device ID and such in the body.
	public static class NotificationHelper
	{
		private static PushNotificationChannel channel;

		public static event EventHandler<PushNotificationReceivedEventArgs> NotificationRecieved;

		#region Register
		async public static Task UnRegisterNotifications()
		{
			try
			{
				var result = await (new HttpClient()).DeleteAsync(Locations.NotificationDelete);
			}
			catch { }
		}

		//<summary>
		//Unbinds, closes, and rebinds notification channel.
		//</summary>
		async public static Task ReRegisterForNotifications()
		{
			if (CoreServices.Instance.LoggedIn)
			{
				await UnRegisterNotifications();
				await RegisterForNotifications();
			}
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
			var result = await (new HttpClient()).PostAsync(Locations.NotificationSubscription, new StringContent(channel.Uri.ToString()));
		}
		#endregion
	}
}
