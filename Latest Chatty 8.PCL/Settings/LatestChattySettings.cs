using Latest_Chatty_8.DataModel;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Latest_Chatty_8.Shared.Networking;
using Newtonsoft.Json.Linq;
using Windows.UI.Xaml;
using System.IO;

namespace Latest_Chatty_8.Shared.Settings
{
	public class LatestChattySettings : INotifyPropertyChanged
	{
		//private static readonly string commentSize = "CommentSize";
		private static readonly string threadNavigationByDate = "ThreadNavigationByDate";
		private static readonly string showInlineImages = "embedimages";
		private static readonly string enableNotifications = "enableNotifications";
		private static readonly string username = "username";
		private static readonly string password = "password";
		private static readonly string notificationUID = "notificationid";
		private static readonly string autocollapsenws = "autocollapsenws";
		private static readonly string autocollapsestupid = "autocollapsestupid";
		private static readonly string autocollapseofftopic = "autocollapseofftopic";
		private static readonly string autocollapsepolitical = "autocollapsepolitical";
		private static readonly string autocollapseinformative = "autocollapseinformative";
		private static readonly string autocollapseinteresting = "autocollapseinteresting";
		private static readonly string autocollapsenews = "autocollapsenews";
		private static readonly string autopinonreply = "autopinonreply";
		private static readonly string autoremoveonexpire = "autoremoveonexpire";
		private static readonly string splitpercent = "splitpercentwin8";
		//private static readonly string pinnedThreads = "PinnedComments";
		private static readonly string cloudSync = "cloudsync";
		private static readonly string lastCloudSyncTime = "lastcloudsynctime";
		private static readonly string sortNewToTop = "sortnewtotop";
		private static readonly string refreshRate = "refreshrate";
		private static readonly string rightList = "rightlist";

		private Windows.Storage.ApplicationDataContainer settingsContainer;

		//private bool loadingSettingsInternal;

		private static LatestChattySettings instance = null;
		public static LatestChattySettings Instance
		{
			get
			{
				if (instance == null)
				{
					instance = new LatestChattySettings();
				}
				return instance;
			}
		}

		public LatestChattySettings()
		{
			this.pinnedThreadsCollection = new ObservableCollection<CommentThread>();
			this.PinnedThreads = new ReadOnlyObservableCollection<CommentThread>(this.pinnedThreadsCollection);

			var localContainer = Windows.Storage.ApplicationData.Current.LocalSettings;
			this.settingsContainer = localContainer.CreateContainer("generalSettings", Windows.Storage.ApplicationDataCreateDisposition.Always);

			if (!this.settingsContainer.Values.ContainsKey(enableNotifications))
			{
				this.settingsContainer.Values.Add(enableNotifications, false);
			}
			//if (!this.settingsContainer.Values.ContainsKey(commentSize))
			//{
			//	this.settingsContainer.Values.Add(commentSize, CommentViewSize.Small);
			//}
			if (!this.settingsContainer.Values.ContainsKey(threadNavigationByDate))
			{
				this.settingsContainer.Values.Add(threadNavigationByDate, true);
			}
			if (!this.settingsContainer.Values.ContainsKey(showInlineImages))
			{
				this.settingsContainer.Values.Add(showInlineImages, true);
			}
			if (!this.settingsContainer.Values.ContainsKey(username))
			{
				this.settingsContainer.Values.Add(username, string.Empty);
			}
			if (!this.settingsContainer.Values.ContainsKey(password))
			{
				this.settingsContainer.Values.Add(password, string.Empty);
			}
			if (!this.settingsContainer.Values.ContainsKey(notificationUID))
			{
				this.settingsContainer.Values.Add(notificationUID, Guid.NewGuid());
			}
			if (!this.settingsContainer.Values.ContainsKey(autocollapsenws))
			{
				this.settingsContainer.Values.Add(autocollapsenws, true);
			}
			if (!this.settingsContainer.Values.ContainsKey(autocollapsestupid))
			{
				this.settingsContainer.Values.Add(autocollapsestupid, false);
			}
			if (!this.settingsContainer.Values.ContainsKey(autocollapseofftopic))
			{
				this.settingsContainer.Values.Add(autocollapseofftopic, false);
			}
			if (!this.settingsContainer.Values.ContainsKey(autocollapsepolitical))
			{
				this.settingsContainer.Values.Add(autocollapsepolitical, false);
			}
			if (!this.settingsContainer.Values.ContainsKey(autocollapseinformative))
			{
				this.settingsContainer.Values.Add(autocollapseinformative, false);
			}
			if (!this.settingsContainer.Values.ContainsKey(autocollapseinteresting))
			{
				this.settingsContainer.Values.Add(autocollapseinteresting, false);
			}
			if (!this.settingsContainer.Values.ContainsKey(autocollapsenews))
			{
				this.settingsContainer.Values.Add(autocollapsenews, false);
			}
			if (!this.settingsContainer.Values.ContainsKey(autopinonreply))
			{
				this.settingsContainer.Values.Add(autopinonreply, false);
			}
			if (!this.settingsContainer.Values.ContainsKey(autoremoveonexpire))
			{
				this.settingsContainer.Values.Add(autoremoveonexpire, false);
			}
			if (!this.settingsContainer.Values.ContainsKey(cloudSync))
			{
				this.settingsContainer.Values.Add(cloudSync, false);
			}
			if (!this.settingsContainer.Values.ContainsKey(lastCloudSyncTime))
			{
				this.settingsContainer.Values.Add(lastCloudSyncTime, false);
			}
			if (!this.settingsContainer.Values.ContainsKey(splitpercent))
			{
				this.settingsContainer.Values.Add(splitpercent, 45);
			}
			if (!this.settingsContainer.Values.ContainsKey(sortNewToTop))
			{
				this.settingsContainer.Values.Add(sortNewToTop, true);
			}
			if (!this.settingsContainer.Values.ContainsKey(refreshRate))
			{
				this.settingsContainer.Values.Add(refreshRate, 5);
			}
			if(!this.settingsContainer.Values.ContainsKey(rightList))
			{
				this.settingsContainer.Values.Add(rightList, false);
			}
		}

