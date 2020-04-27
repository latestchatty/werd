using System;
using System.Collections.Generic;
using System.Linq;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Autofac;
using Autofac.Core;
using Common;
using Latest_Chatty_8.Common;
using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Managers;
using Latest_Chatty_8.Settings;
using Microsoft.HockeyApp;
using Windows.UI.Popups;
using Windows.ApplicationModel.DataTransfer;
using Newtonsoft.Json;

namespace Latest_Chatty_8.Views
{
	internal class FontSizeCombo
	{
		public string Display { get; set; }
		public int Size { get; set; }
		public FontSizeCombo(string display, int size)
		{
			Display = display;
			Size = size;
		}
	}

	public sealed partial class SettingsView
	{
		public override string ViewTitle => "Settings";

		public override event EventHandler<LinkClickedEventArgs> LinkClicked = delegate { }; //Unused
		public override event EventHandler<ShellMessageEventArgs> ShellMessage = delegate { }; //Unused

		private LatestChattySettings npcSettings;
		private AuthenticationManager npcAuthenticationManager;
		private IgnoreManager _ignoreManager;
		private bool npcIsYoutubeAppInstalled;
		//private bool npcIsInternalYoutubePlayer;

		private LatestChattySettings Settings
		{
			get => npcSettings;
			set => SetProperty(ref npcSettings, value);
		}

		private AuthenticationManager AuthenticationManager
		{
			get => npcAuthenticationManager;
			set => SetProperty(ref npcAuthenticationManager, value);
		}

		private bool IsYoutubeAppInstalled
		{
			get => npcIsYoutubeAppInstalled;
			set => SetProperty(ref npcIsYoutubeAppInstalled, value);
		}

		//private bool IsInternalYoutubePlayer
		//{
		//	get => npcIsInternalYoutubePlayer;
		//	set => SetProperty(ref npcIsInternalYoutubePlayer, value);
		//}

		public SettingsView()
		{
			InitializeComponent();
		}

		protected async override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			var container = e.Parameter as Container;
			Settings = container.Resolve<LatestChattySettings>();
			AuthenticationManager = container.Resolve<AuthenticationManager>();
			_ignoreManager = container.Resolve<IgnoreManager>();
			DataContext = Settings;
			Password.Password = AuthenticationManager.GetPassword();
			IgnoredUsersList.ItemsSource = (await _ignoreManager.GetIgnoredUsers()).OrderBy(a => a);
			IgnoredKeywordList.ItemsSource = (await _ignoreManager.GetIgnoredKeywords()).OrderBy(a => a.Match);
			var notificationManager = container.Resolve<INotificationManager>();
			var notificationUser = await notificationManager.GetUser();
			if(notificationUser != null)
			{
				Settings.NotifyOnNameMention = notificationUser.NotifyOnUserName;
			}

			var fontSizes = new List<FontSizeCombo>();
			for (int i = 8; i < 41; i++)
			{
				fontSizes.Add(new FontSizeCombo(i != 15 ? i.ToString() : i + " (default)", i));
			}
			FontSizeCombo.ItemsSource = fontSizes;
			var selectedFont = fontSizes.Single(s => Math.Abs(s.Size - Settings.FontSize) < .2);
			FontSizeCombo.SelectedItem = selectedFont;
		}

		public void Initialize()
		{
			ValidateUser();
		}

		private void LogOutClicked(object sender, RoutedEventArgs e)
		{
			HockeyClient.Current.TrackEvent("Settings-LogOutClicked");
			AuthenticationManager.LogOut();
			Password.Password = "";
			UserName.Text = "";
		}

		private void PasswordChanged(object sender, RoutedEventArgs e)
		{
			AuthenticationManager.LogOut();
		}

		private void UserNameChanged(object sender, TextChangedEventArgs e)
		{
			AuthenticationManager.LogOut();
		}

		private void ValidateUser()
		{
			AuthenticationManager.LogOut();
		}

		private async void LogInClicked(object sender, RoutedEventArgs e)
		{
			var btn = (Button) sender;
			UserName.IsEnabled = false;
			Password.IsEnabled = false;
			btn.IsEnabled = false;
			HockeyClient.Current.TrackEvent("Settings-LogInClicked");
			if (!await AuthenticationManager.AuthenticateUser(UserName.Text, Password.Password))
			{
				Password.Password = "";
				Password.Focus(FocusState.Programmatic);
			}
			UserName.IsEnabled = true;
			Password.IsEnabled = true;
			btn.IsEnabled = true;
		}

