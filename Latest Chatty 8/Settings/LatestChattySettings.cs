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
using Newtonsoft.Json.Linq;
using LatestChatty.Settings;
using Windows.UI.Xaml;

namespace Latest_Chatty_8.Settings
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
		private static readonly string pinnedComments = "PinnedComments";
		private static readonly string cloudSync = "cloudsync";
		private static readonly string lastCloudSyncTime = "lastcloudsynctime";

		private Windows.Storage.ApplicationDataContainer settingsContainer;

		private bool loadingSettingsInternal;

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
			try
			{
				this.loadingSettingsInternal = true;
				if (!this.CloudSync)
				{
					this.pinnedCommentIds = await ComplexSetting.ReadSetting<List<int>>(pinnedComments);
				}
				else
				{

					var json = await JSONDownloader.Download(Locations.MyCloudSettings);

					if (json != null)
					{
						this.AutoCollapseInformative = (bool)json[autocollapseinformative];
						this.AutoCollapseInteresting = (bool)json[autocollapseinteresting];
						this.AutoCollapseNws = (bool)json[autocollapsenws];
						this.AutoCollapseOffTopic = (bool)json[autocollapseofftopic];
						this.AutoCollapsePolitical = (bool)json[autocollapsepolitical];
						this.AutoCollapseStupid = (bool)json[autocollapsestupid];
						this.ShowInlineImages = (bool)json[showInlineImages];

						this.pinnedCommentIds = json["watched"].Children().Select(c => (int)c).ToList<int>();
					}
				}
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
				if (this.pinnedCommentIds == null)
				{
					this.pinnedCommentIds = new List<int>();
				}
				this.loadingSettingsInternal = false;
			}
		}

		public async Task RefreshPinnedComments()
		{
			var commentsToAdd = new List<Comment>();
			foreach (var pinnedItemId in this.pinnedCommentIds)
			{
				commentsToAdd.Add(await CommentDownloader.GetComment((int)pinnedItemId, false));
			}

			this.pinnedCommentsCollection.Clear();
			foreach (var c in commentsToAdd)
			{
				this.pinnedCommentsCollection.Add(c);
			}
		}

		public async void SaveToCloud()
		{
			try
			{
				//If cloud sync is enabled
				if (!this.loadingSettingsInternal && LatestChattySettings.Instance.CloudSync)
				{
					System.Diagnostics.Debug.WriteLine("Syncing to cloud...");
					var saveObject =
						new JObject(
							new JProperty("watched",
								new JArray(this.pinnedCommentIds)
								),
							new JProperty(showInlineImages, this.ShowInlineImages),
							new JProperty(autocollapseinformative, this.AutoCollapseInformative),
							new JProperty(autocollapseinteresting, this.AutoCollapseInteresting),
							new JProperty(autocollapsenws, this.AutoCollapseNws),
							new JProperty(autocollapseofftopic, this.AutoCollapseOffTopic),
							new JProperty(autocollapsepolitical, this.AutoCollapsePolitical),
							new JProperty(autocollapsestupid, this.AutoCollapseStupid));
					await POSTHelper.Send(Locations.MyCloudSettings, saveObject.ToString(), true);
				}
			}
			catch
			{
				System.Diagnostics.Debug.Assert(false);
			}
		}

		internal async void Resume()
		{
			await this.LoadLongRunningSettings();
		}

		void SavePinnedCommentList()
		{
			ComplexSetting.SetSetting<List<int>>(pinnedComments, this.PinnedComments.Select(p => p.Id).ToList());
		}

		private List<int> pinnedCommentIds = new List<int>();
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
				this.SaveToCloud();
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
				this.SaveToCloud();
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
				this.SaveToCloud();
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
				this.SaveToCloud();
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
				this.SaveToCloud();
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
				this.SaveToCloud();
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
			await this.RefreshPinnedComments();
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
					NotificationHelper.ReRegisterForNotifications();
				}
				else
				{
					NotificationHelper.UnRegisterNotifications();
				}
				this.NotifyPropertyChange();
			}
		}

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
				this.SaveToCloud();
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
				this.SaveToCloud();
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

		public void AddPinnedComment(Comment comment)
		{
			if (!this.pinnedCommentIds.Contains(comment.Id))
			{
				this.pinnedCommentIds.Add(comment.Id);
				this.pinnedCommentsCollection.Add(comment);
				this.SaveToCloud();
			}
		}

		public void RemovePinnedComment(Comment comment)
		{
			if (this.pinnedCommentIds.Contains(comment.Id))
			{
				var commentToRemove = this.pinnedCommentsCollection.SingleOrDefault(c => c.Id == comment.Id);
				if (commentToRemove != null)
				{
					this.pinnedCommentIds.Remove(commentToRemove.Id);
					this.pinnedCommentsCollection.Remove(commentToRemove);
					this.SaveToCloud();
				}
			}
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
