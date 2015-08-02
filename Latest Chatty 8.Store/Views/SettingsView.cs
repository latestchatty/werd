using Autofac;
using Autofac.Core;
using Latest_Chatty_8.Common;
using Latest_Chatty_8.Settings;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Latest_Chatty_8.Views
{
	public sealed partial class SettingsView : ShellView
	{
		public override string ViewTitle
		{
			get
			{
				return "Settings";
			}
		}

		private LatestChattySettings npcSettings;
		private AuthenticationManager services;

		private LatestChattySettings Settings
		{
			get { return this.npcSettings; }
			set { this.SetProperty(ref this.npcSettings, value); }
		}

		public SettingsView ()
		{
            this.InitializeComponent();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			var container = e.Parameter as Container;
			this.Settings = container.Resolve<LatestChattySettings>();
			this.services = container.Resolve<AuthenticationManager>();
			this.DataContext = this.Settings; //TODO: Change bindings to use full path
			this.loginGrid.DataContext = this.services;
			this.password.Password = this.services.GetPassword();
        }
		
		public void Initialize()
		{
			this.ValidateUser(false);
		}

		private void LogOutClicked(object sender, RoutedEventArgs e)
		{
			this.services.LogOut();
			this.password.Password = "";
			this.userName.Text = "";
		}

		private void PasswordChanged(object sender, RoutedEventArgs e)
		{
			this.services.LogOut();
		}

		private void UserNameChanged(object sender, TextChangedEventArgs e)
		{
			this.services.LogOut();
		}

		private void ValidateUser(bool updateCloudSettings)
		{
			this.services.LogOut();
		}

		async private void LogInClicked(object sender, RoutedEventArgs e)
		{
			var btn = sender as Button;
			this.userName.IsEnabled = false;
			this.password.IsEnabled = false;
			btn.IsEnabled = false;
			if(!await this.services.AuthenticateUser(this.userName.Text, this.password.Password))
			{
				this.password.Password = "";
				this.password.Focus(FocusState.Programmatic);
			}
			this.userName.IsEnabled = true;
			this.password.IsEnabled = true;
			btn.IsEnabled = true;
		}

		private void ThemeBackgroundColorChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count != 1) return;
			var selection = (ThemeColorOption)e.AddedItems[0];
			this.Settings.ThemeName = selection.Name;
		}
	}
}
