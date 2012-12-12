using Latest_Chatty_8.Settings;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using Latest_Chatty_8.Networking;
using System.Threading.Tasks;
using Windows.UI.Notifications;
using System;
using System.IO;
using System.ComponentModel;
using Latest_Chatty_8.Common;
using Windows.UI.Xaml;

namespace Latest_Chatty_8
{
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

		async public Task Initialize()
		{
			this.PostCounts = (await ComplexSetting.ReadSetting<Dictionary<int, int>>("postcounts")) ?? new Dictionary<int, int>();
			this.AuthenticateUser(Guid.NewGuid().ToString());
		}

		public void Suspend()
		{
			if (this.PostCounts.Count > 10000)
			{
				this.PostCounts = this.PostCounts.Skip(this.PostCounts.Count - 10000) as Dictionary<int, int>;
			}
			ComplexSetting.SetSetting<Dictionary<int, int>>("postcounts", this.PostCounts);
		}

		public NetworkCredential Credentials
		{
			get
			{
				return new NetworkCredential(LatestChattySettings.Instance.Username, LatestChattySettings.Instance.Password);
			}
		}

		public Dictionary<int, int> PostCounts;

		/// <summary>
		/// Gets set to true when a reply was posted so we can refresh the thread upon return.
		/// </summary>
		public bool PostedAComment { get; set; }

		async public Task ClearAndRegisterForNotifications()
		{
			TileUpdateManager.CreateTileUpdaterForApplication().Clear();
			BadgeUpdateManager.CreateBadgeUpdaterForApplication().Clear();
			await NotificationHelper.ReRegisterForNotifications();
		}

		private bool npcLoggedIn;
		public bool LoggedIn
		{
			get { return npcLoggedIn; }
			private set
			{
				this.SetProperty(ref this.npcLoggedIn, value);
			}
		}

		//private string lastTriedUserName;
		//private string lastTriedPassword;
		//private bool? lastValidationResult = null;
		//async public Task<bool> AreCredentialsValid()
		//{
		//	var creds = this.Credentials;
		//	var result = lastValidationResult ?? false;
		//	if (lastValidationResult == null || !creds.UserName.Equals(lastTriedUserName) || !creds.Password.Equals(lastTriedPassword))
		//	{
		//		lastTriedUserName = creds.UserName;
		//		lastTriedPassword = creds.Password;
		//		result = (await this.AuthenticateUser(Guid.NewGuid().ToString())).Item1;
		//		lastValidationResult = result;
		//	}

		//	return result;
		//}

		public async Task<Tuple<bool, string>> AuthenticateUser(string token)
		{
			var result = false;
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

			this.LoggedIn = result;
			return new Tuple<bool, string>(result, token);
		}
	}
}

