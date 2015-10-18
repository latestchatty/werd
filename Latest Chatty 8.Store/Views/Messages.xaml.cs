using Autofac;
using Latest_Chatty_8.Common;
using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Settings;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Latest_Chatty_8.Views
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class Messages : ShellView
	{
		public override string ViewTitle
		{
			get
			{
				return "Messages";
			}
		}

		private int currentPage = 1;

		private List<Message> npcMessages;
		public List<Message> DisplayMessages
		{
			get { return this.npcMessages; }
			set { this.SetProperty(ref this.npcMessages, value); }
		}

		private bool npcCanGoBack;
		public bool CanGoBack
		{
			get { return this.npcCanGoBack; }
			set { this.SetProperty(ref this.npcCanGoBack, value); }
		}

		private bool npcCanGoForward;
		public bool CanGoForward
		{
			get { return this.npcCanGoForward; }
			set { this.SetProperty(ref this.npcCanGoForward, value); }
		}

		private bool npcLoadingMessages;
		public bool LoadingMessages
		{
			get { return this.npcLoadingMessages; }
			set { this.SetProperty(ref this.npcLoadingMessages, value); }
		}

		private bool npcCanSendNewMessage;
		public bool CanSendNewMessage
		{
			get { return this.npcCanSendNewMessage; }
			set { this.SetProperty(ref this.npcCanSendNewMessage, value); }
		}

		private MessageManager messageManager;
		private LatestChattySettings settings;

		public Messages()
		{
			this.InitializeComponent();
		}

		async protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			var container = e.Parameter as IContainer;
			this.messageManager = container.Resolve<MessageManager>();
			this.settings = container.Resolve<LatestChattySettings>();
			CoreWindow.GetForCurrentThread().KeyDown += ShortcutKeyDown;
			CoreWindow.GetForCurrentThread().KeyUp += ShortcutKeyUp;
			await this.LoadThreads();
		}

		private bool ctrlDown = false;
		private bool disableShortcutKeys = false;
		async private void ShortcutKeyDown(CoreWindow sender, KeyEventArgs args)
		{
			if (this.disableShortcutKeys)
			{
				System.Diagnostics.Debug.WriteLine("Suppressed keypress event.");
				return;
			}
			switch (args.VirtualKey)
			{
				case VirtualKey.Control:
					ctrlDown = true;
					break;
				case VirtualKey.F5:
					(new Microsoft.ApplicationInsights.TelemetryClient()).TrackEvent("Message-F5Pressed");
					await this.LoadThreads();
					break;
				case VirtualKey.J:
					this.currentPage--;
					(new Microsoft.ApplicationInsights.TelemetryClient()).TrackEvent("Message-JPressed");
					await this.LoadThreads();
					break;
				case VirtualKey.K:
					this.currentPage++;
					(new Microsoft.ApplicationInsights.TelemetryClient()).TrackEvent("Message-KPressed");
					await this.LoadThreads();
					break;
				case VirtualKey.A:
					(new Microsoft.ApplicationInsights.TelemetryClient()).TrackEvent("Message-APressed");
					this.messagesList.SelectedIndex = Math.Max(this.messagesList.SelectedIndex - 1, 0);
					break;
				case VirtualKey.Z:
					(new Microsoft.ApplicationInsights.TelemetryClient()).TrackEvent("Message-ZPressed");
					this.messagesList.SelectedIndex = Math.Min(this.messagesList.SelectedIndex + 1, this.messagesList.Items.Count - 1);
					break;
				case VirtualKey.D:
					var msg = this.messagesList.SelectedItem as Message;
					if (msg == null) return;
					(new Microsoft.ApplicationInsights.TelemetryClient()).TrackEvent("Message-DPressed");
					await this.DeleteMessage(msg);
					break;
				default:
					break;
			}
			System.Diagnostics.Debug.WriteLine("Keypress event for {0}", args.VirtualKey);
		}

		private void ShortcutKeyUp(CoreWindow sender, KeyEventArgs args)
		{
			if (this.disableShortcutKeys)
			{
				System.Diagnostics.Debug.WriteLine("Suppressed keypress event.");
				return;
			}
			switch (args.VirtualKey)
			{
				case VirtualKey.Control:
					ctrlDown = false;
					break;
				case VirtualKey.N:
					if (ctrlDown)
					{
						(new Microsoft.ApplicationInsights.TelemetryClient()).TrackEvent("Message-CtrlNPressed");
						this.newMessageButton.IsChecked = true;
					}
					break;
				case VirtualKey.R:
					this.showReply.IsChecked = true;
					break;
				default:
					break;
			}
		}

		protected override void OnNavigatedFrom(NavigationEventArgs e)
		{
			CoreWindow.GetForCurrentThread().KeyDown -= ShortcutKeyDown;
			CoreWindow.GetForCurrentThread().KeyUp -= ShortcutKeyUp;
			this.messageWebView.Close();
		}

		async private void PreviousPageClicked(object sender, RoutedEventArgs e)
		{
			this.currentPage--;
			(new Microsoft.ApplicationInsights.TelemetryClient()).TrackEvent("Message-PreviousPageClicked");
			await this.LoadThreads();
		}

		async private void NextPageClicked(object sender, RoutedEventArgs e)
		{
			this.currentPage++;
			(new Microsoft.ApplicationInsights.TelemetryClient()).TrackEvent("Message-NextPageClicked");
			await this.LoadThreads();
		}

		async private void RefreshClicked(object sender, RoutedEventArgs e)
		{
			(new Microsoft.ApplicationInsights.TelemetryClient()).TrackEvent("Message-RefreshClicked");
			await this.LoadThreads();
		}

		async private void DeleteMessageClicked(object sender, RoutedEventArgs e)
		{
			var msg = this.messagesList.SelectedItem as Message;
			if (msg == null) return;
			(new Microsoft.ApplicationInsights.TelemetryClient()).TrackEvent("Message-DeleteMessageClicked");
			await this.DeleteMessage(msg);
		}

		async private void SubmitPostButtonClicked(object sender, RoutedEventArgs e)
		{
			var msg = this.messagesList.SelectedItem as Message;
			if (msg == null) return;
			var btn = sender as Button;
			try
			{
				btn.IsEnabled = false;
				//If we're replying to a sent message, we want to send to the person we sent it to, not to ourselves.
				var viewingSentMessage = ((ComboBoxItem)this.mailboxCombo.SelectedItem).Tag.ToString().Equals("sent", StringComparison.OrdinalIgnoreCase);
				var success = await this.messageManager.SendMessage(viewingSentMessage ? msg.To : msg.From, string.Format("Re: {0}", msg.Subject), this.replyTextBox.Text);
				(new Microsoft.ApplicationInsights.TelemetryClient()).TrackEvent("Message-SentReplyMessage");
				if (success)
				{
					this.showReply.IsChecked = false;
					this.disableShortcutKeys = false;
					this.Focus(FocusState.Programmatic);
					if (viewingSentMessage)
					{
						await this.LoadThreads();
					}
				}
				else
				{
					var dlg = new Windows.UI.Popups.MessageDialog("Failed to send message.");
					await dlg.ShowAsync();
				}
			}
			finally
			{
				btn.IsEnabled = true;
			}
		}

		private void ShowReplyChecked(object sender, RoutedEventArgs e)
		{
			var msg = this.messagesList.SelectedItem as Message;
			if (msg == null) return;
			this.disableShortcutKeys = true;
			this.replyTextBox.Text = string.Format("{2}{2}On {0} {1} wrote: {2} {3}", msg.Date, msg.From, Environment.NewLine, msg.Body);
			this.replyTextBox.Focus(FocusState.Programmatic);
		}

		private void ShowReplyUnchecked(object sender, RoutedEventArgs e)
		{
			this.disableShortcutKeys = false;
		}

		async private void SendNewMessageClicked(object sender, RoutedEventArgs e)
		{
			var btn = sender as Button;
			try
			{
				btn.IsEnabled = false;
				var success = await this.messageManager.SendMessage(this.toTextBox.Text, this.subjectTextBox.Text, this.newMessageTextBox.Text);
				(new Microsoft.ApplicationInsights.TelemetryClient()).TrackEvent("Message-SentNewMessage");
				if (success)
				{
					this.newMessageButton.IsChecked = false;
					this.disableShortcutKeys = false;
					this.Focus(FocusState.Programmatic);
					if (((ComboBoxItem)this.mailboxCombo.SelectedItem).Tag.ToString().Equals("sent", StringComparison.OrdinalIgnoreCase))
					{
						await this.LoadThreads();
					}
				}
				else
				{
					var dlg = new Windows.UI.Popups.MessageDialog("Failed to send message.");
					await dlg.ShowAsync();
				}
			}
			finally
			{
				btn.IsEnabled = true;
			}
		}

		private void ShowNewMessageButtonChecked(object sender, RoutedEventArgs e)
		{
			this.disableShortcutKeys = true;
			this.toTextBox.Text = string.Empty;
			this.subjectTextBox.Text = string.Empty;
			this.newMessageTextBox.Text = string.Empty;
			this.toTextBox.Focus(FocusState.Programmatic);
		}

		private void ShowNewMessageButtonUnchecked(object sender, RoutedEventArgs e)
		{
			this.disableShortcutKeys = false;
		}

		private void NewMessageTextChanged(object sender, TextChangedEventArgs e)
		{
			this.CanSendNewMessage = !string.IsNullOrWhiteSpace(this.toTextBox.Text) &&
				!string.IsNullOrWhiteSpace(this.subjectTextBox.Text) &&
				!string.IsNullOrWhiteSpace(this.newMessageTextBox.Text);
		}

		async private Task LoadThreads()
		{
			if (this.messageManager == null) return;

			this.LoadingMessages = true;

			this.CanGoBack = false;
			this.CanGoForward = false;

			if (this.currentPage <= 1) this.currentPage = 1;

			var folder = ((ComboBoxItem)this.mailboxCombo.SelectedItem).Tag.ToString();
			(new Microsoft.ApplicationInsights.TelemetryClient()).TrackEvent("Message-Load" + folder);
			var result = await this.messageManager.GetMessages(this.currentPage, folder);

			this.DisplayMessages = result.Item1;

			this.CanGoBack = this.currentPage > 1;
			this.CanGoForward = this.currentPage < result.Item2;

			this.LoadingMessages = false;
		}

		async private void MessageSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			this.showReply.IsChecked = false;
			if (e.AddedItems.Count != 1) return;

			var message = e.AddedItems[0] as Message;
			if (message != null)
			{
				var embedResult = EmbedHelper.RewriteEmbeds(message.Body);
				this.messageWebView.LoadPost(WebBrowserHelper.GetPostHtml(embedResult.Item1, embedResult.Item2), this.settings);
				//Mark read.
				await this.messageManager.MarkMessageRead(message);
			}
		}

		async private void FolderSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			await this.LoadThreads();
		}

		async private Task DeleteMessage(Message msg)
		{
			this.deleteButton.IsEnabled = false;
			try
			{
				if (await this.messageManager.DeleteMessage(msg, ((ComboBoxItem)this.mailboxCombo.SelectedItem).Tag.ToString()))
				{
					await this.LoadThreads();
				}
			}
			finally
			{
				this.deleteButton.IsEnabled = true;
			}
		}
	}
}
