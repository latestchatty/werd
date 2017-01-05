using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Latest_Chatty_8.Settings
{
	public class YouTubeResolution
	{
		public string DisplayName { get; private set; }

		public MyToolkit.Multimedia.YouTubeQuality Quality { get; private set; }

		public YouTubeResolution(MyToolkit.Multimedia.YouTubeQuality quality, string displayName)
		{
			this.DisplayName = displayName;
			this.Quality = quality;
		}
	}
}
