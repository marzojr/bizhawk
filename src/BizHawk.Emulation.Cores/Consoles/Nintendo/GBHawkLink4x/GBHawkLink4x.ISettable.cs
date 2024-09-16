using System.ComponentModel;
using System.Text.Json.Serialization;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawkLink4x
{
	public partial class GBHawkLink4x : IEmulator, IStatable, ISettable<GBHawkLink4x.GBLink4xSettings, GBHawkLink4x.GBLink4xSyncSettings>
	{
		public GBLink4xSettings GetSettings() => Link4xSettings.Clone();

		public GBLink4xSyncSettings GetSyncSettings() => Link4xSyncSettings.Clone();

		public PutSettingsDirtyBits PutSettings(GBLink4xSettings o)
		{
			Link4xSettings = o;
			return PutSettingsDirtyBits.None;
		}

		public PutSettingsDirtyBits PutSyncSettings(GBLink4xSyncSettings o)
		{
			bool ret = GBLink4xSyncSettings.NeedsReboot(Link4xSyncSettings, o);
			Link4xSyncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		private GBLink4xSettings Link4xSettings = new GBLink4xSettings();
		public GBLink4xSyncSettings Link4xSyncSettings = new GBLink4xSyncSettings();

		[CoreSettings]
		public class GBLink4xSettings
		{
			[DisplayName("Color Mode A")]
			[Description("Pick Between Green scale and Grey scale colors")]
			[DefaultValue(GBHawk.GBHawk.GBSettings.PaletteType.BW)]
			public GBHawk.GBHawk.GBSettings.PaletteType Palette_A { get; set; }

			[DisplayName("Color Mode B")]
			[Description("Pick Between Green scale and Grey scale colors")]
			[DefaultValue(GBHawk.GBHawk.GBSettings.PaletteType.BW)]
			public GBHawk.GBHawk.GBSettings.PaletteType Palette_B { get; set; }

			[DisplayName("Color Mode C")]
			[Description("Pick Between Green scale and Grey scale colors")]
			[DefaultValue(GBHawk.GBHawk.GBSettings.PaletteType.BW)]
			public GBHawk.GBHawk.GBSettings.PaletteType Palette_C { get; set; }

			[DisplayName("Color Mode D")]
			[Description("Pick Between Green scale and Grey scale colors")]
			[DefaultValue(GBHawk.GBHawk.GBSettings.PaletteType.BW)]
			public GBHawk.GBHawk.GBSettings.PaletteType Palette_D { get; set; }

			public enum AudioSrc
			{
				A,
				B,
				C,
				D,
				None
			}

			[DisplayName("Audio Selection")]
			[Description("Choose Audio Source. Both will produce Stereo sound.")]
			[DefaultValue(AudioSrc.A)]
			public AudioSrc AudioSet { get; set; }

			public GBLink4xSettings Clone() => (GBLink4xSettings)MemberwiseClone();

			public GBLink4xSettings() => SettingsUtil.SetDefaultValues(this);
		}

		[CoreSettings]
		public class GBLink4xSyncSettings
		{
			[DisplayName("Console Mode A")]
			[Description("Pick which console to run, 'Auto' chooses from ROM extension, 'GB' and 'GBC' chooses the respective system")]
			[DefaultValue(GBHawk.GBHawk.GBSyncSettings.ConsoleModeType.Auto)]
			public GBHawk.GBHawk.GBSyncSettings.ConsoleModeType ConsoleMode_A { get; set; }

			[DisplayName("Console Mode B")]
			[Description("Pick which console to run, 'Auto' chooses from ROM extension, 'GB' and 'GBC' chooses the respective system")]
			[DefaultValue(GBHawk.GBHawk.GBSyncSettings.ConsoleModeType.Auto)]
			public GBHawk.GBHawk.GBSyncSettings.ConsoleModeType ConsoleMode_B { get; set; }

			[DisplayName("Console Mode C")]
			[Description("Pick which console to run, 'Auto' chooses from ROM extension, 'GB' and 'GBC' chooses the respective system")]
			[DefaultValue(GBHawk.GBHawk.GBSyncSettings.ConsoleModeType.Auto)]
			public GBHawk.GBHawk.GBSyncSettings.ConsoleModeType ConsoleMode_C { get; set; }

			[DisplayName("Console Mode D")]
			[Description("Pick which console to run, 'Auto' chooses from ROM extension, 'GB' and 'GBC' chooses the respective system")]
			[DefaultValue(GBHawk.GBHawk.GBSyncSettings.ConsoleModeType.Auto)]
			public GBHawk.GBHawk.GBSyncSettings.ConsoleModeType ConsoleMode_D { get; set; }

			[DisplayName("CGB in GBA")]
			[Description("Emulate GBA hardware running a CGB game, instead of CGB hardware.  Relevant only for titles that detect the presense of a GBA, such as Shantae.")]
			[DefaultValue(false)]
			public bool GBACGB { get; set; }

			[DisplayName("RTC Initial Time A")]
			[Description("Set the initial RTC time in terms of elapsed seconds.")]
			[DefaultValue(0)]
			public int RTCInitialTime_A
			{
				get => _RTCInitialTime_A;
				set => _RTCInitialTime_A = Math.Max(0, Math.Min(1024 * 24 * 60 * 60, value));
			}

			[DisplayName("RTC Initial Time B")]
			[Description("Set the initial RTC time in terms of elapsed seconds.")]
			[DefaultValue(0)]
			public int RTCInitialTime_B
			{
				get => _RTCInitialTime_B;
				set => _RTCInitialTime_B = Math.Max(0, Math.Min(1024 * 24 * 60 * 60, value));
			}

			[DisplayName("RTC Initial Time C")]
			[Description("Set the initial RTC time in terms of elapsed seconds.")]
			[DefaultValue(0)]
			public int RTCInitialTime_C
			{
				get => _RTCInitialTime_C;
				set => _RTCInitialTime_C = Math.Max(0, Math.Min(1024 * 24 * 60 * 60, value));
			}

			[DisplayName("RTC Initial Time D")]
			[Description("Set the initial RTC time in terms of elapsed seconds.")]
			[DefaultValue(0)]
			public int RTCInitialTime_D
			{
				get => _RTCInitialTime_D;
				set => _RTCInitialTime_D = Math.Max(0, Math.Min(1024 * 24 * 60 * 60, value));
			}

			[DisplayName("RTC Offset A")]
			[Description("Set error in RTC clocking (-127 to 127)")]
			[DefaultValue(0)]
			public int RTCOffset_A
			{
				get => _RTCOffset_A;
				set => _RTCOffset_A = Math.Max(-127, Math.Min(127, value));
			}

			[DisplayName("RTC Offset B")]
			[Description("Set error in RTC clocking (-127 to 127)")]
			[DefaultValue(0)]
			public int RTCOffset_B
			{
				get => _RTCOffset_B;
				set => _RTCOffset_B = Math.Max(-127, Math.Min(127, value));
			}

			[DisplayName("RTC Offset C")]
			[Description("Set error in RTC clocking (-127 to 127)")]
			[DefaultValue(0)]
			public int RTCOffset_C
			{
				get => _RTCOffset_C;
				set => _RTCOffset_C = Math.Max(-127, Math.Min(127, value));
			}

			[DisplayName("RTC Offset D")]
			[Description("Set error in RTC clocking (-127 to 127)")]
			[DefaultValue(0)]
			public int RTCOffset_D
			{
				get => _RTCOffset_D;
				set => _RTCOffset_D = Math.Max(-127, Math.Min(127, value));
			}

			[DisplayName("Use Existing SaveRAM")]
			[Description("(Intended for development, for regular use leave as true.) When true, existing SaveRAM will be loaded at boot up.")]
			[DefaultValue(true)]
			public bool Use_SRAM { get; set; }

			private int _RTCInitialTime_A;
			private int _RTCInitialTime_B;
			private int _RTCInitialTime_C;
			private int _RTCInitialTime_D;
			private int _RTCOffset_A;
			private int _RTCOffset_B;
			private int _RTCOffset_C;
			private int _RTCOffset_D;
			[JsonIgnore]
			public ushort _DivInitialTime_A = 8;
			[JsonIgnore]
			public ushort _DivInitialTime_B = 8;
			[JsonIgnore]
			public ushort _DivInitialTime_C = 8;
			[JsonIgnore]
			public ushort _DivInitialTime_D = 8;

			public GBLink4xSyncSettings Clone() => (GBLink4xSyncSettings)MemberwiseClone();

			public GBLink4xSyncSettings() => SettingsUtil.SetDefaultValues(this);
			public static bool NeedsReboot(GBLink4xSyncSettings x, GBLink4xSyncSettings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}
		}
	}
}
