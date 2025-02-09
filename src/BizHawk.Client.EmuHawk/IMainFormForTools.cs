using BizHawk.Bizware.Graphics;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public interface IMainFormForTools : IDialogController
	{
		CheatCollection CheatList { get; }

		string CurrentlyOpenRom { get; }

		/// <remarks>only referenced from <see cref="HexEditor"/></remarks>
		LoadRomArgs CurrentlyOpenRomArgs { get; }

		/// <remarks>only referenced from <see cref="TAStudio"/></remarks>
		bool EmulatorPaused { get; }

		/// <remarks>only referenced from <see cref="TAStudio"/></remarks>
		// TODO: remove? or does anything ever need access to the FirmwareManager
		FirmwareManager FirmwareManager { get; }

		/// <remarks>only referenced from <see cref="TAStudio"/></remarks>
		bool GameIsClosing { get; }

		/// <remarks>only referenced from <see cref="PlaybackBox"/></remarks>
		bool HoldFrameAdvance { get; set; }

		/// <remarks>only referenced from <see cref="BasicBot"/></remarks>
		bool InvisibleEmulation { get; set; }

		/// <remarks>only referenced from <see cref="TAStudio"/></remarks>
		bool IsSeeking { get; }

		/// <remarks>only referenced from <see cref="LuaConsole"/></remarks>
		bool IsTurboing { get; }

		int? PauseOnFrame { get; set; }

		/// <remarks>only referenced from <see cref="PlaybackBox"/></remarks>
		bool PressRewind { get; set; }

		BitmapBuffer CaptureOSD();

		/// <remarks>only referenced from <see cref="TAStudio"/></remarks>
		void DisableRewind();

		/// <remarks>only referenced from <see cref="TAStudio"/></remarks>
		void EnableRewind(bool enabled);

		/// <remarks>only referenced from <see cref="TAStudio"/></remarks>
		bool EnsureCoreIsAccurate();

		/// <remarks>only referenced from <see cref="TAStudio"/></remarks>
		void FrameAdvance(bool discardApiHawkSurfaces = true);

		/// <remarks>only referenced from <see cref="LuaConsole"/></remarks>
		/// <param name="forceWindowResize">Override <see cref="Common.Config.ResizeWithFramebuffer"/></param>
		void FrameBufferResized(bool forceWindowResize = false);

		/// <remarks>only referenced from <see cref="BasicBot"/></remarks>
		bool LoadQuickSave(int slot, bool suppressOSD = false);

		/// <remarks>only referenced from <see cref="MultiDiskBundler"/></remarks>
		bool LoadRom(LoadRomArgs args);

		/// <remarks>only referenced from <see cref="BookmarksBranchesBox"/></remarks>
		BitmapBuffer MakeScreenshotImage();

		void MaybePauseFromMenuOpened();

		void MaybeUnpauseFromMenuClosed();

		void PauseEmulator();

		bool BlockFrameAdvance { get; set; }

		/// <remarks>only referenced from <see cref="TAStudio"/></remarks>
		void SeekFrameAdvance();

		void SetMainformMovieInfo();

		bool StartNewMovie(IMovie movie, bool newMovie);

		/// <remarks>only referenced from <see cref="BasicBot"/></remarks>
		void Throttle();

		/// <remarks>only referenced from <see cref="TAStudio"/></remarks>
		void TogglePause();

		void UnpauseEmulator();

		/// <remarks>only referenced from <see cref="BasicBot"/></remarks>
		void Unthrottle();

		/// <remarks>only referenced from <see cref="LogWindow"/></remarks>
		void UpdateDumpInfo(RomStatus? newStatus = null);

		/// <remarks>only referenced from <see cref="BookmarksBranchesBox"/></remarks>
		void UpdateStatusSlots();

		/// <remarks>only referenced from <see cref="TAStudio"/></remarks>
		void UpdateWindowTitle();
	}
}
