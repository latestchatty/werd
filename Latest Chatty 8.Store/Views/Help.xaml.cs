using System;
using System.Collections.Generic;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace Latest_Chatty_8.Views
{
	/// <summary>
	/// A basic page that provides characteristics common to most applications.
	/// </summary>
	public sealed partial class Help : ShellView
	{
		public Help()
		{
			this.InitializeComponent();
			this.helpWebView.Navigate(new Uri("http://bit-shift.com/latestchatty8/help.html"));
		}

		public override string ViewTitle
		{
			get
			{
				return "Help";
			}
		}
	}
}
