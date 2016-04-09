using Common;
using Latest_Chatty_8.Common;
using Latest_Chatty_8.Networking;
using Latest_Chatty_8.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Latest_Chatty_8.Managers
{
	public class IgnoreManager : ICloudSync
	{
		private const string IGNORED_USER_SETTING = "ignoredUsers";
		private List<string> ignoredUsers;
		private SemaphoreSlim locker = new SemaphoreSlim(1);
		private CloudSettingsManager cloudSettingsManager;

		public IgnoreManager(CloudSettingsManager cloudSettingsManager)
		{
			this.cloudSettingsManager = cloudSettingsManager;
		}

		async public Task Initialize()
		{
			await this.Sync();
		}

		async public Task Suspend()
		{
			try
			{
				await this.locker.WaitAsync();
				await this.InternalSaveToCloud();
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
				try
				{
					//FUTURE : If something happens when trying to retrieve the data, should we prevent from saving over top of data that's potentially good?
					//         It's possible that the data's corrupt or something, so maybe not the best idea.
					this.ignoredUsers = await this.cloudSettingsManager.GetCloudSetting<List<string>>(IGNORED_USER_SETTING);
				}
				catch { }

				if (this.ignoredUsers == null)
				{
					this.ignoredUsers = new List<string>();
				}
			}
			finally
			{
				this.locker.Release();
			}
		}

		async public Task<List<string>> GetIgnoredUsers()
		{
			try
			{
				await this.locker.WaitAsync();
				return this.ignoredUsers;
			}
			finally
			{
				this.locker.Release();
			}
		}

		async public Task AddIgnoredUser(string user)
		{
			try
			{
				await this.locker.WaitAsync();
				user = user.ToLower();
				if (!this.ignoredUsers.Contains(user))
				{
					this.ignoredUsers.Add(user);
					await this.InternalSaveToCloud();
				}
			}
			finally
			{
				this.locker.Release();
			}
		}

		async public Task RemoveIgnoredUser(string user)
		{
			try
			{
				await this.locker.WaitAsync();
				user = user.ToLower();
				if (this.ignoredUsers.Contains(user))
				{
					this.ignoredUsers.Remove(user);
					await this.InternalSaveToCloud();
				}
			}
			finally
			{
				this.locker.Release();
			}
		}

		internal async Task RemoveAll()
		{
			try
			{
				await this.locker.WaitAsync();
				this.ignoredUsers = new List<string>();
				await this.InternalSaveToCloud();
			}
			finally
			{
				this.locker.Release();
			}
		}

		async public Task<bool> ShouldIgnoreComment(DataModel.Comment c)
		{
			try
			{
				await this.locker.WaitAsync();
				var ignore = this.ignoredUsers.Contains(c.Author.ToLower());
				if (ignore)
				{
					System.Diagnostics.Debug.WriteLine($"Should ignore post id {c.Id} by user {c.Author}");
				}
				return ignore;
			}
			finally
			{
				this.locker.Release();
			}
		}

		/// <summary>
		/// Call from within a lock.
		/// </summary>
		async private Task InternalSaveToCloud()
		{
			await this.cloudSettingsManager.SetCloudSettings<List<string>>(IGNORED_USER_SETTING, this.ignoredUsers);
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
