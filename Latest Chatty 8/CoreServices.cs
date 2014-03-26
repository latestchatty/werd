using Latest_Chatty_8.Settings;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using Latest_Chatty_8.Networking;
using System.Threading.Tasks;
using Windows.UI.Notifications;
using System;
using System.IO;
using Latest_Chatty_8.Common;
using Newtonsoft.Json.Linq;
using Latest_Chatty_8.DataModel;
using System.Threading;
using System.Collections.ObjectModel;
using Windows.UI.Core;

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
		private CancellationTokenSource cancelChattyRefreshSource;

		async public Task Initialize()
		{
			if (!this.initialized)
			{
				this.initialized = true;
				this.chatty = new MoveableObservableCollection<CommentThread>();
				this.Chatty = new ReadOnlyObservableCollection<CommentThread>(this.chatty);
				this.SeenPosts = (await ComplexSetting.ReadSetting<List<int>>("seenposts")) ?? new List<int>();
				await this.AuthenticateUser();
				await LatestChattySettings.Instance.LoadLongRunningSettings();
				await this.RefreshChatty();
			}
		}

		/// <summary>
		/// Suspends this instance.
		/// </summary>
		async public Task Suspend()
		{
			if (this.SeenPosts != null)
			{
				if (this.SeenPosts.Count > 50000)
				{
					this.SeenPosts = this.SeenPosts.Skip(this.SeenPosts.Count - 50000) as List<int>;
				}
				ComplexSetting.SetSetting<List<int>>("seenposts", this.SeenPosts);
			}
			await LatestChattySettings.Instance.SaveToCloud();
			this.StopAutoChattyRefresh();
			//this.PostCounts = null;
			//GC.Collect();
		}

		async public Task Resume()
		{
			await this.ClearTile(false);
			await this.RefreshChatty();
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

		async public Task<IEnumerable<int>> GetPinnedPostIds()
		{
			var pinnedPostIds = new List<int>();
			var data = POSTHelper.BuildDataString(new Dictionary<string, string> { { "clientSessionToken", LatestChattySettings.Instance.ClientSessionToken } });
			var response = await POSTHelper.Send(Locations.GetMarkedPosts, data, false);
			using (var reader = new StreamReader(response.GetResponseStream()))
			{
				var responseData = await reader.ReadToEndAsync();
				var parsedResponse = JToken.Parse(responseData);
				foreach (var post in parsedResponse["markedPosts"].Children())
				{
					pinnedPostIds.Add((int)post["id"]);
				}
			}
			return pinnedPostIds;
		}

		async private Task MarkThread(int id, string type)
		{
			var data = POSTHelper.BuildDataString(new Dictionary<string, string> {
				{ "clientSessionToken", LatestChattySettings.Instance.ClientSessionToken },
				{ "postId", id.ToString() },
				{ "type", type}
			});
			var t = await POSTHelper.Send(Locations.MarkPost, data, false);
		}
		async public Task PinThread(int id)
		{
			await this.MarkThread(id, "pinned");
			await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				var thread = this.chatty.SingleOrDefault(t => t.Id == id);
				if (thread != null)
				{
					thread.IsPinned = true;
				}
				this.CleanupChattyList();
			});
		}

		async public Task UnPinThread(int id)
		{
			await this.MarkThread(id, "unmarked");
			await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				var thread = this.chatty.SingleOrDefault(t => t.Id == id);
				if (thread != null)
				{
					thread.IsPinned = false;
					this.CleanupChattyList();
				}
			});
		}

		async public Task GetPinnedPosts()
		{
			//:TODO: Handle updating this stuff more gracefully.
			var pinnedIds = await GetPinnedPostIds();
			//:TODO: Only need to grab stuff that isn't in the active chatty already.
			//:BUG: If this occurs before the live update happens, we'll fail to add at that point.
			var threads = await CommentDownloader.DownloadThreads(pinnedIds);

			await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				//If it's not marked as pinned from the server, but it is locally, unmark it.
				//It probably got unmarked somewhere else.
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
					if(existingThread == null)
					{
						//Didn't exist in the list, add it.
						this.chatty.Add(thread);
					}
					else
					{
						//Make sure if it's in the active chatty that it's marked as pinned.
						existingThread.IsPinned = true;
						if (existingThread.Comments.Count != thread.Comments.Count)
						{
							foreach (var c in thread.Comments)
							{
								if(!existingThread.Comments.Any(c1 => c1.Id == c.Id))
								{
									thread.AddReply(c); //Add new replies cleanly so we don't lose focus and such.
								}
							}
						}
					}
				}
				this.CleanupChattyList();
			});
		}

		/// <summary>
		/// List of posts we've seen before.
		/// </summary>
		public List<int> SeenPosts { get; set; }

		/// <summary>
		/// Gets set to true when a reply was posted so we can refresh the thread upon return.
		/// </summary>
		public bool PostedAComment { get; set; }

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

		private DateTime npcLastUpdate;
		public DateTime LastUpdate
		{
			get { return npcLastUpdate; }
			set { this.SetProperty(ref npcLastUpdate, value); }
		}

		/// <summary>
		/// Forces a full refresh of the chatty.
		/// </summary>
		/// <returns></returns>
		public async Task RefreshChatty()
		{
			this.StopAutoChattyRefresh();
			var latestEventJson = await JSONDownloader.Download(Networking.Locations.GetNewestEventId);
			this.lastEventId = (int)latestEventJson["eventId"];
			var chattyJson = await JSONDownloader.Download(Networking.Locations.Chatty);
			var parsedChatty = CommentDownloader.ParseThreads(chattyJson);
			this.chatty.Clear();
			foreach (var comment in parsedChatty)
			{
				this.chatty.Add(comment);
			}
			await GetPinnedPosts();
			lastPinAutoRefresh = DateTime.Now;
			await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				this.LastUpdate = DateTime.Now;
			});
			this.StartAutoChattyRefresh();
		}

		private int lastEventId = 0;
		private DateTime lastPinAutoRefresh = DateTime.MinValue;
		public void StartAutoChattyRefresh()
		{
			if (this.cancelChattyRefreshSource == null)
			{
				this.cancelChattyRefreshSource = new CancellationTokenSource();

				var ct = this.cancelChattyRefreshSource.Token;
				Task.Factory.StartNew(async () =>
				{
					while (!ct.IsCancellationRequested)
					{
						try
						{
							var events = await JSONDownloader.Download(Networking.Locations.WaitForEvent + "?lastEventId=" + this.lastEventId);
							this.lastEventId = (int)events["lastEventId"];
							System.Diagnostics.Debug.WriteLine("Event Data: {0}", events.ToString());
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
											var newComment = CommentDownloader.ParseCommentFromJson(newPostJson, null);
											//:TODO: Shouldn't have to do this.
											newComment.IsNew = true;
											var newThread = new CommentThread(newComment);

											await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
											{
												this.chatty.Add(newThread); //Add it at the bottom and resort it
												this.CleanupChattyList();
											});
										}
										else
										{
											var threadRoot = this.chatty.SingleOrDefault(c => c.Id == threadRootId);
											if (threadRoot != null)
											{
												var parent = threadRoot.Comments.SingleOrDefault(c => c.Id == parentId);
												if (parent != null)
												{
													var newComment = CommentDownloader.ParseCommentFromJson(newPostJson, parent);
													await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
													{
														threadRoot.AddReply(newComment);
													});
												}
											}
											if (LatestChattySettings.Instance.SortNewToTop)
											{
												await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
												{
													this.CleanupChattyList();
												});
											}
										}
										break;
									case "nuked":
										break;

								}
							}
						}
						catch (Exception e)
						{
							System.Diagnostics.Debug.WriteLine("Exception {0}", e);
							//:TODO: Do I just want to swallow all exceptions?  Probably.  Everything should continue to function alright, we just won't "push" update.
						}
						//We refresh pinned posts specifically after we get the latest updates to avoid adding stuff out of turn.
						//Come to think of it though, this won't really prevent that.  Oh well.  Some other time.
						try
						{
							if (DateTime.Now.Subtract(lastPinAutoRefresh).TotalSeconds > 30)
							{
								lastPinAutoRefresh = DateTime.Now;
								await this.GetPinnedPosts();
							}
						}
						catch { }
						await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
						{
							this.LastUpdate = DateTime.Now;
						});
					}
					System.Diagnostics.Debug.WriteLine("Bailing on auto refresh thread.");
				}, ct);
			}
		}

		private object orderLocker = new object();
		private void CleanupChattyList()
		{
			lock (orderLocker)
			{
				int position = 0;
				List<CommentThread> allThreads = this.Chatty.Where(t => !t.IsExpired || t.IsPinned).ToList();
				var removedThreads = this.chatty.Where(t => t.IsExpired && !t.IsPinned).ToList();
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
			}
		}

		public void StopAutoChattyRefresh()
		{
			if (this.cancelChattyRefreshSource != null)
			{
				this.cancelChattyRefreshSource.Cancel();
				this.cancelChattyRefreshSource.Dispose();
				this.cancelChattyRefreshSource = null;
			}
		}

		public void MarkAllCommentsRead()
		{
			foreach (var thread in this.chatty)
			{
				foreach (var cs in thread.Comments)
				{
					if (!this.SeenPosts.Contains(cs.Id))
					{
						this.SeenPosts.Add(cs.Id);
						cs.IsNew = false;
					}
				}
				thread.HasNewReplies = false;
			}
		}
		/// <summary>
		/// Authenticates the user set in the application settings.
		/// </summary>
		/// <param name="token">A token that can be used to identify a result.</param>
		/// <returns></returns>
		public async Task<Tuple<bool, string>> AuthenticateUser(string token = "")
		{
			var result = false;
			this.credentials = null;
			var request = (HttpWebRequest)HttpWebRequest.Create("https://www.shacknews.com/account/signin");
			request.Method = "POST";
			request.Headers["x-requested-with"] = "XMLHttpRequest";
			request.Headers[HttpRequestHeader.Pragma] = "no-cache";

			request.ContentType = "application/x-www-form-urlencoded";

			var requestStream = await request.GetRequestStreamAsync();
			var streamWriter = new StreamWriter(requestStream);
			streamWriter.Write(String.Format("user-identifier={0}&supplied-pass={1}&get_fields[]=result", Uri.EscapeUriString(CoreServices.Instance.Credentials.UserName), Uri.EscapeUriString(CoreServices.Instance.Credentials.Password)));
			streamWriter.Flush();
			streamWriter.Dispose();
			var response = await request.GetResponseAsync() as HttpWebResponse;
			//Doesn't seem like the API is actually returning failure codes, but... might as well handle it in case it does some time.
			if (response.StatusCode == HttpStatusCode.OK)
			{
				using (var responseStream = new StreamReader(response.GetResponseStream()))
				{
					var data = await responseStream.ReadToEndAsync();
					System.Diagnostics.Debug.WriteLine("Response {0}", data);
					try
					{
						var jsonResult = JObject.Parse(data)["result"];
						result = jsonResult["valid"].ToString().Equals("true");
					}
					catch
					{
						result = false;
					}

				}
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
				if (cancelChattyRefreshSource != null)
				{
					cancelChattyRefreshSource.Dispose();
				}
			}

			// Free any unmanaged objects here. 
			//
			disposed = true;
		}
	}
}

