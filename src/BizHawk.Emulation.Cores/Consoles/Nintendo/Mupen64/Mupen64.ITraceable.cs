using BizHawk.Emulation.Common;
using static BizHawk.Emulation.Cores.Consoles.Nintendo.Mupen64.Mupen64Api.m64p_dbg_runstate;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Mupen64;

public partial class Mupen64 : ITraceable
{
	public string Header => "R3400: PC, mnemonic, operands";

	private readonly Mupen64Api.dbg_frontend_init _debuggerInitCallback;
	private readonly Mupen64Api.dbg_frontend_update _traceCallback;

	private void DebuggerInitCallback() => Mupen64Api.DebugSetRunState(RUNNING);

	private void TraceCallback(uint pc)
	{
		string disassembly = this.Disassemble(_memoryDomains.SystemBus, pc, out _);
		string registerInfo = ""; // TODO
		_sink.Put(new TraceInfo(disassembly, registerInfo));
	}

	private ITraceSink _sink;
	public ITraceSink Sink
	{
		get => _sink;
		set
		{
			Mupen64Api.DebugSetRunState(value is null ? RUNNING : STEPPING);
			_sink = value;
		}
	}
}
