using Latest_Chatty_8.Common;
using Latest_Chatty_8.Settings;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;

namespace Latest_Chatty_8.DataModel
{
	public class CommentThread : BindableBase
	{
		private LatestChattySettings settings;
		private ObservableCollection<Comment> comments;

		#region Properties
		private ReadOnlyObservableCollection<Comment> npcCommentsRO;
		public ReadOnlyObservableCollection<Comment> Comments { get { return npcCommentsRO; } private set { this.SetProperty(ref npcCommentsRO, value); } }

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

		private bool npcHasRepliesToUser;
		[DataMember]
		public bool HasRepliesToUser
		{
			get { return npcHasRepliesToUser; }
			set { this.SetProperty(ref this.npcHasRepliesToUser, value); }
		}

		private bool npcHasNewRepliesToUser;
		[DataMember]
		public bool HasNewRepliesToUser
		{
			get { return npcHasNewRepliesToUser; }
			set { this.SetProperty(ref this.npcHasNewRepliesToUser, value); }
		}

		private bool npcIsPinned = false;
		/// <summary>
		/// Indicates if this comment is pinned or not
		/// </summary>
		[DataMember]
		public bool IsPinned
		{
			get { return npcIsPinned; }
			set { this.SetProperty(ref this.npcIsPinned, value); }
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

		public bool IsExpired
		{
			get { return (this.Comments[0].Date.AddHours(18).ToUniversalTime() < DateTime.UtcNow); }
		}

		public bool NewlyAdded { get; set; }

		private bool npcViewedNewlyAdded;
		public bool ViewedNewlyAdded
		{
			get { return this.npcViewedNewlyAdded; }
			set { this.SetProperty(ref this.npcViewedNewlyAdded, value); }
		}
		#endregion

		#region Ctor
		public CommentThread(Comment rootComment, LatestChattySettings settings, bool newlyAdded = false)
		{
			this.settings = settings;
			this.comments = new ObservableCollection<Comment>();
			this.Comments = new ReadOnlyObservableCollection<Comment>(this.comments);

			this.Id = rootComment.Id;
			if (rootComment.AuthorType == AuthorType.Self) { this.UserParticipated = true; }
			this.HasNewReplies = rootComment.IsNew;
			this.NewlyAdded = newlyAdded;
			this.ViewedNewlyAdded = !newlyAdded;
			this.comments.Add(rootComment);
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// This is pretty shady but it will send a propertychanged event for the Date property causing bindings to be updated.
		/// </summary>
		public void ForceDateRefresh()
		{
			this.OnPropertyChanged("Date");
		}

		public void AddReply(Comment c, bool recalculateDepth = true)
		{
			//Can't directly add a parent comment.
			if (c.ParentId == 0) return;

			Comment insertAfter = null;
			var repliesToParent = this.comments.Where(c1 => c1.ParentId == c.ParentId);
			if (repliesToParent.Any())
			{
				//If there are replies, we need to figure out where we fit in.
				var lastReplyBeforeUs = repliesToParent.OrderBy(r => r.Id).LastOrDefault(r => r.Id < c.Id);  //Find the last reply that should come before this one.
				if (lastReplyBeforeUs != null)
				{
					insertAfter = FindLastCommentInChain(lastReplyBeforeUs);    //Now we look at all the replies to this comment, if any.  Find the last one of those.  That's where we need to insert ourselves.
				}
				else
				{
					//If there are no comments that come before this one, we find the parent comment and insert ourselves right after it.
					insertAfter = this.comments.SingleOrDefault(p => p.Id == c.ParentId);
				}
			}
			else
			{
				//If there aren't any replies to the parent of this post, we're the first one.  We'll just stick ourselves at the end.
				insertAfter = this.comments.SingleOrDefault(p => p.Id == c.ParentId);
			}
			if (insertAfter != null)
			{
				var location = this.comments.IndexOf(insertAfter);
				if (this.Comments.First().Author == c.Author)
				{
					c.AuthorType = AuthorType.ThreadOP;
				}
				this.comments.Insert(location + 1, c);
				if (c.AuthorType == AuthorType.Self)
				{
					this.UserParticipated = true;
				}
				//If we already have replies to the user, we don't have to update this.  Posts can get nuked but that happens elsewhere.
				if (!this.HasRepliesToUser)
				{
					this.HasRepliesToUser = this.Comments.Any(c1 => this.Comments.Any(c2 => c2.Id == c1.ParentId && c2.AuthorType == AuthorType.Self));
				}
				if (!this.HasNewRepliesToUser)
				{
					this.HasNewRepliesToUser = this.Comments.Any(c1 => c1.IsNew && this.Comments.Any(c2 => c2.Id == c1.ParentId && c2.AuthorType == AuthorType.Self));
				}
			}
			this.HasNewReplies = this.comments.Any(c1 => c1.IsNew);
			if (recalculateDepth)
			{
				this.RecalculateDepthIndicators();
			}
		}

		public void ChangeCommentCategory(int commentId, PostCategory newCategory)
		{
			var comment = this.comments.First(c => c.Id == commentId);
			if (newCategory == PostCategory.nuked)
			{
				try
				{
					this.RemoveAllChildComments(comment);
				}
				//It's hard to test nuked posts (yeah, yeah, unit testing...) so we'll just ignore it if it fails in "production", otherwise if there's a debugger attached we'll check it out.
				catch (Exception)
				{ System.Diagnostics.Debugger.Break(); }
			}
			else
			{
				comment.Category = newCategory;
			}
		}
		public void RecalculateDepthIndicators()
		{
			var orderedById = this.comments.OrderBy(c => c.Id);
			foreach (var c in this.comments)
			{
				var indicators = new char[c.Depth];
				for (var depth = 0; depth < c.Depth; depth++)
				{
					//Figure out if we're the last at our depth.
					if (depth == c.Depth - 1)
					{

						indicators[depth] = this.IsLastCommentAtDepth(c) ? TreeImageRepo.END : TreeImageRepo.JUNCTION;
					}
					else
					{
						var parentForDepth = this.FindParentAtDepth(c, depth + 1);
						if (!this.IsLastCommentAtDepth(parentForDepth))
						{
							indicators[depth] = TreeImageRepo.PASSTHROUGH;
						}
						else
						{
							indicators[depth] = TreeImageRepo.EMPTY;
						}
					}
				}
				c.DepthImage = TreeImageRepo.FetchTreeImage(indicators);
			}
		}

		#endregion

		#region Private Helpers
		private Comment FindParentAtDepth(Comment c, int depth)
		{
			var parent = this.comments.Single(c1 => c1.Id == c.ParentId);
			if (parent.Depth == depth)
			{
				return parent;
			}
			return FindParentAtDepth(parent, depth);
		}

		private bool IsLastCommentAtDepth(Comment c)
		{
			var threadsAtDepth = this.comments.Where(c1 => c1.ParentId == c.ParentId).OrderBy(c1 => c1.Id);
			return threadsAtDepth.Last().Id == c.Id;
		}

		private Comment FindLastCommentInChain(Comment c)
		{
			var childComments = this.comments.Where(c1 => c1.ParentId == c.Id);
			if (childComments.Any())
			{
				var lastComment = childComments.OrderBy(c1 => c1.Id).LastOrDefault();
				return FindLastCommentInChain(lastComment);
			}
			else
			{
				return c;
			}
		}

		private void RemoveAllChildComments(Comment start)
		{
			var commentsToRemove = this.comments.Where(c => c.ParentId == start.Id).ToList();
			foreach (var child in commentsToRemove)
			{
				RemoveAllChildComments(child);
			}
			this.comments.Remove(start);
		}
		#endregion
	}
}
