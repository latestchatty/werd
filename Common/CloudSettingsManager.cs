using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
				var compressed = true;
				var response = await JsonDownloader.Download(new Uri(Locations.GetSettings + $"?username={Uri.EscapeUriString(_authManager.UserName)}&client=werd{Uri.EscapeUriString(settingName)}")).ConfigureAwait(false);
				if (response == null || response["data"] == null || string.IsNullOrWhiteSpace(response["data"]?.ToString()))
				{
					response = await JsonDownloader.Download(new Uri(Locations.GetSettings + $"?username={Uri.EscapeUriString(_authManager.UserName)}&client=latestchattyUWP{Uri.EscapeUriString(settingName)}")).ConfigureAwait(false);
					compressed = false;
				}

				if (response != null && response["data"] != null)
				{
					var d = response["data"].ToString();
					if (!string.IsNullOrWhiteSpace(d))
					{
						var data = d;
						if (compressed)
						{
							data = CompressionHelper.DecompressStringFromBase64(d);
						}
						var returnObj = JsonConvert.DeserializeObject<T>(data);
						if (!compressed)
						{
							//Migrate to compressed.
							await SetCloudSettings<T>(settingName, returnObj).ConfigureAwait(false);
							await SetCloudSettings(settingName, "  ", "latestchattyUWP").ConfigureAwait(false);
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

		public async Task SetCloudSettings<T>(string settingName, T value, string appname = "werd")
		{
			if (!_authManager.LoggedIn)
			{
				return;
			}

			var data = JsonConvert.SerializeObject(value);
			data = appname == "werd" ? CompressionHelper.CompressStringToBase64(data) : data;

			await DebugLog.AddMessage($"Setting cloud setting [{settingName}] with length of {data.Length} bytes").ConfigureAwait(false);

			using (await PostHelper.Send(Locations.SetSettings, new List<KeyValuePair<string, string>> {
				new KeyValuePair<string, string>("username", _authManager.UserName),
				new KeyValuePair<string, string>("client", $"{appname}{settingName}"),
				new KeyValuePair<string, string>("data", data)
			}, false, _authManager).ConfigureAwait(false)) { }
		}
	}
}
