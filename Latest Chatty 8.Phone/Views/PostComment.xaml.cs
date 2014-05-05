using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Shared;
using Latest_Chatty_8.Shared.Common;
using Latest_Chatty_8.Shared.Networking;
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
	public sealed partial class PostComment : Page, IFileOpenPickerContinuable
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
			this.replyingToComment = CoreServices.Instance.Chatty.Single(t => t.Comments.Any(c => c.Id == (int)e.Parameter)).Comments.Single(c => c.Id == (int)e.Parameter);
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
			this.ShowLoading();
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
				this.HideLoading(false);
			}
		}

		async private void AttachClicked(object sender, RoutedEventArgs e)
		{
			this.ShowLoading();
			try
			{
				var picker = new Windows.Storage.Pickers.FileOpenPicker();
				picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
				picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
				picker.FileTypeFilter.Add(".jpg");
				picker.FileTypeFilter.Add(".jpeg");
				//picker.FileTypeFilter.Add(".gif");
				picker.FileTypeFilter.Add(".png");
				//picker.FileTypeFilter.Add(".bmp");
				picker.ContinuationData["Operation"] = "UploadPhoto";
				picker.ContinuationData["Input Text"] = this.replyText.Text;
				picker.PickSingleFileAndContinue();
			}
			finally
			{
				this.HideLoading(true);
			}
		}

		private void ShowLoading()
		{
			this.loadingIndicator.IsActive = true;
			this.loadingOverlay.Visibility = Windows.UI.Xaml.Visibility.Visible;
			this.loadingIndicator.Focus(FocusState.Programmatic);
			this.appBar.IsEnabled = false;
		}

		private void HideLoading(bool giveKeyboardFocus)
		{
			this.loadingIndicator.IsActive = false;
			this.loadingOverlay.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
			this.appBar.IsEnabled = true;
			if(giveKeyboardFocus)
			{
				this.replyText.Focus(FocusState.Programmatic);
			}
		}

		async public void ContinueFileOpenPicker(Windows.ApplicationModel.Activation.FileOpenPickerContinuationEventArgs args)
		{
			if(args.ContinuationData.ContainsKey("Operation") && args.ContinuationData["Operation"].ToString().Equals("UploadPhoto"))
			{
				//Does this happen on the UI thread or do we need to dispatch?
				this.ShowLoading();
				this.replyText.Text = args.ContinuationData["Input Text"].ToString();
				foreach(var file in args.Files)
				{
					this.replyText.Text += await ChattyPics.UploadPhoto(file) + Environment.NewLine;
				}
				this.HideLoading(true);
			}
		}
	}
}
