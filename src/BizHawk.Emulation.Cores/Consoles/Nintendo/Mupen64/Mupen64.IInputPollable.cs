using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Mupen64;

public partial class Mupen64 : IInputPollable
{
	public int LagCount { get; set; }

	public bool IsLagFrame { get; set; }

	public IInputCallbackSystem InputCallbacks { get; } = new InputCallbackSystem();
}
