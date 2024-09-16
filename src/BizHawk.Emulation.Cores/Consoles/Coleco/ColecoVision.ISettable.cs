using System.Text.Json.Serialization;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.ColecoVision
{
	public partial class ColecoVision : IEmulator, ISettable<ColecoVision.ColecoSettings, ColecoVision.ColecoSyncSettings>
	{
		public ColecoSettings GetSettings()
		{
			return _settings.Clone();
		}

		public ColecoSyncSettings GetSyncSettings()
		{
			return _syncSettings.Clone();
		}

		public PutSettingsDirtyBits PutSettings(ColecoSettings o)
		{
			_settings = o;
			return PutSettingsDirtyBits.None;
		}

		public PutSettingsDirtyBits PutSyncSettings(ColecoSyncSettings o)
		{
			bool ret = o.SkipBiosIntro != _syncSettings.SkipBiosIntro;
			ret |= ColecoSyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		public class ColecoSettings
		{
			public ColecoSettings Clone()
			{
				return (ColecoSettings)MemberwiseClone();
			}
		}

		private ColecoSettings _settings = new ColecoSettings();
		private ColecoSyncSettings _syncSettings = new ColecoSyncSettings();

		public class ColecoSyncSettings
		{
			public bool SkipBiosIntro { get; set; }
			public bool UseSGM { get; set; }

			private string _port1 = ColecoVisionControllerDeck.DefaultControllerName;
			private string _port2 = ColecoVisionControllerDeck.DefaultControllerName;

			[JsonIgnore]
			public string Port1
			{
				get => _port1;
				set
				{
					if (!ColecoVisionControllerDeck.ControllerCtors.ContainsKey(value))
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
					if (!ColecoVisionControllerDeck.ControllerCtors.ContainsKey(value))
					{
						throw new InvalidOperationException("Invalid controller type: " + value);
					}

					_port2 = value;
				}
			}

			public ColecoSyncSettings Clone()
			{
				return (ColecoSyncSettings)MemberwiseClone();
			}

			public static bool NeedsReboot(ColecoSyncSettings x, ColecoSyncSettings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}
		}
	}
}
