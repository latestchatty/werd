namespace Latest_Chatty_8.Settings
{
	public enum ExternalYoutubeAppType
	{
		Hyper,
		Browser,
		Tubecast,
		Mytube
	}

	public class ExternalYoutubeApp
	{
		public string DisplayName { get; private set; }

		public ExternalYoutubeAppType Type { get; private set; }

		public string UriFormat { get; private set; }

		public ExternalYoutubeApp(ExternalYoutubeAppType type, string uriFormat, string displayName)
		{
			this.DisplayName = displayName;
			this.UriFormat = uriFormat;
			this.Type = type;
		}
	}
}
