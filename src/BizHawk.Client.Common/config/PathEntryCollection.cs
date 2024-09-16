using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

using BizHawk.Common.CollectionExtensions;
using BizHawk.Common.PathExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class PathEntryCollection
	{
		private static readonly string COMBINED_SYSIDS_GB = string.Join("_", VSystemID.Raw.GB, VSystemID.Raw.GBC, VSystemID.Raw.SGB);

		private static readonly string COMBINED_SYSIDS_PCE = string.Join("_", VSystemID.Raw.PCE, VSystemID.Raw.PCECD, VSystemID.Raw.SGX, VSystemID.Raw.SGXCD);

		public static readonly string GLOBAL = string.Join("_", "Global", VSystemID.Raw.NULL);

		private static readonly Dictionary<string, string> _displayNameLookup = new()
		{
			[GLOBAL] = "Global",
			[VSystemID.Raw.Amiga] = "Amiga",
			[VSystemID.Raw.Arcade] = "Arcade",
			[VSystemID.Raw.INTV] = "Intellivision",
			[VSystemID.Raw.NES] = "NES",
			[VSystemID.Raw.SNES] = "SNES",
			[VSystemID.Raw.GBA] = "GBA",
			[VSystemID.Raw.SMS] = "SMS",
			[VSystemID.Raw.GG] = "GG",
			[VSystemID.Raw.SG] = "SG",
			[VSystemID.Raw.GEN] = "Genesis",
			[COMBINED_SYSIDS_PCE] = "PC Engine",
			[COMBINED_SYSIDS_GB] = "Gameboy",
			[VSystemID.Raw.GBL] = "Gameboy Link",
			[VSystemID.Raw.TI83] = "TI83",
			[VSystemID.Raw.A26] = "Atari 2600",
			[VSystemID.Raw.A78] = "Atari 7800",
			[VSystemID.Raw.C64] = "Commodore 64",
			[VSystemID.Raw.ZXSpectrum] = "Sinclair ZX Spectrum",
			[VSystemID.Raw.AmstradCPC] = "Amstrad CPC",
			[VSystemID.Raw.PSX] = "Playstation",
			[VSystemID.Raw.Coleco] = "Coleco",
			[VSystemID.Raw.N64] = "N64",
			[VSystemID.Raw.SAT] = "Saturn",
			[VSystemID.Raw.WSWAN] = "WonderSwan",
			[VSystemID.Raw.Jaguar] = "Jaguar",
			[VSystemID.Raw.Lynx] = "Lynx",
			[VSystemID.Raw.AppleII] = "Apple II",
			[VSystemID.Raw.Libretro] = "Libretro",
			[VSystemID.Raw.VB] = "VB",
			[VSystemID.Raw.NGP] = "NGP",
			[VSystemID.Raw.PCFX] = "PCFX",
			[VSystemID.Raw.ChannelF] = "Fairchild Channel F",
			[VSystemID.Raw.VEC] = "VEC",
			[VSystemID.Raw.O2] = "O2",
			[VSystemID.Raw.MSX] = "MSX",
			[VSystemID.Raw.TIC80] = "TIC80",
			[VSystemID.Raw.UZE] = "UZE",
			[VSystemID.Raw.NDS] = "NDS",
			[VSystemID.Raw.Sega32X] = "Sega 32X",
			[VSystemID.Raw.GGL] = "Dual Game Gear",
			[VSystemID.Raw.Satellaview] = "Satellaview",
			[VSystemID.Raw.N3DS] = "3DS"
		};

		private static PathEntry BaseEntryFor(string sysID, string path)
			=> new(sysID, "Base", path);

		private static PathEntry CheatsEntryFor(string sysID)
			=> new(sysID, "Cheats", Path.Combine(".", "Cheats"));

		private static IEnumerable<PathEntry> CommonEntriesFor(string sysID, string basePath, bool omitSaveRAM = false)
			=> [
				BaseEntryFor(sysID, basePath),
				ROMEntryFor(sysID),
				SavestatesEntryFor(sysID),
				..(omitSaveRAM ? [ ] : new[] { SaveRAMEntryFor(sysID) }),
				ScreenshotsEntryFor(sysID),
				CheatsEntryFor(sysID),
			];

		public static string GetDisplayNameFor(string sysID)
			=> _displayNameLookup.GetValueOrPut(sysID, static s => s + " (INTERIM)");

		public static bool InGroup(string sysID, string group)
			=> sysID == group || group.Split('_').Contains(sysID);

		private static PathEntry PalettesEntryFor(string sysID)
			=> new(sysID, "Palettes", Path.Combine(".", "Palettes"));

		private static PathEntry ROMEntryFor(string sysID, string path = ".")
			=> new(sysID, "ROM", path);

		private static PathEntry SaveRAMEntryFor(string sysID)
			=> new(sysID, "Save RAM", Path.Combine(".", "SaveRAM"));

		private static PathEntry SavestatesEntryFor(string sysID)
			=> new(sysID, "Savestates", Path.Combine(".", "State"));

		private static PathEntry ScreenshotsEntryFor(string sysID)
			=> new(sysID, "Screenshots", Path.Combine(".", "Screenshots"));

		private static PathEntry UserEntryFor(string sysID)
			=> new(sysID, "User", Path.Combine(".", "User"));

		public List<PathEntry> Paths { get; }

		[JsonConstructor]
		public PathEntryCollection(List<PathEntry> paths)
		{
			Paths = paths;
		}

		public PathEntryCollection() : this(new List<PathEntry>(Defaults.Value)) {}

		public bool UseRecentForRoms { get; set; }
		public string LastRomPath { get; set; } = ".";

		public PathEntry this[string system, string type]
			=> Paths.Find(p => p.IsSystem(system) && p.Type == type) ?? TryGetDebugPath(system, type);

		private PathEntry TryGetDebugPath(string system, string type)
		{
			if (Paths.Exists(p => p.IsSystem(system)))
			{
				// we have the system, but not the type.  don't attempt to add an unknown type
				return null;
			}

			// we don't have anything for the system in question.  add a set of stock paths
			Paths.AddRange(CommonEntriesFor(system, basePath: Path.Combine(".", $"{system.RemoveInvalidFileSystemChars()}_INTERIM")));

			return this[system, type];
		}

		public void ResolveWithDefaults()
		{
			// Add missing entries
			foreach (var defaultPath in Defaults.Value)
			{
				if (!Paths.Exists(p => p.System == defaultPath.System && p.Type == defaultPath.Type)) Paths.Add(defaultPath);
			}

			var entriesToRemove = new List<PathEntry>();

			// Remove entries that no longer exist in defaults
			foreach (PathEntry pathEntry in Paths)
			{
				var path = Defaults.Value.FirstOrDefault(p => p.System == pathEntry.System && p.Type == pathEntry.Type);
				if (path == null)
				{
					entriesToRemove.Add(pathEntry);
				}
			}

			foreach (PathEntry entry in entriesToRemove)
			{
				Paths.Remove(entry);
			}
		}

		[JsonIgnore]
		public string FirmwaresPathFragment => this[GLOBAL, "Firmware"].Path;

		internal string TempFilesFragment => this[GLOBAL, "Temp Files"].Path;

		public static readonly Lazy<IReadOnlyList<PathEntry>> Defaults = new(() => new[]
		{
			new[] {
				BaseEntryFor(GLOBAL, "."),
				ROMEntryFor(GLOBAL),
				new(GLOBAL, "Firmware", Path.Combine(".", "Firmware")),
				new(GLOBAL, "Movies", Path.Combine(".", "Movies")),
				new(GLOBAL, "Movie backups", Path.Combine(".", "Movies", "backup")),
				new(GLOBAL, "A/V Dumps", "."),
				new(GLOBAL, "Tools", Path.Combine(".", "Tools")),
				new(GLOBAL, "Lua", Path.Combine(".", "Lua")),
				new(GLOBAL, "Watch (.wch)", Path.Combine(".", ".")),
				new(GLOBAL, "Debug Logs", Path.Combine(".", "")),
				new(GLOBAL, "Macros", Path.Combine(".", "Movies", "Macros")),
				new(GLOBAL, "Multi-Disk Bundles", Path.Combine(".", "")),
				new(GLOBAL, "External Tools", Path.Combine(".", "ExternalTools")),
				new(GLOBAL, "Temp Files", ""),
			},

			CommonEntriesFor(VSystemID.Raw.N3DS, basePath: Path.Combine(".", "3DS"), omitSaveRAM: true),
			new[] {
				UserEntryFor(VSystemID.Raw.N3DS),
			},

			CommonEntriesFor(VSystemID.Raw.Sega32X, basePath: Path.Combine(".", "32X")),

			CommonEntriesFor(VSystemID.Raw.Amiga, basePath: Path.Combine(".", "Amiga")),

			CommonEntriesFor(VSystemID.Raw.A26, basePath: Path.Combine(".", "Atari 2600"), omitSaveRAM: true),

			CommonEntriesFor(VSystemID.Raw.A78, basePath: Path.Combine(".", "Atari 7800")),

			CommonEntriesFor(VSystemID.Raw.AmstradCPC, basePath: Path.Combine(".", "AmstradCPC"), omitSaveRAM: true),

			CommonEntriesFor(VSystemID.Raw.AppleII, basePath: Path.Combine(".", "Apple II")),

			CommonEntriesFor(VSystemID.Raw.Arcade, basePath: Path.Combine(".", "Arcade")),

			CommonEntriesFor(VSystemID.Raw.C64, basePath: Path.Combine(".", "C64")),

			CommonEntriesFor(VSystemID.Raw.ChannelF, basePath: Path.Combine(".", "Channel F"), omitSaveRAM: true),

			CommonEntriesFor(VSystemID.Raw.Coleco, basePath: Path.Combine(".", "Coleco"), omitSaveRAM: true),

			CommonEntriesFor(VSystemID.Raw.GBL, basePath: Path.Combine(".", "Gameboy Link")),
			new[] {
				PalettesEntryFor(VSystemID.Raw.GBL),
			},

			CommonEntriesFor(COMBINED_SYSIDS_GB, basePath: Path.Combine(".", "Gameboy")),
			new[] {
				PalettesEntryFor(COMBINED_SYSIDS_GB),
			},

			CommonEntriesFor(VSystemID.Raw.GBA, basePath: Path.Combine(".", "GBA")),

			CommonEntriesFor(VSystemID.Raw.GEN, basePath: Path.Combine(".", "Genesis")),

			CommonEntriesFor(VSystemID.Raw.GG, basePath: Path.Combine(".", "Game Gear")),

			CommonEntriesFor(VSystemID.Raw.GGL, basePath: Path.Combine(".", "Dual Game Gear")),

			CommonEntriesFor(VSystemID.Raw.INTV, basePath: Path.Combine(".", "Intellivision")),
			new[] {
				PalettesEntryFor(VSystemID.Raw.INTV),
			},

			CommonEntriesFor(VSystemID.Raw.Jaguar, basePath: Path.Combine(".", "Jaguar")),

			new[] {
				BaseEntryFor(VSystemID.Raw.Libretro, Path.Combine(".", "Libretro")),
				// It doesn't make much sense to have a ROM dir for libretro, but a lot of stuff is built around the assumption of a ROM dir existing
				// also, note, sometimes when path gets used, it's for opening a rom, which will be... loaded by... the default system for that rom, i.e. NOT libretro.
				// Really, "Open Rom" for instance doesn't make sense when you have a libretro core open.
				// Well, this is better than nothing.
				ROMEntryFor(VSystemID.Raw.Libretro, "%recent%"),
				new(VSystemID.Raw.Libretro, "Cores", Path.Combine(".", "Cores")),
				new(VSystemID.Raw.Libretro, "System", Path.Combine(".", "System")),
				SavestatesEntryFor(VSystemID.Raw.Libretro),
				SaveRAMEntryFor(VSystemID.Raw.Libretro),
				ScreenshotsEntryFor(VSystemID.Raw.Libretro),
				CheatsEntryFor(VSystemID.Raw.Libretro),
			},

			CommonEntriesFor(VSystemID.Raw.Lynx, basePath: Path.Combine(".", "Lynx")),

			CommonEntriesFor(VSystemID.Raw.MSX, basePath: Path.Combine(".", "MSX")),

			CommonEntriesFor(VSystemID.Raw.N64, basePath: Path.Combine(".", "N64")),

			CommonEntriesFor(VSystemID.Raw.NDS, basePath: Path.Combine(".", "NDS")),

			CommonEntriesFor(VSystemID.Raw.NES, basePath: Path.Combine(".", "NES")),
			new[] {
				PalettesEntryFor(VSystemID.Raw.NES),
			},

			CommonEntriesFor(VSystemID.Raw.NGP, basePath: Path.Combine(".", "NGP")),

			CommonEntriesFor(VSystemID.Raw.O2, basePath: Path.Combine(".", "O2")),

			CommonEntriesFor(COMBINED_SYSIDS_PCE, basePath: Path.Combine(".", "PC Engine")),

			CommonEntriesFor(VSystemID.Raw.PCFX, basePath: Path.Combine(".", "PCFX")),

			CommonEntriesFor(VSystemID.Raw.PSX, basePath: Path.Combine(".", "PSX")),

			CommonEntriesFor(VSystemID.Raw.SAT, basePath: Path.Combine(".", "Saturn")),

			CommonEntriesFor(VSystemID.Raw.Satellaview, basePath: Path.Combine(".", "Satellaview")),

			CommonEntriesFor(VSystemID.Raw.SG, basePath: Path.Combine(".", "SG-1000")),

			CommonEntriesFor(VSystemID.Raw.SMS, basePath: Path.Combine(".", "SMS")),

			CommonEntriesFor(VSystemID.Raw.SNES, basePath: Path.Combine(".", "SNES")),

			CommonEntriesFor(VSystemID.Raw.TI83, basePath: Path.Combine(".", "TI83")),

			CommonEntriesFor(VSystemID.Raw.TIC80, basePath: Path.Combine(".", "TIC80")),

			CommonEntriesFor(VSystemID.Raw.UZE, basePath: Path.Combine(".", "Uzebox")),

			CommonEntriesFor(VSystemID.Raw.VB, basePath: Path.Combine(".", "VB")),

			CommonEntriesFor(VSystemID.Raw.VEC, basePath: Path.Combine(".", "VEC")),

			CommonEntriesFor(VSystemID.Raw.WSWAN, basePath: Path.Combine(".", "WonderSwan")),

			CommonEntriesFor(VSystemID.Raw.ZXSpectrum, basePath: Path.Combine(".", "ZXSpectrum"), omitSaveRAM: true),
		}.SelectMany(a => a).ToArray());
	}
}
