﻿using System;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components;

namespace BizHawk.Emulation.Cores.ColecoVision
{
	public partial class ColecoVision
	{
		private SN76489 PSG;
		private FakeSyncSound _fakeSyncSound; 
	}
}
