using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ApplicationSettings;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Latest_Chatty_8.Settings
{
	public sealed partial class MainSettings : UserControl, INotifyPropertyChanged
	{
		private LatestChattySettings settings;
		private string userAsyncToken;

		private bool npcValidatingUser;
		public bool ValidatingUser
		{
			get { return npcValidatingUser; }
			set { this.SetProperty(ref this.npcValidatingUser, value); }
		}

		private bool npcValidUser;
		public bool ValidUser
		{
			get { return npcValidUser; }
			set { this.SetProperty(ref this.npcValidUser, value); }
		}

		private bool npcInvalidUser;
		public bool InvalidUser
		{
			get { return npcInvalidUser; }
			set { this.SetProperty(ref this.npcInvalidUser, value); }
		}

		private bool npcSyncingSettings;
		public bool SyncingSettings
		{
			get { return npcSyncingSettings; }
			set { this.SetProperty(ref this.npcSyncingSettings, value); }
		}

		public MainSettings(LatestChattySettings settings)
		{
			this.InitializeComponent();
			this.DataContext = settings;
			this.settings = settings;
			this.password.Password = this.settings.Password;
			this.userValidation.DataContext = this;
		}

		public void Initialize()
		{
			this.ValidateUser(false);
		}

		private void LogOutClicked(object sender, RoutedEventArgs e)
		{
			this.settings.Username = this.settings.Password = this.password.Password = string.Empty;
		}

		private void PasswordChanged(object sender, RoutedEventArgs e)
		{
			this.settings.Password = ((PasswordBox)sender).Password;
			this.ValidateUser(true);
		}

		private void UserNameChanged(object sender, TextChangedEventArgs e)
		{
			this.ValidateUser(true);
		}

		private async void ValidateUser(bool updateCloudSettings)
		{
			this.ValidatingUser = true;
			this.InvalidUser = false;
			this.ValidUser = false;
			this.userAsyncToken = Guid.NewGuid().ToString();

			try
			{
				var validResponse = await GetUserValid(this.userAsyncToken);

				if (this.userAsyncToken == validResponse.Item2)
				{
					if (validResponse.Item1)
					{
						if (updateCloudSettings)
						{
							this.SyncingSettings = true;
							await this.settings.LoadLongRunningSettings();
							this.SyncingSettings = false;
						}
						this.ValidatingUser = false;
						this.ValidUser = true;
					}
					else
					{
						this.ValidatingUser = false;
						this.InvalidUser = true;
					}
				}
			}
			catch { }
		}

		private async Task<Tuple<bool, string>> GetUserValid(string token)
		{
			var request = (HttpWebRequest)HttpWebRequest.Create("http://www.shacknews.com/account/signin");
			request.Method = "POST";
			request.Headers["x-requested-with"] = "XMLHttpRequest";
			request.Headers[HttpRequestHeader.Pragma] = "no-cache";
			//request.Headers[HttpRequestHeader.Connection] = "keep-alive";

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
					return new Tuple<bool, string>(data.Equals("{\"result\":\"true\"}"), token);
				}
			}
			return new Tuple<bool, string>(false, token);
		}

		private void MySettingsBackClicked(object sender, RoutedEventArgs e)
		{
			if (this.Parent.GetType() == typeof(Popup))
			{
				((Popup)this.Parent).IsOpen = false;
			}
			SettingsPane.Show();
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] String propertyName = null)
		{
			if (object.Equals(storage, value)) return false;

			storage = value;
			this.OnPropertyChanged(propertyName);
			return true;
		}

		private void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			var eventHandler = this.PropertyChanged;
			if (eventHandler != null)
			{
				eventHandler(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}
}
