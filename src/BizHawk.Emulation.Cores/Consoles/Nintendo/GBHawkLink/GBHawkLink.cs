using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.Nintendo.GBHawk;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawkLink
{
	[Core(
		name: CoreNames.GBHawkLink,
		author: "alyosha and BizHawk contributors")]
	public partial class GBHawkLink : IEmulator, ISaveRam, IDebuggable, IStatable, IInputPollable, IRegionable, ILinkable,
		ISettable<GBHawkLink.GBLinkSettings, GBHawkLink.GBLinkSyncSettings>,
		ILinkedGameBoyCommon
	{
		// we want to create two GBHawk instances that we will run concurrently
		// maybe up to 4 eventually?
		public GBHawk.GBHawk L;
		public GBHawk.GBHawk R;

		public IGameboyCommon First
			=> L;

		// if true, the link cable is currently connected
		private bool _cableconnected = true;

		// if true, the link cable toggle signal is currently asserted
		private bool _cablediscosignal = false;

		private bool do_r_next = false;

		public byte L_controller, R_controller;

		public bool do_frame_fill;

		[CoreConstructor(VSystemID.Raw.GBL)]
		public GBHawkLink(CoreLoadParameters<GBHawkLink.GBLinkSettings, GBHawkLink.GBLinkSyncSettings> lp)
		{
			if (lp.Roms.Count != 2)
				throw new InvalidOperationException("Wrong number of roms");

			var ser = new BasicServiceProvider(this);
			ServiceProvider = ser;

			linkSettings = lp.Settings ?? new GBLinkSettings();
			linkSyncSettings = lp.SyncSettings ?? new GBLinkSyncSettings();
			_controllerDeck = new(
				GBHawkControllerDeck.DefaultControllerName,
				GBHawkControllerDeck.DefaultControllerName);

			var temp_set_L = new GBHawk.GBHawk.GBSettings();
			var temp_set_R = new GBHawk.GBHawk.GBSettings();

			var temp_sync_L = new GBHawk.GBHawk.GBSyncSettings();
			var temp_sync_R = new GBHawk.GBHawk.GBSyncSettings();

			temp_sync_L.ConsoleMode = linkSyncSettings.ConsoleMode_L;
			temp_sync_R.ConsoleMode = linkSyncSettings.ConsoleMode_R;

			temp_sync_L.GBACGB = linkSyncSettings.GBACGB;
			temp_sync_R.GBACGB = linkSyncSettings.GBACGB;

			temp_sync_L.RTCInitialTime = linkSyncSettings.RTCInitialTime_L;
			temp_sync_R.RTCInitialTime = linkSyncSettings.RTCInitialTime_R;
			temp_sync_L.RTCOffset = linkSyncSettings.RTCOffset_L;
			temp_sync_R.RTCOffset = linkSyncSettings.RTCOffset_R;

			L = new GBHawk.GBHawk(lp.Comm, lp.Roms[0].Game, lp.Roms[0].RomData, temp_set_L, temp_sync_L);
			R = new GBHawk.GBHawk(lp.Comm, lp.Roms[1].Game, lp.Roms[1].RomData, temp_set_R, temp_sync_R);

			ser.Register<IVideoProvider>(this);
			ser.Register<ISoundProvider>(this); 

			_tracer = new TraceBuffer(L.cpu.TraceHeader);
			ser.Register<ITraceable>(_tracer);

			_lStates = L.ServiceProvider.GetService<IStatable>();
			_rStates = R.ServiceProvider.GetService<IStatable>();

			SetupMemoryDomains();

			HardReset();
		}

		public void HardReset()
		{
			L.HardReset();
			R.HardReset();
		}

		public DisplayType Region => DisplayType.NTSC;

		public int _frame = 0;

		private readonly GBHawkLinkControllerDeck _controllerDeck;

		private readonly ITraceable _tracer;

		public bool LinkConnected
		{
			get => _cableconnected;
			set => _cableconnected = value;
		}

		private void ExecFetch(ushort addr)
		{
			uint flags = (uint)(MemoryCallbackFlags.AccessExecute);
			MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "System Bus");
		}
	}
}
