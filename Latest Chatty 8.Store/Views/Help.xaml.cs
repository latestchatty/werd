using System;
using System.Collections.Generic;
using System.Reflection;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace Latest_Chatty_8.Views
{
	/// <summary>
	/// A basic page that provides characteristics common to most applications.
	/// </summary>
	public sealed partial class Help : ShellView
	{
		private readonly string appName;
		private readonly string version;

		public Help()
		{
			this.InitializeComponent();
			var assemblyName = new AssemblyName(typeof(App).GetTypeInfo().Assembly.FullName);
			this.appName = assemblyName.Name;
			this.version = assemblyName.Version.ToString();

			this.appNameTextArea.Text = this.appName;
			this.versionTextArea.Text = this.version;
		}

		public override string ViewTitle
		{
			get
			{
				return "About";
			}
		}

		async private void ContactSupportClicked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			await Windows.System.Launcher.LaunchUriAsync(new Uri(string.Format("mailto:support@bit-shift.com?subject={0} v{1}&body=I should really make this SM virus...", this.appName, this.version)));
		}

		async private void ReviewClicked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			await (new Windows.UI.Popups.MessageDialog("Launch app store.")).ShowAsync();
		}
	}
}
