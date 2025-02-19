using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

using BizHawk.Common.PathExtensions;
using BizHawk.Common.StringExtensions;

using ISOParser;

namespace BizHawk.Emulation.DiscSystem
{
	/// <summary>
	/// Parsing Alcohol 120% files
	/// Info taken from:
	/// * http://forum.redump.org/post/41803/#p41803
	/// * Libmirage image-mds parser - https://sourceforge.net/projects/cdemu/files/libmirage/
	/// * DiscImageChef -   https://github.com/claunia/DiscImageChef/blob/master/DiscImageChef.DiscImages/Alcohol120.cs
	/// </summary>
	public class MDS_Format
	{
		/// <summary>
		/// A loose representation of an Alcohol 120 .mds file (with a few extras)
		/// </summary>
		public class AFile
		{
			/// <summary>
			/// Full path to the MDS file
			/// </summary>
			public string MDSPath;

			/// <summary>
			/// MDS Header
			/// </summary>
			public AHeader Header = new();

			/// <summary>
			/// List of MDS session blocks
			/// </summary>
			public readonly IList<ASession> Sessions = new List<ASession>();

			/// <summary>
			/// List of track blocks
			/// </summary>
			public readonly IList<ATrack> Tracks = new List<ATrack>();

			/// <summary>
			/// Current parsed session objects
			/// </summary>
			public List<Session> ParsedSession = new();

			/// <summary>
			/// Calculated MDS TOC entries (still to be parsed into BizHawk)
			/// </summary>
			public readonly IList<ATOCEntry> TOCEntries = new List<ATOCEntry>();
		}

		public class AHeader
		{
			/// <summary>
			/// Standard alcohol 120% signature - usually "MEDIA DESCRIPTOR"
			/// </summary>
			public string Signature;                // 16 bytes

			/// <summary>
			/// Alcohol version?
			/// </summary>
			public byte[] Version;                  // 2 bytes

			/// <summary>
			/// The medium type
			/// * 0x00  -   CD
			/// * 0x01  -   CD-R
			/// * 0x02  -   CD-RW
			/// * 0x10  -   DVD
			/// * 0x12  -   DVD-R
			/// </summary>
			public int Medium;

			/// <summary>
			/// Number of sessions
			/// </summary>
			public int SessionCount;

			/// <summary>
			/// Burst Cutting Area length
			/// </summary>
			public int BCALength;

			/// <summary>
			/// Burst Cutting Area data offset
			/// </summary>
			public long BCAOffset;

			/// <summary>
			/// Offset to disc (DVD?) structures
			/// </summary>
			public long StructureOffset;

			/// <summary>
			/// Offset to the first session block
			/// </summary>
			public long SessionOffset;

			/// <summary>
			/// Data Position Measurement offset
			/// </summary>
			public long DPMOffset;

			/// <summary>
			/// Parse mds stream for the header
			/// </summary>
			public AHeader Parse(Stream stream)
			{
				var bc = EndianBitConverter.CreateForLittleEndian();
				
				var header = new byte[88];
				_ = stream.Read(header, offset: 0, count: header.Length); // stream size checked at callsite

				this.Signature = Encoding.ASCII.GetString(header.Take(16).ToArray());
				this.Version = header.Skip(16).Take(2).ToArray();
				this.Medium = bc.ToInt16(header.Skip(18).Take(2).ToArray());
				this.SessionCount = bc.ToInt16(header.Skip(20).Take(2).ToArray());
				this.BCALength = bc.ToInt16(header.Skip(26).Take(2).ToArray());
				this.BCAOffset = bc.ToInt32(header.Skip(36).Take(4).ToArray());
				this.StructureOffset = bc.ToInt32(header.Skip(64).Take(4).ToArray());
				this.SessionOffset = bc.ToInt32(header.Skip(80).Take(4).ToArray());
				this.DPMOffset = bc.ToInt32(header.Skip(84).Take(4).ToArray());

				return this;
			}
		}

		/// <summary>
		/// MDS session block representation
		/// </summary>
		public class ASession
		{
			public int SessionStart;        /* Session's start address */
			public int SessionEnd;          /* Session's end address */
			public int SessionNumber;       /* Session number */
			public byte AllBlocks;          /* Number of all data blocks. */
			public byte NonTrackBlocks;     /* Number of lead-in data blocks */
			public int FirstTrack;          /* First track in session */
			public int LastTrack;           /* Last track in session */
			public long TrackOffset;       /* Offset of lead-in+regular track data blocks. */
		}

