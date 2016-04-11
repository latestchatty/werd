using Latest_Chatty_8.Common;
using Latest_Chatty_8.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Latest_Chatty_8.Managers
{
	public enum MarkType
	{
		Unmarked,
		Pinned,
		Collapsed
	}

	public class ThreadMarkEventArgs : EventArgs
	{
		public int ThreadID { get; private set; }
		public MarkType Type { get; private set; }

		public ThreadMarkEventArgs(int threadId, MarkType type)
		{
			this.ThreadID = threadId;
			this.Type = type;
		}
	}

	public class ThreadMarkManager : ICloudSync, IDisposable
	{
		private Dictionary<int, MarkType> markedThreads = new Dictionary<int, MarkType>();

		public ThreadMarkManager()
		{
			this.markedThreads = new Dictionary<int, MarkType>();
		}

		private SemaphoreSlim locker = new SemaphoreSlim(1);

		private AuthenticationManager authenticationManager;

		public event EventHandler<ThreadMarkEventArgs> PostThreadMarkChanged;

		public ThreadMarkManager(AuthenticationManager authMgr)
		{
			this.authenticationManager = authMgr;
		}

		async public Task MarkThread(int id, MarkType type, bool preventChangeEvent = false)
		{
			try
			{
				await this.locker.WaitAsync();
				var stringType = Enum.GetName(typeof(MarkType), type).ToLower();

				System.Diagnostics.Debug.WriteLine("Marking thread {0} as type {1}", id, stringType);

				using (var result = await POSTHelper.Send(Locations.MarkPost,
					new List<KeyValuePair<string, string>>()
					{
					new KeyValuePair<string, string>("username", this.authenticationManager.UserName),
					new KeyValuePair<string, string>("postId", id.ToString()),
					new KeyValuePair<string, string>("type", stringType)
					},
					false,
					this.authenticationManager)) { }
				if (type == MarkType.Unmarked)
				{
					if (this.markedThreads.ContainsKey(id))
					{
						this.markedThreads.Remove(id);
					}
				}
				else
				{
					if (!this.markedThreads.ContainsKey(id))
					{
						this.markedThreads.Add(id, type);
					}
					else
					{
						this.markedThreads[id] = type;
					}
				}
				if (preventChangeEvent) return;

				if (this.PostThreadMarkChanged != null)
				{
					this.PostThreadMarkChanged(this, new ThreadMarkEventArgs(id, type));
				}
			}
			finally
			{
				this.locker.Release();
			}
		}

		public MarkType GetMarkType(int id)
		{
			try
			{
				this.locker.Wait();
				if (!this.markedThreads.ContainsKey(id)) return MarkType.Unmarked;
				return this.markedThreads[id];
			}
			finally
			{
				this.locker.Release();
			}
		}

		async private Task<Dictionary<int, MarkType>> GetCloudMarkedPosts()
		{
			var markedPosts = new Dictionary<int, MarkType>();
			if (this.authenticationManager.LoggedIn)
			{
				var parsedResponse = await JSONDownloader.Download(Locations.GetMarkedPosts + "?username=" + Uri.EscapeUriString(this.authenticationManager.UserName));
				foreach (var post in parsedResponse["markedPosts"].Children())
				{
					markedPosts.Add((int)post["id"], (MarkType)Enum.Parse(typeof(MarkType), post["type"].ToString(), true));
				}
			}
			return markedPosts;
		}

		async private Task MergeMarks()
		{
			var cloudThreads = await this.GetCloudMarkedPosts();
			if (cloudThreads.ContainsKey(29374230))
			{
				cloudThreads.Remove(29374230);
			}

			//Remove anything that's not still pinned in the cloud.
			var toRemove = this.markedThreads.Keys.Where(tId => tId != 29374230 && !cloudThreads.Keys.Contains(tId)).ToList();
			foreach (var idToRemove in toRemove)
			{
				this.markedThreads.Remove(idToRemove);
				if (this.PostThreadMarkChanged != null)
				{
					this.PostThreadMarkChanged(this, new ThreadMarkEventArgs(idToRemove, MarkType.Unmarked));
				}
			}

			//Now add anything new or update anything that's changed.
			foreach (var mark in cloudThreads)
			{
				if (!this.markedThreads.ContainsKey(mark.Key))
				{
					this.markedThreads.Add(mark.Key, mark.Value);
					if (this.PostThreadMarkChanged != null)
					{
						this.PostThreadMarkChanged(this, new ThreadMarkEventArgs(mark.Key, mark.Value));
					}
				}
				else
				{
					//If the status has changed, update it, otherwise we're good to go.
					if (mark.Value != this.markedThreads[mark.Key])
					{
						if (this.PostThreadMarkChanged != null)
						{
							this.PostThreadMarkChanged(this, new ThreadMarkEventArgs(mark.Key, mark.Value));
						}
					}
				}
			}
#if DEBUG
			if(!this.markedThreads.ContainsKey(29374230))
			{
				this.markedThreads.Add(29374230, MarkType.Pinned);
				if (this.PostThreadMarkChanged != null)
				{
					this.PostThreadMarkChanged(this, new ThreadMarkEventArgs(29374230, MarkType.Pinned));
				}
			}
#endif
		}

		async public Task Initialize()
		{
			try
			{
				System.Diagnostics.Debug.WriteLine($"Initializing {this.GetType().Name}");
				await this.locker.WaitAsync();
				this.markedThreads.Clear();
				await this.MergeMarks();
			}
			finally
			{
				this.locker.Release();
			}
		}

		async public Task Sync()
		{
			try
			{
				await this.locker.WaitAsync();
				await this.MergeMarks();
			}
			finally
			{
				this.locker.Release();
			}
		}

		public Task Suspend()
		{
			return Task.FromResult(false); //Nothing to do, marked posts are always immediately persisted.
		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					this.locker.Dispose();
				}

				disposedValue = true;
			}
		}

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
		}
		#endregion
	}
}
