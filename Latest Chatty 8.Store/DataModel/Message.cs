using Common;
using System;

namespace Latest_Chatty_8.DataModel
{
	public class Message : BindableBase
	{
		public int Id { get; private set; }
		public string From { get; private set; }
		public string To { get; private set; }
		public string DisplayName { get; set; }
		public string Subject { get; private set; }
		public DateTime Date { get; private set; }
		public string Body { get; private set; }
		private bool npcUnread;
		public bool Unread
		{
			get => npcUnread;
			set => SetProperty(ref npcUnread, value);
		}

		public Message(int id, string from, string to, string subject, DateTime date, string body, bool unread, string folder)
		{
			Id = id;
			From = from;
			To = to;
			Subject = subject;
			Date = date;
			Body = body;
			Unread = unread;
			DisplayName = folder.Equals("sent", StringComparison.OrdinalIgnoreCase) ? To : From;
		}
	}
}
