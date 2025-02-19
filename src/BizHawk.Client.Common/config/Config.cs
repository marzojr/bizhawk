using System.Collections.Generic;
using System.Drawing;
using System.IO;

using BizHawk.Bizware.Graphics;
using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Common.PathExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BizHawk.Client.Common
{
	public class Config
	{
		public static string ControlDefaultPath => Path.Combine(PathUtils.ExeDirectoryPath, "defctrl.json");

		/// <remarks>
		/// <c>CoreNames[0]</c> is the default (out-of-the-box) core.<br/>
		/// <c>AppliesTo</c> are concatenated to make the submenu's label, and
		/// <c>Config.PreferredCores[AppliesTo[0]]</c> (lookup on global <see cref="Config"/> instance) determines which option is shown as checked.<br/>
		/// The order within submenus and the order of the submenus themselves are determined by the declaration order here.
		/// </remarks>
		public static readonly IReadOnlyList<(string[] AppliesTo, string[] CoreNames)> CorePickerUIData = new List<(string[], string[])>
		{
			([ VSystemID.Raw.A26 ],
				[ CoreNames.Atari2600Hawk, CoreNames.Stella ]),
			([ VSystemID.Raw.Satellaview ],
				[ CoreNames.Bsnes115, CoreNames.SubBsnes115 ]),
			([ VSystemID.Raw.GB, VSystemID.Raw.GBC ],
				[ CoreNames.Gambatte, CoreNames.Sameboy, CoreNames.GbHawk, CoreNames.SubGbHawk ]),
			([ VSystemID.Raw.GBL ],
				[ CoreNames.GambatteLink, CoreNames.GBHawkLink, CoreNames.GBHawkLink3x, CoreNames.GBHawkLink4x ]),
			([ VSystemID.Raw.SGB ],
				[ CoreNames.Gambatte, CoreNames.Bsnes115, CoreNames.SubBsnes115, CoreNames.Bsnes ]),
			([ VSystemID.Raw.GEN ],
				[ CoreNames.Gpgx, CoreNames.PicoDrive ]),
			([ VSystemID.Raw.N64 ],
				[ CoreNames.Mupen64Plus, CoreNames.Ares64 ]),
			([ VSystemID.Raw.NES ],
				[ CoreNames.QuickNes, CoreNames.NesHawk, CoreNames.SubNesHawk ]),
			([ VSystemID.Raw.PCE, VSystemID.Raw.PCECD, VSystemID.Raw.SGX, VSystemID.Raw.SGXCD ],
				[ CoreNames.TurboNyma, CoreNames.HyperNyma, CoreNames.PceHawk ]),
			([ VSystemID.Raw.PSX ],
				[ CoreNames.Nymashock, CoreNames.Octoshock ]),
			([ VSystemID.Raw.SMS, VSystemID.Raw.GG, VSystemID.Raw.SG ],
				[ CoreNames.Gpgx, CoreNames.SMSHawk ]),
			([ VSystemID.Raw.SNES ],
				[ CoreNames.Snes9X, CoreNames.Bsnes115, CoreNames.SubBsnes115, CoreNames.Faust, CoreNames.Bsnes ]),
			([ VSystemID.Raw.TI83 ],
				[ CoreNames.Emu83, CoreNames.TI83Hawk ]),
		};

		public static Dictionary<string, string> GenDefaultCorePreferences()
		{
			Dictionary<string, string> dict = new();
			foreach (var (appliesTo, coreNames) in CorePickerUIData)
			{
				var defaultCore = coreNames[0];
				foreach (var sysID in appliesTo) dict[sysID] = defaultCore;
			}
			return dict;
		}

		public Config()
		{
			if (AllTrollers.Count == 0
				&& AllTrollersAutoFire.Count == 0
				&& AllTrollersAnalog.Count == 0
				&& AllTrollersFeedbacks.Count == 0)
			{
				var cd = ConfigService.Load<DefaultControls>(ControlDefaultPath);
				AllTrollers = cd.AllTrollers;
				AllTrollersAutoFire = cd.AllTrollersAutoFire;
				AllTrollersAnalog = cd.AllTrollersAnalog;
				AllTrollersFeedbacks = cd.AllTrollersFeedbacks;
			}
		}

		public void ResolveDefaults()
		{
			PathEntries.ResolveWithDefaults();
			HotkeyInfo.ResolveWithDefaults(HotkeyBindings);
			PathEntries.RefreshTempPath();
		}

		/// <summary>
		/// Used to determine the system a rom is classified as (and thus which core to use) for the times when our romloading autodetect magic can't determine a system.
		/// Keys are file extensions, include the leading period in each, and use lowercase;
		/// values are system IDs, use <see cref="string.Empty"/> for unset (though <see langword="null"/> should also work, omitting will remove from UI).
		/// </summary>
		public Dictionary<string, string> PreferredPlatformsForExtensions { get; set; } = new Dictionary<string, string>
		{
			[".bin"] = "",
			[".cue"] = "",
			[".img"] = "",
			[".iso"] = "",
			[".rom"] = "",
		};

		public PathEntryCollection PathEntries { get; set; } = new PathEntryCollection();

		// BIOS Paths
		// key: sysId+firmwareId; value: absolute path
		public Dictionary<string, string> FirmwareUserSpecifications { get; set; } = new Dictionary<string, string>();

		// General Client Settings
		public int InputHotkeyOverrideOptions { get; set; }
		public bool NoMixedInputHokeyOverride { get; set; }

		public bool StackOSDMessages { get; set; } = true;

		private Dictionary<string, int> TargetZoomFactors { get; set; } = new()
		{
			[VSystemID.Raw.Doom] = 1,
			[VSystemID.Raw.GB] = 3,
			[VSystemID.Raw.GBA] = 3,
			[VSystemID.Raw.GBC] = 3,
			[VSystemID.Raw.N64] = 1,
			[VSystemID.Raw.WSWAN] = 3,
		};

		public int GetWindowScaleFor(string sysID)
			=> TargetZoomFactors.GetValueOrPut(sysID, static _ => 2);

		public void SetWindowScaleFor(string sysID, int windowScale)
			=> TargetZoomFactors[sysID] = windowScale;

		// choose between 0 and 256
		public int TargetScanlineFilterIntensity { get; set; } = 128;
		public int TargetDisplayFilter { get; set; }
		public int DispFinalFilter { get; set; } = 0; // None
		public string DispUserFilterPath { get; set; } = "";
		public RecentFiles RecentRoms { get; set; } = new RecentFiles(10);
		public bool PauseWhenMenuActivated { get; set; } = true;
		public bool SaveWindowPosition { get; set; } = true;
		public bool MainFormStayOnTop { get; set; }
		public bool StartPaused { get; set; }
		public bool StartFullscreen { get; set; }
		public Point? MainWindowPosition { get; set; }
		public Size? MainWindowSize { get; set; }
		public bool MainWindowMaximized { get; set; }
		public bool RunInBackground { get; set; } = true;
		public bool AcceptBackgroundInput { get; set; }
		public bool AcceptBackgroundInputControllerOnly { get; set; }
		public bool HandleAlternateKeyboardLayouts { get; set; }
		public bool SingleInstanceMode { get; set; }
		public OpposingDirPolicy OpposingDirPolicy { get; set; }
		public bool ShowContextMenu { get; set; } = true;
		public bool HotkeyConfigAutoTab { get; set; } = true;
		public bool InputConfigAutoTab { get; set; } = true;
		public bool SkipWaterboxIntegrityChecks { get; set; } = false;
		public int AutofireOn { get; set; } = 1;
		public int AutofireOff { get; set; } = 1;
		public bool AutofireLagFrames { get; set; } = true;
		public int SaveSlot { get; set; } // currently selected savestate slot
		public bool AutoLoadLastSaveSlot { get; set; }
		public bool AutoSaveLastSaveSlot { get; set; }
		public bool SkipLagFrame { get; set; }
		public bool SuppressAskSave { get; set; }
		public bool AviCaptureOsd { get; set; }
		public bool AviCaptureLua { get; set; }
		public bool ScreenshotCaptureOsd { get; set; }
		public bool FirstBoot { get; set; } = true;
		public bool UpdateAutoCheckEnabled { get; set; } = true;
		public DateTime? UpdateLastCheckTimeUtc { get; set; }
		public string UpdateLatestVersion { get; set; } = "";
		public string UpdateIgnoreVersion { get; set; } = "";
		public bool SkipOutdatedOsCheck { get; set; }

		public bool SkipSuperuserPrivsCheck { get; set; }

		/// <summary>
		/// Makes a .bak file before any saveram-writing operation (could be extended to make timestamped backups)
		/// </summary>
		public bool BackupSaveram { get; set; } = true;

		/// <summary>
		/// Whether to make AutoSave files at periodic intervals
		/// </summary>
		public bool AutosaveSaveRAM { get; set; }

		/// <summary>
		/// Intervals at which to make AutoSave files
		/// </summary>
		public int FlushSaveRamFrames { get; set; }

		public bool TurboSeek { get; set; }

		public ClientProfile SelectedProfile { get; set; } = ClientProfile.Unknown;

		// TODO: make this not N64-specific, it doesn't need to be
		public bool N64UseCircularAnalogConstraint { get; set; } = true;

		// Run-Control settings
		public int FrameProgressDelayMs { get; set; } = 500; // how long until a frame advance hold turns into a frame progress?
		public int FrameSkip { get; set; } = 4;
		public int SpeedPercent { get; set; } = 100;
		public int SpeedPercentAlternate { get; set; } = 400;
		public bool ClockThrottle { get; set; } = true;
		public bool Unthrottled { get; set; } = false;
		public bool AutoMinimizeSkipping { get; set; } = true;
		public bool VSyncThrottle { get; set; } = false;
#if BIZHAWKBUILD_SUPERHAWK
		public bool SuperHawkThrottle { get; set; } = false;
#endif

		public RewindConfig Rewind { get; set; } = new RewindConfig();

		public SaveStateConfig Savestates { get; set; } = new SaveStateConfig();

		public MovieConfig Movies { get; set; } = new MovieConfig();

		/// <summary>
		/// Use vsync when presenting all 3d accelerated windows.
		/// For the main window, if VSyncThrottle = false, this will try to use vsync without throttling to it
		/// </summary>
		public bool VSync { get; set; }

		/// <summary>
		/// Allows non-vsync'd video to tear, this is needed for VFR monitors reportedly
		/// </summary>
		public bool DispAllowTearing { get; set; }

		// Display options
		public bool DisplayFps { get; set; }
		public bool DisplayFrameCounter { get; set; }
		public bool DisplayLagCounter { get; set; }
		public bool DisplayInput { get; set; }
		public bool DisplayRerecordCount { get; set; }
		public bool DisplayMessages { get; set; } = true;

		public bool DispFixAspectRatio { get; set; } = true;
		public bool DispFixScaleInteger { get; set; }
		public bool DispFullscreenHacks { get; set; }
		public bool DispAutoPrescale { get; set; }
		public int DispSpeedupFeatures { get; set; } = 2;

		public MessagePosition Fps { get; set; } = DefaultMessagePositions.Fps.Clone();
		public MessagePosition FrameCounter { get; set; } = DefaultMessagePositions.FrameCounter.Clone();
		public MessagePosition LagCounter { get; set; } = DefaultMessagePositions.LagCounter.Clone();
		public MessagePosition InputDisplay { get; set; } = DefaultMessagePositions.InputDisplay.Clone();
		public MessagePosition ReRecordCounter { get; set; } = DefaultMessagePositions.ReRecordCounter.Clone();
		public MessagePosition Messages { get; set; } = DefaultMessagePositions.Messages.Clone();
		public MessagePosition Autohold { get; set; } = DefaultMessagePositions.Autohold.Clone();
		public MessagePosition RamWatches { get; set; } = DefaultMessagePositions.RamWatches.Clone();

		public int MessagesColor { get; set; } = DefaultMessagePositions.MessagesColor;
		public int AlertMessageColor { get; set; } = DefaultMessagePositions.AlertMessageColor;
		public int LastInputColor { get; set; } = DefaultMessagePositions.LastInputColor;
		public int MovieInputColor { get; set; } = DefaultMessagePositions.MovieInputColor;

		public int DispPrescale { get; set; } = 1;

		public EDispMethod DispMethod { get; set; } = HostCapabilityDetector.HasD3D11 && !OSTailoredCode.IsWine ? EDispMethod.D3D11 : EDispMethod.OpenGL;

		public int DispChromeFrameWindowed { get; set; } = 2;
		public bool DispChromeStatusBarWindowed { get; set; } = true;
		public bool DispChromeCaptionWindowed { get; set; } = true;
		public bool DispChromeMenuWindowed { get; set; } = true;
		public bool DispChromeStatusBarFullscreen { get; set; }
		public bool DispChromeMenuFullscreen { get; set; }
		public bool DispChromeFullscreenAutohideMouse { get; set; } = true;
		public bool DispChromeAllowDoubleClickFullscreen { get; set; } = true;

		public EDispManagerAR DispManagerAR { get; set; } = EDispManagerAR.System;

		// these are misnomers. they're actually a fixed size (fixme on major release)
		public int DispCustomUserARWidth { get; set; } = -1;
		public int DispCustomUserARHeight { get; set; } = -1;

		// these are more like the actual AR ratio (i.e. 4:3) (fixme on major release)
		public float DispCustomUserArx { get; set; } = -1;
		public float DispCustomUserAry { get; set; } = -1;

		//these default to 0 because by default we crop nothing
		public int DispCropLeft { get; set; } = 0;
		public int DispCropTop { get; set; } = 0;
		public int DispCropRight { get; set; } = 0;
		public int DispCropBottom { get; set; } = 0;

		/// <summary>
		/// Automatically resize main window when framebuffer size changes (default behavior)
		/// </summary>
		public bool ResizeWithFramebuffer { get; set; } = true;

		// Sound options
		public ESoundOutputMethod SoundOutputMethod { get; set; } = HostCapabilityDetector.HasXAudio2 ? ESoundOutputMethod.XAudio2 : ESoundOutputMethod.OpenAL;

		/// <value>iff <see langword="false"/>, cores may skip processing audio</value>
		/// <seealso cref="SoundEnabledNormal"/>
		/// <seealso cref="SoundEnabledRWFF"/>
		/// <seealso cref="MuteFrameAdvance"/>
		public bool SoundEnabled { get; set; } = true;

		/// <value>whether to pass audio through to the host while emulating to normal throttle</value>
		/// <remarks>separate from <see cref="SoundVolume"/> so that the config UI can "remember" the previous value</remarks>
		/// <seealso cref="SoundVolume"/>
		/// <seealso cref="SoundEnabled"/>
		/// <seealso cref="SoundEnabledRWFF"/>
		/// <seealso cref="MuteFrameAdvance"/>
		public bool SoundEnabledNormal { get; set; } = true;

		/// <value>whether to pass audio through to the host while rewinding or fast-forwarding</value>
		/// <remarks>separate from <see cref="SoundVolumeRWFF"/> so that the config UI can "remember" the previous value</remarks>
		/// <seealso cref="SoundVolumeRWFF"/>
		/// <seealso cref="SoundEnabled"/>
		/// <seealso cref="SoundEnabledNormal"/>
		/// <seealso cref="MuteFrameAdvance"/>
		public bool SoundEnabledRWFF { get; set; } = true;

		/// <value>whether to pass audio through to the host when doing a frame advance while paused</value>
		/// <remarks>
		/// sets sample amplitude multiplier to 0x iff <see langword="true"/>,
		/// otherwise the main <see cref="SoundVolume"/> has effect
		/// </remarks>
		/// <seealso cref="SoundEnabled"/>
		/// <seealso cref="SoundEnabledNormal"/>
		/// <seealso cref="SoundEnabledRWFF"/>
		public bool MuteFrameAdvance { get; set; } = true;

		/// <value>
		/// volume level; interpreted as a percentage (i.e. scaled down to 0.0..1.0)
		/// and passed to the platform audio implementation, which should use it as a simple multiplier on each sample;<br/>
		/// so <c>0</c> is scale each sample by 0x (mute),<c>100</c> is scale each sample by 1x (preserve full volume),
		/// <c>50</c> is scale each sample by 0.5x (≈ -3 dB), and <c>25</c> is scale each sample by 0.25x (≈ -6 dB)
		/// </value>
		/// <seealso cref="SoundVolumeRWFF"/>
		/// <seealso cref="SoundEnabledNormal"/>
		public int SoundVolume { get; set; } = 100;

		/// <value>
		/// when rewinding or fast-forwarding, the sample amplitude multiplier is <i>multiplied by this value</i>
		/// (after conversion from percentage), or in other words, <see cref="SoundVolume"/> remains in effect
		/// </value>
		/// <seealso cref="SoundVolume"/>
		/// <seealso cref="SoundEnabledRWFF"/>
		public int SoundVolumeRWFF { get; set; } = 50;

		public bool SoundThrottle { get; set; }
		public string SoundDevice { get; set; } = "";
		public int SoundBufferSizeMs { get; set; } = 100;

		// Lua
		public RecentFiles RecentLua { get; set; } = new RecentFiles(8);
		public RecentFiles RecentLuaSession { get; set; } = new RecentFiles(8);

		// luaconsole-refactor TODO: move this to LuaConsole settings
		public bool RunLuaDuringTurbo { get; set; } = true;

		// Watch Settings
		public RecentFiles RecentWatches { get; set; } = new RecentFiles(8);
		public PreviousType RamWatchDefinePrevious { get; set; } = PreviousType.LastFrame;
		public bool DisplayRamWatch { get; set; }

		// Video dumping settings
		public string VideoWriter { get; set; } = "";
		public int JmdCompression { get; set; } = 3;
		public int JmdThreads { get; set; } = 3;
		public string FFmpegFormat { get; set; } = "";
		public string FFmpegCustomCommand { get; set; } = "-c:a foo -c:v bar -f baz";
		public string AviCodecToken { get; set; } = "";
		public int GifWriterFrameskip { get; set; } = 3;
		public int GifWriterDelay { get; set; } = -1;
		public bool VideoWriterAudioSync { get; set; } = true;

		[JsonIgnore]
		public bool VideoWriterAudioSyncEffective;

		// Emulation core settings
		internal Dictionary<string, JToken> CoreSettings { get; set; } = new Dictionary<string, JToken>();
		internal Dictionary<string, JToken> CoreSyncSettings { get; set; } = new Dictionary<string, JToken>();

		public Dictionary<string, ToolDialogSettings> CommonToolSettings { get; set; } = new Dictionary<string, ToolDialogSettings>();
		public Dictionary<string, Dictionary<string, object>> CustomToolSettings { get; set; } = new Dictionary<string, Dictionary<string, object>>();

		public CheatConfig Cheats { get; set; } = new CheatConfig();

		// Macro Tool
		public RecentFiles RecentMacros { get; set; } = new RecentFiles(8);

		// Movie Settings
		public RecentFiles RecentMovies { get; set; } = new RecentFiles(8);
		public string DefaultAuthor { get; set; } = "default user";
		public bool UseDefaultAuthor { get; set; } = true;
		public bool DisplaySubtitles { get; set; } = true;

		// Play Movie Dialog
		public bool PlayMovieIncludeSubDir { get; set; }
		public bool PlayMovieMatchHash { get; set; } = true;

		public Dictionary<string, string> HotkeyBindings { get; set; } = new();

		// Analog Hotkey values
		public int AnalogLargeChange { get; set; } = 10;
		public int AnalogSmallChange { get; set; } = 1;

		// [ControllerType][ButtonName] => Physical Bind
		public Dictionary<string, Dictionary<string, string>> AllTrollers { get; set; } = new Dictionary<string, Dictionary<string, string>>();
		public Dictionary<string, Dictionary<string, string>> AllTrollersAutoFire { get; set; } = new Dictionary<string, Dictionary<string, string>>();
		public Dictionary<string, Dictionary<string, AnalogBind>> AllTrollersAnalog { get; set; } = new Dictionary<string, Dictionary<string, AnalogBind>>();
		public Dictionary<string, Dictionary<string, FeedbackBind>> AllTrollersFeedbacks { get; set; } = new Dictionary<string, Dictionary<string, FeedbackBind>>();

		/// <remarks>as this setting spans multiple cores and doesn't actually affect the behavior of any core, it hasn't been absorbed into the new system</remarks>
		public bool GbAsSgb { get; set; }
		public string LibretroCore { get; set; }

		public Dictionary<string, string> PreferredCores = GenDefaultCorePreferences();

		public bool DontTryOtherCores { get; set; }

		// ReSharper disable once UnusedMember.Global
		public string LastWrittenFrom { get; set; } = VersionInfo.MainVersion;

		// ReSharper disable once UnusedMember.Global
		public string LastWrittenFromDetailed { get; set; } = VersionInfo.GetEmuVersion();

		public bool UseStaticWindowTitles { get; set; }

		public List<string> ModifierKeys { get; set; } = new();

		[JsonIgnore]
		public IReadOnlyList<string> ModifierKeysEffective;

		public bool MergeLAndRModifierKeys { get; set; } = true;

		/// <remarks>in seconds</remarks>
		public int OSDMessageDuration { get; set; } = 2;

		public Queue<string> RecentCores { get; set; } = new();
		
		public Dictionary<string, string> TrustedExtTools { get; set; } = new();

		// RetroAchievements settings
		public bool SkipRATelemetryWarning { get; set; }
		public string RAUsername { get; set; } = "";
		public string RAToken { get; set; } = "";
		public bool RACheevosActive { get; set; } = true;
		public bool RALBoardsActive { get; set; }
		public bool RARichPresenceActive { get; set; } = true;
		public bool RAHardcoreMode { get; set; }
		public bool RASoundEffects { get; set; } = true;
		public bool RAAllowUnofficialCheevos { get; set; }
		public bool RAAutostart { get; set; }

		public bool AVWriterPad { get; set; } = false;

		public int AVWriterResizeHeight { get; set; } = 0;

		public int AVWriterResizeWidth { get; set; } = 0;

		public bool GCAdapterSupportEnabled { get; set; } = false;

		public bool ScaleOSDWithSystemScale { get; set; } = true;
	}
}
