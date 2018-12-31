using System;
using Windows.UI.Xaml.Data;

namespace Latest_Chatty_8.Converters
{
	class RefreshRateValueConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (value == null) return 0.0;
			return System.Convert.ToDouble(value);
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			if (value == null) return 0;
			return System.Convert.ToInt32(value);
		}
	}
}
