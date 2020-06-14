using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

namespace Latest_Chatty_8.Controls
{
	public sealed class CustomToggleButton : ToggleButton
	{
		public CustomToggleButton()
		{
			this.DefaultStyleKey = typeof(CustomToggleButton);
		}

		public Brush CheckedBackground
		{
			get { return (Brush)GetValue(CheckedBackgroundProperty); }
			set { SetValue(CheckedBackgroundProperty, value); }
		}

		public static readonly DependencyProperty CheckedBackgroundProperty =
			DependencyProperty.Register("CheckedBackground", typeof(Brush), typeof(CustomToggleButton), new PropertyMetadata(Application.Current.Resources["ToggleButtonBackgroundChecked"]));
	}
}
