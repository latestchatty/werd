using Common;
using Microsoft.Toolkit.Collections;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Werd.Common;
using Werd.DataModel;
using Werd.Networking;
using Werd.Settings;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace Werd.Managers
{
	public class ChattyManager : BindableBase, IDisposable
	{
		private int _lastEventId;

		private Timer _chattyRefreshTimer;
		private bool _chattyRefreshEnabled;
		private DateTime _lastChattyRefresh = DateTime.MinValue;
		private readonly SeenPostsManager _seenPostsManager;
		private readonly AuthenticationManager _authManager;
		private readonly LatestChattySettings _settings;
		private readonly ThreadMarkManager _markManager;
		private readonly UserFlairManager _flairManager;
		private readonly NetworkConnectionStatus _connectionStatus;

		private ChattyFilterType _currentFilter = ChattyFilterType.All;
		private ChattySortType _currentSort = ChattySortType.Default;
		private string _searchText = string.Empty;

		private MoveableObservableCollection<CommentThread> _chatty;

		private readonly MoveableObservableCollection<CommentThread> _filteredChatty;
		/// <summary>
		/// Gets the active chatty
		/// </summary>
		public ReadOnlyObservableCollection<CommentThread> Chatty
		{
			get;
			private set;
		}

		private readonly ObservableGroupedCollection<CommentThread, Comment> _groupedChatty;
		public ReadOnlyObservableGroupedCollection<CommentThread, Comment> GroupedChatty { get; }

		private readonly SemaphoreSlim _chattyLock = new SemaphoreSlim(1);

		private readonly IgnoreManager _ignoreManager;

		public ChattyManager(SeenPostsManager seenPostsManager, AuthenticationManager authManager, LatestChattySettings settings, ThreadMarkManager markManager, UserFlairManager flairManager, IgnoreManager ignoreManager, NetworkConnectionStatus connectionStatus)
		{
			_chatty = new MoveableObservableCollection<CommentThread>();
			_filteredChatty = new MoveableObservableCollection<CommentThread>();
			Chatty = new ReadOnlyObservableCollection<CommentThread>(_filteredChatty);
			_groupedChatty = new ObservableGroupedCollection<CommentThread, Comment>();
			GroupedChatty = new ReadOnlyObservableGroupedCollection<CommentThread, Comment>(_groupedChatty);
			_ignoreManager = ignoreManager;
			_seenPostsManager = seenPostsManager;
			_authManager = authManager;
			_settings = settings;
			_flairManager = flairManager;
			_connectionStatus = connectionStatus;
			_seenPostsManager.Updated += SeenPostsManager_Updated;
			_markManager = markManager;
			_markManager.PostThreadMarkChanged += MarkManager_PostThreadMarkChanged;
			_authManager.PropertyChanged += AuthManager_PropertyChanged;
		}

		private bool npcUnsortedChattyPosts;
		public bool UnsortedChattyPosts
		{
			get => npcUnsortedChattyPosts;
			set => SetProperty(ref npcUnsortedChattyPosts, value);
		}

		private bool npcIsFullUpdateHappening;
		public bool IsFullUpdateHappening
		{
			get => npcIsFullUpdateHappening;
			set => SetProperty(ref npcIsFullUpdateHappening, value);
		}

		private bool npcChattyIsLoaded;
		public bool ChattyIsLoaded
		{
			get => npcChattyIsLoaded;
			set => SetProperty(ref npcChattyIsLoaded, value);
		}

		private int npcNewThreadCount;
		public int NewThreadCount
		{
			get => npcNewThreadCount;
			set => SetProperty(ref npcNewThreadCount, value);
		}

		private bool npcNewRepliesToUser;
		public bool NewRepliesToUser
		{
			get => npcNewRepliesToUser;
			set => SetProperty(ref npcNewRepliesToUser, value);
		}

		public bool ShouldFullRefresh()
		{
			if (!_connectionStatus.IsWinChattyConnected) return false;
			// if we've never refreshed or it's been more than 15 minutes since the last update, do a full refresh
			return _lastChattyRefresh == DateTime.MinValue || DateTime.Now.Subtract(_lastChattyRefresh).TotalSeconds > 900;
		}

		/// <summary>
		/// Forces a full refresh of the chatty.
		/// </summary>
		/// <returns></returns>
		private async Task RefreshChattyFull()
		{
			await CoreApplication.MainView.CoreWindow.Dispatcher.RunOnUiThreadAndWait(CoreDispatcherPriority.Normal, async () =>
			{
				ChattyIsLoaded = false;
				IsFullUpdateHappening = true;
				NewThreadCount = 0;
				NewRepliesToUser = false;
				await _chattyLock.WaitAsync().ConfigureAwait(true);
				_chatty.Clear();
				_chattyLock.Release();
			}).ConfigureAwait(false);
			var latestEventJson = await JsonDownloader.Download(Locations.GetNewestEventId).ConfigureAwait(false);
			_lastEventId = (int)latestEventJson["eventId"];
			//var downloadTimer = new TelemetryTimer("ChattyDownload");
			//downloadTimer.Start();
			var chattyJson = await JsonDownloader.Download(Locations.Chatty).ConfigureAwait(false);
			//downloadTimer.Stop();
			var parsedChatty = await CommentDownloader.ParseThreads(chattyJson, _seenPostsManager, _authManager, _settings, _markManager, _flairManager, _ignoreManager).ConfigureAwait(false);

			await CoreApplication.MainView.CoreWindow.Dispatcher.RunOnUiThreadAndWait(CoreDispatcherPriority.Normal, async () =>
			{
				await _chattyLock.WaitAsync().ConfigureAwait(true);
				foreach (var comment in parsedChatty)
				{
					AddToChatty(comment);
				}
				_chattyLock.Release();
				FilterChattyInternal(_currentFilter);
				await CleanupChattyList().ConfigureAwait(true);
				IsFullUpdateHappening = false;
			}).ConfigureAwait(false);
		}

		public void StartAutoChattyRefresh()
		{
			if (_chattyRefreshEnabled) return;
			ChattyIsLoaded = false;
			_chattyRefreshEnabled = true;
			if (_chattyRefreshTimer == null)
			{
				_chattyRefreshTimer = new Timer(async a => await RefreshChattyInternal().ConfigureAwait(false), null, 0, Timeout.Infinite);
			}
		}

		public void StopAutoChattyRefresh()
		{
			AppGlobal.DebugLog.AddMessage("Stopping chatty refresh.").GetAwaiter().GetResult();
			_chattyRefreshEnabled = false;
			if (_chattyRefreshTimer != null)
			{
				_chattyRefreshTimer.Dispose();
				_chattyRefreshTimer = null;
			}
		}

		public void ScheduleImmediateChattyRefresh()
		{
			if (_chattyRefreshTimer != null && _chattyRefreshEnabled)
			{
				_chattyRefreshTimer.Change(0, Timeout.Infinite);
			}
		}

		public async Task CleanupChattyList()
		{
			try
			{
				await _chattyLock.WaitAsync().ConfigureAwait(true);
				FilterChattyInternal(_currentFilter);
				CleanupChattyListInternal();
			}
			catch (Exception e)
			{
				await AppGlobal.DebugLog.AddException("Exception in CleanupChattyList", e).ConfigureAwait(false);
			}
			finally
			{
				_chattyLock.Release();
			}
		}

		private void CleanupChattyListInternal()
		{
			int position = 0;

			//var timer = new TelemetryTimer("ApplyChattySort", new Dictionary<string, string> { { "sortType", Enum.GetName(typeof(ChattySortType), this.currentSort) } });
			//timer.Start();
			List<CommentThread> removedThreads = new List<CommentThread>();
			foreach (var thread in _chatty)
			{
				//Set expired but pinned threads invisible so they get hidden from the live chatty.
				if (thread.IsExpired && thread.IsPinned && !thread.Invisible) thread.Invisible = true;
				//If it's expired but not pinned, it needs to be removed from the chatty.
				if (thread.IsExpired && !thread.IsPinned) removedThreads.Add(thread);
				if (thread.Comments.Count > _settings.TruncateLimit) thread.TruncateThread = true; //re-truncate threads
			}
			foreach (var item in removedThreads)
			{
				_chatty.Remove(item);
				if (_filteredChatty.Contains(item))
				{
					_filteredChatty.Remove(item);
					_groupedChatty.RemoveGroup(item);
				}
			}

			var allThreads = _filteredChatty.Where(t => !t.Invisible).ToList();

			IOrderedEnumerable<CommentThread> orderedThreads;

			switch (_currentSort)
			{
				case ChattySortType.Inf:
					orderedThreads = allThreads
						.OrderByDescending(ct => _settings.ShowPinnedThreadsAtChattyTop && ct.IsPinned)
						.ThenByDescending(ct => ct.NewlyAdded)
						.ThenByDescending(ct => ct.Comments.Sum(c => c.InfCount))
						.ThenByDescending(t => t.Comments.Max(c => c.Id));
					break;
				case ChattySortType.Lol:
					orderedThreads = allThreads
						.OrderByDescending(ct => _settings.ShowPinnedThreadsAtChattyTop && ct.IsPinned)
						.ThenByDescending(ct => ct.NewlyAdded)
						.ThenByDescending(ct => ct.Comments.Sum(c => c.LolCount))
						.ThenByDescending(t => t.Comments.Max(c => c.Id));
					break;
				case ChattySortType.ReplyCount:
					orderedThreads = allThreads
						.OrderByDescending(ct => _settings.ShowPinnedThreadsAtChattyTop && ct.IsPinned)
						.ThenByDescending(ct => ct.NewlyAdded)
						.ThenByDescending(ct => ct.Comments.Count)
						.ThenByDescending(t => t.Comments.Max(c => c.Id));
					break;
				case ChattySortType.HasNewReplies:
					orderedThreads = allThreads
						.OrderByDescending(ct => _settings.ShowPinnedThreadsAtChattyTop && ct.IsPinned)
						.ThenByDescending(ct => ct.NewlyAdded)
						.ThenByDescending(ct => ct.HasNewRepliesToUser)
						.ThenByDescending(t => t.Comments.Max(c => c.Id));
					break;
				case ChattySortType.Participated:
					orderedThreads = allThreads
						.OrderByDescending(ct => _settings.ShowPinnedThreadsAtChattyTop && ct.IsPinned)
						.ThenByDescending(ct => ct.NewlyAdded)
						.ThenByDescending(ct => ct.UserParticipated)
						.ThenByDescending(t => t.Comments.Max(c => c.Id));
					break;
				default:
					orderedThreads = allThreads
						.OrderByDescending(ct => _settings.ShowPinnedThreadsAtChattyTop && ct.IsPinned)
						.ThenByDescending(ct => ct.NewlyAdded)
						.ThenByDescending(t => t.Comments.Max(c => c.Id));
					break;
			}

			foreach (var item in orderedThreads)
			{
				foreach (var comment in item.Comments)
				{
					comment.IsSelected = false;
				}
				_filteredChatty.Move(_filteredChatty.IndexOf(item), position);
				var itemToMove = _groupedChatty.First(g => g.Key.Id == item.Id);
				_groupedChatty.Move(_groupedChatty.IndexOf(itemToMove), position);
				position++;
			}
			UnsortedChattyPosts = false;
			NewRepliesToUser = false;
			MarkAllVisibleCommentThreadsNotNew();

			//timer.Stop();
		}

		public async Task SortChatty(ChattySortType sort)
		{
			try
			{
				await _chattyLock.WaitAsync().ConfigureAwait(true);
				_currentSort = sort;
				CleanupChattyListInternal();
			}
			finally
			{
				_chattyLock.Release();
			}
		}

		public async Task FilterChatty(ChattyFilterType filter)
		{
			try
			{
				await _chattyLock.WaitAsync().ConfigureAwait(true);
				FilterChattyInternal(filter);
				CleanupChattyListInternal();
			}
			finally
			{
				_chattyLock.Release();
			}
		}

		public async Task SearchChatty(string search)
		{
			try
			{
				await _chattyLock.WaitAsync().ConfigureAwait(true);
				_searchText = search;
				FilterChattyInternal(ChattyFilterType.Search);
				CleanupChattyListInternal();
			}
			finally
			{
				_chattyLock.Release();
			}
		}

		public async Task<CommentThread> FindOrAddThreadByAnyPostId(int anyId)
		{
			CommentThread rootThread;
			try
			{
				//This is probably going to get me in trouble at some point in the future.
				while (!ChattyIsLoaded)
				{
					await Task.Delay(10).ConfigureAwait(true);
				}
				await _chattyLock.WaitAsync().ConfigureAwait(true);
				rootThread = _chatty.FirstOrDefault(ct => ct.Comments.Any(c => c.Id == anyId));

				if (rootThread == null)
				{
					//Time to download it and add it.
					var thread = await CommentDownloader.TryDownloadThreadById(anyId, _seenPostsManager, _authManager, _settings, _markManager, _flairManager, _ignoreManager).ConfigureAwait(true);
					if (thread != null)
					{
						//If it's expired, we need to prevent it from being removed from the chatty later.  This will keep it live and we'll process events in the thread, but we'll never show it in the chatty view.
						if (thread.IsExpired)
						{
							thread.Invisible = true;
						}
						AddToChatty(thread);
						rootThread = thread;
					}
				}
			}
			//catch (Exception e)
			//{
			//System.Diagnostics.Global.DebugLog.AddMessage($"Exception in {nameof(FindOrAddThreadByAnyPostId)} : {e}");
			//(new TelemetryClient()).TrackException(e);
			//}
			finally
			{
				_chattyLock.Release();
			}
			return rootThread;
		}

		private void FilterChattyInternal(ChattyFilterType filter)
		{
			MarkAllVisibleCommentThreadsSeen();
			_filteredChatty.Clear();
			_groupedChatty.Clear();
			IEnumerable<CommentThread> toAdd = null;
			switch (filter)
			{
				case ChattyFilterType.Participated:
					toAdd = _chatty.Where(ct => ct.UserParticipated && !ct.IsCollapsed);
					break;
				case ChattyFilterType.HasReplies:
					toAdd = _chatty.Where(ct => ct.HasRepliesToUser && !ct.IsCollapsed);
					break;
				case ChattyFilterType.New:
					toAdd = _chatty.Where(ct => ct.HasNewReplies && !ct.IsCollapsed);
					break;
				case ChattyFilterType.News:
					toAdd = _chatty.Where(ct => ct.Comments.FirstOrDefault()?.AuthorType == AuthorType.Shacknews);
					break;
				case ChattyFilterType.Search:
					if (!string.IsNullOrWhiteSpace(_searchText))
					{
						toAdd = _chatty.Where(ct => !ct.IsCollapsed && ct.Comments.Any(c => c.Author.Equals(_searchText, StringComparison.OrdinalIgnoreCase) || c.Body.ToLower().Contains(_searchText.ToLower())));
					}
					break;
				case ChattyFilterType.Collapsed:
					toAdd = _chatty.Where(ct => ct.IsCollapsed);
					break;
				case ChattyFilterType.Pinned:
					toAdd = _chatty.Where(ct => ct.IsPinned && !ct.IsCollapsed);
					break;
				default:
					//By default show everything that isn't collapsed.
					toAdd = _chatty.Where(ct => !ct.IsCollapsed);
					break;
			}

			if (toAdd != null)
			{
				toAdd = toAdd.Where(ct => !ct.Invisible);
			}

			_currentFilter = filter;

			if (toAdd != null)
			{
				foreach (var item in toAdd.ToList())
				{
					_filteredChatty.Add(item);
					_groupedChatty.Add(item.CommentsGroup);
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
				await _chattyLock.WaitAsync().ConfigureAwait(true);
				//HACK: There should never be more than one thread for a given parent post in the chatty at the same time, however this appears to happen sometimes (though I think I've fixed it)
				//  Rather than crash with SingleOrDefault, we'll just iterate over any that exist. Yuck.
				var opCts = _chatty.Where(ct1 => ct1.Comments[0].Id == ct.Comments[0].Id);
				foreach (var opCt in opCts)
				{
					for (int i = 0; i < opCt.Comments.Count; ++i)
					{
						opCt.Comments[i].IsSelected = false;
					}
				}
			}
			finally
			{
				_chattyLock.Release();
			}
		}

		private async Task RefreshChattyInternal()
		{
			try
			{
				try
				{
					//If we haven't loaded anything yet, load the whole shebang.
					if (ShouldFullRefresh())
					{
						await RefreshChattyFull().ConfigureAwait(false);
					}
					JToken events = await JsonDownloader.Download(new Uri((_settings.RefreshRate == 0 ? Locations.WaitForEvent : Locations.PollForEvent) + "?lastEventId=" + _lastEventId)).ConfigureAwait(false);
					if (events != null)
					{
#if GENERATE_THREADS
						ChattyHelper.GenerateNewThreadJson(ref events);
#endif
						//System.Diagnostics.Global.DebugLog.AddMessage("Event Data: {0}", events.ToString());
						foreach (var e in events["events"])
						{
							//var timer = new TelemetryTimer("ParseEvent", new Dictionary<string, string> { { "eventType", (string)e["eventType"] } });
							//timer.Start();
							switch ((string)e["eventType"])
							{
								case "newPost":
									await AddNewPost(e).ConfigureAwait(false);
									break;
								case "categoryChange":
									await CategoryChange(e).ConfigureAwait(false);
									break;
								case "lolCountsUpdate":
									await UpdateLolCount(e).ConfigureAwait(false);
									break;
								default:
									await AppGlobal.DebugLog.AddMessage($"Unhandled API event: {e.ToString()}").ConfigureAwait(false);
									break;
							}
							//timer.Stop();
						}
						_lastEventId = events["lastEventId"].Value<int>(); //Set the last event id after we've completed everything successfully.
						_lastChattyRefresh = DateTime.Now;
					}

					await CoreApplication.MainView.CoreWindow.Dispatcher.RunOnUiThreadAndWait(CoreDispatcherPriority.Normal, async () =>
					{
						var locked = await _chattyLock.WaitAsync(10).ConfigureAwait(true);
						try
						{
							if (locked)
							{
								foreach (var thread in _filteredChatty)
								{
									thread.ForceDateRefresh(); //Force an update to dates to keep the expirations current.
								}
							}
						}
						finally
						{
							if (locked)
							{
								_chattyLock.Release();
							}
						}
						ChattyIsLoaded = true; //At this point chatty's fully loaded even if we're fully refreshing or just picking up where we left off.
					}).ConfigureAwait(false);
				}
				catch (Exception e)
				{
					await AppGlobal.DebugLog.AddException("Exception refreshing chatty events", e).ConfigureAwait(false);
				}
			}
			finally
			{
				if (_chattyRefreshEnabled)
				{
					_chattyRefreshTimer.Change(_settings.RefreshRate * 1000, Timeout.Infinite);
				}
			}
		}

		#region WinChatty Event Handlers
		private async Task AddNewPost(JToken e)
		{
			var newPostJson = e["eventData"]["post"];
			var threadRootId = (int)newPostJson["threadId"];
			var parentId = (int)newPostJson["parentId"];
			await AppGlobal.DebugLog.AddMessage($"Adding a new post to {threadRootId} with parent id {parentId}").ConfigureAwait(false);

			var unsorted = false;

			if (parentId == 0)
			{
				//Brand new post.
				//Parse it and add it to the top.
				var newComment = await CommentDownloader.TryParseCommentFromJson(newPostJson, null, _seenPostsManager, _authManager, _flairManager, _ignoreManager).ConfigureAwait(false);
				if (newComment != null)
				{
					var newThread = new CommentThread(newComment, true);
					if (_settings.ShouldAutoCollapseCommentThread(newThread))
					{
						await _markManager.MarkThread(newThread.Id, MarkType.Collapsed, true).ConfigureAwait(false);
						newThread.IsCollapsed = true;
					}

					await _chattyLock.WaitAsync().ConfigureAwait(false);

					AddToChatty(newThread);

					//If we're viewing all posts, all new posts, or our posts and we made the new post, add it to the viewed posts.
					if (_currentFilter == ChattyFilterType.All
						|| _currentFilter == ChattyFilterType.New
						|| (_currentFilter == ChattyFilterType.Participated && newComment.AuthorType == AuthorType.Self)
						|| (_currentFilter == ChattyFilterType.Search) && newComment.Author.Equals(_searchText, StringComparison.OrdinalIgnoreCase) || newComment.Body.ToLower().Contains(_searchText)
						|| (_currentFilter == ChattyFilterType.Pinned && _markManager.GetMarkType(newThread.Id) == MarkType.Pinned)
						|| (_currentFilter == ChattyFilterType.Collapsed && _markManager.GetMarkType(newThread.Id) == MarkType.Collapsed))
					{
						unsorted = true;
					}
					await CoreApplication.MainView.CoreWindow.Dispatcher.RunOnUiThreadAndWait(CoreDispatcherPriority.Normal, () =>
					{
						NewThreadCount++;
					}).ConfigureAwait(false);
					_chattyLock.Release();
				}
			}
			else
			{
				await _chattyLock.WaitAsync().ConfigureAwait(false);
				var threadRoot = _chatty.SingleOrDefault(c => c.Id == threadRootId);
				if (threadRoot != null)
				{
					var parent = threadRoot.Comments.SingleOrDefault(c => c.Id == parentId);
					if (parent != null)
					{
						var newComment = await CommentDownloader.TryParseCommentFromJson(newPostJson, parent, _seenPostsManager, _authManager, _flairManager, _ignoreManager).ConfigureAwait(false);
						if (newComment != null)
						{
							if (!_filteredChatty.Contains(threadRoot))
							{
								if ((_currentFilter == ChattyFilterType.HasReplies && parent.AuthorType == AuthorType.Self)
									|| (_currentFilter == ChattyFilterType.Participated && newComment.AuthorType == AuthorType.Self)
									|| _currentFilter == ChattyFilterType.New
									|| (_currentFilter == ChattyFilterType.Search) && newComment.Author.Equals(_searchText, StringComparison.OrdinalIgnoreCase) || newComment.Body.ToLower().Contains(_searchText)
									|| (_currentFilter == ChattyFilterType.Pinned && _markManager.GetMarkType(threadRoot.Id) == MarkType.Pinned)
									|| (_currentFilter == ChattyFilterType.Collapsed && _markManager.GetMarkType(threadRoot.Id) == MarkType.Collapsed))
								{
									unsorted = true;
								}
							}
							else
							{
								//If the root thread is already in the filtered list, we're unsorted if we don't already belong to the top post.
								if (_filteredChatty.Count > 0 && _filteredChatty[0].Id != threadRoot.Id)
								{
									unsorted = true;
								}
							}
							await CoreApplication.MainView.CoreWindow.Dispatcher.RunOnUiThreadAndWait(CoreDispatcherPriority.Normal, async () =>
							{
								await threadRoot.AddReply(newComment).ConfigureAwait(true);
								if (!NewRepliesToUser && !threadRoot.Invisible)
								{
									NewRepliesToUser = threadRoot.HasNewRepliesToUser;
								}
							}).ConfigureAwait(false);
						}
					}
				}
				_chattyLock.Release();
			}

			await CoreApplication.MainView.CoreWindow.Dispatcher.RunOnUiThreadAndWait(CoreDispatcherPriority.Normal, () =>
			{
				if (!UnsortedChattyPosts)
				{
					UnsortedChattyPosts = unsorted;
				}
			}).ConfigureAwait(false);
		}

		private async Task CategoryChange(JToken e)
		{
			var commentId = (int)e["eventData"]["postId"];
			var newCategory = (PostCategory)Enum.Parse(typeof(PostCategory), (string)e["eventData"]["category"]);
			await AppGlobal.DebugLog.AddMessage($"Changing category for {commentId} to {Enum.GetName(typeof(PostCategory), newCategory)}").ConfigureAwait(false);
			Comment changed = null;
			CommentThread parentChanged = null;
			await _chattyLock.WaitAsync().ConfigureAwait(false);
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
				await CoreApplication.MainView.CoreWindow.Dispatcher.RunOnUiThreadAndWait(CoreDispatcherPriority.Normal, async () =>
				{
					if (changed.Id == parentChanged.Id && newCategory == PostCategory.nuked)
					{
						_chatty.Remove(parentChanged);
					}
					else
					{
						parentChanged.ChangeCommentCategory(changed.Id, newCategory);
						if (_settings.ShouldAutoCollapseCommentThread(parentChanged))
						{
							if (!parentChanged.IsCollapsed)
							{
								await _markManager.MarkThread(parentChanged.Id, MarkType.Collapsed, true).ConfigureAwait(true);
								parentChanged.IsCollapsed = true;
							}
						}
					}
				}).ConfigureAwait(false);
			}
			_chattyLock.Release();
		}

		private async Task UpdateLolCount(JToken e)
		{
			await _chattyLock.WaitAsync().ConfigureAwait(false);
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
					var tag = update["tag"].ToString();
					await AppGlobal.DebugLog.AddMessage($"Updating '{tag}' count for {updatedId} to {count}").ConfigureAwait(false);
					await CoreApplication.MainView.CoreWindow.Dispatcher.RunOnUiThreadAndWait(CoreDispatcherPriority.Normal, () =>
					{

						switch (tag)
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
							case "wow":
								c.WowCount = count;
								break;
							case "aww":
								c.AwwCount = count;
								break;
						}
					}).ConfigureAwait(false);
				}
			}
			_chattyLock.Release();
		}

		#endregion

		public async Task<Comment> SelectNextComment(CommentThread ct, bool forward)
		{
			try
			{
				await _chattyLock.WaitAsync().ConfigureAwait(true);
				//Get the currently selected comment. If any.
				var selectedComment = ct.Comments.FirstOrDefault(c => c.IsSelected);

				//Don't have a selected comment so select the first non-root post and bail early.
				if (selectedComment == null)
				{
					selectedComment = ct.Comments.ElementAtOrDefault(1);
					selectedComment.IsSelected = true;
					return selectedComment;
				}

				var newlySelectedIndex = ct.Comments.IndexOf(selectedComment) + (forward ? 1 : -1);
				// Loop around if the new selection would be root
				if (newlySelectedIndex == 0) newlySelectedIndex = ct.Comments.Count - 1;
				// Loop around the other way if new selection is out of range
				if (newlySelectedIndex > ct.Comments.Count - 1) newlySelectedIndex = 1;

				for (int i = 0; i < ct.Comments.Count; i++)
				{
					var comment = ct.Comments[i];
					if (i == newlySelectedIndex)
					{
						MarkCommentReadInternal(ct, comment);
						comment.IsSelected = true;
						selectedComment = comment;
					}
					else
					{
						comment.IsSelected = false;
					}
				}
				return selectedComment;
			}
			finally
			{
				_chattyLock.Release();
			}
		}

		#region Read/Unread Stuff
		public async Task MarkCommentRead(CommentThread ct, Comment c)
		{
			//This is not particularly good programming practices, but, eh, whatever.
			if (ct == null) return;
			if (c == null) return;

			try
			{
				await _chattyLock.WaitAsync().ConfigureAwait(true);
				MarkCommentReadInternal(ct, c);
			}
			finally
			{
				_chattyLock.Release();
			}
		}

		private void MarkCommentReadInternal(CommentThread ct, Comment c)
		{
			_seenPostsManager.MarkCommentSeen(c.Id);
			c.IsNew = false;
			ct.HasNewReplies = ct.Comments.Any(c1 => c1.IsNew);
			ct.HasNewRepliesToUser = ct.Comments.Any(c1 => c1.IsNew && ct.Comments.Any(c2 => c2.Id == c1.ParentId && c2.AuthorType == AuthorType.Self));
			if (!ct.HasNewReplies && _currentFilter == ChattyFilterType.New && _filteredChatty.Contains(ct))
			{
				UnsortedChattyPosts = true;
			}
			NewRepliesToUser = _filteredChatty.Any(ct1 => ct1.HasNewRepliesToUser);
		}

		public async Task MarkCommentThreadRead(CommentThread ct)
		{
			if (ct == null) return;

			try
			{
				await _chattyLock.WaitAsync().ConfigureAwait(true);
				foreach (var c in ct.Comments)
				{
					_seenPostsManager.MarkCommentSeen(c.Id);
					c.IsNew = false;
				}
				ct.HasNewReplies = ct.HasNewRepliesToUser = false;
				if (_currentFilter == ChattyFilterType.New && _filteredChatty.Contains(ct))
				{
					UnsortedChattyPosts = true;
				}
				ct.NewlyAdded = false;
				ct.ViewedNewlyAdded = true;
				NewRepliesToUser = _filteredChatty.Any(ct1 => ct1.HasNewRepliesToUser);
			}
			finally
			{
				_chattyLock.Release();
			}
		}

		private void MarkAllVisibleCommentThreadsNotNew()
		{
			foreach (var thread in _filteredChatty)
			{
				if (thread.NewlyAdded)
				{
					thread.NewlyAdded = false;
					NewThreadCount--;
				}
			}
		}

		private void MarkAllVisibleCommentThreadsSeen()
		{
			foreach (var thread in _filteredChatty)
			{
				thread.ViewedNewlyAdded = true;
			}
		}

		public async Task MarkAllVisibleCommentsRead()
		{
			try
			{
				await _chattyLock.WaitAsync().ConfigureAwait(true);
				foreach (var thread in _filteredChatty)
				{
					foreach (var cs in thread.Comments)
					{
						_seenPostsManager.MarkCommentSeen(cs.Id);
						cs.IsNew = false;
					}

					thread.HasNewReplies = thread.HasNewRepliesToUser = false;
					if (_currentFilter == ChattyFilterType.New && _filteredChatty.Contains(thread))
					{
						UnsortedChattyPosts = true;
					}
				}
				NewRepliesToUser = false;
			}
			finally
			{
				_chattyLock.Release();
			}
		}

		//This happens when we save - when we save, we also merge from the cloud, so we have to mark any posts we've seen elsewhere here.
		private async void SeenPostsManager_Updated(object sender, EventArgs e)
		{
			try
			{
				await _chattyLock.WaitAsync().ConfigureAwait(true);
				await UpdateSeenPosts(_chatty).ConfigureAwait(false);
			}
			finally
			{
				_chattyLock.Release();
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
						if (!_seenPostsManager.IsCommentNew(c.Id))
						{
							updated = true;
							await CoreApplication.MainView.CoreWindow.Dispatcher.RunOnUiThreadAndWait(CoreDispatcherPriority.Normal, () =>
							{
								c.IsNew = false;
							}).ConfigureAwait(false);
						}
					}
				}
				if (updated)
				{
					await CoreApplication.MainView.CoreWindow.Dispatcher.RunOnUiThreadAndWait(CoreDispatcherPriority.Normal, () =>
					{
						thread.HasNewReplies = thread.Comments.Any(c1 => c1.IsNew);
						thread.HasNewRepliesToUser = thread.Comments.Any(c1 => c1.IsNew && thread.Comments.Any(c2 => c2.Id == c1.ParentId && c2.AuthorType == AuthorType.Self));
						NewRepliesToUser = _filteredChatty.Any(ct1 => ct1.HasNewRepliesToUser);
					}).ConfigureAwait(false);
				}
			}
		}

		#endregion

		private async void MarkManager_PostThreadMarkChanged(object sender, ThreadMarkEventArgs e)
		{
			try
			{
				await _chattyLock.WaitAsync().ConfigureAwait(false);
				var thread = _chatty.SingleOrDefault(ct => ct.Id == e.ThreadId);
				if (thread != null)
				{
					switch (e.Type)
					{
						case MarkType.Unmarked:
							if (thread.IsPinned || thread.IsCollapsed)
							{
								await CoreApplication.MainView.CoreWindow.Dispatcher.RunOnUiThreadAndWait(CoreDispatcherPriority.Normal, () =>
								{
									thread.IsPinned = thread.IsCollapsed = false;
								}).ConfigureAwait(false);
							}
							break;
						case MarkType.Pinned:
							await CoreApplication.MainView.CoreWindow.Dispatcher.RunOnUiThreadAndWait(CoreDispatcherPriority.Normal, () =>
							{
								thread.IsCollapsed = false;
								thread.IsPinned = true;
							}).ConfigureAwait(false);
							break;
						case MarkType.Collapsed:
							await CoreApplication.MainView.CoreWindow.Dispatcher.RunOnUiThreadAndWait(CoreDispatcherPriority.Normal, () =>
							{
								thread.IsPinned = false;
								thread.IsCollapsed = true;
								if (_filteredChatty.Contains(thread))
								{
									_filteredChatty.Remove(thread);
									_groupedChatty.RemoveGroup(thread);
								}
							}).ConfigureAwait(false);
							break;
					}
				}
			}
			finally
			{
				_chattyLock.Release();
			}
		}

		private async void AuthManager_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (!e.PropertyName.Equals(nameof(AuthenticationManager.LoggedIn), StringComparison.Ordinal)) return;
			try
			{
				await _chattyLock.WaitAsync().ConfigureAwait(false);
				_lastChattyRefresh = DateTime.MinValue;
			}
			finally
			{
				_chattyLock.Release();
			}
		}

		private void AddToChatty(CommentThread ct)
		{
			if (!_chatty.Any(existing => ct.Id == existing.Id))
			{
				_chatty.Add(ct);
			}
		}

		#region IDisposable Support
		private bool _disposedValue; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
					_chattyLock.Dispose();
					if (_chattyRefreshTimer != null)
					{
						_chattyRefreshTimer.Dispose();
						_chattyRefreshTimer = null;
					}
				}

				_chatty.Clear();
				_chatty = null;
				_disposedValue = true;
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
