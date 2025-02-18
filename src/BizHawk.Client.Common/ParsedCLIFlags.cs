#nullable enable

using System.Collections.Generic;
using System.Net.Sockets;

namespace BizHawk.Client.Common
{
	public readonly struct ParsedCLIFlags
	{
		public readonly int? cmdLoadSlot;

		public readonly string? cmdLoadState;

		public readonly string? cmdConfigFile;

		public readonly string? cmdMovie;

		public readonly string? cmdDumpType;

		public readonly HashSet<int>? _currAviWriterFrameList;

		public readonly int _autoDumpLength;

		public readonly bool printVersion;

		public readonly string? cmdDumpName;

		public readonly bool _autoCloseOnDump;

		public readonly bool _chromeless;

		public readonly bool startFullscreen;

		public readonly string? luaScript;

		public readonly bool luaConsole;

		public readonly (string IP, ushort Port)? WebSocketServerAddress;

		public readonly (string IP, ushort Port)? SocketAddress;

		public readonly ProtocolType SocketProtocol;

		public readonly IReadOnlyList<(string Key, string Value)>? UserdataUnparsedPairs;

		public readonly string? MMFFilename;

		public readonly (string? UrlGet, string? UrlPost)? HTTPAddresses;

		public readonly bool? audiosync;

		public readonly string? openExtToolDll;

		public readonly string? cmdRom;

		public ParsedCLIFlags(
			int? cmdLoadSlot,
			string? cmdLoadState,
			string? cmdConfigFile,
			string? cmdMovie,
			string? cmdDumpType,
			HashSet<int>? currAviWriterFrameList,
			int autoDumpLength,
			bool printVersion,
			string? cmdDumpName,
			bool autoCloseOnDump,
			bool chromeless,
			bool startFullscreen,
			string? luaScript,
			bool luaConsole,
			(string IP, ushort Port)? socketAddress,
			(string IP, ushort Port)? webSocketServerAddress,
			string? mmfFilename,
			(string? UrlGet, string? UrlPost)? httpAddresses,
			bool? audiosync,
			string? openExtToolDll,
			ProtocolType socketProtocol,
			IReadOnlyList<(string Key, string Value)>? userdataUnparsedPairs,
			string? cmdRom)
		{
			this.cmdLoadSlot = cmdLoadSlot;
			this.cmdLoadState = cmdLoadState;
			this.cmdConfigFile = cmdConfigFile;
			this.cmdMovie = cmdMovie;
			this.cmdDumpType = cmdDumpType;
			_currAviWriterFrameList = currAviWriterFrameList;
			_autoDumpLength = autoDumpLength;
			this.printVersion = printVersion;
			this.cmdDumpName = cmdDumpName;
			_autoCloseOnDump = autoCloseOnDump;
			_chromeless = chromeless;
			this.startFullscreen = startFullscreen;
			this.luaScript = luaScript;
			this.luaConsole = luaConsole;
			SocketAddress = socketAddress;
			WebSocketServerAddress = webSocketServerAddress;
			MMFFilename = mmfFilename;
			HTTPAddresses = httpAddresses;
			this.audiosync = audiosync;
			this.openExtToolDll = openExtToolDll;
			SocketProtocol = socketProtocol;
			UserdataUnparsedPairs = userdataUnparsedPairs;
			this.cmdRom = cmdRom;
		}
	}
}
