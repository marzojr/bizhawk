using System.Drawing;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public interface IMainFormForApi
	{
		/// <remarks>only referenced from <c>ClientLuaLibrary</c></remarks>
		CheatCollection CheatList { get; }

		/// <remarks>only referenced from <c>EmuClientApi</c></remarks>
		Point DesktopLocation { get; }

		/// <remarks>only referenced from <c>ClientLuaLibrary</c></remarks>
		IEmulator Emulator { get; }

		bool EmulatorPaused { get; }

		/// <remarks>only referenced from <c>EmuClientApi</c></remarks>
		bool InvisibleEmulation { get; set; }

		/// <remarks>only referenced from <c>EmuClientApi</c></remarks>
		bool IsSeeking { get; }

		/// <remarks>only referenced from <c>EmuClientApi</c></remarks>
		bool IsTurboing { get; }

		/// <remarks>only referenced from <see cref="CommApi"/></remarks>
		(HttpCommunication HTTP, MemoryMappedFiles MMF, SocketServer Sockets) NetworkingHelpers { get; }

		/// <remarks>only referenced from <c>EmuClientApi</c></remarks>
		bool PauseAvi { get; set; }

		/// <remarks>only referenced from <c>EmuClientApi</c></remarks>
		void ClearHolds();

		/// <remarks>only referenced from <c>EmuClientApi</c></remarks>
		void ClickSpeedItem(int num);

		/// <remarks>only referenced from <c>EmuClientApi</c></remarks>
		void CloseEmulator(int? exitCode = null);

		/// <remarks>only referenced from <c>EmuClientApi</c></remarks>
		void CloseRom(bool clearSram = false);

		/// <remarks>only referenced from <c>EmuClientApi</c></remarks>
		void EnableRewind(bool enabled);

		/// <remarks>only referenced from <c>EmuClientApi</c></remarks>
		bool FlushSaveRAM(bool autosave = false);

		/// <remarks>only referenced from <c>EmuClientApi</c></remarks>
		void FrameAdvance(bool discardApiHawkSurfaces = true);

		/// <param name="forceWindowResize">Override <see cref="Common.Config.ResizeWithFramebuffer"/></param>
		void FrameBufferResized(bool forceWindowResize = false);

		void FrameSkipMessage();

		/// <remarks>only referenced from <see cref="EmuClientApi"/></remarks>
		int GetApproxFramerate();

		/// <summary>
		/// essentially <c>return MainForm.StartNewMovie(MovieSession.Get(filename), record: false);</c>,
		/// but also ensures a rom is loaded, and defers to TAStudio
		/// </summary>
		/// <param name="archive">unused</param>
		/// <remarks>only referenced from <see cref="MovieApi"/></remarks>
		bool LoadMovie(string filename, string archive = null);

		/// <remarks>only referenced from <see cref="SaveStateApi"/></remarks>
		bool LoadQuickSave(int slot, bool suppressOSD = false);

		/// <remarks>only referenced from <c>EmuClientApi</c></remarks>
		bool LoadRom(LoadRomArgs args);

		bool LoadState(string path, string userFriendlyStateName, bool suppressOSD = false);

		/// <remarks>only referenced from <c>EmuClientApi</c></remarks>
		void PauseEmulator();

		/// <remarks>only referenced from <c>EmuClientApi</c></remarks>
		bool RebootCore();

		/// <remarks>only referenced from <c>EmuClientApi</c></remarks>
		void Render();

		/// <remarks>only referenced from <see cref="MovieApi"/></remarks>
		bool RestartMovie();

		/// <remarks>only referenced from <see cref="SaveStateApi"/></remarks>
		void SaveQuickSave(int slot, bool suppressOSD = false, bool fromLua = false);

		void SaveState(string path, string userFriendlyStateName, bool fromLua = false, bool suppressOSD = false);

		void SeekFrameAdvance();

		/// <remarks>only referenced from <c>EmuClientApi</c></remarks>
		void StepRunLoop_Throttle();

		/// <remarks>only referenced from <see cref="MovieApi"/></remarks>
		void StopMovie(bool saveChanges = true);

		/// <remarks>only referenced from <c>EmuClientApi</c></remarks>
		void TakeScreenshot();

		/// <remarks>only referenced from <c>EmuClientApi</c></remarks>
		void TakeScreenshot(string path);

		/// <remarks>only referenced from <c>EmuClientApi</c></remarks>
		void TakeScreenshotToClipboard();

		/// <remarks>only referenced from <c>EmuClientApi</c></remarks>
		void TogglePause();

		/// <remarks>only referenced from <c>EmuClientApi</c></remarks>
		void ToggleSound();

		void UnpauseEmulator();

		event BeforeQuickLoadEventHandler QuicksaveLoad;

		event BeforeQuickSaveEventHandler QuicksaveSave;

		event EventHandler RomLoaded;

		event StateLoadedEventHandler SavestateLoaded;

		event StateSavedEventHandler SavestateSaved;
	}
}
