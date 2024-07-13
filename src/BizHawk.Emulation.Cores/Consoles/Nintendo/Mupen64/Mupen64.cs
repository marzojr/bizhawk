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

	private readonly Mupen64InputPluginApi InputPluginApi;
	private readonly IntPtr InputPluginApiHandle;

	private readonly Mupen64PluginApi RspPluginApi;
	private readonly IntPtr RspPluginApiHandle;

	private readonly m64p_video_extension_functions_managed _videoExtensionFunctionsManaged;
	private readonly Thread _coreThread;
	private IController _controller;
	private bool _disposed;

	private void DebugCallback(IntPtr context, int level, string message)
	{
		Console.WriteLine($"DEBUG CALLBACK {level}: {message}");
	}

	private readonly DebugCallback _debugCallback;
	private readonly Mupen64InputPluginApi.InputCallback _inputCallback;
	private readonly Mupen64InputPluginApi.RumbleCallback _rumbleCallback;

	[CoreConstructor(VSystemID.Raw.N64)]
	public Mupen64(CoreLoadParameters<object, SyncSettings> loadParameters)
	{
		_openGLProvider = loadParameters.Comm.OpenGLProvider;
		_syncSettings = loadParameters.SyncSettings ?? new SyncSettings();

		var rom = loadParameters.Roms[0];

		byte countryCode = rom.RomData[0x3E];
		// taken from mupen64 source
		Region = countryCode switch
		{
			// PAL codes
			0x44 or 0x46 or 0x49 or 0x50 or 0x53 or 0x55 or 0x58 or 0x59 => DisplayType.PAL,
			// NTSC codes
			_ => DisplayType.NTSC,
		};

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

		error = Mupen64Api.CoreDoCommand(m64p_command.M64CMD_ROM_OPEN, rom.RomData.Length, rom.RomData);
		Console.WriteLine(error.ToString());

		var videoPluginName = $"mupen64plus-video-{_syncSettings.VideoPlugin}";
		(VideoPluginApi, VideoPluginApiHandle) = LoadLib<Mupen64VideoPluginApi>(videoPluginName);
		error = VideoPluginApi.PluginStartup(Mupen64ApiHandle, IntPtr.Zero, IntPtr.Zero);
		Console.WriteLine(error.ToString());
		error = Mupen64Api.CoreAttachPlugin(m64p_plugin_type.M64PLUGIN_GFX, VideoPluginApiHandle);
		Console.WriteLine(error.ToString());

		(AudioPluginApi, AudioPluginApiHandle) = LoadLib<Mupen64AudioPluginApi>("mupen64plus-audio-bkm");
		error = AudioPluginApi.PluginStartup(Mupen64ApiHandle, IntPtr.Zero, IntPtr.Zero);
		Console.WriteLine(error.ToString());
		error = Mupen64Api.CoreAttachPlugin(m64p_plugin_type.M64PLUGIN_AUDIO, AudioPluginApiHandle);
		Console.WriteLine(error.ToString());

		(InputPluginApi, InputPluginApiHandle) = LoadLib<Mupen64InputPluginApi>("mupen64plus-input-bkm");
		error = InputPluginApi.PluginStartup(Mupen64ApiHandle, IntPtr.Zero, IntPtr.Zero);
		Console.WriteLine(error.ToString());
		error = Mupen64Api.CoreAttachPlugin(m64p_plugin_type.M64PLUGIN_INPUT, InputPluginApiHandle);
		Console.WriteLine(error.ToString());

		var rspPluginName = $"mupen64plus-rsp-{_syncSettings.RspPlugin}";
		(RspPluginApi, RspPluginApiHandle) = LoadLib<Mupen64PluginApi>(rspPluginName);
		error = RspPluginApi.PluginStartup(Mupen64ApiHandle, IntPtr.Zero, IntPtr.Zero);
		Console.WriteLine(error.ToString());
		error = Mupen64Api.CoreAttachPlugin(m64p_plugin_type.M64PLUGIN_RSP, RspPluginApiHandle);
		Console.WriteLine(error.ToString());

		_frameCallback = FrameCallback;
		error = Mupen64Api.CoreDoCommand(m64p_command.M64CMD_SET_FRAME_CALLBACK, 0, Marshal.GetFunctionPointerForDelegate(_frameCallback));
		Console.WriteLine(error.ToString());

		ControllerDefinition = Mupen64Controller.MakeControllerDefinition(_syncSettings);
		_inputCallback = InputCallback;
		_rumbleCallback = RumbleCallback;
		InputPluginApi.SetInputCallback(_inputCallback);
		InputPluginApi.SetRumbleCallback(_rumbleCallback);
		InputPluginApi.SetControllerConnected(0, _syncSettings.Port1Connected);
		InputPluginApi.SetControllerConnected(1, _syncSettings.Port2Connected);
		InputPluginApi.SetControllerConnected(2, _syncSettings.Port3Connected);
		InputPluginApi.SetControllerConnected(3, _syncSettings.Port4Connected);
		InputPluginApi.SetControllerPakType(0, _syncSettings.Port1PakType);
		InputPluginApi.SetControllerPakType(1, _syncSettings.Port1PakType);
		InputPluginApi.SetControllerPakType(2, _syncSettings.Port1PakType);
		InputPluginApi.SetControllerPakType(3, _syncSettings.Port1PakType);

		InitSound(AudioPluginApi.GetAudioRate());

		var serviceProvider = new BasicServiceProvider(this);
		serviceProvider.Register<ISoundProvider>(_resampler);
		ServiceProvider = serviceProvider;

		Mupen64Api.CoreStateSet(m64p_core_param.M64CORE_SPEED_LIMITER, 0);
		_coreThread = new Thread(RunEmulator) {IsBackground = true};
		_coreThread.Start();
		_frameFinished.WaitOne();
	}

	public IEmulatorServiceProvider ServiceProvider { get; }
	public ControllerDefinition ControllerDefinition { get; }

	public bool FrameAdvance(IController controller, bool render, bool renderSound = true)
	{
		_controller = controller;
		// m64p_emu_state retState = 0;
		// var retStatePointer = &retState;
		// Mupen64Api.CoreDoCommand(m64p_command.M64CMD_CORE_STATE_QUERY, (int)m64p_core_param.M64CORE_EMU_STATE, (IntPtr)retStatePointer);
		// Console.WriteLine($"Current state: {retState}");

		if (controller.IsPressed("Reset"))
		{
			Mupen64Api.CoreDoCommand(m64p_command.M64CMD_RESET, 0, IntPtr.Zero);
		}
		else if (controller.IsPressed("Power"))
		{
			Mupen64Api.CoreDoCommand(m64p_command.M64CMD_RESET, 1, IntPtr.Zero);
		}

		IsLagFrame = true;
		var error = Mupen64Api.CoreDoCommand(m64p_command.M64CMD_ADVANCE_FRAME, 0, IntPtr.Zero);
		Console.WriteLine(error.ToString());
		_frameFinished.WaitOne();
		UpdateAudio(renderSound);
		Frame++;
		if (IsLagFrame) LagCount++;

		return true;
	}

	public int Frame { get; private set; }
	public string SystemId => VSystemID.Raw.N64;
	public bool DeterministicEmulation => true; // XD

	public void ResetCounters()
	{
		Frame = 0;
		LagCount = 0;
		IsLagFrame = false;
	}

	public void Dispose()
	{
		if (_disposed) return;
		_disposed = true;

		Console.WriteLine("entering dispose...");
		Mupen64Api.CoreDoCommand(m64p_command.M64CMD_STOP, 0, IntPtr.Zero);
		// the stop command requires trying to advance an additional frame before the core actually stops, as the core is currently paused
		var error = Mupen64Api.CoreDoCommand(m64p_command.M64CMD_ADVANCE_FRAME, 0, IntPtr.Zero);
		Console.WriteLine(error.ToString());
		_coreThread.Join();

		Mupen64Api.CoreDetachPlugin(m64p_plugin_type.M64PLUGIN_GFX);
		VideoPluginApi.PluginShutdown();
		OSTailoredCode.LinkedLibManager.FreeByPtr(VideoPluginApiHandle);

		Mupen64Api.CoreDetachPlugin(m64p_plugin_type.M64PLUGIN_AUDIO);
		AudioPluginApi.PluginShutdown();
		OSTailoredCode.LinkedLibManager.FreeByPtr(AudioPluginApiHandle);

		Mupen64Api.CoreDetachPlugin(m64p_plugin_type.M64PLUGIN_INPUT);
		InputPluginApi.PluginShutdown();
		OSTailoredCode.LinkedLibManager.FreeByPtr(InputPluginApiHandle);

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

		if (paramChanged == m64p_core_param.M64CORE_EMU_STATE)
		{
			var newState = (m64p_emu_state)newValue;
			if (newState == m64p_emu_state.M64EMU_RUNNING)
			{
				if (_hasPaused) return;
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

	private Mupen64InputPluginApi.InputState InputCallback(int controller)
	{
		IsLagFrame = false;
		InputCallbacks.Call();

		controller++;
		var ret = new Mupen64InputPluginApi.InputState
		{
			X_AXIS = (sbyte)_controller.AxisValue($"P{controller} X Axis"),
			Y_AXIS = (sbyte)_controller.AxisValue($"P{controller} Y Axis")
		};

		for (int index = 0; index < Mupen64Controller.BoolButtons.Length; index++)
		{
			if (_controller.IsPressed($"P{controller} {Mupen64Controller.BoolButtons[index]}")) ret.boolButtons |= (Mupen64InputPluginApi.BoolButtons)(1 << index);
		}

		return ret;
	}

	private void RumbleCallback(int controller, bool on)
	{
		controller++;
		_controller.SetHapticChannelStrength($"P{controller} Rumble Pak", on ? int.MaxValue : 0);
	}
}
