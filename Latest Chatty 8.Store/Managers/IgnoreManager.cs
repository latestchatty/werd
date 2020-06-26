using Common;
using Latest_Chatty_8.DataModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Latest_Chatty_8.Managers
{
	public class IgnoreManager : ICloudSync, IDisposable
	{
		private const string IgnoredUserSetting = "ignoredUsers";
		private const string IgnoredKeywordsSetting = "ignoredKeywords";
		private List<string> _ignoredUsers;
		private List<KeywordMatch> _ignoredKeywords;
		private readonly SemaphoreSlim _locker = new SemaphoreSlim(1);
		private readonly CloudSettingsManager _cloudSettingsManager;

		public int InitializePriority => 0;

		public IgnoreManager(CloudSettingsManager cloudSettingsManager)
		{
			_cloudSettingsManager = cloudSettingsManager;
		}

		public async Task Initialize()
		{
			await Sync();
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
				try
				{
					//FUTURE : If something happens when trying to retrieve the data, should we prevent from saving over top of data that's potentially good?
					//         It's possible that the data's corrupt or something, so maybe not the best idea.
					_ignoredUsers = await _cloudSettingsManager.GetCloudSetting<List<string>>(IgnoredUserSetting);
					_ignoredKeywords = await _cloudSettingsManager.GetCloudSetting<List<KeywordMatch>>(IgnoredKeywordsSetting);
				}
				catch
				{
					// ignored
				}

				if (_ignoredUsers == null)
				{
					_ignoredUsers = new List<string>();
				}
				if (_ignoredKeywords == null)
				{
					_ignoredKeywords = new List<KeywordMatch>();
				}
			}
			finally
			{
				_locker.Release();
			}
		}

		public async Task<List<string>> GetIgnoredUsers()
		{
			try
			{
				await _locker.WaitAsync();
				return _ignoredUsers;
			}
			finally
			{
				_locker.Release();
			}
		}

		public async Task AddIgnoredUser(string user)
		{
			try
			{
				await _locker.WaitAsync();
				user = user.ToLower();
				if (!_ignoredUsers.Contains(user))
				{
					_ignoredUsers.Add(user);
					await InternalSaveToCloud();
				}
			}
			finally
			{
				_locker.Release();
			}
		}

		public async Task RemoveIgnoredUser(string user)
		{
			try
			{
				await _locker.WaitAsync();
				user = user.ToLower();
				if (_ignoredUsers.Contains(user))
				{
					_ignoredUsers.Remove(user);
					await InternalSaveToCloud();
				}
			}
			finally
			{
				_locker.Release();
			}
		}

		internal async Task RemoveAllUsers()
		{
			try
			{
				await _locker.WaitAsync();
				_ignoredUsers = new List<string>();
				await InternalSaveToCloud();
			}
			finally
			{
				_locker.Release();
			}
		}

		internal async Task AddIgnoredKeyword(KeywordMatch keyword)
		{
			try
			{
				await _locker.WaitAsync();
				if (!_ignoredKeywords.Contains(keyword))
				{
					_ignoredKeywords.Add(keyword);
					await InternalSaveToCloud();
				}
			}
			finally
			{
				_locker.Release();
			}
		}

		internal async Task RemoveIgnoredKeyword(KeywordMatch keyword)
		{
			try
			{
				await _locker.WaitAsync();
				if (_ignoredKeywords.Contains(keyword))
				{
					_ignoredKeywords.Remove(keyword);
					await InternalSaveToCloud();
				}
			}
			finally
			{
				_locker.Release();
			}
		}

		internal async Task<List<KeywordMatch>> GetIgnoredKeywords()
		{
			try
			{
				await _locker.WaitAsync();
				return _ignoredKeywords;
			}
			finally
			{
				_locker.Release();
			}
		}

		internal async Task RemoveAllKeywords()
		{
			try
			{
				await _locker.WaitAsync();
				_ignoredKeywords = new List<KeywordMatch>();
				await InternalSaveToCloud();
			}
			finally
			{
				_locker.Release();
			}
		}

		public async Task<bool> ShouldIgnoreComment(Comment c)
		{
			try
			{
				await _locker.WaitAsync();
				var ignore = _ignoredUsers.Contains(c.Author.ToLower());
				if (ignore)
				{
					Debug.WriteLine($"Should ignore post id {c.Id} by user {c.Author}");
					return true;
				}

				if (_ignoredKeywords.Count == 0) return false;

				var strippedBody = Common.HtmlRemoval.StripTagsRegexCompiled(c.Body.Trim(), " ");
				//OPTIMIZE: Switch to regex with keywords concatenated.  Otherwise this will take significantly longer the more keywords are specified.
				foreach (var keyword in _ignoredKeywords)
				{
					//If it's case sensitive, we'll compare it to the body unaltered, otherwise tolower.
					//Whole word matching will be taken care of when the match was created.
					var compareBody = " " + (keyword.CaseSensitive ? strippedBody : strippedBody.ToLower()) + " ";
					if (compareBody.Contains(keyword.Match))
					{
						Debug.WriteLine($"Should ignore post id {c.Id} for keyword {keyword.Match}");
						return true;
					}
				}
				return false;
			}
			finally
			{
				_locker.Release();
			}
		}

		/// <summary>
		/// Call from within a lock.
		/// </summary>
		private async Task InternalSaveToCloud()
		{
			await _cloudSettingsManager.SetCloudSettings(IgnoredUserSetting, _ignoredUsers);
			await _cloudSettingsManager.SetCloudSettings(IgnoredKeywordsSetting, _ignoredKeywords);
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
