using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace Latest_Chatty_8.Settings
{
	public class ThemeColorOption
	{
		public string Name { get; private set; }

		public Color BackgroundColor { get; private set; }

		public Color ForegroundColor { get; private set; }

		public ThemeColorOption(string name, Color background, Color foreground)
		{
			this.Name = name;
			this.BackgroundColor = background;
			this.ForegroundColor = foreground;
		}
	}
}
