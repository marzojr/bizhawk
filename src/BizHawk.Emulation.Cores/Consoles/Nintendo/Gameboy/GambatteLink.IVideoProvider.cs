using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class GambatteLink : IVideoProvider
	{
		public int VirtualWidth
			=> (ShowAnyBorder() ? 256 : 160) * _numCores;

		public int VirtualHeight
			=> ShowAnyBorder() ? 224 : 144;

		public int BufferWidth
			=> VirtualWidth;

		public int BufferHeight
			=> VirtualHeight;

		public int VsyncNumerator => _linkedCores[P1].VsyncNumerator;

		public int VsyncDenominator => _linkedCores[P1].VsyncDenominator;

		public int BackgroundColor => unchecked((int)0xff000000);

		private readonly int[] FrameBuffer;

		public int[] GetVideoBuffer()
			=> ShowAnyBorder() ? SgbVideoBuffer : VideoBuffer;

		private readonly int[] VideoBuffer;

		private readonly int[] SgbVideoBuffer;

		private int[] CreateVideoBuffer()
		{
			var b = new int[160 * _numCores * 144];
			for (int i = 0; i < b.Length; i++)
			{
				b[i] = -1; // GB/C screen is disabled on bootup, so it always starts as white, not black
			}
			return b;
		}

		private int[] CreateSGBVideoBuffer()
			=> IsAnySgb ? new int[256 * _numCores * 224] : null;
	}
}
