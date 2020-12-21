using System.Globalization;

namespace Werd.DataModel
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
				match = match.ToLower(CultureInfo.CurrentUICulture);
			}
			if (wholeWord)
			{
				match = " " + match.Trim() + " ";
			}
			Match = match;
			WholeWord = wholeWord;
			CaseSensitive = caseSensistive;
		}

		public override bool Equals(object obj)
		{
			var o = obj as KeywordMatch;
			if (o == null) return false;
			return o.CaseSensitive == CaseSensitive &&
				o.WholeWord == WholeWord &&
				o.Match.Equals(Match, System.StringComparison.Ordinal);
		}

		public override int GetHashCode()
		{
			// ReSharper disable once BaseObjectGetHashCodeCallInGetHashCode
			return base.GetHashCode();
		}
	}
}
