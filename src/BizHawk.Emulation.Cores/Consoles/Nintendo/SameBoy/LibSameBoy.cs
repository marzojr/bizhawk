using System.Runtime.InteropServices;

using BizHawk.BizInvoke;
using BizHawk.Emulation.Cores.Consoles.Nintendo.Gameboy;

namespace BizHawk.Emulation.Cores.Nintendo.Sameboy
{
	public abstract class LibSameboy
	{
		private const CallingConvention cc = CallingConvention.Cdecl;

		[Flags]
		public enum Buttons : uint
		{
			RIGHT = 0x01,
			LEFT = 0x02,
			UP = 0x04,
			DOWN = 0x08,
			A = 0x10,
			B = 0x20,
			SELECT = 0x40,
			START = 0x80,
		}

		// mirror of GB_direct_access_t
		public enum MemoryAreas : uint
		{
			ROM,
			RAM,
			CART_RAM,
			VRAM,
			HRAM,
			IO,
			BOOTROM,
			OAM,
			BGP,
			OBP,
			IE,
			BGPRGB,
			OBPRGB,
		}

		[BizImport(cc)]
		public abstract IntPtr sameboy_create(byte[] romdata, int romlength, byte[] biosdata, int bioslength, Sameboy.SameboySyncSettings.GBModel model, bool realtime, bool nobounce);

		[BizImport(cc)]
		public abstract void sameboy_destroy(IntPtr core);

		[StructLayout(LayoutKind.Sequential)]
		public struct GBSInfo
		{
			public byte TrackCount;
			public byte FirstTrack;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 33)]
			public byte[] Title;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 33)]
			public byte[] Author;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 33)]
			public byte[] Copyright;
		}

		[BizImport(cc, Compatibility = true)]
		public abstract bool sameboy_loadgbs(IntPtr core, byte[] gbs, int gbslen, ref GBSInfo gbsInfo);

		[BizImport(cc)]
		public abstract void sameboy_switchgbstrack(IntPtr core, int track);

		[UnmanagedFunctionPointer(cc)]
		public delegate void InputCallback();

		[BizImport(cc)]
		public abstract void sameboy_setinputcallback(IntPtr core, InputCallback callback);

		[UnmanagedFunctionPointer(cc)]
		public delegate void RumbleCallback(int amplitude);

		[BizImport(cc)]
		public abstract void sameboy_setrumblecallback(IntPtr core, RumbleCallback callback);

		[BizImport(cc)]
		public abstract void sameboy_frameadvance(IntPtr core, Buttons buttons, ushort x, ushort y, short[] soundbuf, ref int nsamps, int[] videobuf, bool render, bool border);

		[BizImport(cc)]
		public abstract void sameboy_reset(IntPtr core);

		[BizImport(cc)]
		public abstract bool sameboy_iscgbdmg(IntPtr core);

		[BizImport(cc)]
		public abstract void sameboy_savesram(IntPtr core, byte[] dest);

		[BizImport(cc)]
		public abstract void sameboy_loadsram(IntPtr core, byte[] data, int len);

		[BizImport(cc)]
		public abstract int sameboy_sramlen(IntPtr core);

		[BizImport(cc)]
		public abstract void sameboy_savestate(IntPtr core, byte[] data);

		[BizImport(cc)]
		public abstract bool sameboy_loadstate(IntPtr core, byte[] data, int len);

		[BizImport(cc)]
		public abstract int sameboy_statelen(IntPtr core);

		[BizImport(cc)]
		public abstract bool sameboy_getmemoryarea(IntPtr core, MemoryAreas which, ref IntPtr data, ref long length);

		[BizImport(cc)]
		public abstract byte sameboy_cpuread(IntPtr core, ushort addr);

		[BizImport(cc)]
		public abstract void sameboy_cpuwrite(IntPtr core, ushort addr, byte val);

		[BizImport(cc)]
		public abstract long sameboy_getcyclecount(IntPtr core);

		[BizImport(cc)]
		public abstract void sameboy_setcyclecount(IntPtr core, long newcc);

		[UnmanagedFunctionPointer(cc)]
		public delegate void TraceCallback(ushort pc);

		[BizImport(cc)]
		public abstract void sameboy_settracecallback(IntPtr core, TraceCallback callback);

		[BizImport(cc)]
		public abstract void sameboy_getregs(IntPtr core, int[] buf);

		[BizImport(cc)]
		public abstract void sameboy_setreg(IntPtr core, int which, int value);

		[BizImport(cc)]
		public abstract void sameboy_getbankregs(IntPtr core, int[] buf);

		[UnmanagedFunctionPointer(cc)]
		public delegate void MemoryCallback(ushort address, byte data);

		[BizImport(cc)]
		public abstract void sameboy_setmemorycallback(IntPtr core, int which, MemoryCallback callback);

		[BizImport(cc)]
		public abstract void sameboy_setprintercallback(IntPtr core, PrinterCallback callback);

		[BizImport(cc)]
		public abstract void sameboy_setscanlinecallback(IntPtr core, ScanlineCallback callback, int sl);

		[BizImport(cc)]
		public abstract void sameboy_setrtcdivisoroffset(IntPtr core, int offset);

		[StructLayout(LayoutKind.Sequential)]
		public struct NativeSettings
		{
			public Sameboy.SameboySettings.GBPaletteType Palette;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
			public int[] CustomPalette;
			public Sameboy.SameboySettings.ColorCorrectionMode ColorCorrectionMode;
			public int LightTemperature;
			public Sameboy.SameboySettings.HighPassFilterMode HighPassFilter;
			public int InterferenceVolume;
			public int ChannelMask;
			[MarshalAs(UnmanagedType.U1)]
			public bool BackgroundEnabled;
			[MarshalAs(UnmanagedType.U1)]
			public bool ObjectsEnabled;
		}

		[BizImport(cc, Compatibility = true)]
		public abstract void sameboy_setsettings(IntPtr core, ref NativeSettings settings);
	}
}
