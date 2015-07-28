using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Shared;
using Latest_Chatty_8.Shared.DataModel;
using Latest_Chatty_8.Shared.Networking;
using Latest_Chatty_8.Shared.Settings;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Core;

namespace Latest_Chatty_8.Common
{
	public class ChattyManager : BindableBase, IDisposable
	{
		//TODO: IDisposable and free SeenPostsManger

		private int lastEventId = 0;
		//private DateTime lastPinAutoRefresh = DateTime.MinValue;

		private Timer chattyRefreshTimer = null;
		private bool chattyRefreshEnabled = false;
		private DateTime lastChattyRefresh = DateTime.MinValue;
		private SeenPostsManager seenPostsManager;
		private AuthenticaitonManager services;
		private LatestChattySettings settings;

		private ChattyFilterType currentFilter = ChattyFilterType.All;

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

		public ChattyManager(SeenPostsManager seenPostsManager, AuthenticaitonManager services, LatestChattySettings settings)
		{
			this.chatty = new MoveableObservableCollection<CommentThread>();
			this.filteredChatty = new MoveableObservableCollection<CommentThread>();
			this.Chatty = new ReadOnlyObservableCollection<CommentThread>(this.filteredChatty);
			this.seenPostsManager = seenPostsManager;
			this.services = services;
			this.settings = settings;
			this.seenPostsManager.Updated += SeenPostsManager_Updated;
		}

		private bool npcUnsortedChattyPosts = false;
		public bool UnsortedChattyPosts
		{
			get { return this.npcUnsortedChattyPosts; }
			set { this.SetProperty(ref this.npcUnsortedChattyPosts, value); }
		}

		private String npcUpdateStatus = string.Empty;
		public String UpdateStatus
		{
			get { return npcUpdateStatus; }
			set { this.SetProperty(ref npcUpdateStatus, value); }
		}

		private bool npcIsFullUpdateHappening = false;
		public bool IsFullUpdateHappening
		{
			get { return npcIsFullUpdateHappening; }
			set { this.SetProperty(ref this.npcIsFullUpdateHappening, value); }
		}

