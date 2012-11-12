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
	public sealed partial class PrivacySettings : UserControl, INotifyPropertyChanged
	{
		private LatestChattySettings settings;

		public PrivacySettings(LatestChattySettings settings)
		{
			this.InitializeComponent();
			this.DataContext = settings;
			this.settings = settings;
		}

		public void Initialize()
		{
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
