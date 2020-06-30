using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Werd.Common
{
	static class WebViewExtensions
	{
		public static async Task NavigateWithShackLogin(this WebView webview, Uri uri, AuthenticationManager authManager)
		{
			var cookieContainer = new System.Net.CookieContainer();
			using (var handler = new HttpClientHandler
			{
				CookieContainer = cookieContainer
			})
			{
				var response = await PostHelper.Send("https://www.shacknews.com/account/signin",
					new List<KeyValuePair<string, string>> {
						new KeyValuePair<string, string>("user-identifier", authManager.UserName),
						new KeyValuePair<string, string>("supplied-pass", authManager.GetPassword())
					},
					false,
					authManager,
					"",
					handler);
				var shackCookies = cookieContainer.GetCookies(new Uri("https://www.shacknews.com/")).Cast<System.Net.Cookie>();
				var li = shackCookies.FirstOrDefault(c => c.Name == "_shack_li_");
				var intCookie = shackCookies.FirstOrDefault(c => c.Name == "_shack_int_");
				void SetCookieInWebView(string key, string value)
				{
					Windows.Web.Http.Filters.HttpBaseProtocolFilter filter = new Windows.Web.Http.Filters.HttpBaseProtocolFilter();
					Windows.Web.Http.HttpCookie cookie = new Windows.Web.Http.HttpCookie(key, ".shacknews.com", "/");
					cookie.Value = value;
					filter.CookieManager.SetCookie(cookie, false);
				}
				SetCookieInWebView("_shack_li_", li?.Value);
				SetCookieInWebView("_shack_int_", intCookie?.Value);
				var request = new Windows.Web.Http.HttpRequestMessage(Windows.Web.Http.HttpMethod.Get, uri);
				webview.NavigateWithHttpRequestMessage(request);
			}
		}
	}
}
