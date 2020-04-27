using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace Latest_Chatty_8.Settings
{
	[DataContract]
	public class CustomLauncher
	{
		[DataMember]
		public bool EmbeddedBrowser { get; set; }

		[DataMember]
		public bool Enabled { get; set; }

		[DataMember]
		public string Name { get; set; }

		private string _matchString;
		[DataMember]
		public string MatchString
		{
			get => _matchString;
			set
			{
				_matchString = value;
				Match = new Regex(value, RegexOptions.IgnoreCase | RegexOptions.Compiled);
			}
		}

		public Regex Match { get; private set; }

		[DataMember]
		public string Replace { get; set; }
	}
}
