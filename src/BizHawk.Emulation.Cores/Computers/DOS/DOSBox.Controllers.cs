using System.Collections.Generic;

using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.DOS
{
	public partial class DOSBox
	{
		private LibDOSBox.ControllerType[] _ports { get; set; }
		private static readonly (string Name, LibDOSBox.AllButtons Button)[] _joystickMap = CreateJoystickMap();
		private static readonly (string Name, LibDOSBox.AllButtons Button)[] _cd32padMap = CreateCd32padMap();
		private static readonly (string Name, LibDOSBox.DOSBoxKeyboard Key)[] _keyboardMap = CreateKeyboardMap();

		private static (string Name, LibDOSBox.AllButtons Value)[] CreateJoystickMap()
		{
			var joystickMap = new List<(string, LibDOSBox.AllButtons)>();
			// ReSharper disable once LoopCanBeConvertedToQuery
			foreach (var b in Enum.GetValues(typeof(LibDOSBox.AllButtons)))
			{
				if (((short)b & LibDOSBox.JoystickMask) == 0)
					continue;

				var name = Enum.GetName(typeof(LibDOSBox.AllButtons), b)!.Replace('_', ' ');
				joystickMap.Add((name, (LibDOSBox.AllButtons)b));
			}
			return joystickMap.ToArray();
		}

		private static (string Name, LibDOSBox.AllButtons Value)[] CreateCd32padMap()
		{
			var joystickMap = new List<(string, LibDOSBox.AllButtons)>();
			// ReSharper disable once LoopCanBeConvertedToQuery
			foreach (var b in Enum.GetValues(typeof(LibDOSBox.AllButtons)))
			{
				if (((short)b & LibDOSBox.Cd32padMask) == 0)
					continue;

				var name = Enum.GetName(typeof(LibDOSBox.AllButtons), b)!.Replace('_', ' ');
				joystickMap.Add((name, (LibDOSBox.AllButtons)b));
			}
			return joystickMap.ToArray();
		}

		private static (string Name, LibDOSBox.DOSBoxKeyboard Value)[] CreateKeyboardMap()
		{
			var keyboardMap = new List<(string, LibDOSBox.DOSBoxKeyboard)>();
			// ReSharper disable once LoopCanBeConvertedToQuery
			foreach (var k in Enum.GetValues(typeof(LibDOSBox.DOSBoxKeyboard)))
			{
				var name = Enum.GetName(typeof(LibDOSBox.DOSBoxKeyboard), k)!.Replace('_', ' ');
				keyboardMap.Add((name, (LibDOSBox.DOSBoxKeyboard) k));
			}

			return keyboardMap.ToArray();
		}

		private static ControllerDefinition CreateControllerDefinition(UAESyncSettings settings)
		{
			var controller = new ControllerDefinition("Amiga Controller");

			for (int port = 1; port <= 2; port++)
			{
				LibDOSBox.ControllerType type = port == 1
					? settings.ControllerPort1
					: settings.ControllerPort2;

				switch (type)
				{
					case LibDOSBox.ControllerType.DJoy:
						{
							foreach (var (name, _) in _joystickMap)
							{
								controller.BoolButtons.Add($"P{port} {Inputs.Joystick} {name}");
							}
							break;
						}
					case LibDOSBox.ControllerType.CD32Joy:
						{
							foreach (var (name, _) in _cd32padMap)
							{
								controller.BoolButtons.Add($"P{port} {Inputs.Cd32Pad} {name}");
							}
							break;
						}
					case LibDOSBox.ControllerType.Mouse:
						{
							controller.BoolButtons.AddRange(
							[
								$"P{port} {Inputs.MouseLeftButton}",
								$"P{port} {Inputs.MouseMiddleButton}",
								$"P{port} {Inputs.MouseRightButton}"
							]);
							controller
								.AddAxis($"P{port} {Inputs.MouseX}", 0.RangeTo(LibDOSBox.PAL_WIDTH), LibDOSBox.PAL_WIDTH / 2)
								.AddAxis($"P{port} {Inputs.MouseY}", 0.RangeTo(LibDOSBox.PAL_HEIGHT), LibDOSBox.PAL_HEIGHT / 2);
							break;
						}
				}
			}

			controller.BoolButtons.AddRange(
			[
				Inputs.NextDrive, Inputs.NextSlot, Inputs.InsertDisk, Inputs.EjectDisk
			]);

			foreach (var (name, _) in _keyboardMap)
			{
				controller.BoolButtons.Add(name);
				controller.CategoryLabels[name] = "Keyboard";
			}

			return controller.MakeImmutable();
		}

		private static class Inputs
		{
			public const string Joystick = "Joystick";
			public const string Cd32Pad = "CD32 pad";
			public const string MouseLeftButton = "Mouse Left Button";
			public const string MouseRightButton = "Mouse Right Button";
			public const string MouseMiddleButton = "Mouse Middle Button";
			public const string MouseX = "Mouse X";
			public const string MouseY = "Mouse Y";
			public const string EjectDisk = "Eject Disk";
			public const string InsertDisk = "Insert Disk";
			public const string NextDrive = "Next Drive";
			public const string NextSlot = "Next Slot";
		}
	}
}
