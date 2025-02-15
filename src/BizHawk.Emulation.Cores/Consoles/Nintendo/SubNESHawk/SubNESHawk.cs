using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.NES;

namespace BizHawk.Emulation.Cores.Nintendo.SubNESHawk
{
	[Core(
		name: CoreNames.SubNesHawk,
		author: "zeromus, natt, alyosha, adelikat")]
	public partial class SubNESHawk : IEmulator, IStatable, IInputPollable, ISettable<NES.NES.NESSettings, NES.NES.NESSyncSettings>
	{
		[CoreConstructor(VSystemID.Raw.NES, Priority = CorePriority.SuperLow)]
		public SubNESHawk(CoreComm comm, GameInfo game, byte[] rom, /*string gameDbFn,*/ NES.NES.NESSettings settings, NES.NES.NESSyncSettings syncSettings)
		{
			var subNesSettings = settings ?? new NES.NES.NESSettings();
			var subNesSyncSettings = syncSettings ?? new NES.NES.NESSyncSettings();

			_nesCore = new NES.NES(comm, game, rom, subNesSettings, subNesSyncSettings, true);

			HardReset();
			current_cycle = 0;
			_nesCore.cpu.ext_ppu_cycle = current_cycle;

			_nesStatable = _nesCore.ServiceProvider.GetService<IStatable>();

			var ser = new BasicServiceProvider(this);
			ServiceProvider = ser;

			ser.Register(_nesCore.ServiceProvider.GetService<IVideoProvider>());
			ser.Register(_nesCore.ServiceProvider.GetService<ISoundProvider>());
			ser.Register(_nesCore.ServiceProvider.GetService<ITraceable>());
			ser.Register(_nesCore.ServiceProvider.GetService<IDisassemblable>());
			ser.Register(_nesCore.ServiceProvider.GetService<IMemoryDomains>());
			ser.Register(_nesCore.ServiceProvider.GetService<INESPPUViewable>());
			ser.Register(_nesCore.ServiceProvider.GetService<IBoardInfo>());
			ser.Register(_nesCore.ServiceProvider.GetService<ISaveRam>());
			ser.Register(_nesCore.ServiceProvider.GetService<IDebuggable>());
			ser.Register(_nesCore.ServiceProvider.GetService<IRegionable>());
			ser.Register(_nesCore.ServiceProvider.GetService<ICodeDataLogger>());
			ser.Register(_nesCore.ServiceProvider.GetService<IDriveLight>());
			ser.Register(_nesCore.ServiceProvider.GetService<ICycleTiming>());

			const string TRACE_HEADER = "6502: PC, machine code, mnemonic, operands, registers (A, X, Y, P, SP), flags (NVTBDIZCR), CPU Cycle, PPU Cycle";
			_tracer = new TraceBuffer(TRACE_HEADER);
			ser.Register(_tracer);

			
			var barCodeService = _nesCore.ServiceProvider.GetService<DatachBarcode>();
			if (barCodeService != null)
			{
				ser.Register(barCodeService);
			}
		}

		private readonly NES.NES _nesCore;

		public void HardReset() => _nesCore.HardReset();

		private void SoftReset()
		{
			_nesCore.Board.NesSoftReset();
			_nesCore.cpu.NESSoftReset();
			_nesCore.apu.NESSoftReset();
			_nesCore.ppu.NESSoftReset();
			current_cycle = 0;
			_nesCore.cpu.ext_ppu_cycle = current_cycle;
		}

		private int _frame;

		public bool IsFds => _nesCore.IsFDS;

		public bool IsVs => _nesCore.IsVS;

		private readonly ITraceable _tracer;
		public bool HasMapperProperties => _nesCore.HasMapperProperties;

		public NES.NES.NESSettings GetSettings() => _nesCore.GetSettings();
		public NES.NES.NESSyncSettings GetSyncSettings() => _nesCore.GetSyncSettings();
		public PutSettingsDirtyBits PutSettings(NES.NES.NESSettings o) => _nesCore.PutSettings(o);
		public PutSettingsDirtyBits PutSyncSettings(NES.NES.NESSyncSettings o) => _nesCore.PutSyncSettings(o);
	}
}
