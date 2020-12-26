using Autofac;
using Common;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Werd.DataModel;
using Werd.Managers;
using Werd.Settings;
using Werd.Views;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using IContainer = Autofac.IContainer;

namespace Werd.Controls
{
	public sealed partial class UserStatsControl : UserControl, INotifyPropertyChanged
	{
		private CortexUser _user;

		public CortexUser User
		{
			get => _user;
			set
			{
				if(SetProperty(ref _user, value))
				{
					UserIsSelf = _authManager.LoggedIn && _authManager.UserName.Equals(value.Username, StringComparison.OrdinalIgnoreCase);
					this.Bindings.Update();
				}
			}
		}

		private UserFlair _userFlair;
		public UserFlair UserFlair
		{
			get => _userFlair;
			set => SetProperty(ref _userFlair, value);
		}

		public Brush UserColor { get; set; }

		private bool _userIsSelf;
		private bool UserIsSelf
		{
			get => _userIsSelf;
			set => SetProperty(ref _userIsSelf, value);
		}

		private readonly AppSettings _settings;
		private readonly IgnoreManager _ignoreManager;
		private readonly ChattyManager _chattyManager;
		private readonly AuthenticationManager _authManager;

		public UserStatsControl()
		{
			this.InitializeComponent();
			_settings = AppGlobal.Settings;
			_ignoreManager = AppGlobal.Container.Resolve<IgnoreManager>();
			_chattyManager = AppGlobal.Container.Resolve<ChattyManager>();
			_authManager = AppGlobal.Container.Resolve<AuthenticationManager>();
			UserColor = this.Foreground;
		}

		public Visibility GetModVisibility(UserFlair flair)
		{
			if (flair is null || !flair.IsModerator) return Visibility.Collapsed;
			return Visibility.Visible;
		}

		public Visibility GetNotModVisibility(UserFlair flair)
		{
			if (flair is null || !flair.IsModerator) return Visibility.Visible;
			return Visibility.Collapsed;
		}

		private async void IgnoreAuthorClicked(object sender, RoutedEventArgs e)
		{
			var author = User.Username;
			var dialog = new MessageDialog($"Are you sure you want to ignore posts from { author }?");
			dialog.Commands.Add(new UICommand("Ok", async a =>
			{
				await _ignoreManager.AddIgnoredUser(author).ConfigureAwait(true);
				_chattyManager.ScheduleImmediateFullChattyRefresh();
			}));
			dialog.Commands.Add(new UICommand("Cancel"));
			dialog.CancelCommandIndex = 1;
			dialog.DefaultCommandIndex = 1;
			await dialog.ShowAsync();
		}
		private void SearchAuthorClicked(object sender, RoutedEventArgs e)
		{
			if (Window.Current.Content is Shell f)
			{
				f.NavigateToPage(
					typeof(CustomSearchWebView),
					new Views.NavigationArgs.WebViewNavigationArgs
						(AppGlobal.Container,
						new Uri($"https://www.shacknews.com/search?chatty=1&type=4&chatty_term=&chatty_user={Uri.EscapeUriString(User.Username)}& chatty_author=&chatty_filter=all&result_sort=postdate_desc")
						)
				);
			}
		}

		private void SearchAuthorRepliesClicked(object sender, RoutedEventArgs e)
		{
			if (Window.Current.Content is Shell f)
			{
				f.NavigateToPage(
					typeof(CustomSearchWebView),
					new Views.NavigationArgs.WebViewNavigationArgs
						(AppGlobal.Container,
						new Uri($"https://www.shacknews.com/search?chatty=1&type=4&chatty_term=&chatty_user=&chatty_author={Uri.EscapeUriString(User.Username)}&chatty_filter=all&result_sort=postdate_desc")
						)
				);
			}
		}

		private void MessageAuthorClicked(object sender, RoutedEventArgs e)
		{
			if (Window.Current.Content is Shell f)
			{
				f.NavigateToPage(typeof(Messages), new Tuple<IContainer, string>(AppGlobal.Container, User.Username));
			}
		}

		private void ViewAuthorModHistoryClicked(object sender, RoutedEventArgs e)
		{
			var author = User.Username;
			if (Window.Current.Content is Shell f)
			{
				f.NavigateToPage(typeof(ModToolsWebView), new Views.NavigationArgs.WebViewNavigationArgs(AppGlobal.Container, new Uri($"https://www.shacknews.com/moderators/check?username={author}")));
			}
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
		///     value is optional and can be provided automatically when invoked from compilers that
		///     support CallerMemberName.</param>
		/// <returns>True if the value was changed, false if the existing value matched the
		/// desired value.</returns>
		private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
		{
			if (Equals(storage, value)) return false;

			storage = value;
			OnPropertyChanged(propertyName);
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
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}




		#endregion

	}
}