		public void CreateInstance() { }

		private string clientSessionToken;

		public string ClientSessionToken { get { return clientSessionToken; } }

		#region WinChatty Service
		async private Task RefreshClientToken()
		{
			this.clientSessionToken = string.Empty;
			if (CoreServices.Instance.Credentials != null && !string.IsNullOrWhiteSpace(CoreServices.Instance.Credentials.UserName) && !string.IsNullOrWhiteSpace(CoreServices.Instance.Credentials.Password))
			{
				var data = POSTHelper.BuildDataString(new Dictionary<string, string> {
					{"username", CoreServices.Instance.Credentials.UserName},
					{"password", CoreServices.Instance.Credentials.Password},
					{"client", "latestchatty8"},
					{"version", "1"}
				});
				var response = await POSTHelper.Send(Locations.GetClientSessionToken, data, false);

				var responseData = await response.Content.ReadAsStringAsync();
				var parsedResponse = JToken.Parse(responseData);
				var sessionTokenJson = parsedResponse["clientSessionToken"];
				if (sessionTokenJson != null)
				{
					var sessionToken = sessionTokenJson.ToString();
					if (!string.IsNullOrWhiteSpace(sessionToken))
					{
						this.clientSessionToken = sessionToken;
						System.Diagnostics.Debug.WriteLine("Client Session Token refreshed - new value is {0}", this.clientSessionToken);
					}
				}
			}
		}

		#endregion

		public async Task LoadLongRunningSettings()
		{
			try
			{
				//this.loadingSettingsInternal = true;
				await this.RefreshClientToken();

					//var json = await JSONDownloader.Download(Locations.MyCloudSettings);
					//if (json != null)
					//{
					//	this.cloudSettings = (JObject)json;
					//	this.AutoCollapseInformative = json[autocollapseinformative] != null ? (bool)json[autocollapseinformative] : false;
					//	this.AutoCollapseInteresting = json[autocollapseinteresting] != null ? (bool)json[autocollapseinteresting] : false;
					//	this.AutoCollapseNews = json[autocollapsenews] != null ? (bool)json[autocollapsenews] : false;
					//	this.AutoCollapseNws = json[autocollapsenws] != null ? (bool)json[autocollapsenws] : true;
					//	this.AutoCollapseOffTopic = json[autocollapseofftopic] != null ? (bool)json[autocollapseofftopic] : false;
					//	this.AutoCollapsePolitical = json[autocollapsepolitical] != null ? (bool)json[autocollapsepolitical] : false;
					//	this.AutoCollapseStupid = json[autocollapsestupid] != null ? (bool)json[autocollapsestupid] : false;
					//	this.ShowInlineImages = json[showInlineImages] != null ? (bool)json[showInlineImages] : true;
					//	this.AutoPinOnReply = (json[autopinonreply] != null) ? (bool)json[autopinonreply] : false;
					//	this.AutoRemoveOnExpire = (json[autoremoveonexpire] != null) ? (bool)json[autoremoveonexpire] : false;
					//	this.SortNewToTop = (json[sortNewToTop] != null) ? (bool)json[sortNewToTop] : false;
					//	this.SplitPercent = (json[splitpercent] != null) ? (int)json[splitpercent] : 45;

					//	this.pinnedThreadIds = json["watched"].Children().Select(c => (int)c).ToList<int>();
					//}
			}
			catch (WebException e)
			{
				var r = e.Response as HttpWebResponse;
				if (r != null)
				{
					if (r.StatusCode == HttpStatusCode.Forbidden || r.StatusCode == HttpStatusCode.NotFound)
					{
						return;
					}
				}
				throw;
			}
			finally
			{
				//this.loadingSettingsInternal = false;
			}
		}

