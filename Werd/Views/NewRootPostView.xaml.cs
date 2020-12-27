using Autofac;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using Microsoft.UI.Xaml.Controls;
using System;
using Werd.Common;
using Windows.UI.Xaml.Navigation;

namespace Werd.Views
{
	public sealed partial class NewRootPostView
	{
		public NewRootPostView()
		{
			InitializeComponent();
		}

		public override string ViewIcons { get => "\uE90A"; set { return; } }
		public override string ViewTitle { get => "New root post"; set { return; } }

		public override event EventHandler<LinkClickedEventArgs> LinkClicked = delegate { };
		public override event EventHandler<ShellMessageEventArgs> ShellMessage = delegate { };

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			PostControl.UpdateLayout();
			// TODO: TAB - This doesn't work any more when it's opened in a new tab.
			// Probably because it's not visible when the view is created so focus goes elsewhere.
			// Need a different solution.
			PostControl.SetFocus();
		}

		private void PostControl_Closed(object sender, EventArgs e)
		{
			var containingTab = this.FindParent<TabViewItem>();
			this.Shell.CloseTab(containingTab);
		}
	}
}