		private void ThemeBackgroundColorChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count != 1) return;
			var selection = (ThemeColorOption)e.AddedItems[0];
			Settings.ThemeName = selection.Name;
		}

		private void ChattyLeftSwipeChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count != 1) return;
			var selection = (ChattySwipeOperation)e.AddedItems[0];
			Settings.ChattyLeftSwipeAction = selection;
		}

		private void ChattyRightSwipeChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count != 1) return;
			var selection = (ChattySwipeOperation)e.AddedItems[0];
			Settings.ChattyRightSwipeAction = selection;
		}

		private async void AddIgnoredUserClicked(object sender, RoutedEventArgs e)
		{
			var b = (Button) sender;
			try
			{
				b.IsEnabled = false;
				if (string.IsNullOrWhiteSpace(IgnoreUserAddTextBox.Text)) return;
				await _ignoreManager.AddIgnoredUser(IgnoreUserAddTextBox.Text);
				IgnoredUsersList.ItemsSource = null;
				IgnoredUsersList.ItemsSource = (await _ignoreManager.GetIgnoredUsers()).OrderBy(a => a);
				IgnoreUserAddTextBox.Text = string.Empty;
				HockeyClient.Current.TrackEvent("AddedIgnoredUser");
			}
			finally
			{
				b.IsEnabled = true;
			}
		}

		private async void RemoveIgnoredUserClicked(object sender, RoutedEventArgs e)
		{
			var b = (Button) sender;
			try
			{
				b.IsEnabled = false;
				if (IgnoredUsersList.SelectedIndex == -1) return;
				var selectedItems = IgnoredUsersList.SelectedItems.Cast<string>();
				foreach (var selected in selectedItems)
				{
					await _ignoreManager.RemoveIgnoredUser(selected);
				}
				IgnoredUsersList.ItemsSource = null;
				IgnoredUsersList.ItemsSource = (await _ignoreManager.GetIgnoredUsers()).OrderBy(a => a);
				HockeyClient.Current.TrackEvent("RemovedIgnoredUser");
			}
			finally
			{
				b.IsEnabled = true;
			}
		}

		private async void AddIgnoredKeywordClicked(object sender, RoutedEventArgs e)
		{
			var b = (Button)sender;
			try
			{
				b.IsEnabled = false;
				var ignoredKeyword = IgnoreKeywordAddTextBox.Text;
				if (string.IsNullOrWhiteSpace(ignoredKeyword)) return;
				await _ignoreManager.AddIgnoredKeyword(new KeywordMatch(ignoredKeyword, WholeWordMatchCheckbox.IsChecked != null && WholeWordMatchCheckbox.IsChecked.Value, CaseSensitiveCheckbox.IsChecked != null && CaseSensitiveCheckbox.IsChecked.Value));
				IgnoredKeywordList.ItemsSource = null;
				IgnoredKeywordList.ItemsSource = (await _ignoreManager.GetIgnoredKeywords()).OrderBy(a => a.Match);
				IgnoreKeywordAddTextBox.Text = string.Empty;
				WholeWordMatchCheckbox.IsChecked = false;
				CaseSensitiveCheckbox.IsChecked = false;
				HockeyClient.Current.TrackEvent("AddedIgnoredKeyword-" + ignoredKeyword);
			}
			finally
			{
				b.IsEnabled = true;
			}
		}

		private async void RemoveIgnoredKeywordClicked(object sender, RoutedEventArgs e)
		{
			var b = (Button)sender;
			try
			{
				b.IsEnabled = false;
				if (IgnoredKeywordList.SelectedIndex == -1) return;
				var selectedItems = IgnoredKeywordList.SelectedItems.Cast<KeywordMatch>();
				foreach (var selected in selectedItems)
				{
					await _ignoreManager.RemoveIgnoredKeyword(selected);
				}
				IgnoredKeywordList.ItemsSource = null;
				IgnoredKeywordList.ItemsSource = (await _ignoreManager.GetIgnoredKeywords()).OrderBy(a => a.Match);
				HockeyClient.Current.TrackEvent("RemovedIgnoredKeyword");
			}
			finally
			{
				b.IsEnabled = true;
			}
		}

		private void FontSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count != 1) return;
			var fontSize = e.AddedItems[0] as FontSizeCombo;
			if (fontSize != null)
			{
				Settings.FontSize = fontSize.Size;
			}
		}

		private void CustomLaunchersExportClicked(object sender, RoutedEventArgs e)
		{
			var package = new DataPackage();
			package.SetText(JsonConvert.SerializeObject(Settings.CustomLaunchers, Formatting.Indented));
			Clipboard.SetContent(package);
		}

		private async void CustomLaunchersImportClicked(object sender, RoutedEventArgs e)
		{
			try
			{
				var data = Clipboard.GetContent();
				var text = await data.GetTextAsync();
				Settings.CustomLaunchers = JsonConvert.DeserializeObject<List<CustomLauncher>>(text);
			}
			catch
			{
				var dialog = new MessageDialog("Unable to import settings. Ensure it's properly formatted and try again.");
				await dialog.ShowAsync();
			}
		}

		private async void CustomLaunchersResetDefaultClicked(object sender, RoutedEventArgs e)
		{
			var dialog = new MessageDialog("Are you sure you want to reset the custom launchers?");
			dialog.Commands.Add(new UICommand("Yes", async _ => {
				Settings.ResetCustomLaunchers();
			}));
			dialog.Commands.Add(new UICommand("Cancel"));
			dialog.CancelCommandIndex = 1;
			dialog.DefaultCommandIndex = 1;
			await dialog.ShowAsync();
		}
	}
}
