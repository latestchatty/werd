using System.Threading.Tasks;

namespace Latest_Chatty_8.Common
{
	public interface ICloudSync
	{
		Task Initialize();
		Task Sync();
		Task Suspend();
		/// <summary>
		/// Order to initialize with lower values being initialized first.
		/// </summary>
		int InitializePriority { get; }
	}
}
