using Latest_Chatty_8.Settings;
using LatestChatty.Classes;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using Latest_Chatty_8.Networking;
using System.Threading.Tasks;
using Windows.UI.Notifications;

namespace Latest_Chatty_8
{
	public class CoreServices
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

		async public Task Initialize()
		{
			this.PostCounts = (await ComplexSetting.ReadSetting<Dictionary<int, int>>("postcounts")) ?? new Dictionary<int, int>();
		}

		public void Suspend()
		{
			if (this.PostCounts.Count > 10000)
			{
				this.PostCounts = this.PostCounts.Skip(this.PostCounts.Count - 10000) as Dictionary<int, int>;
			}
			ComplexSetting.SetSetting<Dictionary<int, int>>("postcounts", this.PostCounts);
		}

		//private readonly API_Helper apiHelper = new API_Helper();
		//public void QueueDownload(string uri, LatestChatty.Classes.XMLDownloader.XMLDownloaderCallback callback)
		//{
		//	this.apiHelper.AddDownload(uri, callback);
		//}

		//public void CancelDownloads()
		//{
		//	this.apiHelper.CancelDownloads();
		//}

		public NetworkCredential Credentials
		{
			get
			{
				return new NetworkCredential(LatestChattySettings.Instance.Username, LatestChattySettings.Instance.Password);
			}
		}

		public Dictionary<int, int> PostCounts;

		/// <summary>
		/// Gets set to true when a reply was posted so we can refresh the thread upon return.
		/// </summary>
		public bool PostedAComment { get; set; }

		async public Task Resume()
		{
			TileUpdateManager.CreateTileUpdaterForApplication().Clear();
			BadgeUpdateManager.CreateBadgeUpdaterForApplication().Clear();
			await NotificationHelper.ReRegisterForNotifications();
		}

		public bool LoginVerified
		{
			get
			{
				return !string.IsNullOrWhiteSpace(LatestChattySettings.Instance.Username) && !string.IsNullOrWhiteSpace(LatestChattySettings.Instance.Password);
			}
		}
	}
}


//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.IO.IsolatedStorage;
//using System.Linq;
//using System.Net;
//using System.Runtime.Serialization;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Shapes;
//using LatestChatty.Classes;
//using LatestChatty.ViewModels;
//using Microsoft.Phone.Net.NetworkInformation;
//using LatestChatty.Settings;

//namespace LatestChatty
//{
//	public class CoreServices
//	{

//		public CoreServices()
//		{
//		}

//		~CoreServices()
//		{
//		}

//		public void Initialize()
//		{
//			SetCommentBrowserString();
//			LoadReplyCounts();	
//		}

//		#region Singleton
//		private static CoreServices _coreServices = null;
//		public static CoreServices Instance
//		{
//			get
//			{
//				if (_coreServices == null)
//				{
//					_coreServices = new CoreServices();
//				}
//				return _coreServices;
//			}
//		}
//		#endregion

//		#region ServiceHost
//		public const string ServiceHost = "http://shackapi.stonedonkey.com/";
//		public const string PostUrl = ServiceHost + "post/";

//		#endregion

//		#region StoryCommentCache
//		private CommentList _storyComment;
//		public void AddStoryComments(int story, CommentList comments)
//		{
//			_storyComment = comments;
//		}

//		public CommentList GetStoryComments(int story)
//		{
//			if (_storyComment != null && _storyComment._story == story)
//			{
//				return _storyComment;
//			}
//			return null;
//		}

//		public void SaveCurrentStoryComments()
//		{
//			if (_storyComment != null)
//			{
//				DataContractSerializer ser = new DataContractSerializer(typeof(CommentList));

//				using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
//				{
//					using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream("currentstorycomments.txt", FileMode.Create, isf))
//					{
//						ser.WriteObject(stream, _storyComment);
//					}
//				}
//			}
//		}

//		public void LoadCurrentStoryComments()
//		{
//			try
//			{
//				DataContractSerializer ser = new DataContractSerializer(typeof(CommentList));

//				using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
//				{
//					using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream("currentstorycomments.txt", FileMode.Open, isf))
//					{
//						_storyComment = ser.ReadObject(stream) as CommentList;
//					}
//				}
//			}
//			catch
//			{
//			}
//		}
//		#endregion

//		#region ThreadPageHelper
//		public delegate void SelectedCommentChangedEvent(Comment newSelection);
//		//public SelectedCommentChangedEvent SelectedCommentChanged;
//		public Rectangle SelectedCommentHighlight;

