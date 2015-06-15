using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Shared;
using Latest_Chatty_8.Shared.DataModel;
using Latest_Chatty_8.Shared.Networking;
using Latest_Chatty_8.Shared.Settings;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Notifications;

namespace Latest_Chatty_8
{
	/// <summary>
	/// Singleton object to perform some common functionality across the entire application
	/// </summary>
	public class CoreServices : BindableBase, IDisposable
	{
		#region Singleton
		private static CoreServices _coreServices = null;
		public static CoreServices Instance
		{
			get
			{
				if (_coreServices == null)
				{
					_coreServices = new CoreServices();
				}
				return _coreServices;
			}
		}
		#endregion

		private bool initialized = false;

		async public Task Initialize()
		{
			if (!this.initialized)
			{
				this.initialized = true;
				this.chatty = new MoveableObservableCollection<CommentThread>();
				this.Chatty = new ReadOnlyObservableCollection<CommentThread>(this.chatty);
				this.SeenPosts = (await ComplexSetting.ReadSetting<List<int>>("seenposts")) ?? new List<int>();
				await this.AuthenticateUser();
				//await LatestChattySettings.Instance.LoadLongRunningSettings();
				await this.RefreshChatty();
			}
		}

		/// <summary>
		/// Suspends this instance.
		/// </summary>
		async public Task Suspend()
		{
			this.StopAutoChattyRefresh();
			await LatestChattySettings.Instance.SaveToCloud();
			await ComplexSetting.SetSetting<DateTime>("lastrefresh", this.lastChattyRefresh);
			//this.PostCounts = null;
			//GC.Collect();
		}

		async public Task Resume()
		{
			await this.ClearTile(false);
			System.Diagnostics.Debug.WriteLine("Loading seen posts.");
			this.SeenPosts = (await ComplexSetting.ReadSetting<List<int>>("seenposts")) ?? new List<int>();
			this.lastChattyRefresh = await ComplexSetting.ReadSetting<DateTime?>("lastrefresh") ?? DateTime.MinValue;
			await this.AuthenticateUser();
			await LatestChattySettings.Instance.LoadLongRunningSettings();
			if (DateTime.Now.Subtract(this.lastChattyRefresh).TotalMinutes > 20)
			{
				await this.RefreshChatty(); //Completely refresh the chatty.
			}
			else
			{
				this.StartAutoChattyRefresh(); //Try to apply updates
			}
		}

		private bool npcUnsortedChattyPosts = false;
		public bool UnsortedChattyPosts
		{
			get { return this.npcUnsortedChattyPosts; }
			set { this.SetProperty(ref this.npcUnsortedChattyPosts, value); }
		}

		/// <summary>
		/// Gets the credentials for the currently logged in user.
		/// </summary>
		/// <value>
		/// The credentials.
		/// </value>
		private NetworkCredential credentials = null;
		public NetworkCredential Credentials
		{
			get
			{
				if (this.credentials == null)
				{
					this.credentials = new NetworkCredential(LatestChattySettings.Instance.Username, LatestChattySettings.Instance.Password);
				}
				return this.credentials;
			}
		}

		private bool npcShowAuthor = true;
		public bool ShowAuthor
		{
			get { return npcShowAuthor; }
			set { this.SetProperty(ref npcShowAuthor, value); }
		}

		async public Task<IEnumerable<int>> GetPinnedPostIds()
		{
			var pinnedPostIds = new List<int>();
			if (CoreServices.Instance.LoggedIn)
			{
				var parsedResponse = await JSONDownloader.Download(Locations.GetMarkedPosts + "?username=" + WebUtility.UrlEncode(CoreServices.Instance.Credentials.UserName));
				foreach (var post in parsedResponse["markedPosts"].Children())
				{
					pinnedPostIds.Add((int)post["id"]);
				}

			}
			return pinnedPostIds;
		}

