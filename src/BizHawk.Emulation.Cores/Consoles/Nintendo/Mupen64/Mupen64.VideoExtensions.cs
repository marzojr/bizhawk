using SDL2;
using static BizHawk.Emulation.Cores.Consoles.Nintendo.Mupen64.Mupen64Api;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Mupen64;

public partial class Mupen64
{
	private m64p_error VidExt_Init()
	{
		return m64p_error.M64ERR_SUCCESS;
	}

	private m64p_error VidExt_Quit()
	{
		return m64p_error.M64ERR_SUCCESS;
	}

	private m64p_error VidExt_ListFullscreenModes(m64p_2d_size[] sizeArray, ref int numSizes)
	{
		return m64p_error.M64ERR_UNSUPPORTED;
	}

	private m64p_error VidExt_ListFullscreenRates(m64p_2d_size size, ref int numRates, int[] rates)
	{
		return m64p_error.M64ERR_UNSUPPORTED;
	}

	private m64p_error VidExt_SetVideoMode(int width, int height, int bitsPerPixel, m64p_video_mode screenMode, m64p_video_flags flags)
	{
		if (_glContext is not null)
			_openGLProvider.ReleaseGLContext(_glContext);
		_glContext = _openGLProvider.RequestGLContext(3, 3, true);

		Console.WriteLine($"Attempted to SetVideoMode, width {width}, height {height}, bpp {bitsPerPixel}, screenMode {screenMode}, flags {flags}");
		return m64p_error.M64ERR_SUCCESS;
	}

	private m64p_error VidExt_SetVideoModeWithRate(int width, int height, int refreshRate, int bitsPerPixel, m64p_video_mode screenMode, m64p_video_flags flags)
	{
		if (_glContext is not null)
			_openGLProvider.ReleaseGLContext(_glContext);
		_glContext ??= _openGLProvider.RequestGLContext(3, 3, true);

		Console.WriteLine($"Attempted to SetVideoModeWithRate, width {width}, height {height}, rate {refreshRate}, bpp {bitsPerPixel}, screenMode {screenMode}, flags {flags}");
		return m64p_error.M64ERR_SUCCESS;
	}

	private IntPtr VidExt_GL_GetProcAddress(string proc)
	{
		return _openGLProvider.GetGLProcAddress(proc);
	}

	private m64p_error VidExt_GL_SetAttribute(m64p_GLattr attr, int value)
	{
		// TODO: either set them and don't reset attributes in context creation, or store important attributes like version here and pass them on context creation
		Console.WriteLine($"attempted to set {attr} to {value}");

		return m64p_error.M64ERR_SUCCESS;
	}

	private m64p_error VidExt_GL_GetAttribute(m64p_GLattr attr, ref int pValue)
	{
		SDL.SDL_GLattr attribute = attr switch
		{
			m64p_GLattr.M64P_GL_DOUBLEBUFFER => SDL.SDL_GLattr.SDL_GL_DOUBLEBUFFER,
			m64p_GLattr.M64P_GL_BUFFER_SIZE => SDL.SDL_GLattr.SDL_GL_BUFFER_SIZE,
			m64p_GLattr.M64P_GL_DEPTH_SIZE => SDL.SDL_GLattr.SDL_GL_DEPTH_SIZE,
			m64p_GLattr.M64P_GL_RED_SIZE => SDL.SDL_GLattr.SDL_GL_RED_SIZE,
			m64p_GLattr.M64P_GL_GREEN_SIZE => SDL.SDL_GLattr.SDL_GL_GREEN_SIZE,
			m64p_GLattr.M64P_GL_BLUE_SIZE => SDL.SDL_GLattr.SDL_GL_BLUE_SIZE,
			m64p_GLattr.M64P_GL_ALPHA_SIZE => SDL.SDL_GLattr.SDL_GL_ALPHA_SIZE,
			m64p_GLattr.M64P_GL_MULTISAMPLEBUFFERS => SDL.SDL_GLattr.SDL_GL_MULTISAMPLEBUFFERS,
			m64p_GLattr.M64P_GL_MULTISAMPLESAMPLES => SDL.SDL_GLattr.SDL_GL_MULTISAMPLESAMPLES,
            m64p_GLattr.M64P_GL_CONTEXT_MAJOR_VERSION => SDL.SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION,
            m64p_GLattr.M64P_GL_CONTEXT_MINOR_VERSION => SDL.SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION,
            m64p_GLattr.M64P_GL_CONTEXT_PROFILE_MASK => SDL.SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK,
			_ => (SDL.SDL_GLattr)(-1)
		};
		if (attribute == (SDL.SDL_GLattr)(-1))
			return m64p_error.M64ERR_INPUT_INVALID;

		pValue = _openGLProvider.GLGetAttribute(attribute);
		return m64p_error.M64ERR_SUCCESS;
	}

	private m64p_error VidExt_GL_SwapBuffers()
	{
		// try
		// {
		// 	_openGLProvider.ActivateGLContext(_glContext);
		// }
		// catch
		// {
		// 	// ignored
		// }
		//
		// _openGLProvider.SwapBuffers(_glContext);

		return m64p_error.M64ERR_SUCCESS;
	}

	private m64p_error VidExt_SetCaption(string title)
	{
		return m64p_error.M64ERR_UNSUPPORTED;
	}

	private m64p_error VidExt_ToggleFullScreen()
	{
		return m64p_error.M64ERR_UNSUPPORTED;
	}

	private m64p_error VidExt_ResizeWindow(int width, int height)
	{
		return m64p_error.M64ERR_UNSUPPORTED;
	}

	private uint VidExt_GL_GetDefaultFramebuffer()
	{
		return 0;
	}

	private m64p_error VidExt_InitWithRenderMode(m64p_render_mode renderMode)
	{
		return renderMode is m64p_render_mode.M64P_RENDER_OPENGL ? m64p_error.M64ERR_SUCCESS : m64p_error.M64ERR_UNSUPPORTED;
	}

	private m64p_error VidExt_VK_GetSurface(ref IntPtr surface, IntPtr instance)
	{
		return m64p_error.M64ERR_UNSUPPORTED;
	}

	private m64p_error VidExt_VK_GetInstanceExtensions(ref IntPtr extensions, ref uint numExtensions)
	{
		return m64p_error.M64ERR_UNSUPPORTED;
	}
}
