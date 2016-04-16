using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Networking;
using Latest_Chatty_8.Settings;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Latest_Chatty_8.Common
{
	public static class ChattyHelper
	{
		public async static Task<Tuple<bool, string>> ReplyToComment(this Comment commentToReplyTo, string content, AuthenticationManager authenticationManager)
		{
			return await ChattyHelper.PostComment(content, authenticationManager, commentToReplyTo.Id.ToString());
		}

		public async static Task<Tuple<bool, string>> PostRootComment(string content, AuthenticationManager authenticationManager)
		{
			return await ChattyHelper.PostComment(content, authenticationManager);
		}

		private async static Task<Tuple<bool, string>> PostComment(string content, AuthenticationManager authenticationManager, string parentId = null)
		{
			var message = string.Empty;
			var data = new List<KeyValuePair<string, string>> {
				new KeyValuePair<string, string>("text", content),
				new KeyValuePair<string, string>("parentId", parentId != null ? parentId : "0")
			};

			Newtonsoft.Json.Linq.JObject parsedResponse;
			using (var response = await POSTHelper.Send(Locations.PostUrl, data, true, authenticationManager))
			{
				parsedResponse = Newtonsoft.Json.Linq.JObject.Parse(await response.Content.ReadAsStringAsync());
			}
			var success = (parsedResponse.Property("result") != null && parsedResponse["result"].ToString().Equals("success", StringComparison.OrdinalIgnoreCase));

			if(!success)
			{
				if(parsedResponse.Property("message") != null)
				{
					message = parsedResponse["message"].ToString();
				}
				else
				{
					message = "There was a problem posting, please try again later.";
				}
				var tc = new Microsoft.ApplicationInsights.TelemetryClient();
				tc.TrackEvent("APIPostException", new Dictionary<string, string> { {"text", content }, { "replyingTo", parentId }, { "response", parsedResponse.ToString() } });
			}

			return new Tuple<bool, string>(success, message);
		}

#if DEBUG
		public static CommentThread GenerateMassiveThread(AuthenticationManager authMgr, LatestChattySettings settings, SeenPostsManager seenPostsManager)
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
			var root = new Comment(int.MaxValue - numReplies, PostCategory.stupid, "HugeTest", DateTime.Now.ToString(), "This is a massive thread test.", body, 0, 0, false, authMgr, seenPostsManager);
			var ct = new CommentThread(root, settings);
			var random = new Random();
			for (int i = 0; i < numReplies; i++)
			{
				var reply = new Comment(int.MaxValue - (numReplies - 1) + i, PostCategory.ontopic, "HugeTest" + i.ToString(), DateTime.Now.ToString(), body.Substring(0, 150), i.ToString() + body, i % 10, root.Id, false, authMgr, seenPostsManager);
				ct.AddReply(reply);
			}
			return ct;
		}

		//This is not a very good generator, but it'll do for now.
		public static void GenerateNewThreadJson(ref Newtonsoft.Json.Linq.JToken events)
		{
			var random = new Random();
			var postId = random.Next(133981335, 143981335);
			var newPostJson = "{\"eventId\":3160205,\"eventDate\":\"" + DateTime.Now.ToString() + "\",\"eventType\":\"newPost\",\"eventData\":{\"postId\":33981335,\"post\":{\"id\":" + postId.ToString() + ",\"threadId\":" + postId.ToString() + ",\"parentId\":0,\"author\":\"TESTPOST\",\"category\":\"ontopic\",\"date\":\"" + DateTime.Now.ToString() + "\",\"body\":\"This is a test.\",\"lols\":[]}}}";

			var array = new Newtonsoft.Json.Linq.JArray();
			foreach(var e in events["events"])
			{
				array.Add(e);
			}
			array.Add(Newtonsoft.Json.Linq.JObject.Parse(newPostJson));
			events["events"] = array;
		}
#endif
	}
}
