using Latest_Chatty_8.Common;
using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Networking;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.UI;

namespace Latest_Chatty_8.Settings
{
	public class LatestChattySettings : INotifyPropertyChanged
	{
		private static readonly string enableNotifications = "enableNotifications";
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
		private static readonly string sortNewToTop = "sortnewtotop";
		private static readonly string refreshRate = "refreshrate";
		private static readonly string rightList = "rightlist";
		private static readonly string themeName = "themename";
		private static readonly string markReadOnSort = "markreadonsort";
		private static readonly string orderIndex = "orderindex";
		private static readonly string filterIndex = "filterindex";
		private static readonly string launchCount = "launchcount";
		private static readonly string newInfoVersion = "newInfoVersion";
		private static readonly string newInfoAvailable = "newInfoAvailable";
		private static readonly string chattySwipeLeftAction = "chattySwipeLeftAction";
		private static readonly string chattySwipeRightAction = "chattySwipeRightAction";
		private static readonly string externalYoutubeApp = "externalYoutubeApp";
		private static readonly string openUnknownLinksInEmbedded = "openUnknownLinksInEmbeddedBrowser";
		private static readonly string pinnedSingleThreadInlineAppBar = "pinnedSingleThreadInlineAppBar";
		private static readonly string seenMercuryBlast = "seenMercuryBlast";
		private static readonly string disableSplitView = "disableSplitView";
		private static readonly string disableNewsSplitView = "disableNewsSplitView";

		private Windows.Storage.ApplicationDataContainer remoteSettings;
		private Windows.Storage.ApplicationDataContainer localSettings;
		private AuthenticationManager authenticationManager;
		private readonly string currentVersion;

