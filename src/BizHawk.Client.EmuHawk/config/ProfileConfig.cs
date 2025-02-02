using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;
using BizHawk.Emulation.Cores.Consoles.Sega.gpgx;
using BizHawk.Emulation.Cores.Sega.MasterSystem;
using BizHawk.Emulation.Cores.ColecoVision;
using BizHawk.Emulation.Cores.Atari.Atari2600;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.Nintendo.GBA;
using BizHawk.Emulation.Cores.Consoles.Nintendo.NDS;

namespace BizHawk.Client.EmuHawk
{
	public partial class ProfileConfig : Form
	{
		private readonly MainForm _mainForm;

		private readonly Config _config;

		public ProfileConfig(Config config, MainForm mainForm)
		{
			_mainForm = mainForm;
			_config = config;
			InitializeComponent();
			Icon = Properties.Resources.ProfileIcon;
		}

		private void ProfileConfig_Load(object sender, EventArgs e)
		{
			ProfileSelectComboBox.SelectedItem = _config.SelectedProfile switch
			{
				ClientProfile.Casual => "Casual Gaming",
				ClientProfile.Longplay => "Longplays",
				ClientProfile.Tas => "Tool-assisted Speedruns",
				ClientProfile.N64Tas => "N64 Tool-assisted Speedruns",
				_ => "Casual Gaming"
			};

			AutoCheckForUpdates.Checked = _config.UpdateAutoCheckEnabled;
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			_config.SelectedProfile = ProfileSelectComboBox.SelectedItem.ToString() switch
			{
				"Longplays" => ClientProfile.Longplay,
				"Tool-assisted Speedruns" => ClientProfile.Tas,
				"N64 Tool-assisted Speedruns" => ClientProfile.N64Tas,
				_ => ClientProfile.Casual
			};

			SetCasual();

			switch (_config.SelectedProfile)
			{
				case ClientProfile.Longplay:
					SetLongPlay();
					break;
				case ClientProfile.Tas:
					SetTas();
					break;
				case ClientProfile.N64Tas:
					SetTas();
					SetN64Tas();
					break;
			}

			bool oldUpdateAutoCheckEnabled = _config.UpdateAutoCheckEnabled;
			_config.UpdateAutoCheckEnabled = AutoCheckForUpdates.Checked;
			if (_config.UpdateAutoCheckEnabled != oldUpdateAutoCheckEnabled)
			{
				UpdateChecker.GlobalConfig = _config;
				if (!_config.UpdateAutoCheckEnabled)
				{
					UpdateChecker.ResetHistory();
				}

				UpdateChecker.BeginCheck(); // Call even if auto checking is disabled to trigger event (it won't actually check)
			}

			DialogResult = DialogResult.OK;
			Close();
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void SetCasual()
		{
			_config.Savestates.NoLowResLargeScreenshots = false;
			_config.Savestates.SaveScreenshot = false;
			_config.OpposingDirPolicy = OpposingDirPolicy.Priority;
			_config.Savestates.MakeBackups = false;

			_config.Savestates.CompressionLevelNormal = 0;
			_config.Rewind.Enabled = true;
			_config.SkipLagFrame = false;

			// SNES
			_config.PreferredCores[VSystemID.Raw.SNES] = CoreNames.Snes9X;

			// Genesis
			var genesisSettings = GetSyncSettings<GPGX, GPGX.GPGXSyncSettings>();
			genesisSettings.Region = LibGPGX.Region.Autodetect;
			PutSyncSettings<GPGX>(genesisSettings);

			// SMS
			var smsSettings = GetSyncSettings<SMS, SMS.SmsSyncSettings>();
			smsSettings.UseBios = false;
			PutSyncSettings<SMS>(smsSettings);

			// Coleco
			var colecoSettings = GetSyncSettings<ColecoVision, ColecoVision.ColecoSyncSettings>();
			colecoSettings.SkipBiosIntro = false;
			PutSyncSettings<ColecoVision>(colecoSettings);

			// A2600
			var a2600Settings = GetSyncSettings<Atari2600, Atari2600.A2600SyncSettings>();
			a2600Settings.FastScBios = true;
			a2600Settings.LeftDifficulty = false;
			a2600Settings.RightDifficulty = false;
			PutSyncSettings<Atari2600>(a2600Settings);

			// NES
			_config.PreferredCores[VSystemID.Raw.NES] = CoreNames.QuickNes;

			// NDS
			var ssMelonDS = GetSyncSettings<NDS, NDS.NDSSyncSettings>();
			ssMelonDS.SkipFirmware = true;
			PutSyncSettings<NDS>(ssMelonDS);

			// GB
			_config.PreferredCores[VSystemID.Raw.GB] = CoreNames.Gambatte;
			_config.PreferredCores[VSystemID.Raw.GBC] = CoreNames.Gambatte;
			var s = GetSyncSettings<Gameboy, Gameboy.GambatteSyncSettings>();
			s.EnableBIOS = false;
			PutSyncSettings<Gameboy>(s);

			// GBA
			var mGBASettings = GetSyncSettings<MGBAHawk, MGBAHawk.SyncSettings>();
			mGBASettings.SkipBios = true;
			PutSyncSettings<MGBAHawk>(mGBASettings);
		}

		private void SetLongPlay()
		{
			_config.Savestates.CompressionLevelNormal = 5;

			// SNES
			_config.PreferredCores[VSystemID.Raw.SNES] = CoreNames.Bsnes115;

			// SMS
			var smsSettings = GetSyncSettings<SMS, SMS.SmsSyncSettings>();
			smsSettings.UseBios = true;
			PutSyncSettings<SMS>(smsSettings);

			// A2600
			var a2600Settings = GetSyncSettings<Atari2600, Atari2600.A2600SyncSettings>();
			a2600Settings.FastScBios = false;
			a2600Settings.LeftDifficulty = true;
			a2600Settings.RightDifficulty = true;
			PutSyncSettings<Atari2600>(a2600Settings);

			// NES
			_config.PreferredCores[VSystemID.Raw.NES] = CoreNames.NesHawk;

			// GB
			_config.PreferredCores[VSystemID.Raw.GB] = CoreNames.Gambatte;
			_config.PreferredCores[VSystemID.Raw.GBC] = CoreNames.Gambatte;
			var s = GetSyncSettings<Gameboy, Gameboy.GambatteSyncSettings>();
			s.EnableBIOS = true;
			PutSyncSettings<Gameboy>(s);
		}

		private void SetTas()
		{
			// General
			_config.Savestates.SaveScreenshot = true;
			_config.OpposingDirPolicy = OpposingDirPolicy.Allow;
			_config.Savestates.MakeBackups = true;
			_config.SkipLagFrame = false;
			_config.Savestates.CompressionLevelNormal = 5;

			// Rewind
			_config.Rewind.Enabled = false;

			// SNES
			_config.PreferredCores[VSystemID.Raw.SNES] = CoreNames.Bsnes115;

			// Genesis
			var genesisSettings = GetSyncSettings<GPGX, GPGX.GPGXSyncSettings>();
			genesisSettings.Region = LibGPGX.Region.Autodetect;
			PutSyncSettings<GPGX>(genesisSettings);

			// SMS
			var smsSettings = GetSyncSettings<SMS, SMS.SmsSyncSettings>();
			smsSettings.UseBios = true;
			PutSyncSettings<SMS>(smsSettings);

			// A2600
			var a2600Settings = GetSyncSettings<Atari2600, Atari2600.A2600SyncSettings>();
			a2600Settings.FastScBios = false;
			a2600Settings.LeftDifficulty = true;
			a2600Settings.RightDifficulty = true;
			PutSyncSettings<Atari2600>(a2600Settings);

			// NES
			_config.PreferredCores[VSystemID.Raw.NES] = CoreNames.NesHawk;

			// NDS
			var ssMelonDS = GetSyncSettings<NDS, NDS.NDSSyncSettings>();
			ssMelonDS.SkipFirmware = false;
			ssMelonDS.UseRealBIOS = true;
			PutSyncSettings<NDS>(ssMelonDS);

			// GB
			_config.PreferredCores[VSystemID.Raw.GB] = CoreNames.Gambatte;
			_config.PreferredCores[VSystemID.Raw.GBC] = CoreNames.Gambatte;
			var s = GetSyncSettings<Gameboy, Gameboy.GambatteSyncSettings>();
			s.EnableBIOS = true;
			PutSyncSettings<Gameboy>(s);

			// GBA
			var mGBASettings = GetSyncSettings<MGBAHawk, MGBAHawk.SyncSettings>();
			mGBASettings.SkipBios = false;
			PutSyncSettings<MGBAHawk>(mGBASettings);
		}

		private void SetN64Tas()
		{
			// General
			_config.Savestates.MakeBackups = false;
			_config.SkipLagFrame = true;
			_config.Savestates.CompressionLevelNormal = 0;
		}

		private TSetting GetSyncSettings<TEmulator, TSetting>()
			where TSetting : class, new()
			where TEmulator : IEmulator
		{
			object fromCore = null;
			var settable = _mainForm.GetSettingsAdapterFor<TEmulator>();
			if (settable.HasSyncSettings)
			{
				fromCore = settable.GetSyncSettings();
			}

			return fromCore as TSetting
				?? _config.GetCoreSyncSettings<TEmulator, TSetting>()
				?? new TSetting(); // guaranteed to give sensible defaults
		}

		private void PutSyncSettings<TEmulator>(object o)
			where TEmulator : IEmulator
			=> _mainForm.GetSettingsAdapterFor<TEmulator>().PutCoreSyncSettings(o);
	}
}
