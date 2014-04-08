using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
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
		public async static Task<HttpWebResponse> Send(string url, string content, bool sendAuth)
		{
			System.Diagnostics.Debug.WriteLine("POST to {0} with data {1} {2} auth.", url, content, sendAuth ? "sending" : "not sending");
			HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
			request.Method = "POST";
			request.ContentType = "application/x-www-form-urlencoded";
			if (sendAuth)
			{
				if (url.StartsWith(Locations.CloudHost))
				{
					request.Headers[HttpRequestHeader.Authorization] = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(CoreServices.Instance.Credentials.UserName + ":" + CoreServices.Instance.Credentials.Password));
				}
				else
				{
					content += string.Format("&username={0}&password={1}", Uri.EscapeDataString(CoreServices.Instance.Credentials.UserName), Uri.EscapeDataString(CoreServices.Instance.Credentials.Password));
				}
			}

			using (var requestStream = await request.GetRequestStreamAsync())
			{
				using (var streamWriter = new StreamWriter(requestStream))
				{
					streamWriter.Write(content);
				}
			}
			var response = await request.GetResponseAsync() as HttpWebResponse;
			System.Diagnostics.Debug.WriteLine("POST to {0} got response.", url);
			return response;
		}

		public static string BuildDataString(Dictionary<string, string> kv)
		{
			var sb = new StringBuilder();
			foreach (var kvp in kv)
			{
				if(sb.Length > 0)
				{
					sb.Append("&");
				}
				sb.AppendFormat("{0}={1}", Uri.EscapeDataString(kvp.Key), Uri.EscapeDataString(kvp.Value));
			}
			return sb.ToString();
		}
	}
}
