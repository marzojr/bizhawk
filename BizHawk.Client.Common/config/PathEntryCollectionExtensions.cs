﻿using System;
using System.IO;

namespace BizHawk.Client.Common
{
	public static class PathEntryExtensions
	{
		/// <summary>
		/// Returns the base path of the given system.
		/// If the system can not be found, an empty string is returned
		/// </summary>
		public static string BaseFor(this PathEntryCollection collection, string systemId)
		{
			return string.IsNullOrWhiteSpace(systemId)
				? ""
				: collection[systemId, "Base"]?.Path ?? "";
		}

		public static string GlobalBaseAsAbsolute(this PathEntryCollection collection)
		{
			var globalBase = collection["Global", "Base"].Path;

			// if %exe% prefixed then substitute exe path and repeat
			if (globalBase.StartsWith("%exe%", StringComparison.InvariantCultureIgnoreCase))
			{
				globalBase = PathManager.GetExeDirectoryAbsolute() + globalBase.Substring(5);
			}

			// rooted paths get returned without change
			// (this is done after keyword substitution to avoid problems though)
			if (Path.IsPathRooted(globalBase))
			{
				return globalBase;
			}

			// not-rooted things are relative to exe path
			globalBase = Path.Combine(PathManager.GetExeDirectoryAbsolute(), globalBase);
			return globalBase;
		}

		/// <summary>
		/// Returns an entry for the given system and pathType (ROM, screenshot, etc)
		/// but falls back to the base system or global system if it fails
		/// to find pathType or systemId
		/// </summary>
		public static PathEntry EntryWithFallback(this PathEntryCollection collection, string pathType, string systemId)
		{
			return (collection[systemId, pathType] 
				?? collection[systemId, "Base"])
				?? collection["Global", "Base"];
		}

		/// <summary>
		/// Returns an absolute path for the given relative path.
		/// If provided, the systemId will be used to generate the path.
		/// Wildcards are supported.
		/// Logic will fallback until an absolute path is found,
		/// using Global Base as a last resort
		/// </summary>
		public static string AbsolutePathFor(this PathEntryCollection collection, string path, string systemId)
		{
			// warning: supposedly Path.GetFullPath accesses directories (and needs permissions)
			// if this poses a problem, we need to paste code from .net or mono sources and fix them to not pose problems, rather than homebrew stuff
			return Path.GetFullPath(collection.AbsolutePathForInner(path, systemId));
		}

		private static string AbsolutePathForInner(this PathEntryCollection collection,  string path, string systemId)
		{
			// Hack
			if (systemId == "Global")
			{
				systemId = null;
			}

			// This function translates relative path and special identifiers in absolute paths
			if (path.Length < 1)
			{
				return collection.GlobalBaseAsAbsolute();
			}

			if (path == "%recent%")
			{
				return Environment.SpecialFolder.Recent.ToString();
			}

			if (path.StartsWith("%exe%"))
			{
				return PathManager.GetExeDirectoryAbsolute() + path.Substring(5);
			}

			if (path.StartsWith("%rom%"))
			{
				return Global.Config.LastRomPath + path.Substring(5);
			}

			if (path[0] == '.')
			{
				if (!string.IsNullOrWhiteSpace(systemId))
				{
					path = path.Remove(0, 1);
					path = path.Insert(0, collection.BaseFor(systemId));
				}

				if (path.Length == 1)
				{
					return collection.GlobalBaseAsAbsolute();
				}

				if (path[0] == '.')
				{
					path = path.Remove(0, 1);
					path = path.Insert(0, collection.GlobalBaseAsAbsolute());
				}

				return path;
			}

			if (Path.IsPathRooted(path))
			{
				return path;
			}

			//handling of initial .. was removed (Path.GetFullPath can handle it)
			//handling of file:// or file:\\ was removed  (can Path.GetFullPath handle it? not sure)

			// all bad paths default to EXE
			return PathManager.GetExeDirectoryAbsolute();
		}

