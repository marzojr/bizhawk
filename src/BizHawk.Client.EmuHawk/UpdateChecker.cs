using System.IO;
using System.Net;
using System.Text.Json.Nodes;
using System.Threading;

using BizHawk.Client.Common;
using BizHawk.Common;

namespace BizHawk.Client.EmuHawk
{
	public static class UpdateChecker
	{
		private static readonly string _latestVersionInfoURL = "https://api.github.com/repos/TASVideos/BizHawk/releases/latest";
		private static readonly TimeSpan _minimumCheckDuration = TimeSpan.FromHours(8);

		public static Config GlobalConfig { get; set; }

		private static bool AutoCheckEnabled
		{
			get => GlobalConfig.UpdateAutoCheckEnabled;
			set => GlobalConfig.UpdateAutoCheckEnabled = value;
		}

		private static DateTime? LastCheckTimeUTC
		{
			get => GlobalConfig.UpdateLastCheckTimeUtc;
			set => GlobalConfig.UpdateLastCheckTimeUtc = value;
		}

		private static string LatestVersion
		{
			get => GlobalConfig.UpdateLatestVersion;
			set => GlobalConfig.UpdateLatestVersion = value;
		}

		private static string IgnoreVersion
		{
			get => GlobalConfig.UpdateIgnoreVersion;
			set => GlobalConfig.UpdateIgnoreVersion = value;
		}

		public static void BeginCheck(bool skipCheck = false)
		{
			if (skipCheck || string.IsNullOrEmpty(_latestVersionInfoURL) || !AutoCheckEnabled || LastCheckTimeUTC > DateTime.UtcNow - _minimumCheckDuration)
			{
				OnCheckComplete();
				return;
			}

			ThreadPool.QueueUserWorkItem(s => CheckInternal());
		}

		public static bool IsNewVersionAvailable =>
			AutoCheckEnabled
			&& LatestVersion != IgnoreVersion
			&& VersionInfo.VersionStrToInt(VersionInfo.MainVersion) != 0U // Avoid notifying if current version string is invalid
			&& VersionInfo.VersionStrToInt(LatestVersion) > VersionInfo.VersionStrToInt(VersionInfo.MainVersion);

		public static void IgnoreNewVersion()
		{
			IgnoreVersion = LatestVersion;
		}

		public static void ResetHistory()
		{
			LastCheckTimeUTC = null;
			LatestVersion = "";
			IgnoreVersion = "";
		}

		private static void CheckInternal()
		{
			try
			{
				var response = JsonNode.Parse(DownloadURLAsString(_latestVersionInfoURL));

				LatestVersion = ValidateVersionNumberString(response["name"].GetValue<string>());
			}
			catch
			{
				// Ignore errors, and fall through to set the last check time to avoid requesting too frequently from the web server
			}

			LastCheckTimeUTC = DateTime.UtcNow;

			OnCheckComplete();
		}

		private static string DownloadURLAsString(string url)
		{
			var request = (HttpWebRequest)WebRequest.Create(url);
			request.UserAgent = "BizHawk";
			request.KeepAlive = false;
			using var response = (HttpWebResponse)request.GetResponse();
			using var responseStream = new StreamReader(response.GetResponseStream());
			return responseStream.ReadToEnd();
		}

		private static string ValidateVersionNumberString(string versionNumber)
		{
			return versionNumber != null && VersionInfo.VersionStrToInt(versionNumber) != 0U ? versionNumber : "";
		}

		private static void OnCheckComplete()
		{
			CheckComplete?.Invoke(null, EventArgs.Empty);
		}

		public static event EventHandler CheckComplete;
	}
}
