using Latest_Chatty_8.Common;
using Latest_Chatty_8.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Security.Credentials;
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
				var authManager = new AuthenticationManager();
				await authManager.Initialize();

				if (!authManager.LoggedIn) return;

				var replyToId = details.Argument.Substring(details.Argument.IndexOf("reply=") + 6);
				
				var data = new List<KeyValuePair<string, string>> {
					new KeyValuePair<string, string> ("text", details.UserInput["message"].ToString() ),
					new KeyValuePair<string, string> ( "parentId", replyToId )
				};
					
				var response = await POSTHelper.Send("https://shacknotify.bit-shift.com/replyToNotification", data, true, authManager);

				
			}
			finally
			{
				deferral.Complete();
			}
		}
	}
}
