using Latest_Chatty_8.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace Latest_Chatty_8.Shared.Converters
{
	public class AuthorTypeColorConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			//#ffba00
			if(!(value is AuthorType))
			{
				return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 186, 0));
			}
			var v = (AuthorType)value;
			switch(v)
			{
				case AuthorType.Shacknews:
					return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 147, 112, 219));
				case AuthorType.ThreadOP:
					return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 106, 255, 148));
				case AuthorType.Self:
					return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 102, 204, 255));
				default:
					return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 186, 0));
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			return AuthorType.Default;
		}
	}
}
