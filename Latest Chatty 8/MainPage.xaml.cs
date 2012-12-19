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
using Windows.UI.Core;
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

		#region Constructor
		public MainPage()
		{
			this.InitializeComponent();
			this.storiesData = new ObservableCollection<NewsStory>();
			this.DefaultViewModel["NewsItems"] = this.storiesData;
			this.DefaultViewModel["PinnedComments"] = LatestChattySettings.Instance.PinnedComments;
			this.selfSearch.DataContext = CoreServices.Instance;
		}
		#endregion

		#region Load and Save State
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

		protected override void SaveState(Dictionary<String, Object> pageState)
		{
			pageState.Add("NewsItems", this.storiesData.ToList());
			pageState.Add("ScrollPosition", this.miniScroller.HorizontalOffset);
		}
		#endregion

		#region Event Handlers
		private void ChattyTextTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
		{
			this.Frame.Navigate(typeof(Chatty), "skipsavedload");
		}

		private void MessagesTextTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
		{

		}

		void ChattyCommentClicked(object sender, ItemClickEventArgs e)
		{
			this.Frame.Navigate(typeof(ThreadView), ((Comment)e.ClickedItem).Id);
		}

		async private void RefreshClicked(object sender, RoutedEventArgs e)
		{
			await this.RefreshAllItems();
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
		#endregion

		#region Overrides
		async protected override Task<bool> CorePageKeyActivated(CoreDispatcher sender, AcceleratorKeyEventArgs args)
		{
			await base.CorePageKeyActivated(sender, args);
			//If it's not a key down event, we don't care about it.
			if (args.EventType != CoreAcceleratorKeyEventType.SystemKeyDown &&
				 args.EventType != CoreAcceleratorKeyEventType.KeyDown)
			{
				return true;
			}

			switch (args.VirtualKey)
			{
				case Windows.System.VirtualKey.F5:
					await this.RefreshAllItems();
					break;
				case Windows.System.VirtualKey.C:
					this.Frame.Navigate(typeof(Chatty), "skipsavedload");
					break;
			}
			return true;
		}
		#endregion

		#region Private Helpers
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
			await CoreServices.Instance.ClearTile(false);

			this.loadingProgress.IsIndeterminate = false;
			this.loadingProgress.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
		}
		#endregion
	}
}
