using Autofac;
using Common;
using Windows.ApplicationModel.Background;

namespace Tasks
{
	public sealed class UnreadMessageNotifier : IBackgroundTask
	{
		BackgroundTaskDeferral _deferral;

		public async void Run(IBackgroundTaskInstance taskInstance)
		{
			try
			{
				_deferral = taskInstance.GetDeferral();
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
				_deferral?.Complete();
			}
		}
	}
}
