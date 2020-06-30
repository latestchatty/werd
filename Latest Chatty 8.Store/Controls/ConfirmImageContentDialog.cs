using Windows.UI.Xaml.Media.Imaging;

namespace Werd.Controls
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
