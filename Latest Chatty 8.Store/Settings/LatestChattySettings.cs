using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Settings;
using Latest_Chatty_8.Shared.Networking;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.UI;

namespace Latest_Chatty_8.Shared.Settings
{
	public class LatestChattySettings : INotifyPropertyChanged
	{
		//private static readonly string commentSize = "CommentSize";
		private static readonly string showInlineImages = "embedimages";
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
		//private static readonly string pinnedThreads = "PinnedComments";
		private static readonly string sortNewToTop = "sortnewtotop";
		private static readonly string refreshRate = "refreshrate";
		private static readonly string rightList = "rightlist";
		private static readonly string themeBackgroundColor = "themebackgroundcolor";
		private static readonly string themeForegroundColor = "themeforegroundcolor";
		private static readonly string themeName = "themename";

		private Windows.Storage.ApplicationDataContainer settingsContainer;

		public LatestChattySettings()
		{
			//TODO: Local settings for things like inline image loading since you might want that to not work on metered connections, etc.
			//TODO: Respond to updates to roaming settings coming from other devices
			var localContainer = Windows.Storage.ApplicationData.Current.RoamingSettings;
			System.Diagnostics.Debug.WriteLine("Max roaming storage is {0} KB.", Windows.Storage.ApplicationData.Current.RoamingStorageQuota);
			this.settingsContainer = localContainer.CreateContainer("generalSettings", Windows.Storage.ApplicationDataCreateDisposition.Always);

			if (!this.settingsContainer.Values.ContainsKey(enableNotifications))
			{
				this.settingsContainer.Values.Add(enableNotifications, false);
			}
			//if (!this.settingsContainer.Values.ContainsKey(commentSize))
			//{
			//	this.settingsContainer.Values.Add(commentSize, CommentViewSize.Small);
			//}
			if (!this.settingsContainer.Values.ContainsKey(showInlineImages))
			{
				this.settingsContainer.Values.Add(showInlineImages, true);
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
			if (!this.settingsContainer.Values.ContainsKey(sortNewToTop))
			{
				this.settingsContainer.Values.Add(sortNewToTop, true);
			}
			if (!this.settingsContainer.Values.ContainsKey(refreshRate))
			{
				this.settingsContainer.Values.Add(refreshRate, 5);
			}
			if (!this.settingsContainer.Values.ContainsKey(rightList))
			{
				this.settingsContainer.Values.Add(rightList, false);
			}
			if (!this.settingsContainer.Values.ContainsKey(themeBackgroundColor))
			{
				this.settingsContainer.Values.Add(themeBackgroundColor, ColorToInt32(Windows.UI.Color.FromArgb(255, 63, 110, 127)));
			}
			if (!this.settingsContainer.Values.ContainsKey(themeForegroundColor))
			{
				this.settingsContainer.Values.Add(themeForegroundColor, ColorToInt32(Windows.UI.Colors.White));
			}
			if (!this.settingsContainer.Values.ContainsKey(themeName))
			{
				this.settingsContainer.Values.Add(themeName, "Default");
			}

			this.Theme = this.AvailableThemes.SingleOrDefault(t => t.Name.Equals(this.ThemeName)) ?? this.AvailableThemes.Single(t => t.Name.Equals("Default"));
		}

		#region WinChatty Service


		#endregion

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
					//var t = NotificationHelper.ReRegisterForNotifications();
				}
				else
				{
					//var t = NotificationHelper.UnRegisterNotifications();
				}
				this.NotifyPropertyChange();
			}
		}

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

		public string ThemeName
		{
			get
			{
				object v;
				this.settingsContainer.Values.TryGetValue(themeName, out v);
				return string.IsNullOrWhiteSpace((string)v) ? "Default" : (string)v;
			}
			set
			{
				this.settingsContainer.Values[themeName] = value;
				this.Theme = this.AvailableThemes.SingleOrDefault(t => t.Name.Equals(value)) ?? this.AvailableThemes.Single(t => t.Name.Equals("Default"));
				this.NotifyPropertyChange();
			}
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

		private Int32 ColorToInt32(Color color)
		{
			return (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;
		}

		private Color Int32ToColor(Int32 intColor)
		{
			return Windows.UI.Color.FromArgb((byte)(intColor >> 24), (byte)(intColor >> 16), (byte)(intColor >> 8), (byte)intColor);
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
