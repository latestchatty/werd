using System.Collections.ObjectModel;

namespace Werd.Common
{
	public class MoveableObservableCollection<T> : ObservableCollection<T>
	{
		//ObservableCollection's MoveItem implementation seems to be broken as hell.
		//This way won't cause a full repaint of bound items.
		protected override void MoveItem(int oldIndex, int newIndex)
		{
			//Nothing to do, we're already there.  Don't do unnecessary work!
			if (oldIndex == newIndex)
			{
				return;
			}
			//base.MoveItem(oldIndex, newIndex);
			if (oldIndex >= 0)
			{
				//System.Diagnostics.Global.DebugLog.AddMessage("Moving {0} to {1}", oldIndex, newIndex);
				var oldItem = Items[oldIndex];
				RemoveAt(oldIndex);
				Insert(newIndex, oldItem);
			}
		}
	}
}
