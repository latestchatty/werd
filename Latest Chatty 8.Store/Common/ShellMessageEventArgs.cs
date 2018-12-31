using System;

namespace Latest_Chatty_8.Common
{
	public class ShellMessageEventArgs : EventArgs
	{
		public string Message { get; private set; }

		public ShellMessageType Type { get; private set; }

		public ShellMessageEventArgs(string message)
			: this(message, ShellMessageType.Message)
		{ }

		public ShellMessageEventArgs(string message, ShellMessageType type)
		{
			Message = message;
			Type = type;
		}
	}

	public enum ShellMessageType
	{
		Error,
		Message
	}
}
