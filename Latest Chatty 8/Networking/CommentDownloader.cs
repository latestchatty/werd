using Latest_Chatty_8.Common;
using Latest_Chatty_8.DataModel;
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
		async public static Task<Tuple<int, IEnumerable<Comment>>> GetChattyRootComments(int page)
		{
			var rootComments = new List<Comment>();
			var pageCount = 0;
			var json = await JSONDownloader.Download(string.Format("{0}17.{1}.json", Locations.ServiceHost, page));
			if (json != null)
			{
				foreach (var jsonComment in json["comments"].Children())
				{
					rootComments.Add(CommentDownloader.ParseComments(jsonComment, 0));
				}
				pageCount = int.Parse(ParseJTokenToDefaultString(json["last_page"], "1"));
			}
			return new Tuple<int, IEnumerable<Comment>>(pageCount, rootComments);
		}

		/// <summary>
		/// Gets a comment and all sub-comments
		/// </summary>
		/// <param name="rootId">The root post id.</param>
		/// <param name="storeCount">if set to <c>true</c> the reply count will be stored for determination if this post has new replies or not.</param>
		/// <returns></returns>
		async public static Task<Comment> GetComment(int rootId, bool storeCount = true)
		{
			var comments = await JSONDownloader.Download(Locations.MakeCommentUrl(rootId));
			if (comments != null)
			{
				return CommentDownloader.ParseComments(comments["threads"].First(t => ParseJTokenToDefaultString(t["threadId"], string.Empty).Equals(rootId.ToString())), 0, null, storeCount);
			}
			return null;
		}

		/// <summary>
		/// Gets comments that are replies to the currently logged in users posts.
		/// </summary>
		/// <returns></returns>
		async public static Task<IEnumerable<Comment>> GetReplyComments()
		{
			var comments = new List<Comment>();
			var json = await JSONDownloader.Download(Locations.ReplyComments);
			if ((json != null) && (json["comments"].Children().Count() > 0))
			{
				foreach (var jsonComment in json["comments"].Children())
				{
					comments.Add(CommentDownloader.ParseComments(jsonComment, 0, null, false));
				}
			}
			return comments;
		}

		/// <summary>
		/// Gets the currently logged in users comments
		/// </summary>
		/// <returns></returns>
		async public static Task<IEnumerable<Comment>> MyComments()
		{
			var comments = new List<Comment>();
			var json = await JSONDownloader.Download(Locations.MyComments);
			if ((json != null) && (json["comments"].Children().Count() > 0))
			{
				foreach (var jsonComment in json["comments"].Children())
				{
					comments.Add(CommentDownloader.ParseComments(jsonComment, 0, null, false));
				}
			}
			return comments;
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
				foreach (var jsonComment in json["comments"].Children())
				{
					comments.Add(CommentDownloader.ParseComments(jsonComment, 0, null, false));
				}
			}
			return comments;
		}
		#endregion

		#region Private Helpers
		private static Comment ParseComments(JToken jsonThread, int depth, string originalAuthor = null, bool storeCount = true)
		{
			var threadPosts = jsonThread["posts"];
			var parsedComments = new List<Comment>();

			var firstJsonComment = threadPosts.First(j => j["id"].ToString().Equals(jsonThread["threadId"].ToString()));

			var rootComment = ParseCommentFromJson(firstJsonComment, null, null); //Get the first comment, this is what we'll add everything else to.
			RecursiveAddComments(rootComment, threadPosts, rootComment.Author);
			//TODO: Ensure ReplyCount is correct.

			//if (storeCount)
			//{
			//	if (currentComment.IsNew)
			//	{
			//		CoreServices.Instance.PostCounts.Add(currentComment.Id, currentComment.ReplyCount);
			//	}
			//	else
			//	{
			//		CoreServices.Instance.PostCounts[currentComment.Id] = currentComment.ReplyCount;
			//	}
			//}

			//if (jsonComment["comments"].HasValues)
			//{
			//	currentComment.Replies.Clear();
			//	foreach (var comment in jsonComment["comments"].Children())
			//	{
			//		currentComment.Replies.Add(CommentDownloader.ParseComments(comment, depth + 1, originalAuthor, storeCount));
			//	}
			//}
			return rootComment;
		}

		private static void RecursiveAddComments(Comment parent, JToken threadPosts, string originalAuthor)
		{
			var childPosts = threadPosts.Where(c => c["parentId"].ToString().Equals(parent.Id.ToString()));

			if (childPosts != null)
			{
				foreach (var reply in childPosts)
				{
					var c = ParseCommentFromJson(reply, parent, originalAuthor);
					parent.Replies.Add(c);
					RecursiveAddComments(c, threadPosts, originalAuthor);
				}
			}
		}

		private static Comment ParseCommentFromJson(JToken jComment, Comment parent, string originalAuthor)
		{
			var commentId = (int)jComment["id"];
			var category = (PostCategory)Enum.Parse(typeof(PostCategory), ParseJTokenToDefaultString(jComment["category"], "ontopic"));
			var author = ParseJTokenToDefaultString(jComment["author"], string.Empty);
			var date = jComment["date"].ToString();
			var body = ParseJTokenToDefaultString(jComment["body"], string.Empty);
			var preview = System.Net.WebUtility.HtmlDecode(Uri.UnescapeDataString(body));
			preview = preview.Substring(0, Math.Min(preview.Length, 100));
			//TODO: Fix the remaining things that aren't populated.
			var c = new Comment(commentId, 0, 0, category, author, date, preview, body, false, parent != null ? parent.Depth + 1 : 0, originalAuthor ?? author);
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
