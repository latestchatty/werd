using Common;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Werd.DataModel;
using Windows.Storage;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Werd.Settings
{
	public class AppSettings : INotifyPropertyChanged
	{
		private const string enableNotifications = "enableNotifications";
		private const string notificationUID = "notificationid";
		private const string autocollapsenws = "autocollapsenws";
		private const string autocollapsestupid = "autocollapsestupid";
		private const string autocollapseofftopic = "autocollapseofftopic";
		private const string autocollapsepolitical = "autocollapsepolitical";
		private const string autocollapseinformative = "autocollapseinformative";
		private const string autocollapseinteresting = "autocollapseinteresting";
		private const string autocollapsenews = "autocollapsenews";
		private const string themeName = "themename";
		private const string markReadOnSort = "markreadonsort";
		private const string orderIndex = "orderindex";
		private const string filterIndex = "filterindex";
		private const string launchCount = "launchcount";
		private const string newInfoVersion = "newInfoVersion";
		private const string newInfoAvailable = "newInfoAvailable";
		private const string openUnknownLinksInEmbedded = "openUnknownLinksInEmbeddedBrowser";
		private const string pinnedSingleThreadInlineAppBar = "pinnedSingleThreadInlineAppBar";
		private const string pinnedChattyAppBar = "pinnedChattyAppBar";
		private const string seenMercuryBlast = "seenMercuryBlast";
		private const string disableCortexAndNewsSplitView = "disableCortexAndNewsSplitView";
		private const string fontSize = "fontSize";
		private const string localFirstRun = "localFirstRun";
		private const string notifyOnNameMention = "notifyOnNameMention";
		private const string pinMarkup = "pinMarkup";
		private const string composePreviewShown = "composePreviewShown";
		private const string allowNotificationsWhileActive = "allowNotificationsWhileActive";
		private const string customLaunchers = "customLaunchers";
		private const string loadImagesInline = "loadImagesInline";
		private const string showPinnedThreadsAtChattyTop = "showPinnedThreadsAtChattyTop";
		private const string previewLineCount = "previewLineCount";
		private const string useMainDetail = "useMainDetail";
		private const string useSmoothScrolling = "useSmoothScrolling";
		private const string truncateLimit = "truncateLimit";
		private const string enableDevTools = "enableDevTools";
		private const string enableUserFilter = "enableUserFilter";
		private const string enableKeywordFilter = "enableKeywordFilter";
		private const string lastClipboardPostId = "lastClipboardPostId";
		private const string useCompactLayout = "useCompactLayout";
		private const string enableModTools = "enableModTools";
		private const string largeReply = "largeReply";
		private const string debugLogMessageBufferSize = "debugLogMessageBufferSize";
		private const string lockOutPosting = "lockOutPosting";
		private const string splitViewSplitterPosition = nameof(splitViewSplitterPosition);
		private const string articleSplitViewSplitterPosition = nameof(articleSplitViewSplitterPosition);
		private const string userNotes = nameof(userNotes);

		private readonly ApplicationDataContainer _remoteSettings;
		private readonly ApplicationDataContainer _localSettings;
		private readonly string _currentVersion;
		private double _lineHeight;

		private CloudSettingsManager _cloudSettingsManager;

		public AppSettings()
		{
			var assemblyName = new AssemblyName(typeof(App).GetTypeInfo().Assembly.FullName);
			_currentVersion = assemblyName.Version.ToString();

			_remoteSettings = ApplicationData.Current.RoamingSettings;
			_localSettings = ApplicationData.Current.LocalSettings;

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
			if (!_remoteSettings.Values.ContainsKey(seenMercuryBlast))
				_remoteSettings.Values.Add(seenMercuryBlast, false);
			if (!_remoteSettings.Values.ContainsKey(showPinnedThreadsAtChattyTop))
				_remoteSettings.Values.Add(showPinnedThreadsAtChattyTop, true);
			if (!_remoteSettings.Values.ContainsKey(lockOutPosting))
				_remoteSettings.Values.Add(lockOutPosting, false);

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
			if (!_localSettings.Values.ContainsKey(orderIndex))
				_localSettings.Values.Add(orderIndex, 0);
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
			if (!_localSettings.Values.ContainsKey(disableCortexAndNewsSplitView))
				_localSettings.Values.Add(disableCortexAndNewsSplitView, false);
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
			if (!_localSettings.Values.ContainsKey(useMainDetail))
				_localSettings.Values.Add(useMainDetail, true);
			if (!_localSettings.Values.ContainsKey(useSmoothScrolling))
				_localSettings.Values.Add(useSmoothScrolling, true);
			if (!_localSettings.Values.ContainsKey(truncateLimit))
				_localSettings.Values.Add(truncateLimit, 5);
			if (!_localSettings.Values.ContainsKey(enableDevTools))
				_localSettings.Values.Add(enableDevTools, false);
			if (!_localSettings.Values.ContainsKey(enableKeywordFilter))
				_localSettings.Values.Add(enableKeywordFilter, true);
			if (!_localSettings.Values.ContainsKey(enableUserFilter))
				_localSettings.Values.Add(enableUserFilter, true);
			if (!_localSettings.Values.ContainsKey(lastClipboardPostId))
				_localSettings.Values.Add(lastClipboardPostId, -1L);
			if (!_localSettings.Values.ContainsKey(useCompactLayout))
				_localSettings.Values.Add(useCompactLayout, false);
			if (!_localSettings.Values.ContainsKey(enableModTools))
				_localSettings.Values.Add(enableModTools, false);
			if (!_localSettings.Values.ContainsKey(largeReply))
				_localSettings.Values.Add(largeReply, false);
			if (!_localSettings.Values.ContainsKey(debugLogMessageBufferSize))
				_localSettings.Values.Add(debugLogMessageBufferSize, 500);
			if (!_localSettings.Values.ContainsKey(splitViewSplitterPosition))
				_localSettings.Values.Add(splitViewSplitterPosition, Window.Current.Bounds.Width * .28);
			if (!_localSettings.Values.ContainsKey(articleSplitViewSplitterPosition))
				_localSettings.Values.Add(articleSplitViewSplitterPosition, Window.Current.Bounds.Height * .7);
			if (!_localSettings.Values.ContainsKey(userNotes))
				_localSettings.Values.Add(userNotes, Newtonsoft.Json.JsonConvert.SerializeObject(new Dictionary<string, string>()));
			#endregion

			DebugLog.DebugLogMessageBufferSize = DebugLogMessageBufferSize;
			IsUpdateInfoAvailable = !_localSettings.Values[newInfoVersion].ToString().Equals(_currentVersion, StringComparison.Ordinal);
			Theme = AvailableThemes.SingleOrDefault(t => t.Name.Equals(ThemeName, StringComparison.Ordinal)) ?? AvailableThemes.Single(t => t.Name.Equals("System", StringComparison.Ordinal));

			UpdateLayoutCompactness(UseCompactLayout);
		}

		public void SetCloudManager(CloudSettingsManager manager)
		{
			_cloudSettingsManager = manager;
		}

		#region Remote Settings
		public bool LockOutPosting
		{
			get
			{
				_remoteSettings.Values.TryGetValue(lockOutPosting, out object v);
				return v != null && (bool)v;
			}
			set
			{
				_remoteSettings.Values[lockOutPosting] = value;
				TrackSettingChanged(value.ToString());
				NotifyPropertyChange();
			}
		}

		public bool AutoCollapseNws
		{
			get
			{
				_remoteSettings.Values.TryGetValue(autocollapsenws, out object v);
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
				_remoteSettings.Values.TryGetValue(autocollapsenews, out object v);
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
				_remoteSettings.Values.TryGetValue(autocollapsestupid, out object v);
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
				_remoteSettings.Values.TryGetValue(autocollapseofftopic, out object v);
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
				_remoteSettings.Values.TryGetValue(autocollapsepolitical, out object v);
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
				_remoteSettings.Values.TryGetValue(autocollapseinformative, out object v);
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
				_remoteSettings.Values.TryGetValue(autocollapseinteresting, out object v);
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
				_remoteSettings.Values.TryGetValue(markReadOnSort, out object v);
				return v != null && (bool)v;
			}
			set
			{
				_remoteSettings.Values[markReadOnSort] = value;
				TrackSettingChanged(value.ToString());
				NotifyPropertyChange();
			}
		}

		public int LaunchCount
		{
			get
			{
				_remoteSettings.Values.TryGetValue(launchCount, out object v);
				Debug.Assert(v != null, nameof(v) + " != null");
				return (int)v;
			}
			set
			{
				_remoteSettings.Values[launchCount] = value;
				TrackSettingChanged(value.ToString(CultureInfo.InvariantCulture));
				NotifyPropertyChange();
			}
		}

		public string ThemeName
		{
			get
			{
				_remoteSettings.Values.TryGetValue(themeName, out object v);
				return string.IsNullOrWhiteSpace((string)v) ? "System" : (string)v;
			}
			set
			{
				_remoteSettings.Values[themeName] = value;
				Theme = AvailableThemes.SingleOrDefault(t => t.Name.Equals(value, StringComparison.Ordinal)) ?? AvailableThemes.Single(t => t.Name.Equals("System", StringComparison.Ordinal));
				TrackSettingChanged(value);
				NotifyPropertyChange();
			}
		}

		public bool SeenMercuryBlast
		{
			get
			{
				_remoteSettings.Values.TryGetValue(seenMercuryBlast, out object v);
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

		public async Task<Dictionary<string, string>> GetTemplatePosts()
		{
			return await (_cloudSettingsManager?.GetCloudSetting<Dictionary<string, string>>("templatePosts")).ConfigureAwait(false);
		}

		public async Task SetTemplatePosts(Dictionary<string, string> value)
		{
			await (_cloudSettingsManager?.SetCloudSettings("templatePosts", value)).ConfigureAwait(false);
		}

		#endregion

		#region Local Settings
		private readonly List<CustomLauncher> _defaultCustomLaunchers = new List<CustomLauncher>
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

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only")]
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
				var v = Newtonsoft.Json.JsonConvert.SerializeObject(value);
				_localSettings.Values[customLaunchers] = v;
				TrackSettingChanged(v);
				NotifyPropertyChange();
			}
		}
		public bool UseCompactLayout
		{
			get
			{
				_localSettings.Values.TryGetValue(useCompactLayout, out object v);
				return (bool)v;
			}
			set
			{
				_localSettings.Values[useCompactLayout] = value;
				UpdateLayoutCompactness(value);
				NotifyPropertyChange();
				TrackSettingChanged(value.ToString());
			}
		}
		public bool EnableModTools
		{
			get
			{
				_localSettings.Values.TryGetValue(enableModTools, out object v);
				return (bool)v;
			}
			set
			{
				_localSettings.Values[enableModTools] = value;
				NotifyPropertyChange();
				TrackSettingChanged(value.ToString());
			}
		}

		public bool LargeReply
		{
			get
			{
				_localSettings.Values.TryGetValue(largeReply, out object v);
				return (bool)v;
			}
			set
			{
				_localSettings.Values[largeReply] = value;
				NotifyPropertyChange();
				TrackSettingChanged(value.ToString());
			}
		}
		public bool EnableUserFilter
		{
			get
			{
				_localSettings.Values.TryGetValue(enableUserFilter, out object v);
				return (bool)v;
			}
			set
			{
				_localSettings.Values[enableUserFilter] = value;
				NotifyPropertyChange();
				TrackSettingChanged(value.ToString());
			}
		}
		public bool EnableKeywordFilter
		{
			get
			{
				_localSettings.Values.TryGetValue(enableKeywordFilter, out object v);
				return (bool)v;
			}
			set
			{
				_localSettings.Values[enableKeywordFilter] = value;
				NotifyPropertyChange();
				TrackSettingChanged(value.ToString());
			}
		}
		public bool LoadImagesInline
		{
			get
			{
				_localSettings.Values.TryGetValue(loadImagesInline, out object v);
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
				_localSettings.Values.TryGetValue(enableDevTools, out object v);
				return (bool)v;
			}
			set
			{
				_localSettings.Values[enableDevTools] = value;
				NotifyPropertyChange();
				TrackSettingChanged(value.ToString());
			}
		}

		public bool UseMainDetail
		{
			get
			{
				_localSettings.Values.TryGetValue(useMainDetail, out object v);
				return (bool)v;
			}
			set
			{
				_localSettings.Values[useMainDetail] = value;
				NotifyPropertyChange();
				TrackSettingChanged(value.ToString());
			}
		}

		public bool UseSmoothScrolling
		{
			get
			{
				_localSettings.Values.TryGetValue(useSmoothScrolling, out var v);
				return (bool)v;
			}
			set
			{
				_localSettings.Values[useSmoothScrolling] = value;
				NotifyPropertyChange();
				TrackSettingChanged(value.ToString());
			}
		}

		public bool AllowNotificationsWhileActive
		{
			get
			{
				_localSettings.Values.TryGetValue(allowNotificationsWhileActive, out object v);
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
				_localSettings.Values.TryGetValue(notificationUID, out object v);
				Debug.Assert(v != null, nameof(v) + " != null");
				return (Guid)v;
			}
			set
			{
				_localSettings.Values[notificationUID] = value;
				NotifyPropertyChange();
				TrackSettingChanged(value.ToString());
			}
		}
		public bool PinMarkup
		{
			get
			{
				_localSettings.Values.TryGetValue(pinMarkup, out object v);
				return v != null && (bool)v;
			}
			set
			{
				_localSettings.Values[pinMarkup] = value;
				NotifyPropertyChange();
				TrackSettingChanged(value.ToString());
			}
		}

		public bool EnableNotifications
		{
			get
			{
				_localSettings.Values.TryGetValue(enableNotifications, out object v);
				return v != null && (bool)v;
			}
			set
			{
				_localSettings.Values[enableNotifications] = value;
				NotifyPropertyChange();
				TrackSettingChanged(value.ToString());
			}
		}
		public bool NotifyOnNameMention
		{
			get
			{
				_localSettings.Values.TryGetValue(notifyOnNameMention, out object v);
				return v != null && (bool)v;
			}
			set
			{
				_localSettings.Values[notifyOnNameMention] = value;
				NotifyPropertyChange();
				TrackSettingChanged(value.ToString());
			}
		}

		private List<string> npcNotificationKeywords;

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only")]
		public List<string> NotificationKeywords
		{
			get => npcNotificationKeywords;
			set
			{
				npcNotificationKeywords = value;
				NotifyPropertyChange();
				TrackSettingChanged(value.ToString());
			}
		}

		public bool DisableCortexAndNewsSplitView
		{
			get
			{
				_localSettings.Values.TryGetValue(disableCortexAndNewsSplitView, out object v);
				return v != null && (bool)v;
			}
			set
			{
				_localSettings.Values[disableCortexAndNewsSplitView] = value;
				TrackSettingChanged(value.ToString());
				NotifyPropertyChange();
			}
		}

		public bool PinnedSingleThreadAppBar
		{
			get
			{
				_localSettings.Values.TryGetValue(pinnedSingleThreadInlineAppBar, out object v);
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
				_localSettings.Values.TryGetValue(pinnedChattyAppBar, out object v);
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
				_localSettings.Values.TryGetValue(openUnknownLinksInEmbedded, out object v);
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
			get => 5;
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
				TrackSettingChanged(value.ToString(CultureInfo.InvariantCulture));
				NotifyPropertyChange();
			}
		}

		public int FilterIndex
		{
			get
			{
				_localSettings.Values.TryGetValue(filterIndex, out object v);
				Debug.Assert(v != null, nameof(v) + " != null");
				return (int)v;
			}
			set
			{
				_localSettings.Values[filterIndex] = value;
				this.TrackSettingChanged(value.ToString(CultureInfo.InvariantCulture));
				NotifyPropertyChange();
			}
		}

		public int OrderIndex
		{
			get
			{
				_localSettings.Values.TryGetValue(orderIndex, out object v);
				Debug.Assert(v != null, nameof(v) + " != null");
				return (int)v;
			}
			set
			{
				_localSettings.Values[orderIndex] = value;
				this.TrackSettingChanged(value.ToString(CultureInfo.InvariantCulture));
				NotifyPropertyChange();
			}
		}

		public bool IsUpdateInfoAvailable
		{
			get
			{
				_localSettings.Values.TryGetValue(newInfoAvailable, out object v);
				return v != null && (bool)v;
			}
			set
			{
				_localSettings.Values[newInfoAvailable] = value;
				NotifyPropertyChange();
				TrackSettingChanged(value.ToString());
			}
		}

		public double FontSize
		{
			get
			{
				_localSettings.Values.TryGetValue(fontSize, out object v);
				Debug.Assert(v != null, nameof(v) + " != null");
				return (double)v;
			}
			set
			{
				_localSettings.Values[fontSize] = value;
				UpdateLayoutCompactness(UseCompactLayout);
				TrackSettingChanged(value.ToString(CultureInfo.InvariantCulture));
				NotifyPropertyChange();
			}
		}

		public bool LocalFirstRun
		{
			get
			{
				_localSettings.Values.TryGetValue(localFirstRun, out object v);
				Debug.Assert(v != null, nameof(v) + " != null");
				return (bool)v;
			}
			set
			{
				_localSettings.Values[localFirstRun] = value;
				NotifyPropertyChange();
				TrackSettingChanged(value.ToString());
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
				TrackSettingChanged(value.ToString());
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
				TrackSettingChanged(value.ToString(CultureInfo.InvariantCulture));
			}
		}

		public long LastClipboardPostId
		{
			get
			{
				_localSettings.Values.TryGetValue(lastClipboardPostId, out object v);
				return (long)v;
			}
			set
			{
				_localSettings.Values[lastClipboardPostId] = value;
				NotifyPropertyChange();
				TrackSettingChanged(value.ToString(CultureInfo.InvariantCulture));
			}
		}

		public int DebugLogMessageBufferSize
		{
			get
			{
				_localSettings.Values.TryGetValue(debugLogMessageBufferSize, out object v);
				return (int)v;
			}
			set
			{
				_localSettings.Values[debugLogMessageBufferSize] = value;
				DebugLog.DebugLogMessageBufferSize = value;
				NotifyPropertyChange();
				TrackSettingChanged(value.ToString(CultureInfo.InvariantCulture));
			}
		}

		public double SplitViewSplitterPosition
		{
			get
			{
				_localSettings.Values.TryGetValue(splitViewSplitterPosition, out object v);
				return (double)v;
			}
			set
			{
				_localSettings.Values[splitViewSplitterPosition] = value;
				NotifyPropertyChange();
				TrackSettingChanged(value.ToString(CultureInfo.InvariantCulture));
			}
		}

		public double ArticleSplitViewSplitterPosition
		{
			get
			{
				_localSettings.Values.TryGetValue(articleSplitViewSplitterPosition, out object v);
				return (double)v;
			}
			set
			{
				_localSettings.Values[articleSplitViewSplitterPosition] = value;
				NotifyPropertyChange();
				TrackSettingChanged(value.ToString(CultureInfo.InvariantCulture));
			}
		}

		public async Task<Dictionary<string, string>> GetUserNotes()
		{
			try
			{
				_localSettings.Values.TryGetValue(userNotes, out object value);
				return Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>((string)value);
			}
			catch (Exception ex)
			{
				await DebugLog.AddException("Exception getting user notes", ex).ConfigureAwait(false);
			}
			return new Dictionary<string, string>();
		}

		public void SetUserNotes(Dictionary<string, string> value)
		{
			_localSettings.Values[userNotes] = Newtonsoft.Json.JsonConvert.SerializeObject(value);
			NotifyPropertyChange();
			TrackSettingChanged((string)_localSettings.Values[userNotes]);
		}

		#endregion

		public void MarkUpdateInfoRead()
		{
			_localSettings.Values[newInfoVersion] = _currentVersion;
			IsUpdateInfoAvailable = false;
		}

		private void UpdateLayoutCompactness(bool useCompactLayout)
		{
			var currentFontSize = FontSize;

			Application.Current.Resources["ControlContentThemeFontSize"] = currentFontSize;
			Application.Current.Resources["ContentControlFontSize"] = currentFontSize;
			Application.Current.Resources["ToolTipContentThemeFontSize"] = currentFontSize;
			Application.Current.Resources["ReplyHeaderFontSize"] = currentFontSize + 5;

			var padding = useCompactLayout ? (currentFontSize / 2) / 2 : currentFontSize / 2;
			var treeFontSize = Math.Ceiling((currentFontSize * 1.35) * (useCompactLayout ? 1 : 1.5));

			var tb = new TextBlock { Text = "Xg", FontSize = currentFontSize };

			tb.Measure(new Windows.Foundation.Size(Double.PositiveInfinity, Double.PositiveInfinity));
			_lineHeight = tb.DesiredSize.Height;
			PreviewItemHeight = _lineHeight * PreviewLineCount;

			Debug.WriteLine($"Using tree font size of {treeFontSize}pt");
			Application.Current.Resources["SmallFontSize"] = currentFontSize * .65;
			Application.Current.Resources["TabFontSize"] = useCompactLayout ? currentFontSize * .9 : currentFontSize;
			Application.Current.Resources["InlineButtonPadding"] = new Thickness(padding);
			Application.Current.Resources["InlineToggleButtonPadding"] = new Thickness(padding + 1);
			Application.Current.Resources["InlineButtonFontSize"] = currentFontSize + padding;
			Application.Current.Resources["PreviewRowHeight"] = currentFontSize + (padding * (useCompactLayout ? .75 : 2));
			Application.Current.Resources["TreeFontSize"] = treeFontSize;
			Application.Current.Resources["PreviewTagWidth"] = Math.Max(3 * Math.Ceiling((currentFontSize / 15)), 3);
			Application.Current.Resources["PreviewAuthorWidth"] = Math.Ceiling(120 * (currentFontSize / 15));
			tb.Text = ""; //Tag icon.
			tb.FontFamily = new FontFamily("Segoe MDL2 Assets");
			tb.FontSize = currentFontSize;
			tb.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
			Application.Current.Resources["PreviewTagColumnWidth"] = tb.DesiredSize.Width + 3;
			tb.Text = ""; //Just picking an icon that reaches both edges of the glyph
			tb.FontSize = currentFontSize + padding; //InlineButtonFontSize
			tb.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
			Application.Current.Resources["InlineButtonMinimumWidth"] = tb.DesiredSize.Width + (padding * 2);
			Debug.WriteLine($"Max icon width: {tb.DesiredSize.Width}");
			Application.Current.Resources["TreeDepthFont"] = useCompactLayout ? "/Assets/Fonts/replylinescompact.ttf#replylinescompact" : "/Assets/Fonts/replylines.ttf#replylines";
		}

		private ResourceDictionary currentThemeDictionary;

		private ThemeColorOption npcCurrentTheme;
		public ThemeColorOption Theme
		{
			get => npcCurrentTheme;
			private set
			{
				if (npcCurrentTheme?.Name != value.Name)
				{
					npcCurrentTheme = value;
					if (currentThemeDictionary != null)
					{
						Application.Current.Resources.MergedDictionaries.Remove(currentThemeDictionary);
					}
					currentThemeDictionary = new ResourceDictionary { Source = new Uri($"ms-appx:///Styles/Themes/{value.Name}Theme.xaml") };
					Application.Current.Resources.MergedDictionaries.Add(currentThemeDictionary);
					NotifyPropertyChange();
					TrackSettingChanged(value.ToString());
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
						new ThemeColorOption("Default", Color.FromArgb(255, 63, 110, 127)),
						new ThemeColorOption(
							"System",
							(new UISettings()).GetColorValue(UIColorType.Accent)
						),
						new ThemeColorOption("Lime", Color.FromArgb(255, 164, 196, 0)),
						new ThemeColorOption("Green", Color.FromArgb(255, 96, 169, 23)),
						new ThemeColorOption("Emerald", Color.FromArgb(255, 0, 138, 0)),
						new ThemeColorOption("Teal", Color.FromArgb(255, 0, 171, 169)),
						new ThemeColorOption("Cyan", Color.FromArgb(255, 27, 161, 226)),
						new ThemeColorOption("Cobalt", Color.FromArgb(255, 0, 80, 239)),
						new ThemeColorOption("Indigo", Color.FromArgb(255, 106, 0, 255)),
						new ThemeColorOption("Violet", Color.FromArgb(255, 170, 0, 255)),
						new ThemeColorOption("Pink", Color.FromArgb(255, 244, 114, 208)),
						new ThemeColorOption("Magenta", Color.FromArgb(255, 216, 0, 115)),
						new ThemeColorOption("Crimson", Color.FromArgb(255, 162, 0, 37)),
						new ThemeColorOption("Red", Color.FromArgb(255, 255, 35, 10)),
						new ThemeColorOption("Orange", Color.FromArgb(255, 250, 104, 0)),
						new ThemeColorOption("Amber", Color.FromArgb(255, 240, 163, 10)),
						new ThemeColorOption("Yellow", Color.FromArgb(255, 227, 200, 0)),
						new ThemeColorOption("Brown", Color.FromArgb(255, 130, 90, 44)),
						new ThemeColorOption("Olive", Color.FromArgb(255, 109, 135, 100)),
						new ThemeColorOption("Steel", Color.FromArgb(255, 100, 118, 135)),
						new ThemeColorOption("Mauve", Color.FromArgb(255, 118, 96, 138)),
						new ThemeColorOption("Taupe", Color.FromArgb(255, 135, 121, 78)),
						new ThemeColorOption("Gray", Color.FromArgb(255, 60, 60, 60))

						//new ThemeColorOption("White", Colors.White, Color.FromArgb(255, 0, 0, 0), Color.FromArgb(255, 235, 235, 235), Color.FromArgb(255, 0, 0, 0))
					};
				}
				return _availableThemes;
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
				TrackSettingChanged(value.ToString(CultureInfo.InvariantCulture));
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

		private void TrackSettingChanged(string settingValue, [CallerMemberName] string propertyName = "")
		{
			DebugLog.AddMessage($"Setting-{propertyName}-Updated to {settingValue}").ConfigureAwait(false).GetAwaiter().GetResult();
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
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
