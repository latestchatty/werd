using Common;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Werd.DataModel;

namespace Werd.Common
{
	public static class ChattyHelper
	{
		private static readonly Regex _urlParserRegex = new Regex(@"https?://(www.)?shacknews\.com\/chatty\?.*id=(?<id>\d*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		public static bool TryGetThreadIdFromUrl(string find, out int threadId)
		{
			if (!string.IsNullOrWhiteSpace(find))
			{
				var match = _urlParserRegex.Match(find);
				if (match.Success)
				{
					if (int.TryParse(match.Groups["id"].Value, out threadId))
					{
						return true;
					}
				}
			}
			threadId = 0;
			return false;
		}

		public async static Task<Tuple<bool, string>> ReplyToComment(this Comment commentToReplyTo, string content, AuthenticationManager authenticationManager)
		{
			return await PostHelper.PostComment(content, authenticationManager, commentToReplyTo.Id.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);
		}

		public async static Task<Tuple<bool, string>> PostRootComment(string content, AuthenticationManager authenticationManager)
		{
			return await PostHelper.PostComment(content, authenticationManager).ConfigureAwait(false);
		}

#if DEBUG
		public static async Task<CommentThread> GenerateMassiveThread(AuthenticationManager authMgr, SeenPostsManager seenPostsManager)
		{
			var numReplies = 100;
			var body = @"Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas elementum feugiat imperdiet. Sed vehicula nulla vitae lorem vulputate elementum. Sed suscipit, mi vitae vulputate vehicula, risus risus porttitor eros, vitae lobortis enim nisl eget enim. Nulla eu justo et risus rutrum sagittis. Donec varius porttitor diam, eget placerat nisi tincidunt in. Donec elementum purus eget nunc lobortis scelerisque. Sed auctor feugiat lacus id sollicitudin. Etiam sodales in augue lobortis placerat. Nulla facilisi. Vivamus vehicula at risus vitae mattis. Fusce finibus eros odio, ut porta diam fringilla ac.<br/>
<br/>
Fusce maximus mi id ante lobortis dignissim. Integer molestie urna vel nisl varius, eget porta orci tincidunt. Nulla ac ornare metus. Suspendisse euismod non lacus dignissim sodales. Donec vitae magna vitae augue aliquet consequat rhoncus quis ante. Sed ut felis velit. Proin in ligula ex. Maecenas malesuada imperdiet fringilla. Nunc interdum consectetur magna, vitae ultrices magna dignissim a. Curabitur a faucibus mi, vel bibendum arcu. Ut vel euismod nibh, vel lacinia lorem. Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas. Donec et tempor est. Etiam dapibus eget nunc vitae dapibus. Fusce lorem turpis, faucibus eu iaculis tempor, blandit eu metus. Aenean fermentum placerat faucibus.<br/>
<br/>
Aenean eget ullamcorper dolor. Aliquam tempus commodo elit luctus commodo. Cras in nunc eleifend, vestibulum sapien ut, fermentum ligula. In semper velit id metus interdum, nec fermentum erat pretium. Mauris ac ultricies ligula. Mauris sed mauris vel est lacinia eleifend. Sed eleifend ullamcorper metus, vel gravida justo ullamcorper vitae. Aliquam ac venenatis sem, porta ultricies urna. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia Curae; Vivamus a pretium arcu. Donec dapibus, metus id venenatis scelerisque, nulla nulla euismod risus, vel varius nisl est non diam. Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas.<br/>
<br/>
Cras rhoncus massa ac est viverra, sit amet blandit quam maximus. Maecenas volutpat gravida lectus sit amet efficitur. Nulla consectetur lacinia neque, ac viverra urna consectetur non. Praesent tellus dui, consectetur at malesuada at, accumsan ut leo. Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas. Fusce varius gravida diam ut viverra. Aliquam et erat vel lacus iaculis feugiat sed in leo. Proin et faucibus justo. Integer vel maximus quam. Sed maximus bibendum dapibus.<br/>
<br/>
Donec interdum urna sit amet neque congue, vitae scelerisque leo consequat. Nullam erat justo, tempor nec erat sed, maximus porta dui. Phasellus feugiat ligula id arcu vulputate bibendum. Phasellus odio quam, ultrices id iaculis vitae, iaculis ut arcu. Donec faucibus sapien vel ligula imperdiet, quis dignissim dui vestibulum. Mauris consectetur vitae nisl ac aliquam. Vestibulum pharetra vel arcu sit amet lobortis. Nunc non felis id lorem imperdiet iaculis id at dolor. Sed fringilla ultricies dui eleifend laoreet. Phasellus et justo nulla. Vestibulum varius aliquam erat, ac iaculis nisl tempor nec. Nunc auctor lacinia sapien eu condimentum. Aenean ornare ullamcorper sollicitudin. Maecenas dapibus lacus augue, ut ullamcorper magna dapibus eget. Maecenas interdum vulputate nisl, a viverra sem posuere porttitor. Pellentesque quis pharetra ex, a vulputate massa.";
			var root = new Comment(int.MaxValue - numReplies, PostCategory.stupid, "HugeTest", DateTime.Now.ToString(CultureInfo.InvariantCulture), "This is a massive thread test.", body, 0, 0, authMgr, seenPostsManager);
			var ct = new CommentThread(root);
			//var random = new Random();
			for (int i = 0; i < numReplies; i++)
			{
				var reply = new Comment(int.MaxValue - (numReplies - 1) + i, PostCategory.ontopic, "HugeTest" + i, DateTime.Now.ToString(CultureInfo.InvariantCulture), body.Substring(0, 150), i + body, i % 10, root.Id, authMgr, seenPostsManager);
				await ct.AddReply(reply).ConfigureAwait(true);
			}
			return ct;
		}

		//This is not a very good generator, but it'll do for now.
		public static void GenerateNewThreadJson(ref JToken events)
		{
			var random = new Random();
			var postId = random.Next(133981335, 143981335);
			var newPostJson = "{\"eventId\":3160205,\"eventDate\":\"" + DateTime.Now + "\",\"eventType\":\"newPost\",\"eventData\":{\"postId\":33981335,\"post\":{\"id\":" + postId + ",\"threadId\":" + postId + ",\"parentId\":0,\"author\":\"TESTPOST\",\"category\":\"ontopic\",\"date\":\"" + DateTime.Now + "\",\"body\":\"This is a test.\",\"lols\":[]}}}";

			var array = new JArray();
			foreach (var e in events["events"])
			{
				array.Add(e);
			}
			array.Add(JObject.Parse(newPostJson));
			events["events"] = array;
		}
#endif
	}
}
