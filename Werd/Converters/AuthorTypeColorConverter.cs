using System;
using Werd.DataModel;
using Windows.UI;
using Windows.UI.Xaml;
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
					return new SolidColorBrush(App.Current.RequestedTheme == ApplicationTheme.Light ? Color.FromArgb(255, 85, 0, 255) : Color.FromArgb(255, 147, 112, 219));
				case AuthorType.ThreadOp:
					return new SolidColorBrush(App.Current.RequestedTheme == ApplicationTheme.Light ? Color.FromArgb(255, 0, 255, 72) : Color.FromArgb(255, 106, 255, 148));
				case AuthorType.Self:
					return new SolidColorBrush(App.Current.RequestedTheme == ApplicationTheme.Light ? Color.FromArgb(255, 0, 170, 255) : Color.FromArgb(255, 102, 204, 255));
				default:
					return new SolidColorBrush(App.Current.RequestedTheme == ApplicationTheme.Light ? Color.FromArgb(255, 255, 145, 0) : Color.FromArgb(255, 255, 186, 0));
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			return AuthorType.Default;
		}
	}
}
