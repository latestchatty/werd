using Autofac;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Werd.DataModel;
using Werd.Managers;
using Werd.Settings;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Werd.Controls
{
	public class AddThreadTabEventArgs : EventArgs
	{
		public CommentThread Thread { get; set; }
		public bool AddInBackground { get; set; }

		public AddThreadTabEventArgs(CommentThread thread, bool addInBackground = false)
		{
			AddInBackground = addInBackground;
			Thread = thread;
		}
	}

	public sealed partial class ThreadGroupHeader : UserControl, INotifyPropertyChanged
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
		///     value is optional and can be provided automatically when invoked from compilers that
		///     support CallerMemberName.</param>
		/// <returns>True if the value was changed, false if the existing value matched the
		/// desired value.</returns>
		private void SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
		{
			if (Equals(storage, value)) return;

			storage = value;
			OnPropertyChanged(propertyName);
		}

		/// <summary>
		/// Notifies listeners that a property value has changed.
		/// </summary>
		/// <param name="propertyName">Name of the property used to notify listeners.  This
		/// value is optional and can be provided automatically when invoked from compilers
		/// that support <see cref="CallerMemberNameAttribute"/>.</param>
		private void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			var eventHandler = PropertyChanged;
			if (eventHandler != null)
			{
				eventHandler(this, new PropertyChangedEventArgs(propertyName));
			}
		}
		#endregion

		public static Brush GetDateColor(DateTime dateTime)
		{
			var expireTime = dateTime.AddHours(18).ToUniversalTime();
			if (expireTime > DateTime.UtcNow)
			{
				return new SolidColorBrush(Color.FromArgb(255, 133, 133, 133));
			}
			else
			{
				return new SolidColorBrush(Colors.OrangeRed);
			}
		}

		public static string GetDateTooltip(DateTime dateTime)
		{
			var sb = new StringBuilder();
			sb.Append(dateTime.ToString(CultureInfo.CurrentCulture));
			if (dateTime.AddHours(18).ToUniversalTime() <= DateTime.UtcNow)
			{
				sb.AppendLine();
				sb.AppendLine();
				sb.AppendLine("This thread is no longer part of the active chatty.");
				sb.AppendLine();
				sb.Append("Replies here will not bump the post and other users may not be aware of new activity.");
			}
			return sb.ToString();
		}

		private CommentThread _commentThread;
		public CommentThread CommentThread
		{
			get => _commentThread;
			set
			{
				SetProperty(ref _commentThread, value);
				this.Bindings.Update();
			}
		}

		private bool _showTabAddMenuItem;
		public bool ShowTabAddMenuItem { get => _showTabAddMenuItem; set => SetProperty(ref _showTabAddMenuItem, value); }

		public event EventHandler<AddThreadTabEventArgs> AddThreadTabClicked;
		private readonly ChattyManager _chattyManager;
		private readonly ThreadMarkManager _markManager;
		private readonly LatestChattySettings _settings;

		public ThreadGroupHeader()
		{
			this.InitializeComponent();
			_chattyManager = AppGlobal.Container.Resolve<ChattyManager>();
			_markManager = AppGlobal.Container.Resolve<ThreadMarkManager>();
			_settings = AppGlobal.Settings;
		}

		//private void RefreshSingleThreadClicked(object sender, RoutedEventArgs e)
		//{
		//	CommentThread.ResyncGrouped();
		//}

		private void TabThreadClicked(object sender, RoutedEventArgs e)
		{
			AddThreadTabClicked?.Invoke(this, new AddThreadTabEventArgs(CommentThread));
		}
		private void BackgroundTabThreadClicked(object sender, RoutedEventArgs e)
		{
			AddThreadTabClicked?.Invoke(this, new AddThreadTabEventArgs(CommentThread, true));
		}

		private async void MarkAllReadButtonClicked(object sender, RoutedEventArgs e)
		{
			await _chattyManager.MarkCommentThreadRead(CommentThread).ConfigureAwait(false);
		}

		private async void PinThreadClicked(object sender, RoutedEventArgs e)
		{
			await _markManager.MarkThread(CommentThread.Id, CommentThread.IsPinned ? MarkType.Unmarked : MarkType.Pinned).ConfigureAwait(false);
		}

		private async void CollapseThreadClicked(object sender, RoutedEventArgs e)
		{
			await _markManager.MarkThread(CommentThread.Id, CommentThread.IsCollapsed ? MarkType.Unmarked : MarkType.Collapsed).ConfigureAwait(false);
		}

	}
}
