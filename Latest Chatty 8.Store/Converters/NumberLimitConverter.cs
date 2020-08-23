using System;
using System.Globalization;
using Windows.UI.Xaml.Data;

namespace Werd.Converters
{
	public class NumberLimitConverter : IValueConverter
	{
		public int Limit { get; set; }

		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (value == null) return "0";
			var v = System.Convert.ToInt32(value, CultureInfo.InvariantCulture);
			return v > Limit ? $"{Limit}+" : v.ToString(CultureInfo.InvariantCulture);
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
