using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Sega.MasterSystem;

namespace BizHawk.Emulation.Cores.Sega.GGHawkLink
{
	[Core(
		name: CoreNames.GGHawkLink,
		author: "Vecna",
		isReleased: false)]
	public partial class GGHawkLink : IEmulator, ISaveRam, IDebuggable, IStatable, IInputPollable, IRegionable, ILinkable,
		ISettable<GGHawkLink.GGLinkSettings, GGHawkLink.GGLinkSyncSettings>
	{
		// we want to create two GG instances that we will run concurrently
		public SMS L;
		public SMS R;

		// if true, the link cable is currently connected
		private bool _cableconnected = true;

		// if true, the link cable toggle signal is currently asserted
		private bool _cablediscosignal = false;

		private bool do_r_next = false;

		[CoreConstructor(VSystemID.Raw.GGL)]
		public GGHawkLink(CoreLoadParameters<GGLinkSettings, GGLinkSyncSettings> lp)
		{
			if (lp.Roms.Count != 2)
				throw new InvalidOperationException("Wrong number of roms");

			var ser = new BasicServiceProvider(this);

			linkSettings = lp.Settings ?? new GGLinkSettings();
			linkSyncSettings = lp.SyncSettings ?? new GGLinkSyncSettings();
			_controllerDeck = new GGHawkLinkControllerDeck(GGHawkLinkControllerDeck.DefaultControllerName, GGHawkLinkControllerDeck.DefaultControllerName);

			var temp_set_L = new SMS.SmsSettings();
			var temp_set_R = new SMS.SmsSettings();

			var temp_sync_L = new SMS.SmsSyncSettings();
			var temp_sync_R = new SMS.SmsSyncSettings();

			L = new SMS(lp.Comm, lp.Roms[0].Game, lp.Roms[0].RomData, temp_set_L, temp_sync_L);
			R = new SMS(lp.Comm, lp.Roms[1].Game, lp.Roms[1].RomData, temp_set_R, temp_sync_R);

			ser.Register<ICodeDataLogger>(L);
			ser.Register<IVideoProvider>(this);
			ser.Register<ISoundProvider>(this); 

			_tracer = new TraceBuffer(L.Cpu.TraceHeader);
			ser.Register(_tracer);

			ServiceProvider = ser;

			SetupMemoryDomains();

			HardReset();

			L.stand_alone = false;
			R.stand_alone = false;

			_lStates = L.ServiceProvider.GetService<IStatable>();
			_rStates = R.ServiceProvider.GetService<IStatable>();
		}

		public void HardReset()
		{
			L.HardReset();
			R.HardReset();
		}

		public DisplayType Region => DisplayType.NTSC;

		public int _frame = 0;

		private readonly GGHawkLinkControllerDeck _controllerDeck;

		private readonly ITraceable _tracer;

		public bool LinkConnected
		{
			get => _cableconnected;
			set => _cableconnected = value;
		}

		private void ExecFetch(ushort addr)
		{
			uint flags = (uint)MemoryCallbackFlags.AccessExecute;
			MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "System Bus");
		}
	}
}
