using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.M6502;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Atari.A7800Hawk
{
	[Core(
		name: CoreNames.A7800Hawk,
		author: "alyosha and BizHawk contributors")]
	[ServiceNotApplicable(typeof(ISettable<,>))]
	public partial class A7800Hawk : IEmulator, ISaveRam, IDebuggable, IInputPollable,
		IRegionable, IBoardInfo, ISettable<object, A7800Hawk.A7800SyncSettings>
	{
		internal static class RomChecksums
		{
			public const string KaratekaPAL = "MD5:5E0A1E832BBCEA6FACB832FDE23A440A";

			public const string Serpentine = "MD5:9BD70C06D3386F76F8162881699A777A";
		}

		// this register selects between 2600 and 7800 mode in the A7800
		// however, we already have a 2600 emulator so this core will only be loading A7800 games
		// furthermore, the location of the register is in the same place as TIA registers (0x0-0x1F)
		// any writes to this location before the register is 'locked' will go to the register and not the TIA
		public byte A7800_control_register;

		// memory domains
		public byte[] Maria_regs = new byte[0x20];
		public byte[] RAM = new byte[0x1000];
		public byte[] RAM_6532 = new byte[0x80];
		public byte[] hs_bios_mem = new byte[0x800];
		public byte[] _hsram = new byte[2048];

		public readonly byte[] _rom;
		public readonly byte[] _hsbios;
		public readonly byte[] _bios;

		private int _frame = 0;

		public string s_mapper;
		public MapperBase mapper;
		public bool small_flag = false;
		public bool PAL_Kara = false;
		public int cart_RAM = 0;
		public bool is_pokey = false;
		public bool is_pokey_450 = false;

		private readonly ITraceable _tracer;

		public MOS6502X<CpuLink> cpu;
		public Maria maria;
		public bool _isPAL;
		public M6532 m6532;
		public TIA tia;
		public Pokey pokey;

		public struct CpuLink : IMOS6502XLink
		{
			private readonly A7800Hawk _a7800;

			public CpuLink(A7800Hawk a7800)
			{
				_a7800 = a7800;
			}

			public byte DummyReadMemory(ushort address) => _a7800.ReadMemory(address);

			public void OnExecFetch(ushort address) => _a7800.ExecFetch(address);

			public byte PeekMemory(ushort address) => _a7800.ReadMemory(address);

			public byte ReadMemory(ushort address) => _a7800.ReadMemory(address);

			public void WriteMemory(ushort address, byte value) => _a7800.WriteMemory(address, value);
		}

		[CoreConstructor(VSystemID.Raw.A78)]
		public A7800Hawk(CoreComm comm, byte[] rom, A7800SyncSettings syncSettings)
		{
			var ser = new BasicServiceProvider(this);

			maria = new Maria();
			tia = new TIA();
			m6532 = new M6532();
			pokey = new Pokey();

			cpu = new MOS6502X<CpuLink>(new CpuLink(this));

			maria = new Maria
			{
				ReadMemory = ReadMemory
			};

			_blip.SetRates(1789773, 44100);

			_syncSettings = syncSettings ?? new A7800SyncSettings();
			_controllerDeck = new A7800HawkControllerDeck(_syncSettings.Port1, _syncSettings.Port2);

			var highscoreBios = comm.CoreFileProvider.GetFirmware(new("A78", "Bios_HSC"), "Some functions may not work without the high score BIOS.");
			var palBios = comm.CoreFileProvider.GetFirmware(new("A78", "Bios_PAL"), "The game will not run if the correct region BIOS is not available.");
			var ntscBios = comm.CoreFileProvider.GetFirmware(new("A78", "Bios_NTSC"), "The game will not run if the correct region BIOS is not available.");

			byte[] header = new byte[128];
			bool is_header = false;

			if (rom.Length % 1024 == 128)
			{
				Console.WriteLine("128 byte header detected");
				byte[] newrom = new byte[rom.Length - 128];
				is_header = true;
				Buffer.BlockCopy(rom, 0, header, 0, 128);
				Buffer.BlockCopy(rom, 128, newrom, 0, newrom.Length);
				rom = newrom;
			}

			_isPAL = false;

			// look up hash in gamedb to see what mapper to use
			// if none found default is zero
			// also check for PAL region
			s_mapper = null;
			var hash_md5 = MD5Checksum.ComputePrefixedHex(rom);

			var gi = Database.CheckDatabase(hash_md5);

			if (gi != null)
			{
				var dict = gi.GetOptions();
				if (dict.ContainsKey("PAL"))
				{
					_isPAL = true;
				}

				if (!dict.TryGetValue("board", out s_mapper)) throw new Exception("No Board selected for this game");

				// check if the game uses pokey or RAM
				if (dict.TryGetValue("RAM", out var cartRAMStr))
				{
					int.TryParse(cartRAMStr, out cart_RAM);
				}

				if (dict.TryGetValue("Pokey", out var pokeyStr))
				{
					bool.TryParse(pokeyStr, out is_pokey);
				}

				if (dict.TryGetValue("Pokey_450", out var pokey450Str))
				{
					bool.TryParse(pokey450Str, out is_pokey_450);
				}

				// some games will not function with the high score bios
				// if such a game is being played, tell the user and disable it
				if (dict.TryGetValue("No_HS", out var noHSStr))
				{
					bool.TryParse(noHSStr, out var no_hs);

					if (no_hs)
					{
						Console.WriteLine("This game is incompatible with the High Score BIOS, disabling it");
						highscoreBios = null;
					}
				}
			}
			else if (is_header)
			{
				Console.WriteLine("ROM not in DB, inferring mapper info from header");

				byte cart_1 = header[0x35];
				byte cart_2 = header[0x36];

				_isPAL = (header[0x39] > 0);

				if (cart_2.Bit(1))
				{
					if (cart_2.Bit(3))
					{
						s_mapper = "2";
					}
					else
					{
						s_mapper = "1";
					}
					
					if (cart_2.Bit(2))
					{
						cart_RAM = 8;

						// the homebrew game serpentine requires extra RAM, but in the alternative style
						if (hash_md5 == RomChecksums.Serpentine)
						{
							cart_RAM = 16;
						}
					}
				}
				else
				{
					s_mapper = "0";
				}

				if (cart_2.Bit(0)) { is_pokey = true; }

				// the homebrew game serpentine requires the pokey chip to be available at the alternative location 0x450
				if (cart_2.Bit(6)) { is_pokey_450 = true; }
			}
			else
			{
				throw new Exception("ROM not in gamedb and has no header");
			}

			// some games that use the Super Game mapper only have 4 banks, so let's set a flag to limit bank size
			if (rom.Length < 0x14000)
			{
				small_flag = true;

				// additionally, PAL Karateka  has bank 6 (actually 2) at 0x4000
				if (hash_md5 == RomChecksums.KaratekaPAL)
				{
					PAL_Kara = true;
				}
			}

			_rom = rom;

			Reset_Mapper(s_mapper);
			
			_hsbios = highscoreBios;
			_bios = _isPAL ? palBios : ntscBios;

			if (_bios == null)
			{
				throw new MissingFirmwareException("The BIOS corresponding to the region of the game you loaded is required to run Atari 7800 games.");
			}

			// set up palette and frame rate
			if (_isPAL)
			{
				_frameHz = 50;
				_screen_width = 320;
				_screen_height = 313;
				_vblanklines = 20;
				maria._palette = PALPalette;
			}
			else
			{
				_frameHz = 60;
				_screen_width = 320;
				_screen_height = 263;
				_vblanklines = 20;
				maria._palette = NTSCPalette;
			}

			maria.Core = this;
			m6532.Core = this;
			tia.Core = this;
			pokey.Core = this;

			ser.Register<IVideoProvider>(this);
			ser.Register<ISoundProvider>(this);
			ServiceProvider = ser;

			_tracer = new TraceBuffer(cpu.TraceHeader);
			ser.Register<ITraceable>(_tracer);
			ser.Register<IStatable>(new StateSerializer(SyncState));
			SetupMemoryDomains();
			ser.Register<IDisassemblable>(cpu);
			HardReset();
		}

		public string BoardName => mapper.GetType().Name.Replace("Mapper", "");

		public DisplayType Region => _isPAL ? DisplayType.PAL : DisplayType.NTSC;

		private readonly A7800HawkControllerDeck _controllerDeck;

		private void HardReset()
		{
			A7800_control_register = 0;

			tia.Reset();
			cpu.Reset();

			maria.Reset();
			m6532.Reset();
			pokey.Reset();
			
			Maria_regs = new byte[0x20];
			RAM = new byte[0x1000];

			cpu_cycle = 0;

			_vidbuffer = new int[VirtualWidth * VirtualHeight];

			master_audio_clock = 0;
			samp_c = samp_l = 0;
		}

		private void ExecFetch(ushort addr)
		{
			if (MemoryCallbacks.HasExecutes)
			{
				uint flags = (uint)(MemoryCallbackFlags.AccessExecute);
				MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "System Bus");
			}
		}

		private void Reset_Mapper(string m)
		{
			if (m == "0")
			{
				mapper = new MapperDefault();
			}
			if (m == "1")
			{
				mapper = new MapperSG();
			}
			if (m == "2")
			{
				mapper = new MapperSGE();
			}
			if (m == "3")
			{
				mapper = new MapperF18();
			}
			if (m == "4")
			{
				mapper = new MapperRampage();
			}
			if (m == "5")
			{
				mapper = new MapperFractalus();
			}
			if (m == "6")
			{
				mapper = new MapperFractalus();
			}

			mapper.Core = this;
			mapper.mask = _rom.Length / 0x4000 - 1;
		}

		/*
		 * MariaTables.cs
		 *
		 * Palette tables for the Maria class.
		 * PAL derived from Dan Boris' 7800/MAME code.
		 *
		 * PAL Table: Copyright © 2004 Mike Murphy
		 *
		 * NTSC Table Source: http://atariage.com/forums/topic/95498-7800-color-palette-in-mess/?p=1174461
		 * 
		 * 
		 */

		public static readonly int[] NTSCPalette =
		{
			0x000000, 0x2e2e2e, 0x3c3c3c, 0x595959,
			0x777777, 0x838383, 0xa0a0a0, 0xb7b7b7,
			0xcdcdcd, 0xd8d8d8, 0xdddddd, 0xe0e0e0,
			0xeaeaea, 0xf0f0f0, 0xf6f6f6, 0xffffff,

			0x412000, 0x542800, 0x763706, 0x984f0f,
			0xbb6818, 0xd78016, 0xff911d, 0xffab1d,
			0xffc51d, 0xffd03b, 0xffd84c, 0xffe651,
			0xfff456, 0xfff977, 0xffff98, 0xffffab,

			0x451904, 0x721e11, 0x9f241e, 0xb33a20,
			0xc85122, 0xe36920, 0xfc811e, 0xff8c25,
			0xff982c, 0xffae38, 0xffc455, 0xffc559,
			0xffc66d, 0xffd587, 0xffe4a1, 0xffe6ab,

			0x5f1f0e, 0x7a240d, 0x9c2c0f, 0xb02f0e,
			0xbf3624, 0xd34e2a, 0xe7623e, 0xf36e4a,
			0xfd7854, 0xff8a6a, 0xff987c, 0xffa48b,
			0xffb39e, 0xffc2b2, 0xffd0c3, 0xffdad0,

			0x4a1704, 0x7e1a0d, 0xb21d17, 0xc82119,
			0xdf251c, 0xec3b38, 0xfa5255, 0xfc6161,
			0xff7063, 0xff7f7e, 0xff8f8f, 0xff9d9e,
			0xffabad, 0xffb9bd, 0xffc7ce, 0xffcade,

			0x490136, 0x66014b, 0x80035f, 0x951874,
			0xaa2d89, 0xba3d99, 0xca4da9, 0xd75ab6,
			0xe467c3, 0xef72ce, 0xfb7eda, 0xff8de1,
			0xff9de5, 0xffa5e7, 0xffafea, 0xffb8ec,

			0x48036c, 0x5c0488, 0x650d91, 0x7b23a7,
			0x933bbf, 0x9d45c9, 0xa74fd3, 0xb25ade,
			0xbd65e9, 0xc56df1, 0xce76fa, 0xd583ff,
			0xda90ff, 0xde9cff, 0xe2a9ff, 0xe6b6ff,

			0x051e81, 0x0626a5, 0x082fca, 0x263dd4,
			0x444cde, 0x4f5aec, 0x5a68ff, 0x6575ff,
			0x7183ff, 0x8091ff, 0x90a0ff, 0x97a9ff,
			0x9fb2ff, 0xafbeff, 0xc0cbff, 0xcdd3ff,

			0x0b0779, 0x201c8e, 0x3531a3, 0x4642b4,
			0x5753c5, 0x615dcf, 0x6d69db, 0x7b77e9,
			0x8985f7, 0x918dff, 0x9c98ff, 0xa7a4ff,
			0xb2afff, 0xbbb8ff, 0xc3c1ff, 0xd3d1ff,

			0x1d295a, 0x1d3876, 0x1d4892, 0x1d5cac,
			0x1d71c6, 0x3286cf, 0x489bd9, 0x4ea8ec,
			0x55b6ff, 0x70c7ff, 0x8cd8ff, 0x93dbff,
			0x9bdfff, 0xafe4ff, 0xc3e9ff, 0xcfedff,

			0x014b59, 0x015d6e, 0x016f84, 0x01849c,
			0x0199b5, 0x01abca, 0x01bcde, 0x01d0f5,
			0x1adcff, 0x3ee1ff, 0x64e7ff, 0x76eaff,
			0x8bedff, 0x9aefff, 0xb1f3ff, 0xc7f6ff,

			0x004800, 0x005400, 0x036b03, 0x0e760e,
			0x188018, 0x279227, 0x36a436, 0x4eb94e,
			0x51cd51, 0x72da72, 0x7ce47c, 0x85ed85,
			0xa2ffa2, 0xb5ffb5, 0xc8ffc8, 0xd0ffd0,

			0x164000, 0x1c5300, 0x236600, 0x287800,
			0x2e8c00, 0x3a980c, 0x47a519, 0x51af23,
			0x5cba2e, 0x71cf43, 0x85e357, 0x8deb5f,
			0x97f569, 0xa4ff97, 0xb9ff97, 0xb9ff97,

			0x2c3500, 0x384400, 0x445200, 0x495600,
			0x607100, 0x6c7f00, 0x798d0a, 0x8b9f1c,
			0x9eb22f, 0xabbf3c, 0xb8cc49, 0xc2d653,
			0xcde153, 0xdbef6c, 0xe8fc79, 0xf2ffab,

			0x463a09, 0x4d3f09, 0x544509, 0x6c5809,
			0x907609, 0xab8b0a, 0xc1a120, 0xd0b02f,
			0xdebe3d, 0xe6c645, 0xedcd4c, 0xf6da65,
			0xfde67d, 0xfff2a2, 0xfff9c5, 0xfff9d4,

			0x401a02, 0x581f05, 0x702408, 0x8d3a13,
			0xab511f, 0xb56427, 0xbf7730, 0xd0853a,
			0xe19344, 0xeda04e, 0xf9ad58, 0xfcb75c,
			0xffc160, 0xffc671, 0xffcb83, 0xffd498
		};

		public static readonly int[] PALPalette =
		{
			0x000000, 0x1c1c1c, 0x393939, 0x595959,  // Grey
			0x797979, 0x929292, 0xababab, 0xbcbcbc,
			0xcdcdcd, 0xd9d9d9, 0xe6e6e6, 0xececec,
			0xf2f2f2, 0xf8f8f8, 0xffffff, 0xffffff,

			0x263001, 0x243803, 0x234005, 0x51541b,  // Orange Green
			0x806931, 0x978135, 0xaf993a, 0xc2a73e,
			0xd5b543, 0xdbc03d, 0xe1cb38, 0xe2d836,
			0xe3e534, 0xeff258, 0xfbff7d, 0xfbff7d,

			0x263001, 0x243803, 0x234005, 0x51541b,  // Orange Green
			0x806931, 0x978135, 0xaf993a, 0xc2a73e,
			0xd5b543, 0xdbc03d, 0xe1cb38, 0xe2d836,
			0xe3e534, 0xeff258, 0xfbff7d, 0xfbff7d,

			0x401a02, 0x581f05, 0x702408, 0x8d3a13,  // Light Orange
			0xab511f, 0xb56427, 0xbf7730, 0xd0853a,
			0xe19344, 0xeda04e, 0xf9ad58, 0xfcb75c,
			0xffc160, 0xffc671, 0xffcb83, 0xffcb83,

			0x391701, 0x5e2304, 0x833008, 0xa54716,  // Gold
			0xc85f24, 0xe37820, 0xff911d, 0xffab1d,
			0xffc51d, 0xffce34, 0xffd84c, 0xffe651,
			0xfff456, 0xfff977, 0xffff98, 0xffff98,

			0x451904, 0x721e11, 0x9f241e, 0xb33a20,  // Orange
			0xc85122, 0xe36920, 0xff811e, 0xff8c25,
			0xff982c, 0xffae38, 0xffc545, 0xffc559,
			0xffc66d, 0xffd587, 0xffe4a1, 0xffe4a1,

			0x4a1704, 0x7e1a0d, 0xb21d17, 0xc82119,  // Red Orange
			0xdf251c, 0xec3b38, 0xfa5255, 0xfc6161,
			0xff706e, 0xff7f7e, 0xff8f8f, 0xff9d9e,
			0xffabad, 0xffb9bd, 0xffc7ce, 0xffc7ce,

			0x050568, 0x3b136d, 0x712272, 0x8b2a8c,  // Pink
			0xa532a6, 0xb938ba, 0xcd3ecf, 0xdb47dd,
			0xea51eb, 0xf45ff5, 0xfe6dff, 0xfe7afd,
			0xff87fb, 0xff95fd, 0xffa4ff, 0xffa4ff,

			0x280479, 0x400984, 0x590f90, 0x70249d,  // Purple
			0x8839aa, 0xa441c3, 0xc04adc, 0xd054ed,
			0xe05eff, 0xe96dff, 0xf27cff, 0xf88aff,
			0xff98ff, 0xfea1ff, 0xfeabff, 0xfeabff,

			0x051e81, 0x0626a5, 0x082fca, 0x263dd4,  // Blue1
			0x444cde, 0x4f5aee, 0x5a68ff, 0x6575ff,
			0x7183ff, 0x8091ff, 0x90a0ff, 0x97a9ff,
			0x9fb2ff, 0xafbeff, 0xc0cbff, 0xc0cbff,

			0x0c048b, 0x2218a0, 0x382db5, 0x483ec7,  // Blue2
			0x584fda, 0x6159ec, 0x6b64ff, 0x7a74ff,
			0x8a84ff, 0x918eff, 0x9998ff, 0xa5a3ff,
			0xb1aeff, 0xb8b8ff, 0xc0c2ff, 0xc0c2ff,

			0x1d295a, 0x1d3876, 0x1d4892, 0x1c5cac,  // Light Blue
			0x1c71c6, 0x3286cf, 0x489bd9, 0x4ea8ec,
			0x55b6ff, 0x70c7ff, 0x8cd8ff, 0x93dbff,
			0x9bdfff, 0xafe4ff, 0xc3e9ff, 0xc3e9ff,

			0x2f4302, 0x395202, 0x446103, 0x417a12,  // Turquoise
			0x3e9421, 0x4a9f2e, 0x57ab3b, 0x5cbd55,
			0x61d070, 0x69e27a, 0x72f584, 0x7cfa8d,
			0x87ff97, 0x9affa6, 0xadffb6, 0xadffb6,

			0x0a4108, 0x0d540a, 0x10680d, 0x137d0f,  // Green Blue
			0x169212, 0x19a514, 0x1cb917, 0x1ec919,
			0x21d91b, 0x47e42d, 0x6ef040, 0x78f74d,
			0x83ff5b, 0x9aff7a, 0xb2ff9a, 0xb2ff9a,

			0x04410b, 0x05530e, 0x066611, 0x077714,  // Green
			0x088817, 0x099b1a, 0x0baf1d, 0x48c41f,
			0x86d922, 0x8fe924, 0x99f927, 0xa8fc41,
			0xb7ff5b, 0xc9ff6e, 0xdcff81, 0xdcff81,

			0x02350f, 0x073f15, 0x0c4a1c, 0x2d5f1e,  // Yellow Green
			0x4f7420, 0x598324, 0x649228, 0x82a12e,
			0xa1b034, 0xa9c13a, 0xb2d241, 0xc4d945,
			0xd6e149, 0xe4f04e, 0xf2ff53, 0xf2ff53
		};
	}
}
