using Common;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Werd.Common;
using Werd.DataModel;
using Werd.Managers;
using Werd.Settings;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using AuthenticationManager = Common.AuthenticationManager;

namespace Werd.Networking
{
	/// <summary>
	/// Comment downloading helper methods
	/// </summary>
	public static class CommentDownloader
	{
		public async static Task<int> GetRootPostId(int postId)
		{
			var j = await JsonDownloader.Download(new Uri($"{Locations.GetPost}?id={postId}")).ConfigureAwait(false);
			return j["posts"][0]["threadId"].Value<int>();
		}
		public async static Task<CommentThread> TryDownloadThreadById(int threadId, SeenPostsManager seenPostsManager, AuthenticationManager authManager, LatestChattySettings settings, ThreadMarkManager markManager, IgnoreManager ignoreManager)
		{
			var threadJson = await JsonDownloader.Download(new Uri($"{Locations.GetThread}?id={threadId}")).ConfigureAwait(false);
			var threads = await ParseThreads(threadJson, seenPostsManager, authManager, settings, markManager, ignoreManager).ConfigureAwait(false);
			return threads.FirstOrDefault();
		}

		public async static Task<List<CommentThread>> ParseThreads(JToken chatty, SeenPostsManager seenPostsManager, AuthenticationManager services, LatestChattySettings settings, ThreadMarkManager markManager, IgnoreManager ignoreManager)
		{
			if (chatty == null) return null;
			var threadCount = chatty["threads"].Count();
			var parsedChatty = new CommentThread[threadCount];
			await Task.Run(() =>
			{
				Parallel.For(0, threadCount, i =>
				{
					var thread = chatty["threads"][i];
					var t = TryParseThread(thread, 0, seenPostsManager, services, settings, markManager, ignoreManager);
					t.GetAwaiter().GetResult();
					parsedChatty[i] = t.Result;
				});
			}).ConfigureAwait(false);

			await CoreApplication.MainView.CoreWindow.Dispatcher.RunOnUiThreadAndWait(CoreDispatcherPriority.Normal, () =>
			{
				foreach (var thread in parsedChatty)
				{
					if (thread == null) continue;
					thread.RecalculateDepthIndicators();
				}
			}).ConfigureAwait(false);

			var list = parsedChatty.Where(t => t != null).ToList();
#if GENERATE_THREADS
			if (System.Diagnostics.Debugger.IsAttached)
			{
				list.Add(ChattyHelper.GenerateMassiveThread(services, seenPostsManager));
			}
#endif
			return list;
		}

		#region Private Helpers
		public async static Task<CommentThread> TryParseThread(JToken jsonThread, int depth, SeenPostsManager seenPostsManager, AuthenticationManager services, LatestChattySettings settings, ThreadMarkManager markManager, IgnoreManager ignoreManager, string originalAuthor = null, bool storeCount = true)
		{
			var threadPosts = jsonThread["posts"];

			var firstJsonComment = threadPosts.First(j => j["id"].ToString().Equals(jsonThread["threadId"].ToString()));

			var rootComment = await TryParseCommentFromJson(firstJsonComment, null, seenPostsManager, services, ignoreManager).ConfigureAwait(false); //Get the first comment, this is what we'll add everything else to.

			if (rootComment == null) return null;

			if (!settings.UseMainDetail) seenPostsManager.MarkCommentSeen(rootComment.Id);
			var thread = new CommentThread(rootComment);
			var markType = markManager.GetMarkType(thread.Id);
			if (markType == MarkType.Unmarked)
			{
				//If it's not marked, find out if it should be collapsed because of auto-collapse.
				if (settings.ShouldAutoCollapseCommentThread(thread))
				{
					await markManager.MarkThread(thread.Id, MarkType.Collapsed, true).ConfigureAwait(false);
					thread.IsCollapsed = true;
				}
			}
			else
			{
				thread.IsPinned = markType == MarkType.Pinned;
				thread.IsCollapsed = markType == MarkType.Collapsed;
			}

			await RecursiveAddComments(thread, rootComment, threadPosts, seenPostsManager, services, ignoreManager).ConfigureAwait(false);
			thread.HasNewReplies = thread.Comments.Any(c => c.IsNew);

			return thread;
		}

		private async static Task RecursiveAddComments(CommentThread thread, Comment parent, JToken threadPosts, SeenPostsManager seenPostsManager, AuthenticationManager services, IgnoreManager ignoreManager)
		{
			await thread.AddReply(parent, false).ConfigureAwait(true);
			var childPosts = threadPosts.Where(c => c["parentId"].Value<long>().Equals(parent.Id));

			foreach (var reply in childPosts)
			{
				var c = await TryParseCommentFromJson(reply, parent, seenPostsManager, services, ignoreManager).ConfigureAwait(true);
				if (c != null)
				{
					await RecursiveAddComments(thread, c, threadPosts, seenPostsManager, services, ignoreManager).ConfigureAwait(true);
				}
			}

		}

		public async static Task<Comment> TryParseCommentFromJson(JToken jComment, Comment parent, SeenPostsManager seenPostsManager, AuthenticationManager services, IgnoreManager ignoreManager)
		{
			var commentId = (int)jComment["id"];
			var parentId = (int)jComment["parentId"];
			var category = (PostCategory)Enum.Parse(typeof(PostCategory), ParseJTokenToDefaultString(jComment["category"], "ontopic"));
			var author = ParseJTokenToDefaultString(jComment["author"], string.Empty);
			var date = jComment["date"].ToString();
			var body = WebUtility.HtmlDecode(ParseJTokenToDefaultString(jComment["body"], string.Empty).Replace("<a target=\"_blank\" rel=\"nofollow\"", " <a target=\"_blank\"").Replace("\r<br />", "\n").Replace("<br />", "\n").Replace(char.ConvertFromUtf32(8232), "\n"));//8232 is Unicode LINE SEPARATOR.  Saw this occur in post ID 34112371.
			var preview = HtmlRemoval.StripTagsRegexCompiled(body.Substring(0, Math.Min(body.Length, 500)).Replace('\n', ' '));
			//var isTenYearUser = await flairManager.IsTenYearUser(author);
			var c = new Comment(commentId, category, author, date, preview, body, parent != null ? parent.Depth + 1 : 0, parentId, false, services, seenPostsManager);
			if (await ignoreManager.ShouldIgnoreComment(c).ConfigureAwait(false)) return null;

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
					case "wow":
						c.WowCount = count;
						break;
					case "aww":
						c.AwwCount = count;
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
