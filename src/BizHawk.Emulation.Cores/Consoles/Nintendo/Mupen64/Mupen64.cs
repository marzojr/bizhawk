using System.Runtime.InteropServices;
using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using static BizHawk.Emulation.Cores.Consoles.Nintendo.Mupen64.Mupen64Api;
using Thread = System.Threading.Thread;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Mupen64;

[PortedCore(CoreNames.Mupen64Plus, "", "2.5.9+", "https://github.com/mupen64plus/mupen64plus-core", isReleased: false)]
[ServiceNotApplicable([ typeof(IDriveLight) ])]
public partial class Mupen64 : IEmulator
{
	private readonly IOpenGLProvider _openGLProvider;
	private object _glContext;

	private readonly Mupen64Api Mupen64Api;
	private readonly IntPtr Mupen64ApiHandle;

	private readonly Mupen64VideoPluginApi VideoPluginApi;
	private readonly IntPtr VideoPluginApiHandle;

	private readonly Mupen64AudioPluginApi AudioPluginApi;
	private readonly IntPtr AudioPluginApiHandle;

	private readonly Mupen64PluginApi InputPluginApi;
	private readonly IntPtr InputPluginApiHandle;

	private readonly Mupen64PluginApi RspPluginApi;
	private readonly IntPtr RspPluginApiHandle;

	private readonly m64p_video_extension_functions_managed _videoExtensionFunctionsManaged;
	private readonly Thread _coreThread;
	private bool _disposed;

	private void DebugCallback(IntPtr context, int level, string message)
	{
		Console.WriteLine($"DEBUG CALLBACK {level}: {message}");
	}

	private readonly DebugCallback _debugCallback;

