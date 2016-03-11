using Autofac;
using Common;
using Latest_Chatty_8.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
