using Latest_Chatty_8.DataModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Latest_Chatty_8
{
	public sealed partial class InlineThreadControl : UserControl, INotifyPropertyChanged
	{
		#region Notify Property Changed
		public event PropertyChangedEventHandler PropertyChanged;

		protected bool NotifyPropertyChange([CallerMemberName] String propertyName = null)
		{
			this.OnPropertyChanged(propertyName);
			return true;
		}

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			var eventHandler = this.PropertyChanged;
			if (eventHandler != null)
			{
				eventHandler(this, new PropertyChangedEventArgs(propertyName));
			}
		}
		#endregion

		private Comment _comments;
		private Comment Comments
		{
			get { return _comments; }
			set
			{
				if (!_comments.Equals(value))
				{
					this._comments = value;
					this.NotifyPropertyChange();
				}
			}
		}

		public InlineThreadControl()
		{
			this.InitializeComponent();
		}

		private void SelectedItemChanged(object sender, SelectionChangedEventArgs e)
		{
			var lv = sender as ListView;
			if (lv == null) return; //This would be bad.

			foreach(var notSelected in e.RemovedItems)
			{
				var unselectedComment = notSelected as Comment;
				if (unselectedComment == null) continue;
				var unselectedContainer = lv.ContainerFromItem(unselectedComment);
				if (unselectedContainer == null) continue;
				this.UpdateVisibility(unselectedContainer, true);
			}
			var selectedItem = e.AddedItems[0] as Comment;

			if (selectedItem == null) return; //Bail, we don't know what to 
			var container = lv.ContainerFromItem(selectedItem);
			if (container == null) return; //Bail because the visual tree isn't created yet...
			this.UpdateVisibility(container, false);
		}

		public void UpdateVisibility(DependencyObject container, bool previewMode)
		{
			var children = AllChildren<Grid>(container);
			var previewGrid = children.FirstOrDefault(c => c.Name == "preview");
			if (previewGrid != null)
			{
				previewGrid.Visibility = previewMode ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
			}
			var fullView = children.FirstOrDefault(c => c.Name == "commentSection");
			if (fullView != null)
			{
				fullView.Visibility = previewMode ? Windows.UI.Xaml.Visibility.Collapsed : Windows.UI.Xaml.Visibility.Visible;
			}
		}

		public List<FrameworkElement> AllChildren<T>(DependencyObject parent)
			where T : FrameworkElement
		{
			var controlList = new List<FrameworkElement>();
			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
			{
				var child = VisualTreeHelper.GetChild(parent, i);
				if (child is T)
					controlList.Add(child as FrameworkElement);

				controlList.AddRange(AllChildren<T>(child));
			}
			return controlList;
		}
	}
}
