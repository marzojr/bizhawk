using System.Collections.Generic;
using SDL2;
using static BizHawk.Emulation.Cores.Consoles.Nintendo.Mupen64.Mupen64Api;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Mupen64;

public partial class Mupen64
{
	private static SDL.SDL_GLattr MupenToSDLAttribute(m64p_GLattr value)
	{
		return value switch
		{
			m64p_GLattr.DOUBLEBUFFER => SDL.SDL_GLattr.SDL_GL_DOUBLEBUFFER,
			m64p_GLattr.BUFFER_SIZE => SDL.SDL_GLattr.SDL_GL_BUFFER_SIZE,
			m64p_GLattr.DEPTH_SIZE => SDL.SDL_GLattr.SDL_GL_DEPTH_SIZE,
			m64p_GLattr.RED_SIZE => SDL.SDL_GLattr.SDL_GL_RED_SIZE,
			m64p_GLattr.GREEN_SIZE => SDL.SDL_GLattr.SDL_GL_GREEN_SIZE,
			m64p_GLattr.BLUE_SIZE => SDL.SDL_GLattr.SDL_GL_BLUE_SIZE,
			m64p_GLattr.ALPHA_SIZE => SDL.SDL_GLattr.SDL_GL_ALPHA_SIZE,
			m64p_GLattr.MULTISAMPLEBUFFERS => SDL.SDL_GLattr.SDL_GL_MULTISAMPLEBUFFERS,
			m64p_GLattr.MULTISAMPLESAMPLES => SDL.SDL_GLattr.SDL_GL_MULTISAMPLESAMPLES,
			m64p_GLattr.CONTEXT_MAJOR_VERSION => SDL.SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION,
			m64p_GLattr.CONTEXT_MINOR_VERSION => SDL.SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION,
			m64p_GLattr.CONTEXT_PROFILE_MASK => SDL.SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK,
			_ => (SDL.SDL_GLattr)(-1)
		};
	}

	private readonly Dictionary<SDL.SDL_GLattr, int> GLAttributes = [ ];

	private static m64p_error VidExt_Init()
	{
		return m64p_error.SUCCESS;
	}

	private static m64p_error VidExt_Quit()
	{
		return m64p_error.SUCCESS;
	}

	private static m64p_error VidExt_ListFullscreenModes(m64p_2d_size[] sizeArray, ref int numSizes)
	{
		return m64p_error.UNSUPPORTED;
	}

	private static m64p_error VidExt_ListFullscreenRates(m64p_2d_size size, ref int numRates, int[] rates)
	{
		return m64p_error.UNSUPPORTED;
	}

	private m64p_error VidExt_SetVideoMode(int width, int height, int bitsPerPixel, m64p_video_mode screenMode, m64p_video_flags flags)
	{
		isRenderThread = true;
		Console.WriteLine($"Attempted to SetVideoMode, width {width}, height {height}, bpp {bitsPerPixel}, screenMode {screenMode}, flags {flags}");
		if (_glContext is not null)
			_openGLProvider.ReleaseGLContext(_glContext);
		if (!GLAttributes.TryGetValue(SDL.SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, out int major)) major = 2;
		if (!GLAttributes.TryGetValue(SDL.SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, out int minor)) minor = 1;
		bool coreProfile = GLAttributes.TryGetValue(SDL.SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, out int profile) && (SDL.SDL_GLprofile)profile == SDL.SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_CORE;
		_glContext = _openGLProvider.RequestGLContext(major, minor, coreProfile, width, height, _renderMode is m64p_render_mode.VULKAN);

		BufferWidth = width;
		BufferHeight = height;
		if (_videoBuffer.Length < width * height)
		{
			_videoBuffer = new int[width * height];
			_retVideoBuffer = new byte[width * height * 3];
		}

		return m64p_error.SUCCESS;
	}

	private m64p_error VidExt_SetVideoModeWithRate(int width, int height, int refreshRate, int bitsPerPixel, m64p_video_mode screenMode, m64p_video_flags flags)
	{
		return VidExt_SetVideoMode(width, height, bitsPerPixel, screenMode, flags);
	}

	private IntPtr VidExt_GL_GetProcAddress(string proc)
	{
		return _openGLProvider.GetGLProcAddress(proc);
	}

	private m64p_error VidExt_GL_SetAttribute(m64p_GLattr attr, int value)
	{
		// TODO: this only supports version and profile right now; are others important?
		var attribute = MupenToSDLAttribute(attr);
		if (attribute == (SDL.SDL_GLattr)(-1))
			return m64p_error.INPUT_INVALID;

		GLAttributes[attribute] = value;

		return m64p_error.SUCCESS;
	}

	private m64p_error VidExt_GL_GetAttribute(m64p_GLattr attr, ref int pValue)
	{
		var attribute = MupenToSDLAttribute(attr);
		if (attribute == (SDL.SDL_GLattr)(-1))
			return m64p_error.INPUT_INVALID;

		pValue = GLAttributes.TryGetValue(attribute, out int value) ? value : _openGLProvider.GLGetAttribute(attribute);

		return m64p_error.SUCCESS;
	}

	[ThreadStatic] 
	private static bool isRenderThread;

	private m64p_error VidExt_GL_SwapBuffers()
	{
		if (_renderMode != m64p_render_mode.OPENGL) return m64p_error.INVALID_STATE;

		if (isRenderThread)
			_openGLProvider.SwapBuffers(_glContext);

		Console.WriteLine("SwapBuffers was called.");

		return m64p_error.SUCCESS;
	}

	private static m64p_error VidExt_SetCaption(string title)
	{
		return m64p_error.UNSUPPORTED;
	}

	private static m64p_error VidExt_ToggleFullScreen()
	{
		return m64p_error.UNSUPPORTED;
	}

	private static m64p_error VidExt_ResizeWindow(int width, int height)
	{
		return m64p_error.UNSUPPORTED;
	}

	private static uint VidExt_GL_GetDefaultFramebuffer()
	{
		return 0;
	}

	private m64p_render_mode _renderMode;

	private m64p_error VidExt_InitWithRenderMode(m64p_render_mode renderMode)
	{
		_renderMode = renderMode;
		return m64p_error.SUCCESS;
	}

	private m64p_error VidExt_VK_GetSurface(ref IntPtr surface, IntPtr instance)
	{
		if (_renderMode != m64p_render_mode.VULKAN)
		{
			return m64p_error.INVALID_STATE;
		}

		ulong surfaceID = _openGLProvider.vulkan(_glContext, instance);

		surface = (IntPtr) surfaceID;

		return m64p_error.SUCCESS;
	}

	private IntPtr[] _vulkanInstanceExtensions;

	private m64p_error VidExt_VK_GetInstanceExtensions(ref IntPtr[] extensions, ref uint numExtensions)
	{
		_vulkanInstanceExtensions = _openGLProvider.GetVulkanInstanceExtensions();
		extensions = _vulkanInstanceExtensions;
		numExtensions = (uint)_vulkanInstanceExtensions.Length;

		return m64p_error.SUCCESS;
	}
}
