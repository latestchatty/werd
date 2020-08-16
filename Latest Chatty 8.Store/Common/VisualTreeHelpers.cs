using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Werd.Common
{
	public static class VisualTreeHelpers
	{
		public static T FindFirstControlNamed<T>(this DependencyObject parent, string name)
			where T : FrameworkElement
		{

			return parent.AllChildren<T>().FirstOrDefault(c => c.Name == name);
		}
		public static IEnumerable<T> FindControlsNamed<T>(this DependencyObject parent, string name)
			where T : FrameworkElement
		{

			return parent.AllChildren<T>().Where(c => c.Name == name).Select(c1 => c1);
		}

		public static T FindFirstParentControlNamed<T>(this DependencyObject parent, string name)
			where T : FrameworkElement

		{
			var fe = ((FrameworkElement)parent);
			if (fe.Parent == null) return null;
			if (fe.Name == name && parent is T) return (T)parent;
			return fe.Parent.FindFirstParentControlNamed<T>(name);
		}
		public static List<T> AllChildren<T>(this DependencyObject parent)
	where T : FrameworkElement
		{
			var controlList = new List<T>();
			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
			{
				var child = VisualTreeHelper.GetChild(parent, i);
				if (child is T)
					controlList.Add(child as T);

				controlList.AddRange(AllChildren<T>(child));
			}
			return controlList;
		}

		public static double GetAppWidth()
		{
			return Window.Current.Bounds.Width;
		}

		public static double GetMaxFlyoutWidthForFullScreen()
		{
			return GetAppWidth() - 50;
		}

		public static double GetMaxFlyoutContentWidthForFullScreen()
		{
			return GetMaxFlyoutWidthForFullScreen() - 20;
		}

		public static Brush TagPreviewBrush(int lolCount, int infCount, int unfCount, int tagCount, int wtfCount, int wowCount, int awwCount)
		{
			var brush = new SolidColorBrush(Colors.Transparent);
			//FF8800
			if (lolCount > 0) brush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 136, 0));
			if (infCount > 0)
			{
				if (brush.Color != Colors.Transparent)
				{
					return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 230, 230, 230));
				}
				brush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 153, 204));
			}
			if (unfCount > 0)
			{
				if (brush.Color != Colors.Transparent)
				{
					return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 230, 230, 230));
				}
				brush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 238, 0, 0));
			}
			if (tagCount > 0)
			{
				if (brush.Color != Colors.Transparent)
				{
					return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 230, 230, 230));
				}
				brush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 119, 187, 34));
			}
			if (wtfCount > 0)
			{
				if (brush.Color != Colors.Transparent)
				{
					return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 230, 230, 230));
				}
				brush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 192, 0, 192));
			}
			if (wowCount > 0)
			{
				if (brush.Color != Colors.Transparent)
				{
					return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 230, 230, 230));
				}
				brush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 236, 163, 199));
			}
			if (awwCount > 0)
			{
				if (brush.Color != Colors.Transparent)
				{
					return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 230, 230, 230));
				}
				brush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 19, 164, 167));
			}
			return brush;
		}

		public static string TagPreviewTooltip(int lolCount, int infCount, int unfCount, int tagCount, int wtfCount, int wowCount, int awwCount)
		{
			var builder = new StringBuilder();
			if (lolCount > 0) builder.AppendLine($"{lolCount} lol(s)");
			if (infCount > 0) builder.AppendLine($"{infCount} inf(s)");
			if (unfCount > 0) builder.AppendLine($"{unfCount} unf(s)");
			if (tagCount > 0) builder.AppendLine($"{tagCount} tag(s)");
			if (wtfCount > 0) builder.AppendLine($"{wtfCount} wtf(s)");
			if (wowCount > 0) builder.AppendLine($"{wowCount} wow(s)");
			if (awwCount > 0) builder.AppendLine($"{awwCount} aww(s)");
			if (builder.Length == 0) builder.AppendLine("No tags");
			return builder.ToString().Trim();
		}

		public static Brush TagSidelineBackgroundColor(bool isRootPost)
		{
			return (Brush)(isRootPost ?
				new SolidColorBrush((Windows.UI.Color)Application.Current.Resources["RootPostSidelineColor"])
				: Application.Current.Resources["ApplicationPageBackgroundThemeBrush"]);
		}

		public static Brush UnreadMailMessageIconColor(int unreadCount)
		{
			return GetSystemForegroundOrThemeHighlight(unreadCount > 0);
		}

		public static Brush GetTreeDepthBrush(bool isNew)
		{
			return (Brush)(isNew ?
				Application.Current.Resources["ThemeHighlight"]
				: new SolidColorBrush(Colors.DimGray));
		}

		public static Brush GetSystemForegroundOrThemeHighlight(bool getHighlight)
		{
			return (Brush)(getHighlight ?
				Application.Current.Resources["ThemeHighlight"]
				: new SolidColorBrush((Color)Application.Current.Resources["SystemBaseHighColor"]));
		}

		public static bool AllBooleanTrue(bool a, bool b)
		{
			return AllBooleanTrueParams(a, b);
		}

		public static bool AllBooleanTrueParams(params bool [] values)
		{
			foreach (var v in values)
			{
				if (!v) return false;
			}
			return true;
		}
	}
}
