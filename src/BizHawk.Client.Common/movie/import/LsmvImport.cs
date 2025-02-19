using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

using BizHawk.Common.IOExtensions;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;
using BizHawk.Emulation.Cores.Nintendo.BSNES;

namespace BizHawk.Client.Common.movie.import
{
	/// <summary>For lsnes' <see href="https://tasvideos.org/Lsnes/Movieformat"><c>.lsmv</c> format</see></summary>
	[ImporterFor("LSNES", ".lsmv")]
	internal class LsmvImport : MovieImporter
	{
		private static readonly byte[] Zipheader = { 0x50, 0x4b, 0x03, 0x04 };
		private int _playerCount;
		private SimpleController _controller;
		private SimpleController _emptyController;

		private readonly (string, AxisSpec?)[][] _lsnesGamepadButtons = Enumerable.Range(1, 8)
		.Select(player => new[] { "B", "Y", "Select", "Start", "Up", "Down", "Left", "Right", "A", "X", "L", "R" }
			.Select(button => ($"P{player} {button}", (AxisSpec?)null)).ToArray())
		.ToArray();

		protected override void RunImport()
		{
			Result.Movie.HeaderEntries[HeaderKeys.Core] = CoreNames.SubBsnes115;

			// .LSMV movies are .zip files containing data files.
			using var fs = new FileStream(SourceFile.FullName, FileMode.Open, FileAccess.Read);
			{
				byte[] data = new byte[4];
				_ = fs.Read(data, offset: 0, count: data.Length); // if stream is too short, the next check will catch it
				if (!data.SequenceEqual(Zipheader))
				{
					Result.Errors.Add("This is not a zip file.");
					return;
				}
				fs.Position = 0;
			}

			using var zip = new ZipArchive(fs, ZipArchiveMode.Read, true);

			var ss = new BsnesCore.SnesSyncSettings();

			string platform = VSystemID.Raw.SNES;

			// need to handle ports first to ensure controller types are known
			ZipArchiveEntry portEntry;
			if ((portEntry = zip.GetEntry("port1")) != null)
			{
				using var stream = portEntry.Open();
				string port1 = Encoding.UTF8.GetString(stream.ReadAllBytes()).Trim();
				Result.Movie.HeaderEntries["port1"] = port1;
				ss.LeftPort = port1 switch
				{
					"none" => BsnesApi.BSNES_PORT1_INPUT_DEVICE.None,
					"gamepad16" => BsnesApi.BSNES_PORT1_INPUT_DEVICE.ExtendedGamepad,
					"multitap" => BsnesApi.BSNES_PORT1_INPUT_DEVICE.SuperMultitap,
					"multitap16" => BsnesApi.BSNES_PORT1_INPUT_DEVICE.Payload,
					_ => BsnesApi.BSNES_PORT1_INPUT_DEVICE.Gamepad
				};
			}
			if ((portEntry = zip.GetEntry("port2")) != null)
			{
				using var stream = portEntry.Open();
				string port2 = Encoding.UTF8.GetString(stream.ReadAllBytes()).Trim();
				Result.Movie.HeaderEntries["port2"] = port2;
				ss.RightPort = port2 switch
				{
					"none" => BsnesApi.BSNES_INPUT_DEVICE.None,
					"gamepad16" => BsnesApi.BSNES_INPUT_DEVICE.ExtendedGamepad,
					"multitap" => BsnesApi.BSNES_INPUT_DEVICE.SuperMultitap,
					"multitap16" => BsnesApi.BSNES_INPUT_DEVICE.Payload,
					// will these even work lol
					"superscope" => BsnesApi.BSNES_INPUT_DEVICE.SuperScope,
					"justifier" => BsnesApi.BSNES_INPUT_DEVICE.Justifier,
					"justifiers" => BsnesApi.BSNES_INPUT_DEVICE.Justifiers,
					_ => BsnesApi.BSNES_INPUT_DEVICE.Gamepad
				};
			}

			ControllerDefinition controllerDefinition = new BsnesControllers(ss, true).Definition;
			controllerDefinition.BuildMnemonicsCache(VSystemID.Raw.SNES);
			_emptyController = new SimpleController(controllerDefinition);
			_controller = new SimpleController(controllerDefinition);
			_playerCount = controllerDefinition.PlayerCount;

			Result.Movie.LogKey = Bk2LogEntryGenerator.GenerateLogKey(controllerDefinition);

			foreach (var item in zip.Entries)
			{
				if (item.FullName == "authors")
				{
					using var stream = item.Open();
					string authors = Encoding.UTF8.GetString(stream.ReadAllBytes());
					string authorList = "";
					string authorLast = "";
					using (var reader = new StringReader(authors))
					{
						// Each author is on a different line.
						while (reader.ReadLine() is string line)
						{
							string author = line.Trim();
							if (author != "")
							{
								if (authorLast != "")
								{
									authorList += $"{authorLast}, ";
								}

								authorLast = author;
							}
						}
					}

					if (authorList != "")
					{
						authorList += "and ";
					}

					if (authorLast != "")
					{
						authorList += authorLast;
					}

					Result.Movie.HeaderEntries[HeaderKeys.Author] = authorList;
				}
				else if (item.FullName == "coreversion")
				{
					using var stream = item.Open();
					string coreVersion = Encoding.UTF8.GetString(stream.ReadAllBytes()).Trim();
					Result.Movie.Comments.Add($"CoreOrigin {coreVersion}");
				}
				else if (item.FullName == "gamename")
				{
					using var stream = item.Open();
					string gameName = Encoding.UTF8.GetString(stream.ReadAllBytes()).Trim();
					Result.Movie.HeaderEntries[HeaderKeys.GameName] = gameName;
				}
				else if (item.FullName == "gametype")
				{
					using var stream = item.Open();
					string gametype = Encoding.UTF8.GetString(stream.ReadAllBytes()).Trim();

					// TODO: Handle the other types.
					switch (gametype)
					{
						case "gdmg":
							platform = VSystemID.Raw.GB;
							break;
						case "ggbc":
						case "ggbca":
							platform = VSystemID.Raw.GBC;
							break;
						case "sgb_ntsc":
						case "sgb_pal":
							platform = VSystemID.Raw.SGB;
							break;
					}

					bool pal = gametype == "snes_pal" || gametype == "sgb_pal";
					Result.Movie.HeaderEntries[HeaderKeys.Pal] = pal.ToString();
				}
				else if (item.FullName == "input")
				{
					using var stream = item.Open();
					string input = Encoding.UTF8.GetString(stream.ReadAllBytes());

					// Insert an empty frame in lsmv snes movies
					// see https://github.com/TASEmulators/BizHawk/issues/721
					// note: this is done inside ImportTextFrame already
					using (var reader = new StringReader(input))
					{
						while(reader.ReadLine() is string line)
						{
							if (line.Length is not 0) ImportTextFrame(line);
						}
					}
					Result.Movie.AppendFrame(_controller);
				}
				else if (item.FullName.StartsWithOrdinal("moviesram."))
				{
					using var stream = item.Open();
					byte[] movieSram = stream.ReadAllBytes();
					if (movieSram.Length != 0)
					{
						// TODO:  Why don't we support this?
						Result.Errors.Add("Movies that begin with SRAM are not supported.");
						return;
					}
				}
				else if (item.FullName == "projectid")
				{
					using var stream = item.Open();
					string projectId = Encoding.UTF8.GetString(stream.ReadAllBytes()).Trim();
					Result.Movie.HeaderEntries["ProjectID"] = projectId;
				}
				else if (item.FullName == "rerecords")
				{
					using var stream = item.Open();
					string rerecords = Encoding.UTF8.GetString(stream.ReadAllBytes());
					ulong rerecordCount;

					// Try to parse the re-record count as an integer, defaulting to 0 if it fails.
					try
					{
						rerecordCount = ulong.Parse(rerecords);
					}
					catch
					{
						rerecordCount = 0;
					}

					Result.Movie.Rerecords = rerecordCount;
				}
				else if (item.FullName.EndsWithOrdinal(".sha256"))
				{
					using var stream = item.Open();
					string sha256Hash = Encoding.UTF8.GetString(stream.ReadAllBytes()).Trim();
					string name = item.FullName.RemoveSuffix(".sha256");
					Result.Movie.HeaderEntries[name is "rom" ? HeaderKeys.Sha256 : $"SHA256_{name}"] = sha256Hash;
				}
				else if (item.FullName == "savestate")
				{
					Result.Errors.Add("Movies that begin with a savestate are not supported.");
					return;
				}
				else if (item.FullName == "subtitles")
				{
					using var stream = item.Open();
					string subtitles = Encoding.UTF8.GetString(stream.ReadAllBytes());
					using (var reader = new StringReader(subtitles))
					{
						while (reader.ReadLine() is string line)
						{
							var subtitle = ImportTextSubtitle(line);
							if (!string.IsNullOrEmpty(subtitle))
							{
								Result.Movie.Subtitles.AddFromString(subtitle);
							}
						}
					}
				}
				else if (item.FullName == "starttime.second")
				{
					using var stream = item.Open();
					string startSecond = Encoding.UTF8.GetString(stream.ReadAllBytes()).Trim();
					Result.Movie.HeaderEntries["StartSecond"] = startSecond;
				}
				else if (item.FullName == "starttime.subsecond")
				{
					using var stream = item.Open();
					string startSubSecond = Encoding.UTF8.GetString(stream.ReadAllBytes()).Trim();
					Result.Movie.HeaderEntries["StartSubSecond"] = startSubSecond;
				}
				else if (item.FullName == "systemid")
				{
					using var stream = item.Open();
					string systemId = Encoding.UTF8.GetString(stream.ReadAllBytes()).Trim();
					Result.Movie.Comments.Add($"{EmulationOrigin} {systemId}");
				}
			}

			Result.Movie.HeaderEntries[HeaderKeys.Platform] = platform;
			Result.Movie.SyncSettingsJson = ConfigService.SaveWithType(ss);
		}

