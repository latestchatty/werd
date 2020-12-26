using System;

namespace Werd.Common
{
	public class LinkClickedEventArgs : EventArgs
	{
		public Uri Link { get; private set; }

		public bool OpenInBackground { get; private set; }

		public LinkClickedEventArgs(Uri link, bool openInBackground = false)
		{
			Link = link;
			OpenInBackground = openInBackground;
		}
	}
}
