using System;
using System.Runtime.InteropServices;
using BizHawk.BizInvoke;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Mupen64;

public abstract class Mupen64InputPluginApi : Mupen64PluginApi
{
	[Flags]
	public enum BoolButtons : ushort
	{
		R_DPAD = 1 << 0,
		L_DPAD = 1 << 1,
		D_DPAD = 1 << 2,
		U_DPAD = 1 << 3,
		START_BUTTON = 1 << 4,
		Z_TRIG = 1 << 5,
		B_BUTTON = 1 << 6,
		A_BUTTON = 1 << 7,
		R_CBUTTON = 1 << 8,
		L_CBUTTON = 1 << 9,
		D_CBUTTON = 1 << 10,
		U_CBUTTON = 1 << 11,
		R_TRIG = 1 << 12,
		L_TRIG = 1 << 13,

	}

	[StructLayout(LayoutKind.Sequential)]
	public struct InputState
	{
		public BoolButtons boolButtons;
		public sbyte X_AXIS;
		public sbyte Y_AXIS;
	}

	public delegate InputState InputCallback(int controller);
	public delegate void RumbleCallback(int controller, bool on);

	[BizImport(CallingConvention.Cdecl)]
	public abstract void SetInputCallback(InputCallback inputCallback);

	[BizImport(CallingConvention.Cdecl)]
	public abstract void SetRumbleCallback(RumbleCallback rumbleCallback);

	[BizImport(CallingConvention.Cdecl)]
	public abstract void SetControllerConnected(int idx, bool connected);

	[BizImport(CallingConvention.Cdecl)]
	public abstract void SetControllerPakType(int idx, Mupen64.N64ControllerPakType type);
}
