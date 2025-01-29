using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Sony.PSX;

namespace BizHawk.Emulation.Cores
{
	[Schema(VSystemID.Raw.PSX)]
	public class PsxSchema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core, Action<string> showMessageBox)
		{
			if (core is Nymashock nyma)
			{
				foreach (var result in nyma.ActualPortData)
				{
					var num = int.Parse(result.Port.ShortName.Last().ToString());
					var device = result.Device.ShortName;
					if (device is "none") continue;
					yield return device switch
					{
						"gamepad" => NymaGamePadController(num),
						"dualshock" or "dualanalog" => NymaDualShockController(num),
						"analogjoy" => NymaAnalogJoystick(num),
						"mouse" => NymaMouse(num),
						"negcon" => NymaNeGcon(num),
						"guncon" => NymaGunCon(num),
						"justifier" => NymaKonamiJustifier(num),
						"dancepad" => NymaDancePad(num),
						_ => throw new InvalidOperationException($"device {device} is not supported")
					};
				}

				yield return NymaConsoleButtons(nyma.ControllerDefinition.Axes["Disk Index"]);
			}
		}

		private static PadSchema NymaDualShockController(int controller)
		{
			var stickRanges = new[] { new AxisSpec(0.RangeTo(0xFF), 0x80), new AxisSpec(0.RangeTo(0xFF), 0x80, isReversed: true) };
			return new PadSchema
			{
				Size = new Size(500, 290),
				DisplayName = $"DualShock Player{controller}",
				Buttons = new PadSchemaControl[]
				{
					ButtonSchema.Up(32, 50, $"P{controller} D-Pad Up"),
					ButtonSchema.Down(32, 71, $"P{controller} D-Pad Down"),
					ButtonSchema.Left(11, 62, $"P{controller} D-Pad Left"),
					ButtonSchema.Right(53, 62, $"P{controller} D-Pad Right"),
					new ButtonSchema(3, 32, controller, "L1"),
					new ButtonSchema(191, 32, controller, "R1"),
					new ButtonSchema(3, 10, controller, "L2"),
					new ButtonSchema(191, 10, controller, "R2"),
					new ButtonSchema(72, 90, controller, "Left Stick, Button", "L3"),
					new ButtonSchema(130, 90, controller, "Right Stick, Button", "R3"),
					new ButtonSchema(148, 62, controller, "□") { Icon = VGamepadButtonImage.Square },
					new ButtonSchema(169, 50, controller, "△") { Icon = VGamepadButtonImage.Triangle },
					new ButtonSchema(190, 62, controller, "○") { Icon = VGamepadButtonImage.Circle },
					new ButtonSchema(169, 71, controller, "X") { Icon = VGamepadButtonImage.Cross },
					new ButtonSchema(112, 62, controller, "Start", "S"),
					new ButtonSchema(90, 62, controller, "Select", "s"),
					new AnalogSchema(3, 120, $"P{controller} Left Stick Left / Right")
					{
						SecondaryName = $"P{controller} Left Stick Up / Down",
						Spec = stickRanges[0],
						SecondarySpec = stickRanges[1]
					},
					new AnalogSchema(260, 120, $"P{controller} Right Stick Left / Right")
					{
						SecondaryName = $"P{controller} Right Stick Up / Down",
						Spec = stickRanges[0],
						SecondarySpec = stickRanges[1]
					}
				}
			};
		}

		private static PadSchema NymaGamePadController(int controller)
		{
			return new PadSchema
			{
				Size = new Size(240, 115),
				DisplayName = $"Gamepad Player{controller}",
				Buttons = new[]
				{
					ButtonSchema.Up(37, 55, controller),
					ButtonSchema.Down(37, 76, controller),
					ButtonSchema.Left(16, 67, controller),
					ButtonSchema.Right(58, 67, controller),
					new ButtonSchema(8, 37, controller, "L1"),
					new ButtonSchema(196, 37, controller, "R1"),
					new ButtonSchema(8, 15, controller, "L2"),
					new ButtonSchema(196, 15, controller, "R2"),
					new ButtonSchema(153, 67, controller, "□") { Icon = VGamepadButtonImage.Square },
					new ButtonSchema(174, 55, controller, "△") { Icon = VGamepadButtonImage.Triangle },
					new ButtonSchema(195, 67, controller, "○") { Icon = VGamepadButtonImage.Circle },
					new ButtonSchema(174, 76, controller, "X") { Icon = VGamepadButtonImage.Cross },
					new ButtonSchema(112, 67, controller, "Start", "S"),
					new ButtonSchema(90, 67, controller, "Select", "s")
				}
			};
		}

		private static PadSchema NymaAnalogJoystick(int controller)
		{
			var stickRanges = new[] { new AxisSpec(0.RangeTo(0xFFFF), 0x8000), new AxisSpec(0.RangeTo(0xFFFF), 0x8000, isReversed: true) };
			return new PadSchema
			{
				Size = new Size(500, 290),
				DisplayName = $"Analog Joystick Player{controller}",
				Buttons = new PadSchemaControl[]
				{
					ButtonSchema.Up(32 + 130, 50, $"P{controller} Thumbstick Up"),
					ButtonSchema.Down(32 + 130, 71, $"P{controller} Thumbstick Down"),
					ButtonSchema.Left(11 + 130, 62, $"P{controller} Thumbstick Left"),
					ButtonSchema.Right(53 + 130, 62, $"P{controller} Thumbstick Right"),
					new ButtonSchema(3, 90, controller, "Left Stick, L-Thumb", "LL"),
					new ButtonSchema(3 + 150 + 120, 90, controller, "Right Stick, L-Thumb", "RL"),
					new ButtonSchema(3 + 30, 90, controller, "Left Stick, R-Thumb", "LR"),
					new ButtonSchema(3 + 150 + 150, 90, controller, "Right Stick, R-Thumb", "RR"),
					new ButtonSchema(3 + 60, 90, controller, "Left Stick, Trigger", "LT"),
					new ButtonSchema(3 + 150 + 180, 90, controller, "Right Stick, Trigger", "RT"),
					new ButtonSchema(3 + 90, 90, controller, "Left Stick, Pinky", "LP"),
					new ButtonSchema(3 + 150 + 210, 90, controller, "Right Stick, Pinky", "RP"),
					new ButtonSchema(112 + 140, 62, controller, "Start", "S"),
					new ButtonSchema(90 + 140, 62, controller, "Select", "s"),
					new AnalogSchema(3, 120, $"P{controller} Left Stick, Left / Right")
					{
						SecondaryName = $"P{controller} Left Stick, Fore / Back",
						Spec = stickRanges[0],
						SecondarySpec = stickRanges[1]
					},
					new AnalogSchema(260, 120, $"P{controller} Right Stick, Left / Right")
					{
						SecondaryName = $"P{controller} Right Stick, Fore / Back",
						Spec = stickRanges[0],
						SecondarySpec = stickRanges[1]
					}
				}
			};
		}

		private static PadSchema NymaMouse(int controller)
		{
			return new PadSchema
			{
				DisplayName = $"Mouse Player{controller}",
				Size = new Size(375, 320),
				Buttons = new PadSchemaControl[]
				{
					new TargetedPairSchema(14, 17, $"P{controller} Motion Left / Right", 0xFFFF, 0xFFFF)
					{
						SecondaryName = $"P{controller} Motion Up / Down",
						TargetSize = new Size(256, 256)
					},
					new ButtonSchema(300, 17, controller, "Left Button", "Left"),
					new ButtonSchema(300, 77, controller, "Right Button", "Right"),
				}
			};
		}

		private static PadSchema NeGcon(int controller)
		{
			return new PadSchema
			{
				Size = new Size(343, 195),
				DisplayName = $"NeGcon Player{controller}",
				Buttons = new PadSchemaControl[]
				{
					ButtonSchema.Up(36, 83, controller),
					ButtonSchema.Down(36, 104, controller),
					ButtonSchema.Left(15, 95, controller),
					ButtonSchema.Right(57, 95, controller),
					new ButtonSchema(78, 118, controller, "Start", "S"),
					new ButtonSchema(278, 38, controller, "B"),
					new ButtonSchema(308, 55, controller, "A"),
					new ButtonSchema(308, 15, controller, "R"),
					new SingleAxisSchema(5, 15, controller, "L")
					{
						TargetSize = new Size(128, 55),
						MinValue = 0,
						MaxValue = 255
					},
					new SingleAxisSchema(125, 15, controller, "Twist", isVertical: true)
					{
						TargetSize = new Size(64, 178),
						MinValue = 0,
						MaxValue = 255
					},
					new SingleAxisSchema(180, 60, controller, "2")
					{
						DisplayName = "II",
						TargetSize = new Size(128, 55),
						MinValue = 0,
						MaxValue = 255
					},
					new SingleAxisSchema(220, 120, controller, "1")
					{
						DisplayName = "I",
						TargetSize = new Size(128, 55),
						MinValue = 0,
						MaxValue = 255
					}
				}
			};
		}

		private static PadSchema NymaNeGcon(int controller)
		{
			return new PadSchema
			{
				Size = new Size(343, 195),
				DisplayName = $"NeGcon Player{controller}",
				Buttons = new PadSchemaControl[]
				{
					ButtonSchema.Up(36, 83, $"P{controller} D-Pad Up"),
					ButtonSchema.Down(36, 104, $"P{controller} D-Pad Down"),
					ButtonSchema.Left(15, 95, $"P{controller} D-Pad Left"),
					ButtonSchema.Right(57, 95, $"P{controller} D-Pad Right"),
					new ButtonSchema(78, 118, controller, "Start", "S"),
					new ButtonSchema(278, 38, controller, "B"),
					new ButtonSchema(308, 55, controller, "A"),
					new ButtonSchema(308, 15, controller, "R"),
					new SingleAxisSchema(5, 15, controller, "L")
					{
						TargetSize = new Size(128, 55),
						MinValue = 0,
						MaxValue = 65535
					},
					new SingleAxisSchema(125, 15, controller, "Twist | / |", isVertical: true)
					{
						DisplayName = "Twist",
						TargetSize = new Size(64, 178),
						MinValue = 0,
						MaxValue = 65535
					},
					new SingleAxisSchema(180, 60, controller, "II")
					{
						TargetSize = new Size(128, 55),
						MinValue = 0,
						MaxValue = 65535
					},
					new SingleAxisSchema(220, 120, controller, "I")
					{
						TargetSize = new Size(128, 55),
						MinValue = 0,
						MaxValue = 65535
					}
				}
			};
		}

		private static PadSchema NymaGunCon(int controller)
		{
			return new PadSchema
			{
				DisplayName = $"GunCon Player{controller}",
				Size = new Size(375, 320),
				Buttons = new PadSchemaControl[]
				{
					new TargetedPairSchema(14, 17, $"P{controller} X Axis", 0xFFFF, 0xFFFF)
					{
						SecondaryName = $"P{controller} Y Axis",
						TargetSize = new Size(256, 256)
					},
					new ButtonSchema(300, 17, controller, "Trigger"),
					new ButtonSchema(300, 57, controller, "A"),
					new ButtonSchema(300, 87, controller, "B"),
					new ButtonSchema(300, 290, controller, "Offscreen Shot", "Offscreen")
				}
			};
		}

		private static PadSchema NymaKonamiJustifier(int controller)
		{
			return new PadSchema
			{
				DisplayName = $"Konami Justifier Player{controller}",
				Size = new Size(375, 320),
				Buttons = new PadSchemaControl[]
				{
					new TargetedPairSchema(14, 17, $"P{controller} X Axis", 0xFFFF, 0xFFFF)
					{
						SecondaryName = $"P{controller} Y Axis",
						TargetSize = new Size(256, 256)
					},
					new ButtonSchema(300, 17, controller, "Trigger"),
					new ButtonSchema(300, 57, controller, "O"),
					new ButtonSchema(300, 87, controller, "Start", "S"),
					new ButtonSchema(300, 290, controller, "Offscreen Shot", "Offscreen")
				}
			};
		}

		private static PadSchema NymaDancePad(int controller)
		{
			return new PadSchema
			{
				Size = new Size(240, 115),
				DisplayName = $"Dance Pad Player{controller}",
				Buttons = new[]
				{
					ButtonSchema.Up(37, 55, controller),
					ButtonSchema.Down(37, 76, controller),
					ButtonSchema.Left(16, 67, controller),
					ButtonSchema.Right(58, 67, controller),
					new ButtonSchema(153, 67, controller, "□") { Icon = VGamepadButtonImage.Square },
					new ButtonSchema(174, 55, controller, "△") { Icon = VGamepadButtonImage.Triangle },
					new ButtonSchema(195, 67, controller, "○") { Icon = VGamepadButtonImage.Circle },
					new ButtonSchema(174, 76, controller, "X") { Icon = VGamepadButtonImage.Cross },
					new ButtonSchema(112, 67, controller, "Start", "S"),
					new ButtonSchema(90, 67, controller, "Select", "s")
				}
			};
		}

		private static PadSchema NymaConsoleButtons(AxisSpec diskRange)
		{
			return new ConsoleSchema
			{
				Size = new Size(250, 100),
				Buttons = new PadSchemaControl[]
				{
					new ButtonSchema(10, 15, "Reset"),
					new ButtonSchema(58, 15, "Power"),
					new ButtonSchema(108, 15, "Open Tray"),
					new ButtonSchema(175, 15, "Close Tray"),
					new SingleAxisSchema(10, 35, "Disk Index")
					{
						MinValue = diskRange.Min,
						MaxValue = diskRange.Max,
						TargetSize = new Size(235, 60)
					}
				}
			};
		}
	}
}
