using System;
using System.Globalization;
using Windows.UI.Xaml.Data;

namespace Werd.Converters
{
	public class MultiplicationConverter : IValueConverter
	{
		public double Multiplier { get; set; }

		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (value == null) return 0;
			var v = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
			return v * Multiplier;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
