#nullable disable

using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Common.StringExtensions;

namespace BizHawk.Emulation.Common
{
	public static class Database
	{
		private static readonly Dictionary<string, CompactGameInfo> _builder = new();

		private static FrozenDictionary<string, CompactGameInfo> DB;

		/// <summary>
		/// blocks until the DB is done loading
		/// </summary>
		private static readonly ManualResetEvent _acquire = new(false);

		private static string _bundledRoot = null;

		private static IList<string> _expected = null;

		private static string _userRoot = null;

		/// <summary>
		/// Removes hash type if present and enforces an uppercase hash
		/// </summary>
		/// <param name="hash">The hash to format, this is typically prefixed with a type (e.g. sha1:)</param>
		/// <returns>formatted hash</returns>
		private static string FormatHash(string hash)
		{
			var i = hash.IndexOf(':');
			return (i < 0 ? hash : hash.Substring(startIndex: i + 1)).ToUpperASCIIFast();
		}

		private static void LoadDatabase_Escape(string line, bool inUser, bool silent)
		{
			var isUserInclude = line.StartsWithIgnoreCase("#includeuser");
			if (!isUserInclude && !line.StartsWithIgnoreCase("#include")) return;

			var searchUser = inUser || isUserInclude;
			line = line.Substring(isUserInclude ? 12 : 8).TrimStart();
			var filename = Path.Combine(searchUser ? _userRoot : _bundledRoot, line);
			if (File.Exists(filename))
			{
				if (!silent) Util.DebugWriteLine($"loading external game database {line} ({(searchUser ? "user" : "bundled")})");
				InitializeWork(filename, inUser: searchUser, silent: silent);
			}
			else if (inUser)
			{
				Util.DebugWriteLine($"BENIGN: missing external game database {line} (user)");
			}
			else if (!isUserInclude)
			{
				throw new FileNotFoundException($"missing external game database {line} (bundled)");
			}
		}

		/// <remarks>expensive, as it creates a new <see cref="FrozenDictionary{TKey,TValue}"/></remarks>
		public static void SaveDatabaseEntry(CompactGameInfo gameInfo, string filename = "gamedb_user.txt")
		{
			var sb = new StringBuilder();
			sb
				.Append("sha1:") // TODO: how do we know it is sha1?
				.Append(gameInfo.Hash)
				.Append('\t');

			sb.Append(gameInfo.Status switch
			{
				RomStatus.BadDump => "B",
				RomStatus.TranslatedRom => "T",
				RomStatus.Overdump => "O",
				RomStatus.Bios => "I",
				RomStatus.Homebrew => "D",
				RomStatus.Hack => "H",
				RomStatus.NotInDatabase => "U",
				RomStatus.Unknown => "U",
				_ => ""
			});

			sb
				.Append('\t')
				.Append(gameInfo.Name)
				.Append('\t')
				.Append(gameInfo.System)
				.Append('\t')
				.Append(gameInfo.MetaData)
				.Append(Environment.NewLine);

			_acquire.WaitOne();
			File.AppendAllText(Path.Combine(_userRoot, filename), sb.ToString());
			DB = DB.Append(new(gameInfo.Hash, gameInfo)).ToFrozenDictionary();
		}

		private static bool initialized = false;

		public static CompactGameInfo ParseCGIRecord(string lineStr)
		{
			var line = lineStr.AsSpan();
			const char FIELD_SEPARATOR = '\t';
			var iter = line.Split(FIELD_SEPARATOR);
			_ = iter.MoveNext();
			var hashDigest = FormatHash(lineStr.Substring(iter.Current));
			_ = iter.MoveNext();
			var dumpStatus = line.Slice(iter.Current).Trim() switch
			{
				"B" => RomStatus.BadDump, // see /Assets/gamedb/gamedb.txt
				"V" => RomStatus.BadDump, // see /Assets/gamedb/gamedb.txt
				"T" => RomStatus.TranslatedRom,
				"O" => RomStatus.Overdump,
				"I" => RomStatus.Bios,
				"D" => RomStatus.Homebrew,
				"H" => RomStatus.Hack,
				"U" => RomStatus.Unknown,
				_ => RomStatus.GoodDump
			};
			_ = iter.MoveNext();
			var knownName = lineStr.Substring(iter.Current);
			_ = iter.MoveNext();
			var sysID = lineStr.Substring(iter.Current);
			string/*?*/ metadata = null;
			string region = string.Empty;
			string forcedCore = string.Empty;
			if (iter.MoveNext())
			{
				//_ = line.Slice(iter.Current); // rarely populated; possibly genre or just a remark
				if (iter.MoveNext())
				{
					metadata = lineStr.Substring(iter.Current);
					if (iter.MoveNext())
					{
						region = lineStr.Substring(iter.Current);
						if (iter.MoveNext()) forcedCore = lineStr.Substring(iter.Current);
					}
				}
			}
			return new()
			{
				Hash = hashDigest,
				Status = dumpStatus,
				Name = knownName,
				System = sysID,
				MetaData = metadata,
				Region = region,
				ForcedCore = forcedCore,
			};
		}

