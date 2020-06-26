using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace Latest_Chatty_8.Converters
{
	public class BooleanToStringConverter : BooleanToValueConverter<string> { }
	public class BooleanToNewColorConverter : BooleanToValueConverter<Brush> { }
	public class BooleanToVisibilityConverter : BooleanToValueConverter<Visibility> { }
	public class BooleanToDoubleConverter : BooleanToValueConverter<Double> { }
	public class BooleanToClosedDisplayModeConverter : BooleanToValueConverter<AppBarClosedDisplayMode> { }

	public class BooleanToValueConverter<T> : IValueConverter
	{
		public T FalseValue { get; set; }
		//But not the hardware store... har har.
		public T TrueValue { get; set; }

		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (value == null || !(value is bool)) { return FalseValue; }
			return (bool)value ? TrueValue : FalseValue;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			return value != null && value is T ? value.Equals(TrueValue) : false;
		}
	}
}
