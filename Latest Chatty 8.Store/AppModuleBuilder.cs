using Autofac;
using Autofac.Core;
using Latest_Chatty_8.Common;
using Latest_Chatty_8.Shared.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Latest_Chatty_8
{
	public class AppModuleBuilder
	{
		public IContainer BuildContainer()
		{
			var container = new ContainerBuilder();
			container.RegisterType<ChattyManager>().SingleInstance();
			container.RegisterType<PinManager>().SingleInstance();
			container.RegisterType<SeenPostsManager>().SingleInstance();
			container.RegisterType<AuthenticaitonManager>().SingleInstance();
			container.RegisterType<LatestChattySettings>().SingleInstance();
			return container.Build();
		}
	}
}
