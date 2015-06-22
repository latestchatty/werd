using Autofac;
using Latest_Chatty_8.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Latest_Chatty_8
{
	public sealed partial class Shell : Page
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
		IContainer container;
		#endregion        

		private string npcCurrentViewName;
		public string CurrentViewName
		{
			get { return npcCurrentViewName; }
			set { this.SetProperty(ref this.npcCurrentViewName, value); }
		}

		#region Constructor
		public Shell(Frame rootFrame, IContainer container)
		{
			this.InitializeComponent();
			this.splitter.Content = rootFrame;
			rootFrame.Navigated += FrameNavigatedTo;
			this.container = container;
			var sv = rootFrame.Content as ShellView;
			if (sv != null)
			{
				SetCaptionFromFrame(sv);
			}
		}

		private void FrameNavigatedTo(object sender, NavigationEventArgs e)
		{
			var sv = e.Content as ShellView;
			if (sv != null)
			{
				SetCaptionFromFrame(sv);
			}
		}


		#endregion

		#region Load and Save State
		async protected override void OnNavigatedTo(Windows.UI.Xaml.Navigation.NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			var authManager = this.container.Resolve<AuthenticaitonManager>();
			await authManager.Initialize();
			
			//await LatestChattySettings.Instance.LoadLongRunningSettings();
			//:TODO: RE-enable pinned posts loading here.
			//await LatestChattySettings.Instance();

			//this.CommentThreads = CoreServices.Instance.Chatty;
			//this.chattyControl.LoadChatty();
		}

		#endregion

		private void ClickedNav(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var f = this.splitter.Content as Frame;
            if (this.chattyRadio.IsChecked.HasValue && this.chattyRadio.IsChecked.Value)
            {
                f.Navigate(typeof(Chatty), this.container);
            }
            else if(this.settingsRadio.IsChecked.HasValue && this.settingsRadio.IsChecked.Value)
            {
                f.Navigate(typeof(SettingsView), this.container);
            }
			else if(this.helpRadio.IsChecked.HasValue && this.helpRadio.IsChecked.Value)
			{
				f.Navigate(typeof(Help), this.container);
			}
        }

		private void SetCaptionFromFrame(ShellView sv)
		{
			this.CurrentViewName = sv.ViewTitle;
		}

		private void BackClicked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			var f = this.splitter.Content as Frame;
			if(f.CanGoBack)
			{
				f.GoBack();
			}
		}
	}
}