//		private int _selectedCommentId;
//		private CommentThread _currentCommentThread;

//		public void SetCurrentCommentThread(CommentThread thread)
//		{
//			_currentCommentThread = thread;
//		}

//		public void SetCurrentSelectedComment(Comment c)
//		{
//			System.Diagnostics.Debug.WriteLine("Setting global selected comment Id {0}", c.id);
//			_selectedCommentId = c.id;

//			//if (this.SelectedCommentChanged != null)
//			//{
//			//          Deployment.Current.Dispatcher.BeginInvoke(() => this.SelectedCommentChanged(c));
//			//}
//		}

//		public CommentThread GetCommentThread(int comment)
//		{
//			if (_currentCommentThread != null && _currentCommentThread.SeedCommentId == comment)
//			{
//				return _currentCommentThread;
//			}
//			return null;
//		}

//		public int GetSelectedComment()
//		{
//			return _selectedCommentId;
//		}

//		public void SaveCurrentCommentThread()
//		{
//			if (_currentCommentThread != null)
//			{
//				DataContractSerializer ser = new DataContractSerializer(typeof(CommentThread));

//				using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
//				{
//					using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream("currentcommentthread.txt", FileMode.Create, isf))
//					{
//						ser.WriteObject(stream, _currentCommentThread);
//					}
//				}

//				using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
//				{
//					using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream("currentselectedcomment.txt", FileMode.Create, isf))
//					{
//						StreamWriter sw = new StreamWriter(stream);
//						sw.WriteLine(_selectedCommentId);
//						sw.Close();
//					}
//				}
//			}
//		}

//		public void LoadCurrentCommentThread()
//		{
//			try
//			{
//				DataContractSerializer ser = new DataContractSerializer(typeof(CommentThread));

//				using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
//				{
//					using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream("currentcommentthread.txt", FileMode.Open, isf))
//					{
//						System.Diagnostics.Debug.WriteLine("Loading comment thread from persistent storage.");
//						_currentCommentThread = ser.ReadObject(stream) as CommentThread;
//					}
//				}

//				using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
//				{
//					using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream("currentselectedcomment.txt", FileMode.Open, isf))
//					{
//						System.Diagnostics.Debug.WriteLine("Loading selected comment id from persistent storage.");
//						StreamReader sr = new StreamReader(stream);
//						_selectedCommentId = int.Parse(sr.ReadLine());
//						sr.Close();
//					}
//				}
//			}
//			catch (Exception ex)
//			{
//				System.Diagnostics.Debug.WriteLine("Exception occurred deserializing current comment thread. {0}", ex);
//			}
//		}

//		#endregion

//		#region WebBrowserControlHelper
//		public string CommentBrowserString { get; private set; }
//		private void SetCommentBrowserString()
//		{
//			var resource = Application.GetResourceStream(new Uri("stylesheet.css", UriKind.Relative));
//			StreamReader streamReader = new StreamReader(resource.Stream);
//			string css = streamReader.ReadToEnd();
//			streamReader.Close();
//			//Without the scrolling in there, very weird rendering happens when you've scrolled down on a large post and then switch to a short post.
//			this.CommentBrowserString = "<html><head><meta name='viewport' content='user-scalable=no'/><script type=\"text/javascript\">function setContent(s) { document.body.scrollTop = 0; document.body.scrollLeft = 0; document.getElementById('commentBody').innerHTML = s; } </script><style type='text/css'>" + css + "</style><body><div id='commentBody' class='body'></div></body></html>";
//		}
//		#endregion

//		#region NavigationHelper
//		public void Navigate(Uri uri)
//		{
//			((Page)((App)App.Current).RootFrame.Content).NavigationService.Navigate(uri);
//		}
//		public Comment ReplyToContext;
//		#endregion

//		#region API Helper
//		private readonly API_Helper apiHelper = new API_Helper();
//		public void QueueDownload(string uri, LatestChatty.Classes.XMLDownloader.XMLDownloaderCallback callback)
//		{
//			this.apiHelper.AddDownload(uri, callback);
//		}

//		public void CancelDownloads()
//		{
//			this.apiHelper.CancelDownloads();
//		}

//		#endregion

//		#region LoginHelper
//		public delegate void LoginCallback(bool verified);

//		NetworkCredential userCredentials = new NetworkCredential(LatestChattySettings.Instance.Username, LatestChattySettings.Instance.Password);


//		public NetworkCredential Credentials
//		{
//			get
//			{
//				return userCredentials;
//			}
//		}

//		private LoginCallback loginCallback;

