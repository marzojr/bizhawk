using System.ComponentModel;
using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Nintendo.SNES9X
{
	[PortedCore(
		name: CoreNames.Snes9X,
		author: "Snes9x contributors; port by Morilli",
		portedVersion: "e49165c",
		portedUrl: "https://github.com/snes9xgit/snes9x")]
	public class Snes9x : WaterboxCore,
		ISettable<Snes9x.Settings, Snes9x.SyncSettings>, IRegionable
	{
		private readonly LibSnes9x _core;

		[CoreConstructor(VSystemID.Raw.SNES)]
		public Snes9x(CoreLoadParameters<Settings, SyncSettings> loadParameters)
			:base(loadParameters.Comm, new Configuration
			{
				DefaultWidth = 256,
				DefaultHeight = 224,
				MaxWidth = 512,
				MaxHeight = 480,
				MaxSamples = 8192,
				SystemId = VSystemID.Raw.SNES,
			})
		{
			this._romPath = Path.ChangeExtension(loadParameters.Roms[0].RomPath, null);
			this._currentMsuTrack = new ProxiedFile();

			LibSnes9x.OpenAudio openAudioCb = MsuOpenAudio;
			LibSnes9x.SeekAudio seekAudioCb = _currentMsuTrack.Seek;
			LibSnes9x.ReadAudio readAudioCb = _currentMsuTrack.ReadByte;
			LibSnes9x.AudioEnd audioEndCb = _currentMsuTrack.AtEnd;
			_core = PreInit<LibSnes9x>(new WaterboxOptions
			{
				Filename = "snes9x.wbx",
				SbrkHeapSizeKB = 1024,
				InvisibleHeapSizeKB = 5 * 1024,
				MmapHeapSizeKB = 1024,
				PlainHeapSizeKB = 13 * 1024,
				SkipCoreConsistencyCheck = loadParameters.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = loadParameters.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			}, new Delegate[] { openAudioCb, seekAudioCb, readAudioCb, audioEndCb });

			if (!_core.biz_init())
				throw new InvalidOperationException("Init() failed");

			// add msu data file if it exists
			try
			{
				// "msu1.rom" is hardcoded in the core
				_exe.AddReadonlyFile(File.ReadAllBytes($"{_romPath}.msu"), "msu1.rom");
			}
			catch
			{
				// ignored
			}
			_core.SetMsu1Callbacks(openAudioCb, seekAudioCb, readAudioCb, audioEndCb);

			if (!_core.biz_load_rom(loadParameters.Roms[0].RomData, loadParameters.Roms[0].RomData.Length))
				throw new InvalidOperationException("LoadRom() failed");

			PostInit();

			if (_core.biz_is_ntsc())
			{
				Console.WriteLine("NTSC rom loaded");
				VsyncNumerator = 21477272;
				VsyncDenominator = 357366;
				Region = DisplayType.NTSC;
			}
			else
			{
				Console.WriteLine("PAL rom loaded");
				VsyncNumerator = 21281370;
				VsyncDenominator = 425568;
				Region = DisplayType.PAL;
			}

			_syncSettings = loadParameters.SyncSettings ?? new SyncSettings();
			InitControllers();
			PutSettings(loadParameters.Settings ?? new Settings());
		}

		private readonly ProxiedFile _currentMsuTrack;
		private bool _disposed;

		private bool MsuOpenAudio(ushort trackId) => _currentMsuTrack.OpenMsuTrack(_romPath, trackId);

		public override void Dispose()
		{
			if (_disposed) return;

			_disposed = true;
			_currentMsuTrack?.Dispose();
			base.Dispose();
		}

		private void InitControllers()
		{
			_core.biz_set_port_devices(_syncSettings.LeftPort, _syncSettings.RightPort);

			_controllers = new Snes9xControllers(_syncSettings);
		}

		private Snes9xControllers _controllers;

		public override ControllerDefinition ControllerDefinition => _controllers.ControllerDefinition;

		public DisplayType Region { get; }

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			if (controller.IsPressed("Power"))
				_core.biz_hard_reset();
			else if (controller.IsPressed("Reset"))
				_core.biz_soft_reset();
			_controllers.UpdateControls(controller);
			_core.SetButtons(_controllers.inputState);

			return new LibWaterboxCore.FrameInfo();
		}

		public override int VirtualWidth => BufferWidth == 256 && BufferHeight <= 240 ? 293 : 587;
		public override int VirtualHeight => BufferHeight <= 240 && BufferWidth == 512 ? BufferHeight * 2 : BufferHeight;

		protected override void LoadStateBinaryInternal(BinaryReader reader)
		{
			_core.biz_post_load_state();
		}

		private Settings _settings;
		private SyncSettings _syncSettings;
		private readonly string _romPath;

		public Settings GetSettings()
		{
			return _settings.Clone();
		}

		public SyncSettings GetSyncSettings()
		{
			return _syncSettings.Clone();
		}

		public PutSettingsDirtyBits PutSettings(Settings o)
		{
			_settings = o;
			int s = 0;
			if (o.PlaySound0) s |= 0b1;
			if (o.PlaySound1) s |= 0b10;
			if (o.PlaySound2) s |= 0b100;
			if (o.PlaySound3) s |= 0b1000;
			if (o.PlaySound4) s |= 0b10000;
			if (o.PlaySound5) s |= 0b100000;
			if (o.PlaySound6) s |= 0b1000000;
			if (o.PlaySound7) s |= 0b10000000;
			_core.biz_set_sound_channels(s);
			int l = 0;
			if (o.ShowBg0) l |= 1;
			if (o.ShowBg1) l |= 2;
			if (o.ShowBg2) l |= 4;
			if (o.ShowBg3) l |= 8;
			if (o.ShowWindow) l |= 32;
			if (o.ShowTransparency) l |= 64;
			if (o.ShowSprites0) l |= 256;
			if (o.ShowSprites1) l |= 512;
			if (o.ShowSprites2) l |= 1024;
			if (o.ShowSprites3) l |= 2048;
			_core.biz_set_layers(l);

			return PutSettingsDirtyBits.None; // no reboot needed
		}

		public PutSettingsDirtyBits PutSyncSettings(SyncSettings o)
		{
			var ret = SyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		[CoreSettings]
		public class Settings
		{
			[DefaultValue(true)]
			[DisplayName("Enable Sound Channel 1")]
			public bool PlaySound0 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Enable Sound Channel 2")]
			public bool PlaySound1 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Enable Sound Channel 3")]
			public bool PlaySound2 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Enable Sound Channel 4")]
			public bool PlaySound3 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Enable Sound Channel 5")]
			public bool PlaySound4 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Enable Sound Channel 6")]
			public bool PlaySound5 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Enable Sound Channel 7")]
			public bool PlaySound6 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Enable Sound Channel 8")]
			public bool PlaySound7 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Enable Background Layer 1")]
			public bool ShowBg0 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Enable Background Layer 2")]
			public bool ShowBg1 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Enable Background Layer 3")]
			public bool ShowBg2 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Enable Background Layer 4")]
			public bool ShowBg3 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Show Sprites Priority 1")]
			public bool ShowSprites0 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Show Sprites Priority 2")]
			public bool ShowSprites1 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Show Sprites Priority 3")]
			public bool ShowSprites2 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Show Sprites Priority 4")]
			public bool ShowSprites3 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Show Window")]
			public bool ShowWindow { get; set; }

			[DefaultValue(true)]
			[DisplayName("Show Transparency")]
			public bool ShowTransparency { get; set; }

			public Settings()
			{
				SettingsUtil.SetDefaultValues(this);
			}

			public Settings Clone()
			{
				return (Settings)MemberwiseClone();
			}
		}

		[CoreSettings]
		public class SyncSettings
		{
			[DefaultValue(LibSnes9x.LeftPortDevice.Joypad)]
			[DisplayName("Left Port")]
			[Description("Specifies the controller type plugged into the left controller port on the console")]
			public LibSnes9x.LeftPortDevice LeftPort { get; set; }

			[DefaultValue(LibSnes9x.RightPortDevice.Joypad)]
			[DisplayName("Right Port")]
			[Description("Specifies the controller type plugged into the right controller port on the console")]
			public LibSnes9x.RightPortDevice RightPort { get; set; }

			public SyncSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}

			public SyncSettings Clone()
			{
				return (SyncSettings)MemberwiseClone();
			}

			public static bool NeedsReboot(SyncSettings x, SyncSettings y)
			{
				// the core can handle dynamic plugging and unplugging, but that changes
				// the controllerdefinition, and we're not ready for that
				return !DeepEquality.DeepEquals(x, y);
			}
		}
	}
}