		public bool IsCommentPinned(int id)
		{
			return this.pinnedThreadIds.Contains(id);
		}

		public async Task SaveToCloud()
		{
			//try
			//{
			//	//If cloud sync is enabled
			//	if (!this.loadingSettingsInternal && LatestChattySettings.Instance.CloudSync)
			//	{
			//		System.Diagnostics.Debug.WriteLine("Syncing to cloud...");
			//		//If we don't have settings already, create them.
			//		if (this.cloudSettings == null)
			//		{
			//			this.cloudSettings = new JObject(
			//					  new JProperty("watched",
			//							new JArray(this.pinnedThreadIds)
			//							),
			//					  new JProperty(showInlineImages, this.ShowInlineImages),
			//					  new JProperty(autocollapseinformative, this.AutoCollapseInformative),
			//					  new JProperty(autocollapseinteresting, this.AutoCollapseInteresting),
			//					  new JProperty(autocollapsenews, this.AutoCollapseNews),
			//					  new JProperty(autocollapsenws, this.AutoCollapseNws),
			//					  new JProperty(autocollapseofftopic, this.AutoCollapseOffTopic),
			//					  new JProperty(autocollapsepolitical, this.AutoCollapsePolitical),
			//					  new JProperty(autocollapsestupid, this.AutoCollapseStupid),
			//					  new JProperty(autopinonreply, this.AutoPinOnReply),
			//					  new JProperty(autoremoveonexpire, this.AutoRemoveOnExpire),
			//					  new JProperty(sortNewToTop, this.SortNewToTop),
			//									new JProperty(splitpercent, this.SplitPercent));
			//		}
			//		else
			//		{
			//			//If we do have settings, use them.
			//			this.cloudSettings.CreateOrSet("watched", new JArray(this.pinnedThreadIds));
			//			this.cloudSettings.CreateOrSet(showInlineImages, this.ShowInlineImages);
			//			this.cloudSettings.CreateOrSet(autocollapseinformative, this.AutoCollapseInformative);
			//			this.cloudSettings.CreateOrSet(autocollapseinteresting, this.AutoCollapseInteresting);
			//			this.cloudSettings.CreateOrSet(autocollapsenews, this.AutoCollapseNews);
			//			this.cloudSettings.CreateOrSet(autocollapsenws, this.AutoCollapseNws);
			//			this.cloudSettings.CreateOrSet(autocollapseofftopic, this.AutoCollapseOffTopic);
			//			this.cloudSettings.CreateOrSet(autocollapsepolitical, this.AutoCollapsePolitical);
			//			this.cloudSettings.CreateOrSet(autocollapsestupid, this.AutoCollapseStupid);
			//			this.cloudSettings.CreateOrSet(autopinonreply, this.AutoPinOnReply);
			//			this.cloudSettings.CreateOrSet(autoremoveonexpire, this.AutoRemoveOnExpire);
			//			this.cloudSettings.CreateOrSet(sortNewToTop, this.SortNewToTop);
			//			this.cloudSettings.CreateOrSet(splitpercent, this.SplitPercent);
			//		}
			//		await POSTHelper.Send(Locations.MyCloudSettings, this.cloudSettings.ToString(), true);
			//	}
			//}
			//catch
			//{
			//	System.Diagnostics.Debug.Assert(false);
			//}
		}

		internal async void Resume()
		{
			await this.LoadLongRunningSettings();
		}

		private List<int> pinnedThreadIds = new List<int>();
		private ObservableCollection<CommentThread> pinnedThreadsCollection;
		public ReadOnlyObservableCollection<CommentThread> PinnedThreads
		{
			get;
			private set;
		}

