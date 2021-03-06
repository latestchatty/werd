﻿using Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Werd.Managers
{
	public enum MarkType
	{
		Unmarked,
		Pinned,
		Collapsed
	}

	public class ThreadMarkEventArgs : EventArgs
	{
		public int ThreadId { get; private set; }
		public MarkType Type { get; private set; }

		public ThreadMarkEventArgs(int threadId, MarkType type)
		{
			ThreadId = threadId;
			Type = type;
		}
	}

	public class ThreadMarkManager : ICloudSync, IDisposable
	{
		private readonly Dictionary<int, MarkType> _markedThreads = new Dictionary<int, MarkType>();

		public ThreadMarkManager()
		{
			_markedThreads = new Dictionary<int, MarkType>();
		}

		private readonly SemaphoreSlim _locker = new SemaphoreSlim(1);

		private readonly AuthenticationManager _authenticationManager;

		public event EventHandler<ThreadMarkEventArgs> PostThreadMarkChanged;
		public int InitializePriority => 1000;

		public ThreadMarkManager(AuthenticationManager authMgr)
		{
			_authenticationManager = authMgr;
		}

		public async Task<List<int>> GetAllMarkedThreadsOfType(MarkType type)
		{
			try
			{
				await _locker.WaitAsync().ConfigureAwait(false);
				return _markedThreads.Where(mt => mt.Value == type).Select(mt => mt.Key).ToList();
			}
			finally
			{
				_locker.Release();
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase")]
		public async Task MarkThread(int id, MarkType type, bool preventChangeEvent = false)
		{
			try
			{
				await _locker.WaitAsync().ConfigureAwait(false);
				var stringType = Enum.GetName(typeof(MarkType), type).ToLowerInvariant();

				await DebugLog.AddMessage($"Marking thread {id} as type {stringType}").ConfigureAwait(false);

				using (var _ = await PostHelper.Send(Locations.MarkPost,
					new List<KeyValuePair<string, string>>
					{
					new KeyValuePair<string, string>("username", _authenticationManager.UserName),
					new KeyValuePair<string, string>("postId", id.ToString(CultureInfo.InvariantCulture)),
					new KeyValuePair<string, string>("type", stringType)
					},
					false,
					_authenticationManager).ConfigureAwait(false)) { }
				if (type == MarkType.Unmarked)
				{
					if (_markedThreads.ContainsKey(id))
					{
						_markedThreads.Remove(id);
					}
				}
				else
				{
					if (!_markedThreads.ContainsKey(id))
					{
						_markedThreads.Add(id, type);
					}
					else
					{
						_markedThreads[id] = type;
					}
				}
				if (preventChangeEvent) return;

				PostThreadMarkChanged?.Invoke(this, new ThreadMarkEventArgs(id, type));
			}
			finally
			{
				_locker.Release();
			}
		}

		public MarkType GetMarkType(int id)
		{
			try
			{
				_locker.Wait();
				if (!_markedThreads.ContainsKey(id)) return MarkType.Unmarked;
				return _markedThreads[id];
			}
			finally
			{
				_locker.Release();
			}
		}

		private async Task<Dictionary<int, MarkType>> GetCloudMarkedPosts()
		{
			var markedPosts = new Dictionary<int, MarkType>();
			if (_authenticationManager.LoggedIn)
			{
				var parsedResponse = await JsonDownloader.Download(new Uri(Locations.GetMarkedPosts + "?username=" + Uri.EscapeUriString(_authenticationManager.UserName))).ConfigureAwait(false);
				if (parsedResponse["markedPosts"] != null)
				{
					foreach (var post in parsedResponse["markedPosts"].Children())
					{
						markedPosts.Add((int)post["id"], (MarkType)Enum.Parse(typeof(MarkType), post["type"].ToString(), true));
					}
				}
			}
			return markedPosts;
		}

		private async Task MergeMarks()
		{
			var cloudThreads = await GetCloudMarkedPosts().ConfigureAwait(false);

			//Remove anything that's not still pinned in the cloud.
			var toRemove = _markedThreads.Keys.Where(tId => !cloudThreads.Keys.Contains(tId)).ToList();
			foreach (var idToRemove in toRemove)
			{
				_markedThreads.Remove(idToRemove);
				PostThreadMarkChanged?.Invoke(this, new ThreadMarkEventArgs(idToRemove, MarkType.Unmarked));
			}

			//Now add anything new or update anything that's changed.
			foreach (var mark in cloudThreads)
			{
				if (!_markedThreads.ContainsKey(mark.Key))
				{
					_markedThreads.Add(mark.Key, mark.Value);
					PostThreadMarkChanged?.Invoke(this, new ThreadMarkEventArgs(mark.Key, mark.Value));
				}
				else
				{
					//If the status has changed, update it, otherwise we're good to go.
					if (mark.Value != _markedThreads[mark.Key])
					{
						PostThreadMarkChanged?.Invoke(this, new ThreadMarkEventArgs(mark.Key, mark.Value));
					}
				}
			}
		}

		public async Task Initialize()
		{
			try
			{
				await DebugLog.AddMessage($"Initializing {GetType().Name}").ConfigureAwait(false);
				await _locker.WaitAsync().ConfigureAwait(false);
				_markedThreads.Clear();
				await MergeMarks().ConfigureAwait(false);
			}
			finally
			{
				_locker.Release();
			}
		}

		public async Task Sync()
		{
			try
			{
				await _locker.WaitAsync().ConfigureAwait(false);
				await MergeMarks().ConfigureAwait(false);
			}
			finally
			{
				_locker.Release();
			}
		}

		public Task Suspend()
		{
			return Task.FromResult(false); //Nothing to do, marked posts are always immediately persisted.
		}

		#region IDisposable Support
		private bool _disposedValue; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
					_locker.Dispose();
				}

				_disposedValue = true;
			}
		}

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}
}
