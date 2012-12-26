using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Latest_Chatty_8.Networking
{
	/// <summary>
	/// Helper to download JSON objects
	/// </summary>
	public static class JSONDownloader
	{
		#region Public Methods
		/// <summary>
		/// Performs a GET on the URI and parses into a JObject.
		/// </summary>
		/// <param name="uri">The URI.</param>
		/// <returns></returns>
		public static async Task<JObject> DownloadObject(string uri)
		{
			try
			{
				var data = await JSONDownloader.DownloadJSON(uri);
				var payload = JObject.Parse(data);
				return payload;
			}
			catch { System.Diagnostics.Debug.Assert(false); return null; }
		}

		/// <summary>
		/// Performs a GET on the URI and parses into a JArray.
		/// </summary>
		/// <param name="uri">The URI.</param>
		/// <returns></returns>
		public static async Task<JArray> DownloadArray(string uri)
		{
			try
			{
				var data = await JSONDownloader.DownloadJSON(uri);
				var payload = JArray.Parse(data);
				return payload;
			}
			catch { System.Diagnostics.Debug.Assert(false); return null; }
		}

		/// <summary>
		/// Performs a GET on the URI and parses into a JToken
		/// </summary>
		/// <param name="uri">The URI.</param>
		/// <returns></returns>
		public static async Task<JToken> Download(string uri)
		{
			try
			{
				var data = await JSONDownloader.DownloadJSON(uri);
				var payload = JToken.Parse(data);
				return payload;
			}
			catch { System.Diagnostics.Debug.Assert(false); return null; }
		}
		#endregion

		#region Private Methods
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
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine(string.Format("Error getting JSON data for URL {0}", uri));
				return null;
			}
		}
		#endregion
	}
}
