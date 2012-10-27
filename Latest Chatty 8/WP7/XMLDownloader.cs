using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Xml.Linq;
using Windows.UI.Core;

namespace LatestChatty.Classes
{
	public class XMLDownloader : GETDownloader
	{
		public delegate void XMLDownloaderCallback(XDocument response);
		public event EventHandler<EventArgs> Finished;
		private XMLDownloaderCallback finishedCallback;

		public XMLDownloader(string getURI, XMLDownloaderCallback callback)
			: base(getURI, null)
		{
			finishedCallback = callback;
		}

		protected override void InvokeDelegate(IAsyncResult result)
		{
			try
			{
				WebResponse response = ((HttpWebRequest)result.AsyncState).EndGetResponse(result);
				StreamReader reader = new StreamReader(response.GetResponseStream());
				string responseString = reader.ReadToEnd();
				XDocument XMLResponse = XDocument.Parse(responseString);

				//TODO: This is rasied on its own thread, so it's up to the callback to find the UI thread.
				//TODO: A lot of expensive stuff always happens here
				// We should consider allowing this to run on a separate thread and having the responders be responsible for getting the changes to the UI thread.
				finishedCallback(XMLResponse);
			}
			catch
			{
				if (!this.cancelled) finishedCallback(null);
			}

			if (this.Finished != null)
			{
				if (!this.cancelled) this.Finished(this, EventArgs.Empty);
			}
		}
	}
}