		public LatestChattySettings(AuthenticationManager authenticationManager)
		{
			var assemblyName = new AssemblyName(typeof(App).GetTypeInfo().Assembly.FullName);
			this.currentVersion = assemblyName.Version.ToString();

			this.authenticationManager = authenticationManager;
			this.remoteSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
			this.localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
			System.Diagnostics.Debug.WriteLine("Max roaming storage is {0} KB.", Windows.Storage.ApplicationData.Current.RoamingStorageQuota);

			#region Remote Settings Defaults
			if (!this.remoteSettings.Values.ContainsKey(autocollapsenws))
			{
				this.remoteSettings.Values.Add(autocollapsenws, true);
			}
			if (!this.remoteSettings.Values.ContainsKey(autocollapsestupid))
			{
				this.remoteSettings.Values.Add(autocollapsestupid, false);
			}
			if (!this.remoteSettings.Values.ContainsKey(autocollapseofftopic))
			{
				this.remoteSettings.Values.Add(autocollapseofftopic, false);
			}
			if (!this.remoteSettings.Values.ContainsKey(autocollapsepolitical))
			{
				this.remoteSettings.Values.Add(autocollapsepolitical, false);
			}
			if (!this.remoteSettings.Values.ContainsKey(autocollapseinformative))
			{
				this.remoteSettings.Values.Add(autocollapseinformative, false);
			}
			if (!this.remoteSettings.Values.ContainsKey(autocollapseinteresting))
			{
				this.remoteSettings.Values.Add(autocollapseinteresting, false);
			}
			if (!this.remoteSettings.Values.ContainsKey(autocollapsenews))
			{
				this.remoteSettings.Values.Add(autocollapsenews, false);
			}
			if (!this.remoteSettings.Values.ContainsKey(autopinonreply))
			{
				this.remoteSettings.Values.Add(autopinonreply, false);
			}
			if (!this.remoteSettings.Values.ContainsKey(autoremoveonexpire))
			{
				this.remoteSettings.Values.Add(autoremoveonexpire, false);
			}
			if (!this.remoteSettings.Values.ContainsKey(sortNewToTop))
			{
				this.remoteSettings.Values.Add(sortNewToTop, true);
			}
			if (!this.remoteSettings.Values.ContainsKey(rightList))
			{
				this.remoteSettings.Values.Add(rightList, false);
			}
			if (!this.remoteSettings.Values.ContainsKey(themeName))
			{
				this.remoteSettings.Values.Add(themeName, "Default");
			}
			if (!this.remoteSettings.Values.ContainsKey(markReadOnSort))
			{
				this.remoteSettings.Values.Add(markReadOnSort, false);
			}
			if (!this.remoteSettings.Values.ContainsKey(launchCount))
			{
				this.remoteSettings.Values.Add(launchCount, 0);
			}
			if (!this.remoteSettings.Values.ContainsKey(chattySwipeLeftAction))
			{
				this.remoteSettings.Values.Add(chattySwipeLeftAction, Enum.GetName(typeof(ChattySwipeOperationType), ChattySwipeOperationType.Collapse));
			}
			if (!this.remoteSettings.Values.ContainsKey(chattySwipeRightAction))
			{
				this.remoteSettings.Values.Add(chattySwipeRightAction, Enum.GetName(typeof(ChattySwipeOperationType), ChattySwipeOperationType.Pin));
			}
			if (!this.remoteSettings.Values.ContainsKey(seenMercuryBlast))
			{
				this.remoteSettings.Values.Add(seenMercuryBlast, false);
			}
			#endregion

			#region Local Settings Defaults
			if (!this.localSettings.Values.ContainsKey(enableNotifications))
			{
				this.localSettings.Values.Add(enableNotifications, true);
			}
			if (!this.localSettings.Values.ContainsKey(notificationUID))
			{
				this.localSettings.Values.Add(notificationUID, Guid.NewGuid());
			}
			if (!this.localSettings.Values.ContainsKey(refreshRate))
			{
				this.localSettings.Values.Add(refreshRate, 5);
			}
			if (!this.localSettings.Values.ContainsKey(orderIndex))
			{
				this.localSettings.Values.Add(orderIndex, 2);
			}
			if (!this.localSettings.Values.ContainsKey(filterIndex))
			{
				this.localSettings.Values.Add(filterIndex, 0);
			}
			if (!this.localSettings.Values.ContainsKey(newInfoAvailable))
			{
				this.localSettings.Values.Add(newInfoAvailable, false);
			}
			if (!this.localSettings.Values.ContainsKey(newInfoVersion))
			{
				this.localSettings.Values.Add(newInfoVersion, this.currentVersion);
			}
			if (!this.localSettings.Values.ContainsKey(externalYoutubeApp))
			{
				this.localSettings.Values.Add(externalYoutubeApp, Enum.GetName(typeof(ExternalYoutubeAppType), ExternalYoutubeAppType.Browser));
			}
			if (!this.localSettings.Values.ContainsKey(openUnknownLinksInEmbedded))
			{
				this.localSettings.Values.Add(openUnknownLinksInEmbedded, true);
			}
			if (!this.localSettings.Values.ContainsKey(pinnedSingleThreadInlineAppBar))
			{
				this.localSettings.Values.Add(pinnedSingleThreadInlineAppBar, false);
			}
			if (!this.localSettings.Values.ContainsKey(disableSplitView))
			{
				this.localSettings.Values.Add(disableSplitView, false);
			}
			if (!this.localSettings.Values.ContainsKey(disableNewsSplitView))
			{
				this.localSettings.Values.Add(disableNewsSplitView, false);
			}
			#endregion

			this.IsUpdateInfoAvailable = !this.localSettings.Values[newInfoVersion].ToString().Equals(this.currentVersion, StringComparison.Ordinal);
			this.Theme = this.AvailableThemes.SingleOrDefault(t => t.Name.Equals(this.ThemeName)) ?? this.AvailableThemes.Single(t => t.Name.Equals("Default"));
		}

		#region Remote Settings
		public bool AutoCollapseNws
		{
			get
			{
				object v;
				this.remoteSettings.Values.TryGetValue(autocollapsenws, out v);
				return (bool)v;
			}
			set
			{
				this.remoteSettings.Values[autocollapsenws] = value;
				this.TrackSettingChanged(value.ToString());
				this.NotifyPropertyChange();
			}
		}

		public bool AutoCollapseNews
		{
			get
			{
				object v;
				this.remoteSettings.Values.TryGetValue(autocollapsenews, out v);
				return (bool)v;
			}
			set
			{
				this.remoteSettings.Values[autocollapsenews] = value;
				this.TrackSettingChanged(value.ToString());
				this.NotifyPropertyChange();
			}
		}

		public bool AutoCollapseStupid
		{
			get
			{
				object v;
				this.remoteSettings.Values.TryGetValue(autocollapsestupid, out v);
				return (bool)v;
			}
			set
			{
				this.remoteSettings.Values[autocollapsestupid] = value;
				this.TrackSettingChanged(value.ToString());
				this.NotifyPropertyChange();
			}
		}

