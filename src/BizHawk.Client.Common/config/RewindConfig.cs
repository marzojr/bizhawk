namespace BizHawk.Client.Common
{
	public interface IRewindSettings
	{
		/// <summary>
		/// Gets a value indicating whether or not to compress savestates before storing them
		/// </summary>
		bool UseCompression { get; }

		/// <summary>
		/// Gets a value indicating whether or not to delta compress savestates before storing them
		/// </summary>
		/// <value></value>
		// TODO: This is in here for frontend reasons, but the buffer itself doesn't interact with this.
		bool UseDelta { get; }

		/// <summary>
		/// Buffer space to use in MB
		/// </summary>
		long BufferSize { get; }

		/// <summary>
		/// Specifies whether TargetFrameLength or TargetRewindInterval is used.
		/// </summary>
		public bool UseFixedRewindInterval { get; }

		/// <summary>
		/// Desired frame length (number of emulated frames you can go back before running out of buffer)
		/// </summary>
		int TargetFrameLength { get; }

		/// <summary>
		/// Desired rewind interval (number of emulated frames you can go back per rewind)
		/// </summary>
		int TargetRewindInterval { get; }

		/// <summary>
		/// Number of rewind states to go back per press of the rewind button, without and with fast forwarding.
		/// </summary>
		int speedMultiplier { get; }
		int fastSpeedMultiplier { get; }

		public enum BackingStoreType
		{
			Memory,
			TempFile,
		}

		public BackingStoreType BackingStore { get; }
	}

	public class RewindConfig : IRewindSettings
	{
		public bool UseCompression { get; set; }
		public bool UseDelta { get; set; }
		public bool Enabled { get; set; } = true;
		public long BufferSize { get; set; } = 512; // in mb
		public bool UseFixedRewindInterval { get; set; } = false;
		public int TargetFrameLength { get; set; } = 600;
		public int TargetRewindInterval { get; set; } = 5;
		public int speedMultiplier { get; set; } = 1;
		public int fastSpeedMultiplier { get; set; } = 5;
		public IRewindSettings.BackingStoreType BackingStore { get; set; } = IRewindSettings.BackingStoreType.Memory;
	}
}
