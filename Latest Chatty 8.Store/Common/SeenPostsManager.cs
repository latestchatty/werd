using Latest_Chatty_8.Shared.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Latest_Chatty_8.Common
{
	public class SeenPostsManager
	{
		/// <summary>
		/// List of posts we've seen before.
		/// </summary>
		private List<int> SeenPosts { get; set; }
		private System.Threading.Timer persistenceTimer;
		object locker = new object();

		public SeenPostsManager()
		{
			this.SeenPosts = new List<int>();
        }

		public async Task Initialize()
		{
			this.SeenPosts = (await ComplexSetting.ReadSetting<List<int>>("seenposts")) ?? new List<int>();
			this.persistenceTimer = new System.Threading.Timer(async (a) => await SaveSeenPosts(), null, 10000, System.Threading.Timeout.Infinite);
		}

		public bool IsCommentNew(int postId)
		{
			lock(locker)
			{
				var result = !this.SeenPosts.Contains(postId);
				return result;
			}
		}

		public void MarkCommentSeen(int postId)
		{
			lock(locker)
			{
				this.SeenPosts.Add(postId);
			}
		}

		private async Task SaveSeenPosts()
		{

			lock (locker)
			{
				if (this.SeenPosts.Count > 50000)
				{
					this.SeenPosts = this.SeenPosts.Skip(this.SeenPosts.Count - 50000) as List<int>;
				}
			}
			await ComplexSetting.SetSetting<List<int>>("seenposts", this.SeenPosts);
			System.Diagnostics.Debug.WriteLine("Saving seen posts.");
			this.persistenceTimer = new System.Threading.Timer(async (a) => await SaveSeenPosts(), null, 10000, System.Threading.Timeout.Infinite);
		}
	}
}
