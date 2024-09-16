using System.Text.Json.Serialization;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Intellivision
{
	public partial class Intellivision : IEmulator, ISettable<Intellivision.IntvSettings, Intellivision.IntvSyncSettings>
	{
		public IntvSettings GetSettings()
		{
			return _settings.Clone();
		}

		public IntvSyncSettings GetSyncSettings()
		{
			return _syncSettings.Clone();
		}

		public PutSettingsDirtyBits PutSettings(IntvSettings o)
		{
			_settings = o;
			return PutSettingsDirtyBits.None;
		}

		public PutSettingsDirtyBits PutSyncSettings(IntvSyncSettings o)
		{
			bool ret = IntvSyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		private IntvSettings _settings = new IntvSettings();
		private IntvSyncSettings _syncSettings = new IntvSyncSettings();

		public class IntvSettings
		{
			public IntvSettings Clone()
			{
				return (IntvSettings)MemberwiseClone();
			}
		}

		public class IntvSyncSettings
		{
			private string _port1 = IntellivisionControllerDeck.DefaultControllerName;
			private string _port2 = IntellivisionControllerDeck.DefaultControllerName;

			[JsonIgnore]
			public string Port1
			{
				get => _port1;
				set
				{
					if (!IntellivisionControllerDeck.ControllerCtors.ContainsKey(value))
					{
						throw new InvalidOperationException("Invalid controller type: " + value);
					}

					_port1 = value;
				}
			}

			[JsonIgnore]
			public string Port2
			{
				get => _port2;
				set
				{
					if (!IntellivisionControllerDeck.ControllerCtors.ContainsKey(value))
					{
						throw new InvalidOperationException("Invalid controller type: " + value);
					}

					_port2 = value;
				}
			}

			public IntvSyncSettings Clone()
			{
				return (IntvSyncSettings)MemberwiseClone();
			}

			public static bool NeedsReboot(IntvSyncSettings x, IntvSyncSettings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}
		}
	}
}
