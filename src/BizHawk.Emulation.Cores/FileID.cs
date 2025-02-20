using System.IO;
using System.Collections.Generic;

//HOW TO USE
//we don't expect anyone to use this fully yet. It's just over-engineered for future use.
//for now, just use it when you truly don't know what to do with a file.
//This system depends heavily on the provided extension. We're not going to exhaustively try every format all the time. If someone loads a cue which is named .sfc, we cant cope with that. 
//However, common mistakes will be handled, on an as-needed basis.

//TODO - check for archives too? further, check archive contents (probably just based on filename)?
//TODO - parameter to enable checks vs firmware, game databases
//TODO (in client) - costly hashes could happen only once the file type is known (and a hash for that filetype could be used)

namespace BizHawk.Emulation.Cores
{
	/// <summary>
	/// Each of these should ideally represent a single file type.
	/// However for now they just may resemble a console, and a core would know how to parse some set of those after making its own determination.
	/// If formats are very similar but with small differences, and that determination can be made, then it will be in the ExtraInfo in the FileIDResult
	/// </summary>
	public enum FileIDType
	{
		None, 
		Multiple, //don't think this makes sense. shouldn't the multiple options be returned?

		Disc, //an unknown disc
		PSX, PSX_EXE, PSF,
		PSP, 
		Saturn, MegaCD,

		PCE, SGX, TurboCD,
		INES, FDS, UNIF, NSF,
		SFC, N64,
		GB, GBC, GBA, NDS,
		COL,
		SG, SMS, GG, S32X,
		SMD, //http://en.wikibooks.org/wiki/Genesis_Programming#ROM_header
		
		WS, WSC, NGC,

		C64, 
		ZXSpectrum,
		AmstradCPC,
		INT,
		A26, A52, A78, JAG, LNX,

		JAD, SBI,
		M3U,

		// Doom IWad/PWad File Types
		WAD,

		//audio codec formats
		WAV, APE, MPC, FLAC,
		MP3, //can't be ID'd very readily..
		
		//misc disc-related files:
		ECM,

	    // DOS Floppy Disk Images
		DOS_FLOPPY
	}

	public class FileIDResult
	{
		public FileIDResult()
		{
		}

		public FileIDResult(FileIDType type, int confidence)
		{
			FileIDType = type;
			Confidence = confidence;
		}

		public FileIDResult(FileIDType type)
		{
			FileIDType = type;
		}

		/// <summary>
		/// a percentage between 0 and 100 assessing the confidence of this result
		/// </summary>
		public int Confidence;

		public FileIDType FileIDType;

		/// <summary>
		/// extra information which could be easily gotten during the file ID (region, suspected homebrew, CRC invalid, etc.)
		/// </summary>
		public readonly IDictionary<string, object> ExtraInfo = new Dictionary<string, object>();
	}

	public class FileIDResults : List<FileIDResult>
	{
		public FileIDResults() { }
		public FileIDResults(FileIDResult item)
		{
			base.Add(item);
		}
		public new void Sort()
		{
			base.Sort((x, y) => x.Confidence.CompareTo(y.Confidence));
		}

		/// <summary>
		/// indicates whether the client should try again after mounting the disc image for further inspection
		/// </summary>
		public bool ShouldTryDisc;
	}

	public class FileID
	{
		/// <summary>
		/// parameters for an Identify job
		/// </summary>
		public class IdentifyParams
		{
			/// <summary>
			/// The extension of the original file (with or without the .)
			/// </summary>
			public string Extension;

			/// <summary>
			/// a seekable stream which can be used
			/// </summary>
			public Stream SeekableStream;

			/// <summary>
			/// the file in question mounted as a disc
			/// </summary>
			public DiscSystem.Disc Disc;
		}

		private class IdentifyJob
		{
			public Stream Stream;
			public string Extension;
			public DiscSystem.Disc Disc;
		}

