using System.Runtime.InteropServices;
using BizHawk.BizInvoke;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Mupen64;

public abstract class Mupen64AudioPluginApi : Mupen64PluginApi
{
	[BizImport(CallingConvention.Cdecl)]
	public abstract void ReadAudioBuffer(IntPtr dest);

	[BizImport(CallingConvention.Cdecl)]
	public abstract int GetBufferSize();

	[BizImport(CallingConvention.Cdecl)]
	public abstract int GetAudioRate();
}
