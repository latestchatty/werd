using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Common
{
	public class CloudSettingsManager
	{
		private readonly AuthenticationManager _authManager;

		public CloudSettingsManager(AuthenticationManager authManager)
		{
			_authManager = authManager;
		}

		public async Task<T> GetCloudSetting<T>(string settingName)
		{
			if (!_authManager.LoggedIn)
			{
				return default(T);
			}

			try
			{
				var response = await JsonDownloader.Download(Locations.GetSettings + string.Format("?username={0}&client=latestchattyUWP{1}", Uri.EscapeUriString(_authManager.UserName), Uri.EscapeUriString(settingName)));
				if (response != null && response["data"] != null)
				{
					var data = response["data"].ToString();
					if (!string.IsNullOrWhiteSpace(data))
					{
						return JsonConvert.DeserializeObject<T>(data);
					}
				}
			}
			/* We should always get valid JSON back.
				If we didn't, that means something we wrote to the service got jacked up
				At that point, we need to give up and start over because we'll never be able to recover.
			*/
			catch (JsonReaderException) { }
			catch (JsonSerializationException) { }

			return default(T);
		}

		public async Task SetCloudSettings<T>(string settingName, T value)
		{
			if (!_authManager.LoggedIn)
			{
				return;
			}

			var data = JsonConvert.SerializeObject(value);

			using (await PostHelper.Send(Locations.SetSettings, new List<KeyValuePair<string, string>> {
				new KeyValuePair<string, string>("username", _authManager.UserName),
				new KeyValuePair<string, string>("client", string.Format("latestchattyUWP{0}", settingName)),
				new KeyValuePair<string, string>("data", data)
			}, false, _authManager)) { }
		}
	}
}
