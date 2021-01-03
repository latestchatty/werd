using Autofac;
using Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Werd.Common;
using Werd.Managers;
using Werd.Settings;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Data.Xml.Dom;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Werd.Views
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class DeveloperView
	{
		private IgnoreManager _ignoreManager;
		private ChattyManager _chattyManager;
		private IContainer _container;
		public override string ViewIcons { get => ""; set { return; } }
		public override string ViewTitle { get => "Developer Stuff - Be careful!"; set { return; } }

		public override event EventHandler<LinkClickedEventArgs> LinkClicked = delegate { };

		public override event EventHandler<ShellMessageEventArgs> ShellMessage;

		private readonly AppSettings Settings = AppGlobal.Settings;

		public DeveloperView()
		{
			InitializeComponent();
		}

		protected override async void OnNavigatedTo(NavigationEventArgs e)
		{
			_container = e.Parameter as IContainer;
			_ignoreManager = _container.Resolve<IgnoreManager>();
			_chattyManager = _container.Resolve<ChattyManager>();
			await RefreshDebugLog().ConfigureAwait(true);
			serviceHost.Text = Locations.ServiceHost;
			base.OnNavigatedTo(e);
		}

		private async Task RefreshDebugLog()
		{
			var messages = await global::Common.DebugLog.GetMessages().ConfigureAwait(true);
			DebugLogList.ItemsSource = messages;
			DebugLogList.UpdateLayout();
			DebugLogList.ScrollIntoView(messages.Last());
		}

		private void SendTestToast(object sender, RoutedEventArgs e)
		{
			var threadId = ToastThreadId.Text.Replace("http://www.shacknews.com/chatty?id=", "", StringComparison.OrdinalIgnoreCase);
			threadId = threadId.Substring(0, threadId.IndexOf("#", StringComparison.Ordinal) > 0 ? threadId.IndexOf("#", StringComparison.Ordinal) : threadId.Length);
			var toastDoc = new XDocument(
				new XElement("toast", new XAttribute("launch", $"goToPost?postId={threadId}"),
					new XElement("visual",
						new XElement("binding", new XAttribute("template", "ToastText02"),
							new XElement("text", new XAttribute("id", "1"), "Test Toast"),
							new XElement("text", new XAttribute("id", "2"), $"This is a test toast for thread id {threadId}. With some longer text so I can test word wrapping or something blah blah blah blah blah sflkasjdfasd;flkjas ftest testing.")
						)
					),
					new XElement("actions",
							new XElement("input", new XAttribute("id", "message"),
								new XAttribute("type", "text"),
								new XAttribute("placeHolderContent", "reply")),
							new XElement("action", new XAttribute("activationType", "background"),
								new XAttribute("content", "reply"),
								new XAttribute("arguments", $"reply={threadId}")/*,
								new XAttribute("imageUri", "Assets/success.png"),
								new XAttribute("hint-inputId", "message")*/)
					)
				)
			);

			var doc = new XmlDocument();
			doc.LoadXml(toastDoc.ToString());
			var toast = new ToastNotification(doc)
			{
				Tag = threadId,
				Group = "ReplyToUser"
			};
			var notifier = ToastNotificationManager.CreateToastNotifier();
			notifier.Show(toast);
		}

		private async void ResetIgnoredUsersClicked(object sender, RoutedEventArgs e)
		{
			await _ignoreManager.RemoveAllUsers().ConfigureAwait(false);
		}

		private async void ResetIgnoredKeywordsClicked(object sender, RoutedEventArgs e)
		{
			await _ignoreManager.RemoveAllKeywords().ConfigureAwait(false);
		}

		private void LoadThreadById(object sender, RoutedEventArgs e)
		{
			if (int.TryParse(ToastThreadId.Text, out int threadId))
			{
				Frame.Navigate(typeof(SingleThreadView), new Tuple<IContainer, int, int>(_container, threadId, threadId));
			}
		}

		private void ThrowException(object sender, RoutedEventArgs e)
		{
			throw new Exception("Testing exception.");
		}

		private async void PrintNotificationHistory(object sender, RoutedEventArgs e)
		{
			var history = ToastNotificationManager.History.GetHistory();
			foreach (var historyItem in history)
			{
				await global::Common.DebugLog.AddMessage($"T: {historyItem.Tag} G: {historyItem.Group}").ConfigureAwait(false);
			}
		}

		private void CopyDebugLogClicked(object sender, RoutedEventArgs e)
		{
			var dataPackage = new DataPackage();
			var logItems = global::Common.DebugLog.Messages.ToArray();
			var builder = new StringBuilder();
			foreach (var item in logItems)
			{
				builder.AppendLine(item);
			}
			dataPackage.SetText(builder.ToString());
			Clipboard.SetContent(dataPackage);
			ShellMessage?.Invoke(this, new ShellMessageEventArgs("Log copied to clipboard."));
		}

		private void SetServiceHostClicked(object sender, RoutedEventArgs e)
		{
			_chattyManager.StopAutoChattyRefresh();
			Locations.SetServiceHost(serviceHost.Text);
			_chattyManager.ScheduleImmediateFullChattyRefresh();
			_chattyManager.StartAutoChattyRefresh();
		}

		private void SetWindowPosition(object sender, RoutedEventArgs e)
		{
			ApplicationView.GetForCurrentView().TryResizeView(new Windows.Foundation.Size(resizeX.Value, resizeY.Value));
		}

		private async void ExportUserNotesClicked(object sender, RoutedEventArgs e)
		{
			var package = new DataPackage();
			var userNotesToExport = Newtonsoft.Json.JsonConvert.SerializeObject(await Settings.GetUserNotes(), Newtonsoft.Json.Formatting.Indented);
			package.SetText(userNotesToExport);
			Clipboard.SetContent(package);
		}

		private async void ImportUserNotesClicked(object sender, RoutedEventArgs e)
		{
			var data = Clipboard.GetContent();
			var userNotes = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(await data.GetTextAsync());
			if(userNotes != null)
			{
				Settings.SetUserNotes(userNotes);
			}
		}

		private async void RefreshDebugLogClicked(object sender, RoutedEventArgs e)
		{
			await RefreshDebugLog().ConfigureAwait(true);
		}

		private void ClearUserNotesClicked(object sender, RoutedEventArgs e)
		{
			Settings.SetUserNotes(new Dictionary<string, string>());
		}
	}
}
