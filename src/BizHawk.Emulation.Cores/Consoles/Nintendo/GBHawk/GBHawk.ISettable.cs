using System.ComponentModel;
using System.Text.Json.Serialization;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawk
{
	public partial class GBHawk : IEmulator, ISettable<GBHawk.GBSettings, GBHawk.GBSyncSettings>
	{
		public GBSettings GetSettings()
		{
			return _settings.Clone();
		}

		public GBSyncSettings GetSyncSettings()
		{
			return _syncSettings.Clone();
		}

		public PutSettingsDirtyBits PutSettings(GBSettings o)
		{
			_settings = o;
			_disassembler.UseRGBDSSyntax = _settings.UseRGBDSSyntax;
			return PutSettingsDirtyBits.None;
		}

		public PutSettingsDirtyBits PutSyncSettings(GBSyncSettings o)
		{
			bool ret = GBSyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		public GBSettings _settings = new GBSettings();
		public GBSyncSettings _syncSettings = new GBSyncSettings();

		[CoreSettings]
		public class GBSettings
		{
			public enum PaletteType
			{
				BW,
				Gr
			}

			public enum Cycle_Return
			{
				CPU,
				GBI
			}

			[DisplayName("Color Mode")]
			[Description("Pick Between Green scale and Grey scale colors")]
			[DefaultValue(PaletteType.BW)]
			public PaletteType Palette { get; set; }

			[DisplayName("Read Domains on VBlank")]
			[Description("When true, memory domains are only updated on VBlank. More consistent for LUA. NOTE: Does not work for system bus, does not apply to writes.")]
			[DefaultValue(false)]
			public bool VBL_sync { get; set; }

			[DisplayName("TotalExecutedCycles Return Value")]
			[Description("CPU returns the actual CPU cycles executed, GBI returns the values needed for console verification")]
			[DefaultValue(Cycle_Return.CPU)]
			public Cycle_Return cycle_return_setting { get; set; }

			[DisplayName("Use RGBDS syntax")]
			[Description("When true, rgbds' syntax is used for disassembly.")]
			[DefaultValue(true)]
			public bool UseRGBDSSyntax { get; set; }

			public GBSettings Clone()
			{
				return (GBSettings)MemberwiseClone();
			}

			public GBSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}
		}

		[CoreSettings]
		public class GBSyncSettings
		{
			public enum ConsoleModeType
			{
				Auto,
				GB,
				GBC
			}

			[DisplayName("Console Mode")]
			[Description("Pick which console to run, 'Auto' chooses from ROM extension, 'GB' and 'GBC' chooses the respective system")]
			[DefaultValue(ConsoleModeType.Auto)]
			public ConsoleModeType ConsoleMode { get; set; }

			[DisplayName("CGB in GBA")]
			[Description("Emulate GBA hardware running a CGB game, instead of CGB hardware.  Relevant only for titles that detect the presense of a GBA, such as Shantae.")]
			[DefaultValue(false)]
			public bool GBACGB { get; set; }

			[DisplayName("RTC Initial Time")]
			[Description("Set the initial RTC time in terms of elapsed seconds.")]
			[DefaultValue(0)]
			public int RTCInitialTime
			{
				get => _RTCInitialTime;
				set => _RTCInitialTime = Math.Max(0, Math.Min(1024 * 24 * 60 * 60, value));
			}

			[DisplayName("RTC Offset")]
			[Description("Set error in RTC clocking (-127 to 127)")]
			[DefaultValue(0)]
			public int RTCOffset
			{
				get => _RTCOffset;
				set => _RTCOffset = Math.Max(-127, Math.Min(127, value));
			}

			[DisplayName("Use Existing SaveRAM")]
			[Description("(Intended for development, for regular use leave as true.) When true, existing SaveRAM will be loaded at boot up.")]
			[DefaultValue(true)]
			public bool Use_SRAM { get; set; }

			private int _RTCInitialTime;
			private int _RTCOffset;
			[JsonIgnore]
			public ushort _DivInitialTime = 8;

			public GBSyncSettings Clone()
			{
				return (GBSyncSettings)MemberwiseClone();
			}

			public GBSyncSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}

			public static bool NeedsReboot(GBSyncSettings x, GBSyncSettings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}
		}
	}
}
