using Common;
using Werd.DataModel;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Werd.Managers
{
	//Right now this is unused because it would take so much work to support arbitrary tags.
	//Maybe some time in the future.
	public class AvailableTagsManager
	{
		public List<AvailableTag> AvailableTags { get; set; } = new List<AvailableTag>();

		public async Task Initialize()
		{
			var tags = await JsonDownloader.Download("https://www.shacknews.com/api2/api-index.php?action2=get_allowed_tags");
			foreach (var tag in tags["data"])
			{
				AvailableTags.Add(new AvailableTag()
				{
					Color = Windows.UI.Color.FromArgb(255, 255, 128, 0),
					Id = tag["tag_id"].Value<int>(),
					Tag = tag["tag"].ToString()
				});
			}
		}
	}
}
