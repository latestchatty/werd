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
	public class ChattyManager : BindableBase
	{

		private int lastEventId = 0;
		//private DateTime lastPinAutoRefresh = DateTime.MinValue;

		private Timer chattyRefreshTimer = null;
		private bool chattyRefreshEnabled = false;
		private DateTime lastChattyRefresh = DateTime.MinValue;
		private SeenPostsManager seenPostsManager = new SeenPostsManager();

		private MoveableObservableCollection<CommentThread> chatty;
		/// <summary>
		/// Gets the active chatty
		/// </summary>
		public ReadOnlyObservableCollection<CommentThread> Chatty
		{
			get;
			private set;
		}

		private ReaderWriterLockSlim ChattyLock = new ReaderWriterLockSlim();

		private DateTime lastLolUpdate = DateTime.MinValue;
		private JToken previousLolData;

		public ChattyManager()
		{
			this.chatty = new MoveableObservableCollection<CommentThread>();
			this.Chatty = new ReadOnlyObservableCollection<CommentThread>(this.chatty);
		}


		private bool npcUnsortedChattyPosts = false;
		public bool UnsortedChattyPosts
		{
			get { return this.npcUnsortedChattyPosts; }
			set { this.SetProperty(ref this.npcUnsortedChattyPosts, value); }
		}


		async private Task UpdateLolCounts()
		{
			//Counts are only updated every 5 minutes, so we'll refresh more than that, but still pretty slow.
			if (DateTime.Now.Subtract(lastLolUpdate).TotalMinutes > 1)
			{
				lastLolUpdate = DateTime.Now;
				JToken lolData = await JSONDownloader.Download(Locations.LolCounts);
				//:HACK: This is a horribly inefficient check, but... yeah.
				if (previousLolData == null || !previousLolData.ToString().Equals(lolData.ToString()))
				{
					//:TODO: Can we limit to a few threads?
					//System.Threading.Tasks.Parallel.ForEach(lolData.Children(), async root =>
					foreach (var root in lolData.Children())
					{
						foreach (var parentPost in root.Children())
						{
							int parentThreadId;
							if (!int.TryParse(parentPost.Path, out parentThreadId))
							{
								continue;
							}

							ChattyLock.EnterReadLock();
							var commentThread = Chatty.SingleOrDefault(ct => ct.Id == parentThreadId);
							if (ChattyLock.IsReadLockHeld) ChattyLock.ExitReadLock();
							if (commentThread == null)
							{
								//System.Diagnostics.Debug.WriteLine("Can't find thread with id {0} for lols.", parentThreadId);
								continue;
							}

							foreach (var post in parentPost.Children())
							{
								var postInfo = post.First;
								int commentId;
								if (!int.TryParse(postInfo.Path.Split('.')[1], out commentId))
								{
									continue;
								}

								var comment = commentThread.Comments.SingleOrDefault(c => c.Id == commentId);
								if (comment != null)
								{
									await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
									{
										if (postInfo["lol"] != null)
										{
											comment.LolCount = int.Parse(postInfo["lol"].ToString());
										}
										if (postInfo["inf"] != null)
										{
											comment.InfCount = int.Parse(postInfo["inf"].ToString());
										}
										if (postInfo["unf"] != null)
										{
											comment.UnfCount = int.Parse(postInfo["unf"].ToString());
										}
										if (postInfo["tag"] != null)
										{
											comment.TagCount = int.Parse(postInfo["tag"].ToString());
										}
										if (postInfo["wtf"] != null)
										{
											comment.WtfCount = int.Parse(postInfo["wtf"].ToString());
										}
										if (postInfo["ugh"] != null)
										{
											comment.UghCount = int.Parse(postInfo["ugh"].ToString());
										}

										if (parentThreadId == commentId)
										{
											commentThread.LolCount = comment.LolCount;
											commentThread.InfCount = comment.InfCount;
											commentThread.UnfCount = comment.UnfCount;
											commentThread.TagCount = comment.TagCount;
											commentThread.WtfCount = comment.WtfCount;
											commentThread.UghCount = comment.UghCount;
										}
									});
								}
								else
								{
									//System.Diagnostics.Debug.WriteLine("Can't find post with id {0} for lols.", parentThreadId);
								}
							}
						}
					}//);
					previousLolData = lolData;
				}
				/*
				foreach(var ct in Chatty)
				{
					foreach(var c in ct.Comments)
					{
						if(DateTime.Now.Subtract(c.LolUpdateTime).TotalMinutes > 1)
						{
							//If we're here, that means this thread hasn't been updated by the large query above because it was either out of the time scope or it didn't have lols.
							//Since we don't really know which it is, we're going to query the server directly for the count.
							//This appears to be impossible with the current LOL API, so... yeah.  Guess we won't do this.
						}
					}
				}
				 */
			}
		}

		private String npcUpdateStatus;
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
			await this.StopAutoChattyRefresh();
			await this.seenPostsManager.Initialize();
			//await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			//{
			//	this.UpdateStatus = "Updating ...";
			//});
			await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				ChattyLock.EnterWriteLock();
				this.chatty.Clear();
				if (ChattyLock.IsWriteLockHeld) ChattyLock.ExitWriteLock();
			});
			var latestEventJson = await JSONDownloader.Download(Latest_Chatty_8.Shared.Networking.Locations.GetNewestEventId);
			this.lastEventId = (int)latestEventJson["eventId"];
			var chattyJson = await JSONDownloader.Download(Latest_Chatty_8.Shared.Networking.Locations.Chatty);
			var parsedChatty = CommentDownloader.ParseThreads(chattyJson, this.seenPostsManager);
			await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				ChattyLock.EnterWriteLock();
				foreach (var comment in parsedChatty)
				{
					this.chatty.Add(comment);
				}
				if (ChattyLock.IsWriteLockHeld) ChattyLock.ExitWriteLock();
			});
			//await GetPinnedPosts();
			await UpdateLolCounts();
			await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				this.CleanupChattyList();
				this.UpdateStatus = "Updated: " + DateTime.Now.ToString();
			});
			//this.StartAutoChattyRefresh();
			await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
			{
				this.IsFullUpdateHappening = false;
			});
		}

		public void StartAutoChattyRefresh()
		{
			this.chattyRefreshEnabled = true;
			if (this.chattyRefreshTimer == null)
			{
				this.chattyRefreshTimer = new Timer(async (a) => await RefreshChattyInternal(), null, 0, Timeout.Infinite);
			}
		}

		async private Task RefreshChattyInternal()
		{
			try
			{
				try
				{
					if(this.lastChattyRefresh == DateTime.MinValue)
					{
						await RefreshChatty();
					}
					//await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
					//{
					//	this.UpdateStatus = "Updating ...";
					//});
					JToken events = await JSONDownloader.Download((LatestChattySettings.Instance.RefreshRate == 0 ? Latest_Chatty_8.Shared.Networking.Locations.WaitForEvent : Latest_Chatty_8.Shared.Networking.Locations.PollForEvent) + "?lastEventId=" + this.lastEventId);
					if (events != null)
					{
						//System.Diagnostics.Debug.WriteLine("Event Data: {0}", events.ToString());
						foreach (var e in events["events"])
						{
							switch ((string)e["eventType"])
							{
								case "newPost":
									var newPostJson = e["eventData"]["post"];
									var threadRootId = (int)newPostJson["threadId"];
									var parentId = (int)newPostJson["parentId"];
									if (parentId == 0)
									{
										//Brand new post.
										//Parse it and add it to the top.
										var newComment = CommentDownloader.ParseCommentFromJson(newPostJson, null, this.seenPostsManager);
										var newThread = new CommentThread(newComment);

										ChattyLock.EnterWriteLock();
										var insertLocation = this.chatty.IndexOf(this.chatty.First(ct => !ct.IsPinned));

										await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
										{
											this.chatty.Insert(insertLocation, newThread);  //Add it at the top, after all pinned posts.
										});
										if (ChattyLock.IsWriteLockHeld) ChattyLock.ExitWriteLock();
									}
									else
									{
										ChattyLock.EnterUpgradeableReadLock();
										var threadRoot = this.chatty.SingleOrDefault(c => c.Id == threadRootId);
										if (threadRoot != null)
										{
											var parent = threadRoot.Comments.SingleOrDefault(c => c.Id == parentId);
											if (parent != null)
											{
												var newComment = CommentDownloader.ParseCommentFromJson(newPostJson, parent, this.seenPostsManager);
												ChattyLock.EnterWriteLock();
												await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
												{
													threadRoot.AddReply(newComment);
												});
												if (ChattyLock.IsWriteLockHeld) ChattyLock.ExitWriteLock();
											}
										}
										if (ChattyLock.IsUpgradeableReadLockHeld) ChattyLock.ExitUpgradeableReadLock();
									}
									await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
									{
										this.UnsortedChattyPosts = true;
									});
									break;
								case "categoryChange":
									var commentId = (int)e["eventData"]["postId"];
									var newCategory = (PostCategory)Enum.Parse(typeof(PostCategory), (string)e["eventData"]["category"]);
									Comment changed = null;
									CommentThread parentChanged = null;
									ChattyLock.EnterReadLock();
									foreach (var ct in Chatty)
									{
										changed = ct.Comments.FirstOrDefault(c => c.Id == commentId);
										if (changed != null)
										{
											parentChanged = ct;
											break;
										}
									}
									if (ChattyLock.IsReadLockHeld) ChattyLock.ExitReadLock();

									if (changed != null)
									{
										ChattyLock.EnterWriteLock();
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
										if (ChattyLock.IsWriteLockHeld) ChattyLock.ExitWriteLock();
									}
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
				catch (Exception e)
				{
					System.Diagnostics.Debug.WriteLine("Exception in auto refresh {0}", e);
					throw;
					//:TODO: Handle specific exceptions.  If we can't refresh, that's not really good.
				}

				if (!this.chattyRefreshEnabled) return;

				//We refresh pinned posts specifically after we get the latest updates to avoid adding stuff out of turn.
				//Come to think of it though, this won't really prevent that.  Oh well.  Some other time.
				try
				{
					////if (DateTime.Now.Subtract(lastPinAutoRefresh).TotalSeconds > 30)
					////{
					////	lastPinAutoRefresh = DateTime.Now;
					////	await this.GetPinnedPosts();
					////}
					await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
					{
						this.UpdateStatus = "Updated: " + DateTime.Now.ToString();
					});
				}
				catch
				{ throw; }

				if (!this.chattyRefreshEnabled) return;

				await UpdateLolCounts();

				//Once we're done processing all the events, then sort the list.
				//if (LatestChattySettings.Instance.SortNewToTop)
				//{
				//	await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
				//	{
				//		this.CleanupChattyList();
				//	});
				//}
			}
			finally
			{
				if (this.chattyRefreshEnabled)
				{
					this.chattyRefreshTimer = new Timer(async (a) => await RefreshChattyInternal(), null, LatestChattySettings.Instance.RefreshRate * 1000, Timeout.Infinite);
				}
			}
			System.Diagnostics.Debug.WriteLine("Done refreshing.");
		}

		public void CleanupChattyList()
		{
			int position = 0;
			ChattyLock.EnterUpgradeableReadLock();
			List<CommentThread> allThreads = this.Chatty.Where(t => !t.IsExpired || t.IsPinned).ToList();
			var removedThreads = this.chatty.Where(t => t.IsExpired && !t.IsPinned).ToList();
			ChattyLock.EnterWriteLock();
			foreach (var item in removedThreads)
			{
				this.chatty.Remove(item);
			}
			foreach (var item in allThreads.Where(t => t.IsPinned).OrderByDescending(t => t.Comments.Max(c => c.Id)))
			{
				this.chatty.Move(this.chatty.IndexOf(item), position);
				position++;
			}
			foreach (var item in allThreads.Where(t => !t.IsPinned).OrderByDescending(t => t.Comments.Max(c => c.Id)))
			{
				this.chatty.Move(this.chatty.IndexOf(item), position);
				position++;
			}
			if (ChattyLock.IsWriteLockHeld) ChattyLock.ExitWriteLock();
			if (ChattyLock.IsUpgradeableReadLockHeld) ChattyLock.ExitUpgradeableReadLock();
			this.UnsortedChattyPosts = false;
		}

		public async Task StopAutoChattyRefresh()
		{
			await ComplexSetting.SetSetting<DateTime>("lastrefresh", this.lastChattyRefresh);
			this.chattyRefreshEnabled = false;
			if (this.chattyRefreshTimer != null)
			{
				this.chattyRefreshTimer.Dispose();
				this.chattyRefreshTimer = null;
			}
		}

		public void MarkCommentRead(CommentThread ct, Comment c)
		{
			try
			{
				this.ChattyLock.EnterWriteLock();
				if (this.seenPostsManager.IsCommentNew(c.Id))
				{
					this.seenPostsManager.MarkCommentSeen(c.Id);
					c.IsNew = false;
				}
				ct.HasNewReplies = ct.Comments.Any(c1 => c1.IsNew);
			}
			finally
			{
				if (this.ChattyLock.IsWriteLockHeld) this.ChattyLock.ExitWriteLock();
			}
		}

		public void MarkCommentThreadRead(CommentThread ct)
		{
			try
			{
				this.ChattyLock.EnterWriteLock();
				foreach(var c in ct.Comments)
				{
					if (this.seenPostsManager.IsCommentNew(c.Id))
					{
						this.seenPostsManager.MarkCommentSeen(c.Id);
						c.IsNew = false;
					}
				}
				ct.HasNewReplies = false;
			}
			finally
			{
				if (this.ChattyLock.IsWriteLockHeld) this.ChattyLock.ExitWriteLock();
			}
		}

		public void MarkAllCommentsRead()
		{
			try
			{
				this.ChattyLock.EnterWriteLock();
				foreach(var thread in this.chatty)
				{
					foreach (var cs in thread.Comments)
					{
						if (!this.seenPostsManager.IsCommentNew(cs.Id))
						{
							this.seenPostsManager.MarkCommentSeen(cs.Id);
							cs.IsNew = false;
						}
					}

					thread.HasNewReplies = false;
				}
			}
			finally
			{
				if (this.ChattyLock.IsWriteLockHeld) this.ChattyLock.ExitWriteLock();
			}
		}
	}
}
