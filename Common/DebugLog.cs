using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Common
{
	public static class DebugLog
	{
		private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);

		private static readonly ObservableCollection<string> messages = new ObservableCollection<string>();
		public static ReadOnlyObservableCollection<string> Messages { get; private set; }

		public static int DebugLogMessageBufferSize { get; set; } = 5000;

		static DebugLog()
		{
			Messages = new ReadOnlyObservableCollection<string>(messages);
		}

		public static async Task AddMessage(string message, [CallerMemberName] string caller = "")
		{
			try
			{
				await semaphore.WaitAsync().ConfigureAwait(false);
				var formattedMessage = $"[{DateTime.Now}] - {caller} : {message}";
				//Debug.WriteLine(formattedMessage);
				messages.Add(formattedMessage);
				while (messages.Count > DebugLogMessageBufferSize)
				{
					messages.RemoveAt(0);
				}
			}
			finally
			{
				semaphore.Release();
			}
		}

		public static async Task AddException(string message, Exception e, [CallerMemberName] string caller = "")
		{
			var builder = new StringBuilder();
			builder.AppendLine(message);
			builder.AppendLine(e.Message);
			builder.AppendLine(e.StackTrace);
			await AddMessage(builder.ToString(), caller).ConfigureAwait(false);
		}

		public static async Task AddCallStack(string message = "", [CallerMemberName] string caller = "")
		{
			var stackTrace = new StackTrace();
			var builder = new StringBuilder();

			if (!string.IsNullOrWhiteSpace(message)) builder.AppendLine(message);
			builder.AppendLine(stackTrace.ToString());
			await AddMessage(builder.ToString(), caller).ConfigureAwait(false);
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
