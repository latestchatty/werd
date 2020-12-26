using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Werd.Views
{
	[Windows.UI.Xaml.Data.Bindable]
	public class CortexWebView : ShackWebView { public override string ViewTitle => "Cortex"; }

	[Windows.UI.Xaml.Data.Bindable]
	public class CortexCreateWebView : CortexWebView { public override string ViewTitle => "Cortex Create"; }

	[Windows.UI.Xaml.Data.Bindable]
	public class CortexFeedWebView : CortexWebView { public override string ViewTitle => "Cortex Feed"; }

	[Windows.UI.Xaml.Data.Bindable]
	public class CortexAllPostsWebView : CortexWebView { public override string ViewTitle => "Cortex All Posts"; }

	[Windows.UI.Xaml.Data.Bindable]
	public class CortexMyPostsWebView : CortexWebView { public override string ViewTitle => "Cortex My Posts"; }

	[Windows.UI.Xaml.Data.Bindable]
	public class CortexDraftsWebView : CortexWebView { public override string ViewTitle => "Cortex Drafts"; }

	[Windows.UI.Xaml.Data.Bindable]
	public class CortexFollowingWebView : CortexWebView { public override string ViewTitle => "Cortex Following"; }
}
