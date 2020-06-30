using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Documents;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Werd.Controls
{
	public sealed partial class Spoiler : INotifyPropertyChanged
	{
		private double npcMaxSpoilerWidth = 768;
		private double MaxSpoilerWidth
		{
			get => npcMaxSpoilerWidth;
			set => SetProperty(ref npcMaxSpoilerWidth, value);
		}

		public Spoiler()
		{
			InitializeComponent();
			//TODO: Be responsive to window size changes.
			MaxSpoilerWidth = Math.Min(Window.Current.Bounds.Width * .75, 750);
		}

		public void SetText(Paragraph content)
		{
			SpoiledBlock.Blocks.Clear();
			SpoiledBlock.Blocks.Add(content);
		}

		#region NPC
		/// <summary>
		/// Multicast event for property change notifications.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Checks if a property already matches a desired value.  Sets the property and
		/// notifies listeners only when necessary.
		/// </summary>
		/// <typeparam name="T">Type of the property.</typeparam>
		/// <param name="storage">Reference to a property with both getter and setter.</param>
		/// <param name="value">Desired value for the property.</param>
		/// <param name="propertyName">Name of the property used to notify listeners.  This
		///     value is optional and can be provided automatically when invoked from compilers that
		///     support CallerMemberName.</param>
		/// <returns>True if the value was changed, false if the existing value matched the
		/// desired value.</returns>
		private void SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
		{
			if (Equals(storage, value)) return;

			storage = value;
			OnPropertyChanged(propertyName);
		}

		/// <summary>
		/// Notifies listeners that a property value has changed.
		/// </summary>
		/// <param name="propertyName">Name of the property used to notify listeners.  This
		/// value is optional and can be provided automatically when invoked from compilers
		/// that support <see cref="CallerMemberNameAttribute"/>.</param>
		private void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			var eventHandler = PropertyChanged;
			if (eventHandler != null)
			{
				eventHandler(this, new PropertyChangedEventArgs(propertyName));
			}
		}
		#endregion
	}
}
