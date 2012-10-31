using Latest_Chatty_8.Data;
using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Networking;
using Latest_Chatty_8.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

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
		private readonly ObservableCollection<Comment> chattyComments;

		public MainPage()
		{
			this.InitializeComponent();
			this.storiesData = new ObservableCollection<NewsStory>();
			this.chattyComments = new ObservableCollection<Comment>();
			this.DefaultViewModel["Items"] = this.storiesData;
			this.DefaultViewModel["ChattyComments"] = this.chattyComments;
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

			if (pageState != null)
			{
				if (pageState.ContainsKey("Items"))
				{
					var newsStories = (List<NewsStory>)pageState["Items"];
					this.storiesData.Clear();
					foreach (var story in newsStories)
					{
						this.storiesData.Add(story);
					}
				}
				if (pageState.ContainsKey("ChattyComments"))
				{
					var newsStories = (List<Comment>)pageState["ChattyComments"];
					this.chattyComments.Clear();
					foreach (var c in newsStories)
					{
						this.chattyComments.Add(c);
					}
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

			if (this.chattyComments.Count == 0)
			{
				var comments = await CommentDownloader.GetChattyRootComments();
				this.chattyComments.Clear();
				foreach (var c in comments)
				{
					this.chattyComments.Add(c);
				}
			}

			this.loadingProgress.IsIndeterminate = false;
			this.loadingProgress.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
		}

		/// <summary>
		/// Preserves state associated with this page in case the application is suspended or the
		/// page is discarded from the navigation cache.  Values must conform to the serialization
		/// requirements of <see cref="SuspensionManager.SessionState"/>.
		/// </summary>
		/// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
		protected override void SaveState(Dictionary<String, Object> pageState)
		{
			pageState.Add("Items", this.storiesData.ToList());
			pageState.Add("ChattyComments", this.chattyComments.ToList());
		}

		void ChattyCommentClicked(object sender, ItemClickEventArgs e)
		{
			this.Frame.Navigate(typeof(ThreadView), ((Comment)e.ClickedItem).Id);
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

			var comments = await CommentDownloader.GetChattyRootComments();
			this.chattyComments.Clear();
			foreach (var c in comments)
			{
				this.chattyComments.Add(c);
			}

			this.loadingProgress.IsIndeterminate = false;
			this.loadingProgress.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
		}
	}
}