		/// <summary>
		/// performs wise heuristics to identify a file.
		/// this will attempt to return early if a confident result can be produced.
		/// </summary>
		public FileIDResults Identify(IdentifyParams p)
		{
			IdentifyJob job = new IdentifyJob() { 
				Stream = p.SeekableStream,
				Disc = p.Disc
			};

			//if we have a disc, that's a separate codepath
			if (job.Disc != null)
				return IdentifyDisc(job);

			FileIDResults ret = new FileIDResults();

			string ext = p.Extension;
			if(ext != null)
			{
				ext = ext.TrimStart('.').ToUpperInvariant();
				job.Extension = ext;
			}

			if (job.Extension == "CUE")
			{
				ret.ShouldTryDisc = true;
				return ret;
			}

			if(job.Extension != null)
			{
				//first test everything associated with this extension
				if (ExtensionHandlers.TryGetValue(ext, out var handler))
				{
					foreach (var del in handler.Testers)
					{
						var fidr = del(job);
						if (fidr.FileIDType == FileIDType.None)
							continue;
						ret.Add(fidr);
					}

					ret.Sort();

					//add a low confidence result just based on extension, if it doesnt exist
					if(ret.Find( (x) => x.FileIDType == handler.DefaultForExtension) == null)
					{
						var fidr = new FileIDResult(handler.DefaultForExtension, 5);
						ret.Add(fidr);
					}
				}
			}

			ret.Sort();

			//if we didnt find anything high confidence, try all the testers (TODO)

			return ret;
		}

		/// <summary>
		/// performs wise heuristics to identify a file (simple version)
		/// </summary>
		public FileIDType IdentifySimple(IdentifyParams p)
		{
			var ret = Identify(p);
			if (ret.ShouldTryDisc)
				return FileIDType.Disc;
			if (ret.Count == 0)
				return FileIDType.None;
			else if(ret.Count == 1)
				return ret[0].FileIDType;
			else if (ret[0].Confidence == ret[1].Confidence)
				return FileIDType.Multiple;
			else return ret[0].FileIDType;
		}

		private FileIDResults IdentifyDisc(IdentifyJob job)
		{
			var discIdentifier = new DiscSystem.DiscIdentifier(job.Disc);
			//DiscSystem could use some newer approaches from this file (instead of parsing ISO filesystem... maybe?)
			switch (discIdentifier.DetectDiscType())
			{
				case DiscSystem.DiscType.SegaSaturn:
					return new FileIDResults(new FileIDResult(FileIDType.Saturn, 100));

				case DiscSystem.DiscType.SonyPSP:
					return new FileIDResults(new FileIDResult(FileIDType.PSP, 100));

				case DiscSystem.DiscType.SonyPSX:
					return new FileIDResults(new FileIDResult(FileIDType.PSX, 100));

				case DiscSystem.DiscType.MegaCD:
					return new FileIDResults(new FileIDResult(FileIDType.MegaCD, 100));

				case DiscSystem.DiscType.TurboCD:
					return new FileIDResults(new FileIDResult(FileIDType.TurboCD, 5));

				case DiscSystem.DiscType.UnknownCDFS:
				case DiscSystem.DiscType.UnknownFormat:
				default:
					return new FileIDResults(new FileIDResult());
			}
		}

		private class SimpleMagicRecord
		{
			public int Offset;
			public string Key;
			public int Length = -1;
			public Func<Stream,bool> ExtraCheck;
		}

		//some of these (NES, UNIF for instance) should be lower confidence probably...
		//if you change some of the Length arguments for longer keys, please make notes about why
		private static class SimpleMagics
		{
			public static readonly SimpleMagicRecord INES = new SimpleMagicRecord { Offset = 0, Key = "NES" };
			public static readonly SimpleMagicRecord UNIF = new SimpleMagicRecord { Offset = 0, Key = "UNIF" };
			public static SimpleMagicRecord NSF = new SimpleMagicRecord { Offset = 0, Key = "NESM\x1A" }; 

			public static readonly SimpleMagicRecord FDS_HEADERLESS = new SimpleMagicRecord { Offset = 0, Key = "\x01*NINTENDO-HVC*" };
			public static readonly SimpleMagicRecord FDS_HEADER = new SimpleMagicRecord { Offset = 0, Key = "FDS\x1A" };

