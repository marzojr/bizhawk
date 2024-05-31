using System.ComponentModel;
using System.Text.Json.Serialization;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Vectrex
{
	public partial class VectrexHawk : IEmulator, ISettable<object, VectrexHawk.VectrexSyncSettings>
	{
		public object GetSettings() => _settings;

		public VectrexSyncSettings GetSyncSettings()
		{
			return _syncSettings.Clone();
		}

		public PutSettingsDirtyBits PutSettings(object o)
		{
			_settings = o;
			return PutSettingsDirtyBits.None;
		}

		public PutSettingsDirtyBits PutSyncSettings(VectrexSyncSettings o)
		{
			bool ret = VectrexSyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		private object _settings = new object();
		public VectrexSyncSettings _syncSettings = new VectrexSyncSettings();

		[CoreSettings]
		public class VectrexSyncSettings
		{
			// TODO: should Port2 also be JsonIgnored here?
			[JsonIgnore]
			public string Port1 = VectrexHawkControllerDeck.DefaultControllerName;
			public string Port2 = VectrexHawkControllerDeck.DefaultControllerName;

			public enum ControllerType
			{
				Digital,
				Analog
			}

			private ControllerType _VectrexController1;
			private ControllerType _VectrexController2;

			[DisplayName("Controller 1")]
			[Description("Select Controller Type")]
			[DefaultValue(ControllerType.Digital)]
			public ControllerType VectrexController1
			{
				get => _VectrexController1;
				set
				{
					if (value == ControllerType.Digital) { Port1 = VectrexHawkControllerDeck.DefaultControllerName; }
					else { Port1 = "Vectrex Analog Controller"; }

					_VectrexController1 = value;
				}
			}

			[DisplayName("Controller 2")]
			[Description("Select Controller Type")]
			[DefaultValue(ControllerType.Digital)]
			public ControllerType VectrexController2
			{
				get => _VectrexController2;
				set
				{
					if (value == ControllerType.Digital) { Port2 = VectrexHawkControllerDeck.DefaultControllerName; }
					else { Port2 = "Vectrex Analog Controller"; }

					_VectrexController2 = value;
				}
			}

			public VectrexSyncSettings Clone()
			{
				return (VectrexSyncSettings)MemberwiseClone();
			}

			public VectrexSyncSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}

			public static bool NeedsReboot(VectrexSyncSettings x, VectrexSyncSettings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}
		}
	}
}
