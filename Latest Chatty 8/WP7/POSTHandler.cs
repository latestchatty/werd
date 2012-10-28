using System;
using System.Net;
using System.Windows;
using System.Windows.Input;
using System.Threading;
using System.IO;
using Latest_Chatty_8;
using Latest_Chatty_8.Networking;

namespace LatestChatty.Classes
{
	public class POSTHandler
	{
		private string content;
		public delegate void POSTDelegate(bool success);
		POSTDelegate postCompleteEvent;
		private bool showExceptionMessageBox;

		public POSTHandler(string content, int replyingToId, POSTDelegate callback)
		{
			//Will url encoding help new lines? If not, it'll at least help a lot of other stuff that was probably broken...
			//Nope.  Ok, so maybe replacing newline with %0A
			//... newlines in a text box appear to have just \r ... even when Environment.NewLine is \r\n??
			var encodedBody = Uri.EscapeUriString(content.Replace("\r", "\r\n"));
			content = "body=" + encodedBody;

			if (replyingToId != -1)
			{
				content += "&parent_id=" + replyingToId;
			}

			System.Diagnostics.Debug.WriteLine("Posting: {0}", encodedBody);

			this.content = content;
			this.postCompleteEvent = callback;

			HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(Locations.PostUrl);
			request.Method = "POST";
			request.ContentType = "application/x-www-form-urlencoded";
			request.Credentials = CoreServices.Instance.Credentials;

			this.showExceptionMessageBox = true;

			IAsyncResult token = request.BeginGetRequestStream(new AsyncCallback(BeginPostCallback), request);
		}

		public void BeginPostCallback(IAsyncResult result)
		{
			HttpWebRequest request = result.AsyncState as HttpWebRequest;

			Stream requestStream = request.EndGetRequestStream(result);
			StreamWriter streamWriter = new StreamWriter(requestStream);
			streamWriter.Write(content);
			streamWriter.Flush();
			streamWriter.Dispose();

			request.BeginGetResponse(new AsyncCallback(ResponseCallback), request);
		}

		public void ResponseCallback(IAsyncResult result)
		{
			var failureMessage = string.Empty;

			try
			{
				HttpWebRequest request = result.AsyncState as HttpWebRequest;
				HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(result);

				//Doesn't seem like the API is actually returning failure codes, but... might as well handle it in case it does some time.
				if (response.StatusCode != HttpStatusCode.OK)
				{
					failureMessage = "Bad response code.  Check your username and password.";
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine("Posting failed because: {0}", ex);
				failureMessage = "Posting failed.";
			}

			finally
			{
				//TODO: Show an exception if we couldn't post.
				//Windows.UI.Core.CoreDispatcher.Dispatcher.RunAsync(() =>
				//		{
				//			if (!success && this.showExceptionMessageBox)
				//			{
				//				MessageBox.Show(failureMessage);
				//			}
				//			postCompleteEvent(success);
				//		});
			}
		}
	}
}
