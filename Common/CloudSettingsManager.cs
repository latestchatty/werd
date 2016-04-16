using Latest_Chatty_8.Common;
using Latest_Chatty_8.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
	public class CloudSettingsManager
	{
		private readonly AuthenticationManager authManager;

		public CloudSettingsManager(AuthenticationManager authManager)
		{
			this.authManager = authManager;
		}

		public async Task<T> GetCloudSetting<T>(string settingName)
		{
			if (!this.authManager.LoggedIn)
			{
				return default(T);
			}

			try
			{
				var response = await JSONDownloader.Download(Locations.GetSettings + string.Format("?username={0}&client=latestchattyUWP{1}", Uri.EscapeUriString(this.authManager.UserName), Uri.EscapeUriString(settingName)));
				var data = response["data"].ToString();
				if (!string.IsNullOrWhiteSpace(data))
				{
					return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(data);
				}
			}
			/* We should always get valid JSON back.
				If we didn't, that means something we wrote to the service got jacked up
				At that point, we need to give up and start over because we'll never be able to recover.
			*/
			catch (Newtonsoft.Json.JsonReaderException) { }
			catch (Newtonsoft.Json.JsonSerializationException) { }

			return default(T);
		}

		public async Task SetCloudSettings<T>(string settingName, T value)
		{
			if (!this.authManager.LoggedIn)
			{
				return;
			}

			var serializer = new Newtonsoft.Json.JsonSerializer();
			var data = Newtonsoft.Json.JsonConvert.SerializeObject(value);

			using (await POSTHelper.Send(Locations.SetSettings, new List<KeyValuePair<string, string>> {
				new KeyValuePair<string, string>("username", this.authManager.UserName),
				new KeyValuePair<string, string>("client", string.Format("latestchattyUWP{0}", settingName)),
				new KeyValuePair<string, string>("data", data)
			}, false, this.authManager)) { }
		}
	}
}
