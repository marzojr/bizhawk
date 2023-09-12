﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.N64.NativeApi;

namespace BizHawk.Emulation.Cores.Nintendo.N64
{
	public partial class N64
	{
		private readonly List<MemoryDomain> _memoryDomains = new();

		private IMemoryDomains MemoryDomains;

		private void MakeMemoryDomain(string name, mupen64plusApi.N64_MEMORY id, MemoryDomain.Endian endian, bool swizzled = false)
		{
			int size = api.get_memory_size(id);

			//if this type of memory isnt available, don't make the memory domain
			if (size == 0)
			{
				return;
			}

			var memPtr = api.get_memory_ptr(id);

			Func<long, byte> peekByte;
			Action<long, byte> pokeByte;

			if (swizzled)
			{
				peekByte = addr =>
				{
					if (addr < 0 || addr >= size) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "address out of range");
					return Marshal.ReadByte(memPtr, (int)(addr ^ 3));
				};
				pokeByte = (addr, val) =>
				{
					if (addr < 0 || addr >= size) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "address out of range");
					Marshal.WriteByte(memPtr, (int)(addr ^ 3), val);
				};
			}
			else
			{
				peekByte = addr =>
				{
					if (addr < 0 || addr >= size) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "address out of range");
					return Marshal.ReadByte(memPtr, (int)(addr));
				};
				pokeByte = (addr, val) =>
				{
					if (addr < 0 || addr >= size) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "address out of range");
					Marshal.WriteByte(memPtr, (int)(addr), val);
				};
			}

			MemoryDomainDelegate md = new(name, size, endian, peekByte, pokeByte, 4);

			_memoryDomains.Add(md);
		}

		private void InitMemoryDomains()
		{
			MakeMemoryDomain("RDRAM", mupen64plusApi.N64_MEMORY.RDRAM, MemoryDomain.Endian.Big, true);

			MakeMemoryDomain("ROM", mupen64plusApi.N64_MEMORY.THE_ROM, MemoryDomain.Endian.Big, true);

			MakeMemoryDomain("PI Register", mupen64plusApi.N64_MEMORY.PI_REG, MemoryDomain.Endian.Big, true);
			MakeMemoryDomain("SI Register", mupen64plusApi.N64_MEMORY.SI_REG, MemoryDomain.Endian.Big, true);
			MakeMemoryDomain("VI Register", mupen64plusApi.N64_MEMORY.VI_REG, MemoryDomain.Endian.Big, true);
			MakeMemoryDomain("RI Register", mupen64plusApi.N64_MEMORY.RI_REG, MemoryDomain.Endian.Big, true);
			MakeMemoryDomain("AI Register", mupen64plusApi.N64_MEMORY.AI_REG, MemoryDomain.Endian.Big, true);

			MakeMemoryDomain("EEPROM", mupen64plusApi.N64_MEMORY.EEPROM, MemoryDomain.Endian.Big, true);
			MakeMemoryDomain("SRAM", mupen64plusApi.N64_MEMORY.SRAM, MemoryDomain.Endian.Big, true);
			MakeMemoryDomain("FlashRAM", mupen64plusApi.N64_MEMORY.FLASHRAM, MemoryDomain.Endian.Big, true);

			if (_syncSettings.Controllers[0].IsConnected &&
				_syncSettings.Controllers[0].PakType == N64SyncSettings.N64ControllerSettings.N64ControllerPakType.MEMORY_CARD)
			{
				MakeMemoryDomain("Mempak 1", mupen64plusApi.N64_MEMORY.MEMPAK1, MemoryDomain.Endian.Big, true);
			}

			if (_syncSettings.Controllers[1].IsConnected &&
				_syncSettings.Controllers[1].PakType == N64SyncSettings.N64ControllerSettings.N64ControllerPakType.MEMORY_CARD)
			{
				MakeMemoryDomain("Mempak 2", mupen64plusApi.N64_MEMORY.MEMPAK2, MemoryDomain.Endian.Big, true);
			}

			if (_syncSettings.Controllers[2].IsConnected &&
				_syncSettings.Controllers[2].PakType == N64SyncSettings.N64ControllerSettings.N64ControllerPakType.MEMORY_CARD)
			{
				MakeMemoryDomain("Mempak 3", mupen64plusApi.N64_MEMORY.MEMPAK3, MemoryDomain.Endian.Big, true);
			}

			if (_syncSettings.Controllers[3].IsConnected &&
				_syncSettings.Controllers[3].PakType == N64SyncSettings.N64ControllerSettings.N64ControllerPakType.MEMORY_CARD)
			{
				MakeMemoryDomain("Mempak 4", mupen64plusApi.N64_MEMORY.MEMPAK4, MemoryDomain.Endian.Big, true);
			}


			byte peekByte(long addr) => api.m64p_read_memory_8((uint)addr);
			void pokeByte(long addr, byte val) => api.m64p_write_memory_8((uint)addr, val);

			_memoryDomains.Add(new MemoryDomainDelegate
				(
					"System Bus",
					uint.MaxValue,
					 MemoryDomain.Endian.Big,
					peekByte,
					pokeByte, 4
				));

			MemoryDomains = new MemoryDomainList(_memoryDomains);
			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(MemoryDomains);
		}
	}
}
