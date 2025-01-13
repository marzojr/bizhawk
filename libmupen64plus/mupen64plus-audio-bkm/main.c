/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
 *   Mupen64plus-bkm-audio - main.c                                        *
 *   Mupen64Plus homepage: http://code.google.com/p/mupen64plus/           *
 *   Editet        2013 null_ptr Completely rewritten to suit custom needs *
 *   Copyright (C) 2007-2009 Richard Goedeken                              *
 *   Copyright (C) 2007-2008 Ebenblues                                     *
 *   Copyright (C) 2003 JttL                                               *
 *   Copyright (C) 2002 Hacktarux                                          *
 *                                                                         *
 *   This program is free software; you can redistribute it and/or modify  *
 *   it under the terms of the GNU General Public License as published by  *
 *   the Free Software Foundation; either version 2 of the License, or     *
 *   (at your option) any later version.                                   *
 *                                                                         *
 *   This program is distributed in the hope that it will be useful,       *
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 *   GNU General Public License for more details.                          *
 *                                                                         *
 *   You should have received a copy of the GNU General Public License     *
 *   along with this program; if not, write to the                         *
 *   Free Software Foundation, Inc.,                                       *
 *   51 Franklin Street, Fifth Floor, Boston, MA 02110-1301, USA.          *
 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stdarg.h>

#define M64P_PLUGIN_PROTOTYPES 1
#include "m64p_types.h"
#include "m64p_plugin.h"
#include "m64p_common.h"
#include "m64p_config.h"

#include "main.h"
#include "osal_dynamiclib.h"

#ifdef max
#undef max
#endif
#define max(a, b) __extension__ ({ \
    __typeof__(a) _a = (a); \
    __typeof__(b) _b = (b); \
    _a > _b ? _a : _b; \
})

#ifdef min
#undef min
#endif
#define min(a, b) __extension__ ({ \
    __typeof__(a) _a = (a); \
    __typeof__(b) _b = (b); \
    _a < _b ? _a : _b; \
})

/* This sets default frequency what is used if rom doesn't want to change it.
   Probably only game that needs this is Zelda: Ocarina Of Time Master Quest 
   *NOTICE* We should try to find out why Demos' frequencies are always wrong
   They tend to rely on a default frequency, apparently, never the same one ;)*/
#define DEFAULT_FREQUENCY 33600
#define DEFAULT_BUFFER_SIZE 12480

/* number of bytes per sample */
#define N64_SAMPLE_BYTES 4

/* local variables */
static void (*l_DebugCallback)(void *, int, const char *) = NULL;
static void *l_DebugCallContext = NULL;
static int l_PluginInit = 0;

/* Read header for type definition */
static AUDIO_INFO AudioInfo;
/* Audio frequency, this is usually obtained from the game, but for compatibility we set default value */
static int GameFreq = DEFAULT_FREQUENCY;

/* Audio buffer */
static char* audioBuffer = NULL;
static size_t bufferBack = 0;
static size_t bufferSize = 0;

// Prototype of local functions
static void SetSamplingRate(int freq);
static void SetBufferSize(size_t size);

static int critical_failure = 0;

/* Global functions */
static void DebugMessage(int level, const char *message, ...)
{
  char msgbuf[1024];
  va_list args;

  if (l_DebugCallback == NULL)
      return;

  va_start(args, message);
  vsprintf(msgbuf, message, args);

  (*l_DebugCallback)(l_DebugCallContext, level, msgbuf);

  va_end(args);
}

#pragma region (De-)Initialization
/* Mupen64Plus plugin functions */
EXPORT m64p_error CALL PluginStartup(m64p_dynlib_handle CoreLibHandle, void *Context,
                                   void (*DebugCallback)(void *, int, const char *))
{
    ptr_CoreGetAPIVersions CoreAPIVersionFunc;
    
    int ConfigAPIVersion, DebugAPIVersion, VidextAPIVersion;
    
    if (l_PluginInit)
        return M64ERR_ALREADY_INIT;

    /* first thing is to set the callback function for debug info */
    l_DebugCallback = DebugCallback;
    l_DebugCallContext = Context;

    /* attach and call the CoreGetAPIVersions function, check Config API version for compatibility */
    CoreAPIVersionFunc = (ptr_CoreGetAPIVersions) osal_dynlib_getproc(CoreLibHandle, "CoreGetAPIVersions");
    if (CoreAPIVersionFunc == NULL)
    {
        DebugMessage(M64MSG_ERROR, "Core emulator broken; no CoreAPIVersionFunc() function found.");
        return M64ERR_INCOMPATIBLE;
    }
    
    (*CoreAPIVersionFunc)(&ConfigAPIVersion, &DebugAPIVersion, &VidextAPIVersion, NULL);
    if ((ConfigAPIVersion & 0xffff0000) != (CONFIG_API_VERSION & 0xffff0000))
    {
        DebugMessage(M64MSG_ERROR, "Emulator core Config API (v%i.%i.%i) incompatible with plugin (v%i.%i.%i)",
                VERSION_PRINTF_SPLIT(ConfigAPIVersion), VERSION_PRINTF_SPLIT(CONFIG_API_VERSION));
        return M64ERR_INCOMPATIBLE;
    }

    l_PluginInit = 1;
    return M64ERR_SUCCESS;
}

EXPORT m64p_error CALL PluginShutdown(void)
{
    if (!l_PluginInit)
        return M64ERR_NOT_INIT;

    /* reset some local variables */
    l_DebugCallback = NULL;
    l_DebugCallContext = NULL;

    l_PluginInit = 0;
    return M64ERR_SUCCESS;
}

