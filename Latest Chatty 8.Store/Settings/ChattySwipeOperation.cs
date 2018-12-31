namespace Latest_Chatty_8.Settings
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
			DisplayName = displayName;
			Icon = icon;
			Type = type;
		}
	}
}
