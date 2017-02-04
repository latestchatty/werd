using Autofac;
using Latest_Chatty_8.Common;
using Latest_Chatty_8.Networking;
using System.Collections.Generic;
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
				var builder = new BuilderModule();
				var container = builder.BuildContainer();

				var details = taskInstance.TriggerDetails as ToastNotificationActionTriggerDetail;
				var authManager = container.Resolve<AuthenticationManager>();
				await authManager.Initialize();

				if (!authManager.LoggedIn) return;

				var replyToId = details.Argument.Substring(details.Argument.IndexOf("reply=") + 6);

				var data = new List<KeyValuePair<string, string>> {
					new KeyValuePair<string, string> ("text", details.UserInput["message"].ToString() ),
					new KeyValuePair<string, string> ( "parentId", replyToId )
				};

				using (var response = await POSTHelper.Send(Locations.NotificationReplyToNotification, data, true, authManager, "application/json")) { }

				//Mark the comment read and persist to cloud.
				using (var seenPostsManager = container.Resolve<SeenPostsManager>())
				{
					await seenPostsManager.Initialize();
					seenPostsManager.MarkCommentSeen(int.Parse(replyToId));
					await seenPostsManager.Suspend();
				}
			}
			finally
			{
				deferral?.Complete();
			}
		}
	}
}
