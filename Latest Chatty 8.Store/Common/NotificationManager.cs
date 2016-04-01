using System;
using Latest_Chatty_8.Settings;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Networking.PushNotifications;
using Latest_Chatty_8.Networking;
using System.Collections.Generic;
using Windows.UI.Core;
using Windows.UI.Notifications;
using System.Linq;
using Microsoft.ApplicationInsights;

namespace Latest_Chatty_8.Common
{
	public class NotificationManager : INotificationManager
	{
		private PushNotificationChannel channel;
		private LatestChattySettings settings;
		private AuthenticationManager authManager;
		private bool suppressNotifications = true;
		private List<int> outstandingNotificationIds = new List<int>();

		public NotificationManager(LatestChattySettings settings, AuthenticationManager authManager)
		{
			this.settings = settings;
			this.authManager = authManager;
			this.settings.PropertyChanged += Settings_PropertyChanged;
			Windows.UI.Xaml.Window.Current.Activated += Window_Activated;
		}

		#region Register
		async public Task UnRegisterNotifications()
		{
			try
			{
				var client = new HttpClient();
				var data = new FormUrlEncodedContent(new Dictionary<string, string>
				{
					{ "deviceId", this.settings.NotificationID.ToString() }
				});
				var response = await client.PostAsync(Locations.NotificationDeRegister, data);

				//TODO: Test response.

				//I guess there's nothing to do with WNS
			}
			catch (Exception e)
			{
				(new TelemetryClient()).TrackException(e);
				System.Diagnostics.Debugger.Break();
			}
		}

		/// <summary>
		/// Unbinds, closes, and rebinds notification channel.
		/// </summary>
		async public Task ReRegisterForNotifications(bool resetCount = false)
		{
			if (resetCount)
			{
				await ResetCount();
			}
			await UnRegisterNotifications();
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
			catch (Exception e)
			{
				(new TelemetryClient()).TrackException(e);
				System.Diagnostics.Debugger.Break();
			}
		}

		#endregion

		public async Task RemoveNotificationForCommentId(int postId)
		{
			try
			{
				if (!this.authManager.LoggedIn || !this.settings.EnableNotifications) return;

				if (ToastNotificationManager.History.GetHistory().Any(t => t.Group.Equals("ReplyToUser") && t.Tag.Equals(postId.ToString()))
					|| this.outstandingNotificationIds.Contains(postId))
				{
					if (this.outstandingNotificationIds.Contains(postId))
					{
						this.outstandingNotificationIds.Remove(postId);
					}
					//Remove the toast from the notification center and tell service to remove it from everywhere.
					var client = new HttpClient();
					var data = new FormUrlEncodedContent(new Dictionary<string, string>
					{
						{ "username", this.authManager.UserName },
						{ "group", "ReplyToUser" },
						{ "tag", postId.ToString() }
					});
					var response = await client.PostAsync(Locations.NotificationRemoveNotification, data);
				}

			}
			catch (Exception e)
			{
				(new TelemetryClient()).TrackException(e);
				System.Diagnostics.Debugger.Break();
			}
		}

		public async Task Resume()
		{
			await this.RefreshOutstandingNotifications();
		}

		async public Task ResetCount()
		{
			try
			{
				if (!this.authManager.LoggedIn) return;
				var client = new HttpClient();
				var data = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>()
				{
					new KeyValuePair<string, string>("userName", this.authManager.UserName)
				});
				var response = await client.PostAsync(Locations.NotificationResetCount, data);
			}
			catch (Exception e)
			{
				(new TelemetryClient()).TrackException(e);
				System.Diagnostics.Debugger.Break();
			}
		}

		#region Helper Methods
		private async Task RefreshOutstandingNotifications()
		{
			try
			{
				if (!this.authManager.LoggedIn) return;
				using (var client = new HttpClient())
				{
					client.DefaultRequestHeaders.Add("Accept", "application/json");
					var response = await client.GetAsync(Locations.NotificationOpenReplyNotifications + "?username=" + Uri.EscapeUriString(this.authManager.UserName));
					var data = Newtonsoft.Json.Linq.JToken.Parse(await response.Content.ReadAsStringAsync());
					if (data["result"] != null && data["result"]["data"] != null)
					{
						this.outstandingNotificationIds = data["result"]["data"].Select(o => (int)o["postId"]).ToList();
					}

					foreach (var notification in ToastNotificationManager.History.GetHistory().Where(t => t.Group.Equals("ReplyToUser")))
					{
						int postId;
						if (int.TryParse(notification.Tag, out postId))
						{
							if (!this.outstandingNotificationIds.Contains(postId))
							{
								ToastNotificationManager.History.Remove(notification.Tag, notification.Group);
							}
						}
					}
				}
			}
			catch (Exception e)
			{
				(new TelemetryClient()).TrackException(e);
				System.Diagnostics.Debugger.Break();
			}
		}

		private void NotificationLog(string formatMessage, params object[] args)
		{
			System.Diagnostics.Debug.WriteLine("NOTIFICATION - " + formatMessage, args);
		}

		async private Task NotifyServerOfUriChange()
		{
			if (!this.authManager.LoggedIn) return;

			var client = new HttpClient();
			var data = new FormUrlEncodedContent(new Dictionary<string, string>
				{
					{ "deviceId", this.settings.NotificationID.ToString() },
					{ "userName", this.authManager.UserName },
					{ "channelUri", this.channel.Uri.ToString() },
				});
			await client.PostAsync(Locations.NotificationRegister, data);
		}
		#endregion

		#region Events
		private void Channel_PushNotificationReceived(PushNotificationChannel sender, PushNotificationReceivedEventArgs args)
		{
			if (args.NotificationType == PushNotificationType.Toast && args.ToastNotification.Group.Equals("ReplyToUser"))
			{
				int postId = 0;
				if (int.TryParse(args.ToastNotification.Tag, out postId))
				{
					this.outstandingNotificationIds.Add(postId);
				}
			}
			if (args.NotificationType != PushNotificationType.Badge)
			{
				args.Cancel = this.suppressNotifications; //Cancel all notifications if the application is active.
			}
		}

		private void Window_Activated(object sender, WindowActivatedEventArgs e)
		{
			this.suppressNotifications = e.WindowActivationState != CoreWindowActivationState.Deactivated;
			if (this.suppressNotifications)
			{
				System.Diagnostics.Debug.WriteLine("Suppressing notifications.");
			}
			else
			{
				System.Diagnostics.Debug.WriteLine("Allowing notifications.");
			}
		}

		async private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName.Equals(nameof(LatestChattySettings.EnableNotifications)))
			{
				await this.ReRegisterForNotifications(true);
			}
		}
		#endregion
	}
}
