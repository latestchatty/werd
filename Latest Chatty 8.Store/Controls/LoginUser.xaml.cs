using Latest_Chatty_8.Shared.Settings;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Latest_Chatty_8.Controls
{
	public sealed partial class LoginUser : UserControl, INotifyPropertyChanged
	{
		public LoginUser()
		{
			this.InitializeComponent();

			this.DataContext = LatestChattySettings.Instance;
			this.password.Password = LatestChattySettings.Instance.Password;
			this.userValidation.DataContext = this;
		}

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

		public void Initialize()
		{
			this.ValidateUser(false);
		}

		async private void LogOutClicked(object sender, RoutedEventArgs e)
		{
			LatestChattySettings.Instance.Username = LatestChattySettings.Instance.Password = this.password.Password = string.Empty;
			await CoreServices.Instance.AuthenticateUser();
			await LatestChattySettings.Instance.LoadLongRunningSettings();
			await CoreServices.Instance.RefreshChatty();
		}

		private void PasswordChanged(object sender, RoutedEventArgs e)
		{
			LatestChattySettings.Instance.Password = ((PasswordBox)sender).Password;
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
				var validResponse = await CoreServices.Instance.AuthenticateUser(this.userAsyncToken);

				if (this.userAsyncToken == validResponse.Item2)
				{
					if (validResponse.Item1)
					{
						if (updateCloudSettings)
						{
							this.SyncingSettings = true;
							await LatestChattySettings.Instance.LoadLongRunningSettings();
							await CoreServices.Instance.RefreshChatty();
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
			catch
			{ System.Diagnostics.Debug.Assert(false); }
		}

		#region NotifyPropertyChanged Handlers
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
		#endregion
	}
}
