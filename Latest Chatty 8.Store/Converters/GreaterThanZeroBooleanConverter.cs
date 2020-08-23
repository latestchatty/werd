using System;
using Windows.UI.Xaml.Data;

namespace Werd.Converters
{
	public sealed class GreaterThanZeroBooleanConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			return (value is int && (int)value > 0);
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