		public bool AutoCollapseNws
		{
			get
			{
				object v;
				this.settingsContainer.Values.TryGetValue(autocollapsenws, out v);
				return (bool)v;
			}
			set
			{
				this.settingsContainer.Values[autocollapsenws] = value;
				this.NotifyPropertyChange();
				var t = this.SaveToCloud();
			}
		}

		public bool AutoCollapseNews
		{
			get
			{
				object v;
				this.settingsContainer.Values.TryGetValue(autocollapsenews, out v);
				return (bool)v;
			}
			set
			{
				this.settingsContainer.Values[autocollapsenews] = value;
				this.NotifyPropertyChange();
				var t = this.SaveToCloud();
			}
		}

		public bool AutoCollapseStupid
		{
			get
			{
				object v;
				this.settingsContainer.Values.TryGetValue(autocollapsestupid, out v);
				return (bool)v;
			}
			set
			{
				this.settingsContainer.Values[autocollapsestupid] = value;
				this.NotifyPropertyChange();
				var t = this.SaveToCloud();
			}
		}

		public bool AutoCollapseOffTopic
		{
			get
			{
				object v;
				this.settingsContainer.Values.TryGetValue(autocollapseofftopic, out v);
				return (bool)v;
			}
			set
			{
				this.settingsContainer.Values[autocollapseofftopic] = value;
				this.NotifyPropertyChange();
				var t = this.SaveToCloud();
			}
		}

		public bool AutoCollapsePolitical
		{
			get
			{
				object v;
				this.settingsContainer.Values.TryGetValue(autocollapsepolitical, out v);
				return (bool)v;
			}
			set
			{
				this.settingsContainer.Values[autocollapsepolitical] = value;
				this.NotifyPropertyChange();
				var t = this.SaveToCloud();
			}
		}

		public bool AutoCollapseInformative
		{
			get
			{
				object v;
				this.settingsContainer.Values.TryGetValue(autocollapseinformative, out v);
				return (bool)v;
			}
			set
			{
				this.settingsContainer.Values[autocollapseinformative] = value;
				this.NotifyPropertyChange();
				var t = this.SaveToCloud();
			}
		}

		public bool AutoCollapseInteresting
		{
			get
			{
				object v;
				this.settingsContainer.Values.TryGetValue(autocollapseinteresting, out v);
				return (bool)v;
			}
			set
			{
				this.settingsContainer.Values[autocollapseinteresting] = value;
				this.NotifyPropertyChange();
				var t = this.SaveToCloud();
			}
		}

		public bool AutoPinOnReply
		{
			get
			{
				object v;
				this.settingsContainer.Values.TryGetValue(autopinonreply, out v);
				return (bool)v;
			}
			set
			{
				this.settingsContainer.Values[autopinonreply] = value;
				this.NotifyPropertyChange();
				var t = this.SaveToCloud();
			}
		}

		public bool AutoRemoveOnExpire
		{
			get
			{
				object v;
				this.settingsContainer.Values.TryGetValue(autoremoveonexpire, out v);
				return (bool)v;
			}
			set
			{
				this.settingsContainer.Values[autoremoveonexpire] = value;
				this.NotifyPropertyChange();
				var t = this.SaveToCloud();
			}
		}

		public bool ShowRightChattyList
		{
			get
			{
				object v;
				this.settingsContainer.Values.TryGetValue(rightList, out v);
				return (bool)v;
			}
			set
			{
				this.settingsContainer.Values[rightList] = value;
				this.NotifyPropertyChange();
				var t = this.SaveToCloud();
			}
		}

		public bool CloudSync
		{
			get
			{
				object v;
				this.settingsContainer.Values.TryGetValue(cloudSync, out v);
				return (bool)v;
			}
			set
			{
				this.settingsContainer.Values[cloudSync] = value;
				this.NotifyPropertyChange();
				if (value)
				{
					this.CloudSyncEnabled();
				}

			}
		}

		async private void CloudSyncEnabled()
		{
			await this.LoadLongRunningSettings();
			//await this.RefreshPinnedThreads();
		}

		public DateTime LastCloudSyncTimeUtc
		{
			get
			{
				object v;
				this.settingsContainer.Values.TryGetValue(lastCloudSyncTime, out v);
				return DateTime.Parse((string)v);
			}
			set
			{
				this.settingsContainer.Values[lastCloudSyncTime] = value.ToUniversalTime();
				this.NotifyPropertyChange();
			}
		}

