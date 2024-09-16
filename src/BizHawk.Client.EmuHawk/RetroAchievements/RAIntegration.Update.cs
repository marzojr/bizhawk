using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.Json;

using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Client.Common;
using BizHawk.Common.StringExtensions;

namespace BizHawk.Client.EmuHawk
{
	public partial class RAIntegration
	{
		private static RAInterface _RA;
		private static DynamicLibraryImportResolver _resolver;
		private static Version _version;
		
		public static bool IsAvailable => _RA != null;

		// can't have both a proxy with a monitor and without one, so...
		private class DummyMonitor : IMonitor
		{
			public void Enter() {}
			public void Exit() {}

			public static readonly DummyMonitor Singleton = new();
		}

		private static void AttachDll()
		{
			_resolver = new("RA_Integration-x64.dll", hasLimitedLifetime: true);
			_RA = BizInvoker.GetInvoker<RAInterface>(_resolver, DummyMonitor.Singleton, CallingConventionAdapters.Native);
			_version = new(Marshal.PtrToStringAnsi(_RA.IntegrationVersion())!);
			Console.WriteLine($"Loaded RAIntegration v{_version}");
		}

		private static void DetachDll()
		{
			_RA?.Shutdown();
			_resolver?.Dispose();
			_resolver = null;
			_RA = null;
			_version = new(0, 0);
		}

		private static bool DownloadDll(string url)
		{
			if (url.StartsWithOrdinal("http:"))
			{
				// force https
				url = url.Replace("http:", "https:");
			}

			using var downloadForm = new RAIntegrationDownloaderForm(url);
			downloadForm.ShowDialog();
			return downloadForm.DownloadSucceeded();
		}

		public static bool CheckUpdateRA(IDialogParent dialogParent)
		{
			try
			{
				var http = new HttpCommunication(null, "https://retroachievements.org/dorequest.php?r=latestintegration", null);
				var info = JsonSerializer.Deserialize<Dictionary<string, object>>(http.ExecGet());
				if (info.TryGetValue("Success", out var success) && (bool)success)
				{
					var lastestVer = new Version((string)info["LatestVersion"]);
					var minVer = new Version((string)info["MinimumVersion"]);

					if (_version < minVer)
					{
						if (!dialogParent.ModalMessageBox2(
								text:
								"An update is required to use RetroAchievements. Do you want to download the update now?",
								caption: "Update",
								icon: EMsgBoxIcon.Question,
								useOKCancel: false)) return false;
						DetachDll();
						var ret = DownloadDll((string)info["LatestVersionUrlX64"]);
						AttachDll();
						return ret;
					}

					if (_version >= lastestVer) return true;

					if (!dialogParent.ModalMessageBox2(
							text:
							"An optional update is available for RetroAchievements. Do you want to download the update now?",
							caption: "Update",
							icon: EMsgBoxIcon.Question,
							useOKCancel: false)) return true;

					DetachDll();
					DownloadDll((string)info["LatestVersionUrlX64"]);
					AttachDll();
					return true; // even if this fails, should be OK to use the old dll
				}

				dialogParent.ModalMessageBox(
					text: "Failed to fetch update information, cannot start RetroAchievements.",
					caption: "Error",
					icon: EMsgBoxIcon.Error);

				return false;
			}
			catch (Exception ex)
			{
				// is this needed?
				dialogParent.ModalMessageBox(
					text: $"Exception {ex.Message} occurred when fetching update information, cannot start RetroAchievements.",
					caption: "Error",
					icon: EMsgBoxIcon.Error);

				DetachDll();
				AttachDll();
				return false;
			}
		}
	}
}
