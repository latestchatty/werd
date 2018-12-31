using System;
using System.Text.RegularExpressions;
using Latest_Chatty_8.Settings;

namespace Latest_Chatty_8.Common
{
	internal static class AppLaunchHelper
	{
		private static readonly Regex YoutubeRegex = new Regex(@"(?<link>https?\:\/\/(www\.|m\.)?(youtube\.com|youtu.be)\/(vi?\/|watch\?vi?=|\?vi?=)?(?<id>[^&\?<]+)([^<]*))", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		internal static Uri GetAppLaunchUri(LatestChattySettings settings, Uri link)
		{
			if (settings.ExternalYoutubeApp.Type != ExternalYoutubeAppType.Browser)
			{
				var id = GetYoutubeId(link);
				if (!string.IsNullOrWhiteSpace(id))
				{
					return new Uri(string.Format(settings.ExternalYoutubeApp.UriFormat, id));
				}
			}
			return null;
		}

		internal static string GetYoutubeId(Uri link)
		{
			var match = YoutubeRegex.Match(link.ToString());
			if (match.Success)
			{
				return match.Groups["id"].ToString();
			}
			return null;
		}

		private static readonly Regex ShackLinkRegex = new Regex(@"https?://(www\.)?shacknews.com/.*id=(?<threadId>[0-9]*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		internal static int? GetShackPostId(Uri link)
		{
			var match = ShackLinkRegex.Match(link.ToString());
			if (match.Success)
			{
				int postId;
				if (int.TryParse(match.Groups["threadId"].Value, out postId))
				{
					return postId;
				}
			}
			return null;
		}
	}
}
