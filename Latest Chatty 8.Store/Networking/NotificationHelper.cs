using Latest_Chatty_8.Settings;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Networking.PushNotifications;

namespace Latest_Chatty_8.Networking
{
	public enum NotificationType
	{
		None = 0,
		PhoneTile = 1,
		PhoneTileAndToast = 2,
		StoreNotify = 3
	}

	//:FUTURE: Re-enable notifications

	public static class NotificationHelper
	{
	//	private static PushNotificationChannel channel;

	//	#region Register
	//	async public static Task UnRegisterNotifications()
	//	{
	//		try
	//		{
	//			//var result = await (new HttpClient()).DeleteAsync(Locations.NotificationDelete);
	//		}
	//		catch
	//		{ System.Diagnostics.Debug.Assert(false); }
	//	}

	//	//<summary>
	//	//Unbinds, closes, and rebinds notification channel.
	//	//</summary>
	//	async public static Task ReRegisterForNotifications()
	//	{
	//		if (CoreServices.Instance.LoggedIn)
	//		{
	//			await UnRegisterNotifications();
	//			await RegisterForNotifications();
	//		}
	//	}

	//	//<summary>
	//	//Registers for notifications if not already registered.
	//	//</summary>
	//	async public static Task RegisterForNotifications()
	//	{
	//		if (!LatestChattySettings.Instance.EnableNotifications) return;

	//		try
	//		{
	//			//channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();

	//			//NotifyServerOfUriChange();
	//		}
	//		catch
	//		{ System.Diagnostics.Debug.Assert(false); }
	//	}
	//	#endregion

	//	#region Helper Methods

	//	async private static void NotifyServerOfUriChange()
	//	{
	//		var result = await (new HttpClient()).PostAsync(Locations.NotificationSubscription, new StringContent(channel.Uri.ToString()));
	//	}
	//	#endregion
	}
}
