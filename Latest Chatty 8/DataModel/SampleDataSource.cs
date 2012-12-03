using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel.Resources.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using System.Collections.Specialized;
using Latest_Chatty_8.DataModel;
using Latest_Chatty_8.Common;

// The data model defined by this file serves as a representative example of a strongly-typed
// model that supports notification when members are added, removed, or modified.  The property
// names chosen coincide with data bindings in the standard item templates.
//
// Applications may use this model as a starting point and build on it, or discard it entirely and
// replace it with something appropriate to their needs.

namespace Latest_Chatty_8.Data
{
	/// <summary>
	/// Base class for <see cref="SampleDataItem"/> and <see cref="SampleDataGroup"/> that
	/// defines properties common to both.
	/// </summary>
	[Windows.Foundation.Metadata.WebHostHidden]
	public abstract class SampleDataCommon : Latest_Chatty_8.Common.BindableBase
	{
		private static Uri _baseUri = new Uri("ms-appx:///");

		public SampleDataCommon(String uniqueId, String title, String subtitle, String imagePath, String description)
		{
			this._uniqueId = uniqueId;
			this._title = title;
			this._subtitle = subtitle;
			this._description = description;
			this._imagePath = imagePath;
		}

		private string _uniqueId = string.Empty;
		public string UniqueId
		{
			get { return this._uniqueId; }
			set { this.SetProperty(ref this._uniqueId, value); }
		}

		private string _title = string.Empty;
		public string Title
		{
			get { return this._title; }
			set { this.SetProperty(ref this._title, value); }
		}

		private string _subtitle = string.Empty;
		public string Subtitle
		{
			get { return this._subtitle; }
			set { this.SetProperty(ref this._subtitle, value); }
		}

		private string _description = string.Empty;
		public string Description
		{
			get { return this._description; }
			set { this.SetProperty(ref this._description, value); }
		}

		private ImageSource _image = null;
		private String _imagePath = null;
		public ImageSource Image
		{
			get
			{
				if (this._image == null && this._imagePath != null)
				{
					this._image = new BitmapImage(new Uri(SampleDataCommon._baseUri, this._imagePath));
				}
				return this._image;
			}

			set
			{
				this._imagePath = null;
				this.SetProperty(ref this._image, value);
			}
		}

		public void SetImage(String path)
		{
			this._image = null;
			this._imagePath = path;
			this.OnPropertyChanged("Image");
		}

		public override string ToString()
		{
			return this.Title;
		}
	}

	/// <summary>
	/// Generic item data model.
	/// </summary>
	public class SampleDataItem : SampleDataCommon
	{
		public SampleDataItem(String uniqueId, String title, String subtitle, String imagePath, String description, String content, SampleDataGroup group)
			: base(uniqueId, title, subtitle, imagePath, description)
		{
			this._content = content;
			this._group = group;
		}

		private string _content = string.Empty;
		public string Content
		{
			get { return this._content; }
			set { this.SetProperty(ref this._content, value); }
		}

		private SampleDataGroup _group;
		public SampleDataGroup Group
		{
			get { return this._group; }
			set { this.SetProperty(ref this._group, value); }
		}
	}

	/// <summary>
	/// Generic group data model.
	/// </summary>
	public class SampleDataGroup : SampleDataCommon
	{
		public SampleDataGroup(String uniqueId, String title, String subtitle, String imagePath, String description)
			: base(uniqueId, title, subtitle, imagePath, description)
		{
			Items.CollectionChanged += ItemsCollectionChanged;
		}

