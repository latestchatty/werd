using Common;
using Latest_Chatty_8.Common;
using System.Threading.Tasks;

namespace Tasks
{
	class UselessNotificationManager : BaseNotificationManager
	{
		public UselessNotificationManager(AuthenticationManager authManager) : base(authManager) { }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		public override Task<NotificationUser> GetUser() { return null; }

		public async override Task RegisterForNotifications() { }

		public override void RemoveNotificationForCommentId(int postId) { }

		public async override Task ReRegisterForNotifications() { }

		public async override Task UnRegisterNotifications() { }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
	}
}
