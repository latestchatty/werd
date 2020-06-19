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
//using MyToolkit.Multimedia;
using Newtonsoft.Json.Linq;
using Windows.UI.Xaml.Controls;
using Common;
using System.Threading.Tasks;

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
		private static readonly string refreshRate = "refreshrate";
		private static readonly string themeName = "themename";
		private static readonly string markReadOnSort = "markreadonsort";
		private static readonly string orderIndex = "orderindex";
		private static readonly string filterIndex = "filterindex";
		private static readonly string launchCount = "launchcount";
		private static readonly string newInfoVersion = "newInfoVersion";
		private static readonly string newInfoAvailable = "newInfoAvailable";
		private static readonly string chattySwipeLeftAction = "chattySwipeLeftAction";
		private static readonly string chattySwipeRightAction = "chattySwipeRightAction";
		private static readonly string openUnknownLinksInEmbedded = "openUnknownLinksInEmbeddedBrowser";
		private static readonly string pinnedSingleThreadInlineAppBar = "pinnedSingleThreadInlineAppBar";
		private static readonly string pinnedChattyAppBar = "pinnedChattyAppBar";
		private static readonly string seenMercuryBlast = "seenMercuryBlast";
		private static readonly string disableNewsSplitView = "disableNewsSplitView";
		private static readonly string fontSize = "fontSize";
		private static readonly string localFirstRun = "localFirstRun";
		private static readonly string notifyOnNameMention = "notifyOnNameMention";
		private static readonly string pinMarkup = "pinMarkup";
		private static readonly string composePreviewShown = "composePreviewShown";
		private static readonly string allowNotificationsWhileActive = "allowNotificationsWhileActive";
		private static readonly string customLaunchers = "customLaunchers";
		private static readonly string loadImagesInline = "loadImagesInline";
		private static readonly string showPinnedThreadsAtChattyTop = "showPinnedThreadsAtChattyTop";
		private static readonly string previewLineCount = "previewLineCount";
		private static readonly string useMasterDetail = "useMasterDetail";
		private static readonly string truncateLimit = "truncateLimit";
		private static readonly string enableDevTools = "enableDevTools";

		private readonly ApplicationDataContainer _remoteSettings;
		private readonly ApplicationDataContainer _localSettings;
		private readonly string _currentVersion;
		private double _lineHeight;

		private CloudSettingsManager _cloudSettingsManager;

		public LatestChattySettings()
		{
			var assemblyName = new AssemblyName(typeof(App).GetTypeInfo().Assembly.FullName);
			_currentVersion = assemblyName.Version.ToString();

			_remoteSettings = ApplicationData.Current.RoamingSettings;
			_localSettings = ApplicationData.Current.LocalSettings;
			Debug.WriteLine("Max roaming storage is {0} KB.", ApplicationData.Current.RoamingStorageQuota);

			#region Remote Settings Defaults
			if (!_remoteSettings.Values.ContainsKey(autocollapsenws))
				_remoteSettings.Values.Add(autocollapsenws, true);
			if (!_remoteSettings.Values.ContainsKey(autocollapsestupid))
				_remoteSettings.Values.Add(autocollapsestupid, false);
			if (!_remoteSettings.Values.ContainsKey(autocollapseofftopic))
				_remoteSettings.Values.Add(autocollapseofftopic, false);
			if (!_remoteSettings.Values.ContainsKey(autocollapsepolitical))
				_remoteSettings.Values.Add(autocollapsepolitical, false);
			if (!_remoteSettings.Values.ContainsKey(autocollapseinformative))
				_remoteSettings.Values.Add(autocollapseinformative, false);
			if (!_remoteSettings.Values.ContainsKey(autocollapseinteresting))
				_remoteSettings.Values.Add(autocollapseinteresting, false);
			if (!_remoteSettings.Values.ContainsKey(autocollapsenews))
				_remoteSettings.Values.Add(autocollapsenews, false);
			if (!_remoteSettings.Values.ContainsKey(themeName))
				_remoteSettings.Values.Add(themeName, "System");
			if (!_remoteSettings.Values.ContainsKey(markReadOnSort))
				_remoteSettings.Values.Add(markReadOnSort, false);
			if (!_remoteSettings.Values.ContainsKey(launchCount))
				_remoteSettings.Values.Add(launchCount, 0);
			if (!_remoteSettings.Values.ContainsKey(chattySwipeLeftAction))
				_remoteSettings.Values.Add(chattySwipeLeftAction, Enum.GetName(typeof(ChattySwipeOperationType), ChattySwipeOperationType.Collapse));
			if (!_remoteSettings.Values.ContainsKey(chattySwipeRightAction))
				_remoteSettings.Values.Add(chattySwipeRightAction, Enum.GetName(typeof(ChattySwipeOperationType), ChattySwipeOperationType.Pin));
			if (!_remoteSettings.Values.ContainsKey(seenMercuryBlast))
				_remoteSettings.Values.Add(seenMercuryBlast, false);
			if (!_remoteSettings.Values.ContainsKey(showPinnedThreadsAtChattyTop))
				_remoteSettings.Values.Add(showPinnedThreadsAtChattyTop, true);

			//This is a really lazy way to do this but I don't want to refactor into a dictionary with enums and default values, etc. Way too much work.
			var activeRemoteKeys = new List<string>
			{
				autocollapsenws,
				autocollapsestupid,
				autocollapseofftopic,
				autocollapsepolitical,
				autocollapseinformative,
				autocollapseinteresting,
				autocollapsenews,
				themeName,
				markReadOnSort,
				launchCount,
				chattySwipeLeftAction,
				chattySwipeRightAction,
				seenMercuryBlast,
				showPinnedThreadsAtChattyTop
			};

			//Remove any roaming settings that aren't actively being used to make sure we keep storage usage as low as possible.
			var currentRemoteKeys = _remoteSettings.Values.Keys.ToList();
			foreach (var key in currentRemoteKeys.Except(activeRemoteKeys))
			{
				_remoteSettings.Values.Remove(key);
			}

			#endregion
			
			#region Local Settings Defaults
			if (!_localSettings.Values.ContainsKey(enableNotifications))
				_localSettings.Values.Add(enableNotifications, true);
			if (!_localSettings.Values.ContainsKey(notificationUID))
				_localSettings.Values.Add(notificationUID, Guid.NewGuid());
			if (!_localSettings.Values.ContainsKey(refreshRate))
				_localSettings.Values.Add(refreshRate, 5);
			if (!_localSettings.Values.ContainsKey(orderIndex))
				_localSettings.Values.Add(orderIndex, 2);
			if (!_localSettings.Values.ContainsKey(filterIndex))
				_localSettings.Values.Add(filterIndex, 0);
			if (!_localSettings.Values.ContainsKey(newInfoAvailable))
				_localSettings.Values.Add(newInfoAvailable, false);
			if (!_localSettings.Values.ContainsKey(newInfoVersion))
				_localSettings.Values.Add(newInfoVersion, _currentVersion);
			if (!_localSettings.Values.ContainsKey(openUnknownLinksInEmbedded))
				_localSettings.Values.Add(openUnknownLinksInEmbedded, true);
			if (!_localSettings.Values.ContainsKey(pinnedSingleThreadInlineAppBar))
				_localSettings.Values.Add(pinnedSingleThreadInlineAppBar, false);
			if (!_localSettings.Values.ContainsKey(pinnedChattyAppBar))
				_localSettings.Values.Add(pinnedChattyAppBar, false);
			if (!_localSettings.Values.ContainsKey(disableNewsSplitView))
				_localSettings.Values.Add(disableNewsSplitView, false);
			if (!_localSettings.Values.ContainsKey(fontSize))
				_localSettings.Values.Add(fontSize, 15d);
			if (!_localSettings.Values.ContainsKey(localFirstRun))
				_localSettings.Values.Add(localFirstRun, true);
			if (!_localSettings.Values.ContainsKey(notifyOnNameMention))
				_localSettings.Values.Add(notifyOnNameMention, true);
			if (!_localSettings.Values.ContainsKey(pinMarkup))
				_localSettings.Values.Add(pinMarkup, false);
			if (!_localSettings.Values.ContainsKey(composePreviewShown))
				_localSettings.Values.Add(composePreviewShown, true);
			if (!_localSettings.Values.ContainsKey(allowNotificationsWhileActive))
				_localSettings.Values.Add(allowNotificationsWhileActive, false);
			if (!_localSettings.Values.ContainsKey(customLaunchers))
				_localSettings.Values.Add(customLaunchers, Newtonsoft.Json.JsonConvert.SerializeObject(_defaultCustomLaunchers));
			if (!_localSettings.Values.ContainsKey(loadImagesInline))
				_localSettings.Values.Add(loadImagesInline, true);
			if (!_localSettings.Values.ContainsKey(previewLineCount))
				_localSettings.Values.Add(previewLineCount, 3);
			if (!_localSettings.Values.ContainsKey(useMasterDetail))
				_localSettings.Values.Add(useMasterDetail, true);
			if (!_localSettings.Values.ContainsKey(truncateLimit))
				_localSettings.Values.Add(truncateLimit, 5);
			if (!_localSettings.Values.ContainsKey(enableDevTools))
				_localSettings.Values.Add(enableDevTools, false);
			#endregion

			IsUpdateInfoAvailable = !_localSettings.Values[newInfoVersion].ToString().Equals(_currentVersion, StringComparison.Ordinal);
			Theme = AvailableThemes.SingleOrDefault(t => t.Name.Equals(ThemeName)) ?? AvailableThemes.Single(t => t.Name.Equals("System"));
			Application.Current.Resources["ControlContentFontSize"] = FontSize;
			Application.Current.Resources["ControlContentThemeFontSize"] = FontSize;
			Application.Current.Resources["ContentControlFontSize"] = FontSize;
			Application.Current.Resources["ToolTipContentThemeFontSize"] = FontSize;
			var tb = new TextBlock { Text = "Xg", FontSize = FontSize };
			tb.Measure(new Windows.Foundation.Size(Double.PositiveInfinity, Double.PositiveInfinity));
			_lineHeight = tb.DesiredSize.Height;
			PreviewItemHeight = _lineHeight * PreviewLineCount;
		}

		public void SetCloudManager(CloudSettingsManager manager)
		{
			_cloudSettingsManager = manager;
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
				return string.IsNullOrWhiteSpace((string)v) ? "System" : (string)v;
			}
			set
			{
				_remoteSettings.Values[themeName] = value;
				Theme = AvailableThemes.SingleOrDefault(t => t.Name.Equals(value)) ?? AvailableThemes.Single(t => t.Name.Equals("System"));
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

		public bool ShowPinnedThreadsAtChattyTop
		{
			get
			{
				_remoteSettings.Values.TryGetValue(showPinnedThreadsAtChattyTop, out var v);
				return v != null && (bool)v;
			}
			set
			{
				_remoteSettings.Values[showPinnedThreadsAtChattyTop] = value;
				TrackSettingChanged(value.ToString());
				NotifyPropertyChange();
			}
		}

		public async Task<Dictionary<string, string>>GetTemplatePosts()
		{
			return await _cloudSettingsManager?.GetCloudSetting<Dictionary<string, string>>("templatePosts");
		}

		public async Task SetTemplatePosts(Dictionary<string, string> value)
		{
			await _cloudSettingsManager?.SetCloudSettings("templatePosts", value);
		}

		#endregion

		#region Local Settings
		private List<CustomLauncher> _defaultCustomLaunchers = new List<CustomLauncher>
		{
			new CustomLauncher
			{
				EmbeddedBrowser = true,
				MatchString = @"(?<link>https?\:\/\/(www\.|m\.)?(youtube\.com|youtu\.be)\/(vi?\/|watch\?vi?=|\?vi?=)?(?<id>[^&\?<]+)([^<]*))",
				Replace = @"https://invidio.us/watch?v=${id}",
				Name = "Invidious",
				Enabled = false

			},
			new CustomLauncher
			{
				EmbeddedBrowser = true,
				MatchString = @"https://twitter.com/(.*)",
				Replace = @"https://nitter.net/$1",
				Name = "Nitter",
				Enabled = false
			},
			new CustomLauncher
			{
				EmbeddedBrowser = true,
				MatchString = @"https://(www.)?instagram.com/(.*)",
				Replace = @"https://bibliogram.art/$2",
				Name = "Bibliogram",
				Enabled = false
			},
			new CustomLauncher
			{
				EmbeddedBrowser = false,
				MatchString = @"(?<link>https?\:\/\/(www\.|m\.)?(youtube\.com|youtu\.be)\/(vi?\/|watch\?vi?=|\?vi?=)?(?<id>[^&\?<]+)([^<]*))",
				Replace = @"mytube:link=${link}",
				Name = "myTube! (App)",
				Enabled = false
			},
			new CustomLauncher
			{
				EmbeddedBrowser = true,
				MatchString = @"https?://(www.)shackpics.com/(files/|viewer\.x\?file\=)(?<id>.*)",
				Replace = @"https://www.chattypics.com/files/${id}",
				Name = "Shackpics to Chattypics",
				Enabled = true
			}
		};

		public void ResetCustomLaunchers()
		{
			CustomLaunchers = _defaultCustomLaunchers;
		}

		public List<CustomLauncher> CustomLaunchers
		{
			get
			{
				try
				{
					_localSettings.Values.TryGetValue(customLaunchers, out object v);
					return Newtonsoft.Json.JsonConvert.DeserializeObject<List<CustomLauncher>>((string)v);
				}
				catch
				{
					return _defaultCustomLaunchers;
				}
			}
			set
			{
				_localSettings.Values[customLaunchers] = Newtonsoft.Json.JsonConvert.SerializeObject(value);
				NotifyPropertyChange();
			}
		}
		public bool LoadImagesInline
		{
			get
			{
				object v;
				_localSettings.Values.TryGetValue(loadImagesInline, out v);
				return (bool)v;
			}
			set
			{
				_localSettings.Values[loadImagesInline] = value;
				NotifyPropertyChange();
				TrackSettingChanged(value.ToString());
			}
		}
		public bool EnableDevTools
		{
			get
			{
				object v;
				_localSettings.Values.TryGetValue(enableDevTools, out v);
				return (bool)v;
			}
			set
			{
				_localSettings.Values[enableDevTools] = value;
				NotifyPropertyChange();
				TrackSettingChanged(value.ToString());
			}
		}

		public bool UseMasterDetail
		{
			get
			{
				object v;
				_localSettings.Values.TryGetValue(useMasterDetail, out v);
				return (bool)v;
			}
			set
			{
				_localSettings.Values[useMasterDetail] = value;
				NotifyPropertyChange();
				TrackSettingChanged(value.ToString());
			}
		}

		public bool AllowNotificationsWhileActive
		{
			get
			{
				object v;
				_localSettings.Values.TryGetValue(allowNotificationsWhileActive, out v);
				return (bool)v;
			}
			set
			{
				_localSettings.Values[allowNotificationsWhileActive] = value;
				NotifyPropertyChange();
				TrackSettingChanged(value.ToString());
			}
		}
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

		private List<string> npcNotificationKeywords;
		public List<string> NotificationKeywords
		{
			get => npcNotificationKeywords;
			set
			{
				npcNotificationKeywords = value;
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

		public int TruncateLimit
		{
			get
			{
				_localSettings.Values.TryGetValue(truncateLimit, out object v);
				return (int)v;
			}
			set
			{
				_localSettings.Values[truncateLimit] = value;
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

		public bool ComposePreviewShown
		{
			get
			{
				_localSettings.Values.TryGetValue(composePreviewShown, out var v);
				return (bool)v;
			}
			set
			{
				_localSettings.Values[composePreviewShown] = value;
				NotifyPropertyChange();
			}
		}

		public int PreviewLineCount
		{
			get
			{
				_localSettings.Values.TryGetValue(previewLineCount, out var v);
				return (int)v;
			}
			set
			{
				_localSettings.Values[previewLineCount] = value;
				NotifyPropertyChange();
				PreviewItemHeight = value * _lineHeight;
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

					Application.Current.Resources["SystemAccentColor"] = value.AccentBackgroundColor;

					Application.Current.Resources["SystemListAccentHighColor"] = value.AccentHighColor;
					Application.Current.Resources["SystemListHighColor"] = value.AccentHighColor;
					Application.Current.Resources["SystemListAccentMediumColor"] = value.AccentMediumColor;
					Application.Current.Resources["SystemListMediumColor"] = value.AccentMediumColor;
					Application.Current.Resources["SystemListAccentLowColor"] = value.AccentLowColor;
					Application.Current.Resources["SystemListLowColor"] = value.AccentLowColor;

					Application.Current.Resources["SystemControlHighlightListAccentHighBrush"] = new SolidColorBrush(value.AccentHighColor);
					Application.Current.Resources["SystemControlHighlightListAccentMediumBrush"] = new SolidColorBrush(value.AccentMediumColor);
					Application.Current.Resources["SystemControlHighlightListAccentLowBrush"] = new SolidColorBrush(value.AccentLowColor);

					Application.Current.Resources["ApplicationPageBackgroundThemeBrush"] = new SolidColorBrush(value.AppBackgroundColor);
					Application.Current.Resources["SelectedPostBackgroundColor"] = new SolidColorBrush(value.SelectedPostBackgroundColor);
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
					var defaultBackgroundColor = Color.FromArgb(255, 31, 31, 31);
					var lighterSelectedPostColor = Color.FromArgb(255, 51, 51, 51);
					var darkSelectedPostColor = Color.FromArgb(255, 20, 20, 20);
					_availableThemes = new List<ThemeColorOption>
					{
						new ThemeColorOption("Default", Color.FromArgb(255, 63, 110, 127), Colors.White, defaultBackgroundColor, lighterSelectedPostColor),
						new ThemeColorOption(
							"System",
							(new UISettings()).GetColorValue(UIColorType.Accent),
							Colors.White,
							defaultBackgroundColor,
							lighterSelectedPostColor
						),
						new ThemeColorOption("Lime", Color.FromArgb(255, 164, 196, 0), Colors.White, defaultBackgroundColor, lighterSelectedPostColor),
						new ThemeColorOption("Green", Color.FromArgb(255, 96, 169, 23), Colors.White, defaultBackgroundColor, lighterSelectedPostColor),
						new ThemeColorOption("Emerald", Color.FromArgb(255, 0, 138, 0), Colors.White, defaultBackgroundColor, lighterSelectedPostColor),
						new ThemeColorOption("Teal", Color.FromArgb(255, 0, 171, 169), Colors.White, defaultBackgroundColor, lighterSelectedPostColor),
						new ThemeColorOption("Cyan", Color.FromArgb(255, 27, 161, 226), Colors.White, defaultBackgroundColor, lighterSelectedPostColor),
						new ThemeColorOption("Cobalt", Color.FromArgb(255, 0, 80, 239), Colors.White, defaultBackgroundColor, lighterSelectedPostColor),
						new ThemeColorOption("Indigo", Color.FromArgb(255, 106, 0, 255), Colors.White, defaultBackgroundColor, lighterSelectedPostColor),
						new ThemeColorOption("Violet", Color.FromArgb(255, 170, 0, 255), Colors.White, defaultBackgroundColor, lighterSelectedPostColor),
						new ThemeColorOption("Pink", Color.FromArgb(255, 244, 114, 208), Colors.White, defaultBackgroundColor, lighterSelectedPostColor),
						new ThemeColorOption("Magenta", Color.FromArgb(255, 216, 0, 115), Colors.White, defaultBackgroundColor, lighterSelectedPostColor),
						new ThemeColorOption("Crimson", Color.FromArgb(255, 162, 0, 37), Colors.White, defaultBackgroundColor, lighterSelectedPostColor),
						new ThemeColorOption("Red", Color.FromArgb(255, 255, 35, 10), Colors.White, defaultBackgroundColor, lighterSelectedPostColor),
						new ThemeColorOption("Orange", Color.FromArgb(255, 250, 104, 0), Colors.White, defaultBackgroundColor, lighterSelectedPostColor),
						new ThemeColorOption("Amber", Color.FromArgb(255, 240, 163, 10), Colors.White, defaultBackgroundColor, lighterSelectedPostColor),
						new ThemeColorOption("Yellow", Color.FromArgb(255, 227, 200, 0), Colors.White, defaultBackgroundColor, lighterSelectedPostColor),
						new ThemeColorOption("Brown", Color.FromArgb(255, 130, 90, 44), Colors.White, defaultBackgroundColor, lighterSelectedPostColor),
						new ThemeColorOption("Olive", Color.FromArgb(255, 109, 135, 100), Colors.White, defaultBackgroundColor, lighterSelectedPostColor),
						new ThemeColorOption("Steel", Color.FromArgb(255, 100, 118, 135), Colors.White, defaultBackgroundColor, lighterSelectedPostColor),
						new ThemeColorOption("Mauve", Color.FromArgb(255, 118, 96, 138), Colors.White, defaultBackgroundColor, lighterSelectedPostColor),
						new ThemeColorOption("Taupe", Color.FromArgb(255, 135, 121, 78), Colors.White, defaultBackgroundColor, lighterSelectedPostColor),
						new ThemeColorOption("Black", Color.FromArgb(255, 0, 0, 0), Colors.White, defaultBackgroundColor, lighterSelectedPostColor),
						new ThemeColorOption("Default (Black Background)", Color.FromArgb(255, 63, 110, 127), Colors.White, Colors.Black, darkSelectedPostColor),
						new ThemeColorOption(
							"System (Black Background)",
							(new UISettings()).GetColorValue(UIColorType.Accent),
							Colors.White,
							Colors.Black,
							darkSelectedPostColor
						),
						new ThemeColorOption("Lime (Black Background)", Color.FromArgb(255, 164, 196, 0), Colors.White, Colors.Black, darkSelectedPostColor),
						new ThemeColorOption("Green (Black Background)", Color.FromArgb(255, 96, 169, 23), Colors.White, Colors.Black, darkSelectedPostColor),
						new ThemeColorOption("Emerald (Black Background)", Color.FromArgb(255, 0, 138, 0), Colors.White, Colors.Black, darkSelectedPostColor),
						new ThemeColorOption("Teal (Black Background)", Color.FromArgb(255, 0, 171, 169), Colors.White, Colors.Black, darkSelectedPostColor),
						new ThemeColorOption("Cyan (Black Background)", Color.FromArgb(255, 27, 161, 226), Colors.White, Colors.Black, darkSelectedPostColor),
						new ThemeColorOption("Cobalt (Black Background)", Color.FromArgb(255, 0, 80, 239), Colors.White, Colors.Black, darkSelectedPostColor),
						new ThemeColorOption("Indigo (Black Background)", Color.FromArgb(255, 106, 0, 255), Colors.White, Colors.Black, darkSelectedPostColor),
						new ThemeColorOption("Violet (Black Background)", Color.FromArgb(255, 170, 0, 255), Colors.White, Colors.Black, darkSelectedPostColor),
						new ThemeColorOption("Pink (Black Background)", Color.FromArgb(255, 244, 114, 208), Colors.White, Colors.Black, darkSelectedPostColor),
						new ThemeColorOption("Magenta (Black Background)", Color.FromArgb(255, 216, 0, 115), Colors.White, Colors.Black, darkSelectedPostColor),
						new ThemeColorOption("Crimson (Black Background)", Color.FromArgb(255, 162, 0, 37), Colors.White, Colors.Black, darkSelectedPostColor),
						new ThemeColorOption("Red (Black Background)", Color.FromArgb(255, 255, 35, 10), Colors.White, Colors.Black, darkSelectedPostColor),
						new ThemeColorOption("Orange (Black Background)", Color.FromArgb(255, 250, 104, 0), Colors.White, Colors.Black, darkSelectedPostColor),
						new ThemeColorOption("Amber (Black Background)", Color.FromArgb(255, 240, 163, 10), Colors.White, Colors.Black, darkSelectedPostColor),
						new ThemeColorOption("Yellow (Black Background)", Color.FromArgb(255, 227, 200, 0), Colors.White, Colors.Black, darkSelectedPostColor),
						new ThemeColorOption("Brown (Black Background)", Color.FromArgb(255, 130, 90, 44), Colors.White, Colors.Black, darkSelectedPostColor),
						new ThemeColorOption("Olive (Black Background)", Color.FromArgb(255, 109, 135, 100), Colors.White, Colors.Black, darkSelectedPostColor),
						new ThemeColorOption("Steel (Black Background)", Color.FromArgb(255, 100, 118, 135), Colors.White, Colors.Black, darkSelectedPostColor),
						new ThemeColorOption("Mauve (Black Background)", Color.FromArgb(255, 118, 96, 138), Colors.White, Colors.Black, darkSelectedPostColor),
						new ThemeColorOption("Taupe (Black Background)", Color.FromArgb(255, 135, 121, 78), Colors.White, Colors.Black, darkSelectedPostColor),
						new ThemeColorOption("Gray (Black Background)", Color.FromArgb(255, 60, 60, 60), Colors.White, Colors.Black, darkSelectedPostColor)

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

		private double npcPreviewItemHeight;
		public double PreviewItemHeight
		{
			get => npcPreviewItemHeight;
			set
			{
				npcPreviewItemHeight = value;
				NotifyPropertyChange();
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
