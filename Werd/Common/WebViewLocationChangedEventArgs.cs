using System;

namespace Werd.Common
{
	public class WebViewLocationChangedEventArgs : EventArgs
	{
		public Uri Location { get; private set; }

		public WebViewLocationChangedEventArgs(Uri location)
		{
			this.Location = location;
		}
	}
}
