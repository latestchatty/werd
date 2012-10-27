using Latest_Chatty_8.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Latest_Chatty_8.DataModel
{
	public class NewsStory : SquareGridData
	{
		private int npcStoryId;
		public int StoryId
		{
			get { return npcStoryId; }
			set { this.SetProperty(ref this.npcStoryId, value); }
		}

		private string npcPreviewText;
		public string PreviewText 
		{ 
			get { return this.npcPreviewText; } 
			set { this.SetProperty(ref this.npcPreviewText, value); } 
		}
		
		private int npcCommentCount;
		public int CommentCount 
		{ 
			get { return this.npcCommentCount; } 
			set { this.SetProperty(ref this.npcCommentCount, value); } 
		}
		
		private string npcDateText;
		public string DateText
		{
			get { return npcDateText; }
			set { this.SetProperty(ref this.npcDateText, value); }
		}

		private string npcStoryBody;
		public string StoryBody
		{
			get { return npcStoryBody; }
			set { this.SetProperty(ref this.npcStoryBody, value); }
		}

		public NewsStory(XElement x)
		{
			this.CommentCount = int.Parse(x.Element("comment-count").Value);
			if (x.Element("date") != null)
			{
				this.DateText = x.Element("date").Value;
			}
			this.Title = (x.Element("name").Value);
			this.StoryId = int.Parse(x.Element("id").Value);
			this.UniqueId = this.StoryId.ToString();
			this.StoryBody = StripHTML(x.Element("body").Value.Trim());
			this.PreviewText = (x.Element("preview").Value.Trim());
			this.Subtitle = string.Format("{0} Comments", this.CommentCount);
			//TODO: Get image.
		}

		private string StripHTML(string s)
		{
			return Regex.Replace(s, " target=\"_blank\"", string.Empty);
		}
	}
}
