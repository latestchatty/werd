using System;
using System.Linq;
using System.Text.RegularExpressions;
using Werd.Settings;

namespace Werd.Common
{
	internal static class AppLaunchHelper
	{
		internal static (Uri uri, bool openInEmbeddedBrowser) GetAppLaunchUri(AppSettings settings, Uri link)
		{
			foreach (var launcher in settings.CustomLaunchers.Where(l => l.Enabled))
			{
				var linkText = link.ToString();

				if (launcher.Match.IsMatch(linkText))
				{
					return (new Uri(launcher.Match.Replace(linkText, launcher.Replace)), launcher.EmbeddedBrowser);
				}
			}
			return (link, true);
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
