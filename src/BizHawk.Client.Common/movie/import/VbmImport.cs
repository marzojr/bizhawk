using System.IO;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;
using BizHawk.Emulation.Cores.Nintendo.GBA;
using BizHawk.Emulation.Cores.Nintendo.GBHawk;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.Nintendo.Sameboy;

namespace BizHawk.Client.Common.movie.import
{
	// VBM file format: http://code.google.com/p/vba-rerecording/wiki/VBM
	// ReSharper disable once UnusedMember.Global
	[ImporterFor("Visual Boy Advance", ".vbm")]
	internal class VbmImport : MovieImporter
	{
		protected override void RunImport()
		{
			using var fs = SourceFile.Open(FileMode.Open, FileAccess.Read);
			using var r = new BinaryReader(fs);
			bool is_GBC = false;

			// 000 4-byte signature: 56 42 4D 1A "VBM\x1A"
			string signature = new string(r.ReadChars(4));
			if (signature != "VBM\x1A")
			{
				Result.Errors.Add("This is not a valid .VBM file.");
				return;
			}

			// 004 4-byte little-endian unsigned int: major version number, must be "1"
			uint majorVersion = r.ReadUInt32();
			if (majorVersion != 1)
			{
				Result.Errors.Add(".VBM major movie version must be 1.");
				return;
			}

			/*
			 008 4-byte little-endian integer: movie "uid" - identifies the movie-savestate relationship, also used as the
			 recording time in Unix epoch format
			*/
			uint uid = r.ReadUInt32();

			// 00C 4-byte little-endian unsigned int: number of frames
			uint frameCount = r.ReadUInt32();

			// 010 4-byte little-endian unsigned int: rerecord count
			uint rerecordCount = r.ReadUInt32();
			Result.Movie.Rerecords = rerecordCount;

			// 014 1-byte flags: (movie start flags)
			byte flags = r.ReadByte();

			// bit 0: if "1", movie starts from an embedded "quicksave" snapshot
			bool startFromSavestate = (flags & 0x1) != 0;

			// bit 1: if "1", movie starts from reset with an embedded SRAM
			bool startFromSram = ((flags >> 1) & 0x1) != 0;

			// other: reserved, set to 0
			// (If both bits 0 and 1 are "1", the movie file is invalid)
			if (startFromSavestate && startFromSram)
			{
				Result.Errors.Add("This is not a valid .VBM file.");
				return;
			}

			if (startFromSavestate)
			{
				Result.Errors.Add("Movies that begin with a savestate are not supported.");
				return;
			}

			if (startFromSram)
			{
				Result.Errors.Add("Movies that begin with SRAM are not supported.");
				return;
			}

			// 015 1-byte flags: controller flags
			byte controllerFlags = r.ReadByte();
			/*
			 * bit 0: controller 1 in use
			 * bit 1: controller 2 in use (SGB games can be 2-player multiplayer)
			 * bit 2: controller 3 in use (SGB games can be 3- or 4-player multiplayer with multitap)
			 * bit 3: controller 4 in use (SGB games can be 3- or 4-player multiplayer with multitap)
			*/
			bool[] controllersUsed = new bool[4];
			for (int controller = 1; controller <= controllersUsed.Length; controller++)
			{
				controllersUsed[controller - 1] = ((controllerFlags >> (controller - 1)) & 0x1) != 0;
			}

			if (!controllersUsed[0])
			{
				Result.Errors.Add("Controller 1 must be in use.");
				return;
			}

			// other: reserved
			// 016 1-byte flags: system flags (game always runs at 60 frames/sec)
			flags = r.ReadByte();

			// bit 0: if "1", movie is for the GBA system
			bool isGBA = (flags & 0x1) != 0;

			// bit 1: if "1", movie is for the GBC system
			bool isGBC = ((flags >> 1) & 0x1) != 0;

			// bit 2: if "1", movie is for the SGB system
			bool isSGB = ((flags >> 2) & 0x1) != 0;

			// (If all 3 of these bits are "0", it is for regular GB.)
			string platform = VSystemID.Raw.GB;

			if (isGBA) platform = VSystemID.Raw.GBA;

			if (isGBC)
			{
				is_GBC = true;
				platform = VSystemID.Raw.GB;
				Result.Movie.HeaderEntries.Add("IsCGBMode", "1");
			}

			var finalCoreName = Config.PreferredCores[VSystemID.Raw.GB];
			if (isSGB)
			{
				platform = VSystemID.Raw.SGB;
				if (finalCoreName is not CoreNames.Gambatte)
				{
					Result.Warnings.Add($"{finalCoreName} doesn't support SGB; will use {CoreNames.Gambatte}");
					finalCoreName = CoreNames.Gambatte;
				}
			}

			Result.Movie.HeaderEntries[HeaderKeys.Platform] = platform;

			// 017 1-byte flags: (values of some boolean emulator options)
			flags = r.ReadByte();
			/*
			 * bit 0: (useBiosFile) if "1" and the movie is of a GBA game, the movie was made using a GBA BIOS file.
			 * bit 1: (skipBiosFile) if "0" and the movie was made with a GBA BIOS file, the BIOS intro is included in the
			 * movie.
			 * bit 2: (rtcEnable) if "1", the emulator "real time clock" feature was enabled.
			 * bit 3: (unsupported) must be "0" or the movie file is considered invalid (legacy).
			*/
			if (((flags >> 3) & 0x1) != 0)
			{
				Result.Errors.Add("This is not a valid .VBM file.");
				return;
			}

			/*
			 018 4-byte little-endian unsigned int: theApp.winSaveType (value of that emulator option)
			 01C 4-byte little-endian unsigned int: theApp.winFlashSize (value of that emulator option)
			 020 4-byte little-endian unsigned int: gbEmulatorType (value of that emulator option)
			*/
			r.ReadBytes(12);
			/*
			 024 12-byte character array: the internal game title of the ROM used while recording, not necessarily
			 null-terminated (ASCII?)
			*/
			string gameName = NullTerminated(new string(r.ReadChars(12)));
			Result.Movie.HeaderEntries[HeaderKeys.GameName] = gameName;

			// 030 1-byte unsigned char: minor version/revision number of current VBM version, the latest is "1"
			byte minorVersion = r.ReadByte();
			Result.Movie.Comments.Add($"{MovieOrigin} .VBM version {majorVersion}.{minorVersion}");
			Result.Movie.Comments.Add($"{EmulationOrigin} Visual Boy Advance");

			// 031 1-byte unsigned char: the internal CRC of the ROM used while recording
			r.ReadByte();
			/*
			 032 2-byte little-endian unsigned short: the internal Checksum of the ROM used while recording, or a
			 calculated CRC16 of the BIOS if GBA
			*/
			ushort checksumCRC16 = r.ReadUInt16();
			/*
			 034 4-byte little-endian unsigned int: the Game Code of the ROM used while recording, or the Unit Code if not
			 GBA
			*/
			uint gameCodeUnitCode = r.ReadUInt32();
			if (platform == VSystemID.Raw.GBA)
			{
				Result.Movie.HeaderEntries["CRC16"] = checksumCRC16.ToString();
				Result.Movie.HeaderEntries["GameCode"] = gameCodeUnitCode.ToString();
			}
			else
			{
				Result.Movie.HeaderEntries["InternalChecksum"] = checksumCRC16.ToString();
				Result.Movie.HeaderEntries["UnitCode"] = gameCodeUnitCode.ToString();
			}

			// 038 4-byte little-endian unsigned int: offset to the savestate or SRAM inside file, set to 0 if unused
			r.ReadBytes(4);

			// 03C 4-byte little-endian unsigned int: offset to the controller data inside file
			uint firstFrameOffset = r.ReadUInt32();

			// After the header is 192 bytes of text. The first 64 of these 192 bytes are for the author's name (or names).
			string author = NullTerminated(new string(r.ReadChars(64)));
			Result.Movie.HeaderEntries[HeaderKeys.Author] = author;

			// The following 128 bytes are for a description of the movie. Both parts must be null-terminated.
			string movieDescription = NullTerminated(new string(r.ReadChars(128)));
			Result.Movie.Comments.Add(movieDescription);
			r.BaseStream.Position = firstFrameOffset;

			SimpleController controllers = isGBA
				? GbaController()
				: GbController();
			controllers.Definition.BuildMnemonicsCache(isGBA ? VSystemID.Raw.GBA : VSystemID.Raw.GB);

			/*
			 * 01 00 A
			 * 02 00 B
			 * 04 00 Select
			 * 08 00 Start
			 * 10 00 Right
			 * 20 00 Left
			 * 40 00 Up
			 * 80 00 Down
			 * 00 01 R
			 * 00 02 L
			*/
			string[] buttons = { "A", "B", "Select", "Start", "Right", "Left", "Up", "Down", "R", "L" };
			/*
			 * 00 04 Reset (old timing)
			 * 00 08 Reset (new timing since version 1.1)
			 * 00 10 Left motion sensor
			 * 00 20 Right motion sensor
			 * 00 40 Down motion sensor
			 * 00 80 Up motion sensor
			*/
			string[] other =
			{
				"Reset (old timing)", "Reset (new timing since version 1.1)", "Left motion sensor",
				"Right motion sensor", "Down motion sensor", "Up motion sensor"
			};

			for (int frame = 1; frame <= frameCount; frame++)
			{
				/*
				 A stream of 2-byte bitvectors which indicate which buttons are pressed at each point in time. They will
				 come in groups of however many controllers are active, in increasing order.
				*/
				ushort controllerState = r.ReadUInt16();
				for (int button = 0; button < buttons.Length; button++)
				{
					controllers[buttons[button]] = ((controllerState >> button) & 0x1) != 0;
					if (((controllerState >> button) & 0x1) != 0 && button > 7)
					{
						continue;
					}
				}

				// TODO: Handle the other buttons.
				for (int button = 0; button < other.Length; button++)
				{
					if (((controllerState >> (button + 10)) & 0x1) != 0)
					{
						Result.Warnings.Add($"Unable to import {other[button]} at frame {frame}.");
						break;
					}
				}

				// TODO: Handle the additional controllers.
				for (int player = 2; player <= controllersUsed.Length; player++)
				{
					if (controllersUsed[player - 1])
					{
						r.ReadBytes(2);
					}
				}

				Result.Movie.AppendFrame(controllers);
			}

			if (isGBA)
			{
				Result.Movie.HeaderEntries[HeaderKeys.Core] = CoreNames.Mgba;
				Result.Movie.SyncSettingsJson = ConfigService.SaveWithType(new MGBAHawk.SyncSettings { SkipBios = true });
			}
			else
			{
				Result.Movie.HeaderEntries[HeaderKeys.Core] = finalCoreName;
				switch (finalCoreName)
				{
					case CoreNames.Gambatte:
						Result.Movie.SyncSettingsJson = ConfigService.SaveWithType(new Gameboy.GambatteSyncSettings
						{
							ConsoleMode = isSGB
								? Gameboy.GambatteSyncSettings.ConsoleModeType.SGB2
								: is_GBC ? Gameboy.GambatteSyncSettings.ConsoleModeType.GBC : Gameboy.GambatteSyncSettings.ConsoleModeType.GB,
						});
						break;
					case CoreNames.GbHawk:
					case CoreNames.SubGbHawk:
						Result.Movie.SyncSettingsJson = ConfigService.SaveWithType(new GBHawk.GBSyncSettings
						{
							ConsoleMode = is_GBC ? GBHawk.GBSyncSettings.ConsoleModeType.GBC : GBHawk.GBSyncSettings.ConsoleModeType.GB,
						});
						break;
					case CoreNames.Sameboy:
						Result.Movie.SyncSettingsJson = ConfigService.SaveWithType(new Sameboy.SameboySyncSettings
						{
							ConsoleMode = is_GBC ? Sameboy.SameboySyncSettings.GBModel.GB_MODEL_CGB_E : Sameboy.SameboySyncSettings.GBModel.GB_MODEL_DMG_B,
						});
						break;
				}
			}
		}

		private static SimpleController GbController()
			=> new(new ControllerDefinition("Gameboy Controller")
			{
				BoolButtons = { "Up", "Down", "Left", "Right", "Start", "Select", "B", "A", "Power" }
			}.MakeImmutable());

		private static SimpleController GbaController()
			=> new(MGBAHawk.GBAController);
	}
}
