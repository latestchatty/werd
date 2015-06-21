using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Shared.Networking;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Latest_Chatty_8.Shared
{
	public static class ChattyHelper
	{
		async public static Task<bool> ReplyToComment(this Comment commentToReplyTo, string content, AuthenticaitonManager services)
		{
			return await ChattyHelper.PostComment(content, services, commentToReplyTo.Id.ToString());
		}

		async public static Task<bool> PostRootComment(string content, AuthenticaitonManager services)
		{
			return await ChattyHelper.PostComment(content, services);
		}

		async private static Task<bool> PostComment(string content, AuthenticaitonManager services, string parentId = null)
		{
			if (content.Length <= 5)
			{
				var dlg = new Windows.UI.Popups.MessageDialog("Post something longer.");
				await dlg.ShowAsync();
				return false;
			}

			var data = new List<KeyValuePair<string, string>> {
				new KeyValuePair<string, string>("text", content),
				new KeyValuePair<string, string>("parentId", parentId != null ? parentId : "0")
			};

			//:TODO: Handle failures better.
			var response = await POSTHelper.Send(Locations.PostUrl, data, true, services);
			//:TODO: Immediately add to chatty so we don't have to wait for a refresh?

			return true;
		}
	}
}
