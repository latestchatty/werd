using Autofac;
using Latest_Chatty_8.Common;
using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Settings;
using Microsoft.ApplicationInsights;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace Latest_Chatty_8.Controls
{
	public sealed partial class SingleThreadInlineControl : UserControl, INotifyPropertyChanged
	{
		public bool ShortcutKeysEnabled { get; set; } = true;

		public event EventHandler<LinkClickedEventArgs> LinkClicked;

		public event EventHandler<ShellMessageEventArgs> ShellMessage;

		private Comment selectedComment;
		private ChattyManager chattyManager;
		private AuthenticationManager authManager;
		private CommentThread currentThread;
		private bool initialized = false;
		private CoreWindow keyBindWindow = null;

		private LatestChattySettings npcSettings = null;
		private LatestChattySettings Settings
		{
			get { return this.npcSettings; }
			set { this.SetProperty(ref this.npcSettings, value); }
		}

		public SingleThreadInlineControl()
		{
			this.InitializeComponent();
		}

		public void Initialize(Autofac.IContainer container)
		{
			if (this.initialized) return;
			this.chattyManager = container.Resolve<ChattyManager>();
			this.Settings = container.Resolve<LatestChattySettings>();
			this.authManager = container.Resolve<AuthenticationManager>();
			this.keyBindWindow = CoreWindow.GetForCurrentThread();
			this.keyBindWindow.KeyDown += SingleThreadInlineControl_KeyDown;
			this.keyBindWindow.KeyUp += SingleThreadInlineControl_KeyUp;
			this.initialized = true;
		}

		async public Task Close()
		{
			if (this.currentThread != null)
			{
				await this.chattyManager.DeselectAllPostsForCommentThread(this.currentThread);
			}
			if (this.keyBindWindow != null)
			{
				this.keyBindWindow.KeyDown -= SingleThreadInlineControl_KeyDown;
				this.keyBindWindow.KeyUp -= SingleThreadInlineControl_KeyUp;
			}
			this.initialized = false;
		}

		#region Events
		private void ControlDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
		{
			var thread = args.NewValue as CommentThread;
			this.currentThread = thread;
			if (thread != null)
			{
				this.commentList.ItemsSource = this.currentThread.Comments;
				this.commentList.UpdateLayout();
				this.commentList.SelectedIndex = 0;
				this.navigationBar.Visibility = Visibility.Visible;
			}
			else
			{
				//Clear the list
				this.commentList.ItemsSource = null;
				this.navigationBar.Visibility = Visibility.Collapsed;
			}
		}

		async private void SelectedItemChanged(object sender, SelectionChangedEventArgs e)
		{
			var lv = sender as ListView;
			if (lv == null) return; //This would be bad.
			this.selectedComment = null;
			//this.SetFontSize();

			await this.chattyManager.DeselectAllPostsForCommentThread(this.currentThread);

			if (e.AddedItems.Count == 1)
			{
				var selectedItem = e.AddedItems[0] as Comment;
				if (selectedItem == null) return; //Bail, we don't know what to
				var container = lv.ContainerFromItem(selectedItem);
				//TODO: Optimize the number of on screen elements, I don't think we need them all any more.
				if (container == null)
				{
					this.commentList.SelectedIndex = -1;
					return; //Bail because the visual tree isn't created yet...
				}
				this.selectedComment = selectedItem;
				await this.chattyManager.MarkCommentRead(this.currentThread, this.selectedComment);
				var gridContainer = container.FindFirstControlNamed<Grid>("container");
				gridContainer.FindName("commentSection"); //Using deferred loading, we have to fully realize the post we're now going to be looking at.

				var richPostView = container.FindFirstControlNamed<RichPostView>("postView");
				richPostView.LoadPost(this.selectedComment.Body);
				selectedComment.IsSelected = true;
				this.commentList.UpdateLayout();
				lv.ScrollIntoView(selectedItem);
			}
			this.ShortcutKeysEnabled = true;
		}

		private void SingleThreadInlineControl_KeyUp(CoreWindow sender, KeyEventArgs args)
		{
			try
			{
				if (!this.ShortcutKeysEnabled)
				{
					System.Diagnostics.Debug.WriteLine($"{this.GetType().Name} - Suppressed KeyUp event.");
					return;
				}

				switch (args.VirtualKey)
				{
					case VirtualKey.R:
						if (this.selectedComment == null) return;
						var controlContainer = this.commentList.ContainerFromItem(this.selectedComment);
						var button = controlContainer.FindFirstControlNamed<ToggleButton>("showReply");
						if (button == null) return;
						(new Microsoft.ApplicationInsights.TelemetryClient()).TrackEvent("Chatty-RPressed");
						button.IsChecked = true;
						this.ShowHideReply();
						break;
				}
				System.Diagnostics.Debug.WriteLine($"{this.GetType().Name} - KeyUp event for {args.VirtualKey}");
			}
			catch (Exception e)
			{
				(new Microsoft.ApplicationInsights.TelemetryClient()).TrackException(e, new Dictionary<string, string> { { "keyCode", args.VirtualKey.ToString() } });
			}

		}

		private void SingleThreadInlineControl_KeyDown(CoreWindow sender, KeyEventArgs args)
		{
			try
			{
				if (!this.ShortcutKeysEnabled)
				{
					System.Diagnostics.Debug.WriteLine($"{this.GetType().Name} - Suppressed KeyDown event.");
					return;
				}

				switch (args.VirtualKey)
				{
					case VirtualKey.A:
						(new Microsoft.ApplicationInsights.TelemetryClient()).TrackEvent("Chatty-APressed");
						MoveToPreviousPost();
						break;

					case VirtualKey.Z:
						(new Microsoft.ApplicationInsights.TelemetryClient()).TrackEvent("Chatty-ZPressed");
						MoveToNextPost();
						break;
				}
				System.Diagnostics.Debug.WriteLine($"{this.GetType().Name} - KeyDown event for {args.VirtualKey}");
			}
			catch (Exception e)
			{
				(new Microsoft.ApplicationInsights.TelemetryClient()).TrackException(e, new Dictionary<string, string> { { "keyCode", args.VirtualKey.ToString() } });
			}
		}

		private void CurrentWebView_Resized(object sender, EventArgs e)
		{
			this.commentList.ScrollIntoView(this.commentList.SelectedItem);
		}

		async private void lolPostClicked(object sender, RoutedEventArgs e)
		{
			if (this.selectedComment == null) return;
			var controlContainer = this.commentList.ContainerFromItem(this.selectedComment);
			if (controlContainer != null)
			{
				var tagButton = controlContainer.FindFirstControlNamed<Button>("tagButton");
				if (tagButton == null) return;

				tagButton.IsEnabled = false;
				try
				{
					var mi = sender as MenuFlyoutItem;
					var tag = mi.Text;
					await this.selectedComment.LolTag(tag);
					(new Microsoft.ApplicationInsights.TelemetryClient()).TrackEvent("Chatty-LolTagged-" + tag);
				}
				catch (Exception ex)
				{
					(new Microsoft.ApplicationInsights.TelemetryClient()).TrackException(ex);
					if (this.ShellMessage != null)
					{
						this.ShellMessage(this, new ShellMessageEventArgs("Problem tagging, try again later.", ShellMessageType.Error));
					}
				}
				finally
				{
					tagButton.IsEnabled = true;
				}
			}
		}

		async private void LolTagTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
		{
			Button s = null;
			try
			{
				s = sender as Button;
				if (s == null) return;
				s.IsEnabled = false;
				if (this.selectedComment == null) return;
				var tag = s.Tag as string;
				(new TelemetryClient()).TrackEvent("ViewedTagCount-" + tag);
				var lolUrl = Networking.Locations.GetLolTaggersUrl(this.selectedComment.Id, tag);
				var response = await Networking.JSONDownloader.DownloadObject(lolUrl);
				var names = string.Join(Environment.NewLine, response[tag].Select(a => a.ToString()).OrderBy(a => a));
				var flyout = new Flyout();
				var tb = new TextBlock();
				tb.Text = names;
				flyout.Content = tb;
				flyout.ShowAt(s);
			}
			catch (Exception ex)
			{
				(new TelemetryClient()).TrackException(ex);
				this.ShellMessage(this, new ShellMessageEventArgs("Error retrieving taggers. Try again later.", ShellMessageType.Error));
			}
			finally
			{
				if (s != null)
				{
					s.IsEnabled = true;
				}
			}
		}

		private void ShowReplyClicked(object sender, RoutedEventArgs e)
		{
			this.ShowHideReply();
		}

		private void ReplyControl_TextBoxLostFocus(object sender, EventArgs e)
		{
			this.ShortcutKeysEnabled = true;
		}

		private void ReplyControl_TextBoxGotFocus(object sender, EventArgs e)
		{
			this.ShortcutKeysEnabled = false;
		}

		private void ReplyControl_Closed(object sender, EventArgs e)
		{
			var control = sender as Controls.PostContol;
			control.Closed -= ReplyControl_Closed;
			control.TextBoxGotFocus -= ReplyControl_TextBoxGotFocus;
			control.TextBoxLostFocus -= ReplyControl_TextBoxLostFocus;
			if (this.selectedComment == null) return;
			var controlContainer = this.commentList.ContainerFromItem(this.selectedComment);
			if (controlContainer == null) return;
			var button = controlContainer.FindFirstControlNamed<Windows.UI.Xaml.Controls.Primitives.ToggleButton>("showReply");
			if (button == null) return;
			button.IsChecked = false;
			this.ShortcutKeysEnabled = true;
		}

		private void CopyPostLinkClicked(object sender, RoutedEventArgs e)
		{
			var button = sender as Button;
			if (button == null) return;
			var comment = button.DataContext as Comment;
			if (comment == null) return;
			var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
			dataPackage.SetText(string.Format("http://www.shacknews.com/chatty?id={0}#item_{0}", comment.Id));
			Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
			if (this.ShellMessage != null)
			{
				this.ShellMessage(this, new ShellMessageEventArgs("Link copied to clipboard."));
			}
		}

		private void RichPostLinkClicked(object sender, LinkClickedEventArgs e)
		{
			if (this.LinkClicked != null)
			{
				this.LinkClicked(this, e);
			}
		}

		private void PreviousNavigationButtonClicked(object sender, RoutedEventArgs e)
		{
			this.MoveToPreviousPost();
		}

		private void NextNavigationButtonClicked(object sender, RoutedEventArgs e)
		{
			this.MoveToNextPost();
		}

		async private void MarkAllReadButtonClicked(object sender, RoutedEventArgs e)
		{
			if (this.currentThread == null) return;
			await this.chattyManager.MarkCommentThreadRead(this.currentThread);
		}
		#endregion

		#region Helpers
		private void ShowHideReply()
		{
			if (this.selectedComment == null) return;
			var controlContainer = this.commentList.ContainerFromItem(this.selectedComment);
			if (controlContainer == null) return;
			var button = controlContainer.FindFirstControlNamed<Windows.UI.Xaml.Controls.Primitives.ToggleButton>("showReply");
			if (button == null) return;
			var commentSection = controlContainer.FindFirstControlNamed<Grid>("commentSection");
			if (commentSection == null) return;
			commentSection.FindName("replyArea"); //Lazy load
			var replyControl = commentSection.FindFirstControlNamed<PostContol>("replyControl");
			if (replyControl == null) return;
			if (button.IsChecked.HasValue && button.IsChecked.Value)
			{
				replyControl.Visibility = Visibility.Visible;
				replyControl.SetAuthenticationManager(this.authManager);
				replyControl.SetFocus();
				replyControl.Closed += ReplyControl_Closed;
				replyControl.TextBoxGotFocus += ReplyControl_TextBoxGotFocus;
				replyControl.TextBoxLostFocus += ReplyControl_TextBoxLostFocus;
				replyControl.UpdateLayout();
				this.commentList.ScrollIntoView(this.commentList.SelectedItem);
			}
			else
			{
				this.ShortcutKeysEnabled = true;
				replyControl.Closed -= ReplyControl_Closed;
				replyControl.TextBoxGotFocus -= ReplyControl_TextBoxGotFocus;
				replyControl.TextBoxLostFocus -= ReplyControl_TextBoxLostFocus;
			}
		}
		private void MoveToPreviousPost()
		{
			if (this.commentList.SelectedIndex >= 0)
			{
				this.commentList.SelectedIndex = this.commentList.SelectedIndex == 0 ? 0 : this.commentList.SelectedIndex - 1;
			}
		}

		private void MoveToNextPost()
		{
			if (this.commentList.SelectedIndex >= 0)
			{
				this.commentList.SelectedIndex = this.commentList.SelectedIndex == this.commentList.Items.Count - 1 ? this.commentList.Items.Count - 1 : this.commentList.SelectedIndex + 1;
			}
		}
		#endregion

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
		protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] String propertyName = null)
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
		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			var eventHandler = this.PropertyChanged;
			if (eventHandler != null)
			{
				eventHandler(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		#endregion

	}
}
