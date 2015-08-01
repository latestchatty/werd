using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Latest_Chatty_8.Common
{
	public interface ICloudSync
	{
		Task Initialize();
		Task Sync();
		Task Suspend();
	}
}