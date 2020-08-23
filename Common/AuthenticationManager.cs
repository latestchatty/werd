using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Windows.Security.Credentials;

namespace Common
{
	public class AuthenticationManager : BindableBase
	{
		private const string ResourceName = "LatestChatty";

		private readonly PasswordVault _pwVault = new PasswordVault();

		private bool _initialized;

		public async Task Initialize()
		{
			if (!_initialized)
			{
				_initialized = true;
				for (var i = 0; i < 3; i++)
				{
					if (await AuthenticateUser().ConfigureAwait(true))
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
			get => npcUserName;
			private set => SetProperty(ref npcUserName, value);
		}
		private bool npcLoggedIn;
		private string npcUserName;


		/// <summary>
		/// Gets the password for the currently logged in user
		/// </summary>
		/// <returns></returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1826:Do not use Enumerable methods on indexable collections")]
		public string GetPassword()
		{
			string password = string.Empty;
			if (LoggedIn)
			{
				var cred = _pwVault.RetrieveAll().FirstOrDefault();
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
			get => npcLoggedIn;
			private set => SetProperty(ref npcLoggedIn, value);
		}


		/// <summary>
		/// Attempt to authenticate user and store credentials upon success.
		/// If user and pass are not provided, stored credentials will be used if available.
		/// </summary>
		/// <param name="userName"></param>
		/// <param name="password"></param>
		/// <returns></returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1826:Do not use Enumerable methods on indexable collections")]
		public async Task<bool> AuthenticateUser(string userName = "", string password = "")
		{
			Debug.WriteLine("Attempting login.");
			var result = false;
			if (string.IsNullOrWhiteSpace(userName) && string.IsNullOrWhiteSpace(password))
			{
				//Try to get user/pass from stored creds.
				try
				{
					var cred = _pwVault.RetrieveAll().FirstOrDefault();
					if (cred != null)
					{
						userName = cred.UserName;
						cred.RetrievePassword();
						password = cred.Password;
					}
				}
				catch
				{
					// ignored
				}
			}

			if (!string.IsNullOrWhiteSpace(userName) && !string.IsNullOrWhiteSpace(password))
			{
				try
				{
					using (var response = await PostHelper.Send(
						Locations.VerifyCredentials,
						new List<KeyValuePair<string, string>>
						{
						new KeyValuePair<string, string>("username", userName),
						new KeyValuePair<string, string>("password", password)
						},
						false,
						this).ConfigureAwait(true))
					{

						if (response.StatusCode == HttpStatusCode.OK)
						{
							var data = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
							var json = JToken.Parse(data);
							result = (bool)json["isValid"];
							Debug.WriteLine((result ? "Valid" : "Invalid") + " login");
						}
					}
					LogOut();
					if (result)
					{
						_pwVault.Add(new PasswordCredential(ResourceName, userName, password));
						UserName = userName;
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine("Error occurred while logging in: {0}", ex);
				}   //No matter what happens, fail to log in.
			}
			LoggedIn = result;
			return result;
		}

		public void LogOut()
		{
			LoggedIn = false;
			UserName = string.Empty;
			var creds = _pwVault.RetrieveAll();
			if (creds.Count > 0)
			{
				var credsToRemove = creds.ToList();
				foreach (var cred in credsToRemove)
				{
					_pwVault.Remove(cred);
				}
			}
		}
	}
}

