using Latest_Chatty_8.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tasks
{
	class UselessNotificationManager : INotificationManager
	{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		public async Task RegisterForNotifications()
		{

		}

		public async Task RemoveNotificationForCommentId(int postId)
		{
		}

		public async Task ReRegisterForNotifications(bool resetCount = false)
		{
		}

		public async  Task ResetCount()
		{
		}

		public async Task Resume()
		{
		}

		public async Task UnRegisterNotifications()
		{
		}
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
	}
}
