using Latest_Chatty_8.Common;
using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Shared;
using Latest_Chatty_8.Shared.DataModel;
using Latest_Chatty_8.Shared.Networking;
using Latest_Chatty_8.Shared.Settings;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Notifications;

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

		private CoreServices()
		{
			this.ChattyManager = new ChattyManager();
			this.PinManager = new PinManager();
		}

		private bool initialized = false;

		async public Task Initialize()
		{
			if (!this.initialized)
			{
				this.initialized = true;
				await this.AuthenticateUser();
				//await LatestChattySettings.Instance.LoadLongRunningSettings();
				this.ChattyManager.StartAutoChattyRefresh();
			}
		}

		/// <summary>
		/// Suspends this instance.
		/// </summary>
		async public Task Suspend()
		{
			await this.ChattyManager.StopAutoChattyRefresh();
			await LatestChattySettings.Instance.SaveToCloud();
			//this.PostCounts = null;
			//GC.Collect();
		}

		async public Task Resume()
		{
			await this.ClearTile(false);
			System.Diagnostics.Debug.WriteLine("Loading seen posts.");
//			this.lastChattyRefresh = await ComplexSetting.ReadSetting<DateTime?>("lastrefresh") ?? DateTime.MinValue;
			await this.AuthenticateUser();
			this.ChattyManager.StartAutoChattyRefresh(); //TODO: Make this smarter.
			//await LatestChattySettings.Instance.LoadLongRunningSettings();
			//if (DateTime.Now.Subtract(this.lastChattyRefresh).TotalMinutes > 20)
			//{
			//	await this.RefreshChatty(); //Completely refresh the chatty.
			//}
			//else
			//{
			//	this.StartAutoChattyRefresh(); //Try to apply updates
			//}
		}

		public ChattyManager ChattyManager
		{
			get;
			private set;
		}

		public PinManager PinManager
		{
			get;
			private set;
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

		private bool npcShowAuthor = true;
		public bool ShowAuthor
		{
			get { return npcShowAuthor; }
			set { this.SetProperty(ref npcShowAuthor, value); }
		}


		

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

		



		/// <summary>
		/// Authenticates the user set in the application settings.
		/// </summary>
		/// <param name="token">A token that can be used to identify a result.</param>
		/// <returns></returns>
		public async Task<Tuple<bool, string>> AuthenticateUser(string token = "")
		{
			var result = false;
			//:HACK: :TODO: This feels dirty as hell. Figure out if we even need the credentials object any more.  Seems like we should just use it from settings.
			this.credentials = null; //Clear the cached credentials so they get recreated.
			if (CoreServices.Instance.Credentials != null && !string.IsNullOrEmpty(CoreServices.Instance.Credentials.UserName))
			{
				try
				{
					var response = await POSTHelper.Send(Locations.VerifyCredentials, new List<KeyValuePair<string, string>>(), true);

					if (response.StatusCode == HttpStatusCode.OK)
					{
						var data = await response.Content.ReadAsStringAsync();
						var json = JToken.Parse(data);
						result = (bool)json["isValid"];
						System.Diagnostics.Debug.WriteLine((result ? "Valid" : "Invalid") + " login");
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
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine("Error occurred while logging in: {0}", ex);
				}	//No matter what happens, fail to log in.
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
				//TODO: This is probably really, really bad to do.
				var t = this.ChattyManager.StopAutoChattyRefresh();
			}

			// Free any unmanaged objects here.
			//
			disposed = true;
		}
	}
}

