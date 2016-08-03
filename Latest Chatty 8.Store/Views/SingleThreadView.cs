using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using Autofac;
using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Common;
using Latest_Chatty_8.Managers;

namespace Latest_Chatty_8.Views
{
	public sealed partial class SingleThreadView : ShellView
	{
		public override string ViewTitle
		{
			get
			{
				return "Single Thread";
			}
		}

		public override event EventHandler<LinkClickedEventArgs> LinkClicked;
		public override event EventHandler<ShellMessageEventArgs> ShellMessage;

		public SingleThreadView()
		{
			this.InitializeComponent();
		}

		protected async override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			this.loadingBar.Visibility = Visibility.Visible;
			this.loadingBar.IsActive = true;
			var navArg = e.Parameter as Tuple<IContainer, int, int>;
			if (navArg == null)
			{
				if (this.Frame.CanGoBack)
				{
					this.Frame.GoBack();
				}
			}
			var chattyManager = navArg.Item1.Resolve<ChattyManager>();

			this.threadView.Initialize(navArg.Item1);

			var thread = await chattyManager.FindOrAddThreadByAnyPostId(navArg.Item2);
			if(thread == null)
			{
				this.ShellMessage(this, new ShellMessageEventArgs($"Couldn't load thread for id {navArg.Item2}", ShellMessageType.Error));
			}
			this.threadView.DataContext = thread;
			this.threadView.SelectPostId(navArg.Item3);
			this.loadingBar.Visibility = Visibility.Collapsed;
			this.loadingBar.IsActive = false;
		}

		protected async override void OnNavigatedFrom(NavigationEventArgs e)
		{
			base.OnNavigatedFrom(e);
			await this.threadView.Close();
		}

		private void InlineLinkClicked(object sender, LinkClickedEventArgs e)
		{
			if(this.LinkClicked != null)
			{
				this.LinkClicked(sender, e);
			}
		}

		private void InlineShellMessage(object sender, ShellMessageEventArgs e)
		{
			if(this.ShellMessage != null)
			{
				this.ShellMessage(sender, e);
			}
		}
	}
}
