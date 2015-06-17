using Latest_Chatty_8.Shared;
using Latest_Chatty_8.Shared.DataModel;
using Latest_Chatty_8.Shared.Networking;
using Latest_Chatty_8.Shared.Settings;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Media;

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

		private int npcParentId = 0;
		/// <summary>
		/// Comment Paret ID
		/// </summary>
		[DataMember]
		public int ParentId
		{
			get { return npcParentId; }
			set { this.SetProperty(ref this.npcParentId, value); }
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

		private DateTime npcDate;
		/// <summary>
		/// Gets or sets the date.
		[DataMember]
		public DateTime Date
		{
			get { return npcDate; }
			set { this.SetProperty(ref this.npcDate, value); }
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

		private bool npcAuthorIsOriginalParent;
		/// <summary>
		/// Indicates the author of this post is the author who posted the root comment.
		/// </summary>
		[DataMember]
		public bool AuthorIsOriginalParent
		{
			get { return npcAuthorIsOriginalParent; }
			set { this.SetProperty(ref this.npcAuthorIsOriginalParent, value); }
		}

		private int npcLolCount = 0;
		/// <summary>
		/// Indicates how many times this comment has been Lol'd
		/// </summary>
		[DataMember]
		public int LolCount
		{
			get { return npcLolCount; }
			set { this.SetProperty(ref this.npcLolCount, value); this.LolUpdateTime = DateTime.Now; }
		}

		private int npcInfCount = 0;
		/// <summary>
		/// Indicates how many times this comment has been Inf'd
		/// </summary>
		[DataMember]
		public int InfCount
		{
			get { return npcInfCount; }
			set { this.SetProperty(ref this.npcInfCount, value); this.LolUpdateTime = DateTime.Now; }
		}

		private int npcUnfCount = 0;
		/// <summary>
		/// Indicates how many times this comment has been Unf'd
		/// </summary>
		[DataMember]
		public int UnfCount
		{
			get { return npcUnfCount; }
			set { this.SetProperty(ref this.npcUnfCount, value); this.LolUpdateTime = DateTime.Now; }
		}

		private int npcTagCount = 0;
		/// <summary>
		/// Indicates how many times this comment has been Tag'd
		/// </summary>
		[DataMember]
		public int TagCount
		{
			get { return npcTagCount; }
			set { this.SetProperty(ref this.npcTagCount, value); this.LolUpdateTime = DateTime.Now; }
		}

		private int npcWtfCount = 0;
		/// <summary>
		/// Indicates how many times this comment has been Wtf'd
		/// </summary>
		[DataMember]
		public int WtfCount
		{
			get { return npcWtfCount; }
			set { this.SetProperty(ref this.npcWtfCount, value); this.LolUpdateTime = DateTime.Now; }
		}

		private int npcUghCount = 0;
		/// <summary>
		/// Indicates how many times this comment has been Ugh'd
		/// </summary>
		[DataMember]
		public int UghCount
		{
			get { return npcUghCount; }
			set { this.SetProperty(ref this.npcUghCount, value); this.LolUpdateTime = DateTime.Now; }
		}

		private DateTime npcLolUpdateTime = DateTime.MinValue;

		[DataMember]
		public DateTime LolUpdateTime
		{
			get { return this.npcLolUpdateTime; }
			set { this.SetProperty(ref this.npcLolUpdateTime, value); }
		}

		[DataMember]
		public AuthorType AuthorType { get; set; }

		public Comment(int id,
			PostCategory category,
			string author,
			string dateText,
			string preview,
			string body,
			int depth,
			int parentId,
			bool isNew)
		{
			this.Id = id;
			this.ParentId = parentId;
			this.Category = category;
			//If the post was made by the "shacknews" user, it's a news article and we want to categorize it differently.
			if (author.Equals("shacknews", StringComparison.OrdinalIgnoreCase))
			{
				this.Category = PostCategory.newsarticle;
				this.AuthorType = AuthorType.Shacknews;
			}
			this.Author = author;
			//PDT -7, PST -8 GMT
			if (dateText.Length > 0)
			{
				this.Date = DateTime.Parse(dateText.Replace(" PDT", "-7:00").Replace(" PST", "-8:00"));
				this.DateText = this.Date.ToString("MMM d, yyyy h:mm tt");
			}
			this.Preview = preview.Trim();
			this.Body = RewriteEmbeddedImage(body.Trim());
			this.Depth = depth;
			if(this.Author.Equals(CoreServices.Instance.Credentials.UserName, StringComparison.OrdinalIgnoreCase))
			{
				this.AuthorType = AuthorType.Self;
			}
			this.IsNew = isNew;
		}

		private string RewriteEmbeddedImage(string s)
		{
			if (LatestChattySettings.Instance.ShowInlineImages && this.Category != PostCategory.nws)
			{
				var withPreview = Regex.Replace(s, @">(?<link>https?://[A-Za-z0-9-\._~:/\?#\[\]@!\$&'\(\)*\+,;=]*\.(?:jpe?g|png|gif))(&#13;)?<", "><br/><img border=\"0\" src=\"" + WebBrowserHelper.LoadingImage + "\" onload=\"(function(e) {loadImage(e, '${link}')})(this)\" class=\"embedded\"/><br /><");
				return withPreview.Replace("viewer.php?file=", @"files/");
			}
			return s;
		}

		async public Task LolTag(string tag)
		{
			if (!CoreServices.Instance.LoggedIn)
			{
				var dlg = new Windows.UI.Popups.MessageDialog("You must be logged in to use lol tags.");
				await dlg.ShowAsync();
				return;
			}
			//var data = 'who=' + user + '&what=' + id + '&tag=' + tag + '&version=' + LOL.VERSION;
			await POSTHelper.Send(Locations.LolSubmit,
				new List<KeyValuePair<string, string>> {
					new KeyValuePair<string, string>("who", CoreServices.Instance.Credentials.UserName),
					new KeyValuePair<string, string>("what", this.Id.ToString()),
					new KeyValuePair<string, string>("tag", tag),
					new KeyValuePair<string, string>("version", "-1")
				}, false);

			switch (tag)
			{
				case "lol":
					this.LolCount++;
					break;
				case "inf":
					this.InfCount++;
					break;
				case "unf":
					this.UnfCount++;
					break;
				case "tag":
					this.TagCount++;
					break;
				case "wtf":
					this.WtfCount++;
					break;
				case "ugh":
					this.UghCount++;
					break;
			}
		}
	}
}
