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

		public SingleThreadView()
		{
			this.InitializeComponent();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			var navArg = e.Parameter as Tuple<IContainer, CommentThread>;
			if(navArg == null)
			{
				if(this.Frame.CanGoBack)
				{
					this.Frame.GoBack();
				}
			}
			this.threadView.Initialize(navArg.Item1);
			this.threadView.DataContext = navArg.Item2;
		}

		private void InlineLinkClicked(object sender, LinkClickedEventArgs e)
		{
			if(this.LinkClicked != null)
			{
				this.LinkClicked(this, e);
			}
		}
	}
}
