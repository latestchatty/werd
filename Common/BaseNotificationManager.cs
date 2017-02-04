using Latest_Chatty_8.Common;
using Latest_Chatty_8.Networking;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Common
{
	public abstract class BaseNotificationManager : INotificationManager
	{

		protected readonly AuthenticationManager authManager;

		public BaseNotificationManager(AuthenticationManager authManager)
		{
			this.authManager = authManager;
			this.authManager.PropertyChanged += AuthManager_PropertyChanged;
		}

		private void AuthManager_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (!e.PropertyName.ToLower().Equals("loggedin")) return;
			var am = sender as AuthenticationManager;
			if(am != null)
			{
				if(!am.LoggedIn)
				{
					this.SetBadgeCount(0);
				}
			}
		}

		public abstract Task<NotificationUser> GetUser();
		public abstract Task RegisterForNotifications();
		public abstract void RemoveNotificationForCommentId(int postId);
		public abstract Task ReRegisterForNotifications();
		public abstract Task UnRegisterNotifications();
		public async Task UpdateBadgeCount()
		{
			if (!this.authManager.LoggedIn) return;

			using (var messageCountResponse = await POSTHelper.Send(Locations.GetMessageCount, new List<KeyValuePair<string, string>>(), true, authManager))
			{
				if (messageCountResponse.StatusCode == HttpStatusCode.OK)
				{
					var data = await messageCountResponse.Content.ReadAsStringAsync();
					var jsonMessageCount = JToken.Parse(data);

					if (jsonMessageCount["unread"] != null)
					{
						this.SetBadgeCount(jsonMessageCount["unread"].Value<int>());
					}
				}
			}
		}
		public void ClearBadgeCount()
		{
			this.SetBadgeCount(0);
		}

		private void SetBadgeCount(int count)
		{
			var badgeXml = BadgeUpdateManager.GetTemplateContent(BadgeTemplateType.BadgeNumber);
			var badgeElement = (XmlElement)badgeXml.SelectSingleNode("/badge");
			badgeElement.SetAttribute("value", count.ToString());
			var notification = new BadgeNotification(badgeXml);
			BadgeUpdateManager.CreateBadgeUpdaterForApplication().Update(notification);
		}
	}
}
