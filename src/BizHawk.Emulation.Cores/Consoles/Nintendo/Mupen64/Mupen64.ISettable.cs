using System;
using System.ComponentModel;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Mupen64;

public partial class Mupen64 : ISettable<object, Mupen64.SyncSettings>
{
	private SyncSettings _syncSettings;
	
	public enum CoreType
	{
		[Description("Pure Interpreter")]
		PureInterpreter = 0,

		[Description("Cached Interpreter")]
		CachedInterpreter = 1,

		[Description("Dynamic Recompiler (DynaRec)")]
		Dynarec = 2,
	}

	public enum N64ControllerPakType
	{
		[Description("None")]
		NoPak,
		[Description("Memory Card")]
		MemoryCard,
		[Description("Rumble Pak")]
		RumblePak,
		[Description("Transfer Pak")]
		TransferPak,
	}

	[CoreSettings]
	public record SyncSettings
	{
		[DisplayName("Video Plugin name")]
		[Description("Name of the video plugin to use, e.g. \"angrylion-plus\"")]
		[DefaultValue("GLideN64")]
		public string VideoPlugin { get; set; }

		[DisplayName("Rsp Plugin name")]
		[Description("Name of the rsp plugin to use, e.g. \"hle\"")]
		[DefaultValue("hle")]
		public string RspPlugin { get; set; }

		[Description("The mode to run the cpu in")]
		[DefaultValue(CoreType.Dynarec)]
		[TypeConverter(typeof(DescribableEnumConverter))]
		public CoreType CoreType { get; set; }

		[Description("Disable 4MB expansion RAM pack. May be necessary for some games")]
		[DefaultValue(false)]
		public bool DisableExpansionSlot { get; set; }

		[Description("Whether a controller is connected in port 1")]
		[DefaultValue(true)]
		public bool Port1Connected { get; set; }
		[Description("Whether a controller is connected in port 2")]
		[DefaultValue(false)]
		public bool Port2Connected { get; set; }
		[Description("Whether a controller is connected in port 3")]
		[DefaultValue(false)]
		public bool Port3Connected { get; set; }
		[Description("Whether a controller is connected in port 4")]
		[DefaultValue(false)]
		public bool Port4Connected { get; set; }

		[Description("The type of expansion pack inserted into the expansion port of the controller connected to port 1")]
		[DefaultValue(N64ControllerPakType.NoPak)]
		[TypeConverter(typeof(DescribableEnumConverter))]
		public N64ControllerPakType Port1PakType { get; set; }
		[Description("The type of expansion pack inserted into the expansion port of the controller connected to port 2")]
		[DefaultValue(N64ControllerPakType.NoPak)]
		[TypeConverter(typeof(DescribableEnumConverter))]
		public N64ControllerPakType Port2PakType { get; set; }
		[Description("The type of expansion pack inserted into the expansion port of the controller connected to port 3")]
		[DefaultValue(N64ControllerPakType.NoPak)]
		[TypeConverter(typeof(DescribableEnumConverter))]
		public N64ControllerPakType Port3PakType { get; set; }
		[Description("The type of expansion pack inserted into the expansion port of the controller connected to port 4")]
		[DefaultValue(N64ControllerPakType.NoPak)]
		[TypeConverter(typeof(DescribableEnumConverter))]
		public N64ControllerPakType Port4PakType { get; set; }

		public SyncSettings() => SettingsUtil.SetDefaultValues(this);
	}

	public object GetSettings() => null;

	public SyncSettings GetSyncSettings()
	{
		return _syncSettings with { };
	}

	public PutSettingsDirtyBits PutSettings(object o) => throw new InvalidOperationException("This core does not have any (non-sync) settings");

	public PutSettingsDirtyBits PutSyncSettings(SyncSettings o)
	{
		var ret = PutSettingsDirtyBits.None;
		if (_syncSettings != o) ret = PutSettingsDirtyBits.RebootCore;
		_syncSettings = o;
		return ret;
	}
}
