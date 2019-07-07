using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Windows.Storage;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Latest_Chatty_8.DataModel;
using MyToolkit.Multimedia;
using Newtonsoft.Json.Linq;

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
		private static readonly string pinnedChattyAppBar = "pinnedChattyAppBar";
		private static readonly string seenMercuryBlast = "seenMercuryBlast";
		private static readonly string disableSplitView = "disableSplitView";
		private static readonly string disableNewsSplitView = "disableNewsSplitView";
		private static readonly string fontSize = "fontSize";
		private static readonly string localFirstRun = "localFirstRun";
		private static readonly string embeddedYouTubeResolution = "embeddedYouTubeResolution";
		private static readonly string notifyOnNameMention = "notifyOnNameMention";
		private static readonly string pinMarkup = "pinMarkup";

		private readonly ApplicationDataContainer _remoteSettings;
		private readonly ApplicationDataContainer _localSettings;
		private readonly string _currentVersion;

		public LatestChattySettings()
		{
			var assemblyName = new AssemblyName(typeof(App).GetTypeInfo().Assembly.FullName);
			_currentVersion = assemblyName.Version.ToString();

			_remoteSettings = ApplicationData.Current.RoamingSettings;
			_localSettings = ApplicationData.Current.LocalSettings;
			Debug.WriteLine("Max roaming storage is {0} KB.", ApplicationData.Current.RoamingStorageQuota);

			#region Remote Settings Defaults
			if (!_remoteSettings.Values.ContainsKey(autocollapsenws))
			{
				_remoteSettings.Values.Add(autocollapsenws, true);
			}
			if (!_remoteSettings.Values.ContainsKey(autocollapsestupid))
			{
				_remoteSettings.Values.Add(autocollapsestupid, false);
			}
			if (!_remoteSettings.Values.ContainsKey(autocollapseofftopic))
			{
				_remoteSettings.Values.Add(autocollapseofftopic, false);
			}
			if (!_remoteSettings.Values.ContainsKey(autocollapsepolitical))
			{
				_remoteSettings.Values.Add(autocollapsepolitical, false);
			}
			if (!_remoteSettings.Values.ContainsKey(autocollapseinformative))
			{
				_remoteSettings.Values.Add(autocollapseinformative, false);
			}
			if (!_remoteSettings.Values.ContainsKey(autocollapseinteresting))
			{
				_remoteSettings.Values.Add(autocollapseinteresting, false);
			}
			if (!_remoteSettings.Values.ContainsKey(autocollapsenews))
			{
				_remoteSettings.Values.Add(autocollapsenews, false);
			}
			if (!_remoteSettings.Values.ContainsKey(autopinonreply))
			{
				_remoteSettings.Values.Add(autopinonreply, false);
			}
			if (!_remoteSettings.Values.ContainsKey(autoremoveonexpire))
			{
				_remoteSettings.Values.Add(autoremoveonexpire, false);
			}
			if (!_remoteSettings.Values.ContainsKey(sortNewToTop))
			{
				_remoteSettings.Values.Add(sortNewToTop, true);
			}
			if (!_remoteSettings.Values.ContainsKey(rightList))
			{
				_remoteSettings.Values.Add(rightList, false);
			}
			if (!_remoteSettings.Values.ContainsKey(themeName))
			{
				_remoteSettings.Values.Add(themeName, "Default");
			}
			if (!_remoteSettings.Values.ContainsKey(markReadOnSort))
			{
				_remoteSettings.Values.Add(markReadOnSort, false);
			}
			if (!_remoteSettings.Values.ContainsKey(launchCount))
			{
				_remoteSettings.Values.Add(launchCount, 0);
			}
			if (!_remoteSettings.Values.ContainsKey(chattySwipeLeftAction))
			{
				_remoteSettings.Values.Add(chattySwipeLeftAction, Enum.GetName(typeof(ChattySwipeOperationType), ChattySwipeOperationType.Collapse));
			}
			if (!_remoteSettings.Values.ContainsKey(chattySwipeRightAction))
			{
				_remoteSettings.Values.Add(chattySwipeRightAction, Enum.GetName(typeof(ChattySwipeOperationType), ChattySwipeOperationType.Pin));
			}
			if (!_remoteSettings.Values.ContainsKey(seenMercuryBlast))
			{
				_remoteSettings.Values.Add(seenMercuryBlast, false);
			}
			#endregion

			#region Local Settings Defaults
			if (!_localSettings.Values.ContainsKey(enableNotifications))
			{
				_localSettings.Values.Add(enableNotifications, true);
			}
			if (!_localSettings.Values.ContainsKey(notificationUID))
			{
				_localSettings.Values.Add(notificationUID, Guid.NewGuid());
			}
			if (!_localSettings.Values.ContainsKey(refreshRate))
			{
				_localSettings.Values.Add(refreshRate, 5);
			}
			if (!_localSettings.Values.ContainsKey(orderIndex))
			{
				_localSettings.Values.Add(orderIndex, 2);
			}
			if (!_localSettings.Values.ContainsKey(filterIndex))
			{
				_localSettings.Values.Add(filterIndex, 0);
			}
			if (!_localSettings.Values.ContainsKey(newInfoAvailable))
			{
				_localSettings.Values.Add(newInfoAvailable, false);
			}
			if (!_localSettings.Values.ContainsKey(newInfoVersion))
			{
				_localSettings.Values.Add(newInfoVersion, _currentVersion);
			}
			if (!_localSettings.Values.ContainsKey(externalYoutubeApp))
			{
				_localSettings.Values.Add(externalYoutubeApp, Enum.GetName(typeof(ExternalYoutubeAppType), ExternalYoutubeAppType.InternalMediaPlayer));
			}
			if (!_localSettings.Values.ContainsKey(openUnknownLinksInEmbedded))
			{
				_localSettings.Values.Add(openUnknownLinksInEmbedded, true);
			}
			if (!_localSettings.Values.ContainsKey(pinnedSingleThreadInlineAppBar))
			{
				_localSettings.Values.Add(pinnedSingleThreadInlineAppBar, false);
			}
			if (!_localSettings.Values.ContainsKey(pinnedChattyAppBar))
			{
				_localSettings.Values.Add(pinnedChattyAppBar, false);
			}
			if (!_localSettings.Values.ContainsKey(disableSplitView))
			{
				_localSettings.Values.Add(disableSplitView, false);
			}
			if (!_localSettings.Values.ContainsKey(disableNewsSplitView))
			{
				_localSettings.Values.Add(disableNewsSplitView, false);
			}
			if (!_localSettings.Values.ContainsKey(fontSize))
			{
				_localSettings.Values.Add(fontSize, 15d);
			}
			if (!_localSettings.Values.ContainsKey(localFirstRun))
			{
				_localSettings.Values.Add(localFirstRun, true);
			}
			if (!_localSettings.Values.ContainsKey(embeddedYouTubeResolution))
			{
				_localSettings.Values.Add(embeddedYouTubeResolution, Enum.GetName(typeof(YouTubeQuality), YouTubeQuality.Quality480P));
			}
			if (!_localSettings.Values.ContainsKey(notifyOnNameMention))
			{
				_localSettings.Values.Add(notifyOnNameMention, true);
			}
			if (!_localSettings.Values.ContainsKey(pinMarkup))
			{
				_localSettings.Values.Add(pinMarkup, false);
			}
			#endregion

			IsUpdateInfoAvailable = !_localSettings.Values[newInfoVersion].ToString().Equals(_currentVersion, StringComparison.Ordinal);
			Theme = AvailableThemes.SingleOrDefault(t => t.Name.Equals(ThemeName)) ?? AvailableThemes.Single(t => t.Name.Equals("Default"));
			Application.Current.Resources["ControlContentFontSize"] = FontSize;
			Application.Current.Resources["ControlContentThemeFontSize"] = FontSize;
			Application.Current.Resources["ContentControlFontSize"] = FontSize;
			Application.Current.Resources["ToolTipContentThemeFontSize"] = FontSize;
		}

		#region Remote Settings
		public bool AutoCollapseNws
		{
			get
			{
				object v;
				_remoteSettings.Values.TryGetValue(autocollapsenws, out v);
				return v != null && (bool)v;
			}
			set
			{
				_remoteSettings.Values[autocollapsenws] = value;
				TrackSettingChanged(value.ToString());
				NotifyPropertyChange();
			}
		}

		public bool AutoCollapseNews
		{
			get
			{
				object v;
				_remoteSettings.Values.TryGetValue(autocollapsenews, out v);
				return v != null && (bool)v;
			}
			set
			{
				_remoteSettings.Values[autocollapsenews] = value;
				TrackSettingChanged(value.ToString());
				NotifyPropertyChange();
			}
		}

		public bool AutoCollapseStupid
		{
			get
			{
				object v;
				_remoteSettings.Values.TryGetValue(autocollapsestupid, out v);
				return v != null && (bool)v;
			}
			set
			{
				_remoteSettings.Values[autocollapsestupid] = value;
				TrackSettingChanged(value.ToString());
				NotifyPropertyChange();
			}
		}

		public bool AutoCollapseOffTopic
		{
			get
			{
				object v;
				_remoteSettings.Values.TryGetValue(autocollapseofftopic, out v);
				return v != null && (bool)v;
			}
			set
			{
				_remoteSettings.Values[autocollapseofftopic] = value;
				TrackSettingChanged(value.ToString());
				NotifyPropertyChange();
			}
		}

		public bool AutoCollapsePolitical
		{
			get
			{
				object v;
				_remoteSettings.Values.TryGetValue(autocollapsepolitical, out v);
				return v != null && (bool)v;
			}
			set
			{
				_remoteSettings.Values[autocollapsepolitical] = value;
				TrackSettingChanged(value.ToString());
				NotifyPropertyChange();
			}
		}

		public bool AutoCollapseInformative
		{
			get
			{
				object v;
				_remoteSettings.Values.TryGetValue(autocollapseinformative, out v);
				return v != null && (bool)v;
			}
			set
			{
				_remoteSettings.Values[autocollapseinformative] = value;
				TrackSettingChanged(value.ToString());
				NotifyPropertyChange();
			}
		}

		public bool AutoCollapseInteresting
		{
			get
			{
				object v;
				_remoteSettings.Values.TryGetValue(autocollapseinteresting, out v);
				return v != null && (bool)v;
			}
			set
			{
				_remoteSettings.Values[autocollapseinteresting] = value;
				TrackSettingChanged(value.ToString());
				NotifyPropertyChange();
			}
		}

		public bool AutoPinOnReply
		{
			get
			{
				object v;
				_remoteSettings.Values.TryGetValue(autopinonreply, out v);
				return v != null && (bool)v;
			}
			set
			{
				_remoteSettings.Values[autopinonreply] = value;
				TrackSettingChanged(value.ToString());
				NotifyPropertyChange();
			}
		}

		public bool AutoRemoveOnExpire
		{
			get
			{
				object v;
				_remoteSettings.Values.TryGetValue(autoremoveonexpire, out v);
				return v != null && (bool)v;
			}
			set
			{
				_remoteSettings.Values[autoremoveonexpire] = value;
				TrackSettingChanged(value.ToString());
				NotifyPropertyChange();
			}
		}

		public bool ShowRightChattyList
		{
			get
			{
				object v;
				_remoteSettings.Values.TryGetValue(rightList, out v);
				return v != null && (bool)v;
			}
			set
			{
				_remoteSettings.Values[rightList] = value;
				TrackSettingChanged(value.ToString());
				NotifyPropertyChange();
			}
		}

		public bool MarkReadOnSort
		{
			get
			{
				object v;
				_remoteSettings.Values.TryGetValue(markReadOnSort, out v);
				return v != null && (bool)v;
			}
			set
			{
				_remoteSettings.Values[markReadOnSort] = value;
				TrackSettingChanged(value.ToString());
				NotifyPropertyChange();
			}
		}

		public ChattySwipeOperation ChattyLeftSwipeAction
		{
			get
			{
				object v;
				_remoteSettings.Values.TryGetValue(chattySwipeLeftAction, out v);
				var returnOp = ChattySwipeOperations.SingleOrDefault(op => op.Type == (ChattySwipeOperationType)Enum.Parse(typeof(ChattySwipeOperationType), (string)v));
				if (returnOp == null)
				{
					returnOp = ChattySwipeOperations.Single(op => op.Type == ChattySwipeOperationType.Collapse);
				}
				return returnOp;
			}
			set
			{
				_remoteSettings.Values[chattySwipeLeftAction] = Enum.GetName(typeof(ChattySwipeOperationType), value.Type);
				TrackSettingChanged(value.Type.ToString());
				NotifyPropertyChange();
			}
		}

		public ChattySwipeOperation ChattyRightSwipeAction
		{
			get
			{
				object v;
				_remoteSettings.Values.TryGetValue(chattySwipeRightAction, out v);
				var returnOp = ChattySwipeOperations.SingleOrDefault(op => op.Type == (ChattySwipeOperationType)Enum.Parse(typeof(ChattySwipeOperationType), (string)v));
				if (returnOp == null)
				{
					returnOp = ChattySwipeOperations.Single(op => op.Type == ChattySwipeOperationType.Pin);
				}
				return returnOp;
			}
			set
			{
				_remoteSettings.Values[chattySwipeRightAction] = Enum.GetName(typeof(ChattySwipeOperationType), value.Type);
				TrackSettingChanged(value.Type.ToString());
				NotifyPropertyChange();
			}
		}

		public int LaunchCount
		{
			get
			{
				object v;
				_remoteSettings.Values.TryGetValue(launchCount, out v);
				Debug.Assert(v != null, nameof(v) + " != null");
				return (int)v;
			}
			set
			{
				_remoteSettings.Values[launchCount] = value;
				TrackSettingChanged(value.ToString());
				NotifyPropertyChange();
			}
		}

		public string ThemeName
		{
			get
			{
				object v;
				_remoteSettings.Values.TryGetValue(themeName, out v);
				return string.IsNullOrWhiteSpace((string)v) ? "Default" : (string)v;
			}
			set
			{
				_remoteSettings.Values[themeName] = value;
				Theme = AvailableThemes.SingleOrDefault(t => t.Name.Equals(value)) ?? AvailableThemes.Single(t => t.Name.Equals("Default"));
				TrackSettingChanged(value);
				NotifyPropertyChange();
			}
		}

		public bool SeenMercuryBlast
		{
			get
			{
				object v;
				_remoteSettings.Values.TryGetValue(seenMercuryBlast, out v);
				return v != null && (bool)v;
			}
			set
			{
				_remoteSettings.Values[seenMercuryBlast] = value;
				TrackSettingChanged(value.ToString());
				NotifyPropertyChange();
			}
		}
		#endregion

		#region Local Settings
		public Guid NotificationId
		{
			get
			{
				object v;
				_localSettings.Values.TryGetValue(notificationUID, out v);
				Debug.Assert(v != null, nameof(v) + " != null");
				return (Guid)v;
			}
			set
			{
				_localSettings.Values[notificationUID] = value;
				TrackSettingChanged(value.ToString());
			}
		}
		public bool PinMarkup
		{
			get
			{
				object v;
				_localSettings.Values.TryGetValue(pinMarkup, out v);
				return v != null && (bool)v;
			}
			set
			{
				_localSettings.Values[pinMarkup] = value;
				NotifyPropertyChange();
			}
		}

		public bool EnableNotifications
		{
			get
			{
				object v;
				_localSettings.Values.TryGetValue(enableNotifications, out v);
				return v != null && (bool)v;
			}
			set
			{
				_localSettings.Values[enableNotifications] = value;
				NotifyPropertyChange();
			}
		}
		public bool NotifyOnNameMention
		{
			get
			{
				object v;
				_localSettings.Values.TryGetValue(notifyOnNameMention, out v);
				return v != null && (bool)v;
			}
			set
			{
				_localSettings.Values[notifyOnNameMention] = value;
				NotifyPropertyChange();
			}
		}
		public bool DisableSplitView
		{
			get
			{
				object v;
				_localSettings.Values.TryGetValue(disableSplitView, out v);
				return v != null && (bool)v;
			}
			set
			{
				_localSettings.Values[disableSplitView] = value;
				TrackSettingChanged(value.ToString());
				NotifyPropertyChange();
			}
		}

		public bool DisableNewsSplitView
		{
			get
			{
				object v;
				_localSettings.Values.TryGetValue(disableNewsSplitView, out v);
				return v != null && (bool)v;
			}
			set
			{
				_localSettings.Values[disableNewsSplitView] = value;
				TrackSettingChanged(value.ToString());
				NotifyPropertyChange();
			}
		}

		public bool PinnedSingleThreadAppBar
		{
			get
			{
				object v;
				_localSettings.Values.TryGetValue(pinnedSingleThreadInlineAppBar, out v);
				Debug.Assert(v != null, nameof(v) + " != null");
				return (bool)v;
			}
			set
			{
				_localSettings.Values[pinnedSingleThreadInlineAppBar] = value;
				TrackSettingChanged(value.ToString());
				NotifyPropertyChange();
			}
		}

		public bool PinnedChattyAppBar
		{
			get
			{
				object v;
				_localSettings.Values.TryGetValue(pinnedChattyAppBar, out v);
				Debug.Assert(v != null, nameof(v) + " != null");
				return (bool)v;
			}
			set
			{
				_localSettings.Values[pinnedChattyAppBar] = value;
				TrackSettingChanged(value.ToString());
				NotifyPropertyChange();
			}
		}

		public bool OpenUnknownLinksInEmbeddedBrowser
		{
			get
			{
				object v;
				_localSettings.Values.TryGetValue(openUnknownLinksInEmbedded, out v);
				return v != null && (bool)v;
			}
			set
			{
				_localSettings.Values[openUnknownLinksInEmbedded] = value;
				TrackSettingChanged(value.ToString());
				NotifyPropertyChange();
			}
		}

		public ExternalYoutubeApp ExternalYoutubeApp
		{
			get
			{
				object v;
				_localSettings.Values.TryGetValue(externalYoutubeApp, out v);
				var app = ExternalYoutubeApps.SingleOrDefault(op => op.Type == (ExternalYoutubeAppType)Enum.Parse(typeof(ExternalYoutubeAppType), (string)v));
				if (app == null)
				{
					app = ExternalYoutubeApps.Single(op => op.Type == ExternalYoutubeAppType.Browser);
				}
				return app;
			}
			set
			{
				_localSettings.Values[externalYoutubeApp] = Enum.GetName(typeof(ExternalYoutubeAppType), value.Type);
				TrackSettingChanged(value.Type.ToString());
				NotifyPropertyChange();
			}
		}

		public YouTubeResolution EmbeddedYouTubeResolution
		{
			get
			{
				object v;
				_localSettings.Values.TryGetValue(embeddedYouTubeResolution, out v);
				var app = YouTubeResolutions.SingleOrDefault(op => op.Quality == (YouTubeQuality)Enum.Parse(typeof(YouTubeQuality), (string)v));
				if (app == null)
				{
					app = YouTubeResolutions.Single(op => op.Quality == YouTubeQuality.Quality480P);
				}
				return app;
			}
			set
			{
				_localSettings.Values[embeddedYouTubeResolution] = Enum.GetName(typeof(YouTubeQuality), value.Quality);
				TrackSettingChanged(value.Quality.ToString());
				NotifyPropertyChange();
			}
		}

		public int RefreshRate
		{
			get
			{
				object v;
				_localSettings.Values.TryGetValue(refreshRate, out v);
				Debug.Assert(v != null, nameof(v) + " != null");
				return (int)v;
			}
			set
			{
				_localSettings.Values[refreshRate] = value;
				TrackSettingChanged(value.ToString());
				NotifyPropertyChange();
			}
		}

		public int FilterIndex
		{
			get
			{
				object v;
				_localSettings.Values.TryGetValue(filterIndex, out v);
				Debug.Assert(v != null, nameof(v) + " != null");
				return (int)v;
			}
			set
			{
				_localSettings.Values[filterIndex] = value;
				//this.TrackSettingChanged(value.ToString());
				NotifyPropertyChange();
			}
		}

		public int OrderIndex
		{
			get
			{
				object v;
				_localSettings.Values.TryGetValue(orderIndex, out v);
				Debug.Assert(v != null, nameof(v) + " != null");
				return (int)v;
			}
			set
			{
				_localSettings.Values[orderIndex] = value;
				//this.TrackSettingChanged(value.ToString());
				NotifyPropertyChange();
			}
		}

		public bool IsUpdateInfoAvailable
		{
			get
			{
				object v;
				_localSettings.Values.TryGetValue(newInfoAvailable, out v);
				return v != null && (bool)v;
			}
			set
			{
				_localSettings.Values[newInfoAvailable] = value;
				NotifyPropertyChange();
			}
		}

		public double FontSize
		{
			get
			{
				object v;
				_localSettings.Values.TryGetValue(fontSize, out v);
				Debug.Assert(v != null, nameof(v) + " != null");
				return (double)v;
			}
			set
			{
				_localSettings.Values[fontSize] = value;
				TrackSettingChanged(value.ToString(CultureInfo.InvariantCulture));
				NotifyPropertyChange();
			}
		}

		public bool LocalFirstRun
		{
			get
			{
				object v;
				_localSettings.Values.TryGetValue(localFirstRun, out v);
				Debug.Assert(v != null, nameof(v) + " != null");
				return (bool)v;
			}
			set
			{
				_localSettings.Values[localFirstRun] = value;
				NotifyPropertyChange();
			}
		}
		#endregion

		public void MarkUpdateInfoRead()
		{
			_localSettings.Values[newInfoVersion] = _currentVersion;
			IsUpdateInfoAvailable = false;
		}

		private ThemeColorOption npcCurrentTheme;
		public ThemeColorOption Theme
		{
			get => npcCurrentTheme;
			private set
			{
				if (npcCurrentTheme?.Name != value.Name)
				{
					npcCurrentTheme = value;
					Application.Current.Resources["ThemeHighlight"] = new SolidColorBrush(value.AccentBackgroundColor);
					NotifyPropertyChange();
				}
			}
		}

		private List<ThemeColorOption> _availableThemes;
		public List<ThemeColorOption> AvailableThemes
		{
			get
			{
				if (_availableThemes == null)
				{
					_availableThemes = new List<ThemeColorOption>
					{
						new ThemeColorOption("Default", Color.FromArgb(255, 63, 110, 127), Colors.White),
						new ThemeColorOption(
							"System",
							(new UISettings()).GetColorValue(UIColorType.Accent),
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
						new ThemeColorOption("Black", Color.FromArgb(255, 0, 0, 0), Colors.White)

						//new ThemeColorOption("White", Colors.White, Color.FromArgb(255, 0, 0, 0), Color.FromArgb(255, 235, 235, 235), Color.FromArgb(255, 0, 0, 0))
					};
				}
				return _availableThemes;
			}
		}

		private List<ChattySwipeOperation> _chattySwipeOperations;
		public List<ChattySwipeOperation> ChattySwipeOperations
		{
			get
			{
				if (_chattySwipeOperations == null)
				{
					_chattySwipeOperations = new List<ChattySwipeOperation>
					{
						new ChattySwipeOperation(ChattySwipeOperationType.Collapse, "", "(Un)Collapse"),
						new ChattySwipeOperation(ChattySwipeOperationType.MarkRead, "", "Mark Thread Read"),
						new ChattySwipeOperation(ChattySwipeOperationType.Pin, "", "(Un)Pin")
					};
				}
				return _chattySwipeOperations;
			}
		}

		private List<ExternalYoutubeApp> _externalYoutubeApps;
		public List<ExternalYoutubeApp> ExternalYoutubeApps
		{
			get
			{
				if (_externalYoutubeApps == null)
				{
					_externalYoutubeApps = new List<ExternalYoutubeApp>
					{
						new ExternalYoutubeApp(ExternalYoutubeAppType.InternalMediaPlayer, "https://www.youtube.com/watch?v={0}", "Embedded Player"),
						new ExternalYoutubeApp(ExternalYoutubeAppType.Browser, "https://www.youtube.com/watch?v={0}", "Browser"),
						new ExternalYoutubeApp(ExternalYoutubeAppType.Hyper, "hyper://{0}", "Hyper"),
						new ExternalYoutubeApp(ExternalYoutubeAppType.Tubecast, "tubecast:VideoId={0}", "Tubecast"),
						new ExternalYoutubeApp(ExternalYoutubeAppType.Mytube, "mytube:link=https://www.youtube.com/watch?v={0}", "myTube!")
					};
				}
				return _externalYoutubeApps;
			}
		}

		private List<YouTubeResolution> _youTubeResolutions;
		public List<YouTubeResolution> YouTubeResolutions
		{
			get
			{
				if (_youTubeResolutions == null)
				{
					_youTubeResolutions = new List<YouTubeResolution>
					{
						new YouTubeResolution(YouTubeQuality.Quality480P, "Low (480P)"),
						new YouTubeResolution(YouTubeQuality.Quality720P, "Medium (720P)"),
						new YouTubeResolution(YouTubeQuality.Quality1080P, "High (1080P)"),
						new YouTubeResolution(YouTubeQuality.Quality2160P, "Ultra (4K)")
					};
				}
				return _youTubeResolutions;
			}
		}

		//private Int32 ColorToInt32(Color color)
		//{
		//	return (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;
		//}

		//private Color Int32ToColor(Int32 intColor)
		//{
		//	return Color.FromArgb((byte)(intColor >> 24), (byte)(intColor >> 16), (byte)(intColor >> 8), (byte)intColor);
		//}
		
		// ReSharper disable UnusedParameter.Local
		private void TrackSettingChanged(string settingValue, [CallerMemberName] string propertyName = "")
		// ReSharper restore UnusedParameter.Local
		{
			//Microsoft.HockeyApp.HockeyClient.Current.TrackEvent($"Setting-{propertyName}-Updated", new Dictionary<string, string> { { "settingName", propertyName }, { "settingValue", settingValue } });
		}

		public event PropertyChangedEventHandler PropertyChanged;
		//private JObject cloudSettings;

		protected bool NotifyPropertyChange([CallerMemberName] String propertyName = null)
		{
			OnPropertyChanged(propertyName);
			return true;
		}

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			var eventHandler = PropertyChanged;
			if (eventHandler != null)
			{
				eventHandler(this, new PropertyChangedEventArgs(propertyName));
			}
		}



		public bool ShouldAutoCollapseCommentThread(CommentThread thread)
		{
			var threadCategory = thread.Comments[0].Category;
			return (AutoCollapseInformative && thread.Comments[0].Category == PostCategory.informative)
					|| (AutoCollapseInteresting && threadCategory == PostCategory.interesting)
					|| (AutoCollapseNews && threadCategory == PostCategory.newsarticle)
					|| (AutoCollapseNws && threadCategory == PostCategory.nws)
					|| (AutoCollapseOffTopic && threadCategory == PostCategory.offtopic)
					|| (AutoCollapsePolitical && threadCategory == PostCategory.political)
					|| (AutoCollapseStupid && threadCategory == PostCategory.stupid);
		}
	}

	internal static class JsonExtensions
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
