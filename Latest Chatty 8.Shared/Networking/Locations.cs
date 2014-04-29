using Latest_Chatty_8.Shared.Settings;

namespace Latest_Chatty_8.Shared.Networking
{
	public static class Locations
	{
		#region ServiceHost
		/// <summary>
		/// The location of the chatty API service host
		/// </summary>
		public static string ServiceHost { get { return "https://winchatty.com/v2/"; } }
		/// <summary>
		/// The location to post comments to
		/// </summary>
		public static string PostUrl { get { return ServiceHost + "postComment"; } }

		public static string WaitForEvent { get { return ServiceHost + "waitForEvent"; } }

		public static string PollForEvent { get { return ServiceHost + "pollForEvent"; } }

		public static string GetNewestEventId { get { return ServiceHost + "getNewestEventId"; } }

		public static string GetClientSessionToken { get { return ServiceHost + "clientData/getClientSessionToken"; } }

		public static string GetMarkedPosts { get { return ServiceHost + "clientData/getMarkedPosts"; } }

		public static string GetThread { get { return ServiceHost + "getThread"; } }

		public static string MarkPost { get { return ServiceHost + "clientData/markPost"; } }

		public static string VerifyCredentials { get { return ServiceHost + "verifyCredentials"; } }

		/// <summary>
		/// Location of the full chatty refresh.
		/// </summary>
		public static string Chatty { get { return ServiceHost + "getChatty/"; } }
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
			return string.Format("{0}getThread?id={1}", ServiceHost, commentId);
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
		public static string NotificationService { get { return "http://shacknotify.cloudapp.net:12253/"; } } //12243
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

		public static string PrivacyPolicy
		{
			get { return "http://bit-shift.com/latestchatty8/privacyPolicy.json"; }
		}

	}
}
