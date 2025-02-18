#nullable enable

using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Linq;
using System.Net.Sockets;

using BizHawk.Common.CollectionExtensions;
using BizHawk.Common.StringExtensions;

namespace BizHawk.Client.Common
{
	/// <summary>Parses command-line flags into a <see cref="ParsedCLIFlags"/> struct.</summary>
	public static class ArgParser
	{
		private sealed class BespokeOption<T> : Option<T>
		{
			public BespokeOption(string name)
				: base(name) {}

			public BespokeOption(string name, string description)
				: base(name: name, description: description) {}

			public BespokeOption(string[] aliases)
				: base(aliases) {}

			public BespokeOption(string[] aliases, string description)
				: base(aliases, description) {}
		}

		private static readonly Argument<string?> ArgumentRomFilePath = new(name: "rom", () => null, description: "path; if specified, the file will be loaded the same way as it would be from `File` > `Open...`; this argument can and should be given LAST despite what it says at the top of --help");

		private static readonly IReadOnlyList<Option> GeneratedOptions;

		private static readonly BespokeOption<string?> OptionAVDumpAudioSync = new(name: "--audiosync", description: "bool; `true` is the only truthy value, all else falsey; if not set, uses remembered state from config");

		private static readonly BespokeOption<int?> OptionAVDumpEndAtFrame = new(name: "--dump-length", description: "int; frame index at which to stop A/V dumping (encoding)");

		private static readonly BespokeOption<string?> OptionAVDumpFrameList = new("--dump-frames"); // desc added in static ctor

		private static readonly BespokeOption<string?> OptionAVDumpName = new("--dump-name"); // desc added in static ctor

		private static readonly BespokeOption<bool> OptionAVDumpQuitWhenDone = new(name: "--dump-close", description: "pass to quit completely after A/V dumping (encoding) finishes");

		private static readonly BespokeOption<string?> OptionAVDumpType = new("--dump-type"); // desc added in static ctor

		private static readonly BespokeOption<string?> OptionConfigFilePath = new(name: "--config", description: "path of config file to use");

		private static readonly BespokeOption<string?> OptionHTTPClientURIGET = new(aliases: [ "--url-get", "--url_get" ], description: "string; URI to use for HTTP 'GET' IPC (Lua `comm.http*Get*`)");

		private static readonly BespokeOption<string?> OptionHTTPClientURIPOST = new(aliases: [ "--url-post", "--url_post" ], description: "string; URI to use for HTTP 'POST' IPC (Lua `comm.http*Post*`)");

		private static readonly BespokeOption<bool> OptionLaunchChromeless = new(name: "--chromeless", description: "pass and the 'chrome' (a.k.a. GUI) will never be shown, not even in windowed mode");

		private static readonly BespokeOption<bool> OptionLaunchFullscreen = new(name: "--fullscreen", description: "pass to launch in fullscreen");

		private static readonly BespokeOption<int?> OptionLoadQuicksaveSlot = new(name: "--load-slot", description: "int; quicksave slot which should be loaded on launch");

		private static readonly BespokeOption<string?> OptionLoadSavestateFilePath = new("--load-state"); // desc added in static ctor

		private static readonly BespokeOption<string?> OptionLuaFilePath = new("--lua"); // desc added in static ctor

		private static readonly BespokeOption<string?> OptionMMFPath = new(name: "--mmf", description: "path of file to use for 'memory-mapped file' IPC (Lua `comm.mmf*`)");

		private static readonly BespokeOption<string?> OptionMovieFilePath = new(name: "--movie", description: "path; input movie which should be loaded on launch");

		private static readonly BespokeOption<string?> OptionOpenExternalTool = new(name: "--open-ext-tool-dll", description: "the first ext. tool from ExternalToolManager.ToolStripMenu which satisfies both of these will be opened: 1) available (no load errors, correct system/rom, etc.) and 2) dll path matches given string; or dll filename matches given string with or without `.dll`");

