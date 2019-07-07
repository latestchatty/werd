using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Autofac;
using Common;
using Latest_Chatty_8.Common;
using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Managers;
using Latest_Chatty_8.Settings;
using Latest_Chatty_8.Views;
using Microsoft.HockeyApp;
using IContainer = Autofac.IContainer;

namespace Latest_Chatty_8.Controls
{
	public sealed partial class SingleThreadInlineControl : INotifyPropertyChanged
	{
		public bool ShortcutKeysEnabled { get; set; } = true;

		public event EventHandler<LinkClickedEventArgs> LinkClicked;

		public event EventHandler<ShellMessageEventArgs> ShellMessage;

		private Comment _selectedComment;
		private ChattyManager _chattyManager;
		private AuthenticationManager _authManager;
		private IgnoreManager _ignoreManager;
		private CommentThread _currentThread;
		private bool _initialized;
		private CoreWindow _keyBindWindow;
		private WebView _splitWebView;
		private IContainer _container;

		private LatestChattySettings npcSettings;
		private LatestChattySettings Settings
		{
			get => npcSettings;
			set => SetProperty(ref npcSettings, value);
		}

		public SingleThreadInlineControl()
		{
			InitializeComponent();
		}

		public void Initialize(IContainer container)
		{
			if (_initialized) return;
			_chattyManager = container.Resolve<ChattyManager>();
			Settings = container.Resolve<LatestChattySettings>();
			_authManager = container.Resolve<AuthenticationManager>();
			_ignoreManager = container.Resolve<IgnoreManager>();
			_container = container;
			_keyBindWindow = CoreWindow.GetForCurrentThread();
			_keyBindWindow.KeyDown += SingleThreadInlineControl_KeyDown;
			_keyBindWindow.KeyUp += SingleThreadInlineControl_KeyUp;
			_initialized = true;
		}

		public async Task Close()
		{
			if (_currentThread != null)
			{
				await _chattyManager.DeselectAllPostsForCommentThread(_currentThread);
			}
			if (_keyBindWindow != null)
			{
				_keyBindWindow.KeyDown -= SingleThreadInlineControl_KeyDown;
				_keyBindWindow.KeyUp -= SingleThreadInlineControl_KeyUp;
			}
			CloseWebView();
			_initialized = false;
		}

		public void SelectPostId(int id)
		{
			if (_currentThread == null) return;
			var comment = _currentThread.Comments.SingleOrDefault(c => c.Id == id);
			if (comment == null) return;
			CommentList.SelectedValue = comment;
		}

		#region Events
		private void ControlDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
		{
			var thread = args.NewValue as CommentThread;
			_currentThread = thread;
			var shownWebView = false;

			if (thread != null)
			{
				CommentList.ItemsSource = _currentThread.Comments;
				CommentList.UpdateLayout();
				CommentList.SelectedIndex = 0;
				NavigationBar.Visibility = Visibility.Visible;
				//There appears to be a bug with the CommandBar where if it's initiallized as collapsed, the closed mode will not apply correctly.
				//So to get around that, when we display it, we're basically forcing it to redraw itself.  Not great, but it is what it is.
				NavigationBar.ClosedDisplayMode = AppBarClosedDisplayMode.Hidden;
				NavigationBar.UpdateLayout();
				NavigationBar.ClosedDisplayMode = Settings.PinnedSingleThreadAppBar ? AppBarClosedDisplayMode.Compact : AppBarClosedDisplayMode.Minimal;
				shownWebView = ShowSplitWebViewIfNecessary();
			}
			else
			{
				//Clear the list
				CommentList.ItemsSource = null;
				NavigationBar.Visibility = Visibility.Collapsed;
			}

			if (!shownWebView)
			{
				CloseWebView();
				VisualStateManager.GoToState(this, "Default", false);
			}
		}



