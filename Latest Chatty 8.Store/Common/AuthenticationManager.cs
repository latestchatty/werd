
using Latest_Chatty_8.Networking;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Windows.Security.Credentials;

namespace Latest_Chatty_8.Common
{
	public class AuthenticationManager : BindableBase
	{
		private const string resourceName = "LatestChatty";

		private PasswordVault pwVault = new PasswordVault();

		private bool initialized = false;

		async public Task Initialize()
		{
			if (!this.initialized)
			{
				this.initialized = true;
				for (var i = 0; i < 3; i++)
				{
					if (await this.AuthenticateUser())
					{
						break; //If we successfully log in, we're done. If not, try a few more times before we give up.
					}
				}
			}
		}

		/// <summary>
		/// Username of the currently logged in user.
		/// </summary>
		public string UserName
		{
			get { return npcUserName; }
			private set { this.SetProperty(ref npcUserName, value); }
		}
		private bool npcLoggedIn;
		private string npcUserName;

		/// <summary>
		/// Gets the password for the currently logged in user
		/// </summary>
		/// <returns></returns>
		public string GetPassword()
		{
			string password = string.Empty;
			if (this.LoggedIn)
			{
				var cred = this.pwVault.RetrieveAll().FirstOrDefault();
				if (cred != null)
				{
					cred.RetrievePassword();
					password = cred.Password;
				}
			}
			return password;
		}

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
		/// Attempt to authenticate user and store credentials upon success.
		/// If user and pass are not provided, stored credentials will be used if available.
		/// </summary>
		/// <param name="userName"></param>
		/// <param name="password"></param>
		/// <returns></returns>
		async public Task<bool> AuthenticateUser(string userName = "", string password = "")
		{
			var result = false;
			if (string.IsNullOrWhiteSpace(userName) && string.IsNullOrWhiteSpace(password))
			{
				//Try to get user/pass from stored creds.
				try
				{
					var cred = this.pwVault.RetrieveAll().FirstOrDefault();
					if (cred != null)
					{
						userName = cred.UserName;
						cred.RetrievePassword();
						password = cred.Password;
					}
				}
				catch (Exception) { }
			}

			if (!string.IsNullOrWhiteSpace(userName) && !string.IsNullOrWhiteSpace(password))
			{
				try
				{
					var response = await POSTHelper.Send(
						Locations.VerifyCredentials,
						new List<KeyValuePair<string, string>>() {
						new KeyValuePair<string, string>("username", userName),
						new KeyValuePair<string, string>("password", password)
						},
						false,
						this);

					if (response.StatusCode == HttpStatusCode.OK)
					{
						var data = await response.Content.ReadAsStringAsync();
						var json = JToken.Parse(data);
						result = (bool)json["isValid"];
						System.Diagnostics.Debug.WriteLine((result ? "Valid" : "Invalid") + " login");
					}

					this.LogOut();
					if (result)
					{
						pwVault.Add(new PasswordCredential(resourceName, userName, password));
						this.UserName = userName;
					}
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine("Error occurred while logging in: {0}", ex);
				}   //No matter what happens, fail to log in.
			}
			this.LoggedIn = result;
			return result;
		}

		public void LogOut()
		{
			this.LoggedIn = false;
			this.UserName = string.Empty;
			var creds = this.pwVault.RetrieveAll();
			if (creds.Count > 0)
			{
				var credsToRemove = creds.ToList();
				foreach (var cred in credsToRemove)
				{
					pwVault.Remove(cred);
				}
			}
		}
	}
}

