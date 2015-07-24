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
			this.persistenceTimer = new System.Threading.Timer(async (a) => await SaveSeenPosts(), null, 10000, System.Threading.Timeout.Infinite);
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

		public async Task SaveSeenPosts()
		{
			var lockSucceeded = false;
			try
			{
				System.Diagnostics.Debug.WriteLine("SaveSeenPosts - Enter");
				if (!this.dirty)
				{
					System.Diagnostics.Debug.WriteLine("SaveSeenPosts - Nothing to do. Bailing.");
					return; //Nothing to do.
				}

				System.Diagnostics.Debug.WriteLine("SaveSeenPosts - Getting cloud seen for merge.");
				var cloudSeen = await this.settings.GetCloudSetting<List<int>>("SeenPosts");
				
				if (await this.locker.WaitAsync(10))
				{
					lockSucceeded = true;
					System.Diagnostics.Debug.WriteLine("SaveSeenPosts - Persisting...");
					//OPTIMIZE: At some point we could look through the chatty to see if an id is still active, but right now that seems like a lot of time to tie up the locker that's unecessary.
					if (this.SeenPosts.Count > 9000)
					{
						//Merge with cloud posts, then truncate to the latest
						this.SeenPosts = this.SeenPosts.Union(cloudSeen).OrderByDescending(x => x).Skip(this.SeenPosts.Count - 9000) as List<int>;
					}
					await this.settings.SetCloudSettings<List<int>>("SeenPosts", this.SeenPosts);
                    System.Diagnostics.Debug.WriteLine("SaveSeenPosts - Persisted.");
					var t = Task.Run(() => { if (this.Updated != null) this.Updated(this, EventArgs.Empty); });
					this.dirty = false;
				}
			}
			finally
			{
				if (lockSucceeded) this.locker.Release();
				this.persistenceTimer = new System.Threading.Timer(async (a) => await SaveSeenPosts(), null, 10000, System.Threading.Timeout.Infinite);
				System.Diagnostics.Debug.WriteLine("SaveSeenPosts - Exit");
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
