﻿using Autofac;
using Common;
using Latest_Chatty_8.Managers;
using Latest_Chatty_8.Networking;
using Latest_Chatty_8.Settings;
using System.Diagnostics;

namespace Latest_Chatty_8
{
	public static class Global
	{
		public static LatestChattySettings Settings { get; }
		public static IContainer Container { get; }

		private static bool _shortcutKeysEnabled = true;
		public static bool ShortcutKeysEnabled
		{
			get => _shortcutKeysEnabled; set
			{
				Debug.WriteLine($"Shortcut keys enabled: {value}");
				_shortcutKeysEnabled = value;
			}
		}

		static Global()
		{
			Settings = new LatestChattySettings();
			Container = new AppModuleBuilder().BuildContainer();
		}
	}

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
			builder.Register(x => Global.Settings);
			builder.RegisterType<MessageManager>().SingleInstance();
			builder.RegisterType<CloudSyncManager>().SingleInstance();
			builder.RegisterType<NotificationManager>().As<INotificationManager>().SingleInstance();
			builder.RegisterType<NetworkConnectionStatus>().SingleInstance();
			builder.RegisterType<AvailableTagsManager>().SingleInstance();
			var container = builder.Build();
			Global.Settings.SetCloudManager(container.Resolve<CloudSettingsManager>());
			return container;
		}
	}
}
