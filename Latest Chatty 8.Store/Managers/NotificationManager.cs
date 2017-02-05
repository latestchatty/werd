using Common;
using Latest_Chatty_8.Common;
using Latest_Chatty_8.Networking;
using Latest_Chatty_8.Settings;

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking.PushNotifications;
using Windows.UI.Core;
using Windows.UI.Notifications;

namespace Latest_Chatty_8.Managers
{
	public class NotificationManager : BaseNotificationManager, IDisposable
	{
		private PushNotificationChannel channel;
		private LatestChattySettings settings;
		private bool suppressNotifications = true;
		private List<int> outstandingNotificationIds = new List<int>();

		private SemaphoreSlim locker = new SemaphoreSlim(1);

		public int InitializePriority
		{
			get
			{
				return int.MaxValue;
			}
		}

		public NotificationManager(LatestChattySettings settings, AuthenticationManager authManager)
		: base(authManager)
		{
			this.settings = settings;
			this.settings.PropertyChanged += Settings_PropertyChanged;
			Windows.UI.Xaml.Window.Current.Activated += Window_Activated;
		}

		#region Register
		public async override Task UnRegisterNotifications()
		{
			try
			{
				var client = new HttpClient();
				var data = new FormUrlEncodedContent(new Dictionary<string, string>
				{
					{ "deviceId", this.settings.NotificationID.ToString() }
				});
				client.DefaultRequestHeaders.Add("Accept", "application/json");
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
		public async override Task ReRegisterForNotifications()
		{
			await UnRegisterNotifications();
			await RegisterForNotifications();
		}

		/// <summary>
		/// Registers for notifications if not already registered.
		/// </summary>
		public async override Task RegisterForNotifications()
		{
			if (!this.authManager.LoggedIn || !this.settings.EnableNotifications) return;

			try
			{
				await this.locker.WaitAsync();
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
			finally
			{
				this.locker.Release();
			}
		}

		#endregion

		public override void RemoveNotificationForCommentId(int postId)
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
				}

			}
			catch (Exception e)
			{
				//(new TelemetryClient()).TrackException(e);
				//System.Diagnostics.Debugger.Break();
			}
		}

		public async override Task<NotificationUser> GetUser()
		{
			try
			{
				if (!this.authManager.LoggedIn || !this.settings.EnableNotifications) return null;

				var response = await JSONDownloader.Download(Locations.GetNotificationUserUrl(this.authManager.UserName));
				var user = response.ToObject<NotificationUser>();
				return user;
			}
			catch (Exception e)
			{
				//(new TelemetryClient()).TrackException(e);
				//System.Diagnostics.Debugger.Break();
			}
			return null;
		}

		#region Helper Methods

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
			client.DefaultRequestHeaders.Add("Accept", "application/json");
			using (await client.PostAsync(Locations.NotificationRegister, data)) { }

			data = new FormUrlEncodedContent(new Dictionary<string, string>
			{
				{ "userName", this.authManager.UserName },
				{ "notifyOnUserName", this.settings.NotifyOnNameMention ? "1" : "0" }
			});
			using (await client.PostAsync(Locations.NotificationUser, data)) { }
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
			if (e.PropertyName.Equals(nameof(LatestChattySettings.EnableNotifications)) ||
				e.PropertyName.Equals(nameof(LatestChattySettings.NotifyOnNameMention)))
			{
				await this.ReRegisterForNotifications();
			}
		}

		#endregion

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects).
					this.locker.Dispose();
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~NotificationManager() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion
	}
}
