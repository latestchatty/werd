using System;
using System.Globalization;
using Windows.UI;
using Windows.UI.Xaml.Data;

namespace Werd.Converters
{
	public class ThreadLifeColorConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (value == null) return Color.FromArgb(255, 0, 0, 0);
			var percent = System.Convert.ToInt32(value, CultureInfo.InvariantCulture);
			if (percent > 100 || percent < 0) { throw new ArgumentOutOfRangeException(nameof(value)); }

			var v = (byte)((percent * 2) + 55);
			return Color.FromArgb(255, v, v, v);
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
