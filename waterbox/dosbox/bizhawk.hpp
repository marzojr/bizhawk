#pragma once

// system
#include <limits.h>
#include <stddef.h>
#include <stdarg.h>
#include <stdio.h>

// waterbox
#include "emulibc.h"
#include "waterboxcore.h"

const int KEY_COUNT = 0x65;
const int FILENAME_MAXLENGTH = 64;

enum Axis
{
	AXIS_HORIZONTAL,
	AXIS_VERTICAL
};

enum JoystickRange
{
	JOY_MIN = -1,
	JOY_MID,
	JOY_MAX
};

enum MouseButtons
{
	MOUSE_LEFT,
	MOUSE_RIGHT,
	MOUSE_MIDDLE
};

enum MousePosition
{
	MOUSE_RELATIVE,
	MOUSE_ABSOLUTE
};

enum AudioChannels
{
	AUDIO_MONO = 1,
	AUDIO_STEREO
};

enum DriveAction
{
	ACTION_NONE,
	ACTION_EJECT,
	ACTION_INSERT
};

enum ControllerType
{
	CONTROLLER_NONE,
	CONTROLLER_JOYSTICK,
	CONTROLLER_MOUSE,
	CONTROLLER_CD32PAD
};

typedef union
{
    struct
    {
        bool up:1;
        bool down:1;
        bool left:1;
        bool right:1;
        bool b1:1;
        bool b2:1;
        bool b3:1;
        bool play:1;
        bool rewind:1;
        bool forward:1;
        bool green:1;
        bool yellow:1;
        bool red:1;
        bool blue:1;
    };
    uint16_t data;
} AllButtons;

typedef struct ControllerState
{
	int Type;
	AllButtons Buttons;
	int MouseX;
	int MouseY;
} Controller;

typedef struct
{
	FrameInfo base;
	Controller Port1;
	Controller Port2;
	char Keys[KEY_COUNT];
	int CurrentDrive;
	int Action;
	char FileName[FILENAME_MAXLENGTH];
} MyFrameInfo;
