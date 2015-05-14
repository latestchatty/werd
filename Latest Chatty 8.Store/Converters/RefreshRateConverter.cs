using System;
using Windows.UI.Xaml.Data;

namespace Latest_Chatty_8.Shared.Converters
{
	class RefreshRateConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (value == null) return "Unknown";
			var v = System.Convert.ToInt32(value);
			return v == 0 ? "Live - May reduce battery life." : v.ToString() + " Seconds";
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
