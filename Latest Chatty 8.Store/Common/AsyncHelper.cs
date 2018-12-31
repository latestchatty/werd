using System;
using System.Threading.Tasks;
using Windows.UI.Core;

namespace Latest_Chatty_8.Common
{
	public static class AsyncHelper
	{
		public static async Task RunOnUiThreadAndWait(this CoreDispatcher dispatcher, CoreDispatcherPriority priority, Action action)
		{
			var cs = new TaskCompletionSource<object>();
			await dispatcher.RunAsync(priority, () =>
			{
				try
				{
					action();
					cs.SetResult(null);
				}
				catch (Exception e)
				{
					cs.SetException(e);
				}
			});
			await cs.Task.ConfigureAwait(false);
		}
	}
}
