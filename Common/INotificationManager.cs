using System.Threading.Tasks;

namespace Common
{
	public interface INotificationManager
	{
		Task RegisterForNotifications();
		Task ReRegisterForNotifications();
		void RemoveNotificationForCommentId(int postId);
		Task UnRegisterNotifications();
		Task UpdateBadgeCount();
		void ClearBadgeCount();
		void SetBadgeCount(int count);
		Task SyncSettingsWithServer();
	}
}
