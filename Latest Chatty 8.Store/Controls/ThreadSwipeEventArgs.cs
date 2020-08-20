using Werd.DataModel;
using Werd.Settings;

namespace Werd.Controls
{
	public class ThreadSwipeEventArgs
	{
		public ThreadSwipeEventArgs(ChattySwipeOperationType op, CommentThread thread)
		{
			this.Operation = op;
			this.Thread = thread;
		}

		public ChattySwipeOperationType Operation { get; }
		public CommentThread Thread { get; }
	}
}
