using System.Runtime.InteropServices;
using BizHawk.BizInvoke;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Mupen64;

public abstract class Mupen64VideoPluginApi : Mupen64PluginApi
{
	[BizImport(CallingConvention.Cdecl)]
	public abstract void ReadScreen2(IntPtr dest, ref int width, ref int height, int front);

	public unsafe void ReadScreen2(int[] dest, ref int width, ref int height, int front)
	{
		fixed (int* destPointer = dest)
			ReadScreen2((IntPtr)destPointer, ref width, ref height, front);
	}
}
