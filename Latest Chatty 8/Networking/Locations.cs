using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Latest_Chatty_8.Networking
{
	public static class Locations
	{
		#region ServiceHost
		public const string ServiceHost = "http://shackapi.stonedonkey.com/";
		public const string PostUrl = ServiceHost + "post/";
		public const string Stories = ServiceHost + "stories.json";
		public const string ChattyComments = ServiceHost + "chatty/index.json";
		public static string ReplyComments = ServiceHost + "Search.json/?parent_author=" + CoreServices.Instance.Credentials.UserName;
		public static string MyComments = ServiceHost + "Search.json/?Author=" + CoreServices.Instance.Credentials.UserName;

		public static string MakeCommentUrl(int commentId)
		{
			return string.Format("{0}/thread/{1}.json", ServiceHost, commentId);
		}
		#endregion
	}
}
