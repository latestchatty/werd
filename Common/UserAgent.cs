using System;

namespace Common
{
	public static class UserAgent
	{
		private static string agentString = String.Empty;

		/// <summary>
		/// Gets the user agent string for this application instance.
		/// </summary>
		public static string Agent
		{
			get
			{
				if (string.IsNullOrEmpty(agentString))
				{
					var package = Windows.ApplicationModel.Package.Current;
					agentString = $"{package.Id.Name}/{package.Id.Version.Major}.{package.Id.Version.Minor}.{package.Id.Version.Build}.{package.Id.Version.Revision}";
				}
				return agentString;
			}
		}
	}
}
