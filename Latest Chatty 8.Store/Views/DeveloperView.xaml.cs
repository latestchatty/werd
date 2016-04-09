using Autofac;
using Latest_Chatty_8.Managers;
using System.Xml.Linq;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Latest_Chatty_8.Views
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class DeveloperView : Page
	{
		private IgnoreManager ignoreManager;

		public DeveloperView()
		{
			this.InitializeComponent();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			var container = e.Parameter as IContainer;
			this.ignoreManager = container.Resolve<IgnoreManager>();
		}

		private void SendTestToast(object sender, RoutedEventArgs e)
		{
			var threadId = this.toastThreadId.Text.Replace("http://www.shacknews.com/chatty?id=", "");
			threadId = threadId.Substring(0, threadId.IndexOf("#") > 0 ? threadId.IndexOf("#") : threadId.Length);
			var toastDoc = new XDocument(
				new XElement("toast", new XAttribute("launch", $"goToPost?postId={threadId}"),
					new XElement("visual",
						new XElement("binding", new XAttribute("template", "ToastText02"),
							new XElement("text", new XAttribute("id", "1"), "Test Toast"),
							new XElement("text", new XAttribute("id", "2"), $"This is a test toast for thread id {threadId}.")
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

			var doc = new Windows.Data.Xml.Dom.XmlDocument();
			doc.LoadXml(toastDoc.ToString());
			var toast = new ToastNotification(doc);
			toast.Tag = "ReplyToUser-Dev";
			var notifier = ToastNotificationManager.CreateToastNotifier();
			notifier.Show(toast);
		}

		private async void ResetIgnoredUsersClicked(object sender, RoutedEventArgs e)
		{
			await this.ignoreManager.RemoveAll();
		}
	}
}
