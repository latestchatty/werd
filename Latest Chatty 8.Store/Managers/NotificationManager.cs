using Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Werd.Settings;
using Windows.Networking.PushNotifications;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.Xaml;

namespace Werd.Managers
{
	public class NotificationManager : BaseNotificationManager, IDisposable
	{
		private PushNotificationChannel _channel;
		private readonly LatestChattySettings _settings;
		private bool _suppressNotifications = true;

		private readonly SemaphoreSlim _locker = new SemaphoreSlim(1);
		private bool _preventResync = false;
		private bool _hasUnreadNotifications;


		//private SemaphoreSlim removalLocker = new SemaphoreSlim(1);
		//bool processingRemovalQueue = false;

		public int InitializePriority => int.MaxValue;

		public override bool HasUnreadNotifications
		{
			get => _hasUnreadNotifications;
			set => SetProperty(ref _hasUnreadNotifications, value);
		}

		public NotificationManager(LatestChattySettings settings, AuthenticationManager authManager)
		: base(authManager)
		{
			_settings = settings;
			_settings.PropertyChanged += Settings_PropertyChanged;
			Window.Current.Activated += Window_Activated;
			this.HasUnreadNotifications = ToastNotificationManager.History.GetHistory().Count > 0;
		}

		#region Register
		public async override Task UnRegisterNotifications()
		{
			try
			{
				var client = new HttpClient();
				var data = new FormUrlEncodedContent(new Dictionary<string, string>
				{
					{ "deviceId", _settings.NotificationId.ToString() }
				});
				client.DefaultRequestHeaders.Add("Accept", "application/json");
				using (var _ = await client.PostAsync(Locations.NotificationDeRegister, data)) { }

				//TODO: Test response.

				//I guess there's nothing to do with WNS
			}
			catch (Exception)
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
			if (!AuthManager.LoggedIn || !_settings.EnableNotifications) return;

			try
			{
				await _locker.WaitAsync();
				_channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
				if (_channel != null)
				{
					await NotificationLog($"Re-bound notifications to Uri: {_channel.Uri}");
					_channel.PushNotificationReceived += Channel_PushNotificationReceived;
					await NotifyServerOfUriChange();
				}
			}
			catch (Exception)
			{
				//(new TelemetryClient()).TrackException(e);
				//System.Diagnostics.Debugger.Break();
			}
			finally
			{
				_locker.Release();
			}
		}

		#endregion

		//ConcurrentQueue<int> notificationRemovals = new ConcurrentQueue<int>();

		public override void RemoveNotificationForCommentId(int postId)
		{
			//if (this.notificationRemovals.Contains(postId)) return;
			//this.notificationRemovals.Enqueue(postId);
			//Task.Run(() => ProcessRemovalQueue());
		}

		//private void ProcessRemovalQueue()
		//{
		//	this.removalLocker.Wait();
		//	if (this.processingRemovalQueue)
		//	{
		//		this.removalLocker.Release();
		//		return;
		//	}
		//	this.processingRemovalQueue = true;
		//	this.removalLocker.Release();

		//	try
		//	{
		//		int postId;

		//		while (this.notificationRemovals.TryDequeue(out postId))
		//		{
		//			ToastNotificationManager.History.Remove(postId.ToString(), "ReplyToUser");
		//			System.Diagnostics.Global.DebugLog.AddMessage("Notification Queue Count: " + this.notificationRemovals.Count);
		//		}
		//	}
		//	finally
		//	{
		//		this.removalLocker.Wait();
		//		this.processingRemovalQueue = false;
		//		this.removalLocker.Release();
		//	}
		//}

		public async override Task SyncSettingsWithServer()
		{
			try
			{
				_preventResync = true;
				if (!AuthManager.LoggedIn || !_settings.EnableNotifications) return;

				var response = await JsonDownloader.Download(Locations.GetNotificationUserUrl(AuthManager.UserName));
				var user = response.ToObject<NotificationUser>();
				_settings.NotifyOnNameMention = user.NotifyOnUserName;
				user.NotificationKeywords.Sort();
				_settings.NotificationKeywords = user.NotificationKeywords;
			}
			catch (Exception)
			{
				//(new TelemetryClient()).TrackException(e);
				//System.Diagnostics.Debugger.Break();
			}
			finally
			{
				_preventResync = false;
			}
		}

		#region Helper Methods

		private async Task NotificationLog(string message)
		{
			await AppGlobal.DebugLog.AddMessage($"NOTIFICATION - {message}");
		}

		private async Task NotifyServerOfUriChange()
		{
			if (!AuthManager.LoggedIn) return;

			using (var client = new HttpClient())
			{
				using (var data = new FormUrlEncodedContent(new Dictionary<string, string>
				{
					{ "deviceId", _settings.NotificationId.ToString() },
					{ "userName", AuthManager.UserName },
					{ "channelUri", _channel.Uri }
				}))
				{
					client.DefaultRequestHeaders.Add("Accept", "application/json");
					using (await client.PostAsync(Locations.NotificationRegister, data)) { }
				}

				using (var data = new MultipartFormDataContent())
				{
					data.Add(new StringContent(AuthManager.UserName), "userName");
					data.Add(new StringContent(_settings.NotifyOnNameMention ? "1" : "0"), "notifyOnUserName");
					foreach (var kw in _settings.NotificationKeywords)
					{
						data.Add(new StringContent(kw), "notificationKeywords[]");
					}
					using (await client.PostAsync(Locations.NotificationUser, data)) { }
				}
			}
		}
		#endregion

		#region Events
		private void Channel_PushNotificationReceived(PushNotificationChannel sender, PushNotificationReceivedEventArgs args)
		{
			if (args.NotificationType != PushNotificationType.Badge)
			{
				args.Cancel = _suppressNotifications && !_settings.AllowNotificationsWhileActive;
				this.HasUnreadNotifications = ToastNotificationManager.History.GetHistory().Count > 0;
			}
		}

		private async void Window_Activated(object sender, WindowActivatedEventArgs e)
		{
			_suppressNotifications = e.WindowActivationState != CoreWindowActivationState.Deactivated;
			if (_suppressNotifications)
			{
				await AppGlobal.DebugLog.AddMessage("Suppressing notifications.");
			}
			else
			{
				await AppGlobal.DebugLog.AddMessage("Allowing notifications.");
			}
		}

		private async void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (_preventResync) return;
			if (e.PropertyName.Equals(nameof(LatestChattySettings.EnableNotifications)) ||
				e.PropertyName.Equals(nameof(LatestChattySettings.NotifyOnNameMention)))
			{
				await ReRegisterForNotifications();
			}
		}

		#endregion

		#region IDisposable Support
		private bool _disposedValue; // To detect redundant calls
		
		protected virtual void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects).
					_locker?.Dispose();
					//this.removalLocker?.Dispose();
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				_disposedValue = true;
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
