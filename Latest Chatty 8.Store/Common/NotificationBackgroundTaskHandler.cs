using Latest_Chatty_8.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.UI.Notifications;

namespace Tasks
{
	public sealed class NotificationBackgroundTaskHandler : IBackgroundTask
	{
		BackgroundTaskDeferral deferral;
		public async void Run(IBackgroundTaskInstance taskInstance)
		{
			try
			{
				deferral = taskInstance.GetDeferral();
				var details = taskInstance.TriggerDetails as ToastNotificationActionTriggerDetail;

				var client = new HttpClient();
				var data = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>()
				{
					new KeyValuePair<string, string>("userName", "boarder2")
				});
				var response = await client.PostAsync(Locations.NotificationResetCount, data);

				await Task.Delay(10);
			}
			finally
			{
				deferral.Complete();
			}
		}
	}
}
