namespace Latest_Chatty_8.Common
{
	public class ShellMessageEventArgs
	{
		public string Message { get; private set; }

		public ShellMessageType Type { get; private set; }

		public ShellMessageEventArgs(string message)
			: this(message, ShellMessageType.Message)
		{ }

		public ShellMessageEventArgs(string message, ShellMessageType type)
		{
			this.Message = message;
			this.Type = type;
		}
	}

	public enum ShellMessageType
	{
		Error,
		Message
	}
}
