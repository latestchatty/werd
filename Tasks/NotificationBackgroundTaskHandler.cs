using Autofac;
using Common;
using System.Globalization;
using Windows.ApplicationModel.Background;
using Windows.UI.Notifications;

namespace Tasks
{
	public sealed class NotificationBackgroundTaskHandler : IBackgroundTask
	{
		BackgroundTaskDeferral _deferral;
		public async void Run(IBackgroundTaskInstance taskInstance)
		{
			try
			{
				if (taskInstance is null) return;

				_deferral = taskInstance.GetDeferral();
				var builder = new BuilderModule();
				var container = builder.BuildContainer();

				var details = taskInstance.TriggerDetails as ToastNotificationActionTriggerDetail;
				if (details == null) return;
				var authManager = container.Resolve<AuthenticationManager>();
				await authManager.Initialize().ConfigureAwait(false);

				if (!authManager.LoggedIn) return;

				var replyToId = details.Argument.Replace("reply=", string.Empty);

				var result = await PostHelper.PostComment(details.UserInput["message"].ToString(), authManager, replyToId).ConfigureAwait(false);

				if (!result.Item1) return;

				//Mark the comment read and persist to cloud.
				using (var seenPostsManager = container.Resolve<SeenPostsManager>())
				{
					await seenPostsManager.Initialize().ConfigureAwait(false);
					seenPostsManager.MarkCommentSeen(int.Parse(replyToId, CultureInfo.InvariantCulture));
					await seenPostsManager.Suspend().ConfigureAwait(false);
				}
			}
			finally
			{
				_deferral?.Complete();
			}
		}
	}
}
