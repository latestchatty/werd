using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Input;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Latest_Chatty_8.Shared
{
	public class IgnoreRightClickListView : ListView
	{
		public AppBar AppBarToShow { get; set; }

		protected override Windows.UI.Xaml.DependencyObject GetContainerForItemOverride()
		{
			return new MyListViewItem(this, this.AppBarToShow);
		}

		private class MyListViewItem : ListViewItem
		{
			private ListView list;
			private AppBar appBar = null;

			public MyListViewItem(ListView list, AppBar appBar)
			{
				this.list = list;
				this.appBar = appBar;
			}

			protected override void OnRightTapped(RightTappedRoutedEventArgs e)
			{
				// Note: this setup demonstrates how to still select with right-click [but not deselect]
				// If not wished, then just unconditionally perform 'e.Handled = true'
				// and don't pass the ListView in the constructor since not needed
				object elem = this.DataContext;
				e.Handled = true;
				if (this.appBar != null)
				{
					this.appBar.IsOpen = true;
				}
				//if (this.list.SelectedItems.Contains(elem))
				//	e.Handled = true;
				//else
				//	base.OnRightTapped(e);
			}
		}
	}
}

