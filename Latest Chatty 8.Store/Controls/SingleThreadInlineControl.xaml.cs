using Autofac;
using Latest_Chatty_8.Common;
using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Managers;
using Latest_Chatty_8.Settings;

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
		private IgnoreManager ignoreManager;
		private CommentThread currentThread;
		private bool initialized = false;
		private CoreWindow keyBindWindow = null;
		private WebView splitWebView;

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
			this.ignoreManager = container.Resolve<IgnoreManager>();
			this.keyBindWindow = CoreWindow.GetForCurrentThread();
			this.keyBindWindow.KeyDown += SingleThreadInlineControl_KeyDown;
			this.keyBindWindow.KeyUp += SingleThreadInlineControl_KeyUp;
			this.initialized = true;
		}

		public async Task Close()
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
			this.CloseWebView();
			this.initialized = false;
		}

		public void SelectPostId(int id)
		{
			if (this.currentThread == null) return;
			var comment = this.currentThread.Comments.SingleOrDefault(c => c.Id == id);
			if (comment == null) return;
			this.commentList.SelectedValue = comment;
		}

		#region Events
		private void ControlDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
		{
			var thread = args.NewValue as CommentThread;
			this.currentThread = thread;
			var shownWebView = false;

			if (thread != null)
			{
				this.commentList.ItemsSource = this.currentThread.Comments;
				this.commentList.UpdateLayout();
				this.commentList.SelectedIndex = 0;
				this.navigationBar.Visibility = Visibility.Visible;
				//There appears to be a bug with the CommandBar where if it's initiallized as collapsed, the closed mode will not apply correctly.
				//So to get around that, when we display it, we're basically forcing it to redraw itself.  Not great, but it is what it is.
				this.navigationBar.ClosedDisplayMode = AppBarClosedDisplayMode.Hidden;
				this.navigationBar.UpdateLayout();
				this.navigationBar.ClosedDisplayMode = this.Settings.PinnedSingleThreadAppBar ? AppBarClosedDisplayMode.Compact : AppBarClosedDisplayMode.Minimal;
				shownWebView = ShowSplitWebViewIfNecessary();
			}
			else
			{
				//Clear the list
				this.commentList.ItemsSource = null;
				this.navigationBar.Visibility = Visibility.Collapsed;
			}

			if (!shownWebView)
			{
				this.CloseWebView();
				VisualStateManager.GoToState(this, "Default", false);
			}
		}



		private async void SelectedItemChanged(object sender, SelectionChangedEventArgs e)
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
				//If the container is null it's probably because the list is virtualized and isn't loaded.
				if (container == null)
				{
					lv.ScrollIntoView(selectedItem);
					lv.UpdateLayout();
					container = lv.ContainerFromItem(selectedItem);
				}
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
				lv.UpdateLayout();
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
						Microsoft.HockeyApp.HockeyClient.Current.TrackEvent("Chatty-RPressed");
						button.IsChecked = true;
						this.ShowHideReply();
						break;
				}
				System.Diagnostics.Debug.WriteLine($"{this.GetType().Name} - KeyUp event for {args.VirtualKey}");
			}
			catch (Exception)
			{
				//(new Microsoft.ApplicationInsights.TelemetryClient()).TrackException(e, new Dictionary<string, string> { { "keyCode", args.VirtualKey.ToString() } });
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
						Microsoft.HockeyApp.HockeyClient.Current.TrackEvent("Chatty-APressed");
						MoveToPreviousPost();
						break;

					case VirtualKey.Z:
						Microsoft.HockeyApp.HockeyClient.Current.TrackEvent("Chatty-ZPressed");
						MoveToNextPost();
						break;
				}
				System.Diagnostics.Debug.WriteLine($"{this.GetType().Name} - KeyDown event for {args.VirtualKey}");
			}
			catch (Exception e)
			{
				//(new Microsoft.ApplicationInsights.TelemetryClient()).TrackException(e, new Dictionary<string, string> { { "keyCode", args.VirtualKey.ToString() } });
			}
		}

		private void CurrentWebView_Resized(object sender, EventArgs e)
		{
			this.commentList.ScrollIntoView(this.commentList.SelectedItem);
		}

		private void MessageAuthorClicked(object sender, RoutedEventArgs e)
		{

		}

		private async void IgnoreAuthorClicked(object sender, RoutedEventArgs e)
		{
			if (this.selectedComment == null) return;
			var author = this.selectedComment.Author;
			var dialog = new Windows.UI.Popups.MessageDialog($"Are you sure you want to ignore posts from { author }?");
			dialog.Commands.Add(new Windows.UI.Popups.UICommand("Ok", async (a) =>
			{
				await this.ignoreManager.AddIgnoredUser(author);
				this.ShellMessage(this, new ShellMessageEventArgs($"Posts from { author } will be ignored when the app is restarted"));
			}));
			dialog.Commands.Add(new Windows.UI.Popups.UICommand("Cancel"));
			dialog.CancelCommandIndex = 1;
			dialog.DefaultCommandIndex = 1;
			await dialog.ShowAsync();
		}

		private async void lolPostClicked(object sender, RoutedEventArgs e)
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
					Microsoft.HockeyApp.HockeyClient.Current.TrackEvent("Chatty-LolTagged-" + tag);
				}
				catch (Exception ex)
				{
					//(new Microsoft.ApplicationInsights.TelemetryClient()).TrackException(ex);
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

		private async void LolTagTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
		{
			Button s = null;
			try
			{
				s = sender as Button;
				if (s == null) return;
				s.IsEnabled = false;
				if (this.selectedComment == null) return;
				var tag = s.Tag as string;
				Microsoft.HockeyApp.HockeyClient.Current.TrackEvent("ViewedTagCount-" + tag);
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
				//(new TelemetryClient()).TrackException(ex);
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

		private void ReplyControl_ShellMessage(object sender, ShellMessageEventArgs args)
		{
			if (this.ShellMessage != null)
			{
				this.ShellMessage(sender, args);
			}
		}

		private void ReplyControl_Closed(object sender, EventArgs e)
		{
			var control = sender as Controls.PostContol;
			control.Closed -= ReplyControl_Closed;
			control.TextBoxGotFocus -= ReplyControl_TextBoxGotFocus;
			control.TextBoxLostFocus -= ReplyControl_TextBoxLostFocus;
			control.ShellMessage -= ReplyControl_ShellMessage;
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

		private async void MarkAllReadButtonClicked(object sender, RoutedEventArgs e)
		{
			if (this.currentThread == null) return;
			await this.chattyManager.MarkCommentThreadRead(this.currentThread);
		}
		#endregion

		#region Helpers
		private bool ShowSplitWebViewIfNecessary()
		{
			var shownWebView = false;
			if (!this.Settings.DisableNewsSplitView)
			{
				var firstComment = this.currentThread.Comments.FirstOrDefault();
				try
				{
					if (firstComment != null)
					{
						if (firstComment.AuthorType == AuthorType.Shacknews)
						{
							//Find the first href.
							var find = "<a target=\"_blank\" href=\"";
							var urlStart = firstComment.Body.IndexOf(find, StringComparison.Ordinal);
							var urlEnd = firstComment.Body.IndexOf("\"", urlStart + find.Length, StringComparison.Ordinal);
							if (urlStart > 0 && urlEnd > 0)
							{
								var storyUrl = new Uri(firstComment.Body.Substring(urlStart + find.Length, urlEnd - (urlStart + find.Length)));
								this.FindName(nameof(this.webViewContainer)); //Realize the container since it's deferred.
								VisualStateManager.GoToState(this, "WebViewShown", false);
								this.splitWebView = new WebView(WebViewExecutionMode.SeparateThread);
								Grid.SetRow(this.splitWebView, 0);
								this.webViewContainer.Children.Add(this.splitWebView);
								this.splitWebView.Navigate(storyUrl);
								shownWebView = true;
							}
						}
					}
				}
				catch { }
			}

			return shownWebView;
		}

		private void CloseWebView()
		{
			if (this.splitWebView != null)
			{
				this.splitWebView.Stop();
				this.splitWebView.NavigateToString("");
				this.webViewContainer.Children.Remove(this.splitWebView);
				this.splitWebView = null;
			}
		}

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
				replyControl.ShellMessage += ReplyControl_ShellMessage;
				replyControl.UpdateLayout();
				this.commentList.ScrollIntoView(this.commentList.SelectedItem);
			}
			else
			{
				this.ShortcutKeysEnabled = true;
				replyControl.Closed -= ReplyControl_Closed;
				replyControl.TextBoxGotFocus -= ReplyControl_TextBoxGotFocus;
				replyControl.TextBoxLostFocus -= ReplyControl_TextBoxLostFocus;
				replyControl.ShellMessage -= ReplyControl_ShellMessage;
			}
		}
		private void MoveToPreviousPost()
		{
			if (this.commentList.SelectedIndex >= 0)
			{
				this.commentList.SelectedIndex = this.commentList.SelectedIndex == 0 ? this.commentList.Items.Count - 1 : this.commentList.SelectedIndex - 1;
			}
		}

		private void MoveToNextPost()
		{
			if (this.commentList.SelectedIndex >= 0)
			{
				this.commentList.SelectedIndex = this.commentList.SelectedIndex == this.commentList.Items.Count - 1 ? 0 : this.commentList.SelectedIndex + 1;
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


	}
}
