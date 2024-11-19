using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using BizHawk.Bizware.Graphics;
using BizHawk.Common;
using BizHawk.Common.PathExtensions;
using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.CustomControls;
using BizHawk.Emulation.Cores;

namespace BizHawk.Client.EmuHawk
{
	internal static class Program
	{
		// Declared here instead of a more usual place to avoid dependencies on the more usual place

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool SetDllDirectoryW(string lpPathName);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool DeleteFileW(string lpFileName);

		static Program()
		{
			// This needs to be done before the warnings/errors show up
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			// Quickly check if the user is running this as a 32 bit process somehow
			// TODO: We may want to remove this sometime, EmuHawk should be able to run somewhat as 32 bit if the user really wants to
			// (There are no longer any hard 64 bit deps, i.e. SlimDX is no longer around)
			if (!Environment.Is64BitProcess)
			{
				using (var box = new ExceptionBox(
							"EmuHawk requires a 64 bit environment in order to run! EmuHawk will now close."))
				{
					box.ShowDialog();
				}

				Process.GetCurrentProcess().Kill();
				return;
			}

			// In case assembly resolution fails, such as if we moved them into the dll subdiretory, this event handler can reroute to them
			AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				// Windows needs extra considerations for the dll directory
				// we can skip all this on non-Windows platforms
				return;
			}

			// this will look in subdirectory "dll" to load pinvoked stuff
			var dllDir = Path.Combine(AppContext.BaseDirectory, "dll");
			_ = SetDllDirectoryW(dllDir);

			try
			{
				// but before we even try doing that, whack the MOTW from everything in that directory (that's a dll)
				// otherwise, some people will have crashes at boot-up due to .net security disliking MOTW.
				// some people are getting MOTW through a combination of browser used to download bizhawk, and program used to dearchive it
				// We need to do it here too... otherwise people get exceptions when externaltools we distribute try to startup
				static void RemoveMOTW(string path) => DeleteFileW($"{path}:Zone.Identifier");
				var todo = new Queue<DirectoryInfo>(new[] { new DirectoryInfo(dllDir) });
				while (todo.Count != 0)
				{
					var di = todo.Dequeue();
					foreach (var disub in di.GetDirectories()) todo.Enqueue(disub);
					foreach (var fi in di.GetFiles("*.dll")) RemoveMOTW(fi.FullName);
					foreach (var fi in di.GetFiles("*.exe")) RemoveMOTW(fi.FullName);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine($"MotW remover failed: {e}");
			}
		}

		[STAThread]
		private static int Main(string[] args)
		{
			var exitCode = SubMain(args);
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				Console.WriteLine("BizHawk has completed its shutdown routines, killing process...");
				Process.GetCurrentProcess().Kill();
			}
			return exitCode;
		}

