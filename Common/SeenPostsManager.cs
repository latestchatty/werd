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
		private List<int> SeenPosts { get; set; }
		private bool _dirty;
		private readonly INotificationManager _notificationManager;
		private readonly CloudSettingsManager _cloudSettingsManager;

		readonly SemaphoreSlim _locker = new SemaphoreSlim(1);

		public event EventHandler Updated;

		public int InitializePriority => int.MaxValue;

		public SeenPostsManager(INotificationManager notificationManager, CloudSettingsManager cloudSettingsManager)
		{
			SeenPosts = new List<int>();
			_notificationManager = notificationManager;
			_cloudSettingsManager = cloudSettingsManager;
        }

		public async Task Initialize()
		{
			Debug.WriteLine($"Initializing {GetType().Name}");
			SeenPosts = (await _cloudSettingsManager.GetCloudSetting<List<int>>("SeenPosts")) ?? new List<int>();
			await SyncSeenPosts();
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
			await SyncSeenPosts();
		}

		public async Task Suspend()
		{
			await SyncSeenPosts(false);
		}

		private async Task SyncSeenPosts(bool fireUpdate = true)
		{
			var lockSucceeded = false;
			try
			{
				Debug.WriteLine("SyncSeenPosts - Enter");

				Debug.WriteLine("SyncSeenPosts - Getting cloud seen for merge.");
				var cloudSeen = await _cloudSettingsManager.GetCloudSetting<List<int>>("SeenPosts") ?? new List<int>();

				if (await _locker.WaitAsync(10))
				{
					lockSucceeded = true;
					Debug.WriteLine("SyncSeenPosts - Persisting...");
					SeenPosts = SeenPosts.Union(cloudSeen).ToList();

					//OPTIMIZE: At some point we could look through the chatty to see if an id is still active, but right now that seems like a lot of time to tie up the locker that's unecessary.
					if (SeenPosts.Count > 6800)
					{
						//There is currently a limit in FormUrlEncodedContent of 64K.  We need to keep our payload below that.
						//I didn't do exact math but 7000 worked, so we'll go with 6800 to save room for username and blah blah.  6800 should still be easily more posts than we ever see in a day.
						SeenPosts = SeenPosts.Skip(SeenPosts.Count - 6800).ToList();
					}

					if (fireUpdate)
					{
						var _ = Task.Run(() => { if (Updated != null) Updated(this, EventArgs.Empty); });
					}

					if (!_dirty)
					{
						Debug.WriteLine("SyncSeenPosts - We didn't change anything.");
						return; //Nothing to do.
					}

					await _cloudSettingsManager.SetCloudSettings("SeenPosts", SeenPosts);
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
		}
		#endregion
	}
}
