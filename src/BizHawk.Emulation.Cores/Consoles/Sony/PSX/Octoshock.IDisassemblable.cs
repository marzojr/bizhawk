﻿using System.Collections.Generic;
using System.Text;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sony.PSX
{
	public partial class Octoshock : IDisassemblable
	{
		public string Cpu
		{
			get => "R3000A";
			set { }
		}

		public IEnumerable<string> AvailableCpus
		{
			get
			{
				yield return "R3000A";
			}
		}

		public string PCRegisterName => "pc";

		public string Disassemble(MemoryDomain m, uint addr, out int length)
		{
			length = 4;
			StringBuilder buf = new(32);
			int result = OctoshockDll.shock_Util_DisassembleMIPS(addr, m.PeekUint(addr, false), buf, buf.Capacity);
			return result==0?buf.ToString():"";
		}
	}
}
