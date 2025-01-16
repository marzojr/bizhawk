using System.Collections.Generic;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Nintendo.Gameboy;

namespace BizHawk.Emulation.Cores.Nintendo.SubGBHawk
{
	[Core(
		name: CoreNames.SubGbHawk,
		author: "alyosha and BizHawk contributors")]
	public partial class SubGBHawk : IEmulator, IStatable, IInputPollable,
		ISettable<GBHawk.GBHawk.GBSettings, GBHawk.GBHawk.GBSyncSettings>, IDebuggable, ICycleTiming, IGameboyCommon
	{
		[CoreConstructor(VSystemID.Raw.GB, Priority = CorePriority.SuperLow)]
		[CoreConstructor(VSystemID.Raw.GBC, Priority = CorePriority.SuperLow)]
		public SubGBHawk(CoreComm comm, GameInfo game, byte[] rom, /*string gameDbFn,*/ GBHawk.GBHawk.GBSettings settings, GBHawk.GBHawk.GBSyncSettings syncSettings)
		{
			
			var subGBSettings = settings ?? new GBHawk.GBHawk.GBSettings();
			var subGBSyncSettings = syncSettings ?? new GBHawk.GBHawk.GBSyncSettings();

			_GBCore = new GBHawk.GBHawk(comm, game, rom, subGBSettings, subGBSyncSettings, true);

			HardReset();
			current_cycle = 0;
			_cycleCount = 0;

			_GBStatable = _GBCore.ServiceProvider.GetService<IStatable>();

			var ser = new BasicServiceProvider(this);
			ServiceProvider = ser;

			ser.Register(_GBCore.ServiceProvider.GetService<IVideoProvider>());
			ser.Register(_GBCore.ServiceProvider.GetService<ISoundProvider>());
			ser.Register(_GBCore.ServiceProvider.GetService<ITraceable>());
			ser.Register(_GBCore.ServiceProvider.GetService<IMemoryDomains>());
			ser.Register(_GBCore.ServiceProvider.GetService<ISaveRam>());
			ser.Register(_GBCore.ServiceProvider.GetService<IRegionable>());
			ser.Register(_GBCore.ServiceProvider.GetService<ICodeDataLogger>());

			_tracer = new TraceBuffer(_GBCore.cpu.TraceHeader);
			ser.Register(_tracer);
		}

		public GBHawk.GBHawk _GBCore;

		// needed for movies to accurately calculate timing
		private long _cycleCount;

		public long CycleCount => _cycleCount;

		public double ClockRate => 4194304;

		public void HardReset() => _GBCore.HardReset();

		private int _frame;

		private readonly ITraceable _tracer;

		public GBHawk.GBHawk.GBSettings GetSettings() => _GBCore.GetSettings();
		public GBHawk.GBHawk.GBSyncSettings GetSyncSettings() => _GBCore.GetSyncSettings();
		public PutSettingsDirtyBits PutSettings(GBHawk.GBHawk.GBSettings o) => _GBCore.PutSettings(o);
		public PutSettingsDirtyBits PutSyncSettings(GBHawk.GBHawk.GBSyncSettings o) => _GBCore.PutSyncSettings(o);


		// IDebuggable, declare here so TotalexecutedCycles can reflect the cycle count of the movie
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
			=> _GBCore.cpu.GetCpuFlagsAndRegisters();

		public void SetCpuRegister(string register, int value)
			=> _GBCore.cpu.SetCpuRegister(register, value);

		public IMemoryCallbackSystem MemoryCallbacks => _GBCore.MemoryCallbacks;

		public bool CanStep(StepType type) => false;

		[FeatureNotImplemented]
		public void Step(StepType type) => throw new NotImplementedException();

		public long TotalExecutedCycles => _cycleCount;

		public bool IsCGBMode => _GBCore.IsCGBMode;
		public bool IsCGBDMGMode => _GBCore.IsCGBDMGMode;
		public IGPUMemoryAreas LockGPU() => _GBCore.LockGPU();
		public void SetScanlineCallback(ScanlineCallback callback, int line)
			=> _GBCore.SetScanlineCallback(callback, line);
		public void SetPrinterCallback(PrinterCallback callback)
			=> _GBCore.SetPrinterCallback(callback);
	}
}
