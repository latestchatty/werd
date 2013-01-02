using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Latest_Chatty_8.Networking
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
		public async static Task Send(string url, string content, bool sendAuth)
		{
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
					request.Credentials = CoreServices.Instance.Credentials;
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
		}
	}
}
