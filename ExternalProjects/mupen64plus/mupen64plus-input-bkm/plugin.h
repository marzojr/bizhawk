/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
 *   Mupen64plus-input-sdl - plugin.h                                      *
 *   Mupen64Plus homepage: http://code.google.com/p/mupen64plus/           *
 *   Copyright (C) 2008-2009 Richard Goedeken                              *
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

#ifndef __PLUGIN_H__
#define __PLUGIN_H__

#define M64P_PLUGIN_PROTOTYPES 1
#include <api/m64p_plugin.h>
#include <stdbool.h>

// Some stuff from n-rage plugin
#define RD_GETSTATUS		0x00 // get status
#define RD_READKEYS			0x01 // read button values
#define RD_READPAK			0x02 // read from controllerpack
#define RD_WRITEPAK			0x03 // write to controllerpack
#define RD_RESETCONTROLLER	0xff // reset controller
#define RD_READEEPROM		0x04 // read eeprom
#define RD_WRITEEPROM		0x05 // write eeprom

#define RD_ABSOLUTE			0x01 // Default gamepad
#define RD_RELATIVE			0x02
#define RD_GAMEPAD			0x04 // Default gamepad
        //B2
#define RD_EEPROM			0x80
#define RD_NOEEPROM			0x00

        //C3
        // No Plugin in Controller
#define RD_NOPLUGIN			0x00 // No pak in controller
#define RD_PLUGIN			0x01 // Any pak in controller
#define RD_NOTINITIALIZED	0x02 // Pak was uninitialized before call
#define RD_ADDRCRCERR		0x04 // Last I/O address was invalid
#define RD_EEPROMBUSY		0x80 // EEPROM busy

typedef struct
{
    CONTROL *control;               // pointer to CONTROL struct in Core library
	bool rumbling;
} SController;

/* global data definitions */
extern SController controller[4];   // 4 controllers

/* global function definitions */
extern void DebugMessage(int level, const char *message, ...);

#endif // __PLUGIN_H__
