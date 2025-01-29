﻿using System.Runtime.InteropServices;

using BizHawk.BizInvoke;

namespace BizHawk.Emulation.Cores.Computers.Doom
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
		public abstract void dsda_get_audio(ref int n, ref IntPtr buffer);

		[BizImport(CallingConvention.Cdecl)]
		public abstract bool dsda_init([In] InitSettings settings);

		[BizImport(CallingConvention.Cdecl)]
		public abstract void dsda_frame_advance();

		[BizImport(CallingConvention.Cdecl)]
		public abstract void dsda_get_video(out int w, out int h, out int pitch, ref IntPtr buffer, out int palSize, ref IntPtr palBuffer);

		[BizImport(CallingConvention.Cdecl)]
		public abstract bool dsda_add_wad_file(
			string fileName,
			int fileSize,
			load_archive_cb feload_archive_cb);
	}
}
