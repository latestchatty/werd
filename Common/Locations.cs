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

		public static string GetLolTaggersUrl(int threadId, string tagName)
		{
			return ShackApiRoot + $"ext_get_all_raters&ids[]={threadId}&tag={tagName}";
		}

		public static string GetTagsForUserByPostId(int postId, string user)
		{
			return ShackApiRoot + $"ext_get_all_tags_for_user&user={WebUtility.UrlEncode(user)}&ids[]={postId}";
		}

		public static string TagPost(int postId, string user, string tag)
		{
			return GetTagUrl(postId, user, tag, true);
		}

		public static string UntagPost(int postId, string user, string tag)
		{
			return GetTagUrl(postId, user, tag, false);
		}

		private static string GetTagUrl(int postId, string user, string tag, bool doTag)
		{
			return ShackApiRoot + $"ext_create_tag_via_api&id={postId}&user={WebUtility.UrlEncode(user)}&tag={tag}&untag={(doTag ? "0" : "1")}&secret={SHACK_API_KEY}";
		}
		#endregion

		#region ServiceHost
		/// <summary>
		/// The location of the chatty API service host
		/// </summary>
		public static string ServiceHost => "https://winchatty.com/v2/";

		//public static string ServiceHost { get { return "https://api.woggle.net/v2/"; } }
		/// <summary>
		/// The location to post comments to
		/// </summary>
		public static string PostUrl => ServiceHost + "postComment";

		public static string WaitForEvent => ServiceHost + "waitForEvent";

		public static string PollForEvent => ServiceHost + "pollForEvent";

		public static string GetNewestEventId => ServiceHost + "getNewestEventId";

		public static string GetClientSessionToken => ServiceHost + "clientData/getClientSessionToken";

		public static string GetMarkedPosts => ServiceHost + "clientData/getMarkedPosts";

		public static string GetThread => ServiceHost + "getThread";

		public static string MarkPost => ServiceHost + "clientData/markPost";

		public static string VerifyCredentials => ServiceHost + "verifyCredentials";

		public static string GetMessageCount => ServiceHost + "getMessageCount";

		public static string GetMessages => ServiceHost + "getMessages";

		public static string MarkMessageRead => ServiceHost + "markMessageRead";

		public static string SendMessage => ServiceHost + "sendMessage";

		public static string DeleteMessage => ServiceHost + "deleteMessage";

		public static string GetSettings => ServiceHost + "clientData/getClientData";
		public static string SetSettings => ServiceHost + "clientData/setClientData";
		public static string GetTenYearUsers => ServiceHost + "getAllTenYearUsers";

		public static string GetPostLineage => ServiceHost + "getPostLineage";

		/// <summary>
		/// Location of the full chatty refresh.
		/// </summary>
		public static string Chatty => ServiceHost + "getChatty";

		/// <summary>
		/// The location to retrieve news stories
		/// </summary>
		public static string Stories => ServiceHost + "stories.json";

		/// <summary>
		/// The search root location
		/// </summary>
		public static string SearchRoot => ServiceHost + "Search.json";

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

		#region Notifications
		public static string NotificationBase => "https://shacknotify.bit-shift.com/";
		//public static string NotificationBase { get { return "http://localhost:4000/"; } }

		public static string NotificationRegister => NotificationBase + "register";

		public static string NotificationUser => NotificationBase + "user";

		public static string NotificationDeRegister => NotificationBase + "deregister";
		public static string NotificationReplyToNotification => NotificationBase + "replyToNotification";
		public static string NotificationTest => NotificationBase + "test";

		public static string GetNotificationUserUrl(string userName)
		{
			return $"{NotificationBase}user?userName={WebUtility.UrlEncode(userName)}";
		}
		#endregion

	}
}