		private async void SelectedItemChanged(object sender, SelectionChangedEventArgs e)
		{
			var lv = sender as ListView;
			if (lv == null) return; //This would be bad.
			_selectedComment = null;
			//this.SetFontSize();

			await _chattyManager.DeselectAllPostsForCommentThread(_currentThread);

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
					CommentList.SelectedIndex = -1;
					return; //Bail because the visual tree isn't created yet...
				}
				_selectedComment = selectedItem;
				await _chattyManager.MarkCommentRead(_currentThread, _selectedComment);
				var gridContainer = container.FindFirstControlNamed<Grid>("container");
				gridContainer.FindName("commentSection"); //Using deferred loading, we have to fully realize the post we're now going to be looking at.

				var richPostView = container.FindFirstControlNamed<RichPostView>("postView");
				richPostView.LoadPost(_selectedComment.Body);
				_selectedComment.IsSelected = true;
				lv.UpdateLayout();
				lv.ScrollIntoView(selectedItem);
			}
			ShortcutKeysEnabled = true;
		}

		private void SingleThreadInlineControl_KeyUp(CoreWindow sender, KeyEventArgs args)
		{
			try
			{
				if (!ShortcutKeysEnabled)
				{
					Debug.WriteLine($"{GetType().Name} - Suppressed KeyUp event.");
					return;
				}

				switch (args.VirtualKey)
				{
					case VirtualKey.R:
						if (_selectedComment == null) return;
						var controlContainer = CommentList.ContainerFromItem(_selectedComment);
						var button = controlContainer.FindFirstControlNamed<ToggleButton>("showReply");
						if (button == null) return;
						HockeyClient.Current.TrackEvent("Chatty-RPressed");
						button.IsChecked = true;
						ShowHideReply();
						break;
				}
				Debug.WriteLine($"{GetType().Name} - KeyUp event for {args.VirtualKey}");
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
				if (!ShortcutKeysEnabled)
				{
					Debug.WriteLine($"{GetType().Name} - Suppressed KeyDown event.");
					return;
				}

				switch (args.VirtualKey)
				{
					case VirtualKey.A:
						HockeyClient.Current.TrackEvent("Chatty-APressed");
						MoveToPreviousPost();
						break;

					case VirtualKey.Z:
						HockeyClient.Current.TrackEvent("Chatty-ZPressed");
						MoveToNextPost();
						break;
				}
				Debug.WriteLine($"{GetType().Name} - KeyDown event for {args.VirtualKey}");
			}
			catch (Exception)
			{
				//(new Microsoft.ApplicationInsights.TelemetryClient()).TrackException(e, new Dictionary<string, string> { { "keyCode", args.VirtualKey.ToString() } });
			}
		}

		//private void CurrentWebView_Resized(object sender, EventArgs e)
		//{
		//	CommentList.ScrollIntoView(CommentList.SelectedItem);
		//}

		private void MessageAuthorClicked(object sender, RoutedEventArgs e)
		{
			if (_selectedComment == null) return;
			var f = Window.Current.Content as Shell;
			if (f != null)
			{
				f.NavigateToPage(typeof(Messages), new Tuple<IContainer, string>(_container, _selectedComment.Author));
			}
		}

		private async void IgnoreAuthorClicked(object sender, RoutedEventArgs e)
		{
			if (_selectedComment == null) return;
			var author = _selectedComment.Author;
			var dialog = new MessageDialog($"Are you sure you want to ignore posts from { author }?");
			dialog.Commands.Add(new UICommand("Ok", async a =>
			{
				await _ignoreManager.AddIgnoredUser(author);
				ShellMessage?.Invoke(this, new ShellMessageEventArgs($"Posts from { author } will be ignored when the app is restarted"));
			}));
			dialog.Commands.Add(new UICommand("Cancel"));
			dialog.CancelCommandIndex = 1;
			dialog.DefaultCommandIndex = 1;
			await dialog.ShowAsync();
		}

		private async void LolPostClicked(object sender, RoutedEventArgs e)
		{
			if (_selectedComment == null) return;
			var controlContainer = CommentList.ContainerFromItem(_selectedComment);
			if (controlContainer != null)
			{
				var tagButton = controlContainer.FindFirstControlNamed<Button>("tagButton");
				if (tagButton == null) return;

				tagButton.IsEnabled = false;
				try
				{
					var mi = sender as MenuFlyoutItem;
					var tag = mi?.Text;
					await _selectedComment.LolTag(tag);
					HockeyClient.Current.TrackEvent("Chatty-LolTagged-" + tag);
				}
				catch (Exception)
				{
					//(new Microsoft.ApplicationInsights.TelemetryClient()).TrackException(ex);
					if (ShellMessage != null)
					{
						ShellMessage(this, new ShellMessageEventArgs("Problem tagging, try again later.", ShellMessageType.Error));
					}
				}
				finally
				{
					tagButton.IsEnabled = true;
				}
			}
		}

		private async void LolTagTapped(object sender, TappedRoutedEventArgs e)
		{
			Button s = null;
			try
			{
				s = sender as Button;
				if (s == null) return;
				s.IsEnabled = false;
				if (_selectedComment == null) return;
				var tag = s.Tag as string;
				HockeyClient.Current.TrackEvent("ViewedTagCount-" + tag);
				var lolUrl = Locations.GetLolTaggersUrl(_selectedComment.Id, tag);
				var response = await JsonDownloader.DownloadObject(lolUrl);
				var names = string.Join(Environment.NewLine, response["data"][0]["usernames"].Select(a => a.ToString()).OrderBy(a => a));
				var flyout = new Flyout();
				var tb = new TextBlock();
				tb.Text = names;
				flyout.Content = tb;
				flyout.ShowAt(s);
			}
			catch (Exception)
			{
				//(new TelemetryClient()).TrackException(ex);
				ShellMessage?.Invoke(this, new ShellMessageEventArgs("Error retrieving taggers. Try again later.", ShellMessageType.Error));
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
			ShowHideReply();
		}

		private void ReplyControl_TextBoxLostFocus(object sender, EventArgs e)
		{
			ShortcutKeysEnabled = true;
		}

		private void ReplyControl_TextBoxGotFocus(object sender, EventArgs e)
		{
			ShortcutKeysEnabled = false;
		}

		private void ReplyControl_ShellMessage(object sender, ShellMessageEventArgs args)
		{
			if (ShellMessage != null)
			{
				ShellMessage(sender, args);
			}
		}

		private void ReplyControl_Closed(object sender, EventArgs e)
		{
			var control = (PostContol) sender;
			control.Closed -= ReplyControl_Closed;
			control.TextBoxGotFocus -= ReplyControl_TextBoxGotFocus;
			control.TextBoxLostFocus -= ReplyControl_TextBoxLostFocus;
			control.ShellMessage -= ReplyControl_ShellMessage;
			if (_selectedComment == null) return;
			var controlContainer = CommentList.ContainerFromItem(_selectedComment);
			if (controlContainer == null) return;
			var button = controlContainer.FindFirstControlNamed<ToggleButton>("showReply");
			if (button == null) return;
			button.IsChecked = false;
			ShortcutKeysEnabled = true;
		}

		private void CopyPostLinkClicked(object sender, RoutedEventArgs e)
		{
			var button = sender as Button;
			if (button == null) return;
			var comment = button.DataContext as Comment;
			if (comment == null) return;
			var dataPackage = new DataPackage();
			dataPackage.SetText(string.Format("http://www.shacknews.com/chatty?id={0}#item_{0}", comment.Id));
			Clipboard.SetContent(dataPackage);
			if (ShellMessage != null)
			{
				ShellMessage(this, new ShellMessageEventArgs("Link copied to clipboard."));
			}
		}

		private void RichPostLinkClicked(object sender, LinkClickedEventArgs e)
		{
			if (LinkClicked != null)
			{
				LinkClicked(this, e);
			}
		}

		private void PreviousNavigationButtonClicked(object sender, RoutedEventArgs e)
		{
			MoveToPreviousPost();
		}

		private void NextNavigationButtonClicked(object sender, RoutedEventArgs e)
		{
			MoveToNextPost();
		}

		private async void MarkAllReadButtonClicked(object sender, RoutedEventArgs e)
		{
			if (_currentThread == null) return;
			await _chattyManager.MarkCommentThreadRead(_currentThread);
		}
		#endregion

		#region Helpers
		private bool ShowSplitWebViewIfNecessary()
		{
			var shownWebView = false;
			if (!Settings.DisableNewsSplitView)
			{
				var firstComment = _currentThread.Comments.FirstOrDefault();
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
								FindName(nameof(WebViewContainer)); //Realize the container since it's deferred.
								VisualStateManager.GoToState(this, "WebViewShown", false);
								_splitWebView = new WebView(WebViewExecutionMode.SeparateThread);
								Grid.SetRow(_splitWebView, 0);
								WebViewContainer.Children.Add(_splitWebView);
								_splitWebView.Navigate(storyUrl);
								shownWebView = true;
							}
						}
					}
				}
				catch
				{
					// ignored
				}
			}

			return shownWebView;
		}

		private void CloseWebView()
		{
			if (_splitWebView != null)
			{
				_splitWebView.Stop();
				_splitWebView.NavigateToString("");
				WebViewContainer.Children.Remove(_splitWebView);
				_splitWebView = null;
			}
		}

		private void ShowHideReply()
		{
			if (_selectedComment == null) return;
			var controlContainer = CommentList.ContainerFromItem(_selectedComment);
			if (controlContainer == null) return;
			var button = controlContainer.FindFirstControlNamed<ToggleButton>("showReply");
			if (button == null) return;
			var commentSection = controlContainer.FindFirstControlNamed<Grid>("commentSection");
			if (commentSection == null) return;
			commentSection.FindName("replyArea"); //Lazy load
			var replyControl = commentSection.FindFirstControlNamed<PostContol>("replyControl");
			if (replyControl == null) return;
			if (button.IsChecked.HasValue && button.IsChecked.Value)
			{
				replyControl.Visibility = Visibility.Visible;
				replyControl.SetShared(_authManager, Settings);
				replyControl.SetFocus();
				replyControl.Closed += ReplyControl_Closed;
				replyControl.TextBoxGotFocus += ReplyControl_TextBoxGotFocus;
				replyControl.TextBoxLostFocus += ReplyControl_TextBoxLostFocus;
				replyControl.ShellMessage += ReplyControl_ShellMessage;
				replyControl.UpdateLayout();
				CommentList.ScrollIntoView(CommentList.SelectedItem);
			}
			else
			{
				ShortcutKeysEnabled = true;
				replyControl.Closed -= ReplyControl_Closed;
				replyControl.TextBoxGotFocus -= ReplyControl_TextBoxGotFocus;
				replyControl.TextBoxLostFocus -= ReplyControl_TextBoxLostFocus;
				replyControl.ShellMessage -= ReplyControl_ShellMessage;
			}
		}
		private void MoveToPreviousPost()
		{
			if (CommentList.SelectedIndex >= 0)
			{
				if (CommentList.Items != null)
				{
					CommentList.SelectedIndex = CommentList.SelectedIndex == 0
						? CommentList.Items.Count - 1
						: CommentList.SelectedIndex - 1;
				}
				else
				{
					CommentList.SelectedIndex = 0;
				}
			}
		}

		private void MoveToNextPost()
		{
			if (CommentList.SelectedIndex >= 0)
			{
				CommentList.SelectedIndex = CommentList.Items != null && CommentList.SelectedIndex == CommentList.Items.Count - 1 ? 0 : CommentList.SelectedIndex + 1;
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


	}
}
