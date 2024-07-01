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
	public int VsyncNumerator => 60;
	public int VsyncDenominator => 1;
	public int BackgroundColor => 0;

	private readonly Mupen64Api.m64p_frame_callback _frameCallback;

	private int[] _videoBuffer = [ ];

	private readonly EventWaitHandle _frameFinished = new AutoResetEvent(false);

	private void FrameCallback(uint frameIndex)
	{
		int width = 0;
		int height = 0;
		VideoPluginApi.ReadScreen2(IntPtr.Zero, ref width, ref height, 1);

		if (_videoBuffer.Length < width * height)
		{
			_videoBuffer = new int[width * height];
		}

		BufferWidth = width;
		BufferHeight = height;

		VideoPluginApi.ReadScreen2(_videoBuffer, ref width, ref height, 1);

		_frameFinished.Set();
	}
}
