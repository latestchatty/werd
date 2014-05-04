using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.ApplicationSettings;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Latest_Chatty_8.Shared.Settings;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Latest_Chatty_8.Settings
{
	public sealed partial class MainSettings : UserControl, INotifyPropertyChanged
	{
		public MainSettings(LatestChattySettings settings)
		{
			this.InitializeComponent();
		}
		
		private void MySettingsBackClicked(object sender, RoutedEventArgs e)
		{
			if (this.Parent.GetType() == typeof(Popup))
			{
				((Popup)this.Parent).IsOpen = false;
			}
			SettingsPane.Show();
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
