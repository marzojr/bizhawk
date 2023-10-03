using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

using NLua;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class LuaLibrariesBase : ILuaLibraries
	{
		public LuaLibrariesBase(
			LuaFileList scriptList,
			LuaFunctionList registeredFuncList,
			IMainFormForApi mainFormApi,
			DisplayManagerBase displayManager,
			InputManager inputManager,
			Config config,
			IGameInfo game)
		{
			_th = new NLuaTableHelper(this, LogToLuaConsole);
			_displayManager = displayManager;
			_inputManager = inputManager;
			_mainFormApi = mainFormApi;
			LuaWait = new AutoResetEvent(false);
			PathEntries = config.PathEntries;
			RegisteredFunctions = registeredFuncList;
			ScriptList = scriptList;
			Docs.Clear();
			_apiContainer = ApiManager.RestartLua(_mainFormApi.Emulator.ServiceProvider, LogToLuaConsole, _mainFormApi, _displayManager, _inputManager, _mainFormApi.MovieSession, _mainFormApi.Tools, config, _mainFormApi.Emulator, game);

			UpdatePackageTable(_luaWithoutFile);

			_luaWithoutFile.RegisterFunction("print", this, typeof(LuaLibrariesBase).GetMethod(nameof(Print)));

			RegisterLuaLibraries(ReflectionCache.Types);
		}

		protected void EnumerateLuaFunctions(string name, Type type, LuaLibraryBase instance)
		{
			if (instance != null)
			{
				_luaWithoutFile.NewTable(name);
				_tablesForFunctions.Add(name);
			}
			foreach (var method in type.GetMethods())
			{
				var foundAttrs = method.GetCustomAttributes(typeof(LuaMethodAttribute), false);
				if (foundAttrs.Length == 0) continue;
				if (instance != null)
				{
					string path = $"{name}.{((LuaMethodAttribute)foundAttrs[0]).Name}";
					_luaWithoutFile.RegisterFunction(path, instance, method);
					_functionsToRegister[path] = (instance, method);

				}
				LibraryFunction libFunc = new(
					name,
					type.GetCustomAttributes(typeof(DescriptionAttribute), false).Cast<DescriptionAttribute>()
						.Select(descAttr => descAttr.Description).FirstOrDefault() ?? string.Empty,
					method
				);
				Docs.Add(libFunc);
			}
		}
		protected void RegisterLuaLibraries(IEnumerable<Type> typesToSearch)
		{
			foreach (var lib in typesToSearch
				.Where(t => typeof(LuaLibraryBase).IsAssignableFrom(t) && t.IsSealed && ServiceInjector.IsAvailable(_mainFormApi.Emulator.ServiceProvider, t)))
			{
				if (VersionInfo.DeveloperBuild
					|| lib.GetCustomAttribute<LuaLibraryAttribute>(inherit: false)?.Released is not false)
				{
					var instance = (LuaLibraryBase)Activator.CreateInstance(lib, this, _apiContainer, (Action<string>)LogToLuaConsole);
					if (!ServiceInjector.UpdateServices(_mainFormApi.Emulator.ServiceProvider, instance, mayCache: true)) throw new Exception("Lua lib has required service(s) that can't be fulfilled");

					HandleSpecialLuaLibraryProperties(instance);

					EnumerateLuaFunctions(instance.Name, lib, instance);
					Libraries.Add(lib, instance);
				}
			}
		}

		protected virtual void HandleSpecialLuaLibraryProperties(LuaLibraryBase library)
		{
			if (library is ClientLuaLibrary clientLib)
			{
				clientLib.MainForm = _mainFormApi;
			}
			else if (library is EmulationLuaLibrary emulationLib)
			{
				emulationLib.FrameAdvanceCallback = FrameAdvance;
				emulationLib.YieldCallback = EmuYield;
			}
		}

		private void UpdatePackageTable(Lua lua)
		{
			var packageTable = (LuaTable)lua["package"];
			var luaPath = PathEntries.LuaAbsolutePath();
			if (OSTailoredCode.IsUnixHost)
			{
				// add %exe%/Lua to library resolution pathset (LUA_PATH)
				// this is done already on windows, but not on linux it seems?
				packageTable["path"] = $"{luaPath}/?.lua;{luaPath}?/init.lua;{packageTable["path"]}";
				// we need to modifiy the cpath so it looks at our lua dir too, and remove the relative pathing
				// we do this on Windows too, but keep in mind Linux uses .so and Windows use .dll
				// TODO: Does the relative pathing issue Windows has also affect Linux? I'd assume so...
				packageTable["cpath"] = $"{luaPath}/?.so;{luaPath}/loadall.so;{packageTable["cpath"]}";
				packageTable["cpath"] = ((string)packageTable["cpath"]).Replace(";./?.so", "");
			}
			else
			{
				packageTable["cpath"] = $"{luaPath}\\?.dll;{luaPath}\\loadall.dll;{packageTable["cpath"]}";
				packageTable["cpath"] = ((string)packageTable["cpath"]).Replace(";.\\?.dll", "");
			}
		}

		private Dictionary<LuaFile, Lua> _activeLuas = new();

		private Lua _luaWithoutFile = new();

		private List<string> _tablesForFunctions = new();
		private Dictionary<string, (LuaLibraryBase Lib, MethodInfo Func)> _functionsToRegister = new();

		private ApiContainer _apiContainer;

		private readonly DisplayManagerBase _displayManager;

		private GuiApi GuiAPI => (GuiApi)_apiContainer.Gui;

		private readonly InputManager _inputManager;

		private readonly IMainFormForApi _mainFormApi;

		private LuaThread _currThread;

		private Lua _currLua;

		private readonly NLuaTableHelper _th;

		protected Action<object[]> _logToLuaConsoleCallback = a => Console.WriteLine("a Lua lib is logging during init and the console lib hasn't been initialised yet");

		public LuaDocumentation Docs { get; } = new LuaDocumentation();

		protected EmulationLuaLibrary EmulationLuaLibrary => (EmulationLuaLibrary)Libraries[typeof(EmulationLuaLibrary)];

		public string EngineName => "NLua+Lua";

		public bool IsRebootingCore { get; set; }

		public bool IsUpdateSupressed { get; set; }

		private readonly IDictionary<Type, LuaLibraryBase> Libraries = new Dictionary<Type, LuaLibraryBase>();

		private EventWaitHandle LuaWait;

		public PathEntryCollection PathEntries { get; private set; }

		public LuaFileList ScriptList { get; }

		protected void LogToLuaConsole(object outputs) => _logToLuaConsoleCallback(new[] { outputs });

		public NLuaTableHelper GetTableHelper() => _th;

		/// <summary>
		/// This is called when a Lua script causes the core to reboot, or a new core to load.
		/// </summary>
		public void Restart(Config config, IGameInfo game)
		{
			_apiContainer = ApiManager.RestartLua(_mainFormApi.Emulator.ServiceProvider, LogToLuaConsole, _mainFormApi, _displayManager, _inputManager, _mainFormApi.MovieSession, _mainFormApi.Tools, config, _mainFormApi.Emulator, game);
			PathEntries = config.PathEntries;
			foreach (var lib in Libraries.Values)
			{
				lib.APIs = _apiContainer;
				Debug.Assert(ServiceInjector.UpdateServices(_mainFormApi.Emulator.ServiceProvider, lib, mayCache: true));
				lib.Restarted();
			}
		}

		public bool FrameAdvanceRequested { get; private set; }

		public LuaFunctionList RegisteredFunctions { get; }

		public Lua GetCurrentLua()
		{
			return _currLua ?? _luaWithoutFile;
		}

		public void CallSaveStateEvent(string name)
		{
			try
			{
				foreach (var lf in RegisteredFunctions.Where(static l => l.Event == NamedLuaFunction.EVENT_TYPE_SAVESTATE).ToList())
				{
					CallFunction(lf, name);
				}
			}
			catch (Exception e)
			{
				LogToLuaConsole($"error running function attached by lua function event.onsavestate\nError message: {e.Message}");
			}
		}

		public void CallLoadStateEvent(string name)
		{
			GuiAPI.BeginFrame();

			try
			{
				foreach (var lf in RegisteredFunctions.Where(static l => l.Event == NamedLuaFunction.EVENT_TYPE_LOADSTATE).ToList())
				{
					CallFunction(lf, name);
				}
			}
			catch (Exception e)
			{
				LogToLuaConsole($"error running function attached by lua function event.onloadstate\nError message: {e.Message}");
			}

			GuiAPI.EndFrame();
		}

		public void CallFrameBeforeEvent()
		{
			if (IsUpdateSupressed) return;

			GuiAPI.BeginFrame();

			try
			{
				foreach (var lf in RegisteredFunctions.Where(static l => l.Event == NamedLuaFunction.EVENT_TYPE_PREFRAME).ToList())
				{
					CallFunction(lf);
				}
			}
			catch (Exception e)
			{
				LogToLuaConsole($"error running function attached by lua function event.onframestart\nError message: {e.Message}");
			}
		}

		public void CallFrameAfterEvent()
		{
			if (IsUpdateSupressed) return;

			try
			{
				foreach (var lf in RegisteredFunctions.Where(static l => l.Event == NamedLuaFunction.EVENT_TYPE_POSTFRAME).ToList())
				{
					CallFunction(lf);
				}
			}
			catch (Exception e)
			{
				LogToLuaConsole($"error running function attached by lua function event.onframeend\nError message: {e.Message}");
			}

			GuiAPI.EndFrame();
		}

		public void CallExitEvent(LuaFile lf)
		{
			foreach (var exitCallback in RegisteredFunctions
				.Where(l => l.Event == NamedLuaFunction.EVENT_TYPE_ENGINESTOP
					&& (l.LuaFile.Path == lf.Path || ReferenceEquals(l.LuaFile.Thread, lf.Thread)))
				.ToList())
			{
				CallFunction(exitCallback);
			}
		}

		public void Close()
		{
			foreach (var closeCallback in RegisteredFunctions
				.Where(static l => l.Event == NamedLuaFunction.EVENT_TYPE_CONSOLECLOSE)
				.ToList())
			{
				CallFunction(closeCallback);
			}

			RegisteredFunctions.Clear(_mainFormApi.Emulator);
			ScriptList.Clear();

			foreach (var lib in Libraries.Values)
			{
				if (lib is IDisposable disposable)
					disposable.Dispose();
			}

			_luaWithoutFile.Dispose();
			_luaWithoutFile = null;
			foreach (Lua lua in _activeLuas.Values)
				lua.Dispose();
		}

		public INamedLuaFunction CreateAndRegisterNamedFunction(
			LuaFunction function,
			string theEvent,
			Action<string> logCallback,
			LuaFile luaFile,
			string name = null)
		{
			var nlf = new NamedLuaFunction(function, theEvent, logCallback, luaFile,
				() => { _activeLuas[luaFile].NewThread(out var thread); return thread; }, name);
			RegisteredFunctions.Add(nlf);
			return nlf;
		}

		public bool RemoveNamedFunctionMatching(Func<INamedLuaFunction, bool> predicate)
		{
			var nlf = (NamedLuaFunction)RegisteredFunctions.FirstOrDefault(predicate);
			if (nlf == null) return false;
			RegisteredFunctions.Remove(nlf, _mainFormApi.Emulator);
			return true;
		}

		public LuaThread SpawnCoroutine(LuaFile file)
		{
			var content = File.ReadAllText(file.Path);
			var main = _activeLuas[file].LoadString(content, "main");
			_activeLuas[file].NewThread(main, out var ret);
			return ret;
		}

		public void SpawnAndSetFileThread(LuaFile lf, bool shareGlobals)
		{ 
			if (_activeLuas.TryGetValue(lf, out Lua al) && al != _luaWithoutFile)
				_activeLuas[lf].Dispose();

			if (shareGlobals)
				_activeLuas[lf] = _luaWithoutFile;
			else
			{
				Lua lua = new();
				UpdatePackageTable(lua);
				lua.RegisterFunction("print", this, typeof(LuaLibrariesBase).GetMethod(nameof(Print)));
				// We cannot copy tables from one Lua to another, unfortunately. Directly assigning them doesn't work at all.
				// Transferring each individual value to new tables mostly works, but not always.
				// For example event.onframeend would receive bad LuaFunction references.
				foreach (string name in _tablesForFunctions)
					lua.NewTable(name);
				foreach (var item in _functionsToRegister)
					lua.RegisterFunction(item.Key, item.Value.Lib, item.Value.Func);
				_activeLuas[lf] = lua;
			}

			lf.Thread = SpawnCoroutine(lf);
		}

		public void ExecuteString(string command, LuaFile lf = null)
		{
			Lua lua = _luaWithoutFile;
			if (lf != null)
			{
				if (_activeLuas.TryGetValue(lf, out Lua val))
					lua = val;
			}
			lua.DoString(command);
		}

		private void CallFunction(NamedLuaFunction func, string name = null)
		{
			_currLua = _activeLuas[func.LuaFile];
			func.Call(name);
			_currLua = null;
		}

		public (bool WaitForFrame, bool Terminated) ResumeScript(LuaFile lf)
		{
			_currThread = lf.Thread;
			_currLua = _activeLuas[lf];

			try
			{
				LuaLibraryBase.SetCurrentThread(lf);

				var execResult = _currThread.Resume();

				var result = execResult switch
				{
					LuaStatus.OK => (WaitForFrame: false, Terminated: true),
					LuaStatus.Yield => (WaitForFrame: FrameAdvanceRequested, Terminated: false),
					_ => throw new InvalidOperationException($"{nameof(_currThread.Resume)}() returned {execResult}?")
				};

				FrameAdvanceRequested = false;
				return result;
			}
			finally
			{
				LuaLibraryBase.ClearCurrentThread();
				_currThread = null;
				_currLua = null;
			}
		}

		public void Print(params object[] outputs)
		{
			_logToLuaConsoleCallback(outputs);
		}

		private void FrameAdvance()
		{
			FrameAdvanceRequested = true;
			_currThread.Yield();
		}

		private void EmuYield()
		{
			_currThread.Yield();
		}

		public void DisableLuaScript(LuaFile file)
		{
			if (file.IsSeparator) return;

			file.State = LuaFile.RunState.Disabled;

			if (file.Thread is not null)
			{
				CallExitEvent(file);
				RegisteredFunctions.RemoveForFile(file, _mainFormApi.Emulator);
				file.Stop();
			}
		}

		public void EnableLuaFile(LuaFile item, bool shareGlobals)
		{
			LuaSandbox.Sandbox(null, () =>
			{
				SpawnAndSetFileThread(item, shareGlobals);
				LuaSandbox.CreateSandbox(item.Thread, Path.GetDirectoryName(item.Path));
			}, () =>
			{
				item.State = LuaFile.RunState.Disabled;
			});

			// there used to be a call here which did a redraw of the Gui/OSD, which included a call to `Tools.UpdateToolsAfter` --yoshi
		}
	}
}
