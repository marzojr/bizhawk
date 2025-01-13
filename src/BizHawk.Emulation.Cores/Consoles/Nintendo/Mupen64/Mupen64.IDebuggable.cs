using System.Collections.Generic;
using System.Runtime.InteropServices;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Mupen64;

public partial class Mupen64 : IDebuggable
{
	public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
	{
		var ret = new Dictionary<string, RegisterValue>();

		var pcPointer = Mupen64Api.DebugGetCPUDataPtr(Mupen64Api.m64p_dbg_cpu_data.PC);
		ret.Add("PC", Marshal.ReadInt32(pcPointer));

		var regPointer = Mupen64Api.DebugGetCPUDataPtr(Mupen64Api.m64p_dbg_cpu_data.REG_REG);
		for (int i = 0; i < 32; i++)
		{
			ret.Add($"REG {i}", Marshal.ReadInt64(regPointer, 8 * i));
		}

		var hiPointer = Mupen64Api.DebugGetCPUDataPtr(Mupen64Api.m64p_dbg_cpu_data.REG_HI);
		ret.Add("REG HI", Marshal.ReadInt64(hiPointer));

		var loPointer = Mupen64Api.DebugGetCPUDataPtr(Mupen64Api.m64p_dbg_cpu_data.REG_LO);
		ret.Add("REG LO", Marshal.ReadInt64(loPointer));

		var cop0Pointer = Mupen64Api.DebugGetCPUDataPtr(Mupen64Api.m64p_dbg_cpu_data.REG_COP0);
		for (int i = 0; i < 32; i++)
		{
			ret.Add($"COP0 {i}", Marshal.ReadInt32(cop0Pointer, 4 * i));
		}

		var cop1DoublePointer = Mupen64Api.DebugGetCPUDataPtr(Mupen64Api.m64p_dbg_cpu_data.REG_COP1_DOUBLE_PTR);
		for (int i = 0; i < 32; i++)
		{
			ret.Add($"COP1 Double {i}", Marshal.ReadInt64(cop1DoublePointer, 8 * i));
		}

		var cop1SimplePointer = Mupen64Api.DebugGetCPUDataPtr(Mupen64Api.m64p_dbg_cpu_data.REG_COP1_SIMPLE_PTR);
		for (int i = 0; i < 32; i++)
		{
			ret.Add($"COP1 Simple {i}", Marshal.ReadInt32(cop1SimplePointer, 4 * i));
		}

		var cop1FgrPointer = Mupen64Api.DebugGetCPUDataPtr(Mupen64Api.m64p_dbg_cpu_data.REG_COP1_FGR_64);
		ret.Add("COP1 FGR", Marshal.ReadInt64(cop1FgrPointer));

		// TLB?

		return ret;
	}

	[FeatureNotImplemented] // could probably be implemented, just a bit annoying
	public void SetCpuRegister(string register, int value) => throw new NotImplementedException();

	// TODO: currently nonfunctional, need to do something similar to the old implementation probably
	public IMemoryCallbackSystem MemoryCallbacks { get; } = new MemoryCallbackSystem([ "System Bus" ]);
	public bool CanStep(StepType type)
	{
		return type switch
		{
			StepType.Into => true,
			_ => false
		};
	}

	public void Step(StepType type)
	{
		switch (type)
		{
			case StepType.Into:
				Mupen64Api.DebugSetRunState(Mupen64Api.m64p_dbg_runstate.PAUSED); // no-op when already paused
				Mupen64Api.CoreDoCommand(Mupen64Api.m64p_command.ADVANCE_FRAME, 0, IntPtr.Zero); // no-op when already frame advancing
				Mupen64Api.DebugStep();
				break;

			default:
				throw new NotImplementedException();
		}
	}

	[FeatureNotImplemented] // can probably implement this for pure interpreter at least, but is it worth it?
	public long TotalExecutedCycles => throw new NotImplementedException();
}
