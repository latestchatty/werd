using Autofac;
using Latest_Chatty_8.Common;
using Windows.ApplicationModel.Background;

namespace Tasks
{
	public sealed class UnreadMessageNotifier : IBackgroundTask
	{
		BackgroundTaskDeferral deferral = null;

		public async void Run(IBackgroundTaskInstance taskInstance)
		{
			try
			{
				deferral = taskInstance.GetDeferral();
				var builder = new BuilderModule();
				var container = builder.BuildContainer();

				var authManager = container.Resolve<AuthenticationManager>();
				await authManager.Initialize();
				var notificationManager = container.Resolve<INotificationManager>();

				if (!authManager.LoggedIn) return;
				await notificationManager.UpdateBadgeCount();
			}
			finally
			{
				deferral?.Complete();
			}
		}
	}
}
