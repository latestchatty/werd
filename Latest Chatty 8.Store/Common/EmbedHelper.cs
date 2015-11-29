using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Latest_Chatty_8.Common
{
	internal static class EmbedHelper
	{
		private class EmbedInfo
		{
			public Regex Match { get; set; }
			public string Replace { get; set; }
			public EmbedTypes Type { get; set; }
		}

		private static List<EmbedInfo> infos = null;
		
		internal static string GetEmbedHtml(Uri link)
		{
			if (infos == null)
			{
				CreateInfos();
			}

			var linkText = link.ToString();
			foreach (var info in infos)
			{
				if (info.Match.IsMatch(linkText))
				{
					return info.Match.Replace(linkText, info.Replace);
				}
			}
			return null;
		}

		private static void CreateInfos()
		{
			infos = new List<EmbedInfo>
			{
				new EmbedInfo()
				{
					Type  = EmbedTypes.Image,
					Match = new Regex(@"(?<link>https?://[a-z0-9-\._~:/\?#\[\]@!\$&'\(\)*\+,;=%]*\.(?:jpe?g|png|gif))[^<]*", RegexOptions.Compiled | RegexOptions.IgnoreCase),
					Replace = @"
<html>
<style>
	body {
		background: #212121;
	}
	img {
		max-width: 100%;
		max-height: 100%;
	}
</style>
<body>
	<center>
		<img src='${link}'/>
	</center>
</body>
</html>"
				},
				new EmbedInfo
				{
					Type = EmbedTypes.Video,
					Match = new Regex(@"(?<link>https?\:\/\/i\.imgur\.com\/(?<id>[a-z0-9]+)\.gifv)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
					Replace = @"
<html>
<style>
	body {
		background: #212121;
	}
   img {
		max-width: 100%;
		max-height: 100%;
	}
</style>
<body>
	<center>	
		<video autoplay loop muted type='video/mp4' src='https://i.imgur.com/${id}.mp4' />
	</center>
</body>
</html>"
				},
				new EmbedInfo
				{
					Type = EmbedTypes.Video,
					Match = new Regex(@"(?<link>https?\:\/\/(www\.)?gfycat\.com\/(?<id>[a-z0-9]+)#?([^<]*))", RegexOptions.Compiled | RegexOptions.IgnoreCase),
					Replace = @"
<html>
<style>
	body {
		background: #212121;
	}
   img {
		max-width: 100%;
		max-height: 100%;
	}
</style>
<body>
	<center>	
		<video autoplay loop muted>
			<source src='http://zippy.gfycat.com/${id}.mp4' type='video/mp4' />
			<source src='http://fat.gfycat.com/${id}.mp4' type='video/mp4' />
			<source src='http://giant.gfycat.com/${id}.mp4' type='video/mp4' />
		</video>
	</center>
</body>
</html>"
				},
				new EmbedInfo
				{
					Type = EmbedTypes.Twitter,
					Match = new Regex(@"(?<link>https?\:\/\/(www\.)?twitter\.com\/([a-z0-9]+)#?([^<]*)/status/(?<id>[a-z0-9]+)#?([^<]*))", RegexOptions.Compiled | RegexOptions.IgnoreCase),
					Replace = @"
<html>

<head>
	<script src='https://platform.twitter.com/widgets.js'></script>
	<style>
		body {
			background: #212121;
		}
		
		img {
			max-width: 100%;
			max-height: 100%;
		}
	</style>
	<script type='text/javascript'>
		document.addEventListener('DOMContentLoaded', function(event) { 
			var target = document.getElementById('tweet');  
			twttr.widgets.createTweet('${id}', target, { theme: 'dark' });
		});
	</script>
</head>

<body>
	<center>
		<div id='tweet'></div> 
	</center>
</body>

</html>"
				}
			};
		}
	}
}
