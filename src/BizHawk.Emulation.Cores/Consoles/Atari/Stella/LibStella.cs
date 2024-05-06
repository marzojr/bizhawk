﻿using System;
using System.Runtime.InteropServices;

using BizHawk.BizInvoke;

namespace BizHawk.Emulation.Cores.Consoles.Atari.Stella
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
			[In]InitSettings settings);

		[BizImport(CallingConvention.Cdecl)]
		public abstract void stella_frame_advance(int port1, int port2, bool reset, bool power, bool leftDiffToggled, bool rightDiffToggled);

		[BizImport(CallingConvention.Cdecl)]
		public abstract void stella_get_video(out int w, out int h, out int pitch, ref IntPtr buffer);

		[BizImport(CallingConvention.Cdecl)]
		public abstract void stella_get_frame_rate(out int fps);
	}
}