	[CoreConstructor(VSystemID.Raw.N64)]
	public Mupen64(CoreLoadParameters<object, object> loadParameters)
	{
		_openGLProvider = loadParameters.Comm.OpenGLProvider;

		(Mupen64Api, Mupen64ApiHandle) = LoadLib<Mupen64Api>("mupen64plus");

		_stateCallback = StateChanged;
		_debugCallback = DebugCallback;
		var error = Mupen64Api.CoreStartup(FRONTEND_API_VERSION, null, null, IntPtr.Zero, _debugCallback, IntPtr.Zero, _stateCallback);
		Console.WriteLine(error.ToString());

		_videoExtensionFunctionsManaged = new m64p_video_extension_functions_managed
		{
			VidExt_Init = VidExt_Init,
			VidExt_Quit = VidExt_Quit,
			VidExt_ListFullscreenModes = VidExt_ListFullscreenModes,
			VidExt_ListFullscreenRates = VidExt_ListFullscreenRates,
			VidExt_SetVideoMode = VidExt_SetVideoMode,
			VidExt_SetVideoModeWithRate = VidExt_SetVideoModeWithRate,
			VidExt_GL_GetProcAddress = VidExt_GL_GetProcAddress,
			VidExt_GL_SetAttribute = VidExt_GL_SetAttribute,
			VidExt_GL_GetAttribute = VidExt_GL_GetAttribute,
			VidExt_GL_SwapBuffers = VidExt_GL_SwapBuffers,
			VidExt_SetCaption = VidExt_SetCaption,
			VidExt_ToggleFullScreen = VidExt_ToggleFullScreen,
			VidExt_ResizeWindow = VidExt_ResizeWindow,
			VidExt_GL_GetDefaultFramebuffer = VidExt_GL_GetDefaultFramebuffer,
			VidExt_InitWithRenderMode = VidExt_InitWithRenderMode,
			VidExt_VK_GetSurface = VidExt_VK_GetSurface,
			VidExt_VK_GetInstanceExtensions = VidExt_VK_GetInstanceExtensions,
		};
		var videoExtensions = new m64p_video_extension_functions(_videoExtensionFunctionsManaged);
		error = Mupen64Api.CoreOverrideVidExt(ref videoExtensions);
		Console.WriteLine(error.ToString());

		var rom = loadParameters.Roms[0];

		error = Mupen64Api.CoreDoCommand(m64p_command.M64CMD_ROM_OPEN, rom.RomData.Length, rom.RomData);
		Console.WriteLine(error.ToString());

		(VideoPluginApi, VideoPluginApiHandle) = LoadLib<Mupen64VideoPluginApi>("mupen64plus-video-GLideN64-debug");
		error = VideoPluginApi.PluginStartup(Mupen64ApiHandle, IntPtr.Zero, IntPtr.Zero);
		Console.WriteLine(error.ToString());
		error = Mupen64Api.CoreAttachPlugin(m64p_plugin_type.M64PLUGIN_GFX, VideoPluginApiHandle);
		Console.WriteLine(error.ToString());

		// (AudioPluginApi, AudioPluginApiHandle) = LoadLib<Mupen64AudioPluginApi>("mupen64plus-audio-sdl");
		// error = AudioPluginApi.PluginStartup(Mupen64ApiHandle, IntPtr.Zero, IntPtr.Zero);
		// Console.WriteLine(error.ToString());
		// error = Mupen64Api.CoreAttachPlugin(Mupen64Api.m64p_plugin_type.M64PLUGIN_AUDIO, AudioPluginApiHandle);
		// Console.WriteLine(error.ToString());

		// (InputPluginApi, InputPluginApiHandle) = LoadLib<Mupen64PluginApi>("mupen64plus-input-bkm");
		// error = InputPluginApi.PluginStartup(Mupen64ApiHandle, IntPtr.Zero, IntPtr.Zero);
		// Console.WriteLine(error.ToString());
		// error = Mupen64Api.CoreAttachPlugin(Mupen64Api.m64p_plugin_type.M64PLUGIN_INPUT, InputPluginApiHandle);
		// error = Mupen64Api.CoreDetachPlugin(m64p_plugin_type.M64PLUGIN_AUDIO);
		// Console.WriteLine(error.ToString());
		error = Mupen64Api.CoreDetachPlugin(m64p_plugin_type.M64PLUGIN_INPUT);
		Console.WriteLine(error.ToString());

		(RspPluginApi, RspPluginApiHandle) = LoadLib<Mupen64PluginApi>("mupen64plus-rsp-hle");
		error = RspPluginApi.PluginStartup(Mupen64ApiHandle, IntPtr.Zero, IntPtr.Zero);
		Console.WriteLine(error.ToString());
		error = Mupen64Api.CoreAttachPlugin(m64p_plugin_type.M64PLUGIN_RSP, RspPluginApiHandle);
		// error = Mupen64Api.CoreDetachPlugin(Mupen64Api.m64p_plugin_type.M64PLUGIN_RSP);
		Console.WriteLine(error.ToString());

		_frameCallback = FrameCallback;
		error = Mupen64Api.CoreDoCommand(m64p_command.M64CMD_SET_FRAME_CALLBACK, 0, Marshal.GetFunctionPointerForDelegate(_frameCallback));
		Console.WriteLine(error.ToString());

		ControllerDefinition = NullController.Instance.Definition;

		var serviceProvider = new BasicServiceProvider(this);
		ServiceProvider = serviceProvider;

		_coreThread = new Thread(RunEmulator) {IsBackground = true};
		_coreThread.Start();
		_frameFinished.WaitOne();
	}

	public IEmulatorServiceProvider ServiceProvider { get; }
	public ControllerDefinition ControllerDefinition { get; }

	public unsafe bool FrameAdvance(IController controller, bool render, bool renderSound = true)
	{
		m64p_emu_state retState = 0;
		var retStatePointer = &retState;
		Mupen64Api.CoreDoCommand(m64p_command.M64CMD_CORE_STATE_QUERY, (int)m64p_core_param.M64CORE_EMU_STATE, (IntPtr)retStatePointer);
		Console.WriteLine($"Current state: {retState}");
		var error = Mupen64Api.CoreDoCommand(m64p_command.M64CMD_ADVANCE_FRAME, 0, IntPtr.Zero);
		Console.WriteLine(error.ToString());
		_frameFinished.WaitOne();
		Frame++;

		return true;
	}

