using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Latest_Chatty_8.Networking
{
	public static class POSTHelper
	{
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

			var requestStream = await request.GetRequestStreamAsync();
			StreamWriter streamWriter = new StreamWriter(requestStream);
			streamWriter.Write(content);
			streamWriter.Flush();
			streamWriter.Dispose();
			var response = await request.GetResponseAsync() as HttpWebResponse;
		}
	}
}
