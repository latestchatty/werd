using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Newtonsoft.Json.Linq;

namespace Common
{
	public abstract class BaseNotificationManager : INotificationManager
	{

		protected readonly AuthenticationManager AuthManager;

		public BaseNotificationManager(AuthenticationManager authManager)
		{
			AuthManager = authManager;
			AuthManager.PropertyChanged += AuthManager_PropertyChanged;
		}

		private void AuthManager_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (!e.PropertyName.ToLower().Equals("loggedin")) return;
			var am = sender as AuthenticationManager;
			if(am != null)
			{
				if(!am.LoggedIn)
				{
					SetBadgeCount(0);
				}
			}
		}

		public abstract Task SyncSettingsWithServer();
		public abstract Task RegisterForNotifications();
		public abstract void RemoveNotificationForCommentId(int postId);
		public abstract Task ReRegisterForNotifications();
		public abstract Task UnRegisterNotifications();
		public async Task UpdateBadgeCount()
		{
			if (!AuthManager.LoggedIn) return;

			using (var messageCountResponse = await PostHelper.Send(Locations.GetMessageCount, new List<KeyValuePair<string, string>>(), true, AuthManager))
			{
				if (messageCountResponse.StatusCode == HttpStatusCode.OK)
				{
					var data = await messageCountResponse.Content.ReadAsStringAsync();
					var jsonMessageCount = JToken.Parse(data);

					if (jsonMessageCount["unread"] != null)
					{
						SetBadgeCount(jsonMessageCount["unread"].Value<int>());
					}
				}
			}
		}
		public void ClearBadgeCount()
		{
			SetBadgeCount(0);
		}

		public void SetBadgeCount(int count)
		{
			var badgeXml = BadgeUpdateManager.GetTemplateContent(BadgeTemplateType.BadgeNumber);
			var badgeElement = (XmlElement)badgeXml.SelectSingleNode("/badge");
			badgeElement?.SetAttribute("value", count.ToString());
			var notification = new BadgeNotification(badgeXml);
			BadgeUpdateManager.CreateBadgeUpdaterForApplication().Update(notification);
		}
	}
}
