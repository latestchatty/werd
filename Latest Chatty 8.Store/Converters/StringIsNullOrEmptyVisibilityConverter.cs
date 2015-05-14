using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Latest_Chatty_8.Shared.Converters
{
	public class StringIsNullOrEmptyVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			var s = value as string;
			return string.IsNullOrWhiteSpace(s) ? Visibility.Collapsed : Visibility.Visible;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			return null;
		}
	}

	public class NotStringIsNullOrEmptyVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			var s = value as string;
			return !string.IsNullOrWhiteSpace(s) ? Visibility.Collapsed : Visibility.Visible;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			return null;
		}
	}
}
