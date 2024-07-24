using Autofac;

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
