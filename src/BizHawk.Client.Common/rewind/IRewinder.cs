using System;

namespace BizHawk.Client.Common
{
	public interface IRewinder : IDisposable
	{
		int Count { get; }
		float FullnessRatio { get; }
		long Size { get; }
		int RewindFrequency { get; }

		bool Active { get; }

		void Capture(int frame);
		/// <summary>
		/// Rewind a configurable number of frames while skipping stale frames.
		/// </summary>
		bool Rewind(bool fastForward);

		void Suspend();
		void Resume();

		void Clear();
	}
}
