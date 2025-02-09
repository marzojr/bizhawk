using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public interface IMainFormForRetroAchievements
	{
		LoadRomArgs CurrentlyOpenRomArgs { get; }

		IEmulator Emulator { get; }

		bool FrameInch { get; set; }

		bool FastForward { get; set; }

		GameInfo Game { get; }

		CheatCollection CheatList { get; }

		IMovieSession MovieSession { get; }

		FirmwareManager FirmwareManager { get; }

		event BeforeQuickLoadEventHandler QuicksaveLoad;

		SettingsAdapter GetSettingsAdapterForLoadedCoreUntyped();

		bool LoadRom(LoadRomArgs args);

		void PauseEmulator();

		bool RebootCore();

		void UpdateWindowTitle();

		void UnpauseEmulator();
	}
}