//		public void TryLogin(string username, string password, LoginCallback callback)
//		{
//			this.userCredentials = new NetworkCredential(username, password);
//			this.loginCallback = callback;
//			var request = (HttpWebRequest)HttpWebRequest.Create("http://www.shacknews.com/account/signin");
//			request.Method = "POST";
//			request.Headers["x-requested-with"] = "XMLHttpRequest";
//			request.Headers[HttpRequestHeader.Pragma] = "no-cache";
//			request.Headers[HttpRequestHeader.Connection] = "keep-alive";

//			request.ContentType = "application/x-www-form-urlencoded";
//			request.BeginGetRequestStream(BeginLoginPostCallback, request);
//		}

//		public void BeginLoginPostCallback(IAsyncResult result)
//		{
//			HttpWebRequest request = result.AsyncState as HttpWebRequest;

//			Stream requestStream = request.EndGetRequestStream(result);
//			StreamWriter streamWriter = new StreamWriter(requestStream);
//			streamWriter.Write(String.Format("email={0}&password={1}&get_fields[]=result", HttpUtility.UrlEncode(this.userCredentials.UserName), HttpUtility.UrlEncode(this.userCredentials.Password)));
//			streamWriter.Flush();
//			streamWriter.Close();

//			request.BeginGetResponse(GetLoginCallback, request);
//		}


//		public void GetLoginCallback(IAsyncResult result)
//		{
//			var success = false;
//			try
//			{
//				HttpWebRequest request = result.AsyncState as HttpWebRequest;
//				HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(result);

//				//Doesn't seem like the API is actually returning failure codes, but... might as well handle it in case it does some time.
//				if (response.StatusCode == HttpStatusCode.OK)
//				{
//					using (var responseStream = response.GetResponseStream())
//					{
//						var responseBytes = new byte[responseStream.Length];
//						responseStream.Read(responseBytes, 0, responseBytes.Length);
//						var responseString = System.Text.Encoding.UTF8.GetString(responseBytes, 0, responseBytes.Length);
//						success = responseString.Equals("{\"result\":\"true\"}");
//					}
//				}
//			}
//			catch (Exception ex) {
//				System.Diagnostics.Debug.WriteLine("problem logging in {0}", ex.ToString());
//			}

//			if (!success)
//			{
//				this.userCredentials = null;
//			}
//			//Here we force save, no matter what the success was and then fire the callback.
//			Deployment.Current.Dispatcher.BeginInvoke(() =>
//			{

//				LatestChattySettings.Instance.Username = this.userCredentials != null ? this.userCredentials.UserName : string.Empty;
//				LatestChattySettings.Instance.Password = this.userCredentials != null ? this.userCredentials.Password : string.Empty;

//				if (this.loginCallback != null)
//				{
//					this.loginCallback(success);
//				}
//			});
//		}
//		public void Logout()
//		{
//			//Unregister notifications first
//			NotificationHelper.UnRegisterNotifications(); 

//			//Clear out all the information for the user
//			LatestChattySettings.Instance.Username = this.userCredentials.UserName = string.Empty;
//			LatestChattySettings.Instance.Password = this.userCredentials.Password = string.Empty;
//			LatestChattySettings.Instance.NotificationType = NotificationType.None;

//			//Clear MyPosts, MyReplies, and refresh the watchlist so the participation flag goes away
//			this.MyPosts.Logout();
//			this.MyReplies.Logout();
//			this.WatchList.Refresh();
//		}
//		#endregion

//		#region Tombstone
//		public void Activated()
//		{
//			LoadCurrentStoryComments();
//			LoadCurrentCommentThread();
//			//Should already be loaded, no?
//			//LoadReplyCounts();
//		}

//		public void Deactivated()
//		{
//			SaveCurrentStoryComments();
//			SaveCurrentCommentThread();
//			SaveReplyCounts();
//		}
//		#endregion

//		#region Headlines
//		private StoryList _headlines;
//		public StoryList GetHeadlines(ref bool create)
//		{
//			if (_headlines == null)
//			{
//				_headlines = new StoryList();
//				create = true;
//			}
//			return _headlines;
//		}
//		#endregion

//		#region StoryDetail
//		private List<StoryDetail> _storyDetails = new List<StoryDetail>();
//		public StoryDetail GetStoryDetail(int id, ref bool create)
//		{
//			foreach (StoryDetail s in _storyDetails)
//			{
//				if (s.Detail.id == id)
//				{
//					create = false;
//					return s;
//				}
//			}

//			StoryDetail sd = new StoryDetail(id);
//			_storyDetails.Add(sd);
//			create = true;
//			return sd;
//		}
//		#endregion

