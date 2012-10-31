using Latest_Chatty_8.Common;
using Latest_Chatty_8.DataModel;
using LatestChatty.Classes;
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

		async public static Task<Comment> GetComment(int rootId)
		{
			var comments = await JSONDownloader.Download(Locations.MakeCommentUrl(rootId));
			return CommentDownloader.ParseComments(comments["comments"][0], 0);
		}

		private static Comment ParseComments(JToken jsonComment, int depth)
		{
			var userParticipated = jsonComment["participants"].Children()["username"].Values<string>().Any(s => s.Equals(CoreServices.Instance.Credentials.UserName, StringComparison.OrdinalIgnoreCase));

			var currentComment = new Comment(
				int.Parse((string)jsonComment["id"]),
				0,
				int.Parse((string)jsonComment["reply_count"]),
				(PostCategory)Enum.Parse(typeof(PostCategory), (string)jsonComment["category"]),
				(string)jsonComment["author"],
				(string)jsonComment["date"],
				(string)jsonComment["preview"],
				(string)jsonComment["body"],
				userParticipated,
				depth);

			if (jsonComment["comments"].HasValues)
			{
				currentComment.Replies.Clear();
				foreach (var comment in jsonComment["comments"].Children())
				{
					currentComment.Replies.Add(CommentDownloader.ParseComments(comment, depth + 1));
				}				
			}
			return currentComment;
		}
	}
}
