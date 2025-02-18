using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using BizHawk.Common.CollectionExtensions;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// You probably want <see cref="Raw"/>.
	/// It's laid out this way to match a local branch of mine where this is a struct. --yoshi
	/// </summary>
	public static class VSystemID
	{
		public static class Raw
		{
			public const string A26 = "A26";
			public const string A78 = "A78";
			public const string Amiga = "Amiga";
			public const string AmstradCPC = "AmstradCPC";
			public const string AppleII = "AppleII";
			public const string Arcade = "Arcade";
			public const string C64 = "C64";
			public const string ChannelF = "ChannelF";
			public const string Coleco = "Coleco";
			public const string DEBUG = "DEBUG";
			public const string Dreamcast = "Dreamcast";
			public const string GameCube = "GameCube";
			public const string GB = "GB";
			public const string GBA = "GBA";
			public const string GBC = "GBC";
			public const string GBL = "GBL";
			public const string GEN = "GEN";
			public const string GG = "GG";
			public const string GGL = "GGL";
			public const string INTV = "INTV";
			public const string Jaguar = "Jaguar";
			public const string Libretro = "Libretro";
			public const string Lynx = "Lynx";
			public const string MSX = "MSX";
			public const string N3DS = "3DS";
			public const string N64 = "N64";
			public const string NDS = "NDS";
			public const string NeoGeoCD = "NeoGeoCD";
			public const string NES = "NES";
			public const string NGP = "NGP";
			public const string NULL = "NULL";
			public const string O2 = "O2";
			public const string Panasonic3DO = "Panasonic3DO";
			public const string PCE = "PCE";
			public const string PCECD = "PCECD";
			public const string PCFX = "PCFX";
			public const string PhillipsCDi = "PhillipsCDi";
			public const string Playdia = "Playdia";
			public const string PS2 = "PS2";
			public const string PSP = "PSP";
			public const string PSX = "PSX";
			public const string SAT = "SAT";
			public const string Satellaview = "BSX";
			public const string Sega32X = "32X";
			public const string SG = "SG";
			public const string SGB = "SGB";
			public const string SGX = "SGX";
			public const string SGXCD = "SGXCD";
			public const string SMS = "SMS";
			public const string SNES = "SNES";
			public const string TI83 = "TI83";
			public const string TIC80 = "TIC80";
			public const string UZE = "UZE";
			public const string VB = "VB";
			public const string VEC = "VEC";
			public const string Wii = "Wii";
			public const string WSWAN = "WSWAN";
			public const string ZXSpectrum = "ZXSpectrum";
		}

		private static List<string>? _allSysIDs = null;

		private static List<string> AllSysIDs
			=> _allSysIDs ??= typeof(Raw).GetFields(BindingFlags.Public | BindingFlags.Static)
				.Select(x => (string) x.GetRawConstantValue()!)
				.Order().ToList();

		/// <returns><paramref name="sysID"/> iff it's in <see cref="Raw">the valid list</see>, else <see langword="null"/></returns>
		public static string? Validate(string sysID)
			=> AllSysIDs.BinarySearch(sysID) < 0 ? null : sysID;
	}
}
