using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Latest_Chatty_8.Networking
{
	public static class JSONDownloader
	{
		public static async Task<JObject> DownloadObject(string uri)
		{
			var data = await JSONDownloader.DownloadJSON(uri);
			var payload = JObject.Parse(data);
			return payload;
		}
		public static async Task<JArray> DownloadArray(string uri)
		{
			var data = await JSONDownloader.DownloadJSON(uri);
			var payload = JArray.Parse(data);
			return payload;
		}

		public static async Task<JToken> Download(string uri)
		{
			var data = await JSONDownloader.DownloadJSON(uri);
			var payload = JToken.Parse(data);
			return payload;
		}

		private static async Task<string> DownloadJSON(string uri)
		{
			try
			{
				var request = (HttpWebRequest)HttpWebRequest.Create(new Uri(uri));
				request.Method = "GET";
				request.Headers[HttpRequestHeader.CacheControl] = "no-cache";
				if (uri.StartsWith(Locations.CloudHost))
				{
					request.Headers[HttpRequestHeader.Authorization] = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(CoreServices.Instance.Credentials.UserName + ":" + CoreServices.Instance.Credentials.Password));
				}
				else
				{
					request.Credentials = CoreServices.Instance.Credentials;
				} 
				var response = await request.GetResponseAsync();
				using (var reader = new StreamReader(response.GetResponseStream()))
				{
					var data = await reader.ReadToEndAsync();
					return data;
				}
			}
			catch
			{
				//TODO: Problem!
				throw;
			}
		}
	}
}
