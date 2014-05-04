using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Shared;
using Latest_Chatty_8.Shared.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace Latest_Chatty_8.Views
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class PostComment : Page
	{
		private Comment replyingToComment;

		public PostComment()
		{
			this.InitializeComponent();
			Windows.Phone.UI.Input.HardwareButtons.BackPressed += HardwareButtons_BackPressed;
		}

		private void HardwareButtons_BackPressed(object sender, Windows.Phone.UI.Input.BackPressedEventArgs e)
		{
			Frame frame = Window.Current.Content as Frame;
			if (frame == null)
			{
				return;
			}

			if (frame.CanGoBack)
			{
				frame.GoBack();
				e.Handled = true;
			}
		}

		/// <summary>
		/// Invoked when this page is about to be displayed in a Frame.
		/// </summary>
		/// <param name="e">Event data that describes how this page was reached.
		/// This parameter is typically used to configure the page.</param>
		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			this.replyingToComment = e.Parameter as Comment;
			this.DataContext = this.replyingToComment;
			if (this.replyingToComment != null)
			{
				this.web.NavigateToString(
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
							<div id='commentBody' class='body'>" + this.replyingToComment.Body + @"</div>
						</body>
					</html>");
			}
			else
			{
				this.commentBrowser.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
			}
		}

		async private void SendClicked(object sender, RoutedEventArgs e)
		{
			this.sendButton.IsEnabled = false;
			var success = false;

			if(this.replyingToComment != null)
			{
				success = await this.replyingToComment.ReplyToComment(this.replyText.Text);
			}
			else
			{
				success = await ChattyHelper.PostRootComment(this.replyText.Text);
			}

			if (success)
			{
				if (LatestChattySettings.Instance.AutoPinOnReply)
				{
					if (this.replyingToComment != null)
					{
						await CoreServices.Instance.PinThread(this.replyingToComment.Id);
					}
				}
				if (this.Frame.CanGoBack)
				{
					this.Frame.GoBack();
				}
			}
			else
			{
				this.sendButton.IsEnabled = true;
			}
		}

	}
}
