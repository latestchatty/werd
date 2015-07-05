using Autofac;
using Latest_Chatty_8.Common;
using Latest_Chatty_8.DataModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
			await this.LoadThreads();
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

		//private void MarkAllReadClicked(object sender, RoutedEventArgs e)
		//{

		//}

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
			if (e.AddedItems.Count != 1) return;

			var message = e.AddedItems[0] as Message;
			if (message != null)
			{
				//Mark read.
				await this.messageManager.MarkMessageRead(message);
			}
		}
	}
}
