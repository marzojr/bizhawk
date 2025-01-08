using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.Stella
{
	public partial class Stella : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition => _controllerDeck.Definition;

		private bool _leftDifficultyToggled;
		private bool _rightDifficultyToggled;

		public bool FrameAdvance(IController controller, bool render, bool renderSound)
		{
			int port1 = _controllerDeck.ReadPort1(controller);
			int port2 = _controllerDeck.ReadPort2(controller);
            
			// Handle all the console switches here
			if (controller.IsPressed("Toggle Right Difficulty")) _rightDifficultyToggled = !_rightDifficultyToggled;
			if (controller.IsPressed("Toggle Left Difficulty"))	_leftDifficultyToggled = !_leftDifficultyToggled;
			
			// select and reset switches default to an unpressed state
			// unknown whether TV color switch matters for TASing, so default to Color for now
			int switchPort = 0b00001011;
			if (_rightDifficultyToggled) switchPort |= 0b10000000;
			if (_leftDifficultyToggled)  switchPort |= 0b01000000;
			if (controller.IsPressed("Select")) switchPort &= 0b11111101; // 0 = Pressed
			if (controller.IsPressed("Reset"))  switchPort &= 0b11111110; // 0 = Pressed
			
			bool powerPressed = false;
			if (controller.IsPressed("Power")) powerPressed = true;

			IsLagFrame = true;

			Core.stella_frame_advance(port1, port2, switchPort, powerPressed);

			if (IsLagFrame)
				LagCount++;

			if (render)
				UpdateVideo();

			if (renderSound)
				UpdateAudio();

			Frame++;

			return true;
		}

		public int Frame { get; private set; }

		public string SystemId => VSystemID.Raw.A26;

		public bool DeterministicEmulation => true;

		public void ResetCounters()
		{
			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;
		}

		public void Dispose()
		{
			_elf.Dispose();
			DisposeSound();
		}
	}
}
