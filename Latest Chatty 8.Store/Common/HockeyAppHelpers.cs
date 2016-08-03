using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Latest_Chatty_8.Common
{
	public static class HockeyAppHelpers
	{
		public static async Task<string> GetAPIKey()
		{
			try
			{
				var file = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFileAsync("HockeyAppAPIKey.txt");
				var key = await Windows.Storage.FileIO.ReadTextAsync(file);
				return key.Trim();
			}
			catch (System.IO.FileNotFoundException)
			{
				return string.Empty;
			}
		}
	}
}
