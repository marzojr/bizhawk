using System;
using System.Runtime.InteropServices;

using BizHawk.BizInvoke;

namespace BizHawk.Emulation.Cores.Consoles.Sega.gpgx
{
	public abstract class LibGPGX
	{
		public const string DllName = "libgenplusgx.dll";

		[BizImport(CallingConvention.Cdecl)]
		public abstract void gpgx_get_video(out int w, out int h, out int pitch, ref IntPtr buffer);

		[BizImport(CallingConvention.Cdecl)]
		public abstract void gpgx_get_audio(ref int n, ref IntPtr buffer);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int load_archive_cb(string filename, IntPtr buffer, int maxsize);

		[BizImport(CallingConvention.Cdecl)]
		public abstract void gpgx_advance();

		public enum Region : int
		{
			Autodetect = 0,
			USA = 1,
			Europe = 2,
			Japan_NTSC = 3,
			Japan_PAL = 4
		}

		[StructLayout(LayoutKind.Sequential)]
		public class InitSettings
		{
			public uint BackdropColor;
			public Region Region;
			public ushort LowPassRange;
			public short LowFreq;
			public short HighFreq;
			public short LowGain;
			public short MidGain;
			public short HighGain;
			public enum FilterType : byte
			{
				None = 0,
				LowPass = 1,
				ThreeBand = 2
			}
			public FilterType Filter;
			public INPUT_SYSTEM InputSystemA;
			public INPUT_SYSTEM InputSystemB;
			public bool SixButton;
			public bool ForceSram;
		}

		[BizImport(CallingConvention.Cdecl)]
		public abstract bool gpgx_init(
			string feromextension,
			load_archive_cb feload_archive_cb,
			[In]InitSettings settings);

		[BizImport(CallingConvention.Cdecl)]
		public abstract void gpgx_get_fps(ref int num, ref int den);

		[BizImport(CallingConvention.Cdecl, Compatibility = true)]
		public abstract bool gpgx_get_control([Out]InputData dest, int bytes);
		[BizImport(CallingConvention.Cdecl, Compatibility = true)]
		public abstract bool gpgx_put_control([In]InputData src, int bytes);

		[BizImport(CallingConvention.Cdecl)]
		public abstract IntPtr gpgx_get_sram(ref int size);

		[BizImport(CallingConvention.Cdecl)]
		public abstract bool gpgx_put_sram(byte[] data, int size);

		[BizImport(CallingConvention.Cdecl)]
		public abstract void gpgx_clear_sram();

		public const int MIN_MEM_DOMAIN = 0;
		public const int MAX_MEM_DOMAIN = 13;

		[BizImport(CallingConvention.Cdecl)]
		// apparently, if you use built in string marshalling, the interop will assume that
		// the unmanaged char pointer was allocated in hglobal and try to free it that way
		public abstract IntPtr gpgx_get_memdom(int which, ref IntPtr area, ref int size);

		[BizImport(CallingConvention.Cdecl)]
		public abstract void gpgx_reset(bool hard);

		public const int MAX_DEVICES = 8;

		public enum INPUT_SYSTEM : byte
		{
			SYSTEM_NONE = 0,        // unconnected port	
			SYSTEM_MD_GAMEPAD = 1,  // single 3-buttons or 6-buttons Control Pad 	
			SYSTEM_MOUSE = 2,       // Sega Mouse 	
			SYSTEM_MENACER = 3,     // Sega Menacer -- port B only
			SYSTEM_JUSTIFIER = 4,   // Konami Justifiers -- port B only
			SYSTEM_XE_A1P = 5,      // XE-A1P analog controller -- port A only
			SYSTEM_ACTIVATOR = 6,   // Sega Activator 	
			SYSTEM_MS_GAMEPAD = 7,  // single 2-buttons Control Pad -- Master System
			SYSTEM_LIGHTPHASER = 8, // Sega Light Phaser -- Master System
			SYSTEM_PADDLE = 9,      // Sega Paddle Control -- Master System
			SYSTEM_SPORTSPAD = 10,  // Sega Sports Pad -- Master System
			SYSTEM_TEAMPLAYER = 11, // Multi Tap -- Sega TeamPlayer 	
			SYSTEM_WAYPLAY = 12,    // Multi Tap -- EA 4-Way Play -- use both ports
		}