		public bool AutoCollapseOffTopic
		{
			get
			{
				object v;
				this.remoteSettings.Values.TryGetValue(autocollapseofftopic, out v);
				return (bool)v;
			}
			set
			{
				this.remoteSettings.Values[autocollapseofftopic] = value;
				this.TrackSettingChanged(value.ToString());
				this.NotifyPropertyChange();
			}
		}

		public bool AutoCollapsePolitical
		{
			get
			{
				object v;
				this.remoteSettings.Values.TryGetValue(autocollapsepolitical, out v);
				return (bool)v;
			}
			set
			{
				this.remoteSettings.Values[autocollapsepolitical] = value;
				this.TrackSettingChanged(value.ToString());
				this.NotifyPropertyChange();
			}
		}

		public bool AutoCollapseInformative
		{
			get
			{
				object v;
				this.remoteSettings.Values.TryGetValue(autocollapseinformative, out v);
				return (bool)v;
			}
			set
			{
				this.remoteSettings.Values[autocollapseinformative] = value;
				this.TrackSettingChanged(value.ToString());
				this.NotifyPropertyChange();
			}
		}

		public bool AutoCollapseInteresting
		{
			get
			{
				object v;
				this.remoteSettings.Values.TryGetValue(autocollapseinteresting, out v);
				return (bool)v;
			}
			set
			{
				this.remoteSettings.Values[autocollapseinteresting] = value;
				this.TrackSettingChanged(value.ToString());
				this.NotifyPropertyChange();
			}
		}

		public bool AutoPinOnReply
		{
			get
			{
				object v;
				this.remoteSettings.Values.TryGetValue(autopinonreply, out v);
				return (bool)v;
			}
			set
			{
				this.remoteSettings.Values[autopinonreply] = value;
				this.TrackSettingChanged(value.ToString());
				this.NotifyPropertyChange();
			}
		}

		public bool AutoRemoveOnExpire
		{
			get
			{
				object v;
				this.remoteSettings.Values.TryGetValue(autoremoveonexpire, out v);
				return (bool)v;
			}
			set
			{
				this.remoteSettings.Values[autoremoveonexpire] = value;
				this.TrackSettingChanged(value.ToString());
				this.NotifyPropertyChange();
			}
		}

		public bool ShowRightChattyList
		{
			get
			{
				object v;
				this.remoteSettings.Values.TryGetValue(rightList, out v);
				return (bool)v;
			}
			set
			{
				this.remoteSettings.Values[rightList] = value;
				this.TrackSettingChanged(value.ToString());
				this.NotifyPropertyChange();
			}
		}

		public bool MarkReadOnSort
		{
			get
			{
				object v;
				this.remoteSettings.Values.TryGetValue(markReadOnSort, out v);
				return (bool)v;
			}
			set
			{
				this.remoteSettings.Values[markReadOnSort] = value;
				this.TrackSettingChanged(value.ToString());
				this.NotifyPropertyChange();
			}
		}

		public ChattySwipeOperation ChattyLeftSwipeAction
		{
			get
			{
				object v;
				this.remoteSettings.Values.TryGetValue(chattySwipeLeftAction, out v);
				var returnOp = this.ChattySwipeOperations.SingleOrDefault(op => op.Type == (ChattySwipeOperationType)Enum.Parse(typeof(ChattySwipeOperationType), (string)v));
				if (returnOp == null)
				{
					returnOp = this.ChattySwipeOperations.Single(op => op.Type == ChattySwipeOperationType.Collapse);
				}
				return returnOp;
			}
			set
			{
				this.remoteSettings.Values[chattySwipeLeftAction] = Enum.GetName(typeof(ChattySwipeOperationType), value.Type);
				this.TrackSettingChanged(value.Type.ToString());
				this.NotifyPropertyChange();
			}
		}

