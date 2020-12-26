namespace Werd.Views
{
	//This was fun https://github.com/microsoft/microsoft-ui-xaml/issues/1949#issuecomment-596837959
	[Windows.UI.Xaml.Data.Bindable]
	public sealed class ModToolsWebView : ShackWebView
	{
		public override string ViewTitle => "Mod Tools";
	}
}