		public enum INPUT_DEVICE : byte
		{
			DEVICE_NONE = 0xff,     // unconnected device = fixed ID for Team Player)
			DEVICE_PAD3B = 0x00,    // 3-buttons Control Pad = fixed ID for Team Player)
			DEVICE_PAD6B = 0x01,    // 6-buttons Control Pad = fixed ID for Team Player)
			DEVICE_PAD2B = 0x02,    // 2-buttons Control Pad
			DEVICE_MOUSE = 0x03,    // Sega Mouse
			DEVICE_LIGHTGUN = 0x04, // Sega Light Phaser, Menacer or Konami Justifiers
			DEVICE_PADDLE = 0x05,   // Sega Paddle Control
			DEVICE_SPORTSPAD = 0x06,// Sega Sports Pad
			DEVICE_PICO = 0x07,     // PICO tablet
			DEVICE_TEREBI = 0x08,   // Terebi Oekaki tablet
			DEVICE_XE_A1P = 0x09,   // XE-A1P analog controller
			DEVICE_ACTIVATOR = 0x0a,// Activator
		}


		public enum CDLog_AddrType
		{
			MDCART, RAM68k, RAMZ80, SRAM,
		}

		[Flags]
		public enum CDLog_Flags
		{
			Exec68k = 0x01,
			Data68k = 0x04,
			ExecZ80First = 0x08,
			ExecZ80Operand = 0x10,
			DataZ80 = 0x20,
			DMASource = 0x40,
		}


		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void input_cb();

		[BizImport(CallingConvention.Cdecl)]
		public abstract void gpgx_set_input_callback(input_cb cb);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void mem_cb(uint addr, uint value); //value MUST be uint, since the value will be trimmed if the type is byte (8-bit) or ushort (16-bit) and the value read/written/executed is bigger than that (i.e 32 bits).

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void CDCallback(int addr, CDLog_AddrType addrtype, CDLog_Flags flags);

		[BizImport(CallingConvention.Cdecl)]
		public abstract void gpgx_set_mem_callback(mem_cb read, mem_cb write, mem_cb exec);

		[BizImport(CallingConvention.Cdecl)]
		public abstract void gpgx_set_cd_callback(CDCallback cd);

		/// <summary>
		/// not every flag is valid for every device!
		/// </summary>
		[Flags]
		public enum INPUT_KEYS : ushort
		{
			/* Default Input bitmasks */
			INPUT_MODE = 0x0800,
			INPUT_X = 0x0400,
			INPUT_Y = 0x0200,
			INPUT_Z = 0x0100,
			INPUT_START = 0x0080,
			INPUT_A = 0x0040,
			INPUT_C = 0x0020,
			INPUT_B = 0x0010,
			INPUT_RIGHT = 0x0008,
			INPUT_LEFT = 0x0004,
			INPUT_DOWN = 0x0002,
			INPUT_UP = 0x0001,

			/* Master System specific bitmasks */
			INPUT_BUTTON2 = 0x0020,
			INPUT_BUTTON1 = 0x0010,

			/* Mega Mouse specific bitmask */
			INPUT_MOUSE_START = 0x0080,
			INPUT_MOUSE_CENTER = 0x0040,
			INPUT_MOUSE_RIGHT = 0x0020,
			INPUT_MOUSE_LEFT = 0x0010,

			/* Pico hardware specific bitmask */
			INPUT_PICO_PEN = 0x0080,
			INPUT_PICO_RED = 0x0010,

			/* XE-1AP specific bitmask */
			INPUT_XE_E1 = 0x0800,
			INPUT_XE_E2 = 0x0400,
			INPUT_XE_START = 0x0200,
			INPUT_XE_SELECT = 0x0100,
			INPUT_XE_A = 0x0080,
			INPUT_XE_B = 0x0040,
			INPUT_XE_C = 0x0020,
			INPUT_XE_D = 0x0010,

			/* Activator specific bitmasks */
			INPUT_ACTIVATOR_8U = 0x8000,
			INPUT_ACTIVATOR_8L = 0x4000,
			INPUT_ACTIVATOR_7U = 0x2000,
			INPUT_ACTIVATOR_7L = 0x1000,
			INPUT_ACTIVATOR_6U = 0x0800,
			INPUT_ACTIVATOR_6L = 0x0400,
			INPUT_ACTIVATOR_5U = 0x0200,
			INPUT_ACTIVATOR_5L = 0x0100,
			INPUT_ACTIVATOR_4U = 0x0080,
			INPUT_ACTIVATOR_4L = 0x0040,
			INPUT_ACTIVATOR_3U = 0x0020,
			INPUT_ACTIVATOR_3L = 0x0010,
			INPUT_ACTIVATOR_2U = 0x0008,
			INPUT_ACTIVATOR_2L = 0x0004,
			INPUT_ACTIVATOR_1U = 0x0002,
			INPUT_ACTIVATOR_1L = 0x0001,

