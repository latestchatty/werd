using Latest_Chatty_8.DataModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace Latest_Chatty_8
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class MainPage : Page, INotifyPropertyChanged
	{
		private CommentThread npcSelectedThread = null;
		public CommentThread SelectedThread
		{
			get { return this.npcSelectedThread; }
			set { this.SetProperty(ref this.npcSelectedThread, value); }
		}

		private ReadOnlyObservableCollection<CommentThread> npcCommentThreads;
		public ReadOnlyObservableCollection<CommentThread> CommentThreads
		{
			get { return this.npcCommentThreads; }
			set
			{
				this.SetProperty(ref this.npcCommentThreads, value);
			}
		}
		public MainPage()
		{
			this.InitializeComponent();

			this.NavigationCacheMode = NavigationCacheMode.Required;
		}

		/// <summary>
		/// Invoked when this page is about to be displayed in a Frame.
		/// </summary>
		/// <param name="e">Event data that describes how this page was reached.
		/// This parameter is typically used to configure the page.</param>
		async protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			// TODO: Prepare page for display here.

			// TODO: If your application contains multiple pages, ensure that you are
			// handling the hardware Back button by registering for the
			// Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
			// If you are using the NavigationHelper provided by some templates,
			// this event is handled for you.
			await CoreServices.Instance.Initialize();
			this.loadingIndicator.IsActive = false;
			this.loadingIndicator.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

			this.CommentThreads = CoreServices.Instance.Chatty;
		}

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

		private void CommentSelected(object sender, SelectionChangedEventArgs e)
		{
			if (this.SelectedThread == null) { return; }
			this.Frame.Navigate(typeof(Latest_Chatty_8.Views.CommentThread), this.SelectedThread);
			this.SelectedThread = null;
		}

		private void SettingsClicked(object sender, RoutedEventArgs e)
		{
			this.Frame.Navigate(typeof(Latest_Chatty_8.Views.Settings));
		}

		private void CommentClicked(object sender, RoutedEventArgs e)
		{
			this.Frame.Navigate(typeof(Latest_Chatty_8.Views.PostComment));
		}
	}
}
