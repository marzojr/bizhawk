using System.ComponentModel;
using System.Text.Json.Serialization;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawkLink3x
{
	public partial class GBHawkLink3x : IEmulator, IStatable, ISettable<GBHawkLink3x.GBLink3xSettings, GBHawkLink3x.GBLink3xSyncSettings>
	{
		public GBLink3xSettings GetSettings() => Link3xSettings.Clone();

		public GBLink3xSyncSettings GetSyncSettings() => Link3xSyncSettings.Clone();

		public PutSettingsDirtyBits PutSettings(GBLink3xSettings o)
		{
			Link3xSettings = o;
			return PutSettingsDirtyBits.None;
		}

		public PutSettingsDirtyBits PutSyncSettings(GBLink3xSyncSettings o)
		{
			bool ret = GBLink3xSyncSettings.NeedsReboot(Link3xSyncSettings, o);
			Link3xSyncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		private GBLink3xSettings Link3xSettings = new GBLink3xSettings();
		private GBLink3xSyncSettings Link3xSyncSettings = new GBLink3xSyncSettings();

		[CoreSettings]
		public class GBLink3xSettings
		{
			[DisplayName("Color Mode")]
			[Description("Pick Between Green scale and Grey scale colors")]
			[DefaultValue(GBHawk.GBHawk.GBSettings.PaletteType.BW)]
			public GBHawk.GBHawk.GBSettings.PaletteType Palette_L { get; set; }

			[DisplayName("Color Mode")]
			[Description("Pick Between Green scale and Grey scale colors")]
			[DefaultValue(GBHawk.GBHawk.GBSettings.PaletteType.BW)]
			public GBHawk.GBHawk.GBSettings.PaletteType Palette_C { get; set; }

			[DisplayName("Color Mode")]
			[Description("Pick Between Green scale and Grey scale colors")]
			[DefaultValue(GBHawk.GBHawk.GBSettings.PaletteType.BW)]
			public GBHawk.GBHawk.GBSettings.PaletteType Palette_R { get; set; }

			public enum AudioSrc
			{
				Left,
				Center,
				Right,
				None
			}

			[DisplayName("Audio Selection")]
			[Description("Choose Audio Source. Both will produce Stereo sound.")]
			[DefaultValue(AudioSrc.Left)]
			public AudioSrc AudioSet { get; set; }

			public GBLink3xSettings Clone() => (GBLink3xSettings)MemberwiseClone();

			public GBLink3xSettings() => SettingsUtil.SetDefaultValues(this);
		}

		[CoreSettings]
		public class GBLink3xSyncSettings
		{
			[DisplayName("Console Mode L")]
			[Description("Pick which console to run, 'Auto' chooses from ROM extension, 'GB' and 'GBC' chooses the respective system")]
			[DefaultValue(GBHawk.GBHawk.GBSyncSettings.ConsoleModeType.Auto)]
			public GBHawk.GBHawk.GBSyncSettings.ConsoleModeType ConsoleMode_L { get; set; }

			[DisplayName("Console Mode C")]
			[Description("Pick which console to run, 'Auto' chooses from ROM extension, 'GB' and 'GBC' chooses the respective system")]
			[DefaultValue(GBHawk.GBHawk.GBSyncSettings.ConsoleModeType.Auto)]
			public GBHawk.GBHawk.GBSyncSettings.ConsoleModeType ConsoleMode_C { get; set; }

			[DisplayName("Console Mode R")]
			[Description("Pick which console to run, 'Auto' chooses from ROM extension, 'GB' and 'GBC' chooses the respective system")]
			[DefaultValue(GBHawk.GBHawk.GBSyncSettings.ConsoleModeType.Auto)]
			public GBHawk.GBHawk.GBSyncSettings.ConsoleModeType ConsoleMode_R { get; set; }

			[DisplayName("CGB in GBA")]
			[Description("Emulate GBA hardware running a CGB game, instead of CGB hardware.  Relevant only for titles that detect the presense of a GBA, such as Shantae.")]
			[DefaultValue(false)]
			public bool GBACGB { get; set; }

			[DisplayName("RTC Initial Time L")]
			[Description("Set the initial RTC time in terms of elapsed seconds.")]
			[DefaultValue(0)]
			public int RTCInitialTime_L
			{
				get => _RTCInitialTime_L;
				set => _RTCInitialTime_L = Math.Max(0, Math.Min(1024 * 24 * 60 * 60, value));
			}

			[DisplayName("RTC Initial Time C")]
			[Description("Set the initial RTC time in terms of elapsed seconds.")]
			[DefaultValue(0)]
			public int RTCInitialTime_C
			{
				get => _RTCInitialTime_C;
				set => _RTCInitialTime_C = Math.Max(0, Math.Min(1024 * 24 * 60 * 60, value));
			}

			[DisplayName("RTC Initial Time R")]
			[Description("Set the initial RTC time in terms of elapsed seconds.")]
			[DefaultValue(0)]
			public int RTCInitialTime_R
			{
				get => _RTCInitialTime_R;
				set => _RTCInitialTime_R = Math.Max(0, Math.Min(1024 * 24 * 60 * 60, value));
			}

			[DisplayName("RTC Offset L")]
			[Description("Set error in RTC clocking (-127 to 127)")]
			[DefaultValue(0)]
			public int RTCOffset_L
			{
				get => _RTCOffset_L;
				set => _RTCOffset_L = Math.Max(-127, Math.Min(127, value));
			}

			[DisplayName("RTC Offset C")]
			[Description("Set error in RTC clocking (-127 to 127)")]
			[DefaultValue(0)]
			public int RTCOffset_C
			{
				get => _RTCOffset_C;
				set => _RTCOffset_C = Math.Max(-127, Math.Min(127, value));
			}

			[DisplayName("RTC Offset R")]
			[Description("Set error in RTC clocking (-127 to 127)")]
			[DefaultValue(0)]
			public int RTCOffset_R
			{
				get => _RTCOffset_R;
				set => _RTCOffset_R = Math.Max(-127, Math.Min(127, value));
			}

			[DisplayName("Use Existing SaveRAM")]
			[Description("(Intended for development, for regular use leave as true.) When true, existing SaveRAM will be loaded at boot up.")]
			[DefaultValue(true)]
			public bool Use_SRAM { get; set; }

			private int _RTCInitialTime_L;
			private int _RTCInitialTime_C;
			private int _RTCInitialTime_R;
			private int _RTCOffset_L;
			private int _RTCOffset_C;
			private int _RTCOffset_R;
			[JsonIgnore]
			public ushort _DivInitialTime_L = 8;
			[JsonIgnore]
			public ushort _DivInitialTime_C = 8;
			[JsonIgnore]
			public ushort _DivInitialTime_R = 8;

			public GBLink3xSyncSettings Clone() => (GBLink3xSyncSettings)MemberwiseClone();

			public GBLink3xSyncSettings() => SettingsUtil.SetDefaultValues(this);

			public static bool NeedsReboot(GBLink3xSyncSettings x, GBLink3xSyncSettings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}
		}
	}
}
