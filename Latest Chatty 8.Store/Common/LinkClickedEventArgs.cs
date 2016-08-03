using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Latest_Chatty_8.Common
{
	public class LinkClickedEventArgs : EventArgs
	{
		public Uri Link { get; private set; }

		public LinkClickedEventArgs(Uri link)
		{
			this.Link = link;
		}
	}
}
