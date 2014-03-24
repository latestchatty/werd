using Latest_Chatty_8.Common;
using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Latest_Chatty_8.Views
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class InlineThreadView : LayoutAwarePage
	{
		public InlineThreadView()
		{
			this.InitializeComponent();
		}

		protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
		{
			var thread = navigationParameter as CommentThread;
			//If we have the entire comment passed as the navigation parameter, we can load the whole thing without hitting the interwebs.
			if (thread != null)
			{
				thread.HasNewReplies = false; //Viewed it, no longer has new replies.
				this.DefaultViewModel["Comments"] = thread.Comments;
			}
		}
	}
}
