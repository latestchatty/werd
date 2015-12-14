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

		public override event EventHandler<LinkClickedEventArgs> LinkClicked;
		public override event EventHandler<ShellMessageEventArgs> ShellMessage;

		private LatestChattySettings npcSettings;
		private AuthenticationManager npcAuthenticationManager;
		private ChattySwipeOperation[] npcChattySwipeOperations;
		private bool npcIsYoutubeAppInstalled;

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

		private bool IsYoutubeAppInstalled
		{
			get { return this.npcIsYoutubeAppInstalled; }
			set { this.SetProperty(ref this.npcIsYoutubeAppInstalled, value); }
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
			(new Microsoft.ApplicationInsights.TelemetryClient()).TrackEvent("Settings-LogOutClicked");
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
			(new Microsoft.ApplicationInsights.TelemetryClient()).TrackEvent("Settings-LogInClicked");
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

		private void ChattyLeftSwipeChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count != 1) return;
			var selection = (ChattySwipeOperation)e.AddedItems[0];
			this.Settings.ChattyLeftSwipeAction = selection;
		}

		private void ChattyRightSwipeChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count != 1) return;
			var selection = (ChattySwipeOperation)e.AddedItems[0];
			this.Settings.ChattyRightSwipeAction = selection;
		}

		async private void ExternalYoutubeAppChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count != 1) return;
			var selection = (ExternalYoutubeApp)e.AddedItems[0];
			this.Settings.ExternalYoutubeApp = selection;
			if (this.Settings.ExternalYoutubeApp.Type != ExternalYoutubeAppType.Browser)
			{
				var colonLocation = selection.UriFormat.IndexOf(":");
				var protocol = selection.UriFormat.Substring(0, colonLocation + 1);
				var support = await Windows.System.Launcher.QueryUriSupportAsync(new Uri(protocol), Windows.System.LaunchQuerySupportType.Uri);
				this.IsYoutubeAppInstalled = (support != Windows.System.LaunchQuerySupportStatus.AppNotInstalled) && (support != Windows.System.LaunchQuerySupportStatus.NotSupported);
			}
			else
			{
				this.IsYoutubeAppInstalled = true;
			}
        }

		async private void InstallYoutubeApp(object sender, RoutedEventArgs e)
		{
			var colonLocation = this.Settings.ExternalYoutubeApp.UriFormat.IndexOf(":");
			var protocol = this.Settings.ExternalYoutubeApp.UriFormat.Substring(0, colonLocation);
			await Windows.System.Launcher.LaunchUriAsync(new Uri($"ms-windows-store://assoc/?Protocol={protocol}"));
        }
	}
}
