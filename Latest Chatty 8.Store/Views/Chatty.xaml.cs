using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Shared.Settings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237
namespace Latest_Chatty_8.Views
{
	/// <summary>
	/// A basic page that provides characteristics common to most applications.
	/// </summary>
	public sealed partial class Chatty : Latest_Chatty_8.Shared.LayoutAwarePage, INotifyPropertyChanged
	{
		#region NPC
		/// <summary>
		/// Multicast event for property change notifications.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Checks if a property already matches a desired value.  Sets the property and
		/// notifies listeners only when necessary.
		/// </summary>
		/// <typeparam name="T">Type of the property.</typeparam>
		/// <param name="storage">Reference to a property with both getter and setter.</param>
		/// <param name="value">Desired value for the property.</param>
		/// <param name="propertyName">Name of the property used to notify listeners.  This
		/// value is optional and can be provided automatically when invoked from compilers that
		/// support CallerMemberName.</param>
		/// <returns>True if the value was changed, false if the existing value matched the
		/// desired value.</returns>
		private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] String propertyName = null)
		{
			if (object.Equals(storage, value)) return false;

			storage = value;
			this.OnPropertyChanged(propertyName);
			return true;
		}

		/// <summary>
		/// Notifies listeners that a property value has changed.
		/// </summary>
		/// <param name="propertyName">Name of the property used to notify listeners.  This
		/// value is optional and can be provided automatically when invoked from compilers
		/// that support <see cref="CallerMemberNameAttribute"/>.</param>
		private void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			var eventHandler = this.PropertyChanged;
			if (eventHandler != null)
			{
				eventHandler(this, new PropertyChangedEventArgs(propertyName));
			}
		}
		#endregion

		#region Private Variables
		#endregion

		bool npcChattyChecked = false;
		public bool ChattyChecked
		{
			get { return npcChattyChecked; }
			set
			{
				if (this.SetProperty(ref npcChattyChecked, value))
				{
					this.UpdateViews();
				}
			}
		}

		private void UpdateViews()
		{
			if(this.ChattyChecked)
			{
				var chattyControl = new Controls.ChattyControl();
				this.splitter.Content = chattyControl;
				chattyControl.LoadChatty();
            }
			else
			{
				this.splitter.Content = new Settings.MainSettings();
            }
		}

		private string npcCurrentViewName;
		public string CurrentViewName
		{
			get { return npcCurrentViewName; }
			set { this.SetProperty(ref this.npcCurrentViewName, value); }
		}

		#region Constructor
		public Chatty()
		{
			this.InitializeComponent();
			this.CurrentViewName = "Chatty";
			this.DataContext = this;
		}


		#endregion

		#region Load and Save State
		async protected override void OnNavigatedTo(Windows.UI.Xaml.Navigation.NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			//await LatestChattySettings.Instance.LoadLongRunningSettings();
			await CoreServices.Instance.Initialize();
			//:TODO: RE-enable pinned posts loading here.
			//await LatestChattySettings.Instance();

			await CoreServices.Instance.ClearTile(true);
            //this.CommentThreads = CoreServices.Instance.Chatty;
            //this.chattyControl.LoadChatty();
		}




        #endregion
		
    }
}
