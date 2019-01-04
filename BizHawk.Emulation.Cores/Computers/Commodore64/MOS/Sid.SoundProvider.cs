﻿using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	public sealed partial class Sid : ISoundProvider
	{
		public bool CanProvideAsync => false;

		public SyncSoundMode SyncMode => SyncSoundMode.Sync;

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode != SyncSoundMode.Sync)
			{
				throw new InvalidOperationException("Only Sync mode is supported.");
			}
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new NotSupportedException("Async is not available");
		}

		public void DiscardSamples()
		{
			_outputBufferIndex = 0;
		}

		// Expose this as GetSamplesAsync to support async sound
		// There's not need to do this though unless this core wants to handle async in its own way (the client can handle these situations if not available from the core)
		private void GetSamples(short[] samples)
		{
			Flush(true);
			var length = Math.Min(samples.Length, _outputBufferIndex);
			for (var i = 0; i < length; i++)
			{
				samples[i] = _outputBuffer[i];
			}
			_outputBufferIndex = 0;
		}

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			Flush(true);

			_outputBuffer = new short[_outputBufferIndex * 2];
			for (int i = 0; i < _outputBufferIndex; i++)
			{
				_mixer = _outputBuffer_not_filtered[i] + _outputBuffer_filtered[i];
				_mixer = _mixer >> 7;
				_mixer = (_mixer * _volume_at_sample_time[i]) >> 4;
				_mixer -= _volume_at_sample_time[i] << 8;

				if (_mixer > 0x7FFF)
				{
					_mixer = 0x7FFF;
				}

				if (_mixer < -0x8000)
				{
					_mixer = -0x8000;
				}

				_outputBuffer[i * 2] = unchecked((short)_mixer);
				_outputBuffer[i * 2 + 1] = unchecked((short)_mixer);

			}

			samples = _outputBuffer;
			nsamp = _outputBufferIndex;
			last_filtered_value = _outputBuffer_filtered[_outputBufferIndex - 1];
			_outputBufferIndex = 0;
			filter_index = 0;
		}

	}
}
