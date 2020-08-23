using System;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Werd.Views
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class CrashHandler : Page
	{
		public CrashHandler(Exception e)
		{
			this.InitializeComponent();
			this.exceptionBox.Text = e.ToString();
		}

		private async void AddDebugLogClicked(object sender, RoutedEventArgs e)
		{
			var messages = await AppGlobal.DebugLog.GetMessages().ConfigureAwait(true);
			this.exceptionBox.Text += Environment.NewLine + string.Join(Environment.NewLine, messages);
		}

		private void CopyToClipboard(object sender, RoutedEventArgs e)
		{
			var dp = new DataPackage();
			dp.SetText(this.exceptionBox.Text);
			Clipboard.SetContent(dp);
		}
	}
}
