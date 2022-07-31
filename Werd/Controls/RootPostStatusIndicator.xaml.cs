using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Werd.Controls
{
	public sealed partial class RootPostStatusIndicator : UserControl
	{
		private bool _viewedNewlyAdded;

		public bool ViewedNewlyAdded { get => _viewedNewlyAdded; set { _viewedNewlyAdded = value; SetStatus(); } }

		private bool _userParticipated;

		public bool UserParticipated { get => _userParticipated; set { _userParticipated = value; SetStatus(); } }

		private bool _hasNewRepliesToUser;

		public bool HasNewRepliesToUser { get => _hasNewRepliesToUser; set { _hasNewRepliesToUser = value; SetStatus(); } }

		private bool _isPinned;
		public bool IsPinned { get => _isPinned; set { _isPinned = value; SetStatus(); } }

		private bool _isCortex;
		public bool IsCortex { get => _isCortex; set { _isCortex = value; SetStatus(); } }

		public RootPostStatusIndicator()
		{
			this.InitializeComponent();
		}

		private void SetStatus()
		{
			status.Inlines.Clear();
			var enabledColor = new SolidColorBrush((Color)Application.Current.Resources["SystemAccentColor"]);
			var disabledColor = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255));

			status.Inlines.Add(new Run()
			{
				Text = "\uE909 ",
				Foreground = IsCortex ? enabledColor : disabledColor
			});

			var newThreadIcon = new Run() { Text = ViewedNewlyAdded ? "\uE734 " : "\uE735 ", Foreground = ViewedNewlyAdded ? disabledColor : enabledColor };
			status.Inlines.Add(newThreadIcon);

			var userParticipatedIcon = new Run() { Text = UserParticipated ? "\uE725 " : "\uE724 ", Foreground = UserParticipated ? enabledColor : disabledColor };
			status.Inlines.Add(userParticipatedIcon);

			var hasNewRepliesToUserIcon = new Run() { Text = HasNewRepliesToUser ? "\uE90A " : "\uE8BD ", Foreground = HasNewRepliesToUser ? enabledColor : disabledColor };
			status.Inlines.Add(hasNewRepliesToUserIcon);

			var isPinnedIcon = new Run() { Text = IsPinned ? "\uE841" : "\uE718", Foreground = IsPinned ? enabledColor : disabledColor };
			status.Inlines.Add(isPinnedIcon);
		}
	}
}
