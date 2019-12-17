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

		public async Task<T> GetCloudSetting<T>(string settingName, bool compress = false)
		{
			if (!_authManager.LoggedIn)
			{
				return default(T);
			}

			try
			{
				var compressed = true;
				var response = await JsonDownloader.Download(Locations.GetSettings + string.Format("?username={0}&client=werd{1}", Uri.EscapeUriString(_authManager.UserName), Uri.EscapeUriString(settingName)));
				if (response == null || response["data"] == null || string.IsNullOrWhiteSpace(response["data"]?.ToString()))
				{
					response = await JsonDownloader.Download(Locations.GetSettings + string.Format("?username={0}&client=latestchattyUWP{1}", Uri.EscapeUriString(_authManager.UserName), Uri.EscapeUriString(settingName)));
					compressed = false;
				}

				if (response != null && response["data"] != null)
				{
					var d = response["data"].ToString();
					if (!string.IsNullOrWhiteSpace(d))
					{
						var data = d;
						if(compressed)
						{
							data = CompressionHelper.DecompressStringFromBase64(d);
						}
						var returnObj = JsonConvert.DeserializeObject<T>(data);
						if (!compressed)
						{
							//Migrate to compressed.
							await SetCloudSettings<T>(settingName, returnObj);
							await SetCloudSettings(settingName, "  ", "latestchattyUWP");
						}
						return returnObj;
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

		public async Task SetCloudSettings<T>(string settingName, T value, string appname="werd")
		{
			if (!_authManager.LoggedIn)
			{
				return;
			}

			var data = JsonConvert.SerializeObject(value);

			using (await PostHelper.Send(Locations.SetSettings, new List<KeyValuePair<string, string>> {
				new KeyValuePair<string, string>("username", _authManager.UserName),
				new KeyValuePair<string, string>("client", $"{appname}{settingName}"),
				new KeyValuePair<string, string>("data", appname == "werd" ? CompressionHelper.CompressStringToBase64(data) : data)
			}, false, _authManager)) { }
		}
	}
}
