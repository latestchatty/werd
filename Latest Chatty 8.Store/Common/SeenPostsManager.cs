using Latest_Chatty_8.Shared.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Latest_Chatty_8.Common
{
	public class SeenPostsManager : IDisposable
	{
		/// <summary>
		/// List of posts we've seen before.
		/// </summary>
		private List<int> SeenPosts { get; set; }
		private System.Threading.Timer persistenceTimer;
		private bool dirty = false;
		private LatestChattySettings settings;

		SemaphoreSlim locker = new SemaphoreSlim(1);

		public event EventHandler Updated;

		public SeenPostsManager(LatestChattySettings settings)
		{
			this.SeenPosts = new List<int>();
			this.settings = settings;
        }

		public async Task Initialize()
		{
			this.SeenPosts = (await this.settings.GetCloudSetting<List<int>>("SeenPosts")) ?? new List<int>();
			await this.SyncSeenPosts();
			this.persistenceTimer = new System.Threading.Timer(async (a) => await SyncSeenPosts(), null, Math.Max(Math.Max(this.settings.RefreshRate, 1), 60) * 1000, System.Threading.Timeout.Infinite);
		}

		public bool IsCommentNew(int postId)
		{
			try
			{
				//System.Diagnostics.Debug.WriteLine("IsCommentNew {0}", DateTime.Now.Ticks);
				this.locker.Wait();
				var result = !this.SeenPosts.Contains(postId);
				return result;
			}
			finally
			{
				this.locker.Release();
			}
		}

		public void MarkCommentSeen(int postId)
		{
			try
			{
				//System.Diagnostics.Debug.WriteLine("MarkCommentSeen {0}", DateTime.Now.Ticks);
				this.locker.Wait();
				this.SeenPosts.Add(postId);
				this.dirty = true;
			}
			finally
			{
				this.locker.Release();
			}
		}

		public async Task SyncSeenPosts(bool fireUpdate = true)
		{
			var lockSucceeded = false;
			try
			{
				System.Diagnostics.Debug.WriteLine("SyncSeenPosts - Enter");
				
				System.Diagnostics.Debug.WriteLine("SyncSeenPosts - Getting cloud seen for merge.");
				var cloudSeen = await this.settings.GetCloudSetting<List<int>>("SeenPosts") ?? new List<int>();

				if (await this.locker.WaitAsync(10))
				{
					lockSucceeded = true;
					System.Diagnostics.Debug.WriteLine("SyncSeenPosts - Persisting...");
					this.SeenPosts = this.SeenPosts.Union(cloudSeen).ToList();

					//OPTIMIZE: At some point we could look through the chatty to see if an id is still active, but right now that seems like a lot of time to tie up the locker that's unecessary.
					if (this.SeenPosts.Count > 6800)
					{
						//There is currently a limit in FormUrlEncodedContent of 64K.  We need to keep our payload below that.
						//I didn't do exact math but 7000 worked, so we'll go with 6800 to save room for username and blah blah.  6800 should still be easily more posts than we ever see in a day.
						this.SeenPosts = this.SeenPosts.OrderBy(x => x).Skip(this.SeenPosts.Count - 6800).ToList();
					}

					if (fireUpdate)
					{
						var t = Task.Run(() => { if (this.Updated != null) this.Updated(this, EventArgs.Empty); });
					}

					if (!this.dirty)
					{
						System.Diagnostics.Debug.WriteLine("SyncSeenPosts - We didn't change anything.");
						return; //Nothing to do.
					}

					await this.settings.SetCloudSettings<List<int>>("SeenPosts", this.SeenPosts);
					System.Diagnostics.Debug.WriteLine("SyncSeenPosts - Persisted.");
					this.dirty = false;
				}
			}
			catch { /*System.Diagnostics.Debugger.Break();*/ /*Generally anything that goes wrong here is going to be due to network connectivity.  So really, we just want to try again later. */ }
			finally
			{
				if (lockSucceeded) this.locker.Release();
				this.persistenceTimer = new System.Threading.Timer(async (a) => await SyncSeenPosts(), null, Math.Max(Math.Max(this.settings.RefreshRate, 1), 60) * 1000, System.Threading.Timeout.Infinite);
				System.Diagnostics.Debug.WriteLine("SyncSeenPosts - Exit");
			}
		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					this.dirty = false;
					this.locker.Dispose();
				}
				disposedValue = true;
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
