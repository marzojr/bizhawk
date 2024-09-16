using System.ComponentModel;

namespace BizHawk.Emulation.Cores.Nintendo.N64
{
	public partial class N64SyncSettings
	{
		public class N64ControllerSettings
		{
			/// <summary>
			/// Enumeration defining the different N64 controller pak types
			/// </summary>
			public enum N64ControllerPakType
			{
				[Description("None")]
				NO_PAK = 1,

				[Description("Memory Card")]
				MEMORY_CARD = 2,

				[Description("Rumble Pak")]
				RUMBLE_PAK = 3,

				[Description("Transfer Pak")]
				TRANSFER_PAK = 4
			}

			private N64ControllerPakType _type = N64ControllerPakType.NO_PAK;

			/// <summary>
			/// Type of the pak inserted in the controller
			/// Currently only NO_PAK and MEMORY_CARD are
			/// supported. Other values may be set and
			/// are recognized but they have no function
			/// yet. e.g. TRANSFER_PAK makes the N64
			/// recognize a transfer pak inserted in
			/// the controller but there is no
			/// communication to the transfer pak.
			/// </summary>
			public N64ControllerPakType PakType
			{
				get => _type;
				set => _type = value;
			}

			private bool _isConnected = true;

			/// <summary>
			/// Connection status of the controller i.e.:
			/// Is the controller plugged into the N64?
			/// </summary>
			public bool IsConnected
			{
				get => _isConnected;
				set => _isConnected = value;
			}

			/// <summary>
			/// Clones this object
			/// </summary>
			/// <returns>New object with the same values</returns>
			public N64ControllerSettings Clone()
			{
				return new N64ControllerSettings
				{
					PakType = PakType,
					IsConnected = IsConnected
				};
			}
		}
	}
}
