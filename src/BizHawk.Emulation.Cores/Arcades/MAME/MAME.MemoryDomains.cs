﻿using System;
using System.Collections.Generic;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Arcades.MAME
{
	public partial class MAME
	{
		private MemoryDomainList _memoryDomains;

		public class MAMEMemoryDomain : MemoryDomain
		{
			private readonly LibMAME _core;
			private readonly IMonitor _monitor;
			private readonly int _firstOffset;
			private readonly int _systemBusAddressShift;
			private readonly long _systemBusSize;

			public MAMEMemoryDomain(string name, long size, Endian endian, int dataWidth, bool writable, LibMAME core, IMonitor monitor, int firstOffset, int systemBusAddressShift, long systemBusSize)
			{
				Name = name;
				Size = size;
				EndianType = endian;
				WordSize = dataWidth;
				Writable = writable;

				_core = core;
				_monitor = monitor;
				_firstOffset = firstOffset;
				_systemBusAddressShift = systemBusAddressShift;
				_systemBusSize = systemBusSize;
			}

			public override byte PeekByte(long addr)
			{
				if ((ulong)addr >= (ulong)_systemBusSize) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "address out of range");
				addr += _firstOffset;
				return _core.mame_read_byte((uint)addr << _systemBusAddressShift);
			}

			public override void PokeByte(long addr, byte val)
			{
				if (Writable)
				{
					if ((ulong)addr >= (ulong)_systemBusSize) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "address out of range");
					addr += _firstOffset;
					_core.mame_lua_execute($"{MAMELuaCommand.GetSpace}:write_u8({addr << _systemBusAddressShift}, {val})");
				}
			}

			public override void Enter()
				=> _monitor.Enter();

			public override void Exit()
				=> _monitor.Exit();
		}

		private void InitMemoryDomains()
		{
			List<MemoryDomain> domains = new();

			int systemBusAddressShift = _core.mame_lua_get_int(MAMELuaCommand.GetSpaceAddressShift);
			int dataWidth = _core.mame_lua_get_int(MAMELuaCommand.GetSpaceDataWidth) >> 3; // mame returns in bits
			long size = _core.mame_lua_get_long(MAMELuaCommand.GetSpaceAddressMask) + dataWidth;
			string endianString = MameGetString(MAMELuaCommand.GetSpaceEndianness);
			string deviceName = MameGetString(MAMELuaCommand.GetMainCPUName);
			//var addrSize = (size * 2).ToString();

			var endian = MemoryDomain.Endian.Unknown;

			if (endianString == "little")
			{
				endian = MemoryDomain.Endian.Little;
			}
			else if (endianString == "big")
			{
				endian = MemoryDomain.Endian.Big;
			}

			int mapCount = _core.mame_lua_get_int(MAMELuaCommand.GetSpaceMapCount);

			for (int i = 1; i <= mapCount; i++)
			{
				string read = MameGetString($"return { MAMELuaCommand.SpaceMap }[{ i }].read.handlertype");
				string write = MameGetString($"return { MAMELuaCommand.SpaceMap }[{ i }].write.handlertype");

				if (read == "ram" && write == "ram" || read == "rom")
				{
					int firstOffset = _core.mame_lua_get_int($"return { MAMELuaCommand.SpaceMap }[{ i }].address_start");
					int lastOffset = _core.mame_lua_get_int($"return { MAMELuaCommand.SpaceMap }[{ i }].address_end");
					string name = $"{ deviceName } : { read } : 0x{ firstOffset:X}-0x{ lastOffset:X}";

					domains.Add(new MAMEMemoryDomain(name, lastOffset - firstOffset + 1, endian,
						dataWidth, read != "rom", _core, _exe, firstOffset, systemBusAddressShift, size));
				}
			}

			domains.Add(new MAMEMemoryDomain(deviceName + " : System Bus", size, endian, dataWidth, false, _core, _exe, 0, systemBusAddressShift, size));
			domains.Add(_exe.GetPagesDomain());

			_memoryDomains = new(domains);
			_memoryDomains.SystemBus = _memoryDomains[deviceName + " : System Bus"];
			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(_memoryDomains);
		}
	}
}
