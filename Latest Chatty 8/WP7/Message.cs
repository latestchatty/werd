using System;
using System.Net;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using Windows.UI.Xaml.Data;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace LatestChatty.Classes
{
	public class Message
	{
		public string from { get; set; }
		public bool unread { get; set; }
		public string dateText { get; set; }
		public DateTime date { get; set; }
		public string subject { get; set; }
		public int id { get; set; }
		public string body { get; set; }

		public Message()
		{
		}

		public Message(XElement x)
		{
			from = ((string)x.Attribute("author")).Trim();
			dateText = (string)x.Attribute("date");
			subject = ((string)x.Attribute("subject")).Trim();
			id = int.Parse((string)x.Attribute("id"));
			body = StripHTML(((string)x.Value).Trim());

			if (((string)x.Attribute("status")).Trim() == "read")
			{
				unread = false;
			}
			else
			{
				unread = true;
			}
		}

		private string StripHTML(string s)
		{
			return Regex.Replace(s, " target=\"_blank\"", string.Empty);
		}
	}

	public class UnreadConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			bool unread = (bool)value;
			if (unread)
			{
				return new SolidColorBrush(Color.FromArgb(0xff, 40, 40, 40));
			}

			return new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
