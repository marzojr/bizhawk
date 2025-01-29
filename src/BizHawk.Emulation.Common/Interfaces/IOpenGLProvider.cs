using SDL2;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// Defines an interface for cores to obtain OpenGL contexts and functions
	/// </summary>
	public interface IOpenGLProvider
	{
		/// <summary>
		/// Checks if specified OpenGL version is supported
		/// The current context will be preserved
		/// </summary>
		public bool SupportsGLVersion(int major, int minor);

		/// <summary>
		/// Requests an OpenGL context with specified major / minor version
		/// The core profile can be requested (otherwise, the compatibility profile will be used)
		/// The requested OpenGL context will be shared with the current context
		/// Note: creating a context implicitly makes that created context current
		/// </summary>
		public object RequestGLContext(int major, int minor, bool coreProfile, int width=1, int height=1);

		/// <summary>
		/// Frees this OpenGL or Vulkan context
		/// </summary>
		public void ReleaseContext(object context);

		/// <summary>
		/// Sets this OpenGL context to current
		/// </summary>
		public void ActivateGLContext(object context);

		/// <summary>
		/// Deactivates the current OpenGL context
		/// No context will be current after this call
		/// </summary>
		public void DeactivateGLContext();

		/// <summary>
		/// Gets an OpenGL function pointer
		/// The user must make a context active before using this
		/// </summary>
		public IntPtr GetGLProcAddress(string proc);

		/// <summary>
		/// Gets the current value for <paramref name="attribute"/> in the current OpenGL context
		/// </summary>
		/// <param name="attribute">The attribute to check</param>
		public int GLGetAttribute(SDL.SDL_GLattr attribute);

		public void SwapBuffers(object context);

		public object RequestVulkanContext(int width, int height);

		public ulong CreateVulkanSurface(object context, IntPtr instance);

		public IntPtr[] GetVulkanInstanceExtensions();
	}
}
