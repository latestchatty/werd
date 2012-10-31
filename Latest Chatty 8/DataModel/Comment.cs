using Latest_Chatty_8.Common;
using LatestChatty.Classes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Latest_Chatty_8.DataModel
{
	public class Comment : BindableBase
	{
		private int npcId = 0;
		/// <summary>
		/// Comment ID
		/// </summary>
		public int Id
		{
			get { return npcId; }
			set { this.SetProperty(ref this.npcId, value); }
		}

		private int npcStoryId = 0;
		/// <summary>
		/// ID of the story this comment is part of
		/// </summary>
		public int StoryId
		{
			get { return npcStoryId; }
			set { this.SetProperty(ref this.npcStoryId, value); }
		}

		private int npcReplyCount = 0;
		/// <summary>
		/// Count of replies to this comment
		/// </summary>
		public int ReplyCount
		{
			get { return npcReplyCount; }
			set { this.SetProperty(ref this.npcReplyCount, value); }
		}

		private PostCategory npcCategory = PostCategory.ontopic;
		/// <summary>
		/// Comment category - NWS, Political, etc.
		/// </summary>
		public PostCategory Category
		{
			get { return npcCategory; }
			set { this.SetProperty(ref this.npcCategory, value); }
		}

		private string npcAuthor = string.Empty;
		/// <summary>
		/// Comment author username
		/// </summary>
		public string Author
		{
			get { return npcAuthor; }
			set { this.SetProperty(ref this.npcAuthor, value); }
		}

		private string npcDateText = string.Empty;
		/// <summary>
		/// Date posted as a string
		/// </summary>
		public string DateText
		{
			get { return npcDateText; }
			set { this.SetProperty(ref this.npcDateText, value); }
		}

		private string npcPreview = string.Empty;
		/// <summary>
		/// Preview text
		/// </summary>
		public string Preview
		{
			get { return npcPreview; }
			set { this.SetProperty(ref this.npcPreview, value); }
		}

		private string npcBody = string.Empty;
		public string Body
		{
			get { return npcBody; }
			set { this.SetProperty(ref this.npcBody, value); }
		}

		/// <summary>
		/// Replies to this comment
		/// </summary>
		public ObservableCollection<Comment> Replies = new ObservableCollection<Comment>();

		private bool npcUserParticipated = false;
		/// <summary>
		/// Indicates whether the currently logged in user has participated in this thread or not
		/// </summary>
		public bool UserParticipated
		{
			get { return npcUserParticipated; }
			set { this.SetProperty(ref this.npcUserParticipated, value); }
		}

		private bool npcUserIsAuthor = false;
		/// <summary>
		/// Indicates whether the currently logged in user is the author of this comment or not
		/// </summary>
		public bool UserIsAuthor
		{
			get { return npcUserIsAuthor; }
			set { this.SetProperty(ref this.npcUserIsAuthor, value); }
		}

		private bool npcHasNewReplies = false;
		/// <summary>
		/// Indicates if this comment has new replies since the last time it was loaded
		/// </summary>
		public bool HasNewReplies
		{
			get { return npcHasNewReplies; }
			set { this.SetProperty(ref this.npcHasNewReplies, value); }
		}

		private bool npcIsNew = true;
		/// <summary>
		/// Indicates if this is a brand new comment we've never seen before
		/// </summary>
		public bool IsNew
		{
			get { return npcIsNew; }
			set { this.SetProperty(ref this.npcIsNew, value); }
		}

		private int npcDepth = 0;
		/// <summary>
		/// Indicates the number of levels deep this comment is (How many parent comments)
		/// </summary>
		public int Depth
		{
			get { return npcDepth; }
			set { this.SetProperty(ref this.npcDepth, value); }
		}

		private bool npcIsPinned = false;
		/// <summary>
		/// Indicates if this comment is pinned or not
		/// </summary>
		public bool IsPinned
		{
			get { return npcIsPinned; }
			set { this.SetProperty(ref this.npcIsPinned, value); }
		}

		private bool npcIsCollapsed = false;
		/// <summary>
		/// Indicates if this comment is collapsed or not
		/// </summary>
		public bool IsCollapsed
		{
			get { return npcIsCollapsed; }
			set { this.SetProperty(ref this.npcIsCollapsed, value); }
		}

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
			this.Body = RewriteEmbeddedImage(StripHTML(body.Trim()));
			this.Depth = depth;

			//TODO: Parse extra stuff
			this.UserIsAuthor = this.Author.Equals(CoreServices.Instance.Credentials.UserName, StringComparison.OrdinalIgnoreCase);
			this.UserParticipated = userParticipated;
			//TODO: Implement remembering posts we've seen.
			this.IsNew = true; // !CoreServices.Instance.PostSeenBefore(this.id);
			this.HasNewReplies = (true || this.IsNew);
			this.IsPinned = false; // CoreServices.Instance.WatchList.IsOnWatchList(this);
			this.CollapseIfRequired();
		}

		private void CollapseIfRequired()
		{
			//if (CoreServices.Instance.CollapseList.IsOnCollapseList(this))
			//{
			//	collapsed = true;
			//}

			//TODO: Re-Implement post collapsing
			switch (this.Category)
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
		}

		private string StripHTML(string s)
		{
			return Regex.Replace(s, " target=\"_blank\"", string.Empty);
		}

		private string RewriteEmbeddedImage(string s)
		{
			//TODO: Setting for embedded images.
			//if (LatestChattySettings.Instance.ShouldShowInlineImages && this.category != PostCategory.nws)
			if (this.Category != PostCategory.nws)
			{
				//TODO: Tweak regex so it's a little smarter... maybe.  Require it to end with the image type?
				//I assume the compiler handles making this a single object and not something that gets compiled every time this method gets called.
				//I reeeeeally hope so
				var withPreview = Regex.Replace(s, @">(?<link>https?://.*?\.(?:jpe?g|png|gif)).*?<", "><br/><img border=\"0\" style=\"vertical-align: middle; max-height: 450px; height: 450px;\" src=\"${link}\"/><");
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
