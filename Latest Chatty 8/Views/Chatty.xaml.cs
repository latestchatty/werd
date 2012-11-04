using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Networking;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace Latest_Chatty_8.Views
{
	/// <summary>
	/// A basic page that provides characteristics common to most applications.
	/// </summary>
	public sealed partial class Chatty : Latest_Chatty_8.Common.LayoutAwarePage
	{

		private readonly ObservableCollection<Comment> chattyComments;
		private readonly ObservableCollection<Comment> threadComments;

		public Chatty()
		{
			this.InitializeComponent();
			this.chattyComments = new ObservableCollection<Comment>();
			this.threadComments = new ObservableCollection<Comment>();
			this.DefaultViewModel["ChattyComments"] = this.chattyComments;
			this.DefaultViewModel["ThreadComments"] = this.threadComments;
			this.chattyCommentList.SelectionChanged += ChattyCommentListSelectionChanged;
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
			var skipSavedLoad = ((!CoreServices.Instance.ReturningFromThreadView) && (!CoreServices.Instance.PostedAComment));
			CoreServices.Instance.ReturningFromThreadView = false;
			CoreServices.Instance.PostedAComment = false;

			if (!skipSavedLoad)
			{
				if (pageState != null)
				{
					if (pageState.ContainsKey("ChattyComments"))
					{
						var ps = pageState["ChattyComments"] as List<Comment>;
						if (ps != null)
						{
							foreach (var c in ps)
							{
								this.chattyComments.Add(c);
							}
						}
					}
					if (pageState.ContainsKey("SelectedChattyComment"))
					{
						var ps = pageState["SelectedChattyComment"] as Comment;
						if (ps != null)
						{
							var newSelectedComment = this.chattyComments.SingleOrDefault(c => c.Id == ps.Id);
							if (newSelectedComment != null)
							{
								this.chattyCommentList.SelectedItem = newSelectedComment;
								this.chattyCommentList.ScrollIntoView(newSelectedComment);
							}
						}
					}
				}
			}

			if (this.chattyComments.Count == 0)
			{
				this.RefreshChattyComments();
			}
		}

		async private void RefreshChattyComments()
		{
			this.SetLoading();
			var comments = await CommentDownloader.GetChattyRootComments();
			this.chattyComments.Clear();
			this.threadComments.Clear();
			foreach (var c in comments)
			{
				this.chattyComments.Add(c);
			}
			this.UnsetLoading();
		}

		async void ChattyCommentListSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			this.GetSelectedThread();
		}

		/// <summary>
		/// Preserves state associated with this page in case the application is suspended or the
		/// page is discarded from the navigation cache.  Values must conform to the serialization
		/// requirements of <see cref="SuspensionManager.SessionState"/>.
		/// </summary>
		/// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
		protected override void SaveState(Dictionary<String, Object> pageState)
		{
			pageState["ChattyComments"] = this.chattyComments.ToList();
			pageState["SelectedChattyComment"] = this.chattyCommentList.SelectedItem as Comment;
			pageState["ThreadComments"] = this.threadComments.ToList();
			pageState["SelectedThreadComment"] = this.threadCommentList.SelectedItem as Comment;
		}

		async private void GetSelectedThread()
		{
			var selectedChattyComment = this.chattyCommentList.SelectedItem as Comment;
			if (selectedChattyComment != null)
			{
				this.SetLoading();
				var rootComment = await CommentDownloader.GetComment(selectedChattyComment.Id);


				this.threadComments.Clear();
				foreach (var c in rootComment.FlattenedComments.ToList())
				{
					this.threadComments.Add(c);
				}

				this.threadCommentList.SelectedItem = rootComment;
				this.UnsetLoading();
			}
		}

		private void SetLoading()
		{
			this.loadingBar.IsIndeterminate = true;
			this.loadingBar.Visibility = Visibility.Visible;
		}

		private void UnsetLoading()
		{
			this.loadingBar.IsIndeterminate = false;
			this.loadingBar.Visibility = Visibility.Collapsed;
		}

		private void SnappedCommentListItemClicked(object sender, ItemClickEventArgs e)
		{
			var clickedComment = e.ClickedItem as Comment;
			if (clickedComment != null)
			{
				this.Frame.Navigate(typeof(ThreadView), clickedComment.Id);
			}
		}

		private void PinClicked(object sender, RoutedEventArgs e)
		{
			var comment = this.chattyCommentList.SelectedItem as Comment;
			if (comment != null)
			{
				comment.IsPinned = true;
			}
		}

		private void UnPinClicked(object sender, RoutedEventArgs e)
		{
			var comment = this.chattyCommentList.SelectedItem as Comment;
			if (comment != null)
			{
				comment.IsPinned = true;
			}
		}

		private void ReplyClicked(object sender, RoutedEventArgs e)
		{
			var comment = this.threadCommentList.SelectedItem as Comment;
			if (comment != null)
			{
				this.Frame.Navigate(typeof(ReplyToCommentView), comment);
			}
		}

		private void RefreshClicked(object sender, RoutedEventArgs e)
		{
			this.RefreshChattyComments();
		}
	}
}
