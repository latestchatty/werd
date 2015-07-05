using Latest_Chatty_8.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Latest_Chatty_8.DataModel
{
	public class Message : BindableBase
	{
		public int Id { get; private set; }
		public string From { get; private set; }
		public string To { get; private set; }
		public string Subject { get; private set; }
		public DateTime Date { get; private set; }
		public string Body { get; private set; }
		private bool npcUnread;
		public bool Unread
		{
			get { return this.npcUnread; }
			set
			{
				this.SetProperty(ref this.npcUnread, value);
			}
		}

		public Message(int id, string from, string to, string subject, DateTime date, string body, bool unread)
		{
			this.Id = id;
			this.From = from;
			this.To = to;
			this.Subject = subject;
			this.Date = date;
			this.Body = body;
			this.Unread = unread;
		}
	}
}
