using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Common
{
	public class SeenPostsManager : ICloudSync, IDisposable
	{
		/// <summary>
		/// List of posts we've seen before.
		/// </summary>
		private HashSet<int> SeenPosts { get; set; }
		private bool _dirty;
		private readonly INotificationManager _notificationManager;
		private readonly CloudSettingsManager _cloudSettingsManager;

		readonly SemaphoreSlim _locker = new SemaphoreSlim(1);

		public event EventHandler Updated;

		public int InitializePriority => int.MaxValue;

		public SeenPostsManager(INotificationManager notificationManager, CloudSettingsManager cloudSettingsManager)
		{
			SeenPosts = new HashSet<int>();
			_notificationManager = notificationManager;
			_cloudSettingsManager = cloudSettingsManager;
		}

		public async Task Initialize()
		{
			await DebugLog.AddMessage($"Initializing {GetType().Name}").ConfigureAwait(false);
			try
			{
				SeenPosts = new HashSet<int>();
				await SyncSeenPosts().ConfigureAwait(false);
			}
			catch { SeenPosts = new HashSet<int>(); }
		}

		public bool IsCommentNew(int postId)
		{
			try
			{
				//System.Diagnostics.DebugLog.AddMessage("IsCommentNew {0}", DateTime.Now.Ticks);
				_locker.Wait();
				var result = !SeenPosts.Contains(postId);
				return result;
			}
			finally
			{
				_locker.Release();
			}
		}

		public void MarkCommentSeen(int postId)
		{
			try
			{
				//System.Diagnostics.DebugLog.AddMessage("MarkCommentSeen {0}", DateTime.Now.Ticks);
				_locker.Wait();
				_notificationManager.RemoveNotificationForCommentId(postId);
				var wasMarked = SeenPosts.Contains(postId);
				if (!wasMarked)
				{
					SeenPosts.Add(postId);
					_dirty = true;
				}
			}
			finally
			{
				_locker.Release();
			}
		}

		public async Task Sync()
		{
			await SyncSeenPosts().ConfigureAwait(false);
		}

		public async Task Suspend()
		{
			await SyncSeenPosts(false).ConfigureAwait(false);
		}

		private async Task SyncSeenPosts(bool fireUpdate = true)
		{
			var lockSucceeded = false;
			try
			{
				await DebugLog.AddMessage("Enter").ConfigureAwait(false);

				await DebugLog.AddMessage("Getting cloud seen for merge.").ConfigureAwait(false);
				var cloudSeen = await _cloudSettingsManager.GetCloudSetting<HashSet<int>>("SeenPosts").ConfigureAwait(false) ?? new HashSet<int>();

				try
				{
					if (await _locker.WaitAsync(10).ConfigureAwait(false))
					{
						lockSucceeded = true;
						await DebugLog.AddMessage("Persisting...").ConfigureAwait(false);
						SeenPosts.UnionWith(cloudSeen);

						await DebugLog.AddMessage($"Combined posts total {SeenPosts.Count}").ConfigureAwait(false);
						if (SeenPosts.Count > 20_000)
						{
							//Remove oldest post IDs first.
							SeenPosts = new HashSet<int>(SeenPosts.OrderByDescending(x => x).Take(15_000));
							await DebugLog.AddMessage("Trimmed seen posts").ConfigureAwait(false);
							_dirty = true;
						}

						if (fireUpdate)
						{
							var _ = Task.Run(() => { Updated?.Invoke(this, EventArgs.Empty); });
						}

						if (!_dirty)
						{
							await DebugLog.AddMessage("We didn't change anything.").ConfigureAwait(false);
							return; //Nothing to do.
						}
					}
				}
				finally
				{
					// Release the lock early since there's no more modifications to it here and we don't want to block if the API is slow.
					if (lockSucceeded) _locker.Release();
				}

				await _cloudSettingsManager.SetCloudSettings("SeenPosts", SeenPosts).ConfigureAwait(false);
				await DebugLog.AddMessage("Persisted.").ConfigureAwait(false);
				_dirty = false;
			}
			catch (Exception e)
			{
				await DebugLog.AddException(string.Empty, e).ConfigureAwait(false);
			}
			finally
			{
				await DebugLog.AddMessage("Exit").ConfigureAwait(false);
			}
		}

		#region IDisposable Support
		private bool _disposedValue; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
					_dirty = false;
					_locker.Dispose();
				}
				_disposedValue = true;
			}
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}
}
