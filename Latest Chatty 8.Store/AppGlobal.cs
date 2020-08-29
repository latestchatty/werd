using Autofac;
using Common;
using System.Threading.Tasks;
using Werd.Settings;

namespace Werd
{
	public static class AppGlobal
	{
		public static LatestChattySettings Settings { get; }
		public static IContainer Container { get; }

		private static bool _shortcutKeysEnabled = true;
		public static bool ShortcutKeysEnabled
		{
			get => _shortcutKeysEnabled; set
			{
				Task.Run(() => DebugLog.AddMessage($"Shortcut keys enabled: {value}"));
				_shortcutKeysEnabled = value;
			}
		}

		static AppGlobal()
		{
			Settings = new LatestChattySettings();
			Container = new AppModuleBuilder().BuildContainer();
		}
	}
}
