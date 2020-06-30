using Werd.DataModel;
using Werd.Settings;

namespace Werd.Controls
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
