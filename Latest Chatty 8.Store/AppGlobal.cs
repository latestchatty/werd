using Autofac;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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
				Task.Run(() => AppGlobal.DebugLog.AddMessage($"Shortcut keys enabled: {value}"));
				_shortcutKeysEnabled = value;
			}
		}

		static AppGlobal()
		{
			Settings = new LatestChattySettings();
			Container = new AppModuleBuilder().BuildContainer();
		}

		internal static class DebugLog
		{
			private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);

			private static readonly ObservableCollection<string> messages = new ObservableCollection<string>();
			public static ReadOnlyObservableCollection<string> Messages { get; private set; }

			static DebugLog()
			{
				Messages = new ReadOnlyObservableCollection<string>(messages);
			}

			public static async Task AddMessage(string message)
			{
				try
				{
					await semaphore.WaitAsync().ConfigureAwait(false);
					messages.Add($"[{DateTime.Now}] {message}");
				}
				finally
				{
					semaphore.Release();
				}
			}

			public static async Task AddException(string message, Exception e)
			{
				var builder = new StringBuilder();
				builder.AppendLine(message);
				builder.AppendLine(e.Message);
				builder.AppendLine(e.StackTrace);
				await AddMessage(builder.ToString()).ConfigureAwait(false);
			}

			public static async Task AddCallStack(string message = "", bool includeAddCallStack = false)
			{
				var stackTrace = new StackTrace();
				var frames = stackTrace.GetFrames();
				var builder = new StringBuilder();

				if (!string.IsNullOrWhiteSpace(message)) builder.AppendLine(message);

				var stopAt = includeAddCallStack ? frames.Length : frames.Length - 1;
				for (int i = 0; i < stopAt; i++)
				{
					builder.AppendLine($"{frames[i].GetFileName()}:{frames[i].GetFileLineNumber()} - {frames[i].GetMethod()}");
				}
				await AddMessage(builder.ToString()).ConfigureAwait(false);
			}

			//public static async Task Clear()
			//{
			//	try
			//	{
			//		await semaphore.WaitAsync().ConfigureAwait(false);
			//		await CoreApplication.MainView.CoreWindow.Dispatcher.RunOnUiThreadAndWait(CoreDispatcherPriority.Low, () =>
			//		{
			//			messages.Clear();
			//		}).ConfigureAwait(false);
			//	}
			//	finally
			//	{
			//		semaphore.Release();
			//	}
			//}

			public static async Task<IList<string>> GetMessages()
			{
				try
				{
					await semaphore.WaitAsync().ConfigureAwait(false);
					return messages.ToList();
				}
				finally
				{
					semaphore.Release();
				}
			}
		}
	}
}
