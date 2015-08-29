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
				list.Add(GenerateMassiveThread(services, settings));
			}
#endif
			return list;
		}

		#region Private Helpers
		async private static Task<CommentThread> ParseThread(JToken jsonThread, int depth, SeenPostsManager seenPostsManager, AuthenticationManager services, LatestChattySettings settings, ThreadMarkManager markManager, string originalAuthor = null, bool storeCount = true)
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

#if DEBUG
		private static CommentThread GenerateMassiveThread(AuthenticationManager authMgr, LatestChattySettings settings)
		{
			var numReplies = 1000;
			var body = @"Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas elementum feugiat imperdiet. Sed vehicula nulla vitae lorem vulputate elementum. Sed suscipit, mi vitae vulputate vehicula, risus risus porttitor eros, vitae lobortis enim nisl eget enim. Nulla eu justo et risus rutrum sagittis. Donec varius porttitor diam, eget placerat nisi tincidunt in. Donec elementum purus eget nunc lobortis scelerisque. Sed auctor feugiat lacus id sollicitudin. Etiam sodales in augue lobortis placerat. Nulla facilisi. Vivamus vehicula at risus vitae mattis. Fusce finibus eros odio, ut porta diam fringilla ac.<br/>
<br/>
Fusce maximus mi id ante lobortis dignissim. Integer molestie urna vel nisl varius, eget porta orci tincidunt. Nulla ac ornare metus. Suspendisse euismod non lacus dignissim sodales. Donec vitae magna vitae augue aliquet consequat rhoncus quis ante. Sed ut felis velit. Proin in ligula ex. Maecenas malesuada imperdiet fringilla. Nunc interdum consectetur magna, vitae ultrices magna dignissim a. Curabitur a faucibus mi, vel bibendum arcu. Ut vel euismod nibh, vel lacinia lorem. Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas. Donec et tempor est. Etiam dapibus eget nunc vitae dapibus. Fusce lorem turpis, faucibus eu iaculis tempor, blandit eu metus. Aenean fermentum placerat faucibus.<br/>
<br/>
Aenean eget ullamcorper dolor. Aliquam tempus commodo elit luctus commodo. Cras in nunc eleifend, vestibulum sapien ut, fermentum ligula. In semper velit id metus interdum, nec fermentum erat pretium. Mauris ac ultricies ligula. Mauris sed mauris vel est lacinia eleifend. Sed eleifend ullamcorper metus, vel gravida justo ullamcorper vitae. Aliquam ac venenatis sem, porta ultricies urna. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia Curae; Vivamus a pretium arcu. Donec dapibus, metus id venenatis scelerisque, nulla nulla euismod risus, vel varius nisl est non diam. Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas.<br/>
<br/>
Cras rhoncus massa ac est viverra, sit amet blandit quam maximus. Maecenas volutpat gravida lectus sit amet efficitur. Nulla consectetur lacinia neque, ac viverra urna consectetur non. Praesent tellus dui, consectetur at malesuada at, accumsan ut leo. Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas. Fusce varius gravida diam ut viverra. Aliquam et erat vel lacus iaculis feugiat sed in leo. Proin et faucibus justo. Integer vel maximus quam. Sed maximus bibendum dapibus.<br/>
<br/>
Donec interdum urna sit amet neque congue, vitae scelerisque leo consequat. Nullam erat justo, tempor nec erat sed, maximus porta dui. Phasellus feugiat ligula id arcu vulputate bibendum. Phasellus odio quam, ultrices id iaculis vitae, iaculis ut arcu. Donec faucibus sapien vel ligula imperdiet, quis dignissim dui vestibulum. Mauris consectetur vitae nisl ac aliquam. Vestibulum pharetra vel arcu sit amet lobortis. Nunc non felis id lorem imperdiet iaculis id at dolor. Sed fringilla ultricies dui eleifend laoreet. Phasellus et justo nulla. Vestibulum varius aliquam erat, ac iaculis nisl tempor nec. Nunc auctor lacinia sapien eu condimentum. Aenean ornare ullamcorper sollicitudin. Maecenas dapibus lacus augue, ut ullamcorper magna dapibus eget. Maecenas interdum vulputate nisl, a viverra sem posuere porttitor. Pellentesque quis pharetra ex, a vulputate massa.";
			var root = new Comment(int.MaxValue - numReplies, PostCategory.stupid, "HugeTest", DateTime.Now.ToString(), "This is a massive thread test.", body, 0, 0, true, authMgr);
			var ct = new CommentThread(root, settings);
			var random = new Random();
			for (int i = 0; i < numReplies; i++)
			{
				var reply = new Comment(int.MaxValue - (numReplies - 1) + i, PostCategory.ontopic, "HugeTest" + i.ToString(), DateTime.Now.ToString(), body.Substring(0, 150), i.ToString() + body, i % 10, root.Id, random.Next() % 2 == 0, authMgr);
				ct.AddReply(reply);
			}
			return ct;
		}
#endif
		#endregion
	}
}
