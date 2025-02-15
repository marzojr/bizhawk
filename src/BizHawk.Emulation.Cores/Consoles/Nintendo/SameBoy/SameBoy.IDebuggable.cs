using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Sameboy
{
	public partial class Sameboy : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			int[] data = new int[10];
			LibSameboy.sameboy_getregs(SameboyState, data);

			int[] banks = new int[5];
			LibSameboy.sameboy_getbankregs(SameboyState, banks);

			return new Dictionary<string, RegisterValue>
			{
				["PC"] = (ushort)(data[0] & 0xFFFF),
				["A"] = (byte)(data[1] & 0xFF),
				["F"] = (byte)(data[2] & 0xFF),
				["B"] = (byte)(data[3] & 0xFF),
				["C"] = (byte)(data[4] & 0xFF),
				["D"] = (byte)(data[5] & 0xFF),
				["E"] = (byte)(data[6] & 0xFF),
				["H"] = (byte)(data[7] & 0xFF),
				["L"] = (byte)(data[8] & 0xFF),
				["SP"] = (ushort)(data[9] & 0xFFFF),
				// banks
				["ROM0 BANK"] = (ushort)(banks[0] & 0xFFFF),
				["ROMX BANK"] = (ushort)(banks[1] & 0xFFFF),
				["VRAM BANK"] = (ushort)(banks[4] & 0xFFFF),
				["SRAM BANK"] = (ushort)(banks[3] & 0xFFFF),
				["WRAM BANK"] = (ushort)(banks[2] & 0xFFFF),
			};
		}

		public void SetCpuRegister(string register, int value)
		{
			LibSameboy.sameboy_setreg(SameboyState, register.ToUpperInvariant() switch
			{
				"PC" => 0,
				"A" => 1,
				"F" => 2,
				"B" => 3,
				"C" => 4,
				"D" => 5,
				"E" => 6,
				"H" => 7,
				"L" => 8,
				"SP" => 9,
				_ => throw new InvalidOperationException("Invalid Register?"),
			},
			value);
		}

		public bool CanStep(StepType type) => false;

		[FeatureNotImplemented]
		public void Step(StepType type) => throw new NotImplementedException();

		public long TotalExecutedCycles => CycleCount;

		private const string systemBusScope = "System Bus";

		private readonly MemoryCallbackSystem _memorycallbacks = new MemoryCallbackSystem(new[] { systemBusScope });

		public IMemoryCallbackSystem MemoryCallbacks => _memorycallbacks;

		private LibSameboy.MemoryCallback _readcb;
		private LibSameboy.MemoryCallback _writecb;
		private LibSameboy.MemoryCallback _execcb;

		private void InitMemoryCallbacks()
		{
			LibSameboy.MemoryCallback CreateCallback(MemoryCallbackFlags flags, Func<bool> getHasCBOfType)
			{
				var rawFlags = (uint)flags;
				return (address, data) =>
				{
					if (getHasCBOfType())
					{
						MemoryCallbacks.CallMemoryCallbacks(address, data, rawFlags, systemBusScope);
					}
				};
			}

			_readcb = CreateCallback(MemoryCallbackFlags.AccessRead, () => MemoryCallbacks.HasReads);
			_writecb = CreateCallback(MemoryCallbackFlags.AccessWrite, () => MemoryCallbacks.HasWrites);
			_execcb = CreateCallback(MemoryCallbackFlags.AccessExecute, () => MemoryCallbacks.HasExecutes);

			_memorycallbacks.ActiveChanged += SetMemoryCallbacks;
		}

		private void SetMemoryCallbacks()
		{
			LibSameboy.sameboy_setmemorycallback(SameboyState, 0, MemoryCallbacks.HasReads ? _readcb : null);
			LibSameboy.sameboy_setmemorycallback(SameboyState, 1, MemoryCallbacks.HasWrites ? _writecb : null);
			LibSameboy.sameboy_setmemorycallback(SameboyState, 2, MemoryCallbacks.HasExecutes ? _execcb : null);
		}
	}
}
