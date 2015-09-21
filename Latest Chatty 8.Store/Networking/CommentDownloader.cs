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

		async public static Task<List<CommentThread>> ParseThreads(JToken chatty, SeenPostsManager seenPostsManager, AuthenticationManager services, LatestChattySettings settings, ThreadMarkManager markManager)
		{
			if (chatty == null) return null;
			var timer = new TelemetryTimer("ChattyParse");
			timer.Start();
			var threadCount = chatty["threads"].Count();
			var parsedChatty = new CommentThread[threadCount];
			await Task.Run(() =>
			{
				Parallel.For(0, threadCount, (i) =>
				{
					var thread = chatty["threads"][i];
					var t = ParseThread(thread, 0, seenPostsManager, services, settings, markManager);
					t.Wait();
					parsedChatty[i] = t.Result;
				});
			});
			timer.Stop();
			var list = parsedChatty.ToList();
#if GENERATE_THREADS
			if (System.Diagnostics.Debugger.IsAttached)
			{
				list.Add(ChattyHelper.GenerateMassiveThread(services, settings));
			}
#endif
			return list;
		}

		#region Private Helpers
		async public static Task<CommentThread> ParseThread(JToken jsonThread, int depth, SeenPostsManager seenPostsManager, AuthenticationManager services, LatestChattySettings settings, ThreadMarkManager markManager, string originalAuthor = null, bool storeCount = true)
		{
			var threadPosts = jsonThread["posts"];

			var firstJsonComment = threadPosts.First(j => j["id"].ToString().Equals(jsonThread["threadId"].ToString()));

			var rootComment = ParseCommentFromJson(firstJsonComment, null, seenPostsManager, services); //Get the first comment, this is what we'll add everything else to.
			var thread = new CommentThread(rootComment, settings);
			var markType = markManager.GetMarkType(thread.Id);
			if (markType == MarkType.Unmarked)
			{
				//If it's not marked, find out if it should be collapsed because of auto-collapse.
				if (settings.ShouldAutoCollapseCommentThread(thread))
				{
					await markManager.MarkThread(thread.Id, MarkType.Collapsed, true);
					thread.IsCollapsed = true;
				}
			}
			else
			{
				thread.IsPinned = markType == MarkType.Pinned;
				thread.IsCollapsed = markType == MarkType.Collapsed;
			}

			RecursiveAddComments(thread, rootComment, threadPosts, seenPostsManager, services);
			thread.HasNewReplies = thread.Comments.Any(c => c.IsNew);

			return thread;
		}

		private static void RecursiveAddComments(CommentThread thread, Comment parent, JToken threadPosts, SeenPostsManager seenPostsManager, AuthenticationManager services)
		{
			thread.AddReply(parent);
			var childPosts = threadPosts.Where(c => c["parentId"].ToString().Equals(parent.Id.ToString()));

			if (childPosts != null)
			{
				foreach (var reply in childPosts)
				{
					var c = ParseCommentFromJson(reply, parent, seenPostsManager, services);
					RecursiveAddComments(thread, c, threadPosts, seenPostsManager, services);
				}
			}

		}

		public static Comment ParseCommentFromJson(JToken jComment, Comment parent, SeenPostsManager seenPostsManager, AuthenticationManager services)
		{
			var commentId = (int)jComment["id"];
			var parentId = (int)jComment["parentId"];
			var category = (PostCategory)Enum.Parse(typeof(PostCategory), ParseJTokenToDefaultString(jComment["category"], "ontopic"));
			var author = ParseJTokenToDefaultString(jComment["author"], string.Empty);
			var date = jComment["date"].ToString();
			var body = ParseJTokenToDefaultString(jComment["body"], string.Empty);
			var preview = HtmlRemoval.StripTagsRegexCompiled(System.Net.WebUtility.HtmlDecode(body).Replace("<br />", " "));
			preview = preview.Substring(0, Math.Min(preview.Length, 300));
			var c = new Comment(commentId, category, author, date, preview, body, parent != null ? parent.Depth + 1 : 0, parentId, seenPostsManager.IsCommentNew(commentId), services);
			foreach (var lol in jComment["lols"])
			{
				var count = (int)lol["count"];
				switch (lol["tag"].ToString())
				{
					case "lol":
						c.LolCount = count;
						break;
					case "inf":
						c.InfCount = count;
						break;
					case "unf":
						c.UnfCount = count;
						break;
					case "tag":
						c.TagCount = count;
						break;
					case "wtf":
						c.WtfCount = count;
						break;
					case "ugh":
						c.UghCount = count;
						break;
				}
			}
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
