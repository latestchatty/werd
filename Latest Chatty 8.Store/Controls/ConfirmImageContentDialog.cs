using Windows.UI.Xaml.Media.Imaging;

namespace Latest_Chatty_8.Controls
{
	public sealed partial class ConfirmImageContentDialog
	{
		public ConfirmImageContentDialog(BitmapImage image)
		{
			InitializeComponent();
			ConfirmImage.Source = image;
		}
	}
}