		public ChattySwipeOperation ChattyRightSwipeAction
		{
			get
			{
				object v;
				this.remoteSettings.Values.TryGetValue(chattySwipeRightAction, out v);
				var returnOp = this.ChattySwipeOperations.SingleOrDefault(op => op.Type == (ChattySwipeOperationType)Enum.Parse(typeof(ChattySwipeOperationType), (string)v));
				if (returnOp == null)
				{
					returnOp = this.ChattySwipeOperations.Single(op => op.Type == ChattySwipeOperationType.Pin);
				}
				return returnOp;
			}
			set
			{
				this.remoteSettings.Values[chattySwipeRightAction] = Enum.GetName(typeof(ChattySwipeOperationType), value.Type);
				this.TrackSettingChanged(value.Type.ToString());
				this.NotifyPropertyChange();
			}
		}

		public int LaunchCount
		{
			get
			{
				object v;
				this.remoteSettings.Values.TryGetValue(launchCount, out v);
				return (int)v;
			}
			set
			{
				this.remoteSettings.Values[launchCount] = value;
				this.TrackSettingChanged(value.ToString());
				this.NotifyPropertyChange();
			}
		}

		public string ThemeName
		{
			get
			{
				object v;
				this.remoteSettings.Values.TryGetValue(themeName, out v);
				return string.IsNullOrWhiteSpace((string)v) ? "Default" : (string)v;
			}
			set
			{
				this.remoteSettings.Values[themeName] = value;
				this.Theme = this.AvailableThemes.SingleOrDefault(t => t.Name.Equals(value)) ?? this.AvailableThemes.Single(t => t.Name.Equals("Default"));
				this.TrackSettingChanged(value.ToString());
				this.NotifyPropertyChange();
			}
		}

		public bool SeenMercuryBlast
		{
			get
			{
				object v;
				this.remoteSettings.Values.TryGetValue(seenMercuryBlast, out v);
				return (bool)v;
			}
			set
			{
				this.remoteSettings.Values[seenMercuryBlast] = value;
				this.TrackSettingChanged(value.ToString());
				this.NotifyPropertyChange();
			}
		}
		#endregion

		#region Local Settings
		public Guid NotificationID
		{
			get
			{
				object v;
				this.localSettings.Values.TryGetValue(notificationUID, out v);
				return (Guid)v;
			}
			set
			{
				this.localSettings.Values[notificationUID] = value;
				this.TrackSettingChanged(value.ToString());
			}
		}
		public bool EnableNotifications
		{
			get
			{
				object v;
				this.localSettings.Values.TryGetValue(enableNotifications, out v);
				return (bool)v;
			}
			set
			{
				this.localSettings.Values[enableNotifications] = value;
				this.NotifyPropertyChange();
			}
		}
		public bool DisableSplitView
		{
			get
			{
				object v;
				this.localSettings.Values.TryGetValue(disableSplitView, out v);
				return (bool)v;
			}
			set
			{
				this.localSettings.Values[disableSplitView] = value;
				this.TrackSettingChanged(value.ToString());
				this.NotifyPropertyChange();
			}
		}

		public bool DisableNewsSplitView
		{
			get
			{
				object v;
				this.localSettings.Values.TryGetValue(disableNewsSplitView, out v);
				return (bool)v;
			}
			set
			{
				this.localSettings.Values[disableNewsSplitView] = value;
				this.TrackSettingChanged(value.ToString());
				this.NotifyPropertyChange();
			}
		}

		public bool PinnedSingleThreadAppBar
		{
			get
			{
				object v;
				this.localSettings.Values.TryGetValue(pinnedSingleThreadInlineAppBar, out v);
				return (bool)v;
			}
			set
			{
				this.localSettings.Values[pinnedSingleThreadInlineAppBar] = value;
				this.TrackSettingChanged(value.ToString());
				this.NotifyPropertyChange();
			}
		}

		public bool OpenUnknownLinksInEmbeddedBrowser
		{
			get
			{
				object v;
				this.localSettings.Values.TryGetValue(openUnknownLinksInEmbedded, out v);
				return (bool)v;
			}
			set
			{
				this.localSettings.Values[openUnknownLinksInEmbedded] = value;
				this.TrackSettingChanged(value.ToString());
				this.NotifyPropertyChange();
			}
		}

		public ExternalYoutubeApp ExternalYoutubeApp
		{
			get
			{
				object v;
				this.localSettings.Values.TryGetValue(externalYoutubeApp, out v);
				var app = this.ExternalYoutubeApps.SingleOrDefault(op => op.Type == (ExternalYoutubeAppType)Enum.Parse(typeof(ExternalYoutubeAppType), (string)v));
				if (app == null)
				{
					app = this.ExternalYoutubeApps.Single(op => op.Type == ExternalYoutubeAppType.Browser);
				}
				return app;
			}
			set
			{
				this.localSettings.Values[externalYoutubeApp] = Enum.GetName(typeof(ExternalYoutubeAppType), value.Type);
				this.TrackSettingChanged(value.Type.ToString());
				this.NotifyPropertyChange();
			}
		}