	public int Frame { get; private set; }
	public string SystemId => VSystemID.Raw.N64;
	public bool DeterministicEmulation => true;

	public void ResetCounters()
	{
		Frame = 0;
	}

	public void Dispose()
	{
		if (_disposed) return;
		_disposed = true;

		Console.WriteLine("entering dispose...");
		Mupen64Api.CoreDoCommand(m64p_command.M64CMD_STOP, 0, IntPtr.Zero);
		// the stop command requires trying to advance an additional frame before the core actually stops, as the core is currently paused
		var error = Mupen64Api.CoreDoCommand(m64p_command.M64CMD_ADVANCE_FRAME, 0, IntPtr.Zero);
		_coreThread.Join();

		Mupen64Api.CoreDetachPlugin(m64p_plugin_type.M64PLUGIN_GFX);
		VideoPluginApi.PluginShutdown();
		OSTailoredCode.LinkedLibManager.FreeByPtr(VideoPluginApiHandle);

		// Mupen64Api.CoreDetachPlugin(Mupen64Api.m64p_plugin_type.M64PLUGIN_AUDIO);
		// AudioPluginApi.PluginShutdown();
		// OSTailoredCode.LinkedLibManager.FreeByPtr(AudioPluginApiHandle);
		//
		// Mupen64Api.CoreDetachPlugin(Mupen64Api.m64p_plugin_type.M64PLUGIN_INPUT);
		// InputPluginApi.PluginShutdown();
		// OSTailoredCode.LinkedLibManager.FreeByPtr(InputPluginApiHandle);

		Mupen64Api.CoreDetachPlugin(m64p_plugin_type.M64PLUGIN_RSP);
		RspPluginApi.PluginShutdown();
		OSTailoredCode.LinkedLibManager.FreeByPtr(RspPluginApiHandle);

		Mupen64Api.CoreDoCommand(m64p_command.M64CMD_ROM_CLOSE, 0, IntPtr.Zero);
		Mupen64Api.CoreShutdown();
		OSTailoredCode.LinkedLibManager.FreeByPtr(Mupen64ApiHandle);

		if (_glContext is not null)
			_openGLProvider.ReleaseGLContext(_glContext);
	}

	private bool _hasPaused;
	private readonly StateCallback _stateCallback;
	private void StateChanged(IntPtr context2, m64p_core_param paramChanged, int newValue)
	{
		Console.WriteLine($"State changed! Param {paramChanged}, new value {newValue}");
		if (_hasPaused) return;

		if (paramChanged == m64p_core_param.M64CORE_EMU_STATE)
		{
			var newState = (m64p_emu_state)newValue;
			if (newState == m64p_emu_state.M64EMU_RUNNING)
			{
				_hasPaused = true;
				// can't actually send the pause command here because it'll immediately get overwritten again
				// luckily the frame advance command is effectively the same; just pauses after the next frame
				Mupen64Api.CoreDoCommand(m64p_command.M64CMD_ADVANCE_FRAME, 0, IntPtr.Zero);
			}
		}
	}

	private void RunEmulator() => Mupen64Api.CoreDoCommand(m64p_command.M64CMD_EXECUTE, 0, IntPtr.Zero);

	private static (T Lib, IntPtr NativeHandle) LoadLib<T>(string name) where T : class
	{
		var resolver = new DynamicLibraryImportResolver(OSTailoredCode.IsUnixHost ? $"lib{name}.so" : $"{name}.dll", hasLimitedLifetime: false);
		return (BizInvoker.GetInvoker<T>(resolver, CallingConventionAdapters.Native), resolver.GetHandle());
	}
}
