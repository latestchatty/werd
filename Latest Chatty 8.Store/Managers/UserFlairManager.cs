using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Werd.Settings;

namespace Werd.Managers
{
	public class UserFlairManager : IDisposable//, ICloudSync - Don't use ICloudSync interface so it can be left out of the compiled binary since it's not used but ICloudSync stuff is forced
	{
		private const string TenYearUserSetting = "tenYearUsers";
		private List<string> _tenYearUsers;
		private readonly SemaphoreSlim _locker = new SemaphoreSlim(1);
		private DateTime _lastRefresh = DateTime.MinValue;

		public int InitializePriority => 0;

		public async Task Initialize()
		{
			try
			{
				_tenYearUsers = await ComplexSetting.ReadSetting<List<string>>(TenYearUserSetting).ConfigureAwait(false);
			}
			catch
			{
				// ignored
			}

			if (_tenYearUsers == null || _tenYearUsers.Count == 0)
			{
				await Sync().ConfigureAwait(false);
			}
		}

		public Task Suspend()
		{
			return Task.CompletedTask;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase")]
		public async Task Sync()
		{
			try
			{
				await _locker.WaitAsync().ConfigureAwait(false);
				if (DateTime.UtcNow.Subtract(_lastRefresh).TotalMinutes > 60)
				{
					var parsedResponse = await JsonDownloader.Download(Locations.GetTenYearUsers).ConfigureAwait(false);
					if (parsedResponse["users"] != null)
					{
						_tenYearUsers = parsedResponse["users"].ToObject<List<string>>().Select(x => x.ToLowerInvariant()).ToList();
					}
					try
					{
						await ComplexSetting.SetSetting(TenYearUserSetting, _tenYearUsers).ConfigureAwait(false);
					}
					catch
					{
						// ignored
					}

					_lastRefresh = DateTime.UtcNow;
				}
			}
			finally
			{
				_locker.Release();
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase")]
		public async Task<bool> IsTenYearUser(string user)
		{
			try
			{
				await _locker.WaitAsync().ConfigureAwait(false);
				return _tenYearUsers.Contains(user.ToLowerInvariant());
			}
			finally
			{
				_locker.Release();
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