		// NoInlining should keep this code from getting jammed into Main() which would create dependencies on types which havent been setup by the resolver yet... or something like that
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
		private static int SubMain(string[] args)
		{
			// this check has to be done VERY early.  i stepped through a debug build with wrong .dll versions purposely used,
			// and there was a TypeLoadException before the first line of SubMain was reached (some static ColorType init?)
			var thisAsmVer = ReflectionCache.AsmVersion;
			if (new[]
				{
					BizInvoke.ReflectionCache.AsmVersion,
					Bizware.Audio.ReflectionCache.AsmVersion,
					Bizware.Graphics.ReflectionCache.AsmVersion,
					Bizware.Graphics.Controls.ReflectionCache.AsmVersion,
					Bizware.Input.ReflectionCache.AsmVersion,
					Client.Common.ReflectionCache.AsmVersion,
					Common.ReflectionCache.AsmVersion,
					Emulation.Common.ReflectionCache.AsmVersion,
					Emulation.Cores.ReflectionCache.AsmVersion,
					Emulation.DiscSystem.ReflectionCache.AsmVersion,
					WinForms.Controls.ReflectionCache.AsmVersion,
				}.Any(asmVer => asmVer != thisAsmVer))
			{
				MessageBox.Show("One or more of the BizHawk.* assemblies have the wrong version!\n(Did you attempt to update by overwriting an existing install?)");
				return -1;
			}

			if (!OSTailoredCode.IsUnixHost)
			{
				// Check if we have the C++ VS2015-2022 redist all in one redist be installed
				var p = OSTailoredCode.LinkedLibManager.LoadOrZero("vcruntime140_1.dll");
				if (p != IntPtr.Zero)
				{
					OSTailoredCode.LinkedLibManager.FreeByPtr(p);
				}
				else
				{
					// else it's missing or corrupted
					const string desc =
						"Microsoft Visual C++ Redistributable for Visual Studio 2015, 2017, 2019, and 2022 (x64)";
					MessageBox.Show($"EmuHawk needs {desc} in order to run! See the readme on GitHub for more info. (EmuHawk will now close.) " +
						$"Internal error message: {OSTailoredCode.LinkedLibManager.GetErrorMessage()}");
					return -1;
				}
			}

			typeof(Form).GetField(OSTailoredCode.IsUnixHost ? "default_icon" : "defaultIcon", BindingFlags.NonPublic | BindingFlags.Static)!
				.SetValue(null, Properties.Resources.Logo);

			TempFileManager.Start();

			HawkFile.DearchivalMethod = SharpCompressDearchivalMethod.Instance;

			ParsedCLIFlags cliFlags = default;
			try
			{
				if (ArgParser.ParseArguments(out cliFlags, args) is int exitCode1) return exitCode1;
			}
			catch (ArgParser.ArgParserException e)
			{
				new ExceptionBox(e.Message).ShowDialog();
				return 1;
			}

			var configPath = cliFlags.cmdConfigFile ?? Path.Combine(PathUtils.ExeDirectoryPath, "config.ini");

			Config initialConfig;
			try
			{
				if (!VersionInfo.DeveloperBuild && !ConfigService.IsFromSameVersion(configPath, out var msg))
				{
					new MsgBox(msg, "Mismatched version in config file", MessageBoxIcon.Warning).ShowDialog();
				}
				initialConfig = ConfigService.Load<Config>(configPath);
			}
			catch (Exception e)
			{
				new ExceptionBox(string.Join("\n",
					"It appears your config file (config.ini) is corrupted; an exception was thrown while loading it.",
					"On closing this warning, EmuHawk will delete your config file and generate a new one. You can go make a backup now if you'd like to look into diffs.",
					"The caught exception was:",
					e.ToString()
				)).ShowDialog();
				File.Delete(configPath);
				initialConfig = ConfigService.Load<Config>(configPath);
			}
			initialConfig.ResolveDefaults();
			if (initialConfig.SaveSlot is 0) initialConfig.SaveSlot = 10; //TODO remove after a while
			// initialConfig should really be globalConfig as it's mutable

			//TODO ideally this should happen as early as possible, which means parsing the config earlier, which means parsing the command-line flags earlier... actually there's not that much else above this --yoshi
			if (initialConfig.SetInvariantCulture)
			{
				CultureInfo.CurrentCulture = CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
			}

			StringLogUtil.DefaultToDisk = initialConfig.Movies.MoviesOnDisk;

			// must be done VERY early, before any SDL_Init calls can be done
			// if this isn't done, SIGINT/SIGTERM get swallowed by SDL
			if (OSTailoredCode.IsUnixHost)
			{
				SDL2.SDL.SDL_SetHintWithPriority(SDL2.SDL.SDL_HINT_NO_SIGNAL_HANDLERS, "1", SDL2.SDL.SDL_HintPriority.SDL_HINT_OVERRIDE);
			}

			var glInitCount = 0;

			IGL TryInitIGL(EDispMethod dispMethod)
			{
				glInitCount++;

				(EDispMethod Method, string Name) ChooseFallback()
					=> glInitCount switch
					{
						// try to fallback on the faster option on Windows
						// if we're on a Unix platform, there's only 1 fallback here...
						1 when OSTailoredCode.IsUnixHost => (EDispMethod.GdiPlus, "GDI+"),
						1 or 2 when !OSTailoredCode.IsUnixHost => dispMethod == EDispMethod.D3D11
							? (EDispMethod.OpenGL, "OpenGL")
							: (EDispMethod.D3D11, "Direct3D11"),
						_ => (EDispMethod.GdiPlus, "GDI+")
					};

				IGL CheckRenderer(IGL gl)
				{
					try
					{
						using (gl.CreateGuiRenderer()) return gl;
					}
					catch (Exception ex)
					{
						var (method, name) = ChooseFallback();
						new ExceptionBox(new Exception($"Initialization of Display Method failed; falling back to {name}", ex)).ShowDialog();
						return TryInitIGL(initialConfig.DispMethod = method);
					}
				}

				switch (dispMethod)
				{
					case EDispMethod.D3D11:
						if (OSTailoredCode.IsUnixHost || OSTailoredCode.IsWine)
						{
							// possibly sharing config w/ Windows, assume the user wants the not-slow method (but don't change the config)
							return TryInitIGL(EDispMethod.OpenGL);
						}
						try
						{
							return CheckRenderer(new IGL_D3D11());
						}
						catch (Exception ex)
						{
							var (method, name) = ChooseFallback();
							new ExceptionBox(new Exception($"Initialization of Direct3D11 Display Method failed; falling back to {name}", ex)).ShowDialog();
							return TryInitIGL(initialConfig.DispMethod = method);
						}
					case EDispMethod.OpenGL:
						if (!IGL_OpenGL.Available)
						{
							// too old to use, need to fallback to something else
							var (method, name) = ChooseFallback();
							new ExceptionBox(new Exception($"Initialization of OpenGL Display Method failed; falling back to {name}")).ShowDialog();
							return TryInitIGL(initialConfig.DispMethod = method);
						}
						// need to have a context active for checking renderer, will be disposed afterwards
						using (new SDL2OpenGLContext(3, 2, true))
						{
							using var testOpenGL = new IGL_OpenGL();
							testOpenGL.InitGLState();
							_ = CheckRenderer(testOpenGL);
						}

						// don't return the same IGL, we don't want the test context to be part of this IGL
						return new IGL_OpenGL();
					default:
					case EDispMethod.GdiPlus:
						// if this fails, we're screwed
						return new IGL_GDIPlus();
				}
			}

			// super hacky! this needs to be done first. still not worth the trouble to make this system fully proper
			if (Array.Exists(args, arg => arg.StartsWith("--gdi", StringComparison.InvariantCultureIgnoreCase)))
			{
				initialConfig.DispMethod = EDispMethod.GdiPlus;
			}

			var workingGL = TryInitIGL(initialConfig.DispMethod);

			Sound globalSound = null;

			if (!OSTailoredCode.IsUnixHost)
			{
				// WHY do we have to do this? some intel graphics drivers (ig7icd64.dll 10.18.10.3304 on an unknown chip on win8.1) are calling SetDllDirectory() for the process, which ruins stuff.
				// The relevant initialization happened just before in "create IGL context".
				// It isn't clear whether we need the earlier SetDllDirectory(), but I think we do.
				// note: this is pasted instead of being put in a static method due to this initialization code being sensitive to things like that, and not wanting to cause it to break
				// pasting should be safe (not affecting the jit order of things)
				var dllDir = Path.Combine(AppContext.BaseDirectory, "dll");
				_ = SetDllDirectoryW(dllDir);
			}

			if (!initialConfig.SkipSuperuserPrivsCheck
				&& OSTailoredCode.HostWindowsVersion is null or { Version: >= OSTailoredCode.WindowsVersion._10 }) // "windows isn't capable of being useful for non-administrators until windows 10" --zeromus
			{
				if (EmuHawkUtil.CLRHostHasElevatedPrivileges)
				{
					using MsgBox dialog = new(
						title: "This EmuHawk is privileged",
						message: $"EmuHawk detected it {(OSTailoredCode.IsUnixHost ? "is running as root (Superuser)" : "has Administrator privileges")}.\n"
							+ $"Regularly using {(OSTailoredCode.IsUnixHost ? "Superuser" : "Administrator")} for things other than system administration makes it easier to hack you.\n"
							+ "If you're certain, you may continue anyway (and without support).\n"
							+ $"You'll find a flag \"{nameof(Config.SkipSuperuserPrivsCheck)}\" in the config file, which disables this warning.",
						boxIcon: MessageBoxIcon.Warning);
					dialog.ShowDialog();
				}
				else
				{
					Util.DebugWriteLine("running as unprivileged user");
				}
			}

			FPCtrl.FixFPCtrl();

			var exitCode = 0;
			try
			{
				GameDBHelper.BackgroundInitAll();
#if BIZHAWKBUILD_RUN_ONLY_GAMEDB_INIT
				GameDBHelper.WaitForThreadAndQuickTest();
#else
				MainForm mf = new(
					cliFlags,
					workingGL,
					() => configPath,
					() => initialConfig,
					newSound => globalSound = newSound,
					args,
					out var movieSession,
					out var exitEarly);
				if (exitEarly)
				{
					//TODO also use this for ArgParser failure
					mf.Dispose();
					return 0;
				}
				mf.LoadGlobalConfigFromFile = iniPath =>
				{
					if (!VersionInfo.DeveloperBuild && !ConfigService.IsFromSameVersion(iniPath, out var msg))
					{
						new MsgBox(msg, "Mismatched version in config file", MessageBoxIcon.Warning).ShowDialog();
					}
					initialConfig = ConfigService.Load<Config>(iniPath);
					initialConfig.ResolveDefaults();
					// ReSharper disable once AccessToDisposedClosure
					mf.Config = initialConfig;
				};
				mf.Show();
				try
				{
					exitCode = mf.ProgramRunLoop();
					if (!mf.IsDisposed)
						mf.Dispose();
				}
				catch (Exception e) when (movieSession.Movie.IsActive() && !(Debugger.IsAttached || VersionInfo.DeveloperBuild))
				{
					var result = MessageBox.Show(
						"EmuHawk has thrown a fatal exception and is about to close.\nA movie has been detected. Would you like to try to save?\n(Note: Depending on what caused this error, this may or may not succeed)",
						$"Fatal error: {e.GetType().Name}",
						MessageBoxButtons.YesNo,
						MessageBoxIcon.Exclamation
					);
					if (result == DialogResult.Yes)
					{
						movieSession.Movie.Save();
					}
				}
#endif
			}
			catch (Exception e) when (!Debugger.IsAttached)
			{
				new ExceptionBox(e).ShowDialog();
			}
			finally
			{
				globalSound?.Dispose();
				workingGL.Dispose();
				Input.Instance?.Adapter?.DeInitAll();
			}

			// return 0 assuming things have gone well, non-zero values could be used as error codes or for scripting purposes
			return exitCode;
		}

		/// <remarks>http://www.codeproject.com/Articles/310675/AppDomain-AssemblyResolve-Event-Tips</remarks>
		private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			var requested = args.Name;

			lock (AppDomain.CurrentDomain)
			{
				var firstAsm = Array.Find(AppDomain.CurrentDomain.GetAssemblies(), asm => asm.FullName == requested);
				if (firstAsm != null)
				{
					return firstAsm;
				}

				// load missing assemblies by trying to find them in the dll directory
				var dllname = $"{new AssemblyName(requested).Name}.dll";
				var directory = Path.Combine(AppContext.BaseDirectory, "dll");
				var fname = Path.Combine(directory, dllname);
				// it is important that we use LoadFile here and not load from a byte array; otherwise mixed (managed/unmanaged) assemblies can't load
				return File.Exists(fname) ? Assembly.LoadFile(fname) : null;
			}
		}
	}
}
