/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
 *   Mupen64plus-input-bkm - plugin.c                                      *
 *   Mupen64Plus homepage: http://code.google.com/p/mupen64plus/           *
 *   Edited        2014 null_ptr                                           *
 *   Copyright (C) 2008-2011 Richard Goedeken                              *
 *   Copyright (C) 2008 Tillin9                                            *
 *   Copyright (C) 2002 Blight                                             *
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
#include <api/m64p_types.h>
#include <api/m64p_plugin.h>
#include <api/m64p_common.h>

#include "plugin.h"
#include "version.h"
#include <osal/dynamiclib.h>

#include <errno.h>

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

/* global data definitions */
SController controller[4];   // 4 controllers

/* static data definitions */
static void (*l_DebugCallback)(void *, int, const char *) = NULL;
static void *l_DebugCallContext = NULL;
static int l_PluginInit = 0;

/* Callbacks for data flow out of mupen */
static int (*l_inputCallback)(int i) = NULL;
static void (*l_setrumbleCallback)(int i, int on) = NULL;

static int romopen = 0;         // is a rom opened

/* Global functions */
void DebugMessage(int level, const char *message, ...)
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

    /* reset controllers */
    memset(controller, 0, sizeof(controller));

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
	memset(controller, 0, sizeof(controller));
    return M64ERR_SUCCESS;
}

/******************************************************************
  Function: InitiateControllers
  Purpose:  This function initialises how each of the controllers
            should be handled.
  input:    - The handle to the main window.
            - A controller structure that needs to be filled for
              the emulator to know how to handle each controller.
  output:   none
*******************************************************************/
EXPORT void CALL InitiateControllers(CONTROL_INFO ControlInfo)
{
    int i;
	memset( controller, 0, sizeof(controller) );

    for( i = 0; i < 4; i++ )
    {
		controller[i].control = ControlInfo.Controls + i;
		controller[i].control->Plugin = PLUGIN_MEMPAK;
		controller[i].control->Present = 1;
		controller[i].control->RawData = 0;
    }

    DebugMessage(M64MSG_INFO, "%s version %i.%i.%i initialized.", PLUGIN_NAME, VERSION_PRINTF_SPLIT(PLUGIN_VERSION));
}

/******************************************************************
  Function: RomClosed
  Purpose:  This function is called when a rom is closed.
  input:    none
  output:   none
*******************************************************************/
EXPORT void CALL RomClosed(void)
{
    romopen = 0;
}

/******************************************************************
  Function: RomOpen
  Purpose:  This function is called when a rom is open. (from the
            emulation thread)
  input:    none
  output:   none
*******************************************************************/
EXPORT int CALL RomOpen(void)
{
    romopen = 1;
    return 1;
}

#pragma endregion

EXPORT m64p_error CALL PluginGetVersion(m64p_plugin_type *PluginType, int *PluginVersion, int *APIVersion, const char **PluginNamePtr, int *Capabilities)
{
    /* set version info */
    if (PluginType != NULL)
        *PluginType = M64PLUGIN_INPUT;

    if (PluginVersion != NULL)
        *PluginVersion = PLUGIN_VERSION;

    if (APIVersion != NULL)
        *APIVersion = INPUT_PLUGIN_API_VERSION;

    if (PluginNamePtr != NULL)
        *PluginNamePtr = PLUGIN_NAME;

    if (Capabilities != NULL)
    {
        *Capabilities = 0;
    }

    return M64ERR_SUCCESS;
}

#pragma region Raw read and write

/* ----------------------------------------------------------------------
-------------------------- Controller CRC ----------------------------
---------------------------------------------------------------------- */
static unsigned char DataCRC( unsigned char *Data, int iLenght )
{
	unsigned char Remainder = Data[0];

	int iByte = 1;
	unsigned char bBit = 0;

	while( iByte <= iLenght )
	{
		int HighBit = ((Remainder & 0x80) != 0);
		Remainder = Remainder << 1;

		Remainder += ( iByte < iLenght && Data[iByte] & (0x80 >> bBit )) ? 1 : 0;

		Remainder ^= (HighBit) ? 0x85 : 0;

		bBit++;
		iByte += bBit/8;
		bBit %= 8;
	}

	return Remainder;
}

/******************************************************************
  Function: ControllerCommand
  Purpose:  To process the raw data that has just been sent to a
            specific controller.
  input:    - Controller Number (0 to 3) and -1 signalling end of
              processing the pif ram.
            - Pointer of data to be processed.
  output:   none

  note:     This function is only needed if the DLL is allowing raw
            data, or the plugin is set to raw

            the data that is being processed looks like this:
            initilize controller: 01 03 00 FF FF FF
            read controller:      01 04 01 FF FF FF FF
*******************************************************************/
EXPORT void CALL ControllerCommand(int Control, unsigned char *Command)
{
	if( Control == -1)
		return;
}

/* ----------------------------------------------------------------------
   ----------- Handles raw data read from a rumble pak      -------------
   ----------- Data read at address C01B is 32x the status  -------------
   ----------- Data read at address 8001 is 32x 0x80        -------------
   ---------------------------------------------------------------------- */
void DoRumblePakRead(int Control, unsigned char* Command)
{
	if(Command[3] == 0x80 && Command[4] == 0x01)
		memset(Command+5, 0x80, 32);
	else if(Command[3] == 0xC0 && Command[4] == 0x1B)
		memset(Command+5, controller[Control].rumbling, 32);
	else
		memset(Command+5, 0x00, 32);
}

/* ----------------------------------------------------------------------
   ----------- Writes data to rumble pak                    -------------
   ----------- If writes 0x01 to address 0xC01B rumble on   -------------
   ----------- If writes 0x00 to address 0xC01B rumble off  -------------
   ---------------------------------------------------------------------- */
