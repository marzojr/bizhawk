using Newtonsoft.Json;

using BizHawk.Emulation.Common;
using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;

namespace BizHawk.Emulation.Cores.Sony.PSX
{
	public static class Octoshock
	{
		public static ControllerDefinition CreateControllerDefinition(SyncSettings syncSettings)
		{
			ControllerDefinition definition = new("PSX Front Panel");

			var cfg = syncSettings.FIOConfig.ToLogical();

			for (int i = 0; i < cfg.NumPlayers; i++)
			{
				int pnum = i + 1;

				var type = cfg.DevicesPlayer[i];
				if (type == OctoshockDll.ePeripheralType.NegCon)
				{
					definition.BoolButtons.AddRange(new[]
					{
							"P" + pnum + " Up",
							"P" + pnum + " Down",
							"P" + pnum + " Left",
							"P" + pnum + " Right",
							"P" + pnum + " Start",
							"P" + pnum + " R",
							"P" + pnum + " B",
							"P" + pnum + " A",
					});

					foreach (var axisName in new[] { $"P{pnum} Twist", $"P{pnum} 1", $"P{pnum} 2", $"P{pnum} L" })
					{
						definition.AddAxis(axisName, 0.RangeTo(255), 128);
					}
				}
				else
				{
					definition.BoolButtons.AddRange(new[]
					{
							"P" + pnum + " Up",
							"P" + pnum + " Down",
							"P" + pnum + " Left",
							"P" + pnum + " Right",
							"P" + pnum + " Select",
							"P" + pnum + " Start",
							"P" + pnum + " Square",
							"P" + pnum + " Triangle",
							"P" + pnum + " Circle",
							"P" + pnum + " Cross",
							"P" + pnum + " L1",
							"P" + pnum + " R1",
							"P" + pnum + " L2",
							"P" + pnum + " R2",
						});


					if (type == OctoshockDll.ePeripheralType.DualShock || type == OctoshockDll.ePeripheralType.DualAnalog)
					{
						definition.BoolButtons.Add("P" + pnum + " L3");
						definition.BoolButtons.Add("P" + pnum + " R3");
						definition.BoolButtons.Add("P" + pnum + " MODE");
						definition.AddXYPair($"P{pnum} LStick {{0}}", AxisPairOrientation.RightAndDown, 0.RangeTo(255), 128);
						definition.AddXYPair($"P{pnum} RStick {{0}}", AxisPairOrientation.RightAndDown, 0.RangeTo(255), 128);
					}
				}
			}

			definition.BoolButtons.AddRange(new[]
			{
				"Open",
				"Close",
				"Reset"
			});

			definition.AddAxis("Disc Select", 0.RangeTo(1), 1);

			return definition.MakeImmutable();
		}

		public class SyncSettings
		{
			public SyncSettings Clone()
			{
				return JsonConvert.DeserializeObject<SyncSettings>(JsonConvert.SerializeObject(this));
			}

			public bool EnableLEC;

			public SyncSettings()
			{
				//initialize with single controller and memcard
				var user = new OctoshockFIOConfigUser();
				user.Memcards[0] = true;
				user.Memcards[1] = false;
				user.Multitaps[0] = user.Multitaps[0] = false;
				user.Devices8[0] = OctoshockDll.ePeripheralType.DualShock;
				user.Devices8[4] = OctoshockDll.ePeripheralType.None;
				FIOConfig = user;
			}

			public OctoshockFIOConfigUser FIOConfig;
		}
	}
}
