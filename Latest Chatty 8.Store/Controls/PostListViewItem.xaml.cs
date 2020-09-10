using Autofac;
using Common;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Werd.Common;
using Werd.DataModel;
using Werd.Managers;
using Werd.Settings;
using Werd.Views;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using IContainer = Autofac.IContainer;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Werd.Controls
{
	public class CommentEventArgs : EventArgs
	{
		public Comment Comment { get; private set; }

		public CommentEventArgs(Comment comment)
		{
			Comment = comment;
		}
	}

	public sealed partial class PostListViewItem : UserControl, INotifyPropertyChanged
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

		private Comment _comment;
		public Comment Comment
		{
			get => _comment;
			set
			{
				SetProperty(ref _comment, value);
				this.Bindings.Update();
			}
		}

		private bool _canThreadTruncate;
		public bool CanThreadTruncate
		{
			get => _canThreadTruncate;
			set => SetProperty(ref _canThreadTruncate, value);
		}

		public event EventHandler<LinkClickedEventArgs> LinkClicked;

		public event EventHandler<ShellMessageEventArgs> ShellMessage;

		public event EventHandler<CommentEventArgs> ShowReply;
		public event EventHandler<CommentEventArgs> UntruncateThread;

		private readonly ChattyManager _chattyManager;
		private readonly AuthenticationManager _authManager;
		private readonly MessageManager _messageManager;
		private readonly IgnoreManager _ignoreManager;
		private readonly LatestChattySettings _settings;

		public PostListViewItem()
		{
			this.InitializeComponent();
			_settings = AppGlobal.Settings;
			_chattyManager = AppGlobal.Container.Resolve<ChattyManager>();
			_authManager = AppGlobal.Container.Resolve<AuthenticationManager>();
			_messageManager = AppGlobal.Container.Resolve<MessageManager>();
			_ignoreManager = AppGlobal.Container.Resolve<IgnoreManager>();
		}

		private async void PreviewFlyoutOpened(object _, object _1)
		{
			await _chattyManager.MarkCommentRead(Comment).ConfigureAwait(true);
		}

		private void UntruncateThreadClicked(object sender, RoutedEventArgs e)
		{
			UntruncateThread?.Invoke(this, new CommentEventArgs(Comment));
		}

		private void ShowReplyClicked(object sender, RoutedEventArgs e)
		{
			ShowReply?.Invoke(this, new CommentEventArgs(Comment));
		}

		private async void LolPostClicked(object sender, RoutedEventArgs e)
		{
			var mi = sender as MenuFlyoutItem;
			if (mi == null) return;
			try
			{
				mi.IsEnabled = false;
				var tag = mi?.Text;
				await Comment.LolTag(tag).ConfigureAwait(true);
				await DebugLog.AddMessage("Chatty-LolTagged-" + tag).ConfigureAwait(true);
			}
			catch (Exception ex)
			{
				await DebugLog.AddException(string.Empty, ex).ConfigureAwait(true);
				ShellMessage?.Invoke(this, new ShellMessageEventArgs("Problem tagging, try again later.", ShellMessageType.Error));
			}
			finally
			{
				mi.IsEnabled = true;
			}
		}

		private async void ReportPostClicked(object sender, RoutedEventArgs e)
		{
			if (!_authManager.LoggedIn)
			{
				ShellMessage?.Invoke(this, new ShellMessageEventArgs("You must be logged in to report a post.", ShellMessageType.Error));
				return;
			}

			var dialog = new MessageDialog("Are you sure you want to report this post for violating community guidelines?");
			var comment = ((sender as FrameworkElement)?.DataContext as Comment);
			if (comment == null) return;
			dialog.Commands.Add(new UICommand("Yes", async _ =>
			{
				await _messageManager.SendMessage(
					"duke nuked",
					$"Reporting Post Id {comment.Id}",
					$"I am reporting the following post via the Werd in-app reporting feature.  Please take a look at it to ensure it meets community guidelines.  Thanks!  https://www.shacknews.com/chatty?id={comment.Id}#item_{comment.Id}").ConfigureAwait(true);
				ShellMessage?.Invoke(this, new ShellMessageEventArgs("Post reported.", ShellMessageType.Message));
			}));
			dialog.Commands.Add(new UICommand("Cancel"));
			dialog.CancelCommandIndex = 1;
			dialog.DefaultCommandIndex = 1;
			await dialog.ShowAsync();
		}

		private void CopyPostLinkClicked(object sender, RoutedEventArgs e)
		{
			var dataPackage = new DataPackage();
			dataPackage.SetText($"http://www.shacknews.com/chatty?id={Comment.Id}#item_{Comment.Id}");
			_settings.LastClipboardPostId = Comment.Id;
			Clipboard.SetContent(dataPackage);
			ShellMessage?.Invoke(this, new ShellMessageEventArgs("Link copied to clipboard."));
		}

		private async void IgnoreAuthorClicked(object sender, RoutedEventArgs e)
		{
			var author = Comment.Author;
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
					new Tuple<IContainer, Uri>
						(AppGlobal.Container,
						new Uri($"https://www.shacknews.com/search?chatty=1&type=4&chatty_term=&chatty_user={Uri.EscapeUriString(Comment.Author)}& chatty_author=&chatty_filter=all&result_sort=postdate_desc")
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
					new Tuple<IContainer, Uri>
						(AppGlobal.Container,
						new Uri($"https://www.shacknews.com/search?chatty=1&type=4&chatty_term=&chatty_user=&chatty_author={Uri.EscapeUriString(Comment.Author)}&chatty_filter=all&result_sort=postdate_desc")
						)
				);
			}
		}

		private void MessageAuthorClicked(object sender, RoutedEventArgs e)
		{
			if (Window.Current.Content is Shell f)
			{
				f.NavigateToPage(typeof(Messages), new Tuple<IContainer, string>(AppGlobal.Container, Comment.Author));
			}
		}

		private void ViewAuthorModHistoryClicked(object sender, RoutedEventArgs e)
		{
			var author = Comment.Author;
			if (Window.Current.Content is Shell f)
			{
				f.NavigateToPage(typeof(ModToolsWebView), new Tuple<IContainer, Uri>(AppGlobal.Container, new Uri($"https://www.shacknews.com/moderators/check?username={author}")));
			}
		}

		private async void ModeratePostClicked(object sender, RoutedEventArgs e)
		{
			var menuFlyoutItem = sender as MenuFlyoutItem;
			if (menuFlyoutItem is null) return;

			if (await Comment.Moderate(menuFlyoutItem.Text).ConfigureAwait(true))
			{
				ShellMessage?.Invoke(this, new ShellMessageEventArgs("Post successfully moderated."));
			}
			else
			{
				ShellMessage?.Invoke(this, new ShellMessageEventArgs("Something went wrong while moderating. You probably don't have mod permissions. Stop it.", ShellMessageType.Error));
			}
		}
	}
}
