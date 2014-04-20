using Latest_Chatty_8.Shared;
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
using Windows.Graphics.Display;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Latest_Chatty_8.Shared.Converters;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Latest_Chatty_8.Shared.Controls
{
	public sealed partial class InlineThreadControl : NPCUserControl
	{
		private const int NormalWebFontSize = 14;
		private int currentItemWidth;
		public Comment SelectedComment { get; private set; }
		private WebView currentWebView;
		private int webFontSize = 14;
		//public AppBar AppBarToShow { get { return this.commentList.AppBarToShow; } set { this.commentList.AppBarToShow = value; } }

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

		public InlineThreadControl()
		{
			this.InitializeComponent();
			this.DataContextChanged += DataContextUpdated;
			Window.Current.SizeChanged += WindowSizeChanged;
		}

		private void WindowSizeChanged(object sender, WindowSizeChangedEventArgs e)
		{
			if (this.currentWebView != null)
			{
				//Selecting the comment again will cause the web view to redraw itself, making sure everything fits in the current display.
				//:HACK: This is kinda crappy.  I should probably handle this better.
				var selectedIndex = this.commentList.SelectedIndex;
				this.commentList.SelectedIndex = -1;
				this.commentList.SelectedIndex = selectedIndex;
			}
		}

		private void SetFontSize()
		{
			//Minimum size is 500px.
			//Size that we want max font is 800px.
			//Minimum font size is 9pt.  Max is 14pt.
			var fontScale = (800 - Window.Current.Bounds.Width) / 300;
			if (fontScale <= 1 && fontScale > 0)
			{
				this.webFontSize = (int)(NormalWebFontSize - (5 * fontScale));
			}
			else
			{
				this.webFontSize = NormalWebFontSize;
			}
		}

		private void DataContextUpdated(FrameworkElement sender, DataContextChangedEventArgs args)
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
			var lv = sender as ListView;
			if (lv == null) return; //This would be bad.
			this.SelectedComment = null;
			this.SetFontSize();

			foreach (var notSelected in e.RemovedItems)
			{
				var unselectedComment = notSelected as Comment;
				if (unselectedComment == null) continue;
				var unselectedContainer = lv.ContainerFromItem(unselectedComment);
				if (unselectedContainer == null) continue;
				this.UpdateVisibility(unselectedContainer, true);
			}

			foreach (var added in e.AddedItems)
			{
				var selectedItem = added as Comment;
				if (selectedItem == null) return; //Bail, we don't know what to 
				this.SelectedComment = selectedItem;
				this.SelectedComment.IsNew = false;
				CoreServices.Instance.SeenPosts.Add(this.SelectedComment.Id);
				var container = lv.ContainerFromItem(selectedItem);
				if (container == null) return; //Bail because the visual tree isn't created yet...
				var containerGrid = AllChildren<Grid>(container).FirstOrDefault(c => c.Name == "container") as Grid;

				this.currentItemWidth = (int)(containerGrid.ActualWidth * ResolutionScaleConverter.ScaleFactor);

				System.Diagnostics.Debug.WriteLine("Width of web view container is {0}", this.currentItemWidth);
				var webView = AllChildren<WebView>(container).FirstOrDefault(c => c.Name == "bodyWebView") as WebView;
				this.UpdateVisibility(container, false);

				if (webView != null)
				{
					webView.NavigationStarting += (o, a) => { return; };

					this.currentWebView = webView;
					webView.NavigationCompleted += NavigationCompleted;
					webView.NavigateToString(
					@"<html xmlns='http://www.w3.org/1999/xhtml'>
						<head>
							<meta name='viewport' content='user-scalable=no'/>
							<style type='text/css'>" + WebBrowserHelper.CSS.Replace("$$$FONTSIZE$$$", this.webFontSize.ToString()) + @"</style>
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
							<div id='commentBody' class='body'>" + selectedItem.Body + @"</div>
						</body>
					</html>");
				}
				return;
			}
		}

		async private void NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
		{
			//For some reason the WebView control *sometimes* has a width of NaN, or something small.
			//So we need to set it to what it's going to end up being in order for the text to render correctly.
			await sender.InvokeScriptAsync("eval", new string[] { string.Format("SetViewSize({0});", this.currentItemWidth) });
			var result = await sender.InvokeScriptAsync("eval", new string[] { "GetViewSize();" });
			int viewHeight;
			if (int.TryParse(result, out viewHeight))
			{
				await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(new CoreDispatcherPriority(), () =>
				{
					sender.MinHeight = sender.Height = viewHeight;
					//Scroll into view has to happen after height is set, set low dispatcher priority.
					var t = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
						{
							this.commentList.ScrollIntoView(this.commentList.SelectedItem);
							sender.Focus(Windows.UI.Xaml.FocusState.Programmatic);
						}
					);
				}
				);
			}

			sender.NavigationCompleted -= NavigationCompleted;
		}

		public void UpdateVisibility(DependencyObject container, bool previewMode)
		{
			var children = AllChildren<Grid>(container);
			var previewGrid = children.FirstOrDefault(c => c.Name == "preview");
			if (previewGrid != null)
			{
				previewGrid.Visibility = previewMode ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
			}
			var fullView = children.FirstOrDefault(c => c.Name == "commentSection");
			if (fullView != null)
			{
				fullView.Visibility = previewMode ? Windows.UI.Xaml.Visibility.Collapsed : Windows.UI.Xaml.Visibility.Visible;
			}
		}

		public List<FrameworkElement> AllChildren<T>(DependencyObject parent)
			where T : FrameworkElement
		{
			var controlList = new List<FrameworkElement>();
			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
			{
				var child = VisualTreeHelper.GetChild(parent, i);
				if (child is T)
					controlList.Add(child as FrameworkElement);

				controlList.AddRange(AllChildren<T>(child));
			}
			return controlList;
		}
	}
}
