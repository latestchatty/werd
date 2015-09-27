using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Latest_Chatty_8.Common
{
	public enum ChattySwipeOperationType
	{
		Pin,
		Collapse,
		MarkRead
	}

	public class ChattySwipeOperation
	{
		public string DisplayName { get; private set; }

		public ChattySwipeOperationType Type { get; private set; }

		public string Icon { get; private set; }

		public ChattySwipeOperation(ChattySwipeOperationType type, string icon, string displayName)
		{
			this.DisplayName = displayName;
			this.Icon = icon;
			this.Type = type;
		}
	}
}
