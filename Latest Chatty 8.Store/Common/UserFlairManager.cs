using Latest_Chatty_8.Networking;
using Latest_Chatty_8.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Latest_Chatty_8.Common
{
	public class UserFlairManager : ICloudSync
	{
		private const string TEN_YEAR_USER_SETTING = "tenYearUsers";
		private List<string> tenYearUsers;
		private SemaphoreSlim locker = new SemaphoreSlim(1);


		async public Task Initialize()
		{
			try
			{
				this.tenYearUsers = await ComplexSetting.ReadSetting<List<string>>(TEN_YEAR_USER_SETTING);
			}
			catch { }
			if(this.tenYearUsers == null || this.tenYearUsers.Count == 0)
			{
				await this.Sync();
			}
		}

		async public Task Suspend()
		{
		}

		async public Task Sync()
		{
			try
			{
				await this.locker.WaitAsync();
				var parsedResponse = await JSONDownloader.Download(Locations.GetTenYearUsers);
				if (parsedResponse["users"] != null)
				{
					this.tenYearUsers = parsedResponse["users"].ToObject<List<string>>().Select(x => x.ToLower()).ToList();
				}
				try
				{
					await ComplexSetting.SetSetting(TEN_YEAR_USER_SETTING, this.tenYearUsers);
				}
				catch { }
			}
			finally
			{
				this.locker.Release();
			}
		}

		async public Task<bool> IsTenYearUser(string user)
		{
			try
			{
				await this.locker.WaitAsync();
				return this.tenYearUsers.Contains(user.ToLower());
			}
			finally
			{
				this.locker.Release();
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
