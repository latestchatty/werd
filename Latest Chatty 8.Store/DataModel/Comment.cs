using Common;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Werd.Settings;
using Windows.UI.Popups;
using Windows.UI.Xaml.Media;
using AuthenticationManager = Common.AuthenticationManager;

namespace Werd.DataModel
{
	[DataContract]
	public class Comment : BindableBase
	{
		private int npcId;
		private readonly AuthenticationManager _services;

		public double LastVisibleWidthCalculationSize { get; set; } = -1;

		/// <summary>
		/// Comment ID
		/// </summary>
		[DataMember]
		public int Id
		{
			get => npcId;
			set => SetProperty(ref npcId, value);
		}

		private int npcParentId;
		/// <summary>
		/// Comment Paret ID
		/// </summary>
		[DataMember]
		public int ParentId
		{
			get => npcParentId;
			set => SetProperty(ref npcParentId, value);
		}

		private PostCategory npcCategory = PostCategory.ontopic;
		/// <summary>
		/// Comment category - NWS, Political, etc.
		/// </summary>
		[DataMember]
		public PostCategory Category
		{
			get => npcCategory;
			set => SetProperty(ref npcCategory, value);
		}

		private string npcAuthor = string.Empty;
		/// <summary>
		/// Comment author username
		/// </summary>
		[DataMember]
		public string Author
		{
			get => npcAuthor;
			set => SetProperty(ref npcAuthor, value);
		}

		private string npcDateText = string.Empty;
		/// <summary>
		/// Date posted as a string
		/// </summary>
		[DataMember]
		public string DateText
		{
			get => npcDateText;
			set => SetProperty(ref npcDateText, value);
		}

		private DateTime npcDate;
		/// <summary>
		/// Gets or sets the date.
		/// </summary>
		[DataMember]
		public DateTime Date
		{
			get => npcDate;
			set => SetProperty(ref npcDate, value);
		}

		private string npcPreview = string.Empty;
		/// <summary>
		/// Preview text
		/// </summary>
		[DataMember]
		public string Preview
		{
			get => npcPreview;
			set => SetProperty(ref npcPreview, value);
		}

		private Brush npcPreviewColor;
		[DataMember]
		public Brush PreviewColor
		{
			get => npcPreviewColor;
			set => SetProperty(ref npcPreviewColor, value);
		}

		private string npcBody = string.Empty;
		[DataMember]
		public string Body
		{
			get => npcBody;
			set => SetProperty(ref npcBody, value);
		}

		private bool npcIsNew = true;
		/// <summary>
		/// Indicates if this is a brand new comment we've never seen before
		/// </summary>
		[DataMember]
		public bool IsNew
		{
			get => npcIsNew;
			set => SetProperty(ref npcIsNew, value);
		}

		private bool npcIsRootPost = true;
		/// <summary>
		/// Indicates if this is the root post of the thread
		/// </summary>
		[DataMember]
		public bool IsRootPost
		{
			get => npcIsRootPost;
			set => SetProperty(ref npcIsRootPost, value);
		}

		/// <summary>
		/// Indicates the number of levels deep this comment is (How many parent comments)
		/// </summary>
		[DataMember]
		public int Depth { get; set; }

		private int npcLolCount;
		/// <summary>
		/// Indicates how many times this comment has been Lol'd
		/// </summary>
		[DataMember]
		public int LolCount
		{
			get => npcLolCount;
			set { SetProperty(ref npcLolCount, value); LolUpdateTime = DateTime.Now; }
		}

		private int npcInfCount;
		/// <summary>
		/// Indicates how many times this comment has been Inf'd
		/// </summary>
		[DataMember]
		public int InfCount
		{
			get => npcInfCount;
			set { SetProperty(ref npcInfCount, value); LolUpdateTime = DateTime.Now; }
		}

		private int npcUnfCount;
		/// <summary>
		/// Indicates how many times this comment has been Unf'd
		/// </summary>
		[DataMember]
		public int UnfCount
		{
			get => npcUnfCount;
			set { SetProperty(ref npcUnfCount, value); LolUpdateTime = DateTime.Now; }
		}

		private int npcTagCount;
		/// <summary>
		/// Indicates how many times this comment has been Tag'd
		/// </summary>
		[DataMember]
		public int TagCount
		{
			get => npcTagCount;
			set { SetProperty(ref npcTagCount, value); LolUpdateTime = DateTime.Now; }
		}

		private int npcWtfCount;
		/// <summary>
		/// Indicates how many times this comment has been Wtf'd
		/// </summary>
		[DataMember]
		public int WtfCount
		{
			get => npcWtfCount;
			set { SetProperty(ref npcWtfCount, value); LolUpdateTime = DateTime.Now; }
		}

		private int npcWowCount;
		/// <summary>
		/// Indicates how many times this comment has been Ugh'd
		/// </summary>
		[DataMember]
		public int WowCount
		{
			get => npcWowCount;
			set { SetProperty(ref npcWowCount, value); LolUpdateTime = DateTime.Now; }
		}

		private int npcAwwCount;
		/// <summary>
		/// Indicates how many times this comment has been Ugh'd
		/// </summary>
		[DataMember]
		public int AwwCount
		{
			get => npcAwwCount;
			set { SetProperty(ref npcAwwCount, value); LolUpdateTime = DateTime.Now; }
		}

		private DateTime npcLolUpdateTime = DateTime.MinValue;

		[DataMember]
		public DateTime LolUpdateTime
		{
			get => npcLolUpdateTime;
			set => SetProperty(ref npcLolUpdateTime, value);
		}

		[DataMember]
		public AuthorType AuthorType { get; set; }

