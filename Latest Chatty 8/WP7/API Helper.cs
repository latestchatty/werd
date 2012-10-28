//using Latest_Chatty_8.Common;
//using System.Collections.Generic;

//namespace LatestChatty.Classes
//{
//	public class API_Helper
//	{
//		//private readonly Queue<XMLDownloader> downloads;
//		//private XMLDownloader activeDownload = null;

//		public API_Helper()
//		{
//			downloads = new Queue<XMLDownloader>();
//		}

//		void DoWork()
//		{
//			if (this.downloads.Count > 0 && this.activeDownload == null)
//			{

//				this.activeDownload = this.downloads.Dequeue();
//				this.activeDownload.Finished += (o, e) => this.FinishedDownloadingActive();
//				System.Diagnostics.Debug.WriteLine("Starting download to uri: {0}", this.activeDownload.Uri);
//				this.activeDownload.Start();
//			}
//		}

//		//public void AddDownload(string uri, LatestChatty.Classes.XMLDownloader.XMLDownloaderCallback callback)
//		//{
//		//	System.Diagnostics.Debug.WriteLine("AddDownload to uri: {0}", uri);
//		//	this.downloads.Enqueue(new XMLDownloader(uri, callback));
//		//	this.DoWork();
//		//}

//		public void CancelDownloads()
//		{
//			System.Diagnostics.Debug.WriteLine("CancelDownloads");
//			if (this.activeDownload != null)
//			{
//				this.activeDownload.Cancel();
//			}
//			this.downloads.Clear();
//			this.activeDownload = null;
//		}

//		private void FinishedDownloadingActive()
//		{
//			System.Diagnostics.Debug.WriteLine("FinishedDownloadingActive");
//			this.activeDownload = null;
//			this.DoWork();
//		}
//	}
//}
