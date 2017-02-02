using Latest_Chatty_8.Common;
using Latest_Chatty_8.Settings;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Latest_Chatty_8.Networking;

namespace Latest_Chatty_8.Managers
{
	public class CloudSyncManager : IDisposable
	{
		private Timer persistenceTimer;
		private readonly LatestChattySettings settings;
		private readonly NetworkConnectionStatus connectionStatus;
		private readonly ICloudSync[] syncable;
		private bool initialized = false;
		private bool runTimer = false;

		public CloudSyncManager(ICloudSync[] syncable, LatestChattySettings settings, NetworkConnectionStatus connectionStatus)
		{
			if (syncable == null)
			{
				throw new ArgumentNullException("syncable");
			}

			this.settings = settings;
			this.syncable = syncable;
			this.connectionStatus = connectionStatus;
		}

		public async Task RunSync()
		{
			try
			{
				//If we don't have a connection, there's no use in doing any cloud syncing stuff. It's just going to fail.
				if (this.connectionStatus.IsConnected)
				{
					foreach (var s in this.syncable)
					{
						try
						{
							await s.Sync();
						}
						catch { }
					}
				}
			}
			finally
			{
				if (this.runTimer)
				{
					this.persistenceTimer.Change(Math.Max(Math.Max(this.settings.RefreshRate, 1), System.Diagnostics.Debugger.IsAttached ? 10 : 60) * 1000, Timeout.Infinite);
				}
			}
		}

		internal async Task Initialize()
		{
			if (this.initialized) return;
			this.initialized = true;
			foreach (var s in this.syncable.OrderBy(x => x.InitializePriority))
			{
				await s.Initialize();
			}
			this.runTimer = true;
			this.persistenceTimer = new Timer(async (a) => await RunSync(), null, Math.Max(Math.Max(this.settings.RefreshRate, 1), System.Diagnostics.Debugger.IsAttached ? 10 : 60) * 1000, Timeout.Infinite);
		}

		internal async Task Suspend()
		{
			this.runTimer = false;
			foreach (var s in this.syncable)
			{
				await s.Suspend();
			}
			if (this.persistenceTimer != null)
			{
				this.persistenceTimer.Dispose();
				this.persistenceTimer = null;
			}

			this.initialized = false;
		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					if (this.persistenceTimer != null)
					{
						this.persistenceTimer.Dispose();
						this.persistenceTimer = null;
					}
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
