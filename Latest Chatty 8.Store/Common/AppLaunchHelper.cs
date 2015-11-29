using Latest_Chatty_8.Settings;
using System;
using System.Text.RegularExpressions;

namespace Latest_Chatty_8.Common
{
	internal static class AppLaunchHelper
	{
		internal static Uri GetAppLaunchUri(LatestChattySettings settings, Uri link)
		{
			var youtubeRegex = new Regex(@"(?<link>https?\:\/\/(www\.|m\.)?(youtube\.com|youtu.be)\/(vi?\/|watch\?vi?=|\?vi?=)?(?<id>[^&\?<]+)([^<]*))", RegexOptions.IgnoreCase | RegexOptions.Compiled);
			var match = youtubeRegex.Match(link.ToString());
			if (match.Success && settings.ExternalYoutubeApp.Type != ExternalYoutubeAppType.Browser)
			{
				return new Uri(string.Format(settings.ExternalYoutubeApp.UriFormat, match.Groups["id"]));
			}
			return null;
		}
	}
}
