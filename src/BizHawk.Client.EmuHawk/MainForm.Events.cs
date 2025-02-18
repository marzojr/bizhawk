using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Bizware.Audio;
using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.CustomControls;
using BizHawk.Client.EmuHawk.ToolExtensions;
using BizHawk.Common;
using BizHawk.Common.PathExtensions;
using BizHawk.Emulation.Common;
using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk
{
	public partial class MainForm
	{
		private static readonly FilesystemFilterSet MAMERomsFSFilterSet = new(new FilesystemFilter("MAME Arcade ROMs", new[] { "zip" }))
		{
			AppendAllFilesEntry = false,
		};

		private static readonly FilesystemFilterSet ScreenshotsFSFilterSet = new(FilesystemFilter.PNGs)
		{
			AppendAllFilesEntry = false,
		};

		private void FileSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			SaveStateSubMenu.Enabled =
				LoadStateSubMenu.Enabled =
				SaveSlotSubMenu.Enabled =
				Emulator.HasSavestates();

			OpenRomMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Open ROM"];
			CloseRomMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Close ROM"];

			CloseRomMenuItem.Enabled = !Emulator.IsNull();

			var hasSaveRam = Emulator.HasSaveRam();
			bool needBold = hasSaveRam && Emulator.AsSaveRam().SaveRamModified;

			SaveRAMSubMenu.Enabled = hasSaveRam;
			SaveRAMSubMenu.SetStyle(needBold ? FontStyle.Bold : FontStyle.Regular);

			AVSubMenu.Enabled = Emulator.HasVideoProvider(); //TODO necessary?
		}

		private void RecentRomMenuItem_DropDownOpened(object sender, EventArgs e)
			=> RecentRomSubMenu.ReplaceDropDownItems(Config.RecentRoms.RecentMenu(this, LoadRomFromRecent, "ROM", romLoading: true));

		private bool HasSlot(int slot) => _stateSlots.HasSlot(Emulator, MovieSession.Movie, slot, SaveStatePrefix());

		private void SaveStateSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			void SetSlotFont(ToolStripMenuItemEx menu, int slot) => menu.SetStyle(
				HasSlot(slot) ? (FontStyle.Italic | FontStyle.Bold) : FontStyle.Regular);

			SetSlotFont(SaveState1MenuItem, 1);
			SetSlotFont(SaveState2MenuItem, 2);
			SetSlotFont(SaveState3MenuItem, 3);
			SetSlotFont(SaveState4MenuItem, 4);
			SetSlotFont(SaveState5MenuItem, 5);
			SetSlotFont(SaveState6MenuItem, 6);
			SetSlotFont(SaveState7MenuItem, 7);
			SetSlotFont(SaveState8MenuItem, 8);
			SetSlotFont(SaveState9MenuItem, 9);
			SetSlotFont(SaveState0MenuItem, 10);

			AutosaveLastSlotMenuItem.Checked = Config.AutoSaveLastSaveSlot;

			SaveState1MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Save State 1"];
			SaveState2MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Save State 2"];
			SaveState3MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Save State 3"];
			SaveState4MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Save State 4"];
			SaveState5MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Save State 5"];
			SaveState6MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Save State 6"];
			SaveState7MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Save State 7"];
			SaveState8MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Save State 8"];
			SaveState9MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Save State 9"];
			SaveState0MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Save State 10"];
			SaveNamedStateMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Save Named State"];
		}

		private void LoadStateSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			LoadState1MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Load State 1"];
			LoadState2MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Load State 2"];
			LoadState3MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Load State 3"];
			LoadState4MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Load State 4"];
			LoadState5MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Load State 5"];
			LoadState6MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Load State 6"];
			LoadState7MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Load State 7"];
			LoadState8MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Load State 8"];
			LoadState9MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Load State 9"];
			LoadState0MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Load State 10"];
			LoadNamedStateMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Load Named State"];

			AutoloadLastSlotMenuItem.Checked = Config.AutoLoadLastSaveSlot;

			LoadState1MenuItem.Enabled = HasSlot(1);
			LoadState2MenuItem.Enabled = HasSlot(2);
			LoadState3MenuItem.Enabled = HasSlot(3);
			LoadState4MenuItem.Enabled = HasSlot(4);
			LoadState5MenuItem.Enabled = HasSlot(5);
			LoadState6MenuItem.Enabled = HasSlot(6);
			LoadState7MenuItem.Enabled = HasSlot(7);
			LoadState8MenuItem.Enabled = HasSlot(8);
			LoadState9MenuItem.Enabled = HasSlot(9);
			LoadState0MenuItem.Enabled = HasSlot(10);
		}

		private void SaveSlotSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			SelectSlot1MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Select State 1"];
			SelectSlot2MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Select State 2"];
			SelectSlot3MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Select State 3"];
			SelectSlot4MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Select State 4"];
			SelectSlot5MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Select State 5"];
			SelectSlot6MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Select State 6"];
			SelectSlot7MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Select State 7"];
			SelectSlot8MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Select State 8"];
			SelectSlot9MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Select State 9"];
			SelectSlot0MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Select State 10"];
			PreviousSlotMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Previous Slot"];
			NextSlotMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Next Slot"];
			SaveToCurrentSlotMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Quick Save"];
			LoadCurrentSlotMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Quick Load"];

			SelectSlot1MenuItem.Checked = Config.SaveSlot == 1;
			SelectSlot2MenuItem.Checked = Config.SaveSlot == 2;
			SelectSlot3MenuItem.Checked = Config.SaveSlot == 3;
			SelectSlot4MenuItem.Checked = Config.SaveSlot == 4;
			SelectSlot5MenuItem.Checked = Config.SaveSlot == 5;
			SelectSlot6MenuItem.Checked = Config.SaveSlot == 6;
			SelectSlot7MenuItem.Checked = Config.SaveSlot == 7;
			SelectSlot8MenuItem.Checked = Config.SaveSlot == 8;
			SelectSlot9MenuItem.Checked = Config.SaveSlot == 9;
			SelectSlot0MenuItem.Checked = Config.SaveSlot is 10;
		}

		private void SaveRamSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			FlushSaveRAMMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Flush SaveRAM"];
		}

		private void MovieSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			StopMovieWithoutSavingMenuItem.Enabled = MovieSession.Movie.IsActive() && MovieSession.Movie.Changes;
			StopMovieMenuItem.Enabled
				= SaveMovieMenuItem.Enabled
				= SaveMovieAsMenuItem.Enabled
				= MovieSession.Movie.IsActive();

			ReadonlyMenuItem.Checked = MovieSession.ReadOnly;
			AutomaticallyBackupMoviesMenuItem.Checked = Config.Movies.EnableBackupMovies;
			FullMovieLoadstatesMenuItem.Checked = Config.Movies.VBAStyleMovieLoadState;

			ReadonlyMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Toggle read-only"];
			RecordMovieMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Record Movie"];
			PlayMovieMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Play Movie"];
			StopMovieMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Stop Movie"];
			PlayFromBeginningMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Play from beginning"];
			SaveMovieMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Save Movie"];

			PlayMovieMenuItem.Enabled
				= RecordMovieMenuItem.Enabled
				= RecentMovieSubMenu.Enabled
					= !Emulator.IsNull() && !Tools.IsLoaded<TAStudio>();

			PlayFromBeginningMenuItem.Enabled = MovieSession.Movie.IsActive() && !Tools.IsLoaded<TAStudio>();
		}

		private void RecentMovieSubMenu_DropDownOpened(object sender, EventArgs e)
			=> RecentMovieSubMenu.ReplaceDropDownItems(Config.RecentMovies.RecentMenu(this, LoadMoviesFromRecent, "Movie"));

		private void MovieEndSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			MovieEndFinishMenuItem.Checked = Config.Movies.MovieEndAction == MovieEndAction.Finish;
			MovieEndRecordMenuItem.Checked = Config.Movies.MovieEndAction == MovieEndAction.Record;
			MovieEndStopMenuItem.Checked = Config.Movies.MovieEndAction == MovieEndAction.Stop;
			MovieEndPauseMenuItem.Checked = Config.Movies.MovieEndAction == MovieEndAction.Pause;

			// Arguably an IControlMainForm property should be set here, but in reality only Tastudio is ever going to interfere with this logic
			MovieEndFinishMenuItem.Enabled =
			MovieEndRecordMenuItem.Enabled =
			MovieEndStopMenuItem.Enabled =
			MovieEndPauseMenuItem.Enabled =
				!Tools.Has<TAStudio>();
		}

		private void AVSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			ConfigAndRecordAVMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Record A/V"];
			StopAVMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Stop A/V"];
			CaptureOSDMenuItem.Checked = Config.AviCaptureOsd;
			CaptureLuaMenuItem.Checked = Config.AviCaptureLua || Config.AviCaptureOsd; // or with osd is for better compatibility with old config files

			RecordAVMenuItem.Enabled = !string.IsNullOrEmpty(Config.VideoWriter) && _currAviWriter == null;

			if (_currAviWriter == null)
			{
				ConfigAndRecordAVMenuItem.Enabled = true;
				StopAVMenuItem.Enabled = false;
			}
			else
			{
				ConfigAndRecordAVMenuItem.Enabled = false;
				StopAVMenuItem.Enabled = true;
			}
		}

		private void ScreenshotSubMenu_DropDownOpening(object sender, EventArgs e)
		{
			ScreenshotCaptureOSDMenuItem1.Checked = Config.ScreenshotCaptureOsd;
			ScreenshotMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Screenshot"];
			ScreenshotClipboardMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Screen Raw to Clipboard"];
			ScreenshotClientClipboardMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Screen Client to Clipboard"];
		}

		private void OpenRomMenuItem_Click(object sender, EventArgs e)
		{
			OpenRom();
		}

		private void OpenAdvancedMenuItem_Click(object sender, EventArgs e)
		{
			using var oac = new OpenAdvancedChooser(this, Config, CreateCoreComm, Game, RunLibretroCoreChooser);
			if (this.ShowDialogWithTempMute(oac) == DialogResult.Cancel) return;

			if (oac.Result == AdvancedRomLoaderType.LibretroLaunchNoGame)
			{
				_ = LoadRom(string.Empty, new LoadRomArgs(new OpenAdvanced_LibretroNoGame(Config.LibretroCore)));
				return;
			}

			IOpenAdvanced ioa;
			FilesystemFilterSet filter;
			if (oac.Result == AdvancedRomLoaderType.LibretroLaunchGame)
			{
				ioa = new OpenAdvanced_Libretro();
				filter = oac.SuggestedExtensionFilter!;
			}
			else if (oac.Result == AdvancedRomLoaderType.ClassicLaunchGame)
			{
				ioa = new OpenAdvanced_OpenRom();
				filter = RomLoader.RomFilter;
			}
			else if (oac.Result == AdvancedRomLoaderType.MameLaunchGame)
			{
				ioa = new OpenAdvanced_MAME();
				filter = MAMERomsFSFilterSet;
			}
			else
			{
				throw new InvalidOperationException("Automatic Alpha Sanitizer");
			}
			var result = this.ShowFileOpenDialog(
				filter: filter,
				filterIndex: ref _lastOpenRomFilter,
				initDir: Config.PathEntries.RomAbsolutePath(Emulator.SystemId),
				windowTitle: "Open Advanced");
			if (result is null) return;
			FileInfo file = new(result);
			Config.PathEntries.LastRomPath = file.DirectoryName;
			_ = LoadRom(file.FullName, new LoadRomArgs(ioa));
		}

		private void CloseRomMenuItem_Click(object sender, EventArgs e)
		{
			Console.WriteLine($"Closing rom clicked Frame: {Emulator.Frame} Emulator: {Emulator.GetType().Name}");
			CloseRom();
			Console.WriteLine($"Closing rom clicked DONE Frame: {Emulator.Frame} Emulator: {Emulator.GetType().Name}");
		}

		private void QuickSavestateMenuItem_Click(object sender, EventArgs e)
			=> SaveQuickSave(int.Parse(((ToolStripMenuItem) sender).Text));

		private void SaveNamedStateMenuItem_Click(object sender, EventArgs e) => SaveStateAs();

		private void QuickLoadstateMenuItem_Click(object sender, EventArgs e)
			=> LoadQuickSave(int.Parse(((ToolStripMenuItem) sender).Text));

		private void LoadNamedStateMenuItem_Click(object sender, EventArgs e) => LoadStateAs();

		private void AutoloadLastSlotMenuItem_Click(object sender, EventArgs e)
			=> Config.AutoLoadLastSaveSlot = !Config.AutoLoadLastSaveSlot;

		private void AutosaveLastSlotMenuItem_Click(object sender, EventArgs e)
			=> Config.AutoSaveLastSaveSlot = !Config.AutoSaveLastSaveSlot;

		private void SelectSlotMenuItems_Click(object sender, EventArgs e)
		{
			if (sender == SelectSlot1MenuItem) Config.SaveSlot = 1;
			else if (sender == SelectSlot2MenuItem) Config.SaveSlot = 2;
			else if (sender == SelectSlot3MenuItem) Config.SaveSlot = 3;
			else if (sender == SelectSlot4MenuItem) Config.SaveSlot = 4;
			else if (sender == SelectSlot5MenuItem) Config.SaveSlot = 5;
			else if (sender == SelectSlot6MenuItem) Config.SaveSlot = 6;
			else if (sender == SelectSlot7MenuItem) Config.SaveSlot = 7;
			else if (sender == SelectSlot8MenuItem) Config.SaveSlot = 8;
			else if (sender == SelectSlot9MenuItem) Config.SaveSlot = 9;
			else if (sender == SelectSlot0MenuItem) Config.SaveSlot = 10;

			UpdateStatusSlots();
			SaveSlotSelectedMessage();
		}

		private void PreviousSlotMenuItem_Click(object sender, EventArgs e)
		{
			PreviousSlot();
		}

		private void NextSlotMenuItem_Click(object sender, EventArgs e)
		{
			NextSlot();
		}

		private void SaveToCurrentSlotMenuItem_Click(object sender, EventArgs e)
			=> SavestateCurrentSlot();

		private void SavestateCurrentSlot()
			=> SaveQuickSave(Config.SaveSlot);

		private void LoadCurrentSlotMenuItem_Click(object sender, EventArgs e)
			=> LoadstateCurrentSlot();

		private bool LoadstateCurrentSlot()
			=> LoadQuickSave(Config.SaveSlot);

		private void FlushSaveRAMMenuItem_Click(object sender, EventArgs e)
		{
			FlushSaveRAM();
		}

		private void ReadonlyMenuItem_Click(object sender, EventArgs e)
		{
			ToggleReadOnly();
		}

		private void RecordMovieMenuItem_Click(object sender, EventArgs e)
		{
			if (Game.IsNullInstance()) return;
			if (!Emulator.Attributes().Released)
			{
				var result = this.ModalMessageBox2(
					"Thanks for using BizHawk!  The emulation core you have selected "
						+ "is currently BETA-status.  We appreciate your help in testing BizHawk. "
						+ "You can record a movie on this core if you'd like to, but expect to "
						+ "encounter bugs and sync problems.  Continue?",
					"BizHawk");

				if (!result)
				{
					return;
				}
			}

			// Nag user to user a more accurate core, but let them continue anyway
			EnsureCoreIsAccurate();

			using var form = new RecordMovie(this, Config, Game, Emulator, MovieSession);
			this.ShowDialogWithTempMute(form);
		}

		private string CanProvideFirmware(FirmwareID id, string hash)
			=> FirmwareManager.Resolve(
				Config.PathEntries,
				Config.FirmwareUserSpecifications,
				FirmwareDatabase.FirmwareRecords.First(fr => fr.ID == id),
//				exactFile: hash, //TODO re-scan FW dir for this file, then try autopatching
				forbidScan: true)?.Hash;

		private void PlayMovieMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new PlayMovie(this, Config, Game, Emulator, MovieSession, CanProvideFirmware);
			this.ShowDialogWithTempMute(form);
		}

		private void StopMovieMenuItem_Click(object sender, EventArgs e)
		{
			StopMovie();
		}

		private void PlayFromBeginningMenuItem_Click(object sender, EventArgs e)
			=> _ = RestartMovie();

		private void ImportMovieMenuItem_Click(object sender, EventArgs e)
		{
			var result = this.ShowFileMultiOpenDialog(
				discardCWDChange: false,
				filter: MovieImport.AvailableImporters,
				initDir: Config.PathEntries.RomAbsolutePath(Emulator.SystemId));
			if (result is not null) foreach (var fn in result) ProcessMovieImport(fn, false);
		}

		private void SaveMovieMenuItem_Click(object sender, EventArgs e)
		{
			SaveMovie();
		}

		private void SaveMovieAsMenuItem_Click(object sender, EventArgs e)
		{
			var filename = MovieSession.Movie.Filename;
			if (string.IsNullOrWhiteSpace(filename))
			{
				filename = Game.FilesystemSafeName();
			}

			var file = ToolFormBase.SaveFileDialog(
				currentFile: filename,
				path: Config.PathEntries.MovieAbsolutePath(),
				MovieSession.Movie.GetFSFilterSet(),
				this);

			if (file != null)
			{
				MovieSession.Movie.Filename = file.FullName;
				Config.RecentMovies.Add(MovieSession.Movie.Filename);
				SaveMovie();
			}
		}

		private void StopMovieWithoutSavingMenuItem_Click(object sender, EventArgs e)
		{
			if (Config.Movies.EnableBackupMovies)
			{
				MovieSession.Movie.SaveBackup();
			}

			StopMovie(saveChanges: false);
		}

		private void AutomaticMovieBackupMenuItem_Click(object sender, EventArgs e)
			=> Config.Movies.EnableBackupMovies = !Config.Movies.EnableBackupMovies;

		private void FullMovieLoadstatesMenuItem_Click(object sender, EventArgs e)
			=> Config.Movies.VBAStyleMovieLoadState = !Config.Movies.VBAStyleMovieLoadState;

		private void MovieEndFinishMenuItem_Click(object sender, EventArgs e)
		{
			Config.Movies.MovieEndAction = MovieEndAction.Finish;
		}

		private void MovieEndRecordMenuItem_Click(object sender, EventArgs e)
		{
			Config.Movies.MovieEndAction = MovieEndAction.Record;
		}

		private void MovieEndStopMenuItem_Click(object sender, EventArgs e)
		{
			Config.Movies.MovieEndAction = MovieEndAction.Stop;
		}

		private void MovieEndPauseMenuItem_Click(object sender, EventArgs e)
		{
			Config.Movies.MovieEndAction = MovieEndAction.Pause;
		}

		private void ConfigAndRecordAVMenuItem_Click(object sender, EventArgs e)
		{
			if (OSTailoredCode.IsUnixHost)
			{
				using MsgBox dialog = new("Most of these options will cause crashes on Linux.", "A/V instability warning", MessageBoxIcon.Warning);
				this.ShowDialogWithTempMute(dialog);
			}
			RecordAv();
		}

		private void RecordAVMenuItem_Click(object sender, EventArgs e)
		{
			RecordAv(null, null); // force unattended, but allow traditional setup
		}

		private void StopAVMenuItem_Click(object sender, EventArgs e)
		{
			StopAv();
		}

		private void CaptureOSDMenuItem_Click(object sender, EventArgs e)
		{
			bool c = ((ToolStripMenuItem)sender).Checked;
			Config.AviCaptureOsd = c;
			if (c) // Logic to capture OSD w/o Lua does not currently exist, so disallow that.
				Config.AviCaptureLua = true;
		}

		private void CaptureLuaMenuItem_Click(object sender, EventArgs e)
		{
			bool c = ((ToolStripMenuItem)sender).Checked;
			Config.AviCaptureLua = c;
			if (!c) // Logic to capture OSD w/o Lua does not currently exist, so disallow that.
				Config.AviCaptureOsd = false;
		}

		private void ScreenshotMenuItem_Click(object sender, EventArgs e)
		{
			TakeScreenshot();
		}

		private void ScreenshotAsMenuItem_Click(object sender, EventArgs e)
		{
			var (dir, file) = $"{ScreenshotPrefix()}.{DateTime.Now:yyyy-MM-dd HH.mm.ss}.png".SplitPathToDirAndFile();
			var result = this.ShowFileSaveDialog(
				filter: ScreenshotsFSFilterSet,
				initDir: dir,
				initFileName: file);
			if (result is not null) TakeScreenshot(result);
		}

		private void ScreenshotClipboardMenuItem_Click(object sender, EventArgs e)
		{
			TakeScreenshotToClipboard();
		}

		private void ScreenshotClientClipboardMenuItem_Click(object sender, EventArgs e)
		{
			TakeScreenshotClientToClipboard();
		}

		private void ScreenshotCaptureOSDMenuItem_Click(object sender, EventArgs e)
			=> Config.ScreenshotCaptureOsd = !Config.ScreenshotCaptureOsd;

		private void ExitMenuItem_Click(object sender, EventArgs e)
		{
			if (Tools.AskSave())
			{
				Close();
			}
		}

		private void ScheduleShutdown()
			=> _exitRequestPending = true;

		public void CloseEmulator(int? exitCode = null)
		{
			ScheduleShutdown();
			if (exitCode != null) _exitCode = exitCode.Value;
		}

		private void EmulationMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			PauseMenuItem.Checked = _didMenuPause ? _wasPaused : EmulatorPaused;

			SoftResetMenuItem.Enabled = Emulator.ControllerDefinition.BoolButtons.Contains("Reset")
				&& !MovieSession.Movie.IsPlaying();

			HardResetMenuItem.Enabled = Emulator.ControllerDefinition.BoolButtons.Contains("Power")
				&& !MovieSession.Movie.IsPlaying();

			PauseMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Pause"];
			RebootCoreMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Reboot Core"];
			SoftResetMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Soft Reset"];
			HardResetMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Hard Reset"];
		}

		private void PauseMenuItem_Click(object sender, EventArgs e)
		{
			if (Config.PauseWhenMenuActivated && sender == PauseMenuItem)
			{
				const string ERR_MSG = nameof(PauseMenuItem_Click) + " ran before " + nameof(MaybeUnpauseFromMenuClosed) + "?";
				Debug.Assert(EmulatorPaused == _wasPaused, ERR_MSG);
				// fall through
			}
			TogglePause();
		}

		private void PowerMenuItem_Click(object sender, EventArgs e)
		{
			RebootCore();
		}

		private void SoftResetMenuItem_Click(object sender, EventArgs e)
		{
			SoftReset();
		}

		private void HardResetMenuItem_Click(object sender, EventArgs e)
		{
			HardReset();
		}

		private void ViewSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			DisplayFPSMenuItem.Checked = Config.DisplayFps;
			DisplayFrameCounterMenuItem.Checked = Config.DisplayFrameCounter;
			DisplayLagCounterMenuItem.Checked = Config.DisplayLagCounter;
			DisplayInputMenuItem.Checked = Config.DisplayInput;
			DisplayRerecordCountMenuItem.Checked = Config.DisplayRerecordCount;
			DisplaySubtitlesMenuItem.Checked = Config.DisplaySubtitles;

			DisplayFPSMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Display FPS"];
			DisplayFrameCounterMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Frame Counter"];
			DisplayLagCounterMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Lag Counter"];
			DisplayInputMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Input Display"];
			SwitchToFullscreenMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Full Screen"];

			DisplayStatusBarMenuItem.Checked = Config.DispChromeStatusBarWindowed;
			DisplayLogWindowMenuItem.Checked = Tools.IsLoaded<LogWindow>();

			DisplayLagCounterMenuItem.Enabled = Emulator.CanPollInput();

			DisplayMessagesMenuItem.Checked = Config.DisplayMessages;
		}

		private void WindowSizeSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			var windowScale = Config.GetWindowScaleFor(Emulator.SystemId);
			foreach (var item in WindowSizeSubMenu.DropDownItems)
			{
				// filter out separators
				if (item is ToolStripMenuItem menuItem && menuItem.Tag is int itemScale)
				{
					menuItem.Checked = itemScale == windowScale && Config.ResizeWithFramebuffer;
				}
			}
			DisableResizeWithFramebufferMenuItem.Checked = !Config.ResizeWithFramebuffer;
		}

		private void DisableResizeWithFramebufferMenuItem_Click(object sender, EventArgs e)
		{
			Config.ResizeWithFramebuffer = !DisableResizeWithFramebufferMenuItem.Checked;
			FrameBufferResized();
		}

		private void WindowSize_Click(object sender, EventArgs e)
		{
			Config.SetWindowScaleFor(Emulator.SystemId, (int) ((ToolStripMenuItem) sender).Tag);
			FrameBufferResized(forceWindowResize: true);
		}

		private void SwitchToFullscreenMenuItem_Click(object sender, EventArgs e)
		{
			ToggleFullscreen();
		}

		private void DisplayFpsMenuItem_Click(object sender, EventArgs e)
		{
			ToggleFps();
		}

		private void DisplayFrameCounterMenuItem_Click(object sender, EventArgs e)
		{
			ToggleFrameCounter();
		}

		private void DisplayLagCounterMenuItem_Click(object sender, EventArgs e)
		{
			ToggleLagCounter();
		}

		private void DisplayInputMenuItem_Click(object sender, EventArgs e)
		{
			ToggleInputDisplay();
		}

		private void DisplayRerecordsMenuItem_Click(object sender, EventArgs e)
			=> Config.DisplayRerecordCount = !Config.DisplayRerecordCount;

		private void DisplaySubtitlesMenuItem_Click(object sender, EventArgs e)
			=> Config.DisplaySubtitles = !Config.DisplaySubtitles;

		private void DisplayStatusBarMenuItem_Click(object sender, EventArgs e)
		{
			Config.DispChromeStatusBarWindowed = !Config.DispChromeStatusBarWindowed;
			SetStatusBar();
		}

		private void DisplayMessagesMenuItem_Click(object sender, EventArgs e)
			=> Config.DisplayMessages = !Config.DisplayMessages;

		private void DisplayLogWindowMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<LogWindow>();
		}

		private void ConfigSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			ControllersMenuItem.Enabled = Emulator.ControllerDefinition.Any();
			RewindOptionsMenuItem.Enabled = Emulator.HasSavestates();
		}

		private void FrameSkipMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			MinimizeSkippingMenuItem.Checked = Config.AutoMinimizeSkipping;
			ClockThrottleMenuItem.Checked = Config.ClockThrottle;
			VsyncThrottleMenuItem.Checked = Config.VSyncThrottle;
			NeverSkipMenuItem.Checked = Config.FrameSkip == 0;
			Frameskip1MenuItem.Checked = Config.FrameSkip == 1;
			Frameskip2MenuItem.Checked = Config.FrameSkip == 2;
			Frameskip3MenuItem.Checked = Config.FrameSkip == 3;
			Frameskip4MenuItem.Checked = Config.FrameSkip == 4;
			Frameskip5MenuItem.Checked = Config.FrameSkip == 5;
			Frameskip6MenuItem.Checked = Config.FrameSkip == 6;
			Frameskip7MenuItem.Checked = Config.FrameSkip == 7;
			Frameskip8MenuItem.Checked = Config.FrameSkip == 8;
			Frameskip9MenuItem.Checked = Config.FrameSkip == 9;
			MinimizeSkippingMenuItem.Enabled = !NeverSkipMenuItem.Checked;
			if (!MinimizeSkippingMenuItem.Enabled)
			{
				MinimizeSkippingMenuItem.Checked = true;
			}

			AudioThrottleMenuItem.Enabled = Config.SoundEnabled;
			AudioThrottleMenuItem.Checked = Config.SoundThrottle;
			VsyncEnabledMenuItem.Checked = Config.VSync;

			Speed100MenuItem.Checked = Config.SpeedPercent == 100;
			Speed100MenuItem.Image = (Config.SpeedPercentAlternate == 100) ? Properties.Resources.FastForward : null;
			Speed150MenuItem.Checked = Config.SpeedPercent == 150;
			Speed150MenuItem.Image = (Config.SpeedPercentAlternate == 150) ? Properties.Resources.FastForward : null;
			Speed400MenuItem.Checked = Config.SpeedPercent == 400;
			Speed400MenuItem.Image = (Config.SpeedPercentAlternate == 400) ? Properties.Resources.FastForward : null;
			Speed200MenuItem.Checked = Config.SpeedPercent == 200;
			Speed200MenuItem.Image = (Config.SpeedPercentAlternate == 200) ? Properties.Resources.FastForward : null;
			Speed75MenuItem.Checked = Config.SpeedPercent == 75;
			Speed75MenuItem.Image = (Config.SpeedPercentAlternate == 75) ? Properties.Resources.FastForward : null;
			Speed50MenuItem.Checked = Config.SpeedPercent == 50;
			Speed50MenuItem.Image = (Config.SpeedPercentAlternate == 50) ? Properties.Resources.FastForward : null;

			Speed50MenuItem.Enabled =
				Speed75MenuItem.Enabled =
				Speed100MenuItem.Enabled =
				Speed150MenuItem.Enabled =
				Speed200MenuItem.Enabled =
				Speed400MenuItem.Enabled =
				Config.ClockThrottle;

			miUnthrottled.Checked = Config.Unthrottled;
		}

		private void KeyPriorityMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			BothHkAndControllerMenuItem.Checked = false;
			InputOverHkMenuItem.Checked = false;
			HkOverInputMenuItem.Checked = false;

			switch (Config.InputHotkeyOverrideOptions)
			{
				default:
				case 0:
					BothHkAndControllerMenuItem.Checked = true;
					break;
				case 1:
					InputOverHkMenuItem.Checked = true;
					break;
				case 2:
					HkOverInputMenuItem.Checked = true;
					break;
			}
		}

		private void ControllersMenuItem_Click(object sender, EventArgs e)
		{
			using var controller = new ControllerConfig(this, Emulator, Config);
			if (!this.ShowDialogWithTempMute(controller).IsOk()) return;
			AddOnScreenMessage("Controller settings saved");

			InitControls();
			InputManager.SyncControls(Emulator, MovieSession, Config);
		}

		private void HotkeysMenuItem_Click(object sender, EventArgs e)
		{
			using var hotkeyConfig = new HotkeyConfig(Config);
			if (!this.ShowDialogWithTempMute(hotkeyConfig).IsOk()) return;
			AddOnScreenMessage("Hotkey settings saved");

			InitControls();
			InputManager.SyncControls(Emulator, MovieSession, Config);
		}

		private void OpenFWConfigRomLoadFailed(RomLoader.RomErrorArgs args)
		{
			using FirmwareConfig configForm = new(
				this,
				FirmwareManager,
				Config.FirmwareUserSpecifications,
				Config.PathEntries,
				retryLoadRom: true,
				reloadRomPath: args.RomPath);
			args.Retry = this.ShowDialogWithTempMute(configForm) is DialogResult.Retry;
		}

		private void FirmwareMenuItem_Click(object sender, EventArgs e)
		{
			using var configForm = new FirmwareConfig(this, FirmwareManager, Config.FirmwareUserSpecifications, Config.PathEntries);
			this.ShowDialogWithTempMute(configForm);
		}

		private void MessagesMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new MessageConfig(Config);
			if (this.ShowDialogWithTempMute(form).IsOk()) AddOnScreenMessage("Message settings saved");
		}

		private void PathsMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new PathConfig(Config.PathEntries, Game.System, newPath => MovieSession.BackupDirectory = newPath);
			if (this.ShowDialogWithTempMute(form).IsOk()) AddOnScreenMessage("Path settings saved");
		}

		private void SoundMenuItem_Click(object sender, EventArgs e)
		{
			static IEnumerable<string> GetDeviceNamesCallback(ESoundOutputMethod outputMethod) => outputMethod switch
			{
				ESoundOutputMethod.XAudio2 => XAudio2SoundOutput.GetDeviceNames(),
				ESoundOutputMethod.OpenAL => OpenALSoundOutput.GetDeviceNames(),
				_ => Enumerable.Empty<string>()
			};
			var oldOutputMethod = Config.SoundOutputMethod;
			var oldDevice = Config.SoundDevice;
			using var form = new SoundConfig(this, Config, GetDeviceNamesCallback);
			if (!this.ShowDialogWithTempMute(form).IsOk()) return;

			AddOnScreenMessage("Sound settings saved");
			if (Config.SoundOutputMethod == oldOutputMethod && Config.SoundDevice == oldDevice)
			{
				Sound.StopSound();
			}
			else
			{
				Sound.Dispose();
				Sound = new Sound(Config, () => Emulator.VsyncRate());
			}
			Sound.StartSound();
			RewireSound();
		}

		private void AutofireMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new AutofireConfig(Config, InputManager.AutoFireController, InputManager.StickyAutofireController);
			if (this.ShowDialogWithTempMute(form).IsOk()) AddOnScreenMessage("Autofire settings saved");
		}

		private void RewindOptionsMenuItem_Click(object sender, EventArgs e)
		{
			if (!Emulator.HasSavestates()) return;
			using RewindConfig form = new(
				Config,
				PlatformFrameRates.GetFrameRate(Emulator.SystemId, Emulator.HasRegions() && Emulator.AsRegionable().Region is DisplayType.PAL), // why isn't there a helper for this
				Emulator.AsStatable(),
				CreateRewinder,
				() => this.Rewinder);
			if (this.ShowDialogWithTempMute(form).IsOk()) AddOnScreenMessage("Rewind and State settings saved");
		}

		private void FileExtensionsMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new FileExtensionPreferences(Config.PreferredPlatformsForExtensions);
			if (this.ShowDialogWithTempMute(form).IsOk()) AddOnScreenMessage("Rom Extension Preferences changed");
		}

		private void BumpAutoFlushSaveRamTimer()
		{
			if (AutoFlushSaveRamIn > Config.FlushSaveRamFrames)
			{
				AutoFlushSaveRamIn = Config.FlushSaveRamFrames;
			}
		}

		private void CustomizeMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new EmuHawkOptions(Config, BumpAutoFlushSaveRamTimer);
			if (!this.ShowDialogWithTempMute(form).IsOk()) return;
			AddOnScreenMessage("Custom configurations saved.");
		}

		private void ProfilesMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new ProfileConfig(Config, this);
			if (!this.ShowDialogWithTempMute(form).IsOk()) return;
			AddOnScreenMessage("Profile settings saved");

			// We hide the FirstBoot items since the user setup a Profile
			// Is it a bad thing to do this constantly?
			Config.FirstBoot = false;
			ProfileFirstBootLabel.Visible = false;
		}

		private void ClockThrottleMenuItem_Click(object sender, EventArgs e)
		{
			Config.ClockThrottle = !Config.ClockThrottle;
			if (Config.ClockThrottle)
			{
				var old = Config.SoundThrottle;
				Config.SoundThrottle = false;
				if (old)
				{
					RewireSound();
				}

				old = Config.VSyncThrottle;
				Config.VSyncThrottle = false;
				if (old)
				{
					_presentationPanel.Resized = true;
				}
			}

			ThrottleMessage();
		}

		private void AudioThrottleMenuItem_Click(object sender, EventArgs e)
		{
			Config.SoundThrottle = !Config.SoundThrottle;
			RewireSound();
			if (Config.SoundThrottle)
			{
				Config.ClockThrottle = false;
				var old = Config.VSyncThrottle;
				Config.VSyncThrottle = false;
				if (old)
				{
					_presentationPanel.Resized = true;
				}
			}

			ThrottleMessage();
		}

		private void VsyncThrottleMenuItem_Click(object sender, EventArgs e)
		{
			Config.VSyncThrottle = !Config.VSyncThrottle;
			_presentationPanel.Resized = true;
			if (Config.VSyncThrottle)
			{
				Config.ClockThrottle = false;
				var old = Config.SoundThrottle;
				Config.SoundThrottle = false;
				if (old)
				{
					RewireSound();
				}
			}

			if (!Config.VSync)
			{
				Config.VSync = true;
				VsyncMessage();
			}

			ThrottleMessage();
		}

		private void VsyncEnabledMenuItem_Click(object sender, EventArgs e)
		{
			Config.VSync = !Config.VSync;
			if (!Config.VSyncThrottle) // when vsync throttle is on, vsync is forced to on, so no change to make here
			{
				_presentationPanel.Resized = true;
			}

			VsyncMessage();
		}

		private void UnthrottledMenuItem_Click(object sender, EventArgs e)
			=> ToggleUnthrottled();

		private void ToggleUnthrottled()
		{
			Config.Unthrottled = !Config.Unthrottled;
			ThrottleMessage();
		}

		private void MinimizeSkippingMenuItem_Click(object sender, EventArgs e)
			=> Config.AutoMinimizeSkipping = !Config.AutoMinimizeSkipping;

		private void NeverSkipMenuItem_Click(object sender, EventArgs e) { Config.FrameSkip = 0; FrameSkipMessage(); }
		private void Frameskip1MenuItem_Click(object sender, EventArgs e) { Config.FrameSkip = 1; FrameSkipMessage(); }
		private void Frameskip2MenuItem_Click(object sender, EventArgs e) { Config.FrameSkip = 2; FrameSkipMessage(); }
		private void Frameskip3MenuItem_Click(object sender, EventArgs e) { Config.FrameSkip = 3; FrameSkipMessage(); }
		private void Frameskip4MenuItem_Click(object sender, EventArgs e) { Config.FrameSkip = 4; FrameSkipMessage(); }
		private void Frameskip5MenuItem_Click(object sender, EventArgs e) { Config.FrameSkip = 5; FrameSkipMessage(); }
		private void Frameskip6MenuItem_Click(object sender, EventArgs e) { Config.FrameSkip = 6; FrameSkipMessage(); }
		private void Frameskip7MenuItem_Click(object sender, EventArgs e) { Config.FrameSkip = 7; FrameSkipMessage(); }
		private void Frameskip8MenuItem_Click(object sender, EventArgs e) { Config.FrameSkip = 8; FrameSkipMessage(); }
		private void Frameskip9MenuItem_Click(object sender, EventArgs e) { Config.FrameSkip = 9; FrameSkipMessage(); }

		private void Speed50MenuItem_Click(object sender, EventArgs e) => ClickSpeedItem(50);
		private void Speed75MenuItem_Click(object sender, EventArgs e) => ClickSpeedItem(75);
		private void Speed100MenuItem_Click(object sender, EventArgs e) => ClickSpeedItem(100);
		private void Speed150MenuItem_Click(object sender, EventArgs e) => ClickSpeedItem(150);
		private void Speed200MenuItem_Click(object sender, EventArgs e) => ClickSpeedItem(200);
		private void Speed400MenuItem_Click(object sender, EventArgs e) => ClickSpeedItem(400);

		private void BothHkAndControllerMenuItem_Click(object sender, EventArgs e)
		{
			Config.InputHotkeyOverrideOptions = 0;
			UpdateKeyPriorityIcon();
		}

		private void InputOverHkMenuItem_Click(object sender, EventArgs e)
		{
			Config.InputHotkeyOverrideOptions = 1;
			UpdateKeyPriorityIcon();
		}

		private void HkOverInputMenuItem_Click(object sender, EventArgs e)
		{
			Config.InputHotkeyOverrideOptions = 2;
			UpdateKeyPriorityIcon();
		}

		private void SaveConfigMenuItem_Click(object sender, EventArgs e)
		{
			SaveConfig();
			AddOnScreenMessage("Saved settings");
		}

		private void SaveConfigAsMenuItem_Click(object sender, EventArgs e)
		{
			var (dir, file) = _getConfigPath().SplitPathToDirAndFile();
			var result = this.ShowFileSaveDialog(
				filter: ConfigFileFSFilterSet,
				initDir: dir,
				initFileName: file);
			if (result is not null)
			{
				SaveConfig(result);
				AddOnScreenMessage("Copied settings");
			}
		}

		private void LoadConfigMenuItem_Click(object sender, EventArgs e)
		{
			LoadConfigFile(_getConfigPath());
		}

		private void LoadConfigFromMenuItem_Click(object sender, EventArgs e)
		{
			var (dir, file) = _getConfigPath().SplitPathToDirAndFile();
			var result = this.ShowFileOpenDialog(filter: ConfigFileFSFilterSet, initDir: dir!, initFileName: file!);
			if (result is not null) LoadConfigFile(result);
		}

		private void ToolsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			ToolBoxMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["ToolBox"];
			RamWatchMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["RAM Watch"];
			RamSearchMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["RAM Search"];
			HexEditorMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Hex Editor"];
			LuaConsoleMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Lua Console"];
			CheatsMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Cheats"];
			TAStudioMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["TAStudio"];
			VirtualPadMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Virtual Pad"];
			TraceLoggerMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Trace Logger"];
			TraceLoggerMenuItem.Enabled = Tools.IsAvailable<TraceLogger>();
			CodeDataLoggerMenuItem.Enabled = Tools.IsAvailable<CDL>();

			TAStudioMenuItem.Enabled = Tools.IsAvailable<TAStudio>();

			CheatsMenuItem.Enabled = Tools.IsAvailable<Cheats>();
			HexEditorMenuItem.Enabled = Tools.IsAvailable<HexEditor>();
			RamSearchMenuItem.Enabled = Tools.IsAvailable<RamSearch>();
			RamWatchMenuItem.Enabled = Tools.IsAvailable<RamWatch>();

			DebuggerMenuItem.Enabled = Tools.IsAvailable<GenericDebugger>();

			BatchRunnerMenuItem.Visible = VersionInfo.DeveloperBuild;

			BasicBotMenuItem.Enabled = Tools.IsAvailable<BasicBot>();

			GameSharkConverterMenuItem.Enabled = Tools.IsAvailable<GameShark>();
			MacroToolMenuItem.Enabled = MovieSession.Movie.IsActive() && Tools.IsAvailable<MacroInputTool>();
			VirtualPadMenuItem.Enabled = Emulator.ControllerDefinition.Any();
		}

		private void ExternalToolMenuItem_DropDownOpening(object sender, EventArgs e)
		{
			ExternalToolMenuItem.DropDownItems.Clear();
			ExternalToolMenuItem.DropDownItems.AddRange(ExtToolManager.ToolStripItems.ToArray());
			if (ExternalToolMenuItem.DropDownItems.Count == 0)
			{
				ExternalToolMenuItem.DropDownItems.Add(new ToolStripMenuItemEx { Enabled = false, Text = "(none)" });
			}
			if (Config.TrustedExtTools.Count is 0) return;

			ExternalToolMenuItem.DropDownItems.Add(new ToolStripSeparatorEx());
			ToolStripMenuItemEx forgetTrustedItem = new() { Text = "Forget Trusted Tools" };
			forgetTrustedItem.Click += (_, _) =>
			{
				if (this.ModalMessageBox2(
					caption: "Forget trusted ext. tools?",
					text: "This will cause the warning about running third-party code to show again for all the ext. tools you've previously loaded.\n" +
						"(If a tool has been loaded this session, the warning may not appear until EmuHawk is restarted.)",
					useOKCancel: true))
				{
					Config.TrustedExtTools.Clear();
				}
			};
			ExternalToolMenuItem.DropDownItems.Add(forgetTrustedItem);
		}

		private void ToolBoxMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<ToolBox>();
		}

		private void RamWatchMenuItem_Click(object sender, EventArgs e)
		{
			Tools.LoadRamWatch(true);
		}

		private void RamSearchMenuItem_Click(object sender, EventArgs e) => Tools.Load<RamSearch>();

		private void LuaConsoleMenuItem_Click(object sender, EventArgs e)
		{
			OpenLuaConsole();
		}

		private void TAStudioMenuItem_Click(object sender, EventArgs e)
		{
			if (!Emulator.CanPollInput())
			{
				ShowMessageBox(owner: null, "Current core does not support input polling. TAStudio can't be used.");
				return;
			}
			const int DONT_PROMPT_BEFORE_FRAME = 2 * 60 * 60; // 2 min @ 60 fps
			if (MovieSession.Movie.NotActive() && Emulator.Frame > DONT_PROMPT_BEFORE_FRAME // if playing casually (not recording) AND played for enough frames (prompting always would be annoying)...
				&& !this.ModalMessageBox2("This will reload the rom without saving. Launch TAStudio anyway?", "Confirmation")) // ...AND user responds "No" to "Open TAStudio?", then cancel
			{
				return;
			}
			if (sender == TAStudioMPRToolStripMenuItem) Tools.Load<TAStudioMPR>();
			else Tools.Load<TAStudio>();
		}

		private void HexEditorMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<HexEditor>();
		}

		private void TraceLoggerMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<TraceLogger>();
		}

		private void DebuggerMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<GenericDebugger>();
		}

		private void CodeDataLoggerMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<CDL>();
		}

		private void MacroToolMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<MacroInputTool>();
		}

		private void VirtualPadMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<VirtualpadTool>();
		}

		private void BasicBotMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<BasicBot>();
		}

		private void CheatsMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<Cheats>();
		}

		private void CheatCodeConverterMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<GameShark>();
		}

		private void MultidiskBundlerMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<MultiDiskBundler>();
		}

		private void BatchRunnerMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new BatchRun(this, Config, CreateCoreComm);
			this.ShowDialogWithTempMute(form);
		}

		private void StartRetroAchievementsMenuItem_Click(object sender, EventArgs e)
		{
			OpenRetroAchievements();
		}

		private DialogResult OpenGenericCoreConfigFor<T>(string title)
			where T : IEmulator
			=> GenericCoreConfig.DoDialogFor(this, GetSettingsAdapterFor<T>(), title, isMovieActive: MovieSession.Movie.IsActive());

		private void OpenGenericCoreConfig()
			=> GenericCoreConfig.DoDialog(Emulator, this, isMovieActive: MovieSession.Movie.IsActive());

		private void GenericCoreSettingsMenuItem_Click(object sender, EventArgs e)
			=> OpenGenericCoreConfig();

		private void HelpSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			FeaturesMenuItem.Visible = VersionInfo.DeveloperBuild;
		}

		private void OnlineHelpMenuItem_Click(object sender, EventArgs e)
		{
			System.Diagnostics.Process.Start("https://tasvideos.org/BizHawk");
		}

		private void ForumsMenuItem_Click(object sender, EventArgs e)
		{
			System.Diagnostics.Process.Start("https://tasvideos.org/Forum/Subforum/64");
		}

		private void FeaturesMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<CoreFeatureAnalysis>();
		}

		private void AboutMenuItem_Click(object sender, EventArgs e)
		{
			var atten = Config.SoundEnabledNormal ? Config.SoundVolume / 100.0f : 0.0f;
			using BizBox form = new(() => Sound.PlayWavFile(Properties.Resources.GetNotHawkCallSFX(), atten));
			this.ShowDialogWithTempMute(form);
		}

		private void MainFormContextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
		{
			MaybePauseFromMenuOpened();

			OpenRomContextMenuItem.Visible = Emulator.IsNull() || _inFullscreen;

			bool showMenuVisible = _inFullscreen || !MainMenuStrip.Visible; // need to always be able to restore this as an emergency measure

			if (_argParser._chromeless)
			{
				showMenuVisible = true; // I decided this was always possible in chrome-less mode, we'll see what they think
			}

			var movieIsActive = MovieSession.Movie.IsActive();

			ShowMenuContextMenuItem.Visible =
				ShowMenuContextMenuSeparator.Visible =
				showMenuVisible;

			LoadLastRomContextMenuItem.Visible = Emulator.IsNull();

			StopAVContextMenuItem.Visible = _currAviWriter != null;

			ContextSeparator_AfterMovie.Visible =
				ContextSeparator_AfterUndo.Visible =
				ScreenshotContextMenuItem.Visible =
				CloseRomContextMenuItem.Visible =
				UndoSavestateContextMenuItem.Visible =
				!Emulator.IsNull();

			RecordMovieContextMenuItem.Visible =
				PlayMovieContextMenuItem.Visible =
				LoadLastMovieContextMenuItem.Visible =
				!Emulator.IsNull() && !movieIsActive;

			RestartMovieContextMenuItem.Visible =
				StopMovieContextMenuItem.Visible =
				ViewSubtitlesContextMenuItem.Visible =
				ViewCommentsContextMenuItem.Visible =
				SaveMovieContextMenuItem.Visible =
				SaveMovieAsContextMenuItem.Visible =
					movieIsActive;

			BackupMovieContextMenuItem.Visible = movieIsActive;

			StopNoSaveContextMenuItem.Visible = movieIsActive && MovieSession.Movie.Changes;

			AddSubtitleContextMenuItem.Visible = !Emulator.IsNull() && movieIsActive && !MovieSession.ReadOnly;

			ConfigContextMenuItem.Visible = _inFullscreen;

			ClearSRAMContextMenuItem.Visible = File.Exists(Config.PathEntries.SaveRamAbsolutePath(Game, MovieSession.Movie));

			ContextSeparator_AfterROM.Visible = OpenRomContextMenuItem.Visible || LoadLastRomContextMenuItem.Visible;

			LoadLastRomContextMenuItem.Enabled = !Config.RecentRoms.Empty;
			LoadLastMovieContextMenuItem.Enabled = !Config.RecentMovies.Empty;

			if (movieIsActive)
			{
				if (MovieSession.ReadOnly)
				{
					ViewSubtitlesContextMenuItem.Text = "View Subtitles";
					ViewCommentsContextMenuItem.Text = "View Comments";
				}
				else
				{
					ViewSubtitlesContextMenuItem.Text = "Edit Subtitles";
					ViewCommentsContextMenuItem.Text = "Edit Comments";
				}
			}

			var file = new FileInfo($"{SaveStatePrefix()}.QuickSave{Config.SaveSlot % 10}.State.bak");

			if (file.Exists)
			{
				UndoSavestateContextMenuItem.Enabled = true;
				if (_stateSlots.IsRedo(MovieSession.Movie, Config.SaveSlot))
				{
					UndoSavestateContextMenuItem.Text = $"Redo Save to slot {Config.SaveSlot}";
					UndoSavestateContextMenuItem.Image = Properties.Resources.Redo;
				}
				else
				{
					UndoSavestateContextMenuItem.Text = $"Undo Save to slot {Config.SaveSlot}";
					UndoSavestateContextMenuItem.Image = Properties.Resources.Undo;
				}
			}
			else
			{
				UndoSavestateContextMenuItem.Enabled = false;
				UndoSavestateContextMenuItem.Text = "Undo Savestate";
				UndoSavestateContextMenuItem.Image = Properties.Resources.Undo;
			}

			ShowMenuContextMenuItem.Text = MainMenuStrip.Visible ? "Hide Menu" : "Show Menu";
		}

		private void MainFormContextMenu_Closing(object sender, ToolStripDropDownClosingEventArgs e)
			=> MaybeUnpauseFromMenuClosed();

		private void DisplayConfigMenuItem_Click(object sender, EventArgs e)
		{
			using DisplayConfig window = new(Config, DialogController, GL);
			if (this.ShowDialogWithTempMute(window).IsOk())
			{
				DisplayManager.RefreshUserShader();
				FrameBufferResized();
				SynchChrome();
				if (window.NeedReset)
				{
					AddOnScreenMessage("Restart program for changed settings");
				}
			}
		}

		private void LoadLastRomContextMenuItem_Click(object sender, EventArgs e)
			=> LoadMostRecentROM();

		private void LoadMostRecentROM()
		{
			LoadRomFromRecent(Config.RecentRoms.MostRecent);
		}

		private void LoadLastMovieContextMenuItem_Click(object sender, EventArgs e)
		{
			LoadMoviesFromRecent(Config.RecentMovies.MostRecent);
		}

		private void BackupMovieContextMenuItem_Click(object sender, EventArgs e)
		{
			MovieSession.Movie.SaveBackup();
			AddOnScreenMessage("Backup movie saved.");
		}

		private void ViewSubtitlesContextMenuItem_Click(object sender, EventArgs e)
		{
			if (MovieSession.Movie.NotActive()) return;
			using EditSubtitlesForm form = new(this, MovieSession.Movie, Config.PathEntries, readOnly: MovieSession.ReadOnly);
			this.ShowDialogWithTempMute(form);
		}

		private void AddSubtitleContextMenuItem_Click(object sender, EventArgs e)
		{
			// TODO: rethink this?
			var subForm = new SubtitleMaker();
			subForm.DisableFrame();

			int index = -1;
			var sub = new Subtitle();
			for (int i = 0; i < MovieSession.Movie.Subtitles.Count; i++)
			{
				sub = MovieSession.Movie.Subtitles[i];
				if (Emulator.Frame == sub.Frame)
				{
					index = i;
					break;
				}
			}

			if (index < 0)
			{
				sub = new Subtitle { Frame = Emulator.Frame };
			}

			subForm.Sub = sub;
			if (!this.ShowDialogWithTempMute(subForm).IsOk()) return;

			if (index >= 0) MovieSession.Movie.Subtitles.RemoveAt(index);
			MovieSession.Movie.Subtitles.Add(subForm.Sub);
		}

		private void ViewCommentsContextMenuItem_Click(object sender, EventArgs e)
		{
			if (MovieSession.Movie.NotActive()) return;
			using EditCommentsForm form = new(MovieSession.Movie, MovieSession.ReadOnly);
			this.ShowDialogWithTempMute(form);
		}

		private void UndoSavestateContextMenuItem_Click(object sender, EventArgs e)
		{
			_stateSlots.SwapBackupSavestate(MovieSession.Movie, $"{SaveStatePrefix()}.QuickSave{Config.SaveSlot % 10}.State", Config.SaveSlot);
			AddOnScreenMessage($"Save slot {Config.SaveSlot} restored.");
		}

		private void ClearSramContextMenuItem_Click(object sender, EventArgs e)
		{
			CloseRom(clearSram: true);
		}

		private void ShowMenuContextMenuItem_Click(object sender, EventArgs e)
		{
			MainMenuStrip.Visible = !MainMenuStrip.Visible;
			FrameBufferResized();
		}

		private void DumpStatusButton_Click(object sender, EventArgs e)
		{
			string details = Emulator.RomDetails();
			if (string.IsNullOrWhiteSpace(details))
			{
				details = _defaultRomDetails;
			}

			if (!string.IsNullOrEmpty(details))
			{
				Tools.Load<LogWindow>();
				((LogWindow) Tools.Get<LogWindow>()).ShowReport("Dump Status Report", details);
			}
		}

		private readonly ScreenshotForm _screenshotTooltip = new();

		private void SlotStatusButtons_MouseEnter(object/*?*/ sender, EventArgs e)
		{
			var slot = 10;
			if (sender == Slot1StatusButton) slot = 1;
			else if (sender == Slot2StatusButton) slot = 2;
			else if (sender == Slot3StatusButton) slot = 3;
			else if (sender == Slot4StatusButton) slot = 4;
			else if (sender == Slot5StatusButton) slot = 5;
			else if (sender == Slot6StatusButton) slot = 6;
			else if (sender == Slot7StatusButton) slot = 7;
			else if (sender == Slot8StatusButton) slot = 8;
			else if (sender == Slot9StatusButton) slot = 9;
			//TODO just put the slot number in Control.Tag already
			if (!(HasSlot(slot) && ReadScreenshotFromSavestate(slot: slot) is {} bb))
			{
				_screenshotTooltip.FadeOut();
				return;
			}
			var width = bb.Width;
			var height = bb.Height;
			var location = PointToScreen(MainStatusBar.Location);
			location.Offset(((e as MouseEventArgs)?.X ?? 50) - width/2, -height);
			_screenshotTooltip.UpdateValues(
				bb,
				captionText: string.Empty,
				location,
				width: width,
				height: height,
				Graphics.FromHwnd(Handle).MeasureString);
			_screenshotTooltip.FadeIn();
		}

		private void SlotStatusButtons_MouseLeave(object/*?*/ sender, EventArgs e)
			=> _screenshotTooltip.FadeOut();

		private void SlotStatusButtons_MouseUp(object sender, MouseEventArgs e)
		{
			var slot = 10;
			if (sender == Slot1StatusButton) slot = 1;
			if (sender == Slot2StatusButton) slot = 2;
			if (sender == Slot3StatusButton) slot = 3;
			if (sender == Slot4StatusButton) slot = 4;
			if (sender == Slot5StatusButton) slot = 5;
			if (sender == Slot6StatusButton) slot = 6;
			if (sender == Slot7StatusButton) slot = 7;
			if (sender == Slot8StatusButton) slot = 8;
			if (sender == Slot9StatusButton) slot = 9;
			if (sender == Slot0StatusButton) slot = 10;

			if (e.Button is MouseButtons.Right) SaveQuickSave(slot);
			else if (e.Button is MouseButtons.Left && HasSlot(slot)) _ = LoadQuickSave(slot);
		}

		private void KeyPriorityStatusLabel_Click(object sender, EventArgs e)
		{
			Config.InputHotkeyOverrideOptions = Config.InputHotkeyOverrideOptions switch
			{
				1 => 2,
				2 => Config.NoMixedInputHokeyOverride ? 1 : 0,
				_ => 1,
			};
			UpdateKeyPriorityIcon();
		}

		private void FreezeStatus_Click(object sender, EventArgs e)
		{
			if (CheatStatusButton.Visible)
			{
				Tools.Load<Cheats>();
			}
		}

		private void ProfileFirstBootLabel_Click(object sender, EventArgs e)
		{
			using var profileForm = new ProfileConfig(Config, this);
			_ = this.ShowDialogWithTempMute(profileForm); // interpret Cancel as user acklowledgement (there are instructions for re-opening the dialog anyway)
			Config.FirstBoot = false;
			ProfileFirstBootLabel.Visible = false;
			OSD.ClearRegularMessages();
			AddOnScreenMessage("You can find that again at Config > Profiles", duration: 10/*seconds*/); // intentionally left off the ellipsis from the menu item's name as it could be misinterpreted as the message being truncated
			AddOnScreenMessage("All done! Drag+drop a rom to start playing", duration: 10/*seconds*/);
		}

		private void LinkConnectStatusBarButton_Click(object sender, EventArgs e)
		{
			// toggle Link status (only outside of a movie session)
			if (!MovieSession.Movie.IsPlaying())
			{
				var core = Emulator.AsLinkable();
				core.LinkConnected = !core.LinkConnected;
				Console.WriteLine($"Cable connect status to {core.LinkConnected}");
			}
		}

		private void UpdateNotification_Click(object sender, EventArgs e)
		{
			Sound.StopSound();
			var result = this.ModalMessageBox3(
				$"Version {Config.UpdateLatestVersion} is now available. Would you like to open the BizHawk homepage?\r\n\r\nClick \"No\" to hide the update notification for this version.",
				"New Version Available",
				EMsgBoxIcon.Question);
			Sound.StartSound();

			if (result == true)
			{
				System.Threading.ThreadPool.QueueUserWorkItem(s =>
				{
					using (System.Diagnostics.Process.Start(VersionInfo.HomePage))
					{
					}
				});
			}
			else if (result == false)
			{
				UpdateChecker.GlobalConfig = Config;
				UpdateChecker.IgnoreNewVersion();
				UpdateChecker.BeginCheck(skipCheck: true); // Trigger event to hide new version notification
			}
		}

		private void MainForm_Activated(object sender, EventArgs e)
		{
			if (!Config.RunInBackground) MaybeUnpauseFromMenuClosed();
		}

		private void MainForm_Deactivate(object sender, EventArgs e)
		{
			if (!Config.RunInBackground) MaybePauseFromMenuOpened();
		}

		private void TimerMouseIdle_Tick(object sender, EventArgs e)
		{
			if (_inFullscreen && Config.DispChromeFullscreenAutohideMouse)
			{
				AutohideCursor(hide: true);
			}
		}

		private void MainForm_Enter(object sender, EventArgs e)
		{
			AutohideCursor(hide: false);
		}

		private void MainForm_Resize(object sender, EventArgs e)
		{
			_presentationPanel.Resized = true;
			if (_framebufferResizedPending && WindowState is FormWindowState.Normal)
			{
				_framebufferResizedPending = false;
				FrameBufferResized();
			}
		}

		private void MainForm_Shown(object sender, EventArgs e)
		{
			if (Config.RecentWatches.AutoLoad)
			{
				Tools.LoadRamWatch(!Config.DisplayRamWatch);
			}

			if (Config.Cheats.Recent.AutoLoad)
			{
				Tools.Load<Cheats>();
			}

			Tools.AutoLoad();
			HandlePlatformMenus();
		}

		protected override void OnClosed(EventArgs e)
		{
			_windowClosedAndSafeToExitProcess = true;
			base.OnClosed(e);
		}

		private void MainformMenu_MenuActivate(object sender, EventArgs e)
		{
			HandlePlatformMenus();
			MaybePauseFromMenuOpened();
		}

		public void MaybePauseFromMenuOpened()
		{
			if (!Config.PauseWhenMenuActivated) return;
			_wasPaused = EmulatorPaused;
			PauseEmulator();
			_didMenuPause = true; // overwrites value set during PauseEmulator call
		}

		private void MainformMenu_MenuDeactivate(object sender, EventArgs e) => MaybeUnpauseFromMenuClosed();

		public void MaybeUnpauseFromMenuClosed()
		{
			if (_wasPaused || !Config.PauseWhenMenuActivated) return;
			UnpauseEmulator();
		}

		private static void FormDragEnter(object sender, DragEventArgs e)
		{
			e.Set(DragDropEffects.Copy);
		}

		private void FormDragDrop(object sender, DragEventArgs e)
			=> PathsFromDragDrop = (string[]) e.Data.GetData(DataFormats.FileDrop);
	}
}
