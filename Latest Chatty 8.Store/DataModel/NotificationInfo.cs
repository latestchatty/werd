using System;
using System.Globalization;
using Windows.UI.Notifications;

namespace Werd.DataModel
{
	internal class NotificationInfo
	{
		public string Type { get; set; }
		public string Title { get; set; }
		public int PostId { get; set; }
		public string Message { get; set; }

		public NotificationInfo(ToastNotification notification)
		{
			var textElements = notification.Content.GetElementsByTagName("text");
			Message = textElements[1].InnerText;
			PostId = int.Parse(notification.Content.GetElementsByTagName("action")[0].Attributes.GetNamedItem("arguments").InnerText.Replace("reply=", "", StringComparison.Ordinal), CultureInfo.InvariantCulture);
			Title = textElements[0].InnerText;
			Type = notification.Group;
		}
	}
}
