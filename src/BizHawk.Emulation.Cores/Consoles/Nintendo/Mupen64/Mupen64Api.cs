using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using BizHawk.BizInvoke;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Mupen64;

public abstract class Mupen64Api
{
	public const int FRONTEND_API_VERSION = 0x020106;

	public enum m64p_error
	{
		M64ERR_SUCCESS = 0,
		M64ERR_NOT_INIT,        /* Function is disallowed before InitMupen64Plus() is called */
		M64ERR_ALREADY_INIT,    /* InitMupen64Plus() was called twice */
		M64ERR_INCOMPATIBLE,    /* API versions between components are incompatible */
		M64ERR_INPUT_ASSERT,    /* Invalid parameters for function call, such as ParamValue=NULL for GetCoreParameter() */
		M64ERR_INPUT_INVALID,   /* Invalid input data, such as ParamValue="maybe" for SetCoreParameter() to set a BOOL-type value */
		M64ERR_INPUT_NOT_FOUND, /* The input parameter(s) specified a particular item which was not found */
		M64ERR_NO_MEMORY,       /* Memory allocation failed */
		M64ERR_FILES,           /* Error opening, creating, reading, or writing to a file */
		M64ERR_INTERNAL,        /* Internal error (bug) */
		M64ERR_INVALID_STATE,   /* Current program state does not allow operation */
		M64ERR_PLUGIN_FAIL,     /* A plugin function returned a fatal error */
		M64ERR_SYSTEM_FAIL,     /* A system function call, such as an SDL or file operation, failed */
		M64ERR_UNSUPPORTED,     /* Function call is not supported (ie, core not built with debugger) */
		M64ERR_WRONG_TYPE       /* A given input type parameter cannot be used for desired operation */
	}

	public enum m64p_command
	{
		M64CMD_NOP = 0,
		M64CMD_ROM_OPEN,
		M64CMD_ROM_CLOSE,
		M64CMD_ROM_GET_HEADER,
		M64CMD_ROM_GET_SETTINGS,
		M64CMD_EXECUTE,
		M64CMD_STOP,
		M64CMD_PAUSE,
		M64CMD_RESUME,
		M64CMD_CORE_STATE_QUERY,
		M64CMD_STATE_LOAD,
		M64CMD_STATE_SAVE,
		M64CMD_STATE_SET_SLOT,
		M64CMD_SEND_SDL_KEYDOWN,
		M64CMD_SEND_SDL_KEYUP,
		M64CMD_SET_FRAME_CALLBACK,
		M64CMD_TAKE_NEXT_SCREENSHOT,
		M64CMD_CORE_STATE_SET,
		M64CMD_READ_SCREEN,
		M64CMD_RESET,
		M64CMD_ADVANCE_FRAME
	}

	public enum m64p_plugin_type
	{
		M64PLUGIN_NULL = 0,
		M64PLUGIN_RSP = 1,
		M64PLUGIN_GFX,
		M64PLUGIN_AUDIO,
		M64PLUGIN_INPUT,
		M64PLUGIN_CORE
	}

	public enum m64p_core_param
	{
		M64CORE_EMU_STATE = 1,
		M64CORE_VIDEO_MODE,
		M64CORE_SAVESTATE_SLOT,
		M64CORE_SPEED_FACTOR,
		M64CORE_SPEED_LIMITER,
		M64CORE_VIDEO_SIZE,
		M64CORE_AUDIO_VOLUME,
		M64CORE_AUDIO_MUTE,
		M64CORE_INPUT_GAMESHARK,
		M64CORE_STATE_LOADCOMPLETE,
		M64CORE_STATE_SAVECOMPLETE
	}

	public enum m64p_emu_state
	{
		M64EMU_STOPPED = 1,
		M64EMU_RUNNING,
		M64EMU_PAUSED
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct m64p_2d_size
	{
		public uint uiWidth;
		public uint uiHeight;
	}

	public enum m64p_video_mode
	{
		M64VIDEO_NONE = 1,
		M64VIDEO_WINDOWED,
		M64VIDEO_FULLSCREEN
	}

	public enum m64p_video_flags
	{
		M64VIDEOFLAG_SUPPORT_RESIZING = 1
	}

