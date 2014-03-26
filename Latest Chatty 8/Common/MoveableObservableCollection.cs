using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Latest_Chatty_8.Common
{
	public class MoveableObservableCollection<T> : ObservableCollection<T>
	{
		//ObservableCollection's MoveItem implementation seems to be broken as hell.
		//This way won't cause a full repaint of bound items.
		protected override void MoveItem(int oldIndex, int newIndex)
		{
			//Nothing to do, we're already there.  Don't do unnecessary work!
			if(oldIndex == newIndex)
			{
				return;
			}
			//base.MoveItem(oldIndex, newIndex);
			if (oldIndex >= 0)
			{
				System.Diagnostics.Debug.WriteLine("Moving {0} to {1}", oldIndex, newIndex);
				var oldItem = this.Items[oldIndex];
				this.RemoveAt(oldIndex);
				this.Insert(newIndex, oldItem);
			}
			else //Getting -1 for oldIndex.  Weird.  We'll let it do its thing.
			{
				System.Diagnostics.Debug.WriteLine("Got -1 in MoveItem.");
				//base.MoveItem(oldIndex, newIndex); Apparently it's thing isn't the right thing... 
			}
		}
	}
}