		private void ImportTextFrame(string line)
		{
			// Split up the sections of the frame.
			string[] sections = line.Split('|');

			bool reset = false;
			if (sections.Length != 0)
			{
				string flags = sections[0];
				_controller["Subframe"] = flags[0] != 'F';
				Result.Movie.AppendFrame(_controller); // need to append the subframe input to the previous frame
				reset = flags[1] != '.';
				flags = SingleSpaces(flags.Substring(2));
				string[] splitFlags = flags.Split(' ');
				int delay;
				try
				{
					delay = int.Parse(splitFlags[1]) * 10000 + int.Parse(splitFlags[2]);
				}
				catch
				{
					delay = 0;
				}

				_controller.AcceptNewAxis("Reset Instruction", delay);
				if (delay != 0)
				{
					Result.Warnings.Add("Delayed reset may be mistimed."); // lsnes doesn't count some instructions that our bsnes version does
				}

				_controller["Reset"] = reset;
			}

			// LSNES frames don't start or end with a |.
			int end = sections.Length;

			for (int player = 1; player < end; player++)
			{
				if (player > _playerCount) break;

				var buttons = _controller.Definition.ControlsOrdered[player];
				if (buttons[0].Name.EndsWithOrdinal("Up")) // hack to identify gamepad / multitap which have a different button order in bizhawk compared to lsnes
				{
					buttons = _lsnesGamepadButtons[player - 1];
				}
				// Only consider lines that have the right number of buttons
				if (sections[player].Length == buttons.Count)
				{
					for (int button = 0; button < buttons.Count; button++)
					{
						// Consider the button pressed so long as its spot is not occupied by a ".".
						_controller[buttons[button].Name] = sections[player][button] != '.';
					}
				}
			}

			if (reset) Result.Movie.AppendFrame(_emptyController);
		}

		private static string ImportTextSubtitle(string line)
		{
			line = SingleSpaces(line);

			// The header name, frame, and message are separated by whitespace.
			int first = line.IndexOf(' ');
			int second = line.IndexOf(' ', first + 1);
			if (first != -1 && second != -1)
			{
				// Concatenate the frame and message with default values for the additional fields.
				string frame = line.Substring(0, first);
				string length = line.Substring(first + 1, second - first - 1);
				string message = line.Substring(second + 1).Trim();

				return $"subtitle {frame} 0 0 {length} FFFFFFFF {message}";
			}

			return null;
		}
	}
}
