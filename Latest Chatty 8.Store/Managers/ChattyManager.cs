using Latest_Chatty_8.Common;
using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Networking;
using Latest_Chatty_8.Settings;
using Microsoft.ApplicationInsights;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Core;

namespace Latest_Chatty_8.Managers
{
	public class ChattyManager : BindableBase, IDisposable
	{
		private int lastEventId = 0;

		private Timer chattyRefreshTimer = null;
		private bool chattyRefreshEnabled = false;
		private DateTime lastChattyRefresh = DateTime.MinValue;
		private SeenPostsManager seenPostsManager;
		private AuthenticationManager authManager;
		private LatestChattySettings settings;
		private ThreadMarkManager markManager;
		private UserFlairManager flairManager;
		private INotificationManager notificationManager;

		private ChattyFilterType currentFilter = ChattyFilterType.All;
		private ChattySortType currentSort = ChattySortType.Default;
		private string searchText = string.Empty;

		private MoveableObservableCollection<CommentThread> chatty;

		private MoveableObservableCollection<CommentThread> filteredChatty;
		/// <summary>
		/// Gets the active chatty
		/// </summary>
		public ReadOnlyObservableCollection<CommentThread> Chatty
		{
			get;
			private set;
		}

		private SemaphoreSlim ChattyLock = new SemaphoreSlim(1);

		private DateTime lastLolUpdate = DateTime.MinValue;
		private IgnoreManager ignoreManager;

		public ChattyManager(SeenPostsManager seenPostsManager, AuthenticationManager authManager, LatestChattySettings settings, ThreadMarkManager markManager, UserFlairManager flairManager, INotificationManager notificationManager, IgnoreManager ignoreManager)
		{
			this.chatty = new MoveableObservableCollection<CommentThread>();
			this.filteredChatty = new MoveableObservableCollection<CommentThread>();
			this.Chatty = new ReadOnlyObservableCollection<CommentThread>(this.filteredChatty);
			this.ignoreManager = ignoreManager;
			this.seenPostsManager = seenPostsManager;
			this.authManager = authManager;
			this.settings = settings;
			this.flairManager = flairManager;
			this.notificationManager = notificationManager;
			this.seenPostsManager.Updated += SeenPostsManager_Updated;
			this.markManager = markManager;
			this.markManager.PostThreadMarkChanged += MarkManager_PostThreadMarkChanged;
			this.authManager.PropertyChanged += AuthManager_PropertyChanged;
		}

		private bool npcUnsortedChattyPosts = false;
		public bool UnsortedChattyPosts
		{
			get { return this.npcUnsortedChattyPosts; }
			set { this.SetProperty(ref this.npcUnsortedChattyPosts, value); }
		}

		private bool npcIsFullUpdateHappening = false;
		public bool IsFullUpdateHappening
		{
			get { return npcIsFullUpdateHappening; }
			set { this.SetProperty(ref this.npcIsFullUpdateHappening, value); }
		}

		private bool npcChattyIsLoaded;
		public bool ChattyIsLoaded
		{
			get { return this.npcChattyIsLoaded; }
			set { this.SetProperty(ref this.npcChattyIsLoaded, value); }
		}

		private int npcNewThreadCount = 0;
		public int NewThreadCount
		{
			get { return this.npcNewThreadCount; }
			set { this.SetProperty(ref this.npcNewThreadCount, value); }
		}

		private bool npcNewRepliesToUser = false;
		public bool NewRepliesToUser
		{
			get { return this.npcNewRepliesToUser; }
			set { this.SetProperty(ref this.npcNewRepliesToUser, value); }
		}

		public bool ShouldFullRefresh()
		{
			return this.lastChattyRefresh == DateTime.MinValue || DateTime.Now.Subtract(this.lastChattyRefresh).TotalMinutes > 15;
		}

