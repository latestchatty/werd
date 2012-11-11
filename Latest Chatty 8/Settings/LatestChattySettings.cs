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
using Latest_Chatty_8.Networking;

namespace Latest_Chatty_8.Settings
{
	public class LatestChattySettings : INotifyPropertyChanged
	{
		//TODO: Sync settings - or at least pinned posts(?) to central server.
		private static readonly string commentSize = "CommentSize";
		private static readonly string threadNavigationByDate = "ThreadNavigationByDate";
		private static readonly string showInlineImages = "ShowInline";
		private static readonly string notificationType = "NotificationType";
		private static readonly string username = "username";
		private static readonly string password = "password";
		private static readonly string notificationUID = "notificationid";
		private static readonly string autocollapsenws = "autocollapsenws";
		private static readonly string autocollapsestupid = "autocollapsestupid";
		private static readonly string autocollapseofftopic = "autocollapseofftopic";
		private static readonly string autocollapsepolitical = "autocollapsepolitical";
		private static readonly string autocollapseinformative = "autocollapseinformative";
		private static readonly string autocollapseinteresting = "autocollapseinteresting";
		private static readonly string pinnedComments = "PinnedComments";
		private static readonly string cloudSync = "cloudsync";
		private static readonly string lastCloudSyncTime = "lastcloudsynctime";

		private Windows.Storage.ApplicationDataContainer settingsContainer;

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
			this.pinnedCommentsCollection = new ObservableCollection<Comment>();
			this.PinnedComments = new ReadOnlyObservableCollection<Comment>(this.pinnedCommentsCollection);

			var localContainer = Windows.Storage.ApplicationData.Current.LocalSettings;
			this.settingsContainer = localContainer.CreateContainer("generalSettings", Windows.Storage.ApplicationDataCreateDisposition.Always);

			//if (!this.settingsContainer.Values.ContainsKey(notificationType))
			//{
			//	this.settingsContainer.Values.Add(notificationType, NotificationType.None);
			//}
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
			if (!this.settingsContainer.Values.ContainsKey(cloudSync))
			{
				this.settingsContainer.Values.Add(cloudSync, false);
			}
			if (!this.settingsContainer.Values.ContainsKey(lastCloudSyncTime))
			{
				this.settingsContainer.Values.Add(lastCloudSyncTime, false);
			}
		}

		public void CreateInstance() { }

		public async Task LoadLongRunningSettings()
		{
			this.pinnedCommentsCollection.Clear();
			if (!this.CloudSync)
			{
				var pinnedCommentList = await ComplexSetting.ReadSetting<List<int>>(pinnedComments);
				foreach (var commentId in pinnedCommentList)
				{
					this.pinnedCommentsCollection.Add(await CommentDownloader.GetComment(commentId, false));
				}
			}
			else
			{

			}

			try
			{
				var json = await JSONDownloader.Download(Locations.MyCloudSettings);
			}
			catch (WebException e)
			{
				var r = e.Response as HttpWebResponse;
				if (r != null)
				{
					if (r.StatusCode == HttpStatusCode.Forbidden)
					{
						return;
					}
				}
				throw;
			}
		}

		public async void SaveToCloud()
		{
			//If cloud sync is enabled
			if (LatestChattySettings.Instance.CloudSync)
			{

			}
		}

		internal async void Resume()
		{
			this.LoadLongRunningSettings();
		}

		void SavePinnedCommentList()
		{
			ComplexSetting.SetSetting<List<int>>(pinnedComments, this.PinnedComments.Select(p => p.Id).ToList());
		}

		private ObservableCollection<Comment> pinnedCommentsCollection;
		public ReadOnlyObservableCollection<Comment> PinnedComments
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
			}
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
			//set
			//{
			//   this.isoStore[notificationUID] = value;
			//   this.isoStore.Save();
			//}
		}

		//public NotificationType NotificationType
		//{
		//	get
		//	{
		//		object v;
		//		this.settingsContainer.Values.TryGetValue(notificationType, out v);
		//		return (NotificationType)v;
		//	}
		//	set
		//	{
		//		this.settingsContainer.Values[notificationType] = value;
		//		this.NotifyPropertyChange();
		//	}
		//}

		//public CommentViewSize CommentSize
		//{
		//	get
		//	{
		//		CommentViewSize size;
		//		this.settingsContainer.Values.TryGetValue(commentSize, out size);
		//		return size;
		//	}
		//	set
		//	{
		//		this.settingsContainer.Values[commentSize] = value;
		//		this.NotifyPropertyChange();
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

		public void AddPinnedComment(int commentId)
		{

		}

		public void RemovePinnedComment(int commentId)
		{
		}

		////This should be in an extension method since it's app specific, but... meh.
		//public bool ShouldShowInlineImages
		//{
		//	get
		//	{
		//		ShowInlineImages val;
		//		this.settingsContainer.Values.TryGetValue(showInlineImages, out val);

		//		if (val == ShowInlineImages.Never)
		//		{
		//			return false;
		//		}

		//		if (val == ShowInlineImages.OnWiFi)
		//		{
		//			var type = NetworkInterface.;
		//			return type == NetworkInterfaceType.Ethernet ||
		//				type == NetworkInterfaceType.Wireless80211;
		//		}

		//		//Always.
		//		return true;
		//	}
		//}

		public event PropertyChangedEventHandler PropertyChanged;

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
}
