using System.Text.Json.Serialization;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.A7800Hawk
{
	public partial class A7800Hawk : IEmulator, ISettable<object, A7800Hawk.A7800SyncSettings>
	{
		public object GetSettings()
			=> null;

		public A7800SyncSettings GetSyncSettings()
			=> _syncSettings.Clone();

		public PutSettingsDirtyBits PutSettings(object o)
			=> PutSettingsDirtyBits.None;

		public PutSettingsDirtyBits PutSyncSettings(A7800SyncSettings o)
		{
			var ret = A7800SyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		public A7800SyncSettings _syncSettings = new();

		public class A7800SyncSettings
		{
			private string _port1 = A7800HawkControllerDeck.DefaultControllerName;
			private string _port2 = A7800HawkControllerDeck.DefaultControllerName;

			[JsonIgnore]
			public string Filter { get; set; } = "None";

			[JsonIgnore]
			public string Port1
			{
				get => _port1;
				set
				{
					if (!A7800HawkControllerDeck.ControllerCtors.ContainsKey(value))
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
					if (!A7800HawkControllerDeck.ControllerCtors.ContainsKey(value))
					{
						throw new InvalidOperationException("Invalid controller type: " + value);
					}

					_port2 = value;
				}
			}

			public A7800SyncSettings Clone()
				=> (A7800SyncSettings)MemberwiseClone();

			public static bool NeedsReboot(A7800SyncSettings x, A7800SyncSettings y)
				=> !DeepEquality.DeepEquals(x, y);
		}
	}
}