		public Guid NotificationID
		{
			get
			{
				object v;
				this.settingsContainer.Values.TryGetValue(notificationUID, out v);
				return (Guid)v;
			}
			set
			{
				this.settingsContainer.Values[notificationUID] = value;
			}
		}

		public bool EnableNotifications
		{
			get
			{
				object v;
				this.settingsContainer.Values.TryGetValue(enableNotifications, out v);
				return (bool)v;
			}
			set
			{
				this.settingsContainer.Values[enableNotifications] = value;
				if (value)
				{
					var t = NotificationHelper.ReRegisterForNotifications();
				}
				else
				{
					var t = NotificationHelper.UnRegisterNotifications();
				}
				this.NotifyPropertyChange();
			}
		}

		//public CommentViewSize CommentSize
		//{
		//	get
		//	{
		//	CommentViewSize size;
		//	this.settingsContainer.Values.TryGetValue(commentSize, out size);
		//	return size;
		//	}
		//	set
		//	{
		//	this.settingsContainer.Values[commentSize] = value;
		//	this.NotifyPropertyChange();
		//	}
		//}

		public bool ShowInlineImages
		{
			get
			{
				object v;
				this.settingsContainer.Values.TryGetValue(showInlineImages, out v);
				return (bool)v;
			}
			set
			{
				this.settingsContainer.Values[showInlineImages] = value;
				this.NotifyPropertyChange();
				var t = this.SaveToCloud();
			}
		}


		public bool ThreadNavigationByDate
		{
			get
			{
				return (bool)this.settingsContainer.Values[threadNavigationByDate];
			}
			set
			{
				this.settingsContainer.Values[threadNavigationByDate] = value;
				this.NotifyPropertyChange();
				var t = this.SaveToCloud();
			}
		}

		public bool SortNewToTop
		{
			get
			{
				return (bool)this.settingsContainer.Values[sortNewToTop];
			}
			set
			{
				this.settingsContainer.Values[sortNewToTop] = value;
				this.NotifyPropertyChange();
				var t = this.SaveToCloud();
			}
		}
		public int SplitPercent
		{
			get
			{
				object v;
				this.settingsContainer.Values.TryGetValue(splitpercent, out v);
				return (int)v;
			}
			set
			{
				this.settingsContainer.Values[splitpercent] = value;
				this.NotifyPropertyChange();
				var t = this.SaveToCloud();
			}
		}

		public string Username
		{
			get
			{
				return this.settingsContainer.Values[username].ToString();
			}
			set
			{
				this.settingsContainer.Values[username] = value;
				this.NotifyPropertyChange();
			}
		}

		public string Password
		{
			get
			{
				return this.settingsContainer.Values[password].ToString();
			}
			set
			{
				this.settingsContainer.Values[password] = value;
				this.NotifyPropertyChange();
			}
		}

		public int RefreshRate
		{
			get
			{
				object v;
				this.settingsContainer.Values.TryGetValue(refreshRate, out v);
				return (int)v;
			}
			set
			{
				this.settingsContainer.Values[refreshRate] = value;
				this.NotifyPropertyChange();
			}
		}

		////This should be in an extension method since it's app specific, but... meh.
		//public bool ShouldShowInlineImages
		//{
		//	get
		//	{
		//	ShowInlineImages val;
		//	this.settingsContainer.Values.TryGetValue(showInlineImages, out val);

		//	if (val == ShowInlineImages.Never)
		//	{
		//		return false;
		//	}

		//	if (val == ShowInlineImages.OnWiFi)
		//	{
		//		var type = NetworkInterface.;
		//		return type == NetworkInterfaceType.Ethernet ||
		//		type == NetworkInterfaceType.Wireless80211;
		//	}

		//	//Always.
		//	return true;
		//	}
		//}

		public event PropertyChangedEventHandler PropertyChanged;
		//private JObject cloudSettings;

		protected bool NotifyPropertyChange([CallerMemberName] String propertyName = null)
		{
			this.OnPropertyChanged(propertyName);
			return true;
		}

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			var eventHandler = this.PropertyChanged;
			if (eventHandler != null)
			{
				eventHandler(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}

	internal static class JSONExtensions
	{
		internal static JObject CreateOrSet(this JObject j, string propertyName, JToken obj)
		{
			if (j[propertyName] != null)
			{
				j[propertyName] = obj;
			}
			else
			{
				j.Add(propertyName, obj);
			}
			return j;
		}

		internal static JObject CreateOrSet(this JObject j, string propertyName, object obj)
		{
			return j.CreateOrSet(propertyName, new JValue(obj));
		}
	}
}