	public enum m64p_GLattr
	{
		M64P_GL_DOUBLEBUFFER = 1,
		M64P_GL_BUFFER_SIZE,
		M64P_GL_DEPTH_SIZE,
		M64P_GL_RED_SIZE,
		M64P_GL_GREEN_SIZE,
		M64P_GL_BLUE_SIZE,
		M64P_GL_ALPHA_SIZE,
		M64P_GL_SWAP_CONTROL,
		M64P_GL_MULTISAMPLEBUFFERS,
		M64P_GL_MULTISAMPLESAMPLES,
		M64P_GL_CONTEXT_MAJOR_VERSION,
		M64P_GL_CONTEXT_MINOR_VERSION,
		M64P_GL_CONTEXT_PROFILE_MASK
	}

	public enum m64p_render_mode
	{
		M64P_RENDER_OPENGL = 0,
		M64P_RENDER_VULKAN
	}

	public enum m64p_dbg_memptr_type
	{
		RDRAM = 1,
		PI_REG,
		SI_REG,
		VI_REG,
		RI_REG,
		AI_REG
	}

	public enum m64p_type {
		M64TYPE_INT = 1,
		M64TYPE_FLOAT,
		M64TYPE_BOOL,
		M64TYPE_STRING
	}

	private const int VidExtFunctions = 17;
	public delegate m64p_error VidExtFuncInit();
	public delegate m64p_error VidExtFuncQuit();
	public delegate m64p_error VidExtFuncListModes(m64p_2d_size[] sizeArray, ref int numSizes);
	public delegate m64p_error VidExtFuncListRates(m64p_2d_size size, ref int numRates, int[] rates);
	public delegate m64p_error VidExtFuncSetMode(int width, int height, int bitsPerPixel, m64p_video_mode screenMode, m64p_video_flags flags);
	public delegate m64p_error VidExtFuncSetModeWithRate(int width, int height, int refreshRate, int bitsPerPixel, m64p_video_mode screenMode, m64p_video_flags flags);
	public delegate IntPtr VidExtFuncGLGetProc(string proc);
	public delegate m64p_error VidExtFuncGLSetAttr(m64p_GLattr attr, int value);
	public delegate m64p_error VidExtFuncGLGetAttr(m64p_GLattr attr, ref int pValue);
	public delegate m64p_error VidExtFuncGLSwapBuf();
	public delegate m64p_error VidExtFuncSetCaption(string title);
	public delegate m64p_error VidExtFuncToggleFS();
	public delegate m64p_error VidExtFuncResizeWindow(int width, int height);
	public delegate uint VidExtFuncGLGetDefaultFramebuffer();
    public delegate m64p_error VidExtFuncInitWithRenderMode(m64p_render_mode renderMode);
    public delegate m64p_error VidExtFuncVKGetSurface(ref IntPtr surface, IntPtr instance);
    public delegate m64p_error VidExtFuncVKGetInstanceExtensions(ref IntPtr extensions, ref uint numExtensions);

	[StructLayout(LayoutKind.Sequential)]
	public sealed class m64p_video_extension_functions_managed
	{
		public VidExtFuncInit VidExt_Init;
		public VidExtFuncQuit VidExt_Quit;
		public VidExtFuncListModes VidExt_ListFullscreenModes;
		public VidExtFuncListRates VidExt_ListFullscreenRates;
		public VidExtFuncSetMode VidExt_SetVideoMode;
		public VidExtFuncSetModeWithRate VidExt_SetVideoModeWithRate;
		public VidExtFuncGLGetProc VidExt_GL_GetProcAddress;
		public VidExtFuncGLSetAttr VidExt_GL_SetAttribute;
		public VidExtFuncGLGetAttr VidExt_GL_GetAttribute;
		public VidExtFuncGLSwapBuf VidExt_GL_SwapBuffers;
		public VidExtFuncSetCaption VidExt_SetCaption;
		public VidExtFuncToggleFS VidExt_ToggleFullScreen;
		public VidExtFuncResizeWindow VidExt_ResizeWindow;
		public VidExtFuncGLGetDefaultFramebuffer VidExt_GL_GetDefaultFramebuffer;
		public VidExtFuncInitWithRenderMode VidExt_InitWithRenderMode;
		public VidExtFuncVKGetSurface VidExt_VK_GetSurface;
		public VidExtFuncVKGetInstanceExtensions VidExt_VK_GetInstanceExtensions;

		private static List<FieldInfo> FieldsInOrder;

