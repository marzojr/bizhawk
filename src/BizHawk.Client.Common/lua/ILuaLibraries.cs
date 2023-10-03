using System;

using NLua;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public interface ILuaLibraries
	{
		LuaDocumentation Docs { get; }

		string EngineName { get; }

		/// <remarks>pretty hacky... we don't want a lua script to be able to restart itself by rebooting the core</remarks>
		bool IsRebootingCore { get; set; }

		bool IsUpdateSupressed { get; set; }

		LuaFunctionList RegisteredFunctions { get; }

		public PathEntryCollection PathEntries { get; }

		LuaFileList ScriptList { get; }

		void CallLoadStateEvent(string name);

		void CallSaveStateEvent(string name);

		void CallFrameBeforeEvent();

		void CallFrameAfterEvent();

		void CallExitEvent(LuaFile lf);

		void Close();

		INamedLuaFunction CreateAndRegisterNamedFunction(
			LuaFunction function,
			string theEvent,
			Action<string> logCallback,
			LuaFile luaFile,
			string name = null);

		NLuaTableHelper GetTableHelper();

		void Restart(Config config, IGameInfo game);

		bool RemoveNamedFunctionMatching(Func<INamedLuaFunction, bool> predicate);

		void SpawnAndSetFileThread(LuaFile lf, bool shareGlobals);

		void ExecuteString(string command, LuaFile lf = null);

		(bool WaitForFrame, bool Terminated) ResumeScript(LuaFile lf);

		void EnableLuaFile(LuaFile item, bool shareGlobals);

		void DisableLuaScript(LuaFile file);

		Lua GetCurrentLua();
	}
}