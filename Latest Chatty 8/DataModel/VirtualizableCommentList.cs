using Latest_Chatty_8.Networking;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Latest_Chatty_8.DataModel
{
	public class VirtualizableCommentList : ObservableCollection<Comment>, ISupportIncrementalLoading, INotifyPropertyChanged
	{
		List<Comment> cachedComments = new List<Comment>();
		int maxItems = 1000;

		public bool HasMoreItems
		{
			get { return this.Count < this.maxItems; }
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

				if ((this.Count + additionalItemsRequested > 40) && this.cachedComments.Count < this.maxItems)
				{
					//TODO: Are there always 3 pages available?  I don't think there are. But I don't have a way to get the number of pages available right now.
					//It's available in the JSON data, I'm just not grabbing it.  Should probably do that.
					System.Diagnostics.Debug.WriteLine("Not enough items in cache to satisfy loading needs. Retrieving more...");
					this.cachedComments.AddRange((await CommentDownloader.GetChattyRootComments(2)).ToList().Where(cNew => !this.cachedComments.Any(c1 => c1.Id == cNew.Id)));
					this.cachedComments.AddRange((await CommentDownloader.GetChattyRootComments(3)).ToList().Where(cNew => !this.cachedComments.Any(c1 => c1.Id == cNew.Id)));
					this.maxItems = this.cachedComments.Count;
				}
				else if(this.cachedComments.Count == 0)
				{
					System.Diagnostics.Debug.WriteLine("No items loaded.  Retrieving first set...");
					this.cachedComments.AddRange((await CommentDownloader.GetChattyRootComments(1)).ToList());
				}
				await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
					() =>
					{
						var commentsToAdd = this.cachedComments.GetRange(this.Count, Math.Min(additionalItemsRequested, this.cachedComments.Count - this.Count));
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
			base.ClearItems();
			this.cachedComments.Clear();
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
