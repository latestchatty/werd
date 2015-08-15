using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Latest_Chatty_8.Common
{
	internal static class EmbedHelper
	{
		private class EmbedInfo
		{
			public Regex Match { get; set; }
			public string Replace { get; set; }
			public string Name { get; set; }
		}

		private static List<EmbedInfo> infos = null;

		internal static string RewriteEmbeds(string postBody)
		{
			if (infos == null)
			{
				CreateInfos();
			}

			var result = postBody;
			foreach (var info in infos)
			{
				if (info.Match.IsMatch(result))
				{
					result = info.Match.Replace(result, info.Replace);
				}
			}
			return result;
		}

		private static void CreateInfos()
		{
			infos = new List<EmbedInfo>
			{
				new EmbedInfo()
				{
					Name = "Generic Image",
					Match = new Regex(@">(?<link>https?://[A-Za-z0-9-\._~:/\?#\[\]@!\$&'\(\)*\+,;=]*\.(?:jpe?g|png|gif))(&#13;)?<", RegexOptions.Compiled | RegexOptions.IgnoreCase),
					Replace = " onclick='return toggleImage(this);' oncontextmenu='rightClickedImage(this.href);'>${link}<br/><img border=\"0\" src=\"" + WebBrowserHelper.LoadingImage + "\" onload=\"(function(e) {loadImage(e, '${link}')})(this)\" class=\"hidden\" /><"
				},
				new EmbedInfo
				{
					Name = "Imgur Gifv",
					Match = new Regex(@">(?<link>https?\:\/\/i\.imgur\.com\/(?<id>[a-z0-9]+)\.gifv)<", RegexOptions.Compiled | RegexOptions.IgnoreCase),
					Replace = " onclick=\"return toggleImgurGifv(this, 'https://i.imgur.com/${id}.gifv#embed');\">${link}<"
				}
			};
		}
	}
}
