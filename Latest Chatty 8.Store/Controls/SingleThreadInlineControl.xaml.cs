using Autofac;
using Latest_Chatty_8.Common;
using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Settings;
using System;
using System.Collections.Generic;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace Latest_Chatty_8.Controls
{
	public sealed partial class SingleThreadInlineControl : UserControl
	{
		public bool ShortcutKeysEnabled { get; set; } = false;

		private Grid currentWebViewContainer;
		private Comment selectedComment;
		private ChattyManager chattyManager;
		private LatestChattySettings settings;
		private AuthenticationManager authManager;
		private RichPostView currentRichPostView;
		private CommentThread currentThread;

		public SingleThreadInlineControl()
		{
			this.InitializeComponent();
		}

		public void Initialize(Autofac.IContainer container)
		{
			this.chattyManager = container.Resolve<ChattyManager>();
			this.settings = container.Resolve<LatestChattySettings>();
			this.authManager = container.Resolve<AuthenticationManager>();
			CoreWindow.GetForCurrentThread().KeyDown += SingleThreadInlineControl_KeyDown;
			CoreWindow.GetForCurrentThread().KeyUp += SingleThreadInlineControl_KeyUp;
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
			}
			else
			{
				//Clear the list
				this.commentList.ItemsSource = null;
			}
		}

		async private void SelectedItemChanged(object sender, SelectionChangedEventArgs e)
		{
			try
			{
				var lv = sender as ListView;
				if (lv == null) return; //This would be bad.
				this.selectedComment = null;
				//this.SetFontSize();

				foreach (var removed in e.RemovedItems)
				{
					var removedItem = removed as Comment;
					removedItem.IsSelected = false;
				}

				foreach (var added in e.AddedItems)
				{
					var selectedItem = added as Comment;
					if (selectedItem == null) return; //Bail, we don't know what to
					this.selectedComment = selectedItem;
					await this.chattyManager.MarkCommentRead(this.currentThread, this.selectedComment);
					var container = lv.ContainerFromItem(selectedItem);
					if (container == null) return; //Bail because the visual tree isn't created yet...
					this.currentWebViewContainer = container.FindFirstControlNamed<Grid>("container");
					((FrameworkElement)this.currentWebViewContainer).FindName("commentSection"); //Using deferred loading, we have to fully realize the post we're now going to be looking at.

					var richPostView = container.FindFirstControlNamed<RichPostView>("postView");
					selectedComment.IsSelected = true;
					if (this.currentRichPostView != null)
					{
						this.currentRichPostView.Resized -= CurrentWebView_Resized;
						this.currentRichPostView.Close();
					}

					if (richPostView != null)
					{
						this.currentRichPostView = richPostView;
						this.currentRichPostView.Resized += CurrentWebView_Resized;
						richPostView.LoadPost(WebBrowserHelper.GetPostHtml(this.selectedComment.Body, this.selectedComment.EmbeddedTypes), this.settings);
					}
				}
				this.ShortcutKeysEnabled = true;
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine("Exception in SelectedItemChanged {0}", ex);
				//var msg = new MessageDialog(string.Format("Exception in SelectedItemChanged {0}", ex));
				//await msg.ShowAsync();
				System.Diagnostics.Debugger.Break();
			}
		}

		private void SingleThreadInlineControl_KeyUp(CoreWindow sender, KeyEventArgs args)
		{
			try
			{
				if (!this.ShortcutKeysEnabled)
				{
					System.Diagnostics.Debug.WriteLine("Suppressed keypress event.");
					return;
				}

				switch (args.VirtualKey)
				{
					case VirtualKey.R:
						if (this.currentWebViewContainer == null) return;
						var button = this.currentWebViewContainer.FindFirstControlNamed<ToggleButton>("showReply");
						if (button == null) return;
						(new Microsoft.ApplicationInsights.TelemetryClient()).TrackEvent("Chatty-RPressed");
						button.IsChecked = true;
						this.ShowHideReply();
						break;
				}
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
					System.Diagnostics.Debug.WriteLine("Suppressed keypress event.");
					return;
				}

				switch (args.VirtualKey)
				{
					case VirtualKey.A:
						(new Microsoft.ApplicationInsights.TelemetryClient()).TrackEvent("Chatty-APressed");
						if (this.commentList.SelectedIndex >= 0)
						{
							this.commentList.SelectedIndex = this.commentList.SelectedIndex == 0 ? this.commentList.Items.Count - 1 : this.commentList.SelectedIndex - 1;
						}
						break;

					case VirtualKey.Z:
						(new Microsoft.ApplicationInsights.TelemetryClient()).TrackEvent("Chatty-ZPressed");
						if (this.commentList.SelectedIndex >= 0)
						{
							this.commentList.SelectedIndex = this.commentList.SelectedIndex == this.commentList.Items.Count - 1 ? 0 : this.commentList.SelectedIndex + 1;
						}
						break;
				}
				System.Diagnostics.Debug.WriteLine("Keypress event for {0}", args.VirtualKey);
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
				finally
				{
					tagButton.IsEnabled = true;
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
		}
		#endregion

		#region Helpers
		private void ShowHideReply()
		{
			if (this.currentWebViewContainer == null) return;
			var button = this.currentWebViewContainer.FindFirstControlNamed<Windows.UI.Xaml.Controls.Primitives.ToggleButton>("showReply");
			if (button == null) return;
			var replyControl = this.currentWebViewContainer.FindName("replyArea") as Controls.PostContol;
			if (replyControl == null) return;
			if (button.IsChecked.HasValue && button.IsChecked.Value)
			{
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
		#endregion
	}
}
