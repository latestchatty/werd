using System.Text.RegularExpressions;

namespace Latest_Chatty_8.Common
{
	public static class HtmlRemoval
	{
		/// <summary>
		/// Compiled regular expression for performance.
		/// </summary>
		static Regex tagsRegex = new Regex("<.*?>", RegexOptions.Compiled);
		static Regex spoilerRegex = new Regex("<span class=\"jt_spoiler\" onclick=\"this.className = '';\">.*?</span>", RegexOptions.Compiled);

		/// <summary>
		/// Remove HTML from string with compiled Regex.
		/// </summary>
		public static string StripTagsRegexCompiled(string source)
		{
			var result = spoilerRegex.Replace(source, "______");
			return tagsRegex.Replace(result, string.Empty);
		}
	}
}
