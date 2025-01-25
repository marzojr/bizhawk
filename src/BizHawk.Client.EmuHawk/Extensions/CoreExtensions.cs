using System.Drawing;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES;
using BizHawk.Emulation.Cores.Nintendo.BSNES;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.Nintendo.SNES9X;
using BizHawk.Emulation.Cores.Consoles.Sega.gpgx;
using BizHawk.Emulation.Cores.Arcades.MAME;
using BizHawk.Emulation.Cores.Consoles.Nintendo.NDS;
using BizHawk.Emulation.Cores.Nintendo.GBA;
using BizHawk.Emulation.Cores.Computers.Amiga;

namespace BizHawk.Client.EmuHawk.CoreExtensions
{
	public static class CoreExtensions
	{
		public static Bitmap Icon(this IEmulator core)
		{
			var attributes = core.Attributes();

			if (attributes is not PortedCoreAttribute)
			{
				return Properties.Resources.CorpHawkSmall;
			}

			return core switch
			{
				QuickNES => Properties.Resources.QuickNes,
				BsnesCore => Properties.Resources.Bsnes,
				GPGX => Properties.Resources.GenPlus,
				Gameboy => Properties.Resources.Gambatte,
				Snes9x => Properties.Resources.Snes9X,
				MAME => Properties.Resources.Mame,
				MGBAHawk => Properties.Resources.Mgba,
				NDS => Properties.Resources.MelonDS,
				UAE => Properties.Resources.Amiga,
				_ => null
			};
		}

		public static string GetSystemDisplayName(this IEmulator emulator) => emulator switch
		{
			NullEmulator => string.Empty,
#if false
			IGameboyCommon gb when gb.IsCGBMode() => EmulatorExtensions.SystemIDToDisplayName(VSystemID.Raw.GBC),
#endif
			_ => EmulatorExtensions.SystemIDToDisplayName(emulator.SystemId)
		};
	}
}