EXPORT int CALL RomOpen(void)
{
    if (!l_PluginInit)
        return 0;

    SetSamplingRate(GameFreq);
	SetBufferSize(DEFAULT_BUFFER_SIZE);
    return 1;
}

EXPORT void CALL RomClosed( void )
{
    if (!l_PluginInit)
        return;
   if (critical_failure == 1)
       return;
    
    // Delete the buffer, as we are done producing sound
    if (audioBuffer != NULL)
    {
		bufferSize = 0;
		bufferBack = 0;
		free(audioBuffer);
		audioBuffer = NULL;
    }
}

EXPORT int CALL InitiateAudio( AUDIO_INFO Audio_Info )
{
    if (!l_PluginInit)
        return 0;

    AudioInfo = Audio_Info;
    return 1;
}

static void SetBufferSize(size_t size)
{
	char* oldBuffer = audioBuffer;
	audioBuffer = (char*)malloc(size);
	if(audioBuffer != NULL)
	{
		memcpy(audioBuffer, oldBuffer, min(size, bufferBack));
		free(oldBuffer);
	}
	bufferSize = size;
	bufferBack = min(size, bufferBack);
}
#pragma endregion

#pragma region Pluginversion
EXPORT m64p_error CALL PluginGetVersion(m64p_plugin_type *PluginType, int *PluginVersion, int *APIVersion, const char **PluginNamePtr, int *Capabilities)
{
    /* set version info */
    if (PluginType != NULL)
        *PluginType = M64PLUGIN_AUDIO;

    if (PluginVersion != NULL)
        *PluginVersion = BKM_AUDIO_PLUGIN_VERSION;

    if (APIVersion != NULL)
        *APIVersion = AUDIO_PLUGIN_API_VERSION;
    
    if (PluginNamePtr != NULL)
        *PluginNamePtr = "Mupen64Plus BKM Audio Plugin for Bizhawk";

    if (Capabilities != NULL)
    {
        *Capabilities = 0;
    }

    return M64ERR_SUCCESS;
}
#pragma endregion

#pragma region Handle audio
/* --- Called when sampling rate changes --- */
EXPORT void CALL AiDacrateChanged( int SystemType )
{
    int f = GameFreq;

    if (!l_PluginInit)
        return;

	if (*AudioInfo.AI_DACRATE_REG == 0)
	{
		f = DEFAULT_FREQUENCY;
	}
	else
	{
		switch (SystemType)
		{
			case SYSTEM_NTSC:
				f = 48681812 / (*AudioInfo.AI_DACRATE_REG + 1);
				break;
			case SYSTEM_PAL:
				f = 49656530 / (*AudioInfo.AI_DACRATE_REG + 1);
				break;
			case SYSTEM_MPAL:
				f = 48628316 / (*AudioInfo.AI_DACRATE_REG + 1);
				break;
		}
	}
    SetSamplingRate(f);
}

/* --- Called when length of n64 audio buffer changes --- */
/* --- i.e. new audio data in buffer --- */
EXPORT void CALL AiLenChanged( void )
{
    unsigned int LenReg;
    unsigned char *p;

    if (critical_failure == 1)
        return;
    if (!l_PluginInit)
        return;

    LenReg = *AudioInfo.AI_LEN_REG;
    p = AudioInfo.RDRAM + (*AudioInfo.AI_DRAM_ADDR_REG & 0xFFFFFF);

	if (bufferBack + LenReg < bufferSize)
    {
        unsigned int i;

        for ( i = 0 ; i < LenReg ; i += 4 )
        {
            // Left channel
			audioBuffer[ bufferBack + i ] = p[ i + 2 ];
            audioBuffer[ bufferBack + i + 1 ] = p[ i + 3 ];

            // Right channel
            audioBuffer[ bufferBack + i + 2 ] = p[ i ];
            audioBuffer[ bufferBack + i + 3 ] = p[ i + 1 ];
        }
        bufferBack += i;

    }
    else
    {
        DebugMessage(M64MSG_WARNING, "AiLenChanged(): Audio buffer overflow.");
    }
}

static void SetSamplingRate(int freq)
{
    GameFreq = freq; // This is important for the sync
}
#pragma endregion

#pragma region Unused methods
/* ----------------------------------------------------------------------
 * ------------ STUBS. WE  DO NOT USE THESE API FUNCTIONS ---------------
 * -------- MUPEN EXPECTS THEM AND FAILS IF THEY DON'T EXIST ------------
 * ---------------------------------------------------------------------- */
EXPORT void CALL ProcessAList(void)
{
}

EXPORT void CALL SetSpeedFactor(int percentage)
{
}

static int VolumeGetUnmutedLevel(void)
{
	return 100;
}

EXPORT void CALL VolumeMute(void)
{
}

EXPORT void CALL VolumeUp(void)
{
}

EXPORT void CALL VolumeDown(void)
{
}

EXPORT int CALL VolumeGetLevel(void)
{
    return 100;
}

EXPORT void CALL VolumeSetLevel(int level)
{
}

EXPORT const char * CALL VolumeGetString(void)
{
	return "100%";
}
/* ----------------------------------------------------------------------
 * --------------------------- STUBS END --------------------------------
 * ---------------------------------------------------------------------- */
#pragma endregion

#pragma region Buffer export
/* --- Moves content of audio buffer to destination --- */
EXPORT void CALL ReadAudioBuffer(short* dest)
{
	memcpy(dest, audioBuffer, bufferBack);
	bufferBack = 0;
}
/* --- Returns number of shorts of internal data --- */
EXPORT int CALL GetBufferSize()
{
	return max(bufferBack/2, 0);
}

/* --- Returns current sampling rate --- */
EXPORT int CALL GetAudioRate()
{
	return GameFreq;
}
#pragma endregion