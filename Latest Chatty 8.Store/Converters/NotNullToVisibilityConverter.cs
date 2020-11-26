using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Werd.Converters
{
	public sealed class NotNullToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			return (value is null) ? Visibility.Visible : Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
