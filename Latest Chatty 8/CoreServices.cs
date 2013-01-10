using Latest_Chatty_8.Settings;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using Latest_Chatty_8.Networking;
using System.Threading.Tasks;
using Windows.UI.Notifications;
using System;
using System.IO;
using Latest_Chatty_8.Common;

namespace Latest_Chatty_8
{
	/// <summary>
	/// Singleton object to perform some common functionality across the entire application
	/// </summary>
	public class CoreServices : BindableBase
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

        private bool initialized = false;

        async public Task Initialize()
        {
            if (!this.initialized)
            {
                this.initialized = true;
                this.PostCounts = (await ComplexSetting.ReadSetting<Dictionary<int, int>>("postcounts")) ?? new Dictionary<int, int>();
                await this.AuthenticateUser();
                await LatestChattySettings.Instance.LoadLongRunningSettings();
            }
        }

		/// <summary>
		/// Suspends this instance.
		/// </summary>
		async public Task Suspend()
		{
			if (this.PostCounts != null)
			{
				if (this.PostCounts.Count > 50000)
				{
					this.PostCounts = this.PostCounts.Skip(this.PostCounts.Count - 50000) as Dictionary<int, int>;
				}
				ComplexSetting.SetSetting<Dictionary<int, int>>("postcounts", this.PostCounts);
			}
			await LatestChattySettings.Instance.SaveToCloud();
			//this.PostCounts = null;
			//GC.Collect();
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

		/// <summary>
		/// The post counts
		/// </summary>
		public Dictionary<int, int> PostCounts;

		/// <summary>
		/// Gets set to true when a reply was posted so we can refresh the thread upon return.
		/// </summary>
		public bool PostedAComment { get; set; }

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
			this.credentials = null;
			var request = (HttpWebRequest)HttpWebRequest.Create("http://www.shacknews.com/account/signin");
			request.Method = "POST";
			request.Headers["x-requested-with"] = "XMLHttpRequest";
			request.Headers[HttpRequestHeader.Pragma] = "no-cache";

			request.ContentType = "application/x-www-form-urlencoded";

			var requestStream = await request.GetRequestStreamAsync();
			var streamWriter = new StreamWriter(requestStream);
			streamWriter.Write(String.Format("email={0}&password={1}&get_fields[]=result", Uri.EscapeUriString(CoreServices.Instance.Credentials.UserName), Uri.EscapeUriString(CoreServices.Instance.Credentials.Password)));
			streamWriter.Flush();
			streamWriter.Dispose();
			var response = await request.GetResponseAsync() as HttpWebResponse;
			//Doesn't seem like the API is actually returning failure codes, but... might as well handle it in case it does some time.
			if (response.StatusCode == HttpStatusCode.OK)
			{
				using (var responseStream = new StreamReader(response.GetResponseStream()))
				{
					var data = await responseStream.ReadToEndAsync();
					result = data.Equals("{\"result\":\"true\"}");
				}
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
				LatestChattySettings.Instance.ClearPinnedComments();
			}

			this.LoggedIn = result;
			return new Tuple<bool, string>(result, token);
		}
	}
}