		public static string MovieAbsolutePath(this PathEntryCollection collection)
		{
			var path = collection["Global", "Movies"].Path;
			return collection.AbsolutePathFor(path, null);
		}

		public static string MovieBackupsAbsolutePath(this PathEntryCollection collection)
		{
			var path = collection["Global", "Movie backups"].Path;
			return collection.AbsolutePathFor(path, null);
		}

		public static string AvAbsolutePath(this PathEntryCollection collection)
		{
			var path = collection["Global", "A/V Dumps"].Path;
			return collection.AbsolutePathFor(path, null);
		}

		public static string LuaAbsolutePath(this PathEntryCollection collection)
		{
			var path = collection["Global", "Lua"].Path;
			return collection.AbsolutePathFor(path, null);
		}

		public static string FirmwareAbsolutePath(this PathEntryCollection collection)
		{
			return collection.AbsolutePathFor(collection.FirmwaresPathFragment, null);
		}

		public static string LogAbsolutePath(this PathEntryCollection collection)
		{
			var path = collection.ResolveToolsPath(collection["Global", "Debug Logs"].Path);
			return collection.AbsolutePathFor(path, null);
		}

		public static string WatchAbsolutePath(this PathEntryCollection collection)
		{
			var path = 	collection.ResolveToolsPath(collection["Global", "Watch (.wch)"].Path);
			return collection.AbsolutePathFor(path, null);
		}

		public static string ToolsAbsolutePath(this PathEntryCollection collection)
		{
			var path = collection["Global", "Tools"].Path;
			return collection.AbsolutePathFor(path, null);
		}

		public static string TastudioStatesAbsolutePath(this PathEntryCollection collection)
		{
			var path = collection["Global", "TAStudio states"].Path;
			return collection.AbsolutePathFor(path, null);
		}

		public static string MultiDiskAbsolutePath(this PathEntryCollection collection)
		{
			var path = collection.ResolveToolsPath(collection["Global", "Multi-Disk Bundles"].Path);
			return collection.AbsolutePathFor(path, null);
		}

		public static string RomAbsolutePath(this PathEntryCollection collection, string sysId = null)
		{
			if (string.IsNullOrWhiteSpace(sysId))
			{
				return collection.AbsolutePathFor(collection["Global_NULL", "ROM"].Path, "Global_NULL");
			}

			if (Global.Config.UseRecentForRoms) // PathManager TODO: how about we movie this value into path entry collection?
			{
				return Environment.SpecialFolder.Recent.ToString();
			}

			var path = collection[sysId, "ROM"];

			if (path == null || !PathManager.PathIsSet(path.Path))
			{
				path = collection["Global", "ROM"];

				if (path != null && PathManager.PathIsSet(path.Path))
				{
					return collection.AbsolutePathFor(path.Path, null);
				}
			}

			return collection.AbsolutePathFor(path.Path, sysId);
		}

		public static string ScreenshotAbsolutePathFor(this PathEntryCollection collection, string system)
		{
			return collection.AbsolutePathFor(collection[system, "Screenshots"].Path, system);
		}

		public static string PalettesAbsolutePathFor(this PathEntryCollection collection, string system)
		{
			return collection.AbsolutePathFor(collection[system, "Palettes"].Path, system);
		}

		private static string ResolveToolsPath(this PathEntryCollection collection, string subPath)
		{
			if (Path.IsPathRooted(subPath) || subPath.StartsWith("%"))
			{
				return subPath;
			}

			var toolsPath = collection["Global", "Tools"].Path;

			// Hack for backwards compatibility, prior to 1.11.5, .wch files were in .\Tools, we don't want that to turn into .Tools\Tools
			if (subPath == "Tools")
			{
				return toolsPath;
			}

			return Path.Combine(toolsPath, subPath);
		}
	}
}