			//the GBA nintendo logo.. we'll only use 16 bytes of it but theyre all here, for reference
			//we cant expect these roms to be normally sized, but we may be able to find other features of the header to use for extra checks
			public static readonly SimpleMagicRecord GBA = new SimpleMagicRecord {  Offset = 4, Length = 16, Key = "\x24\xFF\xAE\x51\x69\x9A\xA2\x21\x3D\x84\x82\x0A\x84\xE4\x09\xAD\x11\x24\x8B\x98\xC0\x81\x7F\x21\xA3\x52\xBE\x19\x93\x09\xCE\x20\x10\x46\x4A\x4A\xF8\x27\x31\xEC\x58\xC7\xE8\x33\x82\xE3\xCE\xBF\x85\xF4\xDF\x94\xCE\x4B\x09\xC1\x94\x56\x8A\xC0\x13\x72\xA7\xFC\x9F\x84\x4D\x73\xA3\xCA\x9A\x61\x58\x97\xA3\x27\xFC\x03\x98\x76\x23\x1D\xC7\x61\x03\x04\xAE\x56\xBF\x38\x84\x00\x40\xA7\x0E\xFD\xFF\x52\xFE\x03\x6F\x95\x30\xF1\x97\xFB\xC0\x85\x60\xD6\x80\x25\xA9\x63\xBE\x03\x01\x4E\x38\xE2\xF9\xA2\x34\xFF\xBB\x3E\x03\x44\x78\x00\x90\xCB\x88\x11\x3A\x94\x65\xC0\x7C\x63\x87\xF0\x3C\xAF\xD6\x25\xE4\x8B\x38\x0A\xAC\x72\x21\xD4\xF8\x07" };
			public static readonly SimpleMagicRecord NDS = new SimpleMagicRecord { Offset = 0xC0, Length = 16, Key = "\x24\xFF\xAE\x51\x69\x9A\xA2\x21\x3D\x84\x82\x0A\x84\xE4\x09\xAD\x11\x24\x8B\x98\xC0\x81\x7F\x21\xA3\x52\xBE\x19\x93\x09\xCE\x20\x10\x46\x4A\x4A\xF8\x27\x31\xEC\x58\xC7\xE8\x33\x82\xE3\xCE\xBF\x85\xF4\xDF\x94\xCE\x4B\x09\xC1\x94\x56\x8A\xC0\x13\x72\xA7\xFC\x9F\x84\x4D\x73\xA3\xCA\x9A\x61\x58\x97\xA3\x27\xFC\x03\x98\x76\x23\x1D\xC7\x61\x03\x04\xAE\x56\xBF\x38\x84\x00\x40\xA7\x0E\xFD\xFF\x52\xFE\x03\x6F\x95\x30\xF1\x97\xFB\xC0\x85\x60\xD6\x80\x25\xA9\x63\xBE\x03\x01\x4E\x38\xE2\xF9\xA2\x34\xFF\xBB\x3E\x03\x44\x78\x00\x90\xCB\x88\x11\x3A\x94\x65\xC0\x7C\x63\x87\xF0\x3C\xAF\xD6\x25\xE4\x8B\x38\x0A\xAC\x72\x21\xD4\xF8\x07" };

			public static readonly SimpleMagicRecord GB = new SimpleMagicRecord {  Offset=0x104, Length = 16, Key = "\xCE\xED\x66\x66\xCC\x0D\x00\x0B\x03\x73\x00\x83\x00\x0C\x00\x0D\x00\x08\x11\x1F\x88\x89\x00\x0E\xDC\xCC\x6E\xE6\xDD\xDD\xD9\x99\xBB\xBB\x67\x63\x6E\x0E\xEC\xCC\xDD\xDC\x99\x9F\xBB\xB9\x33\x3E" };

			public static readonly SimpleMagicRecord S32X = new SimpleMagicRecord { Offset = 0x100, Key = "SEGA 32X" };

			public static readonly SimpleMagicRecord SEGAGENESIS = new SimpleMagicRecord { Offset = 0x100, Key = "SEGA GENESIS" };
			public static readonly SimpleMagicRecord SEGAMEGADRIVE = new SimpleMagicRecord { Offset = 0x100, Key = "SEGA MEGA DRIVE" };
			public static readonly SimpleMagicRecord SEGASATURN = new SimpleMagicRecord { Offset = 0, Key = "SEGA SEGASATURN" };
			public static readonly SimpleMagicRecord SEGADISCSYSTEM = new SimpleMagicRecord { Offset = 0, Key = "SEGADISCSYSTEM" };

