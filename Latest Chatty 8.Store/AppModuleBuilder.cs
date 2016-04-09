using Autofac;
using Common;
using Latest_Chatty_8.Common;
using Latest_Chatty_8.Managers;
using Latest_Chatty_8.Settings;

namespace Latest_Chatty_8
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
			builder.RegisterType<LatestChattySettings>().SingleInstance();
			builder.RegisterType<MessageManager>().SingleInstance();
			builder.RegisterType<CloudSyncManager>().SingleInstance();
			builder.RegisterType<NotificationManager>().As<INotificationManager>().SingleInstance();
			return builder.Build();
		}
	}
}
