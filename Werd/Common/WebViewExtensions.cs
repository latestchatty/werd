using Common;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Werd.Common
{
	static class WebViewExtensions
	{
		public static async Task NavigateWithShackLogin(this WebView2 webview, Uri uri, AuthenticationManager authManager)
		{
			var cookieContainer = new System.Net.CookieContainer();
			using (var handler = new HttpClientHandler
			{
				CookieContainer = cookieContainer
			})
			{
				if (authManager.LoggedIn)
				{
					var response = await PostHelper.Send(new Uri("https://www.shacknews.com/account/signin"),
						new List<KeyValuePair<string, string>> {
						new KeyValuePair<string, string>("user-identifier", authManager.UserName),
						new KeyValuePair<string, string>("supplied-pass", authManager.GetPassword())
						},
						false,
						authManager,
						"",
						handler).ConfigureAwait(true);
					var shackCookies = cookieContainer.GetCookies(new Uri("https://www.shacknews.com/")).Cast<System.Net.Cookie>();
					var li = shackCookies.FirstOrDefault(c => c.Name == "_shack_li_");
					var intCookie = shackCookies.FirstOrDefault(c => c.Name == "_shack_int_");
					void SetCookieInWebView(string key, string value)
					{
						if(value == null) { return; }
						var cookie = webview.CoreWebView2.CookieManager.CreateCookie(key, value, ".shacknews.com", "/");
						webview.CoreWebView2.CookieManager.AddOrUpdateCookie(cookie);
					}
					SetCookieInWebView("_shack_li_", li?.Value);
					SetCookieInWebView("_shack_int_", intCookie?.Value);
				}

				webview.Source = uri;
			}
		}
	}
}
