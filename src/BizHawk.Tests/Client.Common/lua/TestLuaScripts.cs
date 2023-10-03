using System.Drawing;
using System.IO;

using BizHawk.Bizware.BizwareGL;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Tests.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BizHawk.Tests.Client.Common.Lua
{
	[TestClass]
	public class TestLuaScripts
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		// null values are initialized in the setup method
		private ILuaLibraries luaLibraries = null;
		private DisplayManagerBase displayManager = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		private const string pathToTestLuaScripts = "Client.Common/lua/LuaScripts";

		[TestInitialize]
		public void TestSetup()
		{
			Config config = new Config();
			IGameInfo gameInfo = new GameInfo();

			IMainFormForApi mainForm = new MockMainFormForApi(new NullEmulator());
			displayManager = new TestDisplayManager(mainForm.Emulator);

			luaLibraries = new TestLuaLibraries(
				mainForm,
				displayManager,
				config,
				gameInfo
			);
			luaLibraries.Restart(config, gameInfo);
		}

		private LuaFile AddScript(string path, bool autoStart = true)
		{
			LuaFile luaFile = new LuaFile("", path);
			luaLibraries.ScriptList.Add(luaFile);
			luaLibraries.EnableLuaFile(luaFile, false);

			if (autoStart)
				luaLibraries.ResumeScript(luaFile);

			return luaFile;
		}

		/// <summary>
		/// console.log is actually going through a test implementation. This test is only meant to support TestScriptsDoNotShareGlobals, and not to test BizHawk.
		/// (TestScriptsDoNotShareGlobals cannot pass if this test does not pass.)
		/// </summary>
		[TestMethod]
		public void TestConsoleLog()
		{
			LuaFile lf = AddScript(Path.Combine(pathToTestLuaScripts, "ShareGlobalsTest1.lua"));

			// The script should at this point be waiting on frameadvance. Make it continue.
			luaLibraries.ResumeScript(lf);

			Assert.AreEqual("hi", ConsoleLuaLibrary.messageLog.Dequeue());
		}

		[TestMethod]
		public void TestScriptsDoNotShareGlobals()
		{
			LuaFile lf = AddScript(Path.Combine(pathToTestLuaScripts, "ShareGlobalsTest1.lua"));
			AddScript(Path.Combine(pathToTestLuaScripts, "ShareGlobalsTest2.lua")); // declares global function of same name as the one in ShareGlobalsTest1

			// The script should at this point be waiting on frameadvance. Make it continue.
			luaLibraries.ResumeScript(lf);

			Assert.AreEqual("hi", ConsoleLuaLibrary.messageLog.Dequeue());
		}

		[TestMethod]
		public void TestDrawingWithOneScript()
		{
			AddScript(Path.Combine(pathToTestLuaScripts, "DrawTest1.lua"));

			BitmapBufferVideoProvider vp = new BitmapBufferVideoProvider(new BitmapBuffer(8, 8));
			var buffer = displayManager.RenderOffscreenLua(vp);

			Assert.AreEqual(Color.Red.ToArgb(), buffer.GetPixel(2, 2));
		}

		[TestMethod]
		public void TestDrawingWithTwoScripts()
		{
			AddScript(Path.Combine(pathToTestLuaScripts, "DrawTest1.lua"));
			AddScript(Path.Combine(pathToTestLuaScripts, "DrawTest2.lua"));

			BitmapBufferVideoProvider vp = new BitmapBufferVideoProvider(new BitmapBuffer(8, 8));
			var buffer = displayManager.RenderOffscreenLua(vp);

			Assert.AreEqual(Color.Red.ToArgb(), buffer.GetPixel(2, 2));
			Assert.AreEqual(0xff00ff00, (uint)buffer.GetPixel(2, 4));
		}

	}
}