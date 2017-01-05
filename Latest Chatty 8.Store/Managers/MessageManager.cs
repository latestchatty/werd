using Latest_Chatty_8.Common;
using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Networking;
using Latest_Chatty_8.Settings;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Core;

namespace Latest_Chatty_8.Managers
{
	public class MessageManager : BindableBase, IDisposable
	{
		private readonly LatestChattySettings settings;
		private readonly AuthenticationManager auth;
		private Timer refreshTimer;
		private bool refreshEnabled;

		public int InitializePriority
		{
			get
			{
				return 1000;
			}
		}

		public MessageManager(AuthenticationManager authManager, LatestChattySettings settings)
		{
			this.auth = authManager;
			this.settings = settings;
		}

		private int npcUnreadCount;
		public int UnreadCount
		{
			get { return this.npcUnreadCount; }
			set { this.SetProperty(ref this.npcUnreadCount, value); }
		}

		private int npcTotalCount;
		public int TotalCount
		{
			get { return this.npcTotalCount; }
			set { this.SetProperty(ref this.npcTotalCount, value); }
		}

		public void Start()
		{
			if (this.refreshEnabled || this.refreshTimer != null) return;
			this.refreshEnabled = true;
			this.refreshTimer = new Timer(async (a) => await RefreshMessages(), null, 0, Timeout.Infinite);
		}

		public void Stop()
		{
			this.refreshEnabled = false;
			if (this.refreshTimer != null)
			{
				this.refreshTimer.Dispose();
				this.refreshTimer = null;
			}
		}

		private async Task RefreshMessages()
		{
			try
			{
				if (this.auth.LoggedIn)
				{
					using (var messageCountResponse = await POSTHelper.Send(Locations.GetMessageCount, new List<KeyValuePair<string, string>>(), true, this.auth))
					{
						if (messageCountResponse.StatusCode == HttpStatusCode.OK)
						{
							var data = await messageCountResponse.Content.ReadAsStringAsync();
							var jsonMessageCount = JToken.Parse(data);

							await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunOnUIThreadAndWait(CoreDispatcherPriority.Normal, () =>
							{
								this.UnreadCount = (int)jsonMessageCount["unread"];
								this.TotalCount = (int)jsonMessageCount["total"];
							});

							System.Diagnostics.Debug.WriteLine("Message Count {0} unread, {1} total", this.UnreadCount, this.TotalCount);
						}
					}
				}
			}
			catch { /*System.Diagnostics.Debugger.Break();*/ /*Generally anything that goes wrong here is going to be due to network connectivity.  So really, we just want to try again later. */ }
			finally
			{
				if (this.refreshEnabled)
				{
					//Refresh every 30 seconds, or as often as we refresh the chatty if it's longer.
					this.refreshTimer.Change(Math.Max(Math.Max(this.settings.RefreshRate, 1), 30) * 1000, Timeout.Infinite);
				}
			}
		}

		public async Task<Tuple<List<Message>, int>> GetMessages(int page, string folder)
		{
			var messages = new List<Message>();
			var totalPages = 0;
			using (var response = await POSTHelper.Send(Locations.GetMessages, new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("folder", folder), new KeyValuePair<string, string>("page", page.ToString()) }, true, this.auth))
			{
				if (response.StatusCode == HttpStatusCode.OK)
				{
					var data = await response.Content.ReadAsStringAsync();
					var jsonMessages = JToken.Parse(data);
					totalPages = (int)jsonMessages["totalPages"];

					foreach (var jsonMessage in jsonMessages["messages"])
					{
						messages.Add(new Message(
							(int)jsonMessage["id"],
							jsonMessage["from"].ToString(),
							jsonMessage["to"].ToString(),
							jsonMessage["subject"].ToString(),
							DateTime.Parse(jsonMessage["date"].ToString(), null, System.Globalization.DateTimeStyles.AssumeUniversal),
							jsonMessage["body"].ToString(),
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
			if (!message.Unread) return;

			using (var response = await POSTHelper.Send(Locations.MarkMessageRead, new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("messageId", message.Id.ToString()) }, true, this.auth))
			{
				if (response.StatusCode == HttpStatusCode.OK)
				{
					var data = await response.Content.ReadAsStringAsync();
					var result = JToken.Parse(data);
					if (result["result"].ToString().ToLowerInvariant().Equals("success"))
					{
						await this.RefreshMessages();
						message.Unread = false;
					}
				}
			}
		}
		public async Task<bool> SendMessage(string to, string subject, string message)
		{
			//:HACK: Work-around for https://github.com/boarder2/Latest-Chatty-8/issues/66
			var normalizedLineEndingContent = Regex.Replace(message, "\r\n|\n|\r", "\r\n");

			using (var response = await POSTHelper.Send(Locations.SendMessage,
				new List<KeyValuePair<string, string>>() {
					new KeyValuePair<string, string>("to", to),
					new KeyValuePair<string, string>("subject", subject),
					new KeyValuePair<string, string>("body", normalizedLineEndingContent)
					},
				true, this.auth))
			{
				if (response.StatusCode == HttpStatusCode.OK)
				{
					var data = await response.Content.ReadAsStringAsync();
					var result = JToken.Parse(data);
					if (result["result"].ToString().ToLowerInvariant().Equals("success"))
					{
						return true;
					}
				}
			}
			return false;
		}

		public async Task<bool> DeleteMessage(Message message, string folder)
		{
			var result = false;
			try
			{
				using (var response = await POSTHelper.Send(Locations.DeleteMessage, new List<KeyValuePair<string, string>>() {
					new KeyValuePair<string, string>("messageId", message.Id.ToString()),
					new KeyValuePair<string, string>("folder", folder)
					}, true, this.auth))
				{
					if (response.StatusCode == HttpStatusCode.OK)
					{
						var data = await response.Content.ReadAsStringAsync();
						var r = JToken.Parse(data);
						if (r["result"].ToString().ToLowerInvariant().Equals("success"))
						{
							result = true;
						}
					}
				}
			}
			catch { } //eeeeeh
			return result;
		}
		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					if (this.refreshTimer != null)
					{
						this.refreshTimer.Dispose();
						this.refreshTimer = null;
					}
				}

				disposedValue = true;
			}
		}

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
		}
		#endregion
	}
}