			public static readonly SimpleMagicRecord PSX = new SimpleMagicRecord { Offset = 0x24E0, Key = "  Licensed  by          Sony Computer Entertainment" }; //there might be other ideas for checking in mednafen sources, if we need them
			public static readonly SimpleMagicRecord PSX_EXE = new SimpleMagicRecord { Key = "PS-X EXE\0" };
			public static readonly SimpleMagicRecord PSP = new SimpleMagicRecord { Offset = 0x8000, Key = "\x01CD001\x01\0x00PSP GAME" };
			public static readonly SimpleMagicRecord PSF = new SimpleMagicRecord { Offset = 0, Key = "PSF\x1" };

			//https://sites.google.com/site/atari7800wiki/a78-header
			public static readonly SimpleMagicRecord A78 = new SimpleMagicRecord { Offset = 0, Key = "\x01ATARI7800" };

			//could be at various offsets?
			public static SimpleMagicRecord TMR_SEGA = new SimpleMagicRecord { Offset = 0x7FF0, Key = "TMR SEGA" };

			public static readonly SimpleMagicRecord SBI = new SimpleMagicRecord { Key = "SBI\0" };
			public static readonly SimpleMagicRecord M3U = new SimpleMagicRecord { Key = "#EXTM3U" }; //note: M3U may not have this. EXTM3U only has it. We'll still catch it by extension though.

			public static readonly SimpleMagicRecord ECM = new SimpleMagicRecord { Key = "ECM\0" };
			public static readonly SimpleMagicRecord FLAC = new SimpleMagicRecord { Key = "fLaC" };
			public static readonly SimpleMagicRecord MPC = new SimpleMagicRecord { Key = "MP+", ExtraCheck = (s) => { s.Position += 3; return s.ReadByte() >= 7; } };
			public static readonly SimpleMagicRecord APE = new SimpleMagicRecord { Key = "MAC " };
			public static readonly SimpleMagicRecord[] WAV = {
				new SimpleMagicRecord { Offset = 0, Key = "RIFF" },
				new SimpleMagicRecord { Offset = 8, Key = "WAVEfmt " }
			};
		}

		private class ExtensionInfo
		{
			public ExtensionInfo(FileIDType defaultForExtension, FormatTester tester)
			{
				Testers = new List<FormatTester>(1);
				if(tester != null)
					Testers.Add(tester);
				DefaultForExtension = defaultForExtension;
			}

			public readonly FileIDType DefaultForExtension;
			public readonly List<FormatTester> Testers;
		}

