using System.Runtime.InteropServices;
using BizHawk.BizInvoke;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Mupen64;

public abstract class Mupen64PluginApi
{
	[BizImport(CallingConvention.Cdecl)]
	public abstract Mupen64Api.m64p_error PluginStartup(IntPtr coreLibHandle, IntPtr context, Mupen64Api.DebugCallback debugCallback);

	[BizImport(CallingConvention.Cdecl)]
	public abstract Mupen64Api.m64p_error PluginShutdown();

	[BizImport(CallingConvention.Cdecl)]
	public abstract unsafe Mupen64Api.m64p_error PluginGetVersion(ref Mupen64Api.m64p_plugin_type pluginType, ref int pluginVersion, ref int apiVersion, char** pluginNamePtr, ref int capabilities);

	public unsafe Mupen64Api.m64p_error PluginGetVersion(ref Mupen64Api.m64p_plugin_type pluginType, ref int pluginVersion, ref int apiVersion, ref int capabilities)
	{
		return PluginGetVersion(ref pluginType, ref pluginVersion, ref apiVersion, null, ref capabilities);
	}
}
