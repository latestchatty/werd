namespace Werd.Views
{
	// https://github.com/microsoft/microsoft-ui-xaml/issues/1949#issuecomment-596837959
	[Windows.UI.Xaml.Data.Bindable]
	public class SearchWebView : ShackWebView { public override string ViewTitle => "Search"; }

	[Windows.UI.Xaml.Data.Bindable]
	public sealed class CustomSearchWebView : SearchWebView { public override string ViewTitle => "Custom Search"; }

	[Windows.UI.Xaml.Data.Bindable]
	public sealed class VanitySearchWebView : SearchWebView { public override string ViewTitle => "Vanity Search"; }

	[Windows.UI.Xaml.Data.Bindable]
	public sealed class MyPostsSearchWebView : SearchWebView { public override string ViewTitle => "My Posts Search"; }

	[Windows.UI.Xaml.Data.Bindable]
	public sealed class RepliesToMeSearchWebView : SearchWebView { public override string ViewTitle => "Replies Search"; }
}
