using Latest_Chatty_8.DataModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Latest_Chatty_8.Shared.Controls
{
	public sealed partial class SplitThreadControl : NPCUserControl
	{
		private Comment selectedComment;
		public Comment SelectedComment { 
			get { return selectedComment;}
			set { if (this.selectedComment != null && this.selectedComment.Equals(value)) return; else this.selectedComment = value; this.NotifyPropertyChange(); }
		}

		private IEnumerable<Comment> currentComments;
		public IEnumerable<Comment> Comments
		{
			get { return this.currentComments; }
			set { if (this.currentComments != null && this.currentComments.Equals(value)) return; else this.currentComments = value; this.NotifyPropertyChange(); }
		}

		private CommentThread thread;
		public CommentThread Thread
		{
			get { return this.thread; }
			set { if (this.thread != null && this.thread.Equals(value)) return; else this.thread = value; this.NotifyPropertyChange(); }
		}

		public SplitThreadControl()
		{
			this.InitializeComponent();
			this.DataContextChanged += SplitThreadControl_DataContextChanged;
		}

		void SplitThreadControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
		{
			var commentThread = args.NewValue as CommentThread;

			if (commentThread != null)
			{
				this.Thread = commentThread;
				this.Comments = commentThread.Comments;

				//Any time we view a thread, we check to see if we've seen a post before.
				//If we have, make sure it's not marked as new.
				//If we haven't, add it to the list of comments we've seen, but leave it marked as new.
				foreach (var c in commentThread.Comments)
				{
					if (CoreServices.Instance.SeenPosts.Contains(c.Id))
					{
						c.IsNew = false;
					}
					else
					{
						CoreServices.Instance.SeenPosts.Add(c.Id);
						c.IsNew = true;
					}
				}

				var t = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
				{
					if (commentThread.Comments.Count() > 0) this.commentList.SelectedIndex = 0;
				});
			}
			this.root.DataContext = this;
		}

		private void SelectedItemChanged(object sender, SelectionChangedEventArgs e)
		{
			Comment selectedComment = ((e.AddedItems != null && e.AddedItems.Count > 0) ? e.AddedItems[0] : null) as Comment;

			this.SelectedComment = selectedComment;
			if (selectedComment == null) { return; }

			this.SelectedComment.IsNew = false;
			CoreServices.Instance.SeenPosts.Add(this.SelectedComment.Id);

			bodyWebView.NavigationCompleted += NavigationCompleted;
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
							</script>
						</head>
						<body>
							<div id='commentBody' class='body'>" + this.SelectedComment.Body + @"</div>
						</body>
					</html>");
			return;
		}

		async private void NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
		{
			//For some reason the WebView control *sometimes* has a width of NaN, or something small.
			//So we need to set it to what it's going to end up being in order for the text to render correctly.
			//await sender.InvokeScriptAsync("eval", new string[] { string.Format("SetViewSize({0});", this.currentItemWidth) });
			//var result = await sender.InvokeScriptAsync("eval", new string[] { "GetViewSize();" });
			//int viewHeight;
			//if (int.TryParse(result, out viewHeight))
			//{
			await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
			{
				this.commentList.ScrollIntoView(this.commentList.SelectedItem);
				sender.Focus(Windows.UI.Xaml.FocusState.Programmatic);
			});
			//}

			sender.NavigationCompleted -= NavigationCompleted;
		}
	}
}
