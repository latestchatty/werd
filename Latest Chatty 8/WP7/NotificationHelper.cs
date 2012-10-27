//TODO: Implement Notifications
//using System;
//using System.Net;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Documents;
//using System.Windows.Ink;
//using System.Windows.Input;
//using System.Windows.Media;
//using System.Windows.Media.Animation;
//using System.Windows.Shapes;
//using Microsoft.Phone.Notification;
//using System.Windows.Threading;
//using System.Text;
//using LatestChatty.Settings;

//namespace LatestChatty.Classes
//{
//	public static class NotificationHelper
//	{
//		private const string ChannelName = "LatestChattyNotificaiton";
//		private const string ServiceHostName = "shacknotify.bit-shift.com";
//		//private const string ServiceHostName = "boarder2.dyndns.org";
//		//Prod
//		private const int ServicePort = 12243;
//		//Dev
//		//private const int ServicePort = 12244;

//		private static HttpNotificationChannel channel;

//		#region Register
//		public static void UnRegisterNotifications()
//		{
//			try
//			{
//				var uriBuilder = new UriBuilder(
//					"http",
//					ServiceHostName,
//					ServicePort,
//					"users/" + LatestChattySettings.Instance.Username.ToLowerInvariant(),
//					"?deviceId=" + LatestChattySettings.Instance.NotificationID);

//				var req = WebRequest.CreateHttp(uriBuilder.Uri);
//				req.Method = "DELETE";
//				req.BeginGetResponse(ar =>
//				{
//					try
//					{
//						var r = (HttpWebRequest)ar.AsyncState;
//						var res = (HttpWebResponse)r.EndGetResponse(ar);
//					} catch { }
//				}, req);

//				channel = HttpNotificationChannel.Find(ChannelName);
//				//TODO: Should wait for the response here, otherwise we could run into a race condition where you unsubscribe then subscribe immediately but unsubscribe happens afterwards.
//				if (channel != null)
//				{
//					channel.UnbindToShellTile();
//					channel.UnbindToShellToast();
//					channel.Close();
//					channel = null;
//				}
//			}
//			catch { }
//		}

//		/// <summary>
//		/// Unbinds, closes, and rebinds notification channel.
//		/// </summary>
//		public static void ReRegisterForNotifications()
//		{
//			UnRegisterNotifications();
//			RegisterForNotifications();
//		}

//		/// <summary>
//		/// Registers for notifications if not already registered.
//		/// </summary>
//		public static void RegisterForNotifications()
//		{
//			if (LatestChattySettings.Instance.NotificationType == NotificationType.None) return;

//			channel = HttpNotificationChannel.Find(ChannelName);

//			if (channel == null)
//			{
//				channel = new HttpNotificationChannel(ChannelName);
//				SubscribeToEvents();
//				channel.Open();
//				BindNotifications();
//			}
//			else
//			{
//				NotificationLog("Re-bound notifications to Uri: {0} \n Connection Status: {1}", channel.ChannelUri, channel.ConnectionStatus);
//				SubscribeToEvents();
//				NotifyServerOfUriChange();
//			}
//		}
//		#endregion

//		#region Helper Methods
//		private static void SubscribeToEvents()
//		{
//			channel.ConnectionStatusChanged += ConnectionStatusChanged;
//			channel.ChannelUriUpdated += ChannelUriUpdated;
//			channel.ErrorOccurred += ChannelErrorOccurred;
//		}

//		static void BindNotifications()
//		{
//			if (!channel.IsShellToastBound && LatestChattySettings.Instance.NotificationType == NotificationType.TileAndToast)
//			{
//				channel.BindToShellToast();
//			}
//			if (!channel.IsShellTileBound && (LatestChattySettings.Instance.NotificationType == NotificationType.Tile || LatestChattySettings.Instance.NotificationType == NotificationType.TileAndToast))
//			{
//				channel.BindToShellTile();
//			}
//		}

//		private static void NotificationLog(string formatMessage, params object[] args)
//		{
//			System.Diagnostics.Debug.WriteLine("NOTIFICATION - " + formatMessage, args);
//		}

//		private static void NotifyServerOfUriChange()
//		{
//			var client = new WebClient();

//			//1 if Tile only, 2 if Tile and Toast
//			var notificationType = LatestChattySettings.Instance.NotificationType == NotificationType.Tile ? 1 : 2;

//			client.Encoding = System.Text.Encoding.UTF8;
//			var uriBuilder = new UriBuilder(
//				"http",
//				ServiceHostName,
//				ServicePort,
//				"users/" + LatestChattySettings.Instance.Username.ToLowerInvariant(),
//				"?deviceId=" + LatestChattySettings.Instance.NotificationID +
//					"&notificationType=" + notificationType);

//			client.UploadStringAsync(uriBuilder.Uri, channel.ChannelUri.ToString());
//		}
//		#endregion

//		#region Events
//		static void ConnectionStatusChanged(object sender, NotificationChannelConnectionEventArgs e)
//		{
//			NotificationLog("Connection status changed: {0}", e.ConnectionStatus.ToString());
//		}

//		static void HttpNotificationReceived(object sender, HttpNotificationEventArgs e)
//		{
//			NotificationLog("Got http notification: {0} {1}", e.Notification.Headers.ToString(), e.Notification.Body);
//		}

//		private static void ChannelUriUpdated(object sender, NotificationChannelUriEventArgs e)
//		{
//			NotificationLog("Channel URI Updated: {0}", e.ChannelUri);
//			NotifyServerOfUriChange();
//		}

//		private static void ChannelErrorOccurred(object sender, NotificationChannelErrorEventArgs e)
//		{
//			NotificationLog("!!! Error setting up notification channel {0} - {1}", e.Message, e.ErrorAdditionalData);
//		}
//		#endregion
//	}
//}