		public IEnumerable<Delegate> AllDelegatesInMemoryOrder()
		{
			FieldsInOrder ??= typeof(m64p_video_extension_functions_managed)
				.GetFields()
				.OrderBy(BizInvokerUtilities.ComputeFieldOffset)
				.ToList();
			return FieldsInOrder
				.Select(f => (Delegate)f.GetValue(this));
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct m64p_video_extension_functions
	{
		private uint Functions;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
		private IntPtr[] ExtensionFunctions;

		public m64p_video_extension_functions(m64p_video_extension_functions_managed extensionFunctionsManaged)
		{
			ExtensionFunctions = extensionFunctionsManaged.AllDelegatesInMemoryOrder()
				.Select(Marshal.GetFunctionPointerForDelegate)
				.ToArray();
			Functions = (uint)ExtensionFunctions.Length;
			if (Functions != VidExtFunctions)
				throw new InvalidOperationException($"{nameof(m64p_video_extension_functions_managed)} needs to have {VidExtFunctions} function pointers set, but got {Functions}.");
		}
	}

	public delegate void m64p_frame_callback(uint frameIndex);
	public delegate void StateCallback(IntPtr context2, m64p_core_param paramChanged, int newValue);
	public delegate void DebugCallback(IntPtr context, int level, string message);

	[BizImport(CallingConvention.Cdecl)]
	public abstract m64p_error CoreStartup(int apiVersion, string configPath, string dataPath, IntPtr context, DebugCallback debugCallback, IntPtr context2, StateCallback stateCallback);

	[BizImport(CallingConvention.Cdecl)]
	public abstract m64p_error CoreShutdown();

	[BizImport(CallingConvention.Cdecl)]
	public abstract m64p_error CoreAttachPlugin(m64p_plugin_type pluginType, IntPtr pluginLibHandle);

	[BizImport(CallingConvention.Cdecl)]
	public abstract m64p_error CoreDetachPlugin(m64p_plugin_type pluginType);

	[BizImport(CallingConvention.Cdecl)]
	public abstract m64p_error CoreDoCommand(m64p_command command, int paramInt, IntPtr paramPtr);

	public unsafe m64p_error CoreDoCommand(m64p_command command, int arrayLength, byte[] array)
	{
		fixed (byte* arrayPointer = array)
			return CoreDoCommand(command, arrayLength, (IntPtr)arrayPointer);
	}
	public unsafe m64p_error CoreDoCommand(m64p_command command, int paramInt, string paramString)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(paramString);
		fixed (byte* bytePointer = bytes)
			return CoreDoCommand(command, paramInt, (IntPtr)bytePointer);
	}
	public unsafe m64p_error CoreStateSet(m64p_core_param parameter, int value)
	{
		int* valuePointer = &value;
		return CoreDoCommand(m64p_command.M64CMD_CORE_STATE_SET, (int)parameter, (IntPtr)valuePointer);
	}

	[BizImport(CallingConvention.Cdecl, Compatibility = true)]
	public abstract m64p_error CoreOverrideVidExt(ref m64p_video_extension_functions videoFunctionStruct);

	[BizImport(CallingConvention.Cdecl)]
	public abstract m64p_error GetSaveRamSize(ref int size);

	[BizImport(CallingConvention.Cdecl)]
	public abstract m64p_error GetSaveRam(byte[] buffer);

	[BizImport(CallingConvention.Cdecl)]
	public abstract m64p_error PutSaveRam(byte[] buffer);

	[BizImport(CallingConvention.Cdecl)]
	public abstract IntPtr DebugMemGetPointer(m64p_dbg_memptr_type memPtrType);

	[BizImport(CallingConvention.Cdecl)]
	public abstract ulong DebugMemGetSize(m64p_dbg_memptr_type memPtrType);

	[BizImport(CallingConvention.Cdecl)]
	public abstract byte DebugMemRead8(uint address);

	[BizImport(CallingConvention.Cdecl)]
	public abstract void DebugMemWrite8(uint address, byte value);

	[BizImport(CallingConvention.Cdecl)]
	public abstract m64p_error ConfigOpenSection(string sectionName, ref IntPtr configSectionHandle);

	[BizImport(CallingConvention.Cdecl)]
	public abstract m64p_error ConfigSetParameter(IntPtr configSectionHandle, string paramName, m64p_type paramType, IntPtr paramValue);
}
