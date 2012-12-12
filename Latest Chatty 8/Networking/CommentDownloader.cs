using Latest_Chatty_8.Common;
using Latest_Chatty_8.DataModel;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Latest_Chatty_8.Networking
{
	public static class CommentDownloader
	{
		async public static Task<IEnumerable<Comment>> GetChattyRootComments()
		{
			var rootComments = new List<Comment>();
			var json = await JSONDownloader.Download(Locations.ChattyComments);
			foreach (var jsonComment in json["comments"].Children())
			{
				rootComments.Add(CommentDownloader.ParseComments(jsonComment, 0));
			}
			return rootComments;
		}

		async public static Task<Comment> GetComment(int rootId, bool storeCount = true)
		{
			var comments = await JSONDownloader.Download(Locations.MakeCommentUrl(rootId));
			return CommentDownloader.ParseComments(comments["comments"][0], 0, storeCount);
		}


		async public static Task<IEnumerable<Comment>> GetReplyComments()
		{
			var comments = new List<Comment>();
			var json = await JSONDownloader.Download(Locations.ReplyComments);
			if (json["comments"].Children().Count() > 0)
			{
				foreach (var jsonComment in json["comments"].Children())
				{
					comments.Add(CommentDownloader.ParseComments(jsonComment, 0, false));
				}
			}
			return comments;
		}

		async public static Task<IEnumerable<Comment>> MyComments()
		{
			var comments = new List<Comment>();
			var json = await JSONDownloader.Download(Locations.MyComments);
			if (json["comments"].Children().Count() > 0)
			{
				foreach (var jsonComment in json["comments"].Children())
				{
					comments.Add(CommentDownloader.ParseComments(jsonComment, 0, false));
				}
			}
			return comments;
		}

		async public static Task<IEnumerable<Comment>> SearchComments(string queryString)
		{
			var comments = new List<Comment>();
			var json = await JSONDownloader.Download(Locations.SearchRoot + queryString);
			if (json["comments"].Children().Count() > 0)
			{
				foreach (var jsonComment in json["comments"].Children())
				{
					comments.Add(CommentDownloader.ParseComments(jsonComment, 0, false));
				}
			}
			return comments;
		}
		
		private static Comment ParseComments(JToken jsonComment, int depth, bool storeCount = true)
		{
			var userParticipated = false;
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
				depth);

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
					currentComment.Replies.Add(CommentDownloader.ParseComments(comment, depth + 1, storeCount));
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
	}
}
