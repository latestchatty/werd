using Autofac;
using Common;
using Werd.Managers;
using Werd.Networking;

namespace Werd
{

	public class AppModuleBuilder
	{
		public IContainer BuildContainer()
		{
			var builder = new ContainerBuilder();
			builder.RegisterType<ChattyManager>().SingleInstance();
			builder.RegisterType<CloudSettingsManager>().InstancePerDependency();
			builder.RegisterType<ThreadMarkManager>().AsSelf().As<ICloudSync>().SingleInstance();
			builder.RegisterType<SeenPostsManager>().AsSelf().As<ICloudSync>().SingleInstance();
			builder.RegisterType<UserFlairManager>().AsSelf().As<ICloudSync>().SingleInstance();
			builder.RegisterType<IgnoreManager>().AsSelf().As<ICloudSync>().SingleInstance();
			builder.RegisterType<AuthenticationManager>().SingleInstance();
			builder.Register(x => AppGlobal.Settings);
			builder.RegisterType<MessageManager>().SingleInstance();
			builder.RegisterType<CloudSyncManager>().SingleInstance();
			builder.RegisterType<NotificationManager>().As<INotificationManager>().SingleInstance();
			builder.RegisterType<NetworkConnectionStatus>().SingleInstance();
			builder.RegisterType<AvailableTagsManager>().SingleInstance();
			var container = builder.Build();
			AppGlobal.Settings.SetCloudManager(container.Resolve<CloudSettingsManager>());
			return container;
		}
	}
}
