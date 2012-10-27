using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Latest_Chatty_8.Networking
{
	public static class Locations
	{
		#region ServiceHost
		public const string ServiceHost = "http://shackapi.stonedonkey.com/";
		public const string PostUrl = ServiceHost + "post/";
		public const string Stories = ServiceHost + "stories.xml";

		#endregion
	}
}
