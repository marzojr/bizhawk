using System.ComponentModel;
using System.Drawing;

using BizHawk.Emulation.Common;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	public partial class Atari2600 : ISettable<Atari2600.A2600Settings, Atari2600.A2600SyncSettings>
	{
		public A2600Settings GetSettings()
		{
			return Settings.Clone();
		}

		public A2600SyncSettings GetSyncSettings()
		{
			return SyncSettings.Clone();
		}

		public PutSettingsDirtyBits PutSettings(A2600Settings o)
		{
			if (Settings == null || Settings.SECAMColors != o.SECAMColors)
			{
				_tia?.SetSecam(o.SECAMColors);
			}

			Settings = o;
			return PutSettingsDirtyBits.None;
		}

		public PutSettingsDirtyBits PutSyncSettings(A2600SyncSettings o)
		{
			bool ret = A2600SyncSettings.NeedsReboot(SyncSettings, o);
			SyncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		internal A2600Settings Settings { get; private set; }
		internal A2600SyncSettings SyncSettings { get; private set; }

		[CoreSettings]
		public class A2600Settings
		{
			private int _ntscTopLine;

			private int _ntscBottomLine;

			private int _palTopLine;

			private int _palBottomLine;

			[DisplayName("Show Background")]
			[Description("Sets whether or not the Background layer will be displayed")]
			[DefaultValue(true)]
			public bool ShowBG { get; set; }

			[DisplayName("Show Player 1")]
			[Description("Sets whether or not the Player 1 layer will be displayed")]
			[DefaultValue(true)]
			public bool ShowPlayer1 { get; set; }

			[DisplayName("Show Player 2")]
			[Description("Sets whether or not the Player 2 layer will be displayed")]
			[DefaultValue(true)]
			public bool ShowPlayer2 { get; set; }

			[DisplayName("Show Missle 1")]
			[Description("Sets whether or not the Missle 1 layer will be displayed")]
			[DefaultValue(true)]
			public bool ShowMissle1 { get; set; }

			[DisplayName("Show Missle 2")]
			[Description("Sets whether or not the Missle 2 layer will be displayed")]
			[DefaultValue(true)]
			public bool ShowMissle2 { get; set; }

			[DisplayName("Show Ball")]
			[Description("Sets whether or not the Ball layer will be displayed")]
			[DefaultValue(true)]
			public bool ShowBall { get; set; }

			[DisplayName("Show Playfield")]
			[Description("Sets whether or not the Playfield layer will be displayed")]
			[DefaultValue(true)]
			public bool ShowPlayfield { get; set; }

			[DisplayName("SECAM Colors")]
			[Description("If true, PAL mode will show with SECAM (French) colors.")]
			[DefaultValue(false)]
			public bool SECAMColors { get; set; }

			[DisplayName("NTSC Top Line")]
			[Description("First line of the video image to display in NTSC mode.")]
			[DefaultValue(24)]
			public int NTSCTopLine
			{
				get => _ntscTopLine;
				set => _ntscTopLine = Math.Min(64, Math.Max(value, 0));
			}

			[DisplayName("NTSC Bottom Line")]
			[Description("Last line of the video image to display in NTSC mode.")]
			[DefaultValue(248)]
			public int NTSCBottomLine
			{
				get => _ntscBottomLine;
				set => _ntscBottomLine = Math.Min(260, Math.Max(value, 192));
			}

			[DisplayName("PAL Top Line")]
			[Description("First line of the video image to display in PAL mode.")]
			[DefaultValue(24)]
			public int PALTopLine
			{
				get => _palTopLine;
				set => _palTopLine = Math.Min(64, Math.Max(value, 0));
			}

			[DisplayName("PAL Bottom Line")]
			[Description("Last line of the video image to display in PAL mode.")]
			[DefaultValue(296)]
			public int PALBottomLine
			{
				get => _palBottomLine;
				set => _palBottomLine = Math.Min(310, Math.Max(value, 192));
			}

			[DisplayName("Background Color")]
			[DefaultValue(typeof(Color), "Black")]
			public Color BackgroundColor { get; set; }

			public A2600Settings Clone()
			{
				return (A2600Settings)MemberwiseClone();
			}

			public A2600Settings()
			{
				SettingsUtil.SetDefaultValues(this);
			}
		}

		[CoreSettings]
		public class A2600SyncSettings
		{
			[DefaultValue(Atari2600ControllerTypes.Joystick)]
			[DisplayName("Port 1 Device")]
			[Description("The type of controller plugged into the first controller port")]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public Atari2600ControllerTypes Port1 { get; set; } = Atari2600ControllerTypes.Joystick;

			[DefaultValue(Atari2600ControllerTypes.Joystick)]
			[DisplayName("Port 2 Device")]
			[Description("The type of controller plugged into the second controller port")]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public Atari2600ControllerTypes Port2 { get; set; } = Atari2600ControllerTypes.Joystick;

			[DisplayName("Black and White Mode")]
			[Description("Set the TV Type switch on the console to B&W or Color.  This only affects the displayed image if the game supports it.")]
			[DefaultValue(false)]
			public bool BW { get; set; }

			[DisplayName("Left Difficulty")]
			[Description("Set the Left Difficulty switch on the console")]
			[DefaultValue(true)]
			public bool LeftDifficulty { get; set; }

			[DisplayName("Right Difficulty")]
			[Description("Set the Right Difficulty switch on the console")]
			[DefaultValue(true)]
			public bool RightDifficulty { get; set; }

			[DisplayName("Super Charger BIOS Skip")]
			[Description("On Super Charger carts, this will skip the BIOS intro")]
			[DefaultValue(false)]
			public bool FastScBios { get; set; }

			public A2600SyncSettings Clone()
			{
				return (A2600SyncSettings)MemberwiseClone();
			}

			public A2600SyncSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}

			public static bool NeedsReboot(A2600SyncSettings x, A2600SyncSettings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}
		}
	}
}
