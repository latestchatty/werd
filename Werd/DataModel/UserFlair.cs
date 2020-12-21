using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Werd.DataModel
{
	public class UserFlair
	{
		public bool IsModerator { get; set; }
		public bool IsTenYear { get; set; }
		public bool IsTwentyYear { get; set; }
		public MercuryStatus MercuryStatus { get; set; }
	}
}
