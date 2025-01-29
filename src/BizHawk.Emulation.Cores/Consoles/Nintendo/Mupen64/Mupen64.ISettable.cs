using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Mupen64;

public partial class Mupen64 : ISettable<object, Mupen64.SyncSettings>
{
	private SyncSettings _syncSettings;

	public enum CoreType
	{
		[Display(Name = "Pure Interpreter")]
		PureInterpreter = 0,
		[Display(Name = "Cached Interpreter")]
		CachedInterpreter = 1,
		[Display(Name = "Dynamic Recompiler (DynaRec)")]
		Dynarec = 2,
	}

	public enum N64ControllerPakType
	{
		[Display(Name = "None")]
		NoPak,
		[Display(Name = "Memory Card")]
		MemoryCard,
		[Display(Name = "Rumble Pak")]
		RumblePak,
		[Display(Name = "Transfer Pak")]
		TransferPak,
	}

	public enum N64VideoPlugin
	{
		[Display(Name = "paraLLEl")]
		Parallel,
		[Display(Name = "GLideN64")]
		GlideN64,
		[Display(Name = "angrylion-plus")]
		AngrylionPlus,
	}

	public enum N64RspPlugin
	{
		[Display(Name = "paraLLEl")]
		Parallel,
		[Display(Name = "cxd4")]
		Cxd4,
		[Display(Name = "hle")]
		Hle,
	}

	public string RspPluginFileName(N64RspPlugin rspPlugin)
	{
		return rspPlugin switch
		{
			N64RspPlugin.Parallel => "parallel",
			N64RspPlugin.Cxd4 => "cxd4-sse2",
			N64RspPlugin.Hle => "hle",
			_ => "parallel",
		};
	}

	public string VideoPluginFileName(N64VideoPlugin videoPlugin)
	{
		return videoPlugin switch
		{
			N64VideoPlugin.Parallel => "parallel",
			N64VideoPlugin.GlideN64 => "GLideN64",
			N64VideoPlugin.AngrylionPlus => "angrylion-plus",
			_ => "parallel",
		};
	}

	[CoreSettings]
	public record class SyncSettings
	{
		[Description("Video plugin to be used. paraLLEl and angrylion-plus will not work with the hle rsp plugin")]
		[DefaultValue(N64VideoPlugin.Parallel)]
		[TypeConverter(typeof(DescribableEnumConverter))]
		[Category("General")]
		public N64VideoPlugin VideoPlugin { get; set; }

		[DisplayName("Rsp Plugin name")]
		[Description("Rsp plugin to be used. hle will not work with the paraLLEl and angrylion-plus video plugins")]
		[DefaultValue(N64RspPlugin.Parallel)]
		[TypeConverter(typeof(DescribableEnumConverter))]
		[Category("General")]
		public N64RspPlugin RspPlugin { get; set; }

		[Description("The mode to run the cpu in")]
		[DefaultValue(CoreType.Dynarec)]
		[TypeConverter(typeof(DescribableEnumConverter))]
		[Category("General")]
		public CoreType CoreType { get; set; }

		[Description("Disable 4MB expansion RAM pack. May be necessary for some games")]
		[DefaultValue(false)]
		[Category("General")]
		public bool DisableExpansionSlot { get; set; }

		[Description("Whether a controller is connected in port 1")]
		[DefaultValue(true)]
		[Category("General")]
		public bool Port1Connected { get; set; }
		[Description("Whether a controller is connected in port 2")]
		[DefaultValue(false)]
		[Category("General")]
		public bool Port2Connected { get; set; }
		[Description("Whether a controller is connected in port 3")]
		[DefaultValue(false)]
		[Category("General")]
		public bool Port3Connected { get; set; }
		[Description("Whether a controller is connected in port 4")]
		[DefaultValue(false)]
		[Category("General")]
		public bool Port4Connected { get; set; }

