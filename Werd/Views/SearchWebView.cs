namespace Werd.Views
{
	// https://github.com/microsoft/microsoft-ui-xaml/issues/1949#issuecomment-596837959
	[Windows.UI.Xaml.Data.Bindable]
	public class SearchWebView : ShackWebView { }

	[Windows.UI.Xaml.Data.Bindable]
	public sealed class CustomSearchWebView : SearchWebView { }

	[Windows.UI.Xaml.Data.Bindable]
	public sealed class VanitySearchWebView : SearchWebView { }

	[Windows.UI.Xaml.Data.Bindable]
	public sealed class MyPostsSearchWebView : SearchWebView { }

	[Windows.UI.Xaml.Data.Bindable]
	public sealed class RepliesToMeSearchWebView : SearchWebView { }
}