		/// <summary>
		/// testers to try for each extension, along with a default for the extension
		/// </summary>
		private static readonly Dictionary<string, ExtensionInfo> ExtensionHandlers = new Dictionary<string, ExtensionInfo> {
		  { "NES", new ExtensionInfo(FileIDType.INES, Test_INES ) },
			{ "FDS", new ExtensionInfo(FileIDType.FDS, Test_FDS ) },
			{ "GBA", new ExtensionInfo(FileIDType.GBA, (j)=>Test_Simple(j,FileIDType.GBA,SimpleMagics.GBA) ) },
			{ "NDS", new ExtensionInfo(FileIDType.NDS, (j)=>Test_Simple(j,FileIDType.NDS,SimpleMagics.NDS) ) },
			{ "UNF", new ExtensionInfo(FileIDType.UNIF, Test_UNIF ) },
			{ "UNIF", new ExtensionInfo(FileIDType.UNIF, Test_UNIF ) },
			{ "GB", new ExtensionInfo(FileIDType.GB, Test_GB_GBC ) },
			{ "GBC", new ExtensionInfo(FileIDType.GBC, Test_GB_GBC ) },
			{ "N64", new ExtensionInfo(FileIDType.N64, Test_N64 ) },
			{ "Z64", new ExtensionInfo(FileIDType.N64, Test_N64 ) },
			{ "V64", new ExtensionInfo(FileIDType.N64, Test_N64 ) },
			{ "A78", new ExtensionInfo(FileIDType.A78, Test_A78 ) },
			{ "SMS", new ExtensionInfo(FileIDType.SMS, Test_SMS ) },

			{ "BIN", new ExtensionInfo(FileIDType.Multiple, Test_BIN_ISO ) },
			{ "ISO", new ExtensionInfo(FileIDType.Multiple, Test_BIN_ISO ) },
			{ "M3U", new ExtensionInfo(FileIDType.M3U, (j)=>Test_Simple(j,FileIDType.M3U,SimpleMagics.M3U) ) },

			{ "JAD", new ExtensionInfo(FileIDType.Multiple, Test_JAD_JAC ) },
			{ "JAC", new ExtensionInfo(FileIDType.Multiple, Test_JAD_JAC ) },
			{ "SBI", new ExtensionInfo(FileIDType.SBI, (j)=>Test_Simple(j,FileIDType.SBI,SimpleMagics.SBI) ) },

			{ "EXE", new ExtensionInfo(FileIDType.PSX_EXE, (j)=>Test_Simple(j,FileIDType.PSX_EXE,SimpleMagics.PSX_EXE) ) },

			//royal mess
			{ "MD", new ExtensionInfo(FileIDType.SMD, null ) },
			{ "SMD", new ExtensionInfo(FileIDType.SMD, null ) },
			{ "GEN", new ExtensionInfo(FileIDType.SMD, null ) },

			//nothing yet...
			{ "PSF", new ExtensionInfo(FileIDType.PSF, (j)=>Test_Simple(j,FileIDType.PSF,SimpleMagics.PSF) ) },
			{ "INT", new ExtensionInfo(FileIDType.INT, null) },
			{ "SFC", new ExtensionInfo(FileIDType.SFC, null) },
			{ "SMC", new ExtensionInfo(FileIDType.SFC, null) },
			{ "JAG", new ExtensionInfo(FileIDType.JAG, null ) },
			{ "LNX", new ExtensionInfo(FileIDType.LNX, null ) },
			{ "SG", new ExtensionInfo(FileIDType.SG, null ) },
			{ "SGX", new ExtensionInfo(FileIDType.SGX, null ) },
			{ "COL", new ExtensionInfo(FileIDType.COL, null ) },
			{ "A52", new ExtensionInfo(FileIDType.A52, null ) },
			{ "A26", new ExtensionInfo(FileIDType.A26, null ) },
			{ "PCE", new ExtensionInfo(FileIDType.PCE, null ) },
			{ "GG", new ExtensionInfo(FileIDType.GG, null ) },
			{ "WS", new ExtensionInfo(FileIDType.WS, null ) },
			{ "WSC", new ExtensionInfo(FileIDType.WSC, null ) },
			{ "NGC", new ExtensionInfo(FileIDType.NGC, null ) },
			{ "32X", new ExtensionInfo(FileIDType.S32X, (j)=>Test_Simple(j,FileIDType.S32X,SimpleMagics.S32X) ) },

			//various C64 formats.. can we distinguish between these?
			{ "PRG", new ExtensionInfo(FileIDType.C64, null ) },
			{ "D64", new ExtensionInfo(FileIDType.C64, null ) },
			{ "T64", new ExtensionInfo(FileIDType.C64, null ) },
			{ "G64", new ExtensionInfo(FileIDType.C64, null ) },
			{ "CRT", new ExtensionInfo(FileIDType.C64, null ) },
			{ "NIB", new ExtensionInfo(FileIDType.C64, null ) }, //not supported yet

			// DOS Floppy Disks
			{ "IMA", new ExtensionInfo(FileIDType.DOS_FLOPPY, null ) },
			{ "IMG", new ExtensionInfo(FileIDType.DOS_FLOPPY, null ) },
			
			// Doom IWad / PWad
			{ "WAD", new ExtensionInfo(FileIDType.WAD, null ) },

			//for now
			{ "ROM", new ExtensionInfo(FileIDType.Multiple, null ) }, //could be MSX too

			{ "MP3", new ExtensionInfo(FileIDType.MP3, null ) },
			{ "WAV", new ExtensionInfo(FileIDType.WAV, (j)=>Test_Simple(j,FileIDType.WAV,SimpleMagics.WAV) ) },
			{ "APE", new ExtensionInfo(FileIDType.APE, (j)=>Test_Simple(j,FileIDType.APE,SimpleMagics.APE) ) },
			{ "MPC", new ExtensionInfo(FileIDType.MPC, (j)=>Test_Simple(j,FileIDType.MPC,SimpleMagics.MPC) ) },
			{ "FLAC", new ExtensionInfo(FileIDType.FLAC, (j)=>Test_Simple(j,FileIDType.FLAC,SimpleMagics.FLAC) ) },
			{ "ECM", new ExtensionInfo(FileIDType.ECM, (j)=>Test_Simple(j,FileIDType.ECM,SimpleMagics.ECM) ) },
		};

		private delegate FileIDResult FormatTester(IdentifyJob job);

