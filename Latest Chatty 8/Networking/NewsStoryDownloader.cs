using Latest_Chatty_8.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Latest_Chatty_8.Networking
{
	public static class NewsStoryDownloader
	{
		public static async Task<IEnumerable<NewsStory>> DownloadStories()
		{
			var stories = new List<NewsStory>();
			var jsonStories = await JSONDownloader.Download(Locations.Stories);
			foreach (var jsonStory in jsonStories)
			{
				var story = new NewsStory(int.Parse((string)jsonStory["id"]), (string)jsonStory["name"], (string)jsonStory["preview"], (string)jsonStory["body"], int.Parse((string)jsonStory["comment_count"]), (string)jsonStory["date"]);
				stories.Add(story);
			}
			return stories;
		}
	}
}
