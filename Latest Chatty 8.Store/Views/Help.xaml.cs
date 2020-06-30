using Autofac;
using Werd.Common;
using Werd.Settings;

using Microsoft.Services.Store.Engagement;
using Newtonsoft.Json;
using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace Werd.Views
{
	/// <summary>
	/// A basic page that provides characteristics common to most applications.
	/// </summary>
	public sealed partial class Help
	{
		private readonly string _appName;
		private readonly string _version;
		private LatestChattySettings _settings;
		public override event EventHandler<LinkClickedEventArgs> LinkClicked = delegate { }; //Unused
		public override event EventHandler<ShellMessageEventArgs> ShellMessage = delegate { };

		public Help()
		{
			InitializeComponent();
			_appName = Package.Current.DisplayName;
			var version = Package.Current.Id.Version;
			_version = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";

			AppNameTextArea.Text = _appName;
			VersionTextArea.Text = _version;
		}

		public override string ViewTitle => "About";


		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			var p = e.Parameter as Tuple<IContainer, bool>;
			var container = p?.Item1;
			_settings = container.Resolve<LatestChattySettings>();
			if (p != null && p.Item2)
			{
				Pivot.SelectedIndex = 1;
			}
		}

		private async void FeedbackHubClicked(object sender, RoutedEventArgs e)
		{
			if (StoreServicesFeedbackLauncher.IsSupported())
			{
				var launcher = StoreServicesFeedbackLauncher.GetDefault();
				await launcher.LaunchAsync();
			}
			else
			{
				await AppGlobal.DebugLog.AddMessage("HelpSupportClicked");
				await Launcher.LaunchUriAsync(new Uri(string.Format("mailto:support@bit-shift.com?subject={0} v{1}&body=I should really make this SM virus...", _appName, _version)));
			}
		}

		private async void ReviewClicked(object sender, RoutedEventArgs e)
		{
			await AppGlobal.DebugLog.AddMessage("HelpReviewClicked");
			await Launcher.LaunchUriAsync(new Uri("ms-windows-store://review/?ProductId=9WZDNCRDKLBD"));
		}

		private async void VersionDoubleClicked(object sender, DoubleTappedRoutedEventArgs e)
		{
			var serializedSettings = JsonConvert.SerializeObject(_settings);
			var dialog = new MessageDialog("Settings info", "Info");
			dialog.Commands.Add(new UICommand("Copy info to clipboard", a =>
			{
				var dataPackage = new DataPackage();
				dataPackage.SetText(serializedSettings);
				Clipboard.SetContent(dataPackage);
			}));
			dialog.Commands.Add(new UICommand("Close"));
			await dialog.ShowAsync();
		}

		private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var item = e.AddedItems[0] as PivotItem;
			if (item != null)
			{
				var headerText = item.Header as string;
				if (!string.IsNullOrWhiteSpace(headerText) && headerText.Equals("Change History"))
				{
					_settings.MarkUpdateInfoRead();
				}
			}
		}


	}
}