//		#region Messages
//		public MessageList Inbox { get; set; }
//		public MessageList Outbox { get; set; }
//		public MessageList Archive { get; set; }

//		public delegate void SelectedMessageChangedEvent(Message newSelection);
//		public SelectedMessageChangedEvent SelectedMessageChanged;
//		#endregion

//		#region WatchList
//		public WatchList WatchList = new WatchList();

//		public bool IsOnWatchedList(Comment c)
//		{
//			return WatchList.IsOnWatchList(c);
//		}

//		public bool AddOrRemoveWatch(Comment c)
//		{
//			var result = WatchList.AddOrRemove(c);
//			WatchList.SaveWatchList(); //Force saving right away.
//			return result;
//		}
//		#endregion

//		#region CollapseList
//		public CollapseList CollapseList = new CollapseList();
//		#endregion

//		#region Reply Count Cache
//		private Dictionary<int, int> knownReplyCounts = new System.Collections.Generic.Dictionary<int, int>();
//		private void LoadReplyCounts()
//		{
//			System.Diagnostics.Debug.WriteLine("Loading reply counts.");
//			try
//			{
//				DataContractSerializer ser = new DataContractSerializer(knownReplyCounts.GetType());

//				using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
//				{
//					using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream("postcounts.txt", FileMode.Open, isf))
//					{
//						this.knownReplyCounts = ser.ReadObject(stream) as Dictionary<int, int>;
//					}
//				}
//			}
//			catch
//			{
//				//Something failed, make a new dictionary and start over.
//				this.knownReplyCounts = new System.Collections.Generic.Dictionary<int, int>();
//			}

//			System.Diagnostics.Debug.WriteLine("Loaded {0} reply counts.", this.knownReplyCounts.Count);
//			this.knownReplyCounts = new Dictionary<int, int>(this.knownReplyCounts.OrderByDescending(r => r.Key).Take(2000).ToDictionary(x => x.Key, x => x.Value));
//		}

//		public void SaveReplyCounts()
//		{
//			System.Diagnostics.Debug.WriteLine("Saving reply counts.");
//			DataContractSerializer ser = new DataContractSerializer(knownReplyCounts.GetType());

//			using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
//			{
//				using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream("postcounts.txt", FileMode.Create, isf))
//				{
//					ser.WriteObject(stream, this.knownReplyCounts);
//				}
//			}
//		}

//		/// <summary>
//		/// Checks if we have previously checked the reply count for a thread ever.
//		/// </summary>
//		/// <param name="threadId">The thread id.</param>
//		/// <returns></returns>
//		public bool PostSeenBefore(int threadId)
//		{
//			return this.knownReplyCounts.ContainsKey(threadId);
//		}

//		/// <summary>
//		/// Checks to see if the comment is in the cache and if the current reply count is greater than the last known.
//		/// </summary>
//		/// <param name="threadId">The thread id.</param>
//		/// <param name="currentReplyCount">The current reply count.</param>
//		/// <param name="updateCount">if set to <c>true</c> the count will be updated if it is not the same.</param>
//		/// <returns>
//		/// The number of new posts, -1 if it's new.
//		/// </returns>
//		public int NewReplyCount(int threadId, int currentReplyCount, bool updateCount)
//		{
//			//We COULD save reply counts as we get them, but that's a lot of excessive writing... let's not do that.
//			if (this.knownReplyCounts.ContainsKey(threadId))
//			{
//				var newReplyCount = currentReplyCount - this.knownReplyCounts[threadId];
//				if (newReplyCount > 0)
//				{
//					System.Diagnostics.Debug.WriteLine("{0} new replies for thread id {1}", newReplyCount, threadId);
//					if (updateCount)
//					{
//						System.Diagnostics.Debug.WriteLine("Updating reply count for thread id {0}", threadId);
//						this.knownReplyCounts[threadId] = currentReplyCount;
//					}
//				}
//				return newReplyCount;
//			}
//			if (updateCount)
//			{
//				//Haven't seen this thread before, add it to the cache.
//				System.Diagnostics.Debug.WriteLine("Thread id {0} is unknown, adding to cache with {1} replies", threadId, currentReplyCount);
//				this.knownReplyCounts.Add(threadId, currentReplyCount);
//			}
//			return -1;
//		}

//		#endregion

//		#region MyPosts
//		public MyPostsList MyPosts = new MyPostsList();
//		public MyRepliesList MyReplies = new MyRepliesList();
//		#endregion
//	}
//}
