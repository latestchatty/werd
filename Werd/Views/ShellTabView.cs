﻿using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Werd.Common;
using Windows.UI.Xaml.Controls;

namespace Werd.Views
{
	public abstract class ShellTabView : Page, INotifyPropertyChanged
	{
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
		/// value is optional and can be provided automatically when invoked from compilers that
		/// support CallerMemberName.</param>
		/// <returns>True if the value was changed, false if the existing value matched the
		/// desired value.</returns>
		protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] String propertyName = null)
		{
			if (Equals(storage, value)) return false;

			storage = value;
			OnPropertyChanged(propertyName);
			return true;
		}

		/// <summary>
		/// Notifies listeners that a property value has changed.
		/// </summary>
		/// <param name="propertyName">Name of the property used to notify listeners.  This
		/// value is optional and can be provided automatically when invoked from compilers
		/// that support <see cref="CallerMemberNameAttribute"/>.</param>
		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			var eventHandler = PropertyChanged;
			if (eventHandler != null)
			{
				eventHandler(this, new PropertyChangedEventArgs(propertyName));
			}
		}
		#endregion

		public Shell Shell { get; set; }
		public Guid Id { get; } = Guid.NewGuid();

		public abstract string ViewIcons { get; set; }
		public abstract string ViewTitle { get; set; }

		public abstract event EventHandler<LinkClickedEventArgs> LinkClicked;

		public abstract event EventHandler<ShellMessageEventArgs> ShellMessage;

		private bool _hasFocus;
		public bool HasFocus { get => _hasFocus; set => SetProperty(ref _hasFocus, value); }
	}
}
