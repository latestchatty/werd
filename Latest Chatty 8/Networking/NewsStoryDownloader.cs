using Latest_Chatty_8.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace Latest_Chatty_8.Networking
{
	/// <summary>
	/// News story download helper
	/// </summary>
	public static class NewsStoryDownloader
	{
		private static Regex findImageRegex = new Regex(@".*<img class=\""little-promo.*src=\""(?<imgUrl>\S*)\""");
		/// <summary>
		/// Downloads the stories.
		/// </summary>
		/// <returns></returns>
		public static async Task<IEnumerable<NewsStory>> DownloadStories()
		{
			var stories = new List<NewsStory>();
			var jsonStories = await JSONDownloader.DownloadArray(Locations.Stories);
			foreach (var jsonStory in jsonStories)
			{
				var body = (string)jsonStory["body"];
				var story = new NewsStory(
					int.Parse((string)jsonStory["id"]),
					(string)jsonStory["name"],
					(string)jsonStory["preview"],
					body,
					int.Parse((string)jsonStory["comment_count"]),
					(string)jsonStory["date"],
					findImageRegex.IsMatch(body) ? findImageRegex.Match(body).Groups["imgUrl"].Value : null,
					(string)jsonStory["url"]);

				stories.Add(story);
			}
			return stories;
		}
	}
}