		private static readonly BespokeOption<bool> OptionOpenLuaConsole = new(name: "--luaconsole", description: "pass to open the Lua Console");

		private static readonly BespokeOption<bool> OptionQueryAppVersion = new(name: "--version", description: "pass to print version information and immediately quit");

		private static readonly BespokeOption<string?> OptionSocketServerIP = new(aliases: [ "--socket-ip", "--socket_ip" ]); // desc added in static ctor

		private static readonly BespokeOption<ushort?> OptionSocketServerPort = new(aliases: [ "--socket-port", "--socket_port" ]); // desc added in static ctor

		private static readonly BespokeOption<bool> OptionSocketServerUseUDP = new(aliases: [ "--socket-udp", "--socket_udp" ]); // desc added in static ctor

		private static readonly BespokeOption<string?> OptionWebSocketServerIP = new(aliases: [ "--websocket-server-ip", "--ws_ip" ]); // desc added in static ctor

		private static readonly BespokeOption<ushort?> OptionWebSocketServerPort = new(aliases: [ "--websocket-server-port", "--ws_port" ]); // desc added in static ctor

		private static readonly BespokeOption<string?> OptionUserdataUnparsedPairs = new(name: "--userdata", description: "pairs in the format `k1:v1;k2:v2` (mind your shell escape sequences); if the value is `true`/`false` it's interpreted as a boolean, if it's a valid 32-bit signed integer e.g. `-1234` it's interpreted as such, if it's a valid 32-bit float e.g. `12.34` it's interpreted as such, else it's interpreted as a string");

		private static readonly Parser Parser;

		static ArgParser()
		{
			OptionAVDumpFrameList.Description = $"comma-separated list of integers, indices of frames which should be included in the A/V dump (encoding); implies `--{OptionAVDumpEndAtFrame.Name}=<end>` where `<end>` is the highest frame listed";
			OptionAVDumpName.Description = $"ignored unless `--{OptionAVDumpType.Name}` also passed";
			OptionAVDumpType.Description = $"ignored unless `--{OptionAVDumpName.Name}` also passed";
			OptionLoadSavestateFilePath.Description = "path; savestate which should be loaded on launch; this takes precedence over `--{OptionLoadQuicksaveSlot.Name}`";
			OptionLuaFilePath.Description = $"implies `--{OptionOpenLuaConsole.Name}`";
			OptionSocketServerIP.Description = $"string; IP address for Unix socket IPC (Lua `comm.socket*`); must be paired with `--{OptionSocketServerPort.Name}`";
			OptionSocketServerPort.Description = $"int; port for Unix socket IPC (Lua `comm.socket*`); must be paired with `--{OptionSocketServerIP.Name}`";
			OptionSocketServerUseUDP.Description = $"pass to use UDP instead of TCP for Unix socket IPC (Lua `comm.socket*`); ignored unless `--{OptionSocketServerIP.Name} --{OptionSocketServerPort.Name}` also passed";
			OptionWebSocketServerIP.Description = $"string; IP address for websocket server; must be paired with `--{OptionWebSocketServerPort.Name}`";
			OptionWebSocketServerPort.Description = $"int; port for websocket server; must be paired with `--{OptionWebSocketServerIP.Name}`";

			RootCommand root = new();
			root.Add(ArgumentRomFilePath);
			// `--help` uses this order, so keep alphabetised by flag
			root.Add(/* --audiosync */ OptionAVDumpAudioSync);
			root.Add(/* --chromeless */ OptionLaunchChromeless);
			root.Add(/* --config */ OptionConfigFilePath);
			root.Add(/* --dump-close */ OptionAVDumpQuitWhenDone);
			root.Add(/* --dump-frames */ OptionAVDumpFrameList);
			root.Add(/* --dump-length */ OptionAVDumpEndAtFrame);
			root.Add(/* --dump-name */ OptionAVDumpName);
			root.Add(/* --dump-type */ OptionAVDumpType);
			root.Add(/* --fullscreen */ OptionLaunchFullscreen);
			root.Add(/* --load-slot */ OptionLoadQuicksaveSlot);
			root.Add(/* --load-state */ OptionLoadSavestateFilePath);
			root.Add(/* --lua */ OptionLuaFilePath);
			root.Add(/* --luaconsole */ OptionOpenLuaConsole);
			root.Add(/* --mmf */ OptionMMFPath);
			root.Add(/* --movie */ OptionMovieFilePath);
			root.Add(/* --open-ext-tool-dll */ OptionOpenExternalTool);
			root.Add(/* --socket-ip */ OptionSocketServerIP);
			root.Add(/* --socket-port */ OptionSocketServerPort);
			root.Add(/* --socket-udp */ OptionSocketServerUseUDP);
			root.Add(/* --websocket-server-ip */ OptionWebSocketServerIP);
			root.Add(/* --websocket-server-port */ OptionWebSocketServerPort);
			root.Add(/* --url-get */ OptionHTTPClientURIGET);
			root.Add(/* --url-post */ OptionHTTPClientURIPOST);
			root.Add(/* --userdata */ OptionUserdataUnparsedPairs);
			root.Add(/* --version */ OptionQueryAppVersion);

			Parser = new CommandLineBuilder(root)
//				.UseVersionOption() // "cannot be combined with other arguments" which is fair enough but `--config` is crucial on NixOS
				.UseHelp()
//				.UseEnvironmentVariableDirective() // useless
				.UseParseDirective()
				.UseSuggestDirective()
//				.RegisterWithDotnetSuggest() // intended for dotnet tools
//				.UseTypoCorrections() // we're only using the parser, and I guess this only works with the full buy-in
//				.UseParseErrorReporting() // we're only using the parser, and I guess this only works with the full buy-in
//				.UseExceptionHandler() // we're only using the parser, so nothing should be throwing
//				.CancelOnProcessTermination() // we're only using the parser, so there's not really anything to cancel
				.Build();
			GeneratedOptions = root.Options.Where(static o =>
			{
				var t = o.GetType();
				return !t.IsGenericType || t.GetGenericTypeDefinition() != typeof(BespokeOption<>); // no there is no simpler way to do this
			}).ToArray();
		}