		private static readonly int[] no_offsets = { 0 };

		/// <summary>
		/// checks for the magic string (bytewise ASCII check) at the given address
		/// </summary>
		private static bool CheckMagic(Stream stream, IEnumerable<SimpleMagicRecord> recs, params int[] offsets)
		{
			if (offsets.Length == 0)
				offsets = no_offsets;

			foreach (int n in offsets)
			{
				bool ok = true;
				foreach (var r in recs)
				{
					if (!CheckMagicOne(stream, r, n))
					{
						ok = false;
						break;
					}
				}
				if (ok) return true;
			}
			return false;
		}

		private static bool CheckMagic(Stream stream, SimpleMagicRecord rec, params int[] offsets)
		{
			return CheckMagic(stream, new SimpleMagicRecord[] { rec }, offsets);
		}

		private static bool CheckMagicOne(Stream stream, SimpleMagicRecord rec, int offset)
		{
			stream.Position = rec.Offset + offset;
			string key = rec.Key;
			int len = rec.Length;
			if (len == -1)
				len = key.Length;
			for (int i = 0; i < len; i++)
			{
				int n = stream.ReadByte();
				if (n == -1) return false;
				if (n != key[i])
					return false;
			}
			if (rec.ExtraCheck != null)
			{
				stream.Position = rec.Offset + offset;
				return rec.ExtraCheck(stream);
			}
			else return true;
		}

		private static int ReadByte(Stream stream, int ofs)
		{
			stream.Position = ofs;
			return stream.ReadByte();
		}

		private static FileIDResult Test_INES(IdentifyJob job)
		{
			if (!CheckMagic(job.Stream, SimpleMagics.INES))
				return new FileIDResult();

			var ret = new FileIDResult(FileIDType.INES, 100);

			//an INES file should be a multiple of 8k, with the 16 byte header.
			//if it isnt.. this is fishy.
			if (((job.Stream.Length - 16) & (8 * 1024 - 1)) != 0)
				ret.Confidence = 50;

			return ret;
		}

		private static FileIDResult Test_FDS(IdentifyJob job)
		{
			if (CheckMagic(job.Stream, SimpleMagics.FDS_HEADER))
				return new FileIDResult(FileIDType.FDS, 90); //kind of a small header..
			if (CheckMagic(job.Stream, SimpleMagics.FDS_HEADERLESS))
				return new FileIDResult(FileIDType.FDS, 95);

			return new FileIDResult();
		}

		/// <summary>
		/// all magics must pass
		/// </summary>
		private static FileIDResult Test_Simple(IdentifyJob job, FileIDType type, SimpleMagicRecord[] magics)
		{
			var ret = new FileIDResult(type);

			if (CheckMagic(job.Stream, magics))
				return new FileIDResult(type, 100);
			else
				return new FileIDResult();
		}

		private static FileIDResult Test_Simple(IdentifyJob job, FileIDType type, SimpleMagicRecord magic)
		{
			var ret = new FileIDResult(type);

			if (CheckMagic(job.Stream, magic))
				return new FileIDResult(type, 100);
			else
				return new FileIDResult();
		}

		private static FileIDResult Test_UNIF(IdentifyJob job)
		{
			if (!CheckMagic(job.Stream, SimpleMagics.UNIF))
				return new FileIDResult();

			//TODO - simple parser (for starters, check for a known chunk being next, see http://wiki.nesdev.com/w/index.php/UNIF)

			var ret = new FileIDResult(FileIDType.UNIF, 100);

			return ret;
		}

		private static FileIDResult Test_GB_GBC(IdentifyJob job)
		{
			if (!CheckMagic(job.Stream, SimpleMagics.GB))
				return new FileIDResult();

			var ret = new FileIDResult(FileIDType.GB, 100);
			int type = ReadByte(job.Stream, 0x143);
			if ((type & 0x80) != 0)
				ret.FileIDType = FileIDType.GBC;

			//could check cart type and rom size for extra info if necessary

			return ret;
		}

		private static FileIDResult Test_SMS(IdentifyJob job)
		{
			//http://www.smspower.org/Development/ROMHeader

			//actually, not sure how to handle this yet
			return new FileIDResult();
		}

