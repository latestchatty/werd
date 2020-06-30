using System;
using Werd.DataModel;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace Werd.Converters
{
	public class AuthorTypeColorConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			//#ffba00
			if (!(value is AuthorType))
			{
				return new SolidColorBrush(Color.FromArgb(255, 255, 186, 0));
			}
			var v = (AuthorType)value;
			switch (v)
			{
				case AuthorType.Shacknews:
					return new SolidColorBrush(Color.FromArgb(255, 147, 112, 219));
				case AuthorType.ThreadOp:
					return new SolidColorBrush(Color.FromArgb(255, 106, 255, 148));
				case AuthorType.Self:
					return new SolidColorBrush(Color.FromArgb(255, 102, 204, 255));
				default:
					return new SolidColorBrush(Color.FromArgb(255, 255, 186, 0));
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			return AuthorType.Default;
		}
	}
}
