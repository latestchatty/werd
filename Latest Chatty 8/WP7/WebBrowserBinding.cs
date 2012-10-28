//using System;
//using System.Net;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Documents;
//using System.Windows.Ink;
//using System.Windows.Input;
//using System.Windows.Media;
//using System.Windows.Media.Animation;
//using System.Windows.Shapes;
//using Microsoft.Phone.Controls;
//using System.Windows.Threading;

//namespace LatestChatty.Classes
//{
//	public static class WebBrowserBinding
//	{
//		public static readonly DependencyProperty HtmlProperty = DependencyProperty.RegisterAttached("Html", typeof(string), typeof(WebBrowserBinding), new PropertyMetadata(HtmlChanged));

//		public static string GetHtml(DependencyObject obj)
//		{
//			return (string)obj.GetValue(HtmlProperty);
//		}

//		public static void SetHtml(DependencyObject obj, string value)
//		{
//			obj.SetValue(HtmlProperty, value);
//		}

//		private static void HtmlChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
//		{
//			System.Diagnostics.Debug.WriteLine("HtmlChanged invoked.");
//			var browser = obj as WebBrowser;
//			if (browser == null)
//			{
//				System.Diagnostics.Debug.WriteLine("HtmlChanged - browser is null.");
//				return;
//			}

//			if (e.NewValue == null)
//			{
//				System.Diagnostics.Debug.WriteLine("HtmlChanged new value is null.");
//				return;
//			}

//			try
//			{
//				System.Diagnostics.Debug.WriteLine("Setting body to: {0}", e.NewValue.ToString());
//				//We're gonna trick it by starting with opacity of transparent.  When we have something to load, then we'll show the thing.
//				Application.Current.RootVisual.Dispatcher.BeginInvoke(() => { try { browser.InvokeScript("setContent", e.NewValue.ToString()); } catch { } });
//				if (browser.Opacity != 1) browser.Opacity = 1;
//			}
//			catch (Exception ex) {
//				System.Diagnostics.Debug.WriteLine("Problem invoking script on browser control. {0}", ex);
//			}
//		}
//	}
//}
