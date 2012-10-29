using System;
using System.Net;
using System.Windows;
using System.Windows.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Latest_Chatty_8.Common
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

			try
			{
				var css = "body{ overflow:visible; padding:0; margin:0; background:rgb(29,29,29); font-family:'Segoe UI';  font-size:16pt; color:white}div.wrapper{ padding:10px}div.author a{ font-size:10pt; color:rgb(255,186,0); float:left; text-decoration:none}div.date a{ font-size:8pt; color:rgb(100,100,100); float:right; text-decoration:none}div.body{ color:white; padding:20px 0; clear:both}div.body a{ color:#AEAE9B}span.jt_red {color:#F00}span.jt_green {color:#8DC63F}span.jt_blue {color:#44AEDF}span.jt_yellow{color:#FFDE00}span.jt_olive {color:olive}span.jt_lime {color:#C0FFC0}span.jt_orange{color:#F7941C}span.jt_pink {color:#F49AC1}span.jt_spoiler{ background-color:#383838; color:#383838}span.jt_strike{ text-decoration:line-through}span.jt_sample{ font-size:80%}span.jt_quote{ font-family:serif; font-size:110%}pre.jt_code{ border-left:1px solid #666; display:block; font-family:monospace; margin:5px 0pt 5px 10px; padding:3px 0pt 3px 10px}div.youtube-widget{ text-align:center}div.youtube-widget a{ font-size:80%}h1.story-title{ font-size:150%; text-shadow:black 1px 5px 7px}div.focalbox{ background:#444; padding:10px; text-align:center; -webkit-border-radius:20px; margin:10px 0}div.focalbox img{ margin:10px; border:2px solid #888}div.story-content a{ color:#CF262D; font-weight:bold}";
				browser.NavigateToString("<html><head><meta name='viewport' content='user-scalable=no'/><style type='text/css'>"
					+ css + "</style><body><div id='commentBody' class='body'>" 
					+ e.NewValue.ToString() + "</div></body></html>");
				if (browser.Opacity != 1) browser.Opacity = 1;
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine("Problem invoking script on browser control. {0}", ex);
			}
		}
	}
}
