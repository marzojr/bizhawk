using System.Linq;
using System.Windows.Forms;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// configures the FFmpegWriter
	/// </summary>
	public partial class FFmpegWriterForm : Form
	{
		/// <summary>
		/// stores a single format preset
		/// </summary>
		public class FormatPreset : IDisposable
		{
			/// <summary>
			/// Gets the name for the listbox
			/// </summary>
			public string Name { get; }

			/// <summary>
			/// Gets the long human readable description
			/// </summary>
			public string Desc { get; }

			/// <summary>
			/// Gets the actual portion of the ffmpeg commandline
			/// </summary>
			public string Commandline { get; set; }

			/// <summary>
			/// Gets a value indicating whether or not it can be edited
			/// </summary>
			public bool Custom { get; }

			/// <summary>
			/// Gets the default file extension
			/// </summary>
			public string Extension { get; set; }

			/// <summary>
			/// get a list of canned presets
			/// </summary>
			public static FormatPreset[] GetPresets(string customCommand)
			{
				return new[]
				{
					new FormatPreset("Social Media — MP4", "Optimized for quick and easy sharing. Produces small, highly compatible and streamable files. AVC video and Opus audio in a MP4 container.",
						"-c:a libopus -c:v libx264 -pix_fmt yuv420p -movflags +faststart -f mp4", false, "mp4"),
					new FormatPreset("[ Custom ]", "Write your own FFmpeg command. For advanced users only.",
						customCommand, true, "foobar"),
					new FormatPreset("Lossless AVC — MKV", "Lossless AVC video and lossless FLAC audio in a Matroska container. High speed and compression. Compatible with AVISource() if x264vfw or FFmpeg based decoder is installed. Seeking may be unstable.",
						"-c:a flac -c:v libx264rgb -qp 0 -pix_fmt rgb24 -f matroska", false, "mkv"),
					new FormatPreset("Lossless Ut Video — AVI", "Lossless Ut Video and uncompressed audio in an AVI container. Fast, but low compression. Compatible with AVISource(), if Ut Video decoder is installed.",
						"-c:a pcm_s16le -c:v utvideo -pred median -pix_fmt gbrp -f avi", false, "avi"),
					new FormatPreset("Lossless FFV1 — AVI", "Lossless FFV1 video and uncompressed audio in an AVI container. Slow, but high compression. Compatible with AVISource(), if FFmpeg based decoder is installed.",
						"-c:a pcm_s16le -c:v ffv1 -pix_fmt bgr0 -level 1 -g 1 -coder 1 -context 1 -f avi", false, "avi"),
					new FormatPreset("Uncompressed — AVI", "Uncompressed video and audio in an AVI container. Extremely large files, don't use!",
						"-c:a pcm_s16le -c:v rawvideo -f avi", false, "avi")
				};
			}

			/// <summary>
			/// get the default format preset (from config files)
			/// </summary>
			public static FormatPreset GetDefaultPreset(Config config)
			{
				FormatPreset[] fps = GetPresets(config.FFmpegCustomCommand);

				foreach (var fp in fps)
				{
					if (fp.ToString() == config.FFmpegFormat)
					{
						if (fp.Custom)
						{
							return fp;
						}
					}
				}

				// default to xvid?
				return fps[1];
			}

			public override string ToString()
			{
				return Name;
			}

			public void Dispose()
			{
			}

			public void DeduceFormat(string commandline)
			{
				var splitCommandLine = commandline.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				for (int index = 0; index < splitCommandLine.Length - 1; index++)
				{
					if (splitCommandLine[index] == "-f")
					{
						Extension = splitCommandLine[index + 1];
						break;
					}
				}

				// are there other formats that don't match their file extensions?
				if (Extension == "matroska")
				{
					Extension = "mkv";
				}
			}

			private FormatPreset(string name, string desc, string commandline, bool custom, string ext)
			{
				Name = name;
				Desc = desc;
				Commandline = commandline;
				Custom = custom;

				DeduceFormat(Commandline);
			}
		}

		private FFmpegWriterForm()
		{
			InitializeComponent();
		}

		private void ListBox1_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (listBox1.SelectedIndex != -1)
			{
				var f = (FormatPreset)listBox1.SelectedItem;
				label5.Text = $"Extension: {f.Extension}";
				label3.Text = f.Desc;
				textBox1.Text = f.Commandline;
			}
		}

		/// <summary>
		/// return a FormatPreset corresponding to the user's choice
		/// </summary>
		public static FormatPreset DoFFmpegWriterDlg(IWin32Window owner, Config config)
		{
			FFmpegWriterForm dlg = new FFmpegWriterForm();
			dlg.listBox1.Items.AddRange(FormatPreset.GetPresets(config.FFmpegCustomCommand).Cast<object>().ToArray());

			int i = dlg.listBox1.FindStringExact(config.FFmpegFormat);
			if (i != ListBox.NoMatches)
			{
				dlg.listBox1.SelectedIndex = i;
			}

			DialogResult result = dlg.ShowDialog(owner);

			FormatPreset ret;
			if (result != DialogResult.OK || dlg.listBox1.SelectedIndex == -1)
			{
				ret = null;
			}
			else
			{
				ret = (FormatPreset)dlg.listBox1.SelectedItem;
				config.FFmpegFormat = ret.ToString();
				if (ret.Custom)
				{
					ret.Commandline =
						config.FFmpegCustomCommand =
						dlg.textBox1.Text;

					ret.DeduceFormat(ret.Commandline);
				}
			}

			dlg.Dispose();
			return ret;
		}
	}
}
