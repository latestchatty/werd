using Latest_Chatty_8.Common;
using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Settings;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Latest_Chatty_8.Networking
{
	/// <summary>
	/// Comment downloading helper methods
	/// </summary>
	public static class CommentDownloader
	{
		#region Public Comment Fetching Methods
		/// <summary>
		/// Gets the parent comments from the chatty
		/// </summary>
		/// <returns></returns>
		async public static Task<Tuple<int, IEnumerable<CommentThread>>> GetChattyRootComments(int page)
		{
			var rootComments = new List<CommentThread>();
			var pageCount = 0;
			var json = await JSONDownloader.Download(string.Format("{0}17.{1}.json", Locations.ServiceHost, page));
			if (json != null)
			{
				foreach (var jsonComment in json["comments"].Children())
				{
					rootComments.Add(CommentDownloader.ParseThread(jsonComment, 0));
				}
				pageCount = int.Parse(ParseJTokenToDefaultString(json["last_page"], "1"));
			}
			return new Tuple<int, IEnumerable<CommentThread>>(pageCount, rootComments);
		}

		async public static Task<List<CommentThread>> DownloadThreads(IEnumerable<int> threadIds)
		{
			var json = await JSONDownloader.Download(Locations.GetThread + "?id=" + String.Join(",", threadIds));
			return ParseThreads(json);
		}

		/// <summary>
		/// Searches the comments
		/// </summary>
		/// <param name="queryString">The query string.</param>
		/// <returns></returns>
		async public static Task<IEnumerable<Comment>> SearchComments(string queryString)
		{
			var comments = new List<Comment>();
			var json = await JSONDownloader.Download(Locations.SearchRoot + queryString);
			if ((json != null) && (json["comments"].Children().Count() > 0))
			{
				//:TODO: Fix Comment Search.
				//foreach (var jsonComment in json["comments"].Children())
				//{
				//	comments.Add(CommentDownloader.ParseThread(jsonComment, 0, null, false));
				//}
			}
			return comments;
		}
		#endregion

		public static List<CommentThread> ParseThreads(JToken chatty)
		{
			var parsedChatty = new List<CommentThread>();

			foreach (var thread in chatty["threads"])
			{
				parsedChatty.Add(ParseThread(thread, 0));
			}

			return parsedChatty;
		}

		#region Private Helpers
		private static CommentThread ParseThread(JToken jsonThread, int depth, string originalAuthor = null, bool storeCount = true)
		{
			var threadPosts = jsonThread["posts"];

			var firstJsonComment = threadPosts.First(j => j["id"].ToString().Equals(jsonThread["threadId"].ToString()));

			var rootComment = ParseCommentFromJson(firstJsonComment, null); //Get the first comment, this is what we'll add everything else to.
			var thread = new CommentThread(rootComment);
			RecursiveAddComments(thread, rootComment, threadPosts);
			
			return thread;
		}

		private static void RecursiveAddComments(CommentThread thread, Comment parent, JToken threadPosts)
		{
			thread.AddReply(parent);
			var childPosts = threadPosts.Where(c => c["parentId"].ToString().Equals(parent.Id.ToString()));

			if (childPosts != null)
			{
				foreach (var reply in childPosts)
				{
					var c = ParseCommentFromJson(reply, parent);
					RecursiveAddComments(thread, c, threadPosts);
				}
			}

		}

		public static Comment ParseCommentFromJson(JToken jComment, Comment parent)
		{
			var commentId = (int)jComment["id"];
			var parentId = (int)jComment["parentId"];
			var category = (PostCategory)Enum.Parse(typeof(PostCategory), ParseJTokenToDefaultString(jComment["category"], "ontopic"));
			var author = ParseJTokenToDefaultString(jComment["author"], string.Empty);
			var date = jComment["date"].ToString();
			var body = ParseJTokenToDefaultString(jComment["body"], string.Empty);
			var preview = HtmlRemoval.StripTagsRegex(System.Net.WebUtility.HtmlDecode(Uri.UnescapeDataString(body)).Replace("<br />", " "));
			preview = preview.Substring(0, Math.Min(preview.Length, 200));
			//TODO: Fix the remaining things that aren't populated.
			var c = new Comment(commentId, category, author, date, preview, body, parent != null ? parent.Depth + 1 : 0, parentId);
			return c;
		}

		private static string ParseJTokenToDefaultString(JToken token, string defaultString)
		{
			var stringVal = (string)token;

			if (String.IsNullOrWhiteSpace(stringVal) || stringVal.Equals("null"))
			{
				stringVal = defaultString;
			}

			return stringVal;
		}
		#endregion
	}
}