		public int RefreshRate
		{
			get
			{
				object v;
				this.localSettings.Values.TryGetValue(refreshRate, out v);
				return (int)v;
			}
			set
			{
				this.localSettings.Values[refreshRate] = value;
				this.TrackSettingChanged(value.ToString());
				this.NotifyPropertyChange();
			}
		}

		public int FilterIndex
		{
			get
			{
				object v;
				this.localSettings.Values.TryGetValue(filterIndex, out v);
				return (int)v;
			}
			set
			{
				this.localSettings.Values[filterIndex] = value;
				//this.TrackSettingChanged(value.ToString());
				this.NotifyPropertyChange();
			}
		}

		public int OrderIndex
		{
			get
			{
				object v;
				this.localSettings.Values.TryGetValue(orderIndex, out v);
				return (int)v;
			}
			set
			{
				this.localSettings.Values[orderIndex] = value;
				//this.TrackSettingChanged(value.ToString());
				this.NotifyPropertyChange();
			}
		}

		public bool IsUpdateInfoAvailable
		{
			get
			{
				object v;
				this.localSettings.Values.TryGetValue(newInfoAvailable, out v);
				return (bool)v;
			}
			set
			{
				this.localSettings.Values[newInfoAvailable] = value;
				this.NotifyPropertyChange();
			}
		}

		#endregion

		public string UpdateInfo
		{
			get
			{
				return @"New in version " + this.currentVersion + Environment.NewLine + @"
• Clicking a notification will now take you to the thread the notification occurred on
• Minor bug fixes, performance improvements, misc changes
";
			}
		}

		public void MarkUpdateInfoRead()
		{
			this.localSettings.Values[newInfoVersion] = this.currentVersion;
			this.IsUpdateInfoAvailable = false;
		}

		private ThemeColorOption npcCurrentTheme;
		public ThemeColorOption Theme
		{
			get { return this.npcCurrentTheme; }
			private set
			{
				if (npcCurrentTheme?.Name != value.Name)
				{
					this.npcCurrentTheme = value;
					App.Current.Resources["ThemeHighlight"] = new Windows.UI.Xaml.Media.SolidColorBrush(value.AccentBackgroundColor);
					this.NotifyPropertyChange();
				}
			}
		}

		private List<ThemeColorOption> availableThemes;
		public List<ThemeColorOption> AvailableThemes
		{
			get
			{
				if (availableThemes == null)
				{
					availableThemes = new List<ThemeColorOption>
					{
						new ThemeColorOption("Default", Color.FromArgb(255, 63, 110, 127), Colors.White),
						new ThemeColorOption(
							"System",
							(new Windows.UI.ViewManagement.UISettings()).GetColorValue(Windows.UI.ViewManagement.UIColorType.Accent),
							Colors.White
						),
						new ThemeColorOption("Lime", Color.FromArgb(255, 164, 196, 0), Colors.White),
						new ThemeColorOption("Green", Color.FromArgb(255, 96, 169, 23), Colors.White),
						new ThemeColorOption("Emerald", Color.FromArgb(255, 0, 138, 0), Colors.White),
						new ThemeColorOption("Teal", Color.FromArgb(255, 0, 171, 169), Colors.White),
						new ThemeColorOption("Cyan", Color.FromArgb(255, 27, 161, 226), Colors.White),
						new ThemeColorOption("Cobalt", Color.FromArgb(255, 0, 80, 239), Colors.White),
						new ThemeColorOption("Indigo", Color.FromArgb(255, 106, 0, 255), Colors.White),
						new ThemeColorOption("Violet", Color.FromArgb(255, 170, 0, 255), Colors.White),
						new ThemeColorOption("Pink", Color.FromArgb(255, 244, 114, 208), Colors.White),
						new ThemeColorOption("Magenta", Color.FromArgb(255, 216, 0, 115), Colors.White),
						new ThemeColorOption("Crimson", Color.FromArgb(255, 162, 0, 37), Colors.White),
						new ThemeColorOption("Red", Color.FromArgb(255, 255, 35, 10), Colors.White),
						new ThemeColorOption("Orange", Color.FromArgb(255, 250, 104, 0), Colors.White),
						new ThemeColorOption("Amber", Color.FromArgb(255, 240, 163, 10), Colors.White),
						new ThemeColorOption("Yellow", Color.FromArgb(255, 227, 200, 0), Colors.White),
						new ThemeColorOption("Brown", Color.FromArgb(255, 130, 90, 44), Colors.White),
						new ThemeColorOption("Olive", Color.FromArgb(255, 109, 135, 100), Colors.White),
						new ThemeColorOption("Steel", Color.FromArgb(255, 100, 118, 135), Colors.White),
						new ThemeColorOption("Mauve", Color.FromArgb(255, 118, 96, 138), Colors.White),
						new ThemeColorOption("Taupe", Color.FromArgb(255, 135, 121, 78), Colors.White),
						new ThemeColorOption("Black", Color.FromArgb(255, 0, 0, 0), Colors.White),

						//new ThemeColorOption("White", Colors.White, Color.FromArgb(255, 0, 0, 0), Color.FromArgb(255, 235, 235, 235), Color.FromArgb(255, 0, 0, 0))
					};
				}
				return this.availableThemes;
			}
		}

