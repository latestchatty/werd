﻿using System.Text.RegularExpressions;

namespace Werd.Common
{
	public static class HtmlRemoval
	{
		/// <summary>
		/// Compiled regular expression for performance.
		/// </summary>
		static readonly Regex TagsRegex = new Regex("<.*?>", RegexOptions.Compiled);
		static readonly Regex SpoilerRegex = new Regex("<span class=\"jt_spoiler\" onclick=\"this.className = '';\">.*?</span>", RegexOptions.Compiled);

		/// <summary>
		/// Remove HTML from string with compiled Regex.
		/// </summary>
		public static string StripTagsRegexCompiled(string source, string replaceWith = "")
		{
			var result = SpoilerRegex.Replace(source, "______");
			return TagsRegex.Replace(result, replaceWith);
		}
	}
}
