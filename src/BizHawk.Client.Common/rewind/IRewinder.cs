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
		/// Rewind 1 or 2 saved frames, depending on whether the last frame is stale.
		/// </summary>
		bool Rewind(bool fastForward);

		void Suspend();
		void Resume();

		void Clear();
	}
}
