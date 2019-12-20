using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Latest_Chatty_8.Converters
{
	public class BoolToGridRowHeightConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			return ((bool)value == true) ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language) => null;
	}
}
