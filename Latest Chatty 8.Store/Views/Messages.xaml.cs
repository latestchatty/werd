using Autofac;
using Latest_Chatty_8.Common;
using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Shared;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
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

		public Messages()
		{
			this.InitializeComponent();
		}

		async protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			var continer = e.Parameter as IContainer;
			this.messageManager = continer.Resolve<MessageManager>();
			this.messageWebView.ScriptNotify += ScriptNotify;
			this.messageWebView.NavigationCompleted += NavigationCompleted;
			this.messageWebView.NavigationStarting += NavigatingWebView;
			this.SizeChanged += (async (o,a) => await ResizeWebView(this.messageWebView));

			await this.LoadThreads();
		}
		
		protected override void OnNavigatedFrom(NavigationEventArgs e)
		{
			this.messageWebView.ScriptNotify -= ScriptNotify;
			this.messageWebView.NavigationCompleted -= NavigationCompleted;
			this.messageWebView.NavigationStarting -= NavigatingWebView;
		}

		async private void PreviousPageClicked(object sender, RoutedEventArgs e)
		{
			this.currentPage--;
			await this.LoadThreads();
		}

		async private void NextPageClicked(object sender, RoutedEventArgs e)
		{
			this.currentPage++;
			await this.LoadThreads();
		}

		async private void RefreshClicked(object sender, RoutedEventArgs e)
		{
			await this.LoadThreads();
		}
	
		async private void DeleteMessageClicked(object sender, RoutedEventArgs e)
		{
			var msg = this.messagesList.SelectedItem as Message;
			if (msg == null) return;
			var btn = sender as Button;
			btn.IsEnabled = false;
			try
			{
				if(await this.messageManager.DeleteMessage(msg))
				{
					await this.LoadThreads();
				}
			}
			finally
			{
				btn.IsEnabled = true;
			}
		}
		//private void MarkAllReadClicked(object sender, RoutedEventArgs e)
		//{

		//}

		async private void SubmitPostButtonClicked(object sender, RoutedEventArgs e)
		{
			var msg = this.messagesList.SelectedItem as Message;
			if (msg == null) return;
			var btn = sender as Button;
			try
			{
				btn.IsEnabled = false;
				var success = await this.messageManager.SendMessage(msg.From, string.Format("Re: {0}", msg.Subject), this.replyTextBox.Text);
				if(success)
				{
					this.showReply.IsChecked = false;
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

		private void ShowReplyClicked(object sender, RoutedEventArgs e)
		{
			var msg = this.messagesList.SelectedItem as Message;
			if (msg == null) return;
			this.replyTextBox.Text = string.Format("{2}{2}On {0} {1} wrote: {2} {3}", msg.Date, msg.From, Environment.NewLine, msg.Body);
			this.replyTextBox.Focus(FocusState.Programmatic);
		}

		async private void SendNewMessageClicked(object sender, RoutedEventArgs e)
		{
			var btn = sender as Button;
			try
			{
				btn.IsEnabled = false;
				var success = await this.messageManager.SendMessage(this.toTextBox.Text, this.subjectTextBox.Text, this.newMessageTextBox.Text);
				if (success)
				{
					this.newMessageButton.IsChecked = false;
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

		private void ShowNewMessageAreaClicked(object sender, RoutedEventArgs e)
		{
			this.toTextBox.Text = string.Empty;
			this.subjectTextBox.Text = string.Empty;
			this.newMessageTextBox.Text = string.Empty;
			this.toTextBox.Focus(FocusState.Programmatic);
		}

		private void NewMessageTextChanged(object sender, TextChangedEventArgs e)
		{
			this.CanSendNewMessage = !string.IsNullOrWhiteSpace(this.toTextBox.Text) &&
				!string.IsNullOrWhiteSpace(this.subjectTextBox.Text) &&
				!string.IsNullOrWhiteSpace(this.newMessageTextBox.Text);
		}

		private async Task LoadThreads()
		{
			this.LoadingMessages = true;

			this.CanGoBack = false;
			this.CanGoForward = false;

			if (this.currentPage <= 1) this.currentPage = 1;


			var result = await this.messageManager.GetMessages(this.currentPage);

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
				this.messageWebView.NavigateToString(WebBrowserHelper.GetPostHtml(message.Body));
				//Mark read.
				await this.messageManager.MarkMessageRead(message);
			}
		}

		//Well, right now messages don't support embedded links, but maybe in the future...
		private async void ScriptNotify(object s, NotifyEventArgs e)
		{
			var sender = s as WebView;
			var jsonEventData = JToken.Parse(e.Value);

			if (jsonEventData["eventName"].ToString().Equals("imageloaded"))
			{
				await ResizeWebView(sender);
			}
		}

		async private void NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
		{
			await ResizeWebView(sender);
		}

		async private void NavigatingWebView(WebView sender, WebViewNavigationStartingEventArgs args)
		{
			//NavigateToString will not have a uri, so if a WebView is trying to navigate somewhere with a URI, we want to run it in a new browser window.
			//We have to handle navigation like this because if a link has target="_blank" in it, the application will crash entirely when clicking on that link.
			//Maybe this will be fixed in an updated SDK, but for now it is what it is.
			if (args.Uri != null)
			{
				args.Cancel = true;
				await Launcher.LaunchUriAsync(args.Uri);
			}
		}

		private async Task ResizeWebView(WebView wv)
		{
			try
			{
				//For some reason the WebView control *sometimes* has a width of NaN, or something small.
				//So we need to set it to what it's going to end up being in order for the text to render correctly.
				await wv.InvokeScriptAsync("eval", new string[] { string.Format("SetViewSize({0});", this.messageWebView.ActualWidth) }); // * Windows.Graphics.Display.DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel) });
				var result = await wv.InvokeScriptAsync("eval", new string[] { "GetViewSize();" }); 
				int viewHeight;
				if (int.TryParse(result, out viewHeight))
				{
					//viewHeight = (int)(viewHeight / Windows.Graphics.Display.DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel);
					await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
					{
						wv.MinHeight = wv.Height = viewHeight;
					});
				}
			}
			catch { }			
		}
	}
}
