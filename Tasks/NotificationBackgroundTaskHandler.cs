using System;
using System.Collections.Generic;
using Windows.ApplicationModel.Background;
using Windows.UI.Notifications;
using Autofac;
using Common;

namespace Tasks
{
	public sealed class NotificationBackgroundTaskHandler : IBackgroundTask
	{
		BackgroundTaskDeferral _deferral;
		public async void Run(IBackgroundTaskInstance taskInstance)
		{
			try
			{
				_deferral = taskInstance.GetDeferral();
				var builder = new BuilderModule();
				var container = builder.BuildContainer();

				var details = taskInstance.TriggerDetails as ToastNotificationActionTriggerDetail;
				if (details == null) return;
				var authManager = container.Resolve<AuthenticationManager>();
				await authManager.Initialize();

				if (!authManager.LoggedIn) return;

				var replyToId = details.Argument.Substring(details.Argument.IndexOf("reply=", StringComparison.Ordinal) + 6);

				var data = new List<KeyValuePair<string, string>> {
					new KeyValuePair<string, string> ("text", details.UserInput["message"].ToString() ),
					new KeyValuePair<string, string> ( "parentId", replyToId )
				};

				using (var _ = await PostHelper.Send(Locations.NotificationReplyToNotification, data, true, authManager, "application/json")) { }

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
				_deferral?.Complete();
			}
		}
	}
}
