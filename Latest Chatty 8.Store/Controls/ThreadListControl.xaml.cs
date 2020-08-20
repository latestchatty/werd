using Autofac;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Werd.Common;
using Werd.DataModel;
using Werd.Managers;
using Werd.Settings;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Werd.Controls
{
	public sealed partial class ThreadListControl : UserControl, INotifyPropertyChanged
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

		private ThreadMarkManager _markManager;
		private ChattyManager _chattyManager;

		public event EventHandler<RefreshRequestedEventArgs> RefreshRequested;

		public event EventHandler<SelectionChangedEventArgs> SelectionChanged;
		public event EventHandler<ThreadSwipeEventArgs> ThreadSwiped;
		private CommentThread npcSelectedThread;

		public CommentThread SelectedThread
		{
			get => npcSelectedThread;
			set => SetProperty(ref npcSelectedThread, value);
		}

		public double ItemHeight { get; set; } = 68;

		public ThreadListControl()
		{
			this.InitializeComponent();
			_markManager = AppGlobal.Container.Resolve<ThreadMarkManager>();
			_chattyManager = AppGlobal.Container.Resolve<ChattyManager>();
		}

		public void ScrollIntoView(object obj)
		{
			ThreadList.ScrollIntoView(obj);
		}
		internal void ScrollToTop()
		{
			if (ThreadList.Items != null && ThreadList.Items.Count > 0)
			{
				ThreadList.ScrollIntoView(ThreadList.Items[0]);
			}
		}


		internal void SelectNone()
		{
			ThreadList.SelectedIndex = -1;
		}

		internal void SelectNextThread()
		{
			if (ThreadList.Items != null)
			{
				ThreadList.SelectedIndex = Math.Min(ThreadList.SelectedIndex + 1,
					ThreadList.Items.Count - 1);
			}
			else
			{
				ThreadList.SelectedIndex = 0;
			}
		}

		internal void SelectPreviousThread()
		{
			ThreadList.SelectedIndex = Math.Max(ThreadList.SelectedIndex - 1, 0);
		}



		private void RefreshContainerRefreshRequested(RefreshContainer sender, RefreshRequestedEventArgs args) => this?.RefreshRequested(this, args);
		private void ChattyListSelectionChanged(object sender, SelectionChangedEventArgs e) => this?.SelectionChanged(this, e);

		private void GoToChattyTopClicked(object sender, RoutedEventArgs e)
		{
			ScrollToTop();
		}

		private void PreviewDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
		{
			var tb = (TextBlock)sender;

			tb.Height = ItemHeight;
		}

		private async void MarkReadSwipe(SwipeItem sender, SwipeItemInvokedEventArgs args)
		{
			var ct = args.SwipeControl.DataContext as CommentThread;
			if (ct is null) return;
			await SwipeThread(ChattySwipeOperationType.MarkRead, ct).ConfigureAwait(false);
		}

		private async void PinUnpinSwipe(SwipeItem sender, SwipeItemInvokedEventArgs args)
		{
			var ct = args.SwipeControl.DataContext as CommentThread;
			if (ct is null) return;
			await SwipeThread(ChattySwipeOperationType.Pin, ct).ConfigureAwait(false);
		}

		private async void CollapseSwipe(SwipeItem sender, SwipeItemInvokedEventArgs args)
		{
			var ct = args.SwipeControl.DataContext as CommentThread;
			if (ct is null) return;
			await SwipeThread(ChattySwipeOperationType.Collapse, ct).ConfigureAwait(false);
		}

		private async Task SwipeThread(ChattySwipeOperationType op, CommentThread ct)
		{
			MarkType currentMark = _markManager.GetMarkType(ct.Id);
			switch (op)
			{
				case ChattySwipeOperationType.Collapse:

					if (currentMark != MarkType.Collapsed)
					{
						await _markManager.MarkThread(ct.Id, MarkType.Collapsed).ConfigureAwait(true);
					}
					else if (currentMark == MarkType.Collapsed)
					{
						await _markManager.MarkThread(ct.Id, MarkType.Unmarked).ConfigureAwait(true);
					}
					break;
				case ChattySwipeOperationType.Pin:
					if (currentMark != MarkType.Pinned)
					{
						await _markManager.MarkThread(ct.Id, MarkType.Pinned).ConfigureAwait(true);
					}
					else if (currentMark == MarkType.Pinned)
					{
						await _markManager.MarkThread(ct.Id, MarkType.Unmarked).ConfigureAwait(true);
					}
					break;
				case ChattySwipeOperationType.MarkRead:
					await _chattyManager.MarkCommentThreadRead(ct).ConfigureAwait(true);
					break;
			}
			ThreadSwiped?.Invoke(this, new ThreadSwipeEventArgs(op, ct));
		}
	}
}