		private void ItemsCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			// Provides a subset of the full items collection to bind to from a GroupedItemsPage
			// for two reasons: GridView will not virtualize large items collections, and it
			// improves the user experience when browsing through groups with large numbers of
			// items.
			//
			// A maximum of 12 items are displayed because it results in filled grid columns
			// whether there are 1, 2, 3, 4, or 6 rows displayed

			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					if (e.NewStartingIndex < 12)
					{
						TopItems.Insert(e.NewStartingIndex, Items[e.NewStartingIndex]);
						if (TopItems.Count > 12)
						{
							TopItems.RemoveAt(12);
						}
					}
					break;
				case NotifyCollectionChangedAction.Move:
					if (e.OldStartingIndex < 12 && e.NewStartingIndex < 12)
					{
						TopItems.Move(e.OldStartingIndex, e.NewStartingIndex);
					}
					else if (e.OldStartingIndex < 12)
					{
						TopItems.RemoveAt(e.OldStartingIndex);
						TopItems.Add(Items[11]);
					}
					else if (e.NewStartingIndex < 12)
					{
						TopItems.Insert(e.NewStartingIndex, Items[e.NewStartingIndex]);
						TopItems.RemoveAt(12);
					}
					break;
				case NotifyCollectionChangedAction.Remove:
					if (e.OldStartingIndex < 12)
					{
						TopItems.RemoveAt(e.OldStartingIndex);
						if (Items.Count >= 12)
						{
							TopItems.Add(Items[11]);
						}
					}
					break;
				case NotifyCollectionChangedAction.Replace:
					if (e.OldStartingIndex < 12)
					{
						TopItems[e.OldStartingIndex] = Items[e.OldStartingIndex];
					}
					break;
				case NotifyCollectionChangedAction.Reset:
					TopItems.Clear();
					while (TopItems.Count < Items.Count && TopItems.Count < 12)
					{
						TopItems.Add(Items[TopItems.Count]);
					}
					break;
			}
		}

		private ObservableCollection<SampleDataItem> _items = new ObservableCollection<SampleDataItem>();
		public ObservableCollection<SampleDataItem> Items
		{
			get { return this._items; }
		}

		private ObservableCollection<SampleDataItem> _topItem = new ObservableCollection<SampleDataItem>();
		public ObservableCollection<SampleDataItem> TopItems
		{
			get { return this._topItem; }
		}
	}

	public sealed class SampleChattyComments
	{
		private ObservableCollection<Comment> _comments = new ObservableCollection<Comment>();
		public ObservableCollection<Comment> Comments
		{
			get { return this._comments; }
		}

		public SampleChattyComments()
		{
			var comments = CommentGenerator.GenerateComments(0, 1);
			foreach (var c in comments)
			{
				this.Comments.Add(c);
			}
		}
	}

	public sealed class SampleCommentThread : BindableBase
	{
		private ObservableCollection<Comment> _comments = new ObservableCollection<Comment>();
		public ObservableCollection<Comment> Comments
		{
			get { return this._comments; }
		}

		public SampleCommentThread()
		{
			var generatedComment = CommentGenerator.GenerateComments().First();
			var flattened = generatedComment.FlattenedComments;
			foreach (var c in flattened)
			{
				this.Comments.Add(c);
			}
		}
	}

	public static class CommentGenerator
	{
		public static List<Comment> GenerateComments(int depth = 0, int maxDepth = 2)
		{
			if (depth >= maxDepth) return null;
			var rand = new Random();
			var comments = new List<Comment>();
			//var commentsToGenerate = 1;

			//if (depth == 0 && maxDepth == 1)
			//{
			//	commentsToGenerate = 8;
			//}
			//else if (depth > 0)
			//{
			//	commentsToGenerate = rand.Next(1, 5);
			//}

			//for (int i = 0; i < commentsToGenerate; i++)
			//{
			//	List<Comment> nestedComments = null;
			//	if (rand.Next(100) >= 5)
			//	{
			//		nestedComments = GenerateComments(depth + 1, maxDepth);
			//	}
			//	var categoryOptions = Enum.GetValues(typeof(PostCategory));
			//	var category = (PostCategory)categoryOptions.GetValue(rand.Next(categoryOptions.Length - 1));
			//	var comment = new Comment(1, 17, nestedComments == null ? 0 : nestedComments.Count, category, Guid.NewGuid().ToString(), DateTime.Now.ToString(), "This is some preview text " + Guid.NewGuid().ToString(), "Blah blah blah blah blah blahBlah blah blah blah blah blahBlah blah blah blah blah blahBlah blah blah blah blah blahBlah blah blah blah blah blah", rand.Next(100) >= 5, depth);
			//	if (nestedComments != null)
			//	{
			//		foreach (var c in nestedComments)
			//		{
			//			comment.Replies.Add(c);
			//		}
			//	}
			//	comments.Add(comment);
			//}
			return comments;
		}

	}


	/// <summary>
	/// Creates a collection of groups and items with hard-coded content.
	/// 
	/// SampleDataSource initializes with placeholder data rather than live production
	/// data so that sample data is provided at both design-time and run-time.
	/// </summary>
	public sealed class SampleDataSource
	{
		private static SampleDataSource _sampleDataSource = new SampleDataSource();

		private ObservableCollection<SampleDataGroup> _allGroups = new ObservableCollection<SampleDataGroup>();
		public ObservableCollection<SampleDataGroup> AllGroups
		{
			get { return this._allGroups; }
		}

		public static IEnumerable<SampleDataGroup> GetGroups(string uniqueId)
		{
			if (!uniqueId.Equals("AllGroups")) throw new ArgumentException("Only 'AllGroups' is supported as a collection of groups");

			return _sampleDataSource.AllGroups;
		}

		public static SampleDataGroup GetGroup(string uniqueId)
		{
			// Simple linear search is acceptable for small data sets
			var matches = _sampleDataSource.AllGroups.Where((group) => group.UniqueId.Equals(uniqueId));
			if (matches.Count() == 1) return matches.First();
			return null;
		}

		public static SampleDataItem GetItem(string uniqueId)
		{
			// Simple linear search is acceptable for small data sets
			var matches = _sampleDataSource.AllGroups.SelectMany(group => group.Items).Where((item) => item.UniqueId.Equals(uniqueId));
			if (matches.Count() == 1) return matches.First();
			return null;
		}

		public SampleDataSource()
		{
			String ITEM_CONTENT = String.Format("Item Content: {0}\n\n{0}\n\n{0}\n\n{0}\n\n{0}\n\n{0}\n\n{0}",
							"Curabitur class aliquam vestibulum nam curae maecenas sed integer cras phasellus suspendisse quisque donec dis praesent accumsan bibendum pellentesque condimentum adipiscing etiam consequat vivamus dictumst aliquam duis convallis scelerisque est parturient ullamcorper aliquet fusce suspendisse nunc hac eleifend amet blandit facilisi condimentum commodo scelerisque faucibus aenean ullamcorper ante mauris dignissim consectetuer nullam lorem vestibulum habitant conubia elementum pellentesque morbi facilisis arcu sollicitudin diam cubilia aptent vestibulum auctor eget dapibus pellentesque inceptos leo egestas interdum nulla consectetuer suspendisse adipiscing pellentesque proin lobortis sollicitudin augue elit mus congue fermentum parturient fringilla euismod feugiat");

			var group1 = new SampleDataGroup("Group-1",
					  "WEEKEND CONFIRMED 136",
					  "Comments: 24",
					  "http://cf.shacknews.com/promos/WeekendConfirmed.jpg",
					  "Group Description: Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus tempor scelerisque lorem in vehicula. Aliquam tincidunt, lacus ut sagittis tristique, turpis massa volutpat augue, eu rutrum ligula ante a ante");
			#region ItemsData
			group1.Items.Add(new SampleDataItem("Group-1-Item-1",
			  "Item Title: 1",
			  "Item Subtitle: 1",
			  "Assets/LightGray.png",
			  "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
			  ITEM_CONTENT,
			  group1));
			group1.Items.Add(new SampleDataItem("Group-1-Item-2",
					  "Item Title: 2",
					  "Item Subtitle: 2",
					  "Assets/DarkGray.png",
					  "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
					  ITEM_CONTENT,
					  group1));
			group1.Items.Add(new SampleDataItem("Group-1-Item-3",
					  "Item Title: 3",
					  "Item Subtitle: 3",
					  "Assets/MediumGray.png",
					  "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
					  ITEM_CONTENT,
					  group1));
			group1.Items.Add(new SampleDataItem("Group-1-Item-4",
					  "Item Title: 4",
					  "Item Subtitle: 4",
					  "Assets/DarkGray.png",
					  "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
					  ITEM_CONTENT,
					  group1));
			group1.Items.Add(new SampleDataItem("Group-1-Item-5",
					  "Item Title: 5",
					  "Item Subtitle: 5",
					  "Assets/MediumGray.png",
					  "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
					  ITEM_CONTENT,
					  group1));
			#endregion
			this.AllGroups.Add(group1);

			var group2 = new SampleDataGroup("Group-2",
					  "SHACKNEWS DAILY OCT. 26",
					  "Comments: 0",
					  "http://cf.shacknews.com/promos/SN_dailya.jpg",
					  "Group Description: Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus tempor scelerisque lorem in vehicula. Aliquam tincidunt, lacus ut sagittis tristique, turpis massa volutpat augue, eu rutrum ligula ante a ante");
			this.AllGroups.Add(group2);

			var group3 = new SampleDataGroup("Group-3",
					  "MINECRAFT",
					  "Comments: 1234",
					  "http://cf.shacknews.com/promos/FC3meetsMinecraft.jpg",
					  "Group Description: Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus tempor scelerisque lorem in vehicula. Aliquam tincidunt, lacus ut sagittis tristique, turpis massa volutpat augue, eu rutrum ligula ante a ante");
			this.AllGroups.Add(group3);

			var group = new SampleDataGroup("G4 TV",
					  "G4 TV",
					  "Comments: 999",
					  "http://cf.shacknews.com/promos/G4.jpg",
					  "Group Description: Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus tempor scelerisque lorem in vehicula. Aliquam tincidunt, lacus ut sagittis tristique, turpis massa volutpat augue, eu rutrum ligula ante a ante");
			this.AllGroups.Add(group);

			group = new SampleDataGroup("Group-5",
					  "HITMAN: ABSOLUTION",
					  "Comments: 41",
					  "http://cf.shacknews.com/promos/HitmanAbsolution.jpg",
					  "Group Description: Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus tempor scelerisque lorem in vehicula. Aliquam tincidunt, lacus ut sagittis tristique, turpis massa volutpat augue, eu rutrum ligula ante a ante");
			this.AllGroups.Add(group);
		}
	}
}
