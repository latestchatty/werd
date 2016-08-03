using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Latest_Chatty_8.Common
{
	public static class VisualTreeHelpers
	{
		public static T FindFirstControlNamed<T>(this DependencyObject parent, string name)
			where T : FrameworkElement
		{

			return parent.AllChildren<T>().FirstOrDefault(c => c.Name == name) as T;
		}
		public static IEnumerable<T> FindControlsNamed<T>(this DependencyObject parent, string name)
			where T : FrameworkElement
		{

			return parent.AllChildren<T>().Where(c => c.Name == name).Select(c1 => c1 as T);
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
	}
}
