using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Nintendo.Gameboy;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	/// <summary>
	/// a gameboy/gameboy color emulator wrapped around native C++ libgambatte
	/// </summary>
	[PortedCore(CoreNames.Gambatte, "sinamas/PSR org", "r830", "https://github.com/pokemon-speedrunning/gambatte-core")]
	public partial class Gameboy : IInputPollable, IRomInfo, IGameboyCommon, ICycleTiming, ILinkable
	{
		/// <remarks>HACK disables BIOS requirement if the environment looks like a test runner...</remarks>
		private static readonly bool TestromsBIOSDisableHack = Type.GetType("Microsoft.VisualStudio.TestTools.UnitTesting.Assert, Microsoft.VisualStudio.TestPlatform.TestFramework") is not null;

		[CoreConstructor(VSystemID.Raw.GB)]
		[CoreConstructor(VSystemID.Raw.GBC)]
		[CoreConstructor(VSystemID.Raw.SGB)]
		public Gameboy(CoreComm comm, IGameInfo game, byte[] file, GambatteSettings settings, GambatteSyncSettings syncSettings, bool deterministic)
		{
			_serviceProvider = new(this);
			_serviceProvider.Register<IDisassemblable>(_disassembler);
			const string TRACE_HEADER = "LR35902: PC, opcode, registers (A, F, B, C, D, E, H, L, LY, SP, CY)";
			Tracer = new TraceBuffer(TRACE_HEADER);
			_serviceProvider.Register(Tracer);
			InitMemoryCallbacks();

			DeterministicEmulation = deterministic;

			GambatteState = LibGambatte.gambatte_create();

			if (GambatteState == IntPtr.Zero)
			{
				throw new InvalidOperationException($"{nameof(LibGambatte.gambatte_create)}() returned null???");
			}

			Console.WriteLine(game.System);

			try
			{
				_syncSettings = syncSettings ?? new GambatteSyncSettings();

				var flags = LibGambatte.LoadFlags.READONLY_SAV;

				if (game.System == VSystemID.Raw.SGB)
				{
					_syncSettings.ConsoleMode = GambatteSyncSettings.ConsoleModeType.SGB2;
				}

				switch (_syncSettings.ConsoleMode)
				{
					case GambatteSyncSettings.ConsoleModeType.GB:
						break;
					case GambatteSyncSettings.ConsoleModeType.GBC:
						flags |= LibGambatte.LoadFlags.CGB_MODE;
						break;
					case GambatteSyncSettings.ConsoleModeType.GBA:
						flags |= LibGambatte.LoadFlags.CGB_MODE | LibGambatte.LoadFlags.GBA_FLAG;
						break;
					case GambatteSyncSettings.ConsoleModeType.SGB2:
						flags |= LibGambatte.LoadFlags.SGB_MODE;
						IsSgb = true;
						break;
					case GambatteSyncSettings.ConsoleModeType.Auto:
						if (game.System == VSystemID.Raw.GBC)
							flags |= LibGambatte.LoadFlags.CGB_MODE;
						break;
					default:
						throw new InvalidOperationException();
				}

				IsCgb = (flags & LibGambatte.LoadFlags.CGB_MODE) == LibGambatte.LoadFlags.CGB_MODE;

				bool ForceBios()
				{
					// if we're not recording a movie, we don't need to force a bios
					if (!DeterministicEmulation)
					{
						return false;
					}

					// if this rom can't be booted with a bios, don't force one

					// SGB bios on the GB side technically doesn't care at all here and will always boot the rom
					// (there are checks on the SNES side but Gambatte's HLE doesn't bother with that)
					if (IsSgb)
					{
						return true;
					}

					// we need the rom loaded in for this; we'll reload it for real later on with proper flags
					if (LibGambatte.gambatte_loadbuf(GambatteState, file, (uint)file.Length, flags) != 0)
					{
						throw new InvalidOperationException($"{nameof(LibGambatte.gambatte_loadbuf)}() returned non-zero (is this not a gb or gbc rom?)");
					}

					// header checksum must pass for bios to boot the ROM
					var unused = new byte[32];
					LibGambatte.gambatte_pakinfo(GambatteState, unused, out _, out _, out _, out var hcok);
					if (hcok == 0)
					{
						return false;
					}

					// CGB bios checks top half of nintendo logo, DMG bios checks entire logo
					// TODO: this check should probably be moved into the C++ side (in pakinfo)
					const int logoStart = 0x104;
					var logoEnd = 0x134;
					if (IsCgb)
					{
						logoEnd -= (logoEnd - logoStart) / 2;
					}

					var nintendoLogo = new byte[]
					{
						0xCE, 0xED, 0x66, 0x66, 0xCC, 0x0D, 0x00, 0x0B, 0x03, 0x73, 0x00, 0x83, 0x00, 0x0C, 0x00, 0x0D,
						0x00, 0x08, 0x11, 0x1F, 0x88, 0x89, 0x00, 0x0E, 0xDC, 0xCC, 0x6E, 0xE6, 0xDD, 0xDD, 0xD9, 0x99,
						0xBB, 0xBB, 0x67, 0x63, 0x6E, 0x0E, 0xEC, 0xCC, 0xDD, 0xDC, 0x99, 0x9F, 0xBB, 0xB9, 0x33, 0x3E
					};

					for (var i = logoStart; i < logoEnd; i++)
					{
						if (nintendoLogo[i - logoStart] != LibGambatte.gambatte_cpuread(GambatteState, (ushort)i))
						{
							return false;
						}
					}

					// if we get here, the rom can boot with a bios
					return true;
				}

				var useBios = _syncSettings.EnableBIOS || (!TestromsBIOSDisableHack && ForceBios());
				if (useBios)
				{
					FirmwareID fwid = new(
						IsCgb ? "GBC" : "GB",
						IsSgb
							? "SGB2"
							: _syncSettings.ConsoleMode is GambatteSyncSettings.ConsoleModeType.GBA
								? "AGB"
								: "World");
					var bios = comm.CoreFileProvider.GetFirmwareOrThrow(fwid, "BIOS Not Found, cannot Load.  Change GB Settings to run without BIOS.");
					if (LibGambatte.gambatte_loadbiosbuf(GambatteState, bios, (uint)bios.Length) != 0)
					{
						throw new InvalidOperationException($"{nameof(LibGambatte.gambatte_loadbiosbuf)}() returned non-zero (bios error)");
					}
				}
				else
				{
					flags |= LibGambatte.LoadFlags.NO_BIOS;
				}

				if (LibGambatte.gambatte_loadbuf(GambatteState, file, (uint)file.Length, flags) != 0)
				{
					throw new InvalidOperationException($"{nameof(LibGambatte.gambatte_loadbuf)}() returned non-zero (is this not a gb or gbc rom?)");
				}

				if (IsSgb)
				{
					ResetStallTicks = 128 * (2 << 14);
				}
				else if (_syncSettings.EnableBIOS && (_syncSettings.ConsoleMode is GambatteSyncSettings.ConsoleModeType.GBA))
				{
					ResetStallTicks = 485808; // GBA takes 971616 cycles to switch to CGB mode; CGB CPU is inactive during this time.
				}
				else
				{
					ResetStallTicks = 0;
				}

				// set real default colors (before anyone mucks with them at all)
				PutSettings(settings ?? new GambatteSettings());

				InitSound();

				Frame = 0;
				LagCount = 0;
				IsLagFrame = false;

				InputCallback = ControllerCallback;

				LibGambatte.gambatte_setinputgetter(GambatteState, InputCallback, IntPtr.Zero);

				if (_syncSettings.EnableRemote)
				{
					RemoteCallback = RemoteInputCallback;
					LibGambatte.gambatte_setremotecallback(GambatteState, RemoteCallback);
				}

				InitMemoryDomains();

				var mbcBuf = new byte[32 + 1];
				LibGambatte.gambatte_pakinfo(GambatteState, mbcBuf, out var rambanks, out var rombanks, out var crc, out var headerchecksumok);

				var romNameBuf = new byte[16 + 1];
				LibGambatte.gambatte_romtitle(GambatteState, romNameBuf);
				var romname = Encoding.ASCII.GetString(romNameBuf).TrimEnd('\0');

				RomDetails = $"{game.Name}\r\n{SHA1Checksum.ComputePrefixedHex(file)}\r\n{MD5Checksum.ComputePrefixedHex(file)}\r\n\r\n";
				BoardName = Encoding.ASCII.GetString(mbcBuf).TrimEnd('\0');

				RomDetails += $"Core reported Header Name: {romname}\r\n";
				RomDetails += $"Core reported RAM Banks: {rambanks}\r\n";
				RomDetails += $"Core reported ROM Banks: {rombanks}\r\n";
				RomDetails += $"Core reported CRC32: {crc:X8}\r\n";
				RomDetails += $"Core reported Header Checksum Status: {(headerchecksumok != 0 ? "OK" : "BAD")}\r\n";

				switch (useBios)
				{
					case true when headerchecksumok == 0:
						comm.ShowMessage("Core reports the header checksum is bad. This ROM will not boot with the official BIOS.\n" +
							"Disable BIOS in GB settings to boot this game");
						break;
					//TODO doesn't IsCGBDMGMode imply IsCGBMode?
					case false when IsCGBMode && IsCGBDMGMode:
					{
						// without a bios, we need to set the palette for cgbdmg ourselves
						var cgbDmgColors = new[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 };
						if (file[0x14B] == 0x01 || (file[0x14B] == 0x33 && file[0x144] == '0' && file[0x145] == '1')) // Nintendo licencees get special palettes
						{
							cgbDmgColors = ColorsFromTitleHash(file);
						}
						ChangeDMGColors(cgbDmgColors);
						break;
					}
				}

				LibGambatte.gambatte_settimemode(GambatteState, DeterministicEmulation || !_syncSettings.RealTimeRTC);

				if (DeterministicEmulation)
				{
					var dividers = _syncSettings.InitialTime * (0x400000UL + (ulong)_syncSettings.RTCDivisorOffset) / 2UL;
					LibGambatte.gambatte_settime(GambatteState, dividers);
				}

				LibGambatte.gambatte_setrtcdivisoroffset(GambatteState, _syncSettings.RTCDivisorOffset);

				LibGambatte.gambatte_setcartbuspulluptime(GambatteState, (uint)_syncSettings.CartBusPullUpTime);

				_cdCallback = CDCallbackProc;

				ControllerDefinition = CreateControllerDefinition(
					sgb: IsSgb,
					sub: _syncSettings.FrameLength is GambatteSyncSettings.FrameLengthType.UserDefinedFrames,
					tilt: false,
					rumble: false,
					remote: _syncSettings.EnableRemote);

				NewSaveCoreSetBuff();
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		private readonly GBDisassembler _disassembler = new();

		public string RomDetails { get; }

		/// <summary>
		/// the nominal length of one frame
		/// </summary>
		private const uint TICKSINFRAME = 35112;

		/// <summary>
		/// number of ticks per second
		/// </summary>
		private const uint TICKSPERSECOND = 2097152;

		/// <summary>
		/// number of reset stall ticks
		/// </summary>
		private uint ResetStallTicks { get; }

		/// <summary>
		/// keep a copy of the input callback delegate so it doesn't get GCed
		/// </summary>
		private readonly LibGambatte.InputGetter InputCallback;

		/// <summary>
		/// whatever keys are currently depressed
		/// </summary>
		private LibGambatte.Buttons CurrentButtons = 0;

		/// <summary>
		/// remote callback delegate
		/// </summary>
		private readonly LibGambatte.RemoteCallback RemoteCallback;

		/// <summary>
		/// remote command to send
		/// </summary>
		private byte RemoteCommand;

		/// <summary>
		/// internal gambatte state
		/// </summary>
		internal IntPtr GambatteState { get; private set; }

		public int LagCount { get; set; }
		public bool IsLagFrame { get; set; }
		public bool IsCgb { get; set; }
		public bool IsSgb { get; set; }

		// all cycle counts are relative to a 2*1024*1024 hz refclock

		/// <summary>
		/// total cycles actually executed
		/// </summary>
		private ulong _cycleCount;
		private ulong callbackCycleCount;

		/// <summary>
		/// number of extra cycles we overran in the last frame
		/// </summary>
		private uint frameOverflow;
		public long CycleCount => (long)_cycleCount;
		public double ClockRate => TICKSPERSECOND;

		public static ControllerDefinition CreateControllerDefinition(bool sgb, bool sub, bool tilt, bool rumble, bool remote)
		{
			var ret = new ControllerDefinition((sub ? "Subframe " : "") + "Gameboy Controller" + (tilt ? " + Tilt" : ""));
			if (sub)
			{
				ret.AddAxis("Input Length", 0.RangeTo(35112), 35112);
			}
			if (tilt)
			{
				ret.AddXYPair($"Tilt {{0}}", AxisPairOrientation.RightAndUp, (-90).RangeTo(90), 0);
			}
			if (rumble)
			{
				ret.HapticsChannels.Add("Rumble");
			}
			if (remote)
			{
				ret.AddAxis("Remote Command", 0.RangeTo(127), 127);
			}
			if (sgb)
			{
				for (var i = 0; i < 4; i++)
				{
					// ReSharper disable once AccessToModifiedClosure
					ret.BoolButtons.AddRange(
						new[] { "Up", "Down", "Left", "Right", "Start", "Select", "B", "A" }
							.Select(s => $"P{i + 1} {s}"));
				}
				ret.BoolButtons.Add("Power");
			}
			else
			{
				ret.BoolButtons.AddRange(new[] { "Up", "Down", "Left", "Right", "Start", "Select", "B", "A", "Power" });
			}
			return ret.MakeImmutable();
		}

		private LibGambatte.Buttons ControllerCallback(IntPtr p)
		{
			InputCallbacks.Call();
			IsLagFrame = false;

			if (IsSgb)
			{
				var index = LibGambatte.gambatte_getjoypadindex(GambatteState);
				var b = (uint)CurrentButtons;
				b >>= index * 8;
				b &= 0xFF;
				if ((b & 0x30) == 0x30) // snes software side blocks l+r
				{
					b &= ~0x30u;
				}
				if ((b & 0xC0) == 0xC0) // same for u+d
				{
					b &= ~0xC0u;
				}

				return (LibGambatte.Buttons)b;
			}

			return CurrentButtons;
		}

		private byte RemoteInputCallback()
		{
			InputCallbacks.Call();
			IsLagFrame = false;
			return RemoteCommand;
		}

		public bool IsCGBMode
#if true
			=> IsCgb; //TODO inline
#else
			=> LibGambatte.gambatte_iscgb(GambatteState);
#endif

		public bool IsCGBDMGMode
#if true
		{
			get
			{
				var headerCgbCompat = LibGambatte.gambatte_cpuread(GambatteState, 0x143);
				return (headerCgbCompat & 0x80) == 0 || (headerCgbCompat & 0x84) == 0x84;
			}
		}
#else
			=> LibGambatte.gambatte_iscgbdmg(GambatteState);
#endif

		private InputCallbackSystem _inputCallbacks = new();

		// low priority TODO: due to certain aspects of the core implementation,
		// we don't smartly use the ActiveChanged event here.
		public IInputCallbackSystem InputCallbacks => _inputCallbacks;

		/// <summary>
		/// for use in dual core
		/// </summary>
		public void ConnectInputCallbackSystem(InputCallbackSystem ics)
		{
			_inputCallbacks = ics;
		}

		// needs to match the reverse order of Libgambatte's button enum
		private static readonly IReadOnlyList<string> GB_BUTTON_ORDER_IN_BITMASK = new[] { "Down", "Up", "Left", "Right", "Start", "Select", "B", "A" };

		// input callback assumes buttons are ordered from first player in lsbs to last player in msbs
		private static readonly IReadOnlyList<string> SGB_BUTTON_ORDER_IN_BITMASK = new[] {
			"P4 Down", "P4 Up", "P4 Left", "P4 Right", "P4 Start", "P4 Select", "P4 B", "P4 A",
			"P3 Down", "P3 Up", "P3 Left", "P3 Right", "P3 Start", "P3 Select", "P3 B", "P3 A",
			"P2 Down", "P2 Up", "P2 Left", "P2 Right", "P2 Start", "P2 Select", "P2 B", "P2 A",
			"P1 Down", "P1 Up", "P1 Left", "P1 Right", "P1 Start", "P1 Select", "P1 B", "P1 A" };

		internal void FrameAdvancePrep(IController controller)
		{
			// update our local copy of the controller data
			uint b = 0;
			if (IsSgb)
			{
				for (var i = 0; i < 32; i++)
				{
					b <<= 1;
					if (controller.IsPressed(SGB_BUTTON_ORDER_IN_BITMASK[i])) b |= 1;
				}
			}
			else
			{
				for (var i = 0; i < 8; i++)
				{
					b <<= 1;
					if (controller.IsPressed(GB_BUTTON_ORDER_IN_BITMASK[i])) b |= 1;
				}
			}

			CurrentButtons = (LibGambatte.Buttons)b;

			RemoteCommand = _syncSettings.EnableRemote
				? (byte) controller.AxisValue("Remote Command")
				: default(byte);

			// the controller callback will set this to false if it actually gets called during the frame
			IsLagFrame = true;

			if (controller.IsPressed("Power"))
			{
				LibGambatte.gambatte_reset(GambatteState, ResetStallTicks);
			}

			if (Tracer.IsEnabled())
			{
				_tracecb = MakeTrace;
			}
			else
			{
				_tracecb = null;
			}

			LibGambatte.gambatte_settracecallback(GambatteState, _tracecb);

			LibGambatte.gambatte_setlayers(GambatteState, (_syncSettings.DisplayBG ? 1 : 0) | (_syncSettings.DisplayOBJ ? 2 : 0) | (_syncSettings.DisplayWindow ? 4 : 0));
		}

		internal void FrameAdvancePost()
		{
			if (IsLagFrame)
			{
				LagCount++;
			}

			Frame++;

			endofframecallback?.Invoke(LibGambatte.gambatte_cpuread(GambatteState, 0xff40));
		}

		private static int[] ColorsFromTitleHash(byte[] romdata)
		{
			var titleHash = 0;
			for (var i = 0; i < 16; i++)
			{
				titleHash += romdata[0x134 + i];
			}

			return (titleHash & 0xFF) switch
			{
				0x01 or 0x10 or 0x29 or 0x52 or 0x5D or 0x68 or 0x6D or 0xF6 => new[] { 0xFFFFFF, 0xFFAD63, 0x843100, 0x000000, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000, 0xFFFFFF, 0x7BFF31, 0x008400, 0x000000 },
				0x0C or 0x16 or 0x35 or 0x67 or 0x75 or 0x92 or 0x99 or 0xB7 => new[] { 0xFFFFFF, 0xFFAD63, 0x843100, 0x000000, 0xFFFFFF, 0xFFAD63, 0x843100, 0x000000, 0xFFFFFF, 0xFFAD63, 0x843100, 0x000000 },
				0x14 => new[] { 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0x7BFF31, 0x008400, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
				0x15 or 0xDB => new[] { 0xFFFFFF, 0xFFFF00, 0xFF0000, 0x000000, 0xFFFFFF, 0xFFFF00, 0xFF0000, 0x000000, 0xFFFFFF, 0xFFFF00, 0xFF0000, 0x000000 },
				0x17 or 0x8B => new[] { 0xFFFFFF, 0x7BFF31, 0x008400, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000 },
				0x19 => new[] { 0xFFFFFF, 0xFF9C00, 0xFF0000, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
				0x1D => new[] { 0xA59CFF, 0xFFFF00, 0x006300, 0x000000, 0xFF6352, 0xD60000, 0x630000, 0x000000, 0xFF6352, 0xD60000, 0x630000, 0x000000 },
				0x34 => new[] { 0xFFFFFF, 0x7BFF00, 0xB57300, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
				0x36 => new[] { 0x52DE00, 0xFF8400, 0xFFFF00, 0xFFFFFF, 0xFFFFFF, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
				0x39 or 0x43 or 0x97 => new[] { 0xFFFFFF, 0xFFAD63, 0x843100, 0x000000, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000 },
				0x3C => new[] { 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
				0x3D => new[] { 0xFFFFFF, 0x52FF00, 0xFF4200, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
				0x3E or 0xE0 => new[] { 0xFFFFFF, 0xFF9C00, 0xFF0000, 0x000000, 0xFFFFFF, 0xFF9C00, 0xFF0000, 0x000000, 0xFFFFFF, 0x5ABDFF, 0xFF0000, 0x0000FF },
				0x49 or 0x5C => new[] { 0xA59CFF, 0xFFFF00, 0x006300, 0x000000, 0xFF6352, 0xD60000, 0x630000, 0x000000, 0x0000FF, 0xFFFFFF, 0xFFFF7B, 0x0084FF },
				0x4B or 0x90 or 0x9A or 0xBD => new[] { 0xFFFFFF, 0x7BFF31, 0x008400, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
				0x4E => new[] { 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFFFF7B, 0x0084FF, 0xFF0000 },
				0x58 => new[] { 0xFFFFFF, 0xA5A5A5, 0x525252, 0x000000, 0xFFFFFF, 0xA5A5A5, 0x525252, 0x000000, 0xFFFFFF, 0xA5A5A5, 0x525252, 0x000000 },
				0x59 => new[] { 0xFFFFFF, 0xADAD84, 0x42737B, 0x000000, 0xFFFFFF, 0xFF7300, 0x944200, 0x000000, 0xFFFFFF, 0x5ABDFF, 0xFF0000, 0x0000FF },
				0x69 or 0xF2 => new[] { 0xFFFFFF, 0xFFFF00, 0xFF0000, 0x000000, 0xFFFFFF, 0xFFFF00, 0xFF0000, 0x000000, 0xFFFFFF, 0x5ABDFF, 0xFF0000, 0x0000FF },
				0x6B => new[] { 0xFFFFFF, 0x8C8CDE, 0x52528C, 0x000000, 0xFFC542, 0xFFD600, 0x943A00, 0x4A0000, 0xFFFFFF, 0x5ABDFF, 0xFF0000, 0x0000FF },
				0x6F => new[] { 0xFFFFFF, 0xFFCE00, 0x9C6300, 0x000000, 0xFFFFFF, 0xFFCE00, 0x9C6300, 0x000000, 0xFFFFFF, 0xFFCE00, 0x9C6300, 0x000000 },
				0x70 => new[] { 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0x00FF00, 0x318400, 0x004A00, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000 },
				0x71 or 0xFF => new[] { 0xFFFFFF, 0xFF9C00, 0xFF0000, 0x000000, 0xFFFFFF, 0xFF9C00, 0xFF0000, 0x000000, 0xFFFFFF, 0xFF9C00, 0xFF0000, 0x000000 },
				0x86 or 0xA8 => new[] { 0xFFFF9C, 0x94B5FF, 0x639473, 0x003A3A, 0xFFC542, 0xFFD600, 0x943A00, 0x4A0000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
				0x88 => new[] { 0xA59CFF, 0xFFFF00, 0x006300, 0x000000, 0xA59CFF, 0xFFFF00, 0x006300, 0x000000, 0xA59CFF, 0xFFFF00, 0x006300, 0x000000 },
				0x8C => new[] { 0xFFFFFF, 0xADAD84, 0x42737B, 0x000000, 0xFFFFFF, 0xFF7300, 0x944200, 0x000000, 0xFFFFFF, 0xADAD84, 0x42737B, 0x000000 },
				0x95 => new[] { 0xFFFFFF, 0x52FF00, 0xFF4200, 0x000000, 0xFFFFFF, 0x52FF00, 0xFF4200, 0x000000, 0xFFFFFF, 0x5ABDFF, 0xFF0000, 0x0000FF },
				0x9C => new[] { 0xFFFFFF, 0x8C8CDE, 0x52528C, 0x000000, 0xFFFFFF, 0x8C8CDE, 0x52528C, 0x000000, 0xFFC542, 0xFFD600, 0x943A00, 0x4A0000 },
				0x9D => new[] { 0xFFFFFF, 0x8C8CDE, 0x52528C, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFFAD63, 0x843100, 0x000000 },
				0xA2 or 0xF7 => new[] { 0xFFFFFF, 0xFFAD63, 0x843100, 0x000000, 0xFFFFFF, 0x7BFF31, 0x008400, 0x000000, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000 },
				0xAA => new[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000 },
				0xC9 => new[] { 0xFFFFCE, 0x63EFEF, 0x9C8431, 0x5A5A5A, 0xFFFFFF, 0xFF7300, 0x944200, 0x000000, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000 },
				0xCE or 0xD1 or 0xF0 => new[] { 0x6BFF00, 0xFFFFFF, 0xFF524A, 0x000000, 0xFFFFFF, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0xFFFFFF, 0xFFAD63, 0x843100, 0x000000 },
				0xE8 => new[] { 0x000000, 0x008484, 0xFFDE00, 0xFFFFFF, 0x000000, 0x008484, 0xFFDE00, 0xFFFFFF, 0x000000, 0x008484, 0xFFDE00, 0xFFFFFF },
				0x0D => romdata[0x137] switch
				{
					0x45 => new[] { 0xFFFFFF, 0x8C8CDE, 0x52528C, 0x000000, 0xFFC542, 0xFFD600, 0x943A00, 0x4A0000, 0xFFC542, 0xFFD600, 0x943A00, 0x4A0000 },
					0x52 => new[] { 0xFFFFFF, 0xFFFF00, 0xFF0000, 0x000000, 0xFFFFFF, 0xFFFF00, 0xFF0000, 0x000000, 0xFFFFFF, 0x5ABDFF, 0xFF0000, 0x0000FF },
					_ => new[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
				},
				0x18 => romdata[0x137] switch
				{
					0x4B => new[] { 0xFFFFFF, 0x8C8CDE, 0x52528C, 0x000000, 0xFFC542, 0xFFD600, 0x943A00, 0x4A0000, 0xFFFFFF, 0x5ABDFF, 0xFF0000, 0x0000FF },
					_ => new[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
				},
				0x27 => romdata[0x137] switch
				{
					0x42 => new[] { 0xA59CFF, 0xFFFF00, 0x006300, 0x000000, 0xFF6352, 0xD60000, 0x630000, 0x000000, 0x0000FF, 0xFFFFFF, 0xFFFF7B, 0x0084FF },
					0x4E => new[] { 0xFFFFFF, 0x7BFF31, 0x008400, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000 },
					_ => new[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
				},
				0x28 => romdata[0x137] switch
				{
					0x41 => new[] { 0x000000, 0x008484, 0xFFDE00, 0xFFFFFF, 0x000000, 0x008484, 0xFFDE00, 0xFFFFFF, 0x000000, 0x008484, 0xFFDE00, 0xFFFFFF },
					0x46 => new[] { 0xFFFFFF, 0x7BFF31, 0x008400, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
					_ => new[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
				},
				0x46 => romdata[0x137] switch
				{
					0x45 => new[] { 0xB5B5FF, 0xFFFF94, 0xAD5A42, 0x000000, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A },
					0x52 => new[] { 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000, 0xFFFF00, 0xFF0000, 0x630000, 0x000000, 0xFFFFFF, 0x7BFF31, 0x008400, 0x000000 },
					_ => new[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
				},
				0x61 => romdata[0x137] switch
				{
					0x41 => new[] { 0xFFFFFF, 0x7BFF31, 0x008400, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000 },
					0x45 => new[] { 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000 },
					_ => new[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
				},
				0x66 => romdata[0x137] switch
				{
					0x45 => new[] { 0xFFFFFF, 0x7BFF00, 0xB57300, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
					_ => new[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
				},
				0x6A => romdata[0x137] switch
				{
					0x49 => new[] { 0xFFFFFF, 0x52FF00, 0xFF4200, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
					0x4B => new[] { 0xFFFFFF, 0x8C8CDE, 0x52528C, 0x000000, 0xFFC542, 0xFFD600, 0x943A00, 0x4A0000, 0xFFFFFF, 0x5ABDFF, 0xFF0000, 0x0000FF },
					_ => new[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
				},
				0xA5 => romdata[0x137] switch
				{
					0x41 => new[] { 0x000000, 0x008484, 0xFFDE00, 0xFFFFFF, 0x000000, 0x008484, 0xFFDE00, 0xFFFFFF, 0x000000, 0x008484, 0xFFDE00, 0xFFFFFF },
					0x52 => new[] { 0xFFFFFF, 0xFFAD63, 0x843100, 0x000000, 0xFFFFFF, 0x7BFF31, 0x008400, 0x000000, 0xFFFFFF, 0x7BFF31, 0x008400, 0x000000 },
					_ => new[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
				},
				0xB3 => romdata[0x137] switch
				{
					0x42 => new[] { 0xA59CFF, 0xFFFF00, 0x006300, 0x000000, 0xFF6352, 0xD60000, 0x630000, 0x000000, 0x0000FF, 0xFFFFFF, 0xFFFF7B, 0x0084FF },
					0x52 => new[] { 0xFFFFFF, 0x52FF00, 0xFF4200, 0x000000, 0xFFFFFF, 0x52FF00, 0xFF4200, 0x000000, 0xFFFFFF, 0x5ABDFF, 0xFF0000, 0x0000FF },
					0x55 => new[] { 0xFFFFFF, 0xADAD84, 0x42737B, 0x000000, 0xFFFFFF, 0xFF7300, 0x944200, 0x000000, 0xFFFFFF, 0xFF7300, 0x944200, 0x000000 },
					_ => new[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
				},
				0xBF => romdata[0x137] switch
				{
					0x20 => new[] { 0xFFFFFF, 0x8C8CDE, 0x52528C, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
					0x43 => new[] { 0x6BFF00, 0xFFFFFF, 0xFF524A, 0x000000, 0xFFFFFF, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0xFFFFFF, 0xFFAD63, 0x843100, 0x000000 },
					_ => new[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
				},
				0xC6 => romdata[0x137] switch
				{
					0x41 => new[] { 0xFFFFFF, 0xADAD84, 0x42737B, 0x000000, 0xFFFFFF, 0xFF7300, 0x944200, 0x000000, 0xFFFFFF, 0x5ABDFF, 0xFF0000, 0x0000FF },
					_ => new[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
				},
				0xD3 => romdata[0x137] switch
				{
					0x49 => new[] { 0xFFFFFF, 0xADAD84, 0x42737B, 0x000000, 0xFFFFFF, 0xFFAD63, 0x843100, 0x000000, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000 },
					0x52 => new[] { 0xFFFFFF, 0x8C8CDE, 0x52528C, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0x8C8CDE, 0x52528C, 0x000000 },
					_ => new[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
				},
				0xF4 => romdata[0x137] switch
				{
					0x20 => new[] { 0xFFFFFF, 0x7BFF00, 0xB57300, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
					0x2D => new[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0x63A5FF, 0x0000FF, 0x000000 },
					_ => new[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
				},
				_ => new[] { 0xFFFFFF, 0x7BFF31, 0x0063C5, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000, 0xFFFFFF, 0xFF8484, 0x943A3A, 0x000000 },
			};
		}

		public IGPUMemoryAreas LockGPU()
		{
			var _vram = IntPtr.Zero;
			var _bgpal = IntPtr.Zero;
			var _sppal = IntPtr.Zero;
			var _oam = IntPtr.Zero;
			var unused = 0;
			if (!LibGambatte.gambatte_getmemoryarea(GambatteState, LibGambatte.MemoryAreas.vram, ref _vram, ref unused)
				|| !LibGambatte.gambatte_getmemoryarea(GambatteState, LibGambatte.MemoryAreas.bgpal, ref _bgpal, ref unused)
				|| !LibGambatte.gambatte_getmemoryarea(GambatteState, LibGambatte.MemoryAreas.sppal, ref _sppal, ref unused)
				|| !LibGambatte.gambatte_getmemoryarea(GambatteState, LibGambatte.MemoryAreas.oam, ref _oam, ref unused))
			{
				throw new InvalidOperationException("Unexpected error in gambatte_getmemoryarea");
			}
			return new GPUMemoryAreas
			{
				Vram = _vram,
				Oam = _oam,
				Sppal = _sppal,
				Bgpal = _bgpal,
			};
		}

		private class GPUMemoryAreas : IGPUMemoryAreas
		{
			public IntPtr Vram { get; init; }

			public IntPtr Oam { get; init; }

			public IntPtr Sppal { get; init; }

			public IntPtr Bgpal { get; init; }

			public void Dispose() {}
		}

		/// <summary>
		/// set up callback
		/// </summary>
		/// <param name="line">scanline. -1 = end of frame, -2 = RIGHT NOW</param>
		public void SetScanlineCallback(ScanlineCallback callback, int line)
		{
			if (GambatteState == IntPtr.Zero)
			{
				return; // not sure how this is being reached.  tried the debugger...
			}

			endofframecallback = null;
			if (callback == null || line is -1 or -2)
			{
				scanlinecb = null;
				LibGambatte.gambatte_setscanlinecallback(GambatteState, null, 0);
				switch (line)
				{
					case -1:
						endofframecallback = callback;
						break;
					case -2:
						callback(LibGambatte.gambatte_cpuread(GambatteState, 0xFF40));
						break;
				}
			}
			else if (line is >= 0 and <= 153)
			{
				scanlinecb = () => callback(LibGambatte.gambatte_cpuread(GambatteState, 0xFF40));
				LibGambatte.gambatte_setscanlinecallback(GambatteState, scanlinecb, line);
			}
			else
			{
				throw new ArgumentOutOfRangeException(nameof(line), "line must be in [0, 153]");
			}
		}

		private GambattePrinter printer;

		/// <summary>
		/// set up Printer callback
		/// </summary>
		public void SetPrinterCallback(PrinterCallback callback)
		{
			// Copying SetScanlineCallback for this check, I assume this is still a bug somewhere
			if (GambatteState == IntPtr.Zero)
			{
				return; // not sure how this is being reached.  tried the debugger...
			}

			if (callback != null)
			{
				printer = new(this, callback);
				_linkConnected = true;
			}
			else
			{
				_linkConnected = false;
				if (printer != null) // have no idea how this is ever null???
				{
					printer.Disconnect();
					printer = null;
				}
			}
		}

		private LibGambatte.ScanlineCallback scanlinecb;
		private ScanlineCallback endofframecallback;

		/// <summary>
		/// update gambatte core's internal colors
		/// </summary>
		public void ChangeDMGColors(int[] colors)
		{
			for (var i = 0; i < 12; i++)
			{
				LibGambatte.gambatte_setdmgpalettecolor(GambatteState, (LibGambatte.PalType)(i / 4), (uint)i % 4, (uint)colors[i]);
			}
		}

		public void SetCGBColors(GBColors.ColorType type)
		{
			var lut = GBColors.GetLut(type, IsSgb, _syncSettings.ConsoleMode is GambatteSyncSettings.ConsoleModeType.GBA);
			LibGambatte.gambatte_setcgbpalette(GambatteState, lut);
		}
	}
}
