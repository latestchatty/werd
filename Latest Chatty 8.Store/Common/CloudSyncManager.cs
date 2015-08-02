using Latest_Chatty_8.Shared.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Latest_Chatty_8.Common
{
	public class CloudSyncManager : IDisposable
	{
		private Timer persistenceTimer;
		private LatestChattySettings settings;
		private ICloudSync[] syncable;

        public CloudSyncManager(ICloudSync[] syncable, LatestChattySettings settings)
		{
			if(syncable == null)
			{
				throw new ArgumentNullException("syncable");
			}

			this.settings = settings;
			this.syncable = syncable;
		}

		async public Task RunSync()
		{
			try
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
			finally
			{
				this.persistenceTimer = new System.Threading.Timer(async (a) => await RunSync(), null, Math.Max(Math.Max(this.settings.RefreshRate, 1), System.Diagnostics.Debugger.IsAttached ? 10 : 60) * 1000, System.Threading.Timeout.Infinite);
			}
		}

		async internal Task Initialize()
		{
			foreach (var s in this.syncable)
			{
				await s.Initialize();
			}
			this.persistenceTimer = new System.Threading.Timer(async (a) => await RunSync(), null, Math.Max(Math.Max(this.settings.RefreshRate, 1), System.Diagnostics.Debugger.IsAttached ? 10 : 60) * 1000, System.Threading.Timeout.Infinite);
		}

		async internal Task Suspend()
		{
			foreach (var s in this.syncable)
			{
				await s.Suspend();
			}
			if(this.persistenceTimer != null)
			{
				this.persistenceTimer.Dispose();
				this.persistenceTimer = null;
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
					if(this.persistenceTimer != null)
					{
						this.persistenceTimer.Dispose();
						this.persistenceTimer = null;
					}
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~CloudSyncManager() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion
	}
}
