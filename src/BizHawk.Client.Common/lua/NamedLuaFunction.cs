using System;
using NLua;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public sealed class NamedLuaFunction : INamedLuaFunction
	{
		public const string EVENT_TYPE_CONSOLECLOSE = "OnConsoleClose";

		public const string EVENT_TYPE_ENGINESTOP = "OnExit";

		public const string EVENT_TYPE_INPUTPOLL = "OnInputPoll";

		public const string EVENT_TYPE_LOADSTATE = "OnSavestateLoad";

		public const string EVENT_TYPE_MEMEXEC = "OnMemoryExecute";

		public const string EVENT_TYPE_MEMEXECANY = "OnMemoryExecuteAny";

		public const string EVENT_TYPE_MEMREAD = "OnMemoryRead";

		public const string EVENT_TYPE_MEMWRITE = "OnMemoryWrite";

		public const string EVENT_TYPE_POSTFRAME = "OnFrameEnd";

		public const string EVENT_TYPE_PREFRAME = "OnFrameStart";

		public const string EVENT_TYPE_SAVESTATE = "OnSavestateSave";

		private readonly LuaFunction _function;

		public NamedLuaFunction(LuaFunction function, string theEvent, Action<string> logCallback, LuaFile luaFile, Func<LuaThread> createThreadCallback, string name = null)
		{
			_function = function;
			Name = name ?? "Anonymous";
			Event = theEvent;
			CreateThreadCallback = createThreadCallback;

			// When would a file be null?
			// When a script is loaded with a callback, but no infinite loop so it closes
			// Then that callback proceeds to register more callbacks
			// In these situations, we will generate a thread for this new callback on the fly here
			// Scenarios like this suggest that a thread being managed by a LuaFile is a bad idea,
			// and we should refactor
			if (luaFile == null)
			{
				DetachFromScript();
			}
			else
			{
				LuaFile = luaFile;
			}

			Guid = Guid.NewGuid();

			Callback = args =>
			{
				try
				{
					_function.Call(args);
				}
				catch (Exception ex)
				{
					logCallback($"error running function attached by the event {Event}\nError message: {ex.Message}");
				}
			};
			InputCallback = () => Callback(Array.Empty<object>());
			MemCallback = (addr, val, flags) => Callback(new object[] { addr, val, flags });
		}

		public void DetachFromScript()
		{
			var thread = CreateThreadCallback();
			LuaSandbox.CreateSandbox(thread, LuaFile.CurrentDirectory);
			LuaFile.Thread = thread;
		}

		public Guid Guid { get; }

		public string Name { get; }

		public LuaFile LuaFile { get; private set; }

		private Func<LuaThread> CreateThreadCallback { get; }

		public string Event { get; }

		private Action<object[]> Callback { get; }

		public Action InputCallback { get; }

		public MemoryCallbackDelegate MemCallback { get; }

		public void Call(string name = null)
		{
			LuaSandbox.Sandbox(LuaFile.Thread, () =>
			{
				_function.Call(name);
			});
		}
	}
}
