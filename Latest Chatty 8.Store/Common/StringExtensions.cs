using System;

namespace Werd.Common
{
	public static class StringExtensions
	{
		public static int CountOccurrences(this string s, string pattern)
		{
			var count = 0;
			var position = 0;

			while ((position = s.IndexOf(pattern, position, StringComparison.Ordinal)) != -1)
			{
				count++;
				position += pattern.Length;
			}

			return count;
		}
	}
}
