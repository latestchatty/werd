using Common;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Werd.Common;
using Werd.DataModel;
using Werd.Settings;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using AuthenticationManager = Common.AuthenticationManager;

namespace Werd.Managers
{
	public class MessageManager : BindableBase, IDisposable
	{
		private readonly AppSettings _settings;
		private readonly AuthenticationManager _auth;
		private readonly INotificationManager _notificationManager;
		private Timer _refreshTimer;
		private bool _refreshEnabled;

		public int InitializePriority => 1000;

		public MessageManager(AuthenticationManager authManager, AppSettings settings, INotificationManager notificationManager)
		{
			_auth = authManager;
			_settings = settings;
			_notificationManager = notificationManager;
		}

		private int npcUnreadCount;
		public int UnreadCount
		{
			get => npcUnreadCount;
			set => SetProperty(ref npcUnreadCount, value);
		}

		private int npcTotalCount;
		public int TotalCount
		{
			get => npcTotalCount;
			set => SetProperty(ref npcTotalCount, value);
		}

		public void Start()
		{
			if (_refreshEnabled || _refreshTimer != null) return;
			_refreshEnabled = true;
			_refreshTimer = new Timer(async a => await RefreshMessages().ConfigureAwait(false), null, 0, Timeout.Infinite);
		}

		public void Stop()
		{
			_refreshEnabled = false;
			if (_refreshTimer != null)
			{
				_refreshTimer.Dispose();
				_refreshTimer = null;
			}
		}

		private async Task RefreshMessages()
		{
			try
			{
				if (_auth.LoggedIn)
				{
					using (var messageCountResponse = await PostHelper.Send(Locations.GetMessageCount, new List<KeyValuePair<string, string>>(), true, _auth).ConfigureAwait(false))
					{
						if (messageCountResponse.StatusCode == HttpStatusCode.OK)
						{
							var data = await messageCountResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
							var jsonMessageCount = JToken.Parse(data);

							await CoreApplication.MainView.CoreWindow.Dispatcher.RunOnUiThreadAndWait(CoreDispatcherPriority.Normal, () =>
							{
								UnreadCount = (int)jsonMessageCount["unread"];
								TotalCount = (int)jsonMessageCount["total"];
							}).ConfigureAwait(false);

							await DebugLog.AddMessage($"Message Count {UnreadCount} unread, {TotalCount} total").ConfigureAwait(false);
						}
					}
					_notificationManager.SetBadgeCount(UnreadCount);
				}
			}
			catch (Exception e)
			{
				await DebugLog.AddException("Exception refreshing messages.", e).ConfigureAwait(false);
			}
			finally
			{
				if (_refreshEnabled)
				{
					//Refresh every 90 seconds, or as often as we refresh the chatty if it's longer.
					_refreshTimer.Change(Math.Max(Math.Max(_settings.RefreshRate, 1), 90) * 1000, Timeout.Infinite);
				}
			}
		}

		public async Task<Tuple<List<Message>, int>> GetMessages(int page, string folder)
		{
			var messages = new List<Message>();
			var totalPages = 0;
			using (var response = await PostHelper.Send(Locations.GetMessages, new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("folder", folder), new KeyValuePair<string, string>("page", page.ToString(CultureInfo.InvariantCulture)) }, true, _auth).ConfigureAwait(false))
			{
				if (response.StatusCode == HttpStatusCode.OK)
				{
					var data = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
					var jsonMessages = JToken.Parse(data);
					totalPages = (int)jsonMessages["totalPages"];

					foreach (var jsonMessage in jsonMessages["messages"])
					{
						messages.Add(new Message(
							(int)jsonMessage["id"],
							jsonMessage["from"].ToString(),
							jsonMessage["to"].ToString(),
							jsonMessage["subject"].ToString(),
							DateTime.Parse(jsonMessage["date"].ToString(), null, DateTimeStyles.AssumeUniversal),
							jsonMessage["body"].ToString().Replace("<br>", "\n", StringComparison.Ordinal),
							((int)jsonMessage["unread"]) == 1,
							folder
							));
					}
				}
			}
			return new Tuple<List<Message>, int>(messages, totalPages);
		}

		public async Task MarkMessageRead(Message message)
		{
			Contract.Requires(message != null);
			if (!message.Unread) return;

			using (var response = await PostHelper.Send(Locations.MarkMessageRead, new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("messageId", message.Id.ToString(CultureInfo.InvariantCulture)) }, true, _auth).ConfigureAwait(true))
			{
				if (response.StatusCode == HttpStatusCode.OK)
				{
					var data = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
					var result = JToken.Parse(data);
					if (result["result"].ToString().Equals("success", StringComparison.OrdinalIgnoreCase))
					{
						await RefreshMessages().ConfigureAwait(true);
						message.Unread = false;
					}
				}
			}
		}
		public async Task<bool> SendMessage(string to, string subject, string message)
		{
			//:HACK: Work-around for https://github.com/boarder2/Latest-Chatty-8/issues/66
			var normalizedLineEndingContent = Regex.Replace(message, "\r\n|\n|\r", "\r\n");

			using (var response = await PostHelper.Send(Locations.SendMessage,
				new List<KeyValuePair<string, string>>
				{
					new KeyValuePair<string, string>("to", to),
					new KeyValuePair<string, string>("subject", subject),
					new KeyValuePair<string, string>("body", normalizedLineEndingContent)
					},
				true, _auth).ConfigureAwait(false))
			{
				if (response.StatusCode == HttpStatusCode.OK)
				{
					var data = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
					var result = JToken.Parse(data);
					if (result["result"].ToString().Equals("success", StringComparison.OrdinalIgnoreCase))
					{
						return true;
					}
				}
			}
			return false;
		}

		public async Task<bool> DeleteMessage(Message message, string folder)
		{
			Contract.Requires(message != null);
			var result = false;
			try
			{
				using (var response = await PostHelper.Send(Locations.DeleteMessage, new List<KeyValuePair<string, string>>
				{
					new KeyValuePair<string, string>("messageId", message.Id.ToString(CultureInfo.InvariantCulture)),
					new KeyValuePair<string, string>("folder", folder)
					}, true, _auth).ConfigureAwait(false))
				{
					if (response.StatusCode == HttpStatusCode.OK)
					{
						var data = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
						var r = JToken.Parse(data);
						if (r["result"].ToString().Equals("success", StringComparison.OrdinalIgnoreCase))
						{
							result = true;
						}
					}
				}
			}
			catch
			{
				// ignored
			} //eeeeeh
			return result;
		}
		#region IDisposable Support
		private bool _disposedValue; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
					if (_refreshTimer != null)
					{
						_refreshTimer.Dispose();
						_refreshTimer = null;
					}
				}

				_disposedValue = true;
			}
		}

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}
}
