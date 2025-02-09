using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Common.StringExtensions;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class RAIntegration : RetroAchievements
	{
		private readonly RAInterface RA;

		static RAIntegration()
		{
			if (OSTailoredCode.IsUnixHost)
			{
				// RAIntegration is Windows only
				return;
			}

			try
			{
				AttachDll();
			}
			catch
			{
				DetachDll();
			}
		}

		private readonly RAInterface.IsActiveDelegate _isActive;
		private readonly RAInterface.UnpauseDelegate _unpause;
		private readonly RAInterface.PauseDelegate _pause;
		private readonly RAInterface.RebuildMenuDelegate _rebuildMenu;
		private readonly RAInterface.EstimateTitleDelegate _estimateTitle;
		private readonly RAInterface.ResetEmulatorDelegate _resetEmulator;
		private readonly RAInterface.LoadROMDelegate _loadROM;

		private readonly RAInterface.MenuItem[] _menuItems = new RAInterface.MenuItem[40];

		// Memory may be accessed by another thread (mainly rich presence, some other things too)
		// and peeks for us are not thread safe, so we need to guard it
		private readonly ManualResetEventSlim _memLock = new(false);
		private readonly SemaphoreSlim _memSema = new(1);
		private readonly object _memSync = new();
		private readonly RAMemGuard _memGuard;
		private readonly RAMemAccess _memAccess;

		private bool _firstRestart = true;

		private void RebuildMenu()
		{
			var numItems = RA.GetPopupMenuItems(_menuItems);
			_raDropDownItems.Clear();
			{
				var tsi = new ToolStripMenuItem("Shutdown RetroAchievements");
				tsi.Click += (_, _) => _shutdownRACallback();
				_raDropDownItems.Add(tsi);

				tsi = new("Autostart RetroAchievements")
				{
					Checked = _getConfig().RAAutostart,
					CheckOnClick = true,
				};
				tsi.CheckedChanged += (_, _) =>
				{
					var config = _getConfig();
					config.RAAutostart = !config.RAAutostart;
				};
				_raDropDownItems.Add(tsi);

				var tss = new ToolStripSeparator();
				_raDropDownItems.Add(tss);
			}
			for (var i = 0; i < numItems; i++)
			{
				if (_menuItems[i].Label != IntPtr.Zero)
				{
					var tsi = new ToolStripMenuItem(Marshal.PtrToStringUni(_menuItems[i].Label))
					{
						Checked = _menuItems[i].Checked != 0,
					};
					var id = _menuItems[i].ID;
					tsi.Click += (_, _) =>
					{
						RA.InvokeDialog(id);
						_mainForm.UpdateWindowTitle();
					};
					_raDropDownItems.Add(tsi);
				}
				else
				{
					var tss = new ToolStripSeparator();
					_raDropDownItems.Add(tss);
				}
			}
		}

		protected override void HandleHardcoreModeDisable(string reason)
		{
			_dialogParent.ModalMessageBox(
				caption: "Warning",
				icon: EMsgBoxIcon.Warning,
				text: $"{reason} Disabling hardcore mode.");
			RA.WarnDisableHardcore(null);
		}

		protected override uint IdentifyHash(string hash)
			=> RA.IdentifyHash(hash);

		protected override uint IdentifyRom(byte[] rom)
			=> RA.IdentifyRom(rom, rom.Length);

		public RAIntegration(
			MainForm mainForm,
			InputManager inputManager,
			ToolManager tools,
			Func<Config> getConfig,
			ToolStripItemCollection raDropDownItems,
			Action shutdownRACallback)
				: base(mainForm, inputManager, tools, getConfig, raDropDownItems, shutdownRACallback)
		{
			_memGuard = new(_memLock, _memSema, _memSync);
			_memAccess = new(_memLock, _memSema, _memSync);
			
			RA = BizInvoker.GetInvoker<RAInterface>(_resolver, _memAccess, CallingConventionAdapters.Native);

			// make sure clientName and clientVer match our user agent, as these get put into RAIntegration's user agent
			RA.InitClient(mainForm.AsWinFormsHandle().Handle,
				clientName: string.IsNullOrWhiteSpace(VersionInfo.CustomBuildString) ? "EmuHawk" : VersionInfo.CustomBuildString.OnlyAlphanumeric(),
				clientVer: $"{VersionInfo.MainVersion}{(VersionInfo.DeveloperBuild ? "-dev" : string.Empty)}");

			_isActive = () => !Emu.IsNull();
			_unpause = _mainForm.UnpauseEmulator;
			_pause = _mainForm.PauseEmulator;
			_rebuildMenu = RebuildMenu;
			_estimateTitle = buffer =>
			{
				var name = Encoding.UTF8.GetBytes(Game?.Name ?? "No Game Info Available");
				Marshal.Copy(name, 0, buffer, Math.Min(name.Length, 256));
			};
			_resetEmulator = () => _mainForm.RebootCore();
			_loadROM = path => _ = _mainForm.LoadRom(new LoadRomArgs(new OpenAdvanced_OpenRom(path)));

			RA.InstallSharedFunctionsExt(_isActive, _unpause, _pause, _rebuildMenu, _estimateTitle, _resetEmulator, _loadROM);

			RA.AttemptLogin(true);
		}

		public override void Dispose()
		{
			RA?.Shutdown();
			_memGuard.Dispose();
			_mainForm.QuicksaveLoad -= QuickLoadCallback;
		}

		public override void OnSaveState(string path)
			=> RA.OnSaveState(path);

		public override void OnLoadState(string path)
		{
			if (RA.HardcoreModeIsActive())
			{
				HandleHardcoreModeDisable("Loading savestates is not allowed in hardcore mode.");
			}

			RA.OnLoadState(path);
		}
		
		private void QuickLoadCallback(object _, BeforeQuickLoadEventArgs e)
		{
			if (RA.HardcoreModeIsActive())
			{
				e.Handled = !RA.WarnDisableHardcore("load a quicksave");
			}
		}

		public override void Stop()
		{
			RA.ClearMemoryBanks();
			RA.ActivateGame(0);
		}

		public override void Restart()
		{
			if (_firstRestart)
			{
				_firstRestart = false;
				if (RA.HardcoreModeIsActive())
				{
					if (!_mainForm.RebootCore())
					{
						// unset hardcore mode if we fail to reboot core somehow
						HandleHardcoreModeDisable("Failed to reboot core.");
					}
					if (RA.HardcoreModeIsActive() && _mainForm.CurrentlyOpenRomArgs is not null)
					{
						// if we aren't hardcore anymore, we failed to reboot the core (and didn't call Restart probably)
						// if CurrentlyOpenRomArgs is null, then Restart won't be called (as RebootCore returns true immediately), so
						return;
					}
				}
			}

			var consoleId = SystemIdToConsoleId();
			RA.SetConsoleID(consoleId);

			RA.ClearMemoryBanks();

			if (Emu.HasMemoryDomains())
			{
				_memFunctions = CreateMemoryBanks(consoleId, Domains);

				for (var i = 0; i < _memFunctions.Count; i++)
				{
					_memFunctions[i].MemGuard = _memGuard;
					RA.InstallMemoryBank(i, _memFunctions[i].ReadFunc, _memFunctions[i].WriteFunc, (int)_memFunctions[i].BankSize);
					RA.InstallMemoryBankBlockReader(i, _memFunctions[i].ReadBlockFunc);
				}
			}

			AllGamesVerified = true;

			if (_mainForm.CurrentlyOpenRomArgs is not null)
			{
				var ids = GetRAGameIds(_mainForm.CurrentlyOpenRomArgs.OpenAdvanced, consoleId);

				AllGamesVerified = !ids.Contains(0u);

				RA.ActivateGame(ids.Count > 0 ? ids[0] : 0u);
			}
			else
			{
				RA.ActivateGame(0);
			}

			Update();
			RebuildMenu();

			// workaround a bug in RA which will cause the window title to be changed despite us not calling UpdateAppTitle
			_mainForm.UpdateWindowTitle();

			// note: this can only catch quicksaves (probably only case of accidential use from hotkeys)
			_mainForm.QuicksaveLoad += QuickLoadCallback;
		}

		public bool OverlayActive => RA.IsOverlayFullyVisible();

		public override void Update()
		{
			if (RA.HardcoreModeIsActive())
			{
				CheckHardcoreModeConditions();
			}

			if (_inputManager.ClientControls["Open RA Overlay"])
			{
				RA.SetPaused(true);
			}

			if (!OverlayActive) return;

			var ci = new RAInterface.ControllerInput
			{
				UpPressed = _inputManager.ClientControls["RA Up"],
				DownPressed = _inputManager.ClientControls["RA Down"],
				LeftPressed = _inputManager.ClientControls["RA Left"],
				RightPressed = _inputManager.ClientControls["RA Right"],
				ConfirmPressed = _inputManager.ClientControls["RA Confirm"],
				CancelPressed = _inputManager.ClientControls["RA Cancel"],
				QuitPressed = _inputManager.ClientControls["RA Quit"],
			};

			RA.NavigateOverlay(ref ci);

			// todo: suppress user inputs with overlay active?
			// cpp: well this happens now if hotkeys override controller inputs
		}

		public override void OnFrameAdvance()
		{
			var input = _inputManager.ControllerOutput;
			if (input.Definition.BoolButtons.Any(b => (b.Contains("Power") || b.Contains("Reset")) && input.IsPressed(b)))
			{
				RA.OnReset();
			}

			if (Emu.HasMemoryDomains())
			{
				// we want to EnterExit to prevent wbx host spam when peeks are spammed
				using (Domains.MainMemory.EnterExit())
				{
					RA.DoAchievementsFrame();
				}
			}
			else
			{
				RA.DoAchievementsFrame();
			}
		}

		// FIXME: THIS IS GARBAGE
		public IMonitor ThisIsTheRAMemHack()
			=> _memAccess;
	}
}