		private List<ChattySwipeOperation> chattySwipeOperations;
		public List<ChattySwipeOperation> ChattySwipeOperations
		{
			get
			{
				if (this.chattySwipeOperations == null)
				{
					this.chattySwipeOperations = new List<ChattySwipeOperation>
					{
						new ChattySwipeOperation(ChattySwipeOperationType.Collapse, "", "(Un)Collapse"),
						new ChattySwipeOperation(ChattySwipeOperationType.MarkRead, "", "Mark Thread Read"),
						new ChattySwipeOperation(ChattySwipeOperationType.Pin, "", "(Un)Pin")
					};
				}
				return this.chattySwipeOperations;
			}
		}

		private List<ExternalYoutubeApp> externalYoutubeApps;
		public List<ExternalYoutubeApp> ExternalYoutubeApps
		{
			get
			{
				if (this.externalYoutubeApps == null)
				{
					this.externalYoutubeApps = new List<ExternalYoutubeApp>
					{
						new ExternalYoutubeApp(ExternalYoutubeAppType.Browser, "https://www.youtube.com/watch?v={0}", "Browser"),
						new ExternalYoutubeApp(ExternalYoutubeAppType.Hyper, "hyper://{0}", "Hyper"),
						new ExternalYoutubeApp(ExternalYoutubeAppType.Tubecast, "tubecast:VideoId={0}", "Tubecast"),
						new ExternalYoutubeApp(ExternalYoutubeAppType.Mytube, "mytube:link=https://www.youtube.com/watch?v={0}", "myTube!")
					};
				}
				return this.externalYoutubeApps;
			}
		}


		private Int32 ColorToInt32(Color color)
		{
			return (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;
		}

		private Color Int32ToColor(Int32 intColor)
		{
			return Windows.UI.Color.FromArgb((byte)(intColor >> 24), (byte)(intColor >> 16), (byte)(intColor >> 8), (byte)intColor);
		}

		private void TrackSettingChanged(string settingValue, [CallerMemberName] string propertyName = "")
		{
			(new Microsoft.ApplicationInsights.TelemetryClient()).TrackEvent($"Setting-{propertyName}-Updated", new Dictionary<string, string> { { "settingName", propertyName }, { "settingValue", settingValue } });
		}

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



		public bool ShouldAutoCollapseCommentThread(CommentThread thread)
		{
			var threadCategory = thread.Comments[0].Category;
			return (this.AutoCollapseInformative && thread.Comments[0].Category == PostCategory.informative)
					|| (this.AutoCollapseInteresting && threadCategory == PostCategory.interesting)
					|| (this.AutoCollapseNews && threadCategory == PostCategory.newsarticle)
					|| (this.AutoCollapseNws && threadCategory == PostCategory.nws)
					|| (this.AutoCollapseOffTopic && threadCategory == PostCategory.offtopic)
					|| (this.AutoCollapsePolitical && threadCategory == PostCategory.political)
					|| (this.AutoCollapseStupid && threadCategory == PostCategory.stupid);
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
