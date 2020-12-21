using System;

namespace Werd.Common
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
