using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Shared.Networking;
using Latest_Chatty_8.Shared.Settings;
using Latest_Chatty_8.Views;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Search Contract item template is documented at http://go.microsoft.com/fwlink/?LinkId=234240

namespace Latest_Chatty_8
{
	/// <summary>
	/// This page displays search results when a global search is directed to this application.
	/// </summary>
	public sealed partial class Search : Latest_Chatty_8.Shared.LayoutAwarePage
	{

		public Search()
		{
			this.InitializeComponent();
		}

		private Dictionary<string, IEnumerable<Comment>> searchResults = new Dictionary<string, IEnumerable<Comment>>();

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
			try
			{
				this.loadingProgress.IsIndeterminate = true;
				this.loadingProgress.Visibility = Windows.UI.Xaml.Visibility.Visible;
				LatestChattySettings.Instance.CreateInstance();
				await CoreServices.Instance.Initialize();

				var queryText = Uri.EscapeUriString(navigationParameter as String);

				var filterList = new List<Filter>();
				var chattyComments = (await CommentDownloader.SearchComments("?terms=" + queryText)).ToList();
				chattyComments.AddRange((await CommentDownloader.SearchComments("?terms=" + queryText + "&page=2")).ToList());

				var authorComments = (await CommentDownloader.SearchComments("?author=" + queryText)).ToList();
				authorComments.AddRange((await CommentDownloader.SearchComments("?author=" + queryText + "&page=2")).ToList());

				var parentAuthorComments = (await CommentDownloader.SearchComments("?parent_author=" + queryText)).ToList();
				parentAuthorComments.AddRange((await CommentDownloader.SearchComments("?parent_author=" + queryText + "&page=2")).ToList());

				this.searchResults.Add("Chatty", chattyComments);
				this.searchResults.Add("Author", authorComments);
				this.searchResults.Add("Parent Author", parentAuthorComments);

				filterList.Add(new Filter("Chatty", chattyComments.Count(), true));
				filterList.Add(new Filter("Author", authorComments.Count(), false));
				filterList.Add(new Filter("Parent Author", parentAuthorComments.Count(), false));

				// Communicate results through the view model
				this.DefaultViewModel["QueryText"] = '\u201c' + queryText + '\u201d';
				this.DefaultViewModel["Filters"] = filterList;
				this.DefaultViewModel["ShowFilters"] = filterList.Count > 1;
			}
			catch (Exception e)
			{
				this.DefaultViewModel["ExceptionText"] = string.Format("There was a problem searching.{0}{1}", Environment.NewLine, e);
			}
			finally
			{
				this.loadingProgress.IsIndeterminate = false;
				this.loadingProgress.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
			}
		}

		/// <summary>
		/// Invoked when a filter is selected using the ComboBox in snapped view state.
		/// </summary>
		/// <param name="sender">The ComboBox instance.</param>
		/// <param name="e">Event data describing how the selected filter was changed.</param>
		void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// Determine what filter was selected
			var selectedFilter = e.AddedItems.FirstOrDefault() as Filter;
			if (selectedFilter != null)
			{
				// Mirror the results into the corresponding Filter object to allow the
				// RadioButton representation used when not snapped to reflect the change
				selectedFilter.Active = true;

				this.DefaultViewModel["Results"] = this.searchResults[selectedFilter.Name];

				// Ensure results are found
				object results;
				ICollection resultsCollection;
				if (this.DefaultViewModel.TryGetValue("Results", out results) &&
					 (resultsCollection = results as ICollection) != null &&
					 resultsCollection.Count != 0)
				{
					VisualStateManager.GoToState(this, "ResultsFound", true);
					return;
				}
			}

			// Display informational text when there are no search results.
			VisualStateManager.GoToState(this, "NoResultsFound", true);
		}

		/// <summary>
		/// Invoked when a filter is selected using a RadioButton when not snapped.
		/// </summary>
		/// <param name="sender">The selected RadioButton instance.</param>
		/// <param name="e">Event data describing how the RadioButton was selected.</param>
		void Filter_Checked(object sender, RoutedEventArgs e)
		{
			// Mirror the change into the CollectionViewSource used by the corresponding ComboBox
			// to ensure that the change is reflected when snapped
			if (filtersViewSource.View != null)
			{
				var filter = (sender as FrameworkElement).DataContext;
				filtersViewSource.View.MoveCurrentTo(filter);
			}
		}

		/// <summary>
		/// View model describing one of the filters available for viewing search results.
		/// </summary>
		private sealed class Filter : Latest_Chatty_8.Shared.BindableBase
		{
			private String _name;
			private int _count;
			private bool _active;

			public Filter(String name, int count, bool active = false)
			{
				this.Name = name;
				this.Count = count;
				this.Active = active;
			}

			public override String ToString()
			{
				return Description;
			}

			public String Name
			{
				get { return _name; }
				set { if (this.SetProperty(ref _name, value)) this.OnPropertyChanged("Description"); }
			}

			public int Count
			{
				get { return _count; }
				set { if (this.SetProperty(ref _count, value)) this.OnPropertyChanged("Description"); }
			}

			public bool Active
			{
				get { return _active; }
				set { this.SetProperty(ref _active, value); }
			}

			public String Description
			{
				get { return String.Format("{0} ({1})", _name, _count); }
			}
		}

		private void ClickedResult(object sender, ItemClickEventArgs e)
		{
			//this.Frame.Navigate(typeof(InlineThreadView), ((Comment)e.ClickedItem).Id);
			//+pageState.Add("NewsItems", this.storiesData.ToList());
		}
	}
}
