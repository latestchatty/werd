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
		//TODO: Comment paging

		#region Public Comment Fetching Methods
		/// <summary>
		/// Gets the parent comments from the chatty
		/// </summary>
		/// <returns></returns>
		async public static Task<Tuple<int, IEnumerable<Comment>>> GetChattyRootComments(int page)
		{
			var rootComments = new List<Comment>();
			var json = await JSONDownloader.Download(string.Format("{0}17.{1}.json", Locations.ServiceHost, page));
			foreach (var jsonComment in json["comments"].Children())
			{
				rootComments.Add(CommentDownloader.ParseComments(jsonComment, 0));
			}
			var pageCount = int.Parse(ParseJTokenToDefaultString(json["last_page"], "1"));
			return new Tuple<int,IEnumerable<Comment>>(pageCount, rootComments);
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
			return CommentDownloader.ParseComments(comments["comments"][0], 0, null, storeCount);
		}

		/// <summary>
		/// Gets comments that are replies to the currently logged in users posts.
		/// </summary>
		/// <returns></returns>
		async public static Task<IEnumerable<Comment>> GetReplyComments()
		{
			var comments = new List<Comment>();
			var json = await JSONDownloader.Download(Locations.ReplyComments);
			if (json["comments"].Children().Count() > 0)
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
			if (json["comments"].Children().Count() > 0)
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
			if (json["comments"].Children().Count() > 0)
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
		private static Comment ParseComments(JToken jsonComment, int depth, string originalAuthor = null, bool storeCount = true)
		{
			var userParticipated = false;
			originalAuthor = originalAuthor ?? ParseJTokenToDefaultString(jsonComment["author"], string.Empty);
			if (jsonComment["participants"] != null)
			{
				userParticipated = jsonComment["participants"].Children()["username"].Values<string>().Any(s => s.Equals(CoreServices.Instance.Credentials.UserName, StringComparison.OrdinalIgnoreCase));
			}
			var currentComment = new Comment(
				int.Parse(ParseJTokenToDefaultString(jsonComment["id"], "0")),
				0,
				int.Parse(ParseJTokenToDefaultString(jsonComment["reply_count"], "0")),
				(PostCategory)Enum.Parse(typeof(PostCategory), ParseJTokenToDefaultString(jsonComment["category"], "ontopic")),
				ParseJTokenToDefaultString(jsonComment["author"], string.Empty),
				ParseJTokenToDefaultString(jsonComment["date"], string.Empty),
				ParseJTokenToDefaultString(jsonComment["preview"], string.Empty),
				ParseJTokenToDefaultString(jsonComment["body"], string.Empty),
				userParticipated,
				depth,
				originalAuthor);

			if (storeCount)
			{
				if (currentComment.IsNew)
				{
					CoreServices.Instance.PostCounts.Add(currentComment.Id, currentComment.ReplyCount);
				}
				else
				{
					CoreServices.Instance.PostCounts[currentComment.Id] = currentComment.ReplyCount;
				}
			}

			if (jsonComment["comments"].HasValues)
			{
				currentComment.Replies.Clear();
				foreach (var comment in jsonComment["comments"].Children())
				{
					currentComment.Replies.Add(CommentDownloader.ParseComments(comment, depth + 1, originalAuthor, storeCount));
				}
			}
			return currentComment;
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
