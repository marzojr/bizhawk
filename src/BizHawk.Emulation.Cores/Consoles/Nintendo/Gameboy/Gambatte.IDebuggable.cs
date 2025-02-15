using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class Gameboy : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			var data = new int[10];
			LibGambatte.gambatte_getregs(GambatteState, data);

			return new Dictionary<string, RegisterValue>
			{
				["PC"] = (ushort)(data[(int)LibGambatte.RegIndices.PC] & 0xffff),
				["SP"] = (ushort)(data[(int)LibGambatte.RegIndices.SP] & 0xffff),
				["A"] = (byte)(data[(int)LibGambatte.RegIndices.A] & 0xff),
				["B"] = (byte)(data[(int)LibGambatte.RegIndices.B] & 0xff),
				["C"] = (byte)(data[(int)LibGambatte.RegIndices.C] & 0xff),
				["D"] = (byte)(data[(int)LibGambatte.RegIndices.D] & 0xff),
				["E"] = (byte)(data[(int)LibGambatte.RegIndices.E] & 0xff),
				["F"] = (byte)(data[(int)LibGambatte.RegIndices.F] & 0xff),
				["H"] = (byte)(data[(int)LibGambatte.RegIndices.H] & 0xff),
				["L"] = (byte)(data[(int)LibGambatte.RegIndices.L] & 0xff),
				// banks
				["ROM0 BANK"] = (ushort)LibGambatte.gambatte_getbank(GambatteState, LibGambatte.BankType.ROM0),
				["ROMX BANK"] = (ushort)LibGambatte.gambatte_getbank(GambatteState, LibGambatte.BankType.ROMX),
				["VRAM BANK"] = (byte)LibGambatte.gambatte_getbank(GambatteState, LibGambatte.BankType.VRAM),
				["SRAM BANK"] = (byte)LibGambatte.gambatte_getbank(GambatteState, LibGambatte.BankType.SRAM),
				["WRAM BANK"] = (byte)LibGambatte.gambatte_getbank(GambatteState, LibGambatte.BankType.WRAM),
				// todo: maybe do [bc]/[de]/[hl]?
			};
		}

		public void SetCpuRegister(string register, int value)
		{
			if (register.Length is 9 && register.EndsWith(" BANK", StringComparison.OrdinalIgnoreCase))
			{
				var type = (LibGambatte.BankType)Enum.Parse(typeof(LibGambatte.BankType), register.Substring(0, 4).ToUpperInvariant());
				LibGambatte.gambatte_setbank(GambatteState, type, value);
			}
			else
			{
				var data = new int[10];
				LibGambatte.gambatte_getregs(GambatteState, data);
				var index = (LibGambatte.RegIndices)Enum.Parse(typeof(LibGambatte.RegIndices), register.ToUpperInvariant());
				data[(int)index] = value & (index <= LibGambatte.RegIndices.SP ? 0xffff : 0xff);
				LibGambatte.gambatte_setregs(GambatteState, data);
			}
		}

		public bool CanStep(StepType type) => false;

		[FeatureNotImplemented]
		public void Step(StepType type) => throw new NotImplementedException();

		public long TotalExecutedCycles => Math.Max((long)_cycleCount, (long)callbackCycleCount);

		private MemoryCallbackSystem _memorycallbacks = new(new[] { "System Bus", "ROM", "VRAM", "SRAM", "WRAM", "OAM", "HRAM" });
		public IMemoryCallbackSystem MemoryCallbacks => _memorycallbacks;

		private LibGambatte.MemoryCallback _readcb;
		private LibGambatte.MemoryCallback _writecb;
		private LibGambatte.MemoryCallback _execcb;

		/// <summary>
		/// for use in dual core
		/// </summary>
		internal void ConnectMemoryCallbackSystem(MemoryCallbackSystem mcs, int which)
		{
			_memorycallbacks = mcs;
			_readcb = CreateCallback(MemoryCallbackFlags.AccessRead, () => MemoryCallbacks.HasReads, $"P{which + 1} ");
			_writecb = CreateCallback(MemoryCallbackFlags.AccessWrite, () => MemoryCallbacks.HasWrites, $"P{which + 1} ");
			_execcb = CreateCallback(MemoryCallbackFlags.AccessExecute, () => MemoryCallbacks.HasExecutes, $"P{which + 1} ");
			_memorycallbacks.ActiveChanged += SetMemoryCallbacks;
		}

		private LibGambatte.MemoryCallback CreateCallback(MemoryCallbackFlags flags, Func<bool> getHasCBOfType, string which = "")
		{
			var rawFlags = (uint)flags;
			return (address, cycleOffset, data) =>
			{
				callbackCycleCount = _cycleCount + cycleOffset;
				if (getHasCBOfType())
				{
					MemoryCallbacks.CallMemoryCallbacks(address, data, rawFlags, which + "System Bus");
					var bank = LibGambatte.gambatte_getaddrbank(GambatteState, (ushort)address);
					switch (address)
					{
						// usually rom bank 0 for most mbcs, some mbcs might have this at a different rom bank
						case < 0x4000u:
							address += (uint)(bank * 0x4000);
							MemoryCallbacks.CallMemoryCallbacks(address, data, rawFlags, which + "ROM");
							break;
						// rom bank x
						case < 0x8000u:
							address += (uint)(bank * 0x4000);
							address -= 0x4000u;
							MemoryCallbacks.CallMemoryCallbacks(address, data, rawFlags, which + "ROM");
							break;
						// vram (may be banked on CGB in CGB enhanced mode)
						case < 0xA000u:
						{
							if (IsCGBMode && !IsCGBDMGMode)
							{
								address += (uint)(bank * 0x2000);
							}
							address -= 0x8000u;
							MemoryCallbacks.CallMemoryCallbacks(address, data, rawFlags, which + "VRAM");
							break;
						}
						// sram (may be banked)
						case < 0xC000u:
							address += (uint)(bank * 0x2000);
							address -= 0xA000u;
							MemoryCallbacks.CallMemoryCallbacks(address, data, rawFlags, which + "SRAM");
							break;
						// wram bank 0
						case < 0xD000u:
							address -= 0xC000u;
							MemoryCallbacks.CallMemoryCallbacks(address, data, rawFlags, which + "WRAM");
							break;
						// wram bank x (always one for dmg/cgb in dmg mode)
						case < 0xE000u:
						{
							if (IsCGBMode && !IsCGBDMGMode)
							{
								address += (uint)(bank * 0x1000);
							}
							address -= 0xD000u;
							MemoryCallbacks.CallMemoryCallbacks(address, data, rawFlags, which + "WRAM");
							break;
						}
						// echo ram
						case < 0xFE00u:
							// do we do something here?
							break;
						// oam
						case < 0xFEA0u:
							address -= 0xFE00u;
							MemoryCallbacks.CallMemoryCallbacks(address, data, rawFlags, which + "OAM");
							break;
						// "extra" oam
						case < 0xFF00u:
							// do we do something here?
							break;
						// mmio
						case < 0xFF80u:
							// do we do something here?
							break;
						// hram
						case < 0xFFFF:
							address -= 0xFF80u;
							MemoryCallbacks.CallMemoryCallbacks(address, data, rawFlags, which + "HRAM");
							break;
						// ie reg
						case 0xFFFF:
							// do we do something here?
							break;
						default:
							throw new InvalidOperationException("Core accessed invalid address???");
					}
				}
			};
		}

		private void InitMemoryCallbacks()
		{
			_readcb = CreateCallback(MemoryCallbackFlags.AccessRead, () => MemoryCallbacks.HasReads);
			_writecb = CreateCallback(MemoryCallbackFlags.AccessWrite, () => MemoryCallbacks.HasWrites);
			_execcb = CreateCallback(MemoryCallbackFlags.AccessExecute, () => MemoryCallbacks.HasExecutes);

			_memorycallbacks.ActiveChanged += SetMemoryCallbacks;
		}

		private void SetMemoryCallbacks()
		{
			LibGambatte.gambatte_setreadcallback(GambatteState, MemoryCallbacks.HasReads ? _readcb : null);
			LibGambatte.gambatte_setwritecallback(GambatteState, MemoryCallbacks.HasWrites ? _writecb : null);
			LibGambatte.gambatte_setexeccallback(GambatteState, MemoryCallbacks.HasExecutes ? _execcb : null);
		}
	}
}
