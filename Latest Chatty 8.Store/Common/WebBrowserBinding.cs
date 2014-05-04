using System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Latest_Chatty_8.Shared
{
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

		private static void SetHtml(WebView browser)
		{
			try
			{
				browser.NavigateToString(
					@"<html xmlns='http://www.w3.org/1999/xhtml'>
						<head>
							<meta name='viewport' content='user-scalable=no'/>
							<style type='text/css'>" + WebBrowserHelper.CSS.Replace("$$$FONTSIZE$$$", fontSize.ToString()) + @"</style>
							<script type='text/javascript'>
								function GetViewSize() {
									var html = document.documentElement;
									var height = Math.max( html.clientHeight, html.scrollHeight, html.offsetHeight );
									return height.toString();
								}
							</script>
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
