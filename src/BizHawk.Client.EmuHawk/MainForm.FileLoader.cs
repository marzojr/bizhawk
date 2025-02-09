using System.IO;
using System.Linq;
using System.Collections.Generic;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;
using BizHawk.Common.PathExtensions;

namespace BizHawk.Client.EmuHawk
{
	public partial class MainForm
	{
		private enum LoadOrdering
		{
			Rom,
			State,
			Watch,
			CdFile,
			LuaSession,
			LuaScript,
			Cheat,
			MovieFile,
			LegacyMovieFile
		}

		private readonly struct FileInformation
		{
			public string DirectoryName { get; }
			public string FileName { get; }
			public string ArchiveName { get; }

			public FileInformation((string Dir, string File) FilePathSplit, string archive)
			{
				DirectoryName = FilePathSplit.Dir;
				FileName = FilePathSplit.File;
				ArchiveName = archive;
			}
		}

		private readonly string[] _nonArchive = { ".ISO", ".CUE", ".CCD", ".CDI", ".MDS", ".NRG" };

		private void LoadCdl(string filename, string archive = null)
		{
			if (Tools.IsAvailable<CDL>())
			{
				CDL cdl = Tools.Load<CDL>();
				cdl.LoadFile(filename);
			}
		}

		private void LoadCheats(string filename, string archive = null)
		{
			CheatList.Load(Emulator.AsMemoryDomains(), filename, false);
			Tools.Load<Cheats>();
		}

		private void LoadLegacyMovie(string filename, string archive = null)
		{
			if (Emulator.IsNull())
			{
				OpenRom();
			}

			if (Emulator.IsNull())
			{
				return;
			}

			ProcessMovieImport(filename, true);
		}

		private void LoadLuaFile(string filename, string archive = null)
		{
			OpenLuaConsole();
			if (Tools.Has<LuaConsole>()) Tools.LuaConsole.LoadLuaFile(filename);
		}

		private void LoadLuaSession(string filename, string archive = null)
		{
			OpenLuaConsole();
			if (Tools.Has<LuaConsole>())
			{
				Tools.LuaConsole.LoadLuaSession(filename);
			}
		}

		public bool LoadMovie(string filename, string archive = null)
		{
			if (Emulator.IsNull())
			{
				OpenRom();
				if (Emulator.IsNull()) return false;
			}
			return Tools.IsLoaded<TAStudio>()
				? Tools.TAStudio.LoadMovieFile(filename)
				: StartNewMovie(MovieSession.Get(filename, true), false);
		}

		private bool LoadRom(string filename, string archive = null)
			=> LoadRom(new LoadRomArgs(new OpenAdvanced_OpenRom(filename)));

		private bool LoadStateFile(string filename, string archive = null)
			=> LoadState(path: filename, userFriendlyStateName: Path.GetFileName(filename));

		private void LoadWatch(string filename, string archive = null)
		{
			Tools.LoadRamWatch(true);
			((RamWatch) Tools.Get<RamWatch>()).LoadWatchFile(new FileInfo(filename), false);
		}

		private void ProcessFileList(IEnumerable<string> fileList, ref Dictionary<LoadOrdering, List<FileInformation>> sortedFiles, string archive = null)
		{
			foreach (string file in fileList)
			{
				var ext = Path.GetExtension(file)?.ToUpperInvariant() ?? "";
				FileInformation fileInformation = new(file.SplitPathToDirAndFile(), archive);

				switch (ext)
				{
					case ".LUA":
						sortedFiles[LoadOrdering.LuaScript].Add(fileInformation);
						break;
					case ".LUASES":
						sortedFiles[LoadOrdering.LuaSession].Add(fileInformation);
						break;
					case ".STATE":
						sortedFiles[LoadOrdering.State].Add(fileInformation);
						break;
					case ".CHT":
						sortedFiles[LoadOrdering.Cheat].Add(fileInformation);
						break;
					case ".WCH":
						sortedFiles[LoadOrdering.Watch].Add(fileInformation);
						break;
					case ".CDL":
						sortedFiles[LoadOrdering.CdFile].Add(fileInformation);
						break;
					default:
						if (MovieService.IsValidMovieExtension(ext))
						{
							sortedFiles[LoadOrdering.MovieFile].Add(fileInformation);
						}
						else if (MovieImport.IsValidMovieExtension(ext))
						{
							sortedFiles[LoadOrdering.LegacyMovieFile].Add(fileInformation);
						}
						else if (RomLoader.KnownRomExtensions.Contains(ext))
						{
							if (string.IsNullOrEmpty(archive) || !_nonArchive.Contains(ext))
							{
								sortedFiles[LoadOrdering.Rom].Add(fileInformation);
							}
						}
						else
						{
							/* Because the existing behaviour for archives is to try loading
							 * ROMs out of them, that is exactly what we are going to continue
							 * to do at present.  Ideally, the archive should be scanned and
							 * relevant files should be extracted, but see the note below for
							 * further details.
							 */
							var dearchivalMethod = SharpCompressDearchivalMethod.Instance;

#if false // making this run always to restore the default behavior where unrecognized files are treated like roms --adelikat
							if (string.IsNullOrEmpty(archive) && dearchivalMethod.CheckSignature(file, out _, out _))
#endif
							{
								sortedFiles[LoadOrdering.Rom].Add(fileInformation);
							}

#if false
							/*
							 * This is where handling archives would go.
							 * Right now, that's going to be a HUGE hassle, because of the problem with
							 * saving things into the archive (no) and with everything requiring filenames
							 * and not streams (also no), so for the purposes of making drag/drop more robust,
							 * I am not building this out just yet.
							 * -- Adam Michaud (Invariel)
							 */

							// Not going to process nested archives at the moment.
							if (string.IsNullOrEmpty(archive) && archiveHandler.CheckSignature(file, out _, out _))
							{
								using var openedArchive = archiveHandler.Construct(file);
								ProcessFileList(openedArchive.Scan().Select(item => item.Name), ref sortedFiles, file);
							}
							archiveHandler.Dispose();
#endif
						}
						break;
				}
			}
		}

