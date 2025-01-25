using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	internal static class SNESControllerDefExtensions
	{
		/// <remarks>
		/// problem: when you're in 240 line mode, the limit on Y needs to be 0-239. when you're in 224 mode, it needs to be 0-224.
		/// perhaps the deck needs to account for this...
		/// for reference Snes9x is always in 224 mode
		/// </remarks>
		public static ControllerDefinition AddLightGun(this ControllerDefinition def, string nameFormat)
			=> def.AddXYPair(nameFormat, AxisPairOrientation.RightAndDown, 0.RangeTo(255), 128, 0.RangeTo(239), 120); //TODO verify direction against hardware
	}

	public static class SnesMouseController
	{
		public static readonly ControllerDefinition Definition
			= new ControllerDefinition("(SNES Controller fragment)") { BoolButtons = { "0Mouse Left", "0Mouse Right" } }
				.AddXYPair("0Mouse {0}", AxisPairOrientation.RightAndDown, (-127).RangeTo(127), 0); //TODO verify direction against hardware, R+D inferred from behaviour in Mario Paint
	}
}
