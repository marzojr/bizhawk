using System.Collections.Generic;
using System.Linq;

using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.Arcades.MAME;
using BizHawk.Emulation.Cores.Atari.Atari2600;
using BizHawk.Emulation.Cores.Computers.MSX;
using BizHawk.Emulation.Cores.Consoles.Nintendo.NDS;
using BizHawk.Emulation.Cores.Consoles.O2Hawk;
using BizHawk.Emulation.Cores.Consoles.Sega.gpgx;
using BizHawk.Emulation.Cores.Nintendo.BSNES;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.Nintendo.GBA;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.Nintendo.Sameboy;
using BizHawk.Emulation.Cores.Nintendo.SNES9X;
using BizHawk.Emulation.Cores.Nintendo.SubGBHawk;
using BizHawk.Emulation.Cores.Nintendo.SubNESHawk;
using BizHawk.Emulation.Cores.PCEngine;
using BizHawk.Emulation.Cores.Sega.MasterSystem;
using BizHawk.Emulation.Cores.Waterbox;
using BizHawk.Emulation.Cores.WonderSwan;

namespace BizHawk.Client.EmuHawk
{
	public abstract partial class RetroAchievements
	{
		// "Hardcore Mode" is a mode intended for RA's leaderboard, and places various restrictions on the emulator
		// To keep changes outside this file minimal, we'll simply check if any problematic condition arises and disable hardcore mode
		// (with the exception of frame advance and rewind, which we can just suppress)

		private static readonly Type[] HardcoreProhibitedTools =
		{
			typeof(LuaConsole), typeof(RamWatch), typeof(RamSearch),
			typeof(GameShark), typeof(SNESGraphicsDebugger), typeof(PceBgViewer),
			typeof(PceTileViewer), typeof(GenVdpViewer), typeof(SmsVdpViewer),
			typeof(PCESoundDebugger), typeof(MacroInputTool), typeof(GenericDebugger),
			typeof(NESNameTableViewer), typeof(TraceLogger), typeof(CDL),
			typeof(Cheats), typeof(NesPPU), typeof(GbaGpuView),
			typeof(GbGpuView), typeof(BasicBot), typeof(HexEditor),
			typeof(TAStudio),
		};

		private static readonly Dictionary<Type, string[]> CoreGraphicsLayers = new()
		{
			[typeof(MSX)] = new[] { "DispBG", "DispOBJ" },
			[typeof(Atari2600)] = new[] { "ShowBG", "ShowPlayer1", "ShowPlayer2", "ShowMissle1", "ShowMissle2", "ShowBall", "ShowPlayfield" },
			[typeof(O2Hawk)] = new[] { "Show_Chars", "Show_Quads", "Show_Sprites", "Show_G7400_Sprites", "Show_G7400_BG" },
			[typeof(BsnesCore)] = new[] { "ShowBG1_0", "ShowBG2_0", "ShowBG3_0", "ShowBG4_0", "ShowBG1_1", "ShowBG2_1", "ShowBG3_1", "ShowBG4_1", "ShowOBJ_0", "ShowOBJ_1", "ShowOBJ_2", "ShowOBJ_3" },
			[typeof(MGBAHawk)] = new[] { "DisplayBG0", "DisplayBG1", "DisplayBG2", "DisplayBG3", "DisplayOBJ" },
			[typeof(NES)] = new[] { "DispBackground", "DispSprites" },
			[typeof(Sameboy)] = new[] { "EnableBGWIN", "EnableOBJ" },
			[typeof(Snes9x)] = new[] { "ShowBg0", "ShowBg1", "ShowBg2", "ShowBg3", "ShowSprites0", "ShowSprites1", "ShowSprites2", "ShowSprites3", "ShowWindow", "ShowTransparency" },
			[typeof(PCEngine)] = new[] { "ShowBG1", "ShowOBJ1", "ShowBG2", "ShowOBJ2", },
			[typeof(GPGX)] = new[] { "DrawBGA", "DrawBGB", "DrawBGW", "DrawObj", },
			[typeof(SMS)] = new[] { "DispBG", "DispOBJ" },
			[typeof(WonderSwan)] = new[] { "EnableBG", "EnableFG", "EnableSprites", },
		};

		private readonly OverrideAdapter _hardcoreHotkeyOverrides = new();

		protected abstract void HandleHardcoreModeDisable(string reason);