		//HACK: Visual state shouldn't make it into the model, but I'm not going to go through all the work to implement proper MVVM right now.
		private bool npcIsSelected;
		public bool IsSelected
		{
			get => npcIsSelected;
			set => SetProperty(ref npcIsSelected, value);
		}

		private bool npcIsCortex;
		public bool IsCortex
		{
			get => npcIsCortex;
			set => SetProperty(ref npcIsCortex, value);
		}

		private bool npcIsLastComment;
		public bool IsLastComment
		{
			get => npcIsLastComment;
			set => SetProperty(ref npcIsLastComment, value);
		}

		private bool npcShowReply;
		public bool ShowReply
		{
			get => npcShowReply;
			set => SetProperty(ref npcShowReply, value);
		}

		private string npcDepthText;
		public string DepthText
		{
			get => npcDepthText;
			set => SetProperty(ref npcDepthText, value);
		}

		private CommentThread npcCommentThread;
		public CommentThread Thread
		{
			get => npcCommentThread;
			set => SetProperty(ref npcCommentThread, value);
		}

		private LatestChattySettings npcSettings;
		public LatestChattySettings Settings
		{
			get => npcSettings;
			set => SetProperty(ref npcSettings, value);
		}

		private string npcPendingReplyText;
		public string PendingReplyText
		{
			get => npcPendingReplyText;
			set => SetProperty(ref npcPendingReplyText, value);
		}

		private UserFlair npcAuthorFlair;
		public UserFlair AuthorFlair
		{
			get => npcAuthorFlair;
			set => SetProperty(ref npcAuthorFlair, value);
		}

		public Comment(
			int id,
			PostCategory category,
			string author,
			string dateText,
			string preview,
			string body,
			int depth,
			int parentId,
			bool isCortex,
			UserFlair authorFlair,
			AuthenticationManager services,
			SeenPostsManager seenPostsManager)
		{
			Settings = AppGlobal.Settings;
			_services = services;
			Id = id;
			ParentId = parentId;
			Category = category;
			IsCortex = isCortex;
			AuthorFlair = authorFlair;
			//If the post was made by the "shacknews" user, it's a news article and we want to categorize it differently.
			if (author.Equals("shacknews", StringComparison.OrdinalIgnoreCase))
			{
				Category = PostCategory.newsarticle;
				AuthorType = AuthorType.Shacknews;
			}
			Author = author;
			if (dateText.Length > 0)
			{
				Date = DateTime.Parse(dateText, null, DateTimeStyles.AssumeUniversal);
				DateText = Date.ToString("MMM d, yyyy h:mm tt", CultureInfo.CurrentCulture);
			}
			Preview = preview.Trim();
			Body = body.Trim();
			Depth = depth;
			if (Author.Equals(_services.UserName, StringComparison.OrdinalIgnoreCase))
			{
				AuthorType = AuthorType.Self;
			}
			//We've already seen posts we made.  No need to mark them new.
			if (AuthorType == AuthorType.Self)
			{
				seenPostsManager.MarkCommentSeen(id);
			}
			IsNew = seenPostsManager.IsCommentNew(id);
			IsRootPost = ParentId == 0;
		}


		/// <summary>
		/// Tags a comment.  If the commment is already tagged, it will be untagged.
		/// </summary>
		/// <param name="tag">Tag to apply, lol, inf, etc.</param>
		/// <returns></returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase")]
		public async Task LolTag(string tag)
		{
			if (!_services.LoggedIn)
			{
				var dlg = new MessageDialog("You must be logged in to use lol tags.");
				await dlg.ShowAsync();
				return;
			}

			var result = await JsonDownloader.Download(Locations.GetLolTaggersUrl(Id, tag)).ConfigureAwait(true);
			var taggers = new List<string>();
			if (result["data"].ToString().Length > 0)
			{
				taggers = result["data"].First["usernames"].Select(a => a.ToString().ToLowerInvariant()).ToList();
			}

			int delta;

			if (!taggers.Contains(_services.UserName.ToLowerInvariant()))
			{
				delta = 1;
				result = await JsonDownloader.Download(Locations.TagPost(Id, _services.UserName, tag)).ConfigureAwait(true);
			}
			else
			{
				delta = -1;
				result = await JsonDownloader.Download(Locations.UntagPost(Id, _services.UserName, tag)).ConfigureAwait(true);
			}


			if (result["status"].ToString() == "1")
			{
				switch (tag)
				{
					case "lol":
						LolCount += delta;
						break;
					case "inf":
						InfCount += delta;
						break;
					case "unf":
						UnfCount += delta;
						break;
					case "tag":
						TagCount += delta;
						break;
					case "wtf":
						WtfCount += delta;
						break;
					case "wow":
						WowCount += delta;
						break;
					case "aww":
						AwwCount += delta;
						break;
				}
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase")]
		public async Task<bool> Moderate(string category)
		{
			using (var response = await PostHelper.Send(Locations.ModeratePost,
				new List<KeyValuePair<string, string>>
				{
					new KeyValuePair<string, string>("postId", this.Id.ToString(CultureInfo.InvariantCulture)),
					new KeyValuePair<string, string>("category", category.ToLowerInvariant())
				},
				true, _services).ConfigureAwait(true))
			{
				if (response.StatusCode == HttpStatusCode.OK)
				{
					var data = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
					var result = JObject.Parse(data);
					if (result.ContainsKey("result") && result["result"].ToString().ToLowerInvariant().Equals("success", StringComparison.Ordinal))
					{
						return true;
					}
				}
			}
			return false;
		}
	}
}
