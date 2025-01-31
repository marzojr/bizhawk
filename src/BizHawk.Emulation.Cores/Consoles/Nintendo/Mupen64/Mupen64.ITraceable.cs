using System.Text;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using static BizHawk.Emulation.Cores.Consoles.Nintendo.Mupen64.Mupen64Api.m64p_dbg_runstate;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Mupen64;

public partial class Mupen64 : ITraceable
{
	public string Header => "R3400: PC, mnemonic, operands";

	private readonly Mupen64Api.dbg_frontend_init _debuggerInitCallback;
	private readonly Mupen64Api.dbg_frontend_update _traceCallback;
	private ITraceSink _sink;

	private void DebuggerInitCallback() => Mupen64Api.DebugSetRunState(RUNNING);

	private void TraceCallback(uint pc)
	{
		if (Sink is not null)
		{
			string disassembly = this.Disassemble(_memoryDomains.SystemBus, pc, out _);
			var registerInfo = GetCpuFlagsAndRegisters();
			StringBuilder registerStringBuilder = new();
			registerStringBuilder.Append($"PC:{registerInfo["PC"].Value.ToString($"X{registerInfo["PC"].BitSize / 4}")}");
			foreach (var (registerName, registerValue) in registerInfo)
			{
				if (registerName.Contains("REG"))
				{
					registerStringBuilder.Append($" {registerName}:{registerValue.Value.ToString($"X{registerValue.BitSize / 4}")}");
				}

			}

			Sink.Put(new TraceInfo(disassembly, registerStringBuilder.ToString()));
		}
		else
		{
			Mupen64Api.DebugBreakpointTriggeredBy(out var flags, out uint accessed);
			uint address = flags.HasFlag(Mupen64Api.m64p_dbg_bkp_flags.EXEC) ? pc : accessed;
			uint value = flags.HasFlag(Mupen64Api.m64p_dbg_bkp_flags.WRITE) ? 0 : Mupen64Api.DebugMemRead32(address);
			MemoryCallbacks.CallMemoryCallbacks(address, value, (uint) flags >> 1 << 12, "System Bus");
			Mupen64Api.DebugSetRunState(RUNNING); // breakpoint hits set the debugger run state to PAUSED
		}
	}

	public ITraceSink Sink
	{
		get => _sink;
		set
		{
			_sink = value;
			Mupen64Api.DebugSetRunState(_sink is null ? RUNNING : STEPPING);
		}
	}
}
