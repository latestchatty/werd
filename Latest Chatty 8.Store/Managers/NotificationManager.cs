using Latest_Chatty_8.Common;
using Latest_Chatty_8.Networking;
using Latest_Chatty_8.Settings;

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Networking.PushNotifications;
using Windows.UI.Core;
using Windows.UI.Notifications;

namespace Latest_Chatty_8.Managers
{
	public class NotificationManager : INotificationManager
	{
		private PushNotificationChannel channel;
		private LatestChattySettings settings;
		private AuthenticationManager authManager;
		private bool suppressNotifications = true;
		private List<int> outstandingNotificationIds = new List<int>();
		public int InitializePriority
		{
			get
			{
				return int.MaxValue;
			}
		}

		public NotificationManager(LatestChattySettings settings, AuthenticationManager authManager)
		{
			this.settings = settings;
			this.authManager = authManager;
			this.settings.PropertyChanged += Settings_PropertyChanged;
			Windows.UI.Xaml.Window.Current.Activated += Window_Activated;
		}

		#region Register
		public async Task UnRegisterNotifications()
		{
			try
			{
				var client = new HttpClient();
				var data = new FormUrlEncodedContent(new Dictionary<string, string>
				{
					{ "deviceId", this.settings.NotificationID.ToString() }
				});
				using (var response = await client.PostAsync(Locations.NotificationDeRegister, data)) { }

				//TODO: Test response.

				//I guess there's nothing to do with WNS
			}
			catch (Exception e)
			{
				//(new TelemetryClient()).TrackException(e);
				//System.Diagnostics.Debugger.Break();
			}
		}

		/// <summary>
		/// Unbinds, closes, and rebinds notification channel.
		/// </summary>
		public async Task ReRegisterForNotifications(bool resetCount = false)
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
		public async Task RegisterForNotifications()
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
				//(new TelemetryClient()).TrackException(e);
				//System.Diagnostics.Debugger.Break();
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
					using (var response = await client.PostAsync(Locations.NotificationRemoveNotification, data)) { }
				}

			}
			catch (Exception e)
			{
				//(new TelemetryClient()).TrackException(e);
				//System.Diagnostics.Debugger.Break();
			}
		}

		public async Task Resume()
		{
			await this.RefreshOutstandingNotifications();
		}

		public async Task ResetCount()
		{
			try
			{
				if (!this.authManager.LoggedIn) return;
				var client = new HttpClient();
				var data = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>()
				{
					new KeyValuePair<string, string>("userName", this.authManager.UserName)
				});
				using (var response = await client.PostAsync(Locations.NotificationResetCount, data)) { }
			}
			catch (Exception e)
			{
				//(new TelemetryClient()).TrackException(e);
				//System.Diagnostics.Debugger.Break();
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
					JToken data;
					using (var response = await client.GetAsync(Locations.NotificationOpenReplyNotifications + "?username=" + Uri.EscapeUriString(this.authManager.UserName)))
					{
						data = JToken.Parse(await response.Content.ReadAsStringAsync());
					}

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
				//(new TelemetryClient()).TrackException(e);
				//System.Diagnostics.Debugger.Break();
			}
		}

		private void NotificationLog(string formatMessage, params object[] args)
		{
			System.Diagnostics.Debug.WriteLine("NOTIFICATION - " + formatMessage, args);
		}

		private async Task NotifyServerOfUriChange()
		{
			if (!this.authManager.LoggedIn) return;

			var client = new HttpClient();
			var data = new FormUrlEncodedContent(new Dictionary<string, string>
				{
					{ "deviceId", this.settings.NotificationID.ToString() },
					{ "userName", this.authManager.UserName },
					{ "channelUri", this.channel.Uri.ToString() },
				});
			using (await client.PostAsync(Locations.NotificationRegister, data)) { }
		}
		#endregion

		#region Events
		private void Channel_PushNotificationReceived(PushNotificationChannel sender, PushNotificationReceivedEventArgs args)
		{
			var suppress = false;

			int postId = -1;
			//TODO: Allow notifications for expired threads even if the application is in the foreground since there's no other way for the user to know that they got a reply to an expired thread.
			if (args.NotificationType == PushNotificationType.Toast && args.ToastNotification.Group.Equals("ReplyToUser"))
			{
				if (int.TryParse(args.ToastNotification.Tag, out postId))
				{
					this.outstandingNotificationIds.Add(postId);
				}
			}
			if (args.NotificationType != PushNotificationType.Badge)
			{
				suppress = this.suppressNotifications; //Cancel all notifications if the application is active.

				if (postId > 0 && suppress)
				{
					var jThread = JSONDownloader.Download($"{Locations.GetThread}?id={postId}").Result;

					DateTime minDate = DateTime.MaxValue;
					if (jThread != null && jThread["threads"] != null)
					{
						foreach (var post in jThread["threads"][0]["posts"])
						{
							var date = DateTime.Parse(post["date"].ToString(), null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal);
							if (date < minDate)
							{
								minDate = date;
							}
						}
						if (minDate.AddHours(18).Subtract(DateTime.UtcNow).TotalSeconds < 0)
						{
							suppress = false; //Still want to show the notification if the thread is expired.
						}
					}
				}
				args.Cancel = suppress;
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

		private async void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName.Equals(nameof(LatestChattySettings.EnableNotifications)))
			{
				await this.ReRegisterForNotifications(true);
			}
		}
		#endregion
	}
}
