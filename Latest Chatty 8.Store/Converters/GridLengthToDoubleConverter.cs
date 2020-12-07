using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Werd.Converters
{
	public class GridLengthToDoubleConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			try
			{
				return new GridLength((double)value);
			}
			catch
			{
				return new GridLength(0d);
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			var v = (GridLength)value;

			if (v.IsAbsolute) return v.Value;
			return -1d;
		}
	}
}
