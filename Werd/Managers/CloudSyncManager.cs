using Common;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Werd.Networking;
using Werd.Settings;

namespace Werd.Managers
{
	public class CloudSyncManager : IDisposable
	{
		private Timer _persistenceTimer;
		private readonly AppSettings _settings;
		private readonly NetworkConnectionStatus _connectionStatus;
		private readonly ICloudSync[] _syncable;
		private bool _initialized;
		private bool _runTimer;

		public CloudSyncManager(ICloudSync[] syncable, AppSettings settings, NetworkConnectionStatus connectionStatus)
		{
			if (syncable == null)
			{
				throw new ArgumentNullException(nameof(syncable));
			}

			_settings = settings;
			_syncable = syncable;
			_connectionStatus = connectionStatus;
		}

		public async Task RunSync()
		{
			try
			{
				//If we don't have a connection, there's no use in doing any cloud syncing stuff. It's just going to fail.
				if (_connectionStatus.IsConnected)
				{
					foreach (var s in _syncable)
					{
						try
						{
							await DebugLog.AddMessage($"Syncing {s.GetType().Name}").ConfigureAwait(false);
							await s.Sync().ConfigureAwait(false);
						}
						catch
						{
							// ignored
						}
					}
				}
			}
			finally
			{
				if (_runTimer)
				{
					_persistenceTimer.Change(Math.Max(Math.Max(_settings.RefreshRate, 1), 60) * 1000, Timeout.Infinite);
				}
			}
		}

		internal async Task Initialize()
		{
			if (_initialized) return;
			_initialized = true;
			foreach (var s in _syncable.OrderBy(x => x.InitializePriority))
			{
				await s.Initialize().ConfigureAwait(false);
			}
			_runTimer = true;
			_persistenceTimer = new Timer(async a => await RunSync().ConfigureAwait(false), null, Math.Max(Math.Max(_settings.RefreshRate, 1), 60) * 1000, Timeout.Infinite);
		}

		internal async Task Suspend()
		{
			_runTimer = false;
			foreach (var s in _syncable)
			{
				await s.Suspend().ConfigureAwait(true);
			}
			if (_persistenceTimer != null)
			{
				_persistenceTimer.Dispose();
				_persistenceTimer = null;
			}

			_initialized = false;
		}

		#region IDisposable Support
		private bool _disposedValue; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
					if (_persistenceTimer != null)
					{
						_persistenceTimer.Dispose();
						_persistenceTimer = null;
					}
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
