using System;
using System.Collections.Generic;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Tests
{
	internal class TestLuaLibraries : LuaLibrariesBase
	{
		public TestLuaLibraries(IMainFormForApi mainForm, DisplayManagerBase displayManager, Config config, IGameInfo game)
			: base(new LuaFileList(
				new List<LuaFile>(), () => { }),
				  new LuaFunctionList(() => { }),
				  mainForm,
				  displayManager,
				  new InputManager(),
				  config,
				  game
			)
		{
			RegisterLuaLibraries(new Type[] { typeof(ConsoleLuaLibrary) });
		}

		protected override void HandleSpecialLuaLibraryProperties(LuaLibraryBase library)
		{
			base.HandleSpecialLuaLibraryProperties(library);

			if (library is ConsoleLuaLibrary consoleLib)
				_logToLuaConsoleCallback = ConsoleLuaLibrary.Log;
		}
	}

	// LuaLibrariesBase will only register sealed classes
	internal sealed class ConsoleLuaLibrary : LuaLibraryBase
	{
		public ConsoleLuaLibrary(ILuaLibraries luaLibsImpl, ApiContainer apiContainer, Action<string> logOutputCallback)
			: base(luaLibsImpl, apiContainer, logOutputCallback)
		{ }

		public override string Name => "console";

		public static Queue<string> messageLog = new();

		[LuaMethodExample("console.log( \"message\" );")]
		[LuaMethod("log", "Puts the message in the test message log que.")]
		public static void Log(params object[] obj)
		{
			string message = string.Join('\n', obj);
			messageLog.Enqueue(message);
		}
	}
}