		async private Task MarkThread(int id, string type)
		{
			if (!CoreServices.Instance.LoggedIn) return;

			var data = POSTHelper.BuildDataString(new Dictionary<string, string> {
				{ "username", CoreServices.Instance.Credentials.UserName },
				{ "postId", id.ToString() },
				{ "type", type}
			});
			var t = await POSTHelper.Send(Locations.MarkPost, data, false);
		}
		async public Task PinThread(int id)
		{
			await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				this.ChattyLock.EnterReadLock();
				var thread = this.chatty.SingleOrDefault(t => t.Id == id);
				this.ChattyLock.ExitReadLock();
				if (thread != null)
				{
					thread.IsPinned = true;
				}
				this.CleanupChattyList();
			});
			await this.MarkThread(id, "pinned");
		}

		async public Task UnPinThread(int id)
		{
			await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				this.ChattyLock.EnterReadLock();
				var thread = this.chatty.SingleOrDefault(t => t.Id == id);
				this.ChattyLock.ExitReadLock();
				if (thread != null)
				{
					thread.IsPinned = false;
					this.CleanupChattyList();
				}
			});
			await this.MarkThread(id, "unmarked");
		}

		async public Task GetPinnedPosts()
		{
			//:TODO: Handle updating this stuff more gracefully.
			var pinnedIds = await GetPinnedPostIds();
			//:TODO: Only need to grab stuff that isn't in the active chatty already.
			//:BUG: If this occurs before the live update happens, we'll fail to add at that point.
			this.ChattyLock.EnterReadLock();
			var threads = await CommentDownloader.DownloadThreads(pinnedIds.Where(t => !this.Chatty.Any(ct => ct.Id.Equals(t))));
			this.ChattyLock.ExitReadLock();

			//Nothing pinned, bail early.
			if (threads.Count == 0) { return; }
			await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				//If it's not marked as pinned from the server, but it is locally, unmark it.
				//It probably got unmarked somewhere else.
				this.ChattyLock.EnterUpgradeableReadLock();
				foreach (var t in this.chatty.Where(t => t.IsPinned))
				{
					if (!threads.Any(pt => pt.Id == t.Id))
					{
						t.IsPinned = false;
					}
				}

				foreach (var thread in threads.OrderByDescending(t => t.Id))
				{
					thread.IsPinned = true;
					var existingThread = this.chatty.FirstOrDefault(t => t.Id == thread.Id);
					if (existingThread == null)
					{
						this.ChattyLock.EnterWriteLock();
						//Didn't exist in the list, add it.
						this.chatty.Add(thread);
						this.ChattyLock.ExitWriteLock();
					}
					else
					{
						//Make sure if it's in the active chatty that it's marked as pinned.
						existingThread.IsPinned = true;
						if (existingThread.Comments.Count != thread.Comments.Count)
						{
							foreach (var c in thread.Comments)
							{
								if (!existingThread.Comments.Any(c1 => c1.Id == c.Id))
								{
									this.ChattyLock.EnterWriteLock();
									thread.AddReply(c); //Add new replies cleanly so we don't lose focus and such.
									this.ChattyLock.ExitWriteLock();
								}
							}
						}
					}
				}
				this.ChattyLock.ExitUpgradeableReadLock();
			});
		}

		/// <summary>
		/// Clears the tile and optionally registers for notifications if necessary.
		/// </summary>
		/// <param name="registerForNotifications">if set to <c>true</c> [register for notifications].</param>
		/// <returns></returns>
		async public Task ClearTile(bool registerForNotifications)
		{
			TileUpdateManager.CreateTileUpdaterForApplication().Clear();
			BadgeUpdateManager.CreateBadgeUpdaterForApplication().Clear();
			if (registerForNotifications)
			{
				await NotificationHelper.ReRegisterForNotifications();
			}
		}

		private bool npcLoggedIn;
		/// <summary>
		/// Gets a value indicating whether there is a currently logged in (and authenticated) user.
		/// </summary>
		/// <value>
		///   <c>true</c> if [logged in]; otherwise, <c>false</c>.
		/// </value>
		public bool LoggedIn
		{
			get { return npcLoggedIn; }
			private set
			{
				this.SetProperty(ref this.npcLoggedIn, value);
			}
		}

		private MoveableObservableCollection<CommentThread> chatty;
		/// <summary>
		/// Gets the active chatty
		/// </summary>
		public ReadOnlyObservableCollection<CommentThread> Chatty
		{
			get;
			private set;
		}

		public ReaderWriterLockSlim ChattyLock = new ReaderWriterLockSlim();

		private DateTime lastLolUpdate = DateTime.MinValue;
		private JToken previousLolData;

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

							this.ChattyLock.EnterReadLock();
							var commentThread = Chatty.SingleOrDefault(ct => ct.Id == parentThreadId);
							this.ChattyLock.ExitReadLock();
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
		public async Task RefreshChatty()
		{
			await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
			{
				this.IsFullUpdateHappening = true;
			});
			this.StopAutoChattyRefresh();
			//await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			//{
			//	this.UpdateStatus = "Updating ...";
			//});
			await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				this.ChattyLock.EnterWriteLock();
				this.chatty.Clear();
				this.ChattyLock.ExitWriteLock();
			});
			var latestEventJson = await JSONDownloader.Download(Latest_Chatty_8.Shared.Networking.Locations.GetNewestEventId);
			this.lastEventId = (int)latestEventJson["eventId"];
			var chattyJson = await JSONDownloader.Download(Latest_Chatty_8.Shared.Networking.Locations.Chatty);
			var parsedChatty = CommentDownloader.ParseThreads(chattyJson);
			await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				this.ChattyLock.EnterWriteLock();
				foreach (var comment in parsedChatty)
				{
					this.chatty.Add(comment);
				}
				this.ChattyLock.ExitWriteLock();
			});
			await GetPinnedPosts();
			await UpdateLolCounts();
			this.CleanupChattyList();
			lastPinAutoRefresh = DateTime.Now;
			await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				this.UpdateStatus = "Updated: " + DateTime.Now.ToString();
			});
			this.StartAutoChattyRefresh();
			await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
			{
				this.IsFullUpdateHappening = false;
			});
		}

		private int lastEventId = 0;
		private DateTime lastPinAutoRefresh = DateTime.MinValue;

		private Timer chattyRefreshTimer = null;
		private bool chattyRefreshEnabled = false;
		private DateTime lastChattyRefresh = DateTime.MinValue;

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
					//await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
					//{
					//	this.UpdateStatus = "Updating ...";
					//});
					JToken events = await JSONDownloader.Download((LatestChattySettings.Instance.RefreshRate == 0 ? Latest_Chatty_8.Shared.Networking.Locations.WaitForEvent : Latest_Chatty_8.Shared.Networking.Locations.PollForEvent) + "?lastEventId=" + this.lastEventId);
					if (events != null)
					{
						//System.Diagnostics.Debug.WriteLine("Event Data: {0}", events.ToString());
						foreach(var e in events["events"])
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
										var newComment = CommentDownloader.ParseCommentFromJson(newPostJson, null);
										//:TODO: Shouldn't have to do this.
										newComment.IsNew = true;
										var newThread = new CommentThread(newComment);

										this.ChattyLock.EnterReadLock();
										var insertLocation = this.chatty.IndexOf(this.chatty.First(ct => !ct.IsPinned));
										this.ChattyLock.ExitReadLock();

										await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
										{
											this.ChattyLock.EnterWriteLock();
											this.chatty.Insert(insertLocation, newThread);  //Add it at the top, after all pinned posts.
											this.ChattyLock.ExitWriteLock();
										});
									}
									else
									{
										this.ChattyLock.EnterUpgradeableReadLock();
										var threadRoot = this.chatty.SingleOrDefault(c => c.Id == threadRootId);
										if (threadRoot != null)
										{
											var parent = threadRoot.Comments.SingleOrDefault(c => c.Id == parentId);
											if (parent != null)
											{
												var newComment = CommentDownloader.ParseCommentFromJson(newPostJson, parent);
												this.ChattyLock.EnterWriteLock();
												await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
												{
													threadRoot.AddReply(newComment);
												});
												this.ChattyLock.ExitWriteLock();
											}
										}
										this.ChattyLock.ExitUpgradeableReadLock();
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
									this.ChattyLock.EnterReadLock();
									foreach (var ct in Chatty)
									{
										changed = ct.Comments.FirstOrDefault(c => c.Id == commentId);
										if (changed != null)
										{
											parentChanged = ct;
											break;
										}
									}
									this.ChattyLock.ExitReadLock();

									if (changed != null)
									{
										await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
										{
											this.ChattyLock.EnterWriteLock();
											if (changed.Id == parentChanged.Id && newCategory == PostCategory.nuked)
											{
												this.chatty.Remove(parentChanged);
											}
											else
											{
												parentChanged.ChangeCommentCategory(changed.Id, newCategory);
											}
											this.ChattyLock.ExitWriteLock();
										});
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
					if (DateTime.Now.Subtract(lastPinAutoRefresh).TotalSeconds > 30)
					{
						lastPinAutoRefresh = DateTime.Now;
						await this.GetPinnedPosts();
					}
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

		private object orderLocker = new object();
		public void CleanupChattyList()
		{
			lock (orderLocker)
			{
				int position = 0;
				this.ChattyLock.EnterUpgradeableReadLock();
				List<CommentThread> allThreads = this.Chatty.Where(t => !t.IsExpired || t.IsPinned).ToList();
				var removedThreads = this.chatty.Where(t => t.IsExpired && !t.IsPinned).ToList();
				this.ChattyLock.EnterWriteLock();
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
				this.ChattyLock.ExitWriteLock();
				this.ChattyLock.ExitUpgradeableReadLock();
				var th = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
				{
					this.UnsortedChattyPosts = false;
				});
			}
		}

		public void StopAutoChattyRefresh()
		{
			this.chattyRefreshEnabled = false;
			if (this.chattyRefreshTimer != null)
			{
				this.chattyRefreshTimer.Dispose();
				this.chattyRefreshTimer = null;
			}
		}

		/// <summary>
		/// List of posts we've seen before.
		/// </summary>
		private List<int> SeenPosts { get; set; }

		public bool IsCommentNew(int postId)
		{
            var result = !this.SeenPosts.Contains(postId);
            return result;
		}

		private async Task SaveSeenPosts()
		{
			if (this.SeenPosts.Count > 50000)
			{
				this.SeenPosts = this.SeenPosts.Skip(this.SeenPosts.Count - 50000) as List<int>;
			}
			await ComplexSetting.SetSetting<List<int>>("seenposts", this.SeenPosts);
			System.Diagnostics.Debug.WriteLine("Saving seen posts.");
		}

		async public Task MarkCommentRead(CommentThread ct, Comment c)
		{
			if (!this.SeenPosts.Contains(c.Id))
			{
				this.SeenPosts.Add(c.Id);
				c.IsNew = false;

				await SaveSeenPosts();
			}
			this.ChattyLock.EnterReadLock();
			ct.HasNewReplies = ct.Comments.Any(c1 => c1.IsNew);
			this.ChattyLock.ExitReadLock();
		}

		async public Task MarkCommentThreadRead(CommentThread ct)
		{
			this.ChattyLock.EnterReadLock();
			foreach (var cs in ct.Comments)
			{
				if (!this.SeenPosts.Contains(cs.Id))
				{
					this.SeenPosts.Add(cs.Id);
					cs.IsNew = false;
				}
			}
			this.ChattyLock.ExitReadLock();
			ct.HasNewReplies = false;
			await SaveSeenPosts();
		}

		async public Task MarkAllCommentsRead(bool allowparallel = false)
		{
			this.ChattyLock.EnterReadLock();
			foreach (var thread in this.chatty)
			{
				if (allowparallel)
				{
					Parallel.ForEach(thread.Comments, cs =>
					//foreach (var cs in thread.Comments)
					{
						if (!this.SeenPosts.Contains(cs.Id))
						{
							this.SeenPosts.Add(cs.Id);
							cs.IsNew = false;
						}
					});
				}
				else
				{
					foreach (var cs in thread.Comments)
					{
						if (!this.SeenPosts.Contains(cs.Id))
						{
							this.SeenPosts.Add(cs.Id);
							cs.IsNew = false;
						}
					}
				}
				thread.HasNewReplies = false;
			}
			this.ChattyLock.ExitReadLock();
			await SaveSeenPosts();
		}

		/// <summary>
		/// Authenticates the user set in the application settings.
		/// </summary>
		/// <param name="token">A token that can be used to identify a result.</param>
		/// <returns></returns>
		public async Task<Tuple<bool, string>> AuthenticateUser(string token = "")
		{
			var result = false;
			//:HACK: :TODO: This feels dirty as hell. Figure out if we even need the credentials object any more.  Seems like we should just use it from settings.
			this.credentials = null; //Clear the cached credentials so they get recreated.
			if (CoreServices.Instance.Credentials != null && !string.IsNullOrEmpty(CoreServices.Instance.Credentials.UserName))
			{
				try
				{
					var response = await POSTHelper.Send(Locations.VerifyCredentials, new List<KeyValuePair<string, string>>(), true);

					if (response.StatusCode == HttpStatusCode.OK)
					{
						var data = await response.Content.ReadAsStringAsync();
						var json = JToken.Parse(data);
						result = (bool)json["isValid"];
						System.Diagnostics.Debug.WriteLine((result ? "Valid" : "Invalid") + " login");
					}

					if (!result)
					{
						if (LatestChattySettings.Instance.CloudSync)
						{
							LatestChattySettings.Instance.CloudSync = false;
						}
						if (LatestChattySettings.Instance.EnableNotifications)
						{
							await NotificationHelper.UnRegisterNotifications();
						}
						//LatestChattySettings.Instance.ClearPinnedThreads();
					}
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine("Error occurred while logging in: {0}", ex);
				}	//No matter what happens, fail to log in.
			}
			this.LoggedIn = result;
			return new Tuple<bool, string>(result, token);
		}

		bool disposed = false;

		// Public implementation of Dispose pattern callable by consumers.
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Protected implementation of Dispose pattern.
		protected virtual void Dispose(bool disposing)
		{
			if (disposed)
				return;

			if (disposing)
			{
				this.StopAutoChattyRefresh();
			}

			// Free any unmanaged objects here.
			//
			disposed = true;
		}
	}
}

