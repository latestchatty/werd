using Latest_Chatty_8.Common;
using Latest_Chatty_8.Settings;
using LatestChatty.Classes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Latest_Chatty_8.DataModel
{
	[DataContract]
	public class Comment : BindableBase
	{
		private int npcId = 0;
		/// <summary>
		/// Comment ID
		/// </summary>
		[DataMember]
		public int Id
		{
			get { return npcId; }
			set { this.SetProperty(ref this.npcId, value); }
		}

		private int npcStoryId = 0;
		/// <summary>
		/// ID of the story this comment is part of
		/// </summary>
		[DataMember]
		public int StoryId
		{
			get { return npcStoryId; }
			set { this.SetProperty(ref this.npcStoryId, value); }
		}

		private int npcReplyCount = 0;
		/// <summary>
		/// Count of replies to this comment
		/// </summary>
		[DataMember]
		public int ReplyCount
		{
			get { return npcReplyCount; }
			set { this.SetProperty(ref this.npcReplyCount, value); }
		}

		private PostCategory npcCategory = PostCategory.ontopic;
		/// <summary>
		/// Comment category - NWS, Political, etc.
		/// </summary>
		[DataMember]
		public PostCategory Category
		{
			get { return npcCategory; }
			set { this.SetProperty(ref this.npcCategory, value); }
		}

		private string npcAuthor = string.Empty;
		/// <summary>
		/// Comment author username
		/// </summary>
		[DataMember]
		public string Author
		{
			get { return npcAuthor; }
			set { this.SetProperty(ref this.npcAuthor, value); }
		}

		private string npcDateText = string.Empty;
		/// <summary>
		/// Date posted as a string
		/// </summary>
		[DataMember]
		public string DateText
		{
			get { return npcDateText; }
			set { this.SetProperty(ref this.npcDateText, value); }
		}

		private string npcPreview = string.Empty;
		/// <summary>
		/// Preview text
		/// </summary>
		[DataMember]
		public string Preview
		{
			get { return npcPreview; }
			set { this.SetProperty(ref this.npcPreview, value); }
		}

		private string npcBody = string.Empty;
		[DataMember]
		public string Body
		{
			get { return npcBody; }
			set { this.SetProperty(ref this.npcBody, value); }
		}

		/// <summary>
		/// Replies to this comment
		/// </summary>
		private ObservableCollection<Comment> npcReplies = new ObservableCollection<Comment>();
		[DataMember]
		public ObservableCollection<Comment> Replies
		{
			get { return npcReplies; }
			set { npcReplies = value; }
		}

		private bool npcUserParticipated = false;
		/// <summary>
		/// Indicates whether the currently logged in user has participated in this thread or not
		/// </summary>
		[DataMember]
		public bool UserParticipated
		{
			get { return npcUserParticipated; }
			set { this.SetProperty(ref this.npcUserParticipated, value); }
		}

		private bool npcUserIsAuthor = false;
		/// <summary>
		/// Indicates whether the currently logged in user is the author of this comment or not
		/// </summary>
		[DataMember]
		public bool UserIsAuthor
		{
			get { return npcUserIsAuthor; }
			set { this.SetProperty(ref this.npcUserIsAuthor, value); }
		}

		private bool npcHasNewReplies = false;
		/// <summary>
		/// Indicates if this comment has new replies since the last time it was loaded
		/// </summary>
		[DataMember]
		public bool HasNewReplies
		{
			get { return npcHasNewReplies; }
			set { this.SetProperty(ref this.npcHasNewReplies, value); }
		}

		private bool npcIsNew = true;
		/// <summary>
		/// Indicates if this is a brand new comment we've never seen before
		/// </summary>
		[DataMember]
		public bool IsNew
		{
			get { return npcIsNew; }
			set { this.SetProperty(ref this.npcIsNew, value); }
		}

		private int npcDepth = 0;
		/// <summary>
		/// Indicates the number of levels deep this comment is (How many parent comments)
		/// </summary>
		[DataMember]
		public int Depth
		{
			get { return npcDepth; }
			set { this.SetProperty(ref this.npcDepth, value); }
		}

		private bool npcIsPinned = false;
		/// <summary>
		/// Indicates if this comment is pinned or not
		/// </summary>
		[DataMember]
		public bool IsPinned
		{
			get { return npcIsPinned; }
			set
			{
				if (this.SetProperty(ref this.npcIsPinned, value))
				{
					if (value)
					{
						if(!LatestChattySettings.Instance.PinnedComments.Any(c => c.Id == this.Id))
							LatestChattySettings.Instance.AddPinnedComment(this);
					}
					else
					{
						if (LatestChattySettings.Instance.PinnedComments.Any(c => c.Id == this.Id))
							LatestChattySettings.Instance.RemovePinnedComment(this);
					}
				}
			}
		}

		private bool npcIsCollapsed = false;
		/// <summary>
		/// Indicates if this comment is collapsed or not
		/// </summary>
		[DataMember]
		public bool IsCollapsed
		{
			get { return npcIsCollapsed; }
			set { this.SetProperty(ref this.npcIsCollapsed, value); }
		}

		[IgnoreDataMember]
		public IEnumerable<Comment> FlattenedComments
		{
			get { return this.GetFlattenedComments(this); }
		}

		public Comment(int id, 
			int storyId, 
			int replyCount, 
			PostCategory category, 
			string author,
			string dateText,
			string preview,
			string body,
			bool userParticipated,
			int depth)
		{
			this.Id = id;
			this.StoryId = id;
			this.ReplyCount = replyCount;
			this.Category = category;
			this.Author = author;
			this.DateText = dateText;
			this.Preview = preview.Trim();
			this.Body = RewriteEmbeddedImage(body.Trim());
			this.Depth = depth;

			this.UserIsAuthor = this.Author.Equals(CoreServices.Instance.Credentials.UserName, StringComparison.OrdinalIgnoreCase);
			this.UserParticipated = userParticipated;
			this.IsNew = !CoreServices.Instance.PostCounts.ContainsKey(this.Id);
			this.HasNewReplies = (this.IsNew || CoreServices.Instance.PostCounts[this.Id] < this.ReplyCount);
			this.IsPinned = LatestChattySettings.Instance.PinnedComments.Any(c => c.Id == this.Id);
			this.CollapseIfRequired();
		}

		private void CollapseIfRequired()
		{
			//if (CoreServices.Instance.CollapseList.IsOnCollapseList(this))
			//{
			//	this.IsCollapsed= true;
			//}

			//TODO: Re-Implement post collapsing
			switch (this.Category)
			{
				case PostCategory.stupid:
					this.IsCollapsed = LatestChattySettings.Instance.AutoCollapseStupid;
					break;
				case PostCategory.offtopic:
					this.IsCollapsed = LatestChattySettings.Instance.AutoCollapseOffTopic;
					break;
				case PostCategory.nws:
					this.IsCollapsed = LatestChattySettings.Instance.AutoCollapseNws;
					break;
				case PostCategory.political:
					this.IsCollapsed = LatestChattySettings.Instance.AutoCollapsePolitical;
					break;
				case PostCategory.interesting:
					this.IsCollapsed = LatestChattySettings.Instance.AutoCollapseInteresting;
					break;
				case PostCategory.informative:
					this.IsCollapsed = LatestChattySettings.Instance.AutoCollapseInformative;
					break;
			}
		}
		
		private string RewriteEmbeddedImage(string s)
		{
			if (LatestChattySettings.Instance.ShowInlineImages && this.Category != PostCategory.nws)
			{
				//TODO: Tweak regex so it's a little smarter... maybe.  Require it to end with the image type?
				var withPreview = Regex.Replace(s, @">(?<link>https?://.*?\.(?:jpe?g|png|gif)).*?<", "><br/><img border=\"0\" class=\"embedded\" src=\"${link}\"/><");
				return withPreview.Replace("viewer.php?file=", @"files/");
			}
			return s;
		}

		private IEnumerable<Comment> GetFlattenedComments(Comment c)
		{
			yield return c;
			foreach (var comment in c.Replies)
				foreach (var com in GetFlattenedComments(comment))
					yield return com;
		}
	}
}
