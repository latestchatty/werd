using System;

namespace Werd.Views
{
	public static class NavigationHelper
	{
		public static bool CanOpenMultipleInstances(Type page)
		{
			return !(page == typeof(Chatty)
				|| page == typeof(Messages)
				//|| page == typeof(NewRootPostView)
				|| page == typeof(ModToolsWebView)
				|| page == typeof(PinnedThreadsView)
				|| page == typeof(SettingsView)
				|| page == typeof(InlineChattyFast)
				|| page == typeof(Help)
				|| page == typeof(DeveloperView));
		}
	}
}
