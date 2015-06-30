using Autofac.Core;
using Latest_Chatty_8.Shared.Settings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Autofac;

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

		private LatestChattySettings settings;
		private AuthenticaitonManager services;

		public SettingsView ()
		{
            this.InitializeComponent();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			var container = e.Parameter as Container;
			this.settings = container.Resolve<LatestChattySettings>();
			this.services = container.Resolve<AuthenticaitonManager>();
			this.DataContext = this.settings;
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

		private async void LogInClicked(object sender, RoutedEventArgs e)
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
	}
}
