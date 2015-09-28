using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Latest_Chatty_8.Converters
{
	public class NumberLimitConverter : IValueConverter
	{
		public int Limit { get; set; }

		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (value == null) return "0";
			var v = System.Convert.ToInt32(value);
			return v > this.Limit ? string.Format("{0}+", this.Limit) : v.ToString();
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
