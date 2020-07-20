using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Werd.DataModel
{
	internal class NotificationInfo
	{
		public string Type { get; set; }
		public string Title { get; set; }
		public int PostId { get; set; }
		public string Message { get; set; }
	}
}
