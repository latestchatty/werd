using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Settings;

namespace Latest_Chatty_8.Controls
{
	public class ThreadSwipeEventArgs
	{
		public ThreadSwipeEventArgs(ChattySwipeOperation op, CommentThread thread)
		{
			this.Operation = op;
			this.Thread = thread;
		}

		public ChattySwipeOperation Operation { get; }
		public CommentThread Thread { get; }
	}
}