		/// <summary>
		/// Representation of an MDS track block
		/// For convenience (and extra confusion) this also holds the track extrablock, filename(footer) block infos
		/// as well as the calculated image filepath as specified in the MDS file
		/// </summary>
		public class ATrack
		{
			/// <summary>
			/// The specified data mode (only lower 3 bits are actually meaningful)
			/// 0x00    -   None (no data)
			/// 0x02    -   DVD (when header specifies DVD, Mode1 otherwise)
			/// 0xA9    -   Audio
			/// 0xAA    -   Mode1
			/// 0xAB    -   Mode2
			/// 0xAC    -   Mode2 Form1
			/// 0xAD    -   Mode2 Form2
			/// </summary>
			public byte Mode;               /* Track mode */

			/// <summary>
			/// Subchannel mode for the track (0x00 = None, 0x08 = Interleaved)
			/// </summary>
			public byte SubMode;            /* Subchannel mode */

			/* These are the fields from Sub-channel Q information, which are
                also returned in full TOC by READ TOC/PMA/ATIP command */
			public int ADR_Control;         /* Adr/Ctl */
			public int TrackNo;             /* Track number field */
			public int Point;               /* Point field (= track number for track entries) */
			public int AMin;                /* Min */
			public int ASec;                /* Sec */
			public int AFrame;              /* Frame */
			public int Zero;                /* Zero */
			public int PMin;                /* PMin */
			public int PSec;                /* PSec */
			public int PFrame;              /* PFrame */

			public long ExtraOffset;       /* Start offset of this track's extra block. */
			public int SectorSize;          /* Sector size. */
			public long PLBA;               /* Track start sector (PLBA). */
			public ulong StartOffset;       /* Track start offset (from beginning of MDS file) */
			public long Files;             /* Number of filenames for this track */
			public long FooterOffset;      /* Start offset of footer (from beginning of MDS file) */

			/// <summary>
			/// Track extra block
			/// </summary>
			public readonly ATrackExtra ExtraBlock = new();

			/// <summary>
			/// List of footer(filename) blocks for this track
			/// </summary>
			public List<AFooter> FooterBlocks = new();

			/// <summary>
			/// List of the calculated full paths to this track's image file
			/// The MDS file itself may contain a filename, or just an *.extension
			/// </summary>
			public List<string> ImageFileNamePaths = new();

			public int BlobIndex;
		}

		/// <summary>
		/// Extra track block
		/// </summary>
		public class ATrackExtra
		{
			public long Pregap;            /* Number of sectors in pregap. */
			public long Sectors;           /* Number of sectors in track. */
		}

		/// <summary>
		/// Footer (filename) block - potentially one for every track
		/// </summary>
		public class AFooter
		{
			public long FilenameOffset;    /* Start offset of image filename string (from beginning of mds file) */
			public long WideChar;          /* Seems to be set to 1 if widechar filename is used */
		}

		/// <summary>
		/// Represents a parsed MDS TOC entry
		/// </summary>
		public class ATOCEntry
		{
			public ATOCEntry(int entryNum)
			{
				EntryNum = entryNum;
			}

			/// <summary>
			/// these should be 0-indexed
			/// </summary>
			public int EntryNum;


			/// <summary>
			/// 1-indexed - the session that this entry belongs to
			/// </summary>
			public int Session;

//          /// <summary>
//          /// this seems just to be the LBA corresponding to AMIN:ASEC:AFRAME (give or take 150). It's not stored on the disc, and it's redundant.
//          /// </summary>
//          public int ALBA;

			/// <summary>
			/// this seems just to be the LBA corresponding to PMIN:PSEC:PFRAME (give or take 150).
			/// </summary>
			public int PLBA;

			//these correspond pretty directly to values in the Q subchannel fields
			//NOTE: they're specified as absolute MSF. That means, they're 2 seconds off from what they should be when viewed as final TOC values
			public int ADR_Control;
			public int TrackNo;
			public int Point;
			public int AMin;
			public int ASec;
			public int AFrame;
			public int Zero;
			public int PMin;
			public int PSec;
			public int PFrame;

