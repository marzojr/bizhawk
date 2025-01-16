using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.LR35902;

using BizHawk.Emulation.Cores.Consoles.Nintendo.Gameboy;
using System.Runtime.InteropServices;
using System.Collections.Generic;

using BizHawk.Common.ReflectionExtensions;

// TODO: mode1_disableint_gbc.gbc behaves differently between GBC and GBA, why?
// TODO: Window Position A6 behaves differently
// TODO: Verify open bus behaviour for bad SRAM accesses for other MBCs
// TODO: Apparently sprites at x=A7 do not stop the trigger for FF0F bit flip, but still do not dispatch interrupt or
// mode 3 change, see 10spritesPrLine_10xposA7_m0irq_2_dmg08_cgb04c_out2.gbc
// TODO: there is a tile glitch when setting LCDC.Bit(4) in GBC that is not implemented yet, the version of the glitch for reset is implemented though
// TODO: In some GBC models, apparently unmapped memory after OAM contains 48 bytes that are fully read/write'able
// this is not implemented and which models it effects is not clear, see oam_echo_ram_read.gbc and oam_echo_ram_read_2.gbc

namespace BizHawk.Emulation.Cores.Nintendo.GBHawk
{
	[Core(
		name: CoreNames.GbHawk,
		author: "alyosha and BizHawk contributors")]
	public partial class GBHawk : IEmulator, ISaveRam, IDebuggable, IInputPollable, IRegionable, IGameboyCommon,
	ISettable<GBHawk.GBSettings, GBHawk.GBSyncSettings>
	{
		internal static class RomChecksums
		{
			public const string BombermanCollection = "SHA1:385F8FAFA53A83F8F65E1E619FE124BBF7DB4A98";

			public const string BombermanSelectionKORNotInGameDB = "SHA1:52451464A9F4DD5FAEFE4594954CBCE03BFF0D05";

			public const string MortalKombatIAndIIUSAEU = "SHA1:E337489255B33367CE26194FC4038346D3388BD9";

			public const string PirateRockMan8 = "MD5:CAE0998A899DF2EE6ABA8E7695C2A096";

			public const string PirateSachen1 = "MD5:D3C1924D847BC5D125BF54C2076BE27A";

			public const string UnknownRomA = "MD5:97122B9B183AAB4079C8D36A4CE6E9C1";

			public const string WisdomTreeExodus = "SHA1:685D5A47A1FC386D7B451C8B2733E654B7779B71";

			public const string WisdomTreeJoshua = "SHA1:019B4B0E76336E2613AE6E8B415B5C65F6D465A5";

			public const string WisdomTreeKJVBible = "SHA1:6362FDE9DCB08242A64F2FBEA33DE93D1776A6E0";

			public const string WisdomTreeNIVBible = "SHA1:136CF97A8C3560EC9DB3D8F354D91B7DE27E0743";

			public const string WisdomTreeSpiritualWarfare = "SHA1:6E6AE5DBD8FF8B8F41B8411EF119E96E4ECF763F";
		}

		// this register controls whether or not the GB BIOS is mapped into memory
		public byte GB_bios_register;

		public byte input_register;

		// The unused bits in this register are still read/writable
		public byte REG_FFFF;
		// The unused bits in this register (interrupt flags) are always set
		public byte REG_FF0F = 0xE0;
		// Updating reg FF0F seems to be delayed by one cycle,needs more testing
		public byte REG_FF0F_OLD = 0xE0;

		// memory domains
		public byte[] RAM = new byte[0x8000]; // only 0x2000 available to GB
		public byte[] ZP_RAM = new byte[0x80];
		/* 
		 * VRAM is arranged as: 
		 * 0x1800 Tiles
		 * 0x400 BG Map 1
		 * 0x400 BG Map 2
		 * 0x1800 Tiles
		 * 0x400 CA Map 1
		 * 0x400 CA Map 2
		 * Only the top set is available in GB (i.e. VRAM_Bank = 0)
		 */
		public byte[] VRAM = new byte[0x4000];
		public byte[] OAM = new byte[0xA0];

		// vblank memory domains
		public byte[] RAM_vbls = new byte[0x8000];
		public byte[] ZP_RAM_vbls = new byte[0x80];
		public byte[] VRAM_vbls = new byte[0x4000];
		public byte[] OAM_vbls = new byte[0xA0];

		public int RAM_Bank;
		public int RAM_Bank_ret;
		public byte VRAM_Bank;
		internal bool is_GBC, is_GB_in_GBC;
		public bool GBC_compat; // compatibility mode for GB games played on GBC
		public bool double_speed;
		public bool speed_switch;
		public bool HDMA_transfer; // stalls CPU when in progress
		public byte bus_value; // we need the last value on the bus for proper emulation of blocked SRAM
		public ulong bus_access_time; // also need to keep track of the time of the access since it doesn't last very long
		public byte IR_reg, IR_mask, IR_signal, IR_receive, IR_self;
		public int IR_write;

		// several undocumented GBC Registers
		public byte undoc_6C, undoc_72, undoc_73, undoc_74, undoc_75, undoc_76, undoc_77;

		public byte[] _bios;
		public readonly byte[] _rom;
		public readonly byte[] header = new byte[0x50];

		public byte[] cart_RAM;
		public byte[] cart_RAM_vbls;
		public bool has_bat;

		private int _frame = 0;

		public bool Use_MT;
		public ushort addr_access;

		public MapperBase mapper;

		private readonly GBHawkDisassembler _disassembler = new();

		private readonly ITraceable _tracer;

		public LR35902 cpu;
		public PPU ppu;
		public readonly GBTimer timer;
		public Audio audio;
		public SerialPort serialport;

		private static readonly byte[] GBA_override = { 0xFF, 0x00, 0xCD, 0x03, 0x35, 0xAA, 0x31, 0x90, 0x94, 0x00, 0x00, 0x00, 0x00 };

		[CoreConstructor(VSystemID.Raw.GB)]
		[CoreConstructor(VSystemID.Raw.GBC)]
		public GBHawk(CoreComm comm, GameInfo game, byte[] rom, /*string gameDbFn,*/ GBSettings settings, GBSyncSettings syncSettings, bool subframe = false)
		{
			var ser = new BasicServiceProvider(this);

			cpu = new LR35902
			{
				ReadMemory = ReadMemory,
				WriteMemory = WriteMemory,
				PeekMemory = PeekMemory,
				DummyReadMemory = ReadMemory,
				OnExecFetch = ExecFetch,
				SpeedFunc = SpeedFunc,
				GetButtons = GetButtons,
				GetIntRegs = GetIntRegs,
				SetIntRegs = SetIntRegs
			};
			
			timer = new();
			audio = new Audio();
			serialport = new SerialPort();

			_ = PutSettings(settings ?? new GBSettings());
			_syncSettings = syncSettings ?? new GBSyncSettings();

			is_GBC = _syncSettings.ConsoleMode switch
			{
				GBSyncSettings.ConsoleModeType.GB => false,
				GBSyncSettings.ConsoleModeType.GBC => true,
				_ => game.System is not "GB"
			};
			// Load up a BIOS and initialize the correct PPU
			_bios = comm.CoreFileProvider.GetFirmwareOrThrow(new(is_GBC ? "GBC" : "GB", "World"), "BIOS Not Found, Cannot Load");
			ppu = is_GBC ? new GBC_PPU() : new GB_PPU();

			// set up IR register to off state
			if (is_GBC) { IR_mask = 0; IR_reg = 0x3E; IR_receive = 2; IR_self = 2; IR_signal = 2; }

			// Here we modify the BIOS if GBA mode is set (credit to ExtraTricky)
			if (is_GBC && _syncSettings.GBACGB)
			{
				for (int i = 0; i < 13; i++)
				{
					_bios[i + 0xF3] = (byte)((GBA_override[i] + _bios[i + 0xF3]) & 0xFF);
				}
				IR_mask = 2;
			}

			// CPU needs to know about GBC status too
			cpu.is_GBC = is_GBC;

			Buffer.BlockCopy(rom, 0x100, header, 0, 0x50);

			if (is_GBC && ((header[0x43] != 0x80) && (header[0x43] != 0xC0)))
			{
				ppu = new GBC_GB_PPU();
				is_GB_in_GBC = true; // for movie files
			}

			var romHashMD5 = MD5Checksum.ComputePrefixedHex(rom);
			Console.WriteLine(romHashMD5);
			var romHashSHA1 = SHA1Checksum.ComputePrefixedHex(rom);
			Console.WriteLine(romHashSHA1);

			_rom = rom;
			var mppr = Setup_Mapper(romHashMD5, romHashSHA1);
			if (cart_RAM != null) { cart_RAM_vbls = new byte[cart_RAM.Length]; }

			_controllerDeck = new(mppr is "MBC7"
				? typeof(StandardTilt).DisplayName()
				: GBHawkControllerDeck.DefaultControllerName, subframe);

			timer.Core = this;
			audio.Core = this;
			ppu.Core = this;
			serialport.Core = this;

			ser.Register<IVideoProvider>(this);
			ser.Register<ISoundProvider>(audio);
			ServiceProvider = ser;

			_ = PutSettings(settings ?? new GBSettings());
			_syncSettings = syncSettings ?? new GBSyncSettings();

			_tracer = new TraceBuffer(cpu.TraceHeader);
			ser.Register<ITraceable>(_tracer);
			ser.Register<IStatable>(new StateSerializer(SyncState));
            ser.Register<IDisassemblable>(_disassembler);
			SetupMemoryDomains();
			cpu.SetCallbacks(ReadMemory, PeekMemory, PeekMemory, WriteMemory);
			HardReset();

			_scanlineCallback = null;

			DeterministicEmulation = true;
		}

		public bool IsCGBMode
			=> is_GBC; //TODO inline

		public bool IsCGBDMGMode
			=> is_GB_in_GBC; //TODO inline

		/// <summary>
		/// Produces a palette in the form that certain frontend inspection tools.
		/// May or may not return a reference to the core's own palette, so please don't mutate.
		/// </summary>
		private uint[] SynthesizeFrontendBGPal()
		{
			if (is_GBC)
			{
				return ppu.BG_palette;
			}
			else
			{
				var scratch = new uint[4];
				for (int i = 0; i < 4; i++)
				{
					scratch[i] = ppu.color_palette[(ppu.BGP >> (i * 2)) & 3];
				}
				return scratch;
			}
		}

		/// <summary>
		/// Produces a palette in the form that certain frontend inspection tools.
		/// May or may not return a reference to the core's own palette, so please don't mutate.
		/// </summary>
		private uint[] SynthesizeFrontendSPPal()
		{
			if (is_GBC)
			{
				return ppu.OBJ_palette;
			}
			else
			{
				var scratch = new uint[8];
				for (int i = 0; i < 4; i++)
				{
					scratch[i] = ppu.color_palette[(ppu.obj_pal_0 >> (i * 2)) & 3];
					scratch[i + 4] = ppu.color_palette[(ppu.obj_pal_1 >> (i * 2)) & 3];
				}
				return scratch;
			}
		}

		public IGPUMemoryAreas LockGPU()
		{
			return new GPUMemoryAreas(
				VRAM,
				OAM,
				SynthesizeFrontendSPPal(),
				SynthesizeFrontendBGPal()
			);
		}

		private class GPUMemoryAreas : IGPUMemoryAreas
		{
			public IntPtr Vram { get; }

			public IntPtr Oam { get; }

			public IntPtr Sppal { get; }

			public IntPtr Bgpal { get; }

			private readonly List<GCHandle> _handles = new();

			public GPUMemoryAreas(byte[] vram, byte[] oam, uint[] sppal, uint[] bgpal)
			{
				Vram = AddHandle(vram);
				Oam = AddHandle(oam);
				Sppal = AddHandle(sppal);
				Bgpal = AddHandle(bgpal);
			}

			private IntPtr AddHandle(object target)
			{
				var handle = GCHandle.Alloc(target, GCHandleType.Pinned);
				_handles.Add(handle);
				return handle.AddrOfPinnedObject();
			}

			public void Dispose()
			{
				foreach (var h in _handles)
					h.Free();
				_handles.Clear();
			}
		}

		public ScanlineCallback _scanlineCallback;
		public int _scanlineCallbackLine = 0;

		public void SetScanlineCallback(ScanlineCallback callback, int line)
		{
			_scanlineCallback = callback;
			_scanlineCallbackLine = line;

			if (line == -2)
			{
				_scanlineCallback(ppu.LCDC);
			}
		}

#pragma warning disable CS0414
		private PrinterCallback _printerCallback = null;
#pragma warning restore CS0414

		public void SetPrinterCallback(PrinterCallback callback)
		{
			_printerCallback = null;
		}

		public DisplayType Region => DisplayType.NTSC;

		private readonly GBHawkControllerDeck _controllerDeck;

		public void HardReset()
		{
			GB_bios_register = 0; // bios enable
			GBC_compat = is_GBC;
			in_vblank = true; // we start off in vblank since the LCD is off
			in_vblank_old = true;
			double_speed = false;
			VRAM_Bank = 0;
			RAM_Bank = 1; // RAM bank always starts as 1 (even writing zero still sets 1)
			RAM_Bank_ret = 0; // return value can still be zero even though the bank itself cannot be
			delays_to_process = false;
			controller_delay_cd = 0;
			clear_counter = 0;

			Register_Reset();
			timer.Reset();
			ppu.Reset();
			audio.Reset();
			serialport.Reset();
			mapper.Reset();
			cpu.Reset();
			
			vid_buffer = new uint[VirtualWidth * VirtualHeight];
			frame_buffer = new int[VirtualWidth * VirtualHeight];

			uint startup_color = (!is_GBC && (_settings.Palette == GBSettings.PaletteType.Gr)) ? 0xFFA4C505 : 0xFFFFFFFF;
			for (int i = 0; i < vid_buffer.Length; i++)
			{
				vid_buffer[i] = startup_color;
				frame_buffer[i] = (int)vid_buffer[i];
			}

			for (int i = 0; i < ZP_RAM.Length; i++)
			{
				ZP_RAM[i] = 0;
			}

			if (is_GBC)
			{
				if (_syncSettings.GBACGB)
				{
					// on GBA, initial RAM is mostly random, choosing 0 allows for stable clear and hotswap for games that encounter
					// uninitialized RAM
					for (int i = 0; i < RAM.Length; i++)
					{
						RAM[i] = GBA_Init_RAM[i];
					}
				}
				else
				{
					for (int i = 0; i < 0x800; i++)
					{
						if ((i & 0xF) < 8)
						{
							RAM[i] = 0xFF;
							RAM[i + 0x1000] = 0xFF;
							RAM[i + 0x2000] = 0xFF;
							RAM[i + 0x3000] = 0xFF;
							RAM[i + 0x4000] = 0xFF;
							RAM[i + 0x5000] = 0xFF;
							RAM[i + 0x6000] = 0xFF;
							RAM[i + 0x7000] = 0xFF;

							RAM[i + 0x800] = 0;
							RAM[i + 0x1800] = 0;
							RAM[i + 0x2800] = 0;
							RAM[i + 0x3800] = 0;
							RAM[i + 0x4800] = 0;
							RAM[i + 0x5800] = 0;
							RAM[i + 0x6800] = 0;
							RAM[i + 0x7800] = 0;
						}
						else
						{
							RAM[i] = 0;
							RAM[i + 0x1000] = 0;
							RAM[i + 0x2000] = 0;
							RAM[i + 0x3000] = 0;
							RAM[i + 0x4000] = 0;
							RAM[i + 0x5000] = 0;
							RAM[i + 0x6000] = 0;
							RAM[i + 0x7000] = 0;

							RAM[i + 0x800] = 0xFF;
							RAM[i + 0x1800] = 0xFF;
							RAM[i + 0x2800] = 0xFF;
							RAM[i + 0x3800] = 0xFF;
							RAM[i + 0x4800] = 0xFF;
							RAM[i + 0x5800] = 0xFF;
							RAM[i + 0x6800] = 0xFF;
							RAM[i + 0x7800] = 0xFF;
						}
					}

					// some bytes are like this is Gambatte, hardware anomoly? Is it consistent across versions?
					/*
					for (int i = 0; i < 16; i++)
					{
						RAM[0xE02 + (16 * i)] = 0;
						RAM[0xE0A + (16 * i)] = 0xFF;

						RAM[0x1E02 + (16 * i)] = 0;
						RAM[0x1E0A + (16 * i)] = 0xFF;

						RAM[0x2E02 + (16 * i)] = 0;
						RAM[0x2E0A + (16 * i)] = 0xFF;

						RAM[0x3E02 + (16 * i)] = 0;
						RAM[0x3E0A + (16 * i)] = 0xFF;

						RAM[0x4E02 + (16 * i)] = 0;
						RAM[0x4E0A + (16 * i)] = 0xFF;

						RAM[0x5E02 + (16 * i)] = 0;
						RAM[0x5E0A + (16 * i)] = 0xFF;

						RAM[0x6E02 + (16 * i)] = 0;
						RAM[0x6E0A + (16 * i)] = 0xFF;

						RAM[0x7E02 + (16 * i)] = 0;
						RAM[0x7E0A + (16 * i)] = 0xFF;
					}
					*/
				}
			}
			else
			{
				for (int j = 0; j < 2; j++)
				{
					for (int i = 0; i < 0x100; i++)
					{
						RAM[j * 0x1000 + i] = 0;
						RAM[j * 0x1000 + i + 0x100] = 0xFF;
						RAM[j * 0x1000 + i + 0x200] = 0;
						RAM[j * 0x1000 + i + 0x300] = 0xFF;
						RAM[j * 0x1000 + i + 0x400] = 0;
						RAM[j * 0x1000 + i + 0x500] = 0xFF;
						RAM[j * 0x1000 + i + 0x600] = 0;
						RAM[j * 0x1000 + i + 0x700] = 0xFF;
						RAM[j * 0x1000 + i + 0x800] = 0;
						RAM[j * 0x1000 + i + 0x900] = 0xFF;
						RAM[j * 0x1000 + i + 0xA00] = 0;
						RAM[j * 0x1000 + i + 0xB00] = 0xFF;
						RAM[j * 0x1000 + i + 0xC00] = 0;
						RAM[j * 0x1000 + i + 0xD00] = 0xFF;
						RAM[j * 0x1000 + i + 0xE00] = 0;
						RAM[j * 0x1000 + i + 0xF00] = 0xFF;
					}
				}
			}
		}

		// TODO: move callbacks to cpu to avoid having to make a non-inlinable 
		private void ExecFetch(ushort addr)
		{
			if (MemoryCallbacks.HasExecutes)
			{
				uint flags = (uint)MemoryCallbackFlags.AccessExecute;
				MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "System Bus");
			}
		}

