using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Security.AccessControl;
using System.Security.Principal;
using System.IO.Pipes;

using BizHawk.Bizware.Graphics;

using BizHawk.Common;
using BizHawk.Common.BufferExtensions;
using BizHawk.Common.PathExtensions;
using BizHawk.Common.StringExtensions;

using BizHawk.Client.Common;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.Base_Implementations;
using BizHawk.Emulation.Cores;
using BizHawk.Emulation.Cores.Arcades.MAME;
using BizHawk.Emulation.Cores.Computers.AppleII;
using BizHawk.Emulation.Cores.Computers.Commodore64;
using BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES;
using BizHawk.Emulation.Cores.Consoles.SNK;
using BizHawk.Emulation.Cores.Nintendo.GBA;
using BizHawk.Emulation.Cores.Nintendo.N64;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.Nintendo.SNES;

using BizHawk.Emulation.DiscSystem;

using BizHawk.Client.EmuHawk.ToolExtensions;
using BizHawk.Client.EmuHawk.CoreExtensions;
using BizHawk.Client.EmuHawk.CustomControls;
using BizHawk.Common.CollectionExtensions;
using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk
{
	public partial class MainForm : FormBase, IDialogParent, IMainFormForApi, IMainFormForTools, IMainFormForRetroAchievements
	{
		private static readonly FilesystemFilterSet EmuHawkSaveStatesFSFilterSet = new(FilesystemFilter.EmuHawkSaveStates);

		private static readonly FilesystemFilterSet LibretroCoresFSFilterSet = new(new FilesystemFilter("Libretro Cores", new[] { OSTailoredCode.IsUnixHost ? "so" : "dll" }))
		{
			AppendAllFilesEntry = false,
		};

		private const int WINDOW_SCALE_MAX = 10;

		private void MainForm_Load(object sender, EventArgs e)
		{	
			UpdateWindowTitle();
			
			{
				for (int i = 1; i <= WINDOW_SCALE_MAX; i++)
				{
					long quotient = Math.DivRem(i, 10, out long remainder);
					var temp = new ToolStripMenuItemEx
					{
						Tag = i,
						Text = $"{(quotient > 0 ? quotient : "")}&{remainder}x"
					};
					temp.Click += this.WindowSize_Click;
					WindowSizeSubMenu.DropDownItems.Insert(i - 1, temp);
				}
			}

			foreach (var (appliesTo, coreNames) in Config.CorePickerUIData)
			{
				var submenu = new ToolStripMenuItem { Text = string.Join(" | ", appliesTo) };
				submenu.DropDownItems.AddRange(coreNames.Select(coreName => {
					var entry = new ToolStripMenuItem { Text = coreName };
					entry.Click += (_, _) =>
					{
						string currentCoreName = Emulator.Attributes().CoreName;
						if (coreName != currentCoreName && coreNames.Contains(currentCoreName)) FlagNeedsReboot();
						foreach (string system in appliesTo)
							Config.PreferredCores[system] = coreName;
					};
					return (ToolStripItem) entry;
				}).ToArray());
				submenu.DropDownOpened += (openedSender, _1) =>
				{
					_ = Config.PreferredCores.TryGetValue(appliesTo[0], out var preferred);
					if (!coreNames.Contains(preferred))
					{
						// invalid --> default (doing this here rather than when reading config file to allow for hacked-in values, though I'm not sure if that could do anything at the moment --yoshi)
						var defaultCore = coreNames[0];
						Console.WriteLine($"setting preferred core for {submenu.Text} to {defaultCore} (was {preferred ?? "null"})");
						preferred = defaultCore;
						foreach (var sysID in appliesTo) Config.PreferredCores[sysID] = preferred;
					}
					foreach (ToolStripMenuItem entry in ((ToolStripMenuItem) openedSender).DropDownItems) entry.Checked = entry.Text == preferred;
				};
				CoresSubMenu.DropDownItems.Add(submenu);
			}
			CoresSubMenu.DropDownItems.Add(new ToolStripSeparator { AutoSize = true });
			var GBInSGBMenuItem = new ToolStripMenuItem { Text = "GB in SGB" };
			GBInSGBMenuItem.Click += (_, _) =>
			{
				Config.GbAsSgb = !Config.GbAsSgb;
				if (Emulator.SystemId is VSystemID.Raw.GB or VSystemID.Raw.GBC or VSystemID.Raw.SGB) FlagNeedsReboot();
			};
			CoresSubMenu.DropDownItems.Add(GBInSGBMenuItem);
			var setLibretroCoreToolStripMenuItem = new ToolStripMenuItem { Text = "Set Libretro Core..." };
			setLibretroCoreToolStripMenuItem.Click += (_, _) => RunLibretroCoreChooser();
			CoresSubMenu.DropDownItems.Add(setLibretroCoreToolStripMenuItem);
			CoresSubMenu.DropDownOpened += (_, _) => GBInSGBMenuItem.Checked = Config.GbAsSgb;

			ToolStripMenuItemEx recentCoreSettingsSubmenu = new() { Text = "Recent" };
			recentCoreSettingsSubmenu.DropDownItems.AddRange(CreateCoreSettingsSubmenus().ToArray());
			ToolStripMenuItemEx noRecentsItem = new() { Enabled = false, Text = "(N/A)" };
			recentCoreSettingsSubmenu.DropDownItems.Add(noRecentsItem);
			recentCoreSettingsSubmenu.DropDownOpening += (_, _) =>
			{
				foreach (ToolStripItem submenu in recentCoreSettingsSubmenu.DropDownItems) submenu.Visible = Config.RecentCores.Contains(submenu.Text);
				noRecentsItem.Visible = Config.RecentCores.Count is 0;
			};
			ToolStripMenuItemEx consolesCoreSettingsSubmenu = new() { Text = "For Consoles" };
			ToolStripMenuItemEx handheldsCoreSettingsSubmenu = new() { Text = "For Handhelds" };
			ToolStripMenuItemEx pcsCoreSettingsSubmenu = new() { Text = "For Computers" };
			ToolStripMenuItemEx otherCoreSettingsSubmenu = new() { Text = "Other" };
			foreach (var submenu in CreateCoreSettingsSubmenus(includeDupes: true).OrderBy(submenu => submenu.Text))
			{
				var parentMenu = (VSystemCategory) submenu.Tag switch
				{
					VSystemCategory.Consoles => consolesCoreSettingsSubmenu,
					VSystemCategory.Handhelds => handheldsCoreSettingsSubmenu,
					VSystemCategory.PCs => pcsCoreSettingsSubmenu,
					_ => otherCoreSettingsSubmenu
				};
				parentMenu.DropDownItems.Add(submenu);
			}
			foreach (var submenu in new[] { consolesCoreSettingsSubmenu, handheldsCoreSettingsSubmenu, pcsCoreSettingsSubmenu, otherCoreSettingsSubmenu })
			{
				if (submenu.DropDownItems.Count is 0)
				{
					submenu.DropDownItems.Add(new ToolStripMenuItemEx { Text = "(none)" });
					submenu.Enabled = false;
				}
			}
			ConfigSubMenu.DropDownItems.Insert(
				ConfigSubMenu.DropDownItems.IndexOf(CoresSubMenu) + 1,
				new ToolStripMenuItemEx
				{
					DropDownItems =
					{
						recentCoreSettingsSubmenu,
						new ToolStripSeparatorEx { AutoSize = true },
						consolesCoreSettingsSubmenu,
						handheldsCoreSettingsSubmenu,
						pcsCoreSettingsSubmenu,
						otherCoreSettingsSubmenu,
					},
					Text = "Core Settings",
				});

			// Hide Status bar icons and general StatusBar prep
			MainStatusBar.Padding = new Padding(MainStatusBar.Padding.Left, MainStatusBar.Padding.Top, MainStatusBar.Padding.Left, MainStatusBar.Padding.Bottom); // Workaround to remove extra padding on right
			PlayRecordStatusButton.Visible = false;
			AVIStatusLabel.Visible = false;
			SetPauseStatusBarIcon();
			Tools.UpdateCheatRelatedTools(null, null);
			RebootStatusBarIcon.Visible = false;
			UpdateNotification.Visible = false;
			_statusBarDiskLightOnImage = Properties.Resources.LightOn;
			_statusBarDiskLightOffImage = Properties.Resources.LightOff;
			_linkCableOn = Properties.Resources.Connect16X16;
			_linkCableOff = Properties.Resources.NoConnect16X16;
			UpdateCoreStatusBarButton();
			if (Config.FirstBoot)
			{
				ProfileFirstBootLabel.Visible = true;
				AddOnScreenMessage("Click the blue silhouette below for onboarding", duration: 30);
			}

			HandleToggleLightAndLink();
			SetStatusBar();
			_stateSlots.Update(Emulator, MovieSession.Movie, SaveStatePrefix());

			var quickslotButtons = new[]
			{
				Slot1StatusButton, Slot2StatusButton, Slot3StatusButton, Slot4StatusButton, Slot5StatusButton,
				Slot6StatusButton, Slot7StatusButton, Slot8StatusButton, Slot9StatusButton, Slot0StatusButton,
			};
			for (var i = 0; i < quickslotButtons.Length; i++)
			{
				ref var button = ref quickslotButtons[i];
				button.MouseEnter += SlotStatusButtons_MouseEnter;
				button.MouseLeave += SlotStatusButtons_MouseLeave;
			}

			if (OSTailoredCode.IsUnixHost)
			{
				// workaround for https://github.com/mono/mono/issues/12644
				MainFormContextMenu.Items.Insert(0, new ToolStripMenuItemEx { Text = "(Dismiss Menu)" }); // don't even need to attach any behaviour, since clicking anything will dismiss the menu first
				MainFormContextMenu.Items.Insert(1, new ToolStripSeparatorEx());
			}

			// New version notification
			UpdateChecker.CheckComplete += (s2, e2) =>
			{
				if (IsDisposed)
				{
					return;
				}

				this.BeginInvoke(() => { UpdateNotification.Visible = UpdateChecker.IsNewVersionAvailable; });
			};
			UpdateChecker.GlobalConfig = Config;
			UpdateChecker.BeginCheck(); // Won't actually check unless enabled by user

			// open requested ext. tool
			var requestedExtToolDll = _argParser.openExtToolDll;
			if (requestedExtToolDll != null)
			{
				var found = ExtToolManager.ToolStripItems.Where(static item => item.Enabled)
					.Select(static item => (ExternalToolManager.MenuItemInfo) item.Tag)
					.FirstOrNull(info => info.AsmFilename == requestedExtToolDll
						|| Path.GetFileName(info.AsmFilename) == requestedExtToolDll
						|| Path.GetFileNameWithoutExtension(info.AsmFilename) == requestedExtToolDll);
				if (found is not null) found.Value.TryLoad();
				else Console.WriteLine($"requested ext. tool dll {requestedExtToolDll} could not be loaded");
			}

#if DEBUG
			AddDebugMenu();
#endif
		}

		static MainForm()
		{
			// If this isn't here, then our assembly resolving hacks wont work due to the check for MainForm.INTERIM
			// its.. weird. don't ask.
		}

		public CoreComm CreateCoreComm()
		{
			var cfp = new CoreFileProvider(
				this,
				FirmwareManager,
				Config.PathEntries,
				Config.FirmwareUserSpecifications);
			var prefs = CoreComm.CorePreferencesFlags.None;
			if (Config.SkipWaterboxIntegrityChecks)
				prefs = CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck;

			// can't pass self as IDialogParent :(
			return new CoreComm(
				message => this.ModalMessageBox(message, "Warning", EMsgBoxIcon.Warning),
				AddOnScreenMessage,
				cfp,
				prefs,
				new OpenGLProvider());
		}

		private void SetImages()
		{
			OpenRomMenuItem.Image = Properties.Resources.OpenFile;
			RecentRomSubMenu.Image = Properties.Resources.Recent;
			CloseRomMenuItem.Image = Properties.Resources.Close;
			PreviousSlotMenuItem.Image = Properties.Resources.MoveLeft;
			NextSlotMenuItem.Image = Properties.Resources.MoveRight;
			ReadonlyMenuItem.Image = Properties.Resources.ReadOnly;
			RecentMovieSubMenu.Image = Properties.Resources.Recent;
			RecordMovieMenuItem.Image = Properties.Resources.Record;
			PlayMovieMenuItem.Image = Properties.Resources.Play;
			StopMovieMenuItem.Image = Properties.Resources.Stop;
			PlayFromBeginningMenuItem.Image = Properties.Resources.Restart;
			ImportMoviesMenuItem.Image = Properties.Resources.Import;
			SaveMovieMenuItem.Image = Properties.Resources.SaveAs;
			SaveMovieAsMenuItem.Image = Properties.Resources.SaveAs;
			StopMovieWithoutSavingMenuItem.Image = Properties.Resources.Stop;
			RecordAVMenuItem.Image = Properties.Resources.Record;
			ConfigAndRecordAVMenuItem.Image = Properties.Resources.Avi;
			StopAVIMenuItem.Image = Properties.Resources.Stop;
			ScreenshotMenuItem.Image = Properties.Resources.Camera;
			PauseMenuItem.Image = Properties.Resources.Pause;
			RebootCoreMenuItem.Image = Properties.Resources.Reboot;
			SwitchToFullscreenMenuItem.Image = Properties.Resources.Fullscreen;
			ControllersMenuItem.Image = Properties.Resources.GameController;
			HotkeysMenuItem.Image = Properties.Resources.HotKeys;
			DisplayConfigMenuItem.Image = Properties.Resources.TvIcon;
			SoundMenuItem.Image = Properties.Resources.Audio;
			PathsMenuItem.Image = Properties.Resources.CopyFolder;
			FirmwaresMenuItem.Image = Properties.Resources.Pcb;
			MessagesMenuItem.Image = Properties.Resources.MessageConfig;
			AutofireMenuItem.Image = Properties.Resources.Lightning;
			RewindOptionsMenuItem.Image = Properties.Resources.Previous;
			ProfilesMenuItem.Image = Properties.Resources.Profile;
			SaveConfigMenuItem.Image = Properties.Resources.Save;
			LoadConfigMenuItem.Image = Properties.Resources.LoadConfig;
			(ToolBoxMenuItem.Image, /*ToolBoxMenuItem.Text*/_) = ToolManager.IconAndNameCache[typeof(ToolBox)]
				= (/*ToolBox.ToolIcon.ToBitmap()*/Properties.Resources.ToolBox, "Tool Box");
			(RamWatchMenuItem.Image, /*RamWatchMenuItem.Text*/_) = ToolManager.IconAndNameCache[typeof(RamWatch)]
				= (/*RamWatch.ToolIcon.ToBitmap()*/Properties.Resources.Watch, "RAM Watch");
			(RamSearchMenuItem.Image, /*RamSearchMenuItem.Text*/_) = ToolManager.IconAndNameCache[typeof(RamSearch)]
				= (/*RamSearch.ToolIcon.ToBitmap()*/Properties.Resources.Search, "RAM Search");
			(LuaConsoleMenuItem.Image, /*LuaConsoleMenuItem.Text*/_) = ToolManager.IconAndNameCache[typeof(LuaConsole)]
				= (/*LuaConsole.ToolIcon.ToBitmap()*/Properties.Resources.TextDoc, "Lua Console");
			(TAStudioMenuItem.Image, /*TAStudioMenuItem.Text*/_) = ToolManager.IconAndNameCache[typeof(TAStudio)]
				= (/*TAStudio.ToolIcon.ToBitmap()*/Properties.Resources.TAStudio, "TAStudio");
			(HexEditorMenuItem.Image, /*HexEditorMenuItem.Text*/_) = ToolManager.IconAndNameCache[typeof(HexEditor)]
				= (/*HexEditor.ToolIcon.ToBitmap()*/Properties.Resources.Poke, "Hex Editor");
			(TraceLoggerMenuItem.Image, /*TraceLoggerMenuItem.Text*/_) = ToolManager.IconAndNameCache[typeof(TraceLogger)]
				= (/*TraceLogger.ToolIcon.ToBitmap()*/Properties.Resources.Pencil, "Trace Logger");
			(DebuggerMenuItem.Image, /*DebuggerMenuItem.Text*/_) = ToolManager.IconAndNameCache[typeof(GenericDebugger)]
				= (/*GenericDebugger.ToolIcon.ToBitmap()*/Properties.Resources.Bug, "Debugger");
			(CodeDataLoggerMenuItem.Image, /*CodeDataLoggerMenuItem.Text*/_) = ToolManager.IconAndNameCache[typeof(CDL)]
				= (/*CDL.ToolIcon.ToBitmap()*/Properties.Resources.CdLogger, "Code Data Logger");
			(VirtualPadMenuItem.Image, /*VirtualPadMenuItem.Text*/_) = ToolManager.IconAndNameCache[typeof(VirtualpadTool)]
				= (/*VirtualpadTool.ToolIcon.ToBitmap()*/Properties.Resources.GameController, "Virtual Pads");
			(BasicBotMenuItem.Image, /*BasicBotMenuItem.Text*/_) = ToolManager.IconAndNameCache[typeof(BasicBot)]
				= (/*BasicBot.ToolIcon.ToBitmap()*/Properties.Resources.BasicBotBit, "Basic Bot");
			(CheatsMenuItem.Image, /*CheatsMenuItem.Text*/_) = ToolManager.IconAndNameCache[typeof(Cheats)]
				= (/*Cheats.ToolIcon.ToBitmap()*/Properties.Resources.Freeze, "Cheats");
			(GameSharkConverterMenuItem.Image, /*GameSharkConverterMenuItem.Text*/_) = ToolManager.IconAndNameCache[typeof(GameShark)]
				= (/*GameShark.ToolIcon.ToBitmap()*/Properties.Resources.Shark, "Cheat Code Converter");
			(MultiDiskBundlerFileMenuItem.Image, /*MultiDiskBundlerFileMenuItem.Text*/_) = ToolManager.IconAndNameCache[typeof(MultiDiskBundler)]
				= (/*MultiDiskBundler.ToolIcon.ToBitmap()*/Properties.Resources.SaveConfig, "Multi-disk Bundler");
			NesControllerSettingsMenuItem.Image = Properties.Resources.GameController;
			NESGraphicSettingsMenuItem.Image = Properties.Resources.TvIcon;
			NESSoundChannelsMenuItem.Image = Properties.Resources.Audio;
			(KeypadMenuItem.Image, /*KeypadMenuItem.Text*/_) = ToolManager.IconAndNameCache[typeof(TI83KeyPad)]
				= (/*TI83KeyPad.ToolIcon.ToBitmap()*/Properties.Resources.Calculator, "TI-83 Virtual KeyPad");
			PSXControllerSettingsMenuItem.Image = Properties.Resources.GameController;
			SNESControllerConfigurationMenuItem.Image = Properties.Resources.GameController;
			(SnesGfxDebuggerMenuItem.Image, /*SnesGfxDebuggerMenuItem.Text*/_) = ToolManager.IconAndNameCache[typeof(SNESGraphicsDebugger)]
				= (/*SNESGraphicsDebugger.ToolIcon.ToBitmap()*/Properties.Resources.Bug, "Graphics Debugger");
			ColecoControllerSettingsMenuItem.Image = Properties.Resources.GameController;
			N64PluginSettingsMenuItem.Image = Properties.Resources.Monitor;
			N64ControllerSettingsMenuItem.Image = Properties.Resources.GameController;
			IntVControllerSettingsMenuItem.Image = Properties.Resources.GameController;
			OnlineHelpMenuItem.Image = Properties.Resources.Help;
			ForumsMenuItem.Image = Properties.Resources.TAStudio;
			(FeaturesMenuItem.Image, /*FeaturesMenuItem.Text*/_) = ToolManager.IconAndNameCache[typeof(CoreFeatureAnalysis)]
				= (/*CoreFeatureAnalysis.ToolIcon.ToBitmap()*/Properties.Resources.KitchenSink, "Core Features");
			AboutMenuItem.Image = Properties.Resources.CorpHawkSmall;
			DumpStatusButton.Image = Properties.Resources.Blank;
			PlayRecordStatusButton.Image = Properties.Resources.Blank;
			PauseStatusButton.Image = Properties.Resources.Blank;
			RebootStatusBarIcon.Image = Properties.Resources.Reboot;
			AVIStatusLabel.Image = Properties.Resources.Blank;
			LedLightStatusLabel.Image = Properties.Resources.LightOff;
			KeyPriorityStatusLabel.Image = Properties.Resources.Both;
			CoreNameStatusBarButton.Image = Properties.Resources.CorpHawkSmall;
			ProfileFirstBootLabel.Image = Properties.Resources.Profile;
			LinkConnectStatusBarButton.Image = Properties.Resources.Connect16X16;
			OpenRomContextMenuItem.Image = Properties.Resources.OpenFile;
			LoadLastRomContextMenuItem.Image = Properties.Resources.Recent;
			StopAVContextMenuItem.Image = Properties.Resources.Stop;
			RecordMovieContextMenuItem.Image = Properties.Resources.Record;
			PlayMovieContextMenuItem.Image = Properties.Resources.Play;
			RestartMovieContextMenuItem.Image = Properties.Resources.Restart;
			StopMovieContextMenuItem.Image = Properties.Resources.Stop;
			LoadLastMovieContextMenuItem.Image = Properties.Resources.Recent;
			StopNoSaveContextMenuItem.Image = Properties.Resources.Stop;
			SaveMovieContextMenuItem.Image = Properties.Resources.SaveAs;
			SaveMovieAsContextMenuItem.Image = Properties.Resources.SaveAs;
			UndoSavestateContextMenuItem.Image = Properties.Resources.Undo;
			toolStripMenuItem6.Image = Properties.Resources.GameController;
			toolStripMenuItem7.Image = Properties.Resources.HotKeys;
			toolStripMenuItem8.Image = Properties.Resources.TvIcon;
			toolStripMenuItem9.Image = Properties.Resources.Audio;
			toolStripMenuItem10.Image = Properties.Resources.CopyFolder;
			toolStripMenuItem11.Image = Properties.Resources.Pcb;
			toolStripMenuItem12.Image = Properties.Resources.MessageConfig;
			toolStripMenuItem13.Image = Properties.Resources.Lightning;
			toolStripMenuItem14.Image = Properties.Resources.Previous;
			toolStripMenuItem66.Image = Properties.Resources.Save;
			toolStripMenuItem67.Image = Properties.Resources.LoadConfig;
			ScreenshotContextMenuItem.Image = Properties.Resources.Camera;
			CloseRomContextMenuItem.Image = Properties.Resources.Close;
		}

		public MainForm(
			ParsedCLIFlags cliFlags,
			IGL gl,
			Func<string> getConfigPath,
			Func<Config> getGlobalConfig,
			Action<Sound> updateGlobalSound,
			string[] args,
			out IMovieSession movieSession,
			out bool exitEarly)
		{
			movieSession = null;
			exitEarly = false;

			_getGlobalConfig = getGlobalConfig;

			if (Config.SingleInstanceMode)
			{
				if (SingleInstanceInit(args))
				{
					exitEarly = true;
					return;
				}
			}

			//do this threaded stuff early so it has plenty of time to run in background
			Database.InitializeDatabase(
				bundledRoot: Path.Combine(PathUtils.ExeDirectoryPath, "gamedb"),
				userRoot: Path.Combine(PathUtils.DataDirectoryPath, "gamedb"),
				silent: true);
			BootGodDb.Initialize(Path.Combine(PathUtils.ExeDirectoryPath, "gamedb"));
			MAMEMachineDB.Initialize(Path.Combine(PathUtils.ExeDirectoryPath, "gamedb"));

			_argParser = cliFlags;
			_getConfigPath = getConfigPath;
			GL = gl;
			_updateGlobalSound = updateGlobalSound;

			InputManager = new InputManager
			{
				GetMainFormMouseInfo = () =>
				{
					var b = Control.MouseButtons;
					return (
						Control.MousePosition,
						MouseWheelTracker,
						(b & MouseButtons.Left) != 0,
						(b & MouseButtons.Middle) != 0,
						(b & MouseButtons.Right) != 0,
						(b & MouseButtons.XButton1) != 0,
						(b & MouseButtons.XButton2) != 0
					);
				}
			};
			FirmwareManager = new FirmwareManager();
			movieSession = MovieSession = new MovieSession(
				Config.Movies,
				Config.PathEntries.MovieBackupsAbsolutePath(),
				this,
				PauseEmulator,
				SetMainformMovieInfo);

			void MainForm_MouseClick(object sender, MouseEventArgs e)
			{
				AutohideCursor(false);
				if (Config.ShowContextMenu && e.Button == MouseButtons.Right)
				{
					// suppress the context menu if right click has a binding
					// (unless shift is being pressed, similar to double click fullscreening)
					var allowSuppress = ModifierKeys != Keys.Shift;
					if (allowSuppress && InputManager.ActiveController.HasBinding("WMouse R"))
					{
						return;
					}

					MainFormContextMenu.Show(PointToScreen(new Point(e.X, e.Y + MainformMenu.Height)));
				}
			}
			void MainForm_MouseMove(object sender, MouseEventArgs e) => AutohideCursor(false);
			void MainForm_MouseWheel(object sender, MouseEventArgs e) => MouseWheelTracker += e.Delta;
			MouseClick += MainForm_MouseClick;
			MouseMove += MainForm_MouseMove;

			InitializeComponent();
			Icon = Properties.Resources.Logo;
			SetImages();
#if !DEBUG
			ZXSpectrumExportSnapshotMenuItemMenuItem.Enabled = false;
			ZXSpectrumExportSnapshotMenuItemMenuItem.Visible = false;
#endif
#if AVI_SUPPORT
			SynclessRecordingMenuItem.Click += (_, _) => new SynclessRecordingTools(Config, Game, this).Run();
#else
			SynclessRecordingMenuItem.Enabled = false;
#endif

			Game = GameInfo.NullInstance;
			_throttle = new Throttle();
			Emulator = new NullEmulator();

			UpdateStatusSlots();
			UpdateKeyPriorityIcon();

			// TODO GL - a lot of disorganized wiring-up here
			_presentationPanel = new(
				Config,
				GL,
				ToggleFullscreen,
				MainForm_MouseClick,
				MainForm_MouseMove,
				MainForm_MouseWheel);

			DisplayManager = new(Config, Emulator, InputManager, MovieSession, GL, _presentationPanel, () => DisableSecondaryThrottling);
			Controls.Add(_presentationPanel);
			Controls.SetChildIndex(_presentationPanel, 0);

			// set up networking before ApiManager (in ToolManager)
			byte[] NetworkingTakeScreenshot()
				=> (byte[]) new ImageConverter().ConvertTo(MakeScreenshotImage().ToSysdrawingBitmap(), typeof(byte[]));
			NetworkingHelpers = (
				_argParser.HTTPAddresses is var (httpGetURL, httpPostURL)
					? new HttpCommunication(NetworkingTakeScreenshot, httpGetURL, httpPostURL)
					: null,
				new MemoryMappedFiles(NetworkingTakeScreenshot, _argParser.MMFFilename),
				_argParser.SocketAddress is var (socketIP, socketPort)
					? new SocketServer(NetworkingTakeScreenshot, _argParser.SocketProtocol, socketIP, socketPort)
					: null
			);

			ExtToolManager = new(
				Config,
				() => (Emulator.SystemId, Game.Hash),
				(toolPath, customFormTypeName, skipExtToolWarning) => Tools!.LoadExternalToolForm(
					toolPath: toolPath,
					customFormTypeName: customFormTypeName,
					skipExtToolWarning: skipExtToolWarning) is not null);
			Tools = new ToolManager(this, Config, DisplayManager, ExtToolManager, InputManager, Emulator, MovieSession, Game);

			// TODO GL - move these event handlers somewhere less obnoxious line in the On* overrides
			Load += (o, e) =>
			{
				AllowDrop = true;
				DragEnter += FormDragEnter;
				DragDrop += FormDragDrop;
			};

			Closing += CheckMayCloseAndCleanup;

			ResizeBegin += (o, e) =>
			{
				_inResizeLoop = true;
				Sound?.StopSound();
			};

			Resize += (_, _) => UpdateWindowTitle();

			ResizeEnd += (o, e) =>
			{
				_inResizeLoop = false;
				UpdateWindowTitle();

				if (_presentationPanel != null)
				{
					_presentationPanel.Resized = true;
				}

				Sound?.StartSound();
			};

			Input.Instance = new Input(
				Handle,
				() => Config,
				yieldAlt => ActiveForm switch
				{
					null => Config.AcceptBackgroundInput // none of our forms are focused, check the background input config
						? Config.AcceptBackgroundInputControllerOnly
							? AllowInput.OnlyController
							: AllowInput.All
						: AllowInput.None,
					TAStudio when yieldAlt => AllowInput.None,
					FormBase { BlocksInputWhenFocused: false } => AllowInput.All,
					ControllerConfig => AllowInput.All,
					HotkeyConfig => AllowInput.All,
					LuaWinform { BlocksInputWhenFocused: false } => AllowInput.All,
					_ => AllowInput.None
				}
			);
			InitControls();

			InputManager.ResetMainControllers(_autofireNullControls);
			InputManager.AutofireStickyXorAdapter.SetOnOffPatternFromConfig(Config.AutofireOn, Config.AutofireOff);
			var savedOutputMethod = Config.SoundOutputMethod;
			if (savedOutputMethod is ESoundOutputMethod.Dummy) Config.SoundOutputMethod = HostCapabilityDetector.HasXAudio2 ? ESoundOutputMethod.XAudio2 : ESoundOutputMethod.OpenAL;
			try
			{
				Sound = new Sound(Config, () => Emulator.VsyncRate());
			}
			catch
			{
				if (savedOutputMethod is not ESoundOutputMethod.Dummy)
				{
					ShowMessageBox(
						owner: null,
						text: "Couldn't initialize sound device! Try changing the output method in Sound config.",
						caption: "Initialization Error",
						EMsgBoxIcon.Error);
				}
				Config.SoundOutputMethod = ESoundOutputMethod.Dummy;
				Sound = new Sound(Config, () => Emulator.VsyncRate());
			}

			Sound.StartSound();
			InputManager.SyncControls(Emulator, MovieSession, Config);
			CheatList = new CheatCollection(this, Config.Cheats);
			CheatList.Changed += Tools.UpdateCheatRelatedTools;
			RewireSound();

			if (Config.SaveWindowPosition)
			{
				if (Config.MainWindowPosition is Point position)
				{
					Location = position;
				}

				if (Config.MainWindowSize is Size size && !Config.ResizeWithFramebuffer)
				{
					Size = size;
				}

				if (Config.MainWindowMaximized)
				{
					WindowState = FormWindowState.Maximized;
				}
			}

			if (Config.MainFormStayOnTop) TopMost = true;

			if (_argParser.cmdRom != null)
			{
				// Commandline should always override auto-load
				OpenAdvanced_OpenRom ioa = new(_argParser.cmdRom);
				if (ioa is OpenAdvanced_OpenRom oaor) ioa = new(oaor.Path.MakeAbsolute()); // fixes #3224; should this be done for all the IOpenAdvanced types? --yoshi
				_ = LoadRom(ioa.SimplePath, new LoadRomArgs(ioa));
				if (Game.IsNullInstance())
				{
					ShowMessageBox(owner: null, $"Failed to load {_argParser.cmdRom} specified on commandline");
				}
			}
			else if (Config.RecentRoms.AutoLoad && !Config.RecentRoms.Empty)
			{
				LoadMostRecentROM();
			}

			Config.VideoWriterAudioSyncEffective = _argParser.audiosync ?? Config.VideoWriterAudioSync;
			_autoDumpLength = _argParser._autoDumpLength;
			if (_argParser.cmdMovie != null)
			{
				_suppressSyncSettingsWarning = true; // We don't want to be nagged if we are attempting to automate
				if (Game.IsNullInstance())
				{
					OpenRom();
				}

				// If user picked a game, then do the commandline logic
				if (!Game.IsNullInstance())
				{
					var movie = MovieSession.Get(_argParser.cmdMovie, true);
					MovieSession.ReadOnly = true;

					// if user is dumping and didn't supply dump length, make it as long as the loaded movie
					if (_autoDumpLength == 0)
					{
						_autoDumpLength = movie.InputLogLength;
					}

					// Copy pasta from drag & drop
					if (MovieImport.IsValidMovieExtension(Path.GetExtension(_argParser.cmdMovie)))
					{
						ProcessMovieImport(_argParser.cmdMovie, true);
					}
					else
					{
						StartNewMovie(movie, false);
						Config.RecentMovies.Add(_argParser.cmdMovie);
					}

					_suppressSyncSettingsWarning = false;
				}
			}
			else if (Config.RecentMovies.AutoLoad && !Config.RecentMovies.Empty)
			{
				if (Game.IsNullInstance())
				{
					OpenRom();
				}

				// If user picked a game, then do the autoload logic
				if (!Game.IsNullInstance())
				{
					if (File.Exists(Config.RecentMovies.MostRecent))
					{
						StartNewMovie(MovieSession.Get(Config.RecentMovies.MostRecent, true), false);
					}
					else
					{
						Config.RecentMovies.HandleLoadError(this, Config.RecentMovies.MostRecent);
					}
				}
			}

			if (_argParser.startFullscreen || Config.StartFullscreen)
			{
				_needsFullscreenOnLoad = true;
			}

			if (!Game.IsNullInstance())
			{
				if (_argParser.cmdLoadState != null)
				{
					_ = LoadState(
						path: _argParser.cmdLoadState,
						userFriendlyStateName: Path.GetFileName(_argParser.cmdLoadState));
				}
				else if (_argParser.cmdLoadSlot != null)
				{
					_ = LoadQuickSave(_argParser.cmdLoadSlot.Value);
				}
				else if (Config.AutoLoadLastSaveSlot)
				{
					_ = LoadstateCurrentSlot();
				}
			}

			if (_argParser.UserdataUnparsedPairs is {} pairs) foreach (var (k, v) in pairs)
			{
				MovieSession.UserBag[k] = v switch
				{
					"true" => true,
					"false" => false,
					_ when int.TryParse(v, out var i) => i,
					_ when double.TryParse(v, out var d) => d,
					_ => v
				};
			}

			//start Lua Console if requested in the command line arguments
			if (_argParser.luaConsole)
			{
				OpenLuaConsole();
			}
			//load Lua Script if requested in the command line arguments
			if (_argParser.luaScript != null)
			{
				_ = Tools.LuaConsole.LoadByFileExtension(_argParser.luaScript.MakeAbsolute(), out _);
			}

			SetStatusBar();

			if (Config.StartPaused)
			{
				PauseEmulator();
			}

			// start dumping, if appropriate
			if (_argParser.cmdDumpType != null && _argParser.cmdDumpName != null)
			{
				RecordAv(_argParser.cmdDumpType, _argParser.cmdDumpName);
			}

			SetMainformMovieInfo();

			SynchChrome();

			if (Config.RAAutostart)
			{
				OpenRetroAchievements();
			}

			_presentationPanel.Control.Paint += (o, e) =>
			{
				// I would like to trigger a repaint here, but this isn't done yet
			};

			if (!Config.SkipOutdatedOsCheck && OSTailoredCode.HostWindowsVersion is not null)
			{
				var (winVersion, win10PlusVersion) = OSTailoredCode.HostWindowsVersion.Value;
				var message = winVersion switch
				{
//					OSTailoredCode.WindowsVersion._11 when win10PlusVersion! < new Version(10, 0, 22621) => $"Quick reminder: Your copy of Windows 11 (build {win10PlusVersion.Build}) is no longer supported by Microsoft.\nEmuHawk will probably continue working, but please update to at least 21H2 for increased security.",
					OSTailoredCode.WindowsVersion._11 => null,
					OSTailoredCode.WindowsVersion._10 when win10PlusVersion! < new Version(10, 0, 19045) => $"Quick reminder: Your copy of Windows 10 (build {win10PlusVersion.Build}) is no longer supported by Microsoft.\nEmuHawk will probably continue working, but please update to at least 22H2 for increased security.",
					OSTailoredCode.WindowsVersion._10 => null,
					_ => $"Quick reminder: Windows {winVersion.ToString().RemovePrefix('_').Replace('_', '.')} is no longer supported by Microsoft.\nEmuHawk will probably continue working, but please get a new operating system for increased security (either Windows 10+ or a GNU+Linux distro)."
				};
				if (message is not null)
				{
#if DEBUG
				Console.WriteLine(message);
#else
				Load += (_, _) => Config.SkipOutdatedOsCheck = this.ShowMessageBox2($"{message}\n\nSkip this reminder from now on?");
#endif
				}
			}
		}

		private void CheckMayCloseAndCleanup(object/*?*/ closingSender, CancelEventArgs closingArgs)
		{
			if (_currAviWriter is not null)
			{
				if (!this.ModalMessageBox2(
					caption: "Really quit?",
					icon: EMsgBoxIcon.Question,
					text: "You are currently recording A/V.\nChoose \"Yes\" to finalise it and quit EmuHawk.\nChoose \"No\" to cancel shutdown and continue recording."))
				{
					closingArgs.Cancel = true;
					return;
				}
				StopAv();
			}

			if (!Tools.AskSave())
			{
				closingArgs.Cancel = true;
				return;
			}

			MovieSession.StopMovie();
			Tools.Close();
			// zero 03-nov-2015 - close game after other steps. tools might need to unhook themselves from a core.
			CloseGame();
			SaveConfig();
		}

		private readonly bool _suppressSyncSettingsWarning;

		public override bool BlocksInputWhenFocused { get; } = false;

		public int ProgramRunLoop()
		{
			// needs to be done late, after the log console snaps on top
			// fullscreen should snap on top even harder!
			if (_needsFullscreenOnLoad)
			{
				_needsFullscreenOnLoad = false;
				ToggleFullscreen();
			}

			// Simply exit the program if the version is asked for
			if (_argParser.printVersion)
			{
				// Print the version
				Console.WriteLine(VersionInfo.GetEmuVersion());
				// Return and leave
				return _exitCode;
			}

			// incantation required to get the program reliably on top of the console window
			// we might want it in ToggleFullscreen later, but here, it needs to happen regardless
			BringToFront();
			Activate();
			BringToFront();

			InitializeFpsData();

			for (; ; )
			{
				Input.Instance.Update();

				// handle events and dispatch as a hotkey action, or a hotkey button, or an input button
				// ...but prepare haptics first, those get read in ProcessInput
				var finalHostController = InputManager.ControllerInputCoalescer;
				InputManager.ActiveController.PrepareHapticsForHost(finalHostController);
				ProcessInput(
					_hotkeyCoalescer,
					finalHostController,
					InputManager.ClientControls.SearchBindings,
					InputManager.ActiveController.HasBinding);
				InputManager.ClientControls.LatchFromPhysical(_hotkeyCoalescer);

				InputManager.ActiveController.LatchFromPhysical(finalHostController);

				InputManager.ActiveController.ApplyAxisConstraints(
					(Emulator is N64 && Config.N64UseCircularAnalogConstraint) ? "Natural Circle" : null);

				InputManager.ActiveController.OR_FromLogical(InputManager.ClickyVirtualPadController);
				InputManager.AutoFireController.LatchFromPhysical(finalHostController);

				if (InputManager.ClientControls["Autohold"])
				{
					InputManager.ToggleStickies();
				}
				else if (InputManager.ClientControls["Autofire"])
				{
					InputManager.ToggleAutoStickies();
				}

				// autohold/autofire must not be affected by the following inputs
				InputManager.ActiveController.Overrides(InputManager.ButtonOverrideAdapter);

				if (Tools.Has<LuaConsole>())
				{
					Tools.LuaConsole.ResumeScripts(false);
				}

				StepRunLoop_Core();
				Render();
				StepRunLoop_Throttle();

				// HACK: RAIntegration might peek at memory during messages
				// we need this to allow memory access here, otherwise it will deadlock
				var raMemHack = (RA as RAIntegration)?.ThisIsTheRAMemHack();
				raMemHack?.Enter();

				CheckMessages();

				// RA == null possibly due MainForm Dispose disposing RA (which case Exit is not valid anymore)
				// RA != null possibly due to RA object being created (which case raMemHack is null, as RA was null before)
				if (RA is not null) raMemHack?.Exit();

				if (_exitRequestPending)
				{
					_exitRequestPending = false;
					Close();
				}

				if (IsDisposed || _windowClosedAndSafeToExitProcess)
				{
					break;
				}
			}

			Shutdown();
			return _exitCode;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			// NOTE: this gets called twice sometimes. once by using() in Program.cs and once from winforms internals when the form is closed...
			if (DisplayManager != null)
			{
				DisplayManager.Dispose();
				DisplayManager = null;
			}

			RA?.Dispose();
			RA = null;

			if (disposing)
			{
				components?.Dispose();
				SingleInstanceDispose();
			}

			base.Dispose(disposing);
		}

		private bool _emulatorPaused;
		public bool EmulatorPaused
		{
			get => _emulatorPaused;

			private set
			{
				_didMenuPause = false; // overwritten where relevant
				if (_emulatorPaused && !value) // Unpausing
				{
					InitializeFpsData();
				}

				_emulatorPaused = value;
				OnPauseChanged?.Invoke(_emulatorPaused);
			}
		}

		public bool BlockFrameAdvance { get; set; }

		public event Action<bool> OnPauseChanged;

		public string CurrentlyOpenRom { get; private set; } // todo - delete me and use only args instead
		public LoadRomArgs CurrentlyOpenRomArgs { get; private set; }
		public bool PauseAvi { get; set; }
		public bool PressFrameAdvance { get; set; }
		public bool FrameInch { get; set; }
		public bool HoldFrameAdvance { get; set; } // necessary for tastudio > button
		public bool PressRewind { get; set; } // necessary for tastudio < button
		public bool FastForward { get; set; }

		/// <summary>
		/// Disables updates for video/audio, and enters "turbo" mode.
		/// Can be used to replicate Gens-rr's "latency compensation" that involves:
		/// <list type="bullet">
		/// <item><description>Saving a no-framebuffer state that is stored in RAM</description></item>
		/// <item><description>Emulating forth for some frames with updates disabled</description></item>
		/// <item><description><list type="bullet">
		/// <item><description>Optionally hacking in-game memory
		/// (like camera position, to show off-screen areas)</description></item>
		/// </list></description></item>
		/// <item><description>Updating the screen</description></item>
		/// <item><description>Loading the no-framebuffer state from RAM</description></item>
		/// </list>
		/// The most common use case is CamHack for Sonic games.
		/// Accessing this from Lua allows to keep internal code hacks to minimum.
		/// <list type="bullet">
		/// <item><description><see cref="ClientLuaLibrary.InvisibleEmulation(bool)"/></description></item>
		/// <item><description><see cref="ClientLuaLibrary.SeekFrame(int)"/></description></item>
		/// </list>
		/// </summary>
		public bool InvisibleEmulation { get; set; }

		private long MouseWheelTracker;

		private int? _pauseOnFrame;
		public int? PauseOnFrame // If set, upon completion of this frame, the client wil pause
		{
			get => _pauseOnFrame;

			set
			{
				bool wasTurboSeeking = IsTurboSeeking;
				_pauseOnFrame = value;
				SetPauseStatusBarIcon();

				if (wasTurboSeeking && value == null) // TODO: make an Event handler instead, but the logic here is that after turbo seeking, tools will want to do a real update when the emulator finally pauses
				{
					Tools.UpdateToolsBefore();
					Tools.UpdateToolsAfter();
				}
			}
		}

		public bool IsSeeking => PauseOnFrame.HasValue;
		private bool IsTurboSeeking => PauseOnFrame.HasValue && Config.TurboSeek;
		public bool IsTurboing => InputManager.ClientControls["Turbo"] || IsTurboSeeking;

		/// <summary>
		/// Used to disable secondary throttling (e.g. vsync, audio) for unthrottled modes or when the primary (clock) throttle is taking over (e.g. during fast forward/rewind).
		/// </summary>
		public static bool DisableSecondaryThrottling { get; set; }

		public void AddOnScreenMessage(string message, int? duration = null) => OSD.AddMessage(message, duration);

		public void ClearHolds()
		{
			InputManager.StickyXorAdapter.ClearStickies();
			InputManager.AutofireStickyXorAdapter.ClearStickies();

			if (Tools.Has<VirtualpadTool>())
			{
				Tools.VirtualPad.ClearVirtualPadHolds();
			}
		}

		public void FlagNeedsReboot()
		{
			RebootStatusBarIcon.Visible = true;
			AddOnScreenMessage("Core reboot needed for this setting");
		}

		/// <remarks>don't use this, use <see cref="Emulator"/></remarks>
		private IEmulator _emulator;

		public IEmulator Emulator
		{
			get => _emulator;

			private set
			{
				_emulator = value;
				_currentVideoProvider = value.AsVideoProviderOrDefault();
				_currentSoundProvider = value.AsSoundProviderOrDefault();
			}
		}

		public event BeforeQuickLoadEventHandler QuicksaveLoad;

		public event BeforeQuickSaveEventHandler QuicksaveSave;

		public event EventHandler RomLoaded;

		public event StateLoadedEventHandler SavestateLoaded;

		public event StateSavedEventHandler SavestateSaved;

		private readonly InputManager InputManager;

		private IVideoProvider _currentVideoProvider = NullVideo.Instance;

		private ISoundProvider _currentSoundProvider = new NullSound(44100 / 60); // Reasonable default until we have a core instance

		/// <remarks>don't use this, use <see cref="Config"/></remarks>
		private readonly Func<Config> _getGlobalConfig;

		private new Config Config => _getGlobalConfig();

		public Action<string> LoadGlobalConfigFromFile { get; set; }

		private readonly Func<string> _getConfigPath;

		private readonly IGL GL;

		internal readonly ExternalToolManager ExtToolManager;

		public readonly ToolManager Tools;

		private IControlMainform ToolControllingSavestates => Tools.FirstOrNull<IControlMainform>(tool => tool.WantsToControlSavestates);
		private IControlMainform ToolControllingRewind => Tools.FirstOrNull<IControlMainform>(tool => tool.WantsToControlRewind);
		private IControlMainform ToolControllingReboot => Tools.FirstOrNull<IControlMainform>(tool => tool.WantsToControlReboot);
		private IControlMainform ToolControllingStopMovie => Tools.FirstOrNull<IControlMainform>(tool => tool.WantsToControlStopMovie);
		private IControlMainform ToolControllingRestartMovie => Tools.FirstOrNull<IControlMainform>(tool => tool.WantsToControlRestartMovie);
		private IControlMainform ToolControllingReadOnly => Tools.FirstOrNull<IControlMainform>(tool => tool.WantsToControlReadOnly);

		private DisplayManager DisplayManager;

		private OSDManager OSD => DisplayManager.OSD;

		public IMovieSession MovieSession { get; }

		public GameInfo Game { get; private set; }

		/// <remarks>don't use this, use <see cref="Sound"/></remarks>
		private Sound _sound;

		private readonly Action<Sound> _updateGlobalSound;

		private Sound Sound
		{
			get => _sound;
			set => _updateGlobalSound(_sound = value);
		}

		public CheatCollection CheatList { get; }

		public (HttpCommunication HTTP, MemoryMappedFiles MMF, SocketServer Sockets) NetworkingHelpers { get; }

		public IRewinder Rewinder { get; private set; }

		public void CreateRewinder()
		{
			if (ToolControllingRewind is not null) return;

			Rewinder?.Dispose();
			Rewinder = Emulator.HasSavestates() && Config.Rewind.Enabled && (!Emulator.AsStatable().AvoidRewind || Config.Rewind.AllowSlowStates)
				? Config.Rewind.UseDelta
					? new ZeldaWinder(Emulator.AsStatable(), Config.Rewind)
					: new Zwinder(Emulator.AsStatable(), Config.Rewind)
				: null;
			AddOnScreenMessage(Rewinder?.Active == true ? "Rewind started" : "Rewind disabled");
		}

		public FirmwareManager FirmwareManager { get; }

		protected override void OnActivated(EventArgs e)
		{
			base.OnActivated(e);
			Input.Instance.ControlInputFocus(this, ClientInputFocus.Mouse, true);
		}

		protected override void OnDeactivate(EventArgs e)
		{
			Input.Instance.ControlInputFocus(this, ClientInputFocus.Mouse, false);
			base.OnDeactivate(e);
		}

		private void ProcessInput(
			InputCoalescer hotkeyCoalescer,
			ControllerInputCoalescer finalHostController,
			Func<string, List<string>> searchHotkeyBindings,
			Func<string, bool> activeControllerHasBinding)
		{
			Input.Instance.Adapter.SetHaptics(finalHostController.GetHapticsSnapshot());

			// loop through all available events
			InputEvent ie;
			while ((ie = Input.Instance.DequeueEvent()) != null)
			{
				// useful debugging:
				// Console.WriteLine(ie);

				// TODO - wonder what happens if we pop up something interactive as a response to one of these hotkeys? may need to purge further processing

				// look for hotkey bindings for this key
				var triggers = searchHotkeyBindings(ie.LogicalButton.ToString());
				if (triggers.Count == 0)
				{
					// Maybe it is a system alt-key which hasn't been overridden
					if (ie.EventType is InputEventType.Press && (ie.LogicalButton.Modifiers & LogicalButton.MASK_ALT) is not 0U)
					{
						if (ie.LogicalButton.Button.Length == 1)
						{
							var c = ie.LogicalButton.Button.ToLowerInvariant()[0];
							if ((c >= 'a' && c <= 'z') || c == ' ')
							{
								SendAltKeyChar(c);
							}
						}
						else if (ie.LogicalButton.Button == "Space")
						{
							SendPlainAltKey(32);
						}
					}

					// ordinarily, an alt release with nothing else would move focus to the MenuBar. but that is sort of useless, and hard to implement exactly right.

					if (Config.ShowContextMenu && ie.ToString() == "Press:Apps" && ContainsFocus)
					{
						// same as right-click
						MainFormContextMenu.Show(PointToScreen(new(0, MainformMenu.Height)));
					}
				}

				// zero 09-sep-2012 - all input is eligible for controller input. not sure why the above was done.
				// maybe because it doesn't make sense to me to bind hotkeys and controller inputs to the same keystrokes

				switch (Config.InputHotkeyOverrideOptions)
				{
					default:
					case 0: // Both allowed
					{
						finalHostController.Receive(ie);

						var handled = false;
						if (ie.EventType is InputEventType.Press)
						{
							handled = triggers.Aggregate(handled, (current, trigger) => current | CheckHotkey(trigger));
						}

						// hotkeys which aren't handled as actions get coalesced as pollable virtual client buttons
						if (!handled)
						{
							hotkeyCoalescer.Receive(ie);
						}

						break;
					}
					case 1: // Input overrides Hotkeys
					{
						finalHostController.Receive(ie);
						// don't check hotkeys when any of the pressed keys are input
						if (!ie.LogicalButton.ToString().Split('+').Any(activeControllerHasBinding))
						{
							var handled = false;
							if (ie.EventType is InputEventType.Press)
							{
								handled = triggers.Aggregate(false, (current, trigger) => current | CheckHotkey(trigger));
							}

							// hotkeys which aren't handled as actions get coalesced as pollable virtual client buttons
							if (!handled)
							{
								hotkeyCoalescer.Receive(ie);
							}
						}

						break;
					}
					case 2: // Hotkeys override Input
					{
						var handled = false;
						if (ie.EventType is InputEventType.Press)
						{
							handled = triggers.Aggregate(false, (current, trigger) => current | CheckHotkey(trigger));
						}

						// hotkeys which aren't handled as actions get coalesced as pollable virtual client buttons
						if (!handled)
						{
							hotkeyCoalescer.Receive(ie);

							// Check for hotkeys that may not be handled through CheckHotkey() method, reject controller input mapped to these
							if (!triggers.Exists(IsInternalHotkey)) finalHostController.Receive(ie);
						}

						break;
					}
				}
			} // foreach event

			//also handle axes
			//we'll need to isolate the mouse coordinates so we can translate them
			KeyValuePair<string, int>? mouseX = null, mouseY = null;
			foreach (var f in Input.Instance.GetAxisValues())
			{
				if (f.Key == "WMouse X")
					mouseX = f;
				else if (f.Key == "WMouse Y")
					mouseY = f;
				else finalHostController.AcceptNewAxis(f.Key, f.Value);
			}

			//if we found mouse coordinates (and why wouldn't we?) then translate them now
			//NOTE: these must go together, because in the case of screen rotation, X and Y are transformed together
			if(mouseX != null && mouseY != null)
			{
				var p = DisplayManager.UntransformPoint(new Point(mouseX.Value.Value, mouseY.Value.Value));
				float x = p.X / (float)_currentVideoProvider.BufferWidth;
				float y = p.Y / (float)_currentVideoProvider.BufferHeight;
				finalHostController.AcceptNewAxis("WMouse X", (int) ((x * 20000) - 10000));
				finalHostController.AcceptNewAxis("WMouse Y", (int) ((y * 20000) - 10000));
			}

		}

		public bool RebootCore()
		{
			if (ToolControllingReboot is { } tool)
			{
				tool.RebootCore();
				return true;
			}
			else
			{
				if (CurrentlyOpenRomArgs == null) return true;
				return LoadRom(CurrentlyOpenRomArgs.OpenAdvanced.SimplePath, CurrentlyOpenRomArgs);
			}
		}

		public void PauseEmulator()
		{
			EmulatorPaused = true;
			SetPauseStatusBarIcon();
		}

		public void UnpauseEmulator()
		{
			EmulatorPaused = false;
			SetPauseStatusBarIcon();
		}

		public void TogglePause()
		{
			EmulatorPaused = !EmulatorPaused;
			SetPauseStatusBarIcon();

			// TODO: have tastudio set a pause status change callback, or take control over pause
			if (Tools.Has<TAStudio>())
			{
				Tools.UpdateValues<TAStudio>();
			}
		}

		public void TakeScreenshotToClipboard()
		{
			using var bb = Config.ScreenshotCaptureOsd ? CaptureOSD() : MakeScreenshotImage();
			bb.ToSysdrawingBitmap().ToClipBoard();
			AddOnScreenMessage("Screenshot (raw) saved to clipboard.");
		}

		private void TakeScreenshotClientToClipboard()
		{
			using var bb = DisplayManager.RenderOffscreen(_currentVideoProvider, Config.ScreenshotCaptureOsd);
			bb.ToSysdrawingBitmap().ToClipBoard();
			AddOnScreenMessage("Screenshot (client) saved to clipboard.");
		}

		private string ScreenshotPrefix()
		{
			var screenPath = Config.PathEntries.ScreenshotAbsolutePathFor(Game.System);
			var name = Game.FilesystemSafeName();
			return Path.Combine(screenPath, name);
		}

		public void TakeScreenshot()
		{
			var basename = $"{ScreenshotPrefix()}.{DateTime.Now:yyyy-MM-dd HH.mm.ss}";

			var fnameBare = $"{basename}.png";
			var fname = $"{basename} (0).png";

			// if the (0) filename exists, do nothing. we'll bump up the number later
			// if the bare filename exists, move it to (0)
			// otherwise, no related filename exists, and we can proceed with the bare filename
			if (!File.Exists(fname))
			{
				if (File.Exists(fnameBare)) File.Move(fnameBare, fname);
				else fname = fnameBare;
			}

			for (var seq = 0; File.Exists(fname); seq++)
				fname = $"{basename} ({seq}).png";

			TakeScreenshot(fname);
		}

		public void TakeScreenshot(string path)
		{
			var fi = new FileInfo(path);
			if (fi.Directory != null && !fi.Directory.Exists)
			{
				fi.Directory.Create();
			}

			using (var bb = Config.ScreenshotCaptureOsd ? CaptureOSD() : MakeScreenshotImage())
			{
				using var img = bb.ToSysdrawingBitmap();
				if (Path.GetExtension(path).ToUpperInvariant() == ".JPG")
				{
					img.Save(fi.FullName, ImageFormat.Jpeg);
				}
				else
				{
					img.Save(fi.FullName, ImageFormat.Png);
				}
			}

			AddOnScreenMessage($"{fi.Name} saved.");
		}

		public void FrameBufferResized(bool forceWindowResize = false)
		{
			if (WindowState is not FormWindowState.Normal)
			{
				// Wait until no longer maximized/minimized to get correct size/location values
				_framebufferResizedPending = true;
				return;
			}
			if (!Config.ResizeWithFramebuffer && !forceWindowResize)
			{
				return;
			}
			// run this entire thing exactly twice, since the first resize may adjust the menu stacking
			for (int i = 0; i < 2; i++)
			{
				int zoom = Config.GetWindowScaleFor(Emulator.SystemId);
				var area = Screen.FromControl(this).WorkingArea;

				int borderWidth = Size.Width - _presentationPanel.Control.Size.Width;
				int borderHeight = Size.Height - _presentationPanel.Control.Size.Height;

				// start at target zoom and work way down until we find acceptable zoom
				Size lastComputedSize = new Size(1, 1);
				for (; zoom >= 1; zoom--)
				{
					lastComputedSize = DisplayManager.CalculateClientSize(_currentVideoProvider, zoom);
					if (lastComputedSize.Width + borderWidth < area.Width
						&& lastComputedSize.Height + borderHeight < area.Height)
					{
						break;
					}
				}

//				Util.DebugWriteLine($"For emulator framebuffer {new Size(_currentVideoProvider.BufferWidth, _currentVideoProvider.BufferHeight)}:");
//				Util.DebugWriteLine($"  For virtual size {new Size(_currentVideoProvider.VirtualWidth, _currentVideoProvider.VirtualHeight)}:");
//				Util.DebugWriteLine($"  Selecting display size {lastComputedSize}");

				// Change size
				Size = new Size(lastComputedSize.Width + borderWidth, lastComputedSize.Height + borderHeight);
				PerformLayout();
				_presentationPanel.Resized = true;

				// Is window off the screen at this size?
				if (!area.Contains(Bounds))
				{
					// At large framebuffer sizes/low screen resolutions, the window may be too large to fit the screen even at 1x scale
					// Prioritize that the top-left of the window is on-screen so the title bar and menu stay accessible

					if (Bounds.Right > area.Right) // Window is off the right edge
					{
						Left = Math.Max(area.Right - Size.Width, area.Left);
					}

					if (Bounds.Bottom > area.Bottom) // Window is off the bottom edge
					{
						Top = Math.Max(area.Bottom - Size.Height, area.Top);
					}
				}
			}
		}

		private void SynchChrome()
		{
			if (_inFullscreen)
			{
				// TODO - maybe apply a hack tracked during fullscreen here to override it
				FormBorderStyle = FormBorderStyle.None;
				MainMenuStrip.Visible = Config.DispChromeMenuFullscreen && !_argParser._chromeless;
				MainStatusBar.Visible = Config.DispChromeStatusBarFullscreen && !_argParser._chromeless;
			}
			else
			{
				MainStatusBar.Visible = Config.DispChromeStatusBarWindowed && !_argParser._chromeless;
				MainMenuStrip.Visible = Config.DispChromeMenuWindowed && !_argParser._chromeless;
				MaximizeBox = MinimizeBox = Config.DispChromeCaptionWindowed && !_argParser._chromeless;
				if (Config.DispChromeFrameWindowed == 0 || _argParser._chromeless)
				{
					FormBorderStyle = FormBorderStyle.None;
				}
				else if (Config.DispChromeFrameWindowed == 1)
				{
					FormBorderStyle = FormBorderStyle.SizableToolWindow;
				}
				else if (Config.DispChromeFrameWindowed == 2)
				{
					FormBorderStyle = FormBorderStyle.Sizable;
				}
			}
		}

		public void ToggleFullscreen(bool allowSuppress = false)
		{
			AutohideCursor(false);

			// prohibit this operation if the current controls include LMouse
			if (allowSuppress)
			{
				if (InputManager.ActiveController.HasBinding("WMouse L"))
				{
					return;
				}
			}

			if (!_inFullscreen)
			{
				SuspendLayout();

				// Work around an AMD driver bug in >= vista:
				// It seems windows will activate opengl fullscreen mode when a GL control is occupying the exact space of a screen (0,0 and dimensions=screensize)
				// AMD cards manifest a problem under these circumstances, flickering other monitors.
				// It isn't clear whether nvidia cards are failing to employ this optimization, or just not flickering.
				// (this could be determined with more work; other side affects of the fullscreen mode include: corrupted TaskBar, no modal boxes on top of GL control, no screenshots)
				// At any rate, we can solve this by adding a 1px black border around the GL control
				// Please note: It is important to do this before resizing things, otherwise momentarily a GL control without WS_BORDER will be at the magic dimensions and cause the flakeout
				if (!OSTailoredCode.IsUnixHost
					&& Config.DispFullscreenHacks
					&& Config.DispMethod == EDispMethod.OpenGL)
				{
					// ATTENTION: this causes the StatusBar to not work well, since the backcolor is now set to black instead of SystemColors.Control.
					// It seems that some StatusBar elements composite with the backcolor.
					// Maybe we could add another control under the StatusBar. with a different backcolor
					Padding = new Padding(1);
					BackColor = Color.Black;

					// FUTURE WORK:
					// re-add this padding back into the display manager (so the image will get cut off a little but, but a few more resolutions will fully fit into the screen)
				}

				_windowedLocation = Location;

				_inFullscreen = true;
				SynchChrome();
				WindowState = FormWindowState.Maximized; // be sure to do this after setting the chrome, otherwise it wont work fully
				ResumeLayout();

				_presentationPanel.Resized = true;
			}
			else
			{
				SuspendLayout();

				WindowState = FormWindowState.Normal;

				if (!OSTailoredCode.IsUnixHost)
				{
					// do this even if DispFullscreenHacks aren't enabled, to restore it in case it changed underneath us or something
					Padding = new Padding(0);

					// it's important that we set the form color back to this, because the StatusBar icons blend onto the mainform, not onto the StatusBar--
					// so we need the StatusBar and mainform backdrop color to match
					BackColor = SystemColors.Control;
				}

				_inFullscreen = false;

				SynchChrome();
				Location = _windowedLocation;
				ResumeLayout();

				FrameBufferResized();
			}
		}

		private void OpenLuaConsole()
		{
			if (!LuaLibraries.IsAvailable)
			{
				ShowMessageBox(
					owner: null,
					text: "Native Lua dynamic library was unable to be loaded. " + (OSTailoredCode.IsUnixHost
						? "Make sure Lua is installed with your package manager."
						: "This library is provided in the dll/ folder, try redownloading BizHawk to fix this error."),
					caption: "Lua Load Error",
					EMsgBoxIcon.Error);
				return;
			}

			Tools.Load<LuaConsole>();
		}

		public void ClickSpeedItem(int num)
		{
			if ((ModifierKeys & Keys.Control) != 0)
			{
				SetSpeedPercentAlternate(num);
			}
			else
			{
				SetSpeedPercent(num);
			}
		}

		public void Unthrottle()
		{
			Config.Unthrottled = true;
		}

		public void Throttle()
		{
			Config.Unthrottled = false;
		}

		private void ThrottleMessage()
		{
			string type = ":(none)";
			if (Config.SoundThrottle)
			{
				type = ":Sound";
			}

			if (Config.VSyncThrottle)
			{
				type = $":Vsync{(Config.VSync ? "[ena]" : "[dis]")}";
			}

			if (Config.ClockThrottle)
			{
				type = ":Clock";
			}

			string throttled = Config.Unthrottled ? "Unthrottled" : "Throttled";
			string msg = $"{throttled}{type} ";

			AddOnScreenMessage(msg);
		}

		public void FrameSkipMessage()
		{
			AddOnScreenMessage($"Frameskipping set to {Config.FrameSkip}");
		}

		public void UpdateCheatStatus()
		{
			if (CheatList.AnyActive)
			{
				CheatStatusButton.ToolTipText = "Cheats are currently active";
				CheatStatusButton.Image = Properties.Resources.Freeze;
				CheatStatusButton.Visible = true;
			}
			else
			{
				CheatStatusButton.ToolTipText = "";
				CheatStatusButton.Image = Properties.Resources.Blank;
				CheatStatusButton.Visible = false;
			}
		}

		public bool RunLibretroCoreChooser()
		{
			string initFileName = null;
			string initDir;
			if (Config.LibretroCore is not null)
			{
				(initDir, initFileName) = Config.LibretroCore.SplitPathToDirAndFile();
			}
			else
			{
				initDir = Config.PathEntries.AbsolutePathForType(VSystemID.Raw.Libretro, "Cores");
				Directory.CreateDirectory(initDir);
			}
			var result = this.ShowFileOpenDialog(
				discardCWDChange: true,
				filter: LibretroCoresFSFilterSet,
				initDir: initDir!,
				initFileName: initFileName);
			if (result is null) return false;
			Config.LibretroCore = result;
			return true;
		}

		private Size _lastVideoSize = new Size(-1, -1), _lastVirtualSize = new Size(-1, -1);
		private readonly SaveSlotManager _stateSlots = new SaveSlotManager();

		// AVI/WAV state
		private IVideoWriter _currAviWriter;

		private AutofireController _autofireNullControls;

		// Sound refactor TODO: we can enforce async mode here with a property that gets/sets this but does an async check
		private ISoundProvider _aviSoundInputAsync; // Note: This sound provider must be in async mode!

		private SimpleSyncSoundProvider _dumpProxy; // an audio proxy used for dumping

		private bool _windowClosedAndSafeToExitProcess;
		private int _exitCode;
		private bool _exitRequestPending;
		private bool _runloopFrameProgress;
		private long _frameAdvanceTimestamp;
		private long _frameRewindTimestamp;
		private bool _frameRewindWasPaused;
		private bool _runloopFrameAdvance;
		private bool _lastFastForwardingOrRewinding;
		private bool _inResizeLoop;

		private readonly double _fpsUpdatesPerSecond = 4.0;
		private readonly double _fpsSmoothing = 8.0;
		private double _lastFps;
		private int _lastFpsRounded;
		private int _framesSinceLastFpsUpdate;
		private long _timestampLastFpsUpdate;

		public int GetApproxFramerate() => _lastFpsRounded;

		private readonly Throttle _throttle;

		// For handling automatic pausing when entering the menu
		private bool _wasPaused;
		private bool _didMenuPause;

		private bool _cursorHidden;
		private bool _inFullscreen;
		private Point _windowedLocation;
		private bool _needsFullscreenOnLoad;
		private bool _framebufferResizedPending;

		private int _lastOpenRomFilter;

		private readonly ParsedCLIFlags _argParser;

		private int _autoDumpLength;

		// Resources
		private Bitmap _statusBarDiskLightOnImage;
		private Bitmap _statusBarDiskLightOffImage;
		private Bitmap _linkCableOn;
		private Bitmap _linkCableOff;

		// input state which has been destined for game controller inputs are coalesced here
		// public static ControllerInputCoalescer ControllerInputCoalescer = new ControllerInputCoalescer();
		// input state which has been destined for client hotkey consumption are colesced here
		private readonly InputCoalescer _hotkeyCoalescer = new InputCoalescer();

		private readonly PresentationPanel _presentationPanel;

		// countdown for saveram autoflushing
		public int AutoFlushSaveRamIn { get; set; }

		private void SetStatusBar()
		{
			if (!_inFullscreen)
			{
				MainStatusBar.Visible = Config.DispChromeStatusBarWindowed;
				PerformLayout();
				FrameBufferResized();
			}
		}

		protected override string WindowTitle
		{
			get
			{
				var sb = new StringBuilder();

				if (_inResizeLoop)
				{
					var size = _presentationPanel.NativeSize;
					sb.Append($"({size.Width}x{size.Height})={(float) size.Width / size.Height} - ");
				}

				if (Config.DispSpeedupFeatures == 0)
				{
					// we need to display FPS somewhere, in this case
					sb.Append($"({_lastFps:0} fps) - ");
				}

				if (!Emulator.IsNull())
				{
					sb.Append($"{Game.Name} [{Emulator.GetSystemDisplayName()}] - ");
					if (MovieSession.Movie.IsActive())
					{
						sb.Append($"{Path.GetFileName(MovieSession.Movie.Filename)} - ");
					}
				}

				sb.Append(string.IsNullOrEmpty(VersionInfo.CustomBuildString)
					? "BizHawk"
					: VersionInfo.CustomBuildString);
				if (VersionInfo.DeveloperBuild) sb.Append(" (interim)");

				return sb.ToString();
			}
		}

		protected override string WindowTitleStatic
		{
			get
			{
				var sb = new StringBuilder();
				sb.Append(string.IsNullOrEmpty(VersionInfo.CustomBuildString)
					? "BizHawk"
					: VersionInfo.CustomBuildString);
				if (VersionInfo.DeveloperBuild) sb.Append(" (interim)");
				return sb.ToString();
			}
		}

		private void ClearAutohold()
		{
			ClearHolds();
			AddOnScreenMessage("Autohold keys cleared");
		}

		private void UpdateToolsLoadstate()
		{
			if (Tools.Has<SNESGraphicsDebugger>())
			{
				Tools.SNESGraphicsDebugger.UpdateToolsLoadstate();
			}
		}

		private void UpdateToolsAfter()
		{
			Tools.UpdateToolsAfter();
			HandleToggleLightAndLink();
		}

		public void UpdateDumpInfo(RomStatus? newStatus = null)
		{
			DumpStatusButton.Image = Properties.Resources.Blank;
			DumpStatusButton.ToolTipText = "";

			if (Emulator.IsNull() || Game.IsNullInstance())
			{
				return;
			}

			var status = newStatus == null
				? Game.Status
				: (Game.Status = newStatus.Value);
			if (status == RomStatus.BadDump)
			{
				DumpStatusButton.Image = Properties.Resources.ExclamationRed;
				DumpStatusButton.ToolTipText = "Warning: Bad ROM Dump";
			}
			else if (status == RomStatus.Overdump)
			{
				DumpStatusButton.Image = Properties.Resources.ExclamationRed;
				DumpStatusButton.ToolTipText = "Warning: Overdump";
			}
			else if (status == RomStatus.NotInDatabase)
			{
				DumpStatusButton.Image = Properties.Resources.RetroQuestion;
				DumpStatusButton.ToolTipText = "Warning: Unknown ROM";
			}
			else if (status == RomStatus.TranslatedRom)
			{
				DumpStatusButton.Image = Properties.Resources.Translation;
				DumpStatusButton.ToolTipText = "Translated ROM";
			}
			else if (status == RomStatus.Homebrew)
			{
				DumpStatusButton.Image = Properties.Resources.HomeBrew;
				DumpStatusButton.ToolTipText = "Homebrew ROM";
			}
			else if (Game.Status == RomStatus.Hack)
			{
				DumpStatusButton.Image = Properties.Resources.Hack;
				DumpStatusButton.ToolTipText = "Hacked ROM";
			}
			else if (Game.Status == RomStatus.Unknown)
			{
				DumpStatusButton.Image = Properties.Resources.Hack;
				DumpStatusButton.ToolTipText = "Warning: ROM of Unknown Character";
			}
			else if (Game.Status == RomStatus.Imperfect)
			{
				DumpStatusButton.Image = Properties.Resources.RetroQuestion;
				DumpStatusButton.ToolTipText = "Warning: Imperfect emulation";
			}
			else if (Game.Status == RomStatus.Unimplemented)
			{
				DumpStatusButton.Image = Properties.Resources.ExclamationRed;
				DumpStatusButton.ToolTipText = "Warning: Unemulated features";
			}
			else if (Game.Status == RomStatus.NotWorking)
			{
				DumpStatusButton.Image = Properties.Resources.ExclamationRed;
				DumpStatusButton.ToolTipText = "Warning: The game does not work";
			}
			else
			{
				DumpStatusButton.Image = Properties.Resources.GreenCheck;
				DumpStatusButton.ToolTipText = "Verified good dump";
			}

			if (_multiDiskMode
				&& Game.Status is not (RomStatus.Imperfect or RomStatus.Unimplemented or RomStatus.NotWorking))
			{
				DumpStatusButton.ToolTipText = "Multi-disk bundler";
				DumpStatusButton.Image = Properties.Resources.RetroQuestion;
			}
		}

		private bool _multiDiskMode;

		// Rom details as decided by MainForm, which shouldn't happen, the RomLoader or Core should be doing this
		// Better is to just keep the game and rom hashes as properties and then generate the rom info from this
		private string _defaultRomDetails = "";

		private void LoadSaveRam()
		{
			if (Emulator.HasSaveRam())
			{
				try // zero says: this is sort of sketchy... but this is no time for rearchitecting
				{
					var saveRamPath = Config.PathEntries.SaveRamAbsolutePath(Game, MovieSession.Movie);
					if (Config.AutosaveSaveRAM)
					{
						var saveram = new FileInfo(saveRamPath);
						var autosave = new FileInfo(Config.PathEntries.AutoSaveRamAbsolutePath(Game, MovieSession.Movie));
						if (autosave.Exists && autosave.LastWriteTime > saveram.LastWriteTime)
						{
							AddOnScreenMessage("AutoSaveRAM is newer than last saved SaveRAM");
						}
					}

					byte[] sram;

					// some cores might not know how big the saveram ought to be, so just send it the whole file
					if (Emulator is AppleII or C64 or MGBAHawk or NeoGeoPort or NES { BoardName: "FDS" })
					{
						sram = File.ReadAllBytes(saveRamPath);
					}
					else
					{
						var oldRam = Emulator.AsSaveRam().CloneSaveRam();
						if (oldRam == null)
						{
							// we're eating this one now. The possible negative consequence is that a user could lose
							// their saveram and not know why
							// ShowMessageBox(owner: null, "Error: tried to load saveram, but core would not accept it?");
							return;
						}

						// why do we silently truncate\pad here instead of warning\erroring?
						sram = new byte[oldRam.Length];
						using var reader = new BinaryReader(new FileStream(saveRamPath, FileMode.Open, FileAccess.Read));
						reader.Read(sram, 0, sram.Length);
					}

					Emulator.AsSaveRam().StoreSaveRam(sram);
					AutoFlushSaveRamIn = Config.FlushSaveRamFrames;
				}
				catch (IOException)
				{
					AddOnScreenMessage("An error occurred while loading Sram");
				}
			}
		}

		public bool FlushSaveRAM(bool autosave = false)
		{
			if (Emulator.HasSaveRam())
			{
				string path;
				if (autosave)
				{
					path = Config.PathEntries.AutoSaveRamAbsolutePath(Game, MovieSession.Movie);
					AutoFlushSaveRamIn = Config.FlushSaveRamFrames;
				}
				else
				{
					path =  Config.PathEntries.SaveRamAbsolutePath(Game, MovieSession.Movie);
				}

				var file = new FileInfo(path);
				var newPath = $"{path}.new";
				var newFile = new FileInfo(newPath);
				var backupPath = $"{path}.bak";
				var backupFile = new FileInfo(backupPath);
				if (file.Directory != null && !file.Directory.Exists)
				{
					try
					{
						file.Directory.Create();
					}
					catch
					{
						AddOnScreenMessage($"Unable to flush SaveRAM to: {newFile.Directory}");
						return false;
					}
				}

				var saveram = Emulator.AsSaveRam().CloneSaveRam();
				if (saveram == null)
					return true;

				using (var writer = new BinaryWriter(new FileStream(newPath, FileMode.Create, FileAccess.Write)))
					writer.Write(saveram, 0, saveram.Length);

				if (file.Exists)
				{
					if (Config.BackupSaveram)
					{
						if (backupFile.Exists)
						{
							backupFile.Delete();
						}

						file.MoveTo(backupPath);
					}
					else
					{
						file.Delete();
					}
				}

				newFile.MoveTo(path);
			}

			return true;
		}

		private void RewireSound()
		{
			if (_dumpProxy != null)
			{
				// we're video dumping, so async mode only and use the DumpProxy.
				// note that the avi dumper has already rewired the emulator itself in this case.
				Sound.SetInputPin(_dumpProxy);
			}
			else
			{
				bool useAsyncMode = _currentSoundProvider.CanProvideAsync && !Config.SoundThrottle;
				_currentSoundProvider.SetSyncMode(useAsyncMode ? SyncSoundMode.Async : SyncSoundMode.Sync);
				Sound.SetInputPin(_currentSoundProvider);
			}
		}

		private static readonly IList<Type> SpecializedTools = ReflectionCache.Types
			.Where(static t => !t.IsAbstract && typeof(IToolForm).IsAssignableFrom(t)
				&& t.GetCustomAttribute<SpecializedToolAttribute>() is not null)
			.ToList();

		private ISet<char> _availableAccelerators;

		private ISet<char> AvailableAccelerators
		{
			get
			{
				if (_availableAccelerators == null)
				{
					_availableAccelerators = new HashSet<char>();
					for (var c = 'A'; c <= 'Z'; c++) _availableAccelerators.Add(c);
					foreach (ToolStripItem item in MainMenuStrip.Items)
					{
						if (!item.Visible) continue;
						var i = item.Text.IndexOf('&');
						if (i == -1 || i == item.Text.Length - 1) continue;
						_availableAccelerators.Remove(char.ToUpperInvariant(item.Text[i + 1]));
					}
				}
				return _availableAccelerators;
			}
		}

		private void DisplayDefaultCoreMenu()
		{
			GenericCoreSubMenu.Visible = true;
			var sysID = Emulator.SystemId;
			for (var i = 0; i < sysID.Length; i++)
			{
				var upper = char.ToUpperInvariant(sysID[i]);
				if (AvailableAccelerators.Contains(upper))
				{
					AvailableAccelerators.Remove(upper);
					sysID = sysID.Insert(i, "&");
					break;
				}
			}
			GenericCoreSubMenu.Text = sysID;
			GenericCoreSubMenu.DropDownItems.Clear();

			var settingsMenuItem = new ToolStripMenuItem { Text = "&Settings" };
			settingsMenuItem.Click += GenericCoreSettingsMenuItem_Click;
			GenericCoreSubMenu.DropDownItems.Add(settingsMenuItem);

			var specializedTools = SpecializedTools.Where(Tools.IsAvailable).OrderBy(static t => t.Name).ToList();
			if (specializedTools.Count is 0) return;

			GenericCoreSubMenu.DropDownItems.Add(new ToolStripSeparator());
			foreach (var toolType in specializedTools)
			{
				var (icon, name) = Tools.GetIconAndNameFor(toolType);
				ToolStripMenuItem item = new() { Image = icon, Text = $"&{name}" };
				item.Click += (_, _) => Tools.Load(toolType);
				GenericCoreSubMenu.DropDownItems.Add(item);
			}
		}

		private void InitControls()
		{
			Controller controls = new(new ControllerDefinition("Emulator Frontend Controls")
			{
				BoolButtons = Config.HotkeyBindings.Keys.ToList(),
			}.MakeImmutable());

			foreach (var (k, v) in Config.HotkeyBindings) controls.BindMulti(k, v);

			InputManager.ClientControls = controls;
			InputManager.ControllerInputCoalescer = new(); // ctor initialises values for host haptics
			_autofireNullControls = new AutofireController(
				Emulator,
				Config.AutofireOn,
				Config.AutofireOff);
		}

		private void LoadMoviesFromRecent(string path)
		{
			if (File.Exists(path))
			{
				var movie = MovieSession.Get(path, true);
				MovieSession.ReadOnly = true;
				StartNewMovie(movie, false);
			}
			else
			{
				Config.RecentMovies.HandleLoadError(this, path);
			}
		}

		private void LoadRomFromRecent(string rom)
		{
			var ioa = OpenAdvancedSerializer.ParseWithLegacy(rom);

			// if(ioa is this or that) - for more complex behaviour
			string romPath = ioa.SimplePath;

			if (!LoadRom(romPath, new LoadRomArgs(ioa), out var failureIsFromAskSave))
			{
				if (failureIsFromAskSave) AddOnScreenMessage("ROM loading cancelled; a tool had unsaved changes");
				else if (ioa is OpenAdvanced_LibretroNoGame || File.Exists(romPath)) AddOnScreenMessage("ROM loading failed");
				else Config.RecentRoms.HandleLoadError(this, romPath, rom);
			}
		}

		private void SetPauseStatusBarIcon()
		{
			if (EmulatorPaused)
			{
				PauseStatusButton.Image = Properties.Resources.Pause;
				PauseStatusButton.Visible = true;
				PauseStatusButton.ToolTipText = "Emulator Paused";
			}
			else if (IsTurboSeeking)
			{
				PauseStatusButton.Image = Properties.Resources.Lightning;
				PauseStatusButton.Visible = true;
				// ReSharper disable once PossibleInvalidOperationException
				PauseStatusButton.ToolTipText = $"Emulator is turbo seeking to frame {PauseOnFrame.Value} click to stop seek";
			}
			else if (PauseOnFrame.HasValue)
			{
				PauseStatusButton.Image = Properties.Resources.YellowRight;
				PauseStatusButton.Visible = true;
				PauseStatusButton.ToolTipText = $"Emulator is playing to frame {PauseOnFrame.Value} click to stop seek";
			}
			else
			{
				PauseStatusButton.Image = Properties.Resources.Blank;
				PauseStatusButton.Visible = false;
				PauseStatusButton.ToolTipText = "";
			}
		}

		private void SyncThrottle()
		{
			// "unthrottled" = throttle was turned off with "Toggle Throttle" hotkey
			// "turbo" = throttle is off due to the "Turbo" hotkey being held
			// They are basically the same thing but one is a toggle and the other requires a
			// hotkey to be held. There is however slightly different behavior in that turbo
			// skips outputting the audio. There's also a third way which is when no throttle
			// method is selected, but the clock throttle determines that by itself and
			// everything appears normal here.
			var rewind = Rewinder?.Active == true && (InputManager.ClientControls["Rewind"] || PressRewind);
			var fastForward = InputManager.ClientControls["Fast Forward"] || FastForward;
			var turbo = IsTurboing;

			int speedPercent = fastForward ? Config.SpeedPercentAlternate : Config.SpeedPercent;

			if (rewind)
			{
				speedPercent = Math.Max(speedPercent / Rewinder.RewindFrequency, 5);
			}

			DisableSecondaryThrottling = Config.Unthrottled || turbo || fastForward || rewind;

			// realtime throttle is never going to be so exact that using a double here is wrong
			_throttle.SetCoreFps(Emulator.VsyncRate());
			_throttle.signal_paused = EmulatorPaused;
			_throttle.signal_unthrottle = Config.Unthrottled || turbo;

			// zero 26-mar-2016 - vsync and vsync throttle here both is odd, but see comments elsewhere about triple buffering
			_throttle.signal_overrideSecondaryThrottle = (fastForward || rewind) && (Config.SoundThrottle || Config.VSyncThrottle || Config.VSync);
			_throttle.SetSpeedPercent(speedPercent);
		}

		private void SetSpeedPercentAlternate(int value)
		{
			Config.SpeedPercentAlternate = value;
			SyncThrottle();
			AddOnScreenMessage($"Alternate Speed: {value}%");
		}

		private void SetSpeedPercent(int value)
		{
			Config.SpeedPercent = value;
			SyncThrottle();
			AddOnScreenMessage($"Speed: {value}%");
		}

		private void Shutdown()
		{
			if (_currAviWriter != null)
			{
				_currAviWriter.CloseFile();
				_currAviWriter = null;
			}
		}

		private DateTime _lastMessageCheck = DateTime.MinValue;

		private void CheckMessages()
		{
			var currentTime = DateTime.UtcNow;
			// only check window messages a maximum of once per millisecond
			// this check is irrelvant for the 99% of cases where fps are <1k
			// but gives a slight fps boost in those scenarios
			if ((uint)(currentTime - _lastMessageCheck).Milliseconds > 0)
			{
				_lastMessageCheck = currentTime;
				Application.DoEvents();
			}

			if (ActiveForm != null)
			{
				ScreenSaver.ResetTimerPeriodically();
			}

			if (PathsFromDragDrop is not null) this.DoWithTempMute(() =>
			{
				try
				{
					FormDragDrop_internal();
				}
				catch (Exception ex)
				{
					ShowMessageBox(owner: null, $"Exception on drag and drop:\n{ex}");
				}
				PathsFromDragDrop = null;
			});

			string[][] todo = Array.Empty<string[]>();
			lock (_singleInstanceForwardedArgs)
			{
				if (_singleInstanceForwardedArgs.Count > 0)
				{
					todo = _singleInstanceForwardedArgs.ToArray();
					_singleInstanceForwardedArgs.Clear();
				}
			}
			foreach (var args in todo) SingleInstanceProcessArgs(args);
		}

		private void AutohideCursor(bool hide)
		{
			if (hide && !_cursorHidden)
			{
				_presentationPanel.Control.Cursor = Properties.Resources.BlankCursor;
				_cursorHidden = true;
			}
			else if (!hide && _cursorHidden)
			{
				_presentationPanel.Control.Cursor = Cursors.Default;
				timerMouseIdle.Stop();
				timerMouseIdle.Start();
				_cursorHidden = false;
			}
		}

		public BitmapBuffer MakeScreenshotImage()
		{
			var ret = new BitmapBuffer(_currentVideoProvider.BufferWidth, _currentVideoProvider.BufferHeight, _currentVideoProvider.GetVideoBufferCopy());
			ret.DiscardAlpha();
			return ret;
		}

		private void SaveSlotSelectedMessage()
		{
			int slot = Config.SaveSlot;
			string emptyPart = HasSlot(slot) ? "" : " (empty)";
			string message = $"Slot {slot}{emptyPart} selected.";
			AddOnScreenMessage(message);
		}

		/*internal*/public void Render()
		{
			if (Config.DispSpeedupFeatures == 0)
			{
				DisplayManager.DiscardApiHawkSurfaces();
				return;
			}

			var video = _currentVideoProvider;
			Size currVideoSize = new Size(video.BufferWidth, video.BufferHeight);
			Size currVirtualSize = new Size(video.VirtualWidth, video.VirtualHeight);


			bool resizeFramebuffer = currVideoSize != _lastVideoSize || currVirtualSize != _lastVirtualSize;

			bool isZero = currVideoSize.Width == 0 || currVideoSize.Height == 0 || currVirtualSize.Width == 0 || currVirtualSize.Height == 0;

			//don't resize if the new size is 0 somehow; we'll wait until we have a sensible size
			if (isZero)
			{
				resizeFramebuffer = false;
			}

			if (resizeFramebuffer)
			{
				_lastVideoSize = currVideoSize;
				_lastVirtualSize = currVirtualSize;
				FrameBufferResized();
			}

			//rendering flakes out egregiously if we have a zero size
			//can we fix it later not to?
			if (isZero)
				DisplayManager.Blank();
			else
				DisplayManager.UpdateSource(video);
		}

		// sends a simulation of a plain alt key keystroke
		private void SendPlainAltKey(int lparam)
		{
			var m = new Message { WParam = new IntPtr(0xF100), LParam = new IntPtr(lparam), Msg = 0x0112, HWnd = Handle };
			base.WndProc(ref m);
		}

		/// <summary>HACK to send an alt+mnemonic combination</summary>
		private void SendAltKeyChar(char c)
			=> _ = typeof(ToolStrip).InvokeMember(
				OSTailoredCode.IsUnixHost ? "ProcessMnemonic" : "ProcessMnemonicInternal",
				BindingFlags.NonPublic | BindingFlags.InvokeMethod | BindingFlags.Instance,
				null,
				MainformMenu,
				new object/*?*/[] { c },
				CultureInfo.InvariantCulture);

		public static readonly FilesystemFilterSet ConfigFileFSFilterSet = new(new FilesystemFilter("Config File", new[] { "ini" }))
		{
			AppendAllFilesEntry = false,
		};

		private void OpenRom()
		{
			var result = this.ShowFileOpenDialog(
				filter: RomLoader.RomFilter,
				filterIndex: ref _lastOpenRomFilter,
				initDir: Config.PathEntries.RomAbsolutePath(Emulator.SystemId));
			if (result is null) return;
			var filePath = new FileInfo(result).FullName;
			_ = LoadRom(filePath, new LoadRomArgs(new OpenAdvanced_OpenRom(filePath)));
		}

		private void CoreSyncSettings(object sender, RomLoader.SettingsLoadArgs e)
		{
			if (MovieSession.NewMovieQueued)
			{
				if (!string.IsNullOrWhiteSpace(MovieSession.QueuedSyncSettings))
				{
					e.Settings = ConfigService.LoadWithType(MovieSession.QueuedSyncSettings, e.SettingsType);
				}
				else
				{
					e.Settings = Config.GetCoreSyncSettings(e.Core, e.SettingsType);

					// Only show this nag if the core actually has sync settings, not all cores do
					if (e.Settings != null && !_suppressSyncSettingsWarning)
					{
						ShowMessageBox(
							owner: null,
							"No sync settings found, using currently configured settings for this core.",
							"No sync settings found",
							EMsgBoxIcon.Warning);
					}
				}
			}
			else
			{
				e.Settings = Config.GetCoreSyncSettings(e.Core, e.SettingsType);
			}
		}

		private void CoreSettings(object sender, RomLoader.SettingsLoadArgs e)
		{
			e.Settings = Config.GetCoreSettings(e.Core, e.SettingsType);
		}

		private void HandlePutCoreSettings(PutSettingsDirtyBits dirty)
		{
			if (dirty.HasFlag(PutSettingsDirtyBits.RebootCore)) FlagNeedsReboot();
			if (dirty.HasFlag(PutSettingsDirtyBits.ScreenLayoutChanged)) FrameBufferResized();
		}

		private bool MayPutCoreSyncSettings()
		{
			if (MovieSession.Movie.IsActive())
			{
				AddOnScreenMessage("Attempt to change sync-relevant settings while recording BLOCKED.");
				return false;
			}
			return true;
		}

		private void HandlePutCoreSyncSettings(PutSettingsDirtyBits dirty)
		{
			if (dirty.HasFlag(PutSettingsDirtyBits.RebootCore)) FlagNeedsReboot();
		}

		public ISettingsAdapter GetSettingsAdapterFor<T>()
			where T : IEmulator
			=> Emulator is T
				? GetSettingsAdapterForLoadedCoreUntyped()
				: new ConfigSettingsAdapter<T>(Config);

		public ISettingsAdapter GetSettingsAdapterForLoadedCore<T>()
			where T : IEmulator
		{
			if (Emulator is not T) throw new InvalidOperationException();
			return GetSettingsAdapterForLoadedCoreUntyped();
		}

		public SettingsAdapter GetSettingsAdapterForLoadedCoreUntyped()
			=> new(Emulator, static () => true, HandlePutCoreSettings, MayPutCoreSyncSettings, HandlePutCoreSyncSettings);

		private void SaveConfig(string path = "")
		{
			if (Config.SaveWindowPosition)
			{
				if (WindowState is FormWindowState.Normal)
				{
					Config.MainWindowPosition = Location;
					Config.MainWindowSize = Size;
				}
				Config.MainWindowMaximized = WindowState is FormWindowState.Maximized && !_inFullscreen;
			}
			else
			{
				Config.MainWindowPosition = null;
				Config.MainWindowSize = null;
			}

			Config.LastWrittenFrom = VersionInfo.MainVersion;
			Config.LastWrittenFromDetailed = VersionInfo.GetEmuVersion();

			if (string.IsNullOrEmpty(path))
			{
				path = _getConfigPath();
			}

			CommitCoreSettingsToConfig();
			ConfigService.Save(path, Config);
		}

		private void ToggleFps()
			=> Config.DisplayFps = !Config.DisplayFps;

		private void ToggleFrameCounter()
			=> Config.DisplayFrameCounter = !Config.DisplayFrameCounter;

		private void ToggleLagCounter()
			=> Config.DisplayLagCounter = !Config.DisplayLagCounter;

		private void ToggleInputDisplay()
			=> Config.DisplayInput = !Config.DisplayInput;

		public void ToggleSound()
		{
			Config.SoundEnabled = !Config.SoundEnabled;
			Sound.StopSound();
			Sound.StartSound();
		}

		private void VolumeUp()
		{
			Config.SoundVolume += 10;
			if (Config.SoundVolume > 100)
			{
				Config.SoundVolume = 100;
			}

			AddOnScreenMessage($"Volume {Config.SoundVolume}");
		}

		private void VolumeDown()
		{
			Config.SoundVolume -= 10;
			if (Config.SoundVolume < 0)
			{
				Config.SoundVolume = 0;
			}

			AddOnScreenMessage($"Volume {Config.SoundVolume}");
		}

		private void SoftReset()
		{
			// is it enough to run this for one frame? maybe..
			if (Emulator.ControllerDefinition.BoolButtons.Contains("Reset")
				&& !MovieSession.Movie.IsPlaying())
			{
				InputManager.ClickyVirtualPadController.Click("Reset");
				AddOnScreenMessage("Reset button pressed.");
			}
		}

		private void HardReset()
		{
			// is it enough to run this for one frame? maybe..
			if (Emulator.ControllerDefinition.BoolButtons.Contains("Power")
				&& !MovieSession.Movie.IsPlaying())
			{
				InputManager.ClickyVirtualPadController.Click("Power");
				AddOnScreenMessage("Power button pressed.");
			}
		}

		private Color SlotForeColor(int slot)
		{
			return HasSlot(slot)
				? Config.SaveSlot == slot
					? SystemColors.HighlightText
					: SystemColors.WindowText
				: SystemColors.GrayText;
		}

		private Color SlotBackColor(int slot)
		{
			return  Config.SaveSlot == slot
				? SystemColors.Highlight
				: SystemColors.Control;
		}

		public void UpdateStatusSlots()
		{
			_stateSlots.Update(Emulator, MovieSession.Movie, SaveStatePrefix());

			Slot1StatusButton.ForeColor = SlotForeColor(1);
			Slot2StatusButton.ForeColor = SlotForeColor(2);
			Slot3StatusButton.ForeColor = SlotForeColor(3);
			Slot4StatusButton.ForeColor = SlotForeColor(4);
			Slot5StatusButton.ForeColor = SlotForeColor(5);
			Slot6StatusButton.ForeColor = SlotForeColor(6);
			Slot7StatusButton.ForeColor = SlotForeColor(7);
			Slot8StatusButton.ForeColor = SlotForeColor(8);
			Slot9StatusButton.ForeColor = SlotForeColor(9);
			Slot0StatusButton.ForeColor = SlotForeColor(10);

			Slot1StatusButton.BackColor = SlotBackColor(1);
			Slot2StatusButton.BackColor = SlotBackColor(2);
			Slot3StatusButton.BackColor = SlotBackColor(3);
			Slot4StatusButton.BackColor = SlotBackColor(4);
			Slot5StatusButton.BackColor = SlotBackColor(5);
			Slot6StatusButton.BackColor = SlotBackColor(6);
			Slot7StatusButton.BackColor = SlotBackColor(7);
			Slot8StatusButton.BackColor = SlotBackColor(8);
			Slot9StatusButton.BackColor = SlotBackColor(9);
			Slot0StatusButton.BackColor = SlotBackColor(10);

			SaveSlotsStatusLabel.Visible =
				Slot1StatusButton.Visible =
				Slot2StatusButton.Visible =
				Slot3StatusButton.Visible =
				Slot4StatusButton.Visible =
				Slot5StatusButton.Visible =
				Slot6StatusButton.Visible =
				Slot7StatusButton.Visible =
				Slot8StatusButton.Visible =
				Slot9StatusButton.Visible =
				Slot0StatusButton.Visible =
					Emulator.HasSavestates();
		}

		public BitmapBuffer CaptureOSD()
		{
			var bb = DisplayManager.RenderOffscreen(_currentVideoProvider, true);
			bb.DiscardAlpha();
			return bb;
		}
		public BitmapBuffer CaptureLua()
		{
			var bb = DisplayManager.RenderOffscreenLua(_currentVideoProvider);
			bb.DiscardAlpha();
			return bb;
		}

		private void IncreaseWindowSize()
		{
			var windowScale = Config.GetWindowScaleFor(Emulator.SystemId);
			if (windowScale < WINDOW_SCALE_MAX)
			{
				windowScale++;
				Config.SetWindowScaleFor(Emulator.SystemId, windowScale);
			}
			AddOnScreenMessage($"Screensize set to {windowScale}x");
			FrameBufferResized(forceWindowResize: true);
		}

		private void DecreaseWindowSize()
		{
			var windowScale = Config.GetWindowScaleFor(Emulator.SystemId);
			if (windowScale > 1)
			{
				windowScale--;
				Config.SetWindowScaleFor(Emulator.SystemId, windowScale);
			}
			AddOnScreenMessage($"Screensize set to {windowScale}x");
			FrameBufferResized(forceWindowResize: true);
		}

		private static readonly int[] SpeedPercents = { 1, 3, 6, 12, 25, 50, 75, 100, 150, 200, 300, 400, 800, 1600, 3200, 6400 };

		private bool CheckCanSetSpeed()
		{
			if (Config.ClockThrottle)
				return true;

			AddOnScreenMessage("Unable to change speed, please switch to clock throttle");
			return false;
		}

		private void ResetSpeed()
		{
			if (!CheckCanSetSpeed())
				return;

			SetSpeedPercent(100);
		}

		private void IncreaseSpeed()
		{
			if (!CheckCanSetSpeed())
				return;

			var oldPercent = Config.SpeedPercent;
			int newPercent;

			int i = 0;
			do
			{
				i++;
				newPercent = SpeedPercents[i];
			}
			while (newPercent <= oldPercent && i < SpeedPercents.Length - 1);

			SetSpeedPercent(newPercent);
		}

		private void DecreaseSpeed()
		{
			if (!CheckCanSetSpeed())
				return;

			var oldPercent = Config.SpeedPercent;
			int newPercent;

			int i = SpeedPercents.Length - 1;
			do
			{
				i--;
				newPercent = SpeedPercents[i];
			}
			while (newPercent >= oldPercent && i > 0);

			SetSpeedPercent(newPercent);
		}

		private void SaveMovie()
		{
			if (MovieSession.Movie.IsActive())
			{
				MovieSession.Movie.Save();
				AddOnScreenMessage($"{MovieSession.Movie.Filename} saved.");
			}
		}

		private void HandleToggleLightAndLink()
		{
			if (!MainStatusBar.Visible) return;

			if (Emulator.HasDriveLight() && Emulator.AsDriveLight() is { DriveLightEnabled: true } diskLEDCore)
			{
				LedLightStatusLabel.Image = diskLEDCore.DriveLightOn ? _statusBarDiskLightOnImage : _statusBarDiskLightOffImage;
				LedLightStatusLabel.ToolTipText = Emulator.AsDriveLight().DriveLightIconDescription;
				LedLightStatusLabel.Visible = true;
			}
			else
			{
				LedLightStatusLabel.Visible = false;
			}

			if (Emulator.UsesLinkCable())
			{
				var linkableCore = Emulator.AsLinkable();
				LinkConnectStatusBarButton.Image = linkableCore.LinkConnected ? _linkCableOn : _linkCableOff;
				LinkConnectStatusBarButton.ToolTipText = $"Link connection is currently {(linkableCore.LinkConnected ? "enabled" : "disabled")}";
				LinkConnectStatusBarButton.Visible = true;
			}
			else
			{
				LinkConnectStatusBarButton.Visible = false;
			}
		}

		private void UpdateKeyPriorityIcon()
		{
			switch (Config.InputHotkeyOverrideOptions)
			{
				default:
				case 0:
					KeyPriorityStatusLabel.Image = Properties.Resources.Both;
					KeyPriorityStatusLabel.ToolTipText = "Key priority: Allow both hotkeys and controller buttons";
					break;
				case 1:
					KeyPriorityStatusLabel.Image = Properties.Resources.GameController;
					KeyPriorityStatusLabel.ToolTipText = "Key priority: Controller buttons will override hotkeys";
					break;
				case 2:
					KeyPriorityStatusLabel.Image = Properties.Resources.HotKeys;
					KeyPriorityStatusLabel.ToolTipText = "Key priority: Hotkeys will override controller buttons";
					break;
			}
		}

		private void ToggleBackgroundInput()
		{
			Config.AcceptBackgroundInput = !Config.AcceptBackgroundInput;
			AddOnScreenMessage($"Background Input {(Config.AcceptBackgroundInput ? "enabled" : "disabled")}");
		}

		private void VsyncMessage()
		{
			AddOnScreenMessage($"Display Vsync set to {(Config.VSync ? "on" : "off")}");
		}

		private void FdsInsertDiskMenuAdd(string name, string button, string msg)
		{
			FDSControlsMenuItem.DropDownItems.Add(name, null, (sender, e) =>
			{
				if (Emulator.ControllerDefinition.BoolButtons.Contains(button)
					&& !MovieSession.Movie.IsPlaying())
				{
					InputManager.ClickyVirtualPadController.Click(button);
					AddOnScreenMessage(msg);
				}
			});
		}

		private const int WmDeviceChange = 0x0219;

		// Alt key hacks
		protected override void WndProc(ref Message m)
		{
			if (m.Msg == WmDeviceChange) Input.Instance.Adapter.ReInitGamepads(Handle);

			// this is necessary to trap plain alt keypresses so that only our hotkey system gets them
			if (m.Msg == 0x0112) // WM_SYSCOMMAND
			{
				if (m.WParam.ToInt32() == 0xF100) // SC_KEYMENU
				{
					return;
				}
			}

			base.WndProc(ref m);
		}

		protected override bool ProcessDialogChar(char charCode)
		{
			// this is necessary to trap alt+char combinations so that only our hotkey system gets them
			return (ModifierKeys & Keys.Alt) != 0 || base.ProcessDialogChar(charCode);
		}

		private void UpdateCoreStatusBarButton()
		{
			var attributes = Emulator.Attributes();
			var coreDispName = attributes.Released ? attributes.CoreName : $"(Experimental) {attributes.CoreName}";
			LoadedCoreNameMenuItem.Text = $"Loaded core: {coreDispName} ({Emulator.SystemId})";
			if (Emulator.IsNull())
			{
				CoreNameStatusBarButton.Visible = false;
				return;
			}

			CoreNameStatusBarButton.Visible = true;

			CoreNameStatusBarButton.Text = coreDispName;
			CoreNameStatusBarButton.Image = Emulator.Icon();
			CoreNameStatusBarButton.ToolTipText = attributes is PortedCoreAttribute ? "(ported) " : "";


			if (Emulator.SystemId == VSystemID.Raw.ZXSpectrum)
			{
				var core = (Emulation.Cores.Computers.SinclairSpectrum.ZXSpectrum)Emulator;
				CoreNameStatusBarButton.ToolTipText = core.GetMachineType();
			}

			if (Emulator.SystemId == VSystemID.Raw.AmstradCPC)
			{
				var core = (Emulation.Cores.Computers.AmstradCPC.AmstradCPC)Emulator;
				CoreNameStatusBarButton.ToolTipText = core.GetMachineType();
			}
		}

		private void ToggleKeyPriority()
		{
			Config.InputHotkeyOverrideOptions++;
			if (Config.InputHotkeyOverrideOptions > 2)
			{
				Config.InputHotkeyOverrideOptions = 0;
			}

			if (Config.NoMixedInputHokeyOverride && Config.InputHotkeyOverrideOptions == 0)
			{
				Config.InputHotkeyOverrideOptions = 1;
			}

			UpdateKeyPriorityIcon();
			switch (Config.InputHotkeyOverrideOptions)
			{
				case 0:
					AddOnScreenMessage("Key priority set to Both Hotkey and Input");
					break;
				case 1:
					AddOnScreenMessage("Key priority set to Input over Hotkey");
					break;
				case 2:
					AddOnScreenMessage("Key priority set to Input");
					break;
			}
		}

		private void LoadConfigFile(string iniPath)
		{
			LoadGlobalConfigFromFile(iniPath);
			InitControls(); // rebind hotkeys
			InputManager.SyncControls(Emulator, MovieSession, Config);
			Tools.Restart(Config, Emulator, Game);
			ExtToolManager.Restart(Config);
			Sound.Config = Config;
			DisplayManager.UpdateGlobals(Config, Emulator);
			RA?.Restart();
			AddOnScreenMessage($"Config file loaded: {iniPath}");
		}

		/*internal*/public void StepRunLoop_Throttle()
		{
			SyncThrottle();
			_throttle.signal_frameAdvance = _runloopFrameAdvance;
			_throttle.signal_continuousFrameAdvancing = _runloopFrameProgress;

			_throttle.Step(Config, Sound, allowSleep: true, forceFrameSkip: -1);
		}

		public void FrameAdvance(bool discardApiHawkSurfaces)
		{
			PressFrameAdvance = true;
			StepRunLoop_Core(true);
			if (discardApiHawkSurfaces)
			{
				DisplayManager.DiscardApiHawkSurfaces();
			}
		}

		public void SeekFrameAdvance()
		{
			PressFrameAdvance = true;
			StepRunLoop_Core(true);
			DisplayManager.DiscardApiHawkSurfaces();
			PressFrameAdvance = false;
		}

		private void StepRunLoop_Core(bool force = false)
		{
			var runFrame = false;
			_runloopFrameAdvance = false;
			var currentTimestamp = Stopwatch.GetTimestamp();

			double frameAdvanceTimestampDeltaMs = (double)(currentTimestamp - _frameAdvanceTimestamp) / Stopwatch.Frequency * 1000.0;
			bool frameProgressTimeElapsed = frameAdvanceTimestampDeltaMs >= Config.FrameProgressDelayMs;

			if (Config.SkipLagFrame && Emulator.CanPollInput() && Emulator.AsInputPollable().IsLagFrame && frameProgressTimeElapsed && Emulator.Frame > 0)
			{
				runFrame = true;
			}

			RA?.Update();

			bool oldFrameAdvanceCondition = InputManager.ClientControls["Frame Advance"] || PressFrameAdvance || HoldFrameAdvance;
			if (FrameInch)
			{
				FrameInch = false;
				if (EmulatorPaused || oldFrameAdvanceCondition)
				{
					oldFrameAdvanceCondition = true;
				}
				else
				{
					PauseEmulator();
					oldFrameAdvanceCondition = false;
				}
			}

			if (oldFrameAdvanceCondition || FrameInch)
			{
				FrameInch = false;
				_runloopFrameAdvance = true;

				// handle the initial trigger of a frame advance
				if (_frameAdvanceTimestamp == 0)
				{
					PauseEmulator();
					runFrame = true;
					_frameAdvanceTimestamp = currentTimestamp;
				}
				else
				{
					// handle the timed transition from countdown to FrameProgress
					if (frameProgressTimeElapsed)
					{
						runFrame = true;
						_runloopFrameProgress = true;
						UnpauseEmulator();
					}
				}
			}
			else
			{
				// handle release of frame advance: do we need to deactivate FrameProgress?
				if (_runloopFrameProgress)
				{
					_runloopFrameProgress = false;
					PauseEmulator();
				}

				_frameAdvanceTimestamp = 0;
			}

			if (!EmulatorPaused)
			{
				runFrame = true;
			}

			bool isRewinding = Rewind(ref runFrame, currentTimestamp, out var returnToRecording);

			float atten = 0;

			// BlockFrameAdvance (true when input it being editted in TAStudio) supercedes all other frame advance conditions
			if ((runFrame || force) && !BlockFrameAdvance)
			{
				var isFastForwarding = InputManager.ClientControls["Fast Forward"] || IsTurboing || InvisibleEmulation;
				var isFastForwardingOrRewinding = isFastForwarding || isRewinding || Config.Unthrottled;

				if (isFastForwardingOrRewinding != _lastFastForwardingOrRewinding)
				{
					InitializeFpsData();
				}

				_lastFastForwardingOrRewinding = isFastForwardingOrRewinding;

				// client input-related duties
				OSD.ClearGuiText();

				CheatList.Pulse();

				// zero 03-may-2014 - moved this before call to UpdateToolsBefore(), since it seems to clear the state which a lua event.framestart is going to want to alter
				InputManager.ClickyVirtualPadController.FrameTick();
				InputManager.ButtonOverrideAdapter.FrameTick();

				if (IsTurboing)
				{
					Tools.FastUpdateBefore();
				}
				else
				{
					Tools.UpdateToolsBefore();
				}

				if (!InvisibleEmulation)
				{
					CaptureRewind(isRewinding);
				}

				// Set volume, if enabled
				if (Config.SoundEnabledNormal && !InvisibleEmulation)
				{
					atten = Config.SoundVolume / 100.0f;

					if (isFastForwardingOrRewinding)
					{
						if (Config.SoundEnabledRWFF)
						{
							atten *= Config.SoundVolumeRWFF / 100.0f;
						}
						else
						{
							atten = 0;
						}
					}

					// Mute if using Frame Advance/Frame Progress
					if (_runloopFrameAdvance && Config.MuteFrameAdvance)
					{
						atten = 0;
					}
				}

				MovieSession.HandleFrameBefore();

				RA?.OnFrameAdvance();

				if (Config.AutosaveSaveRAM)
				{
					if (AutoFlushSaveRamIn-- <= 0)
					{
						FlushSaveRAM(true);
					}
				}
				// why not skip audio if the user doesn't want sound
				bool renderSound = (Config.SoundEnabled && !IsTurboing)
					|| (_currAviWriter?.UsesAudio ?? false);
				if (!renderSound)
				{
					atten = 0;
				}

				bool render = !InvisibleEmulation && (!_throttle.skipNextFrame || (_currAviWriter?.UsesVideo ?? false));
				bool newFrame = Emulator.FrameAdvance(InputManager.ControllerOutput, render, renderSound);

				MovieSession.HandleFrameAfter();

				if (returnToRecording)
				{
					MovieSession.Movie.SwitchToRecord();
				}

				if (isRewinding && ToolControllingRewind is null && MovieSession.Movie.IsRecording())
				{
					MovieSession.Movie.Truncate(Emulator.Frame);
				}

				CheatList.Pulse();

				if (Emulator.CanPollInput() && Emulator.AsInputPollable().IsLagFrame && Config.AutofireLagFrames)
				{
					InputManager.AutoFireController.IncrementStarts();
				}

				InputManager.AutofireStickyXorAdapter.IncrementLoops(Emulator.CanPollInput() && Emulator.AsInputPollable().IsLagFrame);

				PressFrameAdvance = false;

				// Update tools, but not if we're at the end of a turbo seek. In that case, updating will happen later when the seek is ended.
				if (!(IsTurboSeeking && Emulator.Frame == PauseOnFrame.Value))
				{
					if (IsTurboing)
					{
						Tools.FastUpdateAfter();
					}
					else
					{
						UpdateToolsAfter();
					}
				}

				if (!PauseAvi && newFrame && !InvisibleEmulation)
				{
					AvFrameAdvance();
				}

				if (newFrame)
				{
					_framesSinceLastFpsUpdate++;

					CalcFramerateAndUpdateDisplay(currentTimestamp, isRewinding, isFastForwarding);
				}

				if (IsSeeking && PauseOnFrame.Value <= Emulator.Frame)
				{
					if (PauseOnFrame.Value == Emulator.Frame)
					{
						PauseEmulator();
						if (Tools.IsLoaded<TAStudio>()) Tools.TAStudio.StopSeeking();
						else PauseOnFrame = null;
					}
					else if (Tools.IsLoaded<TAStudio>()
						&& Tools.TAStudio.LastPositionFrame == Emulator.Frame
						&& ((ITasMovie) MovieSession.Movie)[Emulator.Frame].Lagged is null)
					{
						// haven't yet greenzoned the frame, hence it's after editing
						// then we want to pause here. taseditor fashion
						PauseEmulator();
					}
				}
			}
			else if (isRewinding)
			{
				// Tools will want to be updated after rewind (load state), but we only need to manually do this if we did not frame advance.
				UpdateToolsAfter();
			}

			Sound.UpdateSound(atten, DisableSecondaryThrottling);
		}

		private void CalcFramerateAndUpdateDisplay(long currentTimestamp, bool isRewinding, bool isFastForwarding)
		{
			double elapsedSeconds = (currentTimestamp - _timestampLastFpsUpdate) / (double)Stopwatch.Frequency;

			if (elapsedSeconds < 1.0 / _fpsUpdatesPerSecond)
			{
				return;
			}

			if (_lastFps == 0) // Initial calculation
			{
				_lastFps = (_framesSinceLastFpsUpdate - 1) / elapsedSeconds;
			}
			else
			{
				_lastFps = (_lastFps + (_framesSinceLastFpsUpdate * _fpsSmoothing)) / (1.0 + (elapsedSeconds * _fpsSmoothing));
			}
			_lastFpsRounded = (int) Math.Round(_lastFps);

			_framesSinceLastFpsUpdate = 0;
			_timestampLastFpsUpdate = currentTimestamp;

			var fpsString = $"{_lastFpsRounded} fps";
			if (isRewinding)
			{
				fpsString += IsTurboing || isFastForwarding ?
					" <<<<" :
					" <<";
			}
			else if (isFastForwarding)
			{
				fpsString += IsTurboing ?
					" >>>>" :
					" >>";
			}

			OSD.Fps = fpsString;

			// need to refresh window caption in this case
			if (Config.DispSpeedupFeatures is 0) UpdateWindowTitle();
		}

		private void InitializeFpsData()
		{
			_lastFps = _lastFpsRounded = 0;
			_timestampLastFpsUpdate = Stopwatch.GetTimestamp();
			_framesSinceLastFpsUpdate = 0;
		}

		/// <summary>
		/// start AVI recording, unattended
		/// </summary>
		/// <param name="videoWriterName">match the short name of an <see cref="IVideoWriter"/></param>
		/// <param name="filename">filename to save to</param>
		private void RecordAv(string videoWriterName, string filename)
		{
			RecordAvBase(videoWriterName, filename, true);
		}

		/// <summary>
		/// start AV recording, asking user for filename and options
		/// </summary>
		private void RecordAv()
		{
			RecordAvBase(null, null, false);
		}

		/// <summary>
		/// start AV recording
		/// </summary>
		private void RecordAvBase(string videoWriterName, string filename, bool unattended)
		{
			if (_currAviWriter != null) return;

			if (Game.IsNullInstance()) throw new InvalidOperationException("how is an A/V recording starting with no game loaded? please report this including as much detail as possible");

			// select IVideoWriter to use
			IVideoWriter aw;

			if (string.IsNullOrEmpty(videoWriterName) && !string.IsNullOrEmpty(Config.VideoWriter))
			{
				videoWriterName = Config.VideoWriter;
			}

			if (unattended && !string.IsNullOrEmpty(videoWriterName))
			{
				aw = VideoWriterInventory.GetVideoWriter(videoWriterName, this);
			}
			else
			{
				aw = VideoWriterChooserForm.DoVideoWriterChooserDlg(
					VideoWriterInventory.GetAllWriters(),
					this,
					Emulator,
					Config);
			}

			if (aw == null)
			{
				AddOnScreenMessage(
					unattended ? $"Couldn't start video writer \"{videoWriterName}\"" : "A/V capture canceled.");

				return;
			}

			try
			{
#if AVI_SUPPORT
				bool usingAvi = aw is AviWriter; // SO GROSS!
#else
				const bool usingAvi = false;
#endif

				aw = Config.VideoWriterAudioSyncEffective ? new VideoStretcher(aw) : new AudioStretcher(aw);
				aw.SetMovieParameters(Emulator.VsyncNumerator(), Emulator.VsyncDenominator());
				if (Config.AVWriterResizeWidth > 0 && Config.AVWriterResizeHeight > 0)
				{
					aw.SetVideoParameters(Config.AVWriterResizeWidth, Config.AVWriterResizeHeight);
				}
				else
				{
					aw.SetVideoParameters(_currentVideoProvider.BufferWidth, _currentVideoProvider.BufferHeight);
				}

				aw.SetAudioParameters(44100, 2, 16);

				// select codec token
				// do this before save dialog because ffmpeg won't know what extension it wants until it's been configured
				if (unattended && !string.IsNullOrEmpty(filename))
				{
					aw.SetDefaultVideoCodecToken(Config);
				}
				else
				{
					// THIS IS REALLY SLOPPY!
					// PLEASE REDO ME TO NOT CARE WHICH AVWRITER IS USED!
					if (usingAvi && !string.IsNullOrEmpty(Config.AviCodecToken))
					{
						aw.SetDefaultVideoCodecToken(Config);
					}

					var token = aw.AcquireVideoCodecToken(Config);
					if (token == null)
					{
						AddOnScreenMessage("A/V capture canceled.");
						aw.Dispose();
						return;
					}

					aw.SetVideoCodecToken(token);
				}

				// select file to save to
				if (unattended && !string.IsNullOrEmpty(filename))
				{
					aw.OpenFile(filename);
				}
				else
				{
					string ext = aw.DesiredExtension();
					string pathForOpenFile;

					// handle directories first
					if (ext == "<directory>")
					{
						using var fbd = new FolderBrowserEx();
						if (this.ShowDialogWithTempMute(fbd) is DialogResult.Cancel)
						{
							aw.Dispose();
							return;
						}

						pathForOpenFile = fbd.SelectedPath;
					}
					else
					{
						var result = this.ShowFileSaveDialog(
							filter: new(new FilesystemFilter(ext, new[] { ext })),
							initDir: Config.PathEntries.AvAbsolutePath(),
							initFileName: $"{(MovieSession.Movie.IsActive() ? Path.GetFileNameWithoutExtension(MovieSession.Movie.Filename) : Game.FilesystemSafeName())}.{ext}");
						if (result is null)
						{
							aw.Dispose();
							return;
						}
						pathForOpenFile = result;
					}

					aw.OpenFile(pathForOpenFile);
				}

				// commit the avi writing last, in case there were any errors earlier
				_currAviWriter = aw;
				AddOnScreenMessage("A/V capture started");
				AVIStatusLabel.Image = Properties.Resources.Avi;
				AVIStatusLabel.ToolTipText = "A/V capture in progress";
				AVIStatusLabel.Visible = true;
			}
			catch
			{
				AddOnScreenMessage("A/V capture failed!");
				aw.Dispose();
				throw;
			}

			if (Config.VideoWriterAudioSyncEffective)
			{
				_currentSoundProvider.SetSyncMode(SyncSoundMode.Sync);
			}
			else
			{
				if (_currentSoundProvider.CanProvideAsync)
				{
					_currentSoundProvider.SetSyncMode(SyncSoundMode.Async);
					_aviSoundInputAsync = _currentSoundProvider;
				}
				else
				{
					_currentSoundProvider.SetSyncMode(SyncSoundMode.Sync);
					_aviSoundInputAsync = new SyncToAsyncProvider(() => Emulator.VsyncRate(), _currentSoundProvider);
				}
			}

			_dumpProxy = new SimpleSyncSoundProvider();
			RewireSound();
		}

		private void AbortAv()
		{
			if (_currAviWriter == null)
			{
				_dumpProxy = null;
				RewireSound();
				return;
			}

			_currAviWriter.Dispose();
			_currAviWriter = null;
			AddOnScreenMessage("A/V capture aborted");
			AVIStatusLabel.Image = Properties.Resources.Blank;
			AVIStatusLabel.ToolTipText = "";
			AVIStatusLabel.Visible = false;
			_aviSoundInputAsync = null;
			_dumpProxy = null; // return to normal sound output
			RewireSound();
		}

		private void StopAv()
		{
			if (_currAviWriter == null)
			{
				_dumpProxy = null;
				RewireSound();
				return;
			}

			_currAviWriter.CloseFile();
			_currAviWriter.Dispose();
			_currAviWriter = null;
			AddOnScreenMessage("A/V capture stopped");
			AVIStatusLabel.Image = Properties.Resources.Blank;
			AVIStatusLabel.ToolTipText = "";
			AVIStatusLabel.Visible = false;
			_aviSoundInputAsync = null;
			_dumpProxy = null; // return to normal sound output
			RewireSound();
		}

		private void AvFrameAdvance()
		{
			if (_currAviWriter == null) return;

			// is this the best time to handle this? or deeper inside?
			if (_argParser._currAviWriterFrameList?.Contains(Emulator.Frame) != false)
			{
				// TODO ZERO - this code is pretty jacked. we'll want to frugalize buffers better for speedier dumping, and we might want to rely on the GL layer for padding
				try
				{
					IVideoProvider output;
					IDisposable disposableOutput = null;
					if (Config.AVWriterResizeWidth > 0 && Config.AVWriterResizeHeight > 0)
					{
						BitmapBuffer bbIn = null;
						Bitmap bmpIn = null;
						try
						{
							bbIn = Config.AviCaptureOsd
								? CaptureOSD()
								: new BitmapBuffer(_currentVideoProvider.BufferWidth, _currentVideoProvider.BufferHeight, _currentVideoProvider.GetVideoBuffer());

							bbIn.DiscardAlpha();

							Bitmap bmpOut = new(width: Config.AVWriterResizeWidth, height: Config.AVWriterResizeHeight, PixelFormat.Format32bppArgb);
							bmpIn = bbIn.ToSysdrawingBitmap();
							using (var g = Graphics.FromImage(bmpOut))
							{
								if (Config.AVWriterPad)
								{
									g.Clear(Color.FromArgb(_currentVideoProvider.BackgroundColor));
									g.DrawImageUnscaled(bmpIn, (bmpOut.Width - bmpIn.Width) / 2, (bmpOut.Height - bmpIn.Height) / 2);
								}
								else
								{
									g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
									g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
									g.DrawImage(bmpIn, new Rectangle(0, 0, bmpOut.Width, bmpOut.Height));
								}
							}

							output = new BmpVideoProvider(bmpOut, _currentVideoProvider.VsyncNumerator, _currentVideoProvider.VsyncDenominator);
							disposableOutput = (IDisposable) output;
						}
						finally
						{
							bbIn?.Dispose();
							bmpIn?.Dispose();
						}
					}
					else
					{
						if (Config.AviCaptureOsd)
						{
							output = new BitmapBufferVideoProvider(CaptureOSD());
							disposableOutput = (IDisposable) output;
						}
						else if (Config.AviCaptureLua)
						{
							output = new BitmapBufferVideoProvider(CaptureLua());
							disposableOutput = (IDisposable) output;
						}
						else
						{
							output = _currentVideoProvider;
						}
					}

					_currAviWriter.SetFrame(Emulator.Frame);

					short[] samp;
					int nsamp;
					if (Config.VideoWriterAudioSyncEffective)
					{
						((VideoStretcher) _currAviWriter).DumpAV(output, _currentSoundProvider, out samp, out nsamp);
					}
					else
					{
						((AudioStretcher) _currAviWriter).DumpAV(output, _aviSoundInputAsync, out samp, out nsamp);
					}

					disposableOutput?.Dispose();

					_dumpProxy.PutSamples(samp, nsamp);
				}
				catch (Exception e)
				{
					ShowMessageBox(owner: null, $"Video dumping died:\n\n{e}");
					AbortAv();
				}
			}

			if (_autoDumpLength > 0) //TODO this is probably not necessary because of the call to StopAv --yoshi
			{
				_autoDumpLength--;
				if (_autoDumpLength == 0) // finish
				{
					StopAv();
					if (_argParser._autoCloseOnDump) ScheduleShutdown();
				}
			}
		}

		private int? LoadArchiveChooser(HawkFile file)
		{
			using var ac = new ArchiveChooser(file);
			if (this.ShowDialogAsChild(ac).IsOk())
			{
				return ac.SelectedMemberIndex;
			}

			return null;
		}

		public string SaveStatePrefix()
		{
			var name = Game.FilesystemSafeName();
			name += $".{Emulator.Attributes().CoreName}";

			// Bsnes profiles have incompatible savestates so save the profile name
			if (Emulator is LibsnesCore bsnes)
			{
				name += $".{bsnes.CurrentProfile}";
			}

			if (MovieSession.Movie.IsActive())
			{
				name += $".{Path.GetFileNameWithoutExtension(MovieSession.Movie.Filename)}";
			}

			var pathEntry = Config.PathEntries.SaveStateAbsolutePath(Game.System);

			return Path.Combine(pathEntry, name);
		}

		private void ShowLoadError(object sender, RomLoader.RomErrorArgs e)
		{
			if (e.Type == RomLoader.LoadErrorType.MissingFirmware)
			{
				if (this.ShowMessageBox2(
					caption: "Missing Firmware!",
					icon: EMsgBoxIcon.Error,
					text: $"{e.Message}\n\nOpen the firmware manager now?",
					useOKCancel: true))
				{
					OpenFWConfigRomLoadFailed(e);
					if (e.Retry)
					{
						// Retry loading the ROM here. This leads to recursion, as the original call to LoadRom has not exited yet,
						// but unless the user tries and fails to set his firmware a lot of times, nothing should happen.
						// Refer to how RomLoader implemented its LoadRom method for a potential fix on this.
						_ = LoadRom(e.RomPath, _currentLoadRomArgs);
					}
				}
			}
			else
			{
				string title = "load error";
				if (e.AttemptedCoreLoad != null)
				{
					title = $"{e.AttemptedCoreLoad} load error";
				}

				this.ModalMessageBox(e.Message, title, EMsgBoxIcon.Error);
			}
		}

		private string ChoosePlatformForRom(RomGame rom)
		{
			using var platformChooser = new PlatformChooser(Config)
			{
				RomGame = rom
			};
			this.ShowDialogWithTempMute(platformChooser);
			return platformChooser.PlatformChoice;
		}

		private LoadRomArgs _currentLoadRomArgs;
		private bool _isLoadingRom;

		public bool LoadRom(string path, LoadRomArgs args) => LoadRom(path, args, out _);

		public bool LoadRom(string path, LoadRomArgs args, out bool failureIsFromAskSave)
		{
			if (!LoadRomInternal(path, args, out failureIsFromAskSave))
				return false;

			// what's the meaning of the last rom path when opening an archive? based on the archive file location
			if (args.OpenAdvanced is OpenAdvanced_OpenRom)
			{
				var leftPart = path.Split('|')[0];
				Config.PathEntries.LastRomPath = Path.GetFullPath(Path.GetDirectoryName(leftPart) ?? "");
			}

			return true;
		}

		// Still needs a good bit of refactoring
		private bool LoadRomInternal(string path, LoadRomArgs args, out bool failureIsFromAskSave)
		{
			failureIsFromAskSave = false;
			if (path == null)
				throw new ArgumentNullException(nameof(path));
			if (args == null)
				throw new ArgumentNullException(nameof(args));

			_isLoadingRom = true;
			path = EmuHawkUtil.ResolveShortcut(path);

			// if this is the first call to LoadRom (they will come in recursively) then stash the args
			bool firstCall = false;
			if (_currentLoadRomArgs == null)
			{
				firstCall = true;
				_currentLoadRomArgs = args;
			}
			else
			{
				args = _currentLoadRomArgs;
			}

			try
			{
				// movies should require deterministic emulation in ALL cases
				// if the core is managing its own DE through SyncSettings a 'deterministic' bool can be passed into the core's constructor
				// it is then up to the core itself to override its own local DeterministicEmulation setting
				bool deterministic = args.Deterministic ?? MovieSession.NewMovieQueued;

				if (!Tools.AskSave())
				{
					failureIsFromAskSave = true;
					return false;
				}

				var loader = new RomLoader(Config)
				{
					ChooseArchive = LoadArchiveChooser,
					ChoosePlatform = ChoosePlatformForRom,
					Deterministic = deterministic,
					MessageCallback = AddOnScreenMessage,
					OpenAdvanced = args.OpenAdvanced
				};
				FirmwareManager.RecentlyServed.Clear();

				loader.OnLoadError += ShowLoadError;
				loader.OnLoadSettings += CoreSettings;
				loader.OnLoadSyncSettings += CoreSyncSettings;

				// this also happens in CloseGame(). But it needs to happen here since if we're restarting with the same core,
				// any settings changes that we made need to make it back to config before we try to instantiate that core with
				// the new settings objects
				CommitCoreSettingsToConfig(); // adelikat: I Think by reordering things, this isn't necessary anymore
				CloseGame();

				var nextComm = CreateCoreComm();

				IOpenAdvanced ioa = args.OpenAdvanced;
				var oaOpenrom = ioa as OpenAdvanced_OpenRom;
				var ioaRetro = ioa as IOpenAdvancedLibretro;

				// we need to inform LoadRom which Libretro core to use...
				if (ioaRetro != null)
				{
					// prepare a core specification
					// if it wasn't already specified, use the current default
					if (ioaRetro.CorePath == null)
					{
						ioaRetro.CorePath = Config.LibretroCore;
					}

					if (ioaRetro.CorePath == null)
					{
						throw new InvalidOperationException("Can't load a file via Libretro until a core is specified");
					}
				}

				if (oaOpenrom != null)
				{
					// path already has the right value, while ioa.Path is null (interestingly, these are swapped below)
					// I doubt null is meant to be assigned here, and it just prevents game load
					//path = ioa_openrom.Path;
				}

				DisplayManager.ActivateOpenGLContext(); // required in case the core wants to create a shared OpenGL context

				bool result = string.IsNullOrEmpty(MovieSession.QueuedCoreName)
					? loader.LoadRom(path, nextComm, ioaRetro?.CorePath)
					: loader.LoadRom(path, nextComm, ioaRetro?.CorePath, forcedCoreName: MovieSession.QueuedCoreName);

				Game = result ? loader.Game : GameInfo.NullInstance;

				// we need to replace the path in the OpenAdvanced with the canonical one the user chose.
				// It can't be done until loader.LoadRom happens (for CanonicalFullPath)
				// i'm not sure this needs to be more abstractly engineered yet until we have more OpenAdvanced examples
				if (ioa is OpenAdvanced_Libretro oaRetro)
				{
					oaRetro.token.Path = loader.CanonicalFullPath;
				}

				if (oaOpenrom != null)
				{
					oaOpenrom.Path = loader.CanonicalFullPath;
				}

				if (ioa is OpenAdvanced_MAME oaMame)
				{
					oaMame.Path = loader.CanonicalFullPath;
				}

				if (result)
				{
					string openAdvancedArgs = $"*{OpenAdvancedSerializer.Serialize(ioa)}";
					Emulator.Dispose();
					Emulator = loader.LoadedEmulator;
					Config.RecentCores.Enqueue(Emulator.Attributes().CoreName);
					while (Config.RecentCores.Count > 5) Config.RecentCores.Dequeue();
					InputManager.SyncControls(Emulator, MovieSession, Config);
					_multiDiskMode = false;

					if (oaOpenrom != null && Path.GetExtension(oaOpenrom.Path.Replace("|", "")).ToLowerInvariant() == ".xml" && Emulator is not LibsnesCore)
					{
						// this is a multi-disk bundler file
						// determine the xml assets and create RomStatusDetails for all of them
						var xmlGame = XmlGame.Create(new HawkFile(oaOpenrom.Path));

						using var xSw = new StringWriter();

						for (int xg = 0; xg < xmlGame.Assets.Count; xg++)
						{
							var ext = Path.GetExtension(xmlGame.AssetFullPaths[xg])?.ToLowerInvariant();

							var (filename, data) = xmlGame.Assets[xg];
							if (Disc.IsValidExtension(ext))
							{
								xSw.WriteLine(Path.GetFileNameWithoutExtension(filename));
								xSw.WriteLine("SHA1:N/A");
								xSw.WriteLine("MD5:N/A");
								xSw.WriteLine();
							}
							else
							{
								xSw.WriteLine(filename);
								xSw.WriteLine(SHA1Checksum.ComputePrefixedHex(data));
								xSw.WriteLine(MD5Checksum.ComputePrefixedHex(data));
								xSw.WriteLine();
							}
						}

						_defaultRomDetails = xSw.ToString();
						_multiDiskMode = true;
					}

					if (loader.LoadedEmulator is NES nes)
					{
						if (!string.IsNullOrWhiteSpace(nes.GameName))
						{
							Game.Name = nes.GameName;
						}

						Game.Status = nes.RomStatus;
					}
					else if (loader.LoadedEmulator is QuickNES qns)
					{
						if (!string.IsNullOrWhiteSpace(qns.BootGodName))
						{
							Game.Name = qns.BootGodName;
						}

						if (qns.BootGodStatus.HasValue)
						{
							Game.Status = qns.BootGodStatus.Value;
						}
					}

					var romDetails = Emulator.RomDetails();
					if (string.IsNullOrWhiteSpace(romDetails) && loader.Rom != null)
					{
						_defaultRomDetails = $"{loader.Game.Name}\r\n{SHA1Checksum.ComputePrefixedHex(loader.Rom.RomData)}\r\n{MD5Checksum.ComputePrefixedHex(loader.Rom.RomData)}\r\n";
					}
					else if (string.IsNullOrWhiteSpace(romDetails) && loader.Rom == null)
					{
						// single disc game
						_defaultRomDetails = $"{loader.Game.Name}\r\nSHA1:N/A\r\nMD5:N/A\r\n";
					}

					if (Emulator.HasBoardInfo())
					{
						Console.WriteLine("Core reported BoardID: \"{0}\"", Emulator.AsBoardInfo().BoardName);
					}

					Config.RecentRoms.Add(openAdvancedArgs);
					JumpLists.AddRecentItem(openAdvancedArgs, ioa.DisplayName);

					// Don't load Save Ram if a movie is being loaded
					if (!MovieSession.NewMovieQueued)
					{
						if (File.Exists(Config.PathEntries.SaveRamAbsolutePath(loader.Game, MovieSession.Movie)))
						{
							LoadSaveRam();
						}
						else if (Config.AutosaveSaveRAM && File.Exists(Config.PathEntries.SaveRamAbsolutePath(loader.Game, MovieSession.Movie)))
						{
							AddOnScreenMessage("AutoSaveRAM found, but SaveRAM was not saved");
						}
					}

					var previousRom = CurrentlyOpenRom;
					CurrentlyOpenRom = oaOpenrom?.Path ?? openAdvancedArgs;
					CurrentlyOpenRomArgs = args;

					Tools.Restart(Config, Emulator, Game);

					if (previousRom != CurrentlyOpenRom)
					{
						CheatList.NewList(Tools.GenerateDefaultCheatFilename(), autosave: true);
						if (Config.Cheats.LoadFileByGame && Emulator.HasMemoryDomains())
						{
							if (CheatList.AttemptToLoadCheatFile(Emulator.AsMemoryDomains()))
							{
								AddOnScreenMessage("Cheats file loaded");
							}
						}
					}
					else
					{
						if (Emulator.HasMemoryDomains())
						{
							CheatList.UpdateDomains(Emulator.AsMemoryDomains());
						}
						else
						{
							CheatList.NewList(Tools.GenerateDefaultCheatFilename(), autosave: true);
						}
					}

					OnRomChanged();
					DisplayManager.UpdateGlobals(Config, Emulator);
					DisplayManager.Blank();
					CreateRewinder();

					InputManager.StickyXorAdapter.ClearStickies();
					InputManager.StickyXorAdapter.ClearStickyAxes();
					InputManager.AutofireStickyXorAdapter.ClearStickies();

					RewireSound();
					Tools.UpdateCheatRelatedTools(null, null);
					if (!MovieSession.NewMovieQueued && Config.AutoLoadLastSaveSlot && HasSlot(Config.SaveSlot))
					{
						_ = LoadstateCurrentSlot();
					}

					if (FirmwareManager.RecentlyServed.Count > 0)
					{
						Console.WriteLine("Active Firmwares:");
						foreach (var f in FirmwareManager.RecentlyServed)
						{
							Console.WriteLine($"  {f.ID} : {f.Hash}");
						}
					}

					ExtToolManager.BuildToolStrip();

					RomLoaded?.Invoke(this, EventArgs.Empty);
					return true;
				}
				else if (Emulator.IsNull())
				{
					// This shows up if there's a problem
					Tools.Restart(Config, Emulator, Game);
					DisplayManager.UpdateGlobals(Config, Emulator);
					DisplayManager.Blank();
					ExtToolManager.BuildToolStrip();
					CheatList.NewList("", autosave: true);
					OnRomChanged();
					return false;
				}
				else
				{
					// The ROM has been loaded by a recursive invocation of the LoadROM method.
					RomLoaded?.Invoke(this, EventArgs.Empty);
					return true;
				}
			}
			finally
			{
				if (firstCall)
				{
					_currentLoadRomArgs = null;
				}

				_isLoadingRom = false;
			}
		}

		private void OnRomChanged()
		{
			OSD.Fps = "0 fps";
			UpdateWindowTitle();
			HandlePlatformMenus();
			_stateSlots.ClearRedoList();
			UpdateStatusSlots();
			UpdateCoreStatusBarButton();
			UpdateDumpInfo();
			SetMainformMovieInfo();
			RA?.Restart();
		}

		private void CommitCoreSettingsToConfig()
		{
			// save settings object
			var t = Emulator.GetType();
			var settable = GetSettingsAdapterForLoadedCoreUntyped();

			if (settable.HasSettings)
			{
				Config.PutCoreSettings(settable.GetSettings(), t);
			}

			if (settable.HasSyncSettings && MovieSession.Movie.NotActive())
			{
				// don't trample config with loaded-from-movie settings
				Config.PutCoreSyncSettings(settable.GetSyncSettings(), t);
			}
		}

		// whats the difference between these two methods??
		// its very tricky. rename to be more clear or combine them.
		// This gets called whenever a core related thing is changed.
		// Like reboot core.
		private void CloseGame(bool clearSram = false)
		{
			GameIsClosing = true;
			if (clearSram)
			{
				var path = Config.PathEntries.SaveRamAbsolutePath(Game, MovieSession.Movie);
				if (File.Exists(path))
				{
					File.Delete(path);
					AddOnScreenMessage("SRAM cleared.");
				}
			}
			else if (Emulator.HasSaveRam() && Emulator.AsSaveRam().SaveRamModified)
			{
				if (!FlushSaveRAM())
				{
					var msgRes = ShowMessageBox2(
						owner: null,
						"Failed flushing the game's Save RAM to your disk.\nClose without flushing Save RAM?",
						"Directory IO Error",
						EMsgBoxIcon.Error);

					if (!msgRes)
					{
						return;
					}
				}
			}

			StopAv();
			AutoSaveStateIfConfigured();

			CommitCoreSettingsToConfig();
			Rewinder?.Dispose();
			Rewinder = null;

			if (MovieSession.Movie.IsActive()) // Note: this must be called after CommitCoreSettingsToConfig()
			{
				StopMovie();
			}

			RA?.Stop();

			CheatList.SaveOnClose();
			Emulator.Dispose();
			Emulator = new NullEmulator();
			InputManager.ResetMainControllers(_autofireNullControls);
			RewireSound();
			RebootStatusBarIcon.Visible = false;
			GameIsClosing = false;
		}

		private void AutoSaveStateIfConfigured()
		{
			if (Config.AutoSaveLastSaveSlot && Emulator.HasSavestates()) SavestateCurrentSlot();
		}

		public bool GameIsClosing { get; private set; } // Lets tools make better decisions when being called by CloseGame

		public void CloseRom(bool clearSram = false)
		{
			// This gets called after Close Game gets called.
			// Tested with NESHawk and SMB3 (U)
			if (Tools.AskSave())
			{
				CloseGame(clearSram);
				Emulator.Dispose();
				Emulator = new NullEmulator();
				Game = GameInfo.NullInstance;
				Tools.Restart(Config, Emulator, Game);
				RewireSound();
				ClearHolds();
				DisplayManager.UpdateGlobals(Config, Emulator);
				InputManager.SyncControls(Emulator, MovieSession, Config);
				Tools.UpdateCheatRelatedTools(null, null);
				ExtToolManager.BuildToolStrip();
				PauseOnFrame = null;
				CurrentlyOpenRom = null;
				CurrentlyOpenRomArgs = null;
				CheatList.NewList("", autosave: true);
				OnRomChanged();
			}
		}

		private void ProcessMovieImport(string fn, bool start)
		{
			var result = MovieImport.ImportFile(this, MovieSession, fn, Config);

			if (result.Errors.Any())
			{
				ShowMessageBox(owner: null, string.Join("\n", result.Errors), "Conversion error", EMsgBoxIcon.Error);
				return;
			}

			if (result.Warnings.Any())
			{
				AddOnScreenMessage(result.Warnings.First()); // For now, just show the first warning
			}

			AddOnScreenMessage($"{Path.GetFileName(fn)} imported as {result.Movie.Filename}");

			if (start)
			{
				StartNewMovie(result.Movie, false);
				Config.RecentMovies.Add(result.Movie.Filename);
			}
		}

		public void EnableRewind(bool enabled)
		{
			if (!Emulator.HasSavestates())
			{
				return;
			}

			if (Rewinder == null)
			{
				CreateRewinder();
			}

			// CreateRewinder doesn't necessarily create an instance of rewinder, still need to check null
			if (Rewinder != null)
			{
				if (enabled)
				{
					Rewinder.Resume();
				}
				else
				{
					Rewinder.Suspend();
				}

				AddOnScreenMessage($"Rewind {(enabled ? "enabled" : "suspended")}");
			}
		}

		public void DisableRewind()
		{
			Rewinder?.Dispose();
			Rewinder = null;
		}

		private int SlotToInt(string slot)
		{
			return int.Parse(slot.Substring(slot.Length - 1, 1));
		}

		public BitmapBuffer/*?*/ ReadScreenshotFromSavestate(int slot)
		{
			if (!Emulator.HasSavestates()) return null;
			var path = $"{SaveStatePrefix()}.QuickSave{slot % 10}.State";
			return File.Exists(path) ? SavestateFile.GetFrameBufferFrom(path) : null;
		}

		public bool LoadState(string path, string userFriendlyStateName, bool suppressOSD = false) // Move to client.common
		{
			if (!Emulator.HasSavestates()) return false;
			if (ToolControllingSavestates is { } tool) return tool.LoadState();

			if (!new SavestateFile(Emulator, MovieSession, MovieSession.UserBag).Load(path, this))
			{
				AddOnScreenMessage("Loadstate error!");
				return false;
			}

			OSD.ClearGuiText();
			if (SavestateLoaded is not null)
			{
				StateLoadedEventArgs args = new(userFriendlyStateName);
				SavestateLoaded(this, args);
			}
			RA?.OnLoadState(path);

			if (Tools.Has<LuaConsole>())
			{
				Tools.LuaConsole.LuaImp.CallLoadStateEvent(userFriendlyStateName);
			}

			SetMainformMovieInfo();
			Tools.UpdateToolsBefore();
			UpdateToolsAfter();
			UpdateToolsLoadstate();
			InputManager.AutoFireController.ClearStarts();

			//we don't want to analyze how to intermix movies, rewinding, and states
			//so purge rewind history when loading a state while doing a movie
			if (ToolControllingRewind is null && MovieSession.Movie.IsActive())
			{
				Rewinder?.Clear();
			}

			if (!suppressOSD)
			{
				AddOnScreenMessage($"Loaded state: {userFriendlyStateName}");
			}
			return true;
		}

		public bool LoadQuickSave(int slot, bool suppressOSD = false)
		{
			if (!Emulator.HasSavestates()) return false;

			var quickSlotName = $"QuickSave{slot % 10}";
			var handled = false;
			if (QuicksaveLoad is not null)
			{
				BeforeQuickLoadEventArgs args = new(quickSlotName);
				QuicksaveLoad(this, args);
				handled = args.Handled;
			}
			if (handled) return true; // not sure

			if (ToolControllingSavestates is { } tool) return tool.LoadQuickSave(SlotToInt(quickSlotName));

			var path = $"{SaveStatePrefix()}.{quickSlotName}.State";
			if (!File.Exists(path))
			{
				AddOnScreenMessage($"Unable to load {quickSlotName}.State");
				return false;
			}

			return LoadState(path: path, userFriendlyStateName: quickSlotName, suppressOSD: suppressOSD);
		}

		public void SaveState(string path, string userFriendlyStateName, bool fromLua = false, bool suppressOSD = false)
		{
			if (!Emulator.HasSavestates())
			{
				return;
			}

			if (ToolControllingSavestates is { } tool)
			{
				tool.SaveState();
				return;
			}

			if (MovieSession.Movie.IsFinished())
			{
				OSD.AddMessage("Cannot savestate after movie end!");
				return;
			}

			try
			{
				new SavestateFile(Emulator, MovieSession, MovieSession.UserBag).Create(path, Config.Savestates);

				if (SavestateSaved is not null)
				{
					StateSavedEventArgs args = new(userFriendlyStateName);
					SavestateSaved(this, args);
				}
				RA?.OnSaveState(path);

				if (!suppressOSD)
				{
					AddOnScreenMessage($"Saved state: {userFriendlyStateName}");
				}
			}
			catch (IOException)
			{
				AddOnScreenMessage($"Unable to save state {path}");
			}

			if (!fromLua)
			{
				UpdateStatusSlots();
			}
		}

		// TODO: should backup logic be stuffed in into Client.Common.SaveStateManager?
		public void SaveQuickSave(int slot, bool suppressOSD = false, bool fromLua = false)
		{
			if (!Emulator.HasSavestates())
			{
				return;
			}
			var quickSlotName = $"QuickSave{slot % 10}";
			var handled = false;
			if (QuicksaveSave is not null)
			{
				BeforeQuickSaveEventArgs args = new(quickSlotName);
				QuicksaveSave(this, args);
				handled = args.Handled;
			}
			if (handled)
			{
				return;
			}

			if (ToolControllingSavestates is { } tool)
			{
				tool.SaveQuickSave(SlotToInt(quickSlotName));
				return;
			}

			var path = $"{SaveStatePrefix()}.{quickSlotName}.State";

			var file = new FileInfo(path);
			if (file.Directory != null && !file.Directory.Exists)
			{
				file.Directory.Create();
			}

			// Make backup first
			if (Config.Savestates.MakeBackups)
			{
				Util.TryMoveBackupFile(path, $"{path}.bak");
			}

			SaveState(path, quickSlotName, fromLua, suppressOSD);

			if (Tools.Has<LuaConsole>())
			{
				Tools.LuaConsole.LuaImp.CallSaveStateEvent(quickSlotName);
			}
		}

		public bool EnsureCoreIsAccurate()
		{
			bool PromptToSwitchCore(string currentCore, string recommendedCore, Action disableCurrentCore)
			{
				using var box = new MsgBox(
					$"While the {currentCore} core is faster, it is not nearly as accurate as {recommendedCore}.{Environment.NewLine}It is recommended that you switch to the {recommendedCore} core for movie recording.{Environment.NewLine}Switch to {recommendedCore}?",
					"Accuracy Warning",
					MessageBoxIcon.Warning);

				box.SetButtons(
					new[] { "Switch", "Continue" },
					new[] { DialogResult.Yes, DialogResult.Cancel });

				box.MaximumSize = UIHelper.Scale(new Size(575, 175));
				box.SetMessageToAutoSize();
				if (this.ShowDialogWithTempMute(box) is not DialogResult.Yes) return false;

				disableCurrentCore();
				RebootCore();
				return true;
			}

			var currentCoreName = Emulator.Attributes().CoreName;
			var recommendedCore = currentCoreName switch
			{
				CoreNames.Snes9X => CoreNames.Bsnes115,
				CoreNames.QuickNes => CoreNames.NesHawk,
				CoreNames.HyperNyma => CoreNames.TurboNyma,
				_ => null
			};
			return recommendedCore is null
				? true
				: PromptToSwitchCore(currentCoreName, recommendedCore, () => Config.PreferredCores[Emulator.SystemId] = recommendedCore);
		}

		private void SaveStateAs()
		{
			if (!Emulator.HasSavestates())
			{
				return;
			}

			// allow named state export for tastudio, since it's safe, unlike loading one
			// todo: make it not save laglog in that case
			if (Tools.IsLoaded<TAStudio>())
			{
				Tools.TAStudio.NamedStatePending = true;
			}

			if (ToolControllingSavestates is { } tool)
			{
				tool.SaveStateAs();
				return;
			}

			var path = Config.PathEntries.SaveStateAbsolutePath(Game.System);

			var file = new FileInfo(path);
			if (file.Directory != null && !file.Directory.Exists)
			{
				file.Directory.Create();
			}

			var result = this.ShowFileSaveDialog(
				fileExt: "State",
				filter: EmuHawkSaveStatesFSFilterSet,
				initDir: path,
				initFileName: $"{SaveStatePrefix()}.QuickSave0.State");
			if (result is not null) SaveState(path: result, userFriendlyStateName: result);

			if (Tools.IsLoaded<TAStudio>())
			{
				Tools.TAStudio.NamedStatePending = false;
			}
		}

		private bool LoadStateAs()
		{
			if (!Emulator.HasSavestates()) return false;
			if (ToolControllingSavestates is { } tool) return tool.LoadStateAs();

			var result = this.ShowFileOpenDialog(
				discardCWDChange: true,
				filter: EmuHawkSaveStatesFSFilterSet,
				initDir: Config.PathEntries.SaveStateAbsolutePath(Game.System));
			if (result is null || !File.Exists(result)) return false;
			return LoadState(path: result, userFriendlyStateName: Path.GetFileName(result));
		}

		private void SelectSlot(int slot)
		{
			if (!Emulator.HasSavestates()) return;
			if (ToolControllingSavestates is { } tool)
			{
				bool handled = tool.SelectSlot(slot);
				if (handled) return;
			}
			Config.SaveSlot = slot;
			SaveSlotSelectedMessage();
			UpdateStatusSlots();
		}

		private void PreviousSlot()
		{
			if (!Emulator.HasSavestates()) return;
			if (ToolControllingSavestates is { } tool)
			{
				bool handled = tool.PreviousSlot();
				if (handled) return;
			}
			Config.SaveSlot--;
			if (Config.SaveSlot < 1) Config.SaveSlot = 10;
			SaveSlotSelectedMessage();
			UpdateStatusSlots();
		}

		private void NextSlot()
		{
			if (!Emulator.HasSavestates()) return;
			if (ToolControllingSavestates is { } tool)
			{
				bool handled = tool.NextSlot();
				if (handled) return;
			}
			Config.SaveSlot++;
			if (Config.SaveSlot > 10) Config.SaveSlot = 1;
			SaveSlotSelectedMessage();
			UpdateStatusSlots();
		}

		private void CaptureRewind(bool suppressCaptureRewind)
		{
			if (ToolControllingRewind is { } tool)
			{
				tool.CaptureRewind();
			}
			else if (!suppressCaptureRewind && Rewinder?.Active == true)
			{
				Rewinder.Capture(Emulator.Frame);
			}
		}

		private bool Rewind(ref bool runFrame, long currentTimestamp, out bool returnToRecording)
		{
			var isRewinding = false;

			returnToRecording = false;

			if (ToolControllingRewind is { } rewindTool)
			{
				if (InputManager.ClientControls["Rewind"] || PressRewind)
				{
					if (_frameRewindTimestamp == 0)
					{
						isRewinding = true;
						_frameRewindTimestamp = currentTimestamp;
						_frameRewindWasPaused = EmulatorPaused;
					}
					else
					{
						double timestampDeltaMs = (double)(currentTimestamp - _frameRewindTimestamp) / Stopwatch.Frequency * 1000.0;
						isRewinding = timestampDeltaMs >= Config.FrameProgressDelayMs;

						// clear this flag once we get out of the progress stage
						if (isRewinding)
						{
							_frameRewindWasPaused = false;
						}

						// if we're freely running, there's no need for reverse frame progress semantics (that may be debatable though)
						if (!EmulatorPaused)
						{
							isRewinding = true;
						}

						if (_frameRewindWasPaused)
						{
							if (IsSeeking)
							{
								isRewinding = false;
							}
						}
					}

					if (isRewinding)
					{
						runFrame = Emulator.Frame > 1; // TODO: the master should be deciding this!
						rewindTool.Rewind();
					}
				}
				else
				{
					_frameRewindTimestamp = 0;
				}

				return isRewinding;
			}

			if (Rewinder?.Active == true && (InputManager.ClientControls["Rewind"] || PressRewind))
			{
				if (EmulatorPaused)
				{
					if (_frameRewindTimestamp == 0)
					{
						isRewinding = true;
						_frameRewindTimestamp = currentTimestamp;
					}
					else
					{
						double timestampDeltaMs = (double)(currentTimestamp - _frameRewindTimestamp) / Stopwatch.Frequency * 1000.0;
						isRewinding = timestampDeltaMs >= Config.FrameProgressDelayMs;
					}
				}
				else
				{
					isRewinding = true;
				}

				if (isRewinding)
				{
					// Try to avoid the previous frame:  We want to frame advance right after rewinding so we can give a useful
					// framebuffer.
					var frameToAvoid = Emulator.Frame - 1;
					runFrame = Rewinder.Rewind(frameToAvoid);
					if (Emulator.Frame == frameToAvoid)
					{
						// The rewinder was unable to satisfy our request.  Prefer showing a stale framebuffer to
						// advancing in a way that essentially no-ops the entire rewind.
						runFrame = false;
					}

					if (runFrame && MovieSession.Movie.IsRecording())
					{
						MovieSession.Movie.SwitchToPlay();
						returnToRecording = true;
					}
				}
			}
			else
			{
				_frameRewindTimestamp = 0;
			}

			return isRewinding;
		}

		public IDialogController DialogController => this;

		public IReadOnlyList<string>/*?*/ ShowFileMultiOpenDialog(
			IDialogParent dialogParent,
			string/*?*/ filterStr,
			ref int filterIndex,
			string initDir,
			bool discardCWDChange = false,
			string/*?*/ initFileName = null,
			bool maySelectMultiple = false,
			string/*?*/ windowTitle = null)
		{
			using OpenFileDialog ofd = new()
			{
				FileName = initFileName ?? string.Empty,
				Filter = filterStr ?? string.Empty,
				FilterIndex = filterIndex,
				InitialDirectory = initDir,
				Multiselect = maySelectMultiple,
				RestoreDirectory = discardCWDChange,
				Title = windowTitle ?? string.Empty,
			};
			var result = dialogParent.ShowDialogWithTempMute(ofd);
			filterIndex = ofd.FilterIndex;
			return result.IsOk() && ofd.FileNames.Length is not 0 ? ofd.FileNames : null;
		}

		public string/*?*/ ShowFileSaveDialog(
			IDialogParent dialogParent,
			bool discardCWDChange,
			string/*?*/ fileExt,
			string/*?*/ filterStr,
			string initDir,
			string/*?*/ initFileName,
			bool muteOverwriteWarning)
		{
			using SaveFileDialog sfd = new()
			{
				DefaultExt = fileExt ?? string.Empty,
				FileName = initFileName ?? string.Empty,
				Filter = filterStr ?? string.Empty,
				InitialDirectory = initDir,
				OverwritePrompt = !muteOverwriteWarning,
				RestoreDirectory = discardCWDChange,
			};
			var result = dialogParent.ShowDialogWithTempMute(sfd);
			return result.IsOk() ? sfd.FileName : null;
		}

		public void ShowMessageBox(
			IDialogParent/*?*/ owner,
			string text,
			string/*?*/ caption = null,
			EMsgBoxIcon? icon = null)
				=> this.ShowMessageBox(
					owner: owner,
					text: text,
					caption: caption,
					buttons: MessageBoxButtons.OK,
					icon: icon);

		public bool ShowMessageBox2(
			IDialogParent/*?*/ owner,
			string text,
			string/*?*/ caption = null,
			EMsgBoxIcon? icon = null,
			bool useOKCancel = false)
				=> this.ShowMessageBox(
					owner: owner,
					text: text,
					caption: caption,
					buttons: useOKCancel ? MessageBoxButtons.OKCancel : MessageBoxButtons.YesNo,
					icon: icon) switch
				{
					DialogResult.OK => true,
					DialogResult.Yes => true,
					_ => false
				};

		public bool? ShowMessageBox3(
			IDialogParent/*?*/ owner,
			string text,
			string/*?*/ caption = null,
			EMsgBoxIcon? icon = null)
				=> this.ShowMessageBox(
					owner: owner,
					text: text,
					caption: caption,
					buttons: MessageBoxButtons.YesNoCancel,
					icon: icon) switch
				{
					DialogResult.Yes => true,
					DialogResult.No => false,
					_ => null
				};

		public void StartSound() => Sound.StartSound();
		public void StopSound() => Sound.StopSound();

		private Mutex _singleInstanceMutex;
		private NamedPipeServerStream _singleInstanceServer;
		private readonly List<string[]> _singleInstanceForwardedArgs = new();

		private bool SingleInstanceInit(string[] args)
		{
			//note: this isn't 100% reliable, it's just a user convenience
			_singleInstanceMutex = new Mutex(true, "mutex-{84125ACB-F570-4458-9748-321F887FE795}", out bool createdNew);
			if (createdNew)
			{
				StartSingleInstanceServer();
				return false;
			}
			else
			{
				ForwardSingleInstanceStartup(args);
				return true;
			}
		}

		private void SingleInstanceDispose()
		{
			_singleInstanceServer?.Dispose();
		}

		private void ForwardSingleInstanceStartup(string[] args)
		{
			using var namedPipeClientStream = new NamedPipeClientStream(".", "pipe-{84125ACB-F570-4458-9748-321F887FE795}", PipeDirection.Out);
			try
			{
				namedPipeClientStream.Connect(0);
				//do this a bit cryptically to avoid loading up another big assembly (especially ones as frail as http and/or web ones)
				var payloadString = string.Join("|", args.Select(a => Encoding.UTF8.GetBytes(a).BytesToHexString()));
				var payloadBytes = Encoding.ASCII.GetBytes(payloadString);
				namedPipeClientStream.Write(payloadBytes, 0, payloadBytes.Length);
			}
			catch
			{
				Console.WriteLine("Failed forwarding args to already-running single instance");
			}
		}

		private void StartSingleInstanceServer()
		{
			//MIT LICENSE - https://www.autoitconsulting.com/site/development/single-instance-winform-app-csharp-mutex-named-pipes/

			// Create a new pipe accessible by local authenticated users, disallow network
			var sidNetworkService = new SecurityIdentifier(WellKnownSidType.NetworkServiceSid, null);
			var sidWorld = new SecurityIdentifier(WellKnownSidType.WorldSid, null);

			var pipeSecurity = new PipeSecurity();

			// Deny network access to the pipe
			var accessRule = new PipeAccessRule(sidNetworkService, PipeAccessRights.ReadWrite, AccessControlType.Deny);
			pipeSecurity.AddAccessRule(accessRule);

			// Alow Everyone to read/write
			accessRule = new PipeAccessRule(sidWorld, PipeAccessRights.ReadWrite, AccessControlType.Allow);
			pipeSecurity.AddAccessRule(accessRule);

			// Current user is the owner
			SecurityIdentifier sidOwner = WindowsIdentity.GetCurrent().Owner;
			if (sidOwner != null)
			{
				accessRule = new PipeAccessRule(sidOwner, PipeAccessRights.FullControl, AccessControlType.Allow);
				pipeSecurity.AddAccessRule(accessRule);
			}

			// Create pipe and start the async connection wait
			_singleInstanceServer = new NamedPipeServerStream(
					"pipe-{84125ACB-F570-4458-9748-321F887FE795}",
					PipeDirection.In,
					1,
					PipeTransmissionMode.Message,
					PipeOptions.Asynchronous,
					0,
					0,
					pipeSecurity);

			// Begin async wait for connections
			_singleInstanceServer.BeginWaitForConnection(SingleInstanceServerPipeCallback, null);
		}

		//Note: This method is called on a non-UI thread.
		//Note: this seems really frail. I don't think it's industrial strength. Pipes are weak compared to sockets.
		//It was probably frail in the first place with the old vbnet impl
		private void SingleInstanceServerPipeCallback(IAsyncResult iAsyncResult)
		{
			try
			{
				_singleInstanceServer.EndWaitForConnection(iAsyncResult);

				//a bit over-engineered in case someone wants to send a script or a rom or something
				//buffer size is set to something tiny so that we are continually testing it
				var payloadBytes = new MemoryStream();
				while (true)
				{
					var bytes = new byte[16];
					int did = _singleInstanceServer.Read(bytes, 0, bytes.Length);
					payloadBytes.Write(bytes, 0, did);
					if (_singleInstanceServer.IsMessageComplete) break;
				}

				var payloadString = Encoding.ASCII.GetString(payloadBytes.GetBuffer(), 0, (int)payloadBytes.Length);
				var args = payloadString.Split('|').Select(a => Encoding.UTF8.GetString(a.HexStringToBytes())).ToArray();

				Console.WriteLine("RECEIVED SINGLE INSTANCE FORWARDED ARGS:");
				lock (_singleInstanceForwardedArgs)
					_singleInstanceForwardedArgs.Add(args);
			}
			catch (ObjectDisposedException)
			{
				// EndWaitForConnection will exception when someone calls closes the pipe before connection made
				// In that case we dont create any more pipes and just return
				// This will happen when app is closing and our pipe is closed/disposed
				return;
			}
			catch (Exception)
			{
				// ignored
			}
			finally
			{
				// Close the original pipe (we will create a new one each time)
				_singleInstanceServer.Dispose();
			}

			// Create a new pipe for next connection
			StartSingleInstanceServer();
		}

		private void SingleInstanceProcessArgs(string[] args)
		{
			//ulp. it's not clear how to handle these.
			//we only have a legacy case where we can tell the form to load a rom, if it's in a sensible condition for that.
			//er.. let's assume it's always in a sensible condition
			//in case this all sounds insanely sketchy to you, remember, the main 99% use case is double clicking roms in explorer

			//BANZAIIIIIIIIIIIIIIIIIIIIIIIIIII
			_ = LoadRom(args[0]);
		}

		private IRetroAchievements RA { get; set; }

		private void OpenRetroAchievements()
		{
			RA = RetroAchievements.CreateImpl(
				this,
				InputManager,
				Tools,
				() => Config,
				wavFile => Sound.PlayWavFile(wavFile, 1), // TODO: Make this configurable
				RetroAchievementsMenuItem.DropDownItems,
				() =>
				{
					RA.Dispose();
					RA = null;
					RetroAchievementsMenuItem.DropDownItems.Clear();
					RetroAchievementsMenuItem.DropDownItems.Add(StartRetroAchievementsMenuItem);
				});

			RA?.Restart();
		}
	}
}
