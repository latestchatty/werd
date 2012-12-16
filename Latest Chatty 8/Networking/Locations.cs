using Latest_Chatty_8.Settings;

namespace Latest_Chatty_8.Networking
{
	public static class Locations
	{
		#region ServiceHost
		/// <summary>
		/// The location of the chatty API service host
		/// </summary>
		public static string ServiceHost { get { return "http://shackapi.stonedonkey.com/"; } }
		/// <summary>
		/// The location to post comments to
		/// </summary>
		public static string PostUrl { get { return ServiceHost + "post/"; } }
		/// <summary>
		/// The location to retrieve news stories
		/// </summary>
		public static string Stories { get { return ServiceHost + "stories.json"; } }
		/// <summary>
		/// The search root location
		/// </summary>
		public static string SearchRoot { get { return ServiceHost + "Search.json"; } }
		/// <summary>
		/// The location to get replies to the currently logged in users comments
		/// </summary>
		public static string ReplyComments
		{
			get { return ServiceHost + "Search.json/?parent_author=" + CoreServices.Instance.Credentials.UserName; }
		}
		/// <summary>
		/// The location to get the currently logged in users comments
		/// </summary>
		public static string MyComments
		{
			get { return ServiceHost + "Search.json/?Author=" + CoreServices.Instance.Credentials.UserName; }
		}
		/// <summary>
		/// Gets the location for a single post
		/// </summary>
		/// <param name="commentId"></param>
		/// <returns></returns>
		public static string MakeCommentUrl(int commentId)
		{
			return string.Format("{0}/thread/{1}.json", ServiceHost, commentId);
		}
		#endregion

		#region Cloud Host
		/// <summary>
		/// The location of the cloud service host
		/// </summary>
		public static string CloudHost { get { return "https://shacknotify.bit-shift.com:12244/"; } }
		/// <summary>
		/// The location for the currently logged in users cloud settings
		/// </summary>
		public static string MyCloudSettings
		{
			get { return CloudHost + "users/" + CoreServices.Instance.Credentials.UserName + "/settings"; }
		}

		/// <summary>
		/// The location of the cloud service
		/// </summary>
		public static string NotificationService { get { return "https://shacknotify.bit-shift.com:12253/"; } } //12243
		/// <summary>
		/// The location to delete notification subscription for the currently logged in user
		/// </summary>
		public static string NotificationDelete 
		{
			get
			{
				return NotificationService + "users/" + LatestChattySettings.Instance.Username.ToLowerInvariant() +
				"?deviceId=" + LatestChattySettings.Instance.NotificationID;
			} 
		}
		/// <summary>
		/// The location to subscribe to notifications for the currently logged in user
		/// </summary>
		public static string NotificationSubscription
		{
			get { return NotificationDelete + "&notificationType=" + (int)NotificationType.StoreNotify;}
		}
		#endregion
	}
}
