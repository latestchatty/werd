using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Latest_Chatty_8.Controls
{
	public sealed partial class SplitThreadControl : UserControl, INotifyPropertyChanged
	{
		private Comment selectedComment;
		public Comment SelectedComment
		{
			get { return selectedComment; }
			set { this.SetProperty(ref this.selectedComment, value); }
		}

		private IEnumerable<Comment> currentComments;
		public IEnumerable<Comment> Comments
		{
			get { return this.currentComments; }
			set { this.SetProperty(ref this.currentComments, value); }
		}

		private CommentThread thread;
		public CommentThread Thread
		{
			get { return this.thread; }
			set { this.SetProperty(ref this.thread, value); }
		}

		public SplitThreadControl()
		{
			this.InitializeComponent();
			this.DataContextChanged += SplitThreadControl_DataContextChanged;
		}

		public async void ShowFirstUnreadPost()
		{
			var firstComment = this.Comments.Where(c => c.IsNew).OrderBy(c => c.Id).FirstOrDefault();
			if (firstComment != null)
			{
				await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
				{
					this.commentList.SelectedItem = firstComment;
					this.commentList.ScrollIntoView(firstComment, ScrollIntoViewAlignment.Leading);
				});
			}
		}

		void SplitThreadControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
		{
			var commentThread = args.NewValue as CommentThread;

			if (commentThread != null)
			{
				this.Thread = commentThread;
				this.Comments = commentThread.Comments;

				////Any time we view a thread, we check to see if we've seen a post before.
				////If we have, make sure it's not marked as new.
				////If we haven't, add it to the list of comments we've seen, but leave it marked as new.
				//foreach (var c in commentThread.Comments)
				//{
				//	if (CoreServices.Instance.SeenPosts.Contains(c.Id))
				//	{
				//		c.IsNew = false;
				//	}
				//	else
				//	{
				//		CoreServices.Instance.SeenPosts.Add(c.Id);
				//		c.IsNew = true;
				//	}
				//}

				var t = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
				{
					if (commentThread.Comments.Count() > 0) this.commentList.SelectedIndex = 0;
				});
			}
			this.root.DataContext = this;
		}

		async private void SelectedItemChanged(object sender, SelectionChangedEventArgs e)
		{
			Comment selectedComment = ((e.AddedItems != null && e.AddedItems.Count > 0) ? e.AddedItems[0] : null) as Comment;

			this.SelectedComment = selectedComment;
			if (selectedComment == null) { return; }

			await CoreServices.Instance.MarkCommentRead(this.Thread, this.SelectedComment);

			bodyWebView.Opacity = 0;
			bodyWebView.NavigationCompleted += bodyWebView_NavigationCompleted;
			bodyWebView.NavigateToString(
			@"<html xmlns='http://www.w3.org/1999/xhtml'>
						<head>
							<meta name='viewport' content='user-scalable=no'/>
							<style type='text/css'>" + WebBrowserHelper.CSS.Replace("$$$FONTSIZE$$$", FontSize.ToString()) + @"</style>
							<script type='text/javascript'>
								function SetFontSize(size)
								{
									var html = document.getElementById('commentBody');
									html.style.fontSize=size+'pt';
								}
								function SetViewSize(size)
								{
									var html = document.getElementById('commentBody');
									html.style.width=size+'px';
								}
								function GetViewSize() {
									//var html = document.documentElement;
									var html = document.getElementById('commentBody');
									var height = Math.max( html.clientHeight, html.scrollHeight, html.offsetHeight );
									/*var debug = document.createElement('div');
									debug.appendChild(document.createTextNode('clientHeight : ' + html.clientHeight));
									debug.appendChild(document.createTextNode('scrollHeight : ' + html.scrollHeight));
									debug.appendChild(document.createTextNode('offsetHeight : ' + html.offsetHeight));
									debug.appendChild(document.createTextNode('clientWidth : ' + html.clientWidth));
									debug.appendChild(document.createTextNode('scrollWidth : ' + html.scrollWidth));
									debug.appendChild(document.createTextNode('offsetWidth : ' + html.offsetWidth));
									html.appendChild(debug);*/
									return height.toString();
								}
function loadImage(e, url) {
    var img = new Image();
    img.onload= function () {
        e.onload='';
        e.src = img.src;
    };
    img.src = url;
}
</script>
						</head>
						<body>
							<div id='commentBody' class='body'>" + this.SelectedComment.Body + @"</div>
						</body>
					</html>");
			return;
		}

		void bodyWebView_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
		{
			this.bodyWebView.NavigationCompleted -= bodyWebView_NavigationCompleted;
			this.bodyWebView.Opacity = 1;
		}

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
		/// value is optional and can be provided automatically when invoked from compilers that
		/// support CallerMemberName.</param>
		/// <returns>True if the value was changed, false if the existing value matched the
		/// desired value.</returns>
		private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] String propertyName = null)
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
		private void OnPropertyChanged([CallerMemberName] string propertyName = null)
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