		[Description("The type of expansion pak inserted into the expansion port of the controller connected to port 1")]
		[DefaultValue(N64ControllerPakType.NoPak)]
		[TypeConverter(typeof(DescribableEnumConverter))]
		[Category("General")]
		public N64ControllerPakType Port1PakType { get; set; }
		[Description("The type of expansion pak inserted into the expansion port of the controller connected to port 2")]
		[DefaultValue(N64ControllerPakType.NoPak)]
		[TypeConverter(typeof(DescribableEnumConverter))]
		[Category("General")]
		public N64ControllerPakType Port2PakType { get; set; }
		[Description("The type of expansion pak inserted into the expansion port of the controller connected to port 3")]
		[DefaultValue(N64ControllerPakType.NoPak)]
		[TypeConverter(typeof(DescribableEnumConverter))]
		[Category("General")]
		public N64ControllerPakType Port3PakType { get; set; }
		[Description("The type of expansion pak inserted into the expansion port of the controller connected to port 4")]
		[DefaultValue(N64ControllerPakType.NoPak)]
		[TypeConverter(typeof(DescribableEnumConverter))]
		[Category("General")]
		public N64ControllerPakType Port4PakType { get; set; }

		// video-parallel settings
		[Description("Amount of rescaling: 1=None, 2=2x, 4=4x, 8=8x")]
		[DefaultValue(1)]
		[Category("Video Plugin: parallel")]
		public int UpscaleFactor { get; set; }

		[Description("Deinterlacing method. False=Bob, True=Weave")]
		[DefaultValue(false)]
		[Category("Video Plugin: parallel")]
		public bool DeinterlaceMode { get; set; }

		[Description("VI anti-aliasing, smooths polygon edges")]
		[DefaultValue(false)]
		[Category("Video Plugin: parallel")]
		public bool Antialiasing { get; set; }

		[Description("Allow VI divot filter, cleans up stray black pixels")]
		[DefaultValue(true)]
		[Category("Video Plugin: parallel")]
		public bool Divot { get; set; }

		[Description("Allow VI gamma dither")]
		[DefaultValue(true)]
		[Category("Video Plugin: parallel")]
		public bool GammaDither { get; set; }

		[Description("Allow VI bilinear scaling")]
		[DefaultValue(true)]
		[Category("Video Plugin: parallel")]
		public bool BilinearScaling { get; set; }

		[Description("Allow VI dedither filter")]
		[DefaultValue(true)]
		[Category("Video Plugin: parallel")]
		public bool Dedither { get; set; }

		// video-angrylion-plus settings
		[Description("Distribute rendering between multiple processors if True")]
		[DefaultValue(true)]
		[Category("Video Plugin: angrylion-plus")]
		public bool ParallelRendering { get; set; }

		[Description("Scaling interpolation type (0=Blocky (Nearest-neighbor), 1=Blurry (Bilinear), 2=Soft (Bilinear + Nearest-neighbor))")]
		[DefaultValue(2)]
		[Category("Video Plugin: angrylion-plus")]
		public int InterpolationMode { get; set; }

		// video-GLideN64 settings
		[Description("Enable threaded video backend.")]
		[DefaultValue(false)]
		[Category("Video Plugin: GLideN64")]
		public bool ThreadedVideo { get; set; }

		[Description("Frame buffer size is the factor of N64 native resolution.")]
		[DefaultValue(0)]
		[Category("Video Plugin: GLideN64")]
		public int UseNativeResolutionFactor { get; set; }

		[Description("Enable hardware per-pixel lighting.")]
		[DefaultValue(false)]
		[Category("Video Plugin: GLideN64")]
		public bool EnableHWLighting { get; set; }

		[Description("Enable pixel coverage calculation. Used for better blending emulation and wire-frame mode. Needs fast GPU.")]
		[DefaultValue(false)]
		[Category("Video Plugin: GLideN64")]
		public bool EnableCoverage { get; set; }

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
