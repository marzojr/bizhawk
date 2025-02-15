using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using BizHawk.Common;
using BizHawk.Common.PathExtensions;

namespace BizHawk.Client.EmuHawk
{
	public partial class CoreDownloaderForm : Form
	{
		// TODO edit
		private static readonly string BaseUrl = $"https://github.com/Morilli/BizHawk-extra/raw/{VersionInfo.MainVersion}/";
		private static readonly string OutputPath = PathUtils.DllDirectoryPath;

		private static readonly string Encore = OSTailoredCode.IsUnixHost ? "libencore.so" : "encore.dll";

		private static readonly List<string> AvailableDownloads = [ Encore, "libmamearcade.wbx.zst" ];

		private readonly CancellationTokenSource _cancellationTokenSource = new();

		public CoreDownloaderForm()
		{
			InitializeComponent();
		}

		private void OnLoad(object sender, EventArgs e)
		{
			for (int i = 0; i < AvailableDownloads.Count; i++)
			{
				string availableDownload = AvailableDownloads[i];
				bool alreadyDownloaded = File.Exists(Path.Combine(OutputPath, availableDownload));
				groupBox1.Controls.Add(new CheckBox
				{
					Text = availableDownload,
					Location = new Point(10, 20 + 30 * i),
					Checked = alreadyDownloaded,
					AutoCheck = false,
					AutoSize = true,
				});
				var downloadButton = new Button
				{
					Text = "Download",
					Location = new Point(150, 19 + 30 * i),
					Size = new Size(160, 22),
					Enabled = !alreadyDownloaded,
				};
				downloadButton.Click += DownloadClick;
				groupBox1.Controls.Add(downloadButton);
			}
		}

		private async void DownloadClick(object sender, EventArgs e)
		{
			var sourceButton = (Button)sender;
			sourceButton.Enabled = false;
			sourceButton.Text = "Downloading...";

			// fixme what is this
			int downloadIndex = (sourceButton.Location.Y - 19) / 30;
			string toDownload = AvailableDownloads[downloadIndex];

			try
			{
				await Download(toDownload);
			}
			catch (TaskCanceledException)
			{
				// this is fine; the download was canceled because the form was closed
			}
			catch (Exception ex)
			{
				sourceButton.Enabled = true;
				sourceButton.Text = "Download";
				MessageBox.Show(this, $"Failed to download {toDownload}:\n{ex}", "Download failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}

			if (File.Exists(Path.Combine(OutputPath, toDownload)))
			{
				sourceButton.Text = "Download complete!";
				((CheckBox)groupBox1.Controls[downloadIndex * 2]).Checked = true;
			}
		}

		private async Task Download(string name)
		{
			string tempFilename = TempFileManager.GetTempFilename(name, delete: false);

			using var httpClient = new HttpClient();
			using var stream = await httpClient.GetStreamAsync(BaseUrl + name);
			using (var tempFileStream = File.Create(tempFilename))
			{
				await stream.CopyToAsync(tempFileStream, 256 * 1024, _cancellationTokenSource.Token);
			}
			File.Move(tempFilename, Path.Combine(OutputPath, name));
		}

		private void OnClosing(object sender, FormClosingEventArgs e)
		{
			_cancellationTokenSource.Cancel();
		}
	}
}
