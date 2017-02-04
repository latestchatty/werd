using System.Threading.Tasks;

namespace Latest_Chatty_8.Common
{
	public interface INotificationManager
	{
		Task RegisterForNotifications();
		Task ReRegisterForNotifications();
		void RemoveNotificationForCommentId(int postId);
		Task UnRegisterNotifications();
		Task<NotificationUser> GetUser();
		Task UpdateBadgeCount();
		void ClearBadgeCount();
	}
}