		/// <return>exit code, or <see langword="null"/> if should not exit</return>
		/// <exception cref="ArgParserException">parsing failure, or invariant broken</exception>
		public static int? ParseArguments(out ParsedCLIFlags parsed, string[] args)
		{
			parsed = default;
			var result = Parser.Parse(args);
			if (result.Errors.Count is not 0)
			{
				// write all to stdout and show first in modal dialog (done in `catch` block in `Program`)
				Console.WriteLine("failed to parse command-line arguments:");
				foreach (var error in result.Errors) Console.WriteLine(error.Message);
				throw new ArgParserException($"failed to parse command-line arguments: {result.Errors[0].Message}");
			}
			var triggeredGeneratedOption = GeneratedOptions.FirstOrDefault(o => result.FindResultFor(o) is not null);
			if (triggeredGeneratedOption is not null)
			{
				// means e.g. `./EmuHawkMono.sh --help` was passed, run whatever behaviour it normally has...
				var exitCode = result.Invoke();
				// ...and maybe exit
				if (exitCode is not 0
					|| triggeredGeneratedOption.Name is "help") // `Name` may be localised meaning this won't work? I can't grok the source for `HelpOption`
				{
					return exitCode;
				}
			}

			var autoDumpLength = result.GetValueForOption(OptionAVDumpEndAtFrame);
			HashSet<int>? currAviWriterFrameList = null;
			if (result.GetValueForOption(OptionAVDumpFrameList) is string list)
			{
				currAviWriterFrameList = new();
				currAviWriterFrameList.AddRange(list.Split(',').Select(int.Parse));
				// automatically set dump length to maximum frame
				autoDumpLength ??= currAviWriterFrameList.Max();
			}

			var luaScript = result.GetValueForOption(OptionLuaFilePath);
			var luaConsole = luaScript is not null || result.GetValueForOption(OptionOpenLuaConsole);

			var socketIP = result.GetValueForOption(OptionSocketServerIP);
			var socketPort = result.GetValueForOption(OptionSocketServerPort);
			var socketAddress = socketIP is null && socketPort is null
				? ((string, ushort)?) null // don't bother
				: socketIP is not null && socketPort is not null
					? (socketIP, socketPort.Value)
					: throw new ArgParserException("Socket server needs both --socket_ip and --socket_port. Socket server was not started");

			var websocketIP = result.GetValueForOption(OptionWebSocketServerIP);
			var websocketPort = result.GetValueForOption(OptionWebSocketServerPort);
			var webSocketServerAddress = websocketIP is null && websocketPort is null
				? ((string, ushort)?) null // don't bother
				: websocketIP is not null && websocketPort is not null
					? (websocketIP, websocketPort.Value)
					: throw new ArgParserException("Websocket server needs both --ws_ip and --ws_port. Websocket server was not started");

			var httpClientURIGET = result.GetValueForOption(OptionHTTPClientURIGET);
			var httpClientURIPOST = result.GetValueForOption(OptionHTTPClientURIPOST);
			var httpAddresses = httpClientURIGET is null && httpClientURIPOST is null
					? ((string?, string?)?) null // don't bother
					: (httpClientURIGET, httpClientURIPOST);

			var audiosync = result.GetValueForOption(OptionAVDumpAudioSync)?.EqualsIgnoreCase("true");

			List<(string Key, string Value)>? userdataUnparsedPairs = null;
			if (result.GetValueForOption(OptionUserdataUnparsedPairs) is string list1)
			{
				userdataUnparsedPairs = new();
				foreach (var s in list1.Split(';'))
				{
					var iColon = s.IndexOf(':');
					if (iColon is -1) throw new ArgParserException("malformed userdata (';' without ':')");
					userdataUnparsedPairs.Add((s.Substring(startIndex: 0, length: iColon), s.Substring(iColon + 1)));
				}
			}

			parsed = new(
				cmdLoadSlot: result.GetValueForOption(OptionLoadQuicksaveSlot),
				cmdLoadState: result.GetValueForOption(OptionLoadSavestateFilePath),
				cmdConfigFile: result.GetValueForOption(OptionConfigFilePath),
				cmdMovie: result.GetValueForOption(OptionMovieFilePath),
				cmdDumpType: result.GetValueForOption(OptionAVDumpType),
				currAviWriterFrameList: currAviWriterFrameList,
				autoDumpLength: autoDumpLength ?? 0,
				printVersion: result.GetValueForOption(OptionQueryAppVersion),
				cmdDumpName: result.GetValueForOption(OptionAVDumpName),
				autoCloseOnDump: result.GetValueForOption(OptionAVDumpQuitWhenDone),
				chromeless: result.GetValueForOption(OptionLaunchChromeless),
				startFullscreen: result.GetValueForOption(OptionLaunchFullscreen),
				luaScript: luaScript,
				luaConsole: luaConsole,
				socketAddress: socketAddress,
				webSocketServerAddress: webSocketServerAddress,
				mmfFilename: result.GetValueForOption(OptionMMFPath),
				httpAddresses: httpAddresses,
				audiosync: audiosync,
				openExtToolDll: result.GetValueForOption(OptionOpenExternalTool),
				socketProtocol: result.GetValueForOption(OptionSocketServerUseUDP) ? ProtocolType.Udp : ProtocolType.Tcp,
				userdataUnparsedPairs: userdataUnparsedPairs,
				cmdRom: result.GetValueForArgument(ArgumentRomFilePath)
			);
			return null;
		}

		public sealed class ArgParserException : Exception
		{
			public ArgParserException(string message) : base(message) {}
		}
	}
}
