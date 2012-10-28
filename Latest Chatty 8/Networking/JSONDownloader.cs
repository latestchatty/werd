using LatestChatty.Classes;
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
		public static async Task<JArray> Download(string uri)
		{
			JArray payload = null;

			try
			{
				var request = (HttpWebRequest)HttpWebRequest.Create(new Uri(uri));
				request.Method = "GET";
				request.Headers[HttpRequestHeader.CacheControl] = "no-cache";
				//TODO: Re-Implement GET credentials.
				//this.request.Credentials = CoreServices.Instance.Credentials;
				var response = await request.GetResponseAsync();
				var reader = new StreamReader(response.GetResponseStream());
				var data = await reader.ReadToEndAsync();
				payload = JArray.Parse(data);
			}
			catch
			{
				//TODO: Problem!
				throw;
			}
			return payload;
		}
	}
}
