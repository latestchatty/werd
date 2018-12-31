using System;

namespace Latest_Chatty_8.Common
{
	public class LinkClickedEventArgs : EventArgs
	{
		public Uri Link { get; private set; }

		public LinkClickedEventArgs(Uri link)
		{
			Link = link;
		}
	}
}
