using System;
using System.Collections.Generic;
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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace Latest_Chatty_8.Views
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class CommentThread : Page, INotifyPropertyChanged
	{
		private Latest_Chatty_8.DataModel.CommentThread npcSelectedThread = null;
		public Latest_Chatty_8.DataModel.CommentThread SelectedThread
		{
			get { return this.npcSelectedThread; }
			set { this.SetProperty(ref this.npcSelectedThread, value); }
		}

		public CommentThread()
		{
			this.InitializeComponent();
			Windows.Phone.UI.Input.HardwareButtons.BackPressed += HardwareButtons_BackPressed;
		}

		private void HardwareButtons_BackPressed(object sender, Windows.Phone.UI.Input.BackPressedEventArgs e)
		{
			Frame frame = Window.Current.Content as Frame;
			if (frame == null)
			{
				return;
			}

			if (frame.CanGoBack)
			{
				frame.GoBack();
				e.Handled = true;
			}
		}

		/// <summary>
		/// Invoked when this page is about to be displayed in a Frame.
		/// </summary>
		/// <param name="e">Event data that describes how this page was reached.
		/// This parameter is typically used to configure the page.</param>
		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			var ct = e.Parameter as Latest_Chatty_8.DataModel.CommentThread;
			if (ct != null)
			{
				this.SelectedThread = ct;
				this.ShowHidePinButtons();
			}
			else
			{
				if (Frame.CanGoBack) { Frame.GoBack(); }
			}
		}

		//:TODO: Make sure the user is logged in and don't show any of this if they're not.
		private void ShowHidePinButtons()
		{
			this.pinButton.Visibility = this.SelectedThread.IsPinned ? Visibility.Collapsed : Visibility.Visible;
			this.unPinButton.Visibility = this.SelectedThread.IsPinned ? Visibility.Visible : Visibility.Collapsed;
		}

		async private void pinButtonClicked(object sender, RoutedEventArgs e)
		{
			await CoreServices.Instance.PinThread(this.SelectedThread.Id);
			this.ShowHidePinButtons();
		}

		async private void unPinButtonClicked(object sender, RoutedEventArgs e)
		{
			await CoreServices.Instance.UnPinThread(this.SelectedThread.Id);
			this.ShowHidePinButtons();
		}

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

		private void CommentClicked(object sender, RoutedEventArgs e)
		{
			if(this.threadView.SelectedComment != null)
			{
				this.Frame.Navigate(typeof(PostComment), this.threadView.SelectedComment);
			}
		}
	}
}
