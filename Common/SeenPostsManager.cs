using System;
using System.Collections.Generic;
using System.Diagnostics;
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
			Debug.WriteLine($"Initializing {GetType().Name}");
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
				//System.Diagnostics.Debug.WriteLine("IsCommentNew {0}", DateTime.Now.Ticks);
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
				//System.Diagnostics.Debug.WriteLine("MarkCommentSeen {0}", DateTime.Now.Ticks);
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
				Debug.WriteLine("SyncSeenPosts - Enter");

				Debug.WriteLine("SyncSeenPosts - Getting cloud seen for merge.");
				var cloudSeen = await _cloudSettingsManager.GetCloudSetting<HashSet<int>>("SeenPosts").ConfigureAwait(false) ?? new HashSet<int>();

				if (await _locker.WaitAsync(10).ConfigureAwait(false))
				{
					lockSucceeded = true;
					Debug.WriteLine("SyncSeenPosts - Persisting...");
					SeenPosts.UnionWith(cloudSeen);

					if (SeenPosts.Count > 100_000)
					{
						//Remove oldest post IDs first.
						SeenPosts = new HashSet<int>(SeenPosts.OrderBy(x => x).Skip(SeenPosts.Count - 20_000));
					}

					if (fireUpdate)
					{
						var _ = Task.Run(() => { Updated?.Invoke(this, EventArgs.Empty); });
					}

					if (!_dirty)
					{
						Debug.WriteLine("SyncSeenPosts - We didn't change anything.");
						return; //Nothing to do.
					}

					await _cloudSettingsManager.SetCloudSettings("SeenPosts", SeenPosts).ConfigureAwait(false);
					Debug.WriteLine("SyncSeenPosts - Persisted.");
					_dirty = false;
				}
			}
			catch { /*System.Diagnostics.Debugger.Break();*/ /*Generally anything that goes wrong here is going to be due to network connectivity.  So really, we just want to try again later. */ }
			finally
			{
				if (lockSucceeded) _locker.Release();
				Debug.WriteLine("SyncSeenPosts - Exit");
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
