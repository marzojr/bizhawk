using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Sameboy
{
	public partial class Sameboy : ISettable<Sameboy.SameboySettings, Sameboy.SameboySyncSettings>
	{
		private SameboySettings _settings;
		private SameboySyncSettings _syncSettings;

		public SameboySettings GetSettings() => _settings.Clone();

		public PutSettingsDirtyBits PutSettings(SameboySettings o)
		{
			var settings = new LibSameboy.NativeSettings
			{
				Palette = o.GBPalette,
				CustomPalette = o.GetCustomPalette(),
				ColorCorrectionMode = o.ColorCorrection,
				LightTemperature = o.LightTemperature,
				HighPassFilter = o.HighPassFilter,
				InterferenceVolume = o.InterferenceVolume,
				ChannelMask = o.GetChannelMask(),
				BackgroundEnabled = o.EnableBGWIN,
				ObjectsEnabled = o.EnableOBJ,
			};
			LibSameboy.sameboy_setsettings(SameboyState, ref settings);
			_disassembler.UseRGBDSSyntax = o.UseRGBDSSyntax;
			_settings = o;
			return PutSettingsDirtyBits.None;
		}

		public SameboySyncSettings GetSyncSettings() => _syncSettings.Clone();

		public PutSettingsDirtyBits PutSyncSettings(SameboySyncSettings o)
		{
			bool ret = SameboySyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		[CoreSettings]
		public class SameboySettings
		{
			public enum GBPaletteType : uint
			{
				[Display(Name = "Greyscale")]
				GREY,
				[Display(Name = "Lime (Game Boy)")]
				DMG,
				[Display(Name = "Olive (Game Boy Pocket)")]
				MGB,
				[Display(Name = "Teal (Game Boy Light)")]
				GBL,
				[Display(Name = "Custom")]
				CUSTOM,
			}

			[JsonInclude]
			private int[] _customPal;

			[DisplayName("GB Mono Palette")]
			[Description("Selects which palette to use in GB mode. Does nothing in GBC mode.")]
			[DefaultValue(GBPaletteType.GREY)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public GBPaletteType GBPalette { get; set; }

			public enum ColorCorrectionMode : uint
			{
				[Display(Name = "Disabled")]
				DISABLED,
				[Display(Name = "Correct Color Curves")]
				CORRECT_CURVES,
				[Display(Name = "Modern - Balanced")]
				MODERN_BALANCED,
				[Display(Name = "Modern - Boost Contrast")]
				MODERN_BOOST_CONTRAST,
				[Display(Name = "Reduce Contrast")]
				REDUCE_CONTRAST,
				[Display(Name = "Harsh Reality")]
				LOW_CONTRAST,
				[Display(Name = "Modern - Accurate")]
				MODERN_ACCURATE,
			}

			[DisplayName("GBC Color Correction")]
			[Description("Selects which color correction method to use in GBC mode. Does nothing in GB mode.")]
			[DefaultValue(ColorCorrectionMode.MODERN_BALANCED)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public ColorCorrectionMode ColorCorrection { get; set; }

			private int _lighttemperature;

			[DisplayName("Ambient Light Temperature")]
			[Description("Simulates an ambient light's effect on non-backlit screens. Does nothing in GB mode.")]
			[DefaultValue(0)]
			public int LightTemperature
			{
				get => _lighttemperature;
				set => _lighttemperature = Math.Max(-10, Math.Min(10, value));
			}

			[DisplayName("Show Border")]
			[Description("")]
			[DefaultValue(false)]
			public bool ShowBorder { get; set; }

			public enum HighPassFilterMode : uint
			{
				[Display(Name = "None (Keep DC Offset)")]
				HIGHPASS_OFF,
				[Display(Name = "Accurate")]
				HIGHPASS_ACCURATE,
				[Display(Name = "Preserve Waveform")]
				HIGHPASS_REMOVE_DC_OFFSET,
			}

			[DisplayName("High Pass Filter")]
			[Description("Selects which high pass filter to use for audio.")]
			[DefaultValue(HighPassFilterMode.HIGHPASS_ACCURATE)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public HighPassFilterMode HighPassFilter { get; set; }

			private int _interferencevolume;

			[DisplayName("Audio Interference Volume")]
			[Description("Sets the volume of audio interference.")]
			[DefaultValue(0)]
			public int InterferenceVolume
			{
				get => _interferencevolume;
				set => _interferencevolume = Math.Max(0, Math.Min(100, value));
			}

			[DisplayName("Enable Channel 1")]
			[Description("")]
			[DefaultValue(true)]
			public bool EnableCH1 { get; set; }

			[DisplayName("Enable Channel 2")]
			[Description("")]
			[DefaultValue(true)]
			public bool EnableCH2 { get; set; }

			[DisplayName("Enable Channel 3")]
			[Description("")]
			[DefaultValue(true)]
			public bool EnableCH3 { get; set; }

			[DisplayName("Enable Channel 4")]
			[Description("")]
			[DefaultValue(true)]
			public bool EnableCH4 { get; set; }

			public int GetChannelMask()
				=> (EnableCH1 ? 1 : 0) | (EnableCH2 ? 2 : 0) | (EnableCH3 ? 4 : 0) | (EnableCH4 ? 8 : 0);

			[DisplayName("Enable Background/Window")]
			[Description("")]
			[DefaultValue(true)]
			public bool EnableBGWIN { get; set; }

			[DisplayName("Enable Objects")]
			[Description("")]
			[DefaultValue(true)]
			public bool EnableOBJ { get; set; }

			[DisplayName("Use RGBDS Syntax")]
			[Description("Uses RGBDS syntax for disassembling.")]
			[DefaultValue(true)]
			public bool UseRGBDSSyntax { get; set; }

			public SameboySettings()
			{
				SettingsUtil.SetDefaultValues(this);
				_customPal = new[] { 0x00ffffff, 0x00aaaaaa, 0x00555555, 0x00000000, 0x00ffffff, };
			}

			public SameboySettings Clone()
				=> (SameboySettings)MemberwiseClone();

			public int[] GetCustomPalette() 
				=> (int[])_customPal.Clone();

			public void SetCustomPalette(int[] pal)
				=> _customPal = (int[])pal.Clone();
		}

		[CoreSettings]
		public class SameboySyncSettings
		{
			[DisplayName("Use official BIOS")]
			[Description("When false, SameBoy's internal bios is used. The official bios should be used for TASing.")]
			[DefaultValue(false)]
			public bool EnableBIOS { get; set; }

			public enum GBModel : int
			{
				Auto = -1,
				// GB_MODEL_DMG_0 = 0x000,
				// GB_MODEL_DMG_A = 0x001,
				[Display(Name = "DMG-B")]
				GB_MODEL_DMG_B = 0x002,
				// GB_MODEL_DMG_C = 0x003,
				[Display(Name = "MGB")]
				GB_MODEL_MGB = 0x100,
				[Display(Name = "CGB-0 (Experimental)")]
				GB_MODEL_CGB_0 = 0x200,
				[Display(Name = "CGB-A (Experimental)")]
				GB_MODEL_CGB_A = 0x201,
				[Display(Name = "CGB-B (Experimental)")]
				GB_MODEL_CGB_B = 0x202,
				[Display(Name = "CGB-C (Experimental)")]
				GB_MODEL_CGB_C = 0x203,
				[Display(Name = "CGB-D")]
				GB_MODEL_CGB_D = 0x204,
				[Display(Name = "CGB-E")]
				GB_MODEL_CGB_E = 0x205,
				// GB_MODEL_AGB_0 = 0x206,
				// GB_MODEL_AGB_A = 0x207,
				// GB_MODEL_GBP_A = 0x207 | 0x20,
				[Display(Name = "AGB")]
				GB_MODEL_AGB = 0x207,
				[Display(Name = "GBP")]
				GB_MODEL_GBP = 0x207 | 0x20,
				// GB_MODEL_AGB_B = 0x208,
				// GB_MODEL_AGB_E = 0x209,
				// GB_MODEL_GBP_E = 0x209 | 0x20,
			}

			[DisplayName("Console Mode")]
			[Description("Pick which console to run, 'Auto' chooses from ROM header. DMG-B, CGB-E, and AGB are the best options for GB, GBC, and GBA, respectively.")]
			[DefaultValue(GBModel.Auto)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public GBModel ConsoleMode { get; set; }

			[DisplayName("Use Real Time")]
			[Description("If true, RTC clock will be based off of real time instead of emulated time. Ignored (set to false) when recording a movie.")]
			[DefaultValue(false)]
			public bool UseRealTime { get; set; }

			[DisplayName("RTC Divisor Offset")]
			[Description("CPU clock frequency relative to real time clock. Base value is 2^22 Hz. Used in cycle-based RTC to sync on real hardware to account for RTC imperfections.")]
			[DefaultValue(0)]
			public int RTCDivisorOffset { get; set; }

			[DisplayName("Disable Joypad Bounce")]
			[Description("Disables emulation of the bounce from a physical joypad.")]
			[DefaultValue(true)]
			public bool NoJoypadBounce { get; set; }

			public SameboySyncSettings()
				=> SettingsUtil.SetDefaultValues(this);

			public SameboySyncSettings Clone()
				=> (SameboySyncSettings)MemberwiseClone();

			public static bool NeedsReboot(SameboySyncSettings x, SameboySyncSettings y)
				=> !DeepEquality.DeepEquals(x, y);
		}
	}
}
