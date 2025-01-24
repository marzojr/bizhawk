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
		SUCCESS = 0,
		NOT_INIT,        /* Function is disallowed before InitMupen64Plus() is called */
		ALREADY_INIT,    /* InitMupen64Plus() was called twice */
		INCOMPATIBLE,    /* API versions between components are incompatible */
		INPUT_ASSERT,    /* Invalid parameters for function call, such as ParamValue=NULL for GetCoreParameter() */
		INPUT_INVALID,   /* Invalid input data, such as ParamValue="maybe" for SetCoreParameter() to set a BOOL-type value */
		INPUT_NOT_FOUND, /* The input parameter(s) specified a particular item which was not found */
		NO_MEMORY,       /* Memory allocation failed */
		FILES,           /* Error opening, creating, reading, or writing to a file */
		INTERNAL,        /* Internal error (bug) */
		INVALID_STATE,   /* Current program state does not allow operation */
		PLUGIN_FAIL,     /* A plugin function returned a fatal error */
		SYSTEM_FAIL,     /* A system function call, such as an SDL or file operation, failed */
		UNSUPPORTED,     /* Function call is not supported (ie, core not built with debugger) */
		WRONG_TYPE       /* A given input type parameter cannot be used for desired operation */
	}

	public enum m64p_command
	{
		NOP = 0,
		ROM_OPEN,
		ROM_CLOSE,
		ROM_GET_HEADER,
		ROM_GET_SETTINGS,
		EXECUTE,
		STOP,
		PAUSE,
		RESUME,
		CORE_STATE_QUERY,
		STATE_LOAD,
		STATE_SAVE,
		STATE_SET_SLOT,
		SEND_SDL_KEYDOWN,
		SEND_SDL_KEYUP,
		SET_FRAME_CALLBACK,
		TAKE_NEXT_SCREENSHOT,
		CORE_STATE_SET,
		READ_SCREEN,
		RESET,
		ADVANCE_FRAME
	}

	public enum m64p_plugin_type
	{
		NULL = 0,
		RSP = 1,
		GFX,
		AUDIO,
		INPUT,
		CORE
	}

	public enum m64p_core_param
	{
		EMU_STATE = 1,
		VIDEO_MODE,
		SAVESTATE_SLOT,
		SPEED_FACTOR,
		SPEED_LIMITER,
		VIDEO_SIZE,
		AUDIO_VOLUME,
		AUDIO_MUTE,
		INPUT_GAMESHARK,
		STATE_LOADCOMPLETE,
		STATE_SAVECOMPLETE
	}

	public enum m64p_emu_state
	{
		STOPPED = 1,
		RUNNING,
		PAUSED
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct m64p_2d_size
	{
		public uint uiWidth;
		public uint uiHeight;
	}

	public enum m64p_video_mode
	{
		NONE = 1,
		WINDOWED,
		FULLSCREEN
	}

	public enum m64p_video_flags
	{
		SUPPORT_RESIZING = 1
	}

	public enum m64p_GLattr
	{
		DOUBLEBUFFER = 1,
		BUFFER_SIZE,
		DEPTH_SIZE,
		RED_SIZE,
		GREEN_SIZE,
		BLUE_SIZE,
		ALPHA_SIZE,
		SWAP_CONTROL,
		MULTISAMPLEBUFFERS,
		MULTISAMPLESAMPLES,
		CONTEXT_MAJOR_VERSION,
		CONTEXT_MINOR_VERSION,
		CONTEXT_PROFILE_MASK
	}

	public enum m64p_render_mode
	{
		OPENGL = 0,
		VULKAN
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
		INT = 1,
		FLOAT,
		BOOL,
		STRING
	}

	public enum m64p_dbg_runstate
	{
		PAUSED = 0,
		STEPPING,
		RUNNING
	}

	public enum m64p_dbg_cpu_data
	{
		PC = 1,
		REG_REG,
		REG_HI,
		REG_LO,
		REG_COP0,
		REG_COP1_DOUBLE_PTR,
		REG_COP1_SIMPLE_PTR,
		REG_COP1_FGR_64,
		TLB
	}

	public enum m64p_msg_level
	{
		ERROR = 1,
		WARNING,
		INFO,
		STATUS,
		VERBOSE
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
    public delegate m64p_error VidExtFuncVKGetInstanceExtensions(ref IntPtr[] extensions, ref uint numExtensions);

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
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = VidExtFunctions)]
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
	public delegate void DebugCallback(IntPtr context, m64p_msg_level level, string message);

	public delegate void dbg_frontend_init();
	public delegate void dbg_frontend_update(uint pc);
	public delegate void dbg_frontend_vi();

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
		return CoreDoCommand(m64p_command.CORE_STATE_SET, (int)parameter, (IntPtr)valuePointer);
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
	public abstract uint DebugMemRead32(uint address);

	[BizImport(CallingConvention.Cdecl)]
	public abstract void DebugMemWrite8(uint address, byte value);

	[BizImport(CallingConvention.Cdecl)]
	public abstract IntPtr DebugGetCPUDataPtr(m64p_dbg_cpu_data cpuDataType);

	[BizImport(CallingConvention.Cdecl)]
	public abstract m64p_error ConfigOpenSection(string sectionName, ref IntPtr configSectionHandle);

	[BizImport(CallingConvention.Cdecl)]
	public abstract m64p_error ConfigSetParameter(IntPtr configSectionHandle, string paramName, m64p_type paramType, IntPtr paramValue);

	[BizImport(CallingConvention.Cdecl)]
	public abstract m64p_error DebugSetCallbacks(dbg_frontend_init dbgFrontendInit, dbg_frontend_update dbgFrontendUpdate, dbg_frontend_vi dbgFrontendVi);

	[BizImport(CallingConvention.Cdecl)]
	public abstract m64p_error DebugSetRunState(m64p_dbg_runstate runstate);

	[BizImport(CallingConvention.Cdecl)]
	public abstract m64p_error DebugStep();

	[BizImport(CallingConvention.Cdecl)]
	public abstract void DebugDecodeOp(uint instruction, byte[] op, byte[] args, int pc);

	[BizImport(CallingConvention.Cdecl)]
	public abstract void SaveSavestate(byte[] buffer);

	[BizImport(CallingConvention.Cdecl)]
	public abstract bool LoadSavestate(byte[] buffer);
}
