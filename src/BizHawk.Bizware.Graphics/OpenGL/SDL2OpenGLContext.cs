#define DEBUG_OPENGL

#if DEBUG_OPENGL
using System.Runtime.InteropServices;
#endif

using Silk.NET.OpenGL;

using BizHawk.Common;
using BizHawk.Common.StringExtensions;

using static SDL2.SDL;

#pragma warning disable BHI1007 // target-typed Exception TODO don't

namespace BizHawk.Bizware.Graphics
{
	/// <summary>
	/// Wraps an SDL2 OpenGL context
	/// </summary>
	public class SDL2OpenGLContext : IDisposable
	{
		static SDL2OpenGLContext()
		{
			if (OSTailoredCode.IsUnixHost)
			{
				// make sure that Linux uses the x11 video driver
				// we need this as mono winforms uses x11
				// and the user could potentially try to force the wayland video driver via env vars
				SDL_SetHintWithPriority("SDL_VIDEODRIVER", "x11", SDL_HintPriority.SDL_HINT_OVERRIDE);
				// try to use EGL if it is available
				// GLX is the old API, and is the more or less "deprecated" at this point, and potentially more buggy with some drivers
				// we do need to a bit more work, in case EGL is not actually available or potentially doesn't have desktop GL support
				SDL_SetHint(SDL_HINT_VIDEO_X11_FORCE_EGL, "1");
			}

			// init SDL video
			if (SDL_Init(SDL_INIT_VIDEO) != 0)
			{
				throw new($"Could not init SDL video! SDL Error: {SDL_GetError()}");
			}

			if (SDL_Vulkan_LoadLibrary(null) != 0)
			{
				throw new Exception($"SDL_Vulkan_LoadLibrary failed: {SDL_GetError()}");
			}

			if (OSTailoredCode.IsUnixHost)
			{
				// if we fail to load EGL, we'll just try again with GLX...
				var loadGlx = SDL_GL_LoadLibrary(null) != 0;

				if (!loadGlx)
				{
					try
					{
						// check if we can actually create a desktop GL context
						using var glContext = new SDL2OpenGLContext(3, 2, true);
						using var gl = GL.GetApi(GetGLProcAddress);
						var versionString = gl.GetStringS(StringName.Version);
						if (versionString.ContainsOrdinal("OpenGL ES"))
						{
							// driver ended up creating a GL ES context regardless
							// hopefully GLX works here
							loadGlx = true;
						}
						else
						{
							// check if we did in fact get at least GL 3.2
							var versionParts = versionString!.Split('.');
							var major = int.Parse(versionParts[0]);
							var minor = int.Parse(versionParts[1][0].ToString());
							loadGlx = major * 10 + minor < 32;
						}
					}
					catch
					{
						// failed to create a context, fallback to GLX
						loadGlx = true;
					}
				}

				if (loadGlx)
				{
					SDL_GL_UnloadLibrary();
					SDL_SetHint(SDL_HINT_VIDEO_X11_FORCE_EGL, "0");
					if (SDL_GL_LoadLibrary(null) != 0)
					{
						throw new($"Could not load default OpenGL library! SDL Error: {SDL_GetError()}");
					}
				}
			}
			else
			{
				// load the default OpenGL library
				if (SDL_GL_LoadLibrary(null) != 0)
				{
					throw new($"Could not load default OpenGL library! SDL Error: {SDL_GetError()}");
				}
			}

			// we will be turning a foreign window into an SDL window
			// we need this so it knows that it is capable of using OpenGL functions
			SDL_SetHintWithPriority(SDL_HINT_VIDEO_FOREIGN_WINDOW_OPENGL, "1", SDL_HintPriority.SDL_HINT_OVERRIDE);
			// don't allow windows events to be pumped
			// it's not needed and can be dangerous in some rare cases
			SDL_SetHintWithPriority(SDL_HINT_WINDOWS_ENABLE_MESSAGELOOP, "0", SDL_HintPriority.SDL_HINT_OVERRIDE);
		}

#if DEBUG_OPENGL
		private static readonly DebugProc _debugProc = DebugCallback;
		private static void DebugCallback(GLEnum source, GLEnum type, int id, GLEnum severity, int length, IntPtr message, IntPtr userParam)
			=> Console.WriteLine($"{source} {type} {severity}: {Marshal.PtrToStringAnsi(message, length)}");
#endif

		private IntPtr _sdlWindow;
		private IntPtr _glContext;

		private static void SetAttributes(int majorVersion, int minorVersion, bool coreProfile, bool shareContext)
		{
			// set some sensible defaults
			SDL_GL_ResetAttributes();
			if (SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_RED_SIZE, 8) is not 0
				|| SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_GREEN_SIZE, 8) is not 0
				|| SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_BLUE_SIZE, 8) is not 0
				|| SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_ALPHA_SIZE, 0) is not 0
				|| SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_DOUBLEBUFFER, 1) is not 0)
			{
				throw new($"Could not set GL attributes! SDL Error: {SDL_GetError()}");
			}

			if (SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, majorVersion) != 0)
			{
				throw new($"Could not set GL Major Version! SDL Error: {SDL_GetError()}");
			}
			
			if (SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, minorVersion) != 0)
			{
				throw new($"Could not set GL Minor Version! SDL Error: {SDL_GetError()}");
			}

#if DEBUG_OPENGL
			if (SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_FLAGS, (int)SDL_GLcontext.SDL_GL_CONTEXT_DEBUG_FLAG) != 0)
			{
				throw new($"Could not set GL Debug Flag! SDL Error: {SDL_GetError()}");
			}
