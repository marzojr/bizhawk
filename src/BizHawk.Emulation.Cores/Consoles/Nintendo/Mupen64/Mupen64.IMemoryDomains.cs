using System;
using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Mupen64;

public partial class Mupen64
{
	private MemoryDomainList _memoryDomains;

	private void SetupMemoryDomains()
	{
		List<MemoryDomain> memoryDomains = [ ];

		foreach (Mupen64Api.m64p_dbg_memptr_type memoryDomain in Enum.GetValues(typeof(Mupen64Api.m64p_dbg_memptr_type)))
		{
			memoryDomains.Add(new MemoryDomainIntPtr(
				Enum.GetName(typeof(Mupen64Api.m64p_dbg_memptr_type), memoryDomain),
				MemoryDomain.Endian.Big,
				Mupen64Api.DebugMemGetPointer(memoryDomain),
				(long)Mupen64Api.DebugMemGetSize(memoryDomain),
				true,
				4));
		}

		memoryDomains.Add(new MemoryDomainDelegate
		(
			"System Bus",
			uint.MaxValue,
			MemoryDomain.Endian.Big,
			address => Mupen64Api.DebugMemRead8((uint)address),
			(address, value) => Mupen64Api.DebugMemWrite8((uint)address, value),
			4
		));

		_memoryDomains = new MemoryDomainList(memoryDomains);
	}
}
