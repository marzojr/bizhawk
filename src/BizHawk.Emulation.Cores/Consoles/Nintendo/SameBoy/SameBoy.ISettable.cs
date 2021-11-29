﻿using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

using Newtonsoft.Json;

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
			LibSameboy.sameboy_setpalette(SameboyState, o.GBPalette);
			LibSameboy.sameboy_setcolorcorrection(SameboyState, o.ColorCorrection);
			LibSameboy.sameboy_setlighttemperature(SameboyState, o.LightTemperature);
			LibSameboy.sameboy_sethighpassfilter(SameboyState, o.HighPassFilter);
			LibSameboy.sameboy_setinterferencevolume(SameboyState, o.InterferenceVolume);
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
			}

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
				[Display(Name = "Emulate Hardware")]
				EMULATE_HARDWARE,
				[Display(Name = "Preserve Brightness")]
				PRESERVE_BRIGHTNESS,
				[Display(Name = "Reduce Contrast")]
				REDUCE_CONTRAST,
				[Display(Name = "Harsh Reality")]
				LOW_CONTRAST,
			}

			[DisplayName("GBC Color Correction")]
			[Description("Selects which color correction method to use in GBC mode. Does nothing in GB mode.")]
			[DefaultValue(ColorCorrectionMode.EMULATE_HARDWARE)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public ColorCorrectionMode ColorCorrection { get; set; }

			[JsonIgnore]
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

			[JsonIgnore]
			private int _interferencevolume;

			[DisplayName("Audio Interference Volume")]
			[Description("Sets the volume of audio interference.")]
			[DefaultValue(0)]
			public int InterferenceVolume
			{
				get => _interferencevolume;
				set => _interferencevolume = Math.Max(0, Math.Min(100, value));
			}

			public SameboySettings() => SettingsUtil.SetDefaultValues(this);

			public SameboySettings Clone() => MemberwiseClone() as SameboySettings;
		}

		public class SameboySyncSettings
		{
			[DisplayName("Use official BIOS")]
			[Description("When false, SameBoy's internal bios is used. The official bios should be used for TASing.")]
			[DefaultValue(false)]
			public bool EnableBIOS { get; set; }

			public enum ConsoleModeType
			{
				Auto,
				GB,
				GBC,
				GBA
			}

			[DisplayName("Console Mode")]
			[Description("Pick which console to run, 'Auto' chooses from ROM header; 'GB', 'GBC', and 'GBA' chooses the respective system.")]
			[DefaultValue(ConsoleModeType.Auto)]
			public ConsoleModeType ConsoleMode { get; set; }

			[DisplayName("Use Real Time")]
			[Description("If true, RTC clock will be based off of real time instead of emulated time. Ignored (set to false) when recording a movie.")]
			[DefaultValue(false)]
			public bool UseRealTime { get; set; }

			public SameboySyncSettings() => SettingsUtil.SetDefaultValues(this);

			public SameboySyncSettings Clone() => MemberwiseClone() as SameboySyncSettings;

			public static bool NeedsReboot(SameboySyncSettings x, SameboySyncSettings y) => !DeepEquality.DeepEquals(x, y);
		}
	}
}
