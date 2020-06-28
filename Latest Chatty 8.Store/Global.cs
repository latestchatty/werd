using Autofac;
using Latest_Chatty_8.Common;
using Latest_Chatty_8.Settings;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

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
				Task.Run(() => Global.DebugLog.AddMessage($"Shortcut keys enabled: {value}"));
				_shortcutKeysEnabled = value;
			}
		}

		static Global()
		{
			Settings = new LatestChattySettings();
			Container = new AppModuleBuilder().BuildContainer();
		}

		public static class DebugLog
		{
			private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);

			private static readonly ObservableCollection<string> messages = new ObservableCollection<string>();
			public static ReadOnlyObservableCollection<string> Messages { get; private set; }

			public static bool ListVisibleInUI { get; set; } = false;

			static DebugLog()
			{
				Messages = new ReadOnlyObservableCollection<string>(messages);
			}

			public static async Task AddMessage(string message)
			{
				try
				{
					await semaphore.WaitAsync();
					if (ListVisibleInUI)
					{
						//Debug.WriteLine("Adding message on UI thread.");
						await CoreApplication.MainView.CoreWindow.Dispatcher.RunOnUiThreadAndWait(CoreDispatcherPriority.Low, () =>
						{
							messages.Add($"[{DateTime.Now}] {message}");
						});
					}
					else
					{
						//Debug.WriteLine("Adding message not on UI thread.");
						messages.Add($"[{DateTime.Now}] {message}");
					}
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
				await AddMessage(builder.ToString());
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
				await AddMessage(builder.ToString());
			}

			public static async Task Clear()
			{
				try
				{
					await semaphore.WaitAsync();
					messages.Clear();
				}
				finally
				{
					semaphore.Release();
				}
			}
		}
	}
}
