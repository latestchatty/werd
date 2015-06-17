using Latest_Chatty_8.DataModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Core;

namespace Latest_Chatty_8.Common
{
	public class PinManager
	{
		private ObservableCollection<CommentThread> pinnedThreads;
		public ReadOnlyObservableCollection<CommentThread> PinnedThreads { get; private set; }

		public PinManager()
		{
			this.pinnedThreads = new ObservableCollection<CommentThread>();
			this.PinnedThreads = new ReadOnlyObservableCollection<CommentThread>(this.pinnedThreads);
		}

		private ReaderWriterLockSlim ColLock = new ReaderWriterLockSlim();

		async public Task PinThread(int id)
		{
			//await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			//{
			//	this.ColLock.EnterReadLock();
			//	var thread = this.chatty.SingleOrDefault(t => t.Id == id);
			//	if (ChattyLock.IsReadLockHeld) ChattyLock.ExitReadLock();
			//	if (thread != null)
			//	{
			//		thread.IsPinned = true;
			//	}
			//	this.CleanupChattyList();
			//});
			//await this.MarkThread(id, "pinned");
		}

		async public Task UnPinThread(int id)
		{
			//await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			//{
			//	ChattyLock.EnterReadLock();
			//	var thread = this.chatty.SingleOrDefault(t => t.Id == id);
			//	if (ChattyLock.IsReadLockHeld) ChattyLock.ExitReadLock();
			//	if (thread != null)
			//	{
			//		thread.IsPinned = false;
			//		this.CleanupChattyList();
			//	}
			//});
			//await this.MarkThread(id, "unmarked");
		}

		async public Task GetPinnedPosts()
		{
			////:TODO: Handle updating this stuff more gracefully.
			//var pinnedIds = await GetPinnedPostIds();
			////:TODO: Only need to grab stuff that isn't in the active chatty already.
			////:BUG: If this occurs before the live update happens, we'll fail to add at that point.
			//ChattyLock.EnterReadLock();
			//var threads = await CommentDownloader.DownloadThreads(pinnedIds.Where(t => !this.Chatty.Any(ct => ct.Id.Equals(t))));
			//if (ChattyLock.IsReadLockHeld) ChattyLock.ExitReadLock();

			////Nothing pinned, bail early.
			//if (threads.Count == 0) { return; }

			//ChattyLock.EnterWriteLock();
			//await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			//{
			//	//If it's not marked as pinned from the server, but it is locally, unmark it.
			//	//It probably got unmarked somewhere else.
			//	foreach (var t in this.chatty.Where(t => t.IsPinned))
			//	{
			//		if (!threads.Any(pt => pt.Id == t.Id))
			//		{
			//			t.IsPinned = false;
			//		}
			//	}

			//	foreach (var thread in threads.OrderByDescending(t => t.Id))
			//	{
			//		thread.IsPinned = true;
			//		var existingThread = this.chatty.FirstOrDefault(t => t.Id == thread.Id);
			//		if (existingThread == null)
			//		{
			//			//Didn't exist in the list, add it.
			//			this.chatty.Add(thread);
			//		}
			//		else
			//		{
			//			//Make sure if it's in the active chatty that it's marked as pinned.
			//			existingThread.IsPinned = true;
			//			if (existingThread.Comments.Count != thread.Comments.Count)
			//			{
			//				foreach (var c in thread.Comments)
			//				{
			//					if (!existingThread.Comments.Any(c1 => c1.Id == c.Id))
			//					{
			//						thread.AddReply(c); //Add new replies cleanly so we don't lose focus and such.
			//					}
			//				}
			//			}
			//		}
			//	}
			//});
			//if (ChattyLock.IsWriteLockHeld) ChattyLock.ExitWriteLock();
		}

		async public Task<IEnumerable<int>> GetPinnedPostIds()
		{
			return new List<int>();
			//var pinnedPostIds = new List<int>();
			//if (CoreServices.Instance.LoggedIn)
			//{
			//	var parsedResponse = await JSONDownloader.Download(Locations.GetMarkedPosts + "?username=" + WebUtility.UrlEncode(CoreServices.Instance.Credentials.UserName));
			//	foreach (var post in parsedResponse["markedPosts"].Children())
			//	{
			//		pinnedPostIds.Add((int)post["id"]);
			//	}

			//}
			//return pinnedPostIds;
		}


	}
}
