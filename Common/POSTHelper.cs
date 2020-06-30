using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Common
{
	/// <summary>
	/// Help POST requests
	/// </summary>
	public static class PostHelper
	{
		/// <summary>
		/// Sends the POST request.  Authorization credentials will be passed as required by the host urls
		/// </summary>
		/// <param name="uri">The URI.</param>
		/// <param name="content">The content.</param>
		/// <param name="sendAuth">if set to <c>true</c> authorization heaers will be sent.</param>
		/// <param name="services">Auth services</param>
		/// <param name="acceptHeader">Accept header to send</param>
		/// <returns></returns>
		public async static Task<HttpResponseMessage> Send(Uri uri, List<KeyValuePair<string, string>> content, bool sendAuth, AuthenticationManager services, string acceptHeader = "", HttpClientHandler handler = null)
		{
			if (uri is null) { throw new ArgumentNullException(nameof(uri)); }
			if (content is null) { throw new ArgumentNullException(nameof(content)); }
			if (services is null) { throw new ArgumentNullException(nameof(services)); }
			//Debug.WriteLine($"POST to {url} with data {content} {(sendAuth ? "sending" : "not sending")} auth.", nameof(PostHelper));
			var disposeHandler = false;
#pragma warning disable CA2000 // Dispose objects before losing scope
			if (handler == null) { handler = new HttpClientHandler(); disposeHandler = true; }
#pragma warning restore CA2000 // Dispose objects before losing scope
			try
			{
				if (handler.SupportsAutomaticDecompression)
				{
					handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
				}

				using var request = new HttpClient(handler);
				request.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent.Agent);
				var localContent = new List<KeyValuePair<string, string>>(content);
				if (sendAuth)
				{
					localContent.AddRange(new[]
					{
						new KeyValuePair<string, string>("username", services.UserName),
						new KeyValuePair<string, string>("password", services.GetPassword())
					});
				}

				//Winchatty seems to crap itself if the Expect: 100-continue header is there.
				request.DefaultRequestHeaders.ExpectContinue = false;
				if (!string.IsNullOrWhiteSpace(acceptHeader))
				{
					request.DefaultRequestHeaders.Add("Accept", acceptHeader);
				}

				var items = localContent.Select(i => WebUtility.UrlEncode(i.Key) + "=" + WebUtility.UrlEncode(i.Value));
				using var formContent = new StringContent(string.Join("&", items), null, "application/x-www-form-urlencoded");

				var response = await request.PostAsync(uri, formContent).ConfigureAwait(false);
				return response;
			}
			catch
			{
				if (disposeHandler) { handler.Dispose(); }
				throw;
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
