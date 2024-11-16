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

	private void DebuggerInitCallback() => Mupen64Api.DebugSetRunState(RUNNING);

	private void TraceCallback(uint pc)
	{
		if (Sink is null) return;

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

	public ITraceSink Sink { get; set; }
}
