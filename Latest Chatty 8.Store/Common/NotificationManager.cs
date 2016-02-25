using System;
using Latest_Chatty_8.Settings;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Networking.PushNotifications;
using Latest_Chatty_8.Networking;
using System.Collections.Generic;

namespace Latest_Chatty_8.Common
{
	public class NotificationManager
	{
		private PushNotificationChannel channel;
		private LatestChattySettings settings;
		private AuthenticationManager authManager;

		public NotificationManager(LatestChattySettings settings, AuthenticationManager authManager)
		{
			this.settings = settings;
			this.authManager = authManager;
		}

		#region Register
		async public Task UnRegisterNotifications()
		{
			try
			{
				var client = new HttpClient();
				var data = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>()
				{
					new KeyValuePair<string, string>("deviceId", this.settings.NotificationID.ToString())
				});
				var response = await client.PostAsync(Locations.NotificationDeRegister, data);

				//TODO: Test response.

				//I guess there's nothing to do with WNS
			}
			catch { }
		}

		/// <summary>
		/// Unbinds, closes, and rebinds notification channel.
		/// </summary>
		async public Task ReRegisterForNotifications()
		{
			await UnRegisterNotifications();
			await ResetCount();
			await RegisterForNotifications();
		}

		/// <summary>
		/// Registers for notifications if not already registered.
		/// </summary>
		async public Task RegisterForNotifications()
		{
			if (!this.authManager.LoggedIn || !this.settings.EnableNotifications) return;

			try
			{
				this.channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
				if (channel != null)
				{
					NotificationLog($"Re-bound notifications to Uri: {channel.Uri.ToString()}");
					this.channel.PushNotificationReceived += Channel_PushNotificationReceived;
					await NotifyServerOfUriChange();
				}
			}
			catch
			{ }
		}

		#endregion

		async public Task ResetCount()
		{
			if (!this.authManager.LoggedIn || !this.settings.EnableNotifications) return;
			var client = new HttpClient();
			var data = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>()
				{
					new KeyValuePair<string, string>("userName", this.authManager.UserName)
				});
			var response = await client.PostAsync(Locations.NotificationResetCount, data);
		}

		#region Helper Methods

		private void NotificationLog(string formatMessage, params object[] args)
		{
			System.Diagnostics.Debug.WriteLine("NOTIFICATION - " + formatMessage, args);
		}

		async private Task NotifyServerOfUriChange()
		{
			if (!this.authManager.LoggedIn) return;

			var client = new HttpClient();
			var data = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>()
				{
					new KeyValuePair<string, string>("deviceId", this.settings.NotificationID.ToString()),
					new KeyValuePair<string, string>("userName", this.authManager.UserName),
					new KeyValuePair<string, string>("channelUri", this.channel.Uri.ToString()),
				});
			await client.PostAsync(Locations.NotificationRegister, data);
		}
		#endregion

		#region Events
		private void Channel_PushNotificationReceived(PushNotificationChannel sender, PushNotificationReceivedEventArgs args)
		{
			NotificationLog("Got notification {0}", args.RawNotification.Content);
		}
		#endregion
	}
}
