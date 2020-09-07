using System;
using System.Threading.Tasks;
using Windows.UI.Core;

namespace Werd.Common
{
	public static class AsyncHelper
	{
		/// <summary>
		/// Runs an action on the UI thread and waits for completion.
		/// If the current thread is the UI thread, the action will be run immediately.
		/// </summary>
		/// <param name="dispatcher">The dispatcher</param>
		/// <param name="priority">Priority to schedule the action with. If the current thread is the UI thread, this has no effect</param>
		/// <param name="action">Action to run</param>
		/// <returns></returns>
		public static async Task RunOnUiThreadAndWait(this CoreDispatcher dispatcher, CoreDispatcherPriority priority, Action action)
		{
			if (dispatcher.HasThreadAccess)
			{
				action();
			}
			else
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

		/// <summary>
		/// Runs an async action on the UI thread and waits for completion.
		/// If the current thread is the UI thread, the action will be run immediately.
		/// </summary>
		/// <param name="dispatcher">The dispatcher</param>
		/// <param name="priority">Priority to schedule the action with. If the current thread is the UI thread, this has no effect</param>
		/// <param name="action">Action to run</param>
		/// <returns></returns>
		public static async Task RunOnUiThreadAndWaitAsync(this CoreDispatcher dispatcher, CoreDispatcherPriority priority, Func<Task> action)
		{
			if (dispatcher.HasThreadAccess)
			{
				await action().ConfigureAwait(false);
			}
			else
			{
				var cs = new TaskCompletionSource<object>();
				await dispatcher.RunAsync(priority, async () =>
				{
					try
					{
						await action().ConfigureAwait(false);
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
}
