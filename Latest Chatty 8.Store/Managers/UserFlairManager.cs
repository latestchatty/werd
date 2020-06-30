using Common;
using Werd.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Werd.Managers
{
	public class UserFlairManager : ICloudSync, IDisposable
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
				_tenYearUsers = await ComplexSetting.ReadSetting<List<string>>(TenYearUserSetting);
			}
			catch
			{
				// ignored
			}

			if (_tenYearUsers == null || _tenYearUsers.Count == 0)
			{
				await Sync();
			}
		}

		public Task Suspend()
		{
			return Task.CompletedTask;
		}

		public async Task Sync()
		{
			try
			{
				await _locker.WaitAsync();
				if (DateTime.UtcNow.Subtract(_lastRefresh).TotalMinutes > 60)
				{
					var parsedResponse = await JsonDownloader.Download(Locations.GetTenYearUsers);
					if (parsedResponse["users"] != null)
					{
						_tenYearUsers = parsedResponse["users"].ToObject<List<string>>().Select(x => x.ToLower()).ToList();
					}
					try
					{
						await ComplexSetting.SetSetting(TenYearUserSetting, _tenYearUsers);
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

		public async Task<bool> IsTenYearUser(string user)
		{
			try
			{
				await _locker.WaitAsync();
				return _tenYearUsers.Contains(user.ToLower());
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
		}
		#endregion
	}
}