			/// <summary>
			/// Lower 3 bits of ATrack Mode
			/// Upper 5 bits are meaningless (see mirage_parser_mds_convert_track_mode)
			/// 0x0 - None or Mode2 (Depends on sector size)
			/// 0x1 - Audio
			/// 0x2 - DVD or Mode1 (Depends on medium)
			/// 0x3 - Mode2
			/// 0x4 - Mode2 Form1
			/// 0x5 - Mode2 Form2
			/// 0x6 - UNKNOWN
			/// 0x7 - Mode2
			/// </summary>
			public int TrackMode;

			public int SectorSize;
			public long TrackOffset;

			/// <summary>
			/// List of the calculated full paths to this track's image file
			/// The MDS file itself may contain a filename, or just an *.extension
			/// </summary>
			public List<string> ImageFileNamePaths = new();

			/// <summary>
			/// Track extra block
			/// </summary>
			public ATrackExtra ExtraBlock = new();

			public int BlobIndex;
		}

		/// <exception cref="MDSParseException">header is malformed or identifies file as MDS 2.x, or any track has a DVD mode</exception>
		public static AFile Parse(FileStream stream)
		{
			var bc = EndianBitConverter.CreateForLittleEndian();
			var isDvd = false;

			var aFile = new AFile { MDSPath = stream.Name };

			stream.Seek(0, SeekOrigin.Begin);

			// check whether the header in the mds file is long enough
			if (stream.Length < 88) throw new MDSParseException("Malformed MDS format: The descriptor file does not appear to be long enough.");

			// parse header
			aFile.Header = aFile.Header.Parse(stream);

			// check version to make sure this is only v1.x
			// currently NO support for version 2.x

			if (aFile.Header.Version[0] > 1)
			{
				throw new MDSParseException($"MDS Parse Error: Only MDS version 1.x is supported!\nDetected version: {aFile.Header.Version[0]}.{aFile.Header.Version[1]}");
			}

			isDvd = aFile.Header.Medium is 0x10 or 0x12;
			if (isDvd)
			{
				throw new MDSParseException("DVD Detected. Not currently supported!");
			}

			// parse sessions
			var aSessions = new Dictionary<int, ASession>();

			stream.Seek(aFile.Header.SessionOffset, SeekOrigin.Begin);
			for (var se = 0; se < aFile.Header.SessionCount; se++)
			{
				var sessionHeader = new byte[24];
				var bytesRead = stream.Read(sessionHeader, offset: 0, count: sessionHeader.Length);
				Debug.Assert(bytesRead == sessionHeader.Length, "reached end-of-file while reading session header");
				//sessionHeader.Reverse().ToArray();

				var session = new ASession
				{
					SessionStart = bc.ToInt32(sessionHeader.Take(4).ToArray()),
					SessionEnd = bc.ToInt32(sessionHeader.Skip(4).Take(4).ToArray()),
					SessionNumber = bc.ToInt16(sessionHeader.Skip(8).Take(2).ToArray()),
					AllBlocks = sessionHeader[10],
					NonTrackBlocks = sessionHeader[11],
					FirstTrack = bc.ToInt16(sessionHeader.Skip(12).Take(2).ToArray()),
					LastTrack = bc.ToInt16(sessionHeader.Skip(14).Take(2).ToArray()),
					TrackOffset = bc.ToInt32(sessionHeader.Skip(20).Take(4).ToArray())
				};

				//mdsf.Sessions.Add(session);
				aSessions.Add(session.SessionNumber, session);
			}

			long footerOffset = 0;

			// parse track blocks
			var aTracks = new Dictionary<int, ATrack>();

			// iterate through each session block
			foreach (var session in aSessions.Values)
			{
				stream.Seek(session.TrackOffset, SeekOrigin.Begin);
				//Dictionary<int, ATrack> sessionToc = new Dictionary<int, ATrack>();

				// iterate through every block specified in each session
				for (var bl = 0; bl < session.AllBlocks; bl++)
				{
					var trackHeader = new byte[80];
					var track = new ATrack();

					var bytesRead = stream.Read(trackHeader, offset: 0, count: trackHeader.Length);
					Debug.Assert(bytesRead == trackHeader.Length, "reached end-of-file while reading track header");

					track.Mode = trackHeader[0];
					track.SubMode = trackHeader[1];
					track.ADR_Control = trackHeader[2];
					track.TrackNo = trackHeader[3];
					track.Point = trackHeader[4];
					track.AMin = trackHeader[5];
					track.ASec = trackHeader[6];
					track.AFrame = trackHeader[7];
					track.Zero = trackHeader[8];
					track.PMin = trackHeader[9];
					track.PSec = trackHeader[10];
					track.PFrame = trackHeader[11];
					track.ExtraOffset = bc.ToInt32(trackHeader.Skip(12).Take(4).ToArray());
					track.SectorSize = bc.ToInt16(trackHeader.Skip(16).Take(2).ToArray());
					track.PLBA = bc.ToInt32(trackHeader.Skip(36).Take(4).ToArray());
					track.StartOffset = MemoryMarshal.Read<ulong>(trackHeader.AsSpan(start: 12 + sizeof(int) + sizeof(short) + 18 + sizeof(int)));
					track.Files = bc.ToInt32(trackHeader.Skip(48).Take(4).ToArray());
					track.FooterOffset = bc.ToInt32(trackHeader.Skip(52).Take(4).ToArray());

					// check for track extra block - this can probably be handled in a separate loop,
					// but I'll just store the current stream position then seek forward to the extra block for this track
					var currPos = stream.Position;

					// Only CDs have extra blocks - for DVDs ExtraOffset = track length
					if (track.ExtraOffset > 0 && !isDvd)
					{
						var extHeader = new byte[8];
						stream.Seek(track.ExtraOffset, SeekOrigin.Begin);
						var bytesRead1 = stream.Read(extHeader, offset: 0, count: extHeader.Length);
						Debug.Assert(bytesRead1 == extHeader.Length, "reached end-of-file while reading extra block of track");
						track.ExtraBlock.Pregap = bc.ToInt32(extHeader.Take(4).ToArray());
						track.ExtraBlock.Sectors = bc.ToInt32(extHeader.Skip(4).Take(4).ToArray());
						stream.Seek(currPos, SeekOrigin.Begin);
					}
					else if (isDvd)
					{
						track.ExtraBlock.Sectors = track.ExtraOffset;
					}

					// read the footer/filename block for this track
					currPos = stream.Position;
					var numOfFilenames = track.Files;
					for (long fi = 1; fi <= numOfFilenames; fi++)
					{
						// skip leadin/out info tracks
						if (track.FooterOffset == 0)
							continue;

						var foot = new byte[16];
						stream.Seek(track.FooterOffset, SeekOrigin.Begin);
						var bytesRead1 = stream.Read(foot, offset: 0, count: foot.Length);
						Debug.Assert(bytesRead1 == foot.Length, "reached end-of-file while reading track footer");

						var f = new AFooter
						{
							FilenameOffset = bc.ToInt32(foot.Take(4).ToArray()),
							WideChar = bc.ToInt32(foot.Skip(4).Take(4).ToArray())
						};
						track.FooterBlocks.Add(f);
						track.FooterBlocks = track.FooterBlocks.Distinct().ToList();

						// parse the filename string
						var fileName = "*.mdf";
						if (f.FilenameOffset > 0)
						{
							// filename offset is present
							stream.Seek(f.FilenameOffset, SeekOrigin.Begin);
							byte[] fname;

							if (numOfFilenames == 1)
							{
								if (aFile.Header.DPMOffset == 0)
								{
									// filename is in the remaining space to EOF
									fname = new byte[stream.Length - stream.Position];
								}
								else
								{
									// filename is in the remaining space to EOF + dpm offset
									fname = new byte[aFile.Header.DPMOffset - stream.Position];
								}
							}

							else
							{
								// looks like each filename string is 6 bytes with a trailing \0
								fname = new byte[6];
							}
							

							// read the filename
							var bytesRead2 = stream.Read(fname, offset: 0, count: fname.Length);
							Debug.Assert(bytesRead2 == fname.Length, "reached end-of-file while reading track filename");

							// if widechar is 1 filename is stored using 16-bit, otherwise 8-bit is used
							if (f.WideChar == 1)
								fileName = Encoding.Unicode.GetString(fname).TrimEnd('\0');
							else
								fileName = Encoding.Default.GetString(fname).TrimEnd('\0');
						}
						else
						{
							// assume an MDF file with the same name as the MDS
						}

						var (dir, fileNoExt, _) = aFile.MDSPath.SplitPathToDirFileAndExt();

						if (f.FilenameOffset is 0
							|| "*.mdf".EqualsIgnoreCase(fileName))
						{
							fileName = $@"{dir}\{fileNoExt}.mdf";
						}
						else
						{
							fileName = $@"{dir}\{fileName}";
						}

						track.ImageFileNamePaths.Add(fileName);
						track.ImageFileNamePaths = track.ImageFileNamePaths.Distinct().ToList();
					}

					stream.Position = currPos;

					var point = track.Point;
					// each session has its own 0xA0/0xA1/0xA3 track
					// so this can't be used directly as a key
					if (point is 0xA0 or 0xA1 or 0xA2)
					{
						point |= session.SessionNumber << 8;
					}

					aTracks.Add(point, track);
					aFile.Tracks.Add(track);

					if (footerOffset == 0)
						footerOffset = track.FooterOffset;
				}
			}

			
			// build custom session object
			aFile.ParsedSession = new();
			foreach (var s in aSessions.Values)
			{
				var session = new Session();

				if (!aTracks.TryGetValue(s.FirstTrack, out var startTrack))
				{
					break;
				}

				if (!aTracks.TryGetValue(s.LastTrack, out var endTrack))
				{
					break;
				}

				session.StartSector = startTrack.PLBA;
				session.StartTrack = s.FirstTrack;
				session.SessionSequence = s.SessionNumber;
				session.EndSector = endTrack.PLBA + endTrack.ExtraBlock.Sectors - 1;
				session.EndTrack = s.LastTrack;

				aFile.ParsedSession.Add(session);
			}

			// now build the TOC object
			foreach (var se in aFile.ParsedSession)
			{
				ATOCEntry CreateTOCEntryFromTrack(ATrack track)
				{
					return new(track.Point)
					{
						ADR_Control = track.ADR_Control,
						AFrame = track.AFrame,
						AMin = track.AMin,
						ASec = track.ASec,
						BlobIndex = track.BlobIndex,
						EntryNum = track.TrackNo,
						ExtraBlock = track.ExtraBlock,
						ImageFileNamePaths = track.ImageFileNamePaths,
						PFrame = track.PFrame,
						PLBA = Convert.ToInt32(track.PLBA),
						PMin = track.PMin,
						Point = track.Point,
						PSec = track.PSec,
						TrackMode = track.Mode & 0x7,
						SectorSize = track.SectorSize,
						Session = se.SessionSequence,
						TrackOffset = Convert.ToInt64(track.StartOffset),
						Zero = track.Zero
					};
				}

				void AddAXTrack(int x)
				{
					if (aTracks.TryGetValue(se.SessionSequence << 8 | 0xA0 | x, out var axTrack))
					{
						aFile.TOCEntries.Add(CreateTOCEntryFromTrack(axTrack));
					}
				}

				// add in the 0xA0/0xA1/0xA2 tracks
				AddAXTrack(0);
				AddAXTrack(1);
				AddAXTrack(2);

				// add in the rest of the tracks
				foreach (var t in aTracks
							.Where(a => se.StartTrack <= a.Key && a.Key <= se.EndTrack)
							.OrderBy(a => a.Key)
							.Select(a => a.Value))
				{
					aFile.TOCEntries.Add(CreateTOCEntryFromTrack(t));
				}

				// TODO: first session might have 0xB0/0xC0 tracks... not sure how to handle these
			}

			return aFile;
		}

