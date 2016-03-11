using System.Threading.Tasks;

namespace Latest_Chatty_8.Common
{
	public interface INotificationManager
	{
		Task RegisterForNotifications();
		Task RemoveNotificationForCommentId(int postId);
		Task ReRegisterForNotifications(bool resetCount = false);
		Task ResetCount();
		Task Resume();
		Task UnRegisterNotifications();
	}
}