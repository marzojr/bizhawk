using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

using BizHawk.Common;
using BizHawk.Emulation.Common;

// ReSharper disable SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	public partial class NDS : ISettable<NDS.NDSSettings, NDS.NDSSyncSettings>
	{
		private NDSSettings _settings;
		private NDSSyncSettings _syncSettings;

		private readonly NDSSyncSettings _activeSyncSettings;

		public enum ScreenLayoutKind
		{
			Natural,
			Vertical,
			Horizontal,
			Hybrid,
			[Display(Name = "Top Only")]
			Top,
			[Display(Name = "Bottom Only")]
			Bottom,
		}

		public enum ScreenRotationKind
		{
			[Display(Name = "0°")]
			Rotate0,
			[Display(Name = "90°")]
			Rotate90,
			[Display(Name = "180°")]
			Rotate180,
			[Display(Name = "270°")]
			Rotate270,
		}

		[CoreSettings]
		public class NDSSettings
		{
			[DisplayName("Screen Layout")]
			[Description("Adjusts the layout of the screens. Natural will change between Vertical and Horizontal depending on Screen Rotation")]
			[DefaultValue(ScreenLayoutKind.Natural)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public ScreenLayoutKind ScreenLayout { get; set; }

			[DisplayName("Invert Screens")]
			[Description("Inverts the order of the screens.")]
			[DefaultValue(false)]
			public bool ScreenInvert { get; set; }

			[DisplayName("Rotation")]
			[Description("Adjusts the orientation of the screens")]
			[DefaultValue(ScreenRotationKind.Rotate0)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public ScreenRotationKind ScreenRotation { get; set; }

			private int _screengap;

			[DisplayName("Screen Gap")]
			[Description("Gap between the screens")]
			[DefaultValue(0)]
			public int ScreenGap
			{
				get => _screengap;
				set => _screengap = Math.Max(0, Math.Min(128, value));
			}

			public enum AudioBitDepthType : int
			{
				Auto,
				[Display(Name = "10")]
				Ten,
				[Display(Name = "16")]
				Sixteen,
			}

			[DisplayName("Audio Bit Depth")]
			[Description("Auto will set the audio bit depth most accurate to the console (10 for DS, 16 for DSi).")]
			[DefaultValue(AudioBitDepthType.Auto)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public AudioBitDepthType AudioBitDepth { get; set; }

			public enum AudioInterpolationType : int
			{
				None,
				Linear,
				Cosine,
				Cubic,
				[Display(Name = "Gaussian (SNES)")]
				SNESGaussian,
			}

			[DisplayName("Audio Interpolation")]
			[Description("Audio enhancement (original hardware has no audio interpolation).")]
			[DefaultValue(AudioInterpolationType.None)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public AudioInterpolationType AudioInterpolation { get; set; }

			[DisplayName("Alt Lag")]
			[Description("If true, touch screen polling and ARM7 key polling will be considered for lag frames. Otherwise, only ARM9 key polling will be considered.")]
			[DefaultValue(false)]
			public bool ConsiderAltLag { get; set; }

			[DisplayName("Trace ARM7 Thumb")]
			[Description("")]
			[DefaultValue(false)]
			public bool TraceArm7Thumb { get; set; }

			[DisplayName("Trace ARM7 ARM")]
			[Description("")]
			[DefaultValue(false)]
			public bool TraceArm7Arm { get; set; }

			[DisplayName("Trace ARM9 Thumb")]
			[Description("")]
			[DefaultValue(true)]
			public bool TraceArm9Thumb { get; set; }

			[DisplayName("Trace ARM9 ARM")]
			[Description("")]
			[DefaultValue(true)]
			public bool TraceArm9Arm { get; set; }

			public LibMelonDS.TraceMask GetTraceMask()
			{
				var ret = LibMelonDS.TraceMask.NONE;
				if (TraceArm7Thumb)
					ret |= LibMelonDS.TraceMask.ARM7_THUMB;
				if (TraceArm7Arm)
					ret |= LibMelonDS.TraceMask.ARM7_ARM;
				if (TraceArm9Thumb)
					ret |= LibMelonDS.TraceMask.ARM9_THUMB;
				if (TraceArm9Arm)
					ret |= LibMelonDS.TraceMask.ARM9_ARM;
				return ret;
			}

			public NDSSettings Clone()
				=> (NDSSettings)MemberwiseClone();

			public static bool NeedsScreenResize(NDSSettings x, NDSSettings y)
			{
				var ret = false;
				ret |= x.ScreenLayout != y.ScreenLayout;
				ret |= x.ScreenGap != y.ScreenGap;
				ret |= x.ScreenRotation != y.ScreenRotation;
				return ret;
			}

			public NDSSettings()
				=> SettingsUtil.SetDefaultValues(this);
		}

		private static readonly DateTime minDate = new(2000, 1, 1);
		private static readonly DateTime maxDate = new(2099, 12, 31, 23, 59, 59);

		[CoreSettings]
		public class NDSSyncSettings
		{
			public enum ThreeDeeRendererType : int
			{
				Software,
				[Display(Name = "OpenGL Classic")]
				OpenGL_Classic,
				[Display(Name = "OpenGL Compute")]
				OpenGL_Compute,
			}

			[DisplayName("3D Renderer")]
			[Description("Renderer used for 3D. OpenGL Classic requires at least OpenGL 3.2, OpenGL Compute requires at least OpenGL 4.3. Forced to Software when recording a movie.")]
			[DefaultValue(ThreeDeeRendererType.Software)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public ThreeDeeRendererType ThreeDeeRenderer { get; set; }

			[DisplayName("Threaded Software 3D Rendering")]
			[Description("Offloads 3D rendering to a separate thread. Only used for the software renderer.")]
			[DefaultValue(true)]
			public bool ThreadedRendering { get; set; }

			private int _glScaleFactor;

			[DisplayName("OpenGL Scale Factor")]
			[Description("Factor at which OpenGL upscales the final image. Not used for the software renderer.")]
			[DefaultValue(1)]
			public int GLScaleFactor
			{
				get => _glScaleFactor;
				set => _glScaleFactor = Math.Max(1, Math.Min(16, value));
			}

			[DisplayName("OpenGL Better Polygons")]
			[Description("Enhances polygon quality with OpenGL Classic. Not used for the software nor OpenGL Compute renderer.")]
			[DefaultValue(false)]
			public bool GLBetterPolygons { get; set; }

			[DisplayName("OpenGL Hi Res Coordinates")]
			[Description("Uses high resolution coordinates with OpenGL Compute. Not used for the software nor OpenGL Classic renderer.")]
			[DefaultValue(false)]
			public bool GLHiResCoordinates { get; set; }

			private DateTime _initialtime;

			[DisplayName("Initial Time")]
			[Description("Initial time of emulation. Not used if Use Real Time is true")]
			[DefaultValue(typeof(DateTime), "2010-01-01")]
			[TypeConverter(typeof(BizDateTimeConverter))]
			public DateTime InitialTime
			{
				get => _initialtime;
				set => _initialtime = value < minDate ? minDate : (value > maxDate ? maxDate : value);
			}

			[DisplayName("Use Real Time")]
			[Description("If true, the initial RTC clock will be based off of real time instead of the Initial Time setting. Ignored (set to false) when recording a movie.")]
			[DefaultValue(true)]
			public bool UseRealTime { get; set; }

			[DisplayName("DSi Mode")]
			[Description("If true, DSi mode will be used. Forced true if a DSiWare rom is detected.")]
			[DefaultValue(false)]
			public bool UseDSi { get; set; }

			[DisplayName("Use Real BIOS")]
			[Description("If true, real BIOS files will be used. Forced true for DSi.")]
			[DefaultValue(false)]
			public bool UseRealBIOS { get; set; }

			[DisplayName("Skip Firmware")]
			[Description("If true, initial firmware boot will be skipped. Forced true if firmware cannot be booted (no real bios or missing firmware).")]
			[DefaultValue(true)]
			public bool SkipFirmware { get; set; }

			[DisplayName("Firmware Override")]
			[Description("If true, the firmware settings will be overriden by provided settings. Forced true when recording a movie.")]
			[DefaultValue(true)]
			public bool FirmwareOverride { get; set; }

			[DisplayName("Clear NAND")]
			[Description("If true, the DSi NAND will have all its titles cleared. Forced true when recording a movie.")]
			[DefaultValue(true)]
			public bool ClearNAND { get; set; }

			public enum StartUp : int
			{
				[Display(Name = "Auto Boot")]
				AutoBoot,
				[Display(Name = "Manual Boot")]
				ManualBoot,
			}

			[DisplayName("Firmware Start-Up")]
			[Description("The way firmware is booted. Auto Boot will go to the game immediately, while Manual Boot will go into the firmware menu. Only applicable if firmware override is in effect.")]
			[DefaultValue(StartUp.AutoBoot)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public StartUp FirmwareStartUp { get; set; }

			private string _firmwareusername;

			[DisplayName("Firmware Username")]
			[Description("Username in firmware. Only applicable if firmware override is in effect.")]
			[DefaultValue("melonDS")]
			public string FirmwareUsername
			{
				get => _firmwareusername;
				set => _firmwareusername = value.Length != 0 ? value.Substring(0, Math.Min(10, value.Length)) : _firmwareusername.Substring(0, 1);
			}

			public enum Language : int
			{
				Japanese,
				English,
				French,
				German,
				Italian,
				Spanish,
				Chinese,
				Korean,
			}

			[DisplayName("Firmware Language")]
			[Description("Language in firmware. Only applicable if firmware override is in effect.")]
			[DefaultValue(Language.English)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public Language FirmwareLanguage { get; set; }

			public enum Month : int
			{
				January = 1,
				February,
				March,
				April,
				May,
				June,
				July,
				August,
				September,
				October,
				November,
				December,
			}

			private Month _firmwarebirthdaymonth;

			private int _firmwarebirthdayday;

			[DisplayName("Firmware Birthday Month")]
			[Description("Birthday month in firmware. Only applicable if firmware override is in effect.")]
			[DefaultValue(Month.November)]
			public Month FirmwareBirthdayMonth
			{
				get => _firmwarebirthdaymonth;
				set
				{
					FirmwareBirthdayDay = SanitizeBirthdayDay(FirmwareBirthdayDay, value);
					_firmwarebirthdaymonth = value;
				}
			}

			[DisplayName("Firmware Birthday Day")]
			[Description("Birthday day in firmware. Only applicable if firmware override is in effect.")]
			[DefaultValue(3)]
			public int FirmwareBirthdayDay
			{
				get => _firmwarebirthdayday;
				set => _firmwarebirthdayday = SanitizeBirthdayDay(value, FirmwareBirthdayMonth);
			}

			public enum Color : int
			{
				[Display(Name = "Greyish Blue")]
				GreyishBlue,
				Brown,
				Red,
				[Display(Name = "Light Pink")]
				LightPink,
				Orange,
				Yellow,
				Lime,
				[Display(Name = "Light Green")]
				LightGreen,
				[Display(Name = "Dark Green")]
				DarkGreen,
				Turqoise,
				[Display(Name = "Light Blue")]
				LightBlue,
				Blue,
				[Display(Name = "Dark Blue")]
				DarkBlue,
				[Display(Name = "Dark Purple")]
				DarkPurple,
				[Display(Name = "Light Purple")]
				LightPurple,
				[Display(Name = "Dark Pink")]
				DarkPink,
			}

			[DisplayName("Firmware Favorite Color")]
			[Description("Favorite color in firmware. Only applicable if firmware override is in effect.")]
			[DefaultValue(Color.Red)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public Color FirmwareFavouriteColour { get; set; }

			private string _firmwaremessage;

			[DisplayName("Firmware Message")]
			[Description("Message in firmware. Only applicable if firmware override is in effect.")]
			[DefaultValue("Melons Taste Great!")]
			public string FirmwareMessage
			{
				get => _firmwaremessage;
				set => _firmwaremessage = value.Substring(0, Math.Min(26, value.Length));
			}

			public unsafe void GetFirmwareSettings(out LibMelonDS.FirmwareSettings fwSettings)
			{
				fwSettings.OverrideSettings = FirmwareOverride;
				fwSettings.UsernameLength = Math.Min(FirmwareUsername.Length, 10);

				fixed (char* p = fwSettings.Username)
				{
					var username = new Span<char>(p, 10);
					username.Clear();

					FirmwareUsername
						.AsSpan()
						.Slice(0, fwSettings.UsernameLength)
						.CopyTo(username);
				}

				fwSettings.Language = FirmwareLanguage;
				fwSettings.BirthdayMonth = FirmwareBirthdayMonth;
				fwSettings.BirthdayDay = FirmwareBirthdayDay;
				fwSettings.Color = FirmwareFavouriteColour;
				fwSettings.MessageLength = Math.Min(FirmwareMessage.Length, 26);

				fixed (char* p = fwSettings.Message)
				{
					var message = new Span<char>(p, 26);
					message.Clear();

					FirmwareMessage
						.AsSpan()
						.Slice(0, fwSettings.MessageLength)
						.CopyTo(message);
				}

				// TODO make MAC configurable
				fwSettings.MacAddress[0] = 0x00;
				fwSettings.MacAddress[1] = 0x09;
				fwSettings.MacAddress[2] = 0xBF;
				fwSettings.MacAddress[3] = 0x0E;
				fwSettings.MacAddress[4] = 0x49;
				fwSettings.MacAddress[5] = 0x16;
			}

			public NDSSyncSettings Clone()
				=> (NDSSyncSettings)MemberwiseClone();

			public static bool NeedsReboot(NDSSyncSettings x, NDSSyncSettings y)
				=> !DeepEquality.DeepEquals(x, y);

			public NDSSyncSettings()
				=> SettingsUtil.SetDefaultValues(this);
		}

		public NDSSettings GetSettings()
			=> _settings.Clone();

		public NDSSyncSettings GetSyncSettings()
			=> _syncSettings.Clone();

		private void RefreshScreenSettings(NDSSettings settings)
		{
			var screenSettings = new LibMelonDS.ScreenSettings
			{
				ScreenLayout = settings.ScreenLayout switch
				{
					ScreenLayoutKind.Natural => LibMelonDS.ScreenLayout.Natural,
					ScreenLayoutKind.Vertical => LibMelonDS.ScreenLayout.Vertical,
					ScreenLayoutKind.Horizontal => LibMelonDS.ScreenLayout.Horizontal,
					ScreenLayoutKind.Hybrid => LibMelonDS.ScreenLayout.Hybrid,
					_ => LibMelonDS.ScreenLayout.Natural,
				},
				ScreenRotation = settings.ScreenRotation switch
				{
					ScreenRotationKind.Rotate0 => LibMelonDS.ScreenRotation.Deg0,
					ScreenRotationKind.Rotate90 => LibMelonDS.ScreenRotation.Deg90,
					ScreenRotationKind.Rotate180 => LibMelonDS.ScreenRotation.Deg180,
					ScreenRotationKind.Rotate270 => LibMelonDS.ScreenRotation.Deg270,
					_ => LibMelonDS.ScreenRotation.Deg0,
				},
				ScreenSizing = settings.ScreenLayout switch
				{
					ScreenLayoutKind.Top => LibMelonDS.ScreenSizing.TopOnly,
					ScreenLayoutKind.Bottom => LibMelonDS.ScreenSizing.BotOnly,
					_ => LibMelonDS.ScreenSizing.Even,
				},
				ScreenGap = Math.Max(0, Math.Min(settings.ScreenGap, 128)),
				ScreenSwap = settings.ScreenInvert
			};

			_openGLProvider.ActivateGLContext(_glContext); // SetScreenSettings will re-present the frame, so needs OpenGL context active
			_core.SetScreenSettings(_console, ref screenSettings, out var w , out var h, out var vw, out var vh);

			BufferWidth = w;
			BufferHeight = h;
			_glTextureProvider.VirtualWidth = vw;
			_glTextureProvider.VirtualHeight = vh;
			_glTextureProvider.VideoDirty = true;
		}

		public PutSettingsDirtyBits PutSettings(NDSSettings o)
		{
			var ret = NDSSettings.NeedsScreenResize(_settings, o);

			// ScreenInvert changing won't need a screen resize
			// but it will change the underlying image
			if (_glContext != null && (ret || _settings.ScreenInvert != o.ScreenInvert))
			{
				RefreshScreenSettings(o);
			}

			_core.SetSoundConfig(_console, o.AudioBitDepth, o.AudioInterpolation);

			_settings = o;
			return ret ? PutSettingsDirtyBits.ScreenLayoutChanged : PutSettingsDirtyBits.None;
		}

		public PutSettingsDirtyBits PutSyncSettings(NDSSyncSettings o)
		{
			var ret = NDSSyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		private static int SanitizeBirthdayDay(int day, NDSSyncSettings.Month fwMonth)
		{
			var maxdays = fwMonth switch
			{
				NDSSyncSettings.Month.February => 29,
				NDSSyncSettings.Month.April or NDSSyncSettings.Month.June or NDSSyncSettings.Month.September or NDSSyncSettings.Month.November => 30,
				_ => 31
			};

			return Math.Max(1, Math.Min(day, maxdays));
		}
	}
}
