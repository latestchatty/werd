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
					Match = new Regex(@"<a (?<href>[^>]*)>(?<link>https?://[A-Za-z0-9-\._~:/\?#\[\]@!\$&'\(\)*\+,;=]*\.(?:jpe?g|png|gif))(&#13;)?</a>", RegexOptions.Compiled | RegexOptions.IgnoreCase),
					Replace = "<span><a ${href} oncontextmenu=\"rightClickedImage('${link}');\" onclick=\"return toggleEmbeddedImage(this.parentNode, '${link}');\">${link}</a> <a href='${link}' class='openExternal'></a><div></div></span>"
				},
				new EmbedInfo
				{
					Name = "Imgur Gifv",
					Match = new Regex(@"<a (?<href>[^>]*)>(?<link>https?\:\/\/i\.imgur\.com\/(?<id>[a-z0-9]+)\.gifv)</a>", RegexOptions.Compiled | RegexOptions.IgnoreCase),
					Replace = "<span><a ${href} onclick=\"return toggleEmbeddedVideo(this.parentNode, 'https://i.imgur.com/${id}.mp4');\">${link}</a> <a href='${link}' class='openExternal' ></a><div></div></span>"
				},
                new EmbedInfo
				{
					Name = "Gfycat",
					Match = new Regex(@"<a (?<href>[^>]*)>(?<link>https?\:\/\/gfycat\.com\/(?<id>[a-z0-9]+))</a>", RegexOptions.Compiled | RegexOptions.IgnoreCase),
					Replace = "<span><a ${href} onclick=\"return toggleEmbeddedVideo(this.parentNode, 'https://fat.gfycat.com/${id}.mp4');\">${link}</a> <a href='${link}' class='openExternal' ></a><div></div></span>"
				}
			};
		}
	}
}
