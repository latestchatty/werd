using System.Collections.Generic;

namespace Common
{
	public class NotificationUser
	{
		public bool NotifyOnUserName { get; set; }


		[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only")]
		public List<string> NotificationKeywords { get; set; }
	}
}
