using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ApplicationSettings;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Latest_Chatty_8.Settings
{
	public sealed partial class WideSettings : UserControl
	{
		private LatestChattySettings settings;

		public WideSettings(LatestChattySettings settings)
		{
			this.InitializeComponent();
			this.DataContext = settings;
			this.settings = settings;
			this.password.Password = this.settings.Password;
		}

		private void LogOutClicked(object sender, RoutedEventArgs e)
		{
			this.settings.Username = this.settings.Password = this.password.Password = string.Empty;
		}

		private void PasswordChanged(object sender, RoutedEventArgs e)
		{
			this.settings.Password = ((PasswordBox)sender).Password;
		}

		private void MySettingsBackClicked(object sender, RoutedEventArgs e)
		{
			if (this.Parent.GetType() == typeof(Popup))
			{
				((Popup)this.Parent).IsOpen = false;
			}
			SettingsPane.Show();
		}
	}
}
