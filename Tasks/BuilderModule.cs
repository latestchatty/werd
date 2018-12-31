using Autofac;
using Common;

namespace Tasks
{
	internal class BuilderModule
	{
		public IContainer BuildContainer()
		{
			var builder = new ContainerBuilder();
			builder.RegisterType<AuthenticationManager>().SingleInstance();
			builder.RegisterType<UselessNotificationManager>().As<INotificationManager>();
			builder.RegisterType<SeenPostsManager>().SingleInstance();
			builder.RegisterType<CloudSettingsManager>().InstancePerDependency();
			return builder.Build();
		}
	}
}
