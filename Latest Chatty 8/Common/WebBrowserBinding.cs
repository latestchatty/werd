using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Latest_Chatty_8.Common
{
	public class WebBrowserHelper
	{
		#region Browser Templates
		/// <summary>
		/// Replace $$CSS$$ with CSS value and $$BODY$$ with comment body.
		/// </summary>
		public const string CommentHTMLTemplate = @"<html xmlns='http://www.w3.org/1999/xhtml'>
						<head>
							<meta name='viewport' content='user-scalable=no'/>
							<style type='text/css'>$$CSS$$</style>
							<script type='text/javascript'>
								function fireSize() 
								{ 
									window.external.notify(
										Math.max(
											Math.max(
												document.documentElement.clientHeight, 
												document.documentElement.scrollHeight)
											, document.documentElement.offsetHeight)); 
								}
							</script>
						</head>
						<body>
							<div id='commentBody' class='body'>$$BODY$$</div>
						</body>
					</html>";

		public const string CSS = @"body
		{
			overflow:visible;
			background:#1d1d1d;
			font-family:'Segoe UI';
			font-size:$$$FONTSIZE$$$pt;
			color:#FFF;
			margin:0;
			padding:0;
		}

		div.wrapper
		{
			padding:10px;
		}

		div.body
		{
			color:#FFF;
			clear:both;
			padding:20px 0;
		}

		div.body a
		{
			color:#AEAE9B;
		}

		span.jt_red
		{
			color:red;
		}

		span.jt_green
		{
			color:#8DC63F;
		}

		span.jt_blue
		{
			color:#44AEDF;
		}

		span.jt_yellow
		{
			color:#FFDE00;
		}

		span.jt_olive
		{
			color:olive;
		}

		span.jt_lime
		{
			color:#C0FFC0;
		}

		span.jt_orange
		{
			color:#F7941C;
		}

		span.jt_pink
		{
			color:#F49AC1;
		}

		span.jt_spoiler
		{
			background-color:#383838;
			color:#383838;
		}

		span.jt_strike
		{
			text-decoration:line-through;
		}

		span.jt_quote
		{
			font-family:serif;
			font-size:110%;
		}

		pre.jt_code
		{
			border-left:1px solid #666;
			display:block;
			font-family:monospace;
			margin:5px 0 5px 10px;
			padding:3px 0 3px 10px;
		}

		div.youtube-widget
		{
			text-align:center;
		}

		h1.story-title
		{
			font-size:150%;
			text-shadow:#000 1px 5px 7px;
		}

		div.focalbox
		{
			background:#444;
			text-align:center;
			-webkit-border-radius:20px;
			margin:10px 0;
			padding:10px;
		}

		div.focalbox img
		{
			border:2px solid #888;
			margin:10px;
		}

		div.story-content a
		{
			color:#CF262D;
			font-weight:700;
		}

		span.jt_sample,div.youtube-widget a
		{
			font-size:80%;
		}
		img.embedded
		{
			vertical-align: middle;
			max-height: 500px;
			height: 500px;
		}";
	}
		#endregion

	public static class WebBrowserBinding
	{
		public static readonly DependencyProperty HtmlProperty = DependencyProperty.RegisterAttached("Html", typeof(string), typeof(WebBrowserBinding), new PropertyMetadata("", HtmlChanged));

		public static string GetHtml(DependencyObject obj)
		{
			return (string)obj.GetValue(HtmlProperty);
		}

		public static void SetHtml(DependencyObject obj, string value)
		{
			obj.SetValue(HtmlProperty, value);
		}

		public static readonly DependencyProperty FontSizeProperty = DependencyProperty.RegisterAttached("FontSize", typeof(int), typeof(WebBrowserBinding), new PropertyMetadata("", FontSizeChanged));

		public static int GetFontSize(DependencyObject obj)
		{
			return (int)obj.GetValue(FontSizeProperty);
		}

		public static void SetFontSize(DependencyObject obj, int value)
		{
			obj.SetValue(FontSizeProperty, value);
		}

		private static void FontSizeChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
		{
			var browser = obj as WebView;
			if (browser == null)
			{
				System.Diagnostics.Debug.WriteLine("HtmlChanged - browser is null.");
				return;
			}

			fontSize = (int)e.NewValue;
			SetHtml(browser);
		}

		private static int fontSize = 14;
		private static string html = string.Empty;

		private static void HtmlChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
		{
			System.Diagnostics.Debug.WriteLine("HtmlChanged invoked.");
			var browser = obj as WebView;
			if (browser == null)
			{
				System.Diagnostics.Debug.WriteLine("HtmlChanged - browser is null.");
				return;
			}

			if (e.NewValue == null)
			{
				System.Diagnostics.Debug.WriteLine("HtmlChanged new value is null.");
				return;
			}

			html = e.NewValue.ToString();
			SetHtml(browser);
		}

		static void browser_LoadCompleted(object sender, Windows.UI.Xaml.Navigation.NavigationEventArgs e)
		{
			var browser = (WebView)sender;
			browser.LoadCompleted -= browser_LoadCompleted;
			browser.Visibility = Visibility.Visible;
		}

		private static void SetHtml(WebView browser)
		{
			try
			{
				//browser.ScriptNotify += browser_ScriptNotify;
				browser.LoadCompleted += browser_LoadCompleted;
				//browser.AllowedScriptNotifyUris = WebView.AnyScriptNotifyUri;

				browser.NavigateToString(
					@"<html xmlns='http://www.w3.org/1999/xhtml'>
						<head>
							<meta name='viewport' content='user-scalable=no'/>
							<style type='text/css'>" + WebBrowserHelper.CSS.Replace("$$$FONTSIZE$$$", fontSize.ToString()) + @"</style>
						</head>
						<body>
							<div id='commentBody' class='body'>" + html + @"</div>
						</body>
					</html>");
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine("Problem invoking script on browser control. {0}", ex);
			}
		}
	}
}
