﻿using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
	/// <summary>
	/// ZXHawk: Core Class
	/// * Controllers *
	/// </summary>
	public partial class ZXSpectrum
	{
		/// <summary>
		/// The one ZX Hawk ControllerDefinition
		/// </summary>
		public ControllerDefinition ZXSpectrumControllerDefinition
		{
			get
			{
				ControllerDefinition definition = new("ZXSpectrum Controller");

				// joysticks
				List<string> joys1 = new()
				{
					// P1 Joystick
					"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 Button",
				};

				foreach (string s in joys1)
				{
					definition.BoolButtons.Add(s);
					definition.CategoryLabels[s] = "J1 (" + SyncSettings.JoystickType1 + ")";
				}

				List<string> joys2 = new()
				{
					// P2 Joystick
					"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 Button",
				};

				foreach (string s in joys2)
				{
					definition.BoolButtons.Add(s);
					definition.CategoryLabels[s] = "J2 (" + SyncSettings.JoystickType2 + ")";
				}

				List<string> joys3 = new()
				{
					// P3 Joystick
					"P3 Up", "P3 Down", "P3 Left", "P3 Right", "P3 Button",
				};

				foreach (string s in joys3)
				{
					definition.BoolButtons.Add(s);
					definition.CategoryLabels[s] = "J3 (" + SyncSettings.JoystickType3 + ")";
				}

				// keyboard
				List<string> keys = new()
				{
					// Controller mapping includes all keyboard keys from the following models:
					// https://upload.wikimedia.org/wikipedia/commons/thumb/3/33/ZXSpectrum48k.jpg/1200px-ZXSpectrum48k.jpg
					// https://upload.wikimedia.org/wikipedia/commons/c/ca/ZX_Spectrum%2B.jpg

					// Keyboard - row 1    
					"Key True Video", "Key Inv Video", "Key 1", "Key 2", "Key 3", "Key 4", "Key 5", "Key 6", "Key 7", "Key 8", "Key 9", "Key 0", "Key Break",
					// Keyboard - row 2
					"Key Delete", "Key Graph", "Key Q", "Key W", "Key E", "Key R", "Key T", "Key Y", "Key U", "Key I", "Key O", "Key P",
					// Keyboard - row 3
					"Key Extend Mode", "Key Edit", "Key A", "Key S", "Key D", "Key F", "Key G", "Key H", "Key J", "Key K", "Key L", "Key Return",
					// Keyboard - row 4
					"Key Caps Shift", "Key Caps Lock", "Key Z", "Key X", "Key C", "Key V", "Key B", "Key N", "Key M", "Key Period",
					// Keyboard - row 5
					"Key Symbol Shift", "Key Semi-Colon", "Key Quote", "Key Left Cursor", "Key Right Cursor", "Key Space", "Key Up Cursor", "Key Down Cursor", "Key Comma",
				};

				foreach (string s in keys)
				{
					definition.BoolButtons.Add(s);
					definition.CategoryLabels[s] = "Keyboard";
				}

				// Power functions
				List<string> power = new()
				{
					// Power functions
					"Reset", "Power"
				};

				foreach (string s in power)
				{
					definition.BoolButtons.Add(s);
					definition.CategoryLabels[s] = "Power";
				}

				// Datacorder (tape device)
				List<string> tape = new()
				{
					// Tape functions
					"Play Tape", "Stop Tape", "RTZ Tape", "Record Tape", "Insert Next Tape",
					"Insert Previous Tape", "Next Tape Block", "Prev Tape Block", "Get Tape Status"
				};

				foreach (string s in tape)
				{
					definition.BoolButtons.Add(s);
					definition.CategoryLabels[s] = "Datacorder";
				}

				// Datacorder (tape device)
				List<string> disk = new()
				{
					// Tape functions
					"Insert Next Disk", "Insert Previous Disk", /*"Eject Current Disk",*/ "Get Disk Status"
				};

				foreach (string s in disk)
				{
					definition.BoolButtons.Add(s);
					definition.CategoryLabels[s] = "+3 Disk Drive";
				}

				return definition.MakeImmutable();
			}
		}
	}

	/// <summary>
	/// The possible joystick types
	/// </summary>
	public enum JoystickType
	{
		NULL,
		Kempston,
		SinclairLEFT,
		SinclairRIGHT,
		Cursor
	}
}
