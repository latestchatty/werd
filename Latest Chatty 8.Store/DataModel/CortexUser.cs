using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Werd.DataModel
{
	public class CortexUser
	{
		public int UserId { get; set; }
		public string Username { get; set; }
		public int Points { get; set; }
		public int Comments { get; set; }
		public int CortexPosts { get; set; }
		public int Wins { get; set; }
		public DateTime RegistrationDate { get; set; }

	}
}
