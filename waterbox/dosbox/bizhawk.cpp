#include "bizhawk.hpp"
#include "../libco/libco.h"
#include <stdlib.h>
#include <config.h>
#include <sdlmain.h>
#include <render.h>
#include <keyboard.h>
#include <set>
#include <jaffarCommon/file.hpp>

extern int _main(int argc, char* argv[]);
void runMain() { _main(0, nullptr); }
extern void VGA_SetupDrawing(Bitu /*val*/);
cothread_t _emuCoroutine;
cothread_t _driverCoroutine;

#define __FPS__ 60
double ticksElapsed;
uint32_t ticksElapsedInt;
uint32_t _GetTicks() { return ticksElapsedInt; }
jaffarCommon::file::MemoryFileDirectory _memFileDirectory;

std::set<KBD_KEYS> _prevPressedKeys;
extern std::set<KBD_KEYS> _pressedKeys;
extern std::set<KBD_KEYS> _releasedKeys;

bool loadFileIntoMemoryFileDirectory(const std::string& srcFile, const std::string& dstFile, const ssize_t dstSize = -1)
{
		// Loading entire source file
		std::string srcFileData;
		bool        status = jaffarCommon::file::loadStringFromFile(srcFileData, srcFile);
		if (status == false) { fprintf(stderr, "Could not find/read from file: %s\n", srcFile.c_str()); return false; }

		// Uploading file into the mem file directory
		auto f = _memFileDirectory.fopen(dstFile, "w");
		if (f == NULL) { fprintf(stderr, "Could not open mem file for write: %s\n", dstFile.c_str()); return false; }

		// Copying data into mem file
		auto writtenBlocks = jaffarCommon::file::MemoryFile::fwrite(srcFileData.data(), 1, srcFileData.size(), f);
		if (writtenBlocks != srcFileData.size()) 
		{ 
			fprintf(stderr, "Could not write data into mem file: %s\n", dstFile.c_str());
			_memFileDirectory.fclose(f);
		 return false; 
		}

		// If required, resize dst file
		if (dstSize >= 0)
		{
			 auto ret = f->resize(dstSize);
				if (ret < 0) 
				{
					fprintf(stderr, "Could not resize mem file: %s\n", dstFile.c_str());
					_memFileDirectory.fclose(f);
					return false; 
				}
		} 

		// Closing mem file
		_memFileDirectory.fclose(f);

		return true;
}

ECL_EXPORT bool Init(int argc, char **argv)
{
	 // Loading HDD file into mem file directory
		std::string hddSrcFile = "HardDiskDrive";
		std::string hddDstFile = "HardDiskDrive.img";
		size_t hddDstSize = 21411840;
		printf("Creating hard disk drive mem file '%s' -> '%s' (%lu bytes)\n", hddSrcFile.c_str(), hddDstFile.c_str(), hddDstSize);
 	auto result = loadFileIntoMemoryFileDirectory(hddSrcFile, hddDstFile, hddDstSize);
		if (result == false || _memFileDirectory.contains(hddDstFile) == false) 
		{
			fprintf(stderr, "Could not create hard disk drive mem file\n");
			return false; 
		}

		// Setting dummy drivers for env variables
		setenv("SDL_VIDEODRIVER", "dummy", 1);
		setenv("SDL_AUDIODRIVER", "dummy", 1);

		// Setting timer
		ticksElapsed = 0.0;
		ticksElapsedInt = 0;

	printf("Starting DOSBox-x Coroutine...\n");
	_driverCoroutine = co_active();
	constexpr size_t stackSize = 4 * 1024 * 1024;
	_emuCoroutine = co_create(stackSize, runMain);
	co_switch(_emuCoroutine);

	return true;
}

ECL_EXPORT void FrameAdvance(MyFrameInfo* f)
{
 // Processing inputs
	_releasedKeys.clear();
	_pressedKeys.clear();

	std::set<KBD_KEYS> newPressedKeys;
	for (size_t i = 0; i < KEY_COUNT; i++)
	{
		 auto key = (KBD_KEYS)i;
			bool wasPressed = _prevPressedKeys.find(key) != _prevPressedKeys.end();

		 if (f->Keys[i] > 0) 
			{
				if (wasPressed == false)  _pressedKeys.insert(key);
				newPressedKeys.insert(key);
			}  
			if (f->Keys[i] == 0)  if (wasPressed == true)   _releasedKeys.insert(key);
	}
	_prevPressedKeys = newPressedKeys;
 
	// Advancing timer
	constexpr double ticksPerFrame = 1000.0 / __FPS__;
	ticksElapsed += ticksPerFrame; // Miliseconds per frame
	ticksElapsedInt = (uint32_t)std::floor(ticksElapsed);
	// printf("Time Elapsed: %f / %u (delta: %f)\n", ticksElapsed,ticksElapsedInt,ticksPerFrame);

	co_switch(_emuCoroutine);

	// printf("w: %u, h: %u, bytes: %p\n", sdl.surface->w, sdl.surface->h, sdl.surface->pixels);
	f->base.Width = sdl.surface->w;
	f->base.Height = sdl.surface->h;

	// size_t checksum = 0;
	// for (size_t i = 0; i < sdl.surface->w * sdl.surface->h * 4; i++) checksum += ((uint8_t*)sdl.surface->pixels)[i];
	// printf("Video checksum: %lu\n", checksum);
	memcpy(f->base.VideoBuffer, sdl.surface->pixels, f->base.Width * f->base.Height * 4);
}

uint8_t mainRAM[256];

ECL_EXPORT void GetMemoryAreas(MemoryArea *m)
{
	m[0].Data  = mainRAM;
	m[0].Name  = "Main RAM";
	m[0].Size  = 256;
	m[0].Flags = MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_PRIMARY;

}

void (*LEDCallback)();
ECL_EXPORT void SetLEDCallback(void (*callback)())
{
	LEDCallback = callback;
}

void (*InputCallback)();
ECL_EXPORT void SetInputCallback(void (*callback)())
{
	InputCallback = callback;
}