		/// <summary>
		/// Forces a full refresh of the chatty.
		/// </summary>
		/// <returns></returns>
		private async Task RefreshChattyFull()
		{
			await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
			{
				this.IsFullUpdateHappening = true;
				this.NewThreadCount = 0;
				this.NewRepliesToUser = false;
				await this.ChattyLock.WaitAsync();
				var keep = this.chatty.Where(ct => ct.IsPinned && ct.IsExpired).ToList();
				this.chatty.Clear();
				foreach (var t in keep)
				{
					this.chatty.Add(t);
				}
				this.ChattyLock.Release();
			});
			var latestEventJson = await JSONDownloader.Download(Latest_Chatty_8.Networking.Locations.GetNewestEventId);
			this.lastEventId = (int)latestEventJson["eventId"];
			var downloadTimer = new TelemetryTimer("ChattyDownload");
			downloadTimer.Start();
			var chattyJson = await JSONDownloader.Download(Latest_Chatty_8.Networking.Locations.Chatty);
			downloadTimer.Stop();
			var parsedChatty = await CommentDownloader.ParseThreads(chattyJson, this.seenPostsManager, this.authManager, this.settings, this.markManager, this.flairManager, ignoreManager);
			await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
			{
				await this.ChattyLock.WaitAsync();
				foreach (var comment in parsedChatty)
				{
					this.chatty.Add(comment);
				}
				this.ChattyLock.Release();
			});
			//await GetPinnedPosts();
			await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
			{
				this.FilterChattyInternal(this.currentFilter);
				await this.CleanupChattyList();
				this.IsFullUpdateHappening = false;
				this.ChattyIsLoaded = true;
			});
		}

		public void StartAutoChattyRefresh()
		{
			if (this.chattyRefreshEnabled) return;
			this.chattyRefreshEnabled = true;
			if (this.chattyRefreshTimer == null)
			{
				this.chattyRefreshTimer = new Timer(async (a) => await RefreshChattyInternal(), null, 0, Timeout.Infinite);
			}
		}

		public void StopAutoChattyRefresh()
		{
			System.Diagnostics.Debug.WriteLine("Stopping chatty refresh.");
			this.chattyRefreshEnabled = false;
			if (this.chattyRefreshTimer != null)
			{
				this.chattyRefreshTimer.Dispose();
				this.chattyRefreshTimer = null;
			}
		}

		public async Task CleanupChattyList()
		{
			try
			{
				await this.ChattyLock.WaitAsync();
				this.FilterChattyInternal(this.currentFilter);
				this.CleanupChattyListInternal();
			}
			catch (Exception e)
			{
				System.Diagnostics.Debug.WriteLine("Exception in CleanupChattyList {0}", e);
			}
			finally
			{
				this.ChattyLock.Release();
			}
		}

		private void CleanupChattyListInternal()
		{
			int position = 0;

			//var timer = new TelemetryTimer("ApplyChattySort", new Dictionary<string, string> { { "sortType", Enum.GetName(typeof(ChattySortType), this.currentSort) } });
			//timer.Start();

			var removedThreads = this.chatty.Where(t => t.IsExpired && (!t.IsPinned && !t.Invisible)).ToList();
			foreach (var item in removedThreads)
			{
				this.chatty.Remove(item);
				if (this.filteredChatty.Contains(item))
				{
					this.filteredChatty.Remove(item);
				}
			}

			var allThreads = this.filteredChatty.Where(t => !t.Invisible).ToList();

			IOrderedEnumerable<CommentThread> orderedThreads;

			switch (this.currentSort)
			{
				case ChattySortType.Inf:
					orderedThreads = allThreads.OrderByDescending(ct => ct.IsPinned).ThenByDescending(ct => ct.NewlyAdded).ThenByDescending(ct => ct.Comments.Sum(c => c.InfCount)).ThenByDescending(t => t.Comments.Max(c => c.Id));
					break;
				case ChattySortType.Lol:
					orderedThreads = allThreads.OrderByDescending(ct => ct.IsPinned).ThenByDescending(ct => ct.NewlyAdded).ThenByDescending(ct => ct.Comments.Sum(c => c.LolCount)).ThenByDescending(t => t.Comments.Max(c => c.Id));
					break;
				case ChattySortType.ReplyCount:
					orderedThreads = allThreads.OrderByDescending(ct => ct.IsPinned).ThenByDescending(ct => ct.NewlyAdded).ThenByDescending(ct => ct.Comments.Count).ThenByDescending(t => t.Comments.Max(c => c.Id));
					break;
				case ChattySortType.HasNewReplies:
					orderedThreads = allThreads.OrderByDescending(ct => ct.IsPinned).ThenByDescending(ct => ct.NewlyAdded).ThenByDescending(ct => ct.HasNewRepliesToUser).ThenByDescending(t => t.Comments.Max(c => c.Id));
					break;
				case ChattySortType.Participated:
					orderedThreads = allThreads.OrderByDescending(ct => ct.IsPinned).ThenByDescending(ct => ct.NewlyAdded).ThenByDescending(ct => ct.UserParticipated).ThenByDescending(t => t.Comments.Max(c => c.Id));
					break;
				default:
					orderedThreads = allThreads.OrderByDescending(ct => ct.IsPinned).ThenByDescending(ct => ct.NewlyAdded).ThenByDescending(t => t.Comments.Max(c => c.Id));
					break;
			}

			foreach (var item in orderedThreads)
			{
				this.filteredChatty.Move(this.filteredChatty.IndexOf(item), position);
				position++;
			}
			this.UnsortedChattyPosts = false;
			this.NewRepliesToUser = false;
			this.MarkAllVisibleCommentThreadsNotNew();

			//timer.Stop();
		}

		public async Task SortChatty(ChattySortType sort)
		{
			try
			{
				await this.ChattyLock.WaitAsync();
				this.currentSort = sort;
				var tc = new Microsoft.ApplicationInsights.TelemetryClient();
				tc.TrackEvent("SortMode", new Dictionary<string, string> { { "mode", sort.ToString() } });
				this.CleanupChattyListInternal();
			}
			finally
			{
				this.ChattyLock.Release();
			}
		}

		public async Task FilterChatty(ChattyFilterType filter)
		{
			try
			{
				await this.ChattyLock.WaitAsync();
				this.FilterChattyInternal(filter);
				this.CleanupChattyListInternal();
			}
			finally
			{
				this.ChattyLock.Release();
			}
		}

		public async Task SearchChatty(string search)
		{
			try
			{
				await this.ChattyLock.WaitAsync();
				this.searchText = search;
				this.FilterChattyInternal(ChattyFilterType.Search);
				this.CleanupChattyListInternal();
			}
			finally
			{
				this.ChattyLock.Release();
			}
		}

		public async Task<CommentThread> FindOrAddThreadByAnyPostId(int anyID)
		{
			CommentThread rootThread = null;
			try
			{
				//This is probably going to get me in trouble at some point in the future.
				while (!this.ChattyIsLoaded)
				{
					await Task.Delay(10);
				}
				await this.ChattyLock.WaitAsync();
				rootThread = this.chatty.FirstOrDefault(ct => ct.Comments.Any(c => c.Id == anyID));

				if (rootThread == null)
				{
					//Time to download it and add it.
					var thread = await CommentDownloader.TryDownloadThreadById(anyID, this.seenPostsManager, this.authManager, this.settings, this.markManager, this.flairManager, this.ignoreManager);
					if (thread != null)
					{
						//If it's expired, we need to prevent it from being removed from the chatty later.  This will keep it live and we'll process events in the thread, but we'll never show it in the chatty view.
						if (thread.IsExpired)
						{
							thread.Invisible = true;
						}
						this.chatty.Add(thread);
						rootThread = thread;
					}
					(new TelemetryClient()).TrackEvent("ChattyManager-LoadingExpiredThread");
				}
			}
			catch (Exception e)
			{
				System.Diagnostics.Debug.WriteLine($"Exception in {nameof(FindOrAddThreadByAnyPostId)} : {e}");
				(new TelemetryClient()).TrackException(e);
			}
			finally
			{
				this.ChattyLock.Release();
			}
			return rootThread;
		}

		private void FilterChattyInternal(ChattyFilterType filter)
		{
			this.MarkAllVisibleCommentThreadsSeen();
			this.filteredChatty.Clear();
			IEnumerable<CommentThread> toAdd = null;
			switch (filter)
			{
				case ChattyFilterType.Participated:
					toAdd = this.chatty.Where(ct => ct.UserParticipated && !ct.IsCollapsed);
					break;
				case ChattyFilterType.HasReplies:
					toAdd = this.chatty.Where(ct => ct.HasRepliesToUser && !ct.IsCollapsed);
					break;
				case ChattyFilterType.New:
					toAdd = this.chatty.Where(ct => ct.HasNewReplies && !ct.IsCollapsed);
					break;
				case ChattyFilterType.News:
					toAdd = this.chatty.Where(ct => ct.Comments.FirstOrDefault()?.AuthorType == AuthorType.Shacknews);
					break;
				case ChattyFilterType.Search:
					if (!string.IsNullOrWhiteSpace(this.searchText))
					{
						toAdd = this.chatty.Where(ct => !ct.IsCollapsed && ct.Comments.Any(c => c.Author.Equals(this.searchText, StringComparison.OrdinalIgnoreCase) || c.Body.ToLower().Contains(this.searchText.ToLower())));
					}
					break;
				case ChattyFilterType.Collapsed:
					toAdd = this.chatty.Where(ct => ct.IsCollapsed);
					break;
				case ChattyFilterType.Pinned:
					toAdd = this.chatty.Where(ct => ct.IsPinned && !ct.IsCollapsed);
					break;
				default:
					//By default show everything that isn't collapsed.
					toAdd = this.chatty.Where(ct => !ct.IsCollapsed);
					break;
			}

			if (toAdd != null)
			{
				toAdd = toAdd.Where(ct => !ct.Invisible);
			}

			this.currentFilter = filter;

			if (toAdd != null)
			{
				foreach (var item in toAdd.ToList())
				{
					this.filteredChatty.Add(item);
				}
			}
		}

		//public async Task SelectPost(int id)
		//{
		//	try
		//	{
		//		await this.ChattyLock.WaitAsync();
		//		foreach (var commentthread in this.chatty)
		//		{
		//			foreach (var comment in commentthread.Comments)
		//			{
		//				comment.IsSelected = comment.Id == id;
		//			}
		//		}
		//	}
		//	finally
		//	{
		//		this.ChattyLock.Release();
		//	}
		//}

		public async Task DeselectAllPostsForCommentThread(CommentThread ct)
		{
			if (ct == null) return;
			try
			{
				await this.ChattyLock.WaitAsync();
				var opCt = this.chatty.SingleOrDefault(ct1 => ct1.Comments[0].Id == ct.Comments[0].Id);
				foreach (var comment in opCt.Comments)
				{
					comment.IsSelected = false;
				}
			}
			finally
			{
				this.ChattyLock.Release();
			}
		}

		private async Task RefreshChattyInternal()
		{
			try
			{
				try
				{
					//If we haven't loaded anything yet, load the whole shebang.
					if (this.ShouldFullRefresh())
					{
						await RefreshChattyFull();
					}
					JToken events = await JSONDownloader.Download((this.settings.RefreshRate == 0 ? Latest_Chatty_8.Networking.Locations.WaitForEvent : Latest_Chatty_8.Networking.Locations.PollForEvent) + "?lastEventId=" + this.lastEventId);
					if (events != null)
					{
#if GENERATE_THREADS
						ChattyHelper.GenerateNewThreadJson(ref events);
#endif
						//System.Diagnostics.Debug.WriteLine("Event Data: {0}", events.ToString());
						foreach (var e in events["events"])
						{
							//var timer = new TelemetryTimer("ParseEvent", new Dictionary<string, string> { { "eventType", (string)e["eventType"] } });
							//timer.Start();
							switch ((string)e["eventType"])
							{
								case "newPost":
									await this.AddNewPost(e);
									break;
								case "categoryChange":
									await this.CategoryChange(e);
									break;
								case "lolCountsUpdate":
									await this.UpdateLolCount(e);
									break;
								default:
									var tc = new Microsoft.ApplicationInsights.TelemetryClient();
									tc.TrackEvent("UnhandledAPIEvent", new Dictionary<string, string> { { "eventData", e.ToString() } });
									System.Diagnostics.Debug.WriteLine("Unhandled event: {0}", e.ToString());
									break;
							}
							//timer.Stop();
						}
						this.lastEventId = events["lastEventId"].Value<int>(); //Set the last event id after we've completed everything successfully.
						this.lastChattyRefresh = DateTime.Now;
					}

					await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
					{
						var locked = await this.ChattyLock.WaitAsync(10);
						try
						{
							if (locked)
							{
								foreach (var thread in this.filteredChatty)
								{
									thread.ForceDateRefresh(); //Force an update to dates to keep the expirations current.
								}
							}
						}
						finally
						{
							if (locked)
							{
								this.ChattyLock.Release();
							}
						}
					});
				}
				catch { /*System.Diagnostics.Debugger.Break();*/ /*Generally anything that goes wrong here is going to be due to network connectivity.  So really, we just want to try again later. */ }
			}
			finally
			{
				if (this.chattyRefreshEnabled)
				{
					this.chattyRefreshTimer.Change(this.settings.RefreshRate * 1000, Timeout.Infinite);
				}
			}
			System.Diagnostics.Debug.WriteLine("Done refreshing.");
		}

		#region WinChatty Event Handlers
		private async Task AddNewPost(JToken e)
		{
			var newPostJson = e["eventData"]["post"];
			var threadRootId = (int)newPostJson["threadId"];
			var parentId = (int)newPostJson["parentId"];

			var unsorted = false;

			if (parentId == 0)
			{
				//Brand new post.
				//Parse it and add it to the top.
				var newComment = await CommentDownloader.TryParseCommentFromJson(newPostJson, null, this.seenPostsManager, this.authManager, this.flairManager, ignoreManager);
				if (newComment != null)
				{
					var newThread = new CommentThread(newComment, this.settings, true);
					if (this.settings.ShouldAutoCollapseCommentThread(newThread))
					{
						await this.markManager.MarkThread(newThread.Id, MarkType.Collapsed, true);
						newThread.IsCollapsed = true;
					}

					await this.ChattyLock.WaitAsync();

					this.chatty.Add(newThread);

					//If we're viewing all posts, all new posts, or our posts and we made the new post, add it to the viewed posts.
					if (this.currentFilter == ChattyFilterType.All
						|| this.currentFilter == ChattyFilterType.New
						|| (this.currentFilter == ChattyFilterType.Participated && newComment.AuthorType == AuthorType.Self)
						|| (this.currentFilter == ChattyFilterType.Search) && newComment.Author.Equals(this.searchText, StringComparison.OrdinalIgnoreCase) || newComment.Body.ToLower().Contains(this.searchText)
						|| (this.currentFilter == ChattyFilterType.Pinned && this.markManager.GetMarkType(newThread.Id) == MarkType.Pinned)
						|| (this.currentFilter == ChattyFilterType.Collapsed && this.markManager.GetMarkType(newThread.Id) == MarkType.Collapsed))
					{
						unsorted = true;
					}
					await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
					{
						this.NewThreadCount++;
					});
					this.ChattyLock.Release();
				}
			}
			else
			{
				await this.ChattyLock.WaitAsync();
				var threadRoot = this.chatty.SingleOrDefault(c => c.Id == threadRootId);
				if (threadRoot != null)
				{
					var parent = threadRoot.Comments.SingleOrDefault(c => c.Id == parentId);
					if (parent != null)
					{
						var newComment = await CommentDownloader.TryParseCommentFromJson(newPostJson, parent, this.seenPostsManager, this.authManager, this.flairManager, this.ignoreManager);
						if (newComment != null)
						{
							if (!this.filteredChatty.Contains(threadRoot))
							{
								if ((this.currentFilter == ChattyFilterType.HasReplies && parent.AuthorType == AuthorType.Self)
									|| (this.currentFilter == ChattyFilterType.Participated && newComment.AuthorType == AuthorType.Self)
									|| this.currentFilter == ChattyFilterType.New
									|| (this.currentFilter == ChattyFilterType.Search) && newComment.Author.Equals(this.searchText, StringComparison.OrdinalIgnoreCase) || newComment.Body.ToLower().Contains(this.searchText)
									|| (this.currentFilter == ChattyFilterType.Pinned && this.markManager.GetMarkType(threadRoot.Id) == MarkType.Pinned)
									|| (this.currentFilter == ChattyFilterType.Collapsed && this.markManager.GetMarkType(threadRoot.Id) == MarkType.Collapsed))
								{
									unsorted = true;
								}
							}
							else
							{
								//If the root thread is already in the filtered list, we're unsorted if we don't already belong to the top post.
								if (this.filteredChatty.Count > 0 && this.filteredChatty[0].Id != threadRoot.Id)
								{
									unsorted = true;
								}
							}
							await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
							{
								threadRoot.AddReply(newComment);
								if (!this.NewRepliesToUser && !threadRoot.Invisible)
								{
									this.NewRepliesToUser = threadRoot.HasNewRepliesToUser;
								}
							});
						}
					}
				}
				this.ChattyLock.Release();
			}

			await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				if (!this.UnsortedChattyPosts)
				{
					this.UnsortedChattyPosts = unsorted;
				}
			});
		}

		private async Task CategoryChange(JToken e)
		{
			var commentId = (int)e["eventData"]["postId"];
			var newCategory = (PostCategory)Enum.Parse(typeof(PostCategory), (string)e["eventData"]["category"]);
			Comment changed = null;
			CommentThread parentChanged = null;
			await this.ChattyLock.WaitAsync();
			foreach (var ct in Chatty)
			{
				changed = ct.Comments.FirstOrDefault(c => c.Id == commentId);
				if (changed != null)
				{
					parentChanged = ct;
					break;
				}
			}

			if (changed != null)
			{
				await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
				{
					if (changed.Id == parentChanged.Id && newCategory == PostCategory.nuked)
					{
						this.chatty.Remove(parentChanged);
					}
					else
					{
						parentChanged.ChangeCommentCategory(changed.Id, newCategory);
						if (this.settings.ShouldAutoCollapseCommentThread(parentChanged))
						{
							if (!parentChanged.IsCollapsed)
							{
								await this.markManager.MarkThread(parentChanged.Id, MarkType.Collapsed, true);
								parentChanged.IsCollapsed = true;
							}
						}
					}
				});
			}
			this.ChattyLock.Release();
		}

		private async Task UpdateLolCount(JToken e)
		{
			await this.ChattyLock.WaitAsync();
			Comment c = null;
			foreach (var update in e["eventData"]["updates"])
			{
				var updatedId = (int)update["postId"];
				foreach (var ct in Chatty)
				{
					c = ct.Comments.FirstOrDefault(c1 => c1.Id == updatedId);
					if (c != null)
					{
						break;
					}
				}
				if (c != null)
				{
					var count = (int)update["count"];
					await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
					{
						switch (update["tag"].ToString())
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
					});
				}
			}
			this.ChattyLock.Release();
		}

		#endregion

		#region Read/Unread Stuff
		public async Task MarkCommentRead(CommentThread ct, Comment c)
		{
			//This is not particularly good programming practices, but, eh, whatever.
			if (ct == null) return;
			if (c == null) return;

			try
			{
				await this.ChattyLock.WaitAsync();
				this.seenPostsManager.MarkCommentSeen(c.Id);
				c.IsNew = false;
				ct.HasNewReplies = ct.Comments.Any(c1 => c1.IsNew);
				ct.HasNewRepliesToUser = ct.Comments.Any(c1 => c1.IsNew && ct.Comments.Any(c2 => c2.Id == c1.ParentId && c2.AuthorType == AuthorType.Self));
				if (!ct.HasNewReplies && this.currentFilter == ChattyFilterType.New && this.filteredChatty.Contains(ct))
				{
					this.UnsortedChattyPosts = true;
				}
				this.NewRepliesToUser = this.filteredChatty.Any(ct1 => ct1.HasNewRepliesToUser);
			}
			finally
			{
				this.ChattyLock.Release();
			}
		}

		public async Task MarkCommentThreadRead(CommentThread ct)
		{
			if (ct == null) return;

			try
			{
				await this.ChattyLock.WaitAsync();
				foreach (var c in ct.Comments)
				{
					this.seenPostsManager.MarkCommentSeen(c.Id);
					c.IsNew = false;
				}
				ct.HasNewReplies = ct.HasNewRepliesToUser = false;
				if (this.currentFilter == ChattyFilterType.New && this.filteredChatty.Contains(ct))
				{
					this.UnsortedChattyPosts = true;
				}
				ct.NewlyAdded = false;
				ct.ViewedNewlyAdded = true;
				this.NewRepliesToUser = this.filteredChatty.Any(ct1 => ct1.HasNewRepliesToUser);
			}
			finally
			{
				this.ChattyLock.Release();
			}
		}

		private void MarkAllVisibleCommentThreadsNotNew()
		{
			foreach (var thread in this.filteredChatty)
			{
				if (thread.NewlyAdded)
				{
					thread.NewlyAdded = false;
					this.NewThreadCount--;
				}
			}
		}

		private void MarkAllVisibleCommentThreadsSeen()
		{
			foreach (var thread in this.filteredChatty)
			{
				thread.ViewedNewlyAdded = true;
			}
		}

		public async Task MarkAllVisibleCommentsRead()
		{
			try
			{
				await this.ChattyLock.WaitAsync();
				foreach (var thread in this.filteredChatty)
				{
					foreach (var cs in thread.Comments)
					{
						this.seenPostsManager.MarkCommentSeen(cs.Id);
						cs.IsNew = false;
					}

					thread.HasNewReplies = thread.HasNewRepliesToUser = false;
					if (this.currentFilter == ChattyFilterType.New && this.filteredChatty.Contains(thread))
					{
						this.UnsortedChattyPosts = true;
					}
				}
				this.NewRepliesToUser = false;
			}
			finally
			{
				this.ChattyLock.Release();
			}
		}

		//This happens when we save - when we save, we also merge from the cloud, so we have to mark any posts we've seen elsewhere here.
		private async void SeenPostsManager_Updated(object sender, EventArgs e)
		{
			try
			{
				await this.ChattyLock.WaitAsync();
				await this.UpdateSeenPosts(this.chatty);
			}
			finally
			{
				this.ChattyLock.Release();
			}
		}

		private async Task UpdateSeenPosts(IEnumerable<CommentThread> commentThreads)
		{
			foreach (var thread in commentThreads)
			{
				var updated = false;
				foreach (var c in thread.Comments)
				{
					if (c.IsNew)
					{
						if (!this.seenPostsManager.IsCommentNew(c.Id))
						{
							updated = true;
							await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
							{
								c.IsNew = false;
							});
						}
					}
				}
				if (updated)
				{
					await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
					{
						thread.HasNewReplies = thread.Comments.Any(c1 => c1.IsNew);
						thread.HasNewRepliesToUser = thread.Comments.Any(c1 => c1.IsNew && thread.Comments.Any(c2 => c2.Id == c1.ParentId && c2.AuthorType == AuthorType.Self));
						this.NewRepliesToUser = this.filteredChatty.Any(ct1 => ct1.HasNewRepliesToUser);
					});
				}
			}
		}

		#endregion

		private async void MarkManager_PostThreadMarkChanged(object sender, ThreadMarkEventArgs e)
		{
			try
			{
				await this.ChattyLock.WaitAsync();
				var thread = this.chatty.SingleOrDefault(ct => ct.Id == e.ThreadID);
				if (thread != null)
				{
					switch (e.Type)
					{
						case MarkType.Unmarked:
							if (thread.IsPinned || thread.IsCollapsed)
							{
								await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
								{
									if (thread.IsPinned && thread.IsExpired)
									{
										this.chatty.Remove(thread);
										if (this.filteredChatty.Contains(thread))
										{
											this.filteredChatty.Remove(thread);
										}
									}
									else
									{
										thread.IsPinned = thread.IsCollapsed = false;
									}
								});
							}
							break;
						case MarkType.Pinned:
							if (!thread.IsPinned)
							{
								await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
								{
									thread.IsPinned = true;
								});
							}
							break;
						case MarkType.Collapsed:
							if (!thread.IsCollapsed)
							{
								await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
								{
									thread.IsCollapsed = true;
									if (this.filteredChatty.Contains(thread))
									{
										this.filteredChatty.Remove(thread);
									}
								});
							}
							break;
						default:
							break;
					}
				}
				else
				{
					switch (e.Type)
					{
						case MarkType.Pinned:
							//If it's pinned but not in the chatty, we need to add it manually.
							var commentThread = await JSONDownloader.Download(Networking.Locations.GetThread + "?id=" + e.ThreadID);
							var parsedThread = (await CommentDownloader.TryParseThread(commentThread["threads"][0], 0, this.seenPostsManager, this.authManager, this.settings, this.markManager, this.flairManager, ignoreManager));
							if (parsedThread != null && parsedThread.IsExpired)
							{
								parsedThread.RecalculateDepthIndicators();
								this.chatty.Add(parsedThread);
							}
							break;
						default:
							break;
					}
				}
			}
			finally
			{
				this.ChattyLock.Release();
			}
		}

		private async void AuthManager_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName.Equals(nameof(AuthenticationManager.LoggedIn)))
			{
				try
				{
					await this.ChattyLock.WaitAsync();
					if (!this.authManager.LoggedIn)
					{
						foreach (var thread in this.chatty)
						{
							if (thread.Comments[0].AuthorType == AuthorType.Self)
							{
								thread.Comments[0].AuthorType = AuthorType.Default;
							}
							thread.HasNewRepliesToUser = false;
							thread.HasRepliesToUser = false;
							thread.UserParticipated = false;
							var rootCommentAuthor = thread.Comments[0].Author;
							foreach (var comment in thread.Comments)
							{
								if (comment.AuthorType == AuthorType.Self)
								{
									if (comment.Author.Equals(rootCommentAuthor, StringComparison.CurrentCultureIgnoreCase))
									{
										comment.AuthorType = AuthorType.ThreadOP;
									}
									else
									{
										comment.AuthorType = AuthorType.Default;
									}
								}
							}
						}
					}
					else
					{
						foreach (var thread in this.chatty)
						{
							foreach (var comment in thread.Comments)
							{
								if (comment.Author.Equals(this.authManager.UserName, StringComparison.CurrentCultureIgnoreCase))
								{
									comment.AuthorType = AuthorType.Self;
								}
							}
							thread.HasRepliesToUser = thread.Comments.Any(c1 => thread.Comments.Any(c2 => c2.Id == c1.ParentId && c2.AuthorType == AuthorType.Self));
							thread.HasNewRepliesToUser = thread.Comments.Any(c1 => c1.IsNew && thread.Comments.Any(c2 => c2.Id == c1.ParentId && c2.AuthorType == AuthorType.Self));
							thread.UserParticipated = thread.Comments.Any(c1 => c1.AuthorType == AuthorType.Self);
						}
					}
					this.CleanupChattyListInternal();
				}
				finally
				{
					this.ChattyLock.Release();
				}
			}
		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					this.ChattyLock.Dispose();
					if (this.chattyRefreshTimer != null)
					{
						this.chattyRefreshTimer.Dispose();
						this.chattyRefreshTimer = null;
					}
				}

				this.chatty.Clear();
				this.chatty = null;
				disposedValue = true;
			}
		}

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
		}
		#endregion
	}
}
