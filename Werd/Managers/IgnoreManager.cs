using Common;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Werd.DataModel;
using Werd.Settings;

namespace Werd.Managers
{
	public class IgnoreManager : ICloudSync, IDisposable
	{
		private const string IgnoredUserSetting = "ignoredUsers";
		private const string IgnoredKeywordsSetting = "ignoredKeywords";
		private List<string> _ignoredUsers;
		private List<KeywordMatch> _ignoredKeywords;
		private readonly SemaphoreSlim _locker = new SemaphoreSlim(1);
		private readonly CloudSettingsManager _cloudSettingsManager;
		private readonly AppSettings _settings;
		private readonly Regex _normalizePostBodySpaces = new Regex(@"\s+", RegexOptions.Compiled);

		public int InitializePriority => 0;

		public IgnoreManager(CloudSettingsManager cloudSettingsManager, AppSettings settings)
		{
			_cloudSettingsManager = cloudSettingsManager;
			_settings = settings;
		}

		public async Task Initialize()
		{
			await Sync().ConfigureAwait(true);
		}

		public Task Suspend()
		{
			return Task.CompletedTask;
		}

		public async Task Sync()
		{
			try
			{
				await _locker.WaitAsync().ConfigureAwait(false);
				try
				{
					//FUTURE : If something happens when trying to retrieve the data, should we prevent from saving over top of data that's potentially good?
					//         It's possible that the data's corrupt or something, so maybe not the best idea.
					_ignoredUsers = await _cloudSettingsManager.GetCloudSetting<List<string>>(IgnoredUserSetting).ConfigureAwait(false);
					_ignoredKeywords = await _cloudSettingsManager.GetCloudSetting<List<KeywordMatch>>(IgnoredKeywordsSetting).ConfigureAwait(false);
				}
				catch (Exception e)
				{
					await DebugLog.AddException(string.Empty, e).ConfigureAwait(false);
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
				await _locker.WaitAsync().ConfigureAwait(false);
				return _ignoredUsers;
			}
			finally
			{
				_locker.Release();
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Already done this way.")]
		public async Task AddIgnoredUser(string user)
		{
			try
			{
				await _locker.WaitAsync().ConfigureAwait(false);
				user = user.ToLowerInvariant();
				if (!_ignoredUsers.Contains(user))
				{
					_ignoredUsers.Add(user);
					await InternalSaveToCloud().ConfigureAwait(false);
				}
			}
			finally
			{
				_locker.Release();
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Already done this way.")]
		public async Task RemoveIgnoredUser(string user)
		{
			try
			{
				await _locker.WaitAsync().ConfigureAwait(false);
				user = user.ToLowerInvariant();
				if (_ignoredUsers.Contains(user))
				{
					_ignoredUsers.Remove(user);
					await InternalSaveToCloud().ConfigureAwait(false);
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
				await _locker.WaitAsync().ConfigureAwait(false);
				_ignoredUsers = new List<string>();
				await InternalSaveToCloud().ConfigureAwait(false);
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
				await _locker.WaitAsync().ConfigureAwait(false);
				if (!_ignoredKeywords.Contains(keyword))
				{
					_ignoredKeywords.Add(keyword);
					await InternalSaveToCloud().ConfigureAwait(false);
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
				await _locker.WaitAsync().ConfigureAwait(false);
				if (_ignoredKeywords.Contains(keyword))
				{
					_ignoredKeywords.Remove(keyword);
					await InternalSaveToCloud().ConfigureAwait(false);
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
				await _locker.WaitAsync().ConfigureAwait(false);
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
				await _locker.WaitAsync().ConfigureAwait(false);
				_ignoredKeywords = new List<KeywordMatch>();
				await InternalSaveToCloud().ConfigureAwait(false);
			}
			finally
			{
				_locker.Release();
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Already done this way.")]
		public async Task<bool> ShouldIgnoreComment(Comment c)
		{
			try
			{
				await _locker.WaitAsync().ConfigureAwait(false);
				if (_settings.EnableUserFilter)
				{
					var ignore = _ignoredUsers.Contains(c.Author.ToLowerInvariant());
					if (ignore)
					{
						await DebugLog.AddMessage($"Should ignore post id {c.Id} by user {c.Author}").ConfigureAwait(false);
						return true;
					}
				}

				if (_ignoredKeywords.Count == 0 || !_settings.EnableKeywordFilter) return false;

				var strippedBody = _normalizePostBodySpaces.Replace(Common.HtmlRemoval.StripTagsRegexCompiled(c.Body.Trim(), " "), " ");

				foreach (var keyword in _ignoredKeywords)
				{
					if (strippedBody.Contains(keyword.Match, keyword.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase))
					{
						DebugLog.AddMessage($"Should ignore post id {c.Id} for keyword {keyword.Match}").ConfigureAwait(false).GetAwaiter().GetResult();
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
			await _cloudSettingsManager.SetCloudSettings(IgnoredUserSetting, _ignoredUsers).ConfigureAwait(false);
			await _cloudSettingsManager.SetCloudSettings(IgnoredKeywordsSetting, _ignoredKeywords).ConfigureAwait(false);
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
