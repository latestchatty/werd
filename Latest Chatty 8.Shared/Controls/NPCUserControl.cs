using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Latest_Chatty_8.Shared.Controls
{
	protected class NPCUserControl : UserControl, INotifyPropertyChanged
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
	}
}
