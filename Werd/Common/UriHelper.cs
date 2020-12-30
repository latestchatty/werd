using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Werd.Common
{
	public static class UriHelper
	{
		public async static Task<Uri> MakeWebViewSafeUriOrSearch(string uriText)
		{
			if (!uriText.StartsWith("http", StringComparison.OrdinalIgnoreCase))
			{
				if (Uri.TryCreate($"http://{uriText}", UriKind.Absolute, out var url))
				{
					using (var client = new HttpClient())
					{
						try
						{
							var res = await client.GetAsync(url).ConfigureAwait(false);
							if (res.IsSuccessStatusCode) { return url; }
						}
						catch { }
					}
				}
			}
			else
			{
				if (Uri.TryCreate(uriText, UriKind.Absolute, out var parsed)) { return parsed; }
			}

			return new Uri($"https://bing.com/search?q={uriText}");
		}
	}
}
