using Latest_Chatty_8.Settings;

namespace Latest_Chatty_8.Networking
{
	public static class Locations
	{
		#region LOL
		private static string LolHost { get { return "http://www.lmnopc.com/greasemonkey/shacklol/"; } }

		public static string LolSubmit { get { return LolHost + "report.php"; } }

		public static string LolCounts { get { return "http://lol.lmnopc.com/api.php?special=getcounts"; } }
		#endregion

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

		public static string GetMessageCount { get { return ServiceHost + "getMessageCount"; } }

		public static string GetMessages { get { return ServiceHost + "getMessages"; } }

		public static string MarkMessageRead { get { return ServiceHost + "markMessageRead"; } }

		public static string SendMessage { get { return ServiceHost + "sendMessage"; } }

		public static string DeleteMessage { get { return ServiceHost + "deleteMessage"; } }

		public static string GetSettings { get { return ServiceHost + "clientData/getClientData"; } }
		public static string SetSettings { get { return ServiceHost + "clientData/setClientData"; } }

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
		/// Gets the location for a single post
		/// </summary>
		/// <param name="commentId"></param>
		/// <returns></returns>
		public static string MakeCommentUrl(int commentId)
		{
			return string.Format("{0}getThread?id={1}", ServiceHost, commentId);
		}
		#endregion

		public static string PrivacyPolicy
		{
			get { return "http://bit-shift.com/latestchatty8/privacyPolicy.json"; }
		}

	}
}