		public string Setup_Mapper(string romHashMD5, string romHashSHA1)
		{
			// setup up mapper based on header entry
			string mppr;

			switch (header[0x47])
			{
				case 0x0: mapper = new MapperDefault();		mppr = "NROM";							break;
				case 0x1: mapper = new MapperMBC1();		mppr = "MBC1";							break;
				case 0x2: mapper = new MapperMBC1();		mppr = "MBC1";							break;
				case 0x3: mapper = new MapperMBC1();		mppr = "MBC1";		has_bat = true;		break;
				case 0x5: mapper = new MapperMBC2();		mppr = "MBC2";							break;
				case 0x6: mapper = new MapperMBC2();		mppr = "MBC2";		has_bat = true;		break;
				case 0x8: mapper = new MapperDefault();		mppr = "NROM";							break;
				case 0x9: mapper = new MapperDefault();		mppr = "NROM";		has_bat = true;		break;
				case 0xB: mapper = new MapperMMM01();		mppr = "MMM01";							break;
				case 0xC: mapper = new MapperMMM01();		mppr = "MMM01";							break;
				case 0xD: mapper = new MapperMMM01();		mppr = "MMM01";		has_bat = true;		break;
				case 0xF: mapper = new MapperMBC3();		mppr = "MBC3";		has_bat = true;		break;
				case 0x10: mapper = new MapperMBC3();		mppr = "MBC3";		has_bat = true;		break;
				case 0x11: mapper = new MapperMBC3();		mppr = "MBC3";							break;
				case 0x12: mapper = new MapperMBC3();		mppr = "MBC3";							break;
				case 0x13: mapper = new MapperMBC3();		mppr = "MBC3";		has_bat = true;		break;
				case 0x19: mapper = new MapperMBC5();		mppr = "MBC5";							break;
				case 0x1A: mapper = new MapperMBC5();		mppr = "MBC5";							break;
				case 0x1B: mapper = new MapperMBC5();		mppr = "MBC5";		has_bat = true;				break;
				case 0x1C: mapper = new MapperMBC5();		mppr = "MBC5";							break;
				case 0x1D: mapper = new MapperMBC5();		mppr = "MBC5";							break;
				case 0x1E: mapper = new MapperMBC5();		mppr = "MBC5";		has_bat = true;		break;
				case 0x20: mapper = new MapperMBC6();		mppr = "MBC6";							break;
				case 0x22: mapper = new MapperMBC7();		mppr = "MBC7";		has_bat = true;		break;
				case 0xFC: mapper = new MapperCamera();		mppr = "CAM";		has_bat = true;		break;
				case 0xFD: mapper = new MapperTAMA5();		mppr = "TAMA5";		has_bat = true;		break;
				case 0xFE: mapper = new MapperHuC3();		mppr = "HuC3";		has_bat = true;		break;
				case 0xFF: mapper = new MapperHuC1();		mppr = "HuC1";							break;

				// Bootleg mappers
				// NOTE: Sachen mapper selection does not account for scrambling, so if another bootleg mapper
				// identifies itself as 0x31, this will need to be modified
				case 0x31: mapper = new MapperSachen2();	mppr = "Schn2";							break;

				case 0x4:
				case 0x7:
				case 0xA:
				case 0xE:
				case 0x14:
				case 0x15:
				case 0x16:
				case 0x17:
				case 0x18:
				case 0x1F:
				case 0x21:
				default:
					// mapper not implemented
					Console.WriteLine(header[0x47]);
					throw new Exception("Mapper not implemented");
			}

			// special case for multi cart mappers
			if (romHashMD5 is RomChecksums.UnknownRomA
				|| romHashSHA1 is RomChecksums.BombermanCollection or RomChecksums.MortalKombatIAndIIUSAEU or RomChecksums.BombermanSelectionKORNotInGameDB)
			{
				Console.WriteLine("Using Multi-Cart Mapper");
				mapper = new MapperMBC1Multi();
			}
			
			// Wisdom Tree does not identify their mapper, so use hash instead
			else if (romHashSHA1 is RomChecksums.WisdomTreeJoshua or RomChecksums.WisdomTreeSpiritualWarfare or RomChecksums.WisdomTreeExodus or RomChecksums.WisdomTreeKJVBible or RomChecksums.WisdomTreeNIVBible)
			{
				Console.WriteLine("Using Wisdom Tree Mapper");
				mapper = new MapperWT();
				mppr = "Wtree";
			}

			// special case for bootlegs
			else if (romHashMD5 == RomChecksums.PirateRockMan8)
			{
				Console.WriteLine("Using RockMan 8 (Unlicensed) Mapper");
				mapper = new MapperRM8();
			}
			else if (romHashMD5 == RomChecksums.PirateSachen1)
			{
				Console.WriteLine("Using Sachen 1 (Unlicensed) Mapper");
				mapper = new MapperSachen1();
				mppr = "Schn1";
			}

			Console.Write("Mapper: ");
			Console.WriteLine(mppr);

			cart_RAM = null;

			switch (header[0x49])
			{
				case 1:
					cart_RAM = new byte[0x800];
					break;
				case 2:
					cart_RAM = new byte[0x2000];
					break;
				case 3:
					cart_RAM = new byte[0x8000];
					break;
				case 4:
					cart_RAM = new byte[0x20000];
					break;
				case 5:
					cart_RAM = new byte[0x10000];
					break;
				case 0:
					Console.WriteLine("Mapper Number indicates Battery Backed RAM but none present.");
					Console.WriteLine("Disabling Battery Setting.");
					has_bat = false;
					break;
			}

			// Sachen maper not known to have RAM
			if ((mppr == "Schn1") || (mppr == "Schn2"))
			{
				cart_RAM = null;
				Use_MT = true;
			}

			// mbc2 carts have built in RAM
			if (mppr == "MBC2")
			{
				cart_RAM = new byte[0x200];
			}

			// mbc7 has 256 bytes of RAM, regardless of any header info
			if (mppr == "MBC7")
			{
				cart_RAM = new byte[0x100];
				has_bat = true;
			}

			// TAMA5 has 0x1000 bytes of RAM, regardless of any header info
			if (mppr == "TAMA5")
			{
				cart_RAM = new byte[0x20];
				has_bat = true;
			}

			mapper.Core = this;

			if (cart_RAM != null && (mppr != "MBC7"))
			{
				Console.Write("RAM: "); Console.WriteLine(cart_RAM.Length);

				for (int i = 0; i < cart_RAM.Length; i++)
				{
					cart_RAM[i] = 0xFF;
				}
			}
			
			// Extra RTC initialization for mbc3, HuC3, and TAMA5
			if (mppr == "MBC3")
			{
				Use_MT = true;

				mapper.RTC_Get(_syncSettings.RTCOffset, 5);

				int days = (int)Math.Floor(_syncSettings.RTCInitialTime / 86400.0);

				int days_upper = ((days & 0x100) >> 8) | ((days & 0x200) >> 2);

				mapper.RTC_Get(days_upper, 4);
				mapper.RTC_Get(days & 0xFF, 3);

				int remaining = _syncSettings.RTCInitialTime - (days * 86400);

				int hours = (int)Math.Floor(remaining / 3600.0);

				mapper.RTC_Get(hours & 0xFF, 2);

				remaining -= (hours * 3600);

				int minutes = (int)Math.Floor(remaining / 60.0);

				mapper.RTC_Get(minutes & 0xFF, 1);

				remaining -= (minutes * 60);

				mapper.RTC_Get(remaining & 0xFF, 0);
			}

			if (mppr == "HuC3")
			{
				Use_MT = true;

				int years = (int)Math.Floor(_syncSettings.RTCInitialTime / 31536000.0);

				mapper.RTC_Get(years, 24);

				int remaining = _syncSettings.RTCInitialTime - (years * 31536000);

				int days = (int)Math.Floor(remaining / 86400.0);
				int days_upper = (days >> 8) & 0xF;

				mapper.RTC_Get(days_upper, 20);
				mapper.RTC_Get(days & 0xFF, 12);

				remaining -= (days * 86400);

				int minutes = (int)Math.Floor(remaining / 60.0);
				int minutes_upper = (minutes >> 8) & 0xF;

				mapper.RTC_Get(minutes_upper, 8);
				mapper.RTC_Get(remaining & 0xFF, 0);
			}

			if (mppr == "TAMA5")
			{
				Use_MT = true;

				// currently no date / time input for TAMA5

			}

			return mppr;
		}

		public class GBHawkDisassembler : VerifiedDisassembler
		{
			public bool UseRGBDSSyntax;

			public override IEnumerable<string> AvailableCpus { get; } = new[] { "LR35902" };

			public override string PCRegisterName => "PC";

			public override string Disassemble(MemoryDomain m, uint addr, out int length)
			{
				var ret = LR35902.Disassemble((ushort) addr, a => m.PeekByte(a), UseRGBDSSyntax, out var tmp);
				length = tmp;
				return ret;
			}
		}
	}
}
