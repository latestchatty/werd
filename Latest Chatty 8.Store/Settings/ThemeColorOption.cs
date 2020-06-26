using System;
using Windows.UI;

namespace Latest_Chatty_8.Settings
{
	public class ThemeColorOption
	{
		public string Name { get; private set; }

		public Color AccentBackgroundColor { get; private set; }

		public Color AccentForegroundColor { get; private set; }

		public Color WindowTitleBackgroundColor { get; private set; }

		public Color WindowTitleForegroundColor { get; private set; }

		public Color WindowTitleForegroundColorInactive { get; private set; }

		public Color AppBackgroundColor { get; private set; }

		public Color SelectedPostBackgroundColor { get; private set; }

		public Color AccentHighColor { get; private set; }
		public Color AccentMediumColor { get; private set; }
		public Color AccentLowColor { get; private set; }

		public ThemeColorOption(string name, Color accentBackground, Color accentForeground, Color appBackground, Color selectedPostBackground)
		{
			Name = name;

			AccentBackgroundColor = accentBackground;
			AccentForegroundColor = accentForeground;
			AccentHighColor = Color.FromArgb(accentBackground.A, (byte)Math.Max(accentBackground.R - 15, 0), (byte)Math.Max(accentBackground.G - 15, 0), (byte)Math.Max(accentBackground.B - 15, 0));
			AccentMediumColor = Color.FromArgb(accentBackground.A, (byte)Math.Max(accentBackground.R - 30, 0), (byte)Math.Max(accentBackground.G - 30, 0), (byte)Math.Max(accentBackground.B - 30, 0));
			AccentLowColor = Color.FromArgb(accentBackground.A, (byte)Math.Max(accentBackground.R - 45, 0), (byte)Math.Max(accentBackground.G - 45, 0), (byte)Math.Max(accentBackground.B - 45, 0));
			AppBackgroundColor = appBackground;
			SelectedPostBackgroundColor = selectedPostBackground;
			WindowTitleBackgroundColor = Color.FromArgb(accentBackground.A, (byte)Math.Max(accentBackground.R - 20, 0), (byte)Math.Max(accentBackground.G - 20, 0), (byte)Math.Max(accentBackground.B - 20, 0));
			WindowTitleForegroundColor = accentForeground;
			WindowTitleForegroundColorInactive = Color.FromArgb(accentForeground.A, (byte)Math.Max(accentForeground.R - 120, 0), (byte)Math.Max(accentForeground.G - 120, 0), (byte)Math.Max(accentForeground.B - 120, 0));
		}
	}
}
