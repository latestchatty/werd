using MyToolkit.Multimedia;

namespace Latest_Chatty_8.Settings
{
	public class YouTubeResolution
	{
		public string DisplayName { get; private set; }

		public YouTubeQuality Quality { get; private set; }

		public YouTubeResolution(YouTubeQuality quality, string displayName)
		{
			DisplayName = displayName;
			Quality = quality;
		}
	}
}
