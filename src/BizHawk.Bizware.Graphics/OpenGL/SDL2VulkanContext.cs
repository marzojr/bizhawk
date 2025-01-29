using static SDL2.SDL;

namespace BizHawk.Bizware.Graphics;

public class SDL2VulkanContext : IDisposable
{
	private IntPtr _sdlWindow;

	static SDL2VulkanContext()
	{
		if (SDL_Vulkan_LoadLibrary(null) != 0)
		{
			throw new Exception($"SDL_Vulkan_LoadLibrary failed: {SDL_GetError()}");
		}
	}

	public SDL2VulkanContext(int width, int height)
	{
		_sdlWindow = SDL_CreateWindow(null, SDL_WINDOWPOS_UNDEFINED, SDL_WINDOWPOS_UNDEFINED, width, height,
			SDL_WindowFlags.SDL_WINDOW_HIDDEN | SDL_WindowFlags.SDL_WINDOW_VULKAN);
		if (_sdlWindow == IntPtr.Zero)
		{
			throw new Exception($"Could not create SDL Window! SDL Error: {SDL_GetError()}");
		}
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

	public void Dispose()
	{
		if (_sdlWindow != IntPtr.Zero)
		{
			SDL_DestroyWindow(_sdlWindow);
			_sdlWindow = IntPtr.Zero;
		}
	}
}
