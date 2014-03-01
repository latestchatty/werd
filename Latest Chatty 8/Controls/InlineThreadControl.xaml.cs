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

		private Comment _comments;
		private Comment Comments
		{
			get { return _comments; }
			set
			{
				if (!_comments.Equals(value))
				{
					this._comments = value;
					this.NotifyPropertyChange();
				}
			}
		}

		public InlineThreadControl()
		{
			this.InitializeComponent();
		}

		private void SelectedItemChanged(object sender, SelectionChangedEventArgs e)
		{
			var lv = sender as ListView;
			if (lv == null) return; //This would be bad.

			foreach(var notSelected in e.RemovedItems)
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
				var container = lv.ContainerFromItem(selectedItem);
				if (container == null) return; //Bail because the visual tree isn't created yet...
				var webView = AllChildren<WebView>(container).FirstOrDefault(c => c.Name == "bodyWebView") as WebView;
				if(webView != null)
				{
					webView.DOMContentLoaded += LoadComplete;
					//browser.AllowedScriptNotifyUris = WebView.AnyScriptNotifyUri;

					webView.NavigateToString(
						@"<html xmlns='http://www.w3.org/1999/xhtml'>
						<head>
							<meta name='viewport' content='user-scalable=no'/>
							<style type='text/css'>" + WebBrowserHelper.CSS.Replace("$$$FONTSIZE$$$", "14") + @"</style>
							<script type='text/javascript'>
								function GetViewSize() {
									var html = document.documentElement;
									var height = Math.max( html.clientHeight, html.scrollHeight, html.offsetHeight );
									return height.toString();
								}
							</script>
						</head>
						<body>
							<div id='commentBody' class='body'>" + selectedItem.Body + @"</div>
						</body>
					</html>");
				}
				this.UpdateVisibility(container, false);
			}
		}

		async private void LoadComplete(WebView sender, WebViewDOMContentLoadedEventArgs args)
		{
			var result = await sender.InvokeScriptAsync("eval", new string[] { "GetViewSize();" });
			int viewHeight;
			if (int.TryParse(result, out viewHeight))
			{
				await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(new CoreDispatcherPriority(), () =>
				{
					sender.MinHeight = viewHeight;
				}
				);
			}
			sender.DOMContentLoaded -= LoadComplete;
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
