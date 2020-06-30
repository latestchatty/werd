using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Werd.Converters
{
	/// <summary>
	/// Value converter that translates values equal to zero to <see cref="Visibility.Visible"/> and not zero
	/// <see cref="Visibility.Collapsed"/>.
	/// </summary>
	public sealed class EqualsZeroVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			return (value is int && (int)value == 0) ? Visibility.Visible : Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
