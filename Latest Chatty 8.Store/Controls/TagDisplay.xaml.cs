using Common;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Werd.Common;
using Werd.DataModel;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Werd.Controls
{
	public sealed partial class TagDisplay : UserControl, INotifyPropertyChanged
	{
		private Comment _comment;

		public Comment Comment
		{
			get => _comment;
			set => SetProperty(ref _comment, value);
		}

		private Orientation _orientation;
		public Orientation Orientation
		{
			get => _orientation;
			set => SetProperty(ref _orientation, value);
		}

		public event EventHandler<ShellMessageEventArgs> ShellMessage;

		public TagDisplay()
		{
			this.InitializeComponent();
		}

		private async Task ShowTaggers(Button button, int commentId)
		{
			try
			{
				if (button == null) return;
				button.IsEnabled = false;
				var tag = button.Tag as string;
				await AppGlobal.DebugLog.AddMessage("ViewedTagCount-" + tag).ConfigureAwait(true);
				var lolUrl = Locations.GetLolTaggersUrl(commentId, tag);
				var response = await JsonDownloader.DownloadObject(lolUrl).ConfigureAwait(true);
				var names = string.Join(Environment.NewLine, response["data"][0]["usernames"].Select(a => a.ToString()).OrderBy(a => a));
				var flyout = new Flyout();
				var tb = new TextBlock();
				tb.Text = names;
				flyout.Content = tb;
				flyout.ShowAt(button);
			}
			catch (Exception ex)
			{
				await AppGlobal.DebugLog.AddException(string.Empty, ex).ConfigureAwait(true);
				ShellMessage?.Invoke(this, new ShellMessageEventArgs("Error retrieving taggers. Try again later.", ShellMessageType.Error));
			}
			finally
			{
				if (button != null)
				{
					button.IsEnabled = true;
				}
			}
		}

		private async void LolTagTapped(object sender, TappedRoutedEventArgs e)
		{
			var b = sender as Button;
			var id = (b?.DataContext as Comment)?.Id;
			if (b == null || id == null) return;
			await ShowTaggers(b, id.Value).ConfigureAwait(true);
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
		/// value is optional and can be provided automatically when invoked from compilers that
		/// support CallerMemberName.</param>
		/// <returns>True if the value was changed, false if the existing value matched the
		/// desired value.</returns>
		private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] String propertyName = null)
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
		private void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion
	}
}
