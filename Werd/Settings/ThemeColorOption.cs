using Windows.UI;

namespace Werd.Settings
{
	public class ThemeColorOption
	{
		public string Name { get; private set; }

		public Color AccentBackgroundColor { get; private set; }

		public ThemeColorOption(string name, Color accentBackground)
		{
			Name = name;
			AccentBackgroundColor = accentBackground;
		}
	}
}
