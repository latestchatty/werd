using Latest_Chatty_8.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Latest_Chatty_8.DataModel
{
	[DataContract]
	public class NewsStory : SquareGridData
	{
		[DataMember]
		public int npcStoryId;
		public int StoryId
		{
			get { return npcStoryId; }
			set { this.SetProperty(ref this.npcStoryId, value); }
		}

		[DataMember]
		public string npcPreviewText;
		public string PreviewText 
		{ 
			get { return this.npcPreviewText; } 
			set { this.SetProperty(ref this.npcPreviewText, value); } 
		}
		
		[DataMember]
		public int npcCommentCount;
		public int CommentCount 
		{ 
			get { return this.npcCommentCount; } 
			set { this.SetProperty(ref this.npcCommentCount, value); } 
		}
		
		[DataMember]
		public string npcDateText;
		public string DateText
		{
			get { return npcDateText; }
			set { this.SetProperty(ref this.npcDateText, value); }
		}

		[DataMember]
		public string npcStoryBody;
		public string StoryBody
		{
			get { return npcStoryBody; }
			set { this.SetProperty(ref this.npcStoryBody, value); }
		}

		public NewsStory(int id, string title, string preview, string body, int commentCount, string dateString, string imageUrl)
		{
			this.CommentCount = commentCount; 
			this.DateText = dateString;
			this.Title = title;
			this.StoryId = id;
			this.UniqueId = id.ToString();
			this.StoryBody = StripHTML(body);
			this.PreviewText = preview;
			this.Subtitle = string.Format("{0} Comments", this.CommentCount);
			if (imageUrl != null)
			{
				this.SetImage(imageUrl);
			}
		}

		private string StripHTML(string s)
		{
			return Regex.Replace(s, " target=\"_blank\"", string.Empty);
		}
	}
}