void DoRumblePakWrite(int Control, unsigned char* Command)
{
	if(Command[3] == 0xC0 && Command[4] == 0x1B)
	{
		controller[Control].rumbling = Command[5];
		if(l_setrumbleCallback != NULL)
			l_setrumbleCallback(Control, Command[5]);
	}
}

/* ----------------------------------------------------------------------
   ----------- Does a raw read of a controller pak          -------------
   ----------- Currently only handles rumble paks           -------------
   ---------------------------------------------------------------------- */
void DoRawRead(int Control, unsigned char* Command)
{
	switch(controller[Control].control->Plugin)
	{
	case PLUGIN_RUMBLE_PAK:
		DoRumblePakRead(Control, Command);
		break;
	case PLUGIN_NONE:
	case PLUGIN_RAW:
	case PLUGIN_MEMPAK:
	case PLUGIN_TRANSFER_PAK:
	default:
		break;
	}
}

/* ----------------------------------------------------------------------
   ----------- Handles raw write to the controller pak      -------------
   ----------- Currently only handles rumble paks           -------------
   ---------------------------------------------------------------------- */
void DoRawWrite(int Control, unsigned char* Command)
{
	switch(controller[Control].control->Plugin)
	{
	case PLUGIN_RUMBLE_PAK:
		DoRumblePakWrite(Control, Command);
		break;
	case PLUGIN_NONE:
	case PLUGIN_RAW:
	case PLUGIN_MEMPAK:
	case PLUGIN_TRANSFER_PAK:
	default:
		break;
	}
}

/******************************************************************
  Function: ReadController
  Purpose:  To process the raw data in the pif ram that is about to
            be read.
  input:    - Controller Number (0 to 3) and -1 signalling end of
              processing the pif ram.
            - Pointer of data to be processed.
  output:   none
  note:     This function is only needed if the DLL is allowing raw
            data.
*******************************************************************/
EXPORT void CALL ReadController(int Control, unsigned char *Command)
{
	unsigned char * Data = Command + 5;
	int value;
	if(Control == -1)
		return;

	switch(Command[2])
	{
	case RD_RESETCONTROLLER:
	case RD_GETSTATUS:
		Command[3] = RD_GAMEPAD | RD_ABSOLUTE;
		Command[4] = RD_NOEEPROM;
		Command[5] = controller[Control].control->Plugin != PLUGIN_NONE;
		break;
	case RD_READKEYS:
		value = l_inputCallback(Control);
		*((int*)(Command+3)) = value;
		break;
	case RD_READPAK:
		DoRawRead(Control, Command);
		Data[32] = DataCRC(Data, 32);
		break;
	case RD_WRITEPAK:
		DoRawWrite(Control, Command);
		Data[32] = DataCRC(Data, 32);
		break;
	case RD_READEEPROM:
	case RD_WRITEEPROM:
	default:
		break;
	}
}

#pragma endregion

#pragma region Useless stubs

/* ----------------------------------------------------------------------
   -------------------- This functions are not used  --------------------
   -------------------- Plugin api just expects them --------------------
   -------------------- to exist                     --------------------
   ---------------------------------------------------------------------- */

/******************************************************************
  Function: SDL_KeyDown
  Purpose:  To pass the SDL_KeyDown message from the emulator to the
            plugin.
  input:    keymod and keysym of the SDL_KEYDOWN message.
  output:   none
*******************************************************************/
EXPORT void CALL SDL_KeyDown(int keymod, int keysym)
{
}

/******************************************************************
  Function: SDL_KeyUp
  Purpose:  To pass the SDL_KeyUp message from the emulator to the
            plugin.
  input:    keymod and keysym of the SDL_KEYUP message.
  output:   none
*******************************************************************/
EXPORT void CALL SDL_KeyUp(int keymod, int keysym)
{
}

#pragma endregion

/******************************************************************
  Function: GetKeys
  Purpose:  To get the current state of the controllers buttons.
  input:    - Controller Number (0 to 3)
            - A pointer to a BUTTONS structure to be filled with
            the controller state.
  output:   none
*******************************************************************/
EXPORT void CALL GetKeys( int Control, BUTTONS *Keys )
{
	Keys->Value = (*l_inputCallback)(Control);
}

/* ----------------------------------------------------------------------
   ----------- Sets callback to retrieve button and axis data -----------
   ---------------------------------------------------------------------- */
EXPORT void CALL SetInputCallback(int (*inputCallback)(int i))
{
	l_inputCallback = inputCallback;
}

/* ----------------------------------------------------------------------
   ----------- Sets a callback to set rumble on and off     -------------
   ---------------------------------------------------------------------- */
EXPORT void CALL SetRumbleCallback(void (*rumbleCallback)(int Control, int on))
{
	l_setrumbleCallback = rumbleCallback;
}

/* ----------------------------------------------------------------------
   ----------- Sets the type of the controller pak          -------------
   ----------- Possible values for type:                    -------------
   ----------- 1 - No pak inserted                          -------------
   ----------- 2 - Memory card                              -------------
   ----------- 3 - Rumble pak (no default implementation)   -------------
   ----------- 4 - Transfer pak (no default implementation) -------------
   ----------- 5 - Raw data                                 -------------
   ---------------------------------------------------------------------- */
EXPORT void CALL SetControllerPakType(int idx, int type)
{
	controller[idx].control->Plugin = type;
	controller[idx].control->RawData = (type != PLUGIN_MEMPAK);
}

/* ----------------------------------------------------------------------
   ----------- Sets if a controller is connected            -------------
   ---------------------------------------------------------------------- */
EXPORT void CALL SetControllerConnected(int idx, int connected)
{
	controller[idx].control->Present = connected;
}
