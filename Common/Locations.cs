using System;
using System.Net;

namespace Common
{
	public static class Locations
	{
		public static readonly string SHACK_API_KEY;

		static Locations()
		{
			//Set this environment variable
			SHACK_API_KEY = Environment.GetEnvironmentVariable("SHACK_API_KEY");
			if (SHACK_API_KEY == null)
			{
				SHACK_API_KEY = "{{SHACK_API_KEY}}";
			}
		}

		#region Shack API
		private static string ShackApiRoot => "https://www.shacknews.com/api2/api-index.php?action2=";

		public static Uri GetLolTaggersUrl(int threadId, string tagName)
		{
			return new Uri(ShackApiRoot + $"ext_get_all_raters&ids[]={threadId}&tag={tagName}");
		}

		public static Uri GetTagsForUserByPostId(int postId, string user)
		{
			return new Uri(ShackApiRoot + $"ext_get_all_tags_for_user&user={WebUtility.UrlEncode(user)}&ids[]={postId}");
		}

		public static Uri TagPost(int postId, string user, string tag)
		{
			return GetTagUrl(postId, user, tag, true);
		}

		public static Uri UntagPost(int postId, string user, string tag)
		{
			return GetTagUrl(postId, user, tag, false);
		}

		private static Uri GetTagUrl(int postId, string user, string tag, bool doTag)
		{
			return new Uri(ShackApiRoot + $"ext_create_tag_via_api&id={postId}&user={WebUtility.UrlEncode(user)}&tag={tag}&untag={(doTag ? "0" : "1")}&secret={SHACK_API_KEY}");
		}
		#endregion

		#region ServiceHost
		private static string _serviceHost = "https://winchatty.com/v2/";

		public static void SetServiceHost(string value)
		{
			_serviceHost = value;
		}
		/// <summary>
		/// The location of the chatty API service host
		/// </summary>
		public static string ServiceHost => _serviceHost;

		/// <summary>
		/// The location to post comments to
		/// </summary>
		public static Uri PostUrl => new Uri(ServiceHost + "postComment");

		public static Uri WaitForEvent => new Uri(ServiceHost + "waitForEvent");

		public static Uri PollForEvent => new Uri(ServiceHost + "pollForEvent");

		public static Uri GetNewestEventId => new Uri(ServiceHost + "getNewestEventId");

		public static Uri GetClientSessionToken => new Uri(ServiceHost + "clientData/getClientSessionToken");

		public static Uri GetMarkedPosts => new Uri(ServiceHost + "clientData/getMarkedPosts");

		public static Uri GetThread => new Uri(ServiceHost + "getThread");

		public static Uri MarkPost => new Uri(ServiceHost + "clientData/markPost");

		public static Uri VerifyCredentials => new Uri(ServiceHost + "verifyCredentials");

		public static Uri GetMessageCount => new Uri(ServiceHost + "getMessageCount");

		public static Uri GetMessages => new Uri(ServiceHost + "getMessages");

		public static Uri MarkMessageRead => new Uri(ServiceHost + "markMessageRead");

		public static Uri SendMessage => new Uri(ServiceHost + "sendMessage");

		public static Uri DeleteMessage => new Uri(ServiceHost + "deleteMessage");

		public static Uri GetSettings => new Uri(ServiceHost + "clientData/getClientData");
		public static Uri SetSettings => new Uri(ServiceHost + "clientData/setClientData");
		public static Uri GetTenYearUsers => new Uri(ServiceHost + "getAllTenYearUsers");

		public static Uri GetPost => new Uri(ServiceHost + "getPost");

		public static Uri ModeratePost => new Uri(ServiceHost + "setPostCategory");

		/// <summary>
		/// Location of the full chatty refresh.
		/// </summary>
		public static Uri Chatty => new Uri(ServiceHost + "getChatty");
		#endregion

		#region Notifications
		public static string NotificationBase => "https://shacknotify.bit-shift.com/";
		//public static string NotificationBase { get { return "http://192.168.1.135:4000/"; } }

		public static Uri NotificationRegister => new Uri(NotificationBase + "register");

		public static Uri NotificationUser => new Uri(NotificationBase + "v2/user");

		public static Uri NotificationDeRegister => new Uri(NotificationBase + "deregister");
		public static Uri NotificationReplyToNotification => new Uri(NotificationBase + "replyToNotification");
		public static Uri NotificationTest => new Uri(NotificationBase + "test");

		public static Uri GetNotificationUserUrl(string userName)
		{
			return new Uri($"{NotificationBase}user?userName={WebUtility.UrlEncode(userName)}");
		}
		#endregion

	}
}
