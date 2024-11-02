using System.Diagnostics;
using System.Threading;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Mupen64;

public partial class Mupen64 : IVideoProvider
{
	public int[] GetVideoBuffer()
	{
		return _videoBuffer;
	}

	public int VirtualWidth => BufferWidth;
	public int VirtualHeight => BufferHeight;
	public int BufferWidth { get; private set; }
	public int BufferHeight { get; private set; }
	public int VsyncNumerator { get; private init; }
	public int VsyncDenominator => 1;
	public int BackgroundColor => 0;

	private readonly Mupen64Api.m64p_frame_callback _frameCallback;

	private int[] _videoBuffer = [ ];
	private byte[] _retVideoBuffer = [ ];

	private readonly EventWaitHandle _frameFinished = new AutoResetEvent(false);

	private void FrameCallback(uint frameIndex)
	{
		int width = 0;
		int height = 0;
		VideoPluginApi.ReadScreen2(IntPtr.Zero, ref width, ref height, 1);
		Debug.Assert(width <= BufferWidth);
		Debug.Assert(height <= BufferHeight);

		Array.Clear(_videoBuffer, width * height, _videoBuffer.Length - width * height);

		VideoPluginApi.ReadScreen2(_retVideoBuffer, ref width, ref height, 1);
		// the returned video buffer is in format RGB888 and also flipped vertically
		for (int y = 0; y < height; y++)
		for (int x = 0; x < width; x++)
		{
			byte r = _retVideoBuffer[3*(height-y-1) * width + 3*x];
			byte g = _retVideoBuffer[3*(height-y-1) * width + 3*x + 1];
			byte b = _retVideoBuffer[3*(height-y-1) * width + 3*x + 2];
			int argb = (r << 16) | (g << 8) | (b << 0);
			_videoBuffer[y * width + x] = argb;
		}

		_frameFinished.Set();
	}
}
