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

		public async Task RegisterForNotifications() { }

		public void RemoveNotificationForCommentId(int postId) { }

		public async Task ReRegisterForNotifications() { }

		public async Task UnRegisterNotifications() { }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
	}
}
