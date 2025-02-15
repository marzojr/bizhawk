using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.Nintendo.GBHawk;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawkLink4x
{
	[Core(
		name: CoreNames.GBHawkLink4x,
		author: "alyosha and BizHawk contributors")]
	public partial class GBHawkLink4x : IEmulator, ISaveRam, IDebuggable, IStatable, IInputPollable, IRegionable,
		ISettable<GBHawkLink4x.GBLink4xSettings, GBHawkLink4x.GBLink4xSyncSettings>,
		ILinkedGameBoyCommon
	{
		// we want to create two GBHawk instances that we will run concurrently
		public GBHawk.GBHawk A;
		public GBHawk.GBHawk B;
		public GBHawk.GBHawk C;
		public GBHawk.GBHawk D;

		public IGameboyCommon First
			=> A;

		// if true, the link cable is currently connected
		private bool _cableconnected_LR = false;
		private bool _cableconnected_UD = false;
		private bool _cableconnected_X = false;
		private bool _cableconnected_4x = true;

		private bool do_2_next_1 = false;
		private bool do_2_next_2 = false;

		// 4 player adapter variables
		public bool is_pinging, is_transmitting;
		public byte status_byte;
		public int x4_clock;
		public int ping_player;
		public int ping_byte;
		public int bit_count;
		public byte received_byte;
		public int begin_transmitting_cnt;
		public int transmit_speed;
		public int num_bytes_transmit;
		public bool time_out_check;
		public bool ready_to_transmit;
		public int transmit_byte;
		public byte[] x4_buffer = new byte[0x400 * 2];
		public bool buffer_parity;
		public bool pre_transmit;
		public byte temp1_rec, temp2_rec, temp3_rec, temp4_rec;

		public byte A_controller, B_controller, C_controller, D_controller;

		public bool do_frame_fill;

		[CoreConstructor(VSystemID.Raw.GBL)]
		public GBHawkLink4x(CoreLoadParameters<GBLink4xSettings, GBLink4xSyncSettings> lp)
		{
			if (lp.Roms.Count != 4)
				throw new InvalidOperationException("Wrong number of roms");

			var ser = new BasicServiceProvider(this);

			Link4xSettings = lp.Settings ?? new GBLink4xSettings();
			Link4xSyncSettings = lp.SyncSettings ?? new GBLink4xSyncSettings();
			_controllerDeck = new(
				GBHawkControllerDeck.DefaultControllerName,
				GBHawkControllerDeck.DefaultControllerName,
				GBHawkControllerDeck.DefaultControllerName,
				GBHawkControllerDeck.DefaultControllerName);

			var tempSetA = new GBHawk.GBHawk.GBSettings();
			var tempSetB = new GBHawk.GBHawk.GBSettings();
			var tempSetC = new GBHawk.GBHawk.GBSettings();
			var tempSetD = new GBHawk.GBHawk.GBSettings();

			var tempSyncA = new GBHawk.GBHawk.GBSyncSettings();
			var tempSyncB = new GBHawk.GBHawk.GBSyncSettings();
			var tempSyncC = new GBHawk.GBHawk.GBSyncSettings();
			var tempSyncD = new GBHawk.GBHawk.GBSyncSettings();

			tempSyncA.ConsoleMode = Link4xSyncSettings.ConsoleMode_A;
			tempSyncB.ConsoleMode = Link4xSyncSettings.ConsoleMode_B;
			tempSyncC.ConsoleMode = Link4xSyncSettings.ConsoleMode_C;
			tempSyncD.ConsoleMode = Link4xSyncSettings.ConsoleMode_D;

			tempSyncA.GBACGB = Link4xSyncSettings.GBACGB;
			tempSyncB.GBACGB = Link4xSyncSettings.GBACGB;
			tempSyncC.GBACGB = Link4xSyncSettings.GBACGB;
			tempSyncD.GBACGB = Link4xSyncSettings.GBACGB;

			tempSyncA.RTCInitialTime = Link4xSyncSettings.RTCInitialTime_A;
			tempSyncB.RTCInitialTime = Link4xSyncSettings.RTCInitialTime_B;
			tempSyncC.RTCInitialTime = Link4xSyncSettings.RTCInitialTime_C;
			tempSyncD.RTCInitialTime = Link4xSyncSettings.RTCInitialTime_D;
			tempSyncA.RTCOffset = Link4xSyncSettings.RTCOffset_A;
			tempSyncB.RTCOffset = Link4xSyncSettings.RTCOffset_B;
			tempSyncC.RTCOffset = Link4xSyncSettings.RTCOffset_C;
			tempSyncD.RTCOffset = Link4xSyncSettings.RTCOffset_D;

			A = new GBHawk.GBHawk(lp.Comm, lp.Roms[0].Game, lp.Roms[0].RomData, tempSetA, tempSyncA);
			B = new GBHawk.GBHawk(lp.Comm, lp.Roms[1].Game, lp.Roms[1].RomData, tempSetB, tempSyncB);
			C = new GBHawk.GBHawk(lp.Comm, lp.Roms[2].Game, lp.Roms[2].RomData, tempSetC, tempSyncC);
			D = new GBHawk.GBHawk(lp.Comm, lp.Roms[3].Game, lp.Roms[3].RomData, tempSetD, tempSyncD);

			ser.Register<IVideoProvider>(this);
			ser.Register<ISoundProvider>(this); 

			_tracer = new TraceBuffer(A.cpu.TraceHeader);
			ser.Register(_tracer);

			ServiceProvider = ser;

			_aStates = A.ServiceProvider.GetService<IStatable>();
			_bStates = B.ServiceProvider.GetService<IStatable>();
			_cStates = C.ServiceProvider.GetService<IStatable>();
			_dStates = D.ServiceProvider.GetService<IStatable>();

			SetupMemoryDomains();

			HardReset();
		}

		public void HardReset()
		{
			A.HardReset();
			B.HardReset();
			C.HardReset();
			D.HardReset();

			ping_player = 1;
			ping_byte = 0;
			bit_count = 7;
			received_byte = 0;
			begin_transmitting_cnt = 0;
			status_byte = 1;
			x4_clock = 64;
		}

		public DisplayType Region => DisplayType.NTSC;

		public int _frame = 0;

		private readonly GBHawkLink4xControllerDeck _controllerDeck;

		private readonly ITraceable _tracer;
	}
}
