using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.N3DS
{
	public partial class Encore : IEmulator
	{
		private readonly BasicServiceProvider _serviceProvider;

		public IEmulatorServiceProvider ServiceProvider => _serviceProvider;

		public ControllerDefinition ControllerDefinition => _3DSController;

		public int Frame { get; set; }

		public string SystemId => VSystemID.Raw.N3DS;

		public bool DeterministicEmulation { get; }

		public bool FrameAdvance(IController controller, bool render, bool renderSound = true)
		{
			_controller = controller;
			IsLagFrame = true;

			_motionEmu.Update(
				controller.IsPressed("Tilt"),
				controller.AxisValue("Tilt X"),
				controller.AxisValue("Tilt Y"));

			if (_controller.IsPressed("Reset"))
			{
				_core.Encore_Reset(_context);
				// memory domain pointers are no longer valid, reset them
				WireMemoryDomains();
			}

			IsLagFrame = _core.Encore_RunFrame(_context);

			OnVideoRefresh();

			if (renderSound)
			{
				ProcessSound();
			}

			Frame++;
			if (IsLagFrame)
			{
				LagCount++;
			}

			return true;
		}

		private void OnVideoRefresh()
		{
			_core.Encore_GetVideoBufferDimensions(_context, out _encoreVideoProvider.BW, out _encoreVideoProvider.BH);
			_encoreVideoProvider.VideoDirty = true;

			_core.Encore_GetTouchScreenLayout(_context, out var x, out var y, out var width, out var height, out var rotated, out var enabled);
			TouchScreenRectangle = new(x, y, width, height);
			TouchScreenRotated = rotated;
			TouchScreenEnabled = enabled;
		}

		public void ResetCounters()
		{
			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;
		}

		private bool _disposed;

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_core.Encore_DestroyContext(_context);

			foreach (var glContext in _glContexts)
			{
				_openGLProvider.ReleaseContext(glContext);
			}

			_glContexts.Clear();

			CurrentCore = null;
			_disposed = true;
		}
	}
}
