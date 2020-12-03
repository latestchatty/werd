using Autofac;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Werd.Views.NavigationArgs
{
	public class ChattyNavigationArgs
	{
		public IContainer Container { get; set; }

		public int? OpenPostInTabId { get; set; }

		public ChattyNavigationArgs(IContainer container)
		{
			Container = container;
		}
	}
}