		private static void InitializeWork(string path, bool inUser, bool silent)
		{
			if (!inUser) _expected.Remove(Path.GetFileName(path));
			//reminder: this COULD be done on several threads, if it takes even longer
			using var reader = new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read));
			while (reader.ReadLine() is { } line)
			{
				try
				{
					if (line.StartsWith(';')) continue; // comment

					if (line.StartsWith('#'))
					{
						LoadDatabase_Escape(line, inUser: inUser, silent: silent);
						continue;
					}

					if (line.Trim().Length == 0)
					{
						continue;
					}
					var game = ParseCGIRecord(line);
					if (game.Hash is SHA1Checksum.EmptyFile or MD5Checksum.EmptyFile)
					{
						Console.WriteLine($"WARNING: gamedb {path} contains entry for empty rom as \"{game.Name}\"!");
					}
					if (!silent && _builder.TryGetValue(game.Hash, out var dupe))
					{
						Console.WriteLine("gamedb: Multiple hash entries {0}, duplicate detected on \"{1}\" and \"{2}\"", game.Hash, game.Name, dupe.Name);
					}

					_builder[game.Hash] = game;
				}
				catch (FileNotFoundException e) when (e.Message.Contains("missing external game database"))
				{
#if DEBUG
					throw;
#else
					Console.WriteLine(e.Message);
#endif
				}
				catch
				{
					Util.DebugWriteLine($"Error parsing database entry: {line}");
				}
			}
		}

		public static void InitializeDatabase(string bundledRoot, string userRoot, bool silent)
		{
			if (initialized) throw new InvalidOperationException("Did not expect re-initialize of game Database");
			initialized = true;

			_bundledRoot = bundledRoot;
			_userRoot = Directory.Exists(userRoot) ? userRoot : bundledRoot;

			_expected = new DirectoryInfo(_bundledRoot!).EnumerateFiles("*.txt").Select(static fi => fi.Name).ToList();

			var stopwatch = Stopwatch.StartNew();
			ThreadPool.QueueUserWorkItem(_ => {
				InitializeWork(Path.Combine(bundledRoot, "gamedb.txt"), inUser: false, silent: silent);
				DB = _builder.ToFrozenDictionary();
				_builder.Clear();
				if (_expected.Count is not 0) Util.DebugWriteLine($"extra bundled gamedb files were not #included: {string.Join(", ", _expected)}");
#if BIZHAWKBUILD_RUN_ONLY_GAMEDB_INIT
				Console.WriteLine( // should be optimising for measurement so want to print then
#else
				Util.DebugWriteLine( // ...but for most users it's just noise
#endif
					$"GameDB load: {stopwatch.Elapsed} sec");
				_acquire.Set();
			});
		}

		public static GameInfo CheckDatabase(string hash)
		{
#if BIZHAWKBUILD_GAMEDB_ALWAYS_MISS
			_ = hash;
			return null;
#else
			_acquire.WaitOne();

			var hashFormatted = FormatHash(hash);
			_ = DB.TryGetValue(hashFormatted, out var cgi);
			if (cgi == null)
			{
				Console.WriteLine($"DB: hash {hash} not in game database.");
				return null;
			}

			return new GameInfo(cgi);
#endif
		}

		public static GameInfo GetGameInfo(byte[] romData, string fileName)
		{
			var hashSHA1 = SHA1Checksum.ComputeDigestHex(romData);

#if !BIZHAWKBUILD_GAMEDB_ALWAYS_MISS
			_acquire.WaitOne();

			if (DB.TryGetValue(hashSHA1, out var cgi))
			{
				return new GameInfo(cgi);
			}

			var hashMD5 = MD5Checksum.ComputeDigestHex(romData);
			if (DB.TryGetValue(hashMD5, out cgi))
			{
				return new GameInfo(cgi);
			}

			var hashCRC32 = CRC32Checksum.ComputeDigestHex(romData);
			if (DB.TryGetValue(hashCRC32, out cgi))
			{
				return new GameInfo(cgi);
			}
#endif

			// rom is not in database. make some best-guesses
			var game = new GameInfo
			{
				Hash = hashSHA1,
				Status = RomStatus.NotInDatabase,
				NotInDatabase = true
			};

#if !BIZHAWKBUILD_GAMEDB_ALWAYS_MISS
			Console.WriteLine($"Game was not in DB. CRC: {hashCRC32} MD5: {hashMD5}");
#endif

			var ext = Path.GetExtension(fileName)?.ToUpperInvariant();

			switch (ext)
			{
				case ".NES":
				case ".UNF":
				case ".FDS":
					game.System = VSystemID.Raw.NES;
					break;

				case ".SFC":
				case ".SMC":
				case ".BS":
					if (SatellaviewFileTypeDetector.IsSatellaviewRom(romData, out var warnings))
					{
						game.System = VSystemID.Raw.Satellaview;
						foreach (var s in warnings) Console.WriteLine(s);
					}
					else
					{
						game.System = VSystemID.Raw.SNES;
					}
					break;

				case ".GB":
					game.System = VSystemID.Raw.GB;
					break;
				case ".GBC":
					game.System = VSystemID.Raw.GBC;
					break;
				case ".GBA":
					game.System = VSystemID.Raw.GBA;
					break;
				case ".NDS":
					game.System = VSystemID.Raw.NDS;
					break;

				case ".SMS":
					game.System = VSystemID.Raw.SMS;
					break;
				case ".GG":
					game.System = VSystemID.Raw.GG;
					break;
				case ".SG":
					game.System = VSystemID.Raw.SG;
					break;

				case ".GEN":
				case ".MD":
				case ".SMD":
					game.System = VSystemID.Raw.GEN;
					break;

				case ".PSF":
				case ".MINIPSF":
					game.System = VSystemID.Raw.PSX;
					break;

				case ".PCE":
					game.System = VSystemID.Raw.PCE;
					break;
				case ".SGX":
					game.System = VSystemID.Raw.SGX;
					break;

				case ".A26":
					game.System = VSystemID.Raw.A26;
					break;
				case ".A78":
					game.System = VSystemID.Raw.A78;
					break;

				case ".COL":
					game.System = VSystemID.Raw.Coleco;
					break;

				case ".INT":
					game.System = VSystemID.Raw.INTV;
					break;

				case ".PRG":
				case ".D64":
				case ".T64":
				case ".G64":
				case ".CRT":
					game.System = VSystemID.Raw.C64;
					break;

				case ".TZX":
				case ".PZX":
				case ".CSW":
				case ".WAV":
					game.System = VSystemID.Raw.ZXSpectrum;
					break;

				case ".CDT":
					game.System = VSystemID.Raw.AmstradCPC;
					break;

				case ".TAP":
					byte[] head = romData.Take(8).ToArray();
					game.System = Encoding.Default.GetString(head).Contains("C64-TAPE")
						? VSystemID.Raw.C64
						: VSystemID.Raw.ZXSpectrum;
					break;

				case ".Z64":
				case ".V64":
				case ".N64":
				case ".NDD":
					game.System = VSystemID.Raw.N64;
					break;

				case ".DEBUG":
					game.System = VSystemID.Raw.DEBUG;
					break;

				case ".WS":
				case ".WSC":
				case ".PC2":
					game.System = VSystemID.Raw.WSWAN;
					break;

				case ".J64":
				case ".JAG":
					game.System = VSystemID.Raw.Jaguar;
					break;

				case ".LNX":
					game.System = VSystemID.Raw.Lynx;
					break;

				case ".83P":
					game.System = VSystemID.Raw.TI83;
					break;

				case ".DSK":
					var dId = new DskIdentifier(romData);
					game.System = dId.IdentifiedSystem;
					break;

				case ".PO":
				case ".DO":
				case ".NIB":
					game.System = VSystemID.Raw.AppleII;
					break;

				case ".VB":
					game.System = VSystemID.Raw.VB;
					break;

				case ".NGP":
				case ".NGC":
					game.System = VSystemID.Raw.NGP;
					break;

				case ".O2":
					game.System = VSystemID.Raw.O2;
					break;

				case ".UZE":
					game.System = VSystemID.Raw.UZE;
					break;

				case ".TIC":
					game.System = VSystemID.Raw.TIC80;
					break;

				case ".ADF":
				case ".ADZ":
				case ".DMS":
				case ".FDI":
			//	case ".HDF":
			//	case ".LHA":
					game.System = VSystemID.Raw.Amiga;
					break;

				case ".IMA":
				case ".IMG":
					game.System = VSystemID.Raw.DOS;
					break;


				case ".IPF":
					var ipfId = new IpfIdentifier(romData);
					game.System = ipfId.IdentifiedSystem;
					break;

				case ".32X":
					game.System = VSystemID.Raw.Sega32X;
					game.AddOption("32X", "true");
					break;

				case ".VEC":
					game.System = VSystemID.Raw.VEC;
					game.AddOption("VEC", "true");
					break;

				case ".WAD":
					game.System = VSystemID.Raw.Doom;
					break;

				case ".ZIP":
				case ".7Z":
					game.System = VSystemID.Raw.Arcade;
					break;
			}

			game.Name = Path.GetFileNameWithoutExtension(fileName)?.Replace('_', ' ');

			// If filename is all-caps, then attempt to proper-case the title.
#pragma warning disable CA1862 // testing whether it's all-caps
			if (!string.IsNullOrWhiteSpace(game.Name) && game.Name == game.Name.ToUpperInvariant())
#pragma warning restore CA1862
			{
				game.Name = Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(game.Name.ToLowerInvariant());
			}

			return game;
		}
	}

	public class CompactGameInfo
	{
		public string Name { get; set; }
		public string System { get; set; }
		public string MetaData { get; set; }
		public string Hash { get; set; }
		public string Region { get; set; }
		public RomStatus Status { get; set; }
		public string ForcedCore { get; set; }
	}
}
