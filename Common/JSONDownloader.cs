using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Common
{
	/// <summary>
	/// Helper to download JSON objects
	/// </summary>
	public static class JsonDownloader
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
				var data = await DownloadJsonString(uri);
				var payload = JObject.Parse(data);
				return payload;
			}
			catch
			{ Debug.Assert(false); return null; }
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
				var data = await DownloadJsonString(uri);
				var payload = JArray.Parse(data);
				return payload;
			}
			catch
			{ Debug.Assert(false); return null; }
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
				var data = await DownloadJsonString(uri);
				var payload = JToken.Parse(data);
				return payload;
			}
			catch
			{ /*System.Diagnostics.Debug.Assert(false); */ return null; }
		}

		public static async Task<string> DownloadJsonString(string uri)
		{
			try
			{
				using (var handler = new HttpClientHandler())
				{
					if (handler.SupportsAutomaticDecompression)
					{
						handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
						//Debug.WriteLine("Starting download with compression for uri {0} ", uri);
					}
					else
					{
						//Debug.WriteLine("Starting download for uri {0}", uri);
					}

					using (var request = new HttpClient(handler, true))
					{
						request.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent.Agent);
						if (uri.Contains(Locations.NotificationBase))
						{
							request.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
						}
						using (var response = await request.GetAsync(uri))
						{
							//Debug.WriteLine("Got response from uri {0}", uri);
							return await response.Content.ReadAsStringAsync();
						}
					}
				}
			}
			catch (Exception)
			{
				Debug.WriteLine(string.Format("Error getting JSON data for URL {0}", uri));
				return null;
			}
		}
		#endregion
	}
}
