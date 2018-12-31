using System;
using Windows.ApplicationModel;

namespace Common
{
	public static class UserAgent
	{
		private static string _agentString = String.Empty;

		/// <summary>
		/// Gets the user agent string for this application instance.
		/// </summary>
		public static string Agent
		{
			get
			{
				if (string.IsNullOrEmpty(_agentString))
				{
					var package = Package.Current;
					_agentString = $"{package.Id.Name}/{package.Id.Version.Major}.{package.Id.Version.Minor}.{package.Id.Version.Build}.{package.Id.Version.Revision}";
				}
				return _agentString;
			}
		}
	}
}