		private string[] PathsFromDragDrop;

		private void FormDragDrop_internal()
		{
			/*
			 *  Refactor, moving the loading of particular files into separate functions that can
			 *  then be used by this code, and loading individual files through the file dialogue.
			 *
			 *  Step 1:
			 *	  Build a dictionary of relevant files from everything that was dragged and dropped.
			 *	  This includes peeking into all relevant archives and using their files.
			 *
			 *  Step 2:
			 *	  Perhaps ask the user which of a particular file type they want to use.
			 *		  Example:  rom1.nes, rom2.smc, rom3.cue are drag-dropped, ask the user which they want to use.
			 *
			 *  Step 3:
			 *	  Load all of the relevant files, in priority order:
			 *	  1) The ROM
			 *	  2) State
			 *	  3) Watch files
			 *	  4) Code Data Logger (CDL)
			 *	  5) LUA sessions
			 *	  6) LUA scripts
			 *	  7) Cheat files
			 *	  8) Movie Playback Files
			 *
			 *  Bonus:
			 *	  Make that order easy to change in the code, heavily suggesting ROM and playback as first and last respectively.
			 */

			Dictionary<LoadOrdering, List<FileInformation>> sortedFiles = new Dictionary<LoadOrdering, List<FileInformation>>();

			// Initialize the dictionary's lists.
			foreach (LoadOrdering value in Enum.GetValues(typeof(LoadOrdering)))
			{
				sortedFiles.Add(value, new List<FileInformation>());
			}

			ProcessFileList(PathsFromDragDrop.Select(EmuHawkUtil.ResolveShortcut), ref sortedFiles);

			// For each of the different types of item, if there are no items of that type, skip them.
			// If there is exactly one of that type of item, load it.
			// If there is more than one, ask.

			foreach (LoadOrdering value in Enum.GetValues(typeof(LoadOrdering)))
			{
				switch (sortedFiles[value].Count)
				{
					case 0:
						break;
					case 1:
						var fileInformation = sortedFiles[value][0];
						string filename = Path.Combine(new[] { fileInformation.DirectoryName, fileInformation.FileName });

						switch (value)
						{
							case LoadOrdering.Rom:
								_ = LoadRom(filename, fileInformation.ArchiveName);
								break;
							case LoadOrdering.State:
								_ = LoadStateFile(filename, fileInformation.ArchiveName);
								break;
							case LoadOrdering.Watch:
								LoadWatch(filename, fileInformation.ArchiveName);
								break;
							case LoadOrdering.CdFile:
								LoadCdl(filename, fileInformation.ArchiveName);
								break;
							case LoadOrdering.LuaSession:
								LoadLuaSession(filename, fileInformation.ArchiveName);
								break;
							case LoadOrdering.LuaScript:
								LoadLuaFile(filename, fileInformation.ArchiveName);
								break;
							case LoadOrdering.Cheat:
								LoadCheats(filename, fileInformation.ArchiveName);
								break;
							case LoadOrdering.MovieFile:
							case LoadOrdering.LegacyMovieFile:
								// I don't really like this hack, but for now, we only want to load one movie file.
								if (sortedFiles[LoadOrdering.MovieFile].Count + sortedFiles[LoadOrdering.LegacyMovieFile].Count > 1)
									break;

								if (value == LoadOrdering.MovieFile) _ = LoadMovie(filename, fileInformation.ArchiveName);
								else LoadLegacyMovie(filename, fileInformation.ArchiveName);
								break;
						}
						break;
				}
			}
		}
	}
}
