using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Net.Http;
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
				var data = await JSONDownloader.DownloadJSONString(uri);
				var payload = JObject.Parse(data);
				return payload;
			}
			catch
			{ System.Diagnostics.Debug.Assert(false); return null; }
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
				var data = await JSONDownloader.DownloadJSONString(uri);
				var payload = JArray.Parse(data);
				return payload;
			}
			catch
			{ System.Diagnostics.Debug.Assert(false); return null; }
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
				var data = await JSONDownloader.DownloadJSONString(uri);
				var payload = JToken.Parse(data);
				return payload;
			}
			catch
			{ /*System.Diagnostics.Debug.Assert(false); */ return null; }
		}

		public static async Task<string> DownloadJSONString(string uri)
		{
			try
			{
				using (var handler = new HttpClientHandler())
				{
					if (handler.SupportsAutomaticDecompression)
					{
						handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
						System.Diagnostics.Debug.WriteLine("Starting download with compression for uri {0} ", uri);
					}
					else
					{
						System.Diagnostics.Debug.WriteLine("Starting download for uri {0}", uri);
					}

					using (var request = new HttpClient(handler, true))
					{
						using (var response = await request.GetAsync(uri))
						{
							System.Diagnostics.Debug.WriteLine("Got response from uri {0}", uri);
							return await response.Content.ReadAsStringAsync();
						}
					}
				}
			}
			catch (Exception)
			{
				System.Diagnostics.Debug.WriteLine(string.Format("Error getting JSON data for URL {0}", uri));
				return null;
			}
		}
		#endregion
	}
}
