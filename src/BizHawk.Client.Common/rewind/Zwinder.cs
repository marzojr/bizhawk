using System.IO;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// A simple ring buffer rewinder
	/// </summary>
	public class Zwinder : IRewinder
	{
		/*
		Main goals:
		1. No copies, ever.  States are deposited directly to, and read directly from, one giant ring buffer.
			As a consequence, there is no multi-threading because there is nothing to thread.
		2. Support for arbitrary and changeable state sizes.  Frequency is calculated dynamically.
		3. No delta compression.  Keep it simple.  If there are cores that benefit heavily from delta compression, we should
			maintain a separate rewinder alongside this one that is customized for those cores.
		*/

		private readonly ZwinderBuffer _buffer;
		private readonly IStatable _stateSource;

		public Zwinder(IStatable stateSource, IRewindSettings settings)
		{
			_buffer = new ZwinderBuffer(settings);
			_stateSource = stateSource;
			Active = true;
		}

		/// <summary>
		/// How many states are actually in the state ringbuffer
		/// </summary>
		public int Count => _buffer.Count;

		public float FullnessRatio => Used / (float)Size;

		/// <summary>
		/// total number of bytes used
		/// </summary>
		/// <value></value>
		public long Used => _buffer.Used;

		/// <summary>
		/// Total size of the _buffer
		/// </summary>
		/// <value></value>
		public long Size => _buffer.Size;

		/// <summary>
		/// TODO: This is not a frequency, it's the reciprocal
		/// </summary>
		public int RewindFrequency => _buffer.RewindFrequency;

		public bool Active { get; private set; }

		/// <summary>
		/// Whether the last frame we captured is stale and should be avoided when rewinding
		/// </summary>
		public bool HasStaleFrame { get; private set; }

		public void Capture(int frame)
		{
			if (!Active)
				return;
			HasStaleFrame = false;
			_buffer.Capture(frame, s => {HasStaleFrame = true; _stateSource.SaveStateBinary(new BinaryWriter(s)); });
		}

		public bool Rewind(bool fastForward)
		{
			if (!Active || Count == 0)
				return false;

			if (Count == 1 && HasStaleFrame)
			{
				// the only option is to load the stale frame
				// the emulatur will handle this by not frame advancing
				var state = _buffer.GetState(0);
				_stateSource.LoadStateBinary(new BinaryReader(state.GetReadStream()));
				// the emulator won't frame advance, so we should invalidate the state we loaded
				_buffer.InvalidateEnd(0);
			}
			else
			{
				int rewindSteps = fastForward ? 5 : 1;
				if (HasStaleFrame) ++rewindSteps;
				var index = rewindSteps > Count ? 0 : Count - rewindSteps;
				var state = _buffer.GetState(index);
				_stateSource.LoadStateBinary(new BinaryReader(state.GetReadStream()));
				// the emulator will frame advance without giving us a chance to re-capture
				// the state we just loaded -> keep the state but mark it as stale
				_buffer.InvalidateEnd(index + 1);
				HasStaleFrame = true;
			}
			return true;
		}

		public void Suspend()
		{
			Active = false;
		}

		public void Resume()
		{
			Active = true;
		}

		public void Dispose()
		{
			_buffer.Dispose();
		}

		public void Clear()
		{
			_buffer.InvalidateEnd(0);
		}
	}
}
