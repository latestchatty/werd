using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;

namespace Latest_Chatty_8.Common
{
	public static class RunExtensions
	{
		public static void ApplyTypesToRun(this Run run, List<RunType> types)
		{
			foreach(var type in types)
			{
				run.ApplyTypeToRun(type);
			}
		}

		public static void ApplyTypeToRun(this Run run, RunType type)
		{
			switch (type)
			{
				case RunType.Red:
					run.Foreground = new SolidColorBrush(Colors.Red);
					break;
				case RunType.Green:
					run.Foreground = new SolidColorBrush(Color.FromArgb(255, 141, 198, 63));
					break;
				case RunType.Blue:
					run.Foreground = new SolidColorBrush(Color.FromArgb(255, 68, 174, 223));
					break;
				case RunType.Yellow:
					run.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 222, 0));
					break;
				case RunType.Olive:
					run.Foreground = new SolidColorBrush(Colors.Olive);
					break;
				case RunType.Lime:
					run.Foreground = new SolidColorBrush(Color.FromArgb(255, 192, 255, 192));
					break;
				case RunType.Orange:
					run.Foreground = new SolidColorBrush(Color.FromArgb(255, 247, 148, 28));
					break;
				case RunType.Pink:
					run.Foreground = new SolidColorBrush(Color.FromArgb(255, 244, 154, 193));
					break;
				case RunType.Italics:
					run.FontStyle = Windows.UI.Text.FontStyle.Italic;
					break;
				case RunType.Bold:
					run.FontWeight = Windows.UI.Text.FontWeights.Bold;
					break;
				case RunType.Quote:
					run.FontSize += 2;
					break;
				case RunType.Sample:
					run.FontSize -= 2;
					break;
				case RunType.Strike:
					//TODO: Strike
					break;
				case RunType.Code:
					run.FontFamily = new FontFamily("Consolas,Times New Roman");
               break;
				default:
					break;
			}
		}
	}
}