#endif

			var profile = coreProfile
				? SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_CORE
				: SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_COMPATIBILITY;

			if (SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, profile) != 0)
			{
				throw new($"Could not set GL profile! SDL Error: {SDL_GetError()}");
			}

			if (SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_SHARE_WITH_CURRENT_CONTEXT, shareContext ? 1 : 0) != 0)
			{
				throw new($"Could not set share context attribute! SDL Error: {SDL_GetError()}");
			}
		}

		private void CreateContext()
		{
			_glContext = SDL_GL_CreateContext(_sdlWindow);
			if (_glContext == IntPtr.Zero)
			{
				throw new($"Could not create GL Context! SDL Error: {SDL_GetError()}");
			}

#if DEBUG_OPENGL
			if (GetGLProcAddress("glDebugMessageCallback") != IntPtr.Zero)
			{
				using var gl = GL.GetApi(GetGLProcAddress);
				unsafe
				{
					gl.DebugMessageCallback(_debugProc, null);
				}
			}
#endif
		}

		public SDL2OpenGLContext(IntPtr nativeWindowhandle, int majorVersion, int minorVersion, bool coreProfile)
		{
			// Controls are not shared, they are the sharees
			SetAttributes(majorVersion, minorVersion, coreProfile, shareContext: false);

			_sdlWindow = SDL_CreateWindowFrom(nativeWindowhandle);
			if (_sdlWindow == IntPtr.Zero)
			{
				throw new($"Could not create SDL Window! SDL Error: {SDL_GetError()}");
			}

			try
			{
				CreateContext();
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		public SDL2OpenGLContext(int majorVersion, int minorVersion, bool coreProfile, int width=1, int height=1, bool vulkan=false)
		{
			// offscreen contexts are shared (as we want to send texture from it over to our control's context)
			// make sure to set the current graphics control context before creating this context
			SetAttributes(majorVersion, minorVersion, coreProfile, shareContext: true);

			SDL_WindowFlags flags = SDL_WindowFlags.SDL_WINDOW_HIDDEN;
			if (vulkan)
				flags |= SDL_WindowFlags.SDL_WINDOW_VULKAN;
			else
				flags |= SDL_WindowFlags.SDL_WINDOW_OPENGL;

			_sdlWindow = SDL_CreateWindow(null, SDL_WINDOWPOS_UNDEFINED, SDL_WINDOWPOS_UNDEFINED, width, height, flags);
			if (_sdlWindow == IntPtr.Zero)
			{
				throw new($"Could not create SDL Window! SDL Error: {SDL_GetError()}");
			}

			if (vulkan) return;

			try
			{
				CreateContext();
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		public void Dispose()
		{
			if (_glContext != IntPtr.Zero)
			{
				SDL_GL_DeleteContext(_glContext);
				_glContext = IntPtr.Zero;
			}

			if (_sdlWindow != IntPtr.Zero)
			{
				SDL_DestroyWindow(_sdlWindow);
				_sdlWindow = IntPtr.Zero;
			}
		}

		public bool IsCurrent => SDL_GL_GetCurrentContext() == _glContext;

		public void MakeContextCurrent()
		{
			// no-op if already current
			if (SDL_GL_MakeCurrent(_sdlWindow, _glContext) != 0)
			{
				throw new($"Failed to set context to current! SDL error: {SDL_GetError()}");
			}
		}

		public static void MakeNoneCurrent()
		{
			// no-op if nothing is current
			if (SDL_GL_MakeCurrent(IntPtr.Zero, IntPtr.Zero) != 0)
			{
				throw new($"Failed to clear current context! SDL error: {SDL_GetError()}");
			}
		}

		public static IntPtr GetGLProcAddress(string proc)
			=> SDL_GL_GetProcAddress(proc);

		public void SetVsync(bool state)
		{
			if (!IsCurrent)
			{
				throw new InvalidOperationException("Tried to set Vsync on non-active context");
			}

			_ = SDL_GL_SetSwapInterval(state ? 1 : 0);
		}

		public void SwapBuffers()
		{
			if (!IsCurrent)
			{
				throw new InvalidOperationException("Tried to swap buffers on non-active context");
			}

			SDL_GL_SwapWindow(_sdlWindow);
		}

		public ulong CreateVulkanSurface(IntPtr instance)
		{
			if (SDL_Vulkan_CreateSurface(_sdlWindow, instance, out ulong surface) != SDL_bool.SDL_TRUE)
			{
				throw new InvalidOperationException($"Failed to create vulkan surface! SDL error: {SDL_GetError()}");
			}

			return surface;
		}

		public static IntPtr[] GetVulkanInstanceExtensions()
		{
			if (SDL_Vulkan_GetInstanceExtensions(IntPtr.Zero, out uint count, IntPtr.Zero) != SDL_bool.SDL_TRUE)
			{
				throw new InvalidOperationException($"SDL_Vulkan_GetInstanceExtensions failed: {SDL_GetError()}");
			}

			var vulkanInstanceExtensions = new IntPtr[count];

			if (SDL_Vulkan_GetInstanceExtensions(IntPtr.Zero, pCount: out count, vulkanInstanceExtensions) != SDL_bool.SDL_TRUE)
			{
				throw new InvalidOperationException($"SDL_Vulkan_GetInstanceExtensions failed: {SDL_GetError()}");
			}

			return vulkanInstanceExtensions;
		}
	}
}
