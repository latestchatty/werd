using Autofac;
using Autofac.Core;
using Latest_Chatty_8.Common;
using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Managers;
using Latest_Chatty_8.Settings;
using Microsoft.ApplicationInsights;
using System;
using System.Linq;
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

		public override event EventHandler<LinkClickedEventArgs> LinkClicked = delegate { }; //Unused
		public override event EventHandler<ShellMessageEventArgs> ShellMessage = delegate { }; //Unused

		private LatestChattySettings npcSettings;
		private AuthenticationManager npcAuthenticationManager;
		private IgnoreManager ignoreManager;
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

		protected async override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			var container = e.Parameter as Container;
			this.Settings = container.Resolve<LatestChattySettings>();
			this.AuthenticationManager = container.Resolve<AuthenticationManager>();
			this.ignoreManager = container.Resolve<IgnoreManager>();
			this.DataContext = this.Settings;
			this.password.Password = this.AuthenticationManager.GetPassword();
			this.ignoredUsersList.ItemsSource = (await this.ignoreManager.GetIgnoredUsers()).OrderBy(a => a);
			this.ignoredKeywordList.ItemsSource = (await this.ignoreManager.GetIgnoredKeywords()).OrderBy(a => a.Match);
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

		private async void AddIgnoredUserClicked(object sender, RoutedEventArgs e)
		{
			var b = sender as Button;
			try
			{
				b.IsEnabled = false;
				if (string.IsNullOrWhiteSpace(this.ignoreUserAddTextBox.Text)) return;
				await this.ignoreManager.AddIgnoredUser(this.ignoreUserAddTextBox.Text);
				this.ignoredUsersList.ItemsSource = null;
				this.ignoredUsersList.ItemsSource = (await this.ignoreManager.GetIgnoredUsers()).OrderBy(a => a);
				this.ignoreUserAddTextBox.Text = string.Empty;
				(new TelemetryClient()).TrackEvent("AddedIgnoredUser");
			}
			finally
			{
				b.IsEnabled = true;
			}
		}

		private async void RemoveIgnoredUserClicked(object sender, RoutedEventArgs e)
		{
			var b = sender as Button;
			try
			{
				b.IsEnabled = false;
				if (this.ignoredUsersList.SelectedIndex == -1) return;
				var selectedItems = this.ignoredUsersList.SelectedItems.Cast<string>();
				foreach (var selected in selectedItems)
				{
					await this.ignoreManager.RemoveIgnoredUser(selected);
				}
				this.ignoredUsersList.ItemsSource = null;
				this.ignoredUsersList.ItemsSource = (await this.ignoreManager.GetIgnoredUsers()).OrderBy(a => a);
				(new TelemetryClient()).TrackEvent("RemovedIgnoredUser");
			}
			finally
			{
				b.IsEnabled = true;
			}
		}

		private async void AddIgnoredKeywordClicked(object sender, RoutedEventArgs e)
		{
			var b = sender as Button;
			try
			{
				b.IsEnabled = false;
				var ignoredKeyword = this.ignoreKeywordAddTextBox.Text;
				if (string.IsNullOrWhiteSpace(ignoredKeyword)) return;
				await this.ignoreManager.AddIgnoredKeyword(new DataModel.KeywordMatch(ignoredKeyword, this.wholeWordMatchCheckbox.IsChecked.Value, this.caseSensitiveCheckbox.IsChecked.Value));
				this.ignoredKeywordList.ItemsSource = null;
				this.ignoredKeywordList.ItemsSource = (await this.ignoreManager.GetIgnoredKeywords()).OrderBy(a => a.Match);
				this.ignoreKeywordAddTextBox.Text = string.Empty;
				this.wholeWordMatchCheckbox.IsChecked = false;
				this.caseSensitiveCheckbox.IsChecked = false;
				(new TelemetryClient()).TrackEvent("AddedIgnoredKeyword-" + ignoredKeyword);
			}
			finally
			{
				b.IsEnabled = true;
			}
		}

		private async void RemoveIgnoredKeywordClicked(object sender, RoutedEventArgs e)
		{
			var b = sender as Button;
			try
			{
				b.IsEnabled = false;
				if (this.ignoredKeywordList.SelectedIndex == -1) return;
				var selectedItems = this.ignoredKeywordList.SelectedItems.Cast<KeywordMatch>();
				foreach (var selected in selectedItems)
				{
					await this.ignoreManager.RemoveIgnoredKeyword(selected);
				}
				this.ignoredKeywordList.ItemsSource = null;
				this.ignoredKeywordList.ItemsSource = (await this.ignoreManager.GetIgnoredKeywords()).OrderBy(a => a.Match);
				(new TelemetryClient()).TrackEvent("RemovedIgnoredKeyword");
			}
			finally
			{
				b.IsEnabled = true;
			}
		}
	}
}
