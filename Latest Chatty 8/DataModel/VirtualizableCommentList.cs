using Latest_Chatty_8.Networking;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Latest_Chatty_8.DataModel
{
	public class VirtualizableCommentList : ObservableCollection<Comment>, ISupportIncrementalLoading
	{
		List<Comment> cachedComments = new List<Comment>();

		public bool HasMoreItems
		{
			get { return this.Count < 40 * 4; }
		}

		bool loadingItems = false;

		public Windows.Foundation.IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
		{
			if (this.loadingItems) throw new InvalidOperationException("Blah");
			this.loadingItems = true;
			return Task.Run<LoadMoreItemsResult>(async () => await this.LoadMoreItems((int)count)).AsAsyncOperation<LoadMoreItemsResult>();
		}

		async Task<LoadMoreItemsResult> LoadMoreItems(int count)
		{
			try
			{
				System.Diagnostics.Debug.WriteLine("Load more items with count {0} - current count - {1}", count, this.Count);
				if (count > this.Count)
				{
					if (count > this.cachedComments.Count)
					{
						System.Diagnostics.Debug.WriteLine("Not enough items in cache to satisfy loading needs.");
						var page = (int)(count == 0 ? 0 : count / 40) + 1;
						var newComments = (await CommentDownloader.GetChattyRootComments(page)).ToList();
						this.cachedComments.AddRange(newComments);
					}
					await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
						() =>
						{
							var currentCommentCount = this.Count;
							for (int i = currentCommentCount; i < count; i++)
							{
								this.Insert(i, this.cachedComments[i]);
							}
						});
				}
				return new LoadMoreItemsResult() { Count = (uint)this.Count };
			}
			finally
			{
				this.loadingItems = false;
			}
		}
	}
}
