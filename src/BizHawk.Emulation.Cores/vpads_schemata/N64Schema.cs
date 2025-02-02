using System.Collections.Generic;
using System.Drawing;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Nintendo.Ares64;
using BizHawk.Emulation.Cores.Consoles.Nintendo.Mupen64;

namespace BizHawk.Emulation.Cores
{
	[Schema(VSystemID.Raw.N64)]
	public class N64Schema : IVirtualPadSchema
	{
		public IEnumerable<PadSchema> GetPadSchemas(IEmulator core, Action<string> showMessageBox)
		{
			if (core is Mupen64 n64)
			{
				var ss = n64.GetSyncSettings();

				int i = 1;
				if (ss.Port1Connected) yield return StandardController(i++);
				if (ss.Port2Connected) yield return StandardController(i++);
				if (ss.Port3Connected) yield return StandardController(i++);
				if (ss.Port4Connected) yield return StandardController(i);
			}
			else if (core is Ares64 ares64)
			{
				for (var i = 0; i < 4; i++)
				{
					if (ares64.ControllerSettings[i] != LibAres64.ControllerType.Unplugged)
					{
						yield return StandardController(i + 1);
					}
				}
			}
		}

		private static readonly Func<AxisSpec> MakeRange = () => new((-128).RangeTo(127), 0);

		private static PadSchema StandardController(int controller)
		{
			return new PadSchema
			{
				Size = new Size(275, 316),
				Buttons = new PadSchemaControl[]
				{
					ButtonSchema.Up(24, 230, $"P{controller} DPad U"),
					ButtonSchema.Down(24, 251, $"P{controller} DPad D"),
					ButtonSchema.Left(3, 242, $"P{controller} DPad L"),
					ButtonSchema.Right(45, 242, $"P{controller} DPad R"),
					new ButtonSchema(3, 185, controller, "L"),
					new ButtonSchema(191, 185, controller, "R"),
					new ButtonSchema(81, 269, controller, "Z"),
					new ButtonSchema(81, 246, controller, "Start", "S"),
					new ButtonSchema(127, 246, controller, "B"),
					new ButtonSchema(138, 269, controller, "A"),
					new ButtonSchema(173, 210, controller, "C Up") { Icon = VGamepadButtonImage.YellowArrN },
					new ButtonSchema(173, 231, controller, "C Down") { Icon = VGamepadButtonImage.YellowArrS },
					new ButtonSchema(152, 221, controller, "C Left") { Icon = VGamepadButtonImage.YellowArrW },
					new ButtonSchema(194, 221, controller, "C Right") { Icon = VGamepadButtonImage.YellowArrE },
					new AnalogSchema(6, 14, $"P{controller} X Axis")
					{
						Spec = MakeRange(),
						SecondarySpec = MakeRange()
					}
				}
			};
		}
	}
}
