using System.Collections.Generic;
using System.Linq;
using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Emulation.Common;
using static BizHawk.Emulation.Cores.Consoles.Nintendo.Mupen64.Mupen64.N64ControllerPakType;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Mupen64;

public static class Mupen64Controller
{
	public static readonly string[] BoolButtons =
	[
		"DPad R", "DPad L", "DPad D", "DPad U", "Start", "Z", "B", "A", "C Right", "C Left", "C Down", "C Up", "R", "L"
	];

	private static readonly Dictionary<string, int> DisplayButtonOrder = new()
	{
		["DPad U"] = 0,
		["DPad D"] = 1,
		["DPad L"] = 2,
		["DPad R"] = 3,
		["Start"] = 4,
		["Z"] = 5,
		["B"] = 6,
		["A"] = 7,
		["C Up"] = 8,
		["C Down"] = 9,
		["C Left"] = 10,
		["C Right"] = 11,
		["L"] = 12,
		["R"] = 13,
	};

	public static ControllerDefinition MakeControllerDefinition(Mupen64.SyncSettings syncSettings)
	{
		var controllerDefinition = new ControllerDefinition("Nintendo 64 Controller");
		controllerDefinition.BoolButtons.AddRange([ "Reset", "Power" ]);
		if (syncSettings.Port1Connected) AddN64StandardController(controllerDefinition, 1, syncSettings.Port1PakType == RumblePak);
		if (syncSettings.Port2Connected) AddN64StandardController(controllerDefinition, 2, syncSettings.Port2PakType == RumblePak);
		if (syncSettings.Port3Connected) AddN64StandardController(controllerDefinition, 3, syncSettings.Port3PakType == RumblePak);
		if (syncSettings.Port4Connected) AddN64StandardController(controllerDefinition, 4, syncSettings.Port4PakType == RumblePak);
		controllerDefinition.MakeImmutable();

		return controllerDefinition;

		static void AddN64StandardController(ControllerDefinition def, int player, bool hasRumblePak)
		{
			def.BoolButtons.AddRange(BoolButtons.OrderBy(b => DisplayButtonOrder[b]).Select(button => $"P{player} {button}"));
			def.AddXYPair(
				$"P{player} {{0}} Axis",
				AxisPairOrientation.RightAndUp,
				(-128).RangeTo(127),
				0,
				new CircularAxisConstraint("Natural Circle", $"P{player} Y Axis", 127.0f)
			);
			if (hasRumblePak) def.HapticsChannels.Add($"P{player} Rumble Pak");
		}
	}
}
