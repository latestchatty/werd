using Latest_Chatty_8.Networking;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Latest_Chatty_8.DataModel
{
    [CollectionDataContract]
	public class VirtualizableCommentList : ObservableCollection<Comment>, ISupportIncrementalLoading, INotifyPropertyChanged
	{
        [DataMember]
        public List<Comment> CachedComments { get; set; }
        [DataMember]
        int PageCount { get; set; }
        [DataMember]
        int LastFetchedPage { get; set; }

        public VirtualizableCommentList()
        {
            this.CachedComments = new List<Comment>();
            this.PageCount = 1;
            this.LastFetchedPage = 0;
        }

		public bool HasMoreItems
		{
			//If we've got all pages and we've retrieved all the items from the cache, there's nothing more available
			get { return (this.LastFetchedPage < this.PageCount) || (this.Count < this.CachedComments.Count); }
		}

		private bool npcIsLoading;
		public bool IsLoading
		{
			get { return npcIsLoading; }
			set { this.SetProperty(ref this.npcIsLoading, value); }
		}

		public Windows.Foundation.IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
		{
			if (this.IsLoading)
			{
				return null;
			}
			this.IsLoading = true;
			return AsyncInfo.Run((c) => this.LoadMoreItems(c, (int)count));
		}

		async Task<LoadMoreItemsResult> LoadMoreItems(CancellationToken c, int additionalItemsRequested)
		{
			try
			{
				System.Diagnostics.Debug.WriteLine("Load more items, current count - {0} - we want {1} more", this.Count, additionalItemsRequested);
				var totalItemsNeeded = this.Count + additionalItemsRequested;

				if ((totalItemsNeeded > this.CachedComments.Count))
				{
					//Get as many pages as we need to get to satisfy the loading requirements
					var pagesToFetch = (int)Math.Ceiling((totalItemsNeeded - this.CachedComments.Count) / 40d);
					for (int i = this.LastFetchedPage + 1; ((i < (this.LastFetchedPage + pagesToFetch + 1)) && (i <= this.PageCount)); i++)
					{
						await CoreServices.Instance.ClearTile(true);
						System.Diagnostics.Debug.WriteLine("Fetching comments for page {0}", i);
						var result = (await CommentDownloader.GetChattyRootComments(i));
						//This will handle if there are more pages avaialble now.
						this.PageCount = result.Item1;
						//Make sure we don't add duplicate stories
						this.CachedComments.AddRange(result.Item2.Where(cNew => !this.CachedComments.Any(c1 => c1.Id == cNew.Id)).ToList());	
					}
					this.LastFetchedPage += pagesToFetch;
				}

				var commentsToAdd = this.CachedComments.GetRange(this.Count, Math.Min(additionalItemsRequested, this.CachedComments.Count - this.Count));
				await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low,
					() =>
					{		
						foreach (var cAdd in commentsToAdd)
						{
							this.Add(cAdd);
						}
					});
				return new LoadMoreItemsResult() { Count = (uint)this.Count };
			}
			finally
			{
				this.IsLoading = false;
			}
		}

		protected override void ClearItems()
		{
			this.CachedComments.Clear();
			this.PageCount = 1;
			this.LastFetchedPage = 0;
			base.ClearItems();
		}

		#region NotifyPropertyChanged
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
