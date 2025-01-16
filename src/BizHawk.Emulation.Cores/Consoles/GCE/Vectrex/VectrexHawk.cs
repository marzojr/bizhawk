using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.MC6809;

namespace BizHawk.Emulation.Cores.Consoles.Vectrex
{
	[Core(
		name: CoreNames.VectrexHawk,
		author: "alyosha")]
	public partial class VectrexHawk : IEmulator, ISaveRam, IDebuggable, IInputPollable, IRegionable, 
	ISettable<object, VectrexHawk.VectrexSyncSettings>
	{
		internal static class RomChecksums
		{
			public const string Minestorm = "SHA1:65D07426B520DDD3115D40F255511E0FD2E20AE7";
		}

		public byte[] RAM = new byte[0x400];

		public byte[] _bios, minestorm;
		public readonly byte[] _rom;	

		private int _frame = 0;

		public MapperBase mapper;

		private readonly ITraceable _tracer;

		public MC6809 cpu;
		public PPU ppu;
		public Audio audio;
		public SerialPort serialport;

		[CoreConstructor(VSystemID.Raw.VEC)]
		public VectrexHawk(CoreComm comm, byte[] rom, VectrexHawk.VectrexSyncSettings syncSettings)
		{
			var ser = new BasicServiceProvider(this);

			cpu = new MC6809
			{
				ReadMemory = ReadMemory,
				WriteMemory = WriteMemory,
				PeekMemory = PeekMemory,
				DummyReadMemory = ReadMemory,
				OnExecFetch = ExecFetch,
			};

			audio = new Audio();
			ppu = new PPU();
			serialport = new SerialPort();

			_settings = new object(); // TODO: wtf is this
			_syncSettings = syncSettings ?? new VectrexSyncSettings();
			_controllerDeck = new VectrexHawkControllerDeck(_syncSettings.Port1, _syncSettings.Port2);

			/*var Bios =*/ _bios = comm.CoreFileProvider.GetFirmwareOrThrow(new("VEC", "Bios"), "BIOS Not Found, Cannot Load");
			/*var Mine =*/ minestorm = comm.CoreFileProvider.GetFirmwareOrThrow(new("VEC", "Minestorm"), "Minestorm Not Found, Cannot Load");

			var romHashSHA1 = SHA1Checksum.ComputePrefixedHex(rom);
			Console.WriteLine(romHashSHA1);

			_rom = rom;

			// If the game is minestorm, then no cartridge is inserted, retun 0xFF
			if (romHashSHA1 == RomChecksums.Minestorm)
			{
				_rom  = new byte[0x8000];

				for (int i = 0; i < 0x8000; i++)
				{
					_rom[i] = 0xFF;
				}
			}

			// mirror games that are too small
			if (_rom.Length < 0x8000)
			{
				_rom = new byte[0x8000];

				for (int i = 0; i < 0x8000 / rom.Length; i++)
				{
					for (int j = 0; j < rom.Length; j++)
					{
						_rom[j + i * rom.Length] = rom[j];
					}
				}
			}

			// RAM appears to power up to either random values or 0xFF, otherwise all the asteroids in minestorm are on the same side if RAM[0x7E]=0
			for (int i = 0; i < RAM.Length; i++)
			{
				RAM[i] = 0xFF;
			}

			Setup_Mapper();

			_frameHz = 50;

			audio.Core = this;
			ppu.Core = this;
			serialport.Core = this;

			ser.Register<IVideoProvider>(this);
			ser.Register<ISoundProvider>(audio);
			ServiceProvider = ser;

			_settings = new object(); // TODO: wtf is this
			_syncSettings = syncSettings ?? new VectrexSyncSettings();

			_tracer = new TraceBuffer(cpu.TraceHeader);
			ser.Register<ITraceable>(_tracer);
			ser.Register<IStatable>(new StateSerializer(SyncState));
			SetupMemoryDomains();
			HardReset();

			cpu.SetCallbacks(ReadMemory, PeekMemory, PeekMemory, WriteMemory);
		}

		public DisplayType Region => DisplayType.NTSC;

		private readonly VectrexHawkControllerDeck _controllerDeck;

		public void HardReset()
		{
			Register_Reset();
			ppu.Reset();
			audio.Reset();
			serialport.Reset();
			cpu.Reset();

			RAM = new byte[0x400];

			_vidbuffer = new int[VirtualWidth * VirtualHeight];
			_framebuffer = new int[VirtualWidth * VirtualHeight];
		}

		public void SoftReset()
		{
			Register_Reset();
			ppu.Reset();
			audio.Reset();
			serialport.Reset();
			cpu.Reset();

			_vidbuffer = new int[VirtualWidth * VirtualHeight];
			_framebuffer = new int[VirtualWidth * VirtualHeight];
		}

		// TODO: move this inside the cpu to avoid a non-inlinable function call
		private void ExecFetch(ushort addr)
		{
			if (MemoryCallbacks.HasExecutes)
			{
				uint flags = (uint)MemoryCallbackFlags.AccessExecute;
				MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "System Bus");
			}
		}

		private void Setup_Mapper()
		{
			if (_rom.Length == 0x10000)
			{
				mapper = new Mapper_64K();
			}
			else
			{
				mapper = new MapperDefault();
			}
			
			mapper.Core = this;
			mapper.Initialize();
		}
	}
}
