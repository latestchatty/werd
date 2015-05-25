using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Shared.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Latest_Chatty_8.Common;
using Latest_Chatty_8.Shared.Networking;
using Latest_Chatty_8.Shared;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Latest_Chatty_8.Controls
{
	public sealed partial class InlineThreadControl : UserControl, INotifyPropertyChanged
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
			set { this.SetProperty(ref this.currentComments, value); }
		}

		private CommentThread thread;
		public CommentThread Thread
		{
			get { return this.thread; }
			set { this.SetProperty(ref this.thread, value); }
		}

		public InlineThreadControl()
		{
			this.InitializeComponent();
			this.DataContextChanged += DataContextUpdated;
			Window.Current.SizeChanged += WindowSizeChanged;
		}

		private void WindowSizeChanged(object sender, WindowSizeChangedEventArgs e)
		{
			//HACK: This would be better to be based on the control size, not the window size.
			CoreServices.Instance.ShowAuthor = e.Size.Width > 500;
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
			return;
			//Minimum size is 320px.
			//Size that we want max font is 600px.
			//Minimum font size is 9pt.  Max is 14pt.
			var fontScale = (600 - Window.Current.Bounds.Width) / 280;
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

				//:TODO: Can this be removed?  It should be handled by binding and updates to the models.
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
			var lv = sender as ListView;
			if (lv == null) return;	//This would be bad.
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
				await CoreServices.Instance.MarkCommentRead(this.Thread, this.SelectedComment);
				var container = lv.ContainerFromItem(selectedItem);
				if (container == null) return; //Bail because the visual tree isn't created yet...
				var containerGrid = container.FindControlsNamed<Grid>("container").FirstOrDefault();

				System.Diagnostics.Debug.WriteLine("Width: {0} Scale: {1}", containerGrid.ActualWidth, ResolutionScaleConverter.ScaleFactor);
				this.currentItemWidth = (int)containerGrid.ActualWidth;// (int)(containerGrid.ActualWidth * ResolutionScaleConverter.ScaleFactor);

				System.Diagnostics.Debug.WriteLine("Width of web view container is {0}", this.currentItemWidth);
				var webView = container.FindControlsNamed<WebView>("bodyWebView").FirstOrDefault() as WebView;
				webView.ScriptNotify += ScriptNotify;
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
									var html = document.getElementById('commentBody');
									var height = Math.max( html.clientHeight, html.scrollHeight, html.offsetHeight );
									return height.toString();
								}
								function loadImage(e, url) {
									 var img = new Image();
									 img.onload= function () {
										  e.onload=function() { window.external.notify('imageloaded'); };
										  e.src = img.src;
                                e.onclick=function(i) { var originalClassName = e.className; if(e.className == 'fullsize') { e.className = 'embedded'; } else { e.className = 'fullsize'; } window.external.notify('imageloaded|' + i.className + '|' + originalClassName); return false;};
									 };
									 img.src = url;
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

		private async void ScriptNotify(object s, NotifyEventArgs e)
		{
			var sender = s as WebView;

			if (e.Value.Contains("imageloaded"))
			{
				await ResizeWebView(sender);
			}
			//if (e.Value.Equals("imageloaded"))
			//{
			//	await ResizeWebView(sender);
			//}
		}

		async private void NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
		{
			await ResizeWebView(sender);
			sender.NavigationCompleted -= NavigationCompleted;
		}

		private async Task ResizeWebView(WebView wv)
		{
			//For some reason the WebView control *sometimes* has a width of NaN, or something small.
			//So we need to set it to what it's going to end up being in order for the text to render correctly.
			await wv.InvokeScriptAsync("eval", new string[] { string.Format("SetViewSize({0});", this.currentItemWidth) });
			var result = await wv.InvokeScriptAsync("eval", new string[] { "GetViewSize();" });
			int viewHeight;
			if (int.TryParse(result, out viewHeight))
			{
				await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(new CoreDispatcherPriority(), () =>
				{
					wv.MinHeight = wv.Height = viewHeight;
					//Scroll into view has to happen after height is set, set low dispatcher priority.
					var t = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
					{
						this.commentList.ScrollIntoView(this.commentList.SelectedItem);
						wv.Focus(Windows.UI.Xaml.FocusState.Programmatic);
					}
					);
				}
				);
			}
		}

		public void UpdateVisibility(DependencyObject container, bool previewMode)
		{
			var children = container.AllChildren<Grid>();
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

		private async void lolPostClicked(object sender, RoutedEventArgs e)
		{
			if (this.SelectedComment == null) return;
			var controlContainer = this.commentList.ContainerFromItem(this.SelectedComment);
			if (controlContainer != null)
			{
				var tagButton = controlContainer.FindControlsNamed<Button>("tagButton").FirstOrDefault();
				if (tagButton == null) return;

				tagButton.IsEnabled = false;
				try
				{
					var mi = sender as MenuFlyoutItem;
					var tag = mi.Text;
					await this.SelectedComment.LolTag(tag);
				}
				finally
				{
					tagButton.IsEnabled = true;
				}
			}
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
