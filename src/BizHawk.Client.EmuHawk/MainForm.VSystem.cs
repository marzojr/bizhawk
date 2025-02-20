using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;
using BizHawk.Emulation.Cores.Arcades.MAME;
using BizHawk.Emulation.Cores.Atari.A7800Hawk;
using BizHawk.Emulation.Cores.Atari.Atari2600;
using BizHawk.Emulation.Cores.Atari.Jaguar;
using BizHawk.Emulation.Cores.Atari.Lynx;
using BizHawk.Emulation.Cores.Atari.Stella;
using BizHawk.Emulation.Cores.Calculators.Emu83;
using BizHawk.Emulation.Cores.Calculators.TI83;
using BizHawk.Emulation.Cores.ColecoVision;
using BizHawk.Emulation.Cores.Computers.Amiga;
using BizHawk.Emulation.Cores.Computers.DOS;
using BizHawk.Emulation.Cores.Computers.AmstradCPC;
using BizHawk.Emulation.Cores.Computers.AppleII;
using BizHawk.Emulation.Cores.Computers.Commodore64;
using BizHawk.Emulation.Cores.Computers.Doom;
using BizHawk.Emulation.Cores.Computers.MSX;
using BizHawk.Emulation.Cores.Computers.SinclairSpectrum;
using BizHawk.Emulation.Cores.Computers.TIC80;
using BizHawk.Emulation.Cores.Consoles.Belogic;
using BizHawk.Emulation.Cores.Consoles.ChannelF;
using BizHawk.Emulation.Cores.Consoles.NEC.PCE;
using BizHawk.Emulation.Cores.Consoles.NEC.PCFX;
using BizHawk.Emulation.Cores.Consoles.Nintendo.Ares64;
using BizHawk.Emulation.Cores.Consoles.Nintendo.Faust;
using BizHawk.Emulation.Cores.Consoles.Nintendo.N3DS;
using BizHawk.Emulation.Cores.Consoles.Nintendo.NDS;
using BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES;
using BizHawk.Emulation.Cores.Consoles.Nintendo.VB;
using BizHawk.Emulation.Cores.Consoles.O2Hawk;
using BizHawk.Emulation.Cores.Consoles.SNK;
using BizHawk.Emulation.Cores.Consoles.Sega.PicoDrive;
using BizHawk.Emulation.Cores.Consoles.Sega.Saturn;
using BizHawk.Emulation.Cores.Consoles.Sega.gpgx;
using BizHawk.Emulation.Cores.Consoles.Vectrex;
using BizHawk.Emulation.Cores.Intellivision;
using BizHawk.Emulation.Cores.Libretro;
using BizHawk.Emulation.Cores.Nintendo.BSNES;
using BizHawk.Emulation.Cores.Nintendo.GBA;
using BizHawk.Emulation.Cores.Nintendo.GBHawk;
using BizHawk.Emulation.Cores.Nintendo.GBHawkLink3x;
using BizHawk.Emulation.Cores.Nintendo.GBHawkLink4x;
using BizHawk.Emulation.Cores.Nintendo.GBHawkLink;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.Nintendo.N64;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.Nintendo.SNES9X;
using BizHawk.Emulation.Cores.Nintendo.SNES;
using BizHawk.Emulation.Cores.Nintendo.Sameboy;
using BizHawk.Emulation.Cores.Nintendo.SubGBHawk;
using BizHawk.Emulation.Cores.Nintendo.SubNESHawk;
using BizHawk.Emulation.Cores.PCEngine;
using BizHawk.Emulation.Cores.Sega.GGHawkLink;
using BizHawk.Emulation.Cores.Sega.MasterSystem;
using BizHawk.Emulation.Cores.Sony.PSX;
using BizHawk.Emulation.Cores.Waterbox;
using BizHawk.Emulation.Cores.WonderSwan;
using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk
{
	public partial class MainForm
	{
		private DialogResult OpenA7800HawkGamepadSettingsDialog(ISettingsAdapter settable)
		{
			using A7800ControllerSettings form = new(settable);
			return this.ShowDialogWithTempMute(form);
		}

		private void A7800ControllerSettingsMenuItem_Click(object sender, EventArgs e)
			=> _ = Emulator switch
			{
				A7800Hawk => OpenA7800HawkGamepadSettingsDialog(GetSettingsAdapterForLoadedCore<A7800Hawk>()),
				_ => DialogResult.None
			};

		private DialogResult OpenA7800HawkFilterSettingsDialog(ISettingsAdapter settable)
		{
			using A7800FilterSettings form = new(settable);
			return this.ShowDialogWithTempMute(form);
		}

		private void A7800FilterSettingsMenuItem_Click(object sender, EventArgs e)
			=> _ = Emulator switch
			{
				A7800Hawk => OpenA7800HawkFilterSettingsDialog(GetSettingsAdapterForLoadedCore<A7800Hawk>()),
				_ => DialogResult.None
			};

		private void A7800SubMenu_DropDownOpened(object sender, EventArgs e)
			=> A7800ControllerSettingsMenuItem.Enabled = A7800FilterSettingsMenuItem.Enabled
				= MovieSession.Movie.NotActive();



		private void AppleDisksSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			AppleDisksSubMenu.DropDownItems.Clear();
			if (Emulator is not AppleII appleII) return;
			EventHandler clickHandler = (clickSender, _) =>
			{
				if (!object.ReferenceEquals(appleII, Emulator)) return;
				appleII.SetDisk((int) ((ToolStripItem) clickSender).Tag);
			};
			var selected = appleII.CurrentDisk;
			for (int i = 0; i < appleII.DiskCount; i++)
			{
				ToolStripMenuItem menuItem = new()
				{
					Checked = i == selected,
					Tag = i,
					Text = $"Disk{i + 1}",
				};
				menuItem.Click += clickHandler;
				AppleDisksSubMenu.DropDownItems.Add(menuItem);
			}
		}

		private DialogResult OpenVirtuSettingsDialog()
			=> OpenGenericCoreConfigFor<AppleII>(CoreNames.Virtu + " Settings");

		private void AppleIISettingsMenuItem_Click(object sender, EventArgs e)
			=> _ = Emulator switch
			{
				AppleII => OpenVirtuSettingsDialog(),
				_ => DialogResult.None
			};

		private void AppleSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			if (Emulator is not AppleII a) return;
			AppleDisksSubMenu.Enabled = a.DiskCount > 1;
		}



		private void C64DisksSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			C64DisksSubMenu.DropDownItems.Clear();
			if (Emulator is not C64 c64) return;
			EventHandler clickHandler = (clickSender, _) =>
			{
				if (!object.ReferenceEquals(c64, Emulator)) return;
				c64.SetDisk((int) ((ToolStripItem) clickSender).Tag);
			};
			var selected = c64.CurrentDisk;
			for (int i = 0; i < c64.DiskCount; i++)
			{
				ToolStripMenuItem menuItem = new()
				{
					Checked = i == selected,
					Tag = i,
					Text = $"Disk{i + 1}",
				};
				menuItem.Click += clickHandler;
				C64DisksSubMenu.DropDownItems.Add(menuItem);
			}
		}

		private DialogResult OpenC64HawkSettingsDialog()
			=> OpenGenericCoreConfigFor<C64>(CoreNames.C64Hawk + " Settings");

		private void C64SettingsMenuItem_Click(object sender, EventArgs e)
			=> _ = Emulator switch
			{
				C64 => OpenC64HawkSettingsDialog(),
				_ => DialogResult.None
			};

		private void C64SubMenu_DropDownOpened(object sender, EventArgs e)
		{
			if (Emulator is not C64 c64) return;
			C64DisksSubMenu.Enabled = c64.DiskCount > 1;
		}



		private DialogResult OpenColecoHawkGamepadSettingsDialog(ISettingsAdapter settable)
		{
			using ColecoControllerSettings form = new(settable);
			return this.ShowDialogWithTempMute(form);
		}

		private void ColecoControllerSettingsMenuItem_Click(object sender, EventArgs e)
			=> _ = Emulator switch
			{
				ColecoVision => OpenColecoHawkGamepadSettingsDialog(GetSettingsAdapterForLoadedCore<ColecoVision>()),
				_ => DialogResult.None
			};

		private void ColecoHawkSetSkipBIOSIntro(bool newValue, ISettingsAdapter settable)
		{
			var ss = (ColecoVision.ColecoSyncSettings) settable.GetSyncSettings();
			ss.SkipBiosIntro = newValue;
			settable.PutCoreSyncSettings(ss);
		}

		private void ColecoSkipBiosMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is ColecoVision) ColecoHawkSetSkipBIOSIntro(!((ToolStripMenuItem) sender).Checked, GetSettingsAdapterForLoadedCore<ColecoVision>());
		}

		private void ColecoHawkSetSuperGameModule(bool newValue, ISettingsAdapter settable)
		{
			var ss = (ColecoVision.ColecoSyncSettings) settable.GetSyncSettings();
			ss.UseSGM = newValue;
			settable.PutCoreSyncSettings(ss);
		}

		private void ColecoUseSGMMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is ColecoVision) ColecoHawkSetSuperGameModule(!((ToolStripMenuItem) sender).Checked, GetSettingsAdapterForLoadedCore<ColecoVision>());
		}

		private void ColecoSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			if (Emulator is not ColecoVision coleco) return;
			var ss = coleco.GetSyncSettings();
			ColecoSkipBiosMenuItem.Checked = ss.SkipBiosIntro;
			ColecoUseSGMMenuItem.Checked = ss.UseSGM;
			ColecoControllerSettingsMenuItem.Enabled = MovieSession.Movie.NotActive();
		}



		private DialogResult OpenCPCHawkSyncSettingsDialog(ISettingsAdapter settable)
		{
			using AmstradCpcCoreEmulationSettings form = new(settable);
			return this.ShowDialogWithTempMute(form);
		}

		private void AmstradCpcCoreEmulationSettingsMenuItem_Click(object sender, EventArgs e)
			=> _ = Emulator switch
			{
				AmstradCPC => OpenCPCHawkSyncSettingsDialog(GetSettingsAdapterForLoadedCore<AmstradCPC>()),
				_ => DialogResult.None
			};

		private DialogResult OpenCPCHawkAudioSettingsDialog(ISettingsAdapter settable)
		{
			using AmstradCpcAudioSettings form = new(settable);
			return this.ShowDialogWithTempMute(form);
		}

		private void AmstradCpcAudioSettingsMenuItem_Click(object sender, EventArgs e)
			=> _ = Emulator switch
			{
				AmstradCPC => OpenCPCHawkAudioSettingsDialog(GetSettingsAdapterForLoadedCore<AmstradCPC>()),
				_ => DialogResult.None
			};

		private DialogResult OpenCPCHawkSettingsDialog(ISettingsAdapter settable)
		{
			using AmstradCpcNonSyncSettings form = new(settable);
			return this.ShowDialogWithTempMute(form);
		}

		private void AmstradCpcNonSyncSettingsMenuItem_Click(object sender, EventArgs e)
			=> _ = Emulator switch
			{
				AmstradCPC => OpenCPCHawkSettingsDialog(GetSettingsAdapterForLoadedCore<AmstradCPC>()),
				_ => DialogResult.None
			};

		private void AmstradCpcTapesSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			AmstradCPCTapesSubMenu.DropDownItems.Clear();
			if (Emulator is not AmstradCPC ams) return;
			EventHandler clickHandler = (clickSender, _) =>
			{
				if (!object.ReferenceEquals(ams, Emulator)) return;
				ams._machine.TapeMediaIndex = (int) ((ToolStripItem) clickSender).Tag;
			};
			var tapeMediaIndex = ams._machine.TapeMediaIndex;
			for (int i = 0; i < ams._tapeInfo.Count; i++)
			{
				ToolStripMenuItem menuItem = new()
				{
					Checked = i == tapeMediaIndex,
					Tag = i,
					Text = $"{i}: {ams._tapeInfo[i].Name}",
				};
				menuItem.Click += clickHandler;
				AmstradCPCTapesSubMenu.DropDownItems.Add(menuItem);
			}
		}

		private void AmstradCpcDisksSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			AmstradCPCDisksSubMenu.DropDownItems.Clear();
			if (Emulator is not AmstradCPC ams) return;
			EventHandler clickHandler = (clickSender, _) =>
			{
				if (!object.ReferenceEquals(ams, Emulator)) return;
				ams._machine.DiskMediaIndex = (int) ((ToolStripItem) clickSender).Tag;
			};
			var diskMediaIndex = ams._machine.DiskMediaIndex;
			for (int i = 0; i < ams._diskInfo.Count; i++)
			{
				ToolStripMenuItem menuItem = new()
				{
					Checked = i == diskMediaIndex,
					Tag = i,
					Text = $"{i}: {ams._diskInfo[i].Name}",
				};
				menuItem.Click += clickHandler;
				AmstradCPCDisksSubMenu.DropDownItems.Add(menuItem);
			}
		}

		private void AmstradCpcMediaMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			if (Emulator is AmstradCPC cpc)
			{
				AmstradCPCTapesSubMenu.Enabled = cpc._tapeInfo.Count > 0;
				AmstradCPCDisksSubMenu.Enabled = cpc._diskInfo.Count > 0;
			}
		}



		private DialogResult OpenGambatteSettingsDialog(ISettingsAdapter settable)
			=> GBPrefs.DoGBPrefsDialog(Config, this, Game, MovieSession, settable);

		private DialogResult OpenGBHawkSettingsDialog()
			=> OpenGenericCoreConfigFor<GBHawk>(CoreNames.GbHawk + " Settings");

		private DialogResult OpenSameBoySettingsDialog()
			=> OpenGenericCoreConfigFor<Sameboy>(CoreNames.Sameboy + " Settings");

		private DialogResult OpenSubGBHawkSettingsDialog()
			=> OpenGenericCoreConfigFor<SubGBHawk>(CoreNames.SubGbHawk + " Settings");

		private void GbCoreSettingsMenuItem_Click(object sender, EventArgs e)
			=> _ = Emulator switch
			{
				Gameboy => OpenGambatteSettingsDialog(GetSettingsAdapterForLoadedCore<Gameboy>()),
				GBHawk => OpenGBHawkSettingsDialog(),
				Sameboy => OpenSameBoySettingsDialog(),
				SubGBHawk => OpenSubGBHawkSettingsDialog(),
				_ => DialogResult.None
			};

		private DialogResult OpenSameBoyPaletteSettingsDialog(ISettingsAdapter settable)
		{
			using SameBoyColorChooserForm form = new(Config, this, Game, settable);
			return this.ShowDialogWithTempMute(form);
		}

		private void SameboyColorChooserMenuItem_Click(object sender, EventArgs e)
			=> _ = Emulator switch
			{
				Sameboy => OpenSameBoyPaletteSettingsDialog(GetSettingsAdapterForLoadedCore<Sameboy>()),
				_ => DialogResult.None
			};

		private void GbGpuViewerMenuItem_Click(object sender, EventArgs e)
			=> Tools.Load<GbGpuView>();

		private void GbPrinterViewerMenuItem_Click(object sender, EventArgs e)
			=> Tools.Load<GBPrinterView>();

		private DialogResult OpenGambatteLinkSettingsDialog(ISettingsAdapter settable)
			=> GBLPrefs.DoGBLPrefsDialog(Config, this, Game, MovieSession, settable);

		private void GblSettingsMenuItem_Click(object sender, EventArgs e)
			=> _ = Emulator switch
			{
				GambatteLink => OpenGambatteLinkSettingsDialog(GetSettingsAdapterForLoadedCore<GambatteLink>()),
				_ => DialogResult.None
			};

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void ToggleGambatteSyncSetting(
			string name,
			Func<Gameboy.GambatteSyncSettings, bool> getter,
			Action<Gameboy.GambatteSyncSettings, bool> setter)
		{
			if (Emulator is not Gameboy gb) return;
			if (gb.DeterministicEmulation)
			{
				AddOnScreenMessage($"{name} cannot be toggled during movie recording.");
				return;
			}
			var ss = gb.GetSyncSettings();
			var newState = !getter(ss);
			setter(ss, newState);
			gb.PutSyncSettings(ss);
			AddOnScreenMessage($"{name} toggled {(newState ? "on" : "off")}");
		}

		private void GB_ToggleBackgroundLayer()
			=> ToggleGambatteSyncSetting(
				"BG",
				static ss => ss.DisplayBG,
				static (ss, newState) => ss.DisplayBG = newState);

		private void GB_ToggleObjectLayer()
			=> ToggleGambatteSyncSetting(
				"OBJ",
				static ss => ss.DisplayOBJ,
				static (ss, newState) => ss.DisplayOBJ = newState);

		private void GB_ToggleWindowLayer()
			=> ToggleGambatteSyncSetting(
				"WIN",
				static ss => ss.DisplayWindow,
				static (ss, newState) => ss.DisplayWindow = newState);



		private DialogResult OpenIntelliHawkGamepadSettingsDialog(ISettingsAdapter settable)
		{
			using IntvControllerSettings form = new(settable);
			return this.ShowDialogWithTempMute(form);
		}

		private void IntVControllerSettingsMenuItem_Click(object sender, EventArgs e)
			=> _ = Emulator switch
			{
				Intellivision => OpenIntelliHawkGamepadSettingsDialog(GetSettingsAdapterForLoadedCore<Intellivision>()),
				_ => DialogResult.None
			};

		private void IntVSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			IntVControllerSettingsMenuItem.Enabled = MovieSession.Movie.NotActive();
		}



		private DialogResult OpenMupen64PlusGraphicsSettingsDialog(ISettingsAdapter settable)
		{
			using N64VideoPluginConfig form = new(settable);
			return this.ShowDialogWithTempMute(form);
		}

		private void N64PluginSettingsMenuItem_Click(object sender, EventArgs e)
		{
			if (OpenMupen64PlusGraphicsSettingsDialog(GetSettingsAdapterFor<N64>()).IsOk()
				&& Emulator is not N64) // If it's loaded, the reboot required message will appear
			{
				AddOnScreenMessage("Plugin settings saved");
			}
		}

		private DialogResult OpenMupen64PlusGamepadSettingsDialog(ISettingsAdapter settable)
		{
			using N64ControllersSetup form = new(settable);
			return this.ShowDialogWithTempMute(form);
		}

		private void N64ControllerSettingsMenuItem_Click(object sender, EventArgs e)
			=> _ = Emulator switch
			{
				N64 => OpenMupen64PlusGamepadSettingsDialog(GetSettingsAdapterForLoadedCore<N64>()),
				_ => DialogResult.None
			};

		private void N64CircularAnalogRangeMenuItem_Click(object sender, EventArgs e)
			=> Config.N64UseCircularAnalogConstraint = !Config.N64UseCircularAnalogConstraint;

		private static void Mupen64PlusSetMupenStyleLag(bool newValue, ISettingsAdapter settable)
		{
			var s = (N64Settings) settable.GetSettings();
			s.UseMupenStyleLag = newValue;
			settable.PutCoreSettings(s);
		}

		private void MupenStyleLagMenuItem_Click(object sender, EventArgs e)
			=> Mupen64PlusSetMupenStyleLag(!((ToolStripMenuItem) sender).Checked, GetSettingsAdapterForLoadedCore<N64>());

		private void Mupen64PlusSetUseExpansionSlot(bool newValue, ISettingsAdapter settable)
		{
			var ss = (N64SyncSettings) settable.GetSyncSettings();
			ss.DisableExpansionSlot = !newValue;
			settable.PutCoreSyncSettings(ss);
		}

		private void N64ExpansionSlotMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is not N64) return;
			Mupen64PlusSetUseExpansionSlot(!((ToolStripMenuItem) sender).Checked, GetSettingsAdapterForLoadedCore<N64>());
			FlagNeedsReboot();
		}

		private void N64SubMenu_DropDownOpened(object sender, EventArgs e)
		{
			N64PluginSettingsMenuItem.Enabled = N64ControllerSettingsMenuItem.Enabled
				= N64ExpansionSlotMenuItem.Enabled
				= MovieSession.Movie.NotActive();
			N64CircularAnalogRangeMenuItem.Checked = Config.N64UseCircularAnalogConstraint;
			var mupen = (N64) Emulator;
			var s = mupen.GetSettings();
			MupenStyleLagMenuItem.Checked = s.UseMupenStyleLag;
			N64ExpansionSlotMenuItem.Checked = mupen.UsingExpansionSlot;
			N64ExpansionSlotMenuItem.Enabled = !mupen.IsOverridingUserExpansionSlotSetting;
		}

		private void Ares64SettingsMenuItem_Click(object sender, EventArgs e)
			=> OpenGenericCoreConfigFor<Ares64>(CoreNames.Ares64 + " Settings");

		private void Ares64SubMenu_DropDownOpened(object sender, EventArgs e)
			=> Ares64CircularAnalogRangeMenuItem.Checked = Config.N64UseCircularAnalogConstraint;



		private void NDS_IncrementScreenRotate()
		{
			if (Emulator is not NDS ds) return;

			var settings = ds.GetSettings();
			var next = settings.ScreenRotation switch
			{
				NDS.ScreenRotationKind.Rotate0 => NDS.ScreenRotationKind.Rotate90,
				NDS.ScreenRotationKind.Rotate90 => NDS.ScreenRotationKind.Rotate180,
				NDS.ScreenRotationKind.Rotate180 => NDS.ScreenRotationKind.Rotate270,
				NDS.ScreenRotationKind.Rotate270 => NDS.ScreenRotationKind.Rotate0,
				_ => settings.ScreenRotation
			};
			settings.ScreenRotation = next;
			ds.PutSettings(settings);
			AddOnScreenMessage($"Screen rotation to {next}");
			FrameBufferResized();
		}

		private void NDS_IncrementLayout(int delta)
		{
			if (Emulator is not NDS ds) return;

			var settings = ds.GetSettings();
			var t = typeof(NDS.ScreenLayoutKind);
			//TODO WTF is this --yoshi
			var next = (NDS.ScreenLayoutKind) Enum.Parse(t, ((int) settings.ScreenLayout + delta).ToString());
			if (!t.IsEnumDefined(next)) return;

			settings.ScreenLayout = next;
			ds.PutSettings(settings);
			AddOnScreenMessage($"Screen layout to {next}");
			FrameBufferResized();
		}



		/// <remarks>should this not be a separate sysID? --yoshi</remarks>
		private bool LoadedCoreIsNesHawkInVSMode
			=> Emulator is NES { IsVS: true } or SubNESHawk { IsVs: true };

		private void NesPpuViewerMenuItem_Click(object sender, EventArgs e)
			=> Tools.Load<NesPPU>();

		private void NesNametableViewerMenuItem_Click(object sender, EventArgs e)
			=> Tools.Load<NESNameTableViewer>();

		private void MusicRipperMenuItem_Click(object sender, EventArgs e)
			=> Tools.Load<NESMusicRipper>();

		private DialogResult OpenNesHawkGamepadSettingsDialog(ISettingsAdapter settable)
		{
			using NesControllerSettings form = new(settable);
			return this.ShowDialogWithTempMute(form);
		}

		private DialogResult OpenQuickNesGamepadSettingsDialog(ISettingsAdapter settable)
			=> GenericCoreConfig.DoDialogFor(
				this,
				settable,
				CoreNames.QuickNes + " Controller Settings",
				isMovieActive: MovieSession.Movie.IsActive(),
				ignoreSettings: true);

		private void NesControllerSettingsMenuItem_Click(object sender, EventArgs e)
			=> _ = Emulator switch
			{
				NES => OpenNesHawkGamepadSettingsDialog(GetSettingsAdapterForLoadedCore<NES>()),
				SubNESHawk => OpenNesHawkGamepadSettingsDialog(GetSettingsAdapterForLoadedCore<SubNESHawk>()),
				QuickNES => OpenQuickNesGamepadSettingsDialog(GetSettingsAdapterForLoadedCore<QuickNES>()),
				_ => DialogResult.None
			};

		private DialogResult OpenNesHawkGraphicsSettingsDialog(ISettingsAdapter settable)
		{
			using NESGraphicsConfig form = new(Config, this, settable);
			return this.ShowDialogWithTempMute(form);
		}

		private DialogResult OpenQuickNesGraphicsSettingsDialog(ISettingsAdapter settable)
		{
			using QuickNesConfig form = new(Config, DialogController, settable);
			return this.ShowDialogWithTempMute(form);
		}

		private void NesGraphicSettingsMenuItem_Click(object sender, EventArgs e)
			=> _ = Emulator switch
			{
				NES => OpenNesHawkGraphicsSettingsDialog(GetSettingsAdapterForLoadedCore<NES>()),
				SubNESHawk => OpenNesHawkGraphicsSettingsDialog(GetSettingsAdapterForLoadedCore<SubNESHawk>()),
				QuickNES => OpenQuickNesGraphicsSettingsDialog(GetSettingsAdapterForLoadedCore<QuickNES>()),
				_ => DialogResult.None
			};

		private void NesSoundChannelsMenuItem_Click(object sender, EventArgs e)
			=> Tools.Load<NESSoundConfig>();

		private DialogResult OpenNesHawkVSSettingsDialog(ISettingsAdapter settable)
		{
			using NesVsSettings form = new(settable);
			return this.ShowDialogWithTempMute(form);
		}

		private void VsSettingsMenuItem_Click(object sender, EventArgs e)
			=> _ = Emulator switch
			{
				NES { IsVS: true } => OpenNesHawkVSSettingsDialog(GetSettingsAdapterForLoadedCore<NES>()),
				SubNESHawk { IsVs: true } => OpenNesHawkVSSettingsDialog(GetSettingsAdapterForLoadedCore<SubNESHawk>()),
				_ => DialogResult.None
			};

		private DialogResult OpenNesHawkAdvancedSettingsDialog(ISettingsAdapter settable, bool hasMapperProperties)
		{
			using NESSyncSettingsForm form = new(this, settable, hasMapperProperties: hasMapperProperties);
			return this.ShowDialogWithTempMute(form);
		}

		private void MovieSettingsMenuItem_Click(object sender, EventArgs e)
			=> _ = Emulator switch
			{
				NES nesHawk => OpenNesHawkAdvancedSettingsDialog(GetSettingsAdapterForLoadedCore<NES>(), nesHawk.HasMapperProperties),
				SubNESHawk subNESHawk => OpenNesHawkAdvancedSettingsDialog(GetSettingsAdapterForLoadedCore<SubNESHawk>(), subNESHawk.HasMapperProperties),
				_ => DialogResult.None
			};

		private void FdsEjectDiskMenuItem_Click(object sender, EventArgs e)
		{
			if (MovieSession.Movie.IsPlaying()) return;
			InputManager.ClickyVirtualPadController.Click("FDS Eject");
			AddOnScreenMessage("FDS disk ejected.");
		}

		private void FdsControlsMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			var boardName = Emulator.HasBoardInfo() ? Emulator.AsBoardInfo().BoardName : null;
			FdsEjectDiskMenuItem.Enabled = boardName == "FDS";
			while (FDSControlsMenuItem.DropDownItems.Count > 1) FDSControlsMenuItem.DropDownItems.RemoveAt(1);
			string button;
			for (int i = 0; Emulator.ControllerDefinition.BoolButtons.Contains(button = $"FDS Insert {i}"); i++)
			{
				var name = $"Disk {i / 2 + 1} Side {(char)(i % 2 + 'A')}";
				FdsInsertDiskMenuAdd($"Insert {name}", button, $"FDS {name} inserted.");
			}
		}

		private void VsInsertCoinP1MenuItem_Click(object sender, EventArgs e)
		{
			if (MovieSession.Movie.IsPlaying() || !LoadedCoreIsNesHawkInVSMode) return;
			InputManager.ClickyVirtualPadController.Click("Insert Coin P1");
			AddOnScreenMessage("P1 Coin Inserted");
		}

		private void VsInsertCoinP2MenuItem_Click(object sender, EventArgs e)
		{
			if (MovieSession.Movie.IsPlaying() || !LoadedCoreIsNesHawkInVSMode) return;
			InputManager.ClickyVirtualPadController.Click("Insert Coin P2");
			AddOnScreenMessage("P2 Coin Inserted");
		}

		private void VsServiceSwitchMenuItem_Click(object sender, EventArgs e)
		{
			if (MovieSession.Movie.IsPlaying() || !LoadedCoreIsNesHawkInVSMode) return;
			InputManager.ClickyVirtualPadController.Click("Service Switch");
			AddOnScreenMessage("Service Switch Pressed");
		}

		private void BarcodeReaderMenuItem_Click(object sender, EventArgs e)
			=> Tools.Load<BarcodeEntry>();

		private void NesSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			var boardName = Emulator.HasBoardInfo() ? Emulator.AsBoardInfo().BoardName : null;
			FDSControlsMenuItem.Enabled = boardName == "FDS";
			VSControlsMenuItem.Enabled = VSSettingsMenuItem.Enabled = LoadedCoreIsNesHawkInVSMode;
			NESSoundChannelsMenuItem.Enabled = Tools.IsAvailable<NESSoundConfig>();
			MovieSettingsMenuItem.Enabled = Emulator is NES or SubNESHawk && MovieSession.Movie.NotActive();
			NesControllerSettingsMenuItem.Enabled = Tools.IsAvailable<NesControllerSettings>() && MovieSession.Movie.NotActive();
			BarcodeReaderMenuItem.Enabled = ServiceInjector.IsAvailable(Emulator.ServiceProvider, typeof(BarcodeEntry));
			MusicRipperMenuItem.Enabled = Tools.IsAvailable<NESMusicRipper>();
		}



		private DialogResult OpenOctoshockGamepadSettingsDialog(ISettingsAdapter settable)
		{
			using PSXControllerConfig form = new(settable);
			return this.ShowDialogWithTempMute(form);
		}

		private void PsxControllerSettingsMenuItem_Click(object sender, EventArgs e)
			=> _ = Emulator switch
			{
				Octoshock => OpenOctoshockGamepadSettingsDialog(GetSettingsAdapterForLoadedCore<Octoshock>()),
				_ => DialogResult.None
			};

		private DialogResult OpenOctoshockSettingsDialog(ISettingsAdapter settable, OctoshockDll.eVidStandard vidStandard, Size vidSize)
			=> PSXOptions.DoSettingsDialog(Config, this, settable, vidStandard, vidSize);

		private void PsxOptionsMenuItem_Click(object sender, EventArgs e)
		{
			var result = Emulator switch
			{
				Octoshock octoshock => OpenOctoshockSettingsDialog(GetSettingsAdapterForLoadedCore<Octoshock>(), octoshock.SystemVidStandard, octoshock.CurrentVideoSize),
				_ => DialogResult.None
			};
			if (result.IsOk()) FrameBufferResized();
		}

		private void PsxDiscControlsMenuItem_Click(object sender, EventArgs e)
			=> Tools.Load<VirtualpadTool>().ScrollToPadSchema("Console");

		private void PsxHashDiscsMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is not IRedumpDiscChecksumInfo psx) return;
			using PSXHashDiscs form = new() { _psx = psx };
			this.ShowDialogWithTempMute(form);
		}

		private void PsxSubMenu_DropDownOpened(object sender, EventArgs e)
			=> PSXControllerSettingsMenuItem.Enabled = MovieSession.Movie.NotActive();



		private DialogResult OpenOldBSNESGamepadSettingsDialog(ISettingsAdapter settable)
		{
			using SNESControllerSettings form = new(settable);
			return this.ShowDialogWithTempMute(form);
		}

		private DialogResult OpenBSNESGamepadSettingsDialog(ISettingsAdapter settable)
		{
			using BSNESControllerSettings form = new(settable);
			return this.ShowDialogWithTempMute(form);
		}

		private void SNESControllerConfigurationMenuItem_Click(object sender, EventArgs e)
			=> _ = Emulator switch
			{
				LibsnesCore => OpenOldBSNESGamepadSettingsDialog(GetSettingsAdapterForLoadedCore<LibsnesCore>()),
				BsnesCore => OpenBSNESGamepadSettingsDialog(GetSettingsAdapterForLoadedCore<BsnesCore>()),
				SubBsnesCore => OpenBSNESGamepadSettingsDialog(GetSettingsAdapterForLoadedCore<SubBsnesCore>()),
				_ => DialogResult.None
			};

		private void SnesGfxDebuggerMenuItem_Click(object sender, EventArgs e)
			=> Tools.Load<SNESGraphicsDebugger>();

		private DialogResult OpenOldBSNESSettingsDialog(ISettingsAdapter settable)
			=> SNESOptions.DoSettingsDialog(this, settable);

		private DialogResult OpenBSNESSettingsDialog(ISettingsAdapter settable)
			=> BSNESOptions.DoSettingsDialog(this, settable);

		private void SnesOptionsMenuItem_Click(object sender, EventArgs e)
			=> _ = Emulator switch
			{
				LibsnesCore => OpenOldBSNESSettingsDialog(GetSettingsAdapterForLoadedCore<LibsnesCore>()),
				BsnesCore => OpenBSNESSettingsDialog(GetSettingsAdapterForLoadedCore<BsnesCore>()),
				SubBsnesCore => OpenBSNESSettingsDialog(GetSettingsAdapterForLoadedCore<SubBsnesCore>()),
				_ => DialogResult.None
			};

		private void SnesSubMenu_DropDownOpened(object sender, EventArgs e)
			=> SNESControllerConfigurationMenuItem.Enabled = MovieSession.Movie.NotActive();

		private void SNES_ToggleBg(int layer)
		{
			if (layer is < 1 or > 4) return; // should this throw?
			bool result = false;
			switch (Emulator)
			{
				case BsnesCore or SubBsnesCore:
				{
					var settingsProvider = Emulator.ServiceProvider
						.GetService<ISettable<BsnesCore.SnesSettings, BsnesCore.SnesSyncSettings>>();
					var s = settingsProvider.GetSettings();
					switch (layer)
					{
						case 1:
							result = s.ShowBG1_0 = s.ShowBG1_1 = !s.ShowBG1_1;
							break;
						case 2:
							result = s.ShowBG2_0 = s.ShowBG2_1 = !s.ShowBG2_1;
							break;
						case 3:
							result = s.ShowBG3_0 = s.ShowBG3_1 = !s.ShowBG3_1;
							break;
						case 4:
							result = s.ShowBG4_0 = s.ShowBG4_1 = !s.ShowBG4_1;
							break;
					}
					settingsProvider.PutSettings(s);
					break;
				}
				case LibsnesCore libsnes:
				{
					var s = libsnes.GetSettings();
					switch (layer)
					{
						case 1:
							result = s.ShowBG1_0 = s.ShowBG1_1 = !s.ShowBG1_1;
							break;
						case 2:
							result = s.ShowBG2_0 = s.ShowBG2_1 = !s.ShowBG2_1;
							break;
						case 3:
							result = s.ShowBG3_0 = s.ShowBG3_1 = !s.ShowBG3_1;
							break;
						case 4:
							result = s.ShowBG4_0 = s.ShowBG4_1 = !s.ShowBG4_1;
							break;
					}
					libsnes.PutSettings(s);
					break;
				}
				case Snes9x snes9X:
				{
					var s = snes9X.GetSettings();
					switch (layer)
					{
						case 1:
							result = s.ShowBg0 = !s.ShowBg0;
							break;
						case 2:
							result = s.ShowBg1 = !s.ShowBg1;
							break;
						case 3:
							result = s.ShowBg2 = !s.ShowBg2;
							break;
						case 4:
							result = s.ShowBg3 = !s.ShowBg3;
							break;
					}
					snes9X.PutSettings(s);
					break;
				}
				default:
					return;
			}
			AddOnScreenMessage($"BG {layer} Layer {(result ? "On" : "Off")}");
		}

		private void SNES_ToggleObj(int layer)
		{
			if (layer is < 1 or > 4) return; // should this throw?
			bool result = false;
			if (Emulator is LibsnesCore bsnes)
			{
				var s = bsnes.GetSettings();
				result = layer switch
				{
					1 => s.ShowOBJ_0 = !s.ShowOBJ_0,
					2 => s.ShowOBJ_1 = !s.ShowOBJ_1,
					3 => s.ShowOBJ_2 = !s.ShowOBJ_2,
					4 => s.ShowOBJ_3 = !s.ShowOBJ_3,
					_ => result
				};
				bsnes.PutSettings(s);
				AddOnScreenMessage($"Obj {layer} Layer {(result ? "On" : "Off")}");
			}
			else if (Emulator is Snes9x snes9X)
			{
				var s = snes9X.GetSettings();
				result = layer switch
				{
					1 => s.ShowSprites0 = !s.ShowSprites0,
					2 => s.ShowSprites1 = !s.ShowSprites1,
					3 => s.ShowSprites2 = !s.ShowSprites2,
					4 => s.ShowSprites3 = !s.ShowSprites3,
					_ => result
				};
				snes9X.PutSettings(s);
				AddOnScreenMessage($"Sprite {layer} Layer {(result ? "On" : "Off")}");
			}
		}



		private static readonly FilesystemFilterSet TI83ProgramFilesFSFilterSet = new(new FilesystemFilter("TI-83 Program Files", new[] { "83p", "8xp" }));

		private void Ti83KeypadMenuItem_Click(object sender, EventArgs e)
			=> Tools.Load<TI83KeyPad>();

		private void Ti83LoadTIFileMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is not TI83 ti83) return;
			var result = this.ShowFileOpenDialog(
				discardCWDChange: true,
				filter: TI83ProgramFilesFSFilterSet,
				initDir: Config.PathEntries.RomAbsolutePath(Emulator.SystemId));
			if (result is null) return;
			try
			{
				ti83.LinkPort.SendFileToCalc(File.OpenRead(result), true);
				return;
			}
			catch (IOException ex)
			{
				if (this.ShowMessageBox3(
					owner: null,
					icon: EMsgBoxIcon.Question,
					caption: "Upload Failed",
					text: $"Invalid file format. Reason: {ex.Message} \nForce transfer? This may cause the calculator to crash.")
						is not true)
				{
					return;
				}
			}
			ti83.LinkPort.SendFileToCalc(File.OpenRead(result), false);
		}

		private DialogResult OpenTI83PaletteSettingsDialog(ISettingsAdapter settable)
		{
			using TI83PaletteConfig form = new(settable);
			return this.ShowDialogWithTempMute(form);
		}

		private void Ti83PaletteMenuItem_Click(object sender, EventArgs e)
		{
			var result = Emulator switch
			{
				Emu83 => OpenTI83PaletteSettingsDialog(GetSettingsAdapterForLoadedCore<Emu83>()),
				TI83 => OpenTI83PaletteSettingsDialog(GetSettingsAdapterForLoadedCore<TI83>()),
				_ => DialogResult.None
			};
			if (result.IsOk()) AddOnScreenMessage("Palette settings saved");
		}



		private static readonly FilesystemFilterSet ZXStateFilesFSFilterSet = new(new FilesystemFilter("ZX-State files", new[] { "szx" }))
		{
			AppendAllFilesEntry = false,
		};

		private DialogResult OpenZXHawkSyncSettingsDialog(ISettingsAdapter settable)
		{
			using ZxSpectrumCoreEmulationSettings form = new(settable);
			return this.ShowDialogWithTempMute(form);
		}

		private void ZXSpectrumCoreEmulationSettingsMenuItem_Click(object sender, EventArgs e)
			=> _ = Emulator switch
			{
				ZXSpectrum => OpenZXHawkSyncSettingsDialog(GetSettingsAdapterForLoadedCore<ZXSpectrum>()),
				_ => DialogResult.None
			};

		private DialogResult OpenZXHawkGamepadSettingsDialog(ISettingsAdapter settable)
		{
			using ZxSpectrumJoystickSettings form = new(this, settable);
			return this.ShowDialogWithTempMute(form);
		}

		private void ZXSpectrumControllerConfigurationMenuItem_Click(object sender, EventArgs e)
			=> _ = Emulator switch
			{
				ZXSpectrum => OpenZXHawkGamepadSettingsDialog(GetSettingsAdapterForLoadedCore<ZXSpectrum>()),
				_ => DialogResult.None
			};

		private DialogResult OpenZXHawkAudioSettingsDialog(ISettingsAdapter settable)
		{
			using ZxSpectrumAudioSettings form = new(settable);
			return this.ShowDialogWithTempMute(form);
		}

		private void ZXSpectrumAudioSettingsMenuItem_Click(object sender, EventArgs e)
			=> _ = Emulator switch
			{
				ZXSpectrum => OpenZXHawkAudioSettingsDialog(GetSettingsAdapterForLoadedCore<ZXSpectrum>()),
				_ => DialogResult.None
			};

		private DialogResult OpenZXHawkSettingsDialog(ISettingsAdapter settable)
		{
			using ZxSpectrumNonSyncSettings form = new(settable);
			return this.ShowDialogWithTempMute(form);
		}

		private void ZXSpectrumNonSyncSettingsMenuItem_Click(object sender, EventArgs e)
			=> _ = Emulator switch
			{
				ZXSpectrum => OpenZXHawkSettingsDialog(GetSettingsAdapterForLoadedCore<ZXSpectrum>()),
				_ => DialogResult.None
			};

		private void ZXSpectrumTapesSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			ZXSpectrumTapesSubMenu.DropDownItems.Clear();
			if (Emulator is not ZXSpectrum speccy) return;
			EventHandler clickHandler = (clickSender, _) =>
			{
				if (!object.ReferenceEquals(speccy, Emulator)) return;
				speccy._machine.TapeMediaIndex = (int) ((ToolStripItem) clickSender).Tag;
			};
			var tapeMediaIndex = speccy._machine.TapeMediaIndex;
			for (int i = 0; i < speccy._tapeInfo.Count; i++)
			{
				ToolStripMenuItem menuItem = new()
				{
					Checked = i == tapeMediaIndex,
					Tag = i,
					Text = $"{i}: {speccy._tapeInfo[i].Name}",
				};
				menuItem.Click += clickHandler;
				ZXSpectrumTapesSubMenu.DropDownItems.Add(menuItem);
			}
		}

		private void ZXSpectrumDisksSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			ZXSpectrumDisksSubMenu.DropDownItems.Clear();
			if (Emulator is not ZXSpectrum speccy) return;
			EventHandler clickHandler = (clickSender, _) =>
			{
				if (!object.ReferenceEquals(speccy, Emulator)) return;
				speccy._machine.DiskMediaIndex = (int) ((ToolStripItem) clickSender).Tag;
			};
			var diskMediaIndex = speccy._machine.DiskMediaIndex;
			for (int i = 0; i < speccy._diskInfo.Count; i++)
			{
				ToolStripMenuItem menuItem = new()
				{
					Checked = i == diskMediaIndex,
					Tag = i,
					Text = $"{i}: {speccy._diskInfo[i].Name}",
				};
				menuItem.Click += clickHandler;
				ZXSpectrumDisksSubMenu.DropDownItems.Add(menuItem);
			}
		}

		private void ZXSpectrumExportSnapshotMenuItemMenuItem_Click(object sender, EventArgs e)
		{
			try
			{
				var result = this.ShowFileSaveDialog(
					discardCWDChange: true,
					fileExt: "szx",
//					SupportMultiDottedExtensions = true, // I think this should be enabled globally if we're going to do it --yoshi
					filter: ZXStateFilesFSFilterSet,
					initDir: Config.PathEntries.ToolsAbsolutePath());
				if (result is not null)
				{
					var speccy = (ZXSpectrum)Emulator;
					var snap = speccy.GetSZXSnapshot();
					File.WriteAllBytes(result, snap);
				}
			}
			catch (Exception)
			{
				// ignored
			}
		}

		private void ZXSpectrumMediaMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			if (Emulator is not ZXSpectrum speccy) return;
			ZXSpectrumTapesSubMenu.Enabled = speccy._tapeInfo.Count > 0;
			ZXSpectrumDisksSubMenu.Enabled = speccy._diskInfo.Count > 0;
		}



		private enum VSystemCategory : int
		{
			Consoles = 0,
			Handhelds = 1,
			PCs = 2,
			Other = 3,
		}

		private IReadOnlyCollection<ToolStripItem> CreateCoreSettingsSubmenus(bool includeDupes = false)
		{
			static ToolStripMenuItemEx CreateSettingsItem(string text, EventHandler onClick)
			{
				ToolStripMenuItemEx menuItem = new() { Text = text };
				menuItem.Click += onClick;
				return menuItem;
			}
			ToolStripMenuItemEx CreateGenericCoreConfigItem<T>(string coreName)
				where T : IEmulator
				=> CreateSettingsItem("Settings...", (_, _) => OpenGenericCoreConfigFor<T>($"{coreName} Settings"));
			ToolStripMenuItemEx CreateGenericNymaCoreConfigItem<T>(string coreName, Func<CoreComm, NymaCore.NymaSettingsInfo> getCachedSettingsInfo)
				where T : NymaCore
				=> CreateSettingsItem(
					"Settings...",
					(_, _) => GenericCoreConfig.DoNymaDialogFor(
						this,
						GetSettingsAdapterFor<T>(),
						$"{coreName} Settings",
						getCachedSettingsInfo(CreateCoreComm()),
						isMovieActive: MovieSession.Movie.IsActive()));
			ToolStripMenuItemEx CreateCoreSubmenu(VSystemCategory cat, string coreName, params ToolStripItem[] items)
			{
				ToolStripMenuItemEx submenu = new() { Tag = cat, Text = coreName };
				submenu.DropDownItems.AddRange(items);
				return submenu;
			}

			List<ToolStripItem> items = new();
			// cores A-Z by value of `CoreAttribute.CoreName`

			// A7800Hawk
			var a7800HawkGamepadSettingsItem = CreateSettingsItem("Controller Settings...", (_, _) => OpenA7800HawkGamepadSettingsDialog(GetSettingsAdapterFor<A7800Hawk>()));
			var a7800HawkFilterSettingsItem = CreateSettingsItem("Filter Settings...", (_, _) => OpenA7800HawkFilterSettingsDialog(GetSettingsAdapterFor<A7800Hawk>()));
			var a7800HawkSubmenu = CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.A7800Hawk, a7800HawkGamepadSettingsItem, a7800HawkFilterSettingsItem);
			a7800HawkSubmenu.DropDownOpened += (_, _) => a7800HawkGamepadSettingsItem.Enabled = a7800HawkFilterSettingsItem.Enabled = MovieSession.Movie.NotActive() || Emulator is not A7800Hawk;
			items.Add(a7800HawkSubmenu);

			// Ares64
			var ares64AnalogConstraintItem = CreateSettingsItem("Circular Analog Range", N64CircularAnalogRangeMenuItem_Click);
			var ares64Submenu = CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.Ares64, CreateGenericCoreConfigItem<Ares64>(CoreNames.Ares64));
			ares64Submenu.DropDownOpened += (_, _) => ares64AnalogConstraintItem.Checked = Config.N64UseCircularAnalogConstraint;
			items.Add(ares64Submenu);

			// Atari2600Hawk
			items.Add(CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.Atari2600Hawk, CreateGenericCoreConfigItem<Atari2600>(CoreNames.Atari2600Hawk)));

			// BSNES
			var oldBSNESGamepadSettingsItem = CreateSettingsItem("Controller Configuration...", (_, _) => OpenOldBSNESGamepadSettingsDialog(GetSettingsAdapterFor<LibsnesCore>()));
			var oldBSNESSettingsItem = CreateSettingsItem("Options...", (_, _) => OpenOldBSNESSettingsDialog(GetSettingsAdapterFor<LibsnesCore>()));
			var oldBSNESSubmenu = CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.Bsnes, oldBSNESGamepadSettingsItem, oldBSNESSettingsItem);
			oldBSNESSubmenu.DropDownOpened += (_, _) => oldBSNESGamepadSettingsItem.Enabled = MovieSession.Movie.NotActive() || Emulator is not LibsnesCore;
			items.Add(oldBSNESSubmenu);

			// BSNESv115+
			var bsnesGamepadSettingsItem = CreateSettingsItem("Controller Configuration...", (_, _) => OpenBSNESGamepadSettingsDialog(GetSettingsAdapterFor<BsnesCore>()));
			var bsnesSettingsItem = CreateSettingsItem("Options...", (_, _) => OpenBSNESSettingsDialog(GetSettingsAdapterFor<BsnesCore>()));
			var bsnesSubmenu = CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.Bsnes115, bsnesGamepadSettingsItem, bsnesSettingsItem);
			bsnesSubmenu.DropDownOpened += (_, _) => bsnesGamepadSettingsItem.Enabled = MovieSession.Movie.NotActive() || Emulator is not BsnesCore;
			items.Add(bsnesSubmenu);

			// C64Hawk
			items.Add(CreateCoreSubmenu(VSystemCategory.PCs, CoreNames.C64Hawk, CreateSettingsItem("Settings...", (_, _) => OpenC64HawkSettingsDialog())));

			// ChannelFHawk
			items.Add(CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.ChannelFHawk, CreateGenericCoreConfigItem<ChannelF>(CoreNames.ChannelFHawk)));

			// ColecoHawk
			var colecoHawkGamepadSettingsItem = CreateSettingsItem("Controller Settings...", (_, _) => OpenColecoHawkGamepadSettingsDialog(GetSettingsAdapterFor<ColecoVision>()));
			var colecoHawkSkipBIOSItem = CreateSettingsItem("Skip BIOS intro (When Applicable)", (sender, _) => ColecoHawkSetSkipBIOSIntro(!((ToolStripMenuItem) sender).Checked, GetSettingsAdapterFor<ColecoVision>()));
			var colecoHawkUseSGMItem = CreateSettingsItem("Use the Super Game Module", (sender, _) => ColecoHawkSetSuperGameModule(!((ToolStripMenuItem) sender).Checked, GetSettingsAdapterFor<ColecoVision>()));
			var colecoHawkSubmenu = CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.ColecoHawk, colecoHawkGamepadSettingsItem, colecoHawkSkipBIOSItem, colecoHawkUseSGMItem);
			colecoHawkSubmenu.DropDownOpened += (_, _) =>
			{
				var ss = (ColecoVision.ColecoSyncSettings) GetSettingsAdapterFor<ColecoVision>().GetSyncSettings();
				colecoHawkGamepadSettingsItem.Enabled = MovieSession.Movie.NotActive() || Emulator is not ColecoVision;
				colecoHawkSkipBIOSItem.Checked = ss.SkipBiosIntro;
				colecoHawkUseSGMItem.Checked = ss.UseSGM;
			};
			items.Add(colecoHawkSubmenu);

			// CPCHawk
			items.Add(CreateCoreSubmenu(
				VSystemCategory.PCs,
				CoreNames.CPCHawk,
				CreateSettingsItem("Core Emulation Settings...", (_, _) => OpenCPCHawkSyncSettingsDialog(GetSettingsAdapterFor<AmstradCPC>())),
				CreateSettingsItem("Audio Settings...", (_, _) => OpenCPCHawkAudioSettingsDialog(GetSettingsAdapterFor<AmstradCPC>())),
				CreateSettingsItem("Non-Sync Settings...", (_, _) => OpenCPCHawkSettingsDialog(GetSettingsAdapterFor<AmstradCPC>()))));

			// Cygne
			items.Add(CreateCoreSubmenu(VSystemCategory.Handhelds, CoreNames.Cygne, CreateGenericCoreConfigItem<WonderSwan>(CoreNames.Cygne)));

			// DOSBox
			items.Add(CreateCoreSubmenu(VSystemCategory.PCs, CoreNames.DOSBox, CreateGenericCoreConfigItem<DOSBox>(CoreNames.DOSBox)));

			// DSDA-Doom
			items.Add(CreateCoreSubmenu(VSystemCategory.Other, CoreNames.DSDA, CreateGenericCoreConfigItem<DSDA>(CoreNames.DSDA)));

			// Emu83
			items.Add(CreateCoreSubmenu(VSystemCategory.Other, CoreNames.Emu83, CreateSettingsItem("Palette...", (_, _) => OpenTI83PaletteSettingsDialog(GetSettingsAdapterFor<Emu83>()))));

			// Encore
			items.Add(CreateCoreSubmenu(VSystemCategory.Handhelds, CoreNames.Encore, CreateGenericCoreConfigItem<Encore>(CoreNames.Encore)));

			// Faust
			items.Add(CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.Faust, CreateGenericNymaCoreConfigItem<Faust>(CoreNames.Faust, Faust.CachedSettingsInfo)));

			// Gambatte
			items.Add(CreateCoreSubmenu(VSystemCategory.Handhelds, CoreNames.Gambatte, CreateSettingsItem("Settings...", (_, _) => OpenGambatteSettingsDialog(GetSettingsAdapterFor<Gameboy>()))));
			if (includeDupes) items.Add(CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.Gambatte, CreateSettingsItem("Settings...", (_, _) => OpenGambatteSettingsDialog(GetSettingsAdapterFor<Gameboy>()))));

			// GambatteLink
			items.Add(CreateCoreSubmenu(VSystemCategory.Handhelds, CoreNames.GambatteLink, CreateSettingsItem("Settings...", (_, _) => OpenGambatteLinkSettingsDialog(GetSettingsAdapterFor<GambatteLink>()))));

			// GBHawk
			items.Add(CreateCoreSubmenu(VSystemCategory.Handhelds, CoreNames.GbHawk, CreateSettingsItem("Settings...", (_, _) => OpenGBHawkSettingsDialog())));

			// GBHawkLink
			items.Add(CreateCoreSubmenu(VSystemCategory.Handhelds, CoreNames.GBHawkLink, CreateGenericCoreConfigItem<GBHawkLink>(CoreNames.GBHawkLink)));

			// GBHawkLink3x
			items.Add(CreateCoreSubmenu(VSystemCategory.Handhelds, CoreNames.GBHawkLink3x, CreateGenericCoreConfigItem<GBHawkLink3x>(CoreNames.GBHawkLink3x)));

			// GBHawkLink4x
			items.Add(CreateCoreSubmenu(VSystemCategory.Handhelds, CoreNames.GBHawkLink4x, CreateGenericCoreConfigItem<GBHawkLink4x>(CoreNames.GBHawkLink4x)));

			// Genplus-gx
			items.Add(CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.Gpgx, CreateGenericCoreConfigItem<GPGX>(CoreNames.Gpgx)));

			// GGHawkLink
			items.Add(CreateCoreSubmenu(VSystemCategory.Handhelds, CoreNames.GGHawkLink, CreateGenericCoreConfigItem<GGHawkLink>(CoreNames.GGHawkLink)));

			// Handy
			items.Add(CreateCoreSubmenu(VSystemCategory.Handhelds, CoreNames.Handy, CreateGenericCoreConfigItem<Lynx>(CoreNames.Handy))); // as Handy doesn't implement `ISettable<,>`, this opens an empty `GenericCoreConfig`, which is dumb, but matches the existing behaviour

			// HyperNyma
			items.Add(CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.HyperNyma, CreateGenericNymaCoreConfigItem<HyperNyma>(CoreNames.HyperNyma, HyperNyma.CachedSettingsInfo)));

			// IntelliHawk
			var intelliHawkGamepadSettingsItem = CreateSettingsItem("Controller Settings...", (_, _) => OpenIntelliHawkGamepadSettingsDialog(GetSettingsAdapterFor<Intellivision>()));
			var intelliHawkSubmenu = CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.IntelliHawk, intelliHawkGamepadSettingsItem);
			intelliHawkSubmenu.DropDownOpened += (_, _) => intelliHawkGamepadSettingsItem.Enabled = MovieSession.Movie.NotActive() || Emulator is not Intellivision;
			items.Add(intelliHawkSubmenu);

			// Libretro
			items.Add(CreateCoreSubmenu(
				VSystemCategory.Other,
				CoreNames.Libretro,
				CreateGenericCoreConfigItem<LibretroHost>(CoreNames.Libretro))); // as Libretro doesn't implement `ISettable<,>`, this opens an empty `GenericCoreConfig`, which is dumb, but matches the existing behaviour

			// MAME
			var mameSettingsItem = CreateSettingsItem("Settings...", (_, _) => OpenGenericCoreConfig());
			var mameSubmenu = CreateCoreSubmenu(VSystemCategory.Other, CoreNames.MAME, mameSettingsItem);
			mameSubmenu.DropDownOpened += (_, _) => mameSettingsItem.Enabled = Emulator is MAME;
			items.Add(mameSubmenu);

			// melonDS
			items.Add(CreateCoreSubmenu(VSystemCategory.Handhelds, CoreNames.MelonDS, CreateGenericCoreConfigItem<NDS>(CoreNames.MelonDS)));

			// mGBA
			items.Add(CreateCoreSubmenu(VSystemCategory.Handhelds, CoreNames.Mgba, CreateGenericCoreConfigItem<MGBAHawk>(CoreNames.Mgba)));

			// MSXHawk
			items.Add(CreateCoreSubmenu(VSystemCategory.PCs, CoreNames.MSXHawk, CreateGenericCoreConfigItem<MSX>(CoreNames.MSXHawk)));

			// Mupen64Plus
			var mupen64PlusGraphicsSettingsItem = CreateSettingsItem("Video Plugins...", N64PluginSettingsMenuItem_Click);
			var mupen64PlusGamepadSettingsItem = CreateSettingsItem("Controller Settings...", (_, _) => OpenMupen64PlusGamepadSettingsDialog(GetSettingsAdapterFor<N64>()));
			var mupen64PlusAnalogConstraintItem = CreateSettingsItem("Circular Analog Range", N64CircularAnalogRangeMenuItem_Click);
			var mupen64PlusMupenStyleLagFramesItem = CreateSettingsItem("Mupen Style Lag Frames", (sender, _) => Mupen64PlusSetMupenStyleLag(!((ToolStripMenuItem) sender).Checked, GetSettingsAdapterFor<N64>()));
			var mupen64PlusUseExpansionSlotItem = CreateSettingsItem("Use Expansion Slot", (sender, _) => Mupen64PlusSetUseExpansionSlot(!((ToolStripMenuItem) sender).Checked, GetSettingsAdapterFor<N64>()));
			var mupen64PlusSubmenu = CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.Mupen64Plus, mupen64PlusGraphicsSettingsItem, mupen64PlusGamepadSettingsItem, mupen64PlusAnalogConstraintItem, mupen64PlusMupenStyleLagFramesItem, mupen64PlusUseExpansionSlotItem);
			mupen64PlusSubmenu.DropDownOpened += (_, _) =>
			{
				var settable = GetSettingsAdapterFor<N64>();
				var s = (N64Settings) settable.GetSettings();
				var isMovieActive = MovieSession.Movie.IsActive();
				var mupen64Plus = Emulator as N64;
				var loadedCoreIsMupen64Plus = mupen64Plus is not null;
				mupen64PlusGraphicsSettingsItem.Enabled = !loadedCoreIsMupen64Plus || !isMovieActive;
				mupen64PlusGamepadSettingsItem.Enabled = !loadedCoreIsMupen64Plus || !isMovieActive;
				mupen64PlusAnalogConstraintItem.Checked = Config.N64UseCircularAnalogConstraint;
				mupen64PlusMupenStyleLagFramesItem.Checked = s.UseMupenStyleLag;
				if (loadedCoreIsMupen64Plus)
				{
					mupen64PlusUseExpansionSlotItem.Checked = mupen64Plus.UsingExpansionSlot;
					mupen64PlusUseExpansionSlotItem.Enabled = !mupen64Plus.IsOverridingUserExpansionSlotSetting;
				}
				else
				{
					mupen64PlusUseExpansionSlotItem.Checked = !((N64SyncSettings) settable.GetSyncSettings()).DisableExpansionSlot;
					mupen64PlusUseExpansionSlotItem.Enabled = true;
				}
			};
			items.Add(mupen64PlusSubmenu);

			// NeoPop
			items.Add(CreateCoreSubmenu(VSystemCategory.Handhelds, CoreNames.NeoPop, CreateGenericNymaCoreConfigItem<NeoGeoPort>(CoreNames.NeoPop, NeoGeoPort.CachedSettingsInfo)));

			// NesHawk
			var nesHawkGamepadSettingsItem = CreateSettingsItem("Controller Settings...", (_, _) => OpenNesHawkGamepadSettingsDialog(GetSettingsAdapterFor<NES>()));
			var nesHawkVSSettingsItem = CreateSettingsItem("VS Settings...", (_, _) => OpenNesHawkVSSettingsDialog(GetSettingsAdapterFor<NES>()));
			var nesHawkAdvancedSettingsItem = CreateSettingsItem("Advanced Settings...", (_, _) => OpenNesHawkAdvancedSettingsDialog(GetSettingsAdapterFor<NES>(), Emulator is not NES nesHawk || nesHawk.HasMapperProperties));
			var nesHawkSubmenu = CreateCoreSubmenu(
				VSystemCategory.Consoles,
				CoreNames.NesHawk,
				nesHawkGamepadSettingsItem,
				CreateSettingsItem("Graphics Settings...", (_, _) => OpenNesHawkGraphicsSettingsDialog(GetSettingsAdapterFor<NES>())),
				nesHawkVSSettingsItem,
				nesHawkAdvancedSettingsItem);
			nesHawkSubmenu.DropDownOpened += (_, _) =>
			{
				var nesHawk = Emulator as NES;
				var canEditSyncSettings = nesHawk is null || MovieSession.Movie.NotActive();
				nesHawkGamepadSettingsItem.Enabled = canEditSyncSettings && Tools.IsAvailable<NesControllerSettings>();
				nesHawkVSSettingsItem.Enabled = nesHawk?.IsVS is null or true;
				nesHawkAdvancedSettingsItem.Enabled = canEditSyncSettings;
			};
			items.Add(nesHawkSubmenu);

			// Nymashock
			items.Add(CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.Nymashock, CreateGenericNymaCoreConfigItem<Nymashock>(CoreNames.Nymashock, Nymashock.CachedSettingsInfo)));

			// O2Hawk
			items.Add(CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.O2Hawk, CreateGenericCoreConfigItem<O2Hawk>(CoreNames.O2Hawk)));

			// Octoshock
			var octoshockGamepadSettingsItem = CreateSettingsItem("Controller / Memcard Settings...", (_, _) => OpenOctoshockGamepadSettingsDialog(GetSettingsAdapterFor<Octoshock>()));
			var octoshockSettingsItem = CreateSettingsItem("Options...", PsxOptionsMenuItem_Click);
			// using init buffer sizes here (in practice, they don't matter here, but might as well)
			var octoshockNTSCSettingsItem = CreateSettingsItem("Options (as NTSC)...", (_, _) => OpenOctoshockSettingsDialog(GetSettingsAdapterFor<Octoshock>(), OctoshockDll.eVidStandard.NTSC, new(280, 240)));
			var octoshockPALSettingsItem = CreateSettingsItem("Options (as PAL)...", (_, _) => OpenOctoshockSettingsDialog(GetSettingsAdapterFor<Octoshock>(), OctoshockDll.eVidStandard.PAL, new(280, 288)));
			var octoshockSubmenu = CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.Octoshock, octoshockGamepadSettingsItem, octoshockSettingsItem, octoshockNTSCSettingsItem, octoshockPALSettingsItem);
			octoshockSubmenu.DropDownOpened += (_, _) =>
			{
				var loadedCoreIsOctoshock = Emulator is Octoshock;
				octoshockGamepadSettingsItem.Enabled = !loadedCoreIsOctoshock || MovieSession.Movie.NotActive();
				octoshockSettingsItem.Visible = loadedCoreIsOctoshock;
				octoshockNTSCSettingsItem.Visible = octoshockPALSettingsItem.Visible = !loadedCoreIsOctoshock;
			};
			items.Add(octoshockSubmenu);

			// PCEHawk
			items.Add(CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.PceHawk, CreateGenericCoreConfigItem<PCEngine>(CoreNames.PceHawk)));

			// PicoDrive
			items.Add(CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.PicoDrive, CreateGenericCoreConfigItem<PicoDrive>(CoreNames.PicoDrive)));

			// QuickNes
			var quickNesGamepadSettingsItem = CreateSettingsItem("Controller Settings...", (_, _) => OpenQuickNesGamepadSettingsDialog(GetSettingsAdapterFor<QuickNES>()));
			var quickNesSubmenu = CreateCoreSubmenu(
				VSystemCategory.Consoles,
				CoreNames.QuickNes,
				quickNesGamepadSettingsItem,
				CreateSettingsItem("Graphics Settings...", (_, _) => OpenQuickNesGraphicsSettingsDialog(GetSettingsAdapterFor<QuickNES>())));
			quickNesSubmenu.DropDownOpened += (_, _) => quickNesGamepadSettingsItem.Enabled = (MovieSession.Movie.NotActive() || Emulator is not QuickNES) && Tools.IsAvailable<NesControllerSettings>();
			items.Add(quickNesSubmenu);

			// SameBoy
			items.Add(CreateCoreSubmenu(
				VSystemCategory.Handhelds,
				CoreNames.Sameboy,
				CreateSettingsItem("Settings...", (_, _) => OpenSameBoySettingsDialog()),
				CreateSettingsItem("Choose Custom Palette...", (_, _) => OpenSameBoyPaletteSettingsDialog(GetSettingsAdapterFor<Sameboy>()))));

			// Saturnus
			items.Add(CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.Saturnus, CreateGenericNymaCoreConfigItem<Saturnus>(CoreNames.Saturnus, Saturnus.CachedSettingsInfo)));

			// SMSHawk
			items.Add(CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.SMSHawk, CreateGenericCoreConfigItem<SMS>(CoreNames.SMSHawk)));
			if (includeDupes) items.Add(CreateCoreSubmenu(VSystemCategory.Handhelds, CoreNames.SMSHawk, CreateGenericCoreConfigItem<SMS>(CoreNames.SMSHawk)));

			// Snes9x
			items.Add(CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.Snes9X, CreateGenericCoreConfigItem<Snes9x>(CoreNames.Snes9X)));

			// Stella
			items.Add(CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.Stella, CreateGenericCoreConfigItem<Stella>(CoreNames.Stella)));

			// SubBSNESv115+
			var subBsnesGamepadSettingsItem = CreateSettingsItem("Controller Configuration...", (_, _) => OpenBSNESGamepadSettingsDialog(GetSettingsAdapterFor<SubBsnesCore>()));
			var subBsnesSettingsItem = CreateSettingsItem("Options...", (_, _) => OpenBSNESSettingsDialog(GetSettingsAdapterFor<SubBsnesCore>()));
			var subBsnesSubmenu = CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.SubBsnes115, subBsnesGamepadSettingsItem, subBsnesSettingsItem);
			subBsnesSubmenu.DropDownOpened += (_, _) => subBsnesGamepadSettingsItem.Enabled = MovieSession.Movie.NotActive() || Emulator is not SubBsnesCore;
			items.Add(subBsnesSubmenu);

			// SubGBHawk
			items.Add(CreateCoreSubmenu(VSystemCategory.Handhelds, CoreNames.SubGbHawk, CreateSettingsItem("Settings...", (_, _) => OpenSubGBHawkSettingsDialog())));

			// SubNESHawk
			var subNESHawkGamepadSettingsItem = CreateSettingsItem("Controller Settings...", (_, _) => OpenNesHawkGamepadSettingsDialog(GetSettingsAdapterFor<SubNESHawk>()));
			var subNESHawkVSSettingsItem = CreateSettingsItem("VS Settings...", (_, _) => OpenNesHawkVSSettingsDialog(GetSettingsAdapterFor<SubNESHawk>()));
			var subNESHawkAdvancedSettingsItem = CreateSettingsItem("Advanced Settings...", (_, _) => OpenNesHawkAdvancedSettingsDialog(GetSettingsAdapterFor<SubNESHawk>(), Emulator is not SubNESHawk subNESHawk || subNESHawk.HasMapperProperties));
			var subNESHawkSubmenu = CreateCoreSubmenu(
				VSystemCategory.Consoles,
				CoreNames.SubNesHawk,
				subNESHawkGamepadSettingsItem,
				CreateSettingsItem("Graphics Settings...", (_, _) => OpenNesHawkGraphicsSettingsDialog(GetSettingsAdapterFor<SubNESHawk>())),
				subNESHawkVSSettingsItem,
				subNESHawkAdvancedSettingsItem);
			subNESHawkSubmenu.DropDownOpened += (_, _) =>
			{
				var subNESHawk = Emulator as SubNESHawk;
				var canEditSyncSettings = subNESHawk is null || MovieSession.Movie.NotActive();
				subNESHawkGamepadSettingsItem.Enabled = canEditSyncSettings && Tools.IsAvailable<NesControllerSettings>();
				subNESHawkVSSettingsItem.Enabled = subNESHawk?.IsVs is null or true;
				subNESHawkAdvancedSettingsItem.Enabled = canEditSyncSettings;
			};
			items.Add(subNESHawkSubmenu);

			// T. S. T.
			items.Add(CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.TST, CreateGenericNymaCoreConfigItem<Tst>(CoreNames.TST, Tst.CachedSettingsInfo)));

			// TI83Hawk
			items.Add(CreateCoreSubmenu(VSystemCategory.Other, CoreNames.TI83Hawk, CreateSettingsItem("Palette...", (_, _) => OpenTI83PaletteSettingsDialog(GetSettingsAdapterFor<TI83>()))));

			// TIC80
			items.Add(CreateCoreSubmenu(VSystemCategory.PCs, CoreNames.TIC80, CreateGenericCoreConfigItem<TIC80>(CoreNames.TIC80)));

			// TurboNyma
			items.Add(CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.TurboNyma, CreateGenericNymaCoreConfigItem<TurboNyma>(CoreNames.TurboNyma, TurboNyma.CachedSettingsInfo)));

			// UAE
			items.Add(CreateCoreSubmenu(VSystemCategory.PCs, CoreNames.UAE, CreateGenericCoreConfigItem<UAE>(CoreNames.UAE)));

			// uzem
			items.Add(CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.Uzem, CreateGenericCoreConfigItem<Uzem>(CoreNames.Uzem))); // as uzem doesn't implement `ISettable<,>`, this opens an empty `GenericCoreConfig`, which is dumb, but matches the existing behaviour

			// VectrexHawk
			items.Add(CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.VectrexHawk, CreateGenericCoreConfigItem<VectrexHawk>(CoreNames.VectrexHawk)));

			// Virtu
			items.Add(CreateCoreSubmenu(VSystemCategory.PCs, CoreNames.Virtu, CreateSettingsItem("Settings...", (_, _) => OpenVirtuSettingsDialog())));

			// Virtual Boyee
			items.Add(CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.VirtualBoyee, CreateGenericNymaCoreConfigItem<VirtualBoyee>(CoreNames.VirtualBoyee, VirtualBoyee.CachedSettingsInfo)));

			// Virtual Jaguar
			items.Add(CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.VirtualJaguar, CreateGenericCoreConfigItem<VirtualJaguar>(CoreNames.VirtualJaguar)));

			// ZXHawk
			items.Add(CreateCoreSubmenu(
				VSystemCategory.PCs,
				CoreNames.ZXHawk,
				CreateSettingsItem("Core Emulation Settings...", (_, _) => OpenZXHawkSyncSettingsDialog(GetSettingsAdapterFor<ZXSpectrum>())),
				CreateSettingsItem("Joystick Configuration...", (_, _) => OpenZXHawkGamepadSettingsDialog(GetSettingsAdapterFor<ZXSpectrum>())),
				CreateSettingsItem("Audio Settings...", (_, _) => OpenZXHawkAudioSettingsDialog(GetSettingsAdapterFor<ZXSpectrum>())),
				CreateSettingsItem("Non-Sync Settings...", (_, _) => OpenZXHawkSettingsDialog(GetSettingsAdapterFor<ZXSpectrum>()))));

			return items;
		}

		private void HandlePlatformMenus()
		{
			if (GenericCoreSubMenu.Visible)
			{
				var i = GenericCoreSubMenu.Text.IndexOf('&');
				if (i != -1) AvailableAccelerators.Add(GenericCoreSubMenu.Text[i + 1]);
			}
			NullHawkVSysSubmenu.Visible = false;
			GenericCoreSubMenu.Visible = false;
			TI83SubMenu.Visible = false;
			NESSubMenu.Visible = false;
			GBSubMenu.Visible = false;
			A7800SubMenu.Visible = false;
			SNESSubMenu.Visible = false;
			PSXSubMenu.Visible = false;
			ColecoSubMenu.Visible = false;
			N64SubMenu.Visible = false;
			Ares64SubMenu.Visible = false;
			GBLSubMenu.Visible = false;
			AppleSubMenu.Visible = false;
			C64SubMenu.Visible = false;
			IntvSubMenu.Visible = false;
			zXSpectrumToolStripMenuItem.Visible = false;
			amstradCPCToolStripMenuItem.Visible = false;

			var sysID = Emulator.SystemId;
			switch (sysID)
			{
				case VSystemID.Raw.NULL:
					NullHawkVSysSubmenu.Visible = true;
					break;
				case VSystemID.Raw.A78:
					A7800SubMenu.Visible = true;
					break;
				case VSystemID.Raw.AmstradCPC:
					amstradCPCToolStripMenuItem.Visible = true;
					break;
				case VSystemID.Raw.AppleII:
					AppleSubMenu.Visible = true;
					break;
				case VSystemID.Raw.C64:
					C64SubMenu.Visible = true;
					break;
				case VSystemID.Raw.Coleco:
					ColecoSubMenu.Visible = true;
					break;
				case VSystemID.Raw.INTV:
					IntvSubMenu.Visible = true;
					break;
				case VSystemID.Raw.N64 when Emulator is N64:
					N64SubMenu.Visible = true;
					break;
				case VSystemID.Raw.N64 when Emulator is Ares64:
					Ares64SubMenu.Visible = true;
					break;
				case VSystemID.Raw.NES:
					NESSubMenu.Visible = true;
					break;
				case VSystemID.Raw.PSX when Emulator is Octoshock:
					PSXSubMenu.Visible = true;
					break;
				case VSystemID.Raw.TI83:
					TI83SubMenu.Visible = true;
					LoadTIFileMenuItem.Visible = Emulator is TI83;
					break;
				case VSystemID.Raw.ZXSpectrum:
					zXSpectrumToolStripMenuItem.Visible = true;
					break;
				case VSystemID.Raw.GBL when Emulator is GambatteLink:
					GBLSubMenu.Visible = true;
					break;
				case VSystemID.Raw.GB:
				case VSystemID.Raw.GBC:
				case VSystemID.Raw.SGB when Emulator is Gameboy:
					GBSubMenu.Visible = true;
					SameBoyColorChooserMenuItem.Visible = Emulator is Sameboy { IsCGBMode: false }; // palette config only works in DMG mode
					break;
				case VSystemID.Raw.SNES when Emulator is LibsnesCore oldBSNES: // doesn't use "SGB" sysID, always "SNES"
					SNESSubMenu.Text = oldBSNES.IsSGB ? "&SGB" : "&SNES";
					SNESSubMenu.Visible = true;
					break;
				case var _ when Emulator is BsnesCore or SubBsnesCore:
					SNESSubMenu.Text = $"&{sysID}";
					SNESSubMenu.Visible = true;
					break;
				default:
					DisplayDefaultCoreMenu();
					break;
			}
		}

	}
}
