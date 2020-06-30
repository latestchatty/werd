using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Werd.Common;
using Werd.DataModel;
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

		private const double SwipeThreshold = 110;
		private bool? _swipingLeft;

		public event EventHandler<RefreshRequestedEventArgs> RefreshRequested;

		public event EventHandler<SelectionChangedEventArgs> SelectionChanged;
		public event EventHandler<ThreadSwipeEventArgs> ThreadSwiped;

		public ChattySwipeOperation SwipeRightOperation { get; set; }
		public ChattySwipeOperation SwipeLeftOperation { get; set; }

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

		#region Swipe Gestures
		private void ChattyListManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
		{
			Grid grid = sender as Grid;
			if (grid == null) return;

			Grid container = grid.FindFirstControlNamed<Grid>("previewContainer");
			if (container == null) return;

			Grid swipeContainer = grid.FindName("swipeContainer") as Grid;
			if (swipeContainer != null)
			{
				swipeContainer.Visibility = Visibility.Visible;
			}

			container.Background = (Brush)Resources["ApplicationPageBackgroundThemeBrush"];
			_swipingLeft = null;
		}

		private void ChattyListManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
		{
			Grid grid = sender as Grid;
			if (grid == null) return;

			Grid container = grid.FindFirstControlNamed<Grid>("previewContainer");
			if (container == null) return;

			Grid swipeContainer = grid.FindName("swipeContainer") as Grid;
			if (swipeContainer != null) swipeContainer.Visibility = Visibility.Collapsed;

			CommentThread ct = container.DataContext as CommentThread;
			if (ct == null) return;
			e.Handled = false;

			bool completedSwipe = Math.Abs(e.Cumulative.Translation.X) > SwipeThreshold;
			ChattySwipeOperation operation = e.Cumulative.Translation.X > 0 ? SwipeRightOperation : SwipeLeftOperation;

			if (completedSwipe)
			{
				this?.ThreadSwiped(this, new ThreadSwipeEventArgs(operation, ct));
			}

			TranslateTransform transform = container.RenderTransform as TranslateTransform;
			if (transform != null) transform.X = 0;
			container.Background = new SolidColorBrush(Colors.Transparent);
			_swipingLeft = null;
		}

		private void ChattyListManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
		{
			Grid grid = sender as Grid;
			if (grid == null) return;

			Grid container = grid.FindFirstControlNamed<Grid>("previewContainer");
			if (container == null) return;

			StackPanel swipeContainer = grid.FindFirstControlNamed<StackPanel>("swipeTextContainer");
			if (swipeContainer == null) return;

			TranslateTransform swipeIconTransform = swipeContainer.RenderTransform as TranslateTransform;

			TranslateTransform transform = container.RenderTransform as TranslateTransform;
			double cumulativeX = e.Cumulative.Translation.X;
			bool showRight = (cumulativeX < 0);

			if (!_swipingLeft.HasValue || _swipingLeft != showRight)
			{
				CommentThread commentThread = grid.DataContext as CommentThread;
				if (commentThread == null) return;

				TextBlock swipeIcon = grid.FindFirstControlNamed<TextBlock>("swipeIcon");
				if (swipeIcon == null) return;
				TextBlock swipeText = grid.FindFirstControlNamed<TextBlock>("swipeText");
				if (swipeText == null) return;

				ChattySwipeOperation op = showRight ? SwipeLeftOperation : SwipeRightOperation;

				swipeIcon.Text = op.Icon;
				swipeText.Text = op.DisplayName;
				swipeContainer.FlowDirection = showRight ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
				_swipingLeft = showRight;
			}

			if (transform != null) transform.X = cumulativeX;
			if (swipeIconTransform == null) return;
			if (Math.Abs(cumulativeX) < SwipeThreshold)
			{
				swipeIconTransform.X = showRight ? -(cumulativeX * .3) : cumulativeX * .3;
			}
			else
			{
				swipeIconTransform.X = 15;
			}
		}

		#endregion

		private void ThreadListRightHeld(object sender, HoldingRoutedEventArgs e)
		{
			FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
		}
		private void ThreadListRightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
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
	}
}
