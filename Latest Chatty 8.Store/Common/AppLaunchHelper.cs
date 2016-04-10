﻿using Latest_Chatty_8.Settings;
using System;
using System.Text.RegularExpressions;

namespace Latest_Chatty_8.Common
{
	internal static class AppLaunchHelper
	{
		private static Regex YoutubeRegex = new Regex(@"(?<link>https?\:\/\/(www\.|m\.)?(youtube\.com|youtu.be)\/(vi?\/|watch\?vi?=|\?vi?=)?(?<id>[^&\?<]+)([^<]*))", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		internal static Uri GetAppLaunchUri(LatestChattySettings settings, Uri link)
		{
			if (settings.ExternalYoutubeApp.Type != ExternalYoutubeAppType.Browser)
			{
				var match = YoutubeRegex.Match(link.ToString());
				if (match.Success)
				{
					return new Uri(string.Format(settings.ExternalYoutubeApp.UriFormat, match.Groups["id"]));
				}
			}
			return null;
		}

		private static Regex ShackLinkRegex = new Regex(@"https?://(www\.)?shacknews.com/.*id=(?<threadId>[0-9]*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
