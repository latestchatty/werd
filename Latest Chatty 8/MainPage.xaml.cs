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
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using WinRTXamlToolkit.Controls.Extensions;

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

		public MainPage()
		{
			this.InitializeComponent();
			this.storiesData = new ObservableCollection<NewsStory>();
			this.DefaultViewModel["NewsItems"] = this.storiesData;
			this.DefaultViewModel["PinnedComments"] = LatestChattySettings.Instance.PinnedComments;
			this.selfSearch.DataContext = CoreServices.Instance;
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
			CoreServices.Instance.PostedAComment = false;

			//First time we've visited the main page - fresh launch.
			if (pageState == null)
			{
				await LatestChattySettings.Instance.LoadLongRunningSettings();

				await this.RefreshAllItems();
			}
			else
			{
				if (pageState.ContainsKey("NewsItems"))
				{
					var items = (List<NewsStory>)pageState["NewsItems"];
					foreach (var story in items)
					{
						this.storiesData.Add(story);
					}
				}
				if (pageState.ContainsKey("ScrollPosition"))
				{
					var position = (double)pageState["ScrollPosition"];
					await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
					{
						this.miniScroller.ScrollToHorizontalOffset(position);
					});
				}
			}
		}

		/// <summary>
		/// Preserves state associated with this page in case the application is suspended or the
		/// page is discarded from the navigation cache.  Values must conform to the serializaSuspensionManager.SessionStatetion
		/// requirements of <see cref=""/>.
		/// </summary>
		/// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
		protected override void SaveState(Dictionary<String, Object> pageState)
		{
			pageState.Add("NewsItems", this.storiesData.ToList());
			pageState.Add("ScrollPosition", this.miniScroller.HorizontalOffset);
		}

		void ChattyCommentClicked(object sender, ItemClickEventArgs e)
		{
			this.Frame.Navigate(typeof(ThreadView), ((Comment)e.ClickedItem).Id);
		}

		async private void RefreshClicked(object sender, RoutedEventArgs e)
		{
			await this.RefreshAllItems();
		}

		private async Task RefreshAllItems()
		{
			this.loadingProgress.IsIndeterminate = true;
			this.loadingProgress.Visibility = Windows.UI.Xaml.Visibility.Visible;

			var stories = (await NewsStoryDownloader.DownloadStories());
			this.storiesData.Clear();
			foreach (var story in stories)
			{
				this.storiesData.Add(story);
			}

			await LatestChattySettings.Instance.RefreshPinnedComments();

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

		async protected override void CorePageKeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
		{
			base.CorePageKeyDown(sender, args);
			switch (args.VirtualKey)
			{
				case Windows.System.VirtualKey.F5:
					await this.RefreshAllItems();
					break;
				case Windows.System.VirtualKey.C:
					this.Frame.Navigate(typeof(Chatty), "skipsavedload");
					break;
			}
		}

		private void SearchTextTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
		{
			Windows.ApplicationModel.Search.SearchPane.GetForCurrentView().Show("");
		}

		private void SelfSearchTextTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
		{
			Windows.ApplicationModel.Search.SearchPane.GetForCurrentView().Show(LatestChattySettings.Instance.Username);
		}

		async private void NewsStoryClicked(object sender, ItemClickEventArgs e)
		{
			var newsStory = e.ClickedItem as NewsStory;
			if (newsStory != null)
			{
				await Launcher.LaunchUriAsync(new Uri(newsStory.Url));
			}
		}
	}
}
