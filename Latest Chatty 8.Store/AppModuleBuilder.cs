using Autofac;
using Latest_Chatty_8.Common;
using Latest_Chatty_8.Settings;

namespace Latest_Chatty_8
{
	public class AppModuleBuilder
	{
		public IContainer BuildContainer()
		{
			var builder = new ContainerBuilder();
			builder.RegisterType<ChattyManager>().SingleInstance();
			builder.RegisterType<ThreadMarkManager>().AsSelf().As<ICloudSync>().SingleInstance();
			builder.RegisterType<SeenPostsManager>().AsSelf().As<ICloudSync>().SingleInstance();
			builder.RegisterType<AuthenticationManager>().SingleInstance();
			builder.RegisterType<LatestChattySettings>().SingleInstance();
			builder.RegisterType<MessageManager>().SingleInstance();
			builder.RegisterType<CloudSyncManager>().SingleInstance();
			return builder.Build();
		}
	}
}