		/// <summary>
		/// Forces a full refresh of the chatty.
		/// </summary>
		/// <returns></returns>
		private async Task RefreshChatty()
		{
			await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
			{
				this.IsFullUpdateHappening = true;
			});
			await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
			{
				await this.ChattyLock.WaitAsync();
				this.chatty.Clear();
				this.ChattyLock.Release();
			});
			var latestEventJson = await JSONDownloader.Download(Latest_Chatty_8.Shared.Networking.Locations.GetNewestEventId);
			this.lastEventId = (int)latestEventJson["eventId"];
			var chattyJson = await JSONDownloader.Download(Latest_Chatty_8.Shared.Networking.Locations.Chatty);
			var parsedChatty = CommentDownloader.ParseThreads(chattyJson, this.seenPostsManager, this.services, this.settings);
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
				this.UpdateStatus = "Updated: " + DateTime.Now.ToString();
			});
			await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
			{
				this.IsFullUpdateHappening = false;
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

		public async Task StopAutoChattyRefresh()
		{
			System.Diagnostics.Debug.WriteLine("Stopping chatty refresh.");
			//await ComplexSetting.SetSetting<DateTime>("lastrefresh", this.lastChattyRefresh);
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
			var allThreads = this.filteredChatty.Where(t => !t.IsExpired || t.IsPinned).ToList();
			var removedThreads = this.chatty.Where(t => t.IsExpired && !t.IsPinned).ToList();
			foreach (var item in removedThreads)
			{
				this.chatty.Remove(item);
				if (this.filteredChatty.Contains(item))
				{
					this.filteredChatty.Remove(item);
				}
			}
			foreach (var item in allThreads.Where(t => t.IsPinned).OrderByDescending(t => t.Comments.Max(c => c.Id)))
			{
				this.filteredChatty.Move(this.filteredChatty.IndexOf(item), position);
				position++;
			}
			foreach (var item in allThreads.Where(t => !t.IsPinned).OrderByDescending(t => t.Comments.Max(c => c.Id)))
			{
				this.filteredChatty.Move(this.filteredChatty.IndexOf(item), position);
				position++;
			}
			this.UnsortedChattyPosts = false;
		}

		async public Task FilterChatty(ChattyFilterType filter)
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

		private void FilterChattyInternal(ChattyFilterType filter)
		{
			this.filteredChatty.Clear();
			IEnumerable<CommentThread> toAdd;
			switch (filter)
			{
				case ChattyFilterType.Participated:
					toAdd = this.chatty.Where(c => c.UserParticipated);
					break;
				case ChattyFilterType.HasReplies:
					toAdd = this.chatty.Where(c => c.HasRepliesToUser);
					break;
				case ChattyFilterType.New:
					toAdd = this.chatty.Where(c => c.HasNewReplies);
					break;
				default:
					//By default show everything.
					toAdd = this.chatty;
					break;
			}
			this.currentFilter = filter;
			foreach (var item in toAdd)
			{
				this.filteredChatty.Add(item);
			}
		}

		async private Task RefreshChattyInternal()
		{
			try
			{
				try
				{
					//If we haven't loaded anything yet, load the whole shebang.
					if(this.lastChattyRefresh == DateTime.MinValue)
					{
						await RefreshChatty();
					}
					JToken events = await JSONDownloader.Download((this.settings.RefreshRate == 0 ? Latest_Chatty_8.Shared.Networking.Locations.WaitForEvent : Latest_Chatty_8.Shared.Networking.Locations.PollForEvent) + "?lastEventId=" + this.lastEventId);
					if (events != null)
					{
						//System.Diagnostics.Debug.WriteLine("Event Data: {0}", events.ToString());
						foreach (var e in events["events"])
						{
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
									System.Diagnostics.Debug.WriteLine("Unhandled event: {0}", e.ToString());
									break;
							}
						}
						this.lastEventId = events["lastEventId"].Value<int>(); //Set the last event id after we've completed everything successfully.
						this.lastChattyRefresh = DateTime.Now;
					}
				}
				catch { /*System.Diagnostics.Debugger.Break();*/ /*Generally anything that goes wrong here is going to be due to network connectivity.  So really, we just want to try again later. */ }

				if (!this.chattyRefreshEnabled) return;

				try
				{
					await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
					{
						this.UpdateStatus = "Updated: " + DateTime.Now.ToString();
					});
				}
				catch
				{ throw; }

				if (!this.chattyRefreshEnabled) return;

			}
			finally
			{
				if (this.chattyRefreshEnabled)
				{
					this.chattyRefreshTimer = new Timer(async (a) => await RefreshChattyInternal(), null, this.settings.RefreshRate * 1000, Timeout.Infinite);
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
				var newComment = CommentDownloader.ParseCommentFromJson(newPostJson, null, this.seenPostsManager, services);
				var newThread = new CommentThread(newComment, this.settings);

				await this.ChattyLock.WaitAsync();

				this.chatty.Add(newThread);

				//If we're viewing all posts, all new posts, or our posts and we made the new post, add it to the viewed posts.
				if (this.currentFilter == ChattyFilterType.All
					|| this.currentFilter == ChattyFilterType.New
					|| (this.currentFilter == ChattyFilterType.Participated && newComment.AuthorType == AuthorType.Self))
				{
					//var insertLocation = this.filteredChatty.IndexOf(this.filteredChatty.First(ct => !ct.IsPinned));

					await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
					{
						//this.filteredChatty.Insert(Math.Max(insertLocation, 0), newThread);  //Add it at the top, after all pinned posts.
						unsorted = true;
					});
				}
				this.ChattyLock.Release();
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
						unsorted = true;
						var newComment = CommentDownloader.ParseCommentFromJson(newPostJson, parent, this.seenPostsManager, services);
						//var addToFilterList = false;

						if ((this.currentFilter == ChattyFilterType.HasReplies && parent.AuthorType == AuthorType.Self)
							|| (this.currentFilter == ChattyFilterType.Participated && newComment.AuthorType == AuthorType.Self)
							|| this.currentFilter == ChattyFilterType.New)
						{
							if (!this.filteredChatty.Contains(threadRoot))
							{
								//addToFilterList = true;
								//unsorted = false; //If it needs to be added to the list, there's no sorting to be done
								unsorted = true;
							}
						}
						await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
						{
							threadRoot.AddReply(newComment);
							//if(addToFilterList)
							//{
							//	this.filteredChatty.Insert(0, threadRoot);
							//}	
						});
					}
				}
				this.ChattyLock.Release();
			}
			
			await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				this.UnsortedChattyPosts = unsorted;
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
				await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
				{
					if (changed.Id == parentChanged.Id && newCategory == PostCategory.nuked)
					{
						this.chatty.Remove(parentChanged);
					}
					else
					{
						parentChanged.ChangeCommentCategory(changed.Id, newCategory);
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
		async public Task MarkCommentRead(CommentThread ct, Comment c)
		{
			//This is not particularly good programming practices, but, eh, whatever.
			if (ct == null) return;
			if (c == null) return;

			try
			{
				await this.ChattyLock.WaitAsync();
				if (this.seenPostsManager.IsCommentNew(c.Id))
				{
					this.seenPostsManager.MarkCommentSeen(c.Id);
					c.IsNew = false;
				}
				ct.HasNewReplies = ct.Comments.Any(c1 => c1.IsNew);
				ct.HasNewRepliesToUser = ct.Comments.Any(c1 => c1.IsNew && ct.Comments.Any(c2 => c2.Id == c1.ParentId && c2.AuthorType == AuthorType.Self));
				if (!ct.HasNewReplies && this.currentFilter == ChattyFilterType.New && this.filteredChatty.Contains(ct))
				{
					this.UnsortedChattyPosts = true;
				}
			}
			finally
			{
				this.ChattyLock.Release();
			}
		}

		async public Task MarkCommentThreadRead(CommentThread ct)
		{
			if (ct == null) return;

			try
			{
				await this.ChattyLock.WaitAsync();
				foreach(var c in ct.Comments)
				{
					if (this.seenPostsManager.IsCommentNew(c.Id))
					{
						this.seenPostsManager.MarkCommentSeen(c.Id);
						c.IsNew = false;
					}
				}
				ct.HasNewReplies = ct.HasNewRepliesToUser = false;
				if (this.currentFilter == ChattyFilterType.New && this.filteredChatty.Contains(ct))
				{
					this.UnsortedChattyPosts = true;
				}
			}
			finally
			{
				this.ChattyLock.Release();
			}
		}

		async public Task MarkAllVisibleCommentsRead()
		{
			try
			{
				await this.ChattyLock.WaitAsync();
                foreach (var thread in this.filteredChatty)
				{
					foreach (var cs in thread.Comments)
					{
						if (this.seenPostsManager.IsCommentNew(cs.Id))
						{
							this.seenPostsManager.MarkCommentSeen(cs.Id);
							cs.IsNew = false;
						}
					}

					thread.HasNewReplies = thread.HasNewRepliesToUser = false;
					if(this.currentFilter == ChattyFilterType.New && this.filteredChatty.Contains(thread))
					{
						this.UnsortedChattyPosts = true;
					}
				}
			}
			finally
			{
				this.ChattyLock.Release();
			}
		}

		//This happens when we save - when we save, we also merge from the cloud, so we have to mark any posts we've seen elsewhere here.
		async private void SeenPostsManager_Updated(object sender, EventArgs e)
		{
			try
			{
				await this.ChattyLock.WaitAsync();
				foreach(var thread in this.chatty)
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
						});
					}
				}
			}
			finally
			{
				this.ChattyLock.Release();
			}
		}

		#endregion

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

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~ChattyManager() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion
	}
}
