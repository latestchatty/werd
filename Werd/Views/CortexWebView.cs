namespace Werd.Views
{
	[Windows.UI.Xaml.Data.Bindable]
	public class CortexWebView : ShackWebView { }

	[Windows.UI.Xaml.Data.Bindable]
	public class CortexCreateWebView : CortexWebView { }

	[Windows.UI.Xaml.Data.Bindable]
	public class CortexFeedWebView : CortexWebView { }

	[Windows.UI.Xaml.Data.Bindable]
	public class CortexAllPostsWebView : CortexWebView { }

	[Windows.UI.Xaml.Data.Bindable]
	public class CortexMyPostsWebView : CortexWebView { }

	[Windows.UI.Xaml.Data.Bindable]
	public class CortexDraftsWebView : CortexWebView { }

	[Windows.UI.Xaml.Data.Bindable]
	public class CortexFollowingWebView : CortexWebView { }
}
