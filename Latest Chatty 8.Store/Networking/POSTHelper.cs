using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Latest_Chatty_8.Shared.Networking
{
	/// <summary>
	/// Help POST requests
	/// </summary>
	public static class POSTHelper
	{
		/// <summary>
		/// Sends the POST request.  Authorization credentials will be passed as required by the host urls
		/// </summary>
		/// <param name="url">The URL.</param>
		/// <param name="content">The content.</param>
		/// <param name="sendAuth">if set to <c>true</c> authorization heaers will be sent.</param>
		/// <returns></returns>
		public async static Task<HttpResponseMessage> Send(string url, List<KeyValuePair<string, string>> content, bool sendAuth, AuthenticaitonManager services)
		{
			System.Diagnostics.Debug.WriteLine("POST to {0} with data {1} {2} auth.", url, content, sendAuth ? "sending" : "not sending");
			using (var handler = new HttpClientHandler())
			{
				if (handler.SupportsAutomaticDecompression)
				{
					handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
				}

				var request = new HttpClient(handler);
				var localContent = new List<KeyValuePair<string, string>>(content);
				if (sendAuth)
				{
					localContent.AddRange(new[]
					{
						new KeyValuePair<string, string>("username", services.Credentials.UserName),
						new KeyValuePair<string, string>("password", services.Credentials.Password)
					});
				}

				//Winchatty seems to crap itself if the Expect: 100-continue header is there.
				request.DefaultRequestHeaders.ExpectContinue = false;

				var formContent = new FormUrlEncodedContent(localContent);

				var response = await request.PostAsync(url, formContent);
				System.Diagnostics.Debug.WriteLine("POST to {0} got response.", url);
				return response;
			}
		}

		public static List<KeyValuePair<string, string>> BuildDataString(Dictionary<string, string> kv)
		{
			var ret = new List<KeyValuePair<string, string>>(kv.Count - 1);
			foreach (var p in kv)
			{
				ret.Add(new KeyValuePair<string, string>(p.Key, p.Value));
			}
			return ret;
		}
	}
}
