using Latest_Chatty_8.Common;
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

namespace Latest_Chatty_8.Controls
{
	public sealed partial class InlineThreadControl : UserControl, INotifyPropertyChanged
	{
		private int currentItemWidth;
		private Comment selectedComment;

		private IEnumerable<Comment> currentComments;
		public IEnumerable<Comment> Comments
		{
			get { return this.currentComments; }
			set { if (this.currentComments != null && this.currentComments.Equals(value)) return; else this.currentComments = value; this.NotifyPropertyChange("Comments"); }
		}

		private bool isExpired;
		public bool IsExpired
		{
			get { return this.isExpired; }
			set { if (this.isExpired.Equals(value)) return; else this.isExpired = value; this.NotifyPropertyChange("IsExpired"); }
		}

		#region Notify Property Changed
		public event PropertyChangedEventHandler PropertyChanged;

		protected bool NotifyPropertyChange([CallerMemberName] String propertyName = null)
		{
			this.OnPropertyChanged(propertyName);
			return true;
		}

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			var eventHandler = this.PropertyChanged;
			if (eventHandler != null)
			{
				eventHandler(this, new PropertyChangedEventArgs(propertyName));
			}
		}
		#endregion

		public InlineThreadControl()
		{
			this.InitializeComponent();
			this.DataContextChanged += DataContextUpdated;
		}

		private void DataContextUpdated(FrameworkElement sender, DataContextChangedEventArgs args)
		{
			var comments = args.NewValue as IEnumerable<Comment>;

			if (comments != null)
			{
				var firstComment = comments.OrderBy(c => c.Id).First();
				this.IsExpired = (firstComment.Date.AddHours(18).ToUniversalTime() < DateTime.UtcNow);
				this.Comments = comments;

				Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
					{
						if (comments.Count() > 0) this.commentList.SelectedIndex = 0;
					});
			}
			this.root.DataContext = this;
		}

		async private void SelectedItemChanged(object sender, SelectionChangedEventArgs e)
		{
			var lv = sender as ListView;
			if (lv == null) return; //This would be bad.

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
				this.selectedComment = selectedItem;
				var container = lv.ContainerFromItem(selectedItem);
				if (container == null) return; //Bail because the visual tree isn't created yet...
				var containerGrid = AllChildren<Grid>(container).FirstOrDefault(c => c.Name == "sizeGrid") as Grid;
				this.currentItemWidth = (int)containerGrid.ActualWidth;

				var webView = AllChildren<WebView>(container).FirstOrDefault(c => c.Name == "bodyWebView") as WebView;
				this.UpdateVisibility(container, false);

				if (webView != null)
				{
					webView.NavigationCompleted += NavigationCompleted;
					webView.NavigateToString(
					@"<html xmlns='http://www.w3.org/1999/xhtml'>
						<head>
							<meta name='viewport' content='user-scalable=no'/>
							<style type='text/css'>" + WebBrowserHelper.CSS.Replace("$$$FONTSIZE$$$", "14") + @"</style>
							<script type='text/javascript'>
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
					Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
						{
							this.commentList.ScrollIntoView(this.commentList.SelectedItem);
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
