using Common;
using System.Threading.Tasks;

namespace Tasks
{
#pragma warning disable CA1812
	class UselessNotificationManager : BaseNotificationManager
#pragma warning restore CA1812
	{
		public override bool HasUnreadNotifications { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

		public UselessNotificationManager(AuthenticationManager authManager) : base(authManager) { }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		public async override Task SyncSettingsWithServer() { }

		public async override Task RegisterForNotifications() { }

		public override void RemoveNotificationForCommentId(int postId) { }

		public async override Task ReRegisterForNotifications() { }

		public async override Task UnRegisterNotifications() { }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
	}
}