			/* Menacer */
			INPUT_MENACER_TRIGGER = 0x0040,
			INPUT_MENACER_START = 0x0080,
			INPUT_MENACER_B = 0x0020,
			INPUT_MENACER_C = 0x0010,
		}

		[StructLayout(LayoutKind.Sequential)]
		public class InputData
		{
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
			public readonly INPUT_SYSTEM[] system = new INPUT_SYSTEM[2];
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_DEVICES)]
			public readonly INPUT_DEVICE[] dev = new INPUT_DEVICE[MAX_DEVICES];
			/// <summary>
			/// digital inputs
			/// </summary>
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_DEVICES)]
			public readonly INPUT_KEYS[] pad = new INPUT_KEYS[MAX_DEVICES];
			/// <summary>
			/// analog (x/y)
			/// </summary>
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_DEVICES * 2)]
			public readonly short[] analog = new short[MAX_DEVICES * 2];
			/// <summary>
			/// gun horizontal offset
			/// </summary>
			public int x_offset;
			/// <summary>
			/// gun vertical offset
			/// </summary>
			public int y_offset;

			public void ClearAllBools()
			{
				for (int i = 0; i < pad.Length; i++)
					pad[i] = 0;
			}
		}

		public const int CD_MAX_TRACKS = 100;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void cd_read_cb(int lba, IntPtr dest, bool audio);

		[StructLayout(LayoutKind.Sequential)]
		public struct CDTrack
		{
			public int start;
			public int end;
		}

		[StructLayout(LayoutKind.Sequential)]
		public class CDData
		{
			public int end;
			public int last;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = CD_MAX_TRACKS)]
			public readonly CDTrack[] tracks = new CDTrack[CD_MAX_TRACKS];
		}

		[BizImport(CallingConvention.Cdecl)]
		public abstract void gpgx_set_cdd_callback(cd_read_cb cddcb);

		[BizImport(CallingConvention.Cdecl, Compatibility = true)]
		public abstract void gpgx_swap_disc(CDData toc);

		[StructLayout(LayoutKind.Sequential)]
		public struct VDPNameTable
		{
			public int Width; // in cells
			public int Height; // in cells
			public int Baseaddr;
		}

		[StructLayout(LayoutKind.Sequential)]
		public class VDPView
		{
			public IntPtr VRAM;
			public IntPtr PatternCache;
			public IntPtr ColorCache;
			public VDPNameTable NTA;
			public VDPNameTable NTB;
			public VDPNameTable NTW;
		}

		[BizImport(CallingConvention.Cdecl, Compatibility = true)]
		public abstract void gpgx_get_vdp_view([Out] VDPView view);

		[BizImport(CallingConvention.Cdecl)]
		public abstract void gpgx_poke_cram(int addr, byte value);

		[BizImport(CallingConvention.Cdecl)]
		public abstract void gpgx_poke_vram(int addr, byte value);

		/// <summary>
		/// regenerate whatever portions of the bg pattern cache are currently dirty.
		/// </summary>
		[BizImport(CallingConvention.Cdecl)] // the core will handle this itself; you only need to call this when using the cache for your own purposes
		public abstract void gpgx_flush_vram();

		/// <summary>
		/// mark the bg pattern cache as dirty
		/// </summary>
		[BizImport(CallingConvention.Cdecl)]
		public abstract void gpgx_invalidate_pattern_cache();

		[StructLayout(LayoutKind.Sequential)]
		public struct RegisterInfo
		{
			public int Value;
			public IntPtr Name;
		}

		[BizImport(CallingConvention.Cdecl)]
		public abstract int gpgx_getmaxnumregs();

		[BizImport(CallingConvention.Cdecl, Compatibility = true)]
		public abstract int gpgx_getregs([Out] RegisterInfo[] regs);

		[Flags]
		public enum DrawMask : int
		{
			BGA = 1,
			BGB = 2,
			BGW = 4,
			Obj = 8,
			Backdrop = 16
		}

		[BizImport(CallingConvention.Cdecl)]
		public abstract void gpgx_set_draw_mask(DrawMask mask);
		[BizImport(CallingConvention.Cdecl)]
		public abstract void gpgx_set_sprite_limit_enabled(bool enabled);

		[BizImport(CallingConvention.Cdecl)]
		public abstract void gpgx_write_m68k_bus(uint addr, byte data);
		[BizImport(CallingConvention.Cdecl)]
		public abstract void gpgx_write_s68k_bus(uint addr, byte data);
		[BizImport(CallingConvention.Cdecl)]
		public abstract byte gpgx_peek_m68k_bus(uint addr);
		[BizImport(CallingConvention.Cdecl)]
		public abstract byte gpgx_peek_s68k_bus(uint addr);
	}
}
