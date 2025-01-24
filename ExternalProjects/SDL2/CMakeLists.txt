cmake_minimum_required(VERSION 3.13 FATAL_ERROR)

project(SDL2-BizHawk C CXX)

# We only use a few subsystems, don't need to build the entire thing
# Note that the defaults will be "enable if supported"
# So sometimes it is better to simply not specify ON/OFF

# set(SDL_ATOMIC ON) # Used in HIDAPI (and disabling it just makes it emulate atomics rather than use intrinsics)
set(SDL_AUDIO OFF)
set(SDL_VIDEO ON) # Used in mupen64plus + OpenGL/D3D9 display methods
set(SDL_RENDER ON) # We use the software renderer for GDI+ImGui 2D rendering
set(SDL_EVENTS ON) # Implied by SDL_VIDEO and SDL_JOYSTICK
set(SDL_JOYSTICK ON) # Used for our SDL2 input adapter
set(SDL_HAPTIC OFF)
set(SDL_HIDAPI ON) # Not strictly necessary, but it gives more joystick support
set(SDL_POWER OFF)
# set(SDL_THREADS ON) # Used in some joystick backends
# set(SDL_FILE ON) # Used in some joystick code (disabling this doesn't actually do anything on most platforms it seems...)
# set(SDL_LOADSO ON) # Used (and sometimes required) in various subsystems
# set(SDL_CPUINFO OFF) # Probably not actually needed (seems only used in SDL surface code?) but doesn't hurt much to add
set(SDL_FILESYSTEM OFF)
set(SDL_SENSOR OFF)
set(SDL_LOCALE OFF)
set(SDL_MISC OFF)

# We're just building a shared library, so
set(SDL2_DISABLE_SDL2MAIN ON)
set(SDL2_DISABLE_INSTALL ON)
set(SDL2_DISABLE_UNINSTALL ON)

# Let's keep our build friendly to ALL x86-64 CPUs
# set(SDL_SSE3 OFF)

# Actually, SDL2 has a bug with clang-cl that makes SSE3 required
# Annoying, but SSE3 shouldn't really hurt compatibility
# (x86-64 was introduced in 2003, SSE3 was introduced in 2004)

# We want to explicitly dynamically link against VC runtimes
set(SDL_FORCE_STATIC_VCRT OFF)
set(SDL_LIBC ON)

# This is only used for IME stuff, we don't use that
set(SDL_IBUS OFF)

# We only currently support OpenGL (TODO: Do we want OGL ES support?)
set(SDL_OPENGLES OFF)

# Something only useful for audio stuff, don't include it
set(SDL_LIBSAMPLERATE OFF)

# We handle ld path finding with LD_LIBRARY_PATH for Linux
set(SDL_RPATH OFF)

# We only use a small portion of X11 functionality
set(SDL_X11_XCURSOR OFF)
set(SDL_X11_XDBE OFF)
set(SDL_X11_XINPUT OFF)
set(SDL_X11_XFIXES OFF)
set(SDL_X11_XRANDR OFF)
set(SDL_X11_XSCRNSAVER ON)
set(SDL_X11_XSHAPE OFF)

# No Wayland support yet (TODO: Enable this if we ever get Wayland support)
set(SDL_WAYLAND OFF)

# We only need WGL for Windows and GLX for Linux
set(SDL_DIRECTFB OFF)
set(SDL_DUMMYVIDEO OFF)
set(SDL_RPI OFF)
set(SDL_RENDER_D3D OFF)
set(SDL_RENDER_METAL OFF)
set(SDL_VIVANTE OFF)
set(SDL_KMSDRM OFF)
set(SDL_OFFSCREEN OFF)

# libusb gives us support for even more joysticks (notably, the official GC Adapter!)
set(SDL_HIDAPI_LIBUSB ON)

# Windows builds don't normally ship libusb support, we'll do some magic to avoid that
if(WIN32)
	# This is enough to trick SDL to build with libusb support, really!
	set(HAVE_LIBUSB ON)
endif()

# Mark this as a BizHawk build
set(SDL_VENDOR_INFO "BizHawk" CACHE STRING "" FORCE)

# We only want a shared library
set(SDL_SHARED ON)
set(SDL_STATIC OFF)
set(SDL_TEST OFF)

# We need the newer behavior for our changes to go through
set(CMAKE_POLICY_DEFAULT_CMP0077 NEW)

# Silence a warning when specifying INTERPROCEDURAL_OPTIMIZATION
set(CMAKE_POLICY_DEFAULT_CMP0069 NEW)

add_subdirectory(SDL)

if(WIN32)
	# We need to build libusb ourselves and statically link it
	add_library(usb STATIC EXCLUDE_FROM_ALL
		libusb/libusb/core.c
		libusb/libusb/descriptor.c
		libusb/libusb/hotplug.c
		libusb/libusb/io.c
		libusb/libusb/strerror.c
		libusb/libusb/sync.c
		libusb/libusb/os/threads_windows.c
		libusb/libusb/os/windows_common.c
		libusb/libusb/os/windows_usbdk.c
		libusb/libusb/os/windows_winusb.c
		libusb/libusb/os/events_windows.c
	)

	# Silence warnings with clang-cl
	if(CMAKE_C_COMPILER_ID MATCHES "Clang")
		target_compile_options(usb PRIVATE -w)
	endif()

	set_target_properties(usb PROPERTIES VERSION 1.0.27)
	target_include_directories(usb BEFORE
		PUBLIC libusb/libusb
		PRIVATE libusb/msvc
	)

	target_link_libraries(SDL2 PRIVATE usb)
endif()

# ensure we have SDL2.dll / libSDL2.so
set_target_properties(SDL2 PROPERTIES
	NO_SONAME ON
	OUTPUT_NAME "SDL2"
)

# Copy output to our dll folders
# This requires a bit of magic (add_custom_command is not allowed for targets made with add_subdirectory)
add_custom_target(SDL2-BizHawk ALL)
add_dependencies(SDL2-BizHawk SDL2)
add_custom_command(
	TARGET SDL2-BizHawk
	POST_BUILD
	COMMAND ${CMAKE_COMMAND}
	ARGS -E copy $<TARGET_FILE:SDL2> ${CMAKE_SOURCE_DIR}/../../Assets/dll
	COMMAND ${CMAKE_COMMAND}
	ARGS -E copy $<TARGET_FILE:SDL2> ${CMAKE_SOURCE_DIR}/../../output/dll
)