		private static FileIDResult Test_N64(IdentifyJob job)
		{
			//  .Z64 = No swapping
			//  .N64 = Word Swapped
			//  .V64 = Byte Swapped

			//not sure how to check for these yet...
			var ret = new FileIDResult(FileIDType.N64, 5);
			if (job.Extension == "V64") ret.ExtraInfo["byteswap"] = true;
			if (job.Extension == "N64") ret.ExtraInfo["wordswap"] = true;
			return ret;
		}

		private static FileIDResult Test_A78(IdentifyJob job)
		{
			int len = (int)job.Stream.Length;
			
			//we may have a header to analyze
			if (len % 1024 == 128)
			{
				if (CheckMagic(job.Stream, SimpleMagics.A78))
					new FileIDResult(FileIDType.A78, 100);
			}
			else if (len % 1024 == 0)
			{
			}
			else { }

			return new FileIDResult(0);
		}

		private static FileIDResult Test_BIN_ISO(IdentifyJob job)
		{
			//ok, this is complicated.
			//there are lots of mislabeled bins/isos so lets just treat them the same (mostly)
			//if the BIN cant be recognized, but it is small, it is more likely some other rom BIN than a disc (turbocd or other)

			if (job.Extension == "BIN")
			{
				//first we can check for SMD magic words.
				//since this extension is ambiguous, we can't be completely sure about it. but it's almost surely accurate
				if (CheckMagic(job.Stream, SimpleMagics.SEGAGENESIS))
					return new FileIDResult(FileIDType.SMD, 95) { ExtraInfo = { ["type"] = "genesis" } };
				if (CheckMagic(job.Stream, SimpleMagics.SEGAMEGADRIVE))
					return new FileIDResult(FileIDType.SMD, 95) { ExtraInfo = { ["type"] = "megadrive" } };
			}

			//well... guess it's a disc.
			//since it's just a bin, we don't need the user to provide a DiscSystem disc.
			//lets just analyze this as best we can.
			//of course, it's a lot of redundant logic with the discsystem disc checker.
			//but you kind of need different approaches when loading a hugely unstructured bin file
			//discsystem won't really even be happy mounting a .bin anyway, and we don't want it to be

			//for PSX, we have a magic word to look for.
			//it's at 0x24E0 with a mode2 (2352 byte) track 1.
			//what if its 2048 byte? 
			//i found a ".iso" which was actually 2352 byte sectors..
			//found a hilarious ".bin.iso" which was actually 2352 byte sectors
			//so, I think it's possible that every valid PSX disc is mode2 in the track 1
			if (CheckMagic(job.Stream, SimpleMagics.PSX))
			{
				var ret = new FileIDResult(FileIDType.PSX, 95);
				//this is an unreliable way to get a PSX game!
				ret.ExtraInfo["unreliable"] = true;
				return ret;
			}

			//it's not proven that this is reliable. this is actually part of the Mode 1 CDFS header. perhaps it's mobile?
			//if it's mobile, we'll need to mount it as an ISO file here via discsystem
			if (CheckMagic(job.Stream, SimpleMagics.PSP))
				return new FileIDResult(FileIDType.PSP, 95);

			//if this was an ISO, we might discover the magic word at offset 0...
			//if it was a mode2/2352 bin, we might discover it at offset 16 (after the data sector header)
			if(CheckMagic(job.Stream, SimpleMagics.SEGASATURN,0))
				return new FileIDResult(FileIDType.Saturn, job.Extension == "ISO" ? 95 : 90);
			if (CheckMagic(job.Stream, SimpleMagics.SEGASATURN, 16))
				return new FileIDResult(FileIDType.Saturn, job.Extension == "BIN" ? 95 : 90);
			if (CheckMagic(job.Stream, SimpleMagics.SEGADISCSYSTEM, 0))
				return new FileIDResult(FileIDType.MegaCD, job.Extension == "ISO" ? 95 : 90);
			if (CheckMagic(job.Stream, SimpleMagics.SEGADISCSYSTEM, 16))
				return new FileIDResult(FileIDType.MegaCD, job.Extension == "BIN" ? 95 : 90);

			if (job.Extension == "ISO")
				return new FileIDResult(FileIDType.Disc, 1);
			else
				return new FileIDResult(FileIDType.Multiple, 1);
		}

		private static FileIDResult Test_JAD_JAC(IdentifyJob job)
		{
			//TBD
			//just mount it as a disc and send it through the disc checker?
			return null;
		}

	}
}
