using Latest_Chatty_8.Common;
using Latest_Chatty_8.Data;
using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Networking;
using Latest_Chatty_8.Settings;
using Latest_Chatty_8.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Items Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234233

namespace Latest_Chatty_8
{
	/// <summary>
	/// A page that displays a collection of item previews.  In the Split Application this page
	/// is used to display and select one of the available groups.
	/// </summary>
	public sealed partial class MainPage : Latest_Chatty_8.Common.LayoutAwarePage
	{
		private readonly ObservableCollection<NewsStory> storiesData;
		private readonly ObservableCollection<Comment> pinnedComments;
		private readonly ObservableCollection<Comment> replyComments;
		private readonly ObservableCollection<Comment> myComments;
		private int readingChattyCommentId;

		public MainPage()
		{
			this.InitializeComponent();
			this.storiesData = new ObservableCollection<NewsStory>();
			this.pinnedComments = new ObservableCollection<Comment>();
			this.replyComments = new ObservableCollection<Comment>();
			this.myComments = new ObservableCollection<Comment>();
			this.DefaultViewModel["Items"] = this.storiesData;
			this.DefaultViewModel["PinnedComments"] = this.pinnedComments;
			this.DefaultViewModel["ReplyComments"] = this.replyComments;
			this.DefaultViewModel["MyComments"] = this.myComments;
		}

		/// <summary>
		/// Populates the page with content passed during navigation.  Any saved state is also
		/// provided when recreating a page from a prior session.
		/// </summary>
		/// <param name="navigationParameter">The parameter value passed to
		/// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
		/// </param>
		/// <param name="pageState">A dictionary of state preserved by this page during an earlier
		/// session.  This will be null the first time a page is visited.</param>
		async protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
		{
			CoreServices.Instance.ReturningFromThreadView = false;
			CoreServices.Instance.PostedAComment = false;
			var scrollToPosition = 0d;

			if (pageState != null)
			{
				if (pageState.ContainsKey("Items"))
				{
					var items = (List<NewsStory>)pageState["Items"];
					this.storiesData.Clear();
					foreach (var story in items)
					{
						this.storiesData.Add(story);
					}
				}
				if (pageState.ContainsKey("PinnedComments"))
				{
					var items = (List<Comment>)pageState["PinnedComments"];
					this.pinnedComments.Clear();
					foreach (var c in items)
					{
						this.pinnedComments.Add(c);
					}
				}
				if (pageState.ContainsKey("ReplyComments"))
				{
					var items = (List<Comment>)pageState["ReplyComments"];
					this.replyComments.Clear();
					foreach (var c in items)
					{
						this.replyComments.Add(c);
					}
				}
				if (pageState.ContainsKey("MyComments"))
				{
					var items = (List<Comment>)pageState["MyComments"];
					this.myComments.Clear();
					foreach (var c in items)
					{
						this.myComments.Add(c);
					}
				}
				if(pageState.ContainsKey("ReadingChattyCommentId"))
				{
					this.readingChattyCommentId = (int)pageState["ReadingChattyCommentId"];
				}
				if (pageState.ContainsKey("MainScrollLocation"))
				{
					scrollToPosition = (double)pageState["MainScrollLocation"];
				}
			}

			if (this.storiesData.Count == 0)
			{
				var stories = (await NewsStoryDownloader.DownloadStories()).Take(6);
				this.storiesData.Clear();
				foreach (var story in stories)
				{
					this.storiesData.Add(story);
				}
			}

			if (this.pinnedComments.Count == 0)
			{
				this.pinnedComments.Clear();
				foreach (var commentId in LatestChattySettings.Instance.PinnedCommentIDs)
				{
					this.pinnedComments.Add(await CommentDownloader.GetComment(commentId, false));
				}
			}

			if (this.replyComments.Count == 0)
			{
				var comments = await CommentDownloader.GetReplyComments();
				this.replyComments.Clear();
				foreach (var c in comments)
				{
					this.replyComments.Add(c);
				}
			}

			if (this.myComments.Count == 0)
			{
				var comments = await CommentDownloader.MyComments();
				this.myComments.Clear();
				foreach (var c in comments)
				{
					this.myComments.Add(c);
				}
			}

			Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () => this.mainScroller.ScrollToHorizontalOffset(scrollToPosition));
			this.loadingProgress.IsIndeterminate = false;
			this.loadingProgress.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
		}

		/// <summary>
		/// Preserves state associated with this page in case the application is suspended or the
		/// page is discarded from the navigation cache.  Values must conform to the serializaSuspensionManager.SessionStatetion
		/// requirements of <see cref=""/>.
		/// </summary>
		/// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
		protected override void SaveState(Dictionary<String, Object> pageState)
		{
			pageState.Add("Items", this.storiesData.ToList());
			pageState.Add("PinnedComments", this.pinnedComments.ToList());
			pageState.Add("MainScrollLocation", this.mainScroller.HorizontalOffset);
			pageState.Add("ReadingChattyCommentId", this.readingChattyCommentId);
		}

		void ChattyCommentClicked(object sender, ItemClickEventArgs e)
		{
			this.readingChattyCommentId = ((Comment)e.ClickedItem).Id;
			this.Frame.Navigate(typeof(ThreadView), this.readingChattyCommentId);
		}

		/// <summary>
		/// Invoked when an item is clicked.
		/// </summary>
		/// <param name="sender">The GridView (or ListView when the application is snapped)
		/// displaying the item clicked.</param>
		/// <param name="e">Event data that describes the item clicked.</param>
		void ItemView_ItemClick(object sender, ItemClickEventArgs e)
		{
			// Navigate to the appropriate destination page, configuring the new page
			// by passing required information as a navigation parameter
			var groupId = ((SampleDataGroup)e.ClickedItem).UniqueId;
			this.Frame.Navigate(typeof(SplitPage), groupId);
		}

		private void RefreshClicked(object sender, RoutedEventArgs e)
		{
			this.RefreshAllItems();
		}

		async private void RefreshAllItems()
		{
			this.loadingProgress.IsIndeterminate = true;
			this.loadingProgress.Visibility = Windows.UI.Xaml.Visibility.Visible;

			var stories = (await NewsStoryDownloader.DownloadStories()).Take(6);
			this.storiesData.Clear();
			foreach (var story in stories)
			{
				this.storiesData.Add(story);
			}

			this.pinnedComments.Clear();
			foreach (var commentId in LatestChattySettings.Instance.PinnedCommentIDs)
			{
				this.pinnedComments.Add(await CommentDownloader.GetComment(commentId));
			}

			var comments = await CommentDownloader.GetReplyComments();
			this.replyComments.Clear();
			foreach (var c in comments)
			{
				this.replyComments.Add(c);
			}

			comments = await CommentDownloader.MyComments();
			this.myComments.Clear();
			foreach (var c in comments)
			{
				this.myComments.Add(c);
			}

			this.loadingProgress.IsIndeterminate = false;
			this.loadingProgress.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
		}

		private void ChattyTextTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
		{
			this.Frame.Navigate(typeof(Chatty), "skipsavedload");
		}

		private void MessagesTextTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
		{

		}

		private void SearchTextTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
		{

		}
	}
}
