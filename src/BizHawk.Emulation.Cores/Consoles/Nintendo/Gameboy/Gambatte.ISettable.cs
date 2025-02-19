using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class Gameboy : ISettable<Gameboy.GambatteSettings, Gameboy.GambatteSyncSettings>
	{
		public GambatteSettings GetSettings()
		{
			return _settings.Clone();
		}

		public PutSettingsDirtyBits PutSettings(GambatteSettings o)
		{
			_settings = o;
			_disassembler.UseRGBDSSyntax = _settings.RgbdsSyntax;
			if (IsCGBMode || IsSgb)
			{
				SetCGBColors(_settings.CGBColors);
			}
			else
			{
				ChangeDMGColors(_settings.GBPalette);
			}

			return PutSettingsDirtyBits.None;
		}

		public GambatteSyncSettings GetSyncSettings()
		{
			return _syncSettings.Clone();
		}

		public PutSettingsDirtyBits PutSyncSettings(GambatteSyncSettings o)
		{
			bool ret = GambatteSyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		private GambatteSettings _settings;
		private GambatteSyncSettings _syncSettings;

		public class GambatteSettings
		{
			/* Green Palette
			private static readonly int[] DefaultPalette =
			{
				10798341, 8956165, 1922333, 337157,
				10798341, 8956165, 1922333, 337157,
				10798341, 8956165, 1922333, 337157
			};
			*/
			// Grey Scale Palette
			private static readonly int[] DefaultPalette =
			{
				0xFFFFFF, 0xAAAAAA, 0x555555, 0,
				0xFFFFFF, 0xAAAAAA, 0x555555, 0,
				0xFFFFFF, 0xAAAAAA, 0x555555, 0
			};

			public int[] GBPalette;
			public GBColors.ColorType CGBColors;

			/// <summary>
			/// true to mute all audio
			/// </summary>
			public bool Muted;
			
			/// <summary>
			/// true to use rgbds syntax
			/// </summary>
			public bool RgbdsSyntax;

			/// <summary>
			/// true to show sgb border (sgb mode only)
			/// </summary>
			public bool ShowBorder;

			public GambatteSettings()
			{
				GBPalette = (int[])DefaultPalette.Clone();
				CGBColors = GBColors.ColorType.sameboy;
				RgbdsSyntax = true;
				ShowBorder = true;
			}


			public GambatteSettings Clone()
			{
				var ret = (GambatteSettings)MemberwiseClone();
				ret.GBPalette = (int[])GBPalette.Clone();
				return ret;
			}
		}

		[CoreSettings]
		public class GambatteSyncSettings
		{
			[DisplayName("Use official Nintendo BootROM")]
			[Description("When false, hacks are used to boot without a BIOS. When true, a provided official BootROM (or \"BIOS\") is used. You must provide the BootROM. Should be used for TASing.")]
			[DefaultValue(false)]
			public bool EnableBIOS { get; set; }

			public enum ConsoleModeType
			{
				Auto,
				GB,
				GBC,
				GBA,
				SGB2,
			}

			[DisplayName("Console Mode")]
			[Description("Picks which console to emulate. Forced to be 'SGB2' when 'GB in SGB' is checked.")]
			[DefaultValue(ConsoleModeType.Auto)]
			public ConsoleModeType ConsoleMode { get; set; }

			[DisplayName("Cart Bus Pull-Up Time")]
			[Description("Time it takes for the cart bus to pull-up to 0xFF in CPU cycles. Used to account for differences in pull-up times between carts/consoles.")]
			[DefaultValue(8)]
			public int CartBusPullUpTime { get; set; }

			[DisplayName("Realtime RTC")]
			[Description("If true, the real time clock in MBC3 and HuC3 games will reflect real time, instead of emulated time.  Ignored (treated as false) when a movie is recording.")]
			[DefaultValue(false)]
			public bool RealTimeRTC { get; set; }

			[DisplayName("RTC Divisor Offset")]
			[Description("CPU clock frequency relative to real time clock. Base value is 2^22 Hz. Used in cycle-based RTC to sync on real hardware to account for RTC imperfections.")]
			[DefaultValue(0)]
			public int RTCDivisorOffset { get; set; }

			[DisplayName("Initial Time")]
			[Description("Initial time of emulation in seconds.")]
			[DefaultValue(typeof(ulong), "0")]
			public ulong InitialTime { get; set; }

			[DisplayName("Enable remote control")]
			[Description("Adds control for the command sent from a TV remote pointed to the system or cart IR.")]
			[DefaultValue(false)]
			public bool EnableRemote { get; set; }

			public enum FrameLengthType
			{
				[Display(Name = "VBlank Driven Frames")]
				VBlankDrivenFrames,
				[Display(Name = "Equal Length Frames")]
				EqualLengthFrames,
				[Display(Name = "User Defined Frames")]
				UserDefinedFrames
			}

			[DisplayName("Frame Length")]
			[Description("Sets how long an emulation frame will last.\nVBlank Driven Frames will make emulation frames sync to VBlank. Recommended for TASing.\nEqual Length Frames will force all frames to emit 35112 samples. Legacy, not recommended for TASing.\nUser Defined Frames allows for the user to define how many samples are emitted for each frame. Only useful if sub-frame input is desired.")]
			[DefaultValue(FrameLengthType.VBlankDrivenFrames)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public FrameLengthType FrameLength { get; set; }

			[DisplayName("Display BG")]
			[Description("Display background")]
			[DefaultValue(true)]
			public bool DisplayBG { get; set; }

			[DisplayName("Display OBJ")]
			[Description("Display objects")]
			[DefaultValue(true)]
			public bool DisplayOBJ { get; set; }

			[DisplayName("Display Window")]
			[Description("Display window")]
			[DefaultValue(true)]
			public bool DisplayWindow { get; set; }

			public GambatteSyncSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}

			public GambatteSyncSettings Clone()
			{
				return (GambatteSyncSettings)MemberwiseClone();
			}

			public static bool NeedsReboot(GambatteSyncSettings x, GambatteSyncSettings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}
		}
	}
}
