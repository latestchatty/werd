using System;
using System.Net;
using System.Windows;
using System.Windows.Input;
using System.Threading;

namespace LatestChatty.Classes
{
	//TODO: Implement async pattern.
	public class GETDownloader
	{
		public delegate void GETDelegate(IAsyncResult result);
		GETDelegate getCallback;
		HttpWebRequest request;
		protected bool cancelled;

		public string Uri { get; private set; }


		public GETDownloader(string getURI, GETDelegate callback)
		{
			this.Uri = getURI;
			getCallback = callback;
		}

		public void Start()
		{
			this.request = (HttpWebRequest)HttpWebRequest.Create(this.Uri);
			this.request.Method = "GET";
			this.request.Headers[HttpRequestHeader.CacheControl] = "no-cache";
			//TODO: Re-Implement GET credentials.
			//this.request.Credentials = CoreServices.Instance.Credentials;

			try
			{
				IAsyncResult token = request.BeginGetResponse(new AsyncCallback(ResponseCallback), request);
			}
			catch (WebException)
			{
				//TODO: Catch cancellation exception and throw everything else.
				System.Diagnostics.Debugger.Break();
			}
		}

		public void Cancel()
		{
			request.Abort();
			System.Diagnostics.Debug.WriteLine("Cancelling download for {0}", request.RequestUri);
			this.cancelled = true;
		}

		public void ResponseCallback(IAsyncResult result)
		{
			if (!this.cancelled)
			{
				InvokeDelegate(result);
			}
			else
			{
				System.Diagnostics.Debug.WriteLine("Skipping callback because request was cancelled.");
			}
		}

		virtual protected void InvokeDelegate(IAsyncResult result)
		{
			getCallback(result);
		}
	}
}
