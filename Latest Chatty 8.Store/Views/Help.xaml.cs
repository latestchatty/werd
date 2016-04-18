using Autofac;
using Latest_Chatty_8.Common;
using Latest_Chatty_8.Settings;
using System;
using System.Reflection;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

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
		private LatestChattySettings settings;
		public override event EventHandler<LinkClickedEventArgs> LinkClicked = delegate { }; //Unused
		public override event EventHandler<ShellMessageEventArgs> ShellMessage = delegate { };

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


		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			var p = e.Parameter as Tuple<IContainer, bool>;
			var container = p.Item1 as IContainer;
			this.settings = container.Resolve<LatestChattySettings>();
			if(p.Item2)
			{
				pivot.SelectedIndex = 1;
			}
		}

		private async void ContactSupportClicked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			(new Microsoft.ApplicationInsights.TelemetryClient()).TrackEvent("HelpSupportClicked");
			await Windows.System.Launcher.LaunchUriAsync(new Uri(string.Format("mailto:support@bit-shift.com?subject={0} v{1}&body=I should really make this SM virus...", this.appName, this.version)));
		}

		private async void ReviewClicked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			(new Microsoft.ApplicationInsights.TelemetryClient()).TrackEvent("HelpReviewClicked");
			await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-windows-store://review/?ProductId=9WZDNCRDKLBD"));
		}

		private async void VersionDoubleClicked(object sender, Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
		{
			var serializedSettings = Newtonsoft.Json.JsonConvert.SerializeObject(this.settings);
			var dialog = new Windows.UI.Popups.MessageDialog("Settings info", "Info");
			dialog.Commands.Add(new Windows.UI.Popups.UICommand("Copy info to clipboard", (a) =>
			{
				var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
				dataPackage.SetText(serializedSettings);
				Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
			}));
			dialog.Commands.Add(new Windows.UI.Popups.UICommand("Close"));
			await dialog.ShowAsync();
		}

		private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var item = e.AddedItems[0] as PivotItem;
			if(item != null)
			{
				var headerText = item.Header as string;
				if (!string.IsNullOrWhiteSpace(headerText) && headerText.Equals("Change History"))
				{
					this.settings.MarkUpdateInfoRead();
				}
			}
		}
	}
}
