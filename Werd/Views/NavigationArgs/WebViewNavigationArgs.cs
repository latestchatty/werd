using Autofac;
using System;

namespace Werd.Views.NavigationArgs
{
	public class WebViewNavigationArgs
	{
		public IContainer Container { get; set; }

		public Uri NavigationUrl { get; set; }
		public string NavigationString { get; set; }

		public WebViewNavigationArgs(IContainer container, Uri navigationUri)
		{
			Container = container;
			NavigationUrl = navigationUri;
		}

		public WebViewNavigationArgs(IContainer container, string navigationString)
		{
			Container = container;
			NavigationString = navigationString;
		}
	}
}
