using Windows.Graphics.Display;

namespace Latest_Chatty_8.Shared.Converters
{
	public static class ResolutionScaleConverter
	{
		public static float ScaleFactor
		{
			get
			{
				switch (DisplayInformation.GetForCurrentView().ResolutionScale)
				{
					case ResolutionScale.Scale100Percent:
						return 1;
					case ResolutionScale.Scale120Percent:
						return 1.2f;
					case ResolutionScale.Scale140Percent:
						return 1.4f;
					case ResolutionScale.Scale150Percent:
						return 1.5f;
					case ResolutionScale.Scale160Percent:
						return 1.6f;
					case ResolutionScale.Scale180Percent:
						return 1.8f;
					case ResolutionScale.Scale225Percent:
						return 2.25f;
					default:
						return -1;
				}
			}
		}
	}
}