		protected void CheckHardcoreModeConditions()
		{
			if (!AllGamesVerified)
			{
				HandleHardcoreModeDisable("All loaded games were not verified.");
				return;
			}

			if (MovieSession.Movie.IsPlaying())
			{
				HandleHardcoreModeDisable("Playing a movie while in hardcore mode is not allowed.");
				return;
			}

			if (CheatList.AnyActive)
			{
				HandleHardcoreModeDisable("Using cheat codes while in hardcore mode is not allowed.");
				return;
			}

			// suppress rewind and frame advance hotkeys
			_hardcoreHotkeyOverrides.FrameTick();
			_hardcoreHotkeyOverrides.SetButton("Frame Advance", false);
			_hardcoreHotkeyOverrides.SetButton("Rewind", false);
			_inputManager.ClientControls.Overrides(_hardcoreHotkeyOverrides);
			_mainForm.FrameInch = false;

			var fastForward = _inputManager.ClientControls["Fast Forward"] || _mainForm.FastForward;
			var speedPercent = fastForward ? _getConfig().SpeedPercentAlternate : _getConfig().SpeedPercent;
			if (speedPercent < 100)
			{
				HandleHardcoreModeDisable("Slow motion in hardcore mode is not allowed.");
				return;
			}

			foreach (var t in HardcoreProhibitedTools)
			{
				if (!_tools.IsLoaded(t)) continue;
				HandleHardcoreModeDisable($"Using {t.Name} in hardcore mode is not allowed.");
				return;
			}

			// can't know what external tools are doing, so just don't allow them here
			if (_tools.IsLoaded<IExternalToolForm>())
			{
				HandleHardcoreModeDisable($"Using external tools in hardcore mode is not allowed.");
				return;
			}

			switch (Emu)
			{
				case SubNESHawk or SubBsnesCore or SubGBHawk:
					// this is mostly due to wonkiness with subframes which can be used as pseudo slowdown
					HandleHardcoreModeDisable("Using subframes in hardcore mode is not allowed.");
					break;
				case MAME:
					// this is a very complicated case that needs special handling the future
					HandleHardcoreModeDisable("Using MAME in hardcore mode is not allowed.");
					break;
				case NymaCore nyma:
					if (nyma.GetSettings().DisabledLayers.Any())
					{
						HandleHardcoreModeDisable($"Disabling {Emu.GetType().Name}'s graphics layers in hardcore mode is not allowed.");
					}
					break;
				case GambatteLink gl:
					if (gl.GetSyncSettings()._linkedSyncSettings.Any(ss => !ss.DisplayBG || !ss.DisplayOBJ || !ss.DisplayWindow))
					{
						HandleHardcoreModeDisable("Disabling GambatteLink's graphics layers in hardcore mode is not allowed.");
					}
					break;
				case Gameboy gb:
				{
					var ss = gb.GetSyncSettings();
					if (!ss.DisplayBG || !ss.DisplayOBJ || !ss.DisplayWindow)
					{
						HandleHardcoreModeDisable("Disabling Gambatte's graphics layers in hardcore mode is not allowed.");
					}
					else if (ss.FrameLength is Gameboy.GambatteSyncSettings.FrameLengthType.UserDefinedFrames)
					{
						HandleHardcoreModeDisable("Using subframes in hardcore mode is not allowed.");
					}
					break;
				}
				case NDS { IsDSi: true } nds:
				{
					var ss = nds.GetSyncSettings();
					if (!ss.ClearNAND)
					{
						HandleHardcoreModeDisable("Disabling DSi NAND clear in hardcore mode is not allowed.");
					}
					else if (!ss.SkipFirmware)
					{
						HandleHardcoreModeDisable("Disabling Skip Firmware in DSi mode in hardcore mode is not allowed.");
					}
					break;
				}
				default:
					if (CoreGraphicsLayers.TryGetValue(Emu.GetType(), out var layers))
					{
						var s = _mainForm.GetSettingsAdapterForLoadedCoreUntyped().GetSettings();
						var t = s.GetType();
						foreach (var layer in layers)
						{
							// annoyingly NES has fields instead of properties for layers
							if ((bool)(t.GetProperty(layer)
									?.GetValue(s) ?? t.GetField(layer)
									.GetValue(s))) continue;
							HandleHardcoreModeDisable($"Disabling {Emu.GetType().Name}'s {layer} in hardcore mode is not allowed.");
							return;
						}
					}
					break;
			}
		}
	}
}
