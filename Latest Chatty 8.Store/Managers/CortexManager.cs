using Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Werd.Common;
using Werd.DataModel;

namespace Werd.Managers
{
	public class CortexManager : BindableBase, ICloudSync
	{
		private readonly AuthenticationManager _authManager;

		private CortexUser npcCurrentUser;
		public CortexUser CurrentUser
		{
			get => npcCurrentUser;
			set => SetProperty(ref npcCurrentUser, value);
		}

		public int InitializePriority => 1000;

		public CortexManager(AuthenticationManager authManager)
		{
			_authManager = authManager;
		}

		public async Task Initialize()
		{
			await Sync().ConfigureAwait(false);
		}

		public async Task Suspend()
		{
			await Task.CompletedTask.ConfigureAwait(false);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1812: Avoid uninstantiated internal classes")]
		private class CortexUserResult
		{
			public CortexUser UserData { get; set; }
		}

		public async Task Sync()
		{
			if (!_authManager.LoggedIn) return;

			var result = JsonConvert.DeserializeObject<CortexUserResult>(
				await JsonDownloader.DownloadJsonString(
					new Uri($"{Locations.GetCortexUser}?userName={_authManager.UserName}")).ConfigureAwait(false));

			await Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunOnUiThreadAndWait(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
			{
				CurrentUser = result.UserData;
			}).ConfigureAwait(false);
		}
	}
}
