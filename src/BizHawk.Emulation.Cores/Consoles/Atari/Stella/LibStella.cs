using System.Runtime.InteropServices;

using BizHawk.BizInvoke;

namespace BizHawk.Emulation.Cores.Atari.Stella
{
	public abstract class CInterface
	{
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int load_archive_cb(string filename, IntPtr buffer, int maxsize);

		[StructLayout(LayoutKind.Sequential)]
		public class InitSettings
		{
			public uint dummy;
		}

		[BizImport(CallingConvention.Cdecl)]
		public abstract void stella_get_audio(ref int n, ref IntPtr buffer);

		[BizImport(CallingConvention.Cdecl)]
		public abstract int stella_get_region();

		[BizImport(CallingConvention.Cdecl)]
		public abstract bool stella_init(
			string fileName,
			load_archive_cb feload_archive_cb,
			[In] InitSettings settings);

		[BizImport(CallingConvention.Cdecl)]
		public abstract void stella_frame_advance(int port1, int port2, bool reset, bool power, bool select, bool leftDiffToggled, bool rightDiffToggled);

		[BizImport(CallingConvention.Cdecl)]
		public abstract void stella_get_video(out int w, out int h, out int pitch, ref IntPtr buffer);

		[BizImport(CallingConvention.Cdecl)]
		public abstract void stella_get_frame_rate(out int fps);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void input_cb();

		[BizImport(CallingConvention.Cdecl)]
		public abstract void stella_set_input_callback(input_cb cb);

		[BizImport(CallingConvention.Cdecl)]
		public abstract byte stella_peek_tia(uint addr);

		[BizImport(CallingConvention.Cdecl)]
		public abstract void stella_poke_tia(uint addr, byte value);

		[BizImport(CallingConvention.Cdecl)]
		public abstract byte stella_peek_m6532(uint addr);

		[BizImport(CallingConvention.Cdecl)]
		public abstract void stella_poke_m6532(uint addr, byte value);

		[BizImport(CallingConvention.Cdecl)]
		public abstract byte stella_peek_systembus(uint addr);

		[BizImport(CallingConvention.Cdecl)]
		public abstract void stella_poke_systembus(uint addr, byte value);

		[BizImport(CallingConvention.Cdecl)]
		public abstract uint stella_get_cartram_size();

		[BizImport(CallingConvention.Cdecl)]
		public abstract byte stella_peek_cartram(uint addr);

		[BizImport(CallingConvention.Cdecl)]
		public abstract void stella_poke_cartram(uint addr, byte value);

		[BizImport(CallingConvention.Cdecl)]
		public abstract void stella_get_mainram_ptr(ref IntPtr addr);

		[BizImport(CallingConvention.Cdecl)]
		public abstract IntPtr stella_get_cart_type();
	}
}
