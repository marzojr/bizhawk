using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Mupen64;

public partial class Mupen64
{
	private const int BIZHAWK_OUTPUT_SAMPLERATE = 44100;
	private SDLResampler _resampler;
	private int _currentInputRate;
	private short[] _audioBuffer = [ ];

	private void InitSound(int sourceRate)
	{
		_currentInputRate = sourceRate;
		_resampler = new SDLResampler(sourceRate, BIZHAWK_OUTPUT_SAMPLERATE);
	}

	private void UpdateAudio(bool renderSound)
	{
		int newAudioRate = AudioPluginApi.GetAudioRate();
		if (newAudioRate != _currentInputRate)
		{
			_resampler.ChangeRate(newAudioRate, BIZHAWK_OUTPUT_SAMPLERATE);
			_currentInputRate = newAudioRate;
		}

		int audioBufferSize = AudioPluginApi.GetBufferSize();
		if (_audioBuffer.Length < audioBufferSize)
		{
			_audioBuffer = new short[audioBufferSize];
		}

		if (audioBufferSize > 0)
		{
			Console.WriteLine($"reading and enqueuing {audioBufferSize} samples");
			AudioPluginApi.ReadAudioBuffer(_audioBuffer);
			if (renderSound)
				_resampler.EnqueueSamples(_audioBuffer, audioBufferSize / 2);
		}
	}
}
