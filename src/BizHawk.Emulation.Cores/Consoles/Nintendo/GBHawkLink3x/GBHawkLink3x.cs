using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.Nintendo.GBHawk;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawkLink3x
{
	[Core(
		name: CoreNames.GBHawkLink3x,
		author: "alyosha and BizHawk contributors")]
	public partial class GBHawkLink3x : IEmulator, ISaveRam, IDebuggable, IStatable, IInputPollable, IRegionable,
		ISettable<GBHawkLink3x.GBLink3xSettings, GBHawkLink3x.GBLink3xSyncSettings>,
		ILinkedGameBoyCommon
	{
		// we want to create two GBHawk instances that we will run concurrently
		// maybe up to 4 eventually?
		public GBHawk.GBHawk L;
		public GBHawk.GBHawk C;
		public GBHawk.GBHawk R;

		public IGameboyCommon First
			=> L;

		// if true, the link cable is currently connected
		private bool _cableconnected_LC = false;
		private bool _cableconnected_CR = false;
		private bool _cableconnected_RL = false;

		private bool do_2_next = false;

		public byte L_controller, C_controller, R_controller;

		public bool do_frame_fill;

		[CoreConstructor(VSystemID.Raw.GBL)]
		public GBHawkLink3x(CoreLoadParameters<GBLink3xSettings, GBLink3xSyncSettings> lp)
		{
			if (lp.Roms.Count != 3)
				throw new InvalidOperationException("Wrong number of roms");

			var ser = new BasicServiceProvider(this);
			ServiceProvider = ser;

			Link3xSettings = lp.Settings ?? new GBLink3xSettings();
			Link3xSyncSettings = lp.SyncSettings ?? new GBLink3xSyncSettings();
			_controllerDeck = new(
				GBHawkControllerDeck.DefaultControllerName,
				GBHawkControllerDeck.DefaultControllerName,
				GBHawkControllerDeck.DefaultControllerName);

			var tempSetL = new GBHawk.GBHawk.GBSettings();
			var tempSetC = new GBHawk.GBHawk.GBSettings();
			var tempSetR = new GBHawk.GBHawk.GBSettings();

			var tempSyncL = new GBHawk.GBHawk.GBSyncSettings();
			var tempSyncC = new GBHawk.GBHawk.GBSyncSettings();
			var tempSyncR = new GBHawk.GBHawk.GBSyncSettings();

			tempSyncL.ConsoleMode = Link3xSyncSettings.ConsoleMode_L;
			tempSyncC.ConsoleMode = Link3xSyncSettings.ConsoleMode_C;
			tempSyncR.ConsoleMode = Link3xSyncSettings.ConsoleMode_R;

			tempSyncL.GBACGB = Link3xSyncSettings.GBACGB;
			tempSyncC.GBACGB = Link3xSyncSettings.GBACGB;
			tempSyncR.GBACGB = Link3xSyncSettings.GBACGB;

			tempSyncL.RTCInitialTime = Link3xSyncSettings.RTCInitialTime_L;
			tempSyncC.RTCInitialTime = Link3xSyncSettings.RTCInitialTime_C;
			tempSyncR.RTCInitialTime = Link3xSyncSettings.RTCInitialTime_R;
			tempSyncL.RTCOffset = Link3xSyncSettings.RTCOffset_L;
			tempSyncC.RTCOffset = Link3xSyncSettings.RTCOffset_C;
			tempSyncR.RTCOffset = Link3xSyncSettings.RTCOffset_R;

			L = new GBHawk.GBHawk(lp.Comm, lp.Roms[0].Game, lp.Roms[0].RomData, tempSetL, tempSyncL);
			C = new GBHawk.GBHawk(lp.Comm, lp.Roms[1].Game, lp.Roms[1].RomData, tempSetC, tempSyncC);
			R = new GBHawk.GBHawk(lp.Comm, lp.Roms[2].Game, lp.Roms[2].RomData, tempSetR, tempSyncR);

			ser.Register<IVideoProvider>(this);
			ser.Register<ISoundProvider>(this); 

			_tracer = new TraceBuffer(L.cpu.TraceHeader);
			ser.Register(_tracer);

			_lStates = L.ServiceProvider.GetService<IStatable>();
			_cStates = C.ServiceProvider.GetService<IStatable>();
			_rStates = R.ServiceProvider.GetService<IStatable>();

			SetupMemoryDomains();
			HardReset();
		}

		public void HardReset()
		{
			L.HardReset();
			C.HardReset();
			R.HardReset();
		}

		public DisplayType Region => DisplayType.NTSC;

		public int _frame = 0;

		private readonly GBHawkLink3xControllerDeck _controllerDeck;

		private readonly ITraceable _tracer;
	}
}
