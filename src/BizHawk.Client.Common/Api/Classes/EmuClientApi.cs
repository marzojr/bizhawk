using System.Drawing;
using System.IO;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public sealed class EmuClientApi : IEmuClientApi
	{
		private readonly Config _config;

		private readonly DisplayManagerBase _displayManager;

		private readonly IMainFormForApi _mainForm;

		private readonly Action<string> _logCallback;

		private readonly IEmulator Emulator;

		private readonly IGameInfo Game;

		private readonly IVideoProvider VideoProvider;

		public event BeforeQuickLoadEventHandler BeforeQuickLoad;

		public event BeforeQuickSaveEventHandler BeforeQuickSave;

		public event EventHandler RomLoaded;

		public event StateLoadedEventHandler StateLoaded;

		public event StateSavedEventHandler StateSaved;

		public EmuClientApi(Action<string> logCallback, IMainFormForApi mainForm, DisplayManagerBase displayManager, Config config, IEmulator emulator, IGameInfo game)
		{
			_config = config;
			_displayManager = displayManager;
			Emulator = emulator;
			Game = game;
			_logCallback = logCallback;
			_mainForm = mainForm;
			VideoProvider = Emulator.AsVideoProviderOrDefault();

			_mainForm.QuicksaveLoad += CallBeforeQuickLoad;
			_mainForm.QuicksaveSave += CallBeforeQuickSave;
			_mainForm.RomLoaded += CallRomLoaded;
			_mainForm.SavestateLoaded += CallStateLoaded;
			_mainForm.SavestateSaved += CallStateSaved;
		}

		public int BorderHeight() => _displayManager.TransformPoint(new Point(0, 0)).Y;

		public int BorderWidth() => _displayManager.TransformPoint(new Point(0, 0)).X;

		public int BufferHeight() => VideoProvider.BufferHeight;

		public int BufferWidth() => VideoProvider.BufferWidth;

#pragma warning disable MA0091 // passing through `sender` is intentional
		private void CallBeforeQuickLoad(object sender, BeforeQuickLoadEventArgs args)
			=> BeforeQuickLoad?.Invoke(sender, args);
 
		private void CallBeforeQuickSave(object sender, BeforeQuickSaveEventArgs args)
			=> BeforeQuickSave?.Invoke(sender, args);
 
		private void CallRomLoaded(object sender, EventArgs args)
			=> RomLoaded?.Invoke(sender, args);
 
		private void CallStateLoaded(object sender, StateLoadedEventArgs args)
			=> StateLoaded?.Invoke(sender, args);
 
		private void CallStateSaved(object sender, StateSavedEventArgs args)
			=> StateSaved?.Invoke(sender, args);
#pragma warning restore MA0091

		public void ClearAutohold() => _mainForm.ClearHolds();

		public void CloseEmulator(int? exitCode = null) => _mainForm.CloseEmulator(exitCode);

		public void CloseRom() => _mainForm.CloseRom();

		public void DisplayMessages(bool value) => _config.DisplayMessages = value;

		public void Dispose()
		{
			_mainForm.QuicksaveLoad -= CallBeforeQuickLoad;
			_mainForm.QuicksaveSave -= CallBeforeQuickSave;
			_mainForm.RomLoaded -= CallRomLoaded;
			_mainForm.SavestateLoaded -= CallStateLoaded;
			_mainForm.SavestateSaved -= CallStateSaved;
		}

		public void DoFrameAdvance()
		{
			_mainForm.FrameAdvance(discardApiHawkSurfaces: false); // we're rendering, so we don't want to discard
			_mainForm.StepRunLoop_Throttle();
			_mainForm.Render();
		}

		public void DoFrameAdvanceAndUnpause()
		{
			DoFrameAdvance();
			Unpause();
		}

		public void EnableRewind(bool enabled) => _mainForm.EnableRewind(enabled);

		public void FrameSkip(int numFrames)
		{
			if (numFrames < 0)
			{
				_logCallback("Invalid frame skip value");
				return;
			}

			_config.FrameSkip = numFrames;
			_mainForm.FrameSkipMessage();
		}

		public int GetApproxFramerate() => _mainForm.GetApproxFramerate();

		public bool GetSoundOn() => _config.SoundEnabled;

		public int GetTargetScanlineIntensity() => _config.TargetScanlineFilterIntensity;

		public int GetWindowSize()
			=> _config.GetWindowScaleFor(Emulator.SystemId);

		public void InvisibleEmulation(bool invisible) => _mainForm.InvisibleEmulation = invisible;

		public bool IsPaused() => _mainForm.EmulatorPaused;

		public bool IsSeeking() => _mainForm.IsSeeking;

		public bool IsTurbo() => _mainForm.IsTurboing;

		public bool LoadState(string name)
			=> _mainForm.LoadState(
				path: Path.Combine(_config.PathEntries.SaveStateAbsolutePath(Game.System), $"{name}.State"),
				userFriendlyStateName: name,
				suppressOSD: false);

		public bool OpenRom(string path)
			=> _mainForm.LoadRom(new LoadRomArgs(new OpenAdvanced_OpenRom(path)));

		public void Pause() => _mainForm.PauseEmulator();

		public void PauseAv() => _mainForm.PauseAvi = true;

		public void RebootCore() => _mainForm.RebootCore();

		public void SaveRam() => _mainForm.FlushSaveRAM();

		public void SaveState(string name) => _mainForm.SaveState(Path.Combine(_config.PathEntries.SaveStateAbsolutePath(Game.System), $"{name}.State"), name, fromLua: false);

		public int ScreenHeight() => _displayManager.GetPanelNativeSize().Height;

		public void Screenshot(string path)
		{
			if (path == null) _mainForm.TakeScreenshot();
			else _mainForm.TakeScreenshot(path);
		}

		public void ScreenshotToClipboard() => _mainForm.TakeScreenshotToClipboard();

		public int ScreenWidth() => _displayManager.GetPanelNativeSize().Width;

		public void SeekFrame(int frame)
		{
			var wasPaused = _mainForm.EmulatorPaused;
			while (Emulator.Frame != frame) _mainForm.SeekFrameAdvance();
			if (!wasPaused) _mainForm.UnpauseEmulator();
		}

		public void SetClientExtraPadding(int left, int top, int right, int bottom)
		{
			_displayManager.ClientExtraPadding = (left, top, right, bottom);
			_mainForm.FrameBufferResized();
		}

		public void SetGameExtraPadding(int left, int top, int right, int bottom)
		{
			_displayManager.GameExtraPadding = (left, top, right, bottom);
			_mainForm.FrameBufferResized();
		}

		public void SetScreenshotOSD(bool value) => _config.ScreenshotCaptureOsd = value;

		public void SetSoundOn(bool enable)
		{
			if (enable != _config.SoundEnabled) _mainForm.ToggleSound();
		}

		public void SetTargetScanlineIntensity(int val) => _config.TargetScanlineFilterIntensity = val;

		public void SetWindowSize(int size)
		{
			if (size == 1 || size == 2 || size == 3 || size == 4 || size == 5 || size == 10)
			{
				_config.SetWindowScaleFor(Emulator.SystemId, size);
				_mainForm.FrameBufferResized(forceWindowResize: true);
				_displayManager.OSD.AddMessage($"Window size set to {size}x");
			}
			else
			{
				_logCallback("Invalid window size");
			}
		}

		public void SpeedMode(int percent)
		{
			if (percent is > 0 and <= 6400) _mainForm.ClickSpeedItem(percent);
			else _logCallback("Invalid speed value");
		}

		public void TogglePause() => _mainForm.TogglePause();

		public Point TransformPoint(Point point) => _displayManager.TransformPoint(point);

		public void Unpause() => _mainForm.UnpauseEmulator();

		public void UnpauseAv() => _mainForm.PauseAvi = false;

		public int Xpos() => _mainForm.DesktopLocation.X;

		public int Ypos() => _mainForm.DesktopLocation.Y;
	}
}