		/// <summary>
		/// Custom session object
		/// </summary>
		public class Session
		{
			public long StartSector;
			public int StartTrack;
			public int SessionSequence;
			public long EndSector;
			public int EndTrack;
		}


		public class MDSParseException : Exception
		{
			public MDSParseException(string message) : base(message) { }
		}
		

		public class LoadResults
		{
			public List<RawTOCEntry> RawTOCEntries;
			public AFile ParsedMDSFile;
			public bool Valid;
			public MDSParseException FailureException;
			public string MdsPath;
		}

		public static LoadResults LoadMDSPath(string path)
		{
			var ret = new LoadResults { MdsPath = path };
			//ret.MdfPath = Path.ChangeExtension(path, ".mdf");
			try
			{
				if (!File.Exists(path)) throw new MDSParseException("Malformed MDS format: nonexistent MDS file!");

				AFile mdsf;
				using (var infMDS = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
					mdsf = Parse(infMDS);

				ret.ParsedMDSFile = mdsf;

				ret.Valid = true;
			}
			catch (MDSParseException ex)
			{
				ret.FailureException = ex;
			}

			return ret;
		}

		/// <exception cref="MDSParseException">path reference no longer points to file</exception>
		private static Dictionary<int, IBlob> MountBlobs(AFile mdsf, Disc disc)
		{
			Debug.Assert(disc.DisposableResources.Count is 0, "no other method should be adding to DisposableResources");
			var BlobIndex = new Dictionary<int, IBlob>();

			var count = 0;
			foreach (var track in mdsf.Tracks)
			{
				foreach (var file in track.ImageFileNamePaths.Distinct())
				{
					if (!File.Exists(file))
						throw new MDSParseException($"Malformed MDS format: nonexistent image file: {file}");

					//mount the file
					if (!disc.DisposableResources.Cast<Blob_RawFile>()
						.Select(static re => re.PhysicalPath).Contains(file))
					{
						Blob_RawFile mdfBlob = new() { PhysicalPath = file };
						// wrap in zeropadadapter
						disc.DisposableResources.Add(mdfBlob);
						BlobIndex[count++] = mdfBlob;
					}
				}
			}

			return BlobIndex;
		}

		private static RawTOCEntry EmitRawTOCEntry(ATOCEntry entry)
		{
			//this should actually be zero. im not sure if this is stored as BCD2 or not
			var tno = BCD2.FromDecimal(entry.TrackNo);

			//these are special values.. I think, taken from this:
			//http://www.staff.uni-mainz.de/tacke/scsi/SCSI2-14.html
			//the CCD will contain Points as decimal values except for these specially converted decimal values which should stay as BCD.
			//Why couldn't they all be BCD? I don't know. I guess because BCD is inconvenient, but only A0 and friends have special meaning. It's confusing.
			var ino = BCD2.FromDecimal(entry.Point);
			ino.BCDValue = entry.Point switch
			{
				0xA0 or 0xA1 or 0xA2 => (byte)entry.Point,
				_ => ino.BCDValue
			};

			// get ADR & Control from ADR_Control byte
			var adrc = Convert.ToByte(entry.ADR_Control);
			var Control = adrc & 0x0F;
			var ADR = adrc >> 4;

			var q = new SubchannelQ
			{
				q_status = SubchannelQ.ComputeStatus(ADR, (EControlQ)(Control & 0xF)),
				q_tno = tno,
				q_index = ino,
				min = BCD2.FromDecimal(entry.AMin),
				sec = BCD2.FromDecimal(entry.ASec),
				frame = BCD2.FromDecimal(entry.AFrame),
				zero = (byte)entry.Zero,
				ap_min = BCD2.FromDecimal(entry.PMin),
				ap_sec = BCD2.FromDecimal(entry.PSec),
				ap_frame = BCD2.FromDecimal(entry.PFrame),
				q_crc = 0, //meaningless
			};

			return new() { QData = q };
		}

		/// <exception cref="MDSParseException">no file found at <paramref name="mdsPath"/> or BLOB error</exception>
		public static Disc LoadMDSToDisc(string mdsPath, DiscMountPolicy IN_DiscMountPolicy)
		{
			var loadResults = LoadMDSPath(mdsPath);
			if (!loadResults.Valid)
				throw loadResults.FailureException;

			var disc = new Disc();

			// load all blobs
			var BlobIndex = MountBlobs(loadResults.ParsedMDSFile, disc);

			var mdsf = loadResults.ParsedMDSFile;
			
			//generate DiscTOCRaw items from the ones specified in the MDS file
			var curSession = 1;
			disc.Sessions.Add(new() { Number = curSession });
			foreach (var entry in mdsf.TOCEntries)
			{
				if (entry.Session != curSession)
				{
					if (entry.Session != curSession + 1)
						throw new MDSParseException("Session incremented more than one!");
					curSession = entry.Session;
					disc.Sessions.Add(new() { Number = curSession });
				}
				
				disc.Sessions[curSession].RawTOCEntries.Add(EmitRawTOCEntry(entry));
			}

			//analyze the RAWTocEntries to figure out what type of track track 1 is
			var tocSynth = new Synthesize_DiscTOC_From_RawTOCEntries_Job(disc.Session1.RawTOCEntries);
			tocSynth.Run();

			// now build the sectors
			var currBlobIndex = 0;
			foreach (var session in mdsf.ParsedSession)
			{
				// leadin track
				// we create this only for session 2+, not session 1
				var leadinSize = session.SessionSequence == 1 ? 0 : 4500;
				for (var i = 0; i < leadinSize; i++)
				{
					// this is most certainly wrong
					// nothing relies on the exact contents for now (only multisession core is VirtualJaguar which doesn't touch leadin)
					// just needs sectors to be present due to track info LBAs of session 2+ accounting for this being present
					var pregapTrackType = CUE.CueTrackType.Audio;
					if (tocSynth.Result.TOCItems[1].IsData)
					{
						pregapTrackType = tocSynth.Result.SessionFormat switch
						{
							SessionFormat.Type20_CDXA => CUE.CueTrackType.Mode2_2352,
							SessionFormat.Type10_CDI => CUE.CueTrackType.CDI_2352,
							SessionFormat.Type00_CDROM_CDDA => CUE.CueTrackType.Mode1_2352,
							_ => pregapTrackType
						};
					}
					disc._Sectors.Add(new CUE.SS_Gap()
					{
						Policy = IN_DiscMountPolicy,
						TrackType = pregapTrackType
					});
				}

				for (var i = session.StartTrack; i <= session.EndTrack; i++)
				{
					var relMSF = -1;

					var track = mdsf.TOCEntries.FirstOrDefault(t => t.Point == i);
					if (track == null) break;

					// ignore the info entries
					if (track.Point is 0xA0 or 0xA1 or 0xA2)
					{
						continue;
					}

					// get the blob(s) for this track
					// it's probably a safe assumption that there will be only one blob per track, but I'm still not 100% sure on this
					var tr = mdsf.TOCEntries.FirstOrDefault(a => a.Point == i) ?? throw new MDSParseException("BLOB Error!");

#if true
					if (tr.ImageFileNamePaths.Count == 0) throw new MDSParseException("BLOB Error!");
#else // this is the worst use of lists and LINQ I've seen in this god-forsaken codebase, I hope for all our sakes that it's not a workaround for some race condition --yoshi
					List<string> blobstrings = new List<string>();
					foreach (var t in tr.ImageFileNamePaths)
					{
						if (!blobstrings.Contains(t))
							blobstrings.Add(t);
					}

					var tBlobs = (from a in tr.ImageFileNamePaths select a).ToList();

					if (tBlobs.Count < 1)
						throw new MDSParseException("BLOB Error!");

					// is the currBlob valid for this track, or do we need to increment?
					string bString = tBlobs.First();
#endif

					// check for track pregap and create if necessary
					// this is specified in the track extras block
					if (track.ExtraBlock.Pregap > 0)
					{
						var pregapTrackType = CUE.CueTrackType.Audio;
						if (tocSynth.Result.TOCItems[1].IsData)
						{
							pregapTrackType = tocSynth.Result.SessionFormat switch
							{
								SessionFormat.Type20_CDXA => CUE.CueTrackType.Mode2_2352,
								SessionFormat.Type10_CDI => CUE.CueTrackType.CDI_2352,
								SessionFormat.Type00_CDROM_CDDA => CUE.CueTrackType.Mode1_2352,
								_ => pregapTrackType
							};
						}
						for (var pre = 0; pre < track.ExtraBlock.Pregap; pre++)
						{
							relMSF++;

							var ss_gap = new CUE.SS_Gap()
							{
								Policy = IN_DiscMountPolicy,
								TrackType = pregapTrackType
							};
							disc._Sectors.Add(ss_gap);

							var qRelMSF = pre - Convert.ToInt32(track.ExtraBlock.Pregap);

							//tweak relMSF due to ambiguity/contradiction in yellowbook docs
							if (!IN_DiscMountPolicy.CUE_PregapContradictionModeA)
								qRelMSF++;

							//setup subQ
							const byte ADR = 1; //absent some kind of policy for how to set it, this is a safe assumption:
							ss_gap.sq.SetStatus(ADR, tocSynth.Result.TOCItems[1].Control);
							ss_gap.sq.q_tno = BCD2.FromDecimal(1);
							ss_gap.sq.q_index = BCD2.FromDecimal(0);
							ss_gap.sq.AP_Timestamp = pre;
							ss_gap.sq.Timestamp = qRelMSF;

							//setup subP
							ss_gap.Pause = true;
						}
						// pregap processing completed
					}
					
					// create track sectors
					var currBlobOffset = track.TrackOffset;
					for (var sector = session.StartSector; sector <= session.EndSector; sector++)
					{
						// get the current blob from the BlobIndex
						var currBlob = (Blob_RawFile) BlobIndex[currBlobIndex];
						var currBlobLength = currBlob.Length;
						if (sector == currBlobLength)
							currBlobIndex++;
						var mdfBlob = (IBlob) disc.DisposableResources[currBlobIndex];

						CUE.SS_Base sBase = track.SectorSize switch
						{
							2352 when track.TrackMode is 1 => new CUE.SS_2352(),
							2048 when track.TrackMode is 2 => new CUE.SS_Mode1_2048(),
							2336 when track.TrackMode is 0 or 3 or 7 => new CUE.SS_Mode2_2336(),
							2048 when track.TrackMode is 4 => new CUE.SS_Mode2_Form1_2048(),
							2324 when track.TrackMode is 5 => new CUE.SS_Mode2_Form2_2324(),
							2328 when track.TrackMode is 5 => new CUE.SS_Mode2_Form2_2328(),
							// best guesses
							2048 => new CUE.SS_Mode1_2048(),
							2336 => new CUE.SS_Mode2_2336(),
							2352 => new CUE.SS_2352(),
							2448 => new CUE.SS_2448_Interleaved(),
							_ => throw new InvalidOperationException($"Not supported: Sector Size {track.SectorSize}, Track Mode {track.TrackMode}")
						};

						sBase.Policy = IN_DiscMountPolicy;

						// configure blob
						sBase.Blob = mdfBlob;
						sBase.BlobOffset = currBlobOffset;

						currBlobOffset += track.SectorSize;
						
						// add subchannel data
						relMSF++;
#if false
						//this should actually be zero. im not sure if this is stored as BCD2 or not
						var tno = BCD2.FromDecimal(track.TrackNo);
#endif
						//these are special values.. I think, taken from this:
						//http://www.staff.uni-mainz.de/tacke/scsi/SCSI2-14.html
						//the CCD will contain Points as decimal values except for these specially converted decimal values which should stay as BCD.
						//Why couldn't they all be BCD? I don't know. I guess because BCD is inconvenient, but only A0 and friends have special meaning. It's confusing.
						var ino = BCD2.FromDecimal(track.Point);
						ino.BCDValue = track.Point switch
						{
							0xA0 or 0xA1 or 0xA2 => (byte)track.Point,
							_ => ino.BCDValue
						};

						// get ADR & Control from ADR_Control byte
						var adrc = Convert.ToByte(track.ADR_Control);
						var Control = adrc & 0x0F;
						var ADR = adrc >> 4;

						var q = new SubchannelQ
						{
							q_status = SubchannelQ.ComputeStatus(ADR, (EControlQ)(Control & 0xF)),
							q_tno = BCD2.FromDecimal(track.Point),
							q_index = ino,
							AP_Timestamp = disc._Sectors.Count,
							Timestamp = relMSF - Convert.ToInt32(track.ExtraBlock.Pregap)
						};

						sBase.sq = q;

						disc._Sectors.Add(sBase);
					}
				}

				// leadout track
				// first leadout is 6750 sectors, later ones are 2250 sectors
				var leadoutSize = session.SessionSequence == 1 ? 6750 : 2250;
				for (var i = 0; i < leadoutSize; i++)
				{
					disc._Sectors.Add(new SS_Leadout
					{
						SessionNumber = session.SessionSequence,
						Policy = IN_DiscMountPolicy
					});
				}
			}

			return disc;
		}
	}
}
