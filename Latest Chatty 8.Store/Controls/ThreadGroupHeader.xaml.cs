using Autofac;
using Microsoft.Toolkit.Collections;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Werd.DataModel;
using Werd.Managers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Werd.Controls
{
	public class ThreadEventEventArgs : EventArgs
	{
		public CommentThread Thread { get; set; }

		public ThreadEventEventArgs(CommentThread thread)
		{
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

		private CommentThread _commentThread;
		public CommentThread CommentThread { get => _commentThread; set => SetProperty(ref _commentThread, value); }

		public event EventHandler<ThreadEventEventArgs> AddThreadTabClicked;
		private readonly ChattyManager _chattyManager;
		private readonly ThreadMarkManager _markManager;

		public ThreadGroupHeader()
		{
			this.InitializeComponent();
			_chattyManager = AppGlobal.Container.Resolve<ChattyManager>();
			_markManager = AppGlobal.Container.Resolve<ThreadMarkManager>();
		}

		private void RefreshSingleThreadClicked(object sender, RoutedEventArgs e)
		{
			CommentThread.ResyncGrouped();
		}

		private async void TabThreadClicked(object sender, RoutedEventArgs e)
		{
			AddThreadTabClicked?.Invoke(this, new ThreadEventEventArgs(CommentThread));
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
