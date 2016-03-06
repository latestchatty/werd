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

				if (!details.Argument.StartsWith("reply=")) return;

				var pwVault = new PasswordVault();
				var userName = string.Empty;
				var password = string.Empty;

				//Try to get user/pass from stored creds.
				try
				{
					var cred = pwVault.RetrieveAll().FirstOrDefault();
					if (cred != null)
					{
						userName = cred.UserName;
						cred.RetrievePassword();
						password = cred.Password;
					}
				}
				catch (Exception) { }

				//Not logged in, can't do anything for ya.  Really shouldn't be getting notifications, so... yeah.
				if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password)) return;

				var replyToId = details.Argument.Substring(details.Argument.IndexOf("reply=") + 6);
				var request = new HttpClient();
				var data = new Dictionary<string, string> {
					{ "text", details.UserInput["message"].ToString() },
					{ "parentId", replyToId },
					{ "username", userName },
					{ "password", password }
				};

				//Winchatty seems to crap itself if the Expect: 100-continue header is there.
				request.DefaultRequestHeaders.ExpectContinue = false;

				var formContent = new FormUrlEncodedContent(data);

				var response = await request.PostAsync("https://shacknotify.bit-shift.com/replyToNotification", formContent);
			}
			finally
			{
				deferral.Complete();
			}
		}
	}
}
