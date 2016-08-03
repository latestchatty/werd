using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Latest_Chatty_8.DataModel
{
	internal class KeywordMatch
	{
		public string Match { get; set; }
		public bool WholeWord { get; set; }
		public bool CaseSensitive { get; set; }

		public KeywordMatch()
		{

		}

		public KeywordMatch(string match, bool wholeWord, bool caseSensistive)
		{
			if (!caseSensistive)
			{
				match = match.ToLower();
			}
			if(wholeWord)
			{
				match = " " + match.Trim() + " ";
			}
			this.Match = match;
			this.WholeWord = wholeWord;
			this.CaseSensitive = caseSensistive;
		}

		public override bool Equals(object obj)
		{
			var o = obj as KeywordMatch;
			if (o == null) return false;
			return o.CaseSensitive == this.CaseSensitive &&
				o.WholeWord == this.WholeWord &&
				o.Match.Equals(this.Match);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}
}
