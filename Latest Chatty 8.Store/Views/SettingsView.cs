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
		private AuthenticationManager npcAuthenticationManager;

		private LatestChattySettings Settings
		{
			get { return this.npcSettings; }
			set { this.SetProperty(ref this.npcSettings, value); }
		}

		private AuthenticationManager AuthenticationManager
		{
			get { return this.npcAuthenticationManager; }
			set { this.SetProperty(ref this.npcAuthenticationManager, value); }
		}

		public SettingsView()
		{
			this.InitializeComponent();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			var container = e.Parameter as Container;
			this.Settings = container.Resolve<LatestChattySettings>();
			this.AuthenticationManager = container.Resolve<AuthenticationManager>();
			this.DataContext = this.Settings;
			this.password.Password = this.AuthenticationManager.GetPassword();
		}

		public void Initialize()
		{
			this.ValidateUser(false);
		}

		private void LogOutClicked(object sender, RoutedEventArgs e)
		{
			this.AuthenticationManager.LogOut();
			this.password.Password = "";
			this.userName.Text = "";
		}

		private void PasswordChanged(object sender, RoutedEventArgs e)
		{
			this.AuthenticationManager.LogOut();
		}

		private void UserNameChanged(object sender, TextChangedEventArgs e)
		{
			this.AuthenticationManager.LogOut();
		}

		private void ValidateUser(bool updateCloudSettings)
		{
			this.AuthenticationManager.LogOut();
		}

		async private void LogInClicked(object sender, RoutedEventArgs e)
		{
			var btn = sender as Button;
			this.userName.IsEnabled = false;
			this.password.IsEnabled = false;
			btn.IsEnabled = false;
			if (!await this.AuthenticationManager.AuthenticateUser(this.userName.Text, this.password.Password))
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
