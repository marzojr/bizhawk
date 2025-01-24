using BizHawk.Bizware.Graphics;
using BizHawk.Emulation.Common;
using SDL2;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// Provides a way for a core to use OpenGL
	/// </summary>
	public class OpenGLProvider : IOpenGLProvider
	{
		public bool SupportsGLVersion(int major, int minor)
			=> OpenGLVersion.SupportsVersion(major, minor);

		public object RequestGLContext(int major, int minor, bool coreProfile, int width=1, int height=1, bool vulkan=false)
		{
			var ret = new SDL2OpenGLContext(major, minor, coreProfile, width, height, vulkan);
			if (!vulkan) ret.SetVsync(false);
			return ret;
		}

		public void ReleaseGLContext(object context)
			=> ((SDL2OpenGLContext)context).Dispose();

		public void ActivateGLContext(object context)
			=> ((SDL2OpenGLContext)context).MakeContextCurrent();

		public void DeactivateGLContext()
			=> SDL2OpenGLContext.MakeNoneCurrent();

		public IntPtr GetGLProcAddress(string proc)
			=> SDL2OpenGLContext.GetGLProcAddress(proc);

		public int GLGetAttribute(SDL.SDL_GLattr attribute)
		{
			_ = SDL.SDL_GL_GetAttribute(attribute, out int value);
			return value;
		}

		public ulong vulkan(object context, IntPtr instance)
		{
			return ((SDL2OpenGLContext) context).CreateVulkanSurface(instance);
		}

		public IntPtr[] GetVulkanInstanceExtensions()
		{
			return SDL2OpenGLContext.GetVulkanInstanceExtensions();
		}

		public void SwapBuffers(object context)
		{
			((SDL2OpenGLContext)context).SwapBuffers();
		}
	}
}
