using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Latest_Chatty_8.Common;
using Latest_Chatty_8;

namespace LatestChatty.Classes
{
	[DataContract]
	public class Comment : BindableBase
	{
		//TODO: Convert to BindableBase stuff.
		[DataMember]
		public string preview { get; set; }
		[DataMember]
		public int reply_count { get; set; }
		[DataMember]
		public PostCategory category { get; set; }
		[DataMember]
		public string dateText { get; set; }
		[DataMember]
		public int id { get; set; }
		[DataMember]
		public string author { get; set; }
		[DataMember]
		public int storyid { get; set; }
		[DataMember]
		public ObservableCollection<Comment> Comments { get; set; }
		[DataMember]
		public string body { get; set; }
		//Set to true if you have a reply below this comment
		[DataMember]
		public bool selfReply { get; set; }
		//Set to true if you are the author of this comment
		[DataMember]
		public bool myPost { get; set; }
		//True if there are new replies since the last time we loaded this comment
		[DataMember]
		public bool HasNewReplies { get; set; }
		//True if this is the first time we've seen this comment
		[DataMember]
		public bool New { get; set; }
		//Contains the number of new posts since we last loaded the comment.
		[DataMember]
		public int NewPostCount { get; set; }
		//If this is set, when we get the reply count we'll update the stored count if it's different.
		[DataMember]
		public bool SavePostCounts { get; set; }

		[DataMember]
		public int Depth { get; set; }

		[DataMember]
		public bool IsSelected { get; set; }

		[DataMember]
		public bool isPinned;
		public bool IsPinned
		{
			get { return this.isPinned; }
			set
			{
				if (this.SetProperty(ref this.isPinned, value))
				{
					if (this.IsPinned)
					{
						//TODO: Implement Pinning
						//CoreServices.Instance.WatchList.Add(this);
					}
					else
					{
						//CoreServices.Instance.WatchList.Remove(this);
					}
				}
			}
		}

		[DataMember]
		public bool isCollapsed;
		public bool IsCollapsed
		{
			get
			{
				return this.isCollapsed;
			}
			set
			{
				if (this.SetProperty(ref this.isCollapsed, value))
				{
					if (this.IsCollapsed)
					{
						//TODO: Implement collapsing
						//CoreServices.Instance.CollapseList.Add(this);
					}
					else
					{
						//CoreServices.Instance.CollapseList.Remove(this);
					}
				}
			}
		}

		public bool IsLoadMoreComment { get; private set; }

		public static Comment LoadMoreComment { get; private set; }

		static Comment()
		{
			LoadMoreComment = new Comment();
		}

		private Comment()
		{
			this.IsLoadMoreComment = true;
			this.SavePostCounts = false;
			this.reply_count = 0;
			this.dateText = string.Empty;
			this.id = 0;
			this.author = string.Empty;
			this.category = PostCategory.offtopic;
			this.body = string.Empty;
			this.storyid = 0;
			this.myPost = false;
			this.selfReply = false;
			this.New = false;
			this.NewPostCount = 0;
			this.Depth = 0;
			this.IsPinned = false;
			this.IsCollapsed = false;
			this.Comments = new ObservableCollection<Comment>();
		}

		public Comment(XElement x, int thisstoryid, bool saveCounts, int depth)
		{
			this.IsLoadMoreComment = false;
			this.SavePostCounts = saveCounts;
			this.reply_count = (int)x.Attribute("reply_count");
			this.dateText = (string)x.Attribute("date");
			this.id = (int)x.Attribute("id");
			this.author = (string)x.Attribute("author");
			this.category = (PostCategory)Enum.Parse(typeof(PostCategory), ((string)x.Attribute("category")).Trim(), true);
			this.body = RewriteEmbeddedImage(StripHTML(((string)x.Element("body")).Trim()));
			this.preview = ((string)x.Attribute("preview")).Trim();
			this.storyid = thisstoryid;
			var element = (from e in x.Elements()
								where e.Name == "participants"
								from p in e.Elements()
								where p.Name == "participant" && p.Value == CoreServices.Instance.Credentials.UserName
								select p).FirstOrDefault();
			this.selfReply = element != null;
			this.myPost = author == CoreServices.Instance.Credentials.UserName;
			//TODO: Implement remembering posts we've seen.
			this.New = true; // !CoreServices.Instance.PostSeenBefore(this.id);
			//TODO: Implemnet new post counts.
			this.NewPostCount = 0; //CoreServices.Instance.NewReplyCount(this.id, reply_count, this.SavePostCounts);
			this.HasNewReplies = (this.NewPostCount > 0 || this.New);
			this.Depth = depth;
			//TODO: Implement pinning.
			this.IsPinned = false; // CoreServices.Instance.WatchList.IsOnWatchList(this);
			this.CollapseIfRequired();

			List<XElement> comments = x.Element("comments").Elements("comment").ToList();
			Comments = new ObservableCollection<Comment>();
			if (comments.Count > 0)
			{
				foreach (XElement xchild in comments)
				{
					Comment child = new Comment(xchild, thisstoryid, this.SavePostCounts, depth + 1);
					Comments.Add(child);
				}
			}
		}

		private void CollapseIfRequired()
		{
			bool collapsed = false;

			//if (CoreServices.Instance.CollapseList.IsOnCollapseList(this))
			//{
			//	collapsed = true;
			//}

			//TODO: Re-Implement post collapsing
			switch (this.category)
			{
				//case PostCategory.stupid:
				//	collapsed = LatestChattySettings.Instance.AutoCollapseStupid;
				//	break;
				//case PostCategory.offtopic:
				//	collapsed = LatestChattySettings.Instance.AutoCollapseOffTopic;
				//	break;
				//case PostCategory.nws:
				//	collapsed = LatestChattySettings.Instance.AutoCollapseNws;
				//	break;
				//case PostCategory.political:
				//	collapsed = LatestChattySettings.Instance.AutoCollapsePolitical;
				//	break;
				//case PostCategory.interesting:
				//	collapsed = LatestChattySettings.Instance.AutoCollapseInteresting;
				//	break;
				//case PostCategory.informative:
				//	collapsed = LatestChattySettings.Instance.AutoCollapseInformative;
				//	break;
			}

			this.SetProperty(ref this.isCollapsed, collapsed, "IsCollapsed");
		}

		private string StripHTML(string s)
		{
			return Regex.Replace(s, " target=\"_blank\"", string.Empty);
		}

		private string RewriteEmbeddedImage(string s)
		{
			//TODO: Setting for embedded images.
			//if (LatestChattySettings.Instance.ShouldShowInlineImages && this.category != PostCategory.nws)
			if (this.category != PostCategory.nws)
			{
				//I assume the compiler handles making this a single object and not something that gets compiled every time this method gets called.
				//I reeeeeally hope so
				var withPreview = Regex.Replace(s, @">(?<link>https?://.*?\.(?:jpe?g|png|gif)).*?<", "><br/><img border=\"0\" style=\"vertical-align: middle; max-height: 100px; height: 100px;\" src=\"${link}\"/><");
				return withPreview.Replace("viewer.php?file=", @"files/");
			}
			return s;
		}

		public Comment GetChild(int searchid)
		{
			if (id == searchid)
			{
				return this;
			}
			Comment found;
			foreach (Comment c in Comments)
			{
				found = c.GetChild(searchid);
				if (found != null)
				{
					return found;
				}
			}
			return null;
		}
	}
}
